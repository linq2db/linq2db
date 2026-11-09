namespace LinqToDB.Internal.SqlQuery
{
	public enum QueryType
	{
		Select,
		Delete,
		Update,
		Insert,
		InsertOrUpdate,
		CreateTable,
		DropTable,
		TruncateTable,
		Merge,
		MultiInsert,
		/// <summary>
		/// A provider-built auxiliary statement whose SQL text is supplied verbatim (<see cref="SqlRawStatement"/>):
		/// identity reset after a truncate, Oracle identity sequence / trigger creation and similar DDL that has no
		/// statement node of its own. Produced only by a provider DML service while building a command scenario.
		/// </summary>
		Raw,
	}
}
