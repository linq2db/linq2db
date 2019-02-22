using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Linq;
	using Expressions;

	using PN = LinqToDB.ProviderName;

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
		const string FunctionToken  = "function";

		#region Call Builders

		class OrderItemBuilder: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var nulls = builder.GetValue<Sql.NullsPosition>("nulls");
				switch (nulls)
				{
					case Sql.NullsPosition.None :
						break;
					case Sql.NullsPosition.First :
						builder.Expression = builder.Expression + " NULLS FIRST";
						break;
					case Sql.NullsPosition.Last :
						builder.Expression = builder.Expression + " NULLS LAST";
						break;
					default :
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		class ApplyAggregateModifier: Sql.IExtensionCallBuilder
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
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		class ApplyNullsModifier: Sql.IExtensionCallBuilder
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
				case Sql.Nulls.None :
					break;
				case Sql.Nulls.Respect :
					return "RESPECT NULLS";
				case Sql.Nulls.Ignore :
					return "IGNORE NULLS";
				default :
					throw new ArgumentOutOfRangeException();
			}
			return string.Empty;
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
					throw new ArgumentOutOfRangeException();
			}
			return string.Empty;
		}

		class ApplyFromAndNullsModifier: Sql.IExtensionCallBuilder
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
			[Sql.Extension("")]
			TR ToValue();
		}

		public interface IReadyToFunctionOrOverWithPartition<out TR> : IReadyToFunction<TR>
		{
			[Sql.Extension("OVER({query_partition_clause?})", TokenName = "over")]
			IOverMayHavePartition<TR> Over();
		}

		public interface IOverWithPartitionNeeded<out TR>
		{
			[Sql.Extension("OVER({query_partition_clause?})", TokenName = "over")]
			IOverMayHavePartition<TR> Over();
		}

		public interface INeedOrderByAndMaybeOverWithPartition<out TR>
		{
			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{expr}", TokenName = "order_item")]
			IOrderedAcceptOverReadyToFunction<TR> OrderBy<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedAcceptOverReadyToFunction<TR> OrderBy<TKey>([ExprParameter] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{expr} DESC", TokenName = "order_item")]
			IOrderedAcceptOverReadyToFunction<TR> OrderByDesc<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedAcceptOverReadyToFunction<TR> OrderByDesc<TKey>([ExprParameter] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);
		}

		public interface INeedSingleOrderByAndMaybeOverWithPartition<out TR>
		{
			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{expr}", TokenName = "order_item")]
			IReadyToFunctionOrOverWithPartition<TR> OrderBy<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{expr} DESC", TokenName = "order_item")]
			IReadyToFunctionOrOverWithPartition<TR> OrderByDesc<TKey>([ExprParameter] TKey expr);
		}

		public interface IOrderedAcceptOverReadyToFunction<out TR> : IReadyToFunctionOrOverWithPartition<TR>
		{
			[Sql.Extension("{expr}", TokenName = "order_item")]
			IOrderedAcceptOverReadyToFunction<TR> ThenBy<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("{expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedAcceptOverReadyToFunction<TR> ThenBy<TKey>([ExprParameter] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);

			[Sql.Extension("{expr} DESC", TokenName = "order_item")]
			IOrderedAcceptOverReadyToFunction<TR> ThenByDesc<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("{expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedAcceptOverReadyToFunction<TR> ThenByDesc<TKey>([ExprParameter] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);
		}

		public interface IOverMayHavePartition<out TR> : IReadyToFunction<TR>
		{
			[Sql.Extension("PARTITION BY {expr, ', '}", TokenName = "query_partition_clause")]
			IReadyToFunction<TR> PartitionBy([ExprParameter("expr")] params object[] expressions);
		}

		public interface IPartitionedMayHaveOrder<out TR> : IReadyToFunction<TR>, INeedsOrderByOnly<TR>
		{
		}

		public interface IOverMayHavePartitionAndOrder<out TR> : IReadyToFunction<TR>, INeedsOrderByOnly<TR>
		{
			[Sql.Extension("PARTITION BY {expr, ', '}", TokenName = "query_partition_clause")]
			IPartitionedMayHaveOrder<TR> PartitionBy([ExprParameter("expr")] params object[] expressions);
		}

		public interface IAnalyticFunction<out TR>
		{
			[Sql.Extension("{function} OVER({query_partition_clause?}{_}{order_by_clause?}{_}{windowing_clause?})",
				TokenName = "over", ChainPrecedence = 10, IsAggregate = true)]
			IReadyForFullAnalyticClause<TR> Over();
		}

		public interface IAnalyticFunctionWithoutWindow<out TR>
		{
			[Sql.Extension("{function} OVER({query_partition_clause?}{_}{order_by_clause?})", TokenName = "over", ChainPrecedence = 10, IsAggregate = true)]
			IOverMayHavePartitionAndOrder<TR> Over();
		}

		public interface IAggregateFunction<out TR> : IAnalyticFunction<TR> {}
		public interface IAggregateFunctionSelfContained<out TR> : IAggregateFunction<TR>, IReadyToFunction<TR> {}

		public interface IOrderedReadyToFunction<out TR> : IReadyToFunction<TR>
		{
			[Sql.Extension("{expr}", TokenName = "order_item")]
			IOrderedReadyToFunction<TR> ThenBy<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("{expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToFunction<TR> ThenBy<TKey>([ExprParameter] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);

			[Sql.Extension("{expr} DESC", TokenName = "order_item")]
			IOrderedReadyToFunction<TR> ThenByDesc<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("{expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToFunction<TR> ThenByDesc<TKey>([ExprParameter] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);
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
			[Sql.Extension("{expr}", TokenName = "order_item")]
			IOrderedReadyToFunction<TR> OrderBy<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToFunction<TR> OrderBy<TKey>([ExprParameter] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{expr} DESC", TokenName = "order_item")]
			IOrderedReadyToFunction<TR> OrderByDesc<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToFunction<TR> OrderByDesc<TKey>([ExprParameter] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);
		}

		#region Full Support

		public interface IReadyForSortingWithWindow<out TR>
		{
			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{expr}", TokenName = "order_item")]
			IOrderedReadyToWindowing<TR> OrderBy<TKey>([ExprParameter("expr")] TKey keySelector);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToWindowing<TR> OrderBy<TKey>([ExprParameter("expr")] TKey keySelector, [SqlQueryDependent] Sql.NullsPosition nulls);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{expr} DESC", TokenName = "order_item")]
			IOrderedReadyToWindowing<TR> OrderByDesc<TKey>([ExprParameter("expr")] TKey keySelector);

			[Sql.Extension("ORDER BY {order_item, ', '}", TokenName = "order_by_clause")]
			[Sql.Extension("{expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToWindowing<TR> OrderByDesc<TKey>([ExprParameter("expr")] TKey keySelector, [SqlQueryDependent] Sql.NullsPosition nulls);
		}

		public interface IReadyForFullAnalyticClause<out TR> : IReadyToFunction<TR>, IReadyForSortingWithWindow<TR>
		{
			[Sql.Extension("PARTITION BY {expr, ', '}", TokenName = "query_partition_clause")]
			IPartitionDefinedReadyForSortingWithWindow<TR> PartitionBy([ExprParameter("expr")] params object[] expressions);
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

			[Sql.Extension("{expr}", TokenName = "order_item")]
			IOrderedReadyToWindowing<TR> ThenBy<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("{expr}", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToWindowing<TR> ThenBy<TKey>([ExprParameter] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);

			[Sql.Extension("{expr} DESC", TokenName = "order_item")]
			IOrderedReadyToWindowing<TR> ThenByDesc<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("{expr} DESC", TokenName = "order_item", BuilderType = typeof(OrderItemBuilder))]
			IOrderedReadyToWindowing<TR> ThenByDesc<TKey>([ExprParameter] TKey expr, [SqlQueryDependent] Sql.NullsPosition nulls);
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
			[Sql.Extension("")]
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

		#region Analytic functions

		#region Average

		[Sql.Extension("AVG({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true)]
		public static double Average<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("AVG({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true)]
		public static double Average<TEntity, TV>([NotNull] this IQueryable<TEntity> source, [NotNull] [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<double>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AnalyticFunctions.Average, source, expr, modifier),
					new Expression[] { currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier) }
				));
		}

		[Sql.Extension("AVG({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> Average<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("AVG({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> Average<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		#endregion Average

		#region Corr

		[Sql.Extension("CORR({expr1}, {expr2})", IsAggregate = true)]
		public static Decimal Corr<T>(this IEnumerable<T> source, [ExprParameter] Expression<Func<T, object>> expr1, [ExprParameter] Expression<Func<T, object>> expr2)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("CORR({expr1}, {expr2})", IsAggregate = true)]
		public static Decimal Corr<TEntity>(
			[NotNull]            this IQueryable<TEntity>               source,
			[NotNull] [ExprParameter] Expression<Func<TEntity, object>> expr1,
			[NotNull] [ExprParameter] Expression<Func<TEntity, object>> expr2)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr1  == null) throw new ArgumentNullException(nameof(expr1));
			if (expr2  == null) throw new ArgumentNullException(nameof(expr2));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<Decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AnalyticFunctions.Corr, source, expr1, expr2),
					new Expression[] { currentSource.Expression, Expression.Quote(expr1), Expression.Quote(expr2) }
				));
		}

		[Sql.Extension("CORR({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> Corr<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		#endregion Corr

		#region Count

		[Sql.Extension("COUNT({expr})", IsAggregate = true)]
		public static long CountExt<TEntity>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, object> expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("COUNT({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true)]
		public static long CountExt<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("COUNT({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true)]
		public static long CountExt<TEntity, TV>([NotNull] this IQueryable<TEntity> source, [NotNull] [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] Sql.AggregateModifier modifier = Sql.AggregateModifier.None)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<long>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AnalyticFunctions.CountExt, source, expr, modifier),
					new Expression[] { currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier) }
				));
		}

		[Sql.Extension("COUNT(*)", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<long> Count(this Sql.ISqlExtension ext)
		{
			throw new NotImplementedException();
		}
		[Sql.Extension("COUNT({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> Count<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("COUNT({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<long> Count(this Sql.ISqlExtension ext, [ExprParameter] object expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region CovarPop

		[Sql.Extension("COVAR_POP({expr1}, {expr2})", IsAggregate = true)]
		public static Decimal CovarPop<T>(this IEnumerable<T> source, [ExprParameter] Expression<Func<T, object>> expr1, [ExprParameter] Expression<Func<T, object>> expr2)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("COVAR_POP({expr1}, {expr2})", IsAggregate = true)]
		public static Decimal CovarPop<TEntity>(
			[NotNull]            this IQueryable<TEntity>               source,
			[NotNull] [ExprParameter] Expression<Func<TEntity, object>> expr1,
			[NotNull] [ExprParameter] Expression<Func<TEntity, object>> expr2)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr1  == null) throw new ArgumentNullException(nameof(expr1));
			if (expr2  == null) throw new ArgumentNullException(nameof(expr2));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<Decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AnalyticFunctions.CovarPop, source, expr1, expr2),
					new Expression[] { currentSource.Expression, Expression.Quote(expr1), Expression.Quote(expr2) }
				));
		}

		[Sql.Extension("COVAR_POP({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> CovarPop<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr1, [ExprParameter]T expr2)
		{
			throw new NotImplementedException();
		}

		#endregion CovarPop

		#region CovarSamp

		[Sql.Extension("COVAR_SAMP({expr1}, {expr2})", IsAggregate = true)]
		public static Decimal CovarSamp<T>(this IEnumerable<T> source, [ExprParameter] Expression<Func<T, object>> expr1, [ExprParameter] Expression<Func<T, object>> expr2)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("COVAR_SAMP({expr1}, {expr2})", IsAggregate = true)]
		public static Decimal CovarSamp<TEntity>(
			[NotNull]            this IQueryable<TEntity>               source,
			[NotNull] [ExprParameter] Expression<Func<TEntity, object>> expr1,
			[NotNull] [ExprParameter] Expression<Func<TEntity, object>> expr2)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr1  == null) throw new ArgumentNullException(nameof(expr1));
			if (expr2  == null) throw new ArgumentNullException(nameof(expr2));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<Decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AnalyticFunctions.CovarSamp, source, expr1, expr2),
					new Expression[] { currentSource.Expression, Expression.Quote(expr1), Expression.Quote(expr2) }
				));
		}

		[Sql.Extension("COVAR_SAMP({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> CovarSamp<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr1, [ExprParameter]T expr2)
		{
			throw new NotImplementedException();
		}

		#endregion CovarSamp

		[Sql.Extension("CUME_DIST({expr, ', '}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static INeedsWithinGroupWithOrderOnly<TR> CumeDist<TR>(this Sql.ISqlExtension ext, [ExprParameter] params object[] expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("CUME_DIST()", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAnalyticFunctionWithoutWindow<TR> CumeDist<TR>(this Sql.ISqlExtension ext)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("DENSE_RANK({expr1}, {expr2}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static INeedsWithinGroupWithOrderOnly<long> DenseRank(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("DENSE_RANK()", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAnalyticFunctionWithoutWindow<long> DenseRank(this Sql.ISqlExtension ext)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("FIRST_VALUE({expr}{_}{modifier?})", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> FirstValue<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.Nulls nulls)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("LAG({expr}{_}{modifier?})", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsAggregate = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lag<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.Nulls nulls)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("LAG({expr}{_}{modifier?}, {offset}, {default})", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsAggregate = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lag<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.Nulls nulls, [ExprParameter] int offset, [ExprParameter] int? @default)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("LAST_VALUE({expr}{_}{modifier?})", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> LastValue<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.Nulls nulls)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("LEAD({expr}{_}{modifier?})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lead<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.Nulls nulls)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("LEAD({expr}{_}{modifier?}, {offset}, {default})", TokenName = FunctionToken, BuilderType = typeof(ApplyNullsModifier), ChainPrecedence = 1, IsAggregate = true)]
		public static IAnalyticFunctionWithoutWindow<T> Lead<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.Nulls nulls, [ExprParameter] int offset, [ExprParameter] int? @default)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("LISTAGG({expr}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static INeedsWithinGroupWithOrderAndMaybePartition<string> ListAgg<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr)
		{
			throw new NotImplementedException();
		}
		[Sql.Extension("LISTAGG({expr}, {delimiter}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static INeedsWithinGroupWithOrderAndMaybePartition<string> ListAgg<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, [ExprParameter] string delimiter)
		{
			throw new NotImplementedException();
		}

		#region Max

		[Sql.Extension("MAX({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true)]
		public static TV Max<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("MAX({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true)]
		public static TV Max<TEntity, TV>([NotNull] this IQueryable<TEntity> source, [NotNull] [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<TV>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AnalyticFunctions.Max, source, expr, modifier),
					new Expression[] { currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier) }
				));
		}

		[Sql.Extension("MAX({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> Max<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("MAX({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> Max<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		#endregion Max

		#region Median

		[Sql.Extension("MEDIAN({expr})", IsAggregate = true)]
		public static long Median<TEntity, T>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, T> expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("MEDIAN({expr})", IsAggregate = true)]
		public static long Median<TEntity, TV>([NotNull] this IQueryable<TEntity> source, [NotNull] [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<long>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AnalyticFunctions.Median, source, expr),
					new Expression[] { currentSource.Expression, Expression.Quote(expr) }
				));
		}

		[Sql.Extension("MEDIAN({expr}) {over}", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IReadyToFunctionOrOverWithPartition<T> Median<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr)
		{
			throw new NotImplementedException();
		}

		#endregion Median

		#region Min

		[Sql.Extension("MIN({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true)]
		public static TV Min<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("MIN({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true)]
		public static TV Min<TEntity, TV>([NotNull] this IQueryable<TEntity> source, [NotNull] [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<TV>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AnalyticFunctions.Min, source, expr, modifier),
					new Expression[] { currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier) }
				));
		}

		[Sql.Extension("MIN({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> Min<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("MIN({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> Min<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		#endregion Min

		[Sql.Extension("NTH_VALUE({expr}, {n})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> NthValue<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, [ExprParameter] long n)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("NTH_VALUE({expr}, {n}){_}{from?}{_}{nulls?}", TokenName = FunctionToken, BuilderType = typeof(ApplyFromAndNullsModifier), ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> NthValue<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, [ExprParameter] long n, [SqlQueryDependent] Sql.From from, [SqlQueryDependent] Sql.Nulls nulls)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("NTILE({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAnalyticFunctionWithoutWindow<T> NTile<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("PERCENT_RANK({expr, ', '}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static INeedsWithinGroupWithOrderOnly<T> PercentRank<T>(this Sql.ISqlExtension ext, [ExprParameter] params object[] expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("PERCENT_RANK()", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAnalyticFunctionWithoutWindow<T> PercentRank<T>(this Sql.ISqlExtension ext)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("PERCENTILE_CONT({expr}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static INeedsWithinGroupWithSingleOrderAndMaybePartition<T> PercentileCont<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		//TODO: check nulls support when ordering
		[Sql.Extension("PERCENTILE_DISC({expr}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static INeedsWithinGroupWithSingleOrderAndMaybePartition<T> PercentileDisc<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("RANK({expr, ', '}) {within_group}", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static INeedsWithinGroupWithOrderOnly<long> Rank(this Sql.ISqlExtension ext, [ExprParameter] params object[] expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("RANK()", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAnalyticFunctionWithoutWindow<long> Rank(this Sql.ISqlExtension ext)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("RATIO_TO_REPORT({expr}) {over}", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IOverWithPartitionNeeded<TR> RatioToReport<TR>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		#region REGR_ function

		[Sql.Extension("REGR_SLOPE({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> RegrSlope<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("REGR_INTERCEPT({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> RegrIntercept<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("REGR_COUNT({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<long> RegrCount(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("REGR_R2({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> RegrR2<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("REGR_AVGX({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> RegrAvgX<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("REGR_AVGY({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> RegrAvgY<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		// ReSharper disable once InconsistentNaming
		[Sql.Extension("REGR_SXX({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> RegrSXX<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		// ReSharper disable once InconsistentNaming
		[Sql.Extension("REGR_SYY({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> RegrSYY<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		// ReSharper disable once InconsistentNaming
		[Sql.Extension("REGR_SXY({expr1}, {expr2})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> RegrSXY<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		#endregion

		[Sql.Extension("ROW_NUMBER()", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAnalyticFunctionWithoutWindow<long> RowNumber(this Sql.ISqlExtension ext)
		{
			throw new NotImplementedException();
		}

		#region StdDev

		[Sql.Extension(              "STDEV({expr})",  TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		[Sql.Extension(PN.Oracle,    "STDDEV({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static double StdDev<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension(              "STDEV({modifier?}{_}{expr})",  TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 1, IsAggregate = true)]
		[Sql.Extension(PN.Oracle,    "STDDEV({modifier?}{_}{expr})", TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 1, IsAggregate = true)]
		public static double StdDev<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension(              "STDEV({modifier?}{_}{expr})",  TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 1, IsAggregate = true)]
		[Sql.Extension(PN.Oracle,    "STDDEV({modifier?}{_}{expr})", TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 1, IsAggregate = true)]
		public static double StdDev<TEntity, TV>([NotNull] this IQueryable<TEntity> source, [NotNull] [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] Sql.AggregateModifier modifier = Sql.AggregateModifier.None )
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<double>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AnalyticFunctions.StdDev, source, expr, modifier),
					new Expression[] { currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier) }
				));
		}

		[Sql.Extension(              "STDEV({expr})",  TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		[Sql.Extension(PN.Oracle,    "STDDEV({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> StdDev<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension(              "STDEV({modifier?}{_}{expr})",  TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 1, IsAggregate = true)]
		[Sql.Extension(PN.Oracle,    "STDDEV({modifier?}{_}{expr})", TokenName = FunctionToken, BuilderType = typeof(ApplyAggregateModifier), ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> StdDev<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		#endregion StdDev

		#region StdDevPop

		[Sql.Extension("STDDEV_POP({expr})", IsAggregate = true)]
		public static decimal StdDevPop<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("STDDEV_POP({expr})", IsAggregate = true)]
		public static decimal StdDevPop<TEntity, TV>([NotNull] this IQueryable<TEntity> source, [NotNull] [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AnalyticFunctions.StdDevPop, source, expr),
					new Expression[] { currentSource.Expression, Expression.Quote(expr) }
				));
		}

		[Sql.Extension("STDDEV_POP({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> StdDevPop<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		#endregion StdDevPop

		#region StdDevSamp

		[Sql.Extension("STDDEV_SAMP({expr})", IsAggregate = true)]
		public static decimal StdDevSamp<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("STDDEV_SAMP({expr})", IsAggregate = true)]
		public static decimal StdDevSamp<TEntity, TV>([NotNull] this IQueryable<TEntity> source, [NotNull] [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AnalyticFunctions.StdDevSamp, source, expr),
					new Expression[] { currentSource.Expression, Expression.Quote(expr) }
				));
		}

		[Sql.Extension("STDDEV_SAMP({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> StdDevSamp<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		#endregion StdDevSamp

		[Sql.Extension("SUM({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> Sum<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("SUM({modifier?}{_}{expr})" , BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> Sum<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		#region VarPop

		[Sql.Extension("VAR_POP({expr})", IsAggregate = true)]
		public static decimal VarPop<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("VAR_POP({expr})", IsAggregate = true)]
		public static decimal VarPop<TEntity, TV>([NotNull] this IQueryable<TEntity> source, [NotNull] [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AnalyticFunctions.VarPop, source, expr),
					new Expression[] { currentSource.Expression, Expression.Quote(expr) }
				));
		}

		[Sql.Extension("VAR_POP({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> VarPop<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		#endregion VarPop

		#region VarSamp

		[Sql.Extension("VAR_SAMP({expr})", IsAggregate = true)]
		public static decimal VarSamp<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("VAR_SAMP({expr})", IsAggregate = true)]
		public static decimal VarSamp<TEntity, TV>([NotNull] this IQueryable<TEntity> source, [NotNull] [ExprParameter] Expression<Func<TEntity, TV>> expr)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<decimal>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AnalyticFunctions.VarSamp, source, expr),
					new Expression[] { currentSource.Expression, Expression.Quote(expr) }
				));
		}

		[Sql.Extension("VAR_SAMP({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> VarSamp<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		#endregion VarSamp

		#region Variance

		[Sql.Extension("VARIANCE({expr})", IsAggregate = true)]
		public static TV Variance<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("VARIANCE({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true)]
		public static TV Variance<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("VARIANCE({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), IsAggregate = true)]
		public static TV Variance<TEntity, TV>([NotNull] this IQueryable<TEntity> source, [NotNull] [ExprParameter] Expression<Func<TEntity, TV>> expr, [SqlQueryDependent] Sql.AggregateModifier modifier = Sql.AggregateModifier.None)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (expr   == null) throw new ArgumentNullException(nameof(expr));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<TV>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AnalyticFunctions.Variance, source, expr, modifier),
					new Expression[] { currentSource.Expression, Expression.Quote(expr), Expression.Constant(modifier) }
				));
		}

		[Sql.Extension("VARIANCE({expr})", TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> Variance<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("VARIANCE({modifier?}{_}{expr})", BuilderType = typeof(ApplyAggregateModifier), TokenName = FunctionToken, ChainPrecedence = 1, IsAggregate = true)]
		public static IAggregateFunctionSelfContained<T> Variance<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr, [SqlQueryDependent] Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		#endregion

		[Sql.Extension("{function} KEEP (DENSE_RANK FIRST {order_by_clause}){_}{over?}", ChainPrecedence = 10, IsAggregate = true)]
		public static INeedOrderByAndMaybeOverWithPartition<TR> KeepFirst<TR>(this IAggregateFunction<TR> ext)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("{function} KEEP (DENSE_RANK LAST {order_by_clause}){_}{over?}", ChainPrecedence = 10, IsAggregate = true)]
		public static INeedOrderByAndMaybeOverWithPartition<TR> KeepLast<TR>(this IAggregateFunction<TR> ext)
		{
			throw new NotImplementedException();
		}

		#endregion Analytic functions
	}

}
