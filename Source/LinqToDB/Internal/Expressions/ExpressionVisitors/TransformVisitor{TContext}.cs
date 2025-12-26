using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	sealed class TransformVisitor<TContext> : TransformVisitorBase
		where TContext : notnull
	{
		internal static readonly ObjectPool<TransformVisitor<TContext>> Pool = new(() => new TransformVisitor<TContext>(), v => v.Cleanup(), 100);

		private TContext?                               _context;
		private Func<TContext, Expression, Expression>? _func;

		public override void Cleanup()
		{
			_context = default;
			_func    = null;
			base.Cleanup();
		}

		public Expression Transform(Expression expression, TContext context, Func<TContext, Expression, Expression> func)
		{
			_context = context;
			_func    = func;

			return Visit(expression);
		}

		[return: NotNullIfNotNull(nameof(node))]
		public override Expression? Visit(Expression? node)
		{
			if (node == null)
				return null;

			var expr = _func!(_context!, node);
			if (expr != node)
				return expr;

			return base.Visit(node);
		}
	}
}
