using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RegexCrafter.Utils;

internal static partial class RegexUtils
{
    private static readonly Dictionary<string, Regex> RegexCache = [];

    private static Regex GetRegex(string pattern)
    {
        if (RegexCache.TryGetValue(pattern, out var value)) return value;
        value = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        RegexCache[pattern] = value;
        return value;
    }

    public static bool MatchesPattern(string text, string pattern)
    {
        var regex = GetRegex(pattern);
        return regex.IsMatch(text);
    }

    public static bool MatchesPattern(IEnumerable<string> lines, string pattern)
    {
        var regex = GetRegex(pattern);
        return lines.Any(regex.IsMatch);
    }

    public static bool MatchesAnyPattern(string text, IEnumerable<string> patterns, out List<string> foundPatterns)
    {
        foundPatterns = [];
        var lines = SplitText(text);

        foundPatterns.AddRange(patterns.Where(pattern => lines.Any(line => MatchesPattern(line, pattern))));

        return foundPatterns.Count > 0;
    }

    public static bool MatchesAnyPattern(IEnumerable<string> lines, IEnumerable<string> patterns,
        out List<string> foundPatterns)
    {
        foundPatterns = patterns.Where(pattern => lines.Any(line => SplitAndMatch(line, pattern))).ToList();
        return foundPatterns.Count > 0;
    }

    public static bool MatchesAllPatterns(string text, IEnumerable<string> patterns, out List<string> foundPatterns)
    {
        foundPatterns = [];
        var patternList = patterns.ToList();
        if (patternList.Count == 0) return false;

        var lines = SplitText(text);

        foreach (var pattern in patternList)
        {
            if (!lines.Any(line => MatchesPattern(line, pattern))) return false;
            foundPatterns.Add(pattern);
        }

        return true;
    }

    public static bool MatchesAllPatterns(IEnumerable<string> lines, IEnumerable<string> patterns,
        out List<string> foundPatterns)
    {
        foundPatterns = [];
        var patternList = patterns.ToList();
        if (patternList.Count == 0) return false;

        var lineList = lines.ToList();

        foreach (var pattern in patternList)
        {
            if (!lineList.Any(line => SplitAndMatch(line, pattern))) return false;
            foundPatterns.Add(pattern);
        }

        return true;
    }

    private static string[] SplitText(string text)
    {
        return text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool SplitAndMatch(string line, string pattern)
    {
        var lines = SplitText(line);
        return lines.Any(line2 => MatchesPattern(line2, pattern));
    }

    public static (List<string> Exclude, List<string> Include, List<string> MaxIncludeOnlyOne) ParsedPattern(
        string sourceRegex)
    {
        var matches = MyRegex().Matches(sourceRegex)
            .Select(m => m.Value)
            .ToArray();

        var exclude = new List<string>();
        var include = new List<string>();
        var maxIncludeOnlyOne = new List<string>();

        foreach (var part in matches)
        {
            var trimmedPart = part.Trim('"');
            if (trimmedPart.StartsWith('!'))
                exclude.Add(trimmedPart[1..]);
            else if (trimmedPart.StartsWith('~'))
                maxIncludeOnlyOne.Add(trimmedPart[1..]);
            else if (!string.IsNullOrWhiteSpace(trimmedPart)) include.Add(trimmedPart);
        }

        return (exclude, include, maxIncludeOnlyOne);
    }

    [GeneratedRegex(@"[\""].+?[\""]|[^ ]+")]
    private static partial Regex MyRegex();
}