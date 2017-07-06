using System;

namespace LinqToDB.DataProvider.Firebird
{
    /// <summary>
    /// Possible modes for Firebird identifier quotes
    /// </summary>
	public enum FirebirdIdentifierQuoteMode
	{
        /// <summary>
        /// Do not quote identifiers
        /// </summary>
		None,
        /// <summary>
        /// Always quote identifiers
        /// </summary>
		Quote,
        /// <summary>
        /// quote identifiers if needed.
        /// </summary>
		Auto
	}
}
