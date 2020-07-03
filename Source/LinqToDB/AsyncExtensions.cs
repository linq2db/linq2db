using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Linq;

	/// <summary>
	/// Provides helper methods for asynchronous operations.
	/// </summary>
	[PublicAPI]
	public static partial class AsyncExtensions
	{
		#region Helpers
		/// <summary>
		/// Executes provided action using task scheduler.
		/// </summary>
		/// <param name="action">Action to execute.</param>
		/// <param name="token">Asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		internal static Task GetActionTask(Action action, CancellationToken token)
		{
			var task = new Task(action, token);

			task.Start();

			return task;
		}

		/// <summary>
		/// Executes provided function using task scheduler.
		/// </summary>
		/// <typeparam name="T">Function result type.</typeparam>
		/// <param name="func">Function to execute.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		internal static Task<T> GetTask<T>(Func<T> func)
		{
			var task = new Task<T>(func);

			task.Start();

			return task;
		}

		/// <summary>
		/// Executes provided function using task scheduler.
		/// </summary>
		/// <typeparam name="T">Function result type.</typeparam>
		/// <param name="func">Function to execute.</param>
		/// <param name="token">Asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		static Task<T> GetTask<T>(Func<T> func, CancellationToken token)
		{
			var task = new Task<T>(func, token);

			task.Start();

			return task;
		}

		#endregion

		internal class ElementAsyncAttribute : Attribute
		{
		}

		#region AsAsyncEnumerable
#if !NET45 && !NET46
		/// <summary>
		/// Returns an <see cref="IAsyncEnumerable{T}"/> that can be enumerated asynchronously.
		/// </summary>
		/// <typeparam name="TSource">Source sequence element type.</typeparam>
		/// <param name="source">Source sequence.</param>
		/// <returns>A query that can be enumerated asynchronously.</returns>
		public static IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(
			this IQueryable<TSource> source)
		{
			if (source is IAsyncEnumerable<TSource> asyncQuery)
				return asyncQuery;

			if (LinqExtensions.ExtensionsAdapter != null)
				return LinqExtensions.ExtensionsAdapter.AsAsyncEnumerable(source);

			// return an enumerator that will synchronously enumerate the source elements
			return new AsyncEnumerableAdapter<TSource>(source);
		}

		private class AsyncEnumerableAdapter<T> : IAsyncEnumerable<T>
		{
			private readonly IQueryable<T> _query;
			public AsyncEnumerableAdapter(IQueryable<T> query)
			{
				_query = query ?? throw new ArgumentNullException(nameof(query));
			}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
			public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
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
#endif
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

			return GetActionTask(() =>
			{
				foreach (var item in source)
				{
					if (token.IsCancellationRequested)
						break;
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

			return GetActionTask(() =>
			{
				foreach (var item in source)
					if (token.IsCancellationRequested || !func(item))
						break;
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
				await query.GetForEachAsync(list.Add, token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				return list;
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return await LinqExtensions.ExtensionsAdapter.ToListAsync(source, token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return await GetTask(() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToList(), token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
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
				await query.GetForEachAsync(list.Add, token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				return list.ToArray();

				//				var list = new List<TSource>();
				//
				//				using (var enumerator = query.GetAsyncEnumerable().GetEnumerator())
				//					while (await enumerator.MoveNext(token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
				//						list.Add(enumerator.Current);
				//
				//				return list.ToArray();
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return await LinqExtensions.ExtensionsAdapter.ToArrayAsync(source, token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return await GetTask(() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToArray(), token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
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
				await query.GetForEachAsync(item => dic.Add(keySelector(item), item), token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				return dic;
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return await LinqExtensions.ExtensionsAdapter.ToDictionaryAsync(source, keySelector, token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return await GetTask(() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector), token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
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
				await query.GetForEachAsync(item => dic.Add(keySelector(item), item), token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				return dic;
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return await LinqExtensions.ExtensionsAdapter.ToDictionaryAsync(source, keySelector, comparer, token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return await GetTask(() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector, comparer), token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
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
				await query.GetForEachAsync(item => dic.Add(keySelector(item), elementSelector(item)), token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				return dic;
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return await LinqExtensions.ExtensionsAdapter.ToDictionaryAsync(source, keySelector, elementSelector, token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return await GetTask(() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector, elementSelector), token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
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
				var dic = new Dictionary<TKey,TElement>();
				await query.GetForEachAsync(item => dic.Add(keySelector(item), elementSelector(item)), token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				return dic;
			}

			if (LinqExtensions.ExtensionsAdapter != null)
				return await LinqExtensions.ExtensionsAdapter.ToDictionaryAsync(source, keySelector, elementSelector, comparer, token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return await GetTask(() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector, elementSelector, comparer), token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		#endregion

	}
}
