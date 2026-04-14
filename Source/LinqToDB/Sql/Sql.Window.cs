using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Linq;

#pragma warning disable MA0048
#pragma warning disable IDE0130
namespace LinqToDB
{
	/// <summary>
	/// Provides SQL window function support via a fluent lambda-based API accessed through <see cref="Sql.Window"/>.
	/// </summary>
	/// <remarks>
	/// <para>Not all window functions and clauses are supported by every database provider.
	/// An exception with a descriptive message will be thrown at query translation time
	/// if the current provider does not support the requested function or clause.</para>
	/// <para>The FILTER (WHERE ...) clause is natively supported by PostgreSQL.
	/// For other providers, it is automatically emulated using CASE WHEN.</para>
	/// <para>NULLS FIRST / NULLS LAST ordering is natively supported by PostgreSQL, Oracle, and Firebird 3+.
	/// For other providers, it is automatically emulated.</para>
	/// </remarks>
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
			TBoundaryDefined Value(object? offset);
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

		/// <summary>
		/// Defines a reusable window specification that can be shared across multiple window function calls via UseWindow.
		/// </summary>
		/// <remarks>
		/// <para>May not be supported by all database providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     let wnd = Sql.Window.DefineWindow(f => f.PartitionBy(t.Dept).OrderBy(t.Date))
		///     select new
		///     {
		///         RN  = Sql.Window.RowNumber(f => f.UseWindow(wnd)),
		///         Sum = Sql.Window.Sum(t.Salary, f => f.UseWindow(wnd)),
		///         Lag = Sql.Window.Lag(t.Salary, f => f.UseWindow(wnd)),
		///     };
		///
		/// </code>
		/// </remarks>
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

		// COUNT — supports Argument, Filter, Partition, Order, Frame
		public interface IOArgumentOFilterOPartitionOOrderOFrameFinal : IArgumentPart<IOFilterOPartitionOOrderOFrameFinal>, IOFilterOPartitionOOrderOFrameFinal
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

		// SUM, AVG, MIN, MAX, COUNT — aggregate window functions: Filter, Partition, Order, Frame
		public interface IOFilterOPartitionOOrderOFrameFinal : IFilterPart<IOPartitionOOrderOFrameFinal>, IOPartitionOOrderOFrameFinal, IUseWindow<IDefinedFunction>
		{
		}

		// FIRST_VALUE, LAST_VALUE, NTH_VALUE — value window functions: Partition, Order, Frame (no Filter)
		public interface IOPartitionOOrderOFrameWithWindowFinal : IOPartitionOOrderOFrameFinal, IUseWindow<IDefinedFunction>
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

		/// <summary>
		/// Generates SQL <c>ROW_NUMBER()</c> window function. Assigns a unique sequential integer to each row within a partition.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.RowNumber(f =&gt; f.[PartitionBy(...)].OrderBy(...)[.ThenBy(...)])</c></para>
		/// <para>May not be supported by all database providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         RN1 = Sql.Window.RowNumber(f => f.PartitionBy(t.Category).OrderBy(t.Id)),
		///         RN2 = Sql.Window.RowNumber(f => f.OrderBy(t.Date)),
		///         RN3 = Sql.Window.RowNumber(f => f.PartitionBy(t.Dept).OrderBy(t.Date).ThenByDesc(t.Id)),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   ROW_NUMBER() OVER (PARTITION BY t.Category ORDER BY t.Id),
		///   ROW_NUMBER() OVER (ORDER BY t.Date),
		///   ROW_NUMBER() OVER (PARTITION BY t.Dept ORDER BY t.Date, t.Id DESC)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static long RowNumber(this Sql.IWindowFunction window, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(RowNumber));

		/// <summary>
		/// Generates SQL <c>RANK()</c> window function. Returns the rank of each row, with gaps for ties.
		/// </summary>
		/// <remarks>
		/// <para>
		/// <b>Syntax:</b> <c>Sql.Window.Rank(f =&gt; f.[PartitionBy(...)].OrderBy(...)[.ThenBy(...)])</c>
		/// </para>
		/// <para>May not be supported by all database providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Rank1 = Sql.Window.Rank(f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Salary)),
		///         Rank2 = Sql.Window.Rank(f =&gt; f.OrderByDesc(t.Score)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   RANK() OVER (PARTITION BY t.Dept ORDER BY t.Salary),
		///   RANK() OVER (ORDER BY t.Score DESC)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static int Rank(this Sql.IWindowFunction window, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Rank));

		/// <summary>
		/// Generates SQL <c>DENSE_RANK()</c> window function. Returns the rank without gaps for ties.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.DenseRank(f =&gt; f.[PartitionBy(...)].OrderBy(...)[.ThenBy(...)])</c></para>
		/// <para>May not be supported by all database providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         DR = Sql.Window.DenseRank(f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Salary)),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT DENSE_RANK() OVER (PARTITION BY t.Dept ORDER BY t.Salary)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static long DenseRank(this Sql.IWindowFunction window, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(DenseRank));

		/// <summary>
		/// Generates SQL <c>PERCENT_RANK()</c> window function. Returns the relative rank (0 to 1).
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.PercentRank(f =&gt; f.[PartitionBy(...)].OrderBy(...)[.ThenBy(...)])</c></para>
		/// <para>May not be supported by all database providers (e.g. ClickHouse).</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         PR = Sql.Window.PercentRank(f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Salary)),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT PERCENT_RANK() OVER (PARTITION BY t.Dept ORDER BY t.Salary)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double PercentRank(this Sql.IWindowFunction window, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(PercentRank));

		/// <summary>
		/// Generates SQL <c>CUME_DIST()</c> window function. Returns the cumulative distribution (0 to 1).
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.CumeDist(f =&gt; f.[PartitionBy(...)].OrderBy(...)[.ThenBy(...)])</c></para>
		/// <para>May not be supported by all database providers (e.g. ClickHouse).</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         CD = Sql.Window.CumeDist(f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Salary)),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT CUME_DIST() OVER (PARTITION BY t.Dept ORDER BY t.Salary)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double CumeDist(this Sql.IWindowFunction window, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(CumeDist));

		/// <summary>
		/// Generates SQL <c>NTILE(n)</c> window function. Distributes rows into <paramref name="n"/> groups.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.NTile(n, f =&gt; f.[PartitionBy(...)].OrderBy(...)[.ThenBy(...)])</c></para>
		/// <para>May not be supported by all database providers (e.g. ClickHouse, Firebird 3).</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Quartile = Sql.Window.NTile(4, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Salary)),
		///         Tercile  = Sql.Window.NTile(3, f =&gt; f.OrderBy(t.Score)),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   NTILE(4) OVER (PARTITION BY t.Dept ORDER BY t.Salary),
		///   NTILE(3) OVER (ORDER BY t.Score)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static long NTile(this Sql.IWindowFunction window, int n, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(NTile));

		#endregion

		#region Lead/Lag

		/// <summary>
		/// Generates SQL <c>LEAD()</c> window function. Accesses a value from a subsequent row.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Lead(expr, [offset, [default,]] f =&gt; f.[PartitionBy(...)].OrderBy(...)[.ThenBy(...)])</c></para>
		/// <para>May not be supported by all database providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Next  = Sql.Window.Lead(t.Value, f =&gt; f.OrderBy(t.Date)),
		///         Skip2 = Sql.Window.Lead(t.Value, 2, f =&gt; f.OrderBy(t.Date)),
		///         Safe  = Sql.Window.Lead(t.Value, 1, 0, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   LEAD(t.Value) OVER (ORDER BY t.Date),
		///   LEAD(t.Value, 2) OVER (ORDER BY t.Date),
		///   LEAD(t.Value, 1, 0) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static T Lead<T>(this Sql.IWindowFunction window, T expr, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Lead));

		/// <summary>
		/// Generates SQL <c>LEAD()</c> window function. Accesses a value from a subsequent row.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Lead(expr, [offset, [default,]] f =&gt; f.[PartitionBy(...)].OrderBy(...)[.ThenBy(...)])</c></para>
		/// <para>May not be supported by all database providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Next  = Sql.Window.Lead(t.Value, f =&gt; f.OrderBy(t.Date)),
		///         Skip2 = Sql.Window.Lead(t.Value, 2, f =&gt; f.OrderBy(t.Date)),
		///         Safe  = Sql.Window.Lead(t.Value, 1, 0, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   LEAD(t.Value) OVER (ORDER BY t.Date),
		///   LEAD(t.Value, 2) OVER (ORDER BY t.Date),
		///   LEAD(t.Value, 1, 0) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static T Lead<T>(this Sql.IWindowFunction window, T expr, int offset, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Lead));

		/// <summary>
		/// Generates SQL <c>LEAD()</c> window function. Accesses a value from a subsequent row.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Lead(expr, [offset, [default,]] f =&gt; f.[PartitionBy(...)].OrderBy(...)[.ThenBy(...)])</c></para>
		/// <para>May not be supported by all database providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Next  = Sql.Window.Lead(t.Value, f =&gt; f.OrderBy(t.Date)),
		///         Skip2 = Sql.Window.Lead(t.Value, 2, f =&gt; f.OrderBy(t.Date)),
		///         Safe  = Sql.Window.Lead(t.Value, 1, 0, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   LEAD(t.Value) OVER (ORDER BY t.Date),
		///   LEAD(t.Value, 2) OVER (ORDER BY t.Date),
		///   LEAD(t.Value, 1, 0) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static T Lead<T>(this Sql.IWindowFunction window, T expr, int offset, T @default, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Lead));

		/// <summary>
		/// Generates SQL <c>LAG()</c> window function. Accesses a value from a preceding row.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Lag(expr, [offset, [default,]] f =&gt; f.[PartitionBy(...)].OrderBy(...)[.ThenBy(...)])</c></para>
		/// <para>May not be supported by all database providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Prev  = Sql.Window.Lag(t.Value, f =&gt; f.OrderBy(t.Date)),
		///         Back2 = Sql.Window.Lag(t.Value, 2, f =&gt; f.OrderBy(t.Date)),
		///         Safe  = Sql.Window.Lag(t.Value, 1, 0, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   LAG(t.Value) OVER (ORDER BY t.Date),
		///   LAG(t.Value, 2) OVER (ORDER BY t.Date),
		///   LAG(t.Value, 1, 0) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static T Lag<T>(this Sql.IWindowFunction window, T expr, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Lag));

		/// <summary>
		/// Generates SQL <c>LAG()</c> window function. Accesses a value from a preceding row.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Lag(expr, [offset, [default,]] f =&gt; f.[PartitionBy(...)].OrderBy(...)[.ThenBy(...)])</c></para>
		/// <para>May not be supported by all database providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Prev  = Sql.Window.Lag(t.Value, f =&gt; f.OrderBy(t.Date)),
		///         Back2 = Sql.Window.Lag(t.Value, 2, f =&gt; f.OrderBy(t.Date)),
		///         Safe  = Sql.Window.Lag(t.Value, 1, 0, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   LAG(t.Value) OVER (ORDER BY t.Date),
		///   LAG(t.Value, 2) OVER (ORDER BY t.Date),
		///   LAG(t.Value, 1, 0) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static T Lag<T>(this Sql.IWindowFunction window, T expr, int offset, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Lag));

		/// <summary>
		/// Generates SQL <c>LAG()</c> window function. Accesses a value from a preceding row.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Lag(expr, [offset, [default,]] f =&gt; f.[PartitionBy(...)].OrderBy(...)[.ThenBy(...)])</c></para>
		/// <para>May not be supported by all database providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Prev  = Sql.Window.Lag(t.Value, f =&gt; f.OrderBy(t.Date)),
		///         Back2 = Sql.Window.Lag(t.Value, 2, f =&gt; f.OrderBy(t.Date)),
		///         Safe  = Sql.Window.Lag(t.Value, 1, 0, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   LAG(t.Value) OVER (ORDER BY t.Date),
		///   LAG(t.Value, 2) OVER (ORDER BY t.Date),
		///   LAG(t.Value, 1, 0) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static T Lag<T>(this Sql.IWindowFunction window, T expr, int offset, T @default, Func<IOPartitionROrderFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Lag));

		#endregion

		#region FirstValue/LastValue/NthValue

		/// <summary>
		/// Generates SQL <c>FIRST_VALUE()</c> window function. Returns the first value in the frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.FirstValue(expr, f =&gt; f.[PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>May not be supported by all database providers. Does not support the FILTER clause.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         First1 = Sql.Window.FirstValue(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         First2 = Sql.Window.FirstValue(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   FIRST_VALUE(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   FIRST_VALUE(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static T FirstValue<T>(this Sql.IWindowFunction window, T expr, Func<IOPartitionOOrderOFrameWithWindowFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(FirstValue));

		/// <summary>
		/// Generates SQL <c>LAST_VALUE()</c> window function. Returns the last value in the frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.LastValue(expr, f =&gt; f.[PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>May not be supported by all database providers. Does not support the FILTER clause.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         // Use UNBOUNDED FOLLOWING to get the true last value in partition
		///         Last = Sql.Window.LastValue(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date).RowsBetween.Unbounded.And.Unbounded),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT LAST_VALUE(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static T LastValue<T>(this Sql.IWindowFunction window, T expr, Func<IOPartitionOOrderOFrameWithWindowFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(LastValue));

		/// <summary>
		/// Generates SQL <c>NTH_VALUE()</c> window function. Returns the value at position <paramref name="n"/> in the frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.NthValue(expr, n, f =&gt; f.[PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>May not be supported by all database providers (e.g. SQL Server). Does not support the FILTER clause.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Second = Sql.Window.NthValue(t.Salary, 2L, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date).RowsBetween.Unbounded.And.Unbounded),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT NTH_VALUE(t.Salary, 2) OVER (PARTITION BY t.Dept ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static T NthValue<T>(this Sql.IWindowFunction window, T expr, long n, Func<IOPartitionOOrderOFrameWithWindowFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(NthValue));

		#endregion

		/// <summary>
		/// Generates SQL <c>COUNT()</c> window function. Use <c>.Argument(expr)</c> for <c>COUNT(expr)</c>; omit for <c>COUNT(*)</c>.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Count(f =&gt; f.[Argument(expr)][.Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Total   = Sql.Window.Count(f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Id)),
		///         NonNull = Sql.Window.Count(f =&gt; f.Argument(t.NullableValue).PartitionBy(t.Dept).OrderBy(t.Id)),
		///         Running = Sql.Window.Count(f =&gt; f.OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
		///         Filt    = Sql.Window.Count(f =&gt; f.Filter(t.Value &gt; 10).PartitionBy(t.Dept).OrderBy(t.Id)),
		///     };
		/// </code>
		/// <para><b>Generated SQL (PostgreSQL):</b></para>
		/// <code>
		/// SELECT
		///   COUNT(*) OVER (PARTITION BY t.Dept ORDER BY t.Id),
		///   COUNT(t.NullableValue) OVER (PARTITION BY t.Dept ORDER BY t.Id),
		///   COUNT(*) OVER (ORDER BY t.Id ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
		///   COUNT(*) FILTER (WHERE t.Value &gt; 10) OVER (PARTITION BY t.Dept ORDER BY t.Id)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static int Count(this Sql.IWindowFunction window, Func<IOArgumentOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Count));

		#region Sum

		/// <summary>
		/// Generates SQL <c>SUM()</c> window function. Computes the sum of values within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Sum(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     let wnd = Sql.Window.DefineWindow(f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date))
		///     select new
		///     {
		///         Sum1 = Sql.Window.Sum(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum2 = Sql.Window.Sum(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///         Sum3 = Sql.Window.Sum(t.Salary, f =&gt; f.Filter(t.IsActive).PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum4 = Sql.Window.Sum(t.Salary, f =&gt; f.UseWindow(wnd)),
		///     };
		/// </code>
		/// <para><b>Generated SQL (PostgreSQL):</b></para>
		/// <code>
		/// SELECT
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
		///   SUM(t.Salary) FILTER (WHERE t.IsActive) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static int Sum(this Sql.IWindowFunction window, int argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		/// <summary>
		/// Generates SQL <c>SUM()</c> window function. Computes the sum of values within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Sum(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     let wnd = Sql.Window.DefineWindow(f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date))
		///     select new
		///     {
		///         Sum1 = Sql.Window.Sum(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum2 = Sql.Window.Sum(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///         Sum3 = Sql.Window.Sum(t.Salary, f =&gt; f.Filter(t.IsActive).PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum4 = Sql.Window.Sum(t.Salary, f =&gt; f.UseWindow(wnd)),
		///     };
		/// </code>
		/// <para><b>Generated SQL (PostgreSQL):</b></para>
		/// <code>
		/// SELECT
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
		///   SUM(t.Salary) FILTER (WHERE t.IsActive) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static int? Sum(this Sql.IWindowFunction window, int? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		/// <summary>
		/// Generates SQL <c>SUM()</c> window function. Computes the sum of values within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Sum(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     let wnd = Sql.Window.DefineWindow(f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date))
		///     select new
		///     {
		///         Sum1 = Sql.Window.Sum(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum2 = Sql.Window.Sum(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///         Sum3 = Sql.Window.Sum(t.Salary, f =&gt; f.Filter(t.IsActive).PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum4 = Sql.Window.Sum(t.Salary, f =&gt; f.UseWindow(wnd)),
		///     };
		/// </code>
		/// <para><b>Generated SQL (PostgreSQL):</b></para>
		/// <code>
		/// SELECT
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
		///   SUM(t.Salary) FILTER (WHERE t.IsActive) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static long Sum(this Sql.IWindowFunction window, long argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		/// <summary>
		/// Generates SQL <c>SUM()</c> window function. Computes the sum of values within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Sum(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     let wnd = Sql.Window.DefineWindow(f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date))
		///     select new
		///     {
		///         Sum1 = Sql.Window.Sum(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum2 = Sql.Window.Sum(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///         Sum3 = Sql.Window.Sum(t.Salary, f =&gt; f.Filter(t.IsActive).PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum4 = Sql.Window.Sum(t.Salary, f =&gt; f.UseWindow(wnd)),
		///     };
		/// </code>
		/// <para><b>Generated SQL (PostgreSQL):</b></para>
		/// <code>
		/// SELECT
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
		///   SUM(t.Salary) FILTER (WHERE t.IsActive) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static long? Sum(this Sql.IWindowFunction window, long? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		/// <summary>
		/// Generates SQL <c>SUM()</c> window function. Computes the sum of values within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Sum(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     let wnd = Sql.Window.DefineWindow(f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date))
		///     select new
		///     {
		///         Sum1 = Sql.Window.Sum(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum2 = Sql.Window.Sum(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///         Sum3 = Sql.Window.Sum(t.Salary, f =&gt; f.Filter(t.IsActive).PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum4 = Sql.Window.Sum(t.Salary, f =&gt; f.UseWindow(wnd)),
		///     };
		/// </code>
		/// <para><b>Generated SQL (PostgreSQL):</b></para>
		/// <code>
		/// SELECT
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
		///   SUM(t.Salary) FILTER (WHERE t.IsActive) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static double Sum(this Sql.IWindowFunction window, double argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		/// <summary>
		/// Generates SQL <c>SUM()</c> window function. Computes the sum of values within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Sum(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     let wnd = Sql.Window.DefineWindow(f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date))
		///     select new
		///     {
		///         Sum1 = Sql.Window.Sum(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum2 = Sql.Window.Sum(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///         Sum3 = Sql.Window.Sum(t.Salary, f =&gt; f.Filter(t.IsActive).PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum4 = Sql.Window.Sum(t.Salary, f =&gt; f.UseWindow(wnd)),
		///     };
		/// </code>
		/// <para><b>Generated SQL (PostgreSQL):</b></para>
		/// <code>
		/// SELECT
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
		///   SUM(t.Salary) FILTER (WHERE t.IsActive) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static double? Sum(this Sql.IWindowFunction window, double? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		/// <summary>
		/// Generates SQL <c>SUM()</c> window function. Computes the sum of values within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Sum(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     let wnd = Sql.Window.DefineWindow(f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date))
		///     select new
		///     {
		///         Sum1 = Sql.Window.Sum(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum2 = Sql.Window.Sum(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///         Sum3 = Sql.Window.Sum(t.Salary, f =&gt; f.Filter(t.IsActive).PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum4 = Sql.Window.Sum(t.Salary, f =&gt; f.UseWindow(wnd)),
		///     };
		/// </code>
		/// <para><b>Generated SQL (PostgreSQL):</b></para>
		/// <code>
		/// SELECT
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
		///   SUM(t.Salary) FILTER (WHERE t.IsActive) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static decimal Sum(this Sql.IWindowFunction window, decimal argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		/// <summary>
		/// Generates SQL <c>SUM()</c> window function. Computes the sum of values within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Sum(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     let wnd = Sql.Window.DefineWindow(f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date))
		///     select new
		///     {
		///         Sum1 = Sql.Window.Sum(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum2 = Sql.Window.Sum(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///         Sum3 = Sql.Window.Sum(t.Salary, f =&gt; f.Filter(t.IsActive).PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum4 = Sql.Window.Sum(t.Salary, f =&gt; f.UseWindow(wnd)),
		///     };
		/// </code>
		/// <para><b>Generated SQL (PostgreSQL):</b></para>
		/// <code>
		/// SELECT
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
		///   SUM(t.Salary) FILTER (WHERE t.IsActive) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static decimal? Sum(this Sql.IWindowFunction window, decimal? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		/// <summary>
		/// Generates SQL <c>SUM()</c> window function. Computes the sum of values within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Sum(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     let wnd = Sql.Window.DefineWindow(f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date))
		///     select new
		///     {
		///         Sum1 = Sql.Window.Sum(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum2 = Sql.Window.Sum(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///         Sum3 = Sql.Window.Sum(t.Salary, f =&gt; f.Filter(t.IsActive).PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum4 = Sql.Window.Sum(t.Salary, f =&gt; f.UseWindow(wnd)),
		///     };
		/// </code>
		/// <para><b>Generated SQL (PostgreSQL):</b></para>
		/// <code>
		/// SELECT
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
		///   SUM(t.Salary) FILTER (WHERE t.IsActive) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static float Sum(this Sql.IWindowFunction window, float argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		/// <summary>
		/// Generates SQL <c>SUM()</c> window function. Computes the sum of values within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Sum(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     let wnd = Sql.Window.DefineWindow(f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date))
		///     select new
		///     {
		///         Sum1 = Sql.Window.Sum(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum2 = Sql.Window.Sum(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///         Sum3 = Sql.Window.Sum(t.Salary, f =&gt; f.Filter(t.IsActive).PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Sum4 = Sql.Window.Sum(t.Salary, f =&gt; f.UseWindow(wnd)),
		///     };
		/// </code>
		/// <para><b>Generated SQL (PostgreSQL):</b></para>
		/// <code>
		/// SELECT
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
		///   SUM(t.Salary) FILTER (WHERE t.IsActive) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static float? Sum(this Sql.IWindowFunction window, float? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		#endregion Sum

		#region Average

		/// <summary>
		/// Generates SQL <c>AVG()</c> window function. Computes the average within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Average(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Avg1 = Sql.Window.Average(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Avg2 = Sql.Window.Average(t.Salary, f =&gt; f.Filter(t.IsActive).OrderBy(t.Date)),
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Value(3).And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   AVG(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   AVG(CASE WHEN t.IsActive THEN t.Salary ELSE NULL END) OVER (ORDER BY t.Date),
		///   AVG(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN 3 PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double Average(this Sql.IWindowFunction window, int argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		/// <summary>
		/// Generates SQL <c>AVG()</c> window function. Computes the average within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Average(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Avg1 = Sql.Window.Average(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Avg2 = Sql.Window.Average(t.Salary, f =&gt; f.Filter(t.IsActive).OrderBy(t.Date)),
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Value(3).And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   AVG(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   AVG(CASE WHEN t.IsActive THEN t.Salary ELSE NULL END) OVER (ORDER BY t.Date),
		///   AVG(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN 3 PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double? Average(this Sql.IWindowFunction window, int? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		/// <summary>
		/// Generates SQL <c>AVG()</c> window function. Computes the average within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Average(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Avg1 = Sql.Window.Average(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Avg2 = Sql.Window.Average(t.Salary, f =&gt; f.Filter(t.IsActive).OrderBy(t.Date)),
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Value(3).And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   AVG(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   AVG(CASE WHEN t.IsActive THEN t.Salary ELSE NULL END) OVER (ORDER BY t.Date),
		///   AVG(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN 3 PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double Average(this Sql.IWindowFunction window, long argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		/// <summary>
		/// Generates SQL <c>AVG()</c> window function. Computes the average within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Average(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Avg1 = Sql.Window.Average(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Avg2 = Sql.Window.Average(t.Salary, f =&gt; f.Filter(t.IsActive).OrderBy(t.Date)),
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Value(3).And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   AVG(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   AVG(CASE WHEN t.IsActive THEN t.Salary ELSE NULL END) OVER (ORDER BY t.Date),
		///   AVG(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN 3 PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double? Average(this Sql.IWindowFunction window, long? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		/// <summary>
		/// Generates SQL <c>AVG()</c> window function. Computes the average within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Average(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Avg1 = Sql.Window.Average(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Avg2 = Sql.Window.Average(t.Salary, f =&gt; f.Filter(t.IsActive).OrderBy(t.Date)),
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Value(3).And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   AVG(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   AVG(CASE WHEN t.IsActive THEN t.Salary ELSE NULL END) OVER (ORDER BY t.Date),
		///   AVG(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN 3 PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double Average(this Sql.IWindowFunction window, double argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		/// <summary>
		/// Generates SQL <c>AVG()</c> window function. Computes the average within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Average(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Avg1 = Sql.Window.Average(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Avg2 = Sql.Window.Average(t.Salary, f =&gt; f.Filter(t.IsActive).OrderBy(t.Date)),
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Value(3).And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   AVG(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   AVG(CASE WHEN t.IsActive THEN t.Salary ELSE NULL END) OVER (ORDER BY t.Date),
		///   AVG(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN 3 PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double? Average(this Sql.IWindowFunction window, double? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		/// <summary>
		/// Generates SQL <c>AVG()</c> window function. Computes the average within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Average(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Avg1 = Sql.Window.Average(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Avg2 = Sql.Window.Average(t.Salary, f =&gt; f.Filter(t.IsActive).OrderBy(t.Date)),
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Value(3).And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   AVG(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   AVG(CASE WHEN t.IsActive THEN t.Salary ELSE NULL END) OVER (ORDER BY t.Date),
		///   AVG(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN 3 PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static decimal Average(this Sql.IWindowFunction window, decimal argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		/// <summary>
		/// Generates SQL <c>AVG()</c> window function. Computes the average within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Average(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Avg1 = Sql.Window.Average(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Avg2 = Sql.Window.Average(t.Salary, f =&gt; f.Filter(t.IsActive).OrderBy(t.Date)),
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Value(3).And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   AVG(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   AVG(CASE WHEN t.IsActive THEN t.Salary ELSE NULL END) OVER (ORDER BY t.Date),
		///   AVG(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN 3 PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static decimal? Average(this Sql.IWindowFunction window, decimal? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		/// <summary>
		/// Generates SQL <c>AVG()</c> window function. Computes the average within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Average(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Avg1 = Sql.Window.Average(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Avg2 = Sql.Window.Average(t.Salary, f =&gt; f.Filter(t.IsActive).OrderBy(t.Date)),
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Value(3).And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   AVG(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   AVG(CASE WHEN t.IsActive THEN t.Salary ELSE NULL END) OVER (ORDER BY t.Date),
		///   AVG(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN 3 PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static float Average(this Sql.IWindowFunction window, float argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		/// <summary>
		/// Generates SQL <c>AVG()</c> window function. Computes the average within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Average(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Avg1 = Sql.Window.Average(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Avg2 = Sql.Window.Average(t.Salary, f =&gt; f.Filter(t.IsActive).OrderBy(t.Date)),
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Value(3).And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   AVG(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   AVG(CASE WHEN t.IsActive THEN t.Salary ELSE NULL END) OVER (ORDER BY t.Date),
		///   AVG(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN 3 PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static float? Average(this Sql.IWindowFunction window, float? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		/// <summary>
		/// Generates SQL <c>AVG()</c> window function. Computes the average within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Average(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Avg1 = Sql.Window.Average(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Avg2 = Sql.Window.Average(t.Salary, f =&gt; f.Filter(t.IsActive).OrderBy(t.Date)),
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Value(3).And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   AVG(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   AVG(CASE WHEN t.IsActive THEN t.Salary ELSE NULL END) OVER (ORDER BY t.Date),
		///   AVG(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN 3 PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double Average(this Sql.IWindowFunction window, short argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		/// <summary>
		/// Generates SQL <c>AVG()</c> window function. Computes the average within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Average(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Avg1 = Sql.Window.Average(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Avg2 = Sql.Window.Average(t.Salary, f =&gt; f.Filter(t.IsActive).OrderBy(t.Date)),
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Value(3).And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   AVG(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   AVG(CASE WHEN t.IsActive THEN t.Salary ELSE NULL END) OVER (ORDER BY t.Date),
		///   AVG(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN 3 PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double? Average(this Sql.IWindowFunction window, short? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		/// <summary>
		/// Generates SQL <c>AVG()</c> window function. Computes the average within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Average(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Avg1 = Sql.Window.Average(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Avg2 = Sql.Window.Average(t.Salary, f =&gt; f.Filter(t.IsActive).OrderBy(t.Date)),
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Value(3).And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   AVG(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   AVG(CASE WHEN t.IsActive THEN t.Salary ELSE NULL END) OVER (ORDER BY t.Date),
		///   AVG(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN 3 PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double Average(this Sql.IWindowFunction window, byte argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		/// <summary>
		/// Generates SQL <c>AVG()</c> window function. Computes the average within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Average(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Avg1 = Sql.Window.Average(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Avg2 = Sql.Window.Average(t.Salary, f =&gt; f.Filter(t.IsActive).OrderBy(t.Date)),
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Value(3).And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   AVG(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   AVG(CASE WHEN t.IsActive THEN t.Salary ELSE NULL END) OVER (ORDER BY t.Date),
		///   AVG(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN 3 PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double? Average(this Sql.IWindowFunction window, byte? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		#endregion Average

		#region Min

		/// <summary>
		/// Generates SQL <c>MIN()</c> window function. Returns the minimum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Min(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Min1 = Sql.Window.Min(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Min2 = Sql.Window.Min(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MIN(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MIN(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static int Min(this Sql.IWindowFunction window, int argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		/// <summary>
		/// Generates SQL <c>MIN()</c> window function. Returns the minimum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Min(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Min1 = Sql.Window.Min(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Min2 = Sql.Window.Min(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MIN(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MIN(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static int? Min(this Sql.IWindowFunction window, int? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		/// <summary>
		/// Generates SQL <c>MIN()</c> window function. Returns the minimum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Min(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Min1 = Sql.Window.Min(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Min2 = Sql.Window.Min(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MIN(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MIN(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static long Min(this Sql.IWindowFunction window, long argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		/// <summary>
		/// Generates SQL <c>MIN()</c> window function. Returns the minimum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Min(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Min1 = Sql.Window.Min(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Min2 = Sql.Window.Min(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MIN(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MIN(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static long? Min(this Sql.IWindowFunction window, long? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		/// <summary>
		/// Generates SQL <c>MIN()</c> window function. Returns the minimum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Min(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Min1 = Sql.Window.Min(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Min2 = Sql.Window.Min(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MIN(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MIN(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double Min(this Sql.IWindowFunction window, double argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		/// <summary>
		/// Generates SQL <c>MIN()</c> window function. Returns the minimum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Min(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Min1 = Sql.Window.Min(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Min2 = Sql.Window.Min(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MIN(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MIN(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double? Min(this Sql.IWindowFunction window, double? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		/// <summary>
		/// Generates SQL <c>MIN()</c> window function. Returns the minimum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Min(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Min1 = Sql.Window.Min(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Min2 = Sql.Window.Min(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MIN(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MIN(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static decimal Min(this Sql.IWindowFunction window, decimal argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		/// <summary>
		/// Generates SQL <c>MIN()</c> window function. Returns the minimum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Min(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Min1 = Sql.Window.Min(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Min2 = Sql.Window.Min(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MIN(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MIN(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static decimal? Min(this Sql.IWindowFunction window, decimal? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		/// <summary>
		/// Generates SQL <c>MIN()</c> window function. Returns the minimum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Min(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Min1 = Sql.Window.Min(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Min2 = Sql.Window.Min(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MIN(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MIN(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static float Min(this Sql.IWindowFunction window, float argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		/// <summary>
		/// Generates SQL <c>MIN()</c> window function. Returns the minimum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Min(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Min1 = Sql.Window.Min(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Min2 = Sql.Window.Min(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MIN(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MIN(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static float? Min(this Sql.IWindowFunction window, float? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		/// <summary>
		/// Generates SQL <c>MIN()</c> window function. Returns the minimum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Min(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Min1 = Sql.Window.Min(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Min2 = Sql.Window.Min(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MIN(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MIN(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static short Min(this Sql.IWindowFunction window, short argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		/// <summary>
		/// Generates SQL <c>MIN()</c> window function. Returns the minimum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Min(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Min1 = Sql.Window.Min(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Min2 = Sql.Window.Min(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MIN(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MIN(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static short? Min(this Sql.IWindowFunction window, short? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		/// <summary>
		/// Generates SQL <c>MIN()</c> window function. Returns the minimum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Min(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Min1 = Sql.Window.Min(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Min2 = Sql.Window.Min(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MIN(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MIN(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static byte Min(this Sql.IWindowFunction window, byte argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		/// <summary>
		/// Generates SQL <c>MIN()</c> window function. Returns the minimum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Min(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Min1 = Sql.Window.Min(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Min2 = Sql.Window.Min(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MIN(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MIN(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static byte? Min(this Sql.IWindowFunction window, byte? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		#endregion Min

		#region Max

		/// <summary>
		/// Generates SQL <c>MAX()</c> window function. Returns the maximum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Max(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Max1 = Sql.Window.Max(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Max2 = Sql.Window.Max(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MAX(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MAX(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static int Max(this Sql.IWindowFunction window, int argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		/// <summary>
		/// Generates SQL <c>MAX()</c> window function. Returns the maximum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Max(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Max1 = Sql.Window.Max(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Max2 = Sql.Window.Max(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MAX(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MAX(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static int? Max(this Sql.IWindowFunction window, int? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		/// <summary>
		/// Generates SQL <c>MAX()</c> window function. Returns the maximum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Max(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Max1 = Sql.Window.Max(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Max2 = Sql.Window.Max(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MAX(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MAX(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static long Max(this Sql.IWindowFunction window, long argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		/// <summary>
		/// Generates SQL <c>MAX()</c> window function. Returns the maximum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Max(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Max1 = Sql.Window.Max(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Max2 = Sql.Window.Max(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MAX(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MAX(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static long? Max(this Sql.IWindowFunction window, long? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		/// <summary>
		/// Generates SQL <c>MAX()</c> window function. Returns the maximum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Max(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Max1 = Sql.Window.Max(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Max2 = Sql.Window.Max(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MAX(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MAX(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double Max(this Sql.IWindowFunction window, double argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		/// <summary>
		/// Generates SQL <c>MAX()</c> window function. Returns the maximum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Max(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Max1 = Sql.Window.Max(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Max2 = Sql.Window.Max(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MAX(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MAX(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double? Max(this Sql.IWindowFunction window, double? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		/// <summary>
		/// Generates SQL <c>MAX()</c> window function. Returns the maximum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Max(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Max1 = Sql.Window.Max(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Max2 = Sql.Window.Max(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MAX(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MAX(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static decimal Max(this Sql.IWindowFunction window, decimal argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		/// <summary>
		/// Generates SQL <c>MAX()</c> window function. Returns the maximum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Max(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Max1 = Sql.Window.Max(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Max2 = Sql.Window.Max(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MAX(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MAX(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static decimal? Max(this Sql.IWindowFunction window, decimal? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		/// <summary>
		/// Generates SQL <c>MAX()</c> window function. Returns the maximum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Max(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Max1 = Sql.Window.Max(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Max2 = Sql.Window.Max(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MAX(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MAX(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static float Max(this Sql.IWindowFunction window, float argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		/// <summary>
		/// Generates SQL <c>MAX()</c> window function. Returns the maximum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Max(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Max1 = Sql.Window.Max(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Max2 = Sql.Window.Max(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MAX(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MAX(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static float? Max(this Sql.IWindowFunction window, float? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		/// <summary>
		/// Generates SQL <c>MAX()</c> window function. Returns the maximum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Max(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Max1 = Sql.Window.Max(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Max2 = Sql.Window.Max(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MAX(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MAX(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static short Max(this Sql.IWindowFunction window, short argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		/// <summary>
		/// Generates SQL <c>MAX()</c> window function. Returns the maximum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Max(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Max1 = Sql.Window.Max(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Max2 = Sql.Window.Max(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MAX(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MAX(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static short? Max(this Sql.IWindowFunction window, short? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		/// <summary>
		/// Generates SQL <c>MAX()</c> window function. Returns the maximum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Max(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Max1 = Sql.Window.Max(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Max2 = Sql.Window.Max(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MAX(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MAX(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static byte Max(this Sql.IWindowFunction window, byte argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		/// <summary>
		/// Generates SQL <c>MAX()</c> window function. Returns the maximum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Max(expr, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Max1 = Sql.Window.Max(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Max2 = Sql.Window.Max(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MAX(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MAX(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static byte? Max(this Sql.IWindowFunction window, byte? argument, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		#endregion Max

		#region Percenile Cont

		/// <summary>
		/// Generates SQL <c>PERCENTILE_CONT()</c> ordered-set aggregate. Computes a percentile based on continuous distribution.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>source.PercentileCont(fraction, (e, f) =&gt; f.OrderBy(e.Column))</c></para>
		/// <para>May not be supported by all database providers (e.g. SQLite, MySQL, ClickHouse).</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     group t by t.Dept into g
		///     select new
		///     {
		///         g.Key,
		///         Median = g.PercentileCont(0.5, (e, f) =&gt; f.OrderBy(e.Salary)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT t.Dept, PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY t.Salary)
		/// FROM Table t
		/// GROUP BY t.Dept
		/// </code>
		/// </remarks>

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

		/// <summary>
		/// Generates SQL <c>PERCENTILE_DISC()</c> ordered-set aggregate. Returns the value at the specified percentile from the sorted set.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>source.PercentileDisc(fraction, (e, f) =&gt; f.OrderBy(e.Column)[.ThenBy(...)])</c></para>
		/// <para>May not be supported by all database providers (e.g. SQLite, MySQL, ClickHouse).</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     group t by t.Dept into g
		///     select new
		///     {
		///         g.Key,
		///         Median = g.PercentileDisc(0.5, (e, f) =&gt; f.OrderBy(e.Salary)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT t.Dept, PERCENTILE_DISC(0.5) WITHIN GROUP (ORDER BY t.Salary)
		/// FROM Table t
		/// GROUP BY t.Dept
		/// </code>
		/// </remarks>

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
}
