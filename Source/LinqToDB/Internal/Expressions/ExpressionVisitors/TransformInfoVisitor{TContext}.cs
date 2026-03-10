using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	sealed class TransformInfoVisitor<TContext> : TransformVisitorsBase
		where TContext : notnull
	{
		internal static readonly ObjectPool<TransformInfoVisitor<TContext>> Pool = new(() => new TransformInfoVisitor<TContext>(), v => v.Cleanup(), 100);

		private TContext?                                  _context;
		private Func<TContext, Expression, TransformInfo>? _func;

		public override void Cleanup()
		{
			_context = default;
			_func    = null;
			base.Cleanup();
		}

		public Expression Transform(Expression expression, TContext context, Func<TContext, Expression, TransformInfo> func)
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

			do
			{
				var ti = _func!(_context!, node);
				if (ti.Stop || (!ti.Continue && ti.Expression != node))
					return ti.Expression;
				if (node == ti.Expression)
					break;
				node = ti.Expression;
			} while (true);

			return base.Visit(node);
		}
	}
}
