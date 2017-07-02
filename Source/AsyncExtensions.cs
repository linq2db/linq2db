using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Linq;

namespace LinqToDB
{
	using System.Linq.Expressions;

	public static class AsyncExtensions
	{
		#region Helpers

		static Task GetActionTask(Action action)
		{
			var task = new Task(action);

			task.Start();

			return task;
		}

		static Task GetActionTask(Action action, CancellationToken token)
		{
			var task = new Task(action, token);

			task.Start();

			return task;
		}

		static Task GetActionTask(Action action, TaskCreationOptions options)
		{
			var task = new Task(action, options);

			task.Start();

			return task;
		}

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

		static Task<T> GetTask<T>(Func<T> func, CancellationToken token, TaskCreationOptions options)
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
			if (source is ExpressionQuery<TSource>)
				return ((ExpressionQuery<TSource>)source).GetForEachAsync(action, token, options);

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
			if (source is ExpressionQuery<TSource>)
			{
				var list = new List<TSource>();
				await ((ExpressionQuery<TSource>)source).GetForEachAsync(list.Add, token, options);
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
			if (source is ExpressionQuery<TSource>)
			{
				var list = new List<TSource>();
				await ((ExpressionQuery<TSource>)source).GetForEachAsync(list.Add, token, options);
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
			if (source is ExpressionQuery<TSource>)
			{
				var dic = new Dictionary<TKey,TSource>();
				await ((ExpressionQuery<TSource>)source).GetForEachAsync(item => dic.Add(keySelector(item), item), token, options);
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
			if (source is ExpressionQuery<TSource>)
			{
				var dic = new Dictionary<TKey,TSource>(comparer);
				await ((ExpressionQuery<TSource>)source).GetForEachAsync(item => dic.Add(keySelector(item), item), token, options);
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
			if (source is ExpressionQuery<TSource>)
			{
				var dic = new Dictionary<TKey,TElement>();
				await ((ExpressionQuery<TSource>)source).GetForEachAsync(item => dic.Add(keySelector(item), elementSelector(item)), token, options);
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
			if (source is ExpressionQuery<TSource>)
			{
				var dic = new Dictionary<TKey,TElement>();
				await ((ExpressionQuery<TSource>)source).GetForEachAsync(item => dic.Add(keySelector(item), elementSelector(item)), token, options);
				return dic;
			}

			return await GetTask(
				() => source.AsEnumerable().TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector, elementSelector, comparer),
				token,
				options);
		}

		#endregion

		#region FirstAsync

		public static Task<TSource> FirstAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return GetTask(source.First);
		}

		public static Task<TSource> FirstAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken)
		{
			return GetTask(source.First, cancellationToken);
		}

		public static Task<TSource> FirstAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate)
		{
			return GetTask(() => source.First(predicate));
		}

		public static Task<TSource> FirstAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken)
		{
			return GetTask(() => source.First(predicate), cancellationToken);
		}

		#endregion

		#region FirstOrDefaultAsync

		public static Task<TSource> FirstOrDefaultAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return GetTask(source.FirstOrDefault);
		}

		public static Task<TSource> FirstOrDefaultAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken)
		{
			return GetTask(source.FirstOrDefault, cancellationToken);
		}

		public static Task<TSource> FirstOrDefaultAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate)
		{
			return GetTask(() => source.FirstOrDefault(predicate));
		}

		public static Task<TSource> FirstOrDefaultAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken)
		{
			return GetTask(() => source.FirstOrDefault(predicate), cancellationToken);
		}

		#endregion

		#region SingleAsync

		public static Task<TSource> SingleAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return GetTask(source.Single);
		}

		public static Task<TSource> SingleAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken)
		{
			return GetTask(source.Single, cancellationToken);
		}

		public static Task<TSource> SingleAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate)
		{
			return GetTask(() => source.Single(predicate));
		}

		public static Task<TSource> SingleAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken)
		{
			return GetTask(() => source.Single(predicate), cancellationToken);
		}

		#endregion

		#region SingleOrDefaultAsync

		public static Task<TSource> SingleOrDefaultAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return GetTask(source.SingleOrDefault);
		}

		public static Task<TSource> SingleOrDefaultAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken)
		{
			return GetTask(source.SingleOrDefault, cancellationToken);
		}

		public static Task<TSource> SingleOrDefaultAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate)
		{
			return GetTask(() => source.SingleOrDefault(predicate));
		}

		public static Task<TSource> SingleOrDefaultAsync<TSource>(
			this IQueryable<TSource>        source,
			Expression<Func<TSource, bool>> predicate,
			CancellationToken               cancellationToken)
		{
			return GetTask(() => source.SingleOrDefault(predicate), cancellationToken);
		}

		#endregion

		#region ContainsAsync

		public static Task<bool> ContainsAsync<TSource>(
			this IQueryable<TSource> source,
			TSource                  item)
		{
			return GetTask(() => source.Contains(item));
		}

		public static Task<bool> ContainsAsync<TSource>(
			this IQueryable<TSource> source,
			TSource                  item,
			CancellationToken        cancellationToken)
		{
			return GetTask(() => source.Contains(item), cancellationToken);
		}

		#endregion

		#region AnyAsync

		public static Task<bool> AnyAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return GetTask(source.Any);
		}

		public static Task<bool> AnyAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken)
		{
			return GetTask(source.Any, cancellationToken);
		}

		public static Task<bool> AnyAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate)
		{
			return GetTask(() => source.Any(predicate));
		}

		public static Task<bool> AnyAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken)
		{
			return GetTask(() => source.Any(predicate), cancellationToken);
		}

		#endregion

		#region AllAsync

		public static Task<bool> AllAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate)
		{
			return GetTask(() => source.All(predicate));
		}

		public static Task<bool> AllAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken)
		{
			return GetTask(() => source.All(predicate), cancellationToken);
		}

		#endregion

		#region CountAsync

		public static Task<int> CountAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return GetTask(source.Count);
		}
		public static Task<int> CountAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken cancellationToken)
		{
			return GetTask(source.Count, cancellationToken);
		}
		public static Task<int> CountAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate)
		{
			return GetTask(() => source.Count(predicate));
		}

		public static Task<int> CountAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken)
		{
			return GetTask(() => source.Count(predicate), cancellationToken);
		}

		#endregion

		#region LongCountAsync

		public static Task<long> LongCountAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return GetTask(source.LongCount);
		}

		public static Task<long> LongCountAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken)
		{
			return GetTask(source.LongCount, cancellationToken);
		}

		public static Task<long> LongCountAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate)
		{
			return GetTask(() => source.LongCount(predicate));
		}

		public static Task<long> LongCountAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken)
		{
			return GetTask(() => source.LongCount(predicate), cancellationToken);
		}

		#endregion

		#region MinAsync

		public static Task<TSource> MinAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return GetTask(source.Min);
		}

		public static Task<TSource> MinAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken)
		{
			return GetTask(source.Min, cancellationToken);
		}

		public static Task<TResult> MinAsync<TSource, TResult>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,TResult>> selector)
		{
			return GetTask(() => source.Min(selector));
		}

		public static Task<TResult> MinAsync<TSource, TResult>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,TResult>> selector,
			CancellationToken                 cancellationToken)
		{
			return GetTask(() => source.Min(selector), cancellationToken);
		}

		#endregion

		#region MaxAsync

		public static Task<TSource> MaxAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return GetTask(source.Max);
		}

		public static Task<TSource> MaxAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken)
		{
			return GetTask(source.Max, cancellationToken);
		}

		public static Task<TResult> MaxAsync<TSource,TResult>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,TResult>> selector)
		{
			return GetTask(() => source.Max(selector));
		}

		public static Task<TResult> MaxAsync<TSource,TResult>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,TResult>> selector,
			CancellationToken                 cancellationToken)
		{
			return GetTask(() => source.Max(selector), cancellationToken);
		}

		#endregion

		#region SumAsync

		public static Task<int> SumAsync(this IQueryable<int> source)
		{
			return GetTask(source.Sum);
		}

		public static Task<int> SumAsync(this IQueryable<int> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Sum, cancellationToken);
		}

		public static Task<int?> SumAsync(this IQueryable<int?> source)
		{
			return GetTask(source.Sum);
		}

		public static Task<int?> SumAsync(this IQueryable<int?> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Sum, cancellationToken);
		}

		public static Task<long> SumAsync(this IQueryable<long> source)
		{
			return GetTask(source.Sum);
		}

		public static Task<long> SumAsync(this IQueryable<long> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Sum, cancellationToken);
		}

		public static Task<long?> SumAsync(this IQueryable<long?> source)
		{
			return GetTask(source.Sum);
		}

		public static Task<long?> SumAsync(this IQueryable<long?> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Sum, cancellationToken);
		}

		public static Task<float> SumAsync(this IQueryable<float> source)
		{
			return GetTask(source.Sum);
		}

		public static Task<float> SumAsync(this IQueryable<float> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Sum, cancellationToken);
		}

		public static Task<float?> SumAsync(this IQueryable<float?> source)
		{
			return GetTask(source.Sum);
		}

		public static Task<float?> SumAsync(this IQueryable<float?> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Sum, cancellationToken);
		}

		public static Task<double> SumAsync(this IQueryable<double> source)
		{
			return GetTask(source.Sum);
		}

		public static Task<double> SumAsync(this IQueryable<double> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Sum, cancellationToken);
		}

		public static Task<double?> SumAsync(this IQueryable<double?> source)
		{
			return GetTask(source.Sum);
		}

		public static Task<double?> SumAsync(this IQueryable<double?> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Sum, cancellationToken);
		}

		public static Task<decimal> SumAsync(this IQueryable<decimal> source)
		{
			return GetTask(source.Sum);
		}

		public static Task<decimal> SumAsync(this IQueryable<decimal> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Sum, cancellationToken);
		}

		public static Task<decimal?> SumAsync(this IQueryable<decimal?> source)
		{
			return GetTask(source.Sum);
		}

		public static Task<decimal?> SumAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Sum, cancellationToken);
		}

		public static Task<int> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int>> selector)
		{
			return GetTask(() => source.Sum(selector));
		}

		public static Task<int> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Sum(selector), cancellationToken);
		}

		public static Task<int?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector)
		{
			return GetTask(() => source.Sum(selector));
		}

		public static Task<int?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Sum(selector), cancellationToken);
		}

		public static Task<long> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long>> selector)
		{
			return GetTask(() => source.Sum(selector));
		}

		public static Task<long> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Sum(selector), cancellationToken);
		}

		public static Task<long?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
		{
			return GetTask(() => source.Sum(selector));
		}

		public static Task<long?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Sum(selector), cancellationToken);
		}

		public static Task<float> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float>> selector)
		{
			return GetTask(() => source.Sum(selector));
		}

		public static Task<float> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Sum(selector), cancellationToken);
		}

		public static Task<float?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
		{
			return GetTask(() => source.Sum(selector));
		}

		public static Task<float?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Sum(selector), cancellationToken);
		}

		public static Task<double> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double>> selector)
		{
			return GetTask(() => source.Sum(selector));
		}

		public static Task<double> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Sum(selector), cancellationToken);
		}

		public static Task<double?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector)
		{
			return GetTask(() => source.Sum(selector));
		}

		public static Task<double?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Sum(selector), cancellationToken);
		}

		public static Task<decimal> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector)
		{
			return GetTask(() => source.Sum(selector));
		}

		public static Task<decimal> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Sum(selector), cancellationToken);
		}

		public static Task<decimal?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector)
		{
			return GetTask(() => source.Sum(selector));
		}

		public static Task<decimal?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Sum(selector), cancellationToken);
		}

		#endregion

		#region AverageAsync

		public static Task<double> AverageAsync(this IQueryable<int> source)
		{
			return GetTask(source.Average);
		}

		public static Task<double> AverageAsync(this IQueryable<int> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Average, cancellationToken);
		}

		public static Task<double?> AverageAsync(this IQueryable<int?> source)
		{
			return GetTask(source.Average);
		}

		public static Task<double?> AverageAsync(this IQueryable<int?> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Average, cancellationToken);
		}

		public static Task<double> AverageAsync(this IQueryable<long> source)
		{
			return GetTask(source.Average);
		}

		public static Task<double> AverageAsync(this IQueryable<long> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Average, cancellationToken);
		}

		public static Task<double?> AverageAsync(this IQueryable<long?> source)
		{
			return GetTask(source.Average);
		}

		public static Task<double?> AverageAsync(this IQueryable<long?> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Average, cancellationToken);
		}

		public static Task<float> AverageAsync(this IQueryable<float> source)
		{
			return GetTask(source.Average);
		}

		public static Task<float> AverageAsync(this IQueryable<float> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Average, cancellationToken);
		}

		public static Task<float?> AverageAsync(this IQueryable<float?> source)
		{
			return GetTask(source.Average);
		}

		public static Task<float?> AverageAsync(this IQueryable<float?> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Average, cancellationToken);
		}

		public static Task<double> AverageAsync(this IQueryable<double> source)
		{
			return GetTask(source.Average);
		}

		public static Task<double> AverageAsync(this IQueryable<double> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Average, cancellationToken);
		}

		public static Task<double?> AverageAsync(this IQueryable<double?> source)
		{
			return GetTask(source.Average);
		}

		public static Task<double?> AverageAsync(this IQueryable<double?> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Average, cancellationToken);
		}

		public static Task<decimal> AverageAsync(this IQueryable<decimal> source)
		{
			return GetTask(source.Average);
		}

		public static Task<decimal> AverageAsync(this IQueryable<decimal> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Average, cancellationToken);
		}

		public static Task<decimal?> AverageAsync(this IQueryable<decimal?> source)
		{
			return GetTask(source.Average);
		}

		public static Task<decimal?> AverageAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken)
		{
			return GetTask(source.Average, cancellationToken);
		}

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int>> selector)
		{
			return GetTask(() => source.Average(selector));
		}

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Average(selector), cancellationToken);
		}

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector)
		{
			return GetTask(() => source.Average(selector));
		}

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Average(selector), cancellationToken);
		}

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long>> selector)
		{
			return GetTask(() => source.Average(selector));
		}

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Average(selector), cancellationToken);
		}

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector)
		{
			return GetTask(() => source.Average(selector));
		}

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Average(selector), cancellationToken);
		}

		public static Task<float> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float>> selector)
		{
			return GetTask(() => source.Average(selector));
		}

		public static Task<float> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Average(selector), cancellationToken);
		}

		public static Task<float?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float?>> selector)
		{
			return GetTask(() => source.Average(selector));
		}

		public static Task<float?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float?>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Average(selector), cancellationToken);
		}

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double>> selector)
		{
			return GetTask(() => source.Average(selector));
		}

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Average(selector), cancellationToken);
		}

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector)
		{
			return GetTask(() => source.Average(selector));
		}

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Average(selector), cancellationToken);
		}

		public static Task<decimal> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector)
		{
			return GetTask(() => source.Average(selector));
		}

		public static Task<decimal> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Average(selector), cancellationToken);
		}

		public static Task<decimal?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector)
		{
			return GetTask(() => source.Average(selector));
		}

		public static Task<decimal?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector, CancellationToken cancellationToken)
		{
			return GetTask(() => source.Average(selector), cancellationToken);
		}

		#endregion
	}
}
