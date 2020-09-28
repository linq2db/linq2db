using System;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.SqlProvider
{
	internal static class SqlOptimizerExtensions
	{
		public static SqlStatement PrepareStatementForRemoting(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema)
		{
			var newStatement = optimizer.OptimizeStatement(statement, true);
			newStatement     = optimizer.ConvertStatement(mappingSchema, newStatement, true);
			newStatement.PrepareQueryAndAliases();
			return newStatement;
		}

		public static SqlStatement PrepareStatementForSql(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema)
		{
			var newStatement = optimizer.OptimizeStatement(statement, true);
			newStatement     = optimizer.ConvertStatement(mappingSchema, newStatement, true);
			newStatement.PrepareQueryAndAliases();
			return newStatement;
		}

	}
}
