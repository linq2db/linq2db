using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LinqToDB.Naming
{
	/// <summary>
	/// Base class for name converters.
	/// </summary>
	public abstract class NameConverterBase : INameConversionProvider
	{
		public record NameParts(string? Prefix, string MainWord, string? Suffix);

		public abstract Func<string, string> GetConverter(Pluralization conversion);

		/// <summary>
		/// Identify word in name to convert. Any text before and after word returned and prefix/suffix strings.
		/// </summary>
		/// <param name="name">Name to convert.</param>
		/// <returns>Word to covert with optional suffix and prefix to add after convertsion.</returns>
		protected static NameParts NormalizeName(string name)
		{
			List<string>? suffix = null;

			// extract suffix (all non-letter trailing characters)
			var stop = false;
			foreach (var (chr, cat) in name.EnumerateCharacters().Reverse())
			{
				switch (cat)
				{
					case UnicodeCategory.UppercaseLetter:
					case UnicodeCategory.LowercaseLetter:
					case UnicodeCategory.TitlecaseLetter:
					case UnicodeCategory.ModifierLetter :
					case UnicodeCategory.OtherLetter    :
					case UnicodeCategory.LetterNumber   :
						stop = true;
						break;
					default                             :
						(suffix ??= new()).Insert(0, chr);
						break;
				}

				if (stop)
					break;
			}

			var suffixStr = suffix == null ? null : string.Join(string.Empty, suffix);

			// extract last word in name without suffix
			var word = GetLastWord(suffixStr == null ? name : name.Substring(0, name.Length - suffixStr.Length));

			// calculate last word prefix length
			var prefixLength = name.Length - word.Length - suffixStr?.Length ?? 0;

			return new(
				prefixLength > 0 ? name.Substring(0, prefixLength) : null,
				word,
				suffixStr);
		}

		/// <summary>
		/// Returns last word in name.
		/// </summary>
		/// <param name="name">Multi-word name.</param>
		/// <returns>Last word in name.</returns>
		private static string GetLastWord(string name)
		{
			// word definition:
			// sequence of letters with same case (upper or lower)
			// for lowercase word we also add leading uppercase letter if it is present
			var word    = new List<string>();
			var isLower = true;
			var stop    = false;

			foreach (var (chr, cat) in name.EnumerateCharacters().Reverse())
			{
				switch (cat)
				{
					case UnicodeCategory.UppercaseLetter:
					{
						word.Insert(0, chr);

						if (word.Count == 0)
							isLower = false;
						else if (isLower)
						{
							// uppercase letter before lowercase word - count it as start letter and stop
							stop = true;
						}

						break;
					}
					case UnicodeCategory.LowercaseLetter:
					case UnicodeCategory.TitlecaseLetter:
					case UnicodeCategory.ModifierLetter :
					case UnicodeCategory.OtherLetter    :
					case UnicodeCategory.LetterNumber   :
					{
						if (word.Count == 0 || isLower)
							word.Insert(0, chr);
						else if (!isLower)
						{
							// lowercase letter before uppercase word - ignore and stop
							stop = true;
						}

						break;
					}
					default                             :
						// non-letter character - mission abort
						stop = true;
						break;
				}

				if (stop)
					break;
			}

			return string.Join(string.Empty, word);
		}
	}
}
