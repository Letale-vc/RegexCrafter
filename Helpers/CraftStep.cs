#nullable enable
namespace RegexCrafter.Helpers;

public class CraftStep
{
    public string UseCondition { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string StopUseCondition { get; set; } = string.Empty;

    public bool IsOneTimeUse { get; set; } = false;

    public bool IsUseCondition(string text)
    {
        return RegexFinder.ContainsPatternInText(text, UseCondition);
    }

    public bool IsStopUseCondition(string text)
    {
        return RegexFinder.ContainsPatternInText(text, StopUseCondition);
    }
}
