using System;
using System.Collections.Generic;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.SqlProvider
{
	internal static class SqlOptimizerExtensions
	{
		public static SqlStatement PrepareStatementForRemoting(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema, EvaluationContext context)
		{
			var newStatement = optimizer.OptimizeStatement(statement, context);
			newStatement     = optimizer.ConvertStatement(mappingSchema, newStatement, context);
			if (!ReferenceEquals(newStatement, statement))
				newStatement.PrepareQueryAndAliases();
			return newStatement;
		}

		public static SqlStatement PrepareStatementForSql(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema, EvaluationContext context)
		{
			var newStatement = optimizer.OptimizeStatement(statement, context);
			newStatement     = optimizer.ConvertStatement(mappingSchema, newStatement, context);
			if (!ReferenceEquals(newStatement, statement))
				newStatement.PrepareQueryAndAliases();
			return newStatement;
		}

	}
}
