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

			var newStatement = optimizationContext.OptimizeAndConvertAll(statement, nullability);

			return newStatement;
		}

		public static SqlStatement PrepareStatementForSql(this SqlStatement statement, OptimizationContext optimizationContext)
		{
			var nullability = NullabilityContext.GetContext(statement.SelectQuery);

			var newStatement = optimizationContext.OptimizeAndConvertAll(statement, nullability);

			return newStatement;
		}
	}
}
