using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RegexCrafter.Utils;
public class RegexUtils
{
	public static bool MatchesPattern(string text, string pattern)
	{
		var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
		return regex.IsMatch(text);
	}

	public static bool MatchesAnyPattern(string text, string[] patterns, out List<string> applyPatterns)
	{
		applyPatterns = [];
		foreach (string pattern in patterns)
		{
			if (MatchesPattern(text, pattern))
			{
				applyPatterns.Add(pattern);
				return true;
			}
		}
		return false;
	}

	public static bool MatchesAllPatterns(string text, string[] patterns, out List<string> applyPatterns)
	{
		applyPatterns = [];
		foreach (string pattern in patterns)
		{
			if (!MatchesPattern(text, pattern))
			{
				return false;
			}
			applyPatterns.Add(pattern);
		}
		return true;
	}
}