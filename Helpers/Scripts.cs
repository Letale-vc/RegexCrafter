using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ExileCore.Shared;
using RegexCrafter.Helpers.Enums;
using Cursor = ExileCore.PoEMemory.MemoryObjects.Cursor;

namespace RegexCrafter.Helpers;

public static class Scripts
{
    private const string LogName = "Scripts";
    private static RegexCrafter _core;
    private static string _currentCurrencyUse;
    private static Settings Settings => _core.Settings;
    private static Cursor Cursor => _core.GameController.Game.IngameState.IngameUi.Cursor;
    private static CancellationToken CancellationToken => _core.Cts.Token;

    public static void Init(RegexCrafter core)
    {
        _core = core;
    }

    public static async SyncTask<bool> UseCurrencyToSingleItem(InventoryItemData item, string currency,
        Func<InventoryItemData, bool> condition)
    {
        try
        {
            if (!await CurrencyUseHelper.TakeForUse(currency)) return false;
            if (!await Input.SimulateKeyEvent(Keys.LShiftKey, true, false)) return false;
            return await ClickUntilCondition(item, condition);
        }
        finally
        {
            await Input.SimulateKeyEvent(Keys.LShiftKey, false);
        }
    }


    public static async SyncTask<List<InventoryItemData>> TryGetUsedItems(Func<InventoryItemData, bool> conditionUse)
    {
        List<InventoryItemData> items = null;
        GlobalLog.Debug("Getting used items.", LogName);
        if (Settings.CraftPlace == CraftPlaceType.Stash)
        {
            GlobalLog.Debug("Trying to switch to craft tab.", LogName);
            if (!await Stash.SwitchToCraftTab()) return items;
        }

        GlobalLog.Debug("Filtered items using condition.", LogName);

        items = Settings.CraftPlace == CraftPlaceType.Inventory
            ? PlayerInventory.Items.Where(conditionUse).ToList()
            : Stash.CurrentTab.VisibleItems.Where(conditionUse).ToList();
        return items;
    }


    public static async SyncTask<bool> UseCurrencyOnMultipleItems(string currency,
        Func<InventoryItemData, bool> conditionUseCurr, Func<InventoryItemData, bool> condition)
    {
        try
        {
            var items = await TryGetUsedItems(conditionUseCurr);
            if (items == null) return false;
            if (items.Count == 0)
            {
                GlobalLog.Debug("No items found.", LogName);
                return true;
            }

            if (currency.Contains("Delirium Orb"))
            {
                if (!await Stash.SwitchToDeliriumTab())
                {
                    GlobalLog.Error("Can't switch to delirium tab.", LogName);
                    return false;
                }
            }
            else if (!await Stash.SwitchToCurrencyTab())
            {
                GlobalLog.Error("Can't switch to currency tab.", LogName);
                return false;
            }

            if (!await CurrencyUseHelper.TakeForUse(currency) ||
                !await Input.SimulateKeyEvent(Keys.LShiftKey, true, false))
            {
                GlobalLog.Error($"Can't take {currency} for use.", LogName);
                return false;
            }

            if (Settings.CraftPlace == CraftPlaceType.Stash && !await Stash.SwitchToCraftTab()) return false;

            foreach (var item in items)
            {
                CancellationToken.ThrowIfCancellationRequested();

                if (await ClickUntilCondition(item, condition)) continue;

                GlobalLog.Error($"Can't apply {currency} to item.", LogName);
                return false;
            }

            return true;
        }
        finally
        {
            await Input.SimulateKeyEvent(Keys.LShiftKey, false);
        }
    }


    public static async SyncTask<bool> UseCurrencyOnMultipleItems(IEnumerable<InventoryItemData> items, string currency,
        Func<InventoryItemData, bool> condition)
    {
        GlobalLog.Debug($"Start using {currency} to items.", LogName);
        try
        {
            if (currency.Contains("Delirium Orb"))
            {
                if (!await Stash.SwitchToDeliriumTab())
                {
                    GlobalLog.Error("Can't switch to delirium tab.", LogName);
                    return false;
                }
            }
            else if (!await Stash.SwitchToCurrencyTab())
            {
                GlobalLog.Error("Can't switch to currency tab.", LogName);
                return false;
            }


            if (!await CurrencyUseHelper.TakeForUse(currency) ||
                !await Input.SimulateKeyEvent(Keys.LShiftKey, true, false))
            {
                GlobalLog.Error($"Can't take {currency} for use.", LogName);
                return false;
            }

            if (Settings.CraftPlace == CraftPlaceType.Stash && !await Stash.SwitchToCraftTab()) return false;

            foreach (var item in items)
            {
                CancellationToken.ThrowIfCancellationRequested();

                if (await ClickUntilCondition(item, condition)) continue;

                GlobalLog.Error($"Can't apply {currency} to item.", LogName);
                return false;
            }

            return true;
        }
        finally
        {
            await Input.SimulateKeyEvent(Keys.LShiftKey, false);
        }
    }

    private static async SyncTask<bool> ClickUntilCondition(InventoryItemData item,
        Func<InventoryItemData, bool> condition, int maxCountClick = 3000)
    {
        var countClick = 0;
        InventoryItemData itemHoverTemp = null;
        while (Stash.IsVisible && PlayerInventory.IsVisible)
        {
            if (countClick >= 3000)
            {
                GlobalLog.Error($"Too many clicks. Max clicks: {maxCountClick}", LogName);
                return false;
            }

            CancellationToken.ThrowIfCancellationRequested();

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
                if (!isSuccess) return false;
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

        return false;
    }

    public static async SyncTask<(bool isSuccess, InventoryItemData hoveredItem)> WaitForHoveredItem(
        Func<InventoryItemData, bool> predicate,
        string operationName
    )
    {
        InventoryItemData hoveredItem = null;

        var isSuccess = await Wait.For(() =>
        {
            if (!TryGetHoveredItem(out var hoverItem)) return false;
            if (!predicate(hoverItem)) return false;
            hoveredItem = hoverItem;
            return true;
        }, operationName, 500);

        if (!isSuccess) return (false, hoveredItem);
        if (!await Input.SimulateKeyEvent(Keys.C, true, true, Keys.LControlKey)) return (false, hoveredItem);

        hoveredItem.ClipboardText = Clipboard.GetClipboardText();
        hoveredItem.ClipboardText = $"{hoveredItem.ClipboardText}explicit:{hoveredItem.ExplicitModsCount}\n";

        GlobalLog.Debug($"Clipboard text: {hoveredItem.ClipboardText}", LogName);

        return (true, hoveredItem);
    }

    public static bool TryGetHoveredItem(out InventoryItemData item)
    {
        item = null;
        var uiHover = _core.GameController.Game.IngameState.UIHoverTooltip;
        var tooltip = uiHover.Tooltip;
        var poeEntity = uiHover.Entity;
        if (tooltip == null || poeEntity.Address == 0 || !poeEntity.IsValid ||
            _core.GameController.Files.BaseItemTypes.Translate(uiHover.Entity.Path) == null)
        {
            GlobalLog.Debug("Hover item not found.", LogName);
            return false;
        }

        item = new InventoryItemData(uiHover);
        return true;
    }
}