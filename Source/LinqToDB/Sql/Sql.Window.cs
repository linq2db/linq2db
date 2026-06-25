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
		/// <summary>Marker interface indicating the window function definition is complete and can be returned from the builder lambda.</summary>
		public interface IDefinedFunction<out TR> { }
		/// <summary>Marker interface indicating the window function definition is complete and can be returned from the builder lambda.</summary>
		public interface IDefinedFunction { }

		/// <summary>Provides the FILTER (WHERE ...) clause for aggregate window functions.</summary>
		public interface IFilterPart<out TFiltered>
			where TFiltered : class
		{
			/// <summary>Adds a <c>FILTER (WHERE ...)</c> clause. Natively supported by PostgreSQL; emulated via <c>CASE WHEN</c> on other providers.</summary>
			TFiltered Filter(bool filter);
		}

		/// <summary>Provides ORDER BY for the window function.</summary>
		public interface IOrderByPart<out TThenPart>
			where TThenPart : class
		{
			/// <summary>Adds an <c>ORDER BY column ASC</c> clause.</summary>
			TThenPart OrderBy(object?     orderBy);
			/// <summary>Adds an <c>ORDER BY column ASC [NULLS FIRST|LAST]</c> clause. NULLS ordering is emulated on providers that don't support it natively.</summary>
			TThenPart OrderBy(object?     orderBy, Sql.NullsPosition nulls);
			/// <summary>Adds an <c>ORDER BY column DESC</c> clause.</summary>
			TThenPart OrderByDesc(object? orderBy);
			/// <summary>Adds an <c>ORDER BY column DESC [NULLS FIRST|LAST]</c> clause. NULLS ordering is emulated on providers that don't support it natively.</summary>
			TThenPart OrderByDesc(object? orderBy, Sql.NullsPosition nulls);
		}

		/// <summary>Terminal for ordered-set aggregates (PERCENTILE_CONT/DISC): optionally adds <c>FILTER (WHERE ...)</c> or completes the definition.</summary>
		public interface IOrderedSetFilter<out TValue> : IFilterPart<IDefinedFunction<TValue>>, IDefinedFunction<TValue>
		{
		}

		/// <summary>Provides ORDER BY for ordered-set aggregate functions (e.g. PERCENTILE_CONT) that require exactly one ordering column.</summary>
		public interface IOnlyOrderByPart
		{
			/// <summary>Specifies the single ORDER BY column for the WITHIN GROUP clause.</summary>
			IOrderedSetFilter<TValue> OrderBy<TValue>(TValue     orderBy);
			/// <summary>Specifies the single ORDER BY column with NULLS position for the WITHIN GROUP clause.</summary>
			IOrderedSetFilter<TValue> OrderBy<TValue>(TValue     orderBy, Sql.NullsPosition nulls);
			/// <summary>Specifies the single ORDER BY column (descending) for the WITHIN GROUP clause.</summary>
			IOrderedSetFilter<TValue> OrderByDesc<TValue>(TValue orderBy);
			/// <summary>Specifies the single ORDER BY column (descending) with NULLS position for the WITHIN GROUP clause.</summary>
			IOrderedSetFilter<TValue> OrderByDesc<TValue>(TValue orderBy, Sql.NullsPosition nulls);
		}

		/// <summary>Provides ORDER BY for ordered-set aggregate functions (e.g. PERCENTILE_DISC) that allow multiple ordering columns.</summary>
		public interface IMultipleOrderByPart
		{
			/// <summary>Specifies the first ORDER BY column for the WITHIN GROUP clause.</summary>
			IMultipleThenByPart<TValue> OrderBy<TValue>(TValue     orderBy);
			/// <summary>Specifies the first ORDER BY column with NULLS position for the WITHIN GROUP clause.</summary>
			IMultipleThenByPart<TValue> OrderBy<TValue>(TValue     orderBy, Sql.NullsPosition nulls);
			/// <summary>Specifies the first ORDER BY column (descending) for the WITHIN GROUP clause.</summary>
			IMultipleThenByPart<TValue> OrderByDesc<TValue>(TValue orderBy);
			/// <summary>Specifies the first ORDER BY column (descending) with NULLS position for the WITHIN GROUP clause.</summary>
			IMultipleThenByPart<TValue> OrderByDesc<TValue>(TValue orderBy, Sql.NullsPosition nulls);
		}

		/// <summary>Provides additional ORDER BY columns after the first one in ordered-set aggregates, plus an optional <c>FILTER (WHERE ...)</c>.</summary>
		public interface IMultipleThenByPart<out TValue> : IDefinedFunction<TValue>, IFilterPart<IDefinedFunction<TValue>>
		{
			/// <summary>Adds an additional ORDER BY column.</summary>
			IMultipleThenByPart<TValue> ThenBy(object?     orderBy);
			/// <summary>Adds an additional ORDER BY column with NULLS position.</summary>
			IMultipleThenByPart<TValue> ThenBy(object?     orderBy, Sql.NullsPosition nulls);
			/// <summary>Adds an additional ORDER BY column (descending).</summary>
			IMultipleThenByPart<TValue> ThenByDesc(object? orderBy);
			/// <summary>Adds an additional ORDER BY column (descending) with NULLS position.</summary>
			IMultipleThenByPart<TValue> ThenByDesc(object? orderBy, Sql.NullsPosition nulls);
		}

		/// <summary>Within-group ORDER BY for a <b>windowed</b> single-key ordered-set aggregate (<c>Sql.Window.PercentileCont</c>): the sort key, then an optional <c>OVER (PARTITION BY ...)</c>.</summary>
		public interface IOrderedSetWindowSingleOrder
		{
			/// <summary>Specifies the single WITHIN GROUP ORDER BY column.</summary>
			IOrderedSetWindowPartition<TValue> OrderBy<TValue>(TValue     orderBy);
			/// <summary>Specifies the single WITHIN GROUP ORDER BY column with NULLS position.</summary>
			IOrderedSetWindowPartition<TValue> OrderBy<TValue>(TValue     orderBy, Sql.NullsPosition nulls);
			/// <summary>Specifies the single WITHIN GROUP ORDER BY column (descending).</summary>
			IOrderedSetWindowPartition<TValue> OrderByDesc<TValue>(TValue orderBy);
			/// <summary>Specifies the single WITHIN GROUP ORDER BY column (descending) with NULLS position.</summary>
			IOrderedSetWindowPartition<TValue> OrderByDesc<TValue>(TValue orderBy, Sql.NullsPosition nulls);
		}

		/// <summary>Optional <c>OVER (PARTITION BY ...)</c> for a windowed single-key ordered-set aggregate, then completes.</summary>
		public interface IOrderedSetWindowPartition<out TValue> : IPartitionPart<IDefinedFunction<TValue>>, IDefinedFunction<TValue>
		{
		}

		/// <summary>Within-group ORDER BY for a <b>windowed</b> multi-key ordered-set aggregate (<c>Sql.Window.PercentileDisc</c>): the first sort key, then ThenBy/ThenByDesc, then an optional <c>OVER (PARTITION BY ...)</c>.</summary>
		public interface IOrderedSetWindowMultiOrder
		{
			/// <summary>Specifies the first WITHIN GROUP ORDER BY column.</summary>
			IOrderedSetWindowThenBy<TValue> OrderBy<TValue>(TValue     orderBy);
			/// <summary>Specifies the first WITHIN GROUP ORDER BY column with NULLS position.</summary>
			IOrderedSetWindowThenBy<TValue> OrderBy<TValue>(TValue     orderBy, Sql.NullsPosition nulls);
			/// <summary>Specifies the first WITHIN GROUP ORDER BY column (descending).</summary>
			IOrderedSetWindowThenBy<TValue> OrderByDesc<TValue>(TValue orderBy);
			/// <summary>Specifies the first WITHIN GROUP ORDER BY column (descending) with NULLS position.</summary>
			IOrderedSetWindowThenBy<TValue> OrderByDesc<TValue>(TValue orderBy, Sql.NullsPosition nulls);
		}

		/// <summary>Additional WITHIN GROUP ORDER BY columns for a windowed ordered-set aggregate, plus an optional <c>OVER (PARTITION BY ...)</c>.</summary>
		public interface IOrderedSetWindowThenBy<out TValue> : IPartitionPart<IDefinedFunction<TValue>>, IDefinedFunction<TValue>
		{
			/// <summary>Adds an additional WITHIN GROUP ORDER BY column.</summary>
			IOrderedSetWindowThenBy<TValue> ThenBy(object?     orderBy);
			/// <summary>Adds an additional WITHIN GROUP ORDER BY column with NULLS position.</summary>
			IOrderedSetWindowThenBy<TValue> ThenBy(object?     orderBy, Sql.NullsPosition nulls);
			/// <summary>Adds an additional WITHIN GROUP ORDER BY column (descending).</summary>
			IOrderedSetWindowThenBy<TValue> ThenByDesc(object? orderBy);
			/// <summary>Adds an additional WITHIN GROUP ORDER BY column (descending) with NULLS position.</summary>
			IOrderedSetWindowThenBy<TValue> ThenByDesc(object? orderBy, Sql.NullsPosition nulls);
		}

		/// <summary>Provides additional ORDER BY columns via ThenBy/ThenByDesc.</summary>
		public interface IThenOrderPart<out TThenPart>
			where TThenPart : class
		{
			/// <summary>Adds an additional ORDER BY column.</summary>
			TThenPart ThenBy(object?     orderBy);
			/// <summary>Adds an additional ORDER BY column with NULLS position.</summary>
			TThenPart ThenBy(object?     orderBy, Sql.NullsPosition nulls);
			/// <summary>Adds an additional ORDER BY column (descending).</summary>
			TThenPart ThenByDesc(object? orderBy);
			/// <summary>Adds an additional ORDER BY column (descending) with NULLS position.</summary>
			TThenPart ThenByDesc(object? orderBy, Sql.NullsPosition nulls);
		}

		/// <summary>Provides the PARTITION BY clause.</summary>
		public interface IPartitionPart<out TPartitioned>
		where TPartitioned: class
		{
			/// <summary>Adds a <c>PARTITION BY</c> clause with one or more partition expressions.</summary>
			TPartitioned PartitionBy(params object?[] partitionBy);
		}

		/// <summary>A reusable window definition created by <see cref="DefineWindow"/>. Pass to <c>UseWindow()</c> to reference it.</summary>
		public interface IDefinedWindow {}

		/// <summary>Provides frame type selection: ROWS, RANGE, or GROUPS with BETWEEN syntax.</summary>
		public interface IFramePartFunction
		{
			/// <summary>Starts a <c>ROWS BETWEEN</c> frame specification. Chain with boundary definitions.</summary>
			IBoundaryPart<IRangePrecedingPartFunction> RowsBetween   { get; }
			/// <summary>Starts a <c>RANGE BETWEEN</c> frame specification. Chain with boundary definitions.</summary>
			IBoundaryPart<IRangePrecedingPartFunction> RangeBetween  { get; }
			/// <summary>Starts a <c>GROUPS BETWEEN</c> frame specification. May not be supported by all providers.</summary>
			IBoundaryPart<IRangePrecedingPartFunction> GroupsBetween { get; }

			/// <summary>
			/// Shortcut for the common <c>ROWS BETWEEN &lt;preceding&gt; PRECEDING AND &lt;following&gt; FOLLOWING</c> frame.
			/// </summary>
			/// <remarks>
			/// <b>Syntax:</b> <c>ROWS BETWEEN &lt;preceding&gt; PRECEDING AND &lt;following&gt; FOLLOWING</c><br/>
			/// <b>C# usage:</b>
			/// <code>
			/// Sql.Window.Sum(t.Value, f => f.OrderBy(t.Id).RowsBetweenValues(1, 2))
			/// </code>
			/// <b>Generated SQL:</b>
			/// <code>
			/// SUM(t.Value) OVER (ORDER BY t.Id ROWS BETWEEN 1 PRECEDING AND 2 FOLLOWING)
			/// </code>
			/// </remarks>
			IDefinedRangeFrameFunction RowsBetweenValues  (object? preceding, object? following);
			/// <summary>
			/// Shortcut for the common <c>RANGE BETWEEN &lt;preceding&gt; PRECEDING AND &lt;following&gt; FOLLOWING</c> frame.
			/// </summary>
			/// <remarks>
			/// <b>Syntax:</b> <c>RANGE BETWEEN &lt;preceding&gt; PRECEDING AND &lt;following&gt; FOLLOWING</c><br/>
			/// <b>C# usage:</b>
			/// <code>
			/// Sql.Window.Sum(t.Value, f => f.OrderBy(t.Id).RangeBetweenValues(1, 2))
			/// </code>
			/// <b>Generated SQL:</b>
			/// <code>
			/// SUM(t.Value) OVER (ORDER BY t.Id RANGE BETWEEN 1 PRECEDING AND 2 FOLLOWING)
			/// </code>
			/// </remarks>
			IDefinedRangeFrameFunction RangeBetweenValues (object? preceding, object? following);
			/// <summary>
			/// Shortcut for the common <c>GROUPS BETWEEN &lt;preceding&gt; PRECEDING AND &lt;following&gt; FOLLOWING</c> frame. May not be supported by all providers.
			/// </summary>
			/// <remarks>
			/// <b>Syntax:</b> <c>GROUPS BETWEEN &lt;preceding&gt; PRECEDING AND &lt;following&gt; FOLLOWING</c><br/>
			/// <b>C# usage:</b>
			/// <code>
			/// Sql.Window.Sum(t.Value, f => f.OrderBy(t.Id).GroupsBetweenValues(1, 2))
			/// </code>
			/// <b>Generated SQL:</b>
			/// <code>
			/// SUM(t.Value) OVER (ORDER BY t.Id GROUPS BETWEEN 1 PRECEDING AND 2 FOLLOWING)
			/// </code>
			/// </remarks>
			IDefinedRangeFrameFunction GroupsBetweenValues(object? preceding, object? following);
		}

		/// <summary>Provides frame boundary options: UNBOUNDED, CURRENT ROW, or a directed value offset.</summary>
		public interface IBoundaryPart<TBoundaryDefined>
		{
			/// <summary>Specifies <c>UNBOUNDED PRECEDING</c> (start) or <c>UNBOUNDED FOLLOWING</c> (end) boundary.</summary>
			TBoundaryDefined Unbounded  { get; }
			/// <summary>Specifies <c>CURRENT ROW</c> boundary.</summary>
			TBoundaryDefined CurrentRow { get; }
			/// <summary>
			/// Specifies an <c>N PRECEDING</c> value offset boundary. Valid at either the start or the end of the frame.
			/// </summary>
			/// <remarks>
			/// <b>Syntax:</b> <c>&lt;offset&gt; PRECEDING</c><br/>
			/// <b>C# usage:</b>
			/// <code>
			/// // ROWS BETWEEN 5 PRECEDING AND 2 PRECEDING
			/// Sql.Window.Sum(t.Value, f => f.OrderBy(t.Id).RowsBetween.ValuePreceding(5).And.ValuePreceding(2))
			/// </code>
			/// <b>Generated SQL:</b>
			/// <code>
			/// SUM(t.Value) OVER (ORDER BY t.Id ROWS BETWEEN 5 PRECEDING AND 2 PRECEDING)
			/// </code>
			/// </remarks>
			TBoundaryDefined ValuePreceding(object? offset);
			/// <summary>
			/// Specifies an <c>N FOLLOWING</c> value offset boundary. Valid at either the start or the end of the frame.
			/// </summary>
			/// <remarks>
			/// <b>Syntax:</b> <c>&lt;offset&gt; FOLLOWING</c><br/>
			/// <b>C# usage:</b>
			/// <code>
			/// // ROWS BETWEEN 1 FOLLOWING AND 3 FOLLOWING
			/// Sql.Window.Sum(t.Value, f => f.OrderBy(t.Id).RowsBetween.ValueFollowing(1).And.ValueFollowing(3))
			/// </code>
			/// <b>Generated SQL:</b>
			/// <code>
			/// SUM(t.Value) OVER (ORDER BY t.Id ROWS BETWEEN 1 FOLLOWING AND 3 FOLLOWING)
			/// </code>
			/// </remarks>
			TBoundaryDefined ValueFollowing(object? offset);
		}

		/// <summary>Provides the AND separator between start and end frame boundaries.</summary>
		public interface IRangePrecedingPartFunction
		{
			/// <summary>Separates start and end boundaries in a <c>BETWEEN ... AND ...</c> frame clause.</summary>
			IBoundaryPart<IDefinedRangeFrameFunction> And { get; }
		}

		/// <summary>Provides optional frame exclusion after the frame boundary specification.</summary>
		public interface IDefinedRangeFrameFunction : IDefinedFunction
		{
			/// <summary>Adds <c>EXCLUDE CURRENT ROW</c> to the frame clause. May not be supported by all providers.</summary>
			public IDefinedFunction ExcludeCurrentRow();
			/// <summary>Adds <c>EXCLUDE GROUP</c> to the frame clause. May not be supported by all providers.</summary>
			public IDefinedFunction ExcludeGroup();
			/// <summary>Adds <c>EXCLUDE TIES</c> to the frame clause. May not be supported by all providers.</summary>
			public IDefinedFunction ExcludeTies();
		}

		/// <summary>Terminal state after OrderBy: allows ThenBy or completes the definition.</summary>
		public interface IThenByPartFinal : IThenOrderPart<IThenByPartFinal>, IDefinedFunction
		{
		}

		/// <summary>State requiring OrderBy, then allows ThenBy. Used after PartitionBy in ranking functions.</summary>
		public interface IROrderByPartOThenByPartFinal : IOrderByPart<IThenByPartFinal>
		{
		}

		/// <summary>Provides the <c>DISTINCT</c> aggregate modifier for aggregate window functions (COUNT(expr)/SUM/AVG/MIN/MAX).</summary>
		public interface IDistinctPart<out TNext>
			where TNext : class
		{
			/// <summary>Adds the <c>DISTINCT</c> aggregate modifier — e.g. <c>SUM(DISTINCT x) OVER (...)</c>. Not supported by every provider.</summary>
			TNext Distinct();
		}

		/// <summary>Provides the ability to reference a predefined window definition.</summary>
		public interface IUseWindow<TWithWindowPart>
		{
			/// <summary>References a window definition created by <see cref="DefineWindow"/>. Allows sharing a single window specification across multiple function calls.</summary>
			public TWithWindowPart UseWindow(IDefinedWindow window);
		}

		/// <summary>Provides KEEP (DENSE_RANK FIRST/LAST) clause for aggregate window functions. Oracle-specific.</summary>
		public interface IKeepPart<out TKeepOrderBy>
			where TKeepOrderBy : class
		{
			/// <summary>
			/// Adds <c>KEEP (DENSE_RANK FIRST ORDER BY ...)</c> clause. Aggregates over the first-ranked group.
			/// Chain with <c>.OrderBy(...)[.ThenBy(...)][.PartitionBy(...)]</c>.
			/// </summary>
			/// <example>
			/// <code>
			/// Sql.Window.Min(t.Salary, f =&gt; f.KeepFirst().OrderBy(t.HireDate).PartitionBy(t.Dept))
			/// </code>
			/// </example>
			TKeepOrderBy KeepFirst();
			/// <summary>
			/// Adds <c>KEEP (DENSE_RANK LAST ORDER BY ...)</c> clause. Aggregates over the last-ranked group.
			/// Chain with <c>.OrderBy(...)[.ThenBy(...)][.PartitionBy(...)]</c>.
			/// </summary>
			TKeepOrderBy KeepLast();
		}

		/// <summary>
		/// Builder state after KeepFirst/KeepLast — requires mandatory ORDER BY.
		/// <para>Chain: <c>.OrderBy(...)[.ThenBy(...)][.PartitionBy(...)]</c></para>
		/// </summary>
		public interface IKeepOrderByRequired : IOrderByPart<IKeepThenByOrPartitionFinal>
		{
		}

		/// <summary>Builder state after KEEP ORDER BY — allows ThenBy, optional PartitionBy, or completes.</summary>
		public interface IKeepThenByOrPartitionFinal : IThenOrderPart<IKeepThenByOrPartitionFinal>, IPartitionPart<IDefinedFunction>, IDefinedFunction
		{
		}

		#region DefineWindow

		/// <summary>Builder interface for <see cref="DefineWindow"/>. Allows specifying PartitionBy, OrderBy, and frame clauses.</summary>
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

		/// <summary>
		/// Builder for ranking functions: ROW_NUMBER, RANK, DENSE_RANK, PERCENT_RANK, CUME_DIST, NTILE, LEAD, LAG.
		/// <para>Chain: <c>[UseWindow(wnd)] | [PartitionBy(...)].OrderBy(...)[.ThenBy(...)]</c></para>
		/// <para>ORDER BY is mandatory. FILTER and frame clauses are not available.</para>
		/// </summary>
		public interface IOPartitionROrderFinal : IUseWindow<IDefinedFunction>, IPartitionPart<IROrderByPartOThenByPartFinal>, IROrderByPartOThenByPartFinal
		{
		}

		/// <summary>
		/// Builder for aggregate window functions with an argument: COUNT(expr), SUM, AVG, MIN, MAX.
		/// <para>Chain: <c>[.Distinct()][.Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...]</c>, or <c>.UseWindow(...)</c>.</para>
		/// <para>Optional <c>DISTINCT</c> aggregate modifier, then the standard aggregate clauses (single-shot Distinct). <c>UseWindow</c> applies a reusable window definition (from <c>DefineWindow</c>) and is an alternative to the inline clauses, not combined with them.</para>
		/// </summary>
		public interface IAggregateFinal : IDistinctPart<IOFilterOPartitionOOrderOFrameFinal>, IOFilterOPartitionOOrderOFrameFinal
		{
		}

		/// <summary>
		/// Builder for two-argument statistical window aggregates: COVAR_POP/COVAR_SAMP, CORR, REGR_*.
		/// <para>Chain: <c>[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...]</c>, or <c>.UseWindow(...)</c>.</para>
		/// <para>Supports FILTER, frame specification, and UseWindow. Does NOT support DISTINCT or KEEP — neither is valid on two-argument aggregates.</para>
		/// </summary>
		public interface IBivariateAggregateFinal : IFilterPart<IOPartitionOOrderOFrameFinal>, IOPartitionOOrderOFrameFinal, IUseWindow<IDefinedFunction>
		{
		}

		/// <summary>State providing PartitionBy only, then completes.</summary>
		public interface IOPartitionFinal : IPartitionPart<IDefinedFunction>, IDefinedFunction
		{
		}

		/// <summary>
		/// Builder for aggregate window functions: SUM, AVG, MIN, MAX.
		/// <para>Chain: <c>[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...]</c>, or <c>.UseWindow(...)</c>.</para>
		/// <para>Or with KEEP: <c>[.KeepFirst()|.KeepLast()].OrderBy(...)[.ThenBy(...)][.PartitionBy(...)]</c></para>
		/// <para>All clauses are optional. Supports FILTER, frame specification, UseWindow, and KEEP (Oracle) — UseWindow (a reusable window definition from <c>DefineWindow</c>) and KEEP are alternatives to the inline clauses, not combined with them.</para>
		/// </summary>
		public interface IOFilterOPartitionOOrderOFrameFinal : IFilterPart<IOPartitionOOrderOFrameFinal>, IOPartitionOOrderOFrameFinal, IUseWindow<IDefinedFunction>, IKeepPart<IKeepOrderByRequired>
		{
		}

		/// <summary>
		/// Builder for value window functions: FIRST_VALUE, LAST_VALUE, NTH_VALUE.
		/// <para>Chain: <c>[PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...]</c>, or <c>.UseWindow(...)</c>.</para>
		/// <para>Supports frame specification and UseWindow (a reusable window definition from <c>DefineWindow</c>, an alternative to the inline clauses). Does NOT support FILTER clause.</para>
		/// </summary>
		public interface IOPartitionOOrderOFrameWithWindowFinal : IOPartitionOOrderOFrameFinal, IUseWindow<IDefinedFunction>
		{
		}

		/// <summary>State providing PartitionBy, OrderBy, and frame specification.</summary>
		public interface IOPartitionOOrderOFrameFinal : IPartitionPart<IOrderOFrameFinal>, IOrderOFrameFinal
		{
		}

		/// <summary>Terminal state after OrderBy in frame-capable chains: allows frame specification or completes.</summary>
		public interface IOFrameFinal : IFramePartFunction, IDefinedFunction
		{
		}

		/// <summary>State after OrderBy in frame-capable chains: allows ThenBy, frame specification, or completes.</summary>
		public interface IOThenPartOFrameFinal : IThenOrderPart<IOThenPartOFrameFinal>, IOFrameFinal
		{
		}

		/// <summary>State providing OrderBy in frame-capable chains.</summary>
		public interface IOrderOFrameFinal : IOrderByPart<IOThenPartOFrameFinal>, IOFrameFinal
		{
		}

		/// <summary>Provides function-level null treatment (<c>IGNORE NULLS</c> / <c>RESPECT NULLS</c>) for value/offset window functions.</summary>
		public interface INullTreatmentPart<out TNext>
			where TNext : class
		{
			/// <summary>Adds <c>IGNORE NULLS</c> — the function skips NULL values. Not supported by every provider.</summary>
			TNext IgnoreNulls();
			/// <summary>Adds <c>RESPECT NULLS</c> — the SQL default. Provided for explicitness; emits nothing on providers where it is the default.</summary>
			TNext RespectNulls();
		}

		/// <summary>Provides the <c>FROM FIRST</c> / <c>FROM LAST</c> modifier for <c>NTH_VALUE</c>.</summary>
		public interface IFromPart<out TNext>
			where TNext : class
		{
			/// <summary>Adds <c>FROM FIRST</c> — counts position from the first row of the frame. The SQL default; emits nothing.</summary>
			TNext FromFirst();
			/// <summary>Adds <c>FROM LAST</c> — counts position from the last row of the frame. Not supported by every provider.</summary>
			TNext FromLast();
		}

		/// <summary>
		/// Builder for offset window functions: LEAD, LAG.
		/// <para>Chain: <c>[.IgnoreNulls()|.RespectNulls()] [UseWindow(wnd)] | [PartitionBy(...)].OrderBy(...)[.ThenBy(...)]</c></para>
		/// <para>Optional null treatment, then mandatory ORDER BY (via the inherited chain).</para>
		/// </summary>
		public interface ILeadLagFinal : INullTreatmentPart<IOPartitionROrderFinal>, IOPartitionROrderFinal
		{
		}

		/// <summary>
		/// Builder for value window functions: FIRST_VALUE, LAST_VALUE.
		/// <para>Chain: <c>[.IgnoreNulls()|.RespectNulls()] [UseWindow(wnd)] | [PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...]</c></para>
		/// </summary>
		public interface IValueFinal : INullTreatmentPart<IOPartitionOOrderOFrameWithWindowFinal>, IOPartitionOOrderOFrameWithWindowFinal
		{
		}

		/// <summary>
		/// Builder for NTH_VALUE. Optional <c>FROM FIRST/LAST</c> then optional <c>IGNORE/RESPECT NULLS</c> (in SQL order), then the window spec.
		/// <para>Chain: <c>[.FromFirst()|.FromLast()] [.IgnoreNulls()|.RespectNulls()] [UseWindow(wnd)] | [PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...]</c></para>
		/// </summary>
		public interface INthValueFinal : IFromPart<INthValueNullsStep>, INullTreatmentPart<IOPartitionOOrderOFrameWithWindowFinal>, IOPartitionOOrderOFrameWithWindowFinal
		{
		}

		/// <summary>Builder state after <c>FROM FIRST/LAST</c> on NTH_VALUE — allows optional <c>IGNORE/RESPECT NULLS</c>, then the window spec.</summary>
		public interface INthValueNullsStep : INullTreatmentPart<IOPartitionOOrderOFrameWithWindowFinal>, IOPartitionOOrderOFrameWithWindowFinal
		{
		}

		#endregion Predefined chains

		// public static object DefineWindow(this Sql.IWindowFunction window, Func<IWindowDefinition, object> func)
		// 	=> throw new ServerSideOnlyException(nameof(RowNumber))

		#region Ranking: RowNumber, Rank, DenseRank, PercentRank, CumeDist, NTile

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
		public static T Lead<T>(this Sql.IWindowFunction window, T expr, Func<ILeadLagFinal, IDefinedFunction> func)
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
		public static T Lead<T>(this Sql.IWindowFunction window, T expr, int offset, Func<ILeadLagFinal, IDefinedFunction> func)
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
		public static T Lead<T>(this Sql.IWindowFunction window, T expr, int offset, T @default, Func<ILeadLagFinal, IDefinedFunction> func)
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
		public static T Lag<T>(this Sql.IWindowFunction window, T expr, Func<ILeadLagFinal, IDefinedFunction> func)
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
		public static T Lag<T>(this Sql.IWindowFunction window, T expr, int offset, Func<ILeadLagFinal, IDefinedFunction> func)
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
		public static T Lag<T>(this Sql.IWindowFunction window, T expr, int offset, T @default, Func<ILeadLagFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Lag));

		#endregion

		#region FirstValue/LastValue/NthValue

		/// <summary>
		/// Generates SQL <c>FIRST_VALUE()</c> window function. Returns the first value in the frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.FirstValue(expr, f =&gt; f.[IgnoreNulls()].[PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>May not be supported by all database providers. Does not support the FILTER clause.</para>
		/// <para>Use <c>.IgnoreNulls()</c> for <c>FIRST_VALUE(x) IGNORE NULLS</c> (skips NULLs). Supported only where the provider allows it; otherwise a translation-time error is thrown.</para>
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
		public static T FirstValue<T>(this Sql.IWindowFunction window, T expr, Func<IValueFinal, IDefinedFunction> func)
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
		public static T LastValue<T>(this Sql.IWindowFunction window, T expr, Func<IValueFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(LastValue));

		/// <summary>
		/// Generates SQL <c>NTH_VALUE()</c> window function. Returns the value at position <paramref name="n"/> in the frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.NthValue(expr, n, f =&gt; f.[FromLast()].[IgnoreNulls()].[PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>May not be supported by all database providers (e.g. SQL Server). Does not support the FILTER clause.</para>
		/// <para>Use <c>.FromLast()</c> and/or <c>.IgnoreNulls()</c> (in that order) for <c>NTH_VALUE(x, n) FROM LAST IGNORE NULLS</c>. Each is supported only where the provider allows it; otherwise a translation-time error is thrown.</para>
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
		public static T NthValue<T>(this Sql.IWindowFunction window, T expr, long n, Func<INthValueFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(NthValue));

		#endregion

		#region Count

		/// <summary>
		/// Generates SQL <c>COUNT(*)</c> window function. Use the <c>Count(expr, ...)</c> overload for <c>COUNT(expr)</c>.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Count(f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Total   = Sql.Window.Count(f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Id)),
		///         Running = Sql.Window.Count(f =&gt; f.OrderBy(t.Id).RowsBetween.Unbounded.And.CurrentRow),
		///         Filt    = Sql.Window.Count(f =&gt; f.Filter(t.Value &gt; 10).PartitionBy(t.Dept).OrderBy(t.Id)),
		///     };
		/// </code>
		/// <para><b>Generated SQL (PostgreSQL):</b></para>
		/// <code>
		/// SELECT
		///   COUNT(*) OVER (PARTITION BY t.Dept ORDER BY t.Id),
		///   COUNT(*) OVER (ORDER BY t.Id ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
		///   COUNT(*) FILTER (WHERE t.Value &gt; 10) OVER (PARTITION BY t.Dept ORDER BY t.Id)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static int Count(this Sql.IWindowFunction window, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Count));

		/// <summary>
		/// Generates SQL <c>COUNT(expr)</c> window function. Use <c>.Distinct()</c> for <c>COUNT(DISTINCT expr)</c>.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Count(expr, f =&gt; f.[Distinct()][.Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para><c>DISTINCT</c> in a window aggregate is not supported by most providers; where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         NonNull  = Sql.Window.Count(t.NullableValue, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Id)),
		///         Distinct = Sql.Window.Count(t.Value, f =&gt; f.Distinct().PartitionBy(t.Dept)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   COUNT(t.NullableValue) OVER (PARTITION BY t.Dept ORDER BY t.Id),
		///   COUNT(DISTINCT t.Value) OVER (PARTITION BY t.Dept)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static int Count(this Sql.IWindowFunction window, object? argument, Func<IAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Count));

		/// <summary>
		/// Generates SQL <c>COUNT(*)</c> window function returning a 64-bit count. Use the <c>LongCount(expr, ...)</c> overload for <c>COUNT(expr)</c>.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.LongCount(f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Identical SQL to <see cref="Count(Sql.IWindowFunction, Func{IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction})"/> (<c>COUNT(*)</c>), returning <see cref="long"/>.</para>
		/// </remarks>
		public static long LongCount(this Sql.IWindowFunction window, Func<IOFilterOPartitionOOrderOFrameFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(LongCount));

		/// <summary>
		/// Generates SQL <c>COUNT(expr)</c> window function returning a 64-bit count. Use <c>.Distinct()</c> for <c>COUNT(DISTINCT expr)</c>.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.LongCount(expr, f =&gt; f.[Distinct()][.Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Identical SQL to <see cref="Count(Sql.IWindowFunction, object?, Func{IAggregateFinal, IDefinedFunction})"/> (<c>COUNT(expr)</c>), returning <see cref="long"/>.</para>
		/// </remarks>
		public static long LongCount(this Sql.IWindowFunction window, object? argument, Func<IAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(LongCount));

		#endregion Count

		#region Sum

		/// <summary>
		/// Generates SQL <c>SUM()</c> window function. Computes the sum of values within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Sum(expr, f =&gt; f.[Distinct()][.Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para>Use <c>.Distinct()</c> for <c>SUM(DISTINCT x) OVER (...)</c>. <c>DISTINCT</c> in a window aggregate is not supported by most providers; where unsupported it throws a descriptive exception at query-translation time.</para>
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
		///         Sum5 = Sql.Window.Sum(t.Salary, f =&gt; f.Distinct().PartitionBy(t.Dept)),
		///     };
		/// </code>
		/// <para><b>Generated SQL (PostgreSQL):</b></para>
		/// <code>
		/// SELECT
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
		///   SUM(t.Salary) FILTER (WHERE t.IsActive) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   SUM(DISTINCT t.Salary) OVER (PARTITION BY t.Dept)
		/// FROM Table t
		/// </code>
		/// </remarks>
		public static int Sum(this Sql.IWindowFunction window, int argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static int? Sum(this Sql.IWindowFunction window, int? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static long Sum(this Sql.IWindowFunction window, long argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static long? Sum(this Sql.IWindowFunction window, long? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static double Sum(this Sql.IWindowFunction window, double argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static double? Sum(this Sql.IWindowFunction window, double? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static decimal Sum(this Sql.IWindowFunction window, decimal argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static decimal? Sum(this Sql.IWindowFunction window, decimal? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static float Sum(this Sql.IWindowFunction window, float argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static float? Sum(this Sql.IWindowFunction window, float? argument, Func<IAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Sum));

		#endregion Sum

		#region Average

		/// <summary>
		/// Generates SQL <c>AVG()</c> window function. Computes the average within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Average(expr, f =&gt; f.[Distinct()][.Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para>Use <c>.Distinct()</c> for <c>AVG(DISTINCT x) OVER (...)</c>. <c>DISTINCT</c> in a window aggregate is not supported by most providers; where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Avg1 = Sql.Window.Average(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Avg2 = Sql.Window.Average(t.Salary, f =&gt; f.Filter(t.IsActive).OrderBy(t.Date)),
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.ValuePreceding(3).And.CurrentRow),
		///         Avg4 = Sql.Window.Average(t.Salary, f =&gt; f.Distinct().PartitionBy(t.Dept)),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   AVG(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   AVG(CASE WHEN t.IsActive THEN t.Salary ELSE NULL END) OVER (ORDER BY t.Date),
		///   AVG(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN 3 PRECEDING AND CURRENT ROW),
		///   AVG(DISTINCT t.Salary) OVER (PARTITION BY t.Dept)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static double Average(this Sql.IWindowFunction window, int argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.ValuePreceding(3).And.CurrentRow),
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
		public static double? Average(this Sql.IWindowFunction window, int? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.ValuePreceding(3).And.CurrentRow),
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
		public static double Average(this Sql.IWindowFunction window, long argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.ValuePreceding(3).And.CurrentRow),
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
		public static double? Average(this Sql.IWindowFunction window, long? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.ValuePreceding(3).And.CurrentRow),
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
		public static double Average(this Sql.IWindowFunction window, double argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.ValuePreceding(3).And.CurrentRow),
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
		public static double? Average(this Sql.IWindowFunction window, double? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.ValuePreceding(3).And.CurrentRow),
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
		public static decimal Average(this Sql.IWindowFunction window, decimal argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.ValuePreceding(3).And.CurrentRow),
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
		public static decimal? Average(this Sql.IWindowFunction window, decimal? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.ValuePreceding(3).And.CurrentRow),
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
		public static float Average(this Sql.IWindowFunction window, float argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.ValuePreceding(3).And.CurrentRow),
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
		public static float? Average(this Sql.IWindowFunction window, float? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.ValuePreceding(3).And.CurrentRow),
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
		public static double Average(this Sql.IWindowFunction window, short argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.ValuePreceding(3).And.CurrentRow),
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
		public static double? Average(this Sql.IWindowFunction window, short? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.ValuePreceding(3).And.CurrentRow),
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
		public static double Average(this Sql.IWindowFunction window, byte argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		///         Avg3 = Sql.Window.Average(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.ValuePreceding(3).And.CurrentRow),
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
		public static double? Average(this Sql.IWindowFunction window, byte? argument, Func<IAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Average));

		#endregion Average

		#region Min

		/// <summary>
		/// Generates SQL <c>MIN()</c> window function. Returns the minimum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Min(expr, f =&gt; f.[Distinct()][.Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para>Use <c>.Distinct()</c> for <c>MIN(DISTINCT x) OVER (...)</c>. <c>DISTINCT</c> in a window aggregate is not supported by most providers; where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Min1 = Sql.Window.Min(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Min2 = Sql.Window.Min(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///         Min3 = Sql.Window.Min(t.Salary, f =&gt; f.Distinct().PartitionBy(t.Dept)),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MIN(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MIN(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
		///   MIN(DISTINCT t.Salary) OVER (PARTITION BY t.Dept)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static int Min(this Sql.IWindowFunction window, int argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static int? Min(this Sql.IWindowFunction window, int? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static long Min(this Sql.IWindowFunction window, long argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static long? Min(this Sql.IWindowFunction window, long? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static double Min(this Sql.IWindowFunction window, double argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static double? Min(this Sql.IWindowFunction window, double? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static decimal Min(this Sql.IWindowFunction window, decimal argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static decimal? Min(this Sql.IWindowFunction window, decimal? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static float Min(this Sql.IWindowFunction window, float argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static float? Min(this Sql.IWindowFunction window, float? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static short Min(this Sql.IWindowFunction window, short argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static short? Min(this Sql.IWindowFunction window, short? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static byte Min(this Sql.IWindowFunction window, byte argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static byte? Min(this Sql.IWindowFunction window, byte? argument, Func<IAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Min));

		#endregion Min

		#region Max

		/// <summary>
		/// Generates SQL <c>MAX()</c> window function. Returns the maximum value within the window frame.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Max(expr, f =&gt; f.[Distinct()][.Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>The FILTER clause is natively supported by PostgreSQL. For other providers, it is emulated using CASE WHEN.</para>
		/// <para>Use <c>.Distinct()</c> for <c>MAX(DISTINCT x) OVER (...)</c>. <c>DISTINCT</c> in a window aggregate is not supported by most providers; where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     select new
		///     {
		///         Max1 = Sql.Window.Max(t.Salary, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date)),
		///         Max2 = Sql.Window.Max(t.Salary, f =&gt; f.OrderBy(t.Date).RowsBetween.Unbounded.And.CurrentRow),
		///         Max3 = Sql.Window.Max(t.Salary, f =&gt; f.Distinct().PartitionBy(t.Dept)),
		///     };
		///
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT
		///   MAX(t.Salary) OVER (PARTITION BY t.Dept ORDER BY t.Date),
		///   MAX(t.Salary) OVER (ORDER BY t.Date ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
		///   MAX(DISTINCT t.Salary) OVER (PARTITION BY t.Dept)
		/// FROM Table t
		///
		/// </code>
		/// </remarks>
		public static int Max(this Sql.IWindowFunction window, int argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static int? Max(this Sql.IWindowFunction window, int? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static long Max(this Sql.IWindowFunction window, long argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static long? Max(this Sql.IWindowFunction window, long? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static double Max(this Sql.IWindowFunction window, double argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static double? Max(this Sql.IWindowFunction window, double? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static decimal Max(this Sql.IWindowFunction window, decimal argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static decimal? Max(this Sql.IWindowFunction window, decimal? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static float Max(this Sql.IWindowFunction window, float argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static float? Max(this Sql.IWindowFunction window, float? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static short Max(this Sql.IWindowFunction window, short argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static short? Max(this Sql.IWindowFunction window, short? argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static byte Max(this Sql.IWindowFunction window, byte argument, Func<IAggregateFinal, IDefinedFunction> func)
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
		public static byte? Max(this Sql.IWindowFunction window, byte? argument, Func<IAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Max));

		#endregion Max

		#region RatioToReport

		/// <summary>
		/// Generates the SQL <c>RATIO_TO_REPORT()</c> window function — the ratio of the value to the sum of the values
		/// within the window (<c>expr / SUM(expr) OVER (...)</c>).
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.RatioToReport(expr, f =&gt; f.[PartitionBy(...)])</c></para>
		/// <para>Emitted natively as <c>RATIO_TO_REPORT</c> on Oracle and DB2; emulated as <c>expr / SUM(expr) OVER (...)</c> on other providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// Sql.Window.RatioToReport(t.Value, f =&gt; f.PartitionBy(t.Dept))
		/// </code>
		/// <para><b>Generated SQL (Oracle):</b></para>
		/// <code>
		/// RATIO_TO_REPORT(t.Value) OVER (PARTITION BY t.Dept)
		/// </code>
		/// </remarks>
		public static double? RatioToReport<T>(this Sql.IWindowFunction window, T argument, Func<IOPartitionFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(RatioToReport));

		#endregion

		#region Median

		/// <summary>
		/// Generates the SQL <c>MEDIAN()</c> window function — the median (50th percentile, continuous) of the values within the window.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Median(expr, f =&gt; f.[PartitionBy(...)])</c></para>
		/// <para>Native on Oracle, DB2, DuckDB and MariaDB; throws a descriptive exception at query-translation time elsewhere. Its OVER clause carries <c>PARTITION BY</c> only (no ORDER BY or frame).</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// Sql.Window.Median(t.Value, f =&gt; f.PartitionBy(t.Dept))
		/// </code>
		/// <para><b>Generated SQL (Oracle):</b></para>
		/// <code>
		/// MEDIAN(t.Value) OVER (PARTITION BY t.Dept)
		/// </code>
		/// </remarks>
		public static double? Median<T>(this Sql.IWindowFunction window, T argument, Func<IOPartitionFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Median));

		#endregion

		#region StdDev/Variance

		/// <summary>
		/// Generates the SQL <c>STDDEV()</c> window function (<c>STDEV()</c> on SQL Server) — the sample standard deviation of values within the window.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.StdDev(expr, f =&gt; f.[Distinct()][.Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// Sql.Window.StdDev(t.Value, f =&gt; f.PartitionBy(t.Dept).OrderBy(t.Date))
		/// </code>
		/// <para><b>Generated SQL (Oracle):</b></para>
		/// <code>
		/// STDDEV(t.Value) OVER (PARTITION BY t.Dept ORDER BY t.Date)
		/// </code>
		/// </remarks>
		public static double? StdDev<T>(this Sql.IWindowFunction window, T argument, Func<IAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(StdDev));

		/// <summary>
		/// Generates the SQL <c>STDDEV_POP()</c> window function — the population standard deviation of values within the window.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.StdDevPop(expr, f =&gt; f.[Distinct()][.Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>STDDEV_POP(expr) OVER (...)</c></para>
		/// </remarks>
		public static double? StdDevPop<T>(this Sql.IWindowFunction window, T argument, Func<IAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(StdDevPop));

		/// <summary>
		/// Generates the SQL <c>STDDEV_SAMP()</c> window function — the sample standard deviation of values within the window.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.StdDevSamp(expr, f =&gt; f.[Distinct()][.Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>STDDEV_SAMP(expr) OVER (...)</c></para>
		/// </remarks>
		public static double? StdDevSamp<T>(this Sql.IWindowFunction window, T argument, Func<IAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(StdDevSamp));

		/// <summary>
		/// Generates the SQL <c>VARIANCE()</c> window function — the sample variance of values within the window.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Variance(expr, f =&gt; f.[Distinct()][.Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>VARIANCE(expr) OVER (...)</c></para>
		/// </remarks>
		public static double? Variance<T>(this Sql.IWindowFunction window, T argument, Func<IAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Variance));

		/// <summary>
		/// Generates the SQL <c>VAR_POP()</c> window function — the population variance of values within the window.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.VarPop(expr, f =&gt; f.[Distinct()][.Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>VAR_POP(expr) OVER (...)</c></para>
		/// </remarks>
		public static double? VarPop<T>(this Sql.IWindowFunction window, T argument, Func<IAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(VarPop));

		/// <summary>
		/// Generates the SQL <c>VAR_SAMP()</c> window function — the sample variance of values within the window.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.VarSamp(expr, f =&gt; f.[Distinct()][.Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>VAR_SAMP(expr) OVER (...)</c></para>
		/// </remarks>
		public static double? VarSamp<T>(this Sql.IWindowFunction window, T argument, Func<IAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(VarSamp));

		#endregion StdDev/Variance

		#region Covar/Corr/Regr

		/// <summary>
		/// Generates the SQL <c>COVAR_POP()</c> window function — the population covariance of the two value pairs within the window.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.CovarPop(expr1, expr2, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>COVAR_POP(expr1, expr2) OVER (...)</c></para>
		/// </remarks>
		public static double? CovarPop<T1, T2>(this Sql.IWindowFunction window, T1 argument1, T2 argument2, Func<IBivariateAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(CovarPop));

		/// <summary>
		/// Generates the SQL <c>COVAR_SAMP()</c> window function — the sample covariance of the two value pairs within the window.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.CovarSamp(expr1, expr2, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>COVAR_SAMP(expr1, expr2) OVER (...)</c></para>
		/// </remarks>
		public static double? CovarSamp<T1, T2>(this Sql.IWindowFunction window, T1 argument1, T2 argument2, Func<IBivariateAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(CovarSamp));

		/// <summary>
		/// Generates the SQL <c>CORR()</c> window function — the correlation coefficient of the two value pairs within the window.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.Corr(expr1, expr2, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>CORR(expr1, expr2) OVER (...)</c></para>
		/// </remarks>
		public static double? Corr<T1, T2>(this Sql.IWindowFunction window, T1 argument1, T2 argument2, Func<IBivariateAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(Corr));

		/// <summary>
		/// Generates the SQL <c>REGR_SLOPE()</c> window function — the slope of the least-squares-fit linear equation of (<paramref name="argument1"/>, <paramref name="argument2"/>) pairs.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.RegrSlope(y, x, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>REGR_SLOPE(y, x) OVER (...)</c></para>
		/// </remarks>
		public static double? RegrSlope<T1, T2>(this Sql.IWindowFunction window, T1 argument1, T2 argument2, Func<IBivariateAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(RegrSlope));

		/// <summary>
		/// Generates the SQL <c>REGR_INTERCEPT()</c> window function — the y-intercept of the least-squares-fit linear equation of (<paramref name="argument1"/>, <paramref name="argument2"/>) pairs.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.RegrIntercept(y, x, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>REGR_INTERCEPT(y, x) OVER (...)</c></para>
		/// </remarks>
		public static double? RegrIntercept<T1, T2>(this Sql.IWindowFunction window, T1 argument1, T2 argument2, Func<IBivariateAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(RegrIntercept));

		/// <summary>
		/// Generates the SQL <c>REGR_COUNT()</c> window function — the number of non-null (<paramref name="argument1"/>, <paramref name="argument2"/>) pairs within the window.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.RegrCount(y, x, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>REGR_COUNT(y, x) OVER (...)</c></para>
		/// </remarks>
		public static long? RegrCount<T1, T2>(this Sql.IWindowFunction window, T1 argument1, T2 argument2, Func<IBivariateAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(RegrCount));

		/// <summary>
		/// Generates the SQL <c>REGR_R2()</c> window function — the coefficient of determination (R²) of the regression of (<paramref name="argument1"/>, <paramref name="argument2"/>) pairs.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.RegrR2(y, x, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>REGR_R2(y, x) OVER (...)</c></para>
		/// </remarks>
		public static double? RegrR2<T1, T2>(this Sql.IWindowFunction window, T1 argument1, T2 argument2, Func<IBivariateAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(RegrR2));

		/// <summary>
		/// Generates the SQL <c>REGR_AVGX()</c> window function — the average of the independent variable (<paramref name="argument2"/>) over non-null pairs.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.RegrAvgX(y, x, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>REGR_AVGX(y, x) OVER (...)</c></para>
		/// </remarks>
		public static double? RegrAvgX<T1, T2>(this Sql.IWindowFunction window, T1 argument1, T2 argument2, Func<IBivariateAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(RegrAvgX));

		/// <summary>
		/// Generates the SQL <c>REGR_AVGY()</c> window function — the average of the dependent variable (<paramref name="argument1"/>) over non-null pairs.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.RegrAvgY(y, x, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>REGR_AVGY(y, x) OVER (...)</c></para>
		/// </remarks>
		public static double? RegrAvgY<T1, T2>(this Sql.IWindowFunction window, T1 argument1, T2 argument2, Func<IBivariateAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(RegrAvgY));

		/// <summary>
		/// Generates the SQL <c>REGR_SXX()</c> window function — the sum of squares of the independent variable (<paramref name="argument2"/>) over non-null pairs.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.RegrSXX(y, x, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>REGR_SXX(y, x) OVER (...)</c></para>
		/// </remarks>
		public static double? RegrSXX<T1, T2>(this Sql.IWindowFunction window, T1 argument1, T2 argument2, Func<IBivariateAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(RegrSXX));

		/// <summary>
		/// Generates the SQL <c>REGR_SYY()</c> window function — the sum of squares of the dependent variable (<paramref name="argument1"/>) over non-null pairs.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.RegrSYY(y, x, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>REGR_SYY(y, x) OVER (...)</c></para>
		/// </remarks>
		public static double? RegrSYY<T1, T2>(this Sql.IWindowFunction window, T1 argument1, T2 argument2, Func<IBivariateAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(RegrSYY));

		/// <summary>
		/// Generates the SQL <c>REGR_SXY()</c> window function — the sum of products of the independent and dependent variables over non-null pairs.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.RegrSXY(y, x, f =&gt; f.[Filter(...)][.PartitionBy(...)][.OrderBy(...)][.RowsBetween|RangeBetween...])</c></para>
		/// <para>Not supported by every provider. Where unsupported it throws a descriptive exception at query-translation time.</para>
		/// <para><b>Generated SQL:</b> <c>REGR_SXY(y, x) OVER (...)</c></para>
		/// </remarks>
		public static double? RegrSXY<T1, T2>(this Sql.IWindowFunction window, T1 argument1, T2 argument2, Func<IBivariateAggregateFinal, IDefinedFunction> func)
			=> throw new ServerSideOnlyException(nameof(RegrSXY));

		#endregion Covar/Corr/Regr

		#region PercentileCont

		/// <summary>
		/// Generates SQL <c>PERCENTILE_CONT()</c> ordered-set aggregate. Computes a percentile based on continuous distribution.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>source.PercentileCont(fraction, (e, f) =&gt; f.OrderBy(e.Column)[.Filter(...)])</c></para>
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
			=> throw new InvalidOperationException($"'{nameof(PercentileCont)}' is a server-side API. Use '{nameof(LinqExtensions.AggregateExecute)}' or '{nameof(LinqExtensions.AggregateExecuteAsync)}' to execute this function.");

#pragma warning restore RS0030

		/// <summary>
		/// Generates the SQL <c>PERCENTILE_CONT()</c> <b>windowed</b> ordered-set aggregate: <c>PERCENTILE_CONT(fraction) WITHIN GROUP (ORDER BY key) OVER (PARTITION BY ...)</c>.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.PercentileCont(fraction, w =&gt; w.OrderBy(key)[.PartitionBy(...)])</c></para>
		/// <para>The windowed form is native on SQL Server, Oracle and MariaDB; PostgreSQL supports only the group form (<c>g.PercentileCont</c>).</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// Sql.Window.PercentileCont(0.5, w =&gt; w.OrderBy(t.Salary).PartitionBy(t.Dept))
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY t.Salary) OVER (PARTITION BY t.Dept)
		/// </code>
		/// </remarks>
		public static TValue PercentileCont<TValue>(this Sql.IWindowFunction window, double fraction, Func<IOrderedSetWindowSingleOrder, IDefinedFunction<TValue>> func)
			=> throw new ServerSideOnlyException(nameof(PercentileCont));

		#endregion

		#region PercentileDisc

		/// <summary>
		/// Generates SQL <c>PERCENTILE_DISC()</c> ordered-set aggregate. Returns the value at the specified percentile from the sorted set.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>source.PercentileDisc(fraction, (e, f) =&gt; f.OrderBy(e.Column)[.ThenBy(...)][.Filter(...)])</c></para>
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
		) => throw new InvalidOperationException($"'{nameof(PercentileDisc)}' is a server-side API. Use '{nameof(LinqExtensions.AggregateExecute)}' or '{nameof(LinqExtensions.AggregateExecuteAsync)}' to execute this function.");

#pragma warning restore RS0030

		/// <summary>
		/// Generates the SQL <c>PERCENTILE_DISC()</c> <b>windowed</b> ordered-set aggregate: <c>PERCENTILE_DISC(fraction) WITHIN GROUP (ORDER BY key) OVER (PARTITION BY ...)</c>.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>Sql.Window.PercentileDisc(fraction, w =&gt; w.OrderBy(key)[.ThenBy(...)][.PartitionBy(...)])</c></para>
		/// <para>The windowed form is native on SQL Server, Oracle and MariaDB; PostgreSQL supports only the group form (<c>g.PercentileDisc</c>).</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// Sql.Window.PercentileDisc(0.5, w =&gt; w.OrderBy(t.Salary).PartitionBy(t.Dept))
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// PERCENTILE_DISC(0.5) WITHIN GROUP (ORDER BY t.Salary) OVER (PARTITION BY t.Dept)
		/// </code>
		/// </remarks>
		public static TValue PercentileDisc<TValue>(this Sql.IWindowFunction window, double fraction, Func<IOrderedSetWindowMultiOrder, IDefinedFunction<TValue>> func)
			=> throw new ServerSideOnlyException(nameof(PercentileDisc));

		#endregion

		#region Hypothetical-set (Rank/DenseRank/PercentRank/CumeDist WITHIN GROUP)

#pragma warning disable RS0030

		/// <summary>
		/// Generates the SQL hypothetical-set <c>RANK()</c> aggregate — the rank the given value would have (with gaps after ties) if inserted into the ordered group.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>source.Rank(value, (e, f) =&gt; f.OrderBy(e.Column)[.Filter(...)])</c></para>
		/// <para>Native on Oracle and PostgreSQL; throws a descriptive exception at query-translation time on other providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     group t by t.Dept into g
		///     select new
		///     {
		///         g.Key,
		///         Rank = g.Rank(1000, (e, f) =&gt; f.OrderBy(e.Salary)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT t.Dept, RANK(1000) WITHIN GROUP (ORDER BY t.Salary)
		/// FROM Table t
		/// GROUP BY t.Dept
		/// </code>
		/// </remarks>
		public static long Rank<TElement, TValue>(this IEnumerable<TElement> source, object? value, Func<TElement, IOnlyOrderByPart, IDefinedFunction<TValue>> func)
			=> throw new ServerSideOnlyException(nameof(Rank));

		/// <summary>
		/// Generates the SQL hypothetical-set <c>RANK()</c> aggregate over two ordering keys — the rank the given values would have (with gaps after ties) in the doubly-ordered group.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>source.Rank(value1, value2, (e, f) =&gt; f.OrderBy(e.Key1).ThenBy(e.Key2)[.Filter(...)])</c></para>
		/// <para>Native on Oracle and PostgreSQL; throws a descriptive exception at query-translation time on other providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     group t by t.Dept into g
		///     select new
		///     {
		///         g.Key,
		///         Rank = g.Rank(1000, 2000, (e, f) =&gt; f.OrderBy(e.Salary).ThenBy(e.Bonus)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT t.Dept, RANK(1000, 2000) WITHIN GROUP (ORDER BY t.Salary, t.Bonus)
		/// FROM Table t
		/// GROUP BY t.Dept
		/// </code>
		/// </remarks>
		public static long Rank<TElement, TValue>(this IEnumerable<TElement> source, object? value1, object? value2, Func<TElement, IMultipleOrderByPart, IDefinedFunction<TValue>> func)
			=> throw new ServerSideOnlyException(nameof(Rank));

		/// <summary>
		/// Generates the SQL hypothetical-set <c>DENSE_RANK()</c> aggregate — the rank the given value would have (no gaps after ties) if inserted into the ordered group.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>source.DenseRank(value, (e, f) =&gt; f.OrderBy(e.Column)[.Filter(...)])</c></para>
		/// <para>Native on Oracle and PostgreSQL; throws a descriptive exception at query-translation time on other providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     group t by t.Dept into g
		///     select new
		///     {
		///         g.Key,
		///         DenseRank = g.DenseRank(1000, (e, f) =&gt; f.OrderBy(e.Salary)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT t.Dept, DENSE_RANK(1000) WITHIN GROUP (ORDER BY t.Salary)
		/// FROM Table t
		/// GROUP BY t.Dept
		/// </code>
		/// </remarks>
		public static long DenseRank<TElement, TValue>(this IEnumerable<TElement> source, object? value, Func<TElement, IOnlyOrderByPart, IDefinedFunction<TValue>> func)
			=> throw new ServerSideOnlyException(nameof(DenseRank));

		/// <summary>
		/// Generates the SQL hypothetical-set <c>DENSE_RANK()</c> aggregate over two ordering keys — the rank the given values would have (no gaps after ties) in the doubly-ordered group.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>source.DenseRank(value1, value2, (e, f) =&gt; f.OrderBy(e.Key1).ThenBy(e.Key2)[.Filter(...)])</c></para>
		/// <para>Native on Oracle and PostgreSQL; throws a descriptive exception at query-translation time on other providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     group t by t.Dept into g
		///     select new
		///     {
		///         g.Key,
		///         DenseRank = g.DenseRank(1000, 2000, (e, f) =&gt; f.OrderBy(e.Salary).ThenBy(e.Bonus)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT t.Dept, DENSE_RANK(1000, 2000) WITHIN GROUP (ORDER BY t.Salary, t.Bonus)
		/// FROM Table t
		/// GROUP BY t.Dept
		/// </code>
		/// </remarks>
		public static long DenseRank<TElement, TValue>(this IEnumerable<TElement> source, object? value1, object? value2, Func<TElement, IMultipleOrderByPart, IDefinedFunction<TValue>> func)
			=> throw new ServerSideOnlyException(nameof(DenseRank));

		/// <summary>
		/// Generates the SQL hypothetical-set <c>PERCENT_RANK()</c> aggregate — the relative rank, a value in <c>[0, 1]</c> computed as <c>(rank - 1) / (rowCount - 1)</c>, the given value would have in the ordered group.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>source.PercentRank(value, (e, f) =&gt; f.OrderBy(e.Column)[.Filter(...)])</c></para>
		/// <para>Native on Oracle and PostgreSQL; throws a descriptive exception at query-translation time on other providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     group t by t.Dept into g
		///     select new
		///     {
		///         g.Key,
		///         PercentRank = g.PercentRank(1000, (e, f) =&gt; f.OrderBy(e.Salary)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT t.Dept, PERCENT_RANK(1000) WITHIN GROUP (ORDER BY t.Salary)
		/// FROM Table t
		/// GROUP BY t.Dept
		/// </code>
		/// </remarks>
		public static double PercentRank<TElement, TValue>(this IEnumerable<TElement> source, object? value, Func<TElement, IOnlyOrderByPart, IDefinedFunction<TValue>> func)
			=> throw new ServerSideOnlyException(nameof(PercentRank));

		/// <summary>
		/// Generates the SQL hypothetical-set <c>PERCENT_RANK()</c> aggregate over two ordering keys — the relative rank (a value in <c>[0, 1]</c>) the given values would have in the doubly-ordered group.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>source.PercentRank(value1, value2, (e, f) =&gt; f.OrderBy(e.Key1).ThenBy(e.Key2)[.Filter(...)])</c></para>
		/// <para>Native on Oracle and PostgreSQL; throws a descriptive exception at query-translation time on other providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     group t by t.Dept into g
		///     select new
		///     {
		///         g.Key,
		///         PercentRank = g.PercentRank(1000, 2000, (e, f) =&gt; f.OrderBy(e.Salary).ThenBy(e.Bonus)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT t.Dept, PERCENT_RANK(1000, 2000) WITHIN GROUP (ORDER BY t.Salary, t.Bonus)
		/// FROM Table t
		/// GROUP BY t.Dept
		/// </code>
		/// </remarks>
		public static double PercentRank<TElement, TValue>(this IEnumerable<TElement> source, object? value1, object? value2, Func<TElement, IMultipleOrderByPart, IDefinedFunction<TValue>> func)
			=> throw new ServerSideOnlyException(nameof(PercentRank));

		/// <summary>
		/// Generates the SQL hypothetical-set <c>CUME_DIST()</c> aggregate — the cumulative distribution, a value in <c>(0, 1]</c> giving the fraction of rows ordering at or before the given value, it would have in the ordered group.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>source.CumeDist(value, (e, f) =&gt; f.OrderBy(e.Column)[.Filter(...)])</c></para>
		/// <para>Native on Oracle and PostgreSQL; throws a descriptive exception at query-translation time on other providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     group t by t.Dept into g
		///     select new
		///     {
		///         g.Key,
		///         CumeDist = g.CumeDist(1000, (e, f) =&gt; f.OrderBy(e.Salary)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT t.Dept, CUME_DIST(1000) WITHIN GROUP (ORDER BY t.Salary)
		/// FROM Table t
		/// GROUP BY t.Dept
		/// </code>
		/// </remarks>
		public static double CumeDist<TElement, TValue>(this IEnumerable<TElement> source, object? value, Func<TElement, IOnlyOrderByPart, IDefinedFunction<TValue>> func)
			=> throw new ServerSideOnlyException(nameof(CumeDist));

		/// <summary>
		/// Generates the SQL hypothetical-set <c>CUME_DIST()</c> aggregate over two ordering keys — the cumulative distribution (a value in <c>(0, 1]</c>) the given values would have in the doubly-ordered group.
		/// </summary>
		/// <remarks>
		/// <para><b>Syntax:</b> <c>source.CumeDist(value1, value2, (e, f) =&gt; f.OrderBy(e.Key1).ThenBy(e.Key2)[.Filter(...)])</c></para>
		/// <para>Native on Oracle and PostgreSQL; throws a descriptive exception at query-translation time on other providers.</para>
		/// <para><b>C# usage:</b></para>
		/// <code>
		/// var query =
		///     from t in db.Table
		///     group t by t.Dept into g
		///     select new
		///     {
		///         g.Key,
		///         CumeDist = g.CumeDist(1000, 2000, (e, f) =&gt; f.OrderBy(e.Salary).ThenBy(e.Bonus)),
		///     };
		/// </code>
		/// <para><b>Generated SQL:</b></para>
		/// <code>
		/// SELECT t.Dept, CUME_DIST(1000, 2000) WITHIN GROUP (ORDER BY t.Salary, t.Bonus)
		/// FROM Table t
		/// GROUP BY t.Dept
		/// </code>
		/// </remarks>
		public static double CumeDist<TElement, TValue>(this IEnumerable<TElement> source, object? value1, object? value2, Func<TElement, IMultipleOrderByPart, IDefinedFunction<TValue>> func)
			=> throw new ServerSideOnlyException(nameof(CumeDist));

#pragma warning restore RS0030

		#endregion

	}
}
