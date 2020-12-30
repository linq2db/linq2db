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
		readonly SqlParameter[]? _staticParameters;

		readonly Dictionary<IQueryElement, IQueryElement> _optimized =
			new(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);

		private List<SqlParameter>? _actualParameters;
		private HashSet<string>? _usedParameterNames;

		public OptimizationContext(EvaluationContext context, SqlParameter[]? staticParameters,
			bool isParameterOrderDepended)
		{
			_staticParameters = staticParameters;
			Context = context;
			IsParameterOrderDepended = isParameterOrderDepended;
		}

		public EvaluationContext Context { get; }
		public bool IsParameterOrderDepended { get; }


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

		private void CorrectParamName(SqlParameter parameter)
		{
			if (_usedParameterNames == null)
			{
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
					p => p.Name.IsNullOrEmpty() ? "p_1" :
						char.IsDigit(p.Name[p.Name.Length - 1]) ? p.Name : p.Name + "_1",
					StringComparer.InvariantCultureIgnoreCase);

			}

			_usedParameterNames.Add(parameter.Name!);
		}

		public void ClearParameters()
		{
			_usedParameterNames = null;
			_actualParameters = null;
		}
	}
}
