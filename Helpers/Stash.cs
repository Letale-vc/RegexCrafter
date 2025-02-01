using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;

namespace RegexCrafter.Helpers;

public static class Stash
{
    private const string LogName = "Stash";
    private static RegexCrafter _core;
    private static Settings Settings => _core.Settings;
    private static CancellationToken CancellationToken => _core.Cts.Token;
    public static bool IsVisible => _core.GameController.Game.IngameState.IngameUi.StashElement.IsVisible;

    public static string CurrentTabName => CurrentTab.Name;

    public static IList<ServerStashTab> ServerStashTabs => _core.GameController.Game.IngameState.ServerData
        .PlayerStashTabs.OrderBy(x => x.VisibleIndex).ToList();

    public static List<string> AllTabNames => ServerStashTabs
        .Where(x => x.TabType is InventoryTabType.Currency or InventoryTabType.Essence or InventoryTabType.Delirium
            or InventoryTabType.Normal
            or InventoryTabType.Premium or InventoryTabType.Quad).OrderBy(x => x.VisibleIndex).Select(x => x.Name)
        .ToList();

    public static StashTab CurrentTab { get; private set; }

    public static void Init(RegexCrafter core)
    {
        _core = core;
        CurrentTab = new StashTab(_core);
    }

    public static int GetIndexOfTab(string tabName)
    {
        var tab = ServerStashTabs.FirstOrDefault(x => x.Name == tabName);
        if (tab == null) return -1;
        return tab.VisibleIndex;
    }


    public static async SyncTask<bool> SwitchTab(string tabName)
    {
        if (!IsVisible)
        {
            GlobalLog.Error("Stash is not visible.", LogName);
            return false;
        }

        if (CurrentTabName == tabName) return true;

        if (!AllTabNames.Contains(tabName))
        {
            GlobalLog.Error($"Tab {tabName} not found.", LogName);
            return false;
        }

        var idxNeedles = GetStashVisibleIndex(tabName);

        return await SearchAndMoveToTab(idxNeedles);
    }

    public static async SyncTask<bool> SwitchTab(int idxNeedles)
    {
        if (!IsVisible)
        {
            GlobalLog.Error("Stash is not visible.", LogName);
            return false;
        }

        if (idxNeedles < 0 || idxNeedles >= ServerStashTabs.Count) return false;

        if (idxNeedles == CurrentTab.Index) return true;
        return await SearchAndMoveToTab(idxNeedles);
    }

    private static async SyncTask<bool> SearchAndMoveToTab(int idxNeedles)
    {
        while (idxNeedles != CurrentTab.Index)
        {
            if (!IsVisible)
            {
                GlobalLog.Error("Stash is not visible.", LogName);
                return false;
            }

            CancellationToken.ThrowIfCancellationRequested();

            var leftOrRight = idxNeedles < CurrentTab.Index ? 0 : 1;

            if (leftOrRight == 0)
                for (var i = CurrentTab.Index - idxNeedles; i > 0; i--)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    await Input.SimulateKeyEvent(Keys.Left);
                    await Wait.LatencySleep();
                }
            else
                for (var i = idxNeedles - CurrentTab.Index; i > 0; i--)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    await Input.SimulateKeyEvent(Keys.Right);
                    await Wait.LatencySleep();
                }
        }

        await Wait.SleepSafe(100, 200);
        await Wait.For(() => CurrentTab.Inventory != null, "Switch tab", 1000);
        return true;
    }

    public static bool TabIsExist(string tabName)
    {
        return AllTabNames.Contains(tabName);
    }

    public static async SyncTask<bool> SwitchToCraftTab()
    {
        if (!IsVisible) return false;
        return await SwitchTab(Settings.TabSettings.WhereCraftItemTab);
    }

    // public static async SyncTask<bool> SwitchToDoneTab()
    // {
    //     if (!IsVisible) return false;
    //     return await SwitchTab(Settings.TabSettings.WhereDoneItemTab);
    // }

    public static async SyncTask<bool> SwitchToCurrencyTab()
    {
        if (!IsVisible) return false;
        return await SwitchTab(Settings.TabSettings.CurrencyTab);
    }

    public static async SyncTask<bool> SwitchToDeliriumTab()
    {
        if (!IsVisible) return false;
        return await SwitchTab(Settings.TabSettings.DeliriumTab);
    }

    public static bool TryGetTabType(string tabName, out InventoryTabType type)
    {
        type = default;
        var tab = ServerStashTabs.FirstOrDefault(x => x.Name == tabName);
        if (tab == null) return false;
        type = tab.TabType;
        return true;
    }

    public static Entity FindStashInWorld()
    {
        return _core.GameController.Entities.FirstOrDefault(x => x.Type == EntityType.Stash);
    }

    public static int GetStashVisibleIndex(string tabName)
    {
        if (!AllTabNames.Contains(tabName)) return -1;
        return ServerStashTabs.First(x => x.Name == tabName).VisibleIndex;
    }
}