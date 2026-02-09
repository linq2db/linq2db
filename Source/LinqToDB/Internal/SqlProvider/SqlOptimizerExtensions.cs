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
			var optimizationContext = optimizer.CreateOptimizationContext(mappingSchema, dataOptions);
			var nullability = NullabilityContext.GetContext(statement.SelectQuery);
			var newStatement = optimizationContext.OptimizeAndConvertAll(statement, nullability);

			return newStatement;
		}

		public static SqlStatement PrepareStatementForSql(this SqlStatement statement, OptimizationContext optimizationContext)
		{
			var nullability = NullabilityContext.GetContext(statement.SelectQuery);

			var newStatement = optimizationContext.OptimizeAndConvertAll(statement, nullability);

			return newStatement;
		}

		public static OptimizationContext CreateOptimizationContext(this ISqlOptimizer sqlOptimizer, MappingSchema mappingSchema, DataOptions dataOptions)
		{
			var evaluationContext = new EvaluationContext(null);
			var treeOptimizer     = sqlOptimizer.CreateOptimizerVisitor(false);
			var convertVisitor    = sqlOptimizer.CreateConvertVisitor(false);
			var factory           = sqlOptimizer.CreateSqlExpressionFactory(mappingSchema, dataOptions);

			return new OptimizationContext(
				evaluationContext,
				dataOptions,
				sqlOptimizer.SqlProviderFlags,
				mappingSchema,
				treeOptimizer,
				convertVisitor,
				factory,
				sqlOptimizer.SqlProviderFlags.IsParameterOrderDependent,
				isAlreadyOptimizedAndConverted: false,
				parametersNormalizerFactory: static () => NoopQueryParametersNormalizer.Instance);
		}
	}
}
