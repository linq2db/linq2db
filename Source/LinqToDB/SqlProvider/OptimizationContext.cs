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
		public SqlProviderFlags?             SqlProviderFlags { get; }
		public MappingSchema                 MappingSchema    { get; }
		public SqlExpressionConvertVisitor   ConvertVisitor   { get; }
		public SqlExpressionOptimizerVisitor OptimizerVisitor { get; }

		readonly Func<IQueryParametersNormalizer>           _parametersNormalizerFactory;

		public SqlQueryVisitor.IVisitorTransformationInfo TransformationInfo => 
			_transformationInfo ??= new SqlQueryVisitor.VisitorTransformationInfo();

		SqlQueryVisitor.IVisitorTransformationInfo? _transformationInfo;

		public OptimizationContext(
			EvaluationContext                evaluationContext,
			DataOptions                      dataOptions,
			SqlProviderFlags?                sqlProviderFlags,
			MappingSchema                    mappingSchema,
			AliasesContext                   aliases,
			SqlExpressionOptimizerVisitor    optimizerVisitor,
			SqlExpressionConvertVisitor      convertVisitor,
			bool                             isParameterOrderDepended,
			Func<IQueryParametersNormalizer> parametersNormalizerFactory)
		{
			EvaluationContext            = evaluationContext;
			DataOptions                  = dataOptions;
			SqlProviderFlags             = sqlProviderFlags;
			MappingSchema                = mappingSchema;
			Aliases                      = aliases ?? throw new ArgumentNullException(nameof(aliases));
			OptimizerVisitor             = optimizerVisitor;
			ConvertVisitor               = convertVisitor;
			IsParameterOrderDependent    = isParameterOrderDepended;
			_parametersNormalizerFactory = parametersNormalizerFactory;
		}

		public EvaluationContext EvaluationContext                   { get; }
		public bool              IsParameterOrderDependent { get; }
		public AliasesContext    Aliases                   { get; }

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
		public T? ConvertAll<T>(T? element, NullabilityContext nullabilityContext)
			where T : class, IQueryElement
		{
			if (element == null)
				return null;

			// if parameters are not initialized, it means that query already optimized in SelectQueryOptimizerVisitor
			var newElement = !EvaluationContext.IsParametersInitialized
				? element
				: OptimizerVisitor.Optimize(EvaluationContext, nullabilityContext, TransformationInfo, DataOptions, element);

			var result = (T)ConvertVisitor.Convert(this, nullabilityContext, newElement);

			return result;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public T? OptimizeAll<T>(T? element, NullabilityContext nullabilityContext)
			where T : class, IQueryElement
		{
			if (element == null)
				return null;

			var newElement = OptimizerVisitor.Optimize(EvaluationContext, nullabilityContext, TransformationInfo, DataOptions, element);

			return (T)newElement;
		}
	}
}
