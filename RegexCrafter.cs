using ExileCore;
using ExileCore.Shared;
using ImGuiNET;
using RegexCrafter.CraftsMethods;
using RegexCrafter.Helpers;
using RegexCrafter.Helpers.Enums;
using RegexCrafter.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Input = RegexCrafter.Helpers.Input;
using Vector2 = System.Numerics.Vector2;

namespace RegexCrafter;

public class RegexCrafter : BaseSettingsPlugin<Settings>
{
    private const string LogName = "RegexCrafter";
    private readonly List<ICrafting> _craftList = [];

    private readonly string[] _craftPlaces =
        Enum.GetValues(typeof(CraftPlaceType)).Cast<CraftPlaceType>().Select(x => x.GetDescription()).ToArray();

    private readonly string[] _currencyPlace = Enum.GetValues(typeof(CurrencyPlaceType)).Cast<CurrencyPlaceType>().Select(x => x.GetDescription()).ToArray();
    private int _craftPlaceIdx;
    private int _currentCraftIndex;
    private int _currencyPlaceIdx;
    public Stash Stash { get; set; }
    public Scripts Scripts { get; set; }
    public PlayerInventory PlayerInventory { get; set; }
    public MousePositionCrafting MousePositionCrafting { get; private set; }
    private SyncTask<bool> _currentOperation;
    public CancellationTokenSource Cts;
    public ICurrencyPlace CurrencyPlace => Settings.CurrencyPlace switch
    {
        CurrencyPlaceType.Inventory => PlayerInventory,
        CurrencyPlaceType.Stash => Stash,
        _ => throw new ArgumentException("Invalid currency place in settings")
    };
    public ICraftingPlace CraftingPlace => Settings.CraftPlace switch
    {
        CraftPlaceType.Inventory => PlayerInventory,
        CraftPlaceType.Stash => Stash,
        CraftPlaceType.MousePosition => MousePositionCrafting,
        _ => throw new ArgumentException("Invalid currency place in settings")
    };

    public override bool Initialise()
    {
        Name = "RegexCrafter";

        GlobalLog.Init(this);
        Stash = new Stash(this);
        PlayerInventory = new PlayerInventory(this);
        MousePositionCrafting = new MousePositionCrafting(this);
        Scripts = new Scripts(this);
        ElementHelper.Init(GameController.Game.IngameState.IngameUi.Cursor);
        Wait.Init(this);
        Input.Init(this);
        _craftPlaceIdx = (int)Settings.CraftPlace;
        _currencyPlaceIdx = (int)Settings.CurrencyPlace;
        _craftList.AddRange([new Map(this), new DefaultCraft(this), new CustomCraft(this)]);
        LogMessage("END override initialize");
        return base.Initialise();
    }

    public override void OnClose()
    {
        foreach (var craft in _craftList) craft.OnClose();
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

                var idx = tabs.IndexOf(prop.GetValue(Settings.TabSettings).ToString());
                if (ImGui.Combo(name, ref idx, tabs.ToArray(), tabs.Count))
                    prop.SetValue(Settings.TabSettings, tabs[idx]);
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
            ImGui.Combo("Craft Method", ref _currentCraftIndex, _craftList.Select(x => x.Name).ToArray(),
                _craftList.Count);
        }
        //var craftingLogic = (int)Settings.CraftingLogic;
        //var craftingLogicNames = Enum.GetNames(typeof(CraftingLogicType));
        //if (ImGui.Combo("Crafting Logic", ref craftingLogic, craftingLogicNames, craftingLogicNames.Length))
        //{
        //    Settings.CraftingLogic = (CraftingLogicType)craftingLogic;
        //}
        ImGui.Dummy(new Vector2(0, 10));
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 10));
        _craftList[_currentCraftIndex].DrawSettings();
    }

    public override void Render()
    {
        _craftList[_currentCraftIndex].Render();
        if (_currentOperation != null)
        {
            GlobalLog.Debug("Craft is running...", LogName);
            TaskUtils.RunOrRestart(ref _currentOperation, () => null);
        }
        if (Cts is { Token.IsCancellationRequested: false })
        {
            if (!Stash.IsVisible && !PlayerInventory.IsVisible)
            {
                Cts.Cancel();
                GlobalLog.Debug("Craft is canceled.", LogName);
                _ = Input.CleanKeys();
                Cts = null;
                _currentOperation = null;
            }

            if (ExileCore.Input.IsKeyDown(Settings.StopCraftHotKey.Value))
            {
                Cts.Cancel();
                GlobalLog.Debug("Craft is canceled.", LogName);
                _ = Input.CleanKeys();
                Cts = null;
                _currentOperation = null;
            }
        }

        if (ExileCore.Input.IsKeyDown(Settings.StartCraftHotKey.Value) && _currentOperation == null)
        {
            Cts = new CancellationTokenSource();
            try
            {
                _currentOperation = _craftList[_currentCraftIndex].StartCrafting();

            }
            catch (Exception e)
            {
                GlobalLog.Error(e.Message, LogName);
                _ = Input.CleanKeys();
            }
        }
    }
}