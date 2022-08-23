using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToDB.Expressions;

#if DEBUG
// ReSharper disable InconsistentNaming

#endif

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	internal static class BuildContextDebuggingHelper
	{
		public static string GetContextInfo(IBuildContext context)
		{
#if DEBUG
			var contextId = $"_{context.ContextId}";
#else
			var contextId = string.Empty;
#endif
			var result = context.SelectQuery == null
				? $"{context.GetType().Name}{contextId}(<none>)"
				: $"{context.GetType().Name}{contextId}({context.SelectQuery.SourceID})";

			if (context is TableBuilder.TableContext tc)
			{
				result += $"(T: {tc.SqlTable.SourceID})";
			}
			else if (context is SubQueryContext sc)
			{
				result += $"(SC)";
			}

			return result;
		}

		public static string GetPath(this IBuildContext context)
		{
			var str = $"this({GetContextInfo(context)})";
			var alreadyProcessed = new HashSet<IBuildContext>();
			alreadyProcessed.Add(context);

			while (true)
			{
				context = context.Parent!;
				if (context == null)
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

	internal interface IBuildContext
	{
#if DEBUG
		string? SqlQueryText  { get; }
		string  Path          { get; }
		int     ContextId     { get; }
#endif

		ExpressionBuilder  Builder     { get; }
		Expression?        Expression  { get; }
		SelectQuery        SelectQuery { get; set; }
		SqlStatement?      Statement   { get; set; }
		IBuildContext?     Parent      { get; set; }

		void               BuildQuery<T>       (Query<T> query, ParameterExpression queryParameter);
		Expression         BuildExpression     (Expression? expression, int level, bool enforceServerSide);
		SqlInfo[]          ConvertToSql        (Expression? expression, int level, ConvertFlags flags);
		SqlInfo[]          ConvertToIndex      (Expression? expression, int level, ConvertFlags flags);


		Expression    MakeExpression(Expression path, ProjectFlags flags);
		IBuildContext Clone(CloningContext      context);
		void          SetRunQuery<T>(Query<T>   query, Expression expr);

		/// <summary>
		/// Returns information about expression according to <paramref name="requestFlag"/>.
		/// </summary>
		/// <param name="expression">Analyzed expression.</param>
		/// <param name="level">Member level.</param>
		/// <param name="requestFlag">Which test or request has to be performed.</param>
		/// <returns><see cref="IsExpressionResult"/> instance.</returns>
		IsExpressionResult IsExpression        (Expression? expression, int level, RequestFor requestFlag);

		IBuildContext?     GetContext          (Expression? expression, int level, BuildInfo buildInfo);
		int                ConvertToParentIndex(int index, IBuildContext context);
		void               SetAlias            (string? alias);
		ISqlExpression?    GetSubQuery         (IBuildContext context);

		SqlStatement       GetResultStatement();
		void               CompleteColumns();
	}
}
