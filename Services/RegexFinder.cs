using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RegexCrafter.Helpers;

namespace RegexCrafter.Services
{
    /// <summary>
    ///     Provides methods for pattern matching and text analysis using regular expressions
    /// </summary>
    public static partial class RegexFinder
    {
        private const string LogName = "RegexFinder";
        private static readonly Dictionary<string, Regex> RegexCache = new();

        /// <summary>
        ///     Gets a cached regex instance or creates a new one for the given pattern
        /// </summary>
        private static Regex GetOrCreateRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (RegexCache.TryGetValue(pattern, out var regex))
            {
                return regex;
            }

            regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
            RegexCache[pattern] = regex;
            return regex;
        }

        /// <summary>
        ///     Checks if text matches the given regex pattern
        /// </summary>
        private static bool IsMatch(string text, string pattern)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(pattern);

            var regex = GetOrCreateRegex(pattern);
            return regex.IsMatch(text);
        }

        #region Pattern Parsing

        /// <summary>
        ///     Parses a pattern string into exclude, include, and single include patterns
        /// </summary>
        public static (string[] Exclude, string[] Include, string[] SingleInclude) ParsePattern(
            string sourceRegex)
        {
            ArgumentNullException.ThrowIfNull(sourceRegex);

            var matches = GetTokenRegex().Matches(sourceRegex);
            var exclude = new List<string>(matches.Count);
            var include = new List<string>(matches.Count);
            var singleInclude = new List<string>(matches.Count);

            foreach (Match match in matches)
            {
                var part = match.Value.Trim('"');
                if (part.StartsWith('!'))
                {
                    exclude.Add(part[1..]);
                }
                else if (part.StartsWith('~'))
                {
                    singleInclude.Add(part[1..]);
                }
                else if (!string.IsNullOrWhiteSpace(part))
                {
                    include.Add(part);
                }
            }

            return (exclude.ToArray(), include.ToArray(), singleInclude.ToArray());
        }

        #endregion

        #region Public Text Analysis Methods

        /// <summary>
        ///     Checks if the text contains any matches for the given pattern
        /// </summary>
        public static bool ContainsPatternInText(string text, string pattern)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(pattern);

            var lines = SplitLines(text);
            return ContainsPatternInLines(lines, pattern);
        }

        /// <summary>
        ///     Checks if the text contains any matches for any of the patterns
        /// </summary>
        public static bool ContainsPatternInText(string text, List<string> patterns)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(patterns);

            var lines = SplitLines(text);
            return patterns.Any(x => ContainsPatternInLines(lines, x));
        }

        #endregion

        #region Pattern Matching Logic

        private static bool ContainsPatternInLines(IEnumerable<string> lines, string patternsLineText)
        {
            ArgumentNullException.ThrowIfNull(lines);
            ArgumentNullException.ThrowIfNull(patternsLineText);

            var linesArray = lines as string[] ?? lines.ToArray();
            var (exclude, include, singleInclude) = ParsePattern(patternsLineText);

            if (exclude.Length == 0 && include.Length == 0 && singleInclude.Length == 0)
            {
                GlobalLog.Error($"Regex pattern is empty: {patternsLineText}", LogName);
                throw new ArgumentException("Pattern cannot be empty", nameof(patternsLineText));
            }

            // Check exclusions first
            if (exclude.Length > 0)
            {
                var result = ContainsAnyPattern(linesArray, exclude, out var foundPatterns);
                GlobalLog.Info(
                    $"Excluded: need find {foundPatterns.Length}/{exclude.Length} \n Found excluded patterns: [{string.Join(", ", foundPatterns)}]",
                    LogName);
                if (result)
                {
                    return false; // If any exclude pattern matches, return false
                }
            }

            // Check single inclusion patterns
            if (singleInclude.Length > 0)
            {
                var result = ContainsAnyPattern(linesArray, singleInclude, out var foundPatterns);
                GlobalLog.Info(
                    $"Include Only one: need find {foundPatterns.Length}/1 \n Found excluded patterns: [{string.Join(", ", foundPatterns)}]",
                    LogName);
                if (result && foundPatterns.Length > 1)
                {
                    return false;
                }
            }

            // Check required inclusion patterns
            if (include.Length > 0)
            {
                var result = ContainsAllPatterns(linesArray, include, out var foundPatterns);
                GlobalLog.Info(
                    $"Include: need find {foundPatterns.Length}/{include.Length} \n Found include patterns: [{string.Join(", ", foundPatterns)}]",
                    LogName);
                return result;
            }

            return true;
        }

        private static bool ContainsAnyPattern(string text, string[] patterns, out string[] foundPatterns)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(patterns);

            var lines = SplitLines(text);
            return ContainsAnyPattern(lines, patterns, out foundPatterns);
        }

        private static bool ContainsAnyPattern(string[] lines, string[] patterns, out string[] foundPatterns)
        {
            ArgumentNullException.ThrowIfNull(lines);
            ArgumentNullException.ThrowIfNull(patterns);

            var found = new List<string>(patterns.Length);
            foreach (var pattern in patterns)
            {
                foreach (var line in lines)
                {
                    if (IsMatch(line, pattern))
                    {
                        found.Add(pattern);
                        break;
                    }
                }
            }

            foundPatterns = found.ToArray();
            return foundPatterns.Length > 0;
        }

        private static bool ContainsAllPatterns(string text, string[] patterns, out string[] foundPatterns)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(patterns);

            var lines = SplitLines(text);
            return ContainsAllPatterns(lines, patterns, out foundPatterns);
        }

        private static bool ContainsAllPatterns(string[] lines, string[] patterns, out string[] foundPatterns)
        {
            ArgumentNullException.ThrowIfNull(lines);
            ArgumentNullException.ThrowIfNull(patterns);

            if (patterns.Length == 0)
            {
                foundPatterns = [];
                return false;
            }

            var found = new List<string>(patterns.Length);
            foreach (var pattern in patterns)
            {
                bool patternFound = false;
                foreach (var line in lines)
                {
                    if (IsMatch(line, pattern))
                    {
                        patternFound = true;
                        break;
                    }
                }

                if (!patternFound)
                {
                    foundPatterns = found.ToArray();
                    return false;
                }

                found.Add(pattern);
            }

            foundPatterns = found.ToArray();
            return true;
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

            return IsMatch(line, pattern);
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
}
