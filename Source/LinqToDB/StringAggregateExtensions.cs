using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Linq;

using PN = LinqToDB.ProviderName;

namespace LinqToDB
{
	public static class StringAggregateExtensions
	{
		public static Sql.IAggregateFunctionOrdered<T, TR> OrderBy<T, TR, TKey>(
							this Sql.IAggregateFunctionNotOrdered<T, TR> aggregate,
			[ExprParameter]      Expression<Func<T, TKey>>               expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderBy, aggregate, expr),
					Expression.Constant(aggregate), Expression.Quote(expr)));

			return new Sql.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		public static Sql.IAggregateFunction<T, TR> OrderBy<T, TR>(
			this Sql.IAggregateFunctionNotOrdered<T, TR> aggregate)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderBy, aggregate), aggregate.Query.Expression
				));

			return new Sql.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		public static Sql.IAggregateFunctionOrdered<T, TR> OrderByDescending<T, TR, TKey>(
							this Sql.IAggregateFunctionNotOrdered<T, TR> aggregate,
			[ExprParameter]      Expression<Func<T, TKey>>               expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderByDescending, aggregate, expr),
					Expression.Constant(aggregate), Expression.Quote(expr)));

			return new Sql.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		public static Sql.IAggregateFunction<T, TR> OrderByDescending<T, TR>(
			this Sql.IAggregateFunctionNotOrdered<T, TR> aggregate)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(OrderByDescending, aggregate),
					Expression.Constant(aggregate)
				));

			return new Sql.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		public static Sql.IAggregateFunctionOrdered<T, TR> ThenBy<T, TR, TKey>(
							this Sql.IAggregateFunctionOrdered<T, TR> aggregate,
			[ExprParameter]      Expression<Func<T, TKey>>            expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(ThenBy, aggregate, expr),
					Expression.Constant(aggregate), Expression.Quote(expr)));

			return new Sql.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		public static Sql.IAggregateFunctionOrdered<T, TR> ThenByDescending<T, TR, TKey>(
							this Sql.IAggregateFunctionOrdered<T, TR> aggregate,
			[ExprParameter]      Expression<Func<T, TKey>>        expr)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
			if (expr      == null) throw new ArgumentNullException(nameof(expr));

			var query = aggregate.Query.Provider.CreateQuery<TR>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(ThenByDescending, aggregate, expr),
					Expression.Constant(aggregate), Expression.Quote(expr)));

			return new Sql.AggregateFunctionNotOrderedImpl<T, TR>(query);
		}

		public static TR ToValue<T, TR>(this Sql.IAggregateFunction<T, TR> aggregate)
		{
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

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
