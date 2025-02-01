using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ImGuiNET;
using RegexCrafter.Helpers;
using RegexCrafter.Helpers.Enums;
using RegexCrafter.Utils;

namespace RegexCrafter.CraftsMethods;

public class MapState : CraftState
{
    public CurrencyMethodCraftType CurrencyMethodCraftType = CurrencyMethodCraftType.Chaos;
    public int DeliriumOrbNameIdx;
    public int TypeChisel;
    public bool UseAddDeliriumOrb;
    public bool UseAddQuality;
}

public class Map(RegexCrafter core) : Craft<MapState>(core)
{
    private const string LogName = "CraftMap";

    private readonly string[] _chiselList = CurrencyNames.GetChiselNames().ToArray();

    private readonly string[] _deliriumOrbList = DeliriumOrbsNames.GetAllDeliriumOrbsNames().ToArray();

    private readonly CurrencyMethodCraftType[] _typeMethodCraft =
        [CurrencyMethodCraftType.Chaos, CurrencyMethodCraftType.ScouringAndAlchemy];

    public override MapState CraftState { get; set; } = new();

    public override string Name { get; } = "Map";

    public override void DrawSettings()
    {
        base.DrawSettings();
        var selectedMethod = (int)CraftState.CurrencyMethodCraftType;
        if (ImGui.Combo("Type Method craft", ref selectedMethod,
                _typeMethodCraft.Select(x => x.GetDescription()).ToArray(), _typeMethodCraft.Length))
            CraftState.CurrencyMethodCraftType = (CurrencyMethodCraftType)selectedMethod;
        ImGui.Checkbox("Use Add Quality", ref CraftState.UseAddQuality);
        if (CraftState.UseAddQuality)
        {
            ImGui.SameLine();
            ImGui.Combo("Type Chisel", ref CraftState.TypeChisel, _chiselList, _chiselList.Length);
        }

        ImGui.Checkbox("Use Add Delirium Orb", ref CraftState.UseAddDeliriumOrb);
        if (CraftState.UseAddDeliriumOrb)
        {
            ImGui.SameLine();
            ImGui.Combo("Delirium Orb Name", ref CraftState.DeliriumOrbNameIdx, _deliriumOrbList,
                _deliriumOrbList.Length);
        }

        ImGui.Separator();
        if (CraftState.RegexPatterns.Count == 0) CraftState.RegexPatterns.Add(string.Empty);
        ImGui.Dummy(new Vector2(0, 10));
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 10));
        ImGui.LabelText("##MainConditionsMap", "Main Conditions");
        for (var i = 0; i < CraftState.RegexPatterns.Count; i++)
        {
            var patternTemp = CraftState.RegexPatterns[i];
            if (ImGui.InputText($"Your regex pattern {i}", ref patternTemp, 1024))
                CraftState.RegexPatterns[i] = patternTemp;
            ImGui.SameLine();
            if (!ImGui.Button($"Remove##{i}")) continue;
            GlobalLog.Debug($"Remove pattern:{CraftState.RegexPatterns[i]}.", LogName);
            CraftState.RegexPatterns.RemoveAt(i);
            //tempPatternList.Add(i);
        }

        if (ImGui.Button("Add Regex Pattern")) CraftState.RegexPatterns.Add(string.Empty);
    }

    public override async SyncTask<bool> Start()
    {
        if (!await CurrencyUseHelper.IdentifyItems()) return false;

        // apply chisel
        if (CraftState.UseAddQuality && !await Scripts.UseCurrencyOnMultipleItems(_chiselList[CraftState.TypeChisel],
                x => x.IsMap && !x.IsCorrupted, RegexQualityCondition)) return false;

        switch (CraftState.CurrencyMethodCraftType)
        {
            case CurrencyMethodCraftType.Chaos:
                return await ChaosSpam();
            case CurrencyMethodCraftType.ScouringAndAlchemy:
                return await ScouringAndAlchemy();
            default:
                GlobalLog.Error("Cannot find type method craft.", LogName);
                return false;
        }
    }

    private bool RegexQualityCondition(InventoryItemData item)
    {
        return _chiselList[CraftState.TypeChisel] switch
        {
            CurrencyNames.CartographersChisel => RegexUtils.MatchesPattern(item.ClipboardText, "lity:.*([2-9].|1..)%"),
            CurrencyNames.ChiselOfAvarice => RegexUtils.MatchesPattern(item.ClipboardText, "urr.*([2-9].|1..)%"),
            CurrencyNames.ChiselOfDivination => RegexUtils.MatchesPattern(item.ClipboardText, "div.*([2-9].|1..)%"),
            CurrencyNames.ChiselOfProcurement => RegexUtils.MatchesPattern(item.ClipboardText, "ty\\).*([2-9].|1..)%"),
            CurrencyNames.ChiselOfScarabs => RegexUtils.MatchesPattern(item.ClipboardText, "sca.*([2-9].|1..)%"),
            CurrencyNames.ChiselOfProliferation =>
                RegexUtils.MatchesPattern(item.ClipboardText, "ze\\).*([2-9].|1..)%"),
            _ => RegexUtils.MatchesPattern(item.ClipboardText, "Quality:*.*([2-9].|1..)%")
        };
    }

    private async SyncTask<bool> ScouringAndAlchemy()
    {
        if (Settings.CraftPlace == CraftPlaceType.MousePosition)
        {
            var (isSuccess, item) = await Scripts.WaitForHoveredItem(
                hoverItem => hoverItem != null,
                "Get the initial hovered item");

            if (!isSuccess)
            {
                GlobalLog.Error("### No hovered item found!", LogName);
                return false;
            }

            if (item.IsCorrupted || !item.IsMap) return false;
            if (CraftState.UseAddQuality &&
                !await Scripts.UseCurrencyToSingleItem(item, _chiselList[CraftState.TypeChisel], RegexQualityCondition))
                return false;

            var resCondition = RegexCondition(item);

            while (!resCondition)
            {
                CancellationToken.ThrowIfCancellationRequested();

                if (item.Rarity is ItemRarity.Rare or ItemRarity.Magic)
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfScouring,
                            x =>
                            {
                                item = x;
                                resCondition = RegexCondition(x);
                                return resCondition && x.Rarity == ItemRarity.Normal;
                            }))
                        return false;

                if (item.Rarity != ItemRarity.Normal) continue;

                if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfAlchemy,
                        x =>
                        {
                            item = x;
                            resCondition = RegexCondition(x);
                            return resCondition && x.Rarity == ItemRarity.Rare;
                        })) return false;
            }

            return true;
        }

        var maps = await GetValidMaps();

        if (maps == null) return false;

        while (maps.Count > 0)
        {
            CancellationToken.ThrowIfCancellationRequested();
            //apply scouring
            if (!await CurrencyUseHelper.ScouringItems(RegexCondition)) return false;

            // apply orb of alchemy
            if (!await CurrencyUseHelper.AlchemyItems(RegexCondition)) return false;

            maps = await GetValidMaps();
            if (maps == null) return false;
        }

        return true;
    }

    private SyncTask<List<InventoryItemData>> GetValidMaps()
    {
        return Scripts.TryGetUsedItems(x =>
            DoneCraftItem.All(s => s.Entity.Address != x.Entity.Address) && !x.IsCorrupted && x.IsMap);
    }

    private async SyncTask<bool> ChaosSpam()
    {
        if (Settings.CraftPlace == CraftPlaceType.MousePosition)
        {
            var (isSuccess, item) = await Scripts.WaitForHoveredItem(
                hoverItem => hoverItem != null,
                "Get the initial hovered item");

            if (!isSuccess)
            {
                GlobalLog.Error("### No hovered item found!", LogName);
                return false;
            }

            if (item.IsCorrupted || !item.IsMap) return false;
            if (CraftState.UseAddQuality &&
                !await Scripts.UseCurrencyToSingleItem(item, _chiselList[CraftState.TypeChisel], x =>
                {
                    item = x;
                    return RegexQualityCondition(x);
                }))
                return false;
            if (RegexCondition(item)) return true;

            switch (item.Rarity)
            {
                case ItemRarity.Normal:
                    // 1. Use alchemy
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfAlchemy,
                            x =>
                            {
                                item = x;
                                return x.Rarity == ItemRarity.Rare;
                            }))
                        return false;
                    break;
                case ItemRarity.Magic:
                    // 1. Scouring to Normal 
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfScouring,
                            x =>
                            {
                                item = x;
                                return x.Rarity == ItemRarity.Normal;
                            }))
                        return false;
                    // 2. Alchemy to Rare 
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfAlchemy,
                            x =>
                            {
                                item = x;
                                return x.Rarity == ItemRarity.Rare;
                            }))
                        return false;
                    break;
                case ItemRarity.Rare:
                    break;
                default:
                    // else return false 
                    return false;
            }

            if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.ChaosOrb, x =>
                {
                    item = x;
                    return RegexCondition(x);
                }))
                return false;
            if (!CraftState.UseAddDeliriumOrb) return true;
            if (!await Scripts.UseCurrencyToSingleItem(item, _deliriumOrbList[CraftState.DeliriumOrbNameIdx],
                    x => x.DeliriumOrbNumber == 5))
                return false;

            return true;
        }

        if (!await CurrencyUseHelper.ScouringItems(x => x.IsMap && x.Rarity == ItemRarity.Magic, RegexCondition))
            return false;
        // apply orb of alchemy
        if (!await CurrencyUseHelper.AlchemyItems(x => x.IsMap, RegexCondition)) return false;
        // chaos spam
        if (!await CurrencyUseHelper.ChaosSpamItems(x => x.IsMap, RegexCondition)) return false;

        if (!CraftState.UseAddDeliriumOrb) return true;
        if (!await Scripts.UseCurrencyOnMultipleItems(_deliriumOrbList[CraftState.DeliriumOrbNameIdx],
                x => DoneCraftItem.All(x => x.Entity.Address == x.Entity.Address)
                , x => x.DeliriumOrbNumber == 5))
            return false;

        return true;
    }
}