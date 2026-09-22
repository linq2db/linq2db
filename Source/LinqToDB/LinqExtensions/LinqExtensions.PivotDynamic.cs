using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Reflection;

namespace LinqToDB
{
	public static partial class LinqExtensions
	{
		/// <summary>
		/// Rotates a runtime set of values of the <paramref name="forColumn"/> column into columns, producing one
		/// generated column per (cell template, value) pair in the <see cref="PivotCells{TSource,TFor}"/> member
		/// the projection assigns. The pivoted values are decided when the query is built, while the key, the FOR
		/// column and every cell expression stay statically typed; <typeparamref name="TResult"/> itself needs no
		/// dynamic-columns store, so an anonymous type works.
		/// </summary>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TKey">Grouping key type.</typeparam>
		/// <typeparam name="TFor">Type of the pivoted column.</typeparam>
		/// <typeparam name="TResult">Result record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="keySelector">Grouping key.</param>
		/// <param name="forColumn">The column whose values become columns.</param>
		/// <param name="forValues">The runtime set of pivoted values.</param>
		/// <param name="selector">
		/// Projection over the grouping and the cell set. The cell set is built by chained
		/// <see cref="PivotCells{TSource,TFor}.Cell{TCell}"/> calls and must be assigned to exactly one member of
		/// the projected object; more than one cell requires each to supply a name factory.
		/// </param>
		/// <returns>Query with one row per key, its cells addressable by name.</returns>
		/// <remarks>
		/// The chain is read out of the expression tree, so the number of cell <i>templates</i> is fixed at compile
		/// time. Use <see cref="Pivot{TSource,TKey,TFor,TResult}(IQueryable{TSource},Expression{Func{TSource,TKey}},Expression{Func{TSource,TFor}},IEnumerable{TFor},Expression{Func{IGrouping{TKey,TSource},TResult}},Func{PivotCells{TSource,TFor},PivotCells{TSource,TFor}})"/>
		/// when the templates themselves are decided at run time.
		/// </remarks>
		[Pure, LinqTunnel]
		public static IQueryable<TResult> Pivot<TSource, TKey, TFor, TResult>(
			this            IQueryable<TSource>                                                           source,
			[InstantHandle] Expression<Func<TSource, TKey>>                                               keySelector,
			[InstantHandle] Expression<Func<TSource, TFor>>                                               forColumn,
			[InstantHandle] IEnumerable<TFor>                                                             forValues,
			[InstantHandle] Expression<Func<IGrouping<TKey, TSource>, PivotCells<TSource, TFor>, TResult>> selector)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(keySelector);
			ArgumentNullException.ThrowIfNull(forColumn);
			ArgumentNullException.ThrowIfNull(forValues);
			ArgumentNullException.ThrowIfNull(selector);

			var cellsParam = selector.Parameters[1];
			var chain      = selector.Body.Find(IsCellCall<TSource, TFor>);

			if (chain == null)
				throw new ArgumentException($"The projection builds no cell: call {nameof(PivotCells<,>.Cell)} on '{cellsParam.Name}' and assign the result to a member.", nameof(selector));

			var templates = ReadTemplates<TSource, TFor>((MethodCallExpression)chain, cellsParam, nameof(selector));

			// The builder recognizes the placeholder and replaces it with the cells it generates, so the cell set
			// lands inside the projected object rather than in a store the result type has to declare itself.
			var body = selector.Body.Replace(chain, Expression.Constant(null, typeof(PivotCells<TSource, TFor>)));

			if (body.Find(cellsParam) != null)
				throw new ArgumentException($"'{cellsParam.Name}' may only be used to build one cell set, assigned to a single member of the projection.", nameof(selector));

			var staticSelector = Expression.Lambda<Func<IGrouping<TKey, TSource>, TResult>>(body, selector.Parameters[0]);

			return BuildPivot(source, keySelector, forColumn, forValues, staticSelector, templates, typeof(PivotCells<TSource, TFor>), nameof(selector));
		}

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
		/// <param name="cells">Builds the cell set. More than one cell requires each to supply a name factory.</param>
		/// <returns>Query with one row per key and one generated column per (cell, value) pair.</returns>
		/// <remarks>
		/// For a value set known at compile time, an ordinary
		/// <c>GroupBy(...).Select(g =&gt; new { ..., V2000 = g.Where(x =&gt; x.Year == 2000).Sum(x =&gt; x.Amount) })</c>
		/// expresses the same query with statically typed members and needs none of this.
		/// </remarks>
		[Pure, LinqTunnel]
		public static IQueryable<TResult> Pivot<TSource, TKey, TFor, TResult>(
			this            IQueryable<TSource>                                        source,
			[InstantHandle] Expression<Func<TSource, TKey>>                            keySelector,
			[InstantHandle] Expression<Func<TSource, TFor>>                            forColumn,
			[InstantHandle] IEnumerable<TFor>                                          forValues,
			[InstantHandle] Expression<Func<IGrouping<TKey, TSource>, TResult>>        staticSelector,
			[InstantHandle] Func<PivotCells<TSource, TFor>, PivotCells<TSource, TFor>> cells)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(keySelector);
			ArgumentNullException.ThrowIfNull(forColumn);
			ArgumentNullException.ThrowIfNull(forValues);
			ArgumentNullException.ThrowIfNull(staticSelector);
			ArgumentNullException.ThrowIfNull(cells);

			var built = cells(PivotCells<TSource, TFor>.Empty)
				?? throw new ArgumentException("The cell set was not built.", nameof(cells));

			return BuildPivot(source, keySelector, forColumn, forValues, staticSelector, built.Templates, null, nameof(cells));
		}

		static bool IsCellCall<TSource, TFor>(Expression expression)
			=> expression is MethodCallExpression call
				&& call.Method.DeclaringType == typeof(PivotCells<TSource, TFor>)
				&& string.Equals(call.Method.Name, nameof(PivotCells<,>.Cell), StringComparison.Ordinal);

		// Reads the templates out of a `p.Cell(...).Cell(...)` chain, outermost call first, so the chain has to be
		// walked down to the cell-set parameter and reversed.
		static PivotCells<TSource, TFor>.Template[] ReadTemplates<TSource, TFor>(MethodCallExpression chain, ParameterExpression cellsParam, string paramName)
		{
			var calls = new List<MethodCallExpression>();
			var node  = (Expression?)chain;

			while (IsCellCall<TSource, TFor>(node!))
			{
				var call = (MethodCallExpression)node!;

				calls.Add(call);
				node = call.Object;
			}

			if (node != cellsParam)
				throw new ArgumentException($"A pivot cell set must be built from '{cellsParam.Name}'.", paramName);

			var templates = new PivotCells<TSource, TFor>.Template[calls.Count];

			for (var i = 0; i < calls.Count; i++)
			{
				var call = calls[calls.Count - 1 - i];

				templates[i] = new PivotCells<TSource, TFor>.Template(
					call.Arguments[0].UnwrapLambda(),
					// A cell no row matches must read null, not default(TCell).
					!call.Method.GetGenericArguments()[0].IsNullableOrReferenceType,
					call.Arguments[1].EvaluateExpression<Func<TFor, string>>());
			}

			return templates;
		}

		static IQueryable<TResult> BuildPivot<TSource, TKey, TFor, TResult>(
			IQueryable<TSource>                                 source,
			Expression<Func<TSource, TKey>>                     keySelector,
			Expression<Func<TSource, TFor>>                     forColumn,
			IEnumerable<TFor>                                   forValues,
			Expression<Func<IGrouping<TKey, TSource>, TResult>> staticSelector,
			PivotCells<TSource, TFor>.Template[]                templates,
			Type?                                               cellsType,
			string                                              paramName)
		{
			if (templates.Length == 0)
				throw new ArgumentException("A pivot needs at least one cell template.", paramName);

			var values = forValues as TFor[] ?? forValues.ToArray();

			var names     = new string[values.Length * templates.Length];
			var cellExprs = new Expression[names.Length];
			var seen      = new HashSet<string>(StringComparer.Ordinal);
			var grouped   = source.GroupBy(keySelector);
			var groupType = typeof(IGrouping<,>).MakeGenericType(typeof(TKey), typeof(TSource));

			var k = 0;

			foreach (var cell in templates)
			{
				if (cell.Name == null && templates.Length > 1)
					throw new ArgumentException("A pivot with more than one cell template needs a name factory on each, so the generated columns can be told apart.", paramName);

				foreach (var value in values)
				{
					var name = cell.Name != null
						? cell.Name(value)
						: Convert.ToString(value, CultureInfo.InvariantCulture);

					if (string.IsNullOrEmpty(name))
						throw new ArgumentException($"Pivot value '{value}' produced an empty column name.", nameof(forValues));

					if (!seen.Add(name!))
						throw new ArgumentException($"Duplicate pivot column name '{name}'.", paramName);

					names[k]     = name!;
					cellExprs[k] = Expression.Quote(BuildCell(cell, forColumn, value, groupType));

					k++;
				}
			}

			return BuildSelectDynamic(grouped, staticSelector, names, cellExprs, cellsType);
		}

		// g => aggregate(g.Where(row => forColumn(row) == value))
		// One filtered aggregate per (cell, value) pair - the shape the engine already models as a grouped
		// aggregate with a filter, so a cell can carry any aggregate the provider can translate.
		static LambdaExpression BuildCell<TSource, TFor>(PivotCells<TSource, TFor>.Template cell, Expression<Func<TSource, TFor>> forColumn, TFor value, Type groupType)
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
