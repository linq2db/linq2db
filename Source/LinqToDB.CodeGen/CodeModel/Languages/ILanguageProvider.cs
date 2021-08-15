using System.Collections.Generic;
using System.Globalization;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Provides access to language-specific functionality.
	/// </summary>
	public interface ILanguageProvider
	{
		/// <summary>
		/// Gets identifier comparer for current language.
		/// E.g. C# and F# use case-sensitive identifiers, but VB.NET works with case-insensitive identifiers.
		/// </summary>
		IEqualityComparer<CodeIdentifier>              IdentifierComparer        { get; }

		/// <summary>
		/// Gets composite identifier comparer (e.g. namespace of full type name).
		/// </summary>
		IEqualityComparer<IEnumerable<CodeIdentifier>> FullNameComparer          { get; }

		/// <summary>
		/// Gets identifier comparer for current language.
		/// E.g. C# and F# use case-sensitive identifiers, but VB.NET works with case-insensitive identifiers.
		/// </summary>
		IEqualityComparer<string>                      RawIdentifierComparer     { get; }

		/// <summary>
		/// Gets <see cref="IType"/> comparer to compare types without taking into account NRT annotations on types.
		/// </summary>
		IEqualityComparer<IType>                       TypeComparerWithoutNRT    { get; }

		/// <summary>
		/// Type and namespace parser service.
		/// </summary>
		ITypeParser                                    TypeParser                { get; }

		/// <summary>
		/// Indicate that language supports nullable reference types annotations.
		/// </summary>
		bool                                           NRTSupported              { get; }

		/// <summary>
		/// Warning code for missing XML-doc comments warning (if supported by language).
		/// </summary>
		string?                                        MissingXmlCommentWarnCode { get; }

		/// <summary>
		/// Default file extension (without leading dot) for source files for current language.
		/// </summary>
		string                                         FileExtension             { get; }

		/// <summary>
		/// Verify that provided character could start indentifier name for current language.
		/// </summary>
		/// <param name="character">Character to validate.</param>
		/// <param name="category">Character unicode category.</param>
		/// <returns><c>true</c> if character is valid at starting position in identifier.</returns>
		bool IsValidIdentifierFirstChar   (string character, UnicodeCategory category);
		/// <summary>
		/// Verify that provided character could be used in indentifier name at non-first position for current language.
		/// </summary>
		/// <param name="character">Character to validate.</param>
		/// <param name="category">Character unicode category.</param>
		/// <returns><c>true</c> if character is valid at non-starting position in identifier.</returns>
		bool IsValidIdentifierNonFirstChar(string character, UnicodeCategory category);

		/// <summary>
		/// Returns language-specific type alias (if any) for type with provided namespace and name.
		/// </summary>
		/// <param name="namespace">Type namespace.</param>
		/// <param name="typeName">Type name.</param>
		/// <returns>Type alias if provided type has it.</returns>
		string? GetAlias(CodeIdentifier[]? @namespace, CodeIdentifier typeName);

		/// <summary>
		/// Returns visitor that could be used to fix identifiers in code model.
		/// </summary>
		/// <returns>Identifers normalization visitor instance.</returns>
		CodeModelVisitor GetIdentifiersNormalizer();

		/// <summary>
		/// Returns visitor that could generate code using target language.
		/// </summary>
		/// <param name="newLine">Newline character sequence.</param>
		/// <param name="indent">Single indent unit character sequence.</param>
		/// <param name="useNRT">Indicates wether NRT annotations should be emmited by code generator.</param>
		/// <param name="knownTypes">Information about namespaces, that include types with specific name.</param>
		/// <param name="sopedNames">Information about names (type name, namespace or type member) in specific naming scopes.</param>
		/// <returns>Visitor instance.</returns>
		CodeGenerationVisitor GetCodeGenerator(
			string                                                                 newLine,
			string                                                                 indent,
			bool                                                                   useNRT,
			IReadOnlyDictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>> knownTypes,
			IReadOnlyDictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> sopedNames);
	}
}
