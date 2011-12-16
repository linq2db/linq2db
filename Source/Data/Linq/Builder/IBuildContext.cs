using System;
using System.Linq.Expressions;

#if DEBUG
#pragma warning disable 3010
#endif

namespace LinqToDB.Data.Linq.Builder
{
	using SqlBuilder;

	public interface IBuildContext
	{
#if DEBUG
// ReSharper disable InconsistentNaming
		[CLSCompliant(false)]
		string _sqlQueryText { get; }
// ReSharper restore InconsistentNaming
#endif

		ExpressionBuilder  Builder    { get; }
		Expression         Expression { get; }
		SqlQuery           SqlQuery   { get; set; }
		IBuildContext      Parent     { get; set; }

		void               BuildQuery<T>       (Query<T> query, ParameterExpression queryParameter);
		Expression         BuildExpression     (Expression expression, int level);
		SqlInfo[]          ConvertToSql        (Expression expression, int level, ConvertFlags flags);
		SqlInfo[]          ConvertToIndex      (Expression expression, int level, ConvertFlags flags);
		IsExpressionResult IsExpression        (Expression expression, int level, RequestFor requestFlag);
		IBuildContext      GetContext          (Expression expression, int level, BuildInfo buildInfo);
		int                ConvertToParentIndex(int index, IBuildContext context);
		void               SetAlias            (string alias);
		ISqlExpression     GetSubQuery         (IBuildContext context);
	}
}
