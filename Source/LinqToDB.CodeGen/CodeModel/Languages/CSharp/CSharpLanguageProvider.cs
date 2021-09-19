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

		private static readonly IReadOnlyDictionary<string, string> _aliasedTypes = new Dictionary<string, string>()
		{
			{ nameof(Byte)     , "byte"    },
			{ nameof(SByte)    , "sbyte"   },
			{ nameof(Int16)    , "short"   },
			{ nameof(UInt16)   , "ushort"  },
			{ nameof(Int32)    , "int"     },
			{ nameof(UInt32)   , "uint"    },
			{ nameof(Int64)    , "long"    },
			{ nameof(UInt64)   , "ulong"   },
			{ nameof(Decimal)  , "decimal" },
			{ nameof(Single)   , "float"   },
			{ nameof(Double)   , "double"  },
			{ nameof(Object)   , "object"  },
			{ nameof(Boolean)  , "bool"    },
			{ nameof(Char)     , "char"    },
			{ nameof(String)   , "string"  },
			// https://github.com/dotnet/roslyn/issues/54233
#pragma warning disable IDE0082 // 'typeof' can be converted  to 'nameof'
			{ typeof(void).Name, "void"    },
#pragma warning restore IDE0082 // 'typeof' can be converted  to 'nameof'
			{ nameof(IntPtr)   , "nint"    },
			{ nameof(UIntPtr)  , "nuint"   },
		};

		private readonly IEqualityComparer<CodeIdentifier>              _identifierEqualityComparer;
		private readonly IComparer<CodeIdentifier>                      _identifierComparer;
		private readonly IEqualityComparer<IEnumerable<CodeIdentifier>> _namespaceEqualityComparer;
		private readonly IComparer<IEnumerable<CodeIdentifier>>         _namespaceComparer;
		private readonly IEqualityComparer<IType>                       _typeEqualityComparerWithoutNRT;
		private readonly IEqualityComparer<IType>                       _typeEqualityComparerWithNRT;
		private readonly ITypeParser                                    _typeEqualityParser;

		internal CSharpLanguageProvider()
		{
			_identifierEqualityComparer     = new CodeIdentifierEqualityComparer(StringComparer.Ordinal);
			_identifierComparer             = new CodeIdentifierComparer(StringComparer.Ordinal);
			_namespaceEqualityComparer      = new NamespaceEqualityComparer(_identifierEqualityComparer);
			_namespaceComparer              = new NamespaceComparer(_identifierComparer);
			_typeEqualityComparerWithoutNRT = new TypeEqualityComparer(_identifierEqualityComparer, _namespaceEqualityComparer, true);
			_typeEqualityComparerWithNRT    = new TypeEqualityComparer(_identifierEqualityComparer, _namespaceEqualityComparer, false);
			_typeEqualityParser             = new TypeParser(this);
		}

		IEqualityComparer<CodeIdentifier>              ILanguageProvider.IdentifierEqualityComparer        => _identifierEqualityComparer;
		IEqualityComparer<IEnumerable<CodeIdentifier>> ILanguageProvider.FullNameEqualityComparer          => _namespaceEqualityComparer;
		IComparer<IEnumerable<CodeIdentifier>>         ILanguageProvider.FullNameComparer                  => _namespaceComparer;
		IEqualityComparer<string>                      ILanguageProvider.RawIdentifierEqualityComparer     => StringComparer.Ordinal;
		IEqualityComparer<IType>                       ILanguageProvider.TypeEqualityComparerWithNRT       => _typeEqualityComparerWithNRT;
		IEqualityComparer<IType>                       ILanguageProvider.TypeEqualityComparerWithoutNRT    => _typeEqualityComparerWithoutNRT;
		ITypeParser                                    ILanguageProvider.TypeParser                        => _typeEqualityParser;
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

		string? ILanguageProvider.GetAlias(CodeIdentifier[]? @namespace, CodeIdentifier typeName)
		{
			if (@namespace?.Length == 1
				&& @namespace[0].Name == "System"
				&& _aliasedTypes.TryGetValue(typeName.Name, out var alias))
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
