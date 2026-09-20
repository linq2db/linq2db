using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Reflection;

namespace LinqToDB
{
	public static partial class LinqExtensions
	{
		/// <summary>
		/// Rotates a runtime set of values of the <paramref name="forColumn"/> column into columns, producing one
		/// generated column per (cell template, value) pair in <typeparamref name="TResult"/>'s
		/// <see cref="Mapping.DynamicColumnsStoreAttribute"/> member. The pivoted values are decided when the
		/// query is built, while the key, the FOR column and every cell expression stay statically typed.
		/// </summary>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TKey">Grouping key type.</typeparam>
		/// <typeparam name="TFor">Type of the pivoted column.</typeparam>
		/// <typeparam name="TResult">Result record type; must expose a dynamic-columns store.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="keySelector">Grouping key.</param>
		/// <param name="forColumn">The column whose values become columns.</param>
		/// <param name="forValues">The runtime set of pivoted values.</param>
		/// <param name="staticSelector">Projection for the statically known members, over the grouping.</param>
		/// <param name="cells">Cell templates. More than one requires each to supply a name factory.</param>
		/// <returns>Query with one row per key and one generated column per (cell, value) pair.</returns>
		/// <remarks>
		/// For a value set known at compile time, an ordinary
		/// <c>GroupBy(...).Select(g =&gt; new { ..., V2000 = g.Where(x =&gt; x.Year == 2000).Sum(x =&gt; x.Amount) })</c>
		/// expresses the same query with statically typed members and needs none of this.
		/// </remarks>
		[Pure, LinqTunnel]
		public static IQueryable<TResult> Pivot<TSource, TKey, TFor, TResult>(
			this            IQueryable<TSource>                                 source,
			[InstantHandle] Expression<Func<TSource, TKey>>                     keySelector,
			[InstantHandle] Expression<Func<TSource, TFor>>                     forColumn,
			[InstantHandle] IEnumerable<TFor>                                   forValues,
			[InstantHandle] Expression<Func<IGrouping<TKey, TSource>, TResult>> staticSelector,
			[InstantHandle] params PivotCell<TSource, TFor>[]                   cells)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(keySelector);
			ArgumentNullException.ThrowIfNull(forColumn);
			ArgumentNullException.ThrowIfNull(forValues);
			ArgumentNullException.ThrowIfNull(staticSelector);
			ArgumentNullException.ThrowIfNull(cells);

			if (cells.Length == 0)
				throw new ArgumentException("A pivot needs at least one cell template.", nameof(cells));

			var values = forValues as TFor[] ?? forValues.ToArray();

			var names     = new string[values.Length * cells.Length];
			var cellExprs = new Expression[names.Length];
			var seen      = new HashSet<string>(StringComparer.Ordinal);
			var grouped   = source.GroupBy(keySelector);
			var groupType = typeof(IGrouping<,>).MakeGenericType(typeof(TKey), typeof(TSource));

			var k = 0;

			foreach (var cell in cells)
			{
				if (cell.Name == null && cells.Length > 1)
					throw new ArgumentException("A pivot with more than one cell template needs a name factory on each, so the generated columns can be told apart.", nameof(cells));

				foreach (var value in values)
				{
					var name = cell.Name != null
						? cell.Name(value)
						: Convert.ToString(value, CultureInfo.InvariantCulture);

					if (string.IsNullOrEmpty(name))
						throw new ArgumentException($"Pivot value '{value}' produced an empty column name.", nameof(forValues));

					if (!seen.Add(name!))
						throw new ArgumentException($"Duplicate pivot column name '{name}'.", nameof(cells));

					names[k]     = name!;
					cellExprs[k] = Expression.Quote(BuildCell(cell, forColumn, value, groupType));

					k++;
				}
			}

			return BuildSelectDynamic(grouped, staticSelector, names, cellExprs);
		}

		/// <summary>
		/// Rotates a runtime set of values into the cells of a <see cref="PivotRow{TKey}"/>, so no result type
		/// has to be declared. See
		/// <see cref="Pivot{TSource,TKey,TFor,TResult}(IQueryable{TSource},Expression{Func{TSource,TKey}},Expression{Func{TSource,TFor}},IEnumerable{TFor},Expression{Func{IGrouping{TKey,TSource},TResult}},PivotCell{TSource,TFor}[])"/>
		/// to project static members alongside the cells.
		/// </summary>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TKey">Grouping key type.</typeparam>
		/// <typeparam name="TFor">Type of the pivoted column.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="keySelector">Grouping key.</param>
		/// <param name="forColumn">The column whose values become columns.</param>
		/// <param name="forValues">The runtime set of pivoted values.</param>
		/// <param name="cells">Cell templates. More than one requires each to supply a name factory.</param>
		/// <returns>Query with one <see cref="PivotRow{TKey}"/> per key.</returns>
		/// <remarks>
		/// For a value set known at compile time, an ordinary
		/// <c>GroupBy(...).Select(g =&gt; new { ..., V2000 = g.Where(x =&gt; x.Year == 2000).Sum(x =&gt; x.Amount) })</c>
		/// expresses the same query with statically typed members and needs none of this.
		/// </remarks>
		[Pure, LinqTunnel]
		public static IQueryable<PivotRow<TKey>> Pivot<TSource, TKey, TFor>(
			this            IQueryable<TSource>                source,
			[InstantHandle] Expression<Func<TSource, TKey>>    keySelector,
			[InstantHandle] Expression<Func<TSource, TFor>>    forColumn,
			[InstantHandle] IEnumerable<TFor>                  forValues,
			[InstantHandle] params PivotCell<TSource, TFor>[]  cells)
		{
			var gParam = Expression.Parameter(typeof(IGrouping<TKey, TSource>), "g");

			var selector = Expression.Lambda<Func<IGrouping<TKey, TSource>, PivotRow<TKey>>>(
				Expression.MemberInit(
					Expression.New(typeof(PivotRow<TKey>)),
					Expression.Bind(
						typeof(PivotRow<TKey>).GetProperty(nameof(PivotRow<>.Key))!,
						Expression.Property(gParam, nameof(IGrouping<,>.Key)))),
				gParam);

			return Pivot(source, keySelector, forColumn, forValues, selector, cells);
		}

		/// <summary>
		/// Rotates a runtime set of values into columns, with the cell templates supplied through a factory so
		/// that <typeparamref name="TSource"/> never has to be written down - the shape a pivot over a join
		/// projected into an anonymous type needs. Otherwise identical to
		/// <see cref="Pivot{TSource,TKey,TFor,TResult}(IQueryable{TSource},Expression{Func{TSource,TKey}},Expression{Func{TSource,TFor}},IEnumerable{TFor},Expression{Func{IGrouping{TKey,TSource},TResult}},PivotCell{TSource,TFor}[])"/>.
		/// </summary>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TKey">Grouping key type.</typeparam>
		/// <typeparam name="TFor">Type of the pivoted column.</typeparam>
		/// <typeparam name="TResult">Result record type; must expose a dynamic-columns store.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="keySelector">Grouping key.</param>
		/// <param name="forColumn">The column whose values become columns.</param>
		/// <param name="forValues">The runtime set of pivoted values.</param>
		/// <param name="staticSelector">Projection for the statically known members, over the grouping.</param>
		/// <param name="cells">Cell templates, each built from the supplied factory. More than one requires each to supply a name factory.</param>
		/// <returns>Query with one row per key and one generated column per (cell, value) pair.</returns>
		[Pure, LinqTunnel]
		public static IQueryable<TResult> Pivot<TSource, TKey, TFor, TResult>(
			this            IQueryable<TSource>                                                      source,
			[InstantHandle] Expression<Func<TSource, TKey>>                                          keySelector,
			[InstantHandle] Expression<Func<TSource, TFor>>                                          forColumn,
			[InstantHandle] IEnumerable<TFor>                                                        forValues,
			[InstantHandle] Expression<Func<IGrouping<TKey, TSource>, TResult>>                      staticSelector,
			[InstantHandle] params Func<PivotCellFactory<TSource, TFor>, PivotCell<TSource, TFor>>[] cells)
			=> Pivot(source, keySelector, forColumn, forValues, staticSelector, InvokeCellFactories(cells));

		/// <summary>
		/// Rotates a runtime set of values into the cells of a <see cref="PivotRow{TKey}"/>, with the cell
		/// templates supplied through a factory so that <typeparamref name="TSource"/> never has to be written
		/// down - the shape a pivot over a join projected into an anonymous type needs.
		/// </summary>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TKey">Grouping key type.</typeparam>
		/// <typeparam name="TFor">Type of the pivoted column.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="keySelector">Grouping key.</param>
		/// <param name="forColumn">The column whose values become columns.</param>
		/// <param name="forValues">The runtime set of pivoted values.</param>
		/// <param name="cells">Cell templates, each built from the supplied factory. More than one requires each to supply a name factory.</param>
		/// <returns>Query with one <see cref="PivotRow{TKey}"/> per key.</returns>
		[Pure, LinqTunnel]
		public static IQueryable<PivotRow<TKey>> Pivot<TSource, TKey, TFor>(
			this            IQueryable<TSource>                                                      source,
			[InstantHandle] Expression<Func<TSource, TKey>>                                          keySelector,
			[InstantHandle] Expression<Func<TSource, TFor>>                                          forColumn,
			[InstantHandle] IEnumerable<TFor>                                                        forValues,
			[InstantHandle] params Func<PivotCellFactory<TSource, TFor>, PivotCell<TSource, TFor>>[] cells)
			=> Pivot(source, keySelector, forColumn, forValues, InvokeCellFactories(cells));

		static PivotCell<TSource, TFor>[] InvokeCellFactories<TSource, TFor>(Func<PivotCellFactory<TSource, TFor>, PivotCell<TSource, TFor>>[] cells)
		{
			ArgumentNullException.ThrowIfNull(cells);

			var result = new PivotCell<TSource, TFor>[cells.Length];

			for (var i = 0; i < cells.Length; i++)
				result[i] = cells[i](PivotCellFactory<TSource, TFor>.Instance)
					?? throw new ArgumentException($"Cell template at index {i.ToString(CultureInfo.InvariantCulture)} was not built.", nameof(cells));

			return result;
		}

		// g => aggregate(g.Where(row => forColumn(row) == value))
		// One filtered aggregate per (cell, value) pair - the shape the engine already models as a grouped
		// aggregate with a filter, so a cell can carry any aggregate the provider can translate.
		static LambdaExpression BuildCell<TSource, TFor>(PivotCell<TSource, TFor> cell, Expression<Func<TSource, TFor>> forColumn, TFor value, Type groupType)
		{
			var gParam   = Expression.Parameter(groupType, "g");
			var rowParam = Expression.Parameter(typeof(TSource), "row");

			var predicate = Expression.Lambda(
				Expression.Equal(forColumn.GetBody(rowParam), Expression.Constant(value, typeof(TFor))),
				rowParam);

			var whereMethod = Methods.Enumerable.Where.MakeGenericMethod(typeof(TSource));
			var body        = cell.Aggregate.GetBody(Expression.Call(whereMethod, gParam, predicate));

			// A cell no row matches must read null, not default(TCell).
			if (cell.LiftResult)
				body = Expression.Convert(body, typeof(Nullable<>).MakeGenericType(body.Type));

			return Expression.Lambda(body, gParam);
		}
	}
}
