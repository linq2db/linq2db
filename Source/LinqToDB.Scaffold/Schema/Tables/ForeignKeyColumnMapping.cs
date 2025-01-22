namespace LinqToDB.Schema
{
	/// <summary>
	/// Foreign key source-target column pair.
	/// </summary>
	/// <param name="SourceColumn">Column in foreign key source table.</param>
	/// <param name="TargetColumn">Column in foreign key target table.</param>
	public sealed record ForeignKeyColumnMapping(string SourceColumn, string TargetColumn);
}
