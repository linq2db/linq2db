using System.Globalization;
using System.Linq;
using System.Text;
using LinqToDB.CodeGen.ContextModel;

namespace LinqToDB.CodeGen.Model
{
	public class NamingServices
	{
		private readonly ILanguageProvider _languageProvider;
		private readonly INameConverterProvider _pluralizationProvider;

		public NamingServices(ILanguageProvider langServices, INameConverterProvider pluralizationProvider)
		{
			_languageProvider = langServices;
			_pluralizationProvider = pluralizationProvider;
		}

		public string NormalizeIdentifier(ObjectNormalizationSettings settings, string name)
		{
			//if (name == string.Empty)
			//	name = settings.DefaultValue ?? name;

			var mixedCase = name.ToUpper() != name;
			// skip normalization for ALLCAPS names
			if (!settings.DontCaseAllCaps || name.EnumerateCharacters().Any(c => c.category != UnicodeCategory.UppercaseLetter))
			{
				// first split identifier into text/non-text fragments, optionally treat underscore as discarded separator
				var words = name.SplitIntoWords(settings.Transformation == NameTransformation.SplitByUnderscore
							|| settings.Transformation == NameTransformation.T4Association);

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
					var (word, isText) = words[i];

					if (!isText)
						identifier.Append(word);
					else
					{
						// apply pluralization (to lowercased form to not confuse pluralizer)
						if (lastTextIndex == i)
						{
							var normalized = word.ToLower();
							var toUpperCase = settings.Casing == NameCasing.T4CompatNonPluralized && normalized != word && word == word.ToUpper();
							word = normalized.Pluralize(settings.Pluralization, _pluralizationProvider);
							if (toUpperCase)
								word = word.ToUpper();
						}

						// apply casing rules
						if (isText)
							word = word.ApplyCasing(settings.Casing, firstWord, i == words.Count - 1, mixedCase);

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
	}
}
