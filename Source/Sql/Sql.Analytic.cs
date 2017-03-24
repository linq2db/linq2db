namespace LinqToDB
{
	using System;
	using System.ComponentModel;
	using System.Linq;
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
			ISqlExpression ConvertExpression(Expression expr);
			ISqlExpression GetArgument(int index);
			SqlAnalyticFunction Function { get; }
		}

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

		static TR SimpleExprHandler<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr", builder.ConvertExpression(expr));

			return default(TR);
		}

		static TR AggregateExprHandler<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, AggregateModifier modifier)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr",     builder.ConvertExpression(expr));
			builder.Function.AddArgument("Modifier", builder.ConvertExpression(expr));

			return default(TR);
		}

		static TR TwoExprHandler<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr1", builder.ConvertExpression(expr1));
			builder.Function.AddArgument("Expr2", builder.ConvertExpression(expr2));

			return default(TR);
		}

		static TR MultipyExprHandler<T, TR>(this IReadyToFunction<T> window, params Expression<Func<T, TR>>[] expressions)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expressions", expressions.Select(e => builder.ConvertExpression(e)).ToList());

			return default(TR);
		}


		[AnalyticFunction("AVG")]
		public static TR Average<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			return SimpleExprHandler(window, expr);
		}

		[AnalyticFunction("AVG")]
		public static TR Average<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, AggregateModifier modifier)
		{
			return AggregateExprHandler(window, expr, modifier);
		}

		[AnalyticFunction("CORR")]
		public static TR Corr<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			return TwoExprHandler(window, expr1, expr2);
		}

		[AnalyticFunction("COUNT")]
		public static long Count<T>(this IReadyToFunction<T> window)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("COUNT")]
		public static long Count<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr", builder.ConvertExpression(expr));

			return default(long);
		}

		[AnalyticFunction("COUNT")]
		public static long Count<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, AggregateModifier modifier)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr",     builder.ConvertExpression(expr));
			builder.Function.AddArgument("Modifier", modifier);

			return default(long);
		}

		[AnalyticFunction("COVAR_POP")]
		public static TR CovarPop<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			return TwoExprHandler(window, expr1, expr2);
		}

		[AnalyticFunction("COVAR_SAMP")]
		public static TR CovarSamp<T, TR>(this IReadyToFunction<T> window, params Expression<Func<T, TR>>[] expressions)
		{
			return MultipyExprHandler(window, expressions);
		}

		[AnalyticFunction("CUME_DIST")]
		public static TR CumeDist<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			return TwoExprHandler(window, expr1, expr2);
		}

		[AnalyticFunction("DENSE_RANK")]
		public static decimal DenseRank<T>(this IReadyToFunction<T> window, params Expression<Func<T, object>>[] expressions)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expressions", expressions.Select(e => builder.ConvertExpression(e)).ToList());

			return default(decimal);
		}

		//TODO: FIRST - http://docs.oracle.com/cloud/latest/db112/SQLRF/functions065.htm#SQLRF00641

		[AnalyticFunction("FIRST_VALUE")]
		public static TR FirstValue<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, SqlAnalyticFunction.Nulls nulls)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr",  builder.ConvertExpression(expr));
			builder.Function.AddArgument("Nulls", nulls);
			
			return default(TR);
		}

		[AnalyticFunction("LAG")]
		public static TR Lag<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, SqlAnalyticFunction.Nulls nulls)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr",  builder.ConvertExpression(expr));
			builder.Function.AddArgument("Nulls", nulls);
			
			return default(TR);
		}

		[AnalyticFunction("LAG")]
		public static TR Lag<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, SqlAnalyticFunction.Nulls nulls, int offset, int? @default)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr",    builder.ConvertExpression(expr));
			builder.Function.AddArgument("Nulls",   nulls);
			builder.Function.AddArgument("Offset",  builder.GetArgument(2));
			builder.Function.AddArgument("Default", builder.GetArgument(3));
			
			return default(TR);
		}

		//TODO: LAST - http://docs.oracle.com/cloud/latest/db112/SQLRF/functions083.htm#SQLRF00653

		[AnalyticFunction("LAST_VALUE")]
		public static TR LastValue<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, SqlAnalyticFunction.Nulls nulls)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr",  builder.ConvertExpression(expr));
			builder.Function.AddArgument("Nulls", nulls);
			
			return default(TR);
		}

		[AnalyticFunction("LEAD")]
		public static TR Lead<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, SqlAnalyticFunction.Nulls nulls)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr",  builder.ConvertExpression(expr));
			builder.Function.AddArgument("Nulls", nulls);
			
			return default(TR);
		}

		[AnalyticFunction("LEAD")]
		public static TR Lead<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, SqlAnalyticFunction.Nulls nulls, int offset, int? @default)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr",    builder.ConvertExpression(expr));
			builder.Function.AddArgument("Nulls",   nulls);
			builder.Function.AddArgument("Offset",  builder.GetArgument(2));
			builder.Function.AddArgument("Default", builder.GetArgument(3));
			
			return default(TR);
		}

		[AnalyticFunction("LISTAGG")]
		public static string ListAgg<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr",  builder.ConvertExpression(expr));
			
			return default(string);
		}

		[AnalyticFunction("LISTAGG")]
		public static string ListAgg<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, string delimiter)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr",      builder.ConvertExpression(expr));
			builder.Function.AddArgument("Delimiter", builder.GetArgument(1));
			
			return default(string);
		}

		[AnalyticFunction("MAX")]
		public static TR Max<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			return SimpleExprHandler(window, expr);
		}

		[AnalyticFunction("MAX")]
		public static TR Max<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, AggregateModifier modifier)
		{
			return AggregateExprHandler(window, expr, modifier);
		}

		[AnalyticFunction("MEDIAN")]
		public static TR Med<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			return SimpleExprHandler(window, expr);
		}

		[AnalyticFunction("MIN")]
		public static TR Min<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			return SimpleExprHandler(window, expr);
		}

		[AnalyticFunction("MIN")]
		public static TR Min<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, AggregateModifier modifier)
		{
			return AggregateExprHandler(window, expr, modifier);
		}

		[AnalyticFunction("NTH_VALUE")]
		public static TR NthValue<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, int n)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr", builder.ConvertExpression(expr));
			builder.Function.AddArgument("N",    builder.GetArgument(1));
			
			return default(TR);
		}

		[AnalyticFunction("NTILE")]
		public static long NTile<T>(this IReadyToFunction<T> window, Expression<Func<T, int>> groupCount)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("GroupCount", builder.ConvertExpression(groupCount));
			
			return default(long);
		}

		[AnalyticFunction("PERCENT_RANK")]
		public static long PercentRank<T>(this IReadyToFunction<T> window, params Expression<Func<T, object>>[] expressions)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expressions", expressions.Select(e => builder.ConvertExpression(e)).ToList());
			
			return default(long);
		}

		[AnalyticFunction("PERCENTILE_CONT")]
		public static double PercentileCont<T>(this IReadyToFunction<T> window, Expression<Func<T, object>> expr)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr", builder.ConvertExpression(expr));
			
			return default(double);
		}

		[AnalyticFunction("PERCENTILE_DISC")]
		public static double PercentileDisc<T>(this IReadyToFunction<T> window, Expression<Func<T, object>> expr)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr", builder.ConvertExpression(expr));
			
			return default(double);
		}

		[AnalyticFunction("RANK")]
		public static long Rank<T>(this IReadyToFunction<T> window)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("RATIO_TO_REPORT")]
		public static double? RatioToReport<T>(this IReadyToFunction<T> window, Expression<Func<T, object>> expr)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.AddArgument("Expr", builder.ConvertExpression(expr));
			
			return default(double?);
		}

		#region REGR_ (Linear Regression) Functions 

		static TR RegrHandler<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1,
			Expression<Func<T, TR>> expr2)
		{
			return TwoExprHandler(window, expr1, expr2);
		}

		[AnalyticFunction("REGR_SLOPE")]
		public static TR RegrSlope<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			return RegrHandler(window, expr1, expr2);
		}
		
		[AnalyticFunction("REGR_INTERCEPT")]
		public static TR RegrIntercept<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			return RegrHandler(window, expr1, expr2);
		}
		
		[AnalyticFunction("REGR_COUNT")]
		public static TR RegrCount<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			return RegrHandler(window, expr1, expr2);
		}
		
		[AnalyticFunction("REGR_R2")]
		public static TR RegrR2<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			return RegrHandler(window, expr1, expr2);
		}
		
		[AnalyticFunction("REGR_AVGX")]
		public static TR RegrAvgX<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			return RegrHandler(window, expr1, expr2);
		}
		
		[AnalyticFunction("REGR_AVGY")]
		public static TR RegrAvgY<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			return RegrHandler(window, expr1, expr2);
		}
		
		[AnalyticFunction("REGR_SXX")]
		public static TR RegrSXX<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			return RegrHandler(window, expr1, expr2);
		}
		
		[AnalyticFunction("REGR_SYY")]
		public static TR RegrSYY<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			return RegrHandler(window, expr1, expr2);
		}
		
		[AnalyticFunction("REGR_SXY")]
		public static TR RegrSXY<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr1, Expression<Func<T, TR>> expr2)
		{
			return RegrHandler(window, expr1, expr2);
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
			return AggregateExprHandler(window, expr, modifier);
		}

		[AnalyticFunction("STDDEV_POP")]
		public static TR StdDevPop<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			return SimpleExprHandler(window, expr);
		}

		[AnalyticFunction("STDDEV_SAMP")]
		public static TR StdDevSamp<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			return SimpleExprHandler(window, expr);
		}

		[AnalyticFunction("SUM")]
		public static TR Sum<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			return SimpleExprHandler(window, expr);
		}

		[AnalyticFunction("SUM")]
		public static TR Sum<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, AggregateModifier modifier)
		{
			return AggregateExprHandler(window, expr, modifier);
		}

		[AnalyticFunction("VAR_POP")]
		public static TR VarPop<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			return SimpleExprHandler(window, expr);
		}

		[AnalyticFunction("VAR_SAMP")]
		public static TR VarSamp<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			return SimpleExprHandler(window, expr);
		}

		[AnalyticFunction("VARIANCE")]
		public static TR Variance<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr)
		{
			return SimpleExprHandler(window, expr);
		}

		[AnalyticFunction("VARIANCE")]
		public static TR Variance<T, TR>(this IReadyToFunction<T> window, Expression<Func<T, TR>> expr, AggregateModifier modifier)
		{
			return AggregateExprHandler(window, expr, modifier);
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