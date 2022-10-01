using System.Collections.Generic;
using System.Globalization;

namespace LinqToDB
{
	/// <summary>
	/// Contains text helpers.
	/// </summary>
	internal static class StringUtilities
	{
		/// <summary>
		/// Splits string into code-points (including surrogate pairs) with corresponding <see cref="UnicodeCategory"/> for each code-point.
		/// </summary>
		/// <param name="str">String to split.</param>
		/// <returns>Sequence of code point + unicode category for provided string.</returns>
		public static IEnumerable<(string codePoint, UnicodeCategory category)> EnumerateCharacters(this string str)
		{
			// currently we ignore existense of modifiers and work with code points directly
			// hardly it will change in future, as people should use some common sense when naming things
			// or be prepared to suffer
			for (var i = 0; i < str.Length; i++)
			{
				var cat             = CharUnicodeInfo.GetUnicodeCategory(str, i);
				var isSurrogatePair = char.IsHighSurrogate(str[i]) && cat != UnicodeCategory.Surrogate;

				// we can safely use 2 here without checking string length as isSurrogatePair will be false otherwise
				yield return (str.Substring(i, isSurrogatePair ? 2 : 1), cat);

				if (isSurrogatePair)
					i++;
			}
		}
	}
}
