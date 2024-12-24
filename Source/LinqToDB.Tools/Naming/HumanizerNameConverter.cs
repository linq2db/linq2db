using System;

using Humanizer;

namespace LinqToDB.Naming
{
	/// <summary>
	/// Name conversion provider implementation using 
	/// <a href="https://github.com/Humanizr/Humanizer">Humanizer</a> library.
	/// </summary>
	public sealed class HumanizerNameConverter : NameConverterBase
	{
		/// <summary>
		/// Gets converter instance.
		/// </summary>
		public static readonly INameConversionProvider Instance = new HumanizerNameConverter();

		static HumanizerNameConverter()
		{
			Humanizer.Inflections.Vocabularies.Default.AddUncountable("all");
		}

		private HumanizerNameConverter()
		{
		}

		public override Func<string, string> GetConverter(Pluralization conversion)
		{
			return conversion switch
			{
				Pluralization.None                  => None,
				Pluralization.Plural                => ToPlural,
				Pluralization.PluralIfLongerThanOne => ToPluralLongerThanOne,
				Pluralization.Singular              => ToSingular,
				_                                   => throw new NotImplementedException($"Name conversion mode not supported: {conversion}")
			};
		}

		/// <summary>
		/// Noop conversion.
		/// </summary>
		/// <param name="name">Name to convert.</param>
		/// <returns>Converted name.</returns>
		private static string None(string name) => name;

		/// <summary>
		/// Conversion to singular form.
		/// </summary>
		/// <param name="name">Name to convert.</param>
		/// <returns>Converted name.</returns>
		private static string ToSingular(string name)
		{
			var (prefix, mainWord, suffix) = NormalizeName(name);
			return prefix + mainWord.Singularize(inputIsKnownToBePlural: false) + suffix;
		}

		/// <summary>
		/// Conversion to plural form.
		/// </summary>
		/// <param name="name">Name to convert.</param>
		/// <returns>Converted name.</returns>
		private static string ToPlural(string name)
		{
			var (prefix, mainWord, suffix) = NormalizeName(name);
			return prefix + mainWord.Pluralize(inputIsKnownToBeSingular: false) + suffix;
		}

		/// <summary>
		/// Conversion to plural form for words with length greater than one character.
		/// </summary>
		/// <param name="name">Name to convert.</param>
		/// <returns>Converted name.</returns>
		private static string ToPluralLongerThanOne(string name)
		{
			var (prefix, mainWord, suffix) = NormalizeName(name);
			return prefix + (mainWord.Length > 1 ? mainWord.Pluralize(inputIsKnownToBeSingular: false) : mainWord) + suffix;
		}
	}
}
