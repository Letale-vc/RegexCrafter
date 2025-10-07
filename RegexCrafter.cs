using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ExileCore;
using ExileCore.Shared;
using ImGuiNET;
using RegexCrafter.Crafting;
using RegexCrafter.Enums;
using RegexCrafter.Helpers;
using RegexCrafter.Interface;
using RegexCrafter.Places;
using Vector2 = System.Numerics.Vector2;

namespace RegexCrafter;

public class RegexCrafter : BaseSettingsPlugin<Settings>
{
    private const string LogName = "RegexCrafter";
    private readonly List<ICrafting> _craftList = [];

    private readonly string[] _craftPlaces =
        Enum.GetValues(typeof(CraftPlaceType)).Cast<CraftPlaceType>().Select(x => x.GetDescription()).ToArray();

    private readonly string[] _currencyPlace = Enum.GetValues(typeof(CurrencyPlaceType)).Cast<CurrencyPlaceType>()
        .Select(x => x.GetDescription()).ToArray();

    private int _craftPlaceIdx;
    private int _currencyPlaceIdx;
    private int _currentCraftIndex;
    private SyncTask<bool> _currentOperation;
    public CancellationTokenSource Cts;
    public Stash Stash { get; set; }
    public Scripts Scripts { get; set; }
    public Wait Wait { get; set; }
    public IInput Input { get; set; }
    public PlayerInventory PlayerInventory { get; set; }
    public MousePositionCrafting MousePositionCrafting { get; private set; }

    public ICurrencyPlace CurrencyPlace
    {
        get => Settings.CurrencyPlace switch
        {
            CurrencyPlaceType.Inventory => PlayerInventory,
            CurrencyPlaceType.Stash => Stash,
            _ => throw new ArgumentException("Invalid currency place in settings")
        };
    }

    public ICraftingPlace CraftingPlace
    {
        get => Settings.CraftPlace switch
        {
            CraftPlaceType.Inventory => PlayerInventory,
            CraftPlaceType.Stash => Stash,
            CraftPlaceType.MousePosition => MousePositionCrafting,
            _ => throw new ArgumentException("Invalid currency place in settings")
        };
    }

    public override bool Initialise()
    {
        Name = "RegexCrafter";

        GlobalLog.Init(this);
        Wait = new Wait(GameController);
        Stash = new Stash(this);
        PlayerInventory = new PlayerInventory(this);
        MousePositionCrafting = new MousePositionCrafting(this);
        Scripts = new Scripts(this);
        Input = new InputWithHumanizer(this);
        ElementHelper.Init(GameController.Game.IngameState.IngameUi.Cursor, Input, Wait);
        _craftPlaceIdx = (int)Settings.CraftPlace;
        _currencyPlaceIdx = (int)Settings.CurrencyPlace;
        _craftList.AddRange([new DefaultCraft(this), new Map(this), new CustomCraft(this)]);
        LogMessage("[RegexCrafter] END override initialize");
        return base.Initialise();
    }

    public override void OnClose()
    {
        foreach (var craft in _craftList)
        {
            craft.OnClose();
        }

        try
        {
            Clipboard.Instance?.Dispose();
        }
        catch
        {
        }
    }

    public override void DrawSettings()
    {
        base.DrawSettings();

        ImGui.Dummy(new Vector2(0, 10));
        var currCraftingPlace = (int)Settings.CraftPlace;
        var craftingPlaceNames = Enum.GetNames(typeof(CraftPlaceType));
        if (ImGui.Combo("Craft Item Place", ref currCraftingPlace, craftingPlaceNames,
                craftingPlaceNames.Length))
        {
            Settings.CraftPlace = (CraftPlaceType)currCraftingPlace;
        }

        if (ImGui.Combo("Currency place", ref _currencyPlaceIdx, _currencyPlace, _currencyPlace.Length))
        {
            Settings.CurrencyPlace = (CurrencyPlaceType)_currencyPlaceIdx;
        }

        ImGui.Dummy(new Vector2(0, 10));
        if (ImGui.CollapsingHeader("Tab settings", ImGuiTreeNodeFlags.None))
        {
            var tabs = Stash.AllTabNames;
            foreach (var prop in Settings.TabSettings.GetType().GetProperties())
            {
                ImGui.Spacing();
                var name = prop.Name;

                var idx = tabs.IndexOf(prop.GetValue(Settings.TabSettings)?.ToString());
                if (ImGui.Combo(name, ref idx, tabs.ToArray(), tabs.Count))
                {
                    prop.SetValue(Settings.TabSettings, tabs[idx]);
                }
            }

            ImGui.Dummy(new Vector2(0, 10));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 10));
        }

        ImGui.Dummy(new Vector2(0, 10));
        if (_craftList.Count == 0)
        {
            ImGui.Text("No craft methods available.");
            return;
        }

        if (_craftList.Count != 0)
        {
            _ = ImGui.Combo("Craft Method", ref _currentCraftIndex, _craftList.Select(x => x.Name).ToArray(), _craftList.Count);
        }


        ImGui.Dummy(new Vector2(0, 10));
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 10));
        _craftList[_currentCraftIndex].DrawSettings();
    }

    public override void Render()
    {
        if (_craftList.Count != 0)
        {
            _craftList[_currentCraftIndex].Render();
        }

        if (_currentOperation != null)
        {
            GlobalLog.Debug("Craft is running...", LogName);
            _ = TaskUtils.RunOrRestart(ref _currentOperation, () => null);
        }

        if (Cts is { Token.IsCancellationRequested: false })
        {
            if (!Stash.IsVisible && !PlayerInventory.IsVisible)
            {
                CancelCraft();
            }

            if (ExileCore.Input.IsKeyDown(Settings.StopCraftHotKey.Value.Key))
            {
                CancelCraft();
            }
        }

        if (ExileCore.Input.IsKeyDown(Settings.StartCraftHotKey.Value.Key) && _currentOperation == null)
        {
            Cts = new CancellationTokenSource();
            try
            {
                _currentOperation = _craftList[_currentCraftIndex].Start(Cts.Token);
            }
            catch (Exception e)
            {
                GlobalLog.Error(e.Message, LogName);
            }
            finally
            {
                Input.CleanKeys();
                Clipboard.Instance?.Dispose();
            }
        }
    }

    private void CancelCraft()
    {
        GlobalLog.Debug("Craft is canceled.", LogName);
        Cts?.Cancel();
        Input.CleanKeys();
        try
        {
            Clipboard.Instance?.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception e)
        {
            GlobalLog.Error(e.Message, LogName);
        }

        Cts = null;
        _currentOperation = null;
    }
}
