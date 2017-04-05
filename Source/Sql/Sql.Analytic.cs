namespace LinqToDB
{
	using System;
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
			string				Configuration { get; }
			ISqlExpression      GetArgument(int index);
			ISqlExpression[]    GetArrayArgument(int index);
			T                   GetValue<T>(int index);
			SqlAnalyticFunction Function { get; }
		}

		public static IOver Over
		{
			get
			{
				throw new NotImplementedException();
			}
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

		private static TR TwoExprDefaultBuilder<TR>(object window)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.Arguments.Add(builder.GetArgument(0));
			builder.Function.Arguments.Add(builder.GetArgument(1));

			return default(TR);
		}

		private static TR AggregateExprDefaultBuilder<TR>(object window, string funcName, AggregateModifier modifier)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.Arguments.Add(builder.GetArgument(0));

			switch (builder.GetValue<AggregateModifier>(1))
			{
				case AggregateModifier.None :
					builder.Function.Expression = funcName + "({0})";
					break;
				case AggregateModifier.Distinct :
					builder.Function.Expression = funcName + "(DISTINCT {0})";
					break;
				case AggregateModifier.All :
					builder.Function.Expression = funcName + "(ALL {0})";
					break;
				default :
					throw new ArgumentOutOfRangeException();
			}

			return default(TR);
		}

		private static TR SingleExpressionDefaultBuilder<TR>(object window)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.Arguments.Add(builder.GetArgument(0));

			return default(TR);
		}

		[AnalyticFunction("AVG({0})")]
		public static TR Average<TR>(this IReadyToFunction window, object expr)
		{
			return AggregateExprDefaultBuilder<TR>(window, "AVG", AggregateModifier.None);
		}

		[AnalyticFunction("AVG({0})")]
		public static TR Average<TR>(this IReadyToFunction window, object expr, AggregateModifier modifier)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			return AggregateExprDefaultBuilder<TR>(window, "AVG", builder.GetValue<AggregateModifier>(1));
		}

		[AnalyticFunction("CORR({0}, {1})")]
		public static TR Corr<TR>(this IReadyToFunction window, object expr1, object expr2)
		{
			return TwoExprDefaultBuilder<TR>(window);
		}

		#region Count

		[AnalyticFunction("COUNT(*)")]
		public static long Count(this IReadyToFunction window)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("COUNT({0})")]
		public static long Count<TR>(this IReadyToFunction window, TR expr)
		{
			return SingleExpressionDefaultBuilder<long>(window);
		}

		[AnalyticFunction("COUNT({0})")]
		public static long Count<TR>(this IReadyToFunction window, TR expr, AggregateModifier modifier)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			return AggregateExprDefaultBuilder<long>(window, "COUNT", builder.GetValue<AggregateModifier>(1));
		}

		#endregion

		[AnalyticFunction("COVAR_POP({0}, {1})")]
		public static TR CovarPop<TR>(this IReadyToFunction window, TR expr1, TR expr2)
		{
			return TwoExprDefaultBuilder<TR>(window);
		}

		[AnalyticFunction("COVAR_SAMP({0}, {1})")]
		public static TR CovarSamp<TR>(this IReadyToFunction window, TR expr1, TR expr2)
		{
			return TwoExprDefaultBuilder<TR>(window);
		}

		[AnalyticFunction("CUME_DIST()")]
		public static double CumeDist(this IOrdered window)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("DENSE_RANK()")]
		public static decimal DenseRank(this IOrdered window)
		{
			throw new NotImplementedException();
		}

		//TODO: FIRST - http://docs.oracle.com/cloud/latest/db112/SQLRF/functions065.htm#SQLRF00641

		[AnalyticFunction("FIRST_VALUE({0})")]
		public static TR FirstValue<TR>(this IReadyToFunction window, TR expr, Nulls nulls)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.Arguments.Add(builder.GetArgument(0));

			switch (builder.GetValue<Nulls>(1))
			{
				case Nulls.None :
					break;
				case Nulls.Respect :
					builder.Function.Expression = "FIRST_VALUE({0} RESPECT NULLS)";
					break;
				case Nulls.Ignore :
					builder.Function.Expression = "FIRST_VALUE({0} IGNORE NULLS)";
					break;
				default :
					throw new ArgumentOutOfRangeException();
			}

			return default(TR);
		}

		[AnalyticFunction("LAG({0})")]
		public static TR Lag<TR>(this IOrdered window, TR expr, Nulls nulls)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.Arguments.Add(builder.GetArgument(0));

			switch (builder.GetValue<Nulls>(1))
			{
				case Nulls.None :
					break;
				case Nulls.Respect :
					builder.Function.Expression = "LAG({0} RESPECT NULLS)";
					break;
				case Nulls.Ignore :
					builder.Function.Expression = "LAG({0} IGNORE NULLS)";
					break;
				default :
					throw new ArgumentOutOfRangeException();
			}

			return default(TR);
		}

		[AnalyticFunction("LAG")]
		public static TR Lag<TR>(this IOrdered window, TR expr, Nulls nulls, int offset, int? @default)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.Arguments.Add(builder.GetArgument(0));
			builder.Function.Arguments.Add(builder.GetArgument(2));
			builder.Function.Arguments.Add(builder.GetArgument(3));

			string expression;

			switch (builder.GetValue<Nulls>(1))
			{
				case Nulls.None :
					expression = "LAG({0}";
					break;
				case Nulls.Respect :
					expression = "LAG({0} RESPECT NULLS";
					break;
				case Nulls.Ignore :
					expression = "LAG({0} IGNORE NULLS";
					break;
				default :
					throw new ArgumentOutOfRangeException();
			}

			expression = expression + ", {1}, {2})";
			builder.Function.Expression = expression;

			return default(TR);
		}

		//TODO: LAST - http://docs.oracle.com/cloud/latest/db112/SQLRF/functions083.htm#SQLRF00653

		[AnalyticFunction("LAST_VALUE({0})")]
		public static TR LastValue<TR>(this IReadyToFunction window, TR expr, Nulls nulls)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.Arguments.Add(builder.GetArgument(0));

			switch (builder.GetValue<Nulls>(1))
			{
				case Nulls.None :
					break;
				case Nulls.Respect :
					builder.Function.Expression = "LAST_VALUE({0} RESPECT NULLS)";
					break;
				case Nulls.Ignore :
					builder.Function.Expression = "LAST_VALUE({0} IGNORE NULLS)";
					break;
				default :
					throw new ArgumentOutOfRangeException();
			}

			return default(TR);
		}

		[AnalyticFunction("LEAD({0})")]
		public static TR Lead<TR>(this IOrdered window, TR expr, Nulls nulls)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.Arguments.Add(builder.GetArgument(0));

			switch (builder.GetValue<Nulls>(1))
			{
				case Nulls.None :
					break;
				case Nulls.Respect :
					builder.Function.Expression = "LEAD({0} RESPECT NULLS)";
					break;
				case Nulls.Ignore :
					builder.Function.Expression = "LEAD({0} IGNORE NULLS)";
					break;
				default :
					throw new ArgumentOutOfRangeException();
			}

			return default(TR);
		}

		[AnalyticFunction("LEAD({0})")]
		public static TR Lead<TR>(this IOrdered window, TR expr, Nulls nulls, int offset, int? @default)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.Arguments.Add(builder.GetArgument(0));
			builder.Function.Arguments.Add(builder.GetArgument(2));
			builder.Function.Arguments.Add(builder.GetArgument(3));

			string expression;

			switch (builder.GetValue<Nulls>(1))
			{
				case Nulls.None :
					expression = "LEAD({0}";
					break;
				case Nulls.Respect :
					expression = "LEAD({0} RESPECT NULLS";
					break;
				case Nulls.Ignore :
					expression = "LEAD({0} IGNORE NULLS";
					break;
				default :
					throw new ArgumentOutOfRangeException();
			}

			expression = expression + ", {1}, {2})";
			builder.Function.Expression = expression;

			return default(TR);
		}

		//TODO: LISTAGG

//		[AnalyticFunction("LISTAGG({0})")]
//		public static string ListAgg<TR>(this IReadyToFunction window, TR expr)
//		{
//			var builder = window as IAnalyticFunctionBuilder;
//			if (builder == null)
//				throw new NotImplementedException();
//
//			builder.Function.Arguments.Add(builder.GetArgument(0));
//			
//			return default(string);
//		}
//
//		[AnalyticFunction("LISTAGG({0}, {1})")]
//		public static string ListAgg<TR>(this IReadyToFunction window, TR expr, string delimiter)
//		{
//			var builder = window as IAnalyticFunctionBuilder;
//			if (builder == null)
//				throw new NotImplementedException();
//
//			builder.Function.Arguments.Add(builder.GetArgument(0));
//			builder.Function.Arguments.Add(builder.GetArgument(1));
//			
//			return default(string);
//		}

		[AnalyticFunction("MAX({0})")]
		public static TR Max<TR>(this IReadyToFunction window, TR expr)
		{
			return SingleExpressionDefaultBuilder<TR>(window);
		}

		[AnalyticFunction("MAX({0})")]
		public static TR Max<TR>(this IReadyToFunction window, TR expr, AggregateModifier modifier)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			return AggregateExprDefaultBuilder<TR>(window, "MAX", builder.GetValue<AggregateModifier>(1));
		}

		[AnalyticFunction("MEDIAN({0})")]
		public static TR Median<TR>(this INotOrdered window, TR expr)
		{
			return SingleExpressionDefaultBuilder<TR>(window);
		}

		[AnalyticFunction("MIN({0})")]
		public static TR Min<TR>(this IReadyToFunction window, TR expr)
		{
			return SingleExpressionDefaultBuilder<TR>(window);
		}

		[AnalyticFunction("MIN({0})")]
		public static TR Min<TR>(this IReadyToFunction window, TR expr, AggregateModifier modifier)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			return AggregateExprDefaultBuilder<TR>(window, "MIN", builder.GetValue<AggregateModifier>(1));
		}

		[AnalyticFunction("NTH_VALUE({0}, {1})")]
		public static TR NthValue<TR>(this IReadyToFunction window, TR expr, int n)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.Arguments.Add(builder.GetArgument(0));
			builder.Function.Arguments.Add(builder.GetArgument(1));
			
			return default(TR);
		}

		[AnalyticFunction("NTH_VALUE({0}, {1})")]
		public static TR NthValue<TR>(this IReadyToFunction window, TR expr, int n, From from, Nulls nulls)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			builder.Function.Arguments.Add(builder.GetArgument(0));
			builder.Function.Arguments.Add(builder.GetArgument(1));

			var expression = builder.Function.Expression;
			switch (builder.GetValue<From>(2))
			{
				case From.None :
					break;
				case From.First :
					expression = expression + " FROM FIRST";
					break;
				case From.Last :
					expression = expression + " FROM LAST";
					break;
				default :
					throw new ArgumentOutOfRangeException();
			}

			switch (builder.GetValue<Nulls>(3))
			{
				case Nulls.None :
					break;
				case Nulls.Respect :
					expression = expression + " RESPECT NULLS";
					break;
				case Nulls.Ignore :
					expression = expression + " IGNORE NULLS";
					break;
				default :
					throw new ArgumentOutOfRangeException();
			}

			builder.Function.Expression = expression;
			
			return default(TR);
		}

		[AnalyticFunction("NTILE({0})")]
		public static long NTile(this IReadyToFunction window, int groupCount)
		{
			return SingleExpressionDefaultBuilder<long>(window);
		}

		[AnalyticFunction("PERCENT_RANK()")]
		public static long PercentRank(this IReadyToFunction window)
		{
			throw new NotImplementedException();
		}

		//TODO: PERCENTILE_CONT
//		[AnalyticFunction("PERCENTILE_CONT({0})")]
//		public static double PercentileCont<T>(this IReadyToFunction window, Expression<Func<T, object>> expr)
//		{
//			return SingleExpressionDefaultBuilder<double>(window);
//		}

		//TODO: PERCENTILE_DISC
//		[AnalyticFunction("PERCENTILE_DISC({0})")]
//		public static double PercentileDisc<T>(this IReadyToFunction window, Expression<Func<T, object>> expr)
//		{
//			return SingleExpressionDefaultBuilder<double>(window);
//		}

		[AnalyticFunction("RANK()")]
		public static long Rank(this IOrdered window)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("RATIO_TO_REPORT({0})")]
		public static double? RatioToReport<TR>(this INotOrdered window, TR expr)
		{
			return SingleExpressionDefaultBuilder<double?>(window);
		}

		//TODO: regr functions
//		#region REGR_ (Linear Regression) Functions 
//
//		static TR RegrHandler<TR>(this IReadyToFunction window)
//		{
//			return TwoExprDefaultBuilder<TR>(window);
//		}
//
//		[AnalyticFunction("REGR_SLOPE({0}, {1})")]
//		public static TR RegrSlope<TR>(this IReadyToFunction window, TR expr1, TR expr2)
//		{
//			return RegrHandler<TR>(window);
//		}
//		
//		[AnalyticFunction("REGR_INTERCEPT({0}, {1})")]
//		public static TR RegrIntercept<TR>(this IReadyToFunction window, TR expr1, TR expr2)
//		{
//			return RegrHandler<TR>(window);
//		}
//		
//		[AnalyticFunction("REGR_COUNT({0}, {1})")]
//		public static TR RegrCount<TR>(this IReadyToFunction window, TR expr1, TR expr2)
//		{
//			return RegrHandler<TR>(window);
//		}
//		
//		[AnalyticFunction("REGR_R2({0}, {1})")]
//		public static TR RegrR2<TR>(this IReadyToFunction window, TR expr1, TR expr2)
//		{
//			return RegrHandler<TR>(window);
//		}
//		
//		[AnalyticFunction("REGR_AVGX({0}, {1})")]
//		public static TR RegrAvgX<TR>(this IReadyToFunction window, TR expr1, TR expr2)
//		{
//			return RegrHandler<TR>(window);
//		}
//		
//		[AnalyticFunction("REGR_AVGY({0}, {1})")]
//		public static TR RegrAvgY<TR>(this IReadyToFunction window, TR expr1, TR expr2)
//		{
//			return RegrHandler<TR>(window);
//		}
//		
//		[AnalyticFunction("REGR_SXX({0}, {1})")]
//		public static TR RegrSXX<TR>(this IReadyToFunction window, TR expr1, TR expr2)
//		{
//			return RegrHandler<TR>(window);
//		}
//		
//		[AnalyticFunction("REGR_SYY({0}, {1})")]
//		public static TR RegrSYY<TR>(this IReadyToFunction window, TR expr1, TR expr2)
//		{
//			return RegrHandler<TR>(window);
//		}
//		
//		[AnalyticFunction("REGR_SXY({0}, {1})")]
//		public static TR RegrSXY<TR>(this IReadyToFunction window, TR expr1, TR expr2)
//		{
//			return RegrHandler<TR>(window);
//		}
//		
//		#endregion

		[AnalyticFunction("ROW_NUMBER()")]
		public static long RowNumber(this IReadyToFunction window)
		{
			throw new NotImplementedException();
		}

		[AnalyticFunction("STDDEV({0})")]
		public static TR StdDev<TR>(this IReadyToFunction window, TR expr)
		{
			return SingleExpressionDefaultBuilder<TR>(window);
		}

		[AnalyticFunction("STDDEV({0})")]
		public static TR StdDev<TR>(this IReadyToFunction window, TR expr, AggregateModifier modifier)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			return AggregateExprDefaultBuilder<TR>(window, "STDDEV", builder.GetValue<AggregateModifier>(1));
		}

		[AnalyticFunction("STDDEV_POP({0})")]
		public static TR StdDevPop<TR>(this IReadyToFunction window, TR expr)
		{
			return SingleExpressionDefaultBuilder<TR>(window);
		}

		[AnalyticFunction("STDDEV_SAMP({0})")]
		public static TR StdDevSamp<TR>(this IReadyToFunction window, TR expr)
		{
			return SingleExpressionDefaultBuilder<TR>(window);
		}

		[AnalyticFunction("SUM({0})")]
		public static TR Sum<TR>(this IReadyToFunction window, TR expr)
		{
			return SingleExpressionDefaultBuilder<TR>(window);
		}

		[AnalyticFunction("SUM({0})")]
		public static TR Sum<TR>(this IReadyToFunction window, TR expr, AggregateModifier modifier)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			return AggregateExprDefaultBuilder<TR>(window, "SUM", builder.GetValue<AggregateModifier>(1));
		}

		[AnalyticFunction("VAR_POP({0})")]
		public static TR VarPop<TR>(this IReadyToFunction window, TR expr)
		{
			return SingleExpressionDefaultBuilder<TR>(window);
		}

		[AnalyticFunction("VAR_SAMP({0})")]
		public static TR VarSamp<TR>(this IReadyToFunction window, TR expr)
		{
			return SingleExpressionDefaultBuilder<TR>(window);
		}

		[AnalyticFunction("VARIANCE({0})")]
		public static TR Variance<TR>(this IReadyToFunction window, TR expr)
		{
			return SingleExpressionDefaultBuilder<TR>(window);
		}

		[AnalyticFunction("VARIANCE({0})")]
		public static TR Variance<TR>(this IReadyToFunction window, TR expr, AggregateModifier modifier)
		{
			var builder = window as IAnalyticFunctionBuilder;
			if (builder == null)
				throw new NotImplementedException();

			return AggregateExprDefaultBuilder<TR>(window, "VARIANCE", builder.GetValue<AggregateModifier>(1));
		}

		public interface IReadyToFunction
		{
		}

		public interface INotOrdered
		{
		}

		public interface IReadyToWindow
		{
			IWindowFrameExtent Rows { get; }
			IWindowFrameExtent Range { get; }
		}

		public interface IOver : IReadyToOrderWithoutPartition, IReadyToFunction
		{
			IPartitionedReadyToOrder PartitionBy(params object[] expressions);
		}

		public interface IPartitioned
		{
		}

		public interface IOrdered
		{
			
		}

		public interface IReadyToOrderWithoutPartition : INotOrdered
		{
			IOrderedWithoutPartition OrderBy<TKey>(TKey keySelector);
			IOrderedWithoutPartition OrderBy<TKey>(TKey keySelector, NullsPosition nulls);

			IOrderedWithoutPartition OrderByDesc<TKey>(TKey keySelector);
			IOrderedWithoutPartition OrderByDesc<TKey>(TKey keySelector, NullsPosition nulls);

			IOrderedWithoutPartition OrderSiblingsBy<TKey>(TKey keySelector);
			IOrderedWithoutPartition OrderSiblingsBy<TKey>(TKey keySelector, NullsPosition nulls);

			IOrderedWithoutPartition OrderSiblingsByDesc<TKey>(TKey keySelector);
			IOrderedWithoutPartition OrderSiblingsByDesc<TKey>(TKey keySelector, NullsPosition nulls);
		}

		public interface IOrderedWithoutPartition : IReadyToFunction, IOrdered
		{
			IOrderedWithoutPartition ThenBy<TKey>(TKey keySelector);
			IOrderedWithoutPartition ThenBy<TKey>(TKey keySelector, NullsPosition nulls);

			IOrderedWithoutPartition ThenByDesc<TKey>(TKey keySelector);
			IOrderedWithoutPartition ThenByDesc<TKey>(TKey keySelector, NullsPosition nulls);
		}

		public interface IPartitionedReadyToOrder : IPartitioned, INotOrdered, IReadyToFunction
		{
			IPartitionOrdered OrderBy<TKey>(TKey keySelector);
			IPartitionOrdered OrderBy<TKey>(TKey keySelector, NullsPosition nulls);

			IPartitionOrdered OrderByDesc<TKey>(TKey keySelector);
			IPartitionOrdered OrderByDesc<TKey>(TKey keySelector, NullsPosition nulls);

			IPartitionOrdered OrderSiblingsBy<TKey>(TKey keySelector);
			IPartitionOrdered OrderSiblingsBy<TKey>(TKey keySelector, NullsPosition nulls);

			IPartitionOrdered OrderSiblingsByDesc<TKey>(TKey keySelector);
			IPartitionOrdered OrderSiblingsByDesc<TKey>(TKey keySelector, NullsPosition nulls);
		}

		public interface IPartitionOrdered : IReadyToFunction, IPartitioned, IReadyToWindow, IOrdered
		{
			IPartitionOrdered ThenBy<TKey>(TKey keySelector);
			IPartitionOrdered ThenBy<TKey>(TKey keySelector, NullsPosition nulls);

			IPartitionOrdered ThenByDesc<TKey>(TKey keySelector);
			IPartitionOrdered ThenByDesc<TKey>(TKey keySelector, NullsPosition nulls);
		}

		public interface IWindowFrameExtent
		{
			IReadyToFunction UnboundedPreceding { get; }
			IWindowFrameBetween Between { get; }
		}

		public interface IWindowFrameBetween
		{
			IWindowFrameBetweenNext UnboundedPreceding { get; }
			IValueExprFirst Value<T>(T value);
		}

		public interface IValueExprFirst
		{
			IWindowFrameBetweenNext Preceding { get; }
			IWindowFrameBetweenNext Following { get; }
			IWindowFrameFollowing And { get; }
		}

		public interface IWindowFrameBetweenNext
		{
			IWindowFrameFollowing And { get; }
		}

		public interface IWindowFrameFollowing
		{
			IReadyToFunction UnboundedFollowing { get; }
			IReadyToFunction CurrentRow { get; }
			IValueExprSecond Value<T>(T value);
		}

		public interface IValueExprSecond
		{
			IReadyToFunction Preceding { get; }
			IReadyToFunction Following { get; }
		}

	}

}