using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Linq;

using PN = LinqToDB.ProviderName;

namespace LinqToDB
{
	public static class StringAggregateExtensions
	{
		[Sql.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering", ChainPrecedence = 2)]
		[Sql.Extension("ORDER BY {order_item, ', '}",      TokenName = "order_by_clause")]
		[Sql.Extension("{expr}",                           TokenName = "order_item")]
		public static Sql.IAggregateFunctionOrdered<T, TR> OrderBy<T, TR, TKey>(
							this Sql.IAggregateFunctionNotOrdered<T, TR> aggregate,
			[ExprParameter]      Expression<Func<T, TKey>>               expr)
		{
			ArgumentNullException.ThrowIfNull(aggregate);
			ArgumentNullException.ThrowIfNull(expr);

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderBy, aggregate, expr),
					Expression.Constant(aggregate), Expression.Quote(expr)));

			return new Sql.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		[Sql.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering", ChainPrecedence = 2)]
		[Sql.Extension("ORDER BY {order_item, ', '}",      TokenName = "order_by_clause")]
		[Sql.Extension("{aggregate}",                      TokenName = "order_item")]
		public static Sql.IAggregateFunction<T, TR> OrderBy<T, TR>(
			this Sql.IAggregateFunctionNotOrdered<T, TR> aggregate)
		{
			ArgumentNullException.ThrowIfNull(aggregate);

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderBy, aggregate), aggregate.Query.Expression
				));

			return new Sql.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		[Sql.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering", ChainPrecedence = 2)]
		[Sql.Extension("ORDER BY {order_item, ', '}",      TokenName = "order_by_clause")]
		[Sql.Extension("{expr} DESC",                      TokenName = "order_item")]
		public static Sql.IAggregateFunctionOrdered<T, TR> OrderByDescending<T, TR, TKey>(
							this Sql.IAggregateFunctionNotOrdered<T, TR> aggregate,
			[ExprParameter]      Expression<Func<T, TKey>>               expr)
		{
			ArgumentNullException.ThrowIfNull(aggregate);
			ArgumentNullException.ThrowIfNull(expr);

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderByDescending, aggregate, expr),
					Expression.Constant(aggregate), Expression.Quote(expr)));

			return new Sql.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		[Sql.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "aggregation_ordering", ChainPrecedence = 2)]
		[Sql.Extension("ORDER BY {order_item, ', '}",      TokenName = "order_by_clause")]
		[Sql.Extension("{aggregate} DESC",                 TokenName = "order_item")]
		public static Sql.IAggregateFunction<T, TR> OrderByDescending<T, TR>(
			this Sql.IAggregateFunctionNotOrdered<T, TR> aggregate)
		{
			ArgumentNullException.ThrowIfNull(aggregate);

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderByDescending, aggregate),
					Expression.Constant(aggregate)
				));

			return new Sql.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		[Sql.Extension("{expr}", TokenName = "order_item")]
		public static Sql.IAggregateFunctionOrdered<T, TR> ThenBy<T, TR, TKey>(
							this Sql.IAggregateFunctionOrdered<T, TR> aggregate,
			[ExprParameter]      Expression<Func<T, TKey>>            expr)
		{
			ArgumentNullException.ThrowIfNull(aggregate);
			ArgumentNullException.ThrowIfNull(expr);

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(ThenBy, aggregate, expr),
					Expression.Constant(aggregate), Expression.Quote(expr)));

			return new Sql.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		[Sql.Extension("{expr} DESC", TokenName = "order_item")]
		public static Sql.IAggregateFunctionOrdered<T, TR> ThenByDescending<T, TR, TKey>(
							this Sql.IAggregateFunctionOrdered<T, TR> aggregate,
			[ExprParameter]      Expression<Func<T, TKey>>        expr)
		{
			ArgumentNullException.ThrowIfNull(aggregate);
			ArgumentNullException.ThrowIfNull(expr);

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(ThenByDescending, aggregate, expr),
					Expression.Constant(aggregate), Expression.Quote(expr)));

			return new Sql.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		// For Oracle we always define at least one ordering by rownum. If ordering defined explicitly, this definition will be replaced.
		[Sql.Extension(PN.Oracle,       "WITHIN GROUP (ORDER BY ROWNUM)", TokenName = "aggregation_ordering", ChainPrecedence = 0, IsAggregate = true)]
		[Sql.Extension(PN.OracleNative, "WITHIN GROUP (ORDER BY ROWNUM)", TokenName = "aggregation_ordering", ChainPrecedence = 0, IsAggregate = true)]
		[Sql.Extension(                  "",                                                                  ChainPrecedence = 0, IsAggregate = true)]
		public static TR ToValue<T, TR>(this Sql.IAggregateFunction<T, TR> aggregate)
		{
			ArgumentNullException.ThrowIfNull(aggregate);

			Expression aggregateExpr;

			if (aggregate is Sql.IQueryableContainer)
			{
				aggregateExpr = aggregate.Query.Expression;
			}
			else
			{
				aggregateExpr = Expression.Constant(aggregate);
			}

			return aggregate.Query.Provider.Execute<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(ToValue, aggregate),
					aggregateExpr));
		}
	}
}
