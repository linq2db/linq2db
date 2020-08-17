namespace LinqToDB.SqlProvider
{
	using SqlQuery;

	public interface ISqlOptimizer
	{
		SqlStatement   Finalize         (SqlStatement statement, bool inlineParameters);
		ISqlExpression ConvertExpression(ISqlExpression expression, bool withParameters);
		ISqlPredicate  ConvertPredicate (SelectQuery selectQuery, ISqlPredicate  predicate, bool withParameters);
		SqlStatement   OptimizeStatement(SqlStatement statement, bool inlineParameters, bool withParameters);
	}
}
