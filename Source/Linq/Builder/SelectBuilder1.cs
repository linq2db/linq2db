using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal class SelectBuilder1 : ExpressionBuilderBase
	{
		public SelectBuilder1(MethodCallExpression expression)
			: base(expression)
		{
			if (expression.Arguments.Count == 2)
			{
				var expr = expression.Arguments[1];

				while (expr.NodeType == ExpressionType.Quote)
					expr = ((UnaryExpression)expr).Operand;

				var l = (LambdaExpression)expr;

				_skip = l.Parameters.Count == 1 && l.Body == l.Parameters[0];
			}
		}

		readonly bool _skip;

		public override SqlBuilderBase GetSqlBuilder()
		{
			if (_skip)
				return Prev.GetSqlBuilder();

			throw new NotImplementedException();
		}

		public override Expression BuildQuery<T>()
		{
			if (_skip)
				return Prev.BuildQuery<T>();

			throw new NotImplementedException();
		}
	}
}