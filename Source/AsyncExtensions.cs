using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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

		internal static Task GetActionTask(Action action, CancellationToken token, TaskCreationOptions options)
		{
			var task = new Task(action, token, options);

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

		static Task<T> GetTask<T>(Func<T> func, TaskCreationOptions options)
		{
			var task = new Task<T>(func, options);

			task.Start();

			return task;
		}

		internal static Task<T> GetTask<T>(Func<T> func, CancellationToken token, TaskCreationOptions options)
		{
			var task = new Task<T>(func, token, options);

			task.Start();

			return task;
		}

		#endregion

		#region ForEachAsync

		public static Task ForEachAsync<TSource>(this IQueryable<TSource> source, Action<TSource> action)
		{
			return ForEachAsync(source, action, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task ForEachAsync<TSource>(
			this IQueryable<TSource> source, Action<TSource> action, CancellationToken token)
		{
			return ForEachAsync(source, action, token, TaskCreationOptions.None);
		}

		public static Task ForEachAsync<TSource>(
			this IQueryable<TSource> source, Action<TSource> action, TaskCreationOptions options)
		{
			return ForEachAsync(source, action, CancellationToken.None, options);
		}

		public static Task ForEachAsync<TSource>(
			this IQueryable<TSource> source, Action<TSource> action, CancellationToken token, TaskCreationOptions options)
		{
			var query = source as ExpressionQuery<TSource>;
			if (query != null)
				return query.GetForEachAsync(action, token, options);

			return GetActionTask(() =>
			{
				foreach (var item in source)
				{
					if (token.IsCancellationRequested)
						break;
					action(item);
				}
			},
			token,
			options);
		}

		#endregion

#if !NOASYNC

		#region ToListAsync

		public static Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source)
		{
			return ToListAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken token)
		{
			return ToListAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, TaskCreationOptions options)
		{
			return ToListAsync(source, CancellationToken.None, options);
		}

		public static async Task<List<TSource>> ToListAsync<TSource>(
			this IQueryable<TSource> source, CancellationToken token, TaskCreationOptions options)
		{
			var query = source as ExpressionQuery<TSource>;

			if (query != null)
			{
				var list = new List<TSource>();
				await query.GetForEachAsync(list.Add, token, options);
				return list;
			}

			return await GetTask(
				() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToList(),
				token,
				options);
		}

		#endregion

		#region ToArrayAsync

		public static Task<TSource[]> ToArrayAsync<TSource>(this IQueryable<TSource> source)
		{
			return ToArrayAsync(source, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<TSource[]> ToArrayAsync<TSource>(this IQueryable<TSource> source, CancellationToken token)
		{
			return ToArrayAsync(source, token, TaskCreationOptions.None);
		}

		public static Task<TSource[]> ToArrayAsync<TSource>(this IQueryable<TSource> source, TaskCreationOptions options)
		{
			return ToArrayAsync(source, CancellationToken.None, options);
		}

		public static async Task<TSource[]> ToArrayAsync<TSource>(
			this IQueryable<TSource> source, CancellationToken token, TaskCreationOptions options)
		{
			var query = source as ExpressionQuery<TSource>;
			if (query != null)
			{
				var list = new List<TSource>();
				await query.GetForEachAsync(list.Add, token, options);
				return list.ToArray();
			}

			return await GetTask(
				() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToArray(),
				token,
				options);
		}

		#endregion

		#region ToDictionaryAsync

		public static Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector)
		{
			return ToDictionaryAsync(source, keySelector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			CancellationToken        token)
		{
			return ToDictionaryAsync(source, keySelector, token, TaskCreationOptions.None);
		}

		public static Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			TaskCreationOptions      options)
		{
			return ToDictionaryAsync(source, keySelector, CancellationToken.None, options);
		}

		public static async Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			CancellationToken        token,
			TaskCreationOptions      options)
		{
			var query = source as ExpressionQuery<TSource>;

			if (query != null)
			{
				var dic = new Dictionary<TKey,TSource>();
				await query.GetForEachAsync(item => dic.Add(keySelector(item), item), token, options);
				return dic;
			}

			return await GetTask(
				() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector),
				token,
				options);
		}

		public static Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			IEqualityComparer<TKey>  comparer)
		{
			return ToDictionaryAsync(source, keySelector, comparer, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			IEqualityComparer<TKey>  comparer,
			CancellationToken        token)
		{
			return ToDictionaryAsync(source, keySelector, comparer, token, TaskCreationOptions.None);
		}

		public static Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			IEqualityComparer<TKey>  comparer,
			TaskCreationOptions      options)
		{
			return ToDictionaryAsync(source, keySelector, comparer, CancellationToken.None, options);
		}

		public static async Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			IEqualityComparer<TKey>  comparer,
			CancellationToken        token,
			TaskCreationOptions      options)
		{
			var query = source as ExpressionQuery<TSource>;
			if (query != null)
			{
				var dic = new Dictionary<TKey,TSource>(comparer);
				await query.GetForEachAsync(item => dic.Add(keySelector(item), item), token, options);
				return dic;
			}

			return await GetTask(
				() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector, comparer),
				token,
				options);
		}

		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector)
		{
			return ToDictionaryAsync(source, keySelector, elementSelector, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			CancellationToken        token)
		{
			return ToDictionaryAsync(source, keySelector, elementSelector, token, TaskCreationOptions.None);
		}

		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			TaskCreationOptions      options)
		{
			return ToDictionaryAsync(source, keySelector, elementSelector, CancellationToken.None, options);
		}

		public static async Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			CancellationToken        token,
			TaskCreationOptions      options)
		{
			var query = source as ExpressionQuery<TSource>;
			if (query != null)
			{
				var dic = new Dictionary<TKey,TElement>();
				await query.GetForEachAsync(item => dic.Add(keySelector(item), elementSelector(item)), token, options);
				return dic;
			}

			return await GetTask(
				() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector, elementSelector),
				token,
				options);
		}

		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			IEqualityComparer<TKey>  comparer)
		{
			return ToDictionaryAsync(source, keySelector, elementSelector, comparer, CancellationToken.None, TaskCreationOptions.None);
		}

		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			IEqualityComparer<TKey>  comparer,
			CancellationToken        token)
		{
			return ToDictionaryAsync(source, keySelector, elementSelector, comparer, token, TaskCreationOptions.None);
		}

		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			IEqualityComparer<TKey>  comparer,
			TaskCreationOptions      options)
		{
			return ToDictionaryAsync(source, keySelector, elementSelector, comparer, CancellationToken.None, options);
		}

		public static async Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			IEqualityComparer<TKey>  comparer,
			CancellationToken        token,
			TaskCreationOptions      options)
		{
			var query = source as ExpressionQuery<TSource>;
			if (query != null)
			{
				var dic = new Dictionary<TKey,TElement>();
				await query.GetForEachAsync(item => dic.Add(keySelector(item), elementSelector(item)), token, options);
				return dic;
			}

			return await GetTask(
				() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector, elementSelector, comparer),
				token,
				options);
		}

		#endregion

#endif
	}
}
