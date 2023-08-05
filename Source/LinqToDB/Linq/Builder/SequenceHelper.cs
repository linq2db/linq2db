using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	static class SequenceHelper
	{
		public static Expression? CorrectExpression(Expression? expression, IBuildContext current, IBuildContext underlying)
		{
			if (expression != null)
			{
				var root = current.Builder.GetRootObject(expression);
				if (root is ContextRefExpression refExpression)
				{
					if (refExpression.BuildContext == current)
					{
						expression = expression.Replace(root, new ContextRefExpression(root.Type, underlying), EqualityComparer<Expression>.Default);
					}
				}
			}

			return expression;
		}

		public static TableBuilder.TableContext? GetTableContext(IBuildContext context)
		{
			var table = context as TableBuilder.TableContext;

			if (table != null)
				return table;

			if (context is LoadWithBuilder.LoadWithContext lwCtx)
				return lwCtx.TableContext;

			if (table == null)
			{
				var isTableResult = context.IsExpression(null, 0, RequestFor.Table);
				if (isTableResult.Result)
				{
					table = isTableResult.Context as TableBuilder.TableContext;
					if (table != null)
						return table;
				}
			}

			return null;
		}

		public static IBuildContext UnwrapSubqueryContext(IBuildContext context)
		{
			while (context is SubQueryContext sc)
			{
				context = sc.SubQuery;
			}

			return context;
		}

	}
}
