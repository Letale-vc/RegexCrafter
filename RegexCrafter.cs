using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ExileCore;
using ExileCore.Shared;
using ImGuiNET;
using RegexCrafter.CraftsMethods;
using RegexCrafter.Helpers;
using RegexCrafter.Helpers.Enums;
using Input = RegexCrafter.Helpers.Input;
using Vector2 = System.Numerics.Vector2;

namespace RegexCrafter;

public class RegexCrafter : BaseSettingsPlugin<Settings>
{
    private const string LogName = "RegexCrafter";
    private readonly List<ICraft> _craftList = [];

    private readonly string[] _craftPlaces =
        Enum.GetValues(typeof(CraftPlaceType)).Cast<CraftPlaceType>().Select(x => x.GetDescription()).ToArray();

    private int _craftPlaceIdx;
    private int _currentCraftIndex;
    private SyncTask<bool> _currentOperation;
    public CancellationTokenSource Cts;

    public override bool Initialise()
    {
        Name = "RegexCrafter";

        GlobalLog.Init(this);
        ElementHelper.Init(this);
        CurrencyUseHelper.Init(this);
        InventoryItemData.Init(this);
        Stash.Init(this);
        PlayerInventory.Init(this);
        Scripts.Init(this);
        Wait.Init(this);
        Input.Init(this);

        _craftPlaceIdx = (int)Settings.CraftPlace;
        _craftList.AddRange([new Map(this), new DefaultCraft(this), new CustomCraft(this)]);
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

        if (ImGui.Combo("Craft Item Place", ref _craftPlaceIdx, _craftPlaces,
                _craftPlaces.Length)) Settings.CraftPlace = (CraftPlaceType)_craftPlaceIdx;

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
        if (_craftList.Count != 0)
            ImGui.Combo("Craft Method", ref _currentCraftIndex, _craftList.Select(x => x.Name).ToArray(),
                _craftList.Count);
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
            if (ExileCore.Input.IsKeyDown(Settings.StopCraftHotKey.Value) || !Stash.IsVisible ||
                !PlayerInventory.IsVisible)
            {
                Cts.Cancel();
                GlobalLog.Debug("Craft is canceled.", LogName);
                Cts.Cancel();
                Cts = null;
                _currentOperation = null;
            }

        if (ExileCore.Input.IsKeyDown(Settings.StartCraftHotKey.Value) && _currentOperation == null)
        {
            _craftList[_currentCraftIndex].Clean();
            if (_craftList[_currentCraftIndex].PreCraftCheck())
            {
                Cts = new CancellationTokenSource();
                try
                {
                    _currentOperation = _craftList[_currentCraftIndex].Start();
                }
                catch (Exception e)
                {
                    GlobalLog.Error(e.Message, LogName);
                    _ = Input.CleanKeys();
                }
            }
            else
            {
                GlobalLog.Error("Craft failed.", LogName);
            }
        }
    }
}