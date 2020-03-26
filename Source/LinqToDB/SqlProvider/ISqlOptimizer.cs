﻿namespace LinqToDB.SqlProvider
{
	using SqlQuery;

	public interface ISqlOptimizer
	{
		SqlStatement   Finalize         (SqlStatement statement);
		ISqlExpression ConvertExpression(ISqlExpression expression);
		ISqlPredicate  ConvertPredicate (SelectQuery selectQuery, ISqlPredicate  predicate);
		SqlStatement   OptimizeStatement(SqlStatement statement, bool inlineParameters);
	}
}
