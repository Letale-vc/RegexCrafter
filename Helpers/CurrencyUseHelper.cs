using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RegexCrafter.Helpers;

public class CurrencyUseHelper(Scripts _scripts)
{
    private const string LogName = "CurrencyUseHelper";

    public async SyncTask<bool> ApplyCurrencyToItems(
        IEnumerable<InventoryItemData> items,
        string currencyName,
        Func<InventoryItemData, bool> itemFilter,
        Func<InventoryItemData, bool> condition,
        string logMessage)
    {
        var filteredItems = items.Where(itemFilter).ToList();

        GlobalLog.Info($"{logMessage}: {filteredItems.Count}", LogName);

        if (filteredItems.Count == 0) return true;

        return await _scripts.UseCurrencyOnMultipleItems(filteredItems, currencyName, condition);
    }


    public async SyncTask<bool> ScouringItems()
    {
        return await _scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.OrbOfScouring,
            x => !x.IsCorrupted && x.Rarity is ItemRarity.Magic or ItemRarity.Rare,
            x => x.Rarity == ItemRarity.Normal);
    }

    public async SyncTask<bool> ScouringItems(Func<InventoryItemData, bool> mainCondition)
    {
        return await _scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.OrbOfScouring,
            x => !x.IsCorrupted && x.Rarity is ItemRarity.Magic or ItemRarity.Rare,
            x => mainCondition(x) || x.Rarity == ItemRarity.Normal);
    }

    public async SyncTask<bool> ScouringItems(Func<InventoryItemData, bool> usedCondition,
        Func<InventoryItemData, bool> mainCondition)
    {
        return await _scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.OrbOfScouring,
            x => usedCondition(x) && !x.IsCorrupted,
            x => mainCondition(x) || x.Rarity == ItemRarity.Normal);
    }

    public async SyncTask<bool> AlchemyItems(Func<InventoryItemData, bool> mainCondition)
    {
        return await _scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.OrbOfAlchemy,
            x => !x.IsCorrupted && x.Rarity == ItemRarity.Normal,
            x => mainCondition(x) || x.Rarity == ItemRarity.Rare);
    }

    public async SyncTask<bool> AlchemyItems(Func<InventoryItemData, bool> usedCondition,
        Func<InventoryItemData, bool> mainCondition)
    {
        return await _scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.OrbOfAlchemy,
            x => usedCondition(x) && !x.IsCorrupted && x.Rarity == ItemRarity.Normal,
            x => mainCondition(x) || x.Rarity == ItemRarity.Rare);
    }


    public async SyncTask<bool> AlchemyItems()
    {
        return await _scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.OrbOfAlchemy,
            x => !x.IsCorrupted && x.Rarity == ItemRarity.Normal,
            x => x.Rarity == ItemRarity.Rare);
    }

    public async SyncTask<bool> ChaosSpamItems(Func<InventoryItemData, bool> usedCondition,
        Func<InventoryItemData, bool> mainCondition)
    {
        return await _scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.ChaosOrb,
            x => usedCondition(x) && !x.IsCorrupted && x.Rarity == ItemRarity.Rare, mainCondition);
    }

    public async SyncTask<bool> ChaosSpamItems(Func<InventoryItemData, bool> mainCondition)
    {
        return await _scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.ChaosOrb,
            x => !x.IsCorrupted && x.Rarity == ItemRarity.Rare, mainCondition);
    }

    public async SyncTask<bool> IdentifyItems(Func<InventoryItemData, bool> mainCondition)
    {
        return await _scripts.UseCurrencyOnMultipleItems(
            CurrencyNames.ScrollOfWisdom,
            x => mainCondition(x) && !x.IsIdentified,
            x => x.IsIdentified);
    }
}