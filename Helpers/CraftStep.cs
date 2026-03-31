#nullable enable
using RegexCrafter.Services;

namespace RegexCrafter.Helpers
{
    public class CraftStep
    {
        public string UseCondition { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string StopUseCondition { get; set; } = string.Empty;

        public bool IsOneTimeUse { get; set; } = false;

        public bool IsUseCondition(string text)
        {
            if (string.IsNullOrWhiteSpace(UseCondition)) return true;
            return RegexFinder.ContainsPatternInText(text, UseCondition);
        }

        public bool IsStopUseCondition(string text)
        {
            if (string.IsNullOrWhiteSpace(StopUseCondition)) return false;
            return RegexFinder.ContainsPatternInText(text, StopUseCondition);
        }
    }
}
