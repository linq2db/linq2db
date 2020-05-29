﻿using System.Collections.Generic;
using System.Linq.Expressions;

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
			var result = context.SelectQuery == null
				? $"{context.GetType().Name}(<none>)"
				: $"{context.GetType().Name}({context.SelectQuery.SourceID})";

			if (context is TableBuilder.TableContext tc)
			{
				result += $"(T: {tc.SqlTable.SourceID})";
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

	interface IBuildContext
	{
#if DEBUG
		string? _sqlQueryText { get; }
		string   Path         { get; }
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
		void               SetAlias            (string alias);
		ISqlExpression?    GetSubQuery         (IBuildContext context);

		SqlStatement       GetResultStatement();
	}
}
