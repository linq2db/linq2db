using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Common;

namespace LinqToDB.SqlQuery
{
	public class EvaluationContext
	{
		private Dictionary<IQueryElement, (object? value, bool success)>? _clientEvaluationCache;
		private Dictionary<IQueryElement, (object? value, bool success)>? _serverEvaluationCache;

		public EvaluationContext(IReadOnlyParameterValues? parameterValues = null)
		{
			ParameterValues = parameterValues;
		}

		public IReadOnlyParameterValues? ParameterValues { get; }

		public bool IsParametersInitialized => ParameterValues != null;

		internal bool TryGetValue(IQueryElement expr, bool forServer, [NotNullWhen(true)] out (object? value, bool success)? info)
		{
			var chache = forServer ? _serverEvaluationCache : _clientEvaluationCache;
			if (chache == null)
			{
				info = null;
				return false;
			}

			if (chache.TryGetValue(expr, out var infoValue))
			{
				info = infoValue;
				return true;
			}

			info = null;
			return false;
		}

		public void Register(IQueryElement expr, bool forServer, object? value)
		{
			if (forServer)
			{
				_serverEvaluationCache ??= new(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);
				_serverEvaluationCache.Add(expr, (value, true));
			}
			else
			{
				_clientEvaluationCache ??= new(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);
				_clientEvaluationCache.Add(expr, (value, true));
			}
		}

		public void RegisterError(IQueryElement expr, bool forServer)
		{
			if (forServer)
			{
				_serverEvaluationCache ??= new(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);
				_serverEvaluationCache.Add(expr, (null, false));
			}
			else
			{
				_clientEvaluationCache ??= new(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);
				_clientEvaluationCache.Add(expr, (null, false));
			}
		}
	}
}
