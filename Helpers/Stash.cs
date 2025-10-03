using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using RegexCrafter.Interface;

namespace RegexCrafter.Helpers;

public class Stash : ICurrencyPlace, ICraftingPlace
{
    private const string LogName = "Stash";
    private readonly RegexCrafter _core;

    public Stash(RegexCrafter core)
    {
        _core = core;
        CurrentTab = new StashTab(_core);
    }

    private IInput Input => _core.Input;
    private Settings Settings => _core.Settings;
    private CancellationToken CancellationToken => _core.Cts.Token;
    public bool IsVisible => _core.GameController.Game.IngameState.IngameUi.StashElement.IsVisible;
    public string CurrentTabName => CurrentTab.TabName;

    public IList<ServerStashTab> ServerStashTabs => _core.GameController.Game.IngameState.ServerData
        .PlayerStashTabs.OrderBy(x => x.VisibleIndex).ToList();

    public List<string> AllTabNames => ServerStashTabs
        .Where(x => x.TabType is InventoryTabType.Currency or InventoryTabType.Essence or InventoryTabType.Delirium
            or InventoryTabType.Normal
            or InventoryTabType.Premium or InventoryTabType.Quad).OrderBy(x => x.VisibleIndex).Select(x => x.Name)
        .ToList();

    public StashTab CurrentTab { get; }

    public int GetIndexOfTab(string tabName)
    {
        var tab = ServerStashTabs.FirstOrDefault(x => x.Name == tabName);
        if (tab == null) return -1;
        return tab.VisibleIndex;
    }

    public async SyncTask<bool> SwitchTab(string tabName, CancellationToken ct = default)
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

    public async SyncTask<bool> SwitchToTabByIndexAsync(int idxNeedles, CancellationToken ct = default)
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
                for (var i = CurrentTab.Index - idxNeedles; i > 0; i--)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    await Input.SimulateKeyEvent(Keys.Left, ct);
                    await _core.Wait.LatencySleep(ct);
                }
            else
                for (var i = idxNeedles - CurrentTab.Index; i > 0; i--)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    await Input.SimulateKeyEvent(Keys.Right, ct);
                    await _core.Wait.LatencySleep(ct);
                }
        }

        await _core.Wait.SleepSafe(100, 200, ct);
        await _core.Wait.For(() => CurrentTab.Inventory != null, "Switch tab", 1000, ct);
        return true;
    }

    public bool TabIsExist(string tabName)
    {
        return AllTabNames.Contains(tabName);
    }

    public async SyncTask<bool> SwitchToCraftTabAsync(CancellationToken ct = default)
    {
        if (!IsVisible) return false;
        return await SwitchTab(Settings.TabSettings.WhereCraftItemTab, ct);
    }

    public async SyncTask<bool> SwitchToCurrencyTabAsync(CancellationToken ct = default)
    {
        if (!IsVisible) return false;
        return await SwitchTab(Settings.TabSettings.CurrencyTab, ct);
    }

    public async SyncTask<bool> SwitchToDeliriumTabAsync(CancellationToken ct = default)
    {
        if (!IsVisible) return false;
        return await SwitchTab(Settings.TabSettings.DeliriumTab, ct);
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

    public async SyncTask<(bool, int)> TakeCurrencyForUseAsync(string currency)
    {
        if (currency.Contains("Delirium Orb"))
        {
            if (!await SwitchToDeliriumTabAsync())
            {
                GlobalLog.Error("Can't switch to delirium tab.", LogName);
                return (false, 0);
            }
        }
        else if (!await SwitchToCurrencyTabAsync())
        {
            GlobalLog.Error("Can't switch to currency tab.", LogName);
            return (false, 0);
        }

        var currencyItems = CurrentTab.VisibleItems
            .Where(x => x.BaseName.Contains(currency, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (currencyItems.Count == 0)
        {
            if (CurrentTab.TabType == InventoryType.CurrencyStash)
            {
                if (!await CurrentTab.SwitchCurrencyTab())
                {
                    GlobalLog.Error("Can't switch Currency tab.", LogName);
                    return (false, 0);
                }

                currencyItems = CurrentTab.VisibleItems
                    .Where(x => x.BaseName.Contains(currency, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (currencyItems.Count == 0)
                {
                    GlobalLog.Error($"No {currency} found in player inventory.", LogName);
                    return (false, 0);
                }
            }
            else
            {
                GlobalLog.Error($"No {currency} found in player inventory.", LogName);
                return (false, 0);
            }
        }

        var totalCount = currencyItems.Sum(x => x.StackSize);
        var item = currencyItems.First();
        if (item is null)
        {
            GlobalLog.Error($"No {currency} found in player inventory.", LogName);
            return (false, 0);
        }

        if (!await item.MoveAndTakeForUse())
        {
            GlobalLog.Error($"Failed to move {currency} for use.", LogName);
            return (false, 0);
        }

        return (true, totalCount);
    }

    #endregion

    #region ICraftingPlace impl

    public bool SupportChainCraft { get; } = true;

    public async SyncTask<(bool Success, List<InventoryItemData> Items)> TryGetItemsAsync(
        Func<InventoryItemData, bool> conditionUse)
    {
        if (!await SwitchToCraftTabAsync()) return (false, []);
        if (CurrentTab.VisibleItems is null) return (false, []);
        return (true, CurrentTab.VisibleItems.Where(conditionUse).ToList());
    }

    public SyncTask<(bool Success, List<InventoryItemData> Items)> TryGetItemsAsync()
    {
        return TryGetItemsAsync(_ => true);
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