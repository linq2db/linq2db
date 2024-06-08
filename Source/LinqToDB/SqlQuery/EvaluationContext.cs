using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.SqlQuery
{
	using Common;

	public class EvaluationContext
	{
		private Dictionary<IQueryElement, (object? value, bool success)>? _evaluationCache;

		public EvaluationContext(IReadOnlyParameterValues? parameterValues = null)
		{
			ParameterValues = parameterValues;
		}

		public IReadOnlyParameterValues? ParameterValues { get; }

		public bool IsParametersInitialized => ParameterValues != null;

		internal bool TryGetValue(IQueryElement expr, [NotNullWhen(true)] out (object? value, bool success)? info)
		{
			if (_evaluationCache == null)
			{
				info = null;
				return false;
			}

			if (_evaluationCache.TryGetValue(expr, out var infoValue))
			{
				info = infoValue;
				return true;
			}

			info = null;
			return false;
		}

		public void Register(IQueryElement expr, object? value)
		{
			_evaluationCache ??= new (Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);
			_evaluationCache.Add(expr, (value, true));
		}

		public void RegisterError(IQueryElement expr)
		{
			_evaluationCache ??= new(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);
			_evaluationCache.Add(expr, (null, false));
		}
	}
}
