namespace LinqToDB.SqlProvider
{
	using Mapping;
	using SqlQuery;

	internal static class SqlOptimizerExtensions
	{
		public static SqlStatement PrepareStatementForRemoting(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema, EvaluationContext context)
		{
			var optimizationContext = new OptimizationContext(context, null, false);
			// We need convert. Some functions works with real objects and can not be serialized
			//
			var newStatement = (SqlStatement)optimizer.ConvertElement(mappingSchema, statement, optimizationContext);

			if (!ReferenceEquals(newStatement, statement))
				newStatement.PrepareQueryAndAliases(out _);

			return newStatement;
		}

		public static SqlStatement PrepareStatementForSql(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema, EvaluationContext context)
		{
			var optimizationContext = new OptimizationContext(context, null, false);
			var newStatement = (SqlStatement)optimizer.ConvertElement(mappingSchema, statement, optimizationContext);

			if (!ReferenceEquals(newStatement, statement))
				newStatement.PrepareQueryAndAliases(out _);
			return newStatement;
		}

	}
}
