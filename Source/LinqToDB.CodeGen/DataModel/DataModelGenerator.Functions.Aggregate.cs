using System;
using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.DataModel
{
	// contains generation logic for aggregate function mappings
	partial class DataModelGenerator
	{
		/// <summary>
		/// Generates aggregate function mapping.
		/// </summary>
		/// <param name="aggregate">Aggrrgate function model.</param>
		/// <param name="functionsGroup">Functions region.</param>
		private void BuildAggregateFunction(AggregateFunctionModel aggregate, Func<RegionGroup> functionsGroup)
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

			var method = DefineMethod(
				functionsGroup().New(aggregate.Method.Name).Methods(false),
				aggregate.Method);

			// aggregates cannot be used outside of query context, so we throw exception from method
			var body = method
				.Body()
				.Append(
					_code.Throw(_code.New(
						WellKnownTypes.System.InvalidOperationException,
						_code.Constant(EXCEPTION_QUERY_ONLY_ASSOCATION_CALL, true))));

			// build mappings
			_metadataBuilder.BuildFunctionMetadata(aggregate.Metadata, method);

			var source = _code.TypeParameter(_code.Name(AGGREGATE_RECORD_TYPE));
			method.TypeParameter(source);

			method.Returns(aggregate.ReturnType);

			// define parameters
			// aggregate has at least one parameter - collection of aggregated values
			// and optionally could have one or more additional scalar parameters
			var sourceParam = _code.Parameter(
				WellKnownTypes.System.Collections.Generic.IEnumerable(source),
				_code.Name(AGGREGATE_SOURCE_PARAMETER),
				ParameterDirection.In);
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

					var p = _code.Parameter(parameterType, _code.Name(param.Parameter.Name, null, i + 1), ParameterDirection.In);
					method.Parameter(p);

					if (param.Parameter.Description != null)
						method.XmlComment().Parameter(p.Name, param.Parameter.Description);
				}
			}
		}
	}
}
