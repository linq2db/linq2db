namespace LinqToDB.DataProvider.Firebird
{
	/// <summary>
	/// Firebird language dialect. Version specifies minimal Firebird version to use this dialect.
	/// </summary>
	public enum FirebirdVersion
	{
		/// <summary>
		/// Use automatic detection of dialect by asking Firebird server for version.
		/// </summary>
		AutoDetect,
		/// <summary>
		/// Firebird 2.5+ SQL dialect.
		/// </summary>
		v25,
		/// <summary>
		/// Firebird 3+ SQL dialect.
		/// </summary>
		v3,
		/// <summary>
		/// Firebird 4+ SQL dialect.
		/// </summary>
		v4,
		/// <summary>
		/// Firebird 5+ SQL dialect.
		/// </summary>
		v5,
	}
}
