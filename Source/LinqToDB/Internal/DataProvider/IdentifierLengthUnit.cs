namespace LinqToDB.Internal.DataProvider
{
	/// <summary>
	/// Unit a database counts its identifier length limit in.
	/// </summary>
	public enum IdentifierLengthUnit
	{
		/// <summary>
		/// The limit counts characters, as .NET stores them.
		/// </summary>
		Characters,

		/// <summary>
		/// The limit counts bytes of the UTF-8 encoded identifier, so a non-ASCII character
		/// consumes two to four of them. Used by PostgreSQL, Oracle, Firebird 3 and below,
		/// Db2, Informix and Sybase ASE.
		/// </summary>
		Utf8Bytes,
	}
}
