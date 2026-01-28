using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	sealed class TransformVisitor : TransformVisitorBase
	{
		internal static readonly ObjectPool<TransformVisitor> Pool = new(() => new TransformVisitor(), v => v.Cleanup(), 100);

		private Func<Expression, Expression>? _func;

		public override void Cleanup()
		{
			_func = null;
			base.Cleanup();
		}

		public Expression Transform(Expression expression, Func<Expression, Expression> func)
		{
			_func = func;

			return Visit(expression);
		}

		[return: NotNullIfNotNull(nameof(node))]
		public override Expression? Visit(Expression? node)
		{
			if (node == null)
				return null;

			var expr = _func!(node);
			if (expr != node)
				return expr;

			return base.Visit(node);
		}
	}
}
