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
		/// The limit counts bytes rather than characters, so a non-ASCII name reaches it sooner than its
		/// character count suggests. Used by PostgreSQL, Oracle, Firebird 3 and below, Db2, Informix and
		/// Sybase ASE.
		/// <para>
		/// The count is taken as UTF-8, which is a deliberate conservative approximation rather than the
		/// exact rule for every one of those servers: Db2 counts bytes after conversion to the database
		/// code page and Informix follows the active locale, neither of which is necessarily UTF-8. Where
		/// the real encoding is narrower this truncates earlier than the server requires - shorter names,
		/// never invalid ones - and the service has no access to the connection's encoding to do better.
		/// </para>
		/// </summary>
		Utf8Bytes,
	}
}
