using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using RegexCrafter.Helpers;
using RegexCrafter.Interface;

namespace RegexCrafter;

public class PlayerInventory : ICurrencyPlace, ICraftingPlace
{
    private readonly RegexCrafter _core;

    public PlayerInventory(RegexCrafter core)
    {
        _core = core ??
                throw new ArgumentNullException(nameof(core),
                    "PlayerInventory requires a valid RegexCrafter instance.");
        GlobalLog.Debug("PlayerInventory initialized.", "PlayerInventory");
    }

    public bool IsVisible => _core.GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible;

    public List<InventoryItemData> Items =>
        _core.GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems
            .Select(x => new InventoryItemData(x)).ToList();

    public List<InventoryItemData> NonCorruptItems => [.. Items.Where(x => !x.IsCorrupted)];

    public bool SupportChainCraft { get; } = true;

    public List<InventoryItemData> GetConditionsItems(Func<InventoryItemData, bool> condition)
    {
        return [.. Items.Where(condition)];
    }

    #region ICurrencyPlace Implementation

    public SyncTask<bool> HasCurrencyAsync(string currency)
    {
        if (!IsVisible)
        {
            GlobalLog.Error("Player inventory is not visible.", "PlayerInventory");
            return SyncTask.FromResult(false);
        }

        if (string.IsNullOrEmpty(currency))
        {
            GlobalLog.Error("Currency name cannot be null or empty.", "PlayerInventory");
            return SyncTask.FromResult(false);
        }

        return SyncTask.FromResult(Items.Any(x => x.BaseName.Contains(currency) || x.BaseName == currency));
    }

    public bool HasCurrency(string currency)
    {
        if (!IsVisible)
        {
            GlobalLog.Error("Player inventory is not visible.", "PlayerInventory");
            return false;
        }

        if (Items.Count == 0)
        {
            GlobalLog.Error("Player inventory is empty.", "PlayerInventory");
            return false;
        }

        if (string.IsNullOrEmpty(currency))
        {
            GlobalLog.Error("Currency name cannot be null or empty.", "PlayerInventory");
            return false;
        }

        return Items.Any(x => x.BaseName.Contains(currency) || x.BaseName == currency);
    }

    public async SyncTask<(bool, int)> TakeCurrencyForUseAsync(string currency)
    {
        if (string.IsNullOrEmpty(currency))
        {
            GlobalLog.Error("Currency name cannot be null or empty.", "PlayerInventory");
            return (false, 0);
        }

        var currencyItems = Items.Where(x => x.BaseName.Contains(currency, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (currencyItems.Count == 0)
        {
            GlobalLog.Error($"No {currency} found in player inventory.", "PlayerInventory");
            return (false, 0);
        }

        var totalCount = currencyItems.Sum(x => x.StackSize);
        var item = currencyItems.First();
        if (item is null)
        {
            GlobalLog.Error($"No {currency} found in player inventory.", "PlayerInventory");
            return (false, 0);
        }

        if (!await item.MoveAndTakeForUse())
        {
            GlobalLog.Error($"Failed to move {currency} for use.", "PlayerInventory");
            return (false, 0);
        }

        return (true, totalCount);
    }

    #endregion

    #region ICraftingPlace Implementation

    public SyncTask<(bool Success, List<InventoryItemData> Items)> TryGetItemsAsync(
        Func<InventoryItemData, bool> conditionUse)
    {
        if (Items is null) return SyncTask.FromResult<(bool Succes, List<InventoryItemData> Items)>((false, []));

        return SyncTask.FromResult((true, Items.Where(conditionUse).ToList()));
    }

    public SyncTask<(bool Success, List<InventoryItemData> Items)> TryGetItemsAsync()
    {
        return TryGetItemsAsync(_ => true);
    }

    public SyncTask<bool> PrepareCraftingPlace()
    {
        return SyncTask.FromResult(true);
    }

    public bool CanCraft()
    {
        return IsVisible;
    }

    #endregion
}