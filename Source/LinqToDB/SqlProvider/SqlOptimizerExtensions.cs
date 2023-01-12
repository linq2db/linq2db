namespace LinqToDB.SqlProvider
{
	using Mapping;
	using SqlQuery;

	internal static class SqlOptimizerExtensions
	{
		public static SqlStatement PrepareStatementForRemoting(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema, AliasesContext aliases, EvaluationContext context)
		{
			var optimizationContext = new OptimizationContext(context, aliases, false);

			var nullability = statement.SelectQuery == null
				? NullabilityContext.NonQuery
				: new (statement.SelectQuery);

			var newStatement = (SqlStatement)optimizer.ConvertElement(mappingSchema, statement, optimizationContext, nullability);

			return newStatement;
		}

		public static SqlStatement PrepareStatementForSql(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema, OptimizationContext optimizationContext)
		{
			var nullability = statement.SelectQuery == null
				? NullabilityContext.NonQuery
				: new (statement.SelectQuery);

			var newStatement = (SqlStatement)optimizer.ConvertElement(mappingSchema, statement, optimizationContext, nullability);

			return newStatement;
		}

	}
}
