using System;
using System.Collections.Generic;

namespace LinqToDB.Internal.Linq.Builder
{
	internal static class BuildContextDebuggingHelper
	{
		public static string GetContextInfo(IBuildContext context)
		{
#if DEBUG
			var contextId = FormattableString.Invariant($"[ID:{context.ContextId}]");
#else
			var contextId = string.Empty;
#endif
			var result = context.SelectQuery == null
				? $"{context.GetType().Name}{contextId}(<none>)"
				: FormattableString.Invariant($"{context.GetType().Name}{contextId}({context.SelectQuery.SourceID})");

			if (context is TableBuilder.TableContext tc)
			{
				result += FormattableString.Invariant($"(T: {tc.SqlTable.SourceID})");
			}

			if (context is ScopeContext scope)
			{
				result += FormattableString.Invariant($"(S:{scope.Context.SelectQuery.SourceID} -> {scope.UpTo.SelectQuery.SourceID})");
			}
			else if (context is SubQueryContext sc)
			{
				result += $"(SC)";
			}

			return result;
		}

		public static string GetPath(this IBuildContext context)
		{
			var str              = $"this({GetContextInfo(context)})";
			var alreadyProcessed = new HashSet<IBuildContext>();
			alreadyProcessed.Add(context);

			var current = (IBuildContext?)context;
			while (true)
			{
				current = current!.Parent;
				if (current == null)
					break;
				str = $"{GetContextInfo(context)} <- {str}";
				if (!alreadyProcessed.Add(context))
				{
					str = $"recursion: {str}";
					break;
				}
			}

			return str;
		}
	}
}
