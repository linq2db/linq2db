using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Linq;

namespace LinqToDB
{
	public static class WindowFunctionBuilder
	{
		public interface IDefinedFunction<out TR> { }
		public interface IDefinedFunction { }

		public interface IFilterPart<out TFiltered>
			where TFiltered : class
		{
			TFiltered Filter(bool filter);
		}

		public interface IOrderByPart<out TThenPart>
			where TThenPart : class
		{
			TThenPart OrderBy(object?     orderBy);
			TThenPart OrderBy(object?     orderBy, Sql.NullsPosition nulls);
			TThenPart OrderByDesc(object? orderBy);
			TThenPart OrderByDesc(object? orderBy, Sql.NullsPosition nulls);
		}

		public interface IOnlyOrderByPart
		{
			IDefinedFunction<TValue> OrderBy<TValue>(TValue     orderBy);
			IDefinedFunction<TValue> OrderBy<TValue>(TValue     orderBy, Sql.NullsPosition nulls);
			IDefinedFunction<TValue> OrderByDesc<TValue>(TValue orderBy);
			IDefinedFunction<TValue> OrderByDesc<TValue>(TValue orderBy, Sql.NullsPosition nulls);
		}

		public interface IMultipleOrderByPart
		{
			IMultipleThenByPart<TValue> OrderBy<TValue>(TValue     orderBy);
			IMultipleThenByPart<TValue> OrderBy<TValue>(TValue     orderBy, Sql.NullsPosition nulls);
			IMultipleThenByPart<TValue> OrderByDesc<TValue>(TValue orderBy);
			IMultipleThenByPart<TValue> OrderByDesc<TValue>(TValue orderBy, Sql.NullsPosition nulls);
		}

		public interface IMultipleThenByPart<out TValue> : IDefinedFunction<TValue>
		{
			IMultipleThenByPart<TValue> ThenBy(object?     orderBy);
			IMultipleThenByPart<TValue> ThenBy(object?     orderBy, Sql.NullsPosition nulls);
			IMultipleThenByPart<TValue> ThenByDesc(object? orderBy);
			IMultipleThenByPart<TValue> ThenByDesc(object? orderBy, Sql.NullsPosition nulls);
		}

		public interface IThenOrderPart<out TThenPart>
			where TThenPart : class
		{
			TThenPart ThenBy(object?     orderBy);
			TThenPart ThenBy(object?     orderBy, Sql.NullsPosition nulls);
			TThenPart ThenByDesc(object? orderBy);
			TThenPart ThenByDesc(object? orderBy, Sql.NullsPosition nulls);
		}

		public interface IPartitionPart<out TPartitioned>
		where TPartitioned: class
		{
			TPartitioned PartitionBy(params object?[] partitionBy);
		}

		public interface IDefinedWindow {}

		public interface IFramePartFunction
		{
			/*
			IBoundaryPart<IDefinedFunction> Rows   { get; }
			IBoundaryPart<IDefinedFunction> Range  { get; }
			IBoundaryPart<IDefinedFunction> Groups { get; }
			*/

			IBoundaryPart<IRangePrecedingPartFunction> RowsBetween   { get; }
			IBoundaryPart<IRangePrecedingPartFunction> RangeBetween  { get; }
			IBoundaryPart<IRangePrecedingPartFunction> GroupsBetween { get; }
		}

		public interface IBoundaryPart<TBoundaryDefined>
		{
			TBoundaryDefined Unbounded  { get; }
			TBoundaryDefined CurrentRow { get; }
			TBoundaryDefined Value(object? preceding);
			TBoundaryDefined Value(object? preceding, Sql.NullsPosition nulls);
		}

		public interface IRangePrecedingPartFunction
		{
			IBoundaryPart<IDefinedRangeFrameFunction> And { get; }
		}

		public interface IDefinedRangeFrameFunction : IDefinedFunction
		{
			public IDefinedFunction ExcludeCurrentRow();
			public IDefinedFunction ExcludeGroup();
			public IDefinedFunction ExcludeTies();
		}

		public interface IOptionalFilter<out TPartitioned> : IFilterPart<IPartitionPart<TPartitioned>>, IPartitionPart<TPartitioned>
			where TPartitioned : class
		{
		}

		public interface IThenByPartFinal : IThenOrderPart<IThenByPartFinal>, IDefinedFunction
		{

		}

		public interface IROrderByPartOThenByPartFinal : IOrderByPart<IThenByPartFinal>
		{
		}

		public interface IArgumentPart<TWithArgument>
		where TWithArgument : class
		{
			TWithArgument Argument(object?               argument);
			TWithArgument Argument(Sql.AggregateModifier modifier, object? argument);
		}

		public interface IUseWindow<TWithWindowPart>
		{
			public TWithWindowPart UseWindow(IDefinedWindow window);
		}

		#region Window

		public interface IWindowBuilder : IOPartitionOOrderOFrameFinal
		{

		}

		public static IDefinedWindow DefineWindow(this Sql.IWindowFunction window, Func<IWindowBuilder, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(DefineWindow));

		#endregion

		#region Predefined chains

		// ROW_NUMBER()
		// RANK()
		// DENSE_RANK()
		// PERCENT_RANK()
		// CUME_DIST()
		// NTILE(n)
		public interface IOPartitionROrderFinal : IUseWindow<IDefinedFunction>, IPartitionPart<IROrderByPartOThenByPartFinal>, IROrderByPartOThenByPartFinal
		{
		}

		// COUNT
		public interface IOArgumentOFilterOPartitionOOrderFinal : IArgumentPart<IOFilterOPartitionOOrderFinal>, IOFilterOPartitionOOrderFinal
		{
		}

		public interface IOThenPartFinal : IThenOrderPart<IOThenPartFinal>, IDefinedFunction
		{
		}

		public interface IOOrderFinal : IOrderByPart<IOThenPartFinal>, IOThenPartFinal
		{
		}

		public interface IOPartitionOOrderFinal : IPartitionPart<IOOrderFinal>, IOOrderFinal
		{
		}

		public interface IOFilterOPartitionOOrderFinal : IFilterPart<IOPartitionOOrderFinal>, IOPartitionOOrderFinal
		{
		}

		public interface IOFilterOPartitionFinal : IFilterPart<IOPartitionFinal>, IOPartitionFinal
		{
		}

		public interface IOPartitionFinal : IPartitionPart<IDefinedFunction>, IDefinedFunction
		{
		}

		// SUM, AVERAGE
		public interface IOFilterOPartitionOOrderOFrameFinal : IFilterPart<IOPartitionOOrderOFrameFinal>, IOPartitionOOrderOFrameFinal, IUseWindow<IDefinedFunction>
		{
		}

		public interface IOPartitionOOrderOFrameFinal : IPartitionPart<IOrderOFrameFinal>, IOrderOFrameFinal
		{
		}

		public interface IOFrameFinal : IFramePartFunction, IDefinedFunction
		{
		}

		public interface IOThenPartOFrameFinal : IThenOrderPart<IOThenPartOFrameFinal>, IOFrameFinal
		{
		}

		public interface IOrderOFrameFinal : IOrderByPart<IOThenPartOFrameFinal>, IOFrameFinal
		{
		}

		#endregion Predefined chains

		// public static object DefineWindow(this Sql.IWindowFunction window, Func<IWindowDefinition, object> func)
		// 	=> throw new ServerSideOnlyException(nameof(RowNumber))

		#region Optional Partition, Mandatory Order, No Filter, No Frame

		public static long RowNumber(this Sql.IWindowFunction window, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(RowNumber));

		public static int Rank(this Sql.IWindowFunction window, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Rank));

		public static long DenseRank(this Sql.IWindowFunction window, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(DenseRank));

		public static double PercentRank(this Sql.IWindowFunction window, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(PercentRank));

		public static double CumeDist(this Sql.IWindowFunction window, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(CumeDist));

		public static long NTile(this Sql.IWindowFunction window, int n, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(NTile));

		#endregion

		public static int Count(this Sql.IWindowFunction window, Func<IOArgumentOFilterOPartitionOOrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Count));

		#region Sum

		public static int Sum(this Sql.IWindowFunction window, int argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static int? Sum(this Sql.IWindowFunction window, int? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static long Sum(this Sql.IWindowFunction window, long argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static long? Sum(this Sql.IWindowFunction window, long? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static double Sum(this Sql.IWindowFunction window, double argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static double? Sum(this Sql.IWindowFunction window, double? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static decimal Sum(this Sql.IWindowFunction window, decimal argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static decimal? Sum(this Sql.IWindowFunction window, decimal? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static float Sum(this Sql.IWindowFunction window, float argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static float? Sum(this Sql.IWindowFunction window, float? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		#endregion Sum

		#region Average

		public static double Average(this Sql.IWindowFunction window, int argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double? Average(this Sql.IWindowFunction window, int? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double Average(this Sql.IWindowFunction window, long argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double? Average(this Sql.IWindowFunction window, long? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double Average(this Sql.IWindowFunction window, double argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double? Average(this Sql.IWindowFunction window, double? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static decimal Average(this Sql.IWindowFunction window, decimal argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static decimal? Average(this Sql.IWindowFunction window, decimal? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static float Average(this Sql.IWindowFunction window, float argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static float? Average(this Sql.IWindowFunction window, float? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double Average(this Sql.IWindowFunction window, short argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double? Average(this Sql.IWindowFunction window, short? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double Average(this Sql.IWindowFunction window, byte argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double? Average(this Sql.IWindowFunction window, byte? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		#endregion Average

		#region Min

		public static int Min(this Sql.IWindowFunction window, int argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		public static int? Min(this Sql.IWindowFunction window, int? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		public static long Min(this Sql.IWindowFunction window, long argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		public static long? Min(this Sql.IWindowFunction window, long? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		public static double Min(this Sql.IWindowFunction window, double argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		public static double? Min(this Sql.IWindowFunction window, double? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		public static decimal Min(this Sql.IWindowFunction window, decimal argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		public static decimal? Min(this Sql.IWindowFunction window, decimal? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		public static float Min(this Sql.IWindowFunction window, float argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		public static float? Min(this Sql.IWindowFunction window, float? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		public static short Min(this Sql.IWindowFunction window, short argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		public static short? Min(this Sql.IWindowFunction window, short? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		public static byte Min(this Sql.IWindowFunction window, byte argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		public static byte? Min(this Sql.IWindowFunction window, byte? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		#endregion Min

		#region Max

		public static int Max(this Sql.IWindowFunction window, int argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		public static int? Max(this Sql.IWindowFunction window, int? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		public static long Max(this Sql.IWindowFunction window, long argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		public static long? Max(this Sql.IWindowFunction window, long? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		public static double Max(this Sql.IWindowFunction window, double argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		public static double? Max(this Sql.IWindowFunction window, double? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		public static decimal Max(this Sql.IWindowFunction window, decimal argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		public static decimal? Max(this Sql.IWindowFunction window, decimal? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		public static float Max(this Sql.IWindowFunction window, float argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		public static float? Max(this Sql.IWindowFunction window, float? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		public static short Max(this Sql.IWindowFunction window, short argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		public static short? Max(this Sql.IWindowFunction window, short? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		public static byte Max(this Sql.IWindowFunction window, byte argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		public static byte? Max(this Sql.IWindowFunction window, byte? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		#endregion Max

		#region Percenile Cont

#pragma warning disable RS0030

		public static TValue PercentileCont<TElement, TValue>(
			this IEnumerable<TElement>                                 source,
			double                                                     argument,
			Func<TElement, IOnlyOrderByPart, IDefinedFunction<TValue>> func
		)
			=> throw new ServerSideOnlyException(nameof(PercentileCont));

		public static TValue PercentileCont<TElement, TValue>(
			this IQueryable<TElement> source,
			double                                                                 argument,
			Expression<Func<TElement, IOnlyOrderByPart, IDefinedFunction<TValue>>> func
		)
		{
			var currentSource   = source.ProcessIQueryable();
			var queryExpression = WindowFunctionHelpers.BuildAggregateExecuteExpression<TElement, TValue>(source, q => q.PercentileCont(argument, func.Compile()));

			return currentSource.Provider.Execute<TValue>(queryExpression);
		}

		public static Task<TValue> PercentileContAsync<TElement, TValue>(
			this IQueryable<TElement> source,
			double                                                                 argument,
			Expression<Func<TElement, IOnlyOrderByPart, IDefinedFunction<TValue>>> func,
			CancellationToken cancellationToken = default
		)
		{
			var currentSource   = source.GetLinqToDBSource();
			var queryExpression = WindowFunctionHelpers.BuildAggregateExecuteExpression<TElement, TValue>(source, q => q.PercentileCont(argument, func.Compile()));

			return currentSource.ExecuteAsync<TValue>(queryExpression, cancellationToken);
		}

#pragma warning restore RS0030

		#endregion

		#region Percenile Disc

#pragma warning disable RS0030

		public static TValue PercentileDisc<TElement, TValue>(
			this IEnumerable<TElement>                                     source,
			double                                                         argument,
			Func<TElement, IMultipleOrderByPart, IDefinedFunction<TValue>> func
		) => throw new ServerSideOnlyException(nameof(PercentileDisc));

		public static TValue PercentileDisc<TElement, TValue>(
			this IQueryable<TElement>                                                  source,
			double                                                                     argument,
			Expression<Func<TElement, IMultipleOrderByPart, IDefinedFunction<TValue>>> func
		)
		{
			var currentSource   = source.ProcessIQueryable();
			var queryExpression = WindowFunctionHelpers.BuildAggregateExecuteExpression<TElement, TValue>(source, q => q.PercentileDisc(argument, func.Compile()));

			return currentSource.Provider.Execute<TValue>(queryExpression);
		}

		public static Task<TValue> PercentileDiscAsync<TElement, TValue>(
			this IQueryable<TElement>                                                  source,
			double                                                                     argument,
			Expression<Func<TElement, IMultipleOrderByPart, IDefinedFunction<TValue>>> func,
			CancellationToken cancellationToken = default
		)
		{
			var currentSource   = source.GetLinqToDBSource();
			var queryExpression = WindowFunctionHelpers.BuildAggregateExecuteExpression<TElement, TValue>(source, q => q.PercentileDisc(argument, func.Compile()));

			return currentSource.ExecuteAsync<TValue>(queryExpression, cancellationToken);
		}

#pragma warning restore RS0030

		#endregion

	}

	partial class Sql
	{
		// Will be removed after completion of the task
		static void Test()
		{
			var query =
				from t in new[] { new { Id = 1, Name = "John" } }
				//let w = Sql.Window.DefineWindow(w => w.Filter(t.Name.StartsWith("Jo")).PartitionBy(t.Name).OrderBy(t.Id))
				select new
				{
					t.Id,
					RNPO = Window.RowNumber(w => w.PartitionBy(t.Name).OrderBy(t.Id)),
					RNO2 = Window.RowNumber(w => w
						.OrderByDesc(t.Id)
						.ThenBy(t.Name)),
					RN01 = Window.RowNumber(w => w.OrderBy(t.Id)),

					Sum      = Window.Sum(t.Id, w => w.PartitionBy(t.Name).OrderBy(t.Id).ThenBy(t.Name)),

					CountP    = Window.Count(w => w.PartitionBy(t.Name)),
					CountPArg = Window.Count(w => w.Argument(t.Id).PartitionBy(t.Name)),
					CountArg = Window.Count(w => w.Argument(AggregateModifier.Distinct, t.Id).PartitionBy(t.Name).OrderBy(t.Id).ThenBy(t.Name)),
				};

			var groupQuery =
				from t in new[] { new { Id = 1, Name = "John" } }
				group t by t.Id into g
				select new
				{
					g.Key,
					PC = g.PercentileCont(0.5, (e, f) => f.OrderBy(e.Id)),
					PD = g.PercentileDisc(0.5, (e, f) => f.OrderBy(e.Id).ThenBy(e.Name))
				};
		}
	}
}
