
using System.Collections.Generic;
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
	// public bool IsT17MapCrafting = false;
	public bool UseAddQuality = true;
	public int TypeChisel = 0;
	public int TypeMethodCraft = 0;
}

public class Map(RegexCrafter core) : Craft<MapState>(core)
{
	private readonly string[] _typeMethodCraft = ["Chaos Orb", "Scouring + Alchemy"];
	public override MapState CraftState
	{ get; set; } = new();
	public override string Name { get; } = "Map";
	private readonly string[] _chiselList = [CurrencyNames.CartographersChisel, CurrencyNames.ChiselOfProliferation, CurrencyNames.ChiselOfProcurement, CurrencyNames.ChiselOfScarabs, CurrencyNames.ChiselOfDivination, CurrencyNames.ChiselOfAvarice];
	public override void DrawSettings()
	{
		// ImGui.Checkbox("Is T17 Map Crafting", ref CraftState.IsT17MapCrafting);
		ImGui.Combo("Type Method craft", ref CraftState.TypeMethodCraft, _typeMethodCraft, _typeMethodCraft.Length);
		ImGui.Checkbox("Use Add Quality", ref CraftState.UseAddQuality);
		ImGui.Combo("Type chisel", ref CraftState.TypeChisel, _chiselList, _chiselList.Length);
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
				CurrencyType = CurrencyNames.ScrollOfWisdom,
				Condition = (x) => x.item.IsIdentified,
				CancellationToken = ct
			};


			if (!await Scripts.ApplyCurrencyToInventoryItems(parameters))
			{
				await Scripts.CleanCancelKey();
				return false;
			}
		}

		// apply chisel
		if (CraftState.UseAddQuality)
		{
			var needAddQuality = nonCorruptedMaps.Where(x => !DoneCraftItem.Any(item => item.Entity.Address == x.Entity.Address) && x.Quality < 20).ToList();
			if (needAddQuality.Count != 0)
			{
				var parameters = new Scripts.CurrencyApplicationParameters
				{
					Items = needAddQuality,
					CurrencyType = _chiselList[CraftState.TypeChisel],
					Condition = RegexQualityCondition,
					CancellationToken = ct
				};

				if (!await Scripts.ApplyCurrencyToInventoryItems(parameters))
				{
					await Scripts.CleanCancelKey();
					return false;
				}
			}
		}
		switch (CraftState.TypeMethodCraft)
		{
			case 0:
				if (!await ChaosSpam(ct))
				{
					return false;
				}
				break;
			case 1:
				if (!await ScouringAndAlchemy(ct))
				{
					return false;
				}
				break;
			default:
				Core.LogError("Cannot find type method craft");
				return false;
		}

		return true;
	}
	private bool RegexQualityCondition((CustomItemData Item, string Text) hoverItem)
	{
		switch (_chiselList[CraftState.TypeChisel])
		{
			case CurrencyNames.CartographersChisel:
				return RegexUtils.MatchesPattern(hoverItem.Text, "lity:.*([2-9].|1..)%");
			case CurrencyNames.ChiselOfAvarice:
				return RegexUtils.MatchesPattern(hoverItem.Text, "urr.*([2-9].|1..)%");
			case CurrencyNames.ChiselOfDivination:
				return RegexUtils.MatchesPattern(hoverItem.Text, "div.*([2-9].|1..)%");
			case CurrencyNames.ChiselOfProcurement:
				return RegexUtils.MatchesPattern(hoverItem.Text, "ty\\).*([2-9].|1..)%");
			case CurrencyNames.ChiselOfScarabs:
				return RegexUtils.MatchesPattern(hoverItem.Text, "sca.*([2-9].|1..)%");
			case CurrencyNames.ChiselOfProliferation:
				return RegexUtils.MatchesPattern(hoverItem.Text, "ze\\).*([2-9].|1..)%");
			default: return RegexUtils.MatchesPattern(hoverItem.Text, "Quality:*.*([2-9].|1..)%");
		}

	}
	private async SyncTask<bool> ScouringAndAlchemy(CancellationToken ct)
	{
		List<CustomItemData> GetCraftList()
		{
			return PlayerInventory.GetInventoryItems().Where(x => !DoneCraftItem.Any(s => s.Entity.Address == x.Entity.Address) && !x.IsCorrupted && x.IsMap).ToList();
		}
		List<CustomItemData> craftList = GetCraftList();
		if (Settings.Debug)
		{
			Core.LogMessage($"Need craft {craftList} maps");
		}
		while (craftList.Count != 0 || ct.IsCancellationRequested)
		{

			var needScouring = craftList.Where(x => x.Rarity == ItemRarity.Magic || x.Rarity == ItemRarity.Rare).ToList();
			if (Settings.Debug) Core.LogMsg($"Find non-corrupted magic map: {needScouring.Count}");
			if (needScouring.Count != 0)
			{
				var parameters = new Scripts.CurrencyApplicationParameters
				{
					Items = needScouring,
					CurrencyType = CurrencyNames.OrbOfScouring,
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

			// apply orb of alchemy
			craftList = GetCraftList();
			var normalMaps = craftList.Where(x => x.Rarity == ItemRarity.Normal).ToList();
			if (Settings.Debug)
			{
				Core.LogMsg($"Find non corrupted normal maps: {normalMaps.Count}");
			}
			if (normalMaps.Count != 0)
			{
				var parameters = new Scripts.CurrencyApplicationParameters
				{
					Items = normalMaps,
					CurrencyType = CurrencyNames.OrbOfAlchemy,
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
			craftList = GetCraftList();
		}
		return true;
	}
	private async SyncTask<bool> ChaosSpam(CancellationToken ct)
	{
		var needScouring = PlayerInventory.GetInventoryItems().Where(x => x.IsMap && !x.IsCorrupted && x.Rarity == ItemRarity.Magic).ToList();
		if (Settings.Debug) Core.LogMsg($"Find non-corrupted magic map: {needScouring.Count}");
		if (needScouring.Count != 0)
		{
			var parameters = new Scripts.CurrencyApplicationParameters
			{
				Items = needScouring,
				CurrencyType = CurrencyNames.OrbOfScouring,
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

		// apply orb of alchemy
		var normalMaps = PlayerInventory.GetInventoryItems().Where(x => !DoneCraftItem.Any(item => item.Entity.Address == x.Entity.Address) && x.IsMap && !x.IsCorrupted && x.Rarity == ItemRarity.Normal).ToList();
		if (Settings.Debug)
		{
			Core.LogMsg($"Find non corrupted normal maps: {normalMaps.Count}");
		}
		if (normalMaps.Count != 0)
		{
			var parameters = new Scripts.CurrencyApplicationParameters
			{
				Items = normalMaps,
				CurrencyType = CurrencyNames.OrbOfAlchemy,
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
		var rareMaps = PlayerInventory.GetInventoryItems().Where(x => !DoneCraftItem.Any(item => item.Entity.Address == x.Entity.Address) && x.IsMap && !x.IsCorrupted && x.Rarity == ItemRarity.Rare).ToList();
		if (Settings.Debug)
		{
			Core.LogMsg($"Find non corrupted rare maps: {rareMaps.Count}");
		}
		if (rareMaps.Count != 0)
		{
			var parameters = new Scripts.CurrencyApplicationParameters
			{
				Items = rareMaps,
				CurrencyType = CurrencyNames.ChaosOrb,
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