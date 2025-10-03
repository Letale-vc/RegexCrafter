using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ExileCore.Shared;
using ImGuiNET;
using Newtonsoft.Json;
using RegexCrafter.Helpers;
using RegexCrafter.Helpers.Enums;
using RegexCrafter.Interface;
using SharpDX;
using Encoding = System.Text.Encoding;
using Vector2 = System.Numerics.Vector2;

namespace RegexCrafter.CraftsMethods;

public class CraftState
{
    public Recipe Recipe { get; set; } = new();
}

public abstract class CraftBase<TState> : ICrafting where TState : CraftState, new()
{
    private const string LogName = "Craft";
    private readonly RegexCrafter _core;
    private readonly List<TState> _stateList;
    private string _importExportText = string.Empty;
    private int _stateIndex;

    protected CraftBase(RegexCrafter core)
    {
        _core = core;
        _stateList = GetFileState();
    }

    private Dictionary<long, InventoryItemData> DoneCraftItem { get; } = [];
    private Dictionary<long, InventoryItemData> NoValidItems { get; } = [];
    protected abstract TState CurrentState { get; set; }

    private Scripts Scripts
    {
        get => _core.Scripts;
    }

    private Stash Stash
    {
        get => _core.Stash;
    }

    private PlayerInventory PlayerInventory
    {
        get => _core.PlayerInventory;
    }

    protected Settings Settings
    {
        get => _core.Settings;
    }

    protected ICurrencyPlace CurrencyPlace
    {
        get => _core.CurrencyPlace;
    }

    private ICraftingPlace CraftingPlace
    {
        get => _core.CraftingPlace;
    }

    private string PathFileState
    {
        get => Path.Combine(_core.ConfigDirectory, Name);
    }

    public CancellationToken CancellationToken
    {
        get => _core.Cts.Token;
    }


    public abstract string Name { get; }

    public void Clean()
    {
        if (NoValidItems.Count != 0)
        {
            NoValidItems.Clear();
        }

        if (DoneCraftItem.Count != 0)
        {
            DoneCraftItem.Clear();
        }
    }

    public void Render()
    {
        if (!Stash.IsVisible && !PlayerInventory.IsVisible)
        {
            Clean();
            return;
        }

        foreach (var item in DoneCraftItem.Values)
        {
            _core.Graphics.DrawFrame(item.GetClientRectCache, Color.Green, 2);
        }

        foreach (var item in NoValidItems.Values)
        {
            _core.Graphics.DrawFrame(item.GetClientRectCache, Color.Red, 2);
        }
    }

    public virtual void DrawSettings()
    {
        ImGui.InputText("Import/export##ImportExportText", ref _importExportText, 10240);
        if (ImGui.Button("Import##Import"))
        {
            Import();
        }

        ImGui.SameLine();
        if (ImGui.Button("Export##Export"))
        {
            Export();
        }

        ImGui.Dummy(new Vector2(0, 20));
        var stateNameList = _stateList.Select(x => x.Recipe.Name).ToArray();
        ImGui.Combo("Saved crafts", ref _stateIndex, stateNameList, _stateList.Count);
        ImGui.SameLine();
        if (ImGui.Button("Load"))
        {
            CurrentState = _stateList[_stateIndex];
        }

        ImGui.SameLine();
        if (ImGui.Button("Remove"))
        {
            _stateList.RemoveAt(_stateIndex);
        }

        var nameTemp = CurrentState.Recipe.Name;
        if (ImGui.InputText("Recipe Name", ref nameTemp, 100))
        {
            CurrentState.Recipe.Name = nameTemp;
        }

        if (ImGui.Button("Save"))
        {
            UpdateLocalState(CurrentState);
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset"))
        {
            CurrentState = new TState();
        }

        ImGui.Dummy(new Vector2(0, 10));
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 10));
    }


    public void OnClose()
    {
        UpdateFileState();
    }

    public async SyncTask<bool> Start(CancellationToken ct = default)
    {
        BeforeStart();
        if (!PreCraftCheck())
        {
            return false;
        }


        List<string> onlyUseOneTimeCurrencies = [];
        while (true)
        {
            foreach (var step in CurrentState.Recipe.CraftSteps)
            {
                if (step.IsOneTimeUse && !onlyUseOneTimeCurrencies.Contains(step.Currency))
                {
                    onlyUseOneTimeCurrencies.Add(step.Currency);
                }
                else if (step.IsOneTimeUse && onlyUseOneTimeCurrencies.Contains(step.Currency))
                {
                    continue;
                }

                ct.ThrowIfCancellationRequested();

                var (success, items) = await CraftingPlace.TryGetItemsAsync();
                if (!success && items.Count == 0)
                {
                    return false;
                }

                var validCraftItems = items.Where(x =>
                    !DoneCraftItem.ContainsKey(x.Entity.Address) &&
                    !NoValidItems.ContainsKey(x.Entity.Address)).ToList();

                if (validCraftItems.Count == 0)
                {
                    return true;
                }

                if (!await Scripts.UseCurrencyOnMultipleItems(validCraftItems, step.Currency,
                        item =>
                        {
                            if (DoneCraftItem.ContainsKey(item.Entity.Address))
                            {
                                return CraftingAction.Skip;
                            }

                            if (!CurrentState.Recipe.IsBaseUseCondition(item.ClipboardText))
                            {
                                NoValidItems.TryAdd(item.Entity.Address, item);

                                return CraftingAction.Skip;
                            }

                            if (NoValidItems.ContainsKey(item.Entity.Address))
                            {
                                return CraftingAction.Skip;
                            }

                            if (!step.IsUseCondition(item.ClipboardText))
                            {
                                return CraftingAction.Skip;
                            }

                            if (CurrentState.Recipe.IsMainCondition(item.ClipboardText))
                            {
                                DoneCraftItem.TryAdd(item.Entity.Address, item);
                                return CraftingAction.Complete;
                            }

                            if (step.IsStopUseCondition(item.ClipboardText))
                            {
                                return CraftingAction.Complete;
                            }

                            return CraftingAction.Continue;
                        }
                        , ct
                    ))
                {
                    return false;
                }

                if (DoneCraftItem.Count + NoValidItems.Count == items.Count)
                {
                    break;
                }
            }

            await TaskUtils.NextFrame();
        }
    }

    private bool PreCraftCheck()
    {
        if (CurrentState.Recipe.MainConditions.Count == 0)
        {
            GlobalLog.Error("Regex pattern is empty.", LogName);
            return false;
        }

        if (CurrentState.Recipe.CraftSteps.Count == 0)
        {
            GlobalLog.Error("Craft steps are empty.", LogName);
            return false;
        }

        return true;
    }

    protected virtual void BeforeStart()
    {
        GlobalLog.Debug("It`s base BeforeStart method. No action", LogName);
    }

    private void Import()
    {
        var jsonStr = Encoding.UTF8.GetString(Convert.FromBase64String(_importExportText));
        CurrentState = JsonConvert.DeserializeObject<TState>(jsonStr);
    }

    private void Export()
    {
        var jsonStr = JsonConvert.SerializeObject(CurrentState);
        _importExportText = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonStr));
        Clipboard.SetClipboardText(_importExportText);
        GlobalLog.Info($"Copy to clipboard: {_importExportText}", LogName);
    }

    private void UpdateLocalState(TState state)
    {
        if (_stateList.Any(x => x.Recipe.Name == state.Recipe.Name))
        {
            var idx = _stateList.FindIndex(x => x.Recipe.Name == state.Recipe.Name);
            _stateList[idx] = state;
        }
        else
        {
            _stateList.Add(state);
        }

        UpdateFileState();
    }

    private void UpdateFileState()
    {
        File.WriteAllText(PathFileState, JsonConvert.SerializeObject(_stateList, Formatting.Indented));
        GlobalLog.Debug($"Update file: {PathFileState}.", LogName);
    }

    private void CreateFile()
    {
        if (Path.Exists(PathFileState))
        {
            return;
        }

        File.WriteAllText(PathFileState, JsonConvert.SerializeObject(new List<TState>(), Formatting.Indented));
        GlobalLog.Debug($"Created file: {PathFileState}.", LogName);
    }

    private bool TryLoadStateFile(out List<TState> list)
    {
        list = [];
        try
        {
            var content = File.ReadAllText(PathFileState);
            var deserialized = JsonConvert.DeserializeObject<List<TState>>(content);
            if (deserialized == null)
            {
                throw new Exception("Deserialized list is null");
            }
            list = deserialized;
            return true;
        }
        catch (Exception ex)
        {
            HandleCorruptedStateFile(ex);
            return false;
        }
    }

    private void HandleCorruptedStateFile(Exception ex)
    {
        GlobalLog.Error($"Failed to deserialize state file '{PathFileState}': {ex.Message}", LogName);
        TryBackupCorruptedFile();
        CreateFile();
    }

    private void TryBackupCorruptedFile()
    {
        try
        {
            if (!File.Exists(PathFileState))
            {
                return;
            }
            var backupPath = PathFileState + ".old";
            if (File.Exists(backupPath))
            {
                var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                backupPath = PathFileState + $".{ts}.old";
            }
            File.Move(PathFileState, backupPath);
            GlobalLog.Debug($"Corrupted state file renamed to: {backupPath}", LogName);
        }
        catch (Exception renameEx)
        {
            GlobalLog.Error($"Failed to rename corrupted state file: {renameEx.Message}", LogName);
        }
    }

    private List<TState> GetFileState()
    {
        if (Path.Exists(PathFileState))
        {
            return TryLoadStateFile(out var list) ? list : [];
        }

        CreateFile();
        return [];
    }
}
