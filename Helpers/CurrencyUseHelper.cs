using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.Shared;
using ExileCore.Shared.Enums;

namespace RegexCrafter.Helpers;

public static class CurrencyUseHelper
{
    private const string LogName = "CurrencyUseHelper";

    private static RegexCrafter _core;

    public static void Init(RegexCrafter core)
    {
        _core = core;
    }

    public static async SyncTask<bool> ApplyCurrencyToItems(
        IEnumerable<InventoryItemData> items,
        string currencyName,
        Func<InventoryItemData, bool> itemFilter,
        Func<InventoryItemData, bool> condition,
        string logMessage)
    {
        var filteredItems = items.Where(itemFilter).ToList();

        GlobalLog.Info($"{logMessage}: {filteredItems.Count}", LogName);

        if (filteredItems.Count == 0) return true;

        return await Scripts.UseCurrencyOnMultipleItems(filteredItems, currencyName, condition);
    }

    public static async SyncTask<bool> TakeForUse(string currencyName)
    {
        if (Stash.CurrentTab.TryGetItem(item => item.BaseName == currencyName, out var currency))
            return await currency.MoveAndTakeForUse();

        if (Stash.CurrentTab.TabType == InventoryType.CurrencyStash)
        {
            if (!await CurrencyTabHelper.SwitchCurrencyTab())
            {
                GlobalLog.Error("Can't switch Currency tab.", LogName);
                return false;
            }

            if (Stash.CurrentTab.TryGetItem(item => item.BaseName == currencyName, out currency))
                return await currency.MoveAndTakeForUse();
        }

        GlobalLog.Error($"No {currencyName} found.", LogName);
        return false;
    }

    public static async SyncTask<bool> ScouringItems()
    {
        return await Scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.OrbOfScouring,
            x => !x.IsCorrupted && x.Rarity is ItemRarity.Magic or ItemRarity.Rare,
            x => x.Rarity == ItemRarity.Normal);
    }

    public static async SyncTask<bool> ScouringItems(Func<InventoryItemData, bool> mainCondition)
    {
        return await Scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.OrbOfScouring,
            x => !x.IsCorrupted && x.Rarity is ItemRarity.Magic or ItemRarity.Rare,
            x => mainCondition(x) || x.Rarity == ItemRarity.Normal);
    }

    public static async SyncTask<bool> ScouringItems(Func<InventoryItemData, bool> usedCondition,
        Func<InventoryItemData, bool> mainCondition)
    {
        return await Scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.OrbOfScouring,
            x => usedCondition(x) && !x.IsCorrupted,
            x => mainCondition(x) || x.Rarity == ItemRarity.Normal);
    }

    public static async SyncTask<bool> AlchemyItems(Func<InventoryItemData, bool> mainCondition)
    {
        return await Scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.OrbOfAlchemy,
            x => !x.IsCorrupted && x.Rarity == ItemRarity.Normal,
            x => mainCondition(x) || x.Rarity == ItemRarity.Rare);
    }

    public static async SyncTask<bool> AlchemyItems(Func<InventoryItemData, bool> usedCondition,
        Func<InventoryItemData, bool> mainCondition)
    {
        return await Scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.OrbOfAlchemy,
            x => usedCondition(x) && !x.IsCorrupted && x.Rarity == ItemRarity.Normal,
            x => mainCondition(x) || x.Rarity == ItemRarity.Rare);
    }


    public static async SyncTask<bool> AlchemyItems()
    {
        return await Scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.OrbOfAlchemy,
            x => !x.IsCorrupted && x.Rarity == ItemRarity.Normal,
            x => x.Rarity == ItemRarity.Rare);
    }

    public static async SyncTask<bool> ChaosSpamItems(Func<InventoryItemData, bool> usedCondition,
        Func<InventoryItemData, bool> mainCondition)
    {
        return await Scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.ChaosOrb,
            x => usedCondition(x) && !x.IsCorrupted && x.Rarity == ItemRarity.Rare, mainCondition);
    }

    public static async SyncTask<bool> ChaosSpamItems(Func<InventoryItemData, bool> mainCondition)
    {
        return await Scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.ChaosOrb,
            x => !x.IsCorrupted && x.Rarity == ItemRarity.Rare, mainCondition);
    }

    public static async SyncTask<bool> IdentifyItems()
    {
        return await Scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.ScrollOfWisdom,
            x => !x.IsIdentified,
            x => x.IsIdentified);
    }
}