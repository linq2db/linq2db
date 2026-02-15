using System.Collections.Generic;
using System.Globalization;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Provides access to language-specific functionality.
	/// </summary>
	public interface ILanguageProvider
	{
		/// <summary>
		/// Gets identifier equality comparer for current language.
		/// E.g. C# and F# use case-sensitive identifiers, but VB.NET works with case-insensitive identifiers.
		/// </summary>
		IEqualityComparer<CodeIdentifier>              IdentifierEqualityComparer     { get; }

		/// <summary>
		/// Gets composite identifier equality comparer (e.g. namespace of full type name).
		/// </summary>
		IEqualityComparer<IEnumerable<CodeIdentifier>> FullNameEqualityComparer       { get; }

		/// <summary>
		/// Gets composite identifier comparer (e.g. namespace of full type name).
		/// </summary>
		IComparer<IEnumerable<CodeIdentifier>>         FullNameComparer               { get; }

		/// <summary>
		/// Gets identifier equality comparer for current language.
		/// E.g. C# and F# use case-sensitive identifiers, but VB.NET works with case-insensitive identifiers.
		/// </summary>
		IEqualityComparer<string>                      RawIdentifierEqualityComparer  { get; }

		/// <summary>
		/// Gets <see cref="IType"/> equality comparer to compare types with taking into account NRT annotations on types.
		/// </summary>
		IEqualityComparer<IType>                       TypeEqualityComparerWithNRT    { get; }

		/// <summary>
		/// Gets <see cref="IType"/> equality comparer to compare types without taking into account NRT annotations on types.
		/// </summary>
		IEqualityComparer<IType>                       TypeEqualityComparerWithoutNRT { get; }

		/// <summary>
		/// Type and namespace parser service.
		/// </summary>
		ITypeParser                                    TypeParser                     { get; }

		/// <summary>
		/// Indicate that language supports nullable reference types annotations.
		/// </summary>
		bool                                           NRTSupported                   { get; }

		/// <summary>
		/// Warning codes for missing XML-doc comments warnings (if supported by language).
		/// </summary>
		string[]                                       MissingXmlCommentWarnCodes     { get; }

		/// <summary>
		/// Default file extension (without leading dot) for source files for current language.
		/// </summary>
		string                                         FileExtension                  { get; }

		CodeBuilder                                    ASTBuilder                     { get; }

		/// <summary>
		/// Verify that provided character could start indentifier name for current language.
		/// </summary>
		/// <param name="character">Character to validate.</param>
		/// <param name="category">Character unicode category.</param>
		/// <returns><see langword="true" /> if character is valid at starting position in identifier.</returns>
		bool IsValidIdentifierFirstChar   (string character, UnicodeCategory category);
		/// <summary>
		/// Verify that provided character could be used in indentifier name at non-first position for current language.
		/// </summary>
		/// <param name="character">Character to validate.</param>
		/// <param name="category">Character unicode category.</param>
		/// <returns><see langword="true" /> if character is valid at non-starting position in identifier.</returns>
		bool IsValidIdentifierNonFirstChar(string character, UnicodeCategory category);

		/// <summary>
		/// Returns language-specific type alias (if any) for provided type.
		/// For nullable type returns alias without nullability annotations.
		/// </summary>
		/// <param name="type">Type to check for alias. Could be nullable type.</param>
		/// <returns>Type alias if provided type has it.</returns>
		string? GetAlias(IType type);

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
		/// <param name="scopedNames">Information about names (type name, namespace or type member) in specific naming scopes.</param>
		/// <param name="scopedTypes">Information about type names (type name and namespace) in specific naming scopes.</param>
		/// <returns>Visitor instance.</returns>
		CodeGenerationVisitor GetCodeGenerator(
			string                                                                 newLine,
			string                                                                 indent,
			bool                                                                   useNRT,
			IReadOnlyDictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>> knownTypes,
			IReadOnlyDictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> scopedNames,
			IReadOnlyDictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> scopedTypes);
	}
}
