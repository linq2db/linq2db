using System;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Expressions
{
	/// <summary>
	/// Marks a parameter whose value shapes the query when it is known before the query runs, and that may also read the
	/// query's own rows: <c>PivotCells.Get&lt;T&gt;(name)</c> and the <c>PivotCells</c> indexer.
	/// </summary>
	/// <remarks>
	/// A row-dependent argument has no value until the query runs, so it is compared as part of the expression tree
	/// rather than evaluated.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Parameter)]
	sealed class SqlQueryDependentValueAttribute : SqlQueryDependentAttribute
	{
		public override bool ExpressionsEqual<TContext>(TContext context, Expression expr1, Expression expr2,
			Func<TContext, Expression, Expression, bool> comparer)
		{
			return ReadsParameter(expr1) || ReadsParameter(expr2)
				? comparer(context, expr1, expr2)
				: base.ExpressionsEqual(context, expr1, expr2, comparer);
		}

		static bool ReadsParameter(Expression expression)
			=> expression.Find(static e => e is ParameterExpression) != null;
	}
}
