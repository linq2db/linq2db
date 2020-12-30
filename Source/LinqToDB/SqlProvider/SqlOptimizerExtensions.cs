namespace LinqToDB.SqlProvider
{
	using Mapping;
	using SqlQuery;

	internal static class SqlOptimizerExtensions
	{
		public static SqlStatement PrepareStatementForRemoting(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema, SqlParameter[]? parameters, EvaluationContext context)
		{
			var optimizationContext = new OptimizationContext(context, parameters, false);

			var newStatement = (SqlStatement)optimizer.ConvertElement(mappingSchema, statement, optimizationContext);

			return newStatement;
		}

		public static SqlStatement PrepareStatementForSql(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema, SqlParameter[]? parameters, OptimizationContext optimizationContext)
		{
			var newStatement = (SqlStatement)optimizer.ConvertElement(mappingSchema, statement, optimizationContext);

			return newStatement;
		}

	}
}
