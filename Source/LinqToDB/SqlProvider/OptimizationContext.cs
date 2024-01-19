using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

		readonly DataOptions                   _dataOptions;
		readonly SqlProviderFlags?             _sqlProviderFlags;
		readonly MappingSchema                 _mappingSchema;
		readonly SqlExpressionOptimizerVisitor _optimizerVisitor;
		readonly SqlExpressionConvertVisitor   _convertVisitor;

		readonly Func<IQueryParametersNormalizer> _parametersNormalizerFactory;

		public OptimizationContext(
			EvaluationContext                context,
			DataOptions                      dataOptions,
			SqlProviderFlags?                sqlProviderFlags,
			MappingSchema                    mappingSchema,
			AliasesContext                   aliases,
			SqlExpressionOptimizerVisitor    optimizerVisitor,
			SqlExpressionConvertVisitor      convertVisitor,
			bool                             isParameterOrderDepended,
			Func<IQueryParametersNormalizer> parametersNormalizerFactory)
		{
			Context                      = context;
			_dataOptions                 = dataOptions;
			_sqlProviderFlags            = sqlProviderFlags;
			_mappingSchema               = mappingSchema;
			Aliases                      = aliases ?? throw new ArgumentNullException(nameof(aliases));
			_optimizerVisitor            = optimizerVisitor;
			_convertVisitor              = convertVisitor;
			IsParameterOrderDependent    = isParameterOrderDepended;
			_optimizerVisitor            = optimizerVisitor;
			_parametersNormalizerFactory = parametersNormalizerFactory;
		}

		public EvaluationContext Context                   { get; }
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

			var newElement = _optimizerVisitor.Optimize(Context, nullabilityContext, _sqlProviderFlags, _dataOptions, element);
			var result = (T)_convertVisitor.Convert(this, nullabilityContext, _sqlProviderFlags, _dataOptions, _mappingSchema, newElement);
			if (!ReferenceEquals(result, newElement))
				result = (T)_optimizerVisitor.Optimize(Context, nullabilityContext, _sqlProviderFlags, _dataOptions, result);

			return result;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public T? OptimizeAll<T>(T? element, NullabilityContext nullabilityContext)
			where T : class, IQueryElement
		{
			if (element == null)
				return null;

			var newElement = _optimizerVisitor.Optimize(Context, nullabilityContext, _sqlProviderFlags, _dataOptions, element);

			return (T)newElement;
		}
	}
}
