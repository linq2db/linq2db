using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Linq;
	using Expressions;

	using PN = ProviderName;

	public static partial class Sql
	{
		public enum AggregateModifier
		{
			None,
			Distinct,
			All,
		}

		public enum From
		{
			None,
			First,
			Last
		}

		public enum Nulls
		{
			None,
			Respect,
			Ignore
		}

		public enum NullsPosition
		{
			None,
			First,
			Last
		}
	}

	[PublicAPI]
	public static class AnalyticFunctions
	{
		/// <summary>
		/// Token name for analytic function. Used for resolving method chain.
		/// </summary>
		public const string FunctionToken  = "function";

		#region Call Builders

		sealed class OrderItemBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var nulls = builder.GetValue<Sql.NullsPosition>("nulls");
				switch (nulls)
				{
					case Sql.NullsPosition.None :
						break;
					case Sql.NullsPosition.First :
						builder.Expression += " NULLS FIRST";
						break;
					case Sql.NullsPosition.Last :
						builder.Expression += " NULLS LAST";
						break;
					default :
						throw new InvalidOperationException($"Unexpected nulls position: {nulls}");
				}
			}
		}

		sealed class ApplyAggregateModifier : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var modifier = builder.GetValue<Sql.AggregateModifier>("modifier");
				switch (modifier)
				{
					case Sql.AggregateModifier.None :
						break;
					case Sql.AggregateModifier.Distinct :
						builder.AddExpression("modifier", "DISTINCT");
						break;
					case Sql.AggregateModifier.All :
						builder.AddExpression("modifier", "ALL");
						break;
					default :
						throw new InvalidOperationException($"Unexpected aggregate modifier: {modifier}");
				}
			}
		}

		sealed class ApplyNullsModifier : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var nulls = builder.GetValue<Sql.Nulls>("nulls");
				var nullsStr = GetNullsStr(nulls);
				if (!string.IsNullOrEmpty(nullsStr))
					builder.AddExpression("modifier", nullsStr);
			}
		}

		static string GetNullsStr(Sql.Nulls nulls)
		{
			switch (nulls)
			{
				case Sql.Nulls.None   :
				case Sql.Nulls.Respect:
					// no need to add RESPECT NULLS, as it is default behavior and token itself supported only by Oracle, Informix and SQL Server 2022
					return string.Empty;
				case Sql.Nulls.Ignore :
					return "IGNORE NULLS";
				default :
					throw new InvalidOperationException($"Unexpected nulls: {nulls}");
			}
		}

		static string GetFromStr(Sql.From from)
		{
			switch (from)
			{
				case Sql.From.None :
					break;
				case Sql.From.First :
					return "FROM FIRST";
				case Sql.From.Last :
					return "FROM LAST";
				default :
					throw new InvalidOperationException($"Unexpected from: {from}");
			}
			return string.Empty;
		}

		sealed class ApplyFromAndNullsModifier : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var nulls = builder.GetValue<Sql.Nulls>("nulls");
				var from  = builder.GetValue<Sql.From>("from");

				var fromStr  = GetFromStr(from);
				var nullsStr = GetNullsStr(nulls);

				if (!string.IsNullOrEmpty(fromStr))
					builder.AddExpression("from", fromStr);
				if (!string.IsNullOrEmpty(nullsStr))
					builder.AddExpression("nulls", nullsStr);
			}
		}

		#endregion

		#region API Interfaces
		public interface IReadyToFunction<out TR>
		{
			[Sql.Extension("", ChainPrecedence = 0)]
			TR ToValue();
		}

		public interface IReadyToFunctionOrOverWithPartition<out TR> : IReadyToFunction<TR>
		{
			[Sql.Extension("OVER({query_partition_clause?})", TokenName = "over", IsWindowFunction = true)]
			IOverMayHavePartition<TR> Over();
		}

		public interface IOverWithPartitionNeeded<out TR>
		{
			[Sql.Extension("OVER({query_partition_clause?})", TokenName = "over", IsWindowFunction = true)]
			IOverMayHavePartition<TR> Over();
		}

		public interface INeedOrderByAndMaybeOverWithPartition<out TR>
		{
			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{order_expr}", TokenName = "order_item")]
			IOrderedAcceptOverReadyToFunction<TR> OrderBy<TKey>([ExprParameter("order_expr")] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{order_expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedAcceptOverReadyToFunction<TR> OrderBy<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{order_expr} DESC", TokenName = "order_item")]
			IOrderedAcceptOverReadyToFunction<TR> OrderByDesc<TKey>([ExprParameter("order_expr")] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{order_expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedAcceptOverReadyToFunction<TR> OrderByDesc<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);
		}

		public interface INeedSingleOrderByAndMaybeOverWithPartition<out TR>
		{
			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{order_expr}", TokenName = "order_item")]
			IReadyToFunctionOrOverWithPartition<TR> OrderBy<TKey>([ExprParameter("order_expr")] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{order_expr} DESC", TokenName = "order_item")]
			IReadyToFunctionOrOverWithPartition<TR> OrderByDesc<TKey>([ExprParameter("order_expr")] TKey expr);
		}

		public interface IOrderedAcceptOverReadyToFunction<out TR> : IReadyToFunctionOrOverWithPartition<TR>
		{
			[Sql.Extension("{order_expr}", TokenName = "order_item")]
			IOrderedAcceptOverReadyToFunction<TR> ThenBy<TKey>([ExprParameter("order_expr")] TKey expr);

			[Sql.Extension("{order_expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedAcceptOverReadyToFunction<TR> ThenBy<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);

			[Sql.Extension("{order_expr} DESC", TokenName = "order_item")]
			IOrderedAcceptOverReadyToFunction<TR> ThenByDesc<TKey>([ExprParameter("order_expr")] TKey expr);

			[Sql.Extension("{order_expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedAcceptOverReadyToFunction<TR> ThenByDesc<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);
		}

		public interface IOverMayHavePartition<out TR> : IReadyToFunction<TR>
		{
			[Sql.Extension("PARTITION BY {partition_expr, ', '}", TokenName = "query_partition_clause")]
			IReadyToFunction<TR> PartitionBy([ExprParameter("partition_expr")] params object?[] expressions);
		}

		public interface IPartitionedMayHaveOrder<out TR> : IReadyToFunction<TR>, INeedsOrderByOnly<TR>
		{
		}

		public interface IOverMayHavePartitionAndOrder<out TR> : IReadyToFunction<TR>, INeedsOrderByOnly<TR>
		{
			[Sql.Extension("PARTITION BY {partition_expr, ', '}", TokenName = "query_partition_clause")]
			IPartitionedMayHaveOrder<TR> PartitionBy([ExprParameter("partition_expr")] params object?[] expressions);
		}

		public interface IAnalyticFunction<out TR>
		{
			[Sql.Extension("{function} OVER({query_partition_clause?}{_}{order_by_clause?}{_}{windowing_clause?})",
				TokenName = "over", ChainPrecedence = 10, IsWindowFunction = true)]
			IReadyForFullAnalyticClause<TR> Over();
		}

		public interface IAnalyticFunctionWithoutWindow<out TR>
		{
			[Sql.Extension("{function} OVER({query_partition_clause?}{_}{order_by_clause?})", TokenName = "over", ChainPrecedence = 10, IsWindowFunction = true)]
			IOverMayHavePartitionAndOrder<TR> Over();
		}

		public interface IAggregateFunction<out TR> : IAnalyticFunction<TR> {}
		public interface IAggregateFunctionSelfContained<out TR> : IAggregateFunction<TR>, IReadyToFunction<TR> {}

		public interface IOrderedReadyToFunction<out TR> : IReadyToFunction<TR>
		{
			[Sql.Extension("{order_expr}", TokenName = "order_item")]
			IOrderedReadyToFunction<TR> ThenBy<TKey>([ExprParameter("order_expr")] TKey expr);

			[Sql.Extension("{order_expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToFunction<TR> ThenBy<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);

			[Sql.Extension("{order_expr} DESC", TokenName = "order_item")]
			IOrderedReadyToFunction<TR> ThenByDesc<TKey>([ExprParameter("order_expr")] TKey expr);

			[Sql.Extension("{order_expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToFunction<TR> ThenByDesc<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);
		}

		public interface INeedsWithinGroupWithOrderOnly<out TR>
		{
			[Sql.Extension("WITHIN GROUP ({order_by_clause})", TokenName = "within_group")]
			INeedsOrderByOnly<TR> WithinGroup { get; }
		}

		public interface INeedsWithinGroupWithOrderAndMaybePartition<out TR>
		{
			[Sql.Extension("WITHIN GROUP ({order_by_clause}){_}{over?}", TokenName = "within_group")]
			INeedOrderByAndMaybeOverWithPartition<TR> WithinGroup { get; }
		}

		public interface INeedsWithinGroupWithSingleOrderAndMaybePartition<out TR>
		{
			[Sql.Extension("WITHIN GROUP ({order_by_clause}){_}{over?}", TokenName = "within_group")]
			INeedSingleOrderByAndMaybeOverWithPartition<TR> WithinGroup { get; }
		}

		public interface INeedsOrderByOnly<out TR>
		{
			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{order_expr}", TokenName = "order_item")]
			IOrderedReadyToFunction<TR> OrderBy<TKey>([ExprParameter("order_expr")] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{order_expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToFunction<TR> OrderBy<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{order_expr} DESC", TokenName = "order_item")]
			IOrderedReadyToFunction<TR> OrderByDesc<TKey>([ExprParameter("order_expr")] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{order_expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToFunction<TR> OrderByDesc<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);
		}

		#region Full Support

		public interface IReadyForSortingWithWindow<out TR>
		{
			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{order_expr}", TokenName = "order_item")]
			IOrderedReadyToWindowing<TR> OrderBy<TKey>([ExprParameter("order_expr")] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{order_expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToWindowing<TR> OrderBy<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{order_expr} DESC", TokenName = "order_item")]
			IOrderedReadyToWindowing<TR> OrderByDesc<TKey>([ExprParameter("order_expr")] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{order_expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToWindowing<TR> OrderByDesc<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);
		}

		public interface IReadyForFullAnalyticClause<out TR> : IReadyToFunction<TR>, IReadyForSortingWithWindow<TR>
		{
			[Sql.Extension("PARTITION BY {partition_expr, ', '}", TokenName = "query_partition_clause")]
			IPartitionDefinedReadyForSortingWithWindow<TR> PartitionBy([ExprParameter("partition_expr")] params object?[] expressions);
		}

		public interface IPartitionDefinedReadyForSortingWithWindow<out TR> : IReadyForSortingWithWindow<TR>, IReadyToFunction<TR>
		{
		}

		public interface IOrderedReadyToWindowing<out TR> : IReadyToFunction<TR>
		{
			[Sql.Extension("ROWS {boundary_clause}", TokenName = "windowing_clause")]
			IBoundaryExpected<TR> Rows { get; }

			[Sql.Extension("RANGE {boundary_clause}", TokenName = "windowing_clause")]
			IBoundaryExpected<TR> Range { get; }

			[Sql.Extension("{order_expr}", TokenName = "order_item")]
			IOrderedReadyToWindowing<TR> ThenBy<TKey>([ExprParameter("order_expr")] TKey expr);

			[Sql.Extension("{order_expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToWindowing<TR> ThenBy<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);

			[Sql.Extension("{order_expr} DESC", TokenName = "order_item")]
			IOrderedReadyToWindowing<TR> ThenByDesc<TKey>([ExprParameter("order_expr")] TKey expr);

			[Sql.Extension("{order_expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToWindowing<TR> ThenByDesc<TKey>([ExprParameter("order_expr")] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);
		}

		public interface IBoundaryExpected<out TR>
		{
			[Sql.Extension("UNBOUNDED PRECEDING", TokenName = "boundary_clause")]
			IReadyToFunction<TR> UnboundedPreceding { get; }

			[Sql.Extension("CURRENT ROW", TokenName = "boundary_clause")]
			IReadyToFunction<TR> CurrentRow { get; }

			[Sql.Extension("{value_expr} PRECEDING", TokenName = "boundary_clause")]
			IReadyToFunction<TR> ValuePreceding<T>([ExprParameter("value_expr")] T value);

			[Sql.Extension("BETWEEN {start_boundary} AND {end_boundary}", TokenName = "boundary_clause")]
			IBetweenStartExpected<TR> Between { get; }
		}

		public interface IBetweenStartExpected<out TR>
		{
			[Sql.Extension("UNBOUNDED PRECEDING", TokenName = "start_boundary")]
			IAndExpected<TR> UnboundedPreceding { get; }

			[Sql.Extension("CURRENT ROW", TokenName = "start_boundary")]
			IAndExpected<TR> CurrentRow { get; }

			[Sql.Extension("{value_expr} PRECEDING", TokenName = "start_boundary")]
			IAndExpected<TR> ValuePreceding<T>([ExprParameter("value_expr")] T value);
		}

		public interface IAndExpected<out TR>
		{
			// TokenName used only for chain continuation
			[Sql.Extension("", TokenName = "and_connector")]
			ISecondBoundaryExpected<TR> And { get; }
		}

		public interface ISecondBoundaryExpected<out TR>
		{
			[Sql.Extension("UNBOUNDED FOLLOWING", TokenName = "end_boundary")]
			IReadyToFunction<TR> UnboundedFollowing { get; }

			[Sql.Extension("CURRENT ROW", TokenName = "end_boundary")]
			IReadyToFunction<TR> CurrentRow { get; }

			[Sql.Extension("{value_expr} PRECEDING", TokenName = "end_boundary")]
			IReadyToFunction<TR> ValuePreceding<T>([ExprParameter("value_expr")] T value);

			[Sql.Extension("{value_expr} FOLLOWING", TokenName = "end_boundary")]
			IReadyToFunction<TR> ValueFollowing<T>([ExprParameter("value_expr")] T value);
		}

		#endregion Full Support

		#endregion API Interfaces

		#region Extensions

		[Sql.Extension("{function} FILTER (WHERE {filter})", TokenName = FunctionToken, ChainPrecedence = 2, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Filter<T>(this IAnalyticFunctionWithoutWindow<T> func,
			[ExprParameter] bool filter)
		{
			throw new LinqException($"'{nameof(Filter)}' is server-side method.");
		}

		#endregion

		#region Analytic functions

		#region Average

		[Sql.Extension("AVG({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 0)]
		public static double Average<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Average)}' is server-side method.");
		}

		[Sql.Extension("AVG({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 0)]
		public static double Average<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = source.GetLinqToDBSource();

			return currentSource.Execute<double>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Average, source, expr, modifier),
					currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier)
				));
		}

		[Sql.Extension("AVG({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> Average<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(Average)}' is server-side method.");
		}

		[Sql.Extension("AVG({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> Average<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Average)}' is server-side method.");
		}

		#endregion Average

		#region Corr

		[Sql.Extension("CORR({expr1}, {expr2})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal? Corr<T>(this IEnumerable<T> source, [ExprParameter] Expression<Func<T, object?>> expr1, [ExprParameter] Expression<Func<T, object?>> expr2)
		{
			throw new LinqException($"'{nameof(Corr)}' is server-side method.");
		}

		[Sql.Extension("CORR({expr1}, {expr2})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal? Corr<TEntity>(
			           this IQueryable<TEntity>               source,
			[ExprParameter] Expression<Func<TEntity, object?>> expr1,
			[ExprParameter] Expression<Func<TEntity, object?>> expr2)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr1  == null) throw new ArgumentNullException(nameof(expr1));
			if (expr2  == null) throw new ArgumentNullException(nameof(expr2));

			var currentSource = source.GetLinqToDBSource();

			return currentSource.Execute<decimal?>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Corr, source, expr1, expr2),
					currentSource.Expression, Expression.Quote(expr1), Expression.Quote(expr2)
				));
		}

		[Sql.Extension("CORR({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> Corr<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(Corr)}' is server-side method.");
		}

		#endregion Corr

		#region Count

		[Sql.Extension("COUNT({expr})", IsAggregate = true, ChainPrecedence = 0, CanBeNull = false)]
		public static int CountExt<TEntity>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, object?> expr)
		{
			throw new LinqException($"'{nameof(CountExt)}' is server-side method.");
		}

		[Sql.Extension("COUNT({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true, ChainPrecedence = 0, CanBeNull = false)]
		public static int CountExt<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(CountExt)}' is server-side method.");
		}

		[Sql.Extension("COUNT({modifier?}{_}{expr})", IsAggregate = true, ChainPrecedence = 0, CanBeNull = false)]
		public static int CountExt<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = source.GetLinqToDBSource();

			return currentSource.Execute<int>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(CountExt, source, expr),
					currentSource.Expression, Expression.Quote(expr))
				);
		}

		[Sql.Extension("COUNT({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true, ChainPrecedence = 0, CanBeNull = false)]
		public static int CountExt<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] Sql.AggregateModifier modifier = Sql.AggregateModifier.None)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = source.GetLinqToDBSource();

			return currentSource.Execute<int>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(CountExt, source, expr, modifier),
					currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier)
				));
		}

		[Sql.Extension("COUNT(*)", TokenName = FunctionToken, IsAggregate = true, ChainPrecedence = 1, CanBeNull = false)]
		public static IAggregateFunctionSelfContained<int> Count(this Sql.ISqlExtension? ext)
		{
			throw new LinqException($"'{nameof(Count)}' is server-side method.");
		}

		[Sql.Extension("COUNT({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true, CanBeNull = false)]
		public static IAggregateFunctionSelfContained<int> Count<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(Count)}' is server-side method.");
		}

		[Sql.Extension("COUNT({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true, CanBeNull = false)]
		public static IAggregateFunctionSelfContained<int> Count(this Sql.ISqlExtension? ext, [ExprParameter] object? expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Count)}' is server-side method.");
		}

		#endregion

		#region LongCount

		[Sql.Extension("COUNT({expr})", IsAggregate = true, ChainPrecedence = 0)]
		public static long LongCountExt<TEntity>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, object?> expr)
		{
			throw new LinqException($"'{nameof(LongCountExt)}' is server-side method.");
		}

		[Sql.Extension("COUNT({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true, ChainPrecedence = 0)]
		public static long LongCountExt<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(LongCountExt)}' is server-side method.");
		}

		[Sql.Extension("COUNT({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true, ChainPrecedence = 0)]
		public static long LongCountExt<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] Sql.AggregateModifier modifier = Sql.AggregateModifier.None)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = source.GetLinqToDBSource();

			return currentSource.Execute<long>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(LongCountExt, source, expr, modifier),
					currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier)
				));
		}

		[Sql.Extension("COUNT(*)", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<long> LongCount(this Sql.ISqlExtension? ext)
		{
			throw new LinqException($"'{nameof(LongCount)}' is server-side method.");
		}

		[Sql.Extension("COUNT({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<long> LongCount<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(LongCount)}' is server-side method.");
		}

		[Sql.Extension("COUNT({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<long> LongCount(this Sql.ISqlExtension? ext, [ExprParameter] object? expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(LongCount)}' is server-side method.");
		}

		#endregion

		#region CovarPop

		[Sql.Extension("COVAR_POP({expr1}, {expr2})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal CovarPop<T>(this IEnumerable<T> source, [ExprParameter] Expression<Func<T, object?>> expr1, [ExprParameter] Expression<Func<T, object?>> expr2)
		{
			throw new LinqException($"'{nameof(CovarPop)}' is server-side method.");
		}

		[Sql.Extension("COVAR_POP({expr1}, {expr2})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal CovarPop<TEntity>(
			           this IQueryable<TEntity>               source,
			[ExprParameter] Expression<Func<TEntity, object?>> expr1,
			[ExprParameter] Expression<Func<TEntity, object?>> expr2)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr1  == null) throw new ArgumentNullException(nameof(expr1));
			if (expr2  == null) throw new ArgumentNullException(nameof(expr2));

			var currentSource = source.GetLinqToDBSource();

			return currentSource.Execute<decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(CovarPop, source, expr1, expr2),
					currentSource.Expression, Expression.Quote(expr1), Expression.Quote(expr2)
				));
		}

		[Sql.Extension("COVAR_POP({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> CovarPop<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr1, [ExprParameter]T expr2)
		{
			throw new LinqException($"'{nameof(CovarPop)}' is server-side method.");
		}

		#endregion CovarPop

		#region CovarSamp

		[Sql.Extension("COVAR_SAMP({expr1}, {expr2})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal? CovarSamp<T>(this IEnumerable<T> source, [ExprParameter] Expression<Func<T, object?>> expr1, [ExprParameter] Expression<Func<T, object?>> expr2)
		{
			throw new LinqException($"'{nameof(CovarSamp)}' is server-side method.");
		}

		[Sql.Extension("COVAR_SAMP({expr1}, {expr2})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal? CovarSamp<TEntity>(
			           this IQueryable<TEntity>                source,
			[ExprParameter] Expression<Func<TEntity, object?>> expr1,
			[ExprParameter] Expression<Func<TEntity, object?>> expr2)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr1  == null) throw new ArgumentNullException(nameof(expr1));
			if (expr2  == null) throw new ArgumentNullException(nameof(expr2));

			var currentSource = source.GetLinqToDBSource();

			return currentSource.Execute<decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(CovarSamp, source, expr1, expr2),
					currentSource.Expression, Expression.Quote(expr1), Expression.Quote(expr2)
				));
		}

		[Sql.Extension("COVAR_SAMP({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> CovarSamp<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr1, [ExprParameter]T expr2)
		{
			throw new LinqException($"'{nameof(CovarSamp)}' is server-side method.");
		}

		#endregion CovarSamp

		[Sql.Extension("CUME_DIST({expr, ', '}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static INeedsWithinGroupWithOrderOnly<TR> CumeDist<TR>(this Sql.ISqlExtension? ext, [ExprParameter] params object?[] expr)
		{
			throw new LinqException($"'{nameof(CumeDist)}' is server-side method.");
		}

		[Sql.Extension("CUME_DIST()", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<TR> CumeDist<TR>(this Sql.ISqlExtension? ext)
		{
			throw new LinqException($"'{nameof(CumeDist)}' is server-side method.");
		}

		[Sql.Extension("DENSE_RANK({expr1}, {expr2}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static INeedsWithinGroupWithOrderOnly<long> DenseRank(this Sql.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(DenseRank)}' is server-side method.");
		}

		[Sql.Extension("DENSE_RANK()", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<long> DenseRank(this Sql.ISqlExtension? ext)
		{
			throw new LinqException($"'{nameof(DenseRank)}' is server-side method.");
		}

		[Sql.Extension("FIRST_VALUE({expr}){_}{modifier?}", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsWindowFunction = true, Configuration = PN.SqlServer2022)]
		[Sql.Extension("FIRST_VALUE({expr}{_}{modifier?})", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> FirstValue<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.Nulls nulls)
		{
			throw new LinqException($"'{nameof(FirstValue)}' is server-side method.");
		}

		[Sql.Extension("LAG({expr}{_}{modifier?})", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lag<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.Nulls nulls)
		{
			throw new LinqException($"'{nameof(Lag)}' is server-side method.");
		}

		[Sql.Extension("LAG({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lag<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(Lag)}' is server-side method.");
		}

		[Sql.Extension("LAG({expr}, {offset})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lag<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [ExprParameter] int offset)
		{
			throw new LinqException($"'{nameof(Lag)}' is server-side method.");
		}

		[Sql.Extension("LAG({expr}, {offset}, {default})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lag<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [ExprParameter] int offset, [ExprParameter] T @default)
		{
			throw new LinqException($"'{nameof(Lag)}' is server-side method.");
		}

		[Sql.Extension("LAG({expr}{_}{modifier?}, {offset}, {default})", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lag<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.Nulls nulls, [ExprParameter] int offset, [ExprParameter] T @default)
		{
			throw new LinqException($"'{nameof(Lag)}' is server-side method.");
		}

		[Sql.Extension("LAST_VALUE({expr}){_}{modifier?}", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsWindowFunction = true, Configuration = PN.SqlServer2022)]
		[Sql.Extension("LAST_VALUE({expr}{_}{modifier?})", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> LastValue<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.Nulls nulls)
		{
			throw new LinqException($"'{nameof(LastValue)}' is server-side method.");
		}

		[Sql.Extension("LEAD({expr}{_}{modifier?})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lead<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.Nulls nulls)
		{
			throw new LinqException($"'{nameof(Lead)}' is server-side method.");
		}

		[Sql.Extension("LEAD({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lead<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(Lead)}' is server-side method.");
		}

		[Sql.Extension("LEAD({expr}, {offset})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lead<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [ExprParameter] int offset)
		{
			throw new LinqException($"'{nameof(Lead)}' is server-side method.");
		}

		[Sql.Extension("LEAD({expr}, {offset}, {default})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lead<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [ExprParameter] int offset, [ExprParameter] T @default)
		{
			throw new LinqException($"'{nameof(Lead)}' is server-side method.");
		}

		[Sql.Extension("LEAD({expr}{_}{modifier?}, {offset}, {default})", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lead<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.Nulls nulls, [ExprParameter] int offset, [ExprParameter] T @default)
		{
			throw new LinqException($"'{nameof(Lead)}' is server-side method.");
		}

		[Sql.Extension("LISTAGG({expr}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static INeedsWithinGroupWithOrderAndMaybePartition<string> ListAgg<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(ListAgg)}' is server-side method.");
		}

		[Sql.Extension("LISTAGG({expr}, {delimiter}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static INeedsWithinGroupWithOrderAndMaybePartition<string> ListAgg<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [ExprParameter] string delimiter)
		{
			throw new LinqException($"'{nameof(ListAgg)}' is server-side method.");
		}

		#region Max

		[Sql.Extension("MAX({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true, ChainPrecedence = 0)]
		public static TV Max<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Max)}' is server-side method.");
		}

		[Sql.Extension("MAX({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true, ChainPrecedence = 0)]
		public static TV Max<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = source.GetLinqToDBSource();

			return currentSource.Execute<TV>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Max, source, expr, modifier),
					currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier)
				));
		}

		[Sql.Extension("MAX({expr})", TokenName = FunctionToken, IsAggregate = true, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> Max<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(Max)}' is server-side method.");
		}

		[Sql.Extension("MAX({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true, TokenName = FunctionToken, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> Max<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Max)}' is server-side method.");
		}

		#endregion Max

		#region Median

		[Sql.Extension("MEDIAN({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static long Median<TEntity, T>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, T> expr)
		{
			throw new LinqException($"'{nameof(Median)}' is server-side method.");
		}

		[Sql.Extension("MEDIAN({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static long Median<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = source.GetLinqToDBSource();

			return currentSource.Execute<long>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Median, source, expr),
					currentSource.Expression, Expression.Quote(expr)
				));
		}

		[Sql.Extension("MEDIAN({expr}) {over}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IReadyToFunctionOrOverWithPartition<T> Median<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(Median)}' is server-side method.");
		}

		#endregion Median

		#region Min

		[Sql.Extension("MIN({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true, ChainPrecedence = 0)]
		public static TV Min<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Min)}' is server-side method.");
		}

		[Sql.Extension("MIN({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true, ChainPrecedence = 0)]
		public static TV Min<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = source.GetLinqToDBSource();

			return currentSource.Execute<TV>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Min, source, expr, modifier),
					currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier)));
		}

		[Sql.Extension("MIN({expr})", TokenName = FunctionToken, IsAggregate = true, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> Min<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(Min)}' is server-side method.");
		}

		[Sql.Extension("MIN({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true, TokenName = FunctionToken, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> Min<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Min)}' is server-side method.");
		}

		#endregion Min

		[Sql.Extension("NTH_VALUE({expr}, {n})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> NthValue<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [ExprParameter] long n)
		{
			throw new LinqException($"'{nameof(NthValue)}' is server-side method.");
		}

		[Sql.Extension("NTH_VALUE({expr}, {n}){_}{from?}{_}{nulls?}", TokenName = FunctionToken, BuilderType = typeof(ApplyFromAndNullsModifier), ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> NthValue<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [ExprParameter] long n, [SqlQueryDependent] Sql.From from, [SqlQueryDependent] Sql.Nulls nulls)
		{
			throw new LinqException($"'{nameof(NthValue)}' is server-side method.");
		}

		[Sql.Extension("NTILE({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> NTile<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(NTile)}' is server-side method.");
		}

		[Sql.Extension("PERCENT_RANK({expr, ', '}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static INeedsWithinGroupWithOrderOnly<T> PercentRank<T>(this Sql.ISqlExtension? ext, [ExprParameter] params object?[] expr)
		{
			throw new LinqException($"'{nameof(PercentRank)}' is server-side method.");
		}

		[Sql.Extension("PERCENT_RANK()", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<T> PercentRank<T>(this Sql.ISqlExtension? ext)
		{
			throw new LinqException($"'{nameof(PercentRank)}' is server-side method.");
		}

		[Sql.Extension("PERCENTILE_CONT({expr}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static INeedsWithinGroupWithSingleOrderAndMaybePartition<T> PercentileCont<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(PercentileCont)}' is server-side method.");
		}

		//TODO: check nulls support when ordering
		[Sql.Extension("PERCENTILE_DISC({expr}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static INeedsWithinGroupWithSingleOrderAndMaybePartition<T> PercentileDisc<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(PercentileDisc)}' is server-side method.");
		}

		[Sql.Extension("RANK({expr, ', '}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static INeedsWithinGroupWithOrderOnly<long> Rank(this Sql.ISqlExtension? ext, [ExprParameter] params object?[] expr)
		{
			throw new LinqException($"'{nameof(Rank)}' is server-side method.");
		}

		[Sql.Extension("RANK()", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAnalyticFunctionWithoutWindow<long> Rank(this Sql.ISqlExtension? ext)
		{
			throw new LinqException($"'{nameof(Rank)}' is server-side method.");
		}

		[Sql.Extension("RATIO_TO_REPORT({expr}) {over}", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IOverWithPartitionNeeded<TR> RatioToReport<TR>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(RatioToReport)}' is server-side method.");
		}

		#region REGR_ function

		[Sql.Extension("REGR_SLOPE({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> RegrSlope<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrSlope)}' is server-side method.");
		}

		[Sql.Extension("REGR_INTERCEPT({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> RegrIntercept<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrIntercept)}' is server-side method.");
		}

		[Sql.Extension("REGR_COUNT({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<long> RegrCount(this Sql.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrCount)}' is server-side method.");
		}

		[Sql.Extension("REGR_R2({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> RegrR2<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrR2)}' is server-side method.");
		}

		[Sql.Extension("REGR_AVGX({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> RegrAvgX<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrAvgX)}' is server-side method.");
		}

		[Sql.Extension("REGR_AVGY({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> RegrAvgY<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrAvgY)}' is server-side method.");
		}

		// ReSharper disable once InconsistentNaming
		[Sql.Extension("REGR_SXX({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> RegrSXX<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrSXX)}' is server-side method.");
		}

		// ReSharper disable once InconsistentNaming
		[Sql.Extension("REGR_SYY({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> RegrSYY<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrSYY)}' is server-side method.");
		}

		// ReSharper disable once InconsistentNaming
		[Sql.Extension("REGR_SXY({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> RegrSXY<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr1, [ExprParameter] object? expr2)
		{
			throw new LinqException($"'{nameof(RegrSXY)}' is server-side method.");
		}

		#endregion

		[Sql.Extension("ROW_NUMBER()", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true, CanBeNull = false)]
		public static IAnalyticFunctionWithoutWindow<long> RowNumber(this Sql.ISqlExtension? ext)
		{
			throw new LinqException($"'{nameof(RowNumber)}' is server-side method.");
		}

		#region StdDev

		[Sql.Extension(              "STDEV({expr})",  TokenName = FunctionToken, ChainPrecedence = 0, IsWindowFunction = true)]
		[Sql.Extension(PN.Oracle,    "STDDEV({expr})", TokenName = FunctionToken, ChainPrecedence = 0, IsWindowFunction = true)]
		public static double? StdDev<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new LinqException($"'{nameof(StdDev)}' is server-side method.");
		}

		[Sql.Extension(              "STDEV({modifier?}{_}{expr})",  TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 0, IsWindowFunction = true)]
		[Sql.Extension(PN.Oracle,    "STDDEV({modifier?}{_}{expr})", TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 0, IsWindowFunction = true)]
		public static double? StdDev<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(StdDev)}' is server-side method.");
		}

		[Sql.Extension(              "STDEV({modifier?}{_}{expr})",  TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 0, IsWindowFunction = true)]
		[Sql.Extension(PN.Oracle,    "STDDEV({modifier?}{_}{expr})", TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 0, IsWindowFunction = true)]
		public static double? StdDev<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] Sql.AggregateModifier modifier = Sql.AggregateModifier.None )
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = source.GetLinqToDBSource();

			return currentSource.Execute<double>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(StdDev, source, expr, modifier),
					currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier)));
		}

		[Sql.Extension(              "STDEV({expr})",  TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		[Sql.Extension(PN.Oracle,    "STDDEV({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> StdDev<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(StdDev)}' is server-side method.");
		}

		[Sql.Extension(              "STDEV({modifier?}{_}{expr})",  TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 1, IsWindowFunction = true)]
		[Sql.Extension(PN.Oracle,    "STDDEV({modifier?}{_}{expr})", TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> StdDev<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(StdDev)}' is server-side method.");
		}

		#endregion StdDev

		#region StdDevPop

		[Sql.Extension("STDDEV_POP({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal StdDevPop<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new LinqException($"'{nameof(StdDevPop)}' is server-side method.");
		}

		[Sql.Extension("STDDEV_POP({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal StdDevPop<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = source.GetLinqToDBSource();

			return currentSource.Execute<decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(StdDevPop, source, expr),
					currentSource.Expression, Expression.Quote(expr)));
		}

		[Sql.Extension("STDDEV_POP({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> StdDevPop<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(StdDevPop)}' is server-side method.");
		}

		#endregion StdDevPop

		#region StdDevSamp

		[Sql.Extension("STDDEV_SAMP({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal? StdDevSamp<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new LinqException($"'{nameof(StdDevSamp)}' is server-side method.");
		}

		[Sql.Extension("STDDEV_SAMP({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal? StdDevSamp<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = source.GetLinqToDBSource();

			return currentSource.Execute<decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(StdDevSamp, source, expr),
					currentSource.Expression, Expression.Quote(expr)));
		}

		[Sql.Extension("STDDEV_SAMP({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> StdDevSamp<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(StdDevSamp)}' is server-side method.");
		}

		#endregion StdDevSamp

		[Sql.Extension("SUM({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> Sum<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(Sum)}' is server-side method.");
		}

		[Sql.Extension("SUM({modifier?}{_}{expr})" , BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> Sum<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Sum)}' is server-side method.");
		}

		#region VarPop

		[Sql.Extension("VAR_POP({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal VarPop<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new LinqException($"'{nameof(VarPop)}' is server-side method.");
		}

		[Sql.Extension("VAR_POP({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal VarPop<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = source.GetLinqToDBSource();

			return currentSource.Execute<decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(VarPop, source, expr),
					currentSource.Expression, Expression.Quote(expr)));
		}

		[Sql.Extension("VAR_POP({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> VarPop<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(VarPop)}' is server-side method.");
		}

		#endregion VarPop

		#region VarSamp

		[Sql.Extension("VAR_SAMP({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal? VarSamp<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new LinqException($"'{nameof(VarSamp)}' is server-side method.");
		}

		[Sql.Extension("VAR_SAMP({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static decimal? VarSamp<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = source.GetLinqToDBSource();

			return currentSource.Execute<decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(VarSamp, source, expr),
					currentSource.Expression, Expression.Quote(expr)));
		}

		[Sql.Extension("VAR_SAMP({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> VarSamp<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(VarSamp)}' is server-side method.");
		}

		#endregion VarSamp

		#region Variance

		[Sql.Extension("VARIANCE({expr})", IsWindowFunction = true, ChainPrecedence = 0)]
		public static TV Variance<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new LinqException($"'{nameof(Variance)}' is server-side method.");
		}

		[Sql.Extension("VARIANCE({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsWindowFunction = true, ChainPrecedence = 0)]
		public static TV Variance<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Variance)}' is server-side method.");
		}

		[Sql.Extension("VARIANCE({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsWindowFunction = true, ChainPrecedence = 0)]
		public static TV Variance<TEntity, TV>(this IQueryable<TEntity> source, [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] Sql.AggregateModifier modifier = Sql.AggregateModifier.None)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = source.GetLinqToDBSource();

			return currentSource.Execute<TV>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Variance, source, expr, modifier),
					currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier)));
		}

		[Sql.Extension("VARIANCE({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> Variance<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr)
		{
			throw new LinqException($"'{nameof(Variance)}' is server-side method.");
		}

		[Sql.Extension("VARIANCE({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsWindowFunction = true)]
		public static IAggregateFunctionSelfContained<T> Variance<T>(this Sql.ISqlExtension? ext, [ExprParameter] object? expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new LinqException($"'{nameof(Variance)}' is server-side method.");
		}

		#endregion

		[Sql.Extension("{function} KEEP (DENSE_RANK FIRST {order_by_clause}){_}{over?}", ChainPrecedence = 10, IsWindowFunction = true)]
		public static INeedOrderByAndMaybeOverWithPartition<TR> KeepFirst<TR>(this IAggregateFunction<TR> ext)
		{
			throw new LinqException($"'{nameof(KeepFirst)}' is server-side method.");
		}

		[Sql.Extension("{function} KEEP (DENSE_RANK LAST {order_by_clause}){_}{over?}", ChainPrecedence = 10, IsWindowFunction = true)]
		public static INeedOrderByAndMaybeOverWithPartition<TR> KeepLast<TR>(this IAggregateFunction<TR> ext)
		{
			throw new LinqException($"'{nameof(KeepLast)}' is server-side method.");
		}

		#endregion Analytic functions
	}

}
