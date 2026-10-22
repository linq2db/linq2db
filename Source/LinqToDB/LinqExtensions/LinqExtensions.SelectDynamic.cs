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
		/// <paramref name="keys"/> into <typeparamref name="TResult"/>'s
		/// <see cref="DynamicColumnsStoreAttribute"/> member. The column set is therefore decided when the query
		/// is built rather than at compile time, while every expression stays statically typed:
		/// <paramref name="valueTemplate"/> is instantiated once per key, with its key parameter replaced by that
		/// key's value.
		/// </summary>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TKey">Type of the runtime keys driving the column set.</typeparam>
		/// <typeparam name="TCell">Type of a generated column's value.</typeparam>
		/// <typeparam name="TResult">Result record type; must expose a <see cref="DynamicColumnsStoreAttribute"/> member.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="staticSelector">Projection for the statically known members of <typeparamref name="TResult"/>.</param>
		/// <param name="keys">The runtime key set. One output column is generated per key.</param>
		/// <param name="valueTemplate">Per-column value expression, parameterized by the key.</param>
		/// <param name="nameSelector">
		/// Maps a key to the store key of the generated column. Defaults to the invariant string form of the key.
		/// Evaluated once per key while the query is built; the resulting names take part in the query cache key.
		/// Written in a sub-query, the selector's own state is compared instead: every value it reads is evaluated when
		/// the query is built and again at each cache lookup, whether or not the selector reaches it, and a captured
		/// delegate or object counts as unchanged while it is the same instance, so replace it rather than mutating it.
		/// </param>
		/// <returns>Query producing <typeparamref name="TResult"/> with one dynamic column per key.</returns>
		[Pure, LinqTunnel]
		public static IQueryable<TResult> SelectDynamic<TSource, TKey, TCell, TResult>(
			this            IQueryable<TSource>                    source,
			[InstantHandle] Expression<Func<TSource, TResult>>     staticSelector,
			[InstantHandle] IEnumerable<TKey>                      keys,
			[InstantHandle] Expression<Func<TSource, TKey, TCell>> valueTemplate,
			[InstantHandle] Func<TKey, string>?                    nameSelector = null)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(staticSelector);
			ArgumentNullException.ThrowIfNull(keys);
			ArgumentNullException.ThrowIfNull(valueTemplate);

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
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.SelectDynamic.Error_DynamicColumnEmptyName, i), nameof(keys));

				if (!seen.Add(name!))
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.SelectDynamic.Error_DynamicColumnDuplicateName, name), nameof(keys));

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
