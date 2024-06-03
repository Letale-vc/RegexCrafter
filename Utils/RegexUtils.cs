using System.Text.RegularExpressions;

namespace RegexCrafter.Utils;
public class RegexUtils
{
    public static bool MatchesPattern(string text, string pattern)
    {
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
        return regex.IsMatch(text);
    }

    public static bool MatchesAnyPattern(string text, string[] patterns)
    {
        foreach (string pattern in patterns)
        {
            if (MatchesPattern(text, pattern))
            {
                return true;
            }
        }
        return false;
    }

    public static bool MatchesAllPatterns(string text, string[] patterns)
    {
        foreach (string pattern in patterns)
        {
            if (!MatchesPattern(text, pattern))
            {
                return false;
            }
        }
        return true;
    }
}