namespace LinqToDB.SqlQuery
{
	public struct SqlConditionalInsert
	{
		public SqlSearchCondition? When;
		public SqlInsertClause     Insert;

		public void Deconstruct(out SqlSearchCondition? when, out SqlInsertClause insert)
		{
			when = When;
			insert = Insert;
		}
	}
}
