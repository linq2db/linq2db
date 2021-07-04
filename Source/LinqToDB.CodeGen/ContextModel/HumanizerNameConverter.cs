using System;
using Humanizer;

namespace LinqToDB.CodeGen.ContextModel
{
	public class HumanizerNameConverter : NameConverterBase
	{
		public override Func<string, string> GetConverter(Pluralization conversion)
		{
			return conversion switch
			{
				Pluralization.None => None,
				Pluralization.Plural => ToPlural,
				Pluralization.PluralIfLongerThanOne => ToPluralLongerThanOne,
				Pluralization.Singular => ToSingular,
				_ => throw new NotImplementedException($"Name conversion mode not implemented: {conversion}")
			};
		}

		private static string None(string name) => name;

		private static string ToSingular(string name)
		{
			var (prefix, mainWord, suffix) = NormalizeName(name);
			return prefix + mainWord.Singularize(inputIsKnownToBePlural: false) + suffix;
		}

		private static string ToPlural(string name)
		{
			var (prefix, mainWord, suffix) = NormalizeName(name);
			return prefix + mainWord.Pluralize(inputIsKnownToBeSingular: false) + suffix;
		}

		private static string ToPluralLongerThanOne(string name)
		{
			var (prefix, mainWord, suffix) = NormalizeName(name);
			return prefix + (mainWord.Length > 1 ? mainWord.Pluralize(inputIsKnownToBeSingular: false) : mainWord) + suffix;
		}
	}
}
