using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Used for controlling query caching of custom SQL Functions.
	/// Parameter with this attribute will be evaluated on client side before generating SQL.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Deprecated; scheduled for removal in v7.
	/// </para>
	/// <para>
	/// The per-parameter expression compile inside <see cref="ExpressionsEqual{TContext}"/> (and the
	/// base <see cref="SqlQueryDependentAttribute.ExpressionsEqual{TContext}"/>) is unsafe whenever
	/// the parameter expression captures outer-scope transparent identifiers, e.g. inside multi-level
	/// eager-loaded projections — see <see href="https://github.com/linq2db/linq2db/issues/5154"/>.
	/// The default structural cache-compare path is sufficient for the cases this attribute was
	/// intended to cover.
	/// </para>
	/// </remarks>
	[AttributeUsage(AttributeTargets.Parameter)]
	[Obsolete("Scheduled for removal in v7. The default structural cache-compare path covers the cases this attribute was intended to handle; see https://github.com/linq2db/linq2db/issues/5154.")]
	public class SqlQueryDependentParamsAttribute : SqlQueryDependentAttribute
	{
		public override bool ExpressionsEqual<TContext>(TContext context, Expression expr1, Expression expr2, Func<TContext, Expression, Expression, bool> comparer)
		{
			return (expr1, expr2) switch
			{
				(ConstantExpression c1, ConstantExpression c2) => comparer(context, c1, c2),
				_ => base.ExpressionsEqual(context, expr1, expr2, comparer),
			};
		}

		public override IEnumerable<Expression> SplitExpression(Expression expression)
		{
			var val = expression.EvaluateExpression();

			// as option, we can throw when applied to non-object[] parameter to prevent incorrect use
			if (val is object[] values)
				foreach (var value in values)
					yield return Expression.Constant(value);
			else
				yield return expression;
		}
	}
}
