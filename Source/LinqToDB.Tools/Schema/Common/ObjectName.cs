namespace LinqToDB.Schema
{
	/// <summary>
	/// Contains fully-qualified name of database object (e.g. table or function).
	/// </summary>
	/// <param name="Server">Linked server name. Optional.</param>
	/// <param name="Database">Database name. Optional.</param>
	/// <param name="Schema">Schema name. Optional.</param>
	/// <param name="Name">Object name.</param>
	public sealed record ObjectName(string? Server, string? Database, string? Schema, string Name)
	{
		public override string ToString() => $"{Server}{(Server != null ? "." : null)}{Database}{(Database != null ? "." : null)}{Schema}{(Schema != null ? "." : null)}{Name}";
	}
}
