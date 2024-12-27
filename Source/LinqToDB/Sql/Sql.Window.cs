using System;
using System.Linq;

namespace LinqToDB
{
	public static class WindowFunctionBuilder
	{
		public interface IDefinedFunction<out TR> {}

		public interface IWindowDefinition
		{
			IWindowDefinition        Filter(bool                  filter);
			IWindowDefinition        PartitionBy(params object?[] partitionBy);
			IWindowDefinitionOrdered OrderBy(object?              orderBy);
			IWindowDefinitionOrdered OrderBy(object?              orderBy, Sql.NullsPosition nulls);
			IWindowDefinitionOrdered OrderByDesc(object?          orderBy);
			IWindowDefinitionOrdered OrderByDesc(object?          orderBy, Sql.NullsPosition nulls);
		}

		public interface IWindowDefinitionFiltered
		{
			IWindowDefinition        PartitionBy(params object?[] partitionBy);
			IWindowDefinitionOrdered OrderBy(object?              orderBy);
			IWindowDefinitionOrdered OrderBy(object?              orderBy, Sql.NullsPosition nulls);
			IWindowDefinitionOrdered OrderByDesc(object?          orderBy);
			IWindowDefinitionOrdered OrderByDesc(object?          orderBy, Sql.NullsPosition nulls);
		}

		public interface IWindowDefinitionPartitioned
		{
			IWindowDefinitionOrdered OrderBy(object? orderBy);
			IWindowDefinitionOrdered OrderBy(object? orderBy, Sql.NullsPosition nulls);
			IWindowDefinitionOrdered OrderByDesc(object? orderBy);
			IWindowDefinitionOrdered OrderByDesc(object? orderBy, Sql.NullsPosition nulls);
		}

		public interface IWindowDefinitionOrdered
		{
			IWindowDefinitionOrdered ThenBy(object? orderBy);
			IWindowDefinitionOrdered ThenBy(object? orderBy, Sql.NullsPosition nulls);
			IWindowDefinitionOrdered ThenByDesc(object? orderBy);
			IWindowDefinitionOrdered ThenByDesc(object? orderBy, Sql.NullsPosition nulls);
		}

		public interface IWindowFunction<out TR>
		{
			IWindowFunctionFiltered<TR>    Filter(bool                  filter);
			IWindowFunctionPartitioned<TR> PartitionBy(params object?[] partitionBy);
			IWindowFunctionOrdered<TR>     OrderBy(object?              orderBy);
			IWindowFunctionOrdered<TR>     OrderBy(object?              orderBy, Sql.NullsPosition nulls);
			IWindowFunctionOrdered<TR>     OrderByDesc(object?          orderBy);
			IWindowFunctionOrdered<TR>     OrderByDesc(object?          orderBy, Sql.NullsPosition nulls);
		}

		public interface IWindowFunctionFiltered<out TR>
		{
			IWindowFunction<TR>        PartitionBy(params object?[] partitionBy);
			IWindowFunctionOrdered<TR> OrderBy(object?              orderBy);
			IWindowFunctionOrdered<TR> OrderBy(object?              orderBy, Sql.NullsPosition nulls);
			IWindowFunctionOrdered<TR> OrderByDesc(object?          orderBy);
			IWindowFunctionOrdered<TR> OrderByDesc(object?          orderBy, Sql.NullsPosition nulls);
		}

		public interface IWindowFunctionPartitioned<out TR>
		{
			IWindowFunctionOrdered<TR> OrderBy(object?     orderBy);
			IWindowFunctionOrdered<TR> OrderBy(object?     orderBy, Sql.NullsPosition nulls);
			IWindowFunctionOrdered<TR> OrderByDesc(object? orderBy);
			IWindowFunctionOrdered<TR> OrderByDesc(object? orderBy, Sql.NullsPosition nulls);
		}

		public interface IWindowFunctionOrdered<out TR> : IDefinedFunction<TR>
		{
			public IWindowFunctionOrdered<TR> ThenBy(object?     orderBy);
			public IWindowFunctionOrdered<TR> ThenBy(object?     orderBy, Sql.NullsPosition nulls);
			public IWindowFunctionOrdered<TR> ThenByDesc(object? orderBy);
			public IWindowFunctionOrdered<TR> ThenByDesc(object? orderBy, Sql.NullsPosition nulls);
		}

		public interface IWindowFunctionWithOptionalOver<out TR> : IWindowFunction<TR>, IDefinedFunction<TR>
		{
		}

		public interface IWindowFunctionWithOptionalArgument<out TR> : IWindowFunction<TR>, IWindowFunctionWithArgument<TR>
		{
		}

		public interface IWindowFunctionWithOptionalArgumentAndOver<out TR> : IWindowFunction<TR>, IWindowFunctionWithArgument<TR>, IDefinedFunction<TR>
		{
		}

		public interface IWindowFunctionWithArgument<out TR>
		{
			IWindowFunction<TR> Argument(object?               argument);
			IWindowFunction<TR> Argument(Sql.AggregateModifier modifier, object? argument);
		}

		public interface IWindowFunctionWithArgumentAndOptionalOver<out TR> : IWindowFunctionWithArgument<TR>, IDefinedFunction<TR>
		{
		}

		public static long RowNumber(this Sql.IWindowFunction window, Func<IWindowFunction<long>, IDefinedFunction<long>> func)
			=> throw new ServerSideOnlyException(nameof(RowNumber));

		public static int Count(this Sql.IWindowFunction window, Func<IWindowFunctionWithOptionalArgumentAndOver<int>, IDefinedFunction<int>> func)
			=> throw new ServerSideOnlyException(nameof(Count));

		public static int Sum(this Sql.IWindowFunction window, int argument, Func<IWindowFunction<int>, IDefinedFunction<int>> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		public static int? Sum(this Sql.IWindowFunction window, int? argument, Func<IWindowFunction<int?>, IDefinedFunction<int?>> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		#region Average

		public static double Average(this Sql.IWindowFunction window, int argument, Func<IWindowFunctionWithOptionalOver<int>, IDefinedFunction<int>> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double Average(this Sql.IWindowFunction window, Func<IWindowFunctionWithArgumentAndOptionalOver<int>, IDefinedFunction<int>> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double? Average(this Sql.IWindowFunction window, int? argument, Func<IWindowFunctionWithOptionalOver<int?>, IDefinedFunction<int?>> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		public static double? Average(this Sql.IWindowFunction window, Func<IWindowFunctionWithArgumentAndOptionalOver<int?>, IDefinedFunction<int?>> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		#endregion Average
	}

	partial class Sql
	{
		public static void Test()
		{
			var query = from t in new[] { new { Id = 1, Name = "John" } }
						select new
						{
							t.Id,
							RowNumber = Sql.Window.RowNumber(w => w.PartitionBy(t.Name).OrderBy(t.Id)),
							RN2 = Sql.Window.RowNumber(w => w
								.Filter(t.Name.StartsWith("Jo"))
								.PartitionBy(t.Name)
								.OrderByDesc(t.Id)),
							Sum = Sql.Window.Sum(t.Id, w => w.PartitionBy(t.Name).OrderBy(t.Id).ThenBy(t.Name)),
							Count = Sql.Window.Count(w => w.PartitionBy(t.Name).OrderBy(t.Id).ThenBy(t.Name)),
							CountArg = Sql.Window.Count(w => w.Argument(AggregateModifier.Distinct, t.Id).PartitionBy(t.Name).OrderBy(t.Id).ThenBy(t.Name)),
						};
		}
	}
}
