using LinqToDB.DataProvider;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.SqlProvider
{
	internal static class SqlOptimizerExtensions
	{
		public static SqlStatement PrepareStatementForRemoting(this ISqlOptimizer optimizer, SqlStatement statement,
			SqlProviderFlags sqlProviderFlags,
			MappingSchema mappingSchema, DataOptions dataOptions, EvaluationContext evaluationContext)
		{
			var factory = optimizer.CreateSqlExpressionFactory(mappingSchema, dataOptions);

			var optimizationContext = new OptimizationContext(
				evaluationContext,
				dataOptions,
				sqlProviderFlags : sqlProviderFlags,
				mappingSchema : mappingSchema,
				optimizerVisitor : optimizer.CreateOptimizerVisitor(false),
				convertVisitor : optimizer.CreateConvertVisitor(false), 
				factory : factory,
				isParameterOrderDepended : false,
				isAlreadyOptimizedAndConverted : false,
				parametersNormalizerFactory : static () => NoopQueryParametersNormalizer.Instance);

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
