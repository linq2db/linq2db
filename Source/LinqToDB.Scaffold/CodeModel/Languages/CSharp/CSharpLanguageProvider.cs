using System;
using System.Collections.Generic;
using System.Globalization;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// C# language services provider implementation.
	/// </summary>
	internal sealed class CSharpLanguageProvider : ILanguageProvider
	{
		// 1573: Parameter ? has no matching param tag in the XML comment https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs1573
		// 1591: Missing XML comment for publicly visible type or member https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs1591
		private static readonly string[] _xmlDocWarnings = new [] { "1573", "1591" };

		private readonly IReadOnlyDictionary<IType, string> _aliasedTypes;
		private readonly CodeIdentifierComparer             _identifierComparer;
		private readonly IEqualityComparer<IType>           _typeEqualityComparerWithoutNRT;
		private readonly IEqualityComparer<IType>           _typeEqualityComparerWithoutNullability;
		private readonly IEqualityComparer<IType>           _typeEqualityComparerWithNRT;
		private readonly ITypeParser                        _typeParser;
		private readonly CodeBuilder                        _builder;

		private CSharpLanguageProvider()
		{
			_identifierComparer                     = new CodeIdentifierComparer(StringComparer.Ordinal);
			_typeEqualityComparerWithoutNRT         = new TypeEqualityComparer(_identifierComparer, _identifierComparer, true , false);
			_typeEqualityComparerWithoutNullability = new TypeEqualityComparer(_identifierComparer, _identifierComparer, true , true );
			_typeEqualityComparerWithNRT            = new TypeEqualityComparer(_identifierComparer, _identifierComparer, false, false);
			_typeParser                             = new TypeParser(this);
			_builder                                = new CodeBuilder(this);

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

		IEqualityComparer<CodeIdentifier>              ILanguageProvider.IdentifierEqualityComparer        => _identifierComparer;
		IEqualityComparer<IEnumerable<CodeIdentifier>> ILanguageProvider.FullNameEqualityComparer          => _identifierComparer;
		IComparer<IEnumerable<CodeIdentifier>>         ILanguageProvider.FullNameComparer                  => _identifierComparer;
		IEqualityComparer<string>                      ILanguageProvider.RawIdentifierEqualityComparer     => StringComparer.Ordinal;
		IEqualityComparer<IType>                       ILanguageProvider.TypeEqualityComparerWithNRT       => _typeEqualityComparerWithNRT;
		IEqualityComparer<IType>                       ILanguageProvider.TypeEqualityComparerWithoutNRT    => _typeEqualityComparerWithoutNRT;
		ITypeParser                                    ILanguageProvider.TypeParser                        => _typeParser;
		bool                                           ILanguageProvider.NRTSupported                      => true;
		string[]                                       ILanguageProvider.MissingXmlCommentWarnCodes        => _xmlDocWarnings;
		string                                         ILanguageProvider.FileExtension                     => "cs";
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

		CodeModelVisitor ILanguageProvider.GetIdentifiersNormalizer() => new CSharpNameNormalizationVisitor(this);

		CodeGenerationVisitor ILanguageProvider.GetCodeGenerator(
			string                                                                 newLine,
			string                                                                 indent,
			bool                                                                   useNRT,
			IReadOnlyDictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>> knownTypes,
			IReadOnlyDictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> scopedNames,
			IReadOnlyDictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> scopedTypes)
		{
			return new CSharpCodeGenerator(
				this,
				newLine,
				indent,
				useNRT,
				knownTypes,
				scopedNames,
				scopedTypes);
		}
	}
}
