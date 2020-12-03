using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.SqlQuery
{
	using Common;

	public class EvaluationContext
	{
		public class EvaluationInfo
		{
			public EvaluationInfo(bool isEvaluated, object? value, string? errorMessage)
			{
				IsEvaluated  = isEvaluated;
				Value        = value;
				ErrorMessage = errorMessage;
			}

			public bool    IsEvaluated  { get; }
			public object? Value        { get; }
			public string? ErrorMessage { get; }
		}

		private Dictionary<IQueryElement, EvaluationInfo>? _evaluationCache;

		public EvaluationContext(IReadOnlyParameterValues? parameterValues = null)
		{
			ParameterValues = parameterValues;
		}

		public IReadOnlyParameterValues? ParameterValues { get; }

		public bool TryGetValue(IQueryElement expr, [MaybeNullWhen(false)] out EvaluationInfo? info)
		{
			if (_evaluationCache == null)
			{
				info = null;
				return false;
			}

			return _evaluationCache.TryGetValue(expr, out info);
		}

		public void Register(IQueryElement expr, EvaluationInfo info)
		{
			_evaluationCache ??= new Dictionary<IQueryElement, EvaluationInfo>(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);
			_evaluationCache.Add(expr, info);
		}

	}
}
