using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	sealed class VisitFuncVisitor : LegacyVisitorBase
	{
		internal static readonly ObjectPool<VisitFuncVisitor> Pool = new(() => new VisitFuncVisitor(), v => v.Cleanup(), 100);

		private Func<Expression, bool>? _func;

		public override void Cleanup()
		{
			_func = null;
			base.Cleanup();
		}

		[return: NotNullIfNotNull(nameof(node))]
		public void VisitFunc(Expression? node, Func<Expression, bool> func)
		{
			if (node == null)
				return;

			_func = func;

			Visit(node);
		}

		[return: NotNullIfNotNull(nameof(node))]
		public override Expression? Visit(Expression? node)
		{
			if (node == null || !_func!(node))
				return node;

			return base.Visit(node);
		}
	}
}
