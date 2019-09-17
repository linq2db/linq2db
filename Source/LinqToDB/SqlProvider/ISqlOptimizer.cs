using System;

namespace LinqToDB.SqlProvider
{
	using SqlQuery;

	public interface ISqlOptimizer
	{
		SqlProviderFlags SqlProviderFlags { get; }

		SqlStatement   Finalize         (SqlStatement statement);
		ISqlExpression ConvertExpression(ISqlExpression expression);
		ISqlPredicate  ConvertPredicate (SelectQuery selectQuery, ISqlPredicate predicate);
		SqlStatement   OptimizeStatement(SqlStatement statement);
	}
}
