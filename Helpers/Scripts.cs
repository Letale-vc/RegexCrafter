using ExileCore.Shared;
using RegexCrafter.Helpers.Enums;
using RegexCrafter.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Cursor = ExileCore.PoEMemory.MemoryObjects.Cursor;

namespace RegexCrafter.Helpers;

public class Scripts(RegexCrafter _core)
{
    private const string LogName = "Scripts";
    private Cursor Cursor => _core.GameController.Game.IngameState.IngameUi.Cursor;
    private CancellationToken CancellationToken => _core.Cts.Token;
    private ICurrencyPlace CurrencyPlace => _core.CurrencyPlace;
    private ICraftingPlace CraftingPlace => _core.CraftingPlace;
    public async SyncTask<bool> UseCurrencyToSingleItem(InventoryItemData item, string currency,
        Func<InventoryItemData, bool> condition)
    {
        try
        {
            if (!await CurrencyPlace.TakeCurrencyForUseAsync(currency))
            {
                GlobalLog.Error($"Failed to take {currency} for use.", LogName);
                return false;
            }
            if (!await Input.SimulateKeyEvent(Keys.LShiftKey, true, false)) return false;
            return await ClickUntilCondition(item, currency, condition);
        }
        finally
        {
            await Input.SimulateKeyEvent(Keys.LShiftKey);
            await Input.SimulateKeyEvent(Keys.LControlKey);
        }
    }

    public async SyncTask<bool> UseCurrencyOnMultipleItems(string currency,
        Func<InventoryItemData, bool> conditionUseCurr, Func<InventoryItemData, bool> condition)
    {
        var (Succes, Items) = await CraftingPlace.TryGetUsedItemsAsync(conditionUseCurr);
        if (!Succes) return false;
        if (Items.Count == 0)
        {
            GlobalLog.Debug("No items found.", LogName);
            return true;
        }
        return await UseCurrencyOnMultipleItems(Items, currency, condition);
    }

    public async SyncTask<bool> UseCurrencyOnMultipleItems(IEnumerable<InventoryItemData> items, string currency,
        Func<InventoryItemData, bool> condition)
    {
        GlobalLog.Debug($"Start using {currency} to items.", LogName);
        try
        {
            if (!await CurrencyPlace.TakeCurrencyForUseAsync(currency))
            {
                GlobalLog.Error($"Failed to take {currency} for use.", LogName);
                return false;
            }
            if (!await Input.SimulateKeyEvent(Keys.LShiftKey, true, false))
            {
                GlobalLog.Error("Failed to hold Shift key.", LogName);
                return false;
            }
            if (!await CraftingPlace.PrepareCraftingPlace()) return false;

            foreach (var item in items)
            {
                CancellationToken.ThrowIfCancellationRequested();

                if (!await ClickUntilCondition(item, currency, condition))
                {
                    GlobalLog.Error($"Can't apply {currency} to item.", LogName);
                    return false;
                }
            }

            return true;
        }
        finally
        {
            await Input.SimulateKeyEvent(Keys.LShiftKey, false, true);
            await Input.SimulateKeyEvent(Keys.LControlKey, false, true);
        }
    }

    private async SyncTask<bool> ClickUntilCondition(InventoryItemData item, string currencyUseName,
        Func<InventoryItemData, bool> condition, int maxCountClick = 3000, CancellationToken ct = default)
    {
        var countClick = 0;
        InventoryItemData itemHoverTemp = null;
        GlobalLog.Debug($"Clicking on item {item} until condition is met.", LogName);
        while (CraftingPlace.CanCraft())
        {
            if (_core.Settings.CraftPlace != CraftPlaceType.Stash && !CurrencyPlace.HasCurrency(currencyUseName))
            {
                GlobalLog.Error($"Currency {currencyUseName} is not available.", LogName);
                return false;
            }

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
            if (itemHoverTemp == null)
            {
                var (isSuccess, hoveredItem) = await WaitForHoveredItem(
                    hoverItem => hoverItem.Entity.Address == item.Entity.Address,
                    "Get the initial hovered item");
                if (!isSuccess)
                {
                    GlobalLog.Error("No hovered item found!", LogName);
                    return false;
                }
                itemHoverTemp = hoveredItem;
            }


            // Check if the item meets the condition after applying currency
            if (condition(itemHoverTemp))
            {
                GlobalLog.Info($"Item {item} meets the condition.", LogName);
                return true;
            }

            // Apply the currency by clicking
            GlobalLog.Debug($"Click on item {item}.", LogName);
            await Input.Click();

            // Wait for the item to update after applying currency
            GlobalLog.Debug("Wait for item update after applying currency.", LogName);
            var (isSuccessAfterClick, updatedHoveredItem) = await WaitForHoveredItem(
                hoverItem => hoverItem.Entity.Address != itemHoverTemp.Entity.Address,
                "Apply currency to item");

            if (!isSuccessAfterClick)
            {
                GlobalLog.Error("No updated hovered item found!", LogName);
                return false;
            }

            GlobalLog.Debug($"Updated hovered item: {updatedHoveredItem}.", LogName);
            itemHoverTemp = updatedHoveredItem;
            countClick++;
        }
        GlobalLog.Debug($"Currency {currencyUseName} is not available or crafting place cannot be used.", LogName);
        return false;
    }

    public async SyncTask<(bool isSuccess, InventoryItemData hoveredItem)> WaitForHoveredItem(
        Func<InventoryItemData, bool> predicate,
        string operationName
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
                    GlobalLog.Debug($"[WaitForHoveredItem] Hovered item does not match predicate: {hoverItem}.", LogName);
                    return false;
                }
                hoveredItem = hoverItem;
                return true;
            }, operationName, 500);

            if (!isSuccess) return (false, hoveredItem);
            if (!await Input.SimulateKeyEvent(Keys.C, true, true, Keys.LControlKey))
            {
                GlobalLog.Error("[WaitForHoveredItem] Failed to simulate Ctrl+C key event.", LogName);
                return (false, hoveredItem);
            }
            hoveredItem.ClipboardText = Clipboard.GetClipboardText();
            hoveredItem.ClipboardText = $"{hoveredItem.ClipboardText}explicit:{hoveredItem.ExplicitModsCount}\n";

            GlobalLog.Debug($"[WaitForHoveredItem] Clipboard text: {hoveredItem.ClipboardText}", LogName);

            return (true, hoveredItem);
        }
        finally
        {
            await Input.SimulateKeyEvent(Keys.LControlKey, false, true);
        }
    }

    public bool TryGetHoveredItem(out InventoryItemData item)
    {
        item = null;
        var uiHover = _core.GameController.Game.IngameState.UIHoverTooltip;
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
        if (_core.GameController.Files.BaseItemTypes.Translate(uiHover.Entity.Path) == null)
        {
            GlobalLog.Debug($"Base item type not found for path: {uiHover.Entity.Path}", LogName);
            return false;
        }

        item = new InventoryItemData(uiHover);
        return true;
    }
}