using System.Text.RegularExpressions;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	public static partial class StringExtensions
	{
		public static string ToSnakeCase(this string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

#if NET8_0_OR_GREATER
			var startUnderscores = UnderscoresMatcher().Match(input);
#pragma warning disable CA1308 // Normalize strings to uppercase
			return startUnderscores + Replacer().Replace(input, "$1_$2").ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
#else
			var startUnderscores = Regex.Match(input, @"^_+");
#pragma warning disable CA1308 // Normalize strings to uppercase
			return startUnderscores + Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
#endif
		}

#if NET8_0_OR_GREATER
		[GeneratedRegex("^_+")]
		private static partial Regex UnderscoresMatcher();
		[GeneratedRegex("([a-z0-9])([A-Z])")]
		private static partial Regex Replacer();
#endif
	}
}
