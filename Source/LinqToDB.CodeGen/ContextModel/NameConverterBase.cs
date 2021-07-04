using System;

namespace LinqToDB.CodeGen.ContextModel
{
	public abstract class NameConverterBase : INameConverterProvider
	{
		public abstract Func<string, string> GetConverter(Pluralization conversion);

		protected static (string? prefix, string mainWord, string? suffix) NormalizeName(string name)
		{
			var prefixIndex = name.Length;
			while (!char.IsLetter(name[prefixIndex - 1]))
			{
				prefixIndex--;
			}

			var word = GetLastWord(name.Substring(0, prefixIndex));

			var suffixLength = prefixIndex - word.Length;
			return (
				suffixLength > 0 ? name.Substring(0, suffixLength) : null,
				word,
				prefixIndex < name.Length ? name.Substring(prefixIndex) : null);
		}

		static string GetLastWord(string name)
		{
			var i = name.Length - 1;
			var isLower = char.IsLower(name[i]);

			while (i > 0 && char.IsLetter(name[i - 1]) && char.IsLower(name[i - 1]) == isLower)
				i--;

			return name.Substring(isLower && i > 0 && char.IsLetter(name[i - 1]) ? i - 1 : i);
		}
	}
}
