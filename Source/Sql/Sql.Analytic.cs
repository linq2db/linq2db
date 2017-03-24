namespace LinqToDB
{
	using System;
	using System.ComponentModel;
	using System.Linq.Expressions;

	using SqlQuery;

	using PN = LinqToDB.ProviderName;

	public static partial class Sql
	{
		public enum AggregateModifier
		{
			None,
			Distinct,
			All,
		}

		public interface IAnalyticFunctionBuilder
		{
			ISqlExpression ConvertParameter(Expression expr);
			T GetValue<T>(Expression expr);
			SqlAnalyticFunction Function { get; }
		}

		[ChainCollector]
		public static IOver<T> Over<T>(T entity)
		{
			throw new NotImplementedException();
		}

		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
		public class FunctionChainAttribute : ChainCollector
		{
		}

		[AttributeUsage(AttributeTargets.Method)]
		public class ChainCollector : ExpressionAttribute
		{
			public ChainCollector() : base(string.Empty, string.Empty)
			{
				ServerSideOnly = true;
				PreferServerSide = true;
			}
		}

		[AnalyticFunction("AVG")]
		public static TR Average<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> prop)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("AVG")]
		public static TR Average<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> prop, AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("CORR")]
		public static TR Corr<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("COUNT")]
		public static long Count<T>(this IReadyToFunction<T> window)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("COUNT")]
		public static long Count<T, TR>(this IReadyToFunction<T> window, Func<T, TR> prop)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			return default(long);
		}

		[AnalyticFunction("COUNT")]
		public static long Count<T, TR>(this IReadyToFunction<T> window, Func<T, TR> prop, AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("COVAR_POP")]
		public static TR CovarPop<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("COVAR_SAMP")]
		public static TR CovarSamp<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expressions)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("CUME_DIST")]
		public static TR CumeDist<T, TR>(this IReadyToFunction<TR> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("DENSE_RANK")]
		public static decimal DenseRank<T>(this IReadyToFunction<T> window, params Expression<Func<T, object>>[] expressions)
		{
			throw new NotImplementedException();
		}

		//TODO: FIRST - http://docs.oracle.com/cloud/latest/db112/SQLRF/functions065.htm#SQLRF00641

		[AnalyticFunction("FIRST_VALUE")]
		public static TR FirstValue<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, SqlAnalyticFunction.Nulls nulls)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("LAG")]
		public static TR Lag<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, SqlAnalyticFunction.Nulls nulls)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("LAG")]
		public static TR Lag<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, SqlAnalyticFunction.Nulls nulls, int offset, int? @default)
		{
			throw new NotImplementedException();
		}

		//TODO: LAST - http://docs.oracle.com/cloud/latest/db112/SQLRF/functions083.htm#SQLRF00653

		[AnalyticFunction("LAST_VALUE")]
		public static TR LastValue<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, SqlAnalyticFunction.Nulls nulls)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("LEAD")]
		public static TR Lead<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, SqlAnalyticFunction.Nulls nulls)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("LEAD")]
		public static TR Lead<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, SqlAnalyticFunction.Nulls nulls, int offset, int? @default)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("LISTAGG")]
		public static string ListAgg<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("LISTAGG")]
		public static string ListAgg<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, string delimiter)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("MAX")]
		public static TR Max<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("MAX")]
		public static TR Max<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("MEDIAN")]
		public static TR Med<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("MIN")]
		public static TR Min<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("MIN")]
		public static TR Min<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("NTH_VALUE")]
		public static TR NthValue<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, int n)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("NTILE")]
		public static long NTile<T>(this IReadyToFunction<T> window, Expression<Func<T, int>> groupCount)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("PERCENT_RANK")]
		public static long PercentRank<T>(this IReadyToFunction<T> window, params Expression<Func<T, object>>[] expressions)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("PERCENTILE_CONT")]
		public static double PercentileCont<T>(this IReadyToFunction<T> window, Expression<Func<T, object>> expr)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("PERCENTILE_DISC")]
		public static double PercentileDisc<T>(this IReadyToFunction<T> window, Expression<Func<T, object>> expr)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("RANK")]
		public static long Rank<T>(this IReadyToFunction<T> window)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("RATIO_TO_REPORT")]
		public static double? RatioToReport<T>(this IReadyToFunction<T> window, Expression<Func<T, object>> expr)
		{
			throw new NotImplementedException();
		}

		#region REGR_ (Linear Regression) Functions 

		[AnalyticFunction("REGR_SLOPE")]
		public static TR RegrSlope<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			throw new NotImplementedException();
		}
		
		[AnalyticFunction("REGR_INTERCEPT")]
		public static TR RegrIntercept<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			throw new NotImplementedException();
		}
		
		[AnalyticFunction("REGR_COUNT")]
		public static TR RegrCount<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			throw new NotImplementedException();
		}
		
		[AnalyticFunction("REGR_R2")]
		public static TR RegrR2<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			throw new NotImplementedException();
		}
		
		[AnalyticFunction("REGR_AVGX")]
		public static TR RegrAvgX<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			throw new NotImplementedException();
		}
		
		[AnalyticFunction("REGR_AVGY")]
		public static TR RegrAvgY<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			throw new NotImplementedException();
		}
		
		[AnalyticFunction("REGR_SXX")]
		public static TR RegrSXX<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			throw new NotImplementedException();
		}
		
		[AnalyticFunction("REGR_SYY")]
		public static TR RegrSYY<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			throw new NotImplementedException();
		}
		
		[AnalyticFunction("REGR_SXY")]
		public static TR RegrSXY<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			throw new NotImplementedException();
		}
		
		#endregion

		[AnalyticFunction("ROW_NUMBER")]
		public static long RowNumber<T>(this IReadyToFunction<T> window)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("STDDEV")]
		public static TR StdDev<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("STDDEV_POP")]
		public static TR StdDevPop<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("STDDEV_SAMP")]
		public static TR StdDevSamp<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("SUM")]
		public static TR Sum<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("SUM")]
		public static TR Sum<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("VAR_POP")]
		public static TR VarPop<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("VAR_SAMP")]
		public static TR VarSamp<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("VARIANCE")]
		public static TR Variance<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("VARIANCE")]
		public static TR Variance<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, AggregateModifier modifier)
		{
			throw new NotImplementedException();
		}

		public interface IReadyToFunction<T>
		{
		}

		public interface IPartitionNotRanged<T>
		{
			[FunctionChain]
			IWindowFrameExtent<T> Rows { get; }

			[FunctionChain]
			IWindowFrameExtent<T> Range { get; }
		}

		public interface IOver<T> : IReadyToFunction<T>, IPartitionNotOrdered<T>
		{
			[FunctionChain]
			IPartitionNotOrdered<T> PartitionBy<TKey>(Expression<Func<T, TKey>> field);
		}

		public interface IPartitionNotOrdered<T> : IPartitionNotRanged<T>
		{
			[FunctionChain]
			IPartitionOrdered<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);

			[FunctionChain]
			IPartitionOrdered<T> OrderByDesc<TKey>(Expression<Func<T, TKey>> keySelector);
		}

		public interface IPartitionOrdered<T> : IReadyToFunction<T>, IPartitionNotRanged<T>
		{
			[FunctionChain]
			IPartitionOrdered<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector);

			[FunctionChain]
			IPartitionOrdered<T> ThenByDesc<TKey>(Expression<Func<T, TKey>> keySelector);
		}

		public interface IWindowFrameExtent<T>
		{
			[FunctionChain]
			IReadyToFunction<T> UnboundedPreceding { get; }

			[FunctionChain]
			IWindowFrameBetween<T> Between { get; }
		}

		public interface IWindowFrameBetween<T>
		{
			[FunctionChain]
			IWindowFrameBetweenNext<T> UnboundedPreceding { get; }

			[FunctionChain]
			IValueExprFirst<T> Value<TKey>(Expression<Func<T, TKey>> valueExpression);
		}

		public interface IValueExprFirst<T>
		{
			[FunctionChain]
			IWindowFrameBetweenNext<T> Preceding { get; }

			[FunctionChain]
			IWindowFrameBetweenNext<T> Following { get; }

			[FunctionChain]
			IWindowFrameFollowing<T> And { get; }
		}

		public interface IWindowFrameBetweenNext<T>
		{
			[FunctionChain]
			IWindowFrameFollowing<T> And { get; }
		}

		public interface IWindowFrameFollowing<T>
		{
			[FunctionChain]
			IReadyToFunction<T> UnboundedFollowing { get; }

			[FunctionChain]
			IReadyToFunction<T> CurrentRow { get; }

			[FunctionChain]
			IValueExprSecond<T> Value<TKey>(Expression<Func<T, TKey>> valueExpression);
		}

		public interface IValueExprSecond<T>
		{
			[FunctionChain]
			IReadyToFunction<T> Preceding { get; }
			[FunctionChain]
			IReadyToFunction<T> Following { get; }
		}

	}

}