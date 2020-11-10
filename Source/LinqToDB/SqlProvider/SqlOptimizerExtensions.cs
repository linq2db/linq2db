namespace LinqToDB.SqlProvider
{
	using Mapping;
	using SqlQuery;

	internal static class SqlOptimizerExtensions
	{
		public static SqlStatement PrepareStatementForRemoting(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema, EvaluationContext context)
		{
			var newStatement = optimizer.OptimizeStatement(statement, context);

			// We need convert. Some functions works with real objects and can not be serialized
			//
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
