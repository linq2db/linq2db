using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LinqToDB.Naming
{
	/// <summary>
	/// Identifier names normalization service. Identify separate words in name and apply requested
	/// pluralization and casing modes.
	/// </summary>
	public sealed class NamingServices
	{
		private readonly INameConversionProvider _pluralizationProvider;

		internal NamingServices(INameConversionProvider pluralizationProvider)
		{
			_pluralizationProvider = pluralizationProvider;
		}

		/// <summary>
		/// Normalize provided identifier using specififed normalization options.
		/// </summary>
		/// <param name="settings">Identifier normalization options.</param>
		/// <param name="name">Identifier.</param>
		/// <returns>Normalized identifier.</returns>
		public string NormalizeIdentifier(NormalizationOptions settings, string name)
		{
			var mixedCase = name.ToUpperInvariant() != name;
			// skip normalization for ALLCAPS names
			if (!settings.DontCaseAllCaps || name.EnumerateCharacters().Any(c => c.category != UnicodeCategory.UppercaseLetter))
			{
				// first split identifier into text/non-text fragments, optionally treat underscore as discarded separator
				var words = SplitIntoWords(
					name,
					settings.Transformation is NameTransformation.SplitByUnderscore or NameTransformation.Association);

				// find last word to apply pluralization to it (if configured)
				var lastTextIndex = -1;
				if (settings.Pluralization != Pluralization.None)
				{
					for (var i = words.Count; i > 0; i--)
					{
						if (words[i - 1].isText)
						{
							lastTextIndex = i - 1;
							break;
						}
					}
				}

				if (settings.PluralizeOnlyIfLastWordIsText && lastTextIndex != words.Count - 1)
					lastTextIndex = -1;

				// recreate identifier from words applying casing rules and pluralization
				var identifier = new StringBuilder();
				var firstWord = true;
				for (var i = 0; i < words.Count; i++)
				{
					var (word, isText, isUpperCase) = words[i];

					if (!isText || (isUpperCase && word.Length <= settings.MaxUpperCaseWordLength))
						identifier.Append(word);
					else
					{
						// apply pluralization (to lowercased form to not confuse pluralizer)
						if (lastTextIndex == i)
						{
							var normalized = word.ToLowerInvariant();
							var toUpperCase = settings.Casing == NameCasing.T4CompatNonPluralized && normalized != word && word == word.ToUpperInvariant();

							word = _pluralizationProvider.GetConverter(settings.Pluralization)(normalized);
							if (toUpperCase)
								word = word.ToUpperInvariant();
						}

						// apply casing rules
						if (isText)
							word = ApplyCasing(word, settings.Casing, firstWord, i == words.Count - 1, mixedCase);

						// append to whole identifier with casing-specific separator (only before text fragment)
						if (isText && settings.Casing == NameCasing.SnakeCase && identifier.Length > 0)
							identifier.Append('_');
						identifier.Append(word);

						firstWord = false;
					}
				}

				name = identifier.ToString();
			}

			// apply fixed prefix/suffix (ignores other options like casing)
			name = settings.Prefix + name + settings.Suffix;

			return name;
		}

		/// <summary>
		/// Re-format word using specified casing.
		/// </summary>
		/// <param name="word">Word to re-format.</param>
		/// <param name="casing">Casing to apply to word.</param>
		/// <param name="firstWord">Indicates that provided word used as first word of identifier.</param>
		/// <param name="lastWord">Indicates that provided word used as last word of identifier.</param>
		/// <param name="mixedCase">Indicates that provided word used in identifier with mixed words casing.</param>
		/// <returns></returns>
		private string ApplyCasing(string word, NameCasing casing, bool firstWord, bool lastWord, bool mixedCase)
		{
			if (casing == NameCasing.None)
				return word;

			if (casing == NameCasing.T4CompatPluralized && !lastWord && word.ToUpperInvariant() == word && word.Length <= 2)
				return word;

			if (casing == NameCasing.T4CompatNonPluralized && mixedCase && word.ToUpperInvariant() == word)
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
							casedWord.Append(chr.ToUpperInvariant());
							break;
						case NameCasing.SnakeCase:
						case NameCasing.LowerCase:
							casedWord.Append(chr.ToLowerInvariant());
							break;
						case NameCasing.CamelCase:
							casedWord.Append(firstWord ? chr.ToLowerInvariant() : chr.ToUpperInvariant());
							break;
					}

					firstLetter = false;
				}
				else
				{
					switch (casing)
					{
						case NameCasing.UpperCase:
							casedWord.Append(chr.ToUpperInvariant());
							break;
						case NameCasing.Pascal:
						case NameCasing.T4CompatPluralized:
						case NameCasing.T4CompatNonPluralized:
						case NameCasing.SnakeCase:
						case NameCasing.CamelCase:
						case NameCasing.LowerCase:
							casedWord.Append(chr.ToLowerInvariant());
							break;
					}
				}

			}

			return casedWord.ToString();
		}

		/// <summary>
		/// Splits string into sub-strings by grouping characters into "words" by:
		/// <list type="bullet">
		/// <item>their letter/non-letter unicode categories;</item>
		/// <item>for string with mixed letter cases, uses uppercase letter as indicator of new word start character;</item>
		/// <item>when <paramref name="removeUnderscores"/> set to <c>true</c>, underscore used as additional separator between words and removed from results</item>
		/// </list>
		/// </summary>
		/// <param name="str">String to split.</param>
		/// <param name="removeUnderscores">Optionally treat underscores as word separators.</param>
		/// <returns>Sequence of pairs word + type of word (text or non-text word).</returns>
		private List<(string word, bool isText, bool isUpperCase)> SplitIntoWords(string str, bool removeUnderscores)
		{
			var results     = new List<(string word, bool isText, bool isUpperCase)>();
			// split text into fragments/sub-texts (not words) by underscore
			var fragments   = removeUnderscores ? str.Split('_') : new []{ str };
			var currentWord = new StringBuilder();

			foreach (var fragment in fragments)
			{
				currentWord.Clear();

				var isText                = false; // current word is text (contains letters)
				var uppercaseWord         = false; // current word contains only uppercase letters
				var splitByUpperCase      = fragment.ToUpperInvariant() != fragment; // apply split-by-uppercase-letter logic to current fragment
				var length                = 0;
				string? previousCharacter = null; // previous identified character in current fragment

				foreach (var (character, category) in fragment.EnumerateCharacters())
				{
					switch (category)
					{
						// various letter unicode categories
						case UnicodeCategory.UppercaseLetter:
						case UnicodeCategory.LowercaseLetter:
						case UnicodeCategory.TitlecaseLetter:
						case UnicodeCategory.ModifierLetter:
						case UnicodeCategory.OtherLetter:
						// don't think it will be correct to include text numbers as letters in context of word
						//case UnicodeCategory.LetterNumber:
						{
							// new word? set word type
							if (currentWord.Length == 0)
								isText = true;
							else if (isText)
							{
								// uppercase letter start new word in mixed-case fragment
								if (splitByUpperCase && category == UnicodeCategory.UppercaseLetter && !uppercaseWord)
								{
									// uppercase letter as word spearator found - save current word and start collect new word
									CommitWord(true);
									isText = true;
								}
								else if (category != UnicodeCategory.UppercaseLetter && uppercaseWord && length > 1)
								{
									// lowercase letter found for uppercase word - save current word and start collect new word
									// Previous upper-case character used as starting character of new word
									currentWord.Length -= previousCharacter!.Length;
									CommitWord(true);
									isText = true;
									currentWord.Append(previousCharacter);
									length++;
								}
							}
							else
							{
								// save current non-text word and start new text word
								CommitWord(false);
								isText = true;
							}

							// set upper-case word flag for uppercase-only character sequence
							uppercaseWord = category == UnicodeCategory.UppercaseLetter && (currentWord.Length == 0 || uppercaseWord);
							currentWord.Append(character);
							length++;
							break;
						}
						default: // non-letter character
						{
							if (currentWord.Length == 0)
								isText = false;
							else if (isText)
								CommitWord(true); // save current text word

							currentWord.Append(character);
							length++;
							break;
						}
					}

					previousCharacter = character;
				}

				if (currentWord.Length > 0)
					results.Add((currentWord.ToString(), isText, uppercaseWord));

				// saves currently collected word and initialize flags for next word
				void CommitWord(bool asText)
				{
					results.Add((currentWord.ToString(), asText, uppercaseWord));
					currentWord.Clear();

					isText        = false;
					uppercaseWord = false;
					length        = 0;
				}
			}

			return results;
		}
	}
}
