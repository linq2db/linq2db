using System;
using System.Collections.Generic;
using System.Globalization;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// F# language services provider implementation.
	/// </summary>
	internal sealed class FSharpLanguageProvider : ILanguageProvider
	{
		// F# does not emit "missing XML documentation" warnings by default, so there is nothing to suppress
		// for auto-generated code.
		private static readonly string[] _xmlDocWarnings = [];

		private readonly IReadOnlyDictionary<IType, string> _aliasedTypes;
		private readonly CodeIdentifierComparer             _identifierComparer;
		private readonly IEqualityComparer<IType>           _typeEqualityComparerWithoutNRT;
		private readonly IEqualityComparer<IType>           _typeEqualityComparerWithoutNullability;
		private readonly IEqualityComparer<IType>           _typeEqualityComparerWithNRT;
		private readonly ITypeParser                        _typeParser;
		private readonly CodeBuilder                        _builder;

		private FSharpLanguageProvider()
		{
			// F# identifiers are case-sensitive, same as C#
			_identifierComparer                     = new CodeIdentifierComparer(StringComparer.Ordinal);
			_typeEqualityComparerWithoutNRT         = new TypeEqualityComparer(_identifierComparer, _identifierComparer, true , false);
			_typeEqualityComparerWithoutNullability = new TypeEqualityComparer(_identifierComparer, _identifierComparer, true , true );
			_typeEqualityComparerWithNRT            = new TypeEqualityComparer(_identifierComparer, _identifierComparer, false, false);
			_typeParser                             = new TypeParser(this);
			_builder                                = new CodeBuilder(this);

			var aliasedTypes = new Dictionary<IType, string>(_typeEqualityComparerWithoutNullability);
			_aliasedTypes    = aliasedTypes;

			// F# primitive type aliases.
			// Note the classic F# gotcha: 'float' is System.Double and 'float32' is System.Single.
			// don't use WellKnownTypes here to avoid circular dependency
			aliasedTypes.Add(_typeParser.Parse<byte   >()   , "byte"      );
			aliasedTypes.Add(_typeParser.Parse<sbyte  >()   , "sbyte"     );
			aliasedTypes.Add(_typeParser.Parse<short  >()   , "int16"     );
			aliasedTypes.Add(_typeParser.Parse<ushort >()   , "uint16"    );
			aliasedTypes.Add(_typeParser.Parse<int    >()   , "int"       );
			aliasedTypes.Add(_typeParser.Parse<uint   >()   , "uint"      );
			aliasedTypes.Add(_typeParser.Parse<long   >()   , "int64"     );
			aliasedTypes.Add(_typeParser.Parse<ulong  >()   , "uint64"    );
			aliasedTypes.Add(_typeParser.Parse<decimal>()   , "decimal"   );
			aliasedTypes.Add(_typeParser.Parse<float  >()   , "float32"   );
			aliasedTypes.Add(_typeParser.Parse<double >()   , "float"     );
			aliasedTypes.Add(_typeParser.Parse<object >()   , "obj"       );
			aliasedTypes.Add(_typeParser.Parse<bool   >()   , "bool"      );
			aliasedTypes.Add(_typeParser.Parse<char   >()   , "char"      );
			aliasedTypes.Add(_typeParser.Parse<string >()   , "string"    );
			aliasedTypes.Add(_typeParser.Parse<nint   >()   , "nativeint" );
			aliasedTypes.Add(_typeParser.Parse<nuint  >()   , "unativeint");
			aliasedTypes.Add(_typeParser.Parse(typeof(void)), "unit"      );
		}

		public static ILanguageProvider Instance { get; } = new FSharpLanguageProvider();

		IEqualityComparer<CodeIdentifier>              ILanguageProvider.IdentifierEqualityComparer        => _identifierComparer;
		IEqualityComparer<IEnumerable<CodeIdentifier>> ILanguageProvider.FullNameEqualityComparer          => _identifierComparer;
		IComparer<IEnumerable<CodeIdentifier>>         ILanguageProvider.FullNameComparer                  => _identifierComparer;
		IEqualityComparer<string>                      ILanguageProvider.RawIdentifierEqualityComparer     => StringComparer.Ordinal;
		IEqualityComparer<IType>                       ILanguageProvider.TypeEqualityComparerWithNRT       => _typeEqualityComparerWithNRT;
		IEqualityComparer<IType>                       ILanguageProvider.TypeEqualityComparerWithoutNRT    => _typeEqualityComparerWithoutNRT;
		ITypeParser                                    ILanguageProvider.TypeParser                        => _typeParser;
		// F# 9 nullness: the generator emits `| null` on nullable reference types when --nrt is enabled.
		bool                                           ILanguageProvider.NRTSupported                      => true;
		string[]                                       ILanguageProvider.MissingXmlCommentWarnCodes        => _xmlDocWarnings;
		string                                         ILanguageProvider.FileExtension                     => "fs";
		CodeBuilder                                    ILanguageProvider.ASTBuilder                        => _builder;

		bool ILanguageProvider.IsValidIdentifierFirstChar(string character, UnicodeCategory category)
		{
			return category
				is UnicodeCategory.UppercaseLetter
				or UnicodeCategory.LowercaseLetter
				or UnicodeCategory.TitlecaseLetter
				or UnicodeCategory.ModifierLetter
				or UnicodeCategory.OtherLetter
				or UnicodeCategory.LetterNumber;
		}

		bool ILanguageProvider.IsValidIdentifierNonFirstChar(string character, UnicodeCategory category)
		{
			return category
				is UnicodeCategory.UppercaseLetter
				or UnicodeCategory.LowercaseLetter
				or UnicodeCategory.TitlecaseLetter
				or UnicodeCategory.ModifierLetter
				or UnicodeCategory.OtherLetter
				or UnicodeCategory.LetterNumber

				or UnicodeCategory.DecimalDigitNumber
				or UnicodeCategory.ConnectorPunctuation
				or UnicodeCategory.NonSpacingMark
				or UnicodeCategory.SpacingCombiningMark
				or UnicodeCategory.Format;
		}

		string? ILanguageProvider.GetAlias(IType type)
		{
			if (_aliasedTypes.TryGetValue(type, out var alias))
				return alias;

			return null;
		}

		CodeModelVisitor ILanguageProvider.GetIdentifiersNormalizer() => new FSharpNameNormalizationVisitor(this);

		CodeGenerationVisitor ILanguageProvider.GetCodeGenerator(
			string                                                                 newLine,
			string                                                                 indent,
			bool                                                                   useNRT,
			IReadOnlyDictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>> knownTypes,
			IReadOnlyDictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> scopedNames,
			IReadOnlyDictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> scopedTypes)
		{
			return new FSharpCodeGenerator(
				this,
				newLine,
				indent,
				// when enabled, nullable reference types are emitted with an F# 9 `| null` annotation
				useNRT,
				knownTypes,
				scopedNames,
				scopedTypes);
		}
	}
}
