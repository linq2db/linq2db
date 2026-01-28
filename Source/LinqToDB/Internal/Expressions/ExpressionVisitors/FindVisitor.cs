using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	sealed class FindVisitor : FindVisitorBase
	{
		internal static readonly ObjectPool<FindVisitor> Pool = new(() => new FindVisitor(), v => v.Cleanup(), 100);

		private Func<Expression, bool>? _func;
		private Expression?             _found;

		public override void Cleanup()
		{
			_func  = null;
			_found = null;
			base.Cleanup();
		}

		public Expression? Find(Expression? node, Func<Expression, bool> action)
		{
			if (node == null)
				return null;

			_func = action;

			Visit(node);

			return _found;
		}

		[return: NotNullIfNotNull(nameof(node))]
		public override Expression? Visit(Expression? node)
		{
			if (node == null || _found != null || _func!(node))
			{
				_found ??= node;
				return node;
			}

			return base.Visit(node);
		}
	}
}
