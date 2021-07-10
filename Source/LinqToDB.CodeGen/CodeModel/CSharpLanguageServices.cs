using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LinqToDB.CodeGen.CodeModel
{
	public class CSharpLanguageServices : ILanguageServices
	{
		private static readonly Dictionary<string, string> _aliases = new ()
		{
			{ nameof(Byte), "byte" },
			{ nameof(SByte), "sbyte" },
			{ nameof(Int16), "short" },
			{ nameof(UInt16), "ushort" },
			{ nameof(Int32), "int" },
			{ nameof(UInt32), "uint" },
			{ nameof(Int64), "long" },
			{ nameof(UInt64), "ulong" },
			{ nameof(Decimal), "decimal" },
			{ nameof(Single), "float" },
			{ nameof(Double), "double" },
			{ nameof(Object), "object" },
			{ nameof(Boolean), "bool" },
			{ nameof(Char), "char" },
			{ nameof(String), "string" },
			// https://github.com/dotnet/roslyn/issues/54233
#pragma warning disable IDE0082 // 'typeof' can be converted  to 'nameof'
			{ typeof(void).Name, "void" },
#pragma warning restore IDE0082 // 'typeof' can be converted  to 'nameof'
			{ nameof(IntPtr), "nint" },
			{ nameof(UIntPtr), "nuint" },
		};

		string? ILanguageServices.GetAlias(CodeIdentifier[]? @namespace, string name)
		{
			if (@namespace != null
				&& @namespace.Length == 1
				&& @namespace[0].Name == "System"
				&& _aliases.TryGetValue(name, out var alias))
				return alias;
			return null;
		}

		IEqualityComparer<string> ILanguageServices.GetNameComparer()
		{
			return EqualityComparer<string>.Default;
		}

		ISet<string> ILanguageServices.GetUniqueNameCollection() => new HashSet<string>();

		bool ILanguageServices.IsValidIdentifierFirstChar(string character, UnicodeCategory category)
		{
			switch (category)
			{
				case UnicodeCategory.UppercaseLetter:
				case UnicodeCategory.LowercaseLetter:
				case UnicodeCategory.TitlecaseLetter:
				case UnicodeCategory.ModifierLetter:
				case UnicodeCategory.OtherLetter:
				case UnicodeCategory.LetterNumber:
					return true;
			}

			return false;
		}

		bool ILanguageServices.IsValidIdentifierNonFirstChar(string character, UnicodeCategory category)
		{
			switch (category)
			{
				case UnicodeCategory.UppercaseLetter:
				case UnicodeCategory.LowercaseLetter:
				case UnicodeCategory.TitlecaseLetter:
				case UnicodeCategory.ModifierLetter:
				case UnicodeCategory.OtherLetter:
				case UnicodeCategory.LetterNumber:

				case UnicodeCategory.DecimalDigitNumber:
				case UnicodeCategory.ConnectorPunctuation:
				case UnicodeCategory.NonSpacingMark:
				case UnicodeCategory.SpacingCombiningMark:
				case UnicodeCategory.Format:
					return true;
			}

			return false;
		}

		string ILanguageServices.MakeUnique(ISet<string> knownNames, string name)
		{
			var newName = name;
			var cnt = 1;

			while (knownNames.Contains(newName))
			{
				newName = name + cnt.ToString(NumberFormatInfo.InvariantInfo);
				cnt++;
			}

			return newName;
		}

		string ILanguageServices.NormalizeIdentifier(string name)
		{
			// character filtration based on spec
			// with one ommission - we don't allow leading @ and add it later based on keyword lookup
			// (https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#identifiers)
			var newName = new StringBuilder();
			foreach (var (chr, cat) in name.EnumerateCharacters())
			{
				switch (cat)
				{
					// valid as first/non-first character
					case UnicodeCategory.UppercaseLetter:
					case UnicodeCategory.LowercaseLetter:
					case UnicodeCategory.TitlecaseLetter:
					case UnicodeCategory.ModifierLetter:
					case UnicodeCategory.OtherLetter:
					case UnicodeCategory.LetterNumber:
						newName.Append(chr);
						break;
					// valid as non-first identifier character
					case UnicodeCategory.DecimalDigitNumber:
					case UnicodeCategory.ConnectorPunctuation:
					case UnicodeCategory.NonSpacingMark:
					case UnicodeCategory.SpacingCombiningMark:
					case UnicodeCategory.Format:
						// if first character is not valid on first position, prefix it with "_"
						if (newName.Length == 0)
							newName.Append('_');
						newName.Append(chr);
						break;
					default:
						// if character invalid - remove it
						break;
				}
			}

			// if identifier starts with two (or more) underscores: remove all but one (__ is MS implementation-specific reserved prefix)
			while (newName.Length > 1 && newName[0] == '_' && newName[1] == '_')
				newName.Remove(0, 1);

			// return empty identifier, it will be normalized later by CSharpNameNormalizationVisitor
			//// if final identifier is empty - replace it with "_"
			//if (newName.Length == 0)
			//	newName.Append('_');

			return newName.ToString();
		}
	}
}
