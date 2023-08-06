using System;
using System.Collections.Generic;

namespace LinqToDB.SqlProvider
{
	using System.Diagnostics.CodeAnalysis;

	using Common;
	using DataProvider;
	using Mapping;
	using SqlQuery;

	public class OptimizationContext
	{
		IQueryParametersNormalizer?                      _parametersNormalizer;
		List<SqlParameter>?                              _actualParameters;
		Dictionary<(DbDataType, object?), SqlParameter>? _dynamicParameters;

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
			IsParameterOrderDepended     = isParameterOrderDepended;
			_optimizerVisitor            = optimizerVisitor;
			_parametersNormalizerFactory = parametersNormalizerFactory;
		}

		public EvaluationContext Context                  { get; }
		public bool              IsParameterOrderDepended { get; }
		public AliasesContext    Aliases                  { get; }

		public bool HasParameters() => _actualParameters?.Count > 0;

		public IReadOnlyList<SqlParameter> GetParameters() => _actualParameters ?? (IReadOnlyList<SqlParameter>)Array<SqlParameter>.Empty;

		public SqlParameter AddParameter(SqlParameter parameter)
		{
			var alreadyRegistered = _actualParameters?.Contains(parameter) == true;
			if (IsParameterOrderDepended || !alreadyRegistered)
			{
				if (alreadyRegistered)
				{
					parameter = new SqlParameter(parameter.Type, parameter.Name, parameter.Value)
					{
						AccessorId = parameter.AccessorId
					};
				}

				parameter.Name = (_parametersNormalizer ??= _parametersNormalizerFactory()).Normalize(parameter.Name);

				(_actualParameters ??= new()).Add(parameter);
			}

			return parameter;
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
