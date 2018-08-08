using System;
using System.Linq.Expressions;

#if DEBUG
// ReSharper disable InconsistentNaming

#pragma warning disable 3010
#endif

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	interface IBuildContext
	{
#if DEBUG
		string _sqlQueryText { get; }
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
		/// <param name="expression">Analysed expression.</param>
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
