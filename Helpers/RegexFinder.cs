using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RegexCrafter.Helpers;

/// <summary>
/// Provides methods for pattern matching and text analysis using regular expressions
/// </summary>
public static partial class RegexFinder
{
    private static readonly Dictionary<string, Regex> RegexCache = new();

    /// <summary>
    /// Gets a cached regex instance or creates a new one for the given pattern
    /// </summary>
    private static Regex GetOrCreateRegex(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentNullException(nameof(pattern));

        if (RegexCache.TryGetValue(pattern, out var regex))
            return regex;

        regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        RegexCache[pattern] = regex;
        return regex;
    }

    /// <summary>
    /// Checks if text matches the given regex pattern
    /// </summary>
    private static bool IsMatch(string text, string pattern)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(pattern);

        var regex = GetOrCreateRegex(pattern);
        return regex.IsMatch(text);
    }

    #region Public Text Analysis Methods

    /// <summary>
    /// Checks if the text contains any matches for the given pattern
    /// </summary>
    public static bool ContainsMatchInText(string text, string pattern)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(pattern);

        var lines = SplitLines(text);
        return ContainsMatchInLines(lines, pattern);
    }

    /// <summary>
    /// Checks if the text contains any matches for any of the patterns
    /// </summary>
    public static bool ContainsMatchInText(string text, List<string> patterns)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(patterns);

        var lines = SplitLines(text);
        return patterns.Any(x => ContainsMatchInLines(lines, x));
    }

    #endregion

    #region Pattern Matching Logic

    public static bool ContainsMatchInLines(IEnumerable<string> lines, string pattern)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(pattern);

        var linesList = lines.ToList();
        var (exclude, include, singleInclude) = ParsePattern(pattern);

        // Check exclusions first
        if (exclude.Count > 0)
        {
            if (ContainsAnyPattern(linesList, exclude, out _))
                return false;
        }

        // Check single inclusion patterns
        if (singleInclude.Count > 0)
        {
            ContainsAnyPattern(linesList, singleInclude, out var singleFound);
            if (singleFound.Count > 1)
                return false;
        }

        // Check required inclusion patterns
        if (include.Count > 0)
        {
            return ContainsAllPatterns(linesList, include, out _);
        }
        return true;
    }

    public static bool ContainsAnyPattern(string text, IEnumerable<string> patterns, out List<string> foundPatterns)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(patterns);

        var lines = SplitLines(text);
        return ContainsAnyPattern(lines, patterns, out foundPatterns);
    }
    public static bool ContainsAnyPattern(IEnumerable<string> lines, IEnumerable<string> patterns, out List<string> foundPatterns)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(patterns);

        foundPatterns = patterns
            .Where(p => lines.Any(line => LineMatches(line, p)))
            .ToList();
        return foundPatterns.Count > 0;
    }
    public static bool ContainsAllPatterns(string text, IEnumerable<string> patterns, out List<string> foundPatterns)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(patterns);

        var lines = SplitLines(text);
        return ContainsAllPatterns(lines, patterns, out foundPatterns);
    }

    public static bool ContainsAllPatterns(IEnumerable<string> lines, IEnumerable<string> patterns, out List<string> foundPatterns)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(patterns);

        foundPatterns = new List<string>();
        var patternsList = patterns.ToList();
        if (patternsList.Count == 0)
            return false;

        foreach (var pattern in patternsList)
        {
            if (!lines.Any(line => LineMatches(line, pattern)))
                return false;
            foundPatterns.Add(pattern);
        }

        return true;
    }

    #endregion

    #region Pattern Parsing

    /// <summary>
    /// Parses a pattern string into exclude, include, and single include patterns
    /// </summary>
    public static (List<string> Exclude, List<string> Include, List<string> SingleInclude) ParsePattern(string sourceRegex)
    {
        ArgumentNullException.ThrowIfNull(sourceRegex);

        var tokens = GetTokenRegex()
            .Matches(sourceRegex)
            .Select(m => m.Value)
            .ToArray();

        var exclude = new List<string>();
        var include = new List<string>();
        var singleInclude = new List<string>();

        foreach (var token in tokens)
        {
            var part = token.Trim('"');
            if (part.StartsWith('!'))
                exclude.Add(part[1..]);
            else if (part.StartsWith('~'))
                singleInclude.Add(part[1..]);
            else if (!string.IsNullOrWhiteSpace(part))
                include.Add(part);
        }

        return (exclude, include, singleInclude);
    }

    #endregion

    #region Helper Methods

    private static string[] SplitLines(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool LineMatches(string line, string pattern)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(pattern);

        var subLines = SplitLines(line);
        return subLines.Any(l => IsMatch(l, pattern));
    }

    private static string NormalizeText(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.Contains(' ')
            ? input.ToLower()
            : GetTokenRegex().Replace(input, " $1").ToLower();
    }

    [GeneratedRegex(@"[\""].+?[\""]|[^ ]+")]
    private static partial Regex GetTokenRegex();

    #endregion
}