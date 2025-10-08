using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using ExileCore.Shared;
using RegexCrafter.Enums;
using RegexCrafter.Interface;
using RegexCrafter.Models;
using Cursor = ExileCore.PoEMemory.MemoryObjects.Cursor;

namespace RegexCrafter.Helpers
{
    public class Scripts(RegexCrafter core)
    {
        private const string LogName = "Scripts";
        private IInput Input => core.Input;
        private Cursor Cursor => core.GameController.Game.IngameState.IngameUi.Cursor;
        private Wait Wait => core.Wait;
        private ICurrencyPlace CurrencyPlace => core.CurrencyPlace;
        private ICraftingPlace CraftingPlace => core.CraftingPlace;

        public async SyncTask<bool> UseCurrencyToSingleItem(InventoryItemData item, string currency,
            Func<InventoryItemData, CraftingAction> condition, CancellationToken ct = default)
        {
            try
            {
                var (success, count) = await CurrencyPlace.TakeCurrencyForUseAsync(currency);

                if (!success)
                {
                    GlobalLog.Error($"Failed to take {currency} for use.", LogName);
                    return false;
                }

                if (!await Input.KeyDown(Keys.LShiftKey, ct))
                {
                    return false;
                }
                return await ClickUntilCondition(item, condition, count, ct);
            }
            finally
            {
                await Input.KeyUp(Keys.LShiftKey, ct);
                await Input.KeyUp(Keys.LControlKey, ct);
            }
        }

        public async SyncTask<bool> UseCurrencyOnMultipleItems(string currencyUseName,
            Func<InventoryItemData, CraftingAction> condition,
            CancellationToken ct = default)
        {
            var (success, items) = await CraftingPlace.TryGetItemsAsync();
            if (!success)
            {
                return false;
            }
            if (items.Count == 0)
            {
                GlobalLog.Debug("No items found.", LogName);
                return false;
            }

            return await UseCurrencyOnMultipleItems(items, currencyUseName, condition, ct);
        }

        public async SyncTask<bool> UseCurrencyOnMultipleItems(IEnumerable<InventoryItemData> items, string currencyUseName,
            Func<InventoryItemData, CraftingAction> condition,
            CancellationToken ct = default)
        {
            GlobalLog.Debug($"Start using {currencyUseName} to items.", LogName);
            try
            {
                var (success, count) = await CurrencyPlace.TakeCurrencyForUseAsync(currencyUseName);

                if (!success)
                {
                    GlobalLog.Error($"Failed to take {currencyUseName} for use.", LogName);
                    return false;
                }

                if (!await Input.KeyDown(Keys.LShiftKey, ct))
                {
                    GlobalLog.Error("Failed to hold Shift key.", LogName);
                    return false;
                }

                if (!await CraftingPlace.PrepareCraftingPlace())
                {
                    return false;
                }

                foreach (var item in items)
                {
                    ct.ThrowIfCancellationRequested();

                    if (!await ClickUntilCondition(item, condition, count, ct))
                    {
                        GlobalLog.Error($"Can't apply {currencyUseName} to item.", LogName);
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                await Input.KeyUp(Keys.LShiftKey, ct);
                await Input.KeyUp(Keys.LControlKey, ct);
            }
        }

        private async SyncTask<bool> ClickUntilCondition(InventoryItemData item,
            Func<InventoryItemData, CraftingAction> condition,
            int maxCountClick = 3000, CancellationToken ct = default)
        {
            var countClick = 0;
            InventoryItemData oldHoverItem = null;
            GlobalLog.Debug($"Clicking on item {item} until condition is met.", LogName);
            while (CraftingPlace.CanCraft())
            {
                if (countClick >= maxCountClick)
                {
                    GlobalLog.Error($"Too many clicks. Max clicks: {maxCountClick}", LogName);
                    return false;
                }

                ct.ThrowIfCancellationRequested();

                var cursorGetClientRectCache = Cursor.GetClientRectCache;
                // Move the mouse to the item
                if (!item.GetClientRectCache.Contains(cursorGetClientRectCache.Center.X, cursorGetClientRectCache.Center.Y))
                {
                    GlobalLog.Debug($"Move mouse to item {item}.", LogName);
                    await item.MoveMouseToItem();
                }

                // Get the initial hovered item
                if (oldHoverItem == null)
                {
                    var (isSuccess, hoveredItem) = await WaitForHoveredItem(
                        hoverItem => hoverItem.Entity.Address == item.Entity.Address,
                        "Get the initial hovered item", ct);
                    if (!isSuccess)
                    {
                        GlobalLog.Error("No hovered item found!", LogName);
                        return false;
                    }

                    oldHoverItem = hoveredItem;
                }

                var craftingAction = condition(oldHoverItem);

                switch (craftingAction)
                {
                    case CraftingAction.Skip:
                        GlobalLog.Debug("Item is not valid for crafting, skipping click.", LogName);
                        return true;
                    // Check if the item meets the condition after applying currency
                    case CraftingAction.Complete:
                        GlobalLog.Info($"Item {item} meets the condition.", LogName);
                        return true;
                    case CraftingAction.Continue:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }


                // Apply the currency by clicking
                GlobalLog.Debug($"Click on item {item}.", LogName);
                await Input.Click(ct);

                // Wait for the item to update after applying currency
                GlobalLog.Debug("Wait for item update after applying currency.", LogName);
                var (isSuccessAfterClick, updatedHoveredItem) = await WaitForHoveredItem(
                    hoverItem => hoverItem.Entity.Address != oldHoverItem.Entity.Address,
                    "Apply currency to item", ct);

                if (!isSuccessAfterClick)
                {
                    GlobalLog.Error("No updated hovered item found!", LogName);
                    return false;
                }

                GlobalLog.Debug($"Updated hovered item: {updatedHoveredItem}.", LogName);
                oldHoverItem = updatedHoveredItem;
                countClick++;
            }

            GlobalLog.Debug("Currency is not available or crafting place cannot be used.", LogName);
            return false;
        }

        public async SyncTask<(bool isSuccess, InventoryItemData hoveredItem)> WaitForHoveredItem(
            Func<InventoryItemData, bool> predicate,
            string operationName, CancellationToken ct = default
        )
        {
            try
            {
                InventoryItemData hoveredItem = null;

                var isSuccess = await Wait.For(() =>
                {
                    if (!TryGetHoveredItem(out var hoverItem))
                    {
                        GlobalLog.Debug("[WaitForHoveredItem] No hovered item found.", LogName);
                        return false;
                    }

                    if (!predicate(hoverItem))
                    {
                        GlobalLog.Debug($"[WaitForHoveredItem] Hovered item does not match predicate: {hoverItem}.",
                            LogName);
                        return false;
                    }

                    hoveredItem = hoverItem;
                    return true;
                }, operationName, 500, ct);

                if (!isSuccess)
                {
                    return (false, hoveredItem);
                }
                if (!await Input.SimulateKeyEvent(Keys.C, Keys.LControlKey, ct))
                {
                    GlobalLog.Error("[WaitForHoveredItem] Failed to simulate Ctrl+C key event.", LogName);
                    return (false, hoveredItem);
                }

                await Wait.Sleep(50, ct);

                hoveredItem.ClipboardText = Clipboard.GetClipboardText();
                hoveredItem.ClipboardText = $"{hoveredItem.ClipboardText}explicit:{hoveredItem.ExplicitModsCount}\n";

                GlobalLog.Debug($"[WaitForHoveredItem] Clipboard text: {hoveredItem.ClipboardText}", LogName);

                return (true, hoveredItem);
            }
            finally
            {
                await Input.KeyUp(Keys.LControlKey, ct);
            }
        }

        public bool TryGetHoveredItem(out InventoryItemData item)
        {
            item = null;
            var uiHover = core.GameController.Game.IngameState.UIHoverTooltip;
            var tooltip = uiHover.Tooltip;
            var poeEntity = uiHover.Entity;
            if (tooltip == null || poeEntity == null)
            {
                GlobalLog.Debug("No hovered item found or tooltip is null.", LogName);
                return false;
            }

            if (poeEntity.Address == 0 && !uiHover.IsValid)
            {
                GlobalLog.Debug("Hovered item is not valid.", LogName);
                return false;
            }

            if (core.GameController.Files.BaseItemTypes.Translate(uiHover.Entity.Path) == null)
            {
                GlobalLog.Debug($"Base item type not found for path: {uiHover.Entity.Path}", LogName);
                return false;
            }

            item = new InventoryItemData(uiHover);
            return true;
        }
    }
}
