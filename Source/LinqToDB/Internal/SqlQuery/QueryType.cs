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
	}
}
