
using System.Collections.Generic;
using System.Linq;
using ExileCore.Shared.Enums;

namespace RegexCrafter;

public class PlayerInventory
{
	private static RegexCrafter Core;
	public static void Init(RegexCrafter core) => Core = core;
	public static bool IsVisible => Core.GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible;
	public static List<CustomItemData> GetInventoryItems()
	{
		if (!IsVisible)
		{
			return [];
		}
		var inventoryItems = Core.GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems;
		return inventoryItems.Select(x => new CustomItemData(x)).ToList();
	}
}