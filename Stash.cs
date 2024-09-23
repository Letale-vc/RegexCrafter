using ExileCore.PoEMemory;
using ExileCore.Shared.Enums;
using System.Collections.Generic;
using System.Linq;

namespace RegexCrafter;

public class Stash
{
	private static RegexCrafter Core;
	public static void Init(RegexCrafter core) => Core = core;

	public static bool IsVisible => Core.GameController.Game.IngameState.IngameUi.StashElement.IsVisible;
	public static InventoryType InventoryType => Core.GameController.Game.IngameState.IngameUi.StashElement.VisibleStash.InvType;
	public static string VisibleTabName => Core.GameController.Game.IngameState.IngameUi.StashElement.GetStashName(Core.GameController.Game.IngameState.IngameUi.StashElement.IndexVisibleStash);
	public static List<CustomItemData> VisibleTabItems => Core.GameController.Game.IngameState.IngameUi.StashElement.VisibleStash.VisibleInventoryItems.Select(x => new CustomItemData(x)).ToList();

	public static bool IsPublicVisibleTab
	{
		get
		{
			var playerStashTabs = Core.GameController.Game.IngameState.ServerData.PlayerStashTabs;
			var tab = playerStashTabs.First(x => x.Name == VisibleTabName);
			return tab.Flags.HasFlag(InventoryTabFlags.Public);
		}
	}

	public static bool IsHaveItem(string baseName)
	{
		if (!IsVisible)
		{
			return false;
		}
		return Core.GameController.Game.IngameState.IngameUi.StashElement.VisibleStash.VisibleInventoryItems
			.Select(x => new CustomItemData(x))
			.Any(x => x.BaseName == baseName);
	}
	public static bool TryGetItem(string baseName, out CustomItemData item)
	{
		if (!IsVisible)
		{
			item = null;
			return false;
		}
		item = Core.GameController.Game.IngameState.IngameUi.StashElement.VisibleStash.VisibleInventoryItems
				.Select(x => new CustomItemData(x))
				.FirstOrDefault(x => x.BaseName == baseName);
		return item != null;
	}

	public static bool TryGetCurrencyButton(string text, out Element element)
	{
		element = Core.GameController.IngameState.IngameUi.StashElement.VisibleStash.FindChildRecursive(x => x.Text == text);
		if (element == null)
		{
			if (Core.Settings.Debug)
			{
				Core.LogError($"Not find button {text}");
			}
			return false;
		}

		if (Core.Settings.Debug)
		{
			Core.LogMessage($"Find button {element.Text}");
		}
		return true;
	}


}
