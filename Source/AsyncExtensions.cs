using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Linq;

	[PublicAPI]
	public static partial class AsyncExtensions
	{
		#region Helpers

		internal static Task GetActionTask(Action action, CancellationToken token)
		{
			var task = new Task(action, token);

			task.Start();

			return task;
		}

		static Task<T> GetTask<T>(Func<T> func)
		{
			var task = new Task<T>(func);

			task.Start();

			return task;
		}

		static Task<T> GetTask<T>(Func<T> func, CancellationToken token)
		{
			var task = new Task<T>(func, token);

			task.Start();

			return task;
		}

		#endregion

		#region ForEachAsync

		public static Task ForEachAsync<TSource>(
			this IQueryable<TSource> source, Action<TSource> action,
			CancellationToken token = default(CancellationToken))
		{
#if !NOASYNC

			var query = source as ExpressionQuery<TSource>;
			if (query != null)
				return query.GetForEachAsync(action, token);

#endif

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

		public static Task ForEachUntilAsync<TSource>(
			this IQueryable<TSource> source, Func<TSource,bool> func,
			CancellationToken token = default(CancellationToken))
		{
#if !NOASYNC

			var query = source as ExpressionQuery<TSource>;
			if (query != null)
				return query.GetForEachUntilAsync(func, token);

#endif

			return GetActionTask(() =>
			{
				foreach (var item in source)
					if (token.IsCancellationRequested || !func(item))
						break;
			},
			token);
		}

		#endregion

#if !NOASYNC

		#region ToListAsync

		public static async Task<List<TSource>> ToListAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default(CancellationToken))
		{
			var query = source as ExpressionQuery<TSource>;

			if (query != null)
			{
				var list = new List<TSource>();
				await query.GetForEachAsync(list.Add, token);
				return list;
			}

			return await GetTask(() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToList(), token);
		}

		#endregion

		#region ToArrayAsync

		public static async Task<TSource[]> ToArrayAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken token = default(CancellationToken))
		{
			var query = source as ExpressionQuery<TSource>;

			if (query != null)
			{
				var list = new List<TSource>();
				await query.GetForEachAsync(list.Add, token);
				return list.ToArray();
			}

			return await GetTask(() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToArray(), token);
		}

		#endregion

		#region ToDictionaryAsync

		public static async Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			CancellationToken        token   = default(CancellationToken))
		{
			var query = source as ExpressionQuery<TSource>;

			if (query != null)
			{
				var dic = new Dictionary<TKey,TSource>();
				await query.GetForEachAsync(item => dic.Add(keySelector(item), item), token);
				return dic;
			}

			return await GetTask(() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector), token);
		}

		public static async Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			IEqualityComparer<TKey>  comparer,
			CancellationToken        token = default(CancellationToken))
		{
			var query = source as ExpressionQuery<TSource>;
			if (query != null)
			{
				var dic = new Dictionary<TKey,TSource>(comparer);
				await query.GetForEachAsync(item => dic.Add(keySelector(item), item), token);
				return dic;
			}

			return await GetTask(() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector, comparer), token);
		}

		public static async Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			CancellationToken        token = default(CancellationToken))
		{
			var query = source as ExpressionQuery<TSource>;
			if (query != null)
			{
				var dic = new Dictionary<TKey,TElement>();
				await query.GetForEachAsync(item => dic.Add(keySelector(item), elementSelector(item)), token);
				return dic;
			}

			return await GetTask(() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector, elementSelector), token);
		}

		public static async Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			IEqualityComparer<TKey>  comparer,
			CancellationToken        token = default(CancellationToken))
		{
			var query = source as ExpressionQuery<TSource>;
			if (query != null)
			{
				var dic = new Dictionary<TKey,TElement>();
				await query.GetForEachAsync(item => dic.Add(keySelector(item), elementSelector(item)), token);
				return dic;
			}

			return await GetTask(() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector, elementSelector, comparer), token);
		}

		#endregion

#endif
	}
}
