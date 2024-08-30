
using System.Linq;
using System.Threading;
using ExileCore;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ImGuiNET;
using RegexCrafter.Utils;

namespace RegexCrafter.Methods;

public class MapState : CraftState
{
	public bool IsT17MapCrafting = false;
	public bool UseAddQuality = true;
}

public class Map(RegexCrafter core) : Craft<MapState>(core)
{
	public override MapState CraftState { get; set; } = new();
	public override string Name { get; } = "Map";

	public override void DrawSettings()
	{
		ImGui.Checkbox("Is T17 Map Crafting", ref CraftState.IsT17MapCrafting);
		ImGui.Checkbox("Use Add Quality", ref CraftState.UseAddQuality);
		base.DrawSettings();
	}

	public override async SyncTask<bool> Start(CancellationToken ct)
	{
		var inventoryItems = PlayerInventory.GetInventoryItems();
		if (Settings.Debug)
		{
			Core.LogMsg("Load player inventory.");

		}
		inventoryItems.ForEach(x =>
		{
			if (!x.IsMap || x.IsCorrupted)
			{
				BadItems.Add(x);
			}
		});
		// Check if all maps are identified
		var nonCorruptedMaps = inventoryItems.Where(x => x.IsMap && !x.IsCorrupted).ToList();

		// Apply Scroll of Wisdom
		var needIdentifies = nonCorruptedMaps.Where(x => !x.IsIdentified).ToList();
		if (Settings.Debug)
		{
			Core.LogMsg($"Find non corrupted maps: {nonCorruptedMaps.Count}");
			Core.LogMsg($"Find non identified maps: {needIdentifies.Count}");

		}
		if (needIdentifies.Count != 0)
		{
			var parameters = new Scripts.CurrencyApplicationParameters
			{
				Items = needIdentifies,
				CurrencyType = CurrencyType.ScrollOfWisdom,
				Condition = (x) => x.item.IsIdentified,
				CancellationToken = ct
			};


			if (!await Scripts.ApplyCurrencyToInventoryItems(parameters))
			{
				await Scripts.CleanCancelKey();
				return false;
			}
		}

		// apply orb of scouring
		if (!CraftState.IsT17MapCrafting)
		{
			var needScouring = nonCorruptedMaps.Where(x => (x.Rarity == ItemRarity.Rare && x.Quality < 20) || x.Rarity == ItemRarity.Magic).ToList();
			if (needScouring.Count != 0)
			{
				var parameters = new Scripts.CurrencyApplicationParameters
				{
					Items = needScouring,
					CurrencyType = CurrencyType.OrbOfScouring,
					Condition = (x) => RegexCondition(x) || x.item.Rarity == ItemRarity.Normal,
					CancellationToken = ct
				};
				if (Settings.Debug)
				{
					Core.LogMsg("Try use scouring.");

				}
				if (!await Scripts.ApplyCurrencyToInventoryItems(parameters))
				{
					await Scripts.CleanCancelKey();
					return false;
				}
			}
		}

		// apply cartographers chisel
		if (CraftState.UseAddQuality)
		{
			var needAddQuality = nonCorruptedMaps.Where(x => !DoneCraftItem.Any(item => item.Entity.Address == x.Entity.Address) && x.Quality < 20).ToList();
			if (needAddQuality.Count != 0)
			{
				var parameters = new Scripts.CurrencyApplicationParameters
				{
					Items = needAddQuality,
					CurrencyType = CurrencyType.CartographersChisel,
					Condition = (x) => RegexCondition(x) || x.item.Quality >= 20,
					CancellationToken = ct
				};

				if (!await Scripts.ApplyCurrencyToInventoryItems(parameters))
				{
					await Scripts.CleanCancelKey();
					return false;
				}
			}
		}

		// apply orb of alchemy
		nonCorruptedMaps = PlayerInventory.GetInventoryItems().Where(x => !DoneCraftItem.Any(item => item.Entity.Address == x.Entity.Address) && x.IsMap && !x.IsCorrupted && x.Rarity == ItemRarity.Normal).ToList();
		if (Settings.Debug)
		{
			Core.LogMsg($"Find non corrupted normal maps: {nonCorruptedMaps.Count}");
		}
		if (nonCorruptedMaps.Count != 0)
		{
			var parameters = new Scripts.CurrencyApplicationParameters
			{
				Items = nonCorruptedMaps,
				CurrencyType = CurrencyType.OrbOfAlchemy,
				Condition = (x) => RegexCondition(x) || x.item.Rarity == ItemRarity.Rare,
				CancellationToken = ct
			};

			if (Settings.Debug)
			{
				Core.LogMsg("Try use alchemy.");
			}
			if (!await Scripts.ApplyCurrencyToInventoryItems(parameters))
			{
				await Scripts.CleanCancelKey();
				return false;
			}
		}

		// chaos spam
		nonCorruptedMaps = PlayerInventory.GetInventoryItems().Where(x => !DoneCraftItem.Any(item => item.Entity.Address == x.Entity.Address) && x.IsMap && !x.IsCorrupted && x.Rarity == ItemRarity.Rare).ToList();
		if (Settings.Debug)
		{
			Core.LogMsg($"Find non corrupted rare maps: {nonCorruptedMaps.Count}");
		}
		if (nonCorruptedMaps.Count != 0)
		{
			var parameters = new Scripts.CurrencyApplicationParameters
			{
				Items = nonCorruptedMaps,
				CurrencyType = CurrencyType.ChaosOrb,
				Condition = RegexCondition,
				CancellationToken = ct
			};

			if (Settings.Debug)
			{
				Core.LogMsg("Try use chaos.");
			}
			if (!await Scripts.ApplyCurrencyToInventoryItems(parameters))
			{
				await Scripts.CleanCancelKey();
				return false;
			}
		}
		return true;
	}
}