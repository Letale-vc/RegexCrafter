
using System.Collections.Generic;
using System.Threading;
using ExileCore.Shared;
using RegexCrafter.Utils;

namespace RegexCrafter.Methods;


public abstract class Craft
{
    private static RegexCrafter Core;

    public static Settings Settings => Core.Settings;
    public abstract string Name { get; }
    public List<CustomItemData> BadItems = [];
    public List<CustomItemData> DoneCraftItem = [];
    public static void Init(RegexCrafter core)
    {
        Core = core;
    }

    public virtual void DrawSettings()
    {

    }
    public bool RegexCondition((CustomItemData Item, string Text) hoverItem)
    {

        if (RegexUtils.MatchesAnyPattern(hoverItem.Text, Core.ParsedPattern.Exclude, out var applyPatterns))
        {
            if (Core.Settings.Debug)
            {
                Core.LogMessage($"Excluded: {string.Join(", ", applyPatterns)} \n");
            }
            return false;
        }
        else if (RegexUtils.MatchesAllPatterns(hoverItem.Text, Core.ParsedPattern.Include, out var applyPatterns2))
        {

            if (Core.Settings.Debug)
            {
                Core.LogMsg($"Included: {string.Join(", ", applyPatterns2)} \n");
            }
            DoneCraftItem.Add(hoverItem.Item);
            return true;
        }
        return false;
    }

    public abstract SyncTask<bool> Start(CancellationToken ct);

    public override string ToString()
    {
        return $"{Name}";
    }

}