using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	static class SequenceHelper
	{
		public static Expression PrepareBody(LambdaExpression lambda, params IBuildContext[] sequences)
		{
			var body = lambda.GetBody(sequences
				.Select((s, idx) => (Expression)new ContextRefExpression(lambda.Parameters[idx].Type, s)).ToArray());

			return body;
		}

		public static bool IsSameContext(Expression? expression, IBuildContext context)
		{
			return expression == null || expression is ContextRefExpression contextRef && contextRef.BuildContext == context;
		}

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
