using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	sealed class VisitActionVisitor<TContext> : LegacyVisitorBase
		where TContext : notnull
	{
		internal static readonly ObjectPool<VisitActionVisitor<TContext>> Pool = new(() => new VisitActionVisitor<TContext>(), v => v.Cleanup(), 100);

		private TContext?                     _context;
		private Action<TContext, Expression>? _action;

		public override void Cleanup()
		{
			_context = default;
			_action  = null;
			base.Cleanup();
		}

		[return: NotNullIfNotNull(nameof(node))]
		public void VisitAction(Expression? node, TContext context, Action<TContext, Expression> action)
		{
			if (node == null)
				return;

			_context = context;
			_action  = action;

			Visit(node);
		}

		[return: NotNullIfNotNull(nameof(node))]
		public override Expression? Visit(Expression? node)
		{
			if (node == null)
				return null;

			node = base.Visit(node);

			_action!(_context!, node);

			return node;
		}
	}
}
