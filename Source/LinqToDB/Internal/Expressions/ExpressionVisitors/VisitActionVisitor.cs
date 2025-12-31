using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	sealed class VisitActionVisitor : LegacyVisitorBase
	{
		internal static readonly ObjectPool<VisitActionVisitor> Pool = new(() => new VisitActionVisitor(), v => v.Cleanup(), 100);

		private Action<Expression>? _action;

		public override void Cleanup()
		{
			_action = null;
			base.Cleanup();
		}

		[return: NotNullIfNotNull(nameof(node))]
		public void VisitAction(Expression? node, Action<Expression> action)
		{
			if (node == null)
				return;

			_action = action;

			Visit(node);
		}

		[return: NotNullIfNotNull(nameof(node))]
		public override Expression? Visit(Expression? node)
		{
			if (node == null)
				return null;

			node = base.Visit(node);

			_action!(node);

			return node;
		}
	}
}
