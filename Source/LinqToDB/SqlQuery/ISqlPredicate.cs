namespace LinqToDB.SqlQuery
{
	public interface ISqlPredicate : IQueryElement, ISqlExpressionWalkable
	{
		bool CanBeNull  { get; }
		int  Precedence { get; }

		bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer);
	}
}
