using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	class SelectBuilder1 : ExpressionBuilderBase
	{
		public static QueryExpression Translate(QueryExpression qe, MethodCallExpression expression)
		{
			if (expression.Arguments.Count == 2)
			{
				var expr = expression.Arguments[1];

				while (expr.NodeType == ExpressionType.Quote)
					expr = ((UnaryExpression)expr).Operand;

				var l = (LambdaExpression)expr;

				if (l.Parameters.Count == 1 && l.Body == l.Parameters[0])
					return qe;
			}

			return qe.AddBuilder(new SelectBuilder1(expression));
		}

		SelectBuilder1(Expression expression) : base(expression)
		{
		}

		public override SqlBuilderBase GetSqlBuilder()
		{
			throw new NotImplementedException();
		}

		public override Expression BuildQuery<T>()
		{
			throw new NotImplementedException();
		}

		public override void BuildQuery<T>(Query<T> query)
		{
			throw new NotImplementedException();
		}
	}
}