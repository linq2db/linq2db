using System;
using System.Collections.Concurrent;
using System.Linq;

namespace LinqToDB
{
	public static class WindowFunctionBuilder
	{
		public interface IDefinedFunction<out TR> { }
		public interface IDefinedFunction { }

		public interface IFilterPart<TFiltered>
			where TFiltered : class
		{
			TFiltered Filter(bool filter);
		}


		public interface IOrderByPart<in TValue, TThenPart>
			where TThenPart : class
		{
			TThenPart OrderBy(TValue     orderBy);
			TThenPart OrderBy(TValue     orderBy, Sql.NullsPosition nulls);
			TThenPart OrderByDesc(TValue orderBy);
			TThenPart OrderByDesc(TValue orderBy, Sql.NullsPosition nulls);
		}

		public interface IOrderByOThenPart<TValue, TOrdered> : IOrderByPart<TValue, IThenOrderPart<TValue, TOrdered>>
			where TOrdered : class
		{
		}

		public interface IOrderByPart<TOrdered> : IOrderByPart<object?, TOrdered>
			where TOrdered : class
		{
		}

		public interface IThenOrderPart<TOrdered> : IThenOrderPart<object?, TOrdered>
			where TOrdered : class
		{
		}

		public interface IThenOrderPart<in TValue, TOrdered>
			where TOrdered : class
		{
			IThenOrderPart<TValue, TOrdered> ThenBy(TValue     orderBy);
			IThenOrderPart<TValue, TOrdered> ThenBy(TValue     orderBy, Sql.NullsPosition nulls);
			IThenOrderPart<TValue, TOrdered> ThenByDesc(TValue orderBy);
			IThenOrderPart<TValue, TOrdered> ThenByDesc(TValue orderBy, Sql.NullsPosition nulls);
		}

		public interface IPartitionPart<TPartitioned>
		where TPartitioned: class
		{
			TPartitioned PartitionBy(params object?[] partitionBy);
		}

		public interface IDefinedWindow {}

		public interface IFramePart<TFramed>
		where TFramed : class
		{
			TFramed Rows();
		}


		public interface IOptionalFilter<TPartitioned> : IFilterPart<IPartitionPart<TPartitioned>>, IPartitionPart<TPartitioned>
			where TPartitioned : class
		{
		}

		public interface IOptionalOrder<TFramed> : IFramePart<TFramed> 
			where TFramed : class
		{}

		public interface IOptionalFilterPartition<TOrdered> : IFilterPart<IPartitionPart<IOrderByPart<TOrdered>>>, IPartitionPart<IOrderByPart<TOrdered>>, IOrderByPart<TOrdered>
			where TOrdered : class
		{
		}

		public interface IOPartitionROrder<TOrdered> : IPartitionPart<IOrderByPart<TOrdered>>, IOrderByPart<TOrdered>
			where TOrdered : class
		{
		}

		/*
		public interface IWindowDefinition
		{
			IWindowDefinitionFramed      Filter(bool                  filter);
			IWindowDefinitionPartitioned PartitionBy(params object?[] partitionBy);
			IWindowDefinitionOrdered     OrderBy(object?              orderBy);
			IWindowDefinitionOrdered     OrderBy(object?              orderBy, Sql.NullsPosition nulls);
			IWindowDefinitionOrdered     OrderByDesc(object?          orderBy);
			IWindowDefinitionOrdered     OrderByDesc(object?          orderBy, Sql.NullsPosition nulls);
			IWindowDefinitionFramed      Rows();
		}
		*/

		public interface IArgumentPart<TWithArgument>
		where TWithArgument : class
		{
			TWithArgument Argument(object?               argument);
			TWithArgument Argument(Sql.AggregateModifier modifier, object? argument);
		}

		// COUNT
		public interface IOArgumentOFilterOPartitionFinal : IArgumentPart<IOFilterOPartitionFinal>, IOFilterOPartitionFinal
		{
		}

		public interface IOFilterOPartitionFinal : IFilterPart<IOPartitionFinal>, IOPartitionFinal
		{
		}

		public interface IOPartitionFinal : IPartitionPart<IDefinedFunction>, IDefinedFunction
		{
		}

		// SUM, AVERAGE
		public interface IOFilterOPartitionOOrderOFrameFinal : IFilterPart<IOPartitionOOrderOFrameFinal>, IOPartitionOOrderOFrameFinal
		{
		}

		public interface IOPartitionOOrderOFrameFinal : IPartitionPart<IOrderOFrameFinal>, IOrderOFrameFinal
		{
		}

		public interface IOrderOFrameFinal : IOrderByPart<IFramePart<IDefinedFunction>>, IFramePart<IDefinedFunction>, IDefinedFunction
		{
		}


		// public static object DefineWindow(this Sql.IWindowFunction window, Func<IWindowDefinition, object> func)
		// 	=> throw new ServerSideOnlyException(nameof(RowNumber))

		public static long RowNumber(this Sql.IWindowFunction window, Func<IOPartitionROrder<IDefinedFunction>, IThenOrderPart<IDefinedFunction>> func)
			=> throw new ServerSideOnlyException(nameof(RowNumber));

		public static int Count(this Sql.IWindowFunction window, Func<IOArgumentOFilterOPartitionFinal, IDefinedFunction> func)
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

		public static short Sum(this Sql.IWindowFunction window, short argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static short? Sum(this Sql.IWindowFunction window, short? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static byte Sum(this Sql.IWindowFunction window, byte argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static byte? Sum(this Sql.IWindowFunction window, byte? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static sbyte Sum(this Sql.IWindowFunction window, sbyte argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static sbyte? Sum(this Sql.IWindowFunction window, sbyte? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static ushort Sum(this Sql.IWindowFunction window, ushort argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static ushort? Sum(this Sql.IWindowFunction window, ushort? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static uint Sum(this Sql.IWindowFunction window, uint argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static uint? Sum(this Sql.IWindowFunction window, uint? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static ulong Sum(this Sql.IWindowFunction window, ulong argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static ulong? Sum(this Sql.IWindowFunction window, ulong? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
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

		public static double Average(this Sql.IWindowFunction window, sbyte argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double? Average(this Sql.IWindowFunction window, sbyte? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double Average(this Sql.IWindowFunction window, ushort argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double? Average(this Sql.IWindowFunction window, ushort? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double Average(this Sql.IWindowFunction window, uint argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double? Average(this Sql.IWindowFunction window, uint? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double Average(this Sql.IWindowFunction window, ulong argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double? Average(this Sql.IWindowFunction window, ulong? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		#endregion Average

		#region Percenile Cont

		public static TValue PercentileCont<TKey, TElement, TValue>(this IGrouping<TKey, TElement> window, double argument, Func<IOrderByPart<TValue, IDefinedFunction<TValue>>, IDefinedFunction<TValue>> func)
			=> throw new ServerSideOnlyException(nameof(PercentileCont));

		#endregion
	}

	partial class Sql
	{
		public static void Test()
		{
			var query =
				from t in new[] { new { Id = 1, Name = "John" } }
				//let w = Sql.Window.DefineWindow(w => w.Filter(t.Name.StartsWith("Jo")).PartitionBy(t.Name).OrderBy(t.Id))
				select new
				{
					t.Id,
					RNPO = Sql.Window.RowNumber(w => w.PartitionBy(t.Name).OrderBy(t.Id)),
					RNO2 = Sql.Window.RowNumber(w => w
						.OrderByDesc(t.Id)
						.ThenBy(t.Name)),
					RN01 = Sql.Window.RowNumber(w => w.OrderBy(t.Id)),

					Sum      = Sql.Window.Sum(t.Id, w => w.PartitionBy(t.Name).OrderBy(t.Id).ThenBy(t.Name)),

					CountP    = Sql.Window.Count(w => w.PartitionBy(t.Name)),
					CountPArg = Sql.Window.Count(w => w.Argument(t.Id).PartitionBy(t.Name)),
					CountArg = Sql.Window.Count(w => w.Argument(AggregateModifier.Distinct, t.Id).PartitionBy(t.Name).OrderBy(t.Id).ThenBy(t.Name)),
				};

			var groupQuery =
				from t in new[] { new { Id = 1, Name = "John" } }
				group t by t.Id into g
				select new
				{
					g.Key,
					PC = g.PercentileCont(0.5, f => f.OrderBy())
				};
		}
	}
}
