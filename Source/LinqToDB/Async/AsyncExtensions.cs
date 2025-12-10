using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Internal.Linq;

namespace LinqToDB.Async
{
	/// <summary>
	/// Provides helper methods for asynchronous operations.
	/// </summary>
	[PublicAPI]
	public static partial class AsyncExtensions
	{
		#region Helpers

		private static List<T> ToListToken<T>(this IEnumerable<T> source, CancellationToken token)
		{
			var list = new List<T>();

#if SUPPORTS_ENSURE_CAPACITY
			if (source.TryGetNonEnumeratedCount(out var count))
				list.EnsureCapacity(count);
#endif

			foreach (var item in source)
			{
				token.ThrowIfCancellationRequested();
				list.Add(item);
			}

			return list;
		}

		private static T[] ToArrayToken<T>(this IEnumerable<T> source, CancellationToken token)
		{
			var list = new List<T>();

#if SUPPORTS_ENSURE_CAPACITY
			if (source.TryGetNonEnumeratedCount(out var count))
				list.EnsureCapacity(count);
#endif

			foreach (var item in source)
			{
				token.ThrowIfCancellationRequested();
				list.Add(item);
			}

			return list.ToArray();
		}

		private static Dictionary<TKey, TSource> ToDictionaryToken<TSource, TKey>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			IEqualityComparer<TKey>? comparer,
			CancellationToken token
		) where TKey : notnull
		{
			var dictionary = new Dictionary<TKey, TSource>(comparer);

#if SUPPORTS_ENSURE_CAPACITY
			if (source.TryGetNonEnumeratedCount(out var count))
				dictionary.EnsureCapacity(count);
#endif

			foreach (var item in source)
			{
				token.ThrowIfCancellationRequested();
				dictionary[keySelector(item)] = item;
			}

			return dictionary;
		}

		private static Dictionary<TKey, TElement> ToDictionaryToken<TSource, TKey, TElement>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			Func<TSource, TElement> elementSelector,
			IEqualityComparer<TKey>? comparer,
			CancellationToken token
		) where TKey : notnull
		{
			var dictionary = new Dictionary<TKey, TElement>(comparer);

#if SUPPORTS_ENSURE_CAPACITY
			if (source.TryGetNonEnumeratedCount(out var count))
				dictionary.EnsureCapacity(count);
#endif

			foreach (var item in source)
			{
				token.ThrowIfCancellationRequested();
				dictionary[keySelector(item)] = elementSelector(item);
			}

			return dictionary;
		}

		static Lookup<TKey,TSource> ToLookupWithToken<TKey,TSource>(
			this IEnumerable<TSource> source,
			Func<TSource,TKey>        keySelector,
			IEqualityComparer<TKey>?  comparer,
			CancellationToken         token)
			where TKey : notnull
		{
			var lookup = new Lookup<TKey,TSource>(comparer);

#if SUPPORTS_ENSURE_CAPACITY
			if (source.TryGetNonEnumeratedCount(out var count))
				lookup.EnsureCapacity(count);
#endif

			foreach (var item in source)
			{
				token.ThrowIfCancellationRequested();
				lookup.AddItem(keySelector(item), item);
			}

			return lookup;
		}

		static Lookup<TKey,TElement> ToLookupWithToken<TKey,TSource,TElement>(
			this IEnumerable<TSource> source,
			Func<TSource,TKey>        keySelector,
			Func<TSource,TElement>    elementSelector,
			IEqualityComparer<TKey>?  comparer,
			CancellationToken         token)
			where TKey : notnull
		{
			var lookup = new Lookup<TKey,TElement>(comparer);

#if SUPPORTS_ENSURE_CAPACITY
			if (source.TryGetNonEnumeratedCount(out var count))
				lookup.EnsureCapacity(count);
#endif

			foreach (var item in source)
			{
				token.ThrowIfCancellationRequested();
				lookup.AddItem(keySelector(item), elementSelector(item));
			}

			return lookup;
		}

		#endregion

		[AttributeUsage(AttributeTargets.Method)]
		internal sealed class ElementAsyncAttribute : Attribute;

		#region AsAsyncEnumerable
		/// <summary>
		/// Returns an <see cref="IAsyncEnumerable{T}"/> that can be enumerated asynchronously.
		/// </summary>
		/// <typeparam name="TSource">Source sequence element type.</typeparam>
		/// <param name="source">Source sequence.</param>
		/// <returns>A query that can be enumerated asynchronously.</returns>
		public static IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(this IQueryable<TSource> source)
		{
			ArgumentNullException.ThrowIfNull(source);

			if (source is IAsyncEnumerable<TSource> asyncQuery)
				return asyncQuery;

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AsAsyncEnumerable(source);

			// return an enumerator that will synchronously enumerate the source elements
			return new AsyncEnumerableAdapter<TSource>(source);
		}

		private sealed class AsyncEnumerableAdapter<T> : IAsyncEnumerable<T>
		{
			private readonly IQueryable<T> _query;
			public AsyncEnumerableAdapter(IQueryable<T> query)
			{
				_query = query ?? throw new ArgumentNullException(nameof(query));
			}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
			async IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
			{
				using var enumerator = _query.GetEnumerator();
				while (enumerator.MoveNext())
				{
					yield return enumerator.Current;
					cancellationToken.ThrowIfCancellationRequested();
				}
			}
		}
#endregion

		#region ForEachAsync

		/// <summary>
		/// Asynchronously apply provided action to each element in source sequence.
		/// Sequence elements processed sequentially.
		/// </summary>
		/// <typeparam name="TSource">Source sequence element type.</typeparam>
		/// <param name="source">Source sequence.</param>
		/// <param name="action">Action to apply to each sequence element.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		public static Task ForEachAsync<TSource>(
			this IQueryable<TSource> source, Action<TSource> action,
			CancellationToken token = default)
		{
			if (source is ExpressionQuery<TSource> query)
				return query.GetForEachAsync(action, token);

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.ForEachAsync(source, action, token);

			return Task.Run(
				() =>
			{
					token.ThrowIfCancellationRequested();
				foreach (var item in source)
				{
						token.ThrowIfCancellationRequested();
					action(item);
				}
			},
			token);
		}

		/// <summary>
		/// Asynchronously apply provided function to each element in source sequence sequentially.
		/// Sequence enumeration stops if function returns <c>false</c>.
		/// </summary>
		/// <typeparam name="TSource">Source sequence element type.</typeparam>
		/// <param name="source">Source sequence.</param>
		/// <param name="func">Function to apply to each sequence element. Returning <c>false</c> from function will stop numeration.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		public static Task ForEachUntilAsync<TSource>(
			this IQueryable<TSource> source, Func<TSource,bool> func,
			CancellationToken token = default)
		{
			if (source is ExpressionQuery<TSource> query)
				return query.GetForEachUntilAsync(func, token);

			return Task.Run(
				() =>
			{
					token.ThrowIfCancellationRequested();
				foreach (var item in source)
					{
						token.ThrowIfCancellationRequested();
						if (!func(item))
						break;
					}
			},
			token);
		}

		#endregion

		#region ToListAsync

		/// <summary>
		/// Asynchronously loads data from query to a list.
		/// </summary>
		/// <typeparam name="TSource">Query element type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>List with query results.</returns>
		public static async Task<List<TSource>> ToListAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default)
		{
			if (source is ExpressionQuery<TSource> query)
			{
				var list = new List<TSource>();
				await query.GetForEachAsync(list.Add, token).ConfigureAwait(false);
				return list;
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return await LinqExtensions.ExtensionsAdapter.ToListAsync(source, token).ConfigureAwait(false);

			return await Task.Run(() => source.AsEnumerable().ToListToken(token), token).ConfigureAwait(false);
		}

		#endregion

		#region ToArrayAsync

		/// <summary>
		/// Asynchronously loads data from query to an array.
		/// </summary>
		/// <typeparam name="TSource">Query element type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Array with query results.</returns>
		public static async Task<TSource[]> ToArrayAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default)
		{
			if (source is ExpressionQuery<TSource> query)
			{
				var list = new List<TSource>();
				await query.GetForEachAsync(list.Add, token).ConfigureAwait(false);
				return list.ToArray();
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return await LinqExtensions.ExtensionsAdapter.ToArrayAsync(source, token).ConfigureAwait(false);

			return await Task.Run(() => source.AsEnumerable().ToArrayToken(token), token).ConfigureAwait(false);
		}

		#endregion

		#region ToDictionaryAsync

		/// <summary>
		/// Asynchronously loads data from query to a dictionary.
		/// </summary>
		/// <typeparam name="TSource">Query element type.</typeparam>
		/// <typeparam name="TKey">Dictionary key type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="keySelector">Source element key selector.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Dictionary with query results.</returns>
		public static async Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			CancellationToken        token   = default)
			where TKey : notnull
		{
			if (source is ExpressionQuery<TSource> query)
			{
				var dic = new Dictionary<TKey,TSource>();
				await query.GetForEachAsync(item => dic.Add(keySelector(item), item), token).ConfigureAwait(false);
				return dic;
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return await LinqExtensions.ExtensionsAdapter.ToDictionaryAsync(source, keySelector, token).ConfigureAwait(false);

			return await Task.Run(() => source.AsEnumerable().ToDictionaryToken(keySelector, null, token), token).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously loads data from query to a dictionary.
		/// </summary>
		/// <typeparam name="TSource">Query element type.</typeparam>
		/// <typeparam name="TKey">Dictionary key type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="keySelector">Source element key selector.</param>
		/// <param name="comparer">Dictionary key comparer.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Dictionary with query results.</returns>
		public static async Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			IEqualityComparer<TKey>  comparer,
			CancellationToken        token = default)
			where TKey : notnull
		{
			if (source is ExpressionQuery<TSource> query)
			{
				var dic = new Dictionary<TKey,TSource>(comparer);
				await query.GetForEachAsync(item => dic.Add(keySelector(item), item), token).ConfigureAwait(false);
				return dic;
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return await LinqExtensions.ExtensionsAdapter.ToDictionaryAsync(source, keySelector, comparer, token).ConfigureAwait(false);

			return await Task.Run(() => source.AsEnumerable().ToDictionaryToken(keySelector, comparer, token), token).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously loads data from query to a dictionary.
		/// </summary>
		/// <typeparam name="TSource">Query element type.</typeparam>
		/// <typeparam name="TKey">Dictionary key type.</typeparam>
		/// <typeparam name="TElement">Dictionary element type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="keySelector">Source element key selector.</param>
		/// <param name="elementSelector">Dictionary element selector.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Dictionary with query results.</returns>
		public static async Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			CancellationToken        token = default)
			where TKey : notnull
		{
			if (source is ExpressionQuery<TSource> query)
			{
				var dic = new Dictionary<TKey,TElement>();
				await query.GetForEachAsync(item => dic.Add(keySelector(item), elementSelector(item)), token).ConfigureAwait(false);
				return dic;
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return await LinqExtensions.ExtensionsAdapter.ToDictionaryAsync(source, keySelector, elementSelector, token).ConfigureAwait(false);

			return await Task.Run(() => source.AsEnumerable().ToDictionaryToken(keySelector, elementSelector, null, token), token).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously loads data from query to a dictionary.
		/// </summary>
		/// <typeparam name="TSource">Query element type.</typeparam>
		/// <typeparam name="TKey">Dictionary key type.</typeparam>
		/// <typeparam name="TElement">Dictionary element type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="keySelector">Source element key selector.</param>
		/// <param name="elementSelector">Dictionary element selector.</param>
		/// <param name="comparer">Dictionary key comparer.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Dictionary with query results.</returns>
		public static async Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			IEqualityComparer<TKey>  comparer,
			CancellationToken        token = default)
			where TKey : notnull
		{
			if (source is ExpressionQuery<TSource> query)
			{
				var dic = new Dictionary<TKey,TElement>(comparer);
				await query.GetForEachAsync(item => dic.Add(keySelector(item), elementSelector(item)), token).ConfigureAwait(false);
				return dic;
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return await LinqExtensions.ExtensionsAdapter.ToDictionaryAsync(source, keySelector, elementSelector, comparer, token).ConfigureAwait(false);

			return await Task.Run(() => source.AsEnumerable().ToDictionaryToken(keySelector, elementSelector, comparer, token), token).ConfigureAwait(false);
		}

		#endregion

		#region ToLookupAsync

		/// <summary>
		/// Asynchronously creates a <see cref="ILookup{TKey,TElement}"/> from an <see cref="IEnumerable{T}"/>
		/// according to a specified key selector function.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of <c>source</c>.</typeparam>
		/// <typeparam name="TKey">The type of the key returned by <c>keySelector</c>.</typeparam>
		/// <param name="source">The <see cref="IQueryable{T}"/> to create a <see cref="ILookup{TKey,TElement}"/> from.</param>
		/// <param name="keySelector">A function to extract a key from each element.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>
		/// A <see cref="ILookup{TKey,TElement}"/> that contains keys and values.
		/// The values within each group are in the same order as in <c>source</c>.
		/// </returns>
		public static Task<ILookup<TKey,TSource>> ToLookupAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			CancellationToken        token = default)
			where TKey : notnull
		{
			return ToLookupAsync(source, keySelector, null, token);
		}

		/// <summary>
		/// Asynchronously creates a <see cref="ILookup{TKey,TElement}"/> from an <see cref="IEnumerable{T}"/>
		/// according to a specified key selector function.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of <c>source</c>.</typeparam>
		/// <typeparam name="TKey">The type of the key returned by <c>keySelector</c>.</typeparam>
		/// <param name="source">The <see cref="IQueryable{T}"/> to create a <see cref="ILookup{TKey,TElement}"/> from.</param>
		/// <param name="keySelector">A function to extract a key from each element.</param>
		/// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>
		/// A <see cref="ILookup{TKey,TElement}"/> that contains keys and values.
		/// The values within each group are in the same order as in <c>source</c>.
		/// </returns>
		public static async Task<ILookup<TKey,TSource>> ToLookupAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			IEqualityComparer<TKey>? comparer,
			CancellationToken        token = default)
			where TKey : notnull
		{
			if (source is ExpressionQuery<TSource> query)
			{
				var lookup = new Lookup<TKey,TSource>(comparer);
				await query.GetForEachAsync(item => lookup.AddItem(keySelector(item), item), token).ConfigureAwait(false);
				return lookup;
			}

			return await Task.Run(() => source.AsEnumerable().ToLookupWithToken(keySelector, comparer, token), token).ConfigureAwait(false);
		}

		/// <summary>
		/// Asynchronously creates a <see cref="ILookup{TKey,TElement}"/> from an <see cref="IEnumerable{T}"/>
		/// according to a specified key selector function.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of <c>source</c>.</typeparam>
		/// <typeparam name="TKey">The type of the key returned by <c>keySelector</c>.</typeparam>
		/// <typeparam name="TElement">The type of the value returned by <c>elementSelector</c>.</typeparam>
		/// <param name="source">The <see cref="IQueryable{T}"/> to create a <see cref="ILookup{TKey,TElement}"/> from.</param>
		/// <param name="keySelector">A function to extract a key from each element.</param>
		/// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>
		/// A <see cref="ILookup{TKey,TElement}"/> that contains keys and values.
		/// The values within each group are in the same order as in <c>source</c>.
		/// </returns>
		public static Task<ILookup<TKey,TElement>> ToLookupAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			CancellationToken        token = default)
			where TKey : notnull
		{
			return ToLookupAsync(source, keySelector, elementSelector, null, token);
		}

		/// <summary>
		/// Asynchronously creates a <see cref="ILookup{TKey,TElement}"/> from an <see cref="IEnumerable{T}"/>
		/// according to a specified key selector function.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of <c>source</c>.</typeparam>
		/// <typeparam name="TKey">The type of the key returned by <c>keySelector</c>.</typeparam>
		/// <typeparam name="TElement">The type of the value returned by <c>elementSelector</c>.</typeparam>
		/// <param name="source">The <see cref="IQueryable{T}"/> to create a <see cref="ILookup{TKey,TElement}"/> from.</param>
		/// <param name="keySelector">A function to extract a key from each element.</param>
		/// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
		/// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>
		/// A <see cref="ILookup{TKey,TElement}"/> that contains keys and values.
		/// The values within each group are in the same order as in <c>source</c>.
		/// </returns>
		public static async Task<ILookup<TKey,TElement>> ToLookupAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			IEqualityComparer<TKey>? comparer,
			CancellationToken        token = default)
			where TKey : notnull
		{
			if (source is ExpressionQuery<TSource> query)
			{
				var lookup = new Lookup<TKey,TElement>(comparer);
				await query.GetForEachAsync(item => lookup.AddItem(keySelector(item), elementSelector(item)), token).ConfigureAwait(false);
				return lookup;
			}

			return await Task.Run(() => source.AsEnumerable().ToLookupWithToken(keySelector, elementSelector, comparer, token), token).ConfigureAwait(false);
		}

		sealed class Grouping<TKey,TElement>(TKey key) : List<TElement>(1), IGrouping<TKey,TElement>
			where TKey : notnull
		{
			public TKey Key { get; } = key;
		}

		sealed class Lookup<TKey,TElement>(IEqualityComparer<TKey>? comparer)
			: Dictionary<TKey,Grouping<TKey,TElement>>(comparer), ILookup<TKey,TElement>
			where TKey : notnull
		{
			IEnumerator<IGrouping<TKey,TElement>> IEnumerable<IGrouping<TKey,TElement>>.GetEnumerator()
			{
				return Values.GetEnumerator();
			}

			bool ILookup<TKey,TElement>.Contains(TKey key)
			{
				return ContainsKey(key);
			}

			IEnumerable<TElement> ILookup<TKey,TElement>.this[TKey key] =>
				TryGetValue(key, out var grouping) ? grouping : Enumerable.Empty<TElement>();

			public void AddItem(TKey key, TElement element)
			{
				if (TryGetValue(key, out var grouping) == false)
					Add(key, grouping = new Grouping<TKey,TElement>(key));

				grouping.Add(element);
			}
		}

		#endregion
	}
}
