namespace LinqToDB.SqlQuery
{
	public class SqlUpdateStatement : SqlSelectStatement
	{
		public SqlUpdateStatement(SelectQuery selectQuery) : base(selectQuery)
		{
		}

		public override QueryType        QueryType   => QueryType.Update;
		public override QueryElementType ElementType => QueryElementType.UpdateStatement;

		public SqlUpdateStatement()
		{
		}
	}
}
