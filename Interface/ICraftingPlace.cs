using ExileCore.Shared;
using RegexCrafter.Helpers.Enums;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RegexCrafter.Interface;

public interface ICraftingPlace
{
    bool SupportChainCraft { get; }
    SyncTask<(bool Success, List<InventoryItemData> Items)> TryGetItemsAsync(Func<InventoryItemData, bool> conditionUse);
    SyncTask<(bool Success, List<InventoryItemData> Items)> TryGetItemsAsync();
    SyncTask<bool> PrepareCraftingPlace();
    bool CanCraft();
    //bool CanCraft(Recipe recipe);
    //SyncTask<(bool Success, InventoryItemData Item)> CraftAsync(Recipe recipe, InventoryItemData item, CancellationToken ct = default);
    //SyncTask<List<(bool Success, InventoryItemData Item)>> ChainCraftAsync(Recipe recipe, List<InventoryItemData> itemsList, ChainCraftType craftType, CancellationToken ct = default);
}