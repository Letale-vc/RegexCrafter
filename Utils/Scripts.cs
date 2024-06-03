

using System;
using ExileCore.Shared;
using InputHumanizer.Input;
using System.Windows.Forms;
using ExileCore.Shared.Helpers;
using System.Diagnostics;
using System.Collections.Generic;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Enums;
using System.Threading;

namespace RegexCrafter.Utils;


public class Scripts
{
    private static IInputController _inputController;
    private static RegexCrafter Core;
    private static SharpDX.Vector2 WindowOffset => Core.GameController.Window.GetWindowRectangleTimeCache.TopLeft;
    public static bool Init(RegexCrafter core)
    {
        Core = core;
        var tryGetInputController = Core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>("InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            Core.LogError("InputHumanizer method not registered.");
            return false;
        }
        _inputController = tryGetInputController(Core.Name);

        return true;
    }


    public class CurrencyApplicationParameters
    {
        public List<CustomItemData> Items { get; set; }
        public string CurrencyType { get; set; }
        public Func<(CustomItemData item, string hoverItemText), bool> Condition { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public void Deconstruct(
             out List<CustomItemData> items,
             out string currencyType,
             out Func<(CustomItemData item, string hoverItemText), bool> condition,
             out CancellationToken ct)
        {
            items = Items;
            currencyType = CurrencyType;
            condition = Condition;
            ct = CancellationToken;
        }
    }
    public static async SyncTask<bool> ApplyCurrencyToInventoryItems(CurrencyApplicationParameters parameters)
    {
        var (items, currencyType, condition, ct) = parameters;

        if (!Stash.TryGetItem(currencyType, out var currency))
        {
            Core.LogError($"No {currencyType} found.");
            return false;
        }

        using (_inputController)
        {
            await PrepareCurrency(currency, ct);

            foreach (var item in items)
            {
                if (ct.IsCancellationRequested)
                {
                    await CancelCurrencyApplication(ct);
                    return false;
                }
                var currencyToItemParams = new CurrencyToItemParams
                {
                    Item = item,
                    CurrencyType = currencyType,
                    Condition = condition,
                    CancellationToken = ct
                };
                if (!await ApplyCurrencyToItem(currencyToItemParams))
                {
                    return false;
                }
            }
            await CancelCurrencyApplication(ct);
        }
        return true;
    }

    public static async SyncTask<bool> CopiedHoverItemToClipboard(CancellationToken ct)
    {
        await _inputController.KeyDown(Keys.LControlKey, ct);
        await _inputController.KeyDown(Keys.C, ct);
        await _inputController.KeyUp(Keys.C, cancellationToken: ct);
        await _inputController.KeyUp(Keys.LControlKey, cancellationToken: ct);
        return true;
    }
    public static async SyncTask<bool> MoveMouse(SharpDX.Vector2 position, CancellationToken ct)
    {
        position += WindowOffset;
        await _inputController.MoveMouse(position.ToVector2Num(), ct);
        return true;
    }
    public static async SyncTask<bool> Click(MouseButtons mouseButton, SharpDX.Vector2 position)
    {
        position += WindowOffset;
        await _inputController.Click(mouseButton, position.ToVector2Num());
        return true;
    }
    private class CurrencyToItemParams
    {
        public CustomItemData Item { get; set; }
        public string CurrencyType { get; set; }
        public Func<(CustomItemData item, string hoverItemText), bool> Condition { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public void Deconstruct(
            out CustomItemData item,
            out string currencyType,
            out Func<(CustomItemData item, string hoverItemText), bool> condition,
            out CancellationToken ct)
        {
            item = Item;
            currencyType = CurrencyType;
            condition = Condition;
            ct = CancellationToken;
        }
    }

    private static async SyncTask<bool> ApplyCurrencyToItem(CurrencyToItemParams Params)
    {
        var (item, currencyType, condition, ct) = Params;
        await MoveMouse(item.Position.Center, ct);

        while (!ct.IsCancellationRequested)
        {
            await Wait();
            await CopiedHoverItemToClipboard(ct);
            var hoverItem = GetHoveredItem();

            if (hoverItem.Item == null || hoverItem.ItemText == null)
            {
                Core.LogError($"No Hover item found.");
                await CancelCurrencyApplication(ct);
                return false;
            }

            if (condition(hoverItem))
            {
                break;
            }

            if (!Stash.IsHaveItem(currencyType))
            {
                Core.LogError($"No {currencyType} found.");
                await CancelCurrencyApplication(ct);
                return false;
            }

            await _inputController.Click(MouseButtons.Left, ct);
        }

        return true;
    }
    private static async SyncTask<bool> CancelCurrencyApplication(CancellationToken ct)
    {
        await _inputController.KeyUp(Keys.LShiftKey, cancellationToken: ct);
        return true;
    }
    private static async SyncTask<bool> PrepareCurrency(CustomItemData currency, CancellationToken ct)
    {
        await MoveMouse(currency.Position.Center, ct);
        await _inputController.Click(MouseButtons.Right, ct);
        await _inputController.KeyDown(Keys.LShiftKey, ct);
        return true;
    }
    public static (CustomItemData Item, string ItemText) GetHoveredItem()
    {
        try
        {
            var uiHover = Core.GameController.Game.IngameState.UIHover;

            if (uiHover.AsObject<HoverItemIcon>().ToolTipType != ToolTipType.ItemInChat)
            {
                var inventoryItemIcon = uiHover.AsObject<NormalInventoryItem>();
                var tooltip = inventoryItemIcon.Tooltip;
                var poeEntity = inventoryItemIcon.Item;
                if (tooltip != null && poeEntity.Address != 0 && poeEntity.IsValid)
                {
                    var item = inventoryItemIcon.Item;
                    var baseItemType = Core.GameController.Files.BaseItemTypes.Translate(item.Path);
                    if (baseItemType != null)
                    {

                        var itemText = Clipboard.GetClipboardText();
                        if (Core.Settings.Debug)
                        {
                            Core.LogMsg($"Copied: {itemText} \n");
                        }
                        if (string.IsNullOrEmpty(itemText))
                        {
                            return (null, null);
                        }
                        return (new CustomItemData(inventoryItemIcon), itemText);
                    }
                }
            }
        }
        catch
        {
            return (null, null);
        }
        return (null, null);
    }
    private static async SyncTask<bool> Wait()
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromMilliseconds(50))
        {
            await TaskUtils.NextFrame();
        }
        return true;
    }
}