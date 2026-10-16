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
		/// LibRed managed Access provider. Requires .NET 11 or greater.
		/// </summary>
		LibRed,
	}
}
