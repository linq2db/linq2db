using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

namespace LinqToDB
{
	public static partial class LinqExtensions
	{
		/// <summary>
		/// Projects <paramref name="staticSelector"/> and, in addition, one column per entry of
		/// <paramref name="keys"/> into <typeparamref name="TResult"/>'s dynamic column store. The column set is therefore decided when the query
		/// is built rather than at compile time, while every expression stays statically typed:
		/// <paramref name="valueTemplate"/> is instantiated once per key, with its key parameter replaced by that
		/// key's value.
		/// </summary>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TKey">Type of the runtime keys driving the column set.</typeparam>
		/// <typeparam name="TCell">Type of a generated column's value.</typeparam>
		/// <typeparam name="TResult">
		/// Result record type; must have a dynamic column setter: a <see cref="DynamicColumnsStoreAttribute"/> member, or a setter
		/// configured through <see cref="DynamicColumnAccessorAttribute"/> or the fluent <c>DynamicPropertyAccessors</c> mapping.
		/// </typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="staticSelector">Projection for the statically known members of <typeparamref name="TResult"/>.</param>
		/// <param name="keys">
		/// The runtime key set. One output column is generated per key.
		/// In a compiled query, pass the key collection itself as a parameter: keys read through a member of a parameter
		/// (<c>s.Keys</c>) are matched on that parameter's instance, so a member reassigned on the same instance reuses the
		/// first call's column set.
		/// </param>
		/// <param name="valueTemplate">Per-column value expression, parameterized by the key.</param>
		/// <param name="nameSelector">
		/// Maps a key to the store key of the generated column. Defaults to the invariant string form of the key.
		/// Evaluated once per key each time the call runs; the resulting names take part in the query cache key.
		/// Written in a sub-query, the selector's own state is compared instead: every value it reads is evaluated when
		/// the query is built and again at each cache lookup, whether or not the selector reaches it, and a captured
		/// delegate or object counts as unchanged while it is the same instance, so replace it rather than mutating it.
		/// In a compiled query the names are computed when the query is first built for a key set, and state the
		/// selector captures is not re-read afterwards; a compiled-query parameter it reads is matched like <paramref name="keys"/>.
		/// </param>
		/// <returns>Query producing <typeparamref name="TResult"/> with one dynamic column per key.</returns>
		/// <remarks>
		/// When the branches of a set operation (<c>Concat</c>, <c>Union</c>, ...) select different key sets, every row
		/// carries the union of the sets: a cell its branch did not select is materialized as <c>default(TCell)</c>, not left
		/// out of the store. Use a nullable <typeparamref name="TCell"/> to tell such a cell (<see langword="null"/>) from a
		/// selected value.
		/// Branches that put cells of different types under one name are read a branch at a time by <c>Concat</c>, and
		/// refused by <c>Union</c>, <c>Except</c> and <c>Intersect</c>, which compare rows. A type and its nullable form
		/// are not different types here: the branches share one nullable column.
		/// </remarks>
		[Pure, LinqTunnel]
		public static IQueryable<TResult> SelectDynamic<TSource, TKey, TCell, TResult>(
			this            IQueryable<TSource>                    source,
			[InstantHandle] Expression<Func<TSource, TResult>>     staticSelector,
			[CompiledQueryDependent]
			[InstantHandle] IEnumerable<TKey>                      keys,
			[InstantHandle] Expression<Func<TSource, TKey, TCell>> valueTemplate,
			[CompiledQueryDependent]
			[InstantHandle] Func<TKey, string>?                    nameSelector = null)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(staticSelector);
			ArgumentNullException.ThrowIfNull(keys);
			ArgumentNullException.ThrowIfNull(valueTemplate);

			// The builder repeats this check for a call it reaches unevaluated; here a misdeclared result type fails at the call.
			if (Internals.GetDataContext(source) is { } dataContext && dataContext.MappingSchema.GetEntityDescriptor(typeof(TResult)).DynamicColumnSetter == null)
				throw new LinqToDBException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.SelectDynamic.Error_NoDynamicColumnSetter, typeof(TResult).Name));

			// Materialize once: the key set may be a lazy query, and it is read by index below.
			var keyArray = keys as TKey[] ?? keys.ToArray();

			var names    = new string[keyArray.Length];
			var cells    = new Expression[keyArray.Length];
			var seen     = new HashSet<string>(StringComparer.Ordinal);
			var rowParam = valueTemplate.Parameters[0];
			var keyParam = valueTemplate.Parameters[1];

			for (var i = 0; i < keyArray.Length; i++)
			{
				var key  = keyArray[i];
				var name = nameSelector != null
					? nameSelector(key)
					: Convert.ToString(key, CultureInfo.InvariantCulture);

				if (string.IsNullOrEmpty(name))
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.SelectDynamic.Error_DynamicColumnEmptyName, i), nameSelector != null ? nameof(nameSelector) : nameof(keys));

				if (!seen.Add(name!))
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.SelectDynamic.Error_DynamicColumnDuplicateName, name), nameSelector != null ? nameof(nameSelector) : nameof(keys));

				names[i] = name;
				cells[i] = Expression.Quote(
					Expression.Lambda<Func<TSource, TCell>>(
						valueTemplate.Body.Replace(keyParam, Expression.Constant(key, typeof(TKey))),
						rowParam));
			}

			var currentSource = source.ProcessIQueryable();

			return BuildSelectDynamic<TSource, TResult>(currentSource, staticSelector, names, cells);
		}

		// Emits the SelectDynamicCore marker. Cells are carried as LambdaExpression, and the builder reads each one's
		// type off its body.
		static IQueryable<TResult> BuildSelectDynamic<TSource, TResult>(
			IQueryable<TSource>                source,
			Expression<Func<TSource, TResult>> staticSelector,
			string[]                           names,
			Expression[]                       cells)
		{
			foreach (var name in names)
			{
				// A real member wins the lookup, so the generated column would be shadowed rather than reachable.
				// Methods and indexers do not take part in that lookup.
				if (typeof(TResult).GetField(name, BindingFlags.Public | BindingFlags.Instance) != null
					|| typeof(TResult).GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(p => string.Equals(p.Name, name, StringComparison.Ordinal) && p.GetIndexParameters().Length == 0))
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.SelectDynamic.Error_DynamicColumnShadowsMember, name, typeof(TResult).Name));
			}

			var expr = Expression.Call(
				null,
				_selectDynamicCoreMethodInfo.MakeGenericMethod(typeof(TSource), typeof(TResult)),
				source.Expression,
				Expression.Quote(staticSelector),
				Expression.Constant(names),
				Expression.NewArrayInit(typeof(LambdaExpression), cells));

			return source.Provider.CreateQuery<TResult>(expr);
		}

		// Lets a compiled query materialise keys or a name selector read from its arguments, which CompiledTable then keys
		// its cache on.
		// Elsewhere the argument is left as written and compared structurally: evaluating it would cache the sequence by
		// reference, and the builder already compares a snapshot of it by value.
		[AttributeUsage(AttributeTargets.Parameter)]
		sealed class CompiledQueryDependentAttribute : SqlQueryDependentAttribute
		{
			public override Expression PrepareForCache(Expression expression, IExpressionEvaluator evaluator) => expression;

			public override bool ExpressionsEqual<TContext>(TContext context, Expression expr1, Expression expr2, Func<TContext, Expression, Expression, bool> comparer)
				=> comparer(context, expr1, expr2);
		}

		static readonly MethodInfo _selectDynamicCoreMethodInfo =
			typeof(LinqExtensions).GetMethod(nameof(SelectDynamicCore), BindingFlags.Static | BindingFlags.NonPublic)!;

		// Query marker for SelectDynamic - never executed; recognized by SelectDynamicBuilder.
		// The template is already instantiated per key by the time this call is built, so the tree carries only
		// ordinary single-parameter lambdas plus the name array.
		internal static IQueryable<TResult> SelectDynamicCore<TSource, TResult>(
			IQueryable<TSource>                source,
			Expression<Func<TSource, TResult>> staticSelector,
			[SqlQueryDependent] string[]       names,
			LambdaExpression[]                 cells)
			=> throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.SelectDynamic.Error_QueryMarkerInvoked, nameof(SelectDynamicCore)));
	}
}
