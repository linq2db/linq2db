using System;

namespace LinqToDB.SqlProvider
{
	using Mapping;
	using SqlQuery;

	internal static class SqlOptimizerExtensions
	{
		public static SqlStatement PrepareStatementForRemoting(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema, DataOptions dataOptions, AliasesContext aliases, EvaluationContext context)
		{
			var optimizationContext = new OptimizationContext(context, aliases, false);

			var newStatement = (SqlStatement)optimizer.ConvertElement(mappingSchema, dataOptions, statement, optimizationContext);

			return newStatement;
		}

		public static SqlStatement PrepareStatementForSql(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema, DataOptions dataOptions, OptimizationContext optimizationContext)
		{
			var newStatement = (SqlStatement)optimizer.ConvertElement(mappingSchema, dataOptions, statement, optimizationContext);

			return newStatement;
		}
	}
}
