using System;
using System.Collections.Generic;
using System.Globalization;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// C# language services provider implementation.
	/// </summary>
	internal class CSharpLanguageProvider : ILanguageProvider
	{
		// 1573: Parameter ? has no matching param tag in the XML comment https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs1573
		// 1591: Missing XML comment for publicly visible type or member https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs1591
		private static readonly string[] _xmlDocWarnings = new [] {"1573", "1591" };

		private readonly IReadOnlyDictionary<IType, string>             _aliasedTypes;
		private readonly IEqualityComparer<CodeIdentifier>              _identifierEqualityComparer;
		private readonly IComparer<CodeIdentifier>                      _identifierComparer;
		private readonly IEqualityComparer<IEnumerable<CodeIdentifier>> _namespaceEqualityComparer;
		private readonly IComparer<IEnumerable<CodeIdentifier>>         _namespaceComparer;
		private readonly IEqualityComparer<IType>                       _typeEqualityComparerWithoutNRT;
		private readonly IEqualityComparer<IType>                       _typeEqualityComparerWithoutNullability;
		private readonly IEqualityComparer<IType>                       _typeEqualityComparerWithNRT;
		private readonly ITypeParser                                    _typeParser;

		private CSharpLanguageProvider()
		{
			_identifierEqualityComparer             = new CodeIdentifierEqualityComparer(StringComparer.Ordinal);
			_identifierComparer                     = new CodeIdentifierComparer(StringComparer.Ordinal);
			_namespaceEqualityComparer              = new NamespaceEqualityComparer(_identifierEqualityComparer);
			_namespaceComparer                      = new NamespaceComparer(_identifierComparer);
			_typeEqualityComparerWithoutNRT         = new TypeEqualityComparer(_identifierEqualityComparer, _namespaceEqualityComparer, true , false);
			_typeEqualityComparerWithoutNullability = new TypeEqualityComparer(_identifierEqualityComparer, _namespaceEqualityComparer, true , true );
			_typeEqualityComparerWithNRT            = new TypeEqualityComparer(_identifierEqualityComparer, _namespaceEqualityComparer, false, false);
			_typeParser                             = new TypeParser(this);

			var aliasedTypes = new Dictionary<IType, string>(_typeEqualityComparerWithoutNullability);
			_aliasedTypes    = aliasedTypes;

			// don't use WellKnownTypes here to avoid circular dependency
			aliasedTypes.Add(_typeParser.Parse<byte   >()   , "byte"   );
			aliasedTypes.Add(_typeParser.Parse<sbyte  >()   , "sbyte"  );
			aliasedTypes.Add(_typeParser.Parse<short  >()   , "short"  );
			aliasedTypes.Add(_typeParser.Parse<ushort >()   , "ushort" );
			aliasedTypes.Add(_typeParser.Parse<int    >()   , "int"    );
			aliasedTypes.Add(_typeParser.Parse<uint   >()   , "uint"   );
			aliasedTypes.Add(_typeParser.Parse<long   >()   , "long"   );
			aliasedTypes.Add(_typeParser.Parse<ulong  >()   , "ulong"  );
			aliasedTypes.Add(_typeParser.Parse<decimal>()   , "decimal");
			aliasedTypes.Add(_typeParser.Parse<float  >()   , "float"  );
			aliasedTypes.Add(_typeParser.Parse<double >()   , "double" );
			aliasedTypes.Add(_typeParser.Parse<object >()   , "object" );
			aliasedTypes.Add(_typeParser.Parse<bool   >()   , "bool"   );
			aliasedTypes.Add(_typeParser.Parse<char   >()   , "char"   );
			aliasedTypes.Add(_typeParser.Parse<string >()   , "string" );
			aliasedTypes.Add(_typeParser.Parse<nint   >()   , "nint"   );
			aliasedTypes.Add(_typeParser.Parse<nuint  >()   , "nuint"  );
			aliasedTypes.Add(_typeParser.Parse(typeof(void)), "void"   );
		}

		public static ILanguageProvider Instance { get; } = new CSharpLanguageProvider();

		IEqualityComparer<CodeIdentifier>              ILanguageProvider.IdentifierEqualityComparer        => _identifierEqualityComparer;
		IEqualityComparer<IEnumerable<CodeIdentifier>> ILanguageProvider.FullNameEqualityComparer          => _namespaceEqualityComparer;
		IComparer<IEnumerable<CodeIdentifier>>         ILanguageProvider.FullNameComparer                  => _namespaceComparer;
		IEqualityComparer<string>                      ILanguageProvider.RawIdentifierEqualityComparer     => StringComparer.Ordinal;
		IEqualityComparer<IType>                       ILanguageProvider.TypeEqualityComparerWithNRT       => _typeEqualityComparerWithNRT;
		IEqualityComparer<IType>                       ILanguageProvider.TypeEqualityComparerWithoutNRT    => _typeEqualityComparerWithoutNRT;
		ITypeParser                                    ILanguageProvider.TypeParser                        => _typeParser;
		bool                                           ILanguageProvider.NRTSupported                      => true;
		string[]                                       ILanguageProvider.MissingXmlCommentWarnCodes        => _xmlDocWarnings;
		string                                         ILanguageProvider.FileExtension                     => "cs";

		bool ILanguageProvider.IsValidIdentifierFirstChar(string character, UnicodeCategory category)
		{
			switch (category)
			{
				case UnicodeCategory.UppercaseLetter:
				case UnicodeCategory.LowercaseLetter:
				case UnicodeCategory.TitlecaseLetter:
				case UnicodeCategory.ModifierLetter :
				case UnicodeCategory.OtherLetter    :
				case UnicodeCategory.LetterNumber   :
					return true;
			}

			return false;
		}

		bool ILanguageProvider.IsValidIdentifierNonFirstChar(string character, UnicodeCategory category)
		{
			switch (category)
			{
				case UnicodeCategory.UppercaseLetter     :
				case UnicodeCategory.LowercaseLetter     :
				case UnicodeCategory.TitlecaseLetter     :
				case UnicodeCategory.ModifierLetter      :
				case UnicodeCategory.OtherLetter         :
				case UnicodeCategory.LetterNumber        :

				case UnicodeCategory.DecimalDigitNumber  :
				case UnicodeCategory.ConnectorPunctuation:
				case UnicodeCategory.NonSpacingMark      :
				case UnicodeCategory.SpacingCombiningMark:
				case UnicodeCategory.Format              :
					return true;
			}

			return false;
		}

		string? ILanguageProvider.GetAlias(IType type)
		{
			if (_aliasedTypes.TryGetValue(type, out var alias))
				return alias;

			return null;
		}

		CodeModelVisitor ILanguageProvider.GetIdentifiersNormalizer() => new CSharpNameNormalizationVisitor(this);

		CodeGenerationVisitor ILanguageProvider.GetCodeGenerator(
			string                                                                 newLine,
			string                                                                 indent,
			bool                                                                   useNRT,
			IReadOnlyDictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>> knownTypes,
			IReadOnlyDictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> sopedNames)
		{
			return new CSharpCodeGenerator(
				this,
				newLine,
				indent,
				useNRT,
				knownTypes,
				sopedNames);
		}
	}
}
