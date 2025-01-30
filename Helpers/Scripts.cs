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
            return await ApplyCurrencyUntilMet(item, condition);
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

            if (!await Stash.SwitchToCurrencyTab() || !await CurrencyUseHelper.TakeForUse(currency) ||
                !await Input.SimulateKeyEvent(Keys.LShiftKey, true, false))
                return false;

            if (Settings.CraftPlace == CraftPlaceType.Stash && !await Stash.SwitchToCraftTab()) return false;

            foreach (var item in items)
            {
                CancellationToken.ThrowIfCancellationRequested();

                if (await ApplyCurrencyUntilMet(item, condition)) continue;

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
            if (!await Stash.SwitchToCurrencyTab() || !await CurrencyUseHelper.TakeForUse(currency) ||
                !await Input.SimulateKeyEvent(Keys.LShiftKey, true, false))
                return false;

            if (Settings.CraftPlace == CraftPlaceType.Stash && !await Stash.SwitchToCraftTab()) return false;

            foreach (var item in items)
            {
                CancellationToken.ThrowIfCancellationRequested();

                if (await ApplyCurrencyUntilMet(item, condition)) continue;

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

    private static async SyncTask<bool> ApplyCurrencyUntilMet(InventoryItemData item,
        Func<InventoryItemData, bool> condition)
    {
        if (condition(item)) return true;

        // GlobalLog.Info($"Start applying Currency UntilMet to {item}.", LogName);

        InventoryItemData itemHoverTemp = null;
        while (Stash.IsVisible && PlayerInventory.IsVisible)
        {
            CancellationToken.ThrowIfCancellationRequested();
            // Check if the required currency is available
            // if (!Stash.CurrentTab.ContainsItem(x => x.BaseName == _currentCurrencyUse))
            // {
            //     GlobalLog.Error($"No {_currentCurrencyUse} found.", LogName);
            //     return false;
            // }

            var cursorGetClientRectCache = Cursor.GetClientRectCache;

            if (!item.GetClientRectCache.Contains(cursorGetClientRectCache.Center.X, cursorGetClientRectCache.Center.Y))
                // Move the mouse to the item
                await item.MoveMouseToItem();

            // Get the initial hovered item
            if (itemHoverTemp == null)
            {
                var (isSuccess, hoveredItem) = await WaitForHoveredItem(
                    hoverItem => hoverItem.Entity != null,
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
            await Input.Click();

            // Wait for the item to update after applying currency
            var (isSuccessAfterClick, updatedHoveredItem) = await WaitForHoveredItem(
                hoverItem => hoverItem.Entity.Address != itemHoverTemp.Entity.Address,
                "Apply currency to item");

            if (!isSuccessAfterClick) return false;
            itemHoverTemp = updatedHoveredItem;
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