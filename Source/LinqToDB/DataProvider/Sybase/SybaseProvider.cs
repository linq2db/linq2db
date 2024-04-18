namespace LinqToDB.DataProvider.Sybase
{
	/// <summary>
	/// Sybase ADO.NET provider.
	/// </summary>
	public enum SybaseProvider
	{
		/// <summary>
		/// Detect provider automatically.
		/// </summary>
		AutoDetect,
		/// <summary>
		/// Unmanaged provider from SAP (Sybase.AdoNet45.AseClient.dll).
		/// </summary>
		Unmanaged,
		/// <summary>
		/// DataAction <see href="https://github.com/DataAction/AdoNetCore.AseClient">managed provider</see>.
		/// </summary>
		DataAction
	}
}
