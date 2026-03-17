using ExileCore.Shared;
using RegexCrafter.Models;
using System.Collections.Generic;

namespace RegexCrafter.Interface
{
    public readonly record struct GetItemsResult(bool Success, IReadOnlyList<ItemData> Item);
    public interface ICraftingPlace
    {
        bool SupportChainCraft { get; }
        SyncTask<GetItemsResult> GetItemsAsync();
        SyncTask<bool> PrepareCraftingPlace();
        bool CanCraft();
        //bool CanCraft(Recipe recipe);
        //SyncTask<(bool Success, InventoryItemData Item)> CraftAsync(Recipe recipe, InventoryItemData item, CancellationToken ct = default);
        //SyncTask<List<(bool Success, InventoryItemData Item)>> ChainCraftAsync(Recipe recipe, List<InventoryItemData> itemsList, ChainCraftType craftType, CancellationToken ct = default);
    }
}
