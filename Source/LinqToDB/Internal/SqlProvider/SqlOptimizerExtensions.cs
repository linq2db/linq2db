using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.SqlProvider
{
	internal static class SqlOptimizerExtensions
	{
		public static SqlStatement PrepareStatementForRemoting(this ISqlOptimizer optimizer, SqlStatement statement,
			SqlProviderFlags sqlProviderFlags,
			MappingSchema mappingSchema, DataOptions dataOptions, EvaluationContext evaluationContext)
		{
			var optimizationContext = new OptimizationContext(
				evaluationContext,
				dataOptions,
				sqlProviderFlags: sqlProviderFlags,
				mappingSchema,
				optimizer.CreateOptimizerVisitor(false),
				optimizer.CreateConvertVisitor(false),
				isParameterOrderDepended: false,
				isAlreadyOptimizedAndConverted: false,
				static () => NoopQueryParametersNormalizer.Instance);

			var nullability = NullabilityContext.GetContext(statement.SelectQuery);

			var newStatement = optimizationContext.OptimizeAndConvertAllForRemoting(statement, nullability);

			return newStatement;
		}

		public static SqlStatement PrepareStatementForSql(this ISqlOptimizer optimizer, SqlStatement statement,
			MappingSchema mappingSchema, DataOptions dataOptions, OptimizationContext optimizationContext)
		{
			var nullability = NullabilityContext.GetContext(statement.SelectQuery);

			var newStatement = optimizationContext.OptimizeAndConvertAll(statement, nullability);

			return newStatement;
		}
	}
}
