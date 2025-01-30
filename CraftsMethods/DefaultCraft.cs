using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ImGuiNET;
using RegexCrafter.Helpers;
using RegexCrafter.Helpers.Enums;

namespace RegexCrafter.CraftsMethods;

public class DefaultCraftState : CraftState
{
    public CurrencyMethodCraftType CurrencyMethodCraftTypeMethodCraft = CurrencyMethodCraftType.Chaos;
}

public class DefaultCraft(RegexCrafter core) : Craft<DefaultCraftState>(core)
{
    private const string LogName = "Default Craft";

    private readonly CurrencyMethodCraftType[] _typeMethodCraft =
        [CurrencyMethodCraftType.Chaos, CurrencyMethodCraftType.ScouringAndAlchemy];

    public override DefaultCraftState CraftState { get; set; } = new();

    public override string Name { get; } = "Default craft";

    public override void DrawSettings()
    {
        base.DrawSettings();
        var selectedMethod = (int)CraftState.CurrencyMethodCraftTypeMethodCraft;
        if (ImGui.Combo("Type Method craft", ref selectedMethod,
                _typeMethodCraft.Select(x => x.GetDescription()).ToArray(), _typeMethodCraft.Length))
            CraftState.CurrencyMethodCraftTypeMethodCraft = (CurrencyMethodCraftType)selectedMethod;
        ImGui.Dummy(new Vector2(0, 10));
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 10));
        if (CraftState.RegexPatterns.Count == 0) CraftState.RegexPatterns.Add(string.Empty);
        ImGui.Dummy(new Vector2(0, 20));
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
        switch (CraftState.CurrencyMethodCraftTypeMethodCraft)
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

            if (item.IsCorrupted) return false;

            if (RegexCondition(item)) return true;
            while (!RegexCondition(item))
            {
                CancellationToken.ThrowIfCancellationRequested();

                if (item.Rarity is ItemRarity.Rare or ItemRarity.Magic)
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfScouring,
                            x => x.Rarity == ItemRarity.Normal))
                        return false;

                if (item.Rarity != ItemRarity.Normal) continue;

                if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfAlchemy,
                        x => x.Rarity == ItemRarity.Rare)) return false;
            }

            return true;
        }

        var maps = await GetValidItem();

        if (maps == null) return false;

        while (maps.Count > 0)
        {
            CancellationToken.ThrowIfCancellationRequested();
            //apply scouring
            if (!await CurrencyUseHelper.ScouringItems(RegexCondition)) return false;

            // apply orb of alchemy
            if (!await CurrencyUseHelper.AlchemyItems(RegexCondition)) return false;

            maps = await GetValidItem();
            if (maps == null) return false;
        }

        return true;
    }

    private SyncTask<List<InventoryItemData>> GetValidItem()
    {
        return Scripts.TryGetUsedItems(x =>
            DoneCraftItem.All(s => s.Entity.Address != x.Entity.Address) && !x.IsCorrupted);
    }

    private async SyncTask<bool> ChaosSpam()
    {
        if (Settings.CraftPlace == CraftPlaceType.MousePosition)
        {
            if (!Scripts.TryGetHoveredItem(out var item)) return false;
            if (item.IsCorrupted) return false;

            if (RegexCondition(item)) return true;

            switch (item.Rarity)
            {
                case ItemRarity.Normal:
                    // 1. Use alchemy
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfAlchemy,
                            x => x.Rarity == ItemRarity.Rare))
                        return false;
                    // 2. After alchemy use chaos 
                    return await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.ChaosOrb, RegexCondition);

                case ItemRarity.Magic:
                    // 1. Scouring to Normal 
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfScouring,
                            x => x.Rarity == ItemRarity.Normal))
                        return false;
                    // 2. Alchemy to Rare 
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfAlchemy,
                            x => x.Rarity == ItemRarity.Rare))
                        return false;
                    // 3. spam chaos
                    return await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.ChaosOrb, RegexCondition);
                case ItemRarity.Rare:
                    // 1. spam chaos
                    return await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.ChaosOrb, RegexCondition);
                default:
                    // else return false 
                    return false;
            }
        }

        if (!await CurrencyUseHelper.ScouringItems(x => x.IsMap, RegexCondition))
            return false;
        // apply orb of alchemy
        if (!await CurrencyUseHelper.AlchemyItems(x => x.IsMap, RegexCondition)) return false;
        // chaos spam
        return await CurrencyUseHelper.ChaosSpamItems(x => x.IsMap, RegexCondition);
    }
}