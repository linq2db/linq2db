namespace LinqToDB.DataProvider.Firebird
{
	/// <summary>
	/// Firebird SQL dialect versions.
	/// More details: <a href="https://www.firebirdsql.org/pdfmanual/html/isql-dialects.html">https://www.firebirdsql.org/pdfmanual/html/isql-dialects.html</a>
	/// </summary>
	internal enum FirebirdDialect : byte
	{
		/// <summary>
		/// Dialect 1.
		/// </summary>
		Dialect1 = 1,

		/// <summary>
		/// Dialect 2.
		/// </summary>
		Dialect2 = 2,

		/// <summary>
		/// Dialect 3. This is default dialect, used by Firebird.
		/// </summary>
		Dialect3 = 3
	}
}
