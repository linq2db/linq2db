using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	sealed class FindVisitor<TContext> : FindVisitorBase
		where TContext : notnull
	{
		internal static readonly ObjectPool<FindVisitor<TContext>> Pool = new(() => new FindVisitor<TContext>(), v => v.Cleanup(), 100);

		private TContext?                         _context;
		private Func<TContext, Expression, bool>? _func;
		private Expression?                       _found;

		public override void Cleanup()
		{
			_context = default;
			_func    = null;
			_found   = null;
			base.Cleanup();
		}

		public Expression? Find(Expression? node, TContext context, Func<TContext, Expression, bool> action)
		{
			if (node == null)
				return null;

			_context = context;
			_func    = action;

			Visit(node);

			return _found;
		}

		[return: NotNullIfNotNull(nameof(node))]
		public override Expression? Visit(Expression? node)
		{
			if (node == null || _found != null || _func!(_context!, node))
			{
				_found ??= node;
				return node;
			}

			return base.Visit(node);
		}
	}
}
