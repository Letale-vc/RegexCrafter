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
    public CurrencyMethodCraftType CurrencyMethodCraftType = CurrencyMethodCraftType.Chaos;
}

public class DefaultCraft(RegexCrafter core) : Craft<DefaultCraftState>(core)
{
    private const string LogName = "Default Craft";

    private readonly CurrencyMethodCraftType[] _typeMethodCraft =
    [
        CurrencyMethodCraftType.Chaos, CurrencyMethodCraftType.ScouringAndAlchemy,
        CurrencyMethodCraftType.AlterationSpam
    ];

    public override DefaultCraftState CraftState { get; set; } = new();

    public override string Name { get; } = "Default craft";

    public override void DrawSettings()
    {
        base.DrawSettings();
        var selectedMethod = (int)CraftState.CurrencyMethodCraftType;
        if (ImGui.Combo("Type Method craft", ref selectedMethod,
                _typeMethodCraft.Select(x => x.GetDescription()).ToArray(), _typeMethodCraft.Length))
            CraftState.CurrencyMethodCraftType = (CurrencyMethodCraftType)selectedMethod;
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
        switch (CraftState.CurrencyMethodCraftType)
        {
            case CurrencyMethodCraftType.Chaos:
                return await ChaosSpam();
            case CurrencyMethodCraftType.ScouringAndAlchemy:
                return await ScouringAndAlchemy();
            case CurrencyMethodCraftType.AlterationSpam:
                return await AlterationSpam();
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
                                return resCondition || x.Rarity == ItemRarity.Normal;
                            }))
                        return false;

                if (item.Rarity != ItemRarity.Normal) continue;

                if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfAlchemy,
                        x =>
                        {
                            item = x;
                            resCondition = RegexCondition(x);
                            return resCondition || x.Rarity == ItemRarity.Rare;
                        })) return false;
            }

            return true;
        }

        var items = await GetValidItem();

        if (items == null) return false;

        while (items.Count > 0)
        {
            CancellationToken.ThrowIfCancellationRequested();
            //apply scouring
            if (!await CurrencyUseHelper.ScouringItems(RegexCondition)) return false;

            // apply orb of alchemy
            if (!await CurrencyUseHelper.AlchemyItems(RegexCondition)) return false;

            items = await GetValidItem();
            if (items == null) return false;
        }

        return true;
    }

    private SyncTask<List<InventoryItemData>> GetValidItem()
    {
        return Scripts.TryGetUsedItems(x =>
            DoneCraftItem.All(s => s.Entity.Address != x.Entity.Address) && !x.IsCorrupted &&
            x.Rarity != ItemRarity.Unique);
    }

    private async SyncTask<bool> AlterationSpam()
    {
        #region Mouse position use

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

            if (item.Rarity == ItemRarity.Unique || item.IsCorrupted) return false;
            var resCondition = RegexCondition(item);
            if (resCondition) return true;

            switch (item.Rarity)
            {
                case ItemRarity.Normal:
                    // 1. Use transmutation 
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfTransmutation,
                            x => UpdateItemAndCondition(x) || x.Rarity == ItemRarity.Magic))
                        return false;
                    break;
                case ItemRarity.Magic:
                    break;
                case ItemRarity.Rare:
                    // 1. Use scouring
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfScouring,
                            x => UpdateItemAndCondition(x) || x.Rarity == ItemRarity.Normal))
                        return false;
                    // 2. Use Transmutation
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfTransmutation,
                            x => UpdateItemAndCondition(x) || x.Rarity == ItemRarity.Magic))
                        return false;
                    break;
                default:
                    return false;
            }

            if (resCondition) return true;

            // end. spam alteration
            return await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfAlteration, RegexCondition);

            bool UpdateItemAndCondition(InventoryItemData x)
            {
                item = x;
                resCondition = RegexCondition(x);
                return resCondition;
            }
        }

        #endregion

        if (!await CurrencyUseHelper.ScouringItems(x => x.Rarity == ItemRarity.Rare && !x.IsCorrupted,
                x => x.Rarity == ItemRarity.Normal)) return false;
        // use transmutation
        if (!await Scripts.UseCurrencyOnMultipleItems(CurrencyNames.OrbOfTransmutation,
                x => x.Rarity == ItemRarity.Normal,
                x => x.Rarity == ItemRarity.Magic))
            return false;
        // use alteration
        if (!await Scripts.UseCurrencyOnMultipleItems(CurrencyNames.OrbOfAlteration,
                x => x.Rarity == ItemRarity.Magic,
                RegexCondition))
            return false;

        return true;
    }

    private async SyncTask<bool> ChaosSpam()
    {
        #region Mouse position use

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

            if (item.IsCorrupted || item.Rarity == ItemRarity.Unique) return false;

            var resCondition = RegexCondition(item);
            if (resCondition) return true;

            switch (item.Rarity)
            {
                case ItemRarity.Normal:
                    // 1. Use alchemy
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfAlchemy,
                            x => UpdateItemAndCondition(x) && x.Rarity == ItemRarity.Rare))
                        return false;
                    break;
                case ItemRarity.Magic:
                    // 1. Scouring to Normal 
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfScouring,
                            x => UpdateItemAndCondition(x) && x.Rarity == ItemRarity.Normal))
                        return false;
                    // 2. Alchemy to Rare 
                    if (!await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.OrbOfAlchemy,
                            x => UpdateItemAndCondition(x) && x.Rarity == ItemRarity.Rare))
                        return false;
                    break;
                case ItemRarity.Rare:
                    break;
                default:
                    // else return false 
                    return false;
            }

            return await Scripts.UseCurrencyToSingleItem(item, CurrencyNames.ChaosOrb, RegexCondition);

            bool UpdateItemAndCondition(InventoryItemData x)
            {
                item = x;
                resCondition = RegexCondition(x);
                return resCondition;
            }
        }

        #endregion

        if (!await CurrencyUseHelper.ScouringItems(x => x.Rarity == ItemRarity.Magic, RegexCondition)) return false;
        // apply orb of alchemy
        if (!await CurrencyUseHelper.AlchemyItems(x => x.Rarity == ItemRarity.Normal, RegexCondition)) return false;
        // chaos spam
        return await CurrencyUseHelper.ChaosSpamItems(RegexCondition);
    }
}