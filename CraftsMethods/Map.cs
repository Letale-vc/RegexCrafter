
using System.Linq;
using System.Threading;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ImGuiNET;
using RegexCrafter.Utils;

namespace RegexCrafter.Methods;

public class Map : Craft
{
	public override string Name { get; } = "Map";
	private bool IsT17MapCrafting = false;
	private bool UseAddQuality = true;

	public override void DrawSettings()
	{
		ImGui.Checkbox("Is T17 Map Crafting", ref IsT17MapCrafting);
		ImGui.Checkbox("Use Add Quality", ref UseAddQuality);
	}

	public override async SyncTask<bool> Start(CancellationToken ct)
	{
		var inventoryItems = PlayerInventory.GetInventoryItems();
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
				return false;
			}
		}

		// apply orb of scouring
		if (!IsT17MapCrafting)
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
				if (!await Scripts.ApplyCurrencyToInventoryItems(parameters))
				{
					return false;
				}
			}
		}

		// apply cartographers chisel
		if (UseAddQuality && !IsT17MapCrafting)
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
					return false;
				}
			}
		}

		// apply orb of alchemy
		nonCorruptedMaps = PlayerInventory.GetInventoryItems().Where(x => !DoneCraftItem.Any(item => item.Entity.Address == x.Entity.Address) && x.IsMap && !x.IsCorrupted && x.Rarity == ItemRarity.Normal).ToList();
		if (nonCorruptedMaps.Count != 0)
		{
			var parameters = new Scripts.CurrencyApplicationParameters
			{
				Items = nonCorruptedMaps,
				CurrencyType = CurrencyType.OrbOfAlchemy,
				Condition = (x) => RegexCondition(x) || x.item.Rarity == ItemRarity.Rare,
				CancellationToken = ct
			};
			if (!await Scripts.ApplyCurrencyToInventoryItems(parameters))
			{
				return false;
			}
		}

		// chaos spam
		nonCorruptedMaps = PlayerInventory.GetInventoryItems().Where(x => !DoneCraftItem.Any(item => item.Entity.Address == x.Entity.Address) && x.IsMap && !x.IsCorrupted && x.Rarity == ItemRarity.Rare).ToList();
		if (nonCorruptedMaps.Count != 0)
		{
			var parameters = new Scripts.CurrencyApplicationParameters
			{
				Items = nonCorruptedMaps,
				CurrencyType = CurrencyType.ChaosOrb,
				Condition = RegexCondition,
				CancellationToken = ct
			};
			if (!await Scripts.ApplyCurrencyToInventoryItems(parameters))
			{
				return false;
			}
		}
		return true;
	}
}