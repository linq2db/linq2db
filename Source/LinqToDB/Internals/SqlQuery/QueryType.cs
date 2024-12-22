namespace LinqToDB.Internals.SqlQuery
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
	}
}
