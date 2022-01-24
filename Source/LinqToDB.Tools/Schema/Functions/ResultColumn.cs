namespace LinqToDB.Schema
{
	/// <summary>
	/// Column descriptor for table function or procedure result set.
	/// </summary>
	/// <param name="Name">Column name.</param>
	/// <param name="Type">Column type.</param>
	/// <param name="Nullable">Column allows <c>NULL</c> values.</param>
	public sealed record ResultColumn(string? Name, DatabaseType Type, bool Nullable)
	{
		public override string ToString() => Name ?? "<empty>";
	}
}
