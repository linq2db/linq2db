namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Represents full name of database object (e.g. table, view, function or procedure) split into components.
	/// </summary>
	/// <param name="Name">Name of object in current scope (e.g. in schema or package).</param>
	/// <param name="Server">Database server or linked server name.</param>
	/// <param name="Database">Database/catalog name.</param>
	/// <param name="Schema">Schema/user name.</param>
	/// <param name="Package">Package/module/library name (used with functions and stored procedures).</param>
	public readonly record struct SqlObjectName(string Name, string? Server = null, string? Database = null, string? Schema = null, string? Package = null)
	{
		public override string ToString() => $"{Server}{(Server != null ? "." : null)}{Database}{(Database != null ? "." : null)}{Schema}{(Schema != null ? "." : null)}{Package}{(Package != null ? "." : null)}{Name}";
	}
}
