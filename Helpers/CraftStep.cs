#nullable enable
namespace RegexCrafter.Helpers;

public abstract class CraftStep
{
    public string CondionUse { get; set; }
    public string Currency { get; set; }
    public string Condition { get; set; }
    public int? GotoStep { get; set; }
}
