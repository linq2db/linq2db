namespace LinqToDB.Internal.SqlQuery.Visitors
{
	public abstract class SqlQueryHelperVisitorBase : QueryElementVisitor
	{
		protected SqlQueryHelperVisitorBase() : base(VisitMode.ReadOnly)
		{
		}

		protected override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			Visit(column);
			return base.VisitSqlColumnExpression(column, expression);
		}

		protected override IQueryElement VisitSqlTable(SqlTable element)
		{
			return base.VisitSqlTable(element);
		}
	}
}
