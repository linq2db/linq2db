namespace LinqToDB.DataProvider.Access
{
	/// <summary>
	/// Access ADO.NET provider.
	/// </summary>
	public enum AccessProvider
	{
		/// <summary>
		/// Detect provider type automatically.
		/// </summary>
		AutoDetect,
		/// <summary>
		/// Access OLE DB provider.
		/// </summary>
		OleDb,
		/// <summary>
		/// Access ODBC provider.
		/// </summary>
		ODBC,
		/// <summary>
		/// LibRed managed Access provider. Requires .NET 10 or greater.
		/// </summary>
		/// <remarks>
		/// <c>LibRed.Ado</c> is a prerelease package. LibRed keeps no Access lock file (<c>.ldb</c> / <c>.laccdb</c>), so
		/// Access or another Microsoft driver cannot see that a database is in use through LibRed: don't open the same
		/// file from another process at the same time.
		/// </remarks>
		LibRed,
	}
}
