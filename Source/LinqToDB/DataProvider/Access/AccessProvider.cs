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
	}
}
