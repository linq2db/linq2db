namespace LinqToDB.SqlQuery.Visitors
{
	public class SqlQueryHelperVisitorBase : QueryElementVisitor
	{
		public SqlQueryHelperVisitorBase() : base(VisitMode.ReadOnly)
		{
		}

		public override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			Visit(column);
			return base.VisitSqlColumnExpression(column, expression);
		}

		public override IQueryElement VisitSqlTable(SqlTable element)
		{
			return base.VisitSqlTable(element);
		}
	}
}
