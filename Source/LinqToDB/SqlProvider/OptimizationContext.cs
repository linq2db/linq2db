using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Common;
using LinqToDB.DataProvider;
using LinqToDB.SqlQuery;
using LinqToDB.SqlQuery.Visitors;

namespace LinqToDB.SqlProvider
{
	public class OptimizationContext
	{
		private IQueryParametersNormalizer?                      _parametersNormalizer;
		private List<SqlParameter>?                              _actualParameters;
		private Dictionary<(DbDataType, object?), SqlParameter>? _dynamicParameters;

		private readonly Dictionary<IQueryElement, IQueryElement> _optimized = new(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);
		private readonly Func<IQueryParametersNormalizer>         _parametersNormalizerFactory;

		public OptimizationContext(
			EvaluationContext                context,
			AliasesContext                   aliases,
			bool                             isParameterOrderDepended,
			Func<IQueryParametersNormalizer> parametersNormalizerFactory)
		{
			Aliases                      = aliases ?? throw new ArgumentNullException(nameof(aliases));
			Context                      = context;
			IsParameterOrderDepended     = isParameterOrderDepended;
			_parametersNormalizerFactory = parametersNormalizerFactory;
		}

		public EvaluationContext Context                  { get; }
		public bool              IsParameterOrderDepended { get; }
		public AliasesContext    Aliases                  { get; }

		public bool IsOptimized(IQueryElement element, [NotNullWhen(true)] out IQueryElement? newExpr)
		{
			if (_optimized.TryGetValue(element, out var replaced))
			{
				if (!ReferenceEquals(replaced, element))
				{
					while (_optimized.TryGetValue(replaced, out var another))
					{
						if (ReferenceEquals(replaced, another))
							break;
						replaced = another;
					}
				}

				newExpr = replaced;
				return true;
			}

			newExpr = null;
			return false;
		}

		public void RegisterOptimized(IQueryElement element, IQueryElement newExpr)
		{
			_optimized[element] = newExpr;
		}

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

		SqlQueryConvertVisitor<BasicSqlOptimizer.RunOptimizationContext>? _visitor;

		int _nestingLevel;

		public T ConvertAll<T>(
			BasicSqlOptimizer.RunOptimizationContext context,
			T                                        element,
			Func<SqlQueryConvertVisitor<BasicSqlOptimizer.RunOptimizationContext>, IQueryElement, IQueryElement> convertAction,
			Func<SqlQueryConvertVisitor<BasicSqlOptimizer.RunOptimizationContext>, bool>                         parentAction)
			where T : class, IQueryElement
		{
			if (_visitor == null)
				_visitor = new SqlQueryConvertVisitor<BasicSqlOptimizer.RunOptimizationContext>(false);
			else
				_visitor.Cleanup();

			// temporary(?) guard
			if (_nestingLevel > 0)
				throw new InvalidOperationException("Nested optimization detected");
			_nestingLevel++;
			var res = (T?)_visitor.Convert(element, context, convertAction, false) ?? element;
			_nestingLevel--;
			return res;
		}
	}
}
