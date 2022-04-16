using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LinqToDB.Common;
using LinqToDB.SqlQuery;

namespace LinqToDB.SqlProvider
{
	public class OptimizationContext
	{
		private SqlParameter[]? _staticParameters;

		readonly Dictionary<IQueryElement, IQueryElement> _optimized =
			new(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);

		private List<SqlParameter>? _actualParameters;
		private ISet<string>?       _usedParameterNames;

		private Dictionary<(DbDataType, string, object?), SqlParameter>? _dynamicParameters;

		public OptimizationContext(EvaluationContext context, AliasesContext aliases, 
			bool isParameterOrderDepended)
		{
			Aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));
			Context = context;
			IsParameterOrderDepended = isParameterOrderDepended;
		}

		public EvaluationContext Context                  { get; }
		public bool              IsParameterOrderDepended { get; }
		public AliasesContext    Aliases                  { get; }

		public bool IsOptimized(IQueryElement element, [NotNullWhen(true)] out IQueryElement? newExpr)
		{
			if (_optimized.TryGetValue(element, out var replaced))
			{
				if (replaced != element)
				{
					while (_optimized.TryGetValue(replaced, out var another))
					{
						if (replaced == another)
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

		public bool HasParameters() => _actualParameters != null && _actualParameters.Count > 0;

		public IEnumerable<SqlParameter> GetParameters()
		{
			if (_actualParameters == null)
				return Array<SqlParameter>.Empty;

			return _actualParameters;
		}

		public SqlParameter AddParameter(SqlParameter parameter)
		{
			_actualParameters ??= new List<SqlParameter>();

			var alreadyRegistered = _actualParameters.Contains(parameter);
			if (IsParameterOrderDepended || !alreadyRegistered)
			{
				if (alreadyRegistered)
				{
					parameter = new SqlParameter(parameter.Type, parameter.Name, parameter.Value)
					{
						AccessorId = parameter.AccessorId
					};
				}

				CorrectParamName(parameter);

				_actualParameters.Add(parameter);
			}

			return parameter;
		}

		public SqlParameter SuggestDynamicParameter(DbDataType dbDataType, string name, object? value)
		{
			var key = (dbDataType, name, value);

			if (_dynamicParameters == null || !_dynamicParameters.TryGetValue(key, out var param))
			{
				// converting to SQL Parameter
				param = new SqlParameter(dbDataType, name, value);

				_dynamicParameters ??= new();
				_dynamicParameters.Add(key, param);
			}

			return param;
		}

		private void CorrectParamName(SqlParameter parameter)
		{
			if (_usedParameterNames == null)
			{
				_staticParameters = Aliases.GetParameters();

				if (_staticParameters == null)
					_usedParameterNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
				else
					_usedParameterNames = new HashSet<string>(_staticParameters.Select(p => p.Name!),
						StringComparer.InvariantCultureIgnoreCase);
			}

			if (!(_staticParameters?.Contains(parameter) == true) 
			    && (string.IsNullOrEmpty(parameter.Name) || _usedParameterNames.Contains(parameter.Name!)))
			{
				Utils.MakeUniqueNames(new[] {parameter}, _usedParameterNames, p => p.Name,
					(p, v, s) => p.Name = v,
					p => string.IsNullOrEmpty(p.Name) ? "p_1" :
						char.IsDigit(p.Name![p.Name.Length - 1]) ? p.Name : p.Name + "_1",
					StringComparer.InvariantCultureIgnoreCase);

			}

			_usedParameterNames.Add(parameter.Name!);
		}

		public void ClearParameters()
		{
			_usedParameterNames = null;
			_actualParameters = null;
		}

		private ConvertVisitor<BasicSqlOptimizer.RunOptimizationContext>? _visitor;

		private int _nestingLevel;
		public T ConvertAll<T>(
			BasicSqlOptimizer.RunOptimizationContext context,
			T                                        element,
			Func<ConvertVisitor<BasicSqlOptimizer.RunOptimizationContext>, IQueryElement, IQueryElement> convertAction,
			Func<ConvertVisitor<BasicSqlOptimizer.RunOptimizationContext>, bool>                         parentAction)
			where T : class, IQueryElement
		{
			if (_visitor == null)
				_visitor = new ConvertVisitor<BasicSqlOptimizer.RunOptimizationContext>(context, convertAction, true, false, false, parentAction);
			else
				_visitor.Reset(context, convertAction, true, false, false, parentAction);

			// temporary(?) guard
			if (_nestingLevel > 0)
				throw new InvalidOperationException("Nested optimization detected");
			_nestingLevel++;
			var res = (T?)_visitor.ConvertInternal(element) ?? element;
			_nestingLevel--;
			return res;
		}
	}
}
