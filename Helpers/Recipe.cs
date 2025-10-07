using System.Collections.Generic;
using System.Linq;

namespace RegexCrafter.Helpers;

public class Recipe
{
    public readonly List<CraftStep> CraftSteps = [];
    public readonly List<string> MainConditions = [];
    public string BaseUseCondition { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public bool IsMainCondition(string text)
    {
        return MainConditions.Any(condition => Services.RegexFinder.ContainsPatternInText(text, condition));
    }

    public void ResetAll()
    {
        BaseUseCondition = string.Empty;
        CraftSteps.Clear();
        MainConditions.Clear();
        Name = string.Empty;
    }
    public void AddStep(CraftStep step)
    {
        CraftSteps.Add(step);
    }
    public void RemoveStepAt(int index)
    {
        if (index < 0 || index >= CraftSteps.Count)
        {
            return;
        }

        CraftSteps.RemoveAt(index);
    }
    public void RemoveAllSteps()
    {
        CraftSteps.Clear();
    }
    public void AddMainCondition(string condition)
    {
        if (!MainConditions.Contains(condition))
        {
            MainConditions.Add(condition);
        }
    }
    public void RemoveMainCondition(string condition)
    {
        if (MainConditions.Contains(condition))
        {
            MainConditions.Remove(condition);
        }
    }
    public void RemoveMainCondition(int index)
    {
        if (index < 0 || index >= MainConditions.Count)
        {
            return;
        }

        MainConditions.RemoveAt(index);
    }
    public void AddRangeStep(IEnumerable<CraftStep> steps)
    {
        CraftSteps.AddRange(steps);
    }

    public void RemoveStepAt(string currency)
    {
        var step = CraftSteps.FirstOrDefault(s => s.Currency == currency);
        if (step != null)
        {
            CraftSteps.Remove(step);
        }
    }
    public void ShiftStepUp(int index)
    {
        if (index <= 0 || index >= CraftSteps.Count)
        {
            return;
        }

        var step = CraftSteps[index];
        CraftSteps.RemoveAt(index);
        CraftSteps.Insert(index - 1, step);
    }

    public void InsertStep(CraftStep step)
    {
        CraftSteps.Insert(0, step);
    }
    public bool IsBaseUseCondition(string text)
    {
        return Services.RegexFinder.ContainsPatternInText(text, BaseUseCondition);
    }
}
