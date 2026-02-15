using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	sealed class TransformInfoVisitor : TransformVisitorsBase
	{
		internal static readonly ObjectPool<TransformInfoVisitor> Pool = new(() => new TransformInfoVisitor(), v => v.Cleanup(), 100);

		private Func<Expression, TransformInfo>? _func;

		public override void Cleanup()
		{
			_func = null;
			base.Cleanup();
		}

		public Expression Transform(Expression expression, Func<Expression, TransformInfo> func)
		{
			_func = func;

			return Visit(expression);
		}

		[return: NotNullIfNotNull(nameof(node))]
		public override Expression? Visit(Expression? node)
		{
			if (node == null)
				return null;

			do
			{
				var ti = _func!(node);
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
