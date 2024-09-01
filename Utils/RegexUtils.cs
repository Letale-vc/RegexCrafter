using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RegexCrafter.Utils
{
	public class RegexUtils
	{
		public static bool MatchesPattern(string text, string pattern)
		{
			var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
			return regex.IsMatch(text);
		}
		public static bool MatchesAnyPattern(string text, string[] patterns, out List<string> applyPatterns)
		{
			applyPatterns = new List<string>();
			var lines = text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
			foreach (string pattern in patterns)
			{
				foreach (string line in lines)
				{
					if (MatchesPattern(line, pattern))
					{
						applyPatterns.Add(pattern);
						return true;
					}
				}
			}
			return false;
		}

		public static bool MatchesAllPatterns(string text, string[] patterns, out List<string> applyPatterns)
		{
			applyPatterns = new List<string>();
			var lines = text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
			foreach (string pattern in patterns)
			{
				bool patternMatched = false;
				foreach (string line in lines)
				{
					if (MatchesPattern(line, pattern))
					{
						patternMatched = true;
						break;
					}
				}
				if (!patternMatched)
				{
					return false;
				}
				applyPatterns.Add(pattern);
			}
			return true;
		}
	}
}