using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

using LinqToDB.Expressions;
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
		/// </param>
		/// <returns>Query producing <typeparamref name="TResult"/> with one dynamic column per key.</returns>
		[Pure, LinqTunnel]
		public static IQueryable<TResult> SelectDynamic<TSource, TKey, TCell, TResult>(
			this            IQueryable<TSource>                    source,
			[InstantHandle] Expression<Func<TSource, TResult>>     staticSelector,
			[InstantHandle] IEnumerable<TKey>                      keys,
			[InstantHandle] Expression<Func<TSource, TKey, TCell>> valueTemplate,
			                Func<TKey, string>?                    nameSelector = null)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(staticSelector);
			ArgumentNullException.ThrowIfNull(keys);
			ArgumentNullException.ThrowIfNull(valueTemplate);

			// Materialize to an array: a List<T> constant is dropped from the query cache key, which would let two
			// different column sets share one compiled query.
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
					throw new ArgumentException($"Key at index {i.ToString(CultureInfo.InvariantCulture)} produced an empty dynamic column name.", nameof(keys));

				// The name is emitted as a SQL identifier and identifier quoting is incomplete on some providers,
				// so reject anything that could terminate a quoted identifier.
				foreach (var ch in name!)
				{
					if (ch is '"' or '\'' or '`' or '[' or ']' or '\\' || char.IsControl(ch))
						throw new ArgumentException($"Dynamic column name '{name}' contains a character that cannot be used in a SQL identifier.", nameof(keys));
				}

				if (!seen.Add(name))
					throw new ArgumentException($"Duplicate dynamic column name '{name}'.", nameof(keys));

				names[i] = name;
				cells[i] = Expression.Quote(
					Expression.Lambda<Func<TSource, TCell>>(
						valueTemplate.Body.Replace(keyParam, Expression.Constant(key, typeof(TKey))),
						rowParam));
			}

			var currentSource = source.ProcessIQueryable();

			return BuildSelectDynamic<TSource, TResult>(currentSource, staticSelector, names, cells);
		}

		// Shared tail for every operator that projects a runtime column set: emits the SelectDynamicCore marker.
		// Cells are carried as LambdaExpression so each one keeps its own result type - a dynamic pivot mixes
		// them (Sum yields decimal?, Count yields int) and the builder reads the type off each lambda's body.
		internal static IQueryable<TResult> BuildSelectDynamic<TSource, TResult>(
			IQueryable<TSource>                source,
			Expression<Func<TSource, TResult>> staticSelector,
			string[]                           names,
			Expression[]                       cells)
		{
			foreach (var name in names)
			{
				// A real member wins the lookup, so the generated column would be shadowed rather than reachable.
				if (typeof(TResult).GetMember(name, BindingFlags.Public | BindingFlags.Instance).Length > 0)
					throw new ArgumentException($"Dynamic column name '{name}' collides with a member of '{typeof(TResult).Name}', which would be resolved instead of the generated column.");
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
			=> throw new InvalidOperationException("SelectDynamicCore is a query marker and must not be invoked directly.");
	}
}
