namespace LinqToDB.DataProvider.SQLite
{
	/// <summary>
	/// SQLite ADO.NET provider.
	/// </summary>
	public enum SQLiteProvider
	{
		/// <summary>
		/// Automatically detect provider.
		/// </summary>
		AutoDetect,
		/// <summary>
		/// System.Data.SQLite provider.
		/// </summary>
		System,
		/// <summary>
		/// Microsoft.Data.Sqlite provider.
		/// </summary>
		Microsoft,
	}
}
