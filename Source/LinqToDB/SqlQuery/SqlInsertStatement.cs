namespace LinqToDB.SqlQuery
{
	public class SqlInsertStatement : SqlSelectStatement
	{
		//TODO: move from SelectQuery
//		public override QueryType        QueryType   => QueryType.Update;
		public override QueryElementType ElementType => QueryElementType.InsertStatement;

		public SqlInsertStatement(SelectQuery selectQuery) : base(selectQuery)
		{
		}

		public SqlInsertStatement()
		{
		}
	}
}
