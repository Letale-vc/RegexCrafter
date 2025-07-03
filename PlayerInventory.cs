
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using RegexCrafter.Helpers;
using RegexCrafter.Helpers.Enums;
using RegexCrafter.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RegexCrafter;

public class PlayerInventory : ICurrencyPlace, ICraftingPlace
{
    private readonly RegexCrafter _core;
    public bool IsVisible => _core.GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible;
    public List<InventoryItemData> Items =>
        _core.GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems.Select(x => new InventoryItemData(x)).ToList();

    public List<InventoryItemData> NonCorruptItems => [.. Items.Where(x => !x.IsCorrupted)];

    public bool SupportChainCraft { get; } = true;

    public List<InventoryItemData> GetConditionsItems(Func<InventoryItemData, bool> condition) => [.. Items.Where(condition)];

    public PlayerInventory(RegexCrafter core)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core), "PlayerInventory requires a valid RegexCrafter instance.");
        GlobalLog.Debug("PlayerInventory initialized.", "PlayerInventory");
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
        return SyncTask.FromResult(Items.Any(x => (x.BaseName.Contains(currency) || x.BaseName == currency)));
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

    public SyncTask<bool> TakeCurrencyForUseAsync(string currency)
    {
        if (string.IsNullOrEmpty(currency))
        {
            GlobalLog.Error("Currency name cannot be null or empty.", "PlayerInventory");
            return SyncTask.FromResult(false);
        }
        var item = Items.FirstOrDefault(x => x.BaseName.Contains(currency) || x.BaseName == currency);
        if (item is null)
        {
            GlobalLog.Error($"No {currency} found in player inventory.", "PlayerInventory");
            return SyncTask.FromResult(false);
        }
        return item.MoveAndTakeForUse();
    }

    #endregion
    #region ICraftingPlace Implementation
    public SyncTask<(bool Succes, List<InventoryItemData> Items)> TryGetUsedItemsAsync(Func<InventoryItemData, bool> conditionUse)
    {
        if (Items is null)
        {
            return SyncTask.FromResult<(bool Succes, List<InventoryItemData> Items)>((false, []));
        }

        return SyncTask.FromResult((true, Items.Where(conditionUse).ToList()));
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