

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
using ExileCore;

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
			if (!await TakeItemUse(currency, ct))
			{
				Core.LogError($"Can't take {currencyType}.");
				return false;
			}

			foreach (var item in items)
			{
				if (ct.IsCancellationRequested)
				{
					Core.LogError($"Cancelling currency application.");
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
					Core.LogError($"Can't apply {currencyType} to item.");
					return false;
				}
			}
			await CancelCurrencyApplication();
		}
		return true;
	}

	public static async SyncTask<bool> CopiedHoverItemToClipboard()
	{
		using (_inputController)
		{
			await _inputController.KeyDown(Keys.LControlKey);
			await _inputController.KeyDown(Keys.C);
			await _inputController.KeyUp(Keys.C);
			await _inputController.KeyUp(Keys.LControlKey);

		}

		return true;
	}
	public static async SyncTask<bool> MoveMouse(
		 SharpDX.RectangleF rec)
	{
		var position = Core.Settings.UseRandomPosition ? rec.ClickRandomNum(20, 20) : rec.Center.ToVector2Num();
		position += WindowOffset.ToVector2Num();
		await _inputController.MoveMouse(position);
		return true;
	}
	public static async SyncTask<bool> Click(
		MouseButtons mouseButton, SharpDX.RectangleF rec)
	{
		var position = Core.Settings.UseRandomPosition ? rec.ClickRandomNum(20, 20) : rec.Center.ToVector2Num();

		position += WindowOffset.ToVector2Num();
		await _inputController.Click(mouseButton, position);
		return true;
	}
	public static async SyncTask<bool> Click(
		MouseButtons mouseButton)
	{
		await _inputController.Click(mouseButton);
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
		await MoveMouse(item.Position);

		while (!ct.IsCancellationRequested)
		{
			await CopiedHoverItemToClipboard();
			var hoverItem = GetHoveredItem();

			if (hoverItem.Item == null || hoverItem.ItemText == null)
			{
				Core.LogError($"No Hover item found.");
				return false;
			}

			if (condition(hoverItem))
			{
				break;
			}

			if (!Stash.IsHaveItem(currencyType))
			{
				Core.LogError($"No {currencyType} found.");
				return false;
			}

			if (Core.Settings.UseRandomPosition) await Click(MouseButtons.Left, item.Position);
			else await Click(MouseButtons.Left);
		}
		return true;
	}

	public static async SyncTask<bool> CleanCancelKey()
	{
		try
		{
			Core.LogMsg("Try cleaning keys.");
			using (_inputController)
			{
				await _inputController.KeyUp(Keys.LControlKey);
				await _inputController.KeyUp(Keys.LShiftKey);
				await _inputController.KeyUp(System.Windows.Forms.Keys.RButton);
				await _inputController.KeyUp(System.Windows.Forms.Keys.LButton);
				if (Core.GameController.Game.IngameState.IngameUi.Cursor.Action == MouseActionType.UseItem)
				{
					await _inputController.KeyDown(Keys.Escape);
					await _inputController.KeyUp(Keys.Escape);
				}
			}
			Core.LogMsg("Cleaned keys.");
		}
		catch (Exception ex)
		{
			Core.LogError($"CLEAN KEYS Error:  {ex.Message}");
			return false;
		}
		return true;
	}
	private static async SyncTask<bool> CancelCurrencyApplication()
	{
		await _inputController.KeyUp(Keys.LShiftKey);
		return true;
	}
	private static async SyncTask<bool> TakeItemUse(
		CustomItemData item, CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			await Click(MouseButtons.Right, item.Position);
			await _inputController.KeyDown(Keys.LShiftKey, ct);
			if (Core.GameController.Game.IngameState.IngameUi.Cursor.Action == MouseActionType.UseItem)
			{
				return true;
			}
		}
		return false;
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