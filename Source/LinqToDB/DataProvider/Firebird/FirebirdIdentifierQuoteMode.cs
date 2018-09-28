using System;

namespace LinqToDB.DataProvider.Firebird
{
	/// <summary>
	/// Possible modes for Firebird identifier quotes. This enumeration covers only identifier quotation logic
	/// and don't handle identifier length limits.
	/// </summary>
	public enum FirebirdIdentifierQuoteMode
	{
		/// <summary>
		/// Do not quote identifiers.
		/// LINQ To DB will not check identifiers for validity (spaces, reserved words) is this mode.
		/// This mode should be used only for SQL Dialect &lt; 3 and it is developer's responsibility to
		/// ensure that there is no identifiers in use that require quotation.
		/// </summary>
		None,
		/// <summary>
		/// Always quote identifiers.
		/// LINQ To DB will quote all identifiers, even if it is not required.
		/// Select this mode, if you need to preserve identifiers casing.
		/// Quoted identifiers not supported by SQL Dialect &lt; 3.
		/// </summary>
		Quote,
		/// <summary>
		/// Quote identifiers if needed.
		/// LINQ To DB will quote identifiers, if they are not valid without quotation.
		/// This includes:
		/// - use of reserved words;
		/// - use of any characters except latin letters, digits, _ and $;
		/// - use digit, _ or $ as first character.
		/// This is default mode.
		/// Note that if you need to preserve casing of identifiers, you should use <see cref="Quote"/> mode.
		/// Quoted identifiers not supported by SQL Dialect &lt; 3.
		/// </summary>
		Auto
	}
}
