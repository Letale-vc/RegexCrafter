using ExileCore;
using ExileCore.Shared;
using ImGuiNET;
using Newtonsoft.Json;
using RegexCrafter.Helpers;
using RegexCrafter.Interface;
using SharpDX;
using SharpDX.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Vector2 = System.Numerics.Vector2;

namespace RegexCrafter.CraftsMethods;

public class CraftState : ICloneable
{
    public readonly List<string> RegexPatterns = [];
    public string Name = string.Empty;
    public object Clone()
    {
        return MemberwiseClone();
    }
}



public abstract class CraftBase<TState> : ICrafting where TState : CraftState, new()
{
    private const string LogName = "Craft";

    protected readonly List<InventoryItemData> BadItems = [];
    protected readonly List<InventoryItemData> DoneCraftItem = [];
    protected readonly List<TState> StateList;
    private string _importExportText = string.Empty;
    private int _stateIndex;
    private readonly RegexCrafter Core;
    protected Scripts Scripts => Core.Scripts;
    protected readonly CurrencyUseHelper CurrencyUseHelper;
    protected Stash Stash => Core.Stash;
    protected PlayerInventory PlayerInventory => Core.PlayerInventory;
    protected Settings Settings => Core.Settings;
    protected ICurrencyPlace CurrencyPlace => Core.CurrencyPlace;
    protected ICraftingPlace CraftingPlace => Core.CraftingPlace;

    protected CraftBase(RegexCrafter core)
    {
        Core = core;
        StateList = GetFileState();
        CurrencyUseHelper = new CurrencyUseHelper(Scripts);
    }

    public string PathFileState => Path.Combine(Core.ConfigDirectory, Name);
    public CancellationToken CancellationToken => Core.Cts.Token;
    public abstract TState CraftState { get; set; }
    public abstract string Name { get; }

    public void Clean()
    {
        if (BadItems.Count != 0)
            BadItems.Clear();

        if (DoneCraftItem.Count != 0)
            DoneCraftItem.Clear();
    }

    public void Render()
    {
        if (!Stash.IsVisible && !PlayerInventory.IsVisible)
        {
            Clean();
            return;
        }

        foreach (var item in DoneCraftItem) Core.Graphics.DrawFrame(item.GetClientRectCache, Color.Green, 2);
        foreach (var item in BadItems) Core.Graphics.DrawFrame(item.GetClientRectCache, Color.Red, 2);
    }

    public virtual void DrawSettings()
    {
        ImGui.InputText("Import/export##ImportExportText", ref _importExportText, 10240);
        if (ImGui.Button("Import##ImportState")) Import();
        ImGui.SameLine();
        if (ImGui.Button("Export##ExportState")) Export();
        ImGui.Dummy(new Vector2(0, 20));
        var stateNameList = StateList.Select(x => x.Name).ToArray();
        _ = ImGui.Combo("States", ref _stateIndex, stateNameList, StateList.Count);
        ImGui.SameLine();
        if (ImGui.Button("Load State")) CraftState = (TState)StateList[_stateIndex].Clone();
        ImGui.SameLine();
        if (ImGui.Button("Remove State")) StateList.RemoveAt(_stateIndex);
        _ = ImGui.InputText("State Name", ref CraftState.Name, 100);
        if (ImGui.Button("Save Current State")) UpdateLocalState(CraftState);
        ImGui.SameLine();
        if (ImGui.Button("Reset State")) CraftState = new TState();
        ImGui.Dummy(new Vector2(0, 10));
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 10));

        // ImGui.Separator();
        // if (CraftState.RegexPatterns.Count == 0) CraftState.RegexPatterns.Add(string.Empty);
        //
        // ImGui.Dummy(new Vector2(0,20));
        // for (var i = 0; i < CraftState.RegexPatterns.Count; i++)
        // {
        //     var patternTemp = CraftState.RegexPatterns[i];
        //     if (ImGui.InputText($"Your regex pattern {i}", ref patternTemp, 1024))
        //         CraftState.RegexPatterns[i] = patternTemp;
        //     ImGui.SameLine();
        //     if (!ImGui.Button($"Remove##{i}")) continue;
        //     GlobalLog.Debug($"Remove pattern:{CraftState.RegexPatterns[i]}.", LogName);
        //     CraftState.RegexPatterns.RemoveAt(i);
        //     //tempPatternList.Add(i);
        // }
        // if (ImGui.Button("Add Regex Pattern")) CraftState.RegexPatterns.Add(string.Empty);
    }

    public bool PreCraftCheck()
    {
        if (CraftState.RegexPatterns.Count == 0)
        {
            GlobalLog.Error("Regex pattern is empty.", LogName);
            return false;
        }

        if (CraftState.RegexPatterns.Any(string.IsNullOrEmpty))
        {
            GlobalLog.Error("Regex pattern is empty.", LogName);
            return false;
        }

        return true;
    }

    protected abstract SyncTask<bool> Start();
    public SyncTask<bool> StartCrafting()
    {
        Clean();
        if (!PreCraftCheck())
            return new SyncTask<bool>(false);
        GlobalLog.Debug($"Start crafting: {Name}.", LogName);
        return Start();
    }

    public void OnClose()
    {
        UpdateFileState();
    }

    public void Import()
    {
        var jsonStr = Encoding.UTF8.GetString(Convert.FromBase64String(_importExportText));
        CraftState = JsonConvert.DeserializeObject<TState>(jsonStr);
    }

    public void Export()
    {
        var jsonStr = JsonConvert.SerializeObject(CraftState);
        _importExportText = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonStr));
        Clipboard.SetClipboardText(_importExportText);
        GlobalLog.Info($"Copy to clipboard: {_importExportText}", LogName);
    }

    public void UpdateLocalState(TState state)
    {
        if (StateList.Any(x => x.Name == state.Name))
        {
            var idx = StateList.FindIndex(x => x.Name == state.Name);
            StateList[idx] = state;
        }
        else
        {
            StateList.Add(state);
        }

        UpdateFileState();
    }

    public void UpdateFileState()
    {
        File.WriteAllText(PathFileState, JsonConvert.SerializeObject(StateList, Formatting.Indented));
        GlobalLog.Debug($"Update file: {PathFileState}.", LogName);
    }

    private void CreateFile()
    {
        if (Path.Exists(PathFileState)) return;

        File.WriteAllText(PathFileState, JsonConvert.SerializeObject(new List<TState>(), Formatting.Indented));
        GlobalLog.Debug($"Created file: {PathFileState}.", LogName);
    }

    private List<TState> GetFileState()
    {
        if (Path.Exists(PathFileState))
            return JsonConvert.DeserializeObject<List<TState>>(File.ReadAllText(PathFileState));
        CreateFile();
        return [];
    }

    public bool RegexCondition(InventoryItemData item)
    {
        if (string.IsNullOrEmpty(item.ClipboardText)) return false;
        foreach (var pattern in CraftState.RegexPatterns)
        {
            var (exclude, include, maxIncludeOnlyOne) = RegexFinder.ParsePattern(pattern);

            var excludeResult = RegexFinder.ContainsAnyPattern(item.ClipboardText, exclude, out var foundPatterns);
            GlobalLog.Info(
                $"Excluded: need find {foundPatterns.Count}/{exclude.Count} \n Found excluded patterns: [{string.Join(", ", foundPatterns)}]",
                LogName);
            if (excludeResult) continue;

            RegexFinder.ContainsAnyPattern(item.ClipboardText, maxIncludeOnlyOne, out var foundPatterns2);
            if (foundPatterns2.Count > 1)
            {
                GlobalLog.Info(
                    $"Include Only one: need find {foundPatterns2.Count}/1 \n Found excluded patterns: [{string.Join(", ", foundPatterns2)}]",
                    LogName);
                continue;
            }

            var includeResult = RegexFinder.ContainsAllPatterns(item.ClipboardText, include, out var foundPatterns3);

            if (!includeResult)
            {
                GlobalLog.Info(
                    $"Include: need find {foundPatterns3.Count}/{include.Count} \n Found include patterns: [{string.Join(", ", foundPatterns3)}]",
                    LogName);
                continue;
            }

            DoneCraftItem.Add(item);
            return true;
        }

        return false;
    }

    public override string ToString()
    {
        return $"{Name}";
    }
}