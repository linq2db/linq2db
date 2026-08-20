using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Internal.DataProvider
{
	/// <summary>
	/// Provider-specific rules for the names linq2db generates: how long they may be, which characters
	/// they may contain, and when they have to be delimited.
	/// </summary>
	public interface IIdentifierService
	{
		/// <summary>
		/// Whether <paramref name="identifier"/> is within the length the server allows for the given
		/// kind of name.
		/// </summary>
		/// <param name="identifierKind">Kind of name being measured; a provider may cap different kinds
		/// differently.</param>
		/// <param name="identifier">Name to measure.</param>
		/// <param name="sizeDecrement">When the name does not fit, the number of trailing *characters*
		/// that have to be dropped for the remainder to fit - not the number of units it overflows by.
		/// The two differ whenever the server counts bytes and the name is not pure ASCII, since one
		/// character can weigh up to four UTF-8 bytes. <see langword="null"/> when the name fits.</param>
		/// <returns><see langword="true"/> when the name fits as-is.</returns>
		bool IsFit(IdentifierKind identifierKind, string identifier, [NotNullWhen(false)] out int? sizeDecrement);

		/// <summary>
		/// Drops the characters that cannot appear in an identifier and any leading underscores,
		/// turning a .NET member or range-variable name into something usable as a SQL alias. The
		/// result can be empty, which callers treat as "no usable name" and replace with a generated
		/// one such as <c>t1</c>.
		/// </summary>
		/// <param name="alias">Name to sanitize.</param>
		/// <returns>The sanitized name, which may be empty.</returns>
		string CorrectAlias(string  alias);

		// Quoting deliberately lives on ISqlBuilder rather than here: the decision needs the provider's
		// reserved words, its quote characters and its per-connection quoting options, all of which the
		// builder already owns. EF Core draws the same line - identifier length is model metadata,
		// while ISqlGenerationHelper.DelimitIdentifier carries the quoting rules and its own reserved
		// word list.
	}
}
