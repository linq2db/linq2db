namespace LinqToDB.Schema
{
	/// <summary>
	/// SQL Server database-specific scaffold options.
	/// </summary>
	public sealed class SqlServerDatabaseOptions : DatabaseOptions
	{
		public static readonly SqlServerDatabaseOptions Instance = new();

		private SqlServerDatabaseOptions() { }

		/// <summary>
		/// Indicates that SQL Server requires schema name on scalar function invocation.
		/// </summary>
		public override bool ScalarFunctionSchemaRequired => true;
	}
}
