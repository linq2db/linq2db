using LinqToDB.CodeModel;

namespace LinqToDB.DataModel
{
	// contains generation logic for aggregate function mappings
	partial class DataModelGenerator
	{
		/// <summary>
		/// Generates aggregate function mapping.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		/// <param name="aggregate">Aggregate function model.</param>
		private static void BuildAggregateFunction(IDataModelGenerationContext context, AggregateFunctionModel aggregate)
		{
			// generation sample:
			/*
			 * [Sql.Function("test_avg", ArgIndices = new []{ 1 }, ServerSideOnly = true, IsAggregate = true)]
			 * public static double? TestAvg<TSource>(this IEnumerable<TSource> src, Expression<Func<TSource, double>> value)
			 * {
			 *     throw new InvalidOperationException("error message here");
			 * }
			 */
			// where
			// - src/TSource: any aggregated table-like source
			// - value: actual aggregated value (value selector from source)

			var method = context.DefineMethod(
				context.AddAggregateFunctionRegion(aggregate.Method.Name).Methods(false),
				aggregate.Method);

			// aggregates cannot be used outside of query context, so we throw exception from method
			var body = method
				.Body()
				.Append(
					context.AST.Throw(context.AST.New(
						WellKnownTypes.System.InvalidOperationException,
						context.AST.Constant(DataModelConstants.EXCEPTION_QUERY_ONLY_ASSOCATION_CALL, true))));

			var source = context.AST.TypeParameter(context.AST.Name(DataModelConstants.AGGREGATE_RECORD_TYPE));
			method.TypeParameter(source);

			method.Returns(aggregate.ReturnType);

			method.Method.ChangeHandler += m =>
			{
				aggregate.ReturnType = m.ReturnType!.Type;
			};

			// define parameters
			// aggregate has at least one parameter - collection of aggregated values
			// and optionally could have one or more additional scalar parameters
			var sourceParam = context.AST.Parameter(
				WellKnownTypes.System.Collections.Generic.IEnumerable(source),
				context.AST.Name(DataModelConstants.AGGREGATE_SOURCE_PARAMETER),
				CodeParameterDirection.In);
			method.Parameter(sourceParam);

			if (aggregate.Parameters.Count > 0)
			{
				for (var i = 0; i < aggregate.Parameters.Count; i++)
				{
					var param         = aggregate.Parameters[i];
					var parameterType = param.Parameter.Type;

					// scalar parameters have following type:
					// Expression<Func<TSource, param_type>>
					// which allows user to specify aggregated value(s) selection from source record
					parameterType = WellKnownTypes.System.Linq.Expressions.Expression(
						WellKnownTypes.System.Func(parameterType, source));

					param.Parameter.Type      = parameterType;
					param.Parameter.Direction = CodeParameterDirection.In;

					context.DefineParameter(method, param.Parameter);
				}
			}

			// metadata last
			context.MetadataBuilder?.BuildFunctionMetadata(context, aggregate.Metadata, method);
		}
	}
}
