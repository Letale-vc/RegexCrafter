
using System.Collections.Generic;
using System.Threading;
using ExileCore.Shared;
using RegexCrafter.Utils;

namespace RegexCrafter.Methods;


public abstract class Craft
{
    private static Settings _settings;

    public static Settings Settings => _settings;
    private static (string[] Exclude, string[] Include) _parsedPattern;
    public abstract string Name { get; }
    public List<CustomItemData> BadItems = [];
    public List<CustomItemData> DoneCraftItem = [];
    public static void Init(RegexCrafter core)
    {
        _settings = core.Settings;
        _parsedPattern = core.ParsedPattern;
    }

    public virtual void DrawSettings()
    {

    }
    public bool RegexCondition((CustomItemData Item, string Text) hoverItem)
    {
        var excludeResult = RegexUtils.MatchesAnyPattern(hoverItem.Text, _parsedPattern.Exclude);
        var includeResult = RegexUtils.MatchesAllPatterns(hoverItem.Text, _parsedPattern.Include);
        var result = !excludeResult && includeResult;
        if (result)
        {
            DoneCraftItem.Add(hoverItem.Item);
        }
        return result;
    }
    public abstract SyncTask<bool> Start(CancellationToken ct);

    public override string ToString()
    {
        return $"{Name}";
    }

}