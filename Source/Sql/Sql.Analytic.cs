namespace LinqToDB
{
	using System;
	using JetBrains.Annotations;

	using SqlQuery;

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

	public static class AnalyticFunctions
	{
		public const string AggregateFunctionName = "aggregate_function";
		public const string AnalyticFunctionName  = "analytic_function";

		#region Static Builders

		[UsedImplicitly]
		static void OrderItemBuilder(Sql.ISqExtensionBuilder builder)
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

		[UsedImplicitly]
		static void ApplyAggregateModifier(Sql.ISqExtensionBuilder builder)
		{
			var modifier = builder.GetValue<Sql.AggregateModifier>("modifier");
			switch (modifier)
			{
				case Sql.AggregateModifier.None :
					break;
				case Sql.AggregateModifier.Distinct :
					builder.AddParameter("modifier", new SqlExtension("DISTINCT"));
					break;
				case Sql.AggregateModifier.All :
					builder.AddParameter("modifier", new SqlExtension("ALL"));
					break;
				default :
					throw new ArgumentOutOfRangeException();
			}
		}

		[UsedImplicitly]
		static void ApplyNullsModifier(Sql.ISqExtensionBuilder builder)
		{
			var nulls = builder.GetValue<Sql.Nulls>("nulls");
			builder.AddParameter("modifier", new SqlExtension(GetNullsStr(nulls)));
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

		[UsedImplicitly]
		static void ApplyFromAndNullsModifier(Sql.ISqExtensionBuilder builder)
		{
			var nulls = builder.GetValue<Sql.Nulls>("nulls");
			var from  = builder.GetValue<Sql.From>("from");

			var fromStr = GetFromStr(from);
			var nullsStr = GetNullsStr(nulls);

			builder.AddParameter("from", new SqlExtension(fromStr));
			builder.AddParameter("nulls", new SqlExtension(nullsStr));
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
			[Sql.Extension("OVER({query_partition_clause?})", "over")]
			IOverMayHavePartition<TR> Over();
		}

		public interface IOverWithPartitionNeeded<out TR>
		{
			[Sql.Extension("OVER({query_partition_clause?})", "over")]
			IOverMayHavePartition<TR> Over();
		}

		public interface INeedOrderByAndMaybeOverWithPartition<out TR>
		{
			[Sql.Extension("ORDER BY {order_item, ', '}", "order_by_clause")]
			[Sql.Extension("{expr}", "order_item")]
			IOrderedAcceptOverReadyToFunction<TR> OrderBy<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", "order_by_clause")]
			[Sql.Extension("{expr}", "order_item", BuilderType = typeof(AnalyticFunctions), BuilderMethod = "OrderItemBuilder")]
			IOrderedAcceptOverReadyToFunction<TR> OrderBy<TKey>([ExprParameter] TKey expr, Sql.NullsPosition nulls);

			[Sql.Extension("ORDER BY {order_item, ', '}", "order_by_clause")]
			[Sql.Extension("{expr} DESC", "order_item")]
			IOrderedAcceptOverReadyToFunction<TR> OrderByDesc<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", "order_by_clause")]
			[Sql.Extension("{expr} DESC", "order_item", BuilderType = typeof(AnalyticFunctions), BuilderMethod = "OrderItemBuilder")]
			IOrderedAcceptOverReadyToFunction<TR> OrderByDesc<TKey>([ExprParameter] TKey expr, Sql.NullsPosition nulls);
		}

		public interface INeedSingleOrderByAndMaybeOverWithPartition<out TR>
		{
			[Sql.Extension("ORDER BY {order_item, ', '}", "order_by_clause")]
			[Sql.Extension("{expr}", "order_item")]
			IReadyToFunctionOrOverWithPartition<TR> OrderBy<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", "order_by_clause")]
			[Sql.Extension("{expr} DESC", "order_item")]
			IReadyToFunctionOrOverWithPartition<TR> OrderByDesc<TKey>([ExprParameter] TKey expr);
		}

		public interface IOrderedAcceptOverReadyToFunction<out TR> : IReadyToFunctionOrOverWithPartition<TR>
		{
			[Sql.Extension("{expr}", "order_item")]
			IOrderedAcceptOverReadyToFunction<TR> ThenBy<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("{expr}", "order_item", BuilderType = typeof(AnalyticFunctions), BuilderMethod = "OrderItemBuilder")] 
			IOrderedAcceptOverReadyToFunction<TR> ThenBy<TKey>([ExprParameter] TKey expr, Sql.NullsPosition nulls);

			[Sql.Extension("{expr} DESC", "order_item")]
			IOrderedAcceptOverReadyToFunction<TR> ThenByDesc<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("{expr} DESC", "order_item", BuilderType = typeof(AnalyticFunctions), BuilderMethod = "OrderItemBuilder")]
			IOrderedAcceptOverReadyToFunction<TR> ThenByDesc<TKey>([ExprParameter] TKey expr, Sql.NullsPosition nulls);
		}

		public interface IOverMayHavePartition<out TR> : IReadyToFunction<TR>
		{
			[Sql.Extension("PARTITION BY {expr, ', '}", "query_partition_clause")]
			IReadyToFunction<TR> PartitionBy([ExprParameter("expr")] params object[] expressions);
		}

		public interface IPartitionedMayHaveOrder<out TR> : IReadyToFunction<TR>, INeedsOrderByOnly<TR>
		{
		}

		public interface IOverMayHavePartitionAndOrder<out TR> : IReadyToFunction<TR>, INeedsOrderByOnly<TR>
		{
			[Sql.Extension("PARTITION BY {expr, ', '}", "query_partition_clause")]
			IPartitionedMayHaveOrder<TR> PartitionBy([ExprParameter("expr")] params object[] expressions);
		}

		public interface IAnalyticFunction<out TR>
		{
			[Sql.Extension("{analytic_function} OVER({query_partition_clause?}{_}{order_by_clause?}{_}{windowing_clause?})",
				"over", ChainPrecedence = 10)]
			IReadyForFullAnalyticClause<TR> Over();
		}

		public interface IAnalyticFunctionWithoutWindow<out TR>
		{
			[Sql.Extension("{analytic_function} OVER({query_partition_clause?}{_}{order_by_clause?})", "over", ChainPrecedence = 10)]
			IOverMayHavePartitionAndOrder<TR> Over();
		}

		public interface IAggregateFunction<out TR> : IAnalyticFunction<TR> {}
		public interface IAggregateFunctionSelfContained<out TR> : IAggregateFunction<TR>, IReadyToFunction<TR> {}

		public interface IOrderedReadyToFunction<out TR> : IReadyToFunction<TR>
		{
			[Sql.Extension("{expr}", "order_item")]
			IOrderedReadyToFunction<TR> ThenBy<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("{expr}", "order_item", BuilderType = typeof(AnalyticFunctions), BuilderMethod = "OrderItemBuilder")]
			IOrderedReadyToFunction<TR> ThenBy<TKey>([ExprParameter] TKey expr, Sql.NullsPosition nulls);

			[Sql.Extension("{expr} DESC", "order_item")]
			IOrderedReadyToFunction<TR> ThenByDesc<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("{expr} DESC", "order_item", BuilderType = typeof(AnalyticFunctions), BuilderMethod = "OrderItemBuilder")]
			IOrderedReadyToFunction<TR> ThenByDesc<TKey>([ExprParameter] TKey expr, Sql.NullsPosition nulls);
		}

		public interface INeedsWithingGroupWithOrderOnly<out TR>
		{
			[Sql.Extension("WITHIN GROUP ({order_by_clause})", "within_group")]
			INeedsOrderByOnly<TR> WithinGroup { get; }
		}

		public interface INeedsWithingGroupWithOrderAndMaybePartition<out TR>
		{
			[Sql.Extension("WITHIN GROUP ({order_by_clause}){_}{over?}", "within_group")]
			INeedOrderByAndMaybeOverWithPartition<TR> WithinGroup { get; }
		}

		public interface INeedsWithingGroupWithSingleOrderAndMaybePartition<out TR>
		{
			[Sql.Extension("WITHIN GROUP ({order_by_clause}){_}{over?}", "within_group")]
			INeedSingleOrderByAndMaybeOverWithPartition<TR> WithinGroup { get; }
		}

		public interface INeedsOrderByOnly<out TR>
		{
			[Sql.Extension("ORDER BY {order_item, ', '}", "order_by_clause")]
			[Sql.Extension("{expr}", "order_item")]
			IOrderedReadyToFunction<TR> OrderBy<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", "order_by_clause")]
			[Sql.Extension("{expr}", "order_item", BuilderType = typeof(AnalyticFunctions), BuilderMethod = "OrderItemBuilder")]
			IOrderedReadyToFunction<TR> OrderBy<TKey>([ExprParameter] TKey expr, Sql.NullsPosition nulls);

			[Sql.Extension("ORDER BY {order_item, ', '}", "order_by_clause")]
			[Sql.Extension("{expr} DESC", "order_item")]
			IOrderedReadyToFunction<TR> OrderByDesc<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("ORDER BY {order_item, ', '}", "order_by_clause")]
			[Sql.Extension("{expr} DESC", "order_item", BuilderType = typeof(AnalyticFunctions), BuilderMethod = "OrderItemBuilder")]
			IOrderedReadyToFunction<TR> OrderByDesc<TKey>([ExprParameter] TKey expr, Sql.NullsPosition nulls);
		}

		#region Full Support

		public interface IReadyForSortingWithWindow<out TR>
		{
			[Sql.Extension("ORDER BY {order_item, ', '}", "order_by_clause")]
			[Sql.Extension("{expr}", "order_item")]
			IOrderedReadyToWindowing<TR> OrderBy<TKey>([ExprParameter("expr")] TKey keySelector);

			[Sql.Extension("ORDER BY {order_item, ', '}", "order_by_clause")]
			[Sql.Extension("{expr}", "order_item", BuilderType = typeof(AnalyticFunctions), BuilderMethod = "OrderItemBuilder")]
			IOrderedReadyToWindowing<TR> OrderBy<TKey>([ExprParameter("expr")] TKey keySelector, Sql.NullsPosition nulls);

			[Sql.Extension("ORDER BY {order_item, ', '}", "order_by_clause")]
			[Sql.Extension("{expr} DESC", "order_item")]
			IOrderedReadyToWindowing<TR> OrderByDesc<TKey>([ExprParameter("expr")] TKey keySelector);

			[Sql.Extension("ORDER BY {order_item, ', '}", "order_by_clause")]
			[Sql.Extension("{expr} DESC", "order_item", BuilderType = typeof(AnalyticFunctions), BuilderMethod = "OrderItemBuilder")]
			IOrderedReadyToWindowing<TR> OrderByDesc<TKey>([ExprParameter("expr")] TKey keySelector, Sql.NullsPosition nulls);
		}

		public interface IReadyForFullAnalyticClause<out TR> : IReadyToFunction<TR>, IReadyForSortingWithWindow<TR>
		{
			[Sql.Extension("PARTITION BY {expr, ', '}", "query_partition_clause")]
			IPartitionDefinedReadyForSortingWithWindow<TR> PartitionBy([ExprParameter("expr")] params object[] expressions);
		}

		public interface IPartitionDefinedReadyForSortingWithWindow<out TR> : IReadyForSortingWithWindow<TR>, IReadyToFunction<TR>
		{
		}

		public interface IOrderedReadyToWindowing<out TR> : IReadyToFunction<TR>
		{
			[Sql.Extension("ROWS {boundary_clause}", "windowing_clause")]
			IBoundaryExpected<TR> Rows { get; }

			[Sql.Extension("RANGE {boundary_clause}", "windowing_clause")]
			IBoundaryExpected<TR> Range { get; }

			[Sql.Extension("{expr}", "order_item")]
			IOrderedReadyToWindowing<TR> ThenBy<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("{expr}", "order_item", BuilderType = typeof(AnalyticFunctions), BuilderMethod = "OrderItemBuilder")]
			IOrderedReadyToWindowing<TR> ThenBy<TKey>([ExprParameter] TKey expr, Sql.NullsPosition nulls);

			[Sql.Extension("{expr} DESC", "order_item")]
			IOrderedReadyToWindowing<TR> ThenByDesc<TKey>([ExprParameter] TKey expr);

			[Sql.Extension("{expr} DESC", "order_item", BuilderType = typeof(AnalyticFunctions), BuilderMethod = "OrderItemBuilder")]
			IOrderedReadyToWindowing<TR> ThenByDesc<TKey>([ExprParameter] TKey expr, Sql.NullsPosition nulls);
		}

		public interface IBoundaryExpected<out TR>
		{
			[Sql.Extension("UNBOUNDED PRECEDING", "boundary_clause")]
			IReadyToFunction<TR> UnboundedPreceding { get; }

			[Sql.Extension("CURRENT ROW", "boundary_clause")]
			IReadyToFunction<TR> CurrentRow { get; }

			[Sql.Extension("{value_expr} PRECEDING", "boundary_clause")]
			IReadyToFunction<TR> ValuePreceding<T>([ExprParameter("value_expr")] T value);

			[Sql.Extension("BETWEEN {start_boundary} AND {end_boundary}", "boundary_clause")]
			IBetweenStartExpected<TR> Between { get; }
		}

		public interface IBetweenStartExpected<out TR>
		{
			[Sql.Extension("UNBOUNDED PRECEDING", "start_boundary")]
			IAndExpected<TR> UnboundedPreceding { get; }

			[Sql.Extension("CURRENT ROW", "start_boundary")]
			IAndExpected<TR> CurrentRow { get; }

			[Sql.Extension("{value_expr} PRECEDING", "start_boundary")]
			IAndExpected<TR> ValuePreceding<T>([ExprParameter("value_expr")] T value);
		}

		public interface IAndExpected<out TR>
		{
			[Sql.Extension("")]
			ISecondBoundaryExpected<TR> And { get; }
		}

		public interface ISecondBoundaryExpected<out TR>
		{
			[Sql.Extension("UNBOUNDED FOLLOWING", "end_boundary")]
			IReadyToFunction<TR> UnboundedFollowing { get; }

			[Sql.Extension("CURRENT ROW", "end_boundary")]
			IReadyToFunction<TR> CurrentRow { get; }

			[Sql.Extension("{value_expr} PRECEDING", "end_boundary")]
			IReadyToFunction<TR> ValuePreceding<T>([ExprParameter("value_expr")] T value);

			[Sql.Extension("{value_expr} FOLLOWING", "end_boundary")]
			IReadyToFunction<TR> ValueFollowing<T>([ExprParameter("value_expr")] T value);
		}

		#endregion Full Support
		
		#endregion API Interfaces

		#region Analytic functions

		[Sql.Extension("AVG({expr})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> Average<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("AVG({modifier?}{_}{expr})", BuilderMethod = "ApplyAggregateModifier", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> Average<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr, Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("CORR({expr1}, {expr2})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> Corr<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		#region Count

		[Sql.Extension("COUNT(*)", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<long> Count(this Sql.ISqlExtension ext)
		{
			throw new NotImplementedException();
		}
		[Sql.Extension("COUNT({expr})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> Count<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("COUNT({modifier?}{_}{expr})", BuilderMethod = "ApplyAggregateModifier", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<long> Count(this Sql.ISqlExtension ext, [ExprParameter] object expr, Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		#endregion

		[Sql.Extension("COVAR_POP({expr1}, {expr2})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> CovarPop<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr1, [ExprParameter]T expr2)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("COVAR_SAMP({expr1}, {expr2})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> CovarSamp<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr1, [ExprParameter]T expr2)
		{
			throw new NotImplementedException();
		}
		
		[Sql.Extension("CUME_DIST({expr, ', '}) {within_group}", Names = AggregateFunctionName, ChainPrecedence = 1)]
		public static INeedsWithingGroupWithOrderOnly<TR> CumeDist<TR>(this Sql.ISqlExtension ext, [ExprParameter] params object[] expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("CUME_DIST()", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAnalyticFunctionWithoutWindow<TR> CumeDist<TR>(this Sql.ISqlExtension ext)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("DENSE_RANK({expr1}, {expr2}) {within_group}", Names = AggregateFunctionName, ChainPrecedence = 1)]
		public static INeedsWithingGroupWithOrderOnly<long> DenseRank(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("DENSE_RANK()", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAnalyticFunctionWithoutWindow<long> DenseRank(this Sql.ISqlExtension ext)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("FIRST_VALUE({expr}{_}{modifier?})", Names = AggregateFunctionName + "," + AnalyticFunctionName, BuilderMethod = "ApplyNullsModifier", ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> FirstValue<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, Sql.Nulls nulls)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("LAG({expr}{_}{modifier?})", Names = AggregateFunctionName + "," + AnalyticFunctionName, BuilderMethod = "ApplyNullsModifier", ChainPrecedence = 1)]
		public static IAnalyticFunctionWithoutWindow<T> Lag<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, Sql.Nulls nulls)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("LAG({expr}{_}{modifier?}, {offset}, {default})", Names = AggregateFunctionName + "," + AnalyticFunctionName, BuilderMethod = "ApplyNullsModifier", ChainPrecedence = 1)]
		public static IAnalyticFunctionWithoutWindow<T> Lag<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, Sql.Nulls nulls, [ExprParameter] int offset, [ExprParameter] int? @default)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("LAST_VALUE({expr}{_}{modifier?})", Names = AggregateFunctionName + "," + AnalyticFunctionName, BuilderMethod = "ApplyNullsModifier", ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> LastValue<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, Sql.Nulls nulls)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("LEAD({expr}{_}{modifier?})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAnalyticFunctionWithoutWindow<T> Lead<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, Sql.Nulls nulls)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("LEAD({expr}{_}{modifier?}, {offset}, {default})", Names = AggregateFunctionName + "," + AnalyticFunctionName, BuilderMethod = "ApplyNullsModifier", ChainPrecedence = 1)]
		public static IAnalyticFunctionWithoutWindow<T> Lead<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, Sql.Nulls nulls, [ExprParameter] int offset, [ExprParameter] int? @default)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("LISTAGG({expr}) {within_group}", Names = AggregateFunctionName, ChainPrecedence = 1)]
		public static INeedsWithingGroupWithOrderAndMaybePartition<string> ListAgg<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr)
		{
			throw new NotImplementedException();
		}
		[Sql.Extension("LISTAGG({expr}, {delimiter}) {within_group}", Names = AggregateFunctionName, ChainPrecedence = 1)]
		public static INeedsWithingGroupWithOrderAndMaybePartition<string> ListAgg<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, [ExprParameter] string delimiter)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("MAX({expr})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> Max<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("MAX({modifier?}{_}{expr})", BuilderMethod = "ApplyAggregateModifier", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> Max<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("MEDIAN({expr})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IReadyToFunctionOrOverWithPartition<T> Median<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("MIN({expr})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> Min<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("MIN({modifier?}{_}{expr})", BuilderMethod = "ApplyAggregateModifier", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> Min<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("NTH_VALUE({expr}, {n})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> NthValue<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, [ExprParameter] long n)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("NTH_VALUE({expr}, {n}){_}{from?}{_}{nulls?}", Names = AggregateFunctionName + "," + AnalyticFunctionName, BuilderMethod = "ApplyFromAndNullsModifier", ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> NthValue<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, [ExprParameter] long n, Sql.From from, Sql.Nulls nulls)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("NTILE({expr})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAnalyticFunctionWithoutWindow<T> NTile<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("PERCENT_RANK({expr, ', '}) {within_group}", Names = AggregateFunctionName, ChainPrecedence = 1)]
		public static INeedsWithingGroupWithOrderOnly<T> PercentRank<T>(this Sql.ISqlExtension ext, [ExprParameter] params object[] expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("PERCENT_RANK()", Names = AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAnalyticFunctionWithoutWindow<T> PercentRank<T>(this Sql.ISqlExtension ext)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("PERCENTILE_CONT({expr}) {within_group}", Names = AnalyticFunctionName, ChainPrecedence = 1)]
		public static INeedsWithingGroupWithSingleOrderAndMaybePartition<T> PercentileCont<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		//TODO: check nulls support when ordering
		[Sql.Extension("PERCENTILE_DISC({expr}) {within_group}", Names = AnalyticFunctionName, ChainPrecedence = 1)]
		public static INeedsWithingGroupWithSingleOrderAndMaybePartition<T> PercentileDisc<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("RANK({expr, ', '}) {within_group}", Names = AggregateFunctionName, ChainPrecedence = 1)]
		public static INeedsWithingGroupWithOrderOnly<long> Rank(this Sql.ISqlExtension ext, [ExprParameter] params object[] expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("RANK()", Names = AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAnalyticFunctionWithoutWindow<long> Rank(this Sql.ISqlExtension ext)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("RATIO_TO_REPORT({expr}) {over}", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IOverWithPartitionNeeded<TR> RatioToReport<TR>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		#region REGR_ function

		[Sql.Extension("REGR_SLOPE({expr1}, {expr2})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> RegrSlope<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("REGR_INTERCEPT({expr1}, {expr2})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> RegrIntercept<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("REGR_COUNT({expr1}, {expr2})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<long> RegrCount(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("REGR_R2({expr1}, {expr2})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> RegrR2<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("REGR_AVGX({expr1}, {expr2})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> RegrAvgX<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("REGR_AVGY({expr1}, {expr2})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> RegrAvgY<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		// ReSharper disable once InconsistentNaming
		[Sql.Extension("REGR_SXX({expr1}, {expr2})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> RegrSXX<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		// ReSharper disable once InconsistentNaming
		[Sql.Extension("REGR_SYY({expr1}, {expr2})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> RegrSYY<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		// ReSharper disable once InconsistentNaming
		[Sql.Extension("REGR_SXY({expr1}, {expr2})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> RegrSXY<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr1, [ExprParameter] object expr2)
		{
			throw new NotImplementedException();
		}

		#endregion

		[Sql.Extension("ROW_NUMBER()", Names = AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAnalyticFunctionWithoutWindow<long> RowNumber(this Sql.ISqlExtension ext)
		{
			throw new NotImplementedException();
		}

		//TODO: support of aggreagate functions
//		[Sql.Extension("STDDEV({expr})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
//		public static IAggregateFunctionSelfContained<double> StdDev<TEntity, TV>(this IEnumerable<TEntity> source, [ExprParameter] Func<TEntity, TV> expr)
//		{
//			throw new NotImplementedException();
//		}

		[Sql.Extension("STDDEV({expr})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> StdDev<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("STDDEV({modifier?}{_}{expr})", Names = AggregateFunctionName + "," + AnalyticFunctionName, BuilderMethod = "ApplyAggregateModifier", ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> StdDev<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr, Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("STDDEV_POP({expr})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> StdDevPop<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("STDDEV_SAMP({expr})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> StdDevSamp<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("SUM({expr})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> Sum<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("SUM({modifier?}{_}{expr})", BuilderMethod = "ApplyAggregateModifier", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> Sum<T>(this Sql.ISqlExtension ext, [ExprParameter] T expr, Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("VAR_POP({expr})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> VarPop<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("VAR_SAMP({expr})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> VarSamp<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("VARIANCE({expr})", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> Variance<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("VARIANCE({modifier?}{_}{expr})", BuilderMethod = "ApplyAggregateModifier", Names = AggregateFunctionName + "," + AnalyticFunctionName, ChainPrecedence = 1)]
		public static IAggregateFunctionSelfContained<T> Variance<T>(this Sql.ISqlExtension ext, [ExprParameter] object expr, Sql.AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[Sql.Extension("{aggregate_function} KEEP (DENSE_RANK FIRST {order_by_clause}){_}{over?}", ChainPrecedence = 10)]
		public static INeedOrderByAndMaybeOverWithPartition<TR> KeepFirst<TR>(this IAggregateFunction<TR> ext)
		{
			throw new NotImplementedException();
		}
		
		[Sql.Extension("{aggregate_function} KEEP (DENSE_RANK LAST {order_by_clause}){_}{over?}", ChainPrecedence = 10)]
		public static INeedOrderByAndMaybeOverWithPartition<TR> KeepLast<TR>(this IAggregateFunction<TR> ext)
		{
			throw new NotImplementedException();
		}

		#endregion Analytic functions
	}

}