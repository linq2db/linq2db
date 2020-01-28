#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;

#if DEBUG
// ReSharper disable InconsistentNaming

#pragma warning disable 3010
#endif

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

#if DEBUG
	internal static class BuildContextDebuggingHelper
	{
		static string GetContextInfo(IBuildContext context)
		{
			if (context.SelectQuery == null)
				return $"{context.GetType()}(<none>)";
			return $"{context.GetType()}({context.SelectQuery.SourceID.ToString()})";
		}

		public static string GetPath(this IBuildContext context)
		{
			var str = $"this({GetContextInfo(context)})";
			var alreadyProcessed = new HashSet<IBuildContext>();
			alreadyProcessed.Add(context);

			while (true)
			{
				context = context.Parent;
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
#endif

	interface IBuildContext
	{
#if DEBUG
		string _sqlQueryText { get; }
		string Path { get; }
#endif

		ExpressionBuilder  Builder     { get; }
		Expression         Expression  { get; }
		SelectQuery        SelectQuery { get; set; }
		SqlStatement       Statement   { get; set; }
		IBuildContext      Parent      { get; set; }

		void               BuildQuery<T>       (Query<T> query, ParameterExpression queryParameter);
		Expression         BuildExpression     (Expression expression, int level, bool enforceServerSide);
		SqlInfo[]          ConvertToSql        (Expression expression, int level, ConvertFlags flags);
		SqlInfo[]          ConvertToIndex      (Expression expression, int level, ConvertFlags flags);

		/// <summary>
		/// Returns information about expression according to <paramref name="requestFlag"/>. 
		/// </summary>
		/// <param name="expression">Analyzed expression.</param>
		/// <param name="level">Member level.</param>
		/// <param name="requestFlag">Which test or request has to be performed.</param>
		/// <returns><see cref="IsExpressionResult"/> instance.</returns>
		IsExpressionResult IsExpression        (Expression expression, int level, RequestFor requestFlag);

		IBuildContext      GetContext          (Expression expression, int level, BuildInfo buildInfo);
		int                ConvertToParentIndex(int index, IBuildContext context);
		void               SetAlias            (string alias);
		ISqlExpression     GetSubQuery         (IBuildContext context);

		SqlStatement       GetResultStatement();
	}
}
