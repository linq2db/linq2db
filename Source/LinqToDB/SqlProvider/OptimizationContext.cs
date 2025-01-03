using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.SqlQuery.Visitors;

namespace LinqToDB.SqlProvider
{
	using Common;
	using DataProvider;
	using Mapping;
	using SqlQuery;

	public class OptimizationContext
	{
		private IQueryParametersNormalizer?                      _parametersNormalizer;
		private Dictionary<SqlParameter, SqlParameter>?          _parametersMap;
		private List<SqlParameter>?                              _actualParameters;
		private Dictionary<(DbDataType, object?), SqlParameter>? _dynamicParameters;

		public DataOptions                   DataOptions      { get; }
		public SqlProviderFlags              SqlProviderFlags { get; }
		public MappingSchema                 MappingSchema    { get; }
		public SqlExpressionConvertVisitor   ConvertVisitor   { get; }
		public SqlExpressionOptimizerVisitor OptimizerVisitor { get; }

		readonly Func<IQueryParametersNormalizer>           _parametersNormalizerFactory;

		public SqlQueryVisitor.IVisitorTransformationInfo TransformationInfo => 
			_transformationInfo ??= new SqlQueryVisitor.VisitorTransformationInfo();

		SqlQueryVisitor.IVisitorTransformationInfo? _transformationInfo;

		public SqlQueryVisitor.IVisitorTransformationInfo TransformationInfoConvert => 
			_transformationInfoConvert ??= new SqlQueryVisitor.VisitorTransformationInfo();

		SqlQueryVisitor.IVisitorTransformationInfo? _transformationInfoConvert;

		public OptimizationContext(
			EvaluationContext                evaluationContext,
			DataOptions                      dataOptions,
			SqlProviderFlags                 sqlProviderFlags,
			MappingSchema                    mappingSchema,
			SqlExpressionOptimizerVisitor    optimizerVisitor,
			SqlExpressionConvertVisitor      convertVisitor,
			bool                             isParameterOrderDepended,
			bool                             isAlreadyOptimizedAndConverted,
			Func<IQueryParametersNormalizer> parametersNormalizerFactory)
		{
			EvaluationContext              = evaluationContext;
			DataOptions                    = dataOptions;
			SqlProviderFlags               = sqlProviderFlags;
			MappingSchema                  = mappingSchema;
			OptimizerVisitor               = optimizerVisitor;
			ConvertVisitor                 = convertVisitor;
			IsParameterOrderDependent      = isParameterOrderDepended;
			IsAlreadyOptimizedAndConverted = isAlreadyOptimizedAndConverted;
			_parametersNormalizerFactory   = parametersNormalizerFactory;
		}

		public EvaluationContext EvaluationContext              { get; }
		public bool              IsParameterOrderDependent      { get; }
		public bool              IsAlreadyOptimizedAndConverted { get; }

		public bool HasParameters() => _actualParameters?.Count > 0;

		public IReadOnlyList<SqlParameter> GetParameters() => _actualParameters ?? (IReadOnlyList<SqlParameter>)[];

		public SqlParameter AddParameter(SqlParameter parameter)
		{
			var returnValue = parameter;

			if (!IsParameterOrderDependent && _parametersMap?.TryGetValue(parameter, out var newParameter) == true)
			{
				returnValue = newParameter;
			}
			else
			{
				var newName = (_parametersNormalizer ??= _parametersNormalizerFactory()).Normalize(parameter.Name);

				if (IsParameterOrderDependent || newName != parameter.Name)
				{
					returnValue = new SqlParameter(parameter.Type, newName, parameter.Value)
					{
						AccessorId     = parameter.AccessorId,
						ValueConverter = parameter.ValueConverter,
						NeedsCast      = parameter.NeedsCast
					};
				}

				if (!IsParameterOrderDependent)
					(_parametersMap ??= new()).Add(parameter, returnValue);

				(_actualParameters ??= new()).Add(returnValue);
			}

			return returnValue;
		}

		public SqlParameter SuggestDynamicParameter(DbDataType dbDataType, object? value)
		{
			var key = (dbDataType, value);

			if (_dynamicParameters == null || !_dynamicParameters.TryGetValue(key, out var param))
			{
				// converting to SQL Parameter
				// real name (in case of conflicts) will be generated on later stage in AddParameter method
				param = new SqlParameter(dbDataType, "value", value);

				_dynamicParameters ??= new();
				_dynamicParameters.Add(key, param);
			}

			return param;
		}

		public void ClearParameters()
		{
			// must discard instance instead of Clean as it is returned by GetParameters
			_actualParameters     = null;
			_parametersNormalizer = null;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public T OptimizeAndConvertAllForRemoting<T>(T element, NullabilityContext nullabilityContext)
			where T : class, IQueryElement
		{
			var newElement = OptimizerVisitor.Optimize(EvaluationContext, nullabilityContext, null, DataOptions, MappingSchema, element, visitQueries : true, isInsideNot : false, reduceBinary: false);
			var result     = (T)ConvertVisitor.Convert(this, nullabilityContext, newElement, visitQueries : true, isInsideNot : false);

			return result;
		}

		[return : NotNullIfNotNull(nameof(element))]
		public T OptimizeAndConvertAll<T>(T element, NullabilityContext nullabilityContext)
			where T : class, IQueryElement
		{
			var newElement = OptimizerVisitor.Optimize(EvaluationContext, nullabilityContext, null, DataOptions, MappingSchema, element, visitQueries : true, isInsideNot : false, reduceBinary: true);
			var result     = (T)ConvertVisitor.Convert(this, nullabilityContext, newElement, visitQueries : true, isInsideNot : false);

			return result;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public T? OptimizeAndConvert<T>(T? element, NullabilityContext nullabilityContext, bool isInsideNot)
			where T : class, IQueryElement
		{
			if (IsAlreadyOptimizedAndConverted)
				return element;

			if (element == null)
				return null;

			var newElement = OptimizerVisitor.Optimize(EvaluationContext, nullabilityContext, null, DataOptions, MappingSchema, element, visitQueries : false, isInsideNot, reduceBinary : false);
			var result     = (T)ConvertVisitor.Convert(this, nullabilityContext, newElement, false, isInsideNot);

			return result;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public T? Optimize<T>(T? element, NullabilityContext nullabilityContext, bool isInsideNot, bool reduceBinary)
			where T : class, IQueryElement
		{
			if (element == null)
				return null;

			var newElement = OptimizerVisitor.Optimize(EvaluationContext, nullabilityContext, null, DataOptions, MappingSchema, element, false, isInsideNot, reduceBinary);

			return (T)newElement;
		}
	}
}
