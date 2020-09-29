using System;
using System.Collections.Generic;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.SqlProvider
{
	internal static class SqlOptimizerExtensions
	{
		public static SqlStatement PrepareStatementForRemoting(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema, IReadOnlyDictionary<SqlParameter, SqlParameterValue>? parameterValues)
		{
			var newStatement = optimizer.OptimizeStatement(statement, parameterValues);
			newStatement     = optimizer.ConvertStatement(mappingSchema, newStatement, parameterValues);
			newStatement.PrepareQueryAndAliases();
			return newStatement;
		}

		public static SqlStatement PrepareStatementForSql(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema, IReadOnlyDictionary<SqlParameter, SqlParameterValue>? parameterValues)
		{
			var newStatement = optimizer.OptimizeStatement(statement, parameterValues);
			newStatement     = optimizer.ConvertStatement(mappingSchema, newStatement, parameterValues);
			newStatement.PrepareQueryAndAliases();
			return newStatement;
		}

	}
}
