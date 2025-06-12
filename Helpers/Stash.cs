using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using RegexCrafter.Helpers.Enums;
using RegexCrafter.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RegexCrafter.Helpers;

public class Stash : ICurrencyPlace, ICraftingPlace
{
    private const string LogName = "Stash";
    private readonly RegexCrafter _core;
    private Settings Settings => _core.Settings;
    private CancellationToken CancellationToken => _core.Cts.Token;
    public bool IsVisible => _core.GameController.Game.IngameState.IngameUi.StashElement.IsVisible;
    public string CurrentTabName => CurrentTab.Name;
    public IList<ServerStashTab> ServerStashTabs => _core.GameController.Game.IngameState.ServerData
        .PlayerStashTabs.OrderBy(x => x.VisibleIndex).ToList();
    public List<string> AllTabNames => ServerStashTabs
        .Where(x => x.TabType is InventoryTabType.Currency or InventoryTabType.Essence or InventoryTabType.Delirium
            or InventoryTabType.Normal
            or InventoryTabType.Premium or InventoryTabType.Quad).OrderBy(x => x.VisibleIndex).Select(x => x.Name)
        .ToList();

    public StashTab CurrentTab { get; }

    public Stash(RegexCrafter core)
    {
        _core = core;
        CurrentTab = new StashTab(_core);
    }

    public int GetIndexOfTab(string tabName)
    {
        var tab = ServerStashTabs.FirstOrDefault(x => x.Name == tabName);
        if (tab == null) return -1;
        return tab.VisibleIndex;
    }

    public async SyncTask<bool> SwitchTab(string tabName)
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

        return await SwitchToTabByIndexAsync(idxNeedles);
    }

    public async SyncTask<bool> SwitchToTabByIndexAsync(int idxNeedles)
    {
        if (!IsVisible)
        {
            GlobalLog.Error("Stash is not visible.", LogName);
            return false;
        }

        if (idxNeedles < 0 || idxNeedles >= ServerStashTabs.Count) return false;

        if (idxNeedles == CurrentTab.Index) return true;
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
            {
                for (var i = CurrentTab.Index - idxNeedles; i > 0; i--)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    await Input.SimulateKeyEvent(Keys.Left);
                    await Wait.LatencySleep();
                }
            }
            else
            {
                for (var i = idxNeedles - CurrentTab.Index; i > 0; i--)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    await Input.SimulateKeyEvent(Keys.Right);
                    await Wait.LatencySleep();
                }
            }
        }

        await Wait.SleepSafe(100, 200);
        await Wait.For(() => CurrentTab.Inventory != null, "Switch tab", 1000);
        return true;
    }

    public bool TabIsExist(string tabName)
    {
        return AllTabNames.Contains(tabName);
    }

    public async SyncTask<bool> SwitchToCraftTabAsync()
    {
        if (!IsVisible) return false;
        return await SwitchTab(Settings.TabSettings.WhereCraftItemTab);
    }

    public async SyncTask<bool> SwitchToCurrencyTabAsync()
    {
        if (!IsVisible) return false;
        return await SwitchTab(Settings.TabSettings.CurrencyTab);
    }

    public async SyncTask<bool> SwitchToDeliriumTabAsync()
    {
        if (!IsVisible) return false;
        return await SwitchTab(Settings.TabSettings.DeliriumTab);
    }

    public bool TryGetTabType(string tabName, out InventoryTabType type)
    {
        type = default;
        var tab = ServerStashTabs.FirstOrDefault(x => x.Name == tabName);
        if (tab == null) return false;
        type = tab.TabType;
        return true;
    }

    public Entity FindStashInWorld()
    {
        return _core.GameController.Entities.FirstOrDefault(x => x.Type == EntityType.Stash);
    }

    public int GetStashVisibleIndex(string tabName)
    {
        if (!AllTabNames.Contains(tabName)) return -1;
        return ServerStashTabs.First(x => x.Name == tabName).VisibleIndex;
    }
    #region ICurrencyPlace implementation
    public async SyncTask<bool> HasCurrencyAsync(string currency)
    {
        if (!await SwitchToCurrencyTabAsync())
        {
            GlobalLog.Error("Failed to switch to Currency tab.", LogName);
            return false;
        }
        return await CurrentTab.ContainsItemAsync(currency);
    }
    public bool HasCurrency(string currency)
    {
        return CurrentTab.ContainsItem(currency);
    }

    public async SyncTask<bool> TakeCurrencyForUseAsync(string currency)
    {
        if (currency.Contains("Delirium Orb"))
        {
            if (!await SwitchToDeliriumTabAsync())
            {
                GlobalLog.Error("Can't switch to delirium tab.", LogName);
                return false;
            }
        }
        else if (!await SwitchToCurrencyTabAsync())
        {
            GlobalLog.Error("Can't switch to currency tab.", LogName);
            return false;
        }
        if (CurrentTab.TryGetItem(item => item.BaseName.Contains(currency, System.StringComparison.CurrentCultureIgnoreCase), out var item))
            return await item.MoveAndTakeForUse();

        if (CurrentTab.TabType == InventoryType.CurrencyStash)
        {
            if (!await CurrentTab.SwitchCurrencyTab())
            {
                GlobalLog.Error("Can't switch Currency tab.", LogName);
                return false;
            }

            if (CurrentTab.TryGetItem(item => item.BaseName.Contains(currency, System.StringComparison.CurrentCultureIgnoreCase), out item))
                return await item.MoveAndTakeForUse();
        }

        GlobalLog.Error($"No {currency} found.", LogName);
        return false;
    }
    #endregion
    #region ICraftingPlace impl
    public bool SupportChainCraft { get; } = true;
    public async SyncTask<(bool Succes, List<InventoryItemData> Items)> TryGetUsedItemsAsync(Func<InventoryItemData, bool> conditionUse)
    {
        if (!await SwitchToCraftTabAsync()) return (false, []);
        if (CurrentTab.VisibleItems is null)
        {
            return (false, []);
        }
        return (true, CurrentTab.VisibleItems.Where(conditionUse).ToList());
    }
    public async SyncTask<bool> PrepareCraftingPlace()
    {
        return await SwitchToCraftTabAsync();
    }
    public bool CanCraft()
    {
        return IsVisible;
    }
    #endregion
}