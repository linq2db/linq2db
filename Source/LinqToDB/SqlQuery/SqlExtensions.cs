namespace LinqToDB.SqlQuery
{
	public static class SqlExtensions
	{
		public static bool IsInsert(this SqlStatement statement)
		{
			return statement.QueryType == QueryType.Insert ||
			       statement.QueryType == QueryType.InsertOrUpdate;
		}

		public static bool IsInsertWithIdentity(this SqlStatement statement)
		{
			return statement.IsInsert() && ((SelectQuery)statement).Insert.WithIdentity;
		}
	}
}
