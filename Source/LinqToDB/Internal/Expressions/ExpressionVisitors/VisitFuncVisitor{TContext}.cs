using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	sealed class VisitFuncVisitor<TContext> : LegacyVisitorBase
		where TContext : notnull
	{
		internal static readonly ObjectPool<VisitFuncVisitor<TContext>> Pool = new(() => new VisitFuncVisitor<TContext>(), v => v.Cleanup(), 100);

		private TContext ?                        _context;
		private Func<TContext, Expression, bool>? _func;

		public override void Cleanup()
		{
			_context = default;
			_func    = null;
			base.Cleanup();
		}

		[return: NotNullIfNotNull(nameof(node))]
		public void VisitFunc(Expression? node, TContext context, Func<TContext, Expression, bool> func)
		{
			if (node == null)
				return;

			_context = context;
			_func    = func;

			Visit(node);
		}

		[return: NotNullIfNotNull(nameof(node))]
		public override Expression? Visit(Expression? node)
		{
			if (node == null || !_func!(_context!, node))
				return node;

			return base.Visit(node);
		}
	}
}
