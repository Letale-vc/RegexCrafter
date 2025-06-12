using RegexCrafter.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RegexCrafter;

public class Recipe
{
    public required List<string> Conditions { get; set; } = new();
    public required List<CraftStep> CraftSteps { get; set; } = new();
    public bool IsMainConditionSatisfied(InventoryItemData item)
    {
        return Conditions.Any(condition => RegexFinder.ContainsMatchInText(item.ClipboardText, condition));
    }
    public bool IsStepConditionSatisfied(InventoryItemData item, string condition)
    {
        return RegexFinder.ContainsMatchInText(item.ClipboardText, condition);
    }
}