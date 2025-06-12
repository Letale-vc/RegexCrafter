using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ImGuiNET;
using RegexCrafter.Helpers;
using RegexCrafter.Helpers.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace RegexCrafter.CraftsMethods;

public class DefaultCraftState : CraftState
{
    public CurrencyMethodCraftType CurrencyMethodCraftType = CurrencyMethodCraftType.Chaos;
}

public class DefaultCraft(RegexCrafter core) : CraftBase<DefaultCraftState>(core)
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

    protected override async SyncTask<bool> Start()
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

        var items = await GetValidItem();

        if (items.Count == 0) return false;

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

    private async SyncTask<List<InventoryItemData>> GetValidItem()
    {
        return (await CraftingPlace.TryGetUsedItemsAsync(x =>
            DoneCraftItem.All(s => s.Entity.Address != x.Entity.Address) && !x.IsCorrupted &&
            x.Rarity != ItemRarity.Unique)).Items;
    }

    private async SyncTask<bool> AlterationSpam()
    {

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

        if (!await CurrencyUseHelper.ScouringItems(x => x.Rarity == ItemRarity.Magic, RegexCondition)) return false;
        // apply orb of alchemy
        if (!await CurrencyUseHelper.AlchemyItems(x => x.Rarity == ItemRarity.Normal, RegexCondition)) return false;
        // chaos spam
        return await CurrencyUseHelper.ChaosSpamItems(RegexCondition);
    }
}