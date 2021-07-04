using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LinqToDB.CodeGen.ContextModel;

namespace LinqToDB.CodeGen
{
	public static class StringUtilities
	{
		public static IEnumerable<(string character, UnicodeCategory category)> EnumerateCharacters(this string str)
		{
			for (var i = 0; i < str.Length; i++)
			{
				var cat = CharUnicodeInfo.GetUnicodeCategory(str, i);
				var isSurrogate = char.IsHighSurrogate(str[i]);
				yield return (str.Substring(i, isSurrogate ? 2 : 1), cat);
				if (isSurrogate)
					i++;
			}
		}

		public static List<(string word, bool isText)> SplitIntoWords(this string name, bool removeUnderscores)
		{
			var words = new List<(string word, bool isText)>();

			var parts = removeUnderscores ? name.Split('_') : new []{ name };
			var currentWord = new StringBuilder();
			foreach (var part in parts)
			{
				currentWord.Clear();
				bool isText = false;
				var splitByUpperCase = part.ToUpper() != part;
				var uppercaseWord = false;
				var length = 0;
				string? previousCharacter = null;
				foreach (var (character, category) in part.EnumerateCharacters())
				{
					switch (category)
					{
						case UnicodeCategory.UppercaseLetter:
						case UnicodeCategory.LowercaseLetter:
						case UnicodeCategory.TitlecaseLetter:
						case UnicodeCategory.ModifierLetter:
						case UnicodeCategory.OtherLetter:
							// I don't think it will be correct to include text numbers as letters in context of word
							//case UnicodeCategory.LetterNumber:

							if (currentWord.Length == 0)
								isText = true;
							else if (isText)
							{
								// uppercase letter start new word in mixed-case fragment
								if (splitByUpperCase && category == UnicodeCategory.UppercaseLetter && !uppercaseWord)
								{
									CommitWord(true);
									isText = true;
								}
								else if (category != UnicodeCategory.UppercaseLetter && uppercaseWord && length > 1)
								{
									currentWord.Length -= previousCharacter!.Length;
									CommitWord(true);
									isText = true;
									currentWord.Append(previousCharacter);
									length++;
								}
							}
							else
							{
								CommitWord(false);
								isText = true;
							}

							uppercaseWord = category == UnicodeCategory.UppercaseLetter && (currentWord.Length == 0 || uppercaseWord);
							currentWord.Append(character);
							length++;
							break;
						default:
							if (currentWord.Length == 0)
								isText = false;
							else if (isText)
								CommitWord(true);

							currentWord.Append(character);
							length++;
							break;
					}

					previousCharacter = character;
				}

				if (currentWord.Length > 0)
					words.Add((currentWord.ToString(), isText));

				void CommitWord(bool asText)
				{
					words.Add((currentWord.ToString(), asText));
					currentWord.Clear();
					isText = false;
					uppercaseWord = false;
					length = 0;
				}
			}

			return words;
		}

		public static string Pluralize(this string word, Pluralization mode, INameConverterProvider provider)
		{
			return provider.GetConverter(mode)(word);
		}

		public static string ApplyCasing(this string word, NameCasing casing, bool firstWord, bool lastWord, bool mixedCase)
		{
			if (casing == NameCasing.None)
				return word;

			if (casing == NameCasing.T4CompatPluralized && !lastWord && word.ToUpper() == word && word.Length <= 2)
				return word;

			if (casing == NameCasing.T4CompatNonPluralized && mixedCase && word.ToUpper() == word)
				return word;

			var firstLetter = true;
			var casedWord = new StringBuilder();
			foreach (var (chr, cat) in word.EnumerateCharacters())
			{
				if (firstLetter)
				{
					switch (casing)
					{
						case NameCasing.UpperCase:
						case NameCasing.Pascal:
						case NameCasing.T4CompatPluralized:
						case NameCasing.T4CompatNonPluralized:
							casedWord.Append(chr.ToUpper());
							break;
						case NameCasing.SnakeCase:
						case NameCasing.LowerCase:
							casedWord.Append(chr.ToLower());
							break;
						case NameCasing.CamelCase:
							casedWord.Append(firstWord ? chr.ToLower() : chr.ToUpper());
							break;
					}
					firstLetter = false;
				}
				else
				{
					switch (casing)
					{
						case NameCasing.UpperCase:
							casedWord.Append(chr.ToUpper());
							break;
						case NameCasing.Pascal:
						case NameCasing.T4CompatPluralized:
						case NameCasing.T4CompatNonPluralized:
						case NameCasing.SnakeCase:
						case NameCasing.CamelCase:
						case NameCasing.LowerCase:
							casedWord.Append(chr.ToLower());
							break;
					}
				}
				
			}

			return casedWord.ToString();
		}
	}
}
