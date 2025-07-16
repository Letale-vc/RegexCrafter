using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ImGuiNET;
using RegexCrafter.Helpers;
using RegexCrafter.Helpers.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace RegexCrafter.CraftsMethods;

public class AnyCurrencyState
{
    public string CurrencyName = string.Empty;
    public List<string> CurrencyUseCondition = [];
    public List<string> StopUseCondition = [];
}

public class CustomCraftState : CraftState
{
    public List<AnyCurrencyState> AnyCurrencyUseList = [];
}

public class CustomCraft(RegexCrafter core) : CraftBase<CustomCraftState>(core)
{
    private const string LogName = "Custom Craft";

    private readonly CurrencyMethodCraftType[] _typeMethodCraft =
        [CurrencyMethodCraftType.Chaos, CurrencyMethodCraftType.ScouringAndAlchemy];

    public override CustomCraftState CraftState { get; set; } = new();
    public override string Name { get; } = "Custom Craft";

    public override void DrawSettings()
    {
        base.DrawSettings();
        ImGui.Separator();
        for (var i = 0; i < CraftState.AnyCurrencyUseList.Count; i++)
        {
            var currencyUseTemp = CraftState.AnyCurrencyUseList[i];

            var isOpen = ImGui.CollapsingHeader($"Currency {i + 1}: {currencyUseTemp.CurrencyName}###Header_{i}");

            if (isOpen)
            {
                ImGui.Dummy(new Vector2(0, 10));
                ImGui.InputText($"###CurrencyName_{i}", ref currencyUseTemp.CurrencyName, 1024);

                ImGui.Text("Currency use condition:");

                for (var ucc = 0; ucc < currencyUseTemp.CurrencyUseCondition.Count; ucc++)
                {
                    var uccTemp = currencyUseTemp.CurrencyUseCondition[ucc];

                    if (ImGui.InputText($"###UseCond_{i}_{ucc}", ref uccTemp, 1024))
                        currencyUseTemp.CurrencyUseCondition[ucc] = uccTemp;

                    ImGui.SameLine();
                    if (ImGui.Button($"Delete###DelUseCond_{i}_{ucc}"))
                    {
                        currencyUseTemp.CurrencyUseCondition.RemoveAt(ucc);
                        ucc--; // correct index after remove
                    }
                }

                if (ImGui.Button($"Add###AddUseCond_{i}")) currencyUseTemp.CurrencyUseCondition.Add(string.Empty);

                ImGui.Dummy(new Vector2(0, 10));
                ImGui.Separator();
                ImGui.Dummy(new Vector2(0, 10));
                ImGui.Text("Stop used currency condition:");

                for (var suc = 0; suc < currencyUseTemp.StopUseCondition.Count; suc++)
                {
                    var cdcTemp = currencyUseTemp.StopUseCondition[suc];

                    if (ImGui.InputText($"###StopCond_{i}_{suc}", ref cdcTemp, 1024))
                        currencyUseTemp.StopUseCondition[suc] = cdcTemp;

                    ImGui.SameLine();
                    if (ImGui.Button($"Delete###DelStopCond_{i}_{suc}"))
                    {
                        currencyUseTemp.StopUseCondition.RemoveAt(suc);
                        suc--; // correct index after remove
                    }
                }

                if (ImGui.Button($"Add###AddStopCond_{i}")) currencyUseTemp.StopUseCondition.Add(string.Empty);
                ImGui.Dummy(new Vector2(0, 10));
                CraftState.AnyCurrencyUseList[i] = currencyUseTemp;
                if (ImGui.Button($"Delete currency {i + 1}###AnyCurrencyState_{i}_{currencyUseTemp.CurrencyName}"))
                {
                    CraftState.AnyCurrencyUseList.RemoveAt(i);
                    i--; // correct index after remove
                }
            }
        }

        ImGui.Dummy(new Vector2(0, 20));
        if (ImGui.Button("Add currency use ##AddCurrencyUse"))
            CraftState.AnyCurrencyUseList.Add(new AnyCurrencyState());
        ImGui.Dummy(new Vector2(0, 20));
        ImGui.LabelText("##MainConditionLabel", "Main conditions:");
        for (var i = 0; i < CraftState.RegexPatterns.Count; i++)
        {
            var patternTemp = CraftState.RegexPatterns[i];
            if (ImGui.InputText($"Your regex pattern {i}", ref patternTemp, 1024))
                CraftState.RegexPatterns[i] = patternTemp;
            ImGui.SameLine();
            if (!ImGui.Button($"Remove##{i}")) continue;
            GlobalLog.Debug($"Remove pattern:{CraftState.RegexPatterns[i]}.", LogName);
            CraftState.RegexPatterns.RemoveAt(i);
        }

        if (ImGui.Button("Add main condition")) CraftState.RegexPatterns.Add(string.Empty);
    }

    private bool CurrencyCondition(InventoryItemData item, IEnumerable<string> patterns)
    {
        GlobalLog.Debug($"### Clipboard text: \n {item.ClipboardText}", LogName);
        if (string.IsNullOrEmpty(item.ClipboardText))
        {
            throw new ArgumentException("Clipboard text is empty or null.", nameof(item.ClipboardText));

        }

        foreach (var pattern in patterns)
        {
            var (exclude, include, maxIncludeOnlyOne) = RegexFinder.ParsePattern(pattern);

            if (exclude.Count > 0)
            {
                var excludeResult = RegexFinder.ContainsAnyPattern(item.ClipboardText, exclude, out var foundPatterns);
                GlobalLog.Info(
                    $"Excluded: need find {foundPatterns.Count}/{exclude.Count} \n Found excluded patterns: [{string.Join(", ", foundPatterns)}]",
                    LogName);
                if (excludeResult) continue;
            }

            if (maxIncludeOnlyOne.Count > 0)
            {
                RegexFinder.ContainsAnyPattern(item.ClipboardText, maxIncludeOnlyOne, out var foundPatterns2);
                if (foundPatterns2.Count > 1)
                {
                    GlobalLog.Info(
                        $"Include Only one: need find {foundPatterns2.Count}/1 \n Found excluded patterns: [{string.Join(", ", foundPatterns2)}]",
                        LogName);
                    continue;
                }
            }

            if (include.Count > 0)
            {
                var includeResult = RegexFinder.ContainsAllPatterns(item.ClipboardText, include, out var foundPatterns3);
                if (!includeResult)
                {
                    GlobalLog.Info(
                        $"Include: need find {foundPatterns3.Count}/{include.Count} \n Found include patterns: [{string.Join(", ", foundPatterns3)}]",
                        LogName);
                    continue;
                }
            }
            return true;
        }

        return false;
    }

    protected override async SyncTask<bool> Start()
    {
        GlobalLog.Debug($"### Start method entered. Craft place: {Settings.CraftPlace}", LogName);

        if (Settings.CraftPlace == CraftPlaceType.MousePosition)
        {
            GlobalLog.Debug("Checking mouse position crafting...", LogName);

            var (isSuccess, item) = await Scripts.WaitForHoveredItem(
                hoverItem => hoverItem != null,
                "Get the initial hovered item");

            if (!isSuccess)
            {
                GlobalLog.Error("### No hovered item found!", LogName);
                return false;
            }

            var resCondition = RegexCondition(item);

            GlobalLog.Info($"### Hovered item: {item?.BaseName} (Address: {item?.Entity.Address})", LogName);

            if (resCondition)
            {
                GlobalLog.Info("### Initial regex condition already met! Returning success.", LogName);
                return true;
            }


            const int maxAttempts = 3000;
            var attempts = 0;
            GlobalLog.Debug($"### Starting crafting loop. Max attempts: {maxAttempts}", LogName);

            while (!resCondition && attempts < maxAttempts)
            {
                GlobalLog.Debug($"### Attempt {attempts + 1}/{maxAttempts}", LogName);
                await TaskUtils.NextFrame();
                CancellationToken.ThrowIfCancellationRequested();
                GlobalLog.Debug("### Cancellation token check passed", LogName);

                foreach (var currencyUse in CraftState.AnyCurrencyUseList)
                {
                    await TaskUtils.NextFrame();
                    GlobalLog.Debug($"### Processing currency: {currencyUse.CurrencyName}", LogName);
                    CancellationToken.ThrowIfCancellationRequested();

                    var currencyConditionResult = CurrencyCondition(item, currencyUse.CurrencyUseCondition);
                    GlobalLog.Debug($"### Currency condition result: {currencyConditionResult} " +
                                    $"(Conditions: {string.Join(", ", currencyUse.CurrencyUseCondition)})", LogName);

                    if (!currencyConditionResult)
                    {
                        GlobalLog.Debug("### Currency conditions not met, skipping", LogName);
                        continue;
                    }

                    GlobalLog.Info($"### Trying to use currency: {currencyUse.CurrencyName}", LogName);
                    var useResult = await Scripts.UseCurrencyToSingleItem(
                        item,
                        currencyUse.CurrencyName,
                        UpdateItemAndCondition);

                    GlobalLog.Debug($"### Currency use result: {useResult}", LogName);

                    if (!useResult)
                    {
                        GlobalLog.Error($"### Failed to use currency: {currencyUse.CurrencyName}", LogName);
                        throw new Exception($"Failed to use currency: {currencyUse.CurrencyName}");
                    }

                    GlobalLog.Info($"### Successfully used currency: {currencyUse.CurrencyName}", LogName);

                    resCondition = RegexCondition(item);
                    if (resCondition)
                    {
                        GlobalLog.Info("### Main regex condition met after currency use", LogName);
                        break;
                    }

                }


                attempts++;
                GlobalLog.Debug($"### Completed attempt {attempts}. Current regex status: {resCondition}",
                    LogName);
            }

            if (attempts >= maxAttempts)
            {
                GlobalLog.Error("### Reached max attempts limit!", LogName);
                throw new TimeoutException("Reached max attempts limit for crafting.");
            }

            GlobalLog.Info("### Crafting completed successfully!", LogName);
            return true;

            bool UpdateItemAndCondition(InventoryItemData x)
            {
                item = x;
                var regexCondition = RegexCondition(x);
                var stopCondition = CurrencyCondition(x, CraftState.AnyCurrencyUseList[0].StopUseCondition);
                GlobalLog.Debug($"### Stop condition check: " +
                                $"Regex={regexCondition}, Stop={stopCondition}", LogName);
                return regexCondition || stopCondition;
            }
        }

        GlobalLog.Error("### Other craft places not supported!", LogName);
        return false;
    }

    private async SyncTask<List<InventoryItemData>> GetValidItem()
    {
        var (Succes, Items) = await CraftingPlace.TryGetUsedItemsAsync(x => DoneCraftItem.All(s => s.Entity.Address != x.Entity.Address) && !x.IsCorrupted);
        return Items;
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