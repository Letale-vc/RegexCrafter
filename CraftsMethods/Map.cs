using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ImGuiNET;
using RegexCrafter.Helpers;
using RegexCrafter.Helpers.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace RegexCrafter.CraftsMethods;

public class MapState : CraftState
{
    public CurrencyMethodCraftType CurrencyMethodCraftType = CurrencyMethodCraftType.Chaos;
    public int TypeChisel;
    public bool UseAddQuality;
}

public class Map(RegexCrafter core) : CraftBase<MapState>(core)
{
    private const string LogName = "CraftMap";

    private readonly string[] _chiselList = CurrencyNames.GetChiselNames().ToArray();

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

    protected override async SyncTask<bool> Start()
    {
        if (!await CurrencyUseHelper.IdentifyItems(x => x.IsMap))
        {
            GlobalLog.Error("Feiled indentify items", LogName);
            return false;
        }

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
            CurrencyNames.CartographersChisel => RegexFinder.ContainsMatchInText(item.ClipboardText, "lity:.*([2-9].|1..)%"),
            CurrencyNames.ChiselOfAvarice => RegexFinder.ContainsMatchInText(item.ClipboardText, "urr.*([2-9].|1..)%"),
            CurrencyNames.ChiselOfDivination => RegexFinder.ContainsMatchInText(item.ClipboardText, "div.*([2-9].|1..)%"),
            CurrencyNames.ChiselOfProcurement => RegexFinder.ContainsMatchInText(item.ClipboardText, "ty\\).*([2-9].|1..)%"),
            CurrencyNames.ChiselOfScarabs => RegexFinder.ContainsMatchInText(item.ClipboardText, "sca.*([2-9].|1..)%"),
            CurrencyNames.ChiselOfProliferation =>
                RegexFinder.ContainsMatchInText(item.ClipboardText, "ze\\).*([2-9].|1..)%"),
            _ => RegexFinder.ContainsMatchInText(item.ClipboardText, "Quality:*.*([2-9].|1..)%")
        };
    }

    private async SyncTask<bool> ScouringAndAlchemy()
    {
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

    private async SyncTask<List<InventoryItemData>> GetValidMaps()
    {
        var (Succes, Items) = await CraftingPlace.TryGetUsedItemsAsync(x =>
            DoneCraftItem.All(s => s.Entity.Address != x.Entity.Address) && !x.IsCorrupted && x.IsMap);
        return Items;
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
            {
                return false;
            }

            return true;
        }

        if (!await CurrencyUseHelper.ScouringItems(x => x.IsMap && x.Rarity == ItemRarity.Magic, RegexCondition))
            return false;
        // apply orb of alchemy
        if (!await CurrencyUseHelper.AlchemyItems(x => x.IsMap, RegexCondition)) return false;
        // chaos spam
        if (!await CurrencyUseHelper.ChaosSpamItems(x => x.IsMap, RegexCondition)) return false;


        return true;
    }
}