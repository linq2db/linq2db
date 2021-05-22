namespace LinqToDB.SqlQuery
{
	public interface ISqlPredicate : IQueryElement, ISqlExpressionWalkable
	{
		bool CanBeNull  { get; }
		int  Precedence { get; }
	}
}
