using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB
{
	using System.Linq.Expressions;

	public static class AsyncExtensions
	{
		#region ForEachAsync

		public static Task ForEachAsync<TSource>(this IQueryable<TSource> source, Action<TSource> action)
		{
			return new Task(() =>
			{
				foreach (var item in source)
					action(item);
			});
		}

		public static Task ForEachAsync<TSource>(
			this IQueryable<TSource> source, Action<TSource> action, CancellationToken token)
		{
			return new Task(() =>
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

		public static Task ForEachAsync<TSource>(
			this IQueryable<TSource> source, Action<TSource> action, TaskCreationOptions options)
		{
			return new Task(() =>
			{
				foreach (var item in source)
					action(item);
			},
			options);
		}

		public static Task ForEachAsync<TSource>(
			this IQueryable<TSource> source, Action<TSource> action, CancellationToken token, TaskCreationOptions options)
		{
			return new Task(() =>
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
			return new Task<List<TSource>>(source.ToList);
		}

		public static Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken token)
		{
			return new Task<List<TSource>>(
				() => source.TakeWhile(_ => !token.IsCancellationRequested).ToList(),
				token);
		}

		public static Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, TaskCreationOptions options)
		{
			return new Task<List<TSource>>(source.ToList, options);
		}

		public static Task<List<TSource>> ToListAsync<TSource>(
			this IQueryable<TSource> source, CancellationToken token, TaskCreationOptions options)
		{
			return new Task<List<TSource>>(
				() => source.TakeWhile(_ => !token.IsCancellationRequested).ToList(),
				token,
				options);
		}

		#endregion

		#region ToArrayAsync

		public static Task<TSource[]> ToArrayAsync<TSource>(this IQueryable<TSource> source)
		{
			return new Task<TSource[]>(source.ToArray);
		}

		public static Task<TSource[]> ToArrayAsync<TSource>(this IQueryable<TSource> source, CancellationToken token)
		{
			return new Task<TSource[]>(
				() => source.TakeWhile(_ => !token.IsCancellationRequested).ToArray(),
				token);
		}

		public static Task<TSource[]> ToArrayAsync<TSource>(this IQueryable<TSource> source, TaskCreationOptions options)
		{
			return new Task<TSource[]>(source.ToArray, options);
		}

		public static Task<TSource[]> ToArrayAsync<TSource>(
			this IQueryable<TSource> source, CancellationToken token, TaskCreationOptions options)
		{
			return new Task<TSource[]>(
				() => source.TakeWhile(_ => !token.IsCancellationRequested).ToArray(),
				token,
				options);
		}

		#endregion

		#region ToDictionaryAsync

		public static Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector)
		{
			return new Task<Dictionary<TKey,TSource>>(() => source.ToDictionary(keySelector));
		}

		public static Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			CancellationToken        token)
		{
			return new Task<Dictionary<TKey,TSource>>(
				() => source.TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector),
				token);
		}

		public static Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			TaskCreationOptions      options)
		{
			return new Task<Dictionary<TKey,TSource>>(() => source.ToDictionary(keySelector), options);
		}

		public static Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			CancellationToken        token,
			TaskCreationOptions      options)
		{
			return new Task<Dictionary<TKey,TSource>>(
				() => source.TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector),
				token,
				options);
		}

		public static Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			IEqualityComparer<TKey>  comparer)
		{
			return new Task<Dictionary<TKey,TSource>>(() => source.ToDictionary(keySelector, comparer));
		}

		public static Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			IEqualityComparer<TKey>  comparer,
			CancellationToken        token)
		{
			return new Task<Dictionary<TKey,TSource>>(
				() => source.TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector, comparer),
				token);
		}

		public static Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			IEqualityComparer<TKey>  comparer,
			TaskCreationOptions      options)
		{
			return new Task<Dictionary<TKey,TSource>>(() => source.ToDictionary(keySelector, comparer), options);
		}

		public static Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			IEqualityComparer<TKey>  comparer,
			CancellationToken        token,
			TaskCreationOptions      options)
		{
			return new Task<Dictionary<TKey,TSource>>(
				() => source.TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector, comparer),
				token,
				options);
		}

		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector)
		{
			return new Task<Dictionary<TKey,TElement>>(
				() => source.ToDictionary(keySelector, elementSelector));
		}

		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			CancellationToken        token)
		{
			return new Task<Dictionary<TKey,TElement>>(
				() => source.TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector, elementSelector),
				token);
		}

		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			TaskCreationOptions      options)
		{
			return new Task<Dictionary<TKey,TElement>>(
				() => source.ToDictionary(keySelector, elementSelector),
				options);
		}

		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			CancellationToken        token,
			TaskCreationOptions      options)
		{
			return new Task<Dictionary<TKey,TElement>>(
				() => source.TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector, elementSelector),
				token,
				options);
		}

		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			IEqualityComparer<TKey>  comparer)
		{
			return new Task<Dictionary<TKey,TElement>>(
				() => source.ToDictionary(keySelector, elementSelector, comparer));
		}

		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			IEqualityComparer<TKey>  comparer,
			CancellationToken        token)
		{
			return new Task<Dictionary<TKey,TElement>>(
				() => source.TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector, elementSelector, comparer),
				token);
		}

		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			IEqualityComparer<TKey>  comparer,
			TaskCreationOptions      options)
		{
			return new Task<Dictionary<TKey,TElement>>(
				() => source.ToDictionary(keySelector, elementSelector, comparer),
				options);
		}

		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			this IQueryable<TSource> source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			IEqualityComparer<TKey>  comparer,
			CancellationToken        token,
			TaskCreationOptions      options)
		{
			return new Task<Dictionary<TKey,TElement>>(
				() => source.TakeWhile(_ => !token.IsCancellationRequested).ToDictionary(keySelector, elementSelector, comparer),
				token,
				options);
		}

		#endregion

		#region FirstAsync

		public static Task<TSource> FirstAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return new Task<TSource>(source.First);
		}

		public static Task<TSource> FirstAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken)
		{
			return new Task<TSource>(source.First, cancellationToken);
		}

		public static Task<TSource> FirstAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate)
		{
			return new Task<TSource>(() => source.First(predicate));
		}

		public static Task<TSource> FirstAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken)
		{
			return new Task<TSource>(() => source.First(predicate), cancellationToken);
		}

		#endregion

		#region FirstOrDefaultAsync

		public static Task<TSource> FirstOrDefaultAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return new Task<TSource>(source.FirstOrDefault);
		}

		public static Task<TSource> FirstOrDefaultAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken)
		{
			return new Task<TSource>(source.FirstOrDefault, cancellationToken);
		}

		public static Task<TSource> FirstOrDefaultAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate)
		{
			return new Task<TSource>(() => source.FirstOrDefault(predicate));
		}

		public static Task<TSource> FirstOrDefaultAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken)
		{
			return new Task<TSource>(() => source.FirstOrDefault(predicate), cancellationToken);
		}

		#endregion

		#region SingleAsync

		public static Task<TSource> SingleAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return new Task<TSource>(source.Single);
		}

		public static Task<TSource> SingleAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken)
		{
			return new Task<TSource>(source.Single, cancellationToken);
		}

		public static Task<TSource> SingleAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate)
		{
			return new Task<TSource>(() => source.Single(predicate));
		}

		public static Task<TSource> SingleAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken)
		{
			return new Task<TSource>(() => source.Single(predicate), cancellationToken);
		}

		#endregion

		#region SingleOrDefaultAsync

		public static Task<TSource> SingleOrDefaultAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return new Task<TSource>(source.SingleOrDefault);
		}

		public static Task<TSource> SingleOrDefaultAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken)
		{
			return new Task<TSource>(source.SingleOrDefault, cancellationToken);
		}

		public static Task<TSource> SingleOrDefaultAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate)
		{
			return new Task<TSource>(() => source.SingleOrDefault(predicate));
		}

		public static Task<TSource> SingleOrDefaultAsync<TSource>(
			this IQueryable<TSource>        source,
			Expression<Func<TSource, bool>> predicate,
			CancellationToken               cancellationToken)
		{
			return new Task<TSource>(() => source.SingleOrDefault(predicate), cancellationToken);
		}

		#endregion

		#region ContainsAsync

		public static Task<bool> ContainsAsync<TSource>(
			this IQueryable<TSource> source,
			TSource                  item)
		{
			return new Task<bool>(() => source.Contains(item));
		}

		public static Task<bool> ContainsAsync<TSource>(
			this IQueryable<TSource> source,
			TSource                  item,
			CancellationToken        cancellationToken)
		{
			return new Task<bool>(() => source.Contains(item), cancellationToken);
		}

		#endregion

		#region AnyAsync

		public static Task<bool> AnyAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return new Task<bool>(source.Any);
		}

		public static Task<bool> AnyAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken)
		{
			return new Task<bool>(source.Any, cancellationToken);
		}

		public static Task<bool> AnyAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate)
		{
			return new Task<bool>(() => source.Any(predicate));
		}

		public static Task<bool> AnyAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken)
		{
			return new Task<bool>(() => source.Any(predicate), cancellationToken);
		}

		#endregion

		#region AllAsync

		public static Task<bool> AllAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate)
		{
			return new Task<bool>(() => source.All(predicate));
		}

		public static Task<bool> AllAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken)
		{
			return new Task<bool>(() => source.All(predicate), cancellationToken);
		}

		#endregion

		#region CountAsync

		public static Task<int> CountAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return new Task<int>(source.Count);
		}
		public static Task<int> CountAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken cancellationToken)
		{
			return new Task<int>(source.Count, cancellationToken);
		}
		public static Task<int> CountAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate)
		{
			return new Task<int>(() => source.Count(predicate));
		}

		public static Task<int> CountAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken)
		{
			return new Task<int>(() => source.Count(predicate), cancellationToken);
		}

		#endregion

		#region LongCountAsync

		public static Task<long> LongCountAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return new Task<long>(source.LongCount);
		}

		public static Task<long> LongCountAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken)
		{
			return new Task<long>(source.LongCount, cancellationToken);
		}

		public static Task<long> LongCountAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate)
		{
			return new Task<long>(() => source.LongCount(predicate));
		}

		public static Task<long> LongCountAsync<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken)
		{
			return new Task<long>(() => source.LongCount(predicate), cancellationToken);
		}

		#endregion

		#region MinAsync

		public static Task<TSource> MinAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return new Task<TSource>(source.Min);
		}

		public static Task<TSource> MinAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken)
		{
			return new Task<TSource>(source.Min, cancellationToken);
		}

		public static Task<TResult> MinAsync<TSource, TResult>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,TResult>> selector)
		{
			return new Task<TResult>(() => source.Min(selector));
		}

		public static Task<TResult> MinAsync<TSource, TResult>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,TResult>> selector,
			CancellationToken                 cancellationToken)
		{
			return new Task<TResult>(() => source.Min(selector), cancellationToken);
		}

		#endregion

		#region MaxAsync

		public static Task<TSource> MaxAsync<TSource>(
			this IQueryable<TSource> source)
		{
			return new Task<TSource>(source.Max);
		}

		public static Task<TSource> MaxAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken)
		{
			return new Task<TSource>(source.Max, cancellationToken);
		}

		public static Task<TResult> MaxAsync<TSource,TResult>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,TResult>> selector)
		{
			return new Task<TResult>(() => source.Max(selector));
		}

		public static Task<TResult> MaxAsync<TSource,TResult>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,TResult>> selector,
			CancellationToken                 cancellationToken)
		{
			return new Task<TResult>(() => source.Max(selector), cancellationToken);
		}

		#endregion

		#region SumAsync

		public static Task<int> SumAsync(this IQueryable<int> source)
		{
			return new Task<int>(source.Sum);
		}

		public static Task<int> SumAsync(this IQueryable<int> source, CancellationToken cancellationToken)
		{
			return new Task<int>(source.Sum, cancellationToken);
		}

		public static Task<int?> SumAsync(this IQueryable<int?> source)
		{
			return new Task<int?>(source.Sum);
		}

		public static Task<int?> SumAsync(this IQueryable<int?> source, CancellationToken cancellationToken)
		{
			return new Task<int?>(source.Sum, cancellationToken);
		}

		public static Task<long> SumAsync(this IQueryable<long> source)
		{
			return new Task<long>(source.Sum);
		}

		public static Task<long> SumAsync(this IQueryable<long> source, CancellationToken cancellationToken)
		{
			return new Task<long>(source.Sum, cancellationToken);
		}

		public static Task<long?> SumAsync(this IQueryable<long?> source)
		{
			return new Task<long?>(source.Sum);
		}

		public static Task<long?> SumAsync(this IQueryable<long?> source, CancellationToken cancellationToken)
		{
			return new Task<long?>(source.Sum, cancellationToken);
		}

		public static Task<float> SumAsync(this IQueryable<float> source)
		{
			return new Task<float>(source.Sum);
		}

		public static Task<float> SumAsync(this IQueryable<float> source, CancellationToken cancellationToken)
		{
			return new Task<float>(source.Sum, cancellationToken);
		}

		public static Task<float?> SumAsync(this IQueryable<float?> source)
		{
			return new Task<float?>(source.Sum);
		}

		public static Task<float?> SumAsync(this IQueryable<float?> source, CancellationToken cancellationToken)
		{
			return new Task<float?>(source.Sum, cancellationToken);
		}

		public static Task<double> SumAsync(this IQueryable<double> source)
		{
			return new Task<double>(source.Sum);
		}

		public static Task<double> SumAsync(this IQueryable<double> source, CancellationToken cancellationToken)
		{
			return new Task<double>(source.Sum, cancellationToken);
		}

		public static Task<double?> SumAsync(this IQueryable<double?> source)
		{
			return new Task<double?>(source.Sum);
		}

		public static Task<double?> SumAsync(this IQueryable<double?> source, CancellationToken cancellationToken)
		{
			return new Task<double?>(source.Sum, cancellationToken);
		}

		public static Task<decimal> SumAsync(this IQueryable<decimal> source)
		{
			return new Task<decimal>(source.Sum);
		}

		public static Task<decimal> SumAsync(this IQueryable<decimal> source, CancellationToken cancellationToken)
		{
			return new Task<decimal>(source.Sum, cancellationToken);
		}

		public static Task<decimal?> SumAsync(this IQueryable<decimal?> source)
		{
			return new Task<decimal?>(source.Sum);
		}

		public static Task<decimal?> SumAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken)
		{
			return new Task<decimal?>(source.Sum, cancellationToken);
		}

		public static Task<int> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int>> selector)
		{
			return new Task<int>(() => source.Sum(selector));
		}

		public static Task<int> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int>> selector, CancellationToken cancellationToken)
		{
			return new Task<int>(() => source.Sum(selector), cancellationToken);
		}

		public static Task<int?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector)
		{
			return new Task<int?>(() => source.Sum(selector));
		}

		public static Task<int?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector, CancellationToken cancellationToken)
		{
			return new Task<int?>(() => source.Sum(selector), cancellationToken);
		}

		public static Task<long> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long>> selector)
		{
			return new Task<long>(() => source.Sum(selector));
		}

		public static Task<long> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long>> selector, CancellationToken cancellationToken)
		{
			return new Task<long>(() => source.Sum(selector), cancellationToken);
		}

		public static Task<long?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
		{
			return new Task<long?>(() => source.Sum(selector));
		}

		public static Task<long?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector, CancellationToken cancellationToken)
		{
			return new Task<long?>(() => source.Sum(selector), cancellationToken);
		}

		public static Task<float> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float>> selector)
		{
			return new Task<float>(() => source.Sum(selector));
		}

		public static Task<float> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float>> selector, CancellationToken cancellationToken)
		{
			return new Task<float>(() => source.Sum(selector), cancellationToken);
		}

		public static Task<float?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
		{
			return new Task<float?>(() => source.Sum(selector));
		}

		public static Task<float?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken)
		{
			return new Task<float?>(() => source.Sum(selector), cancellationToken);
		}

		public static Task<double> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double>> selector)
		{
			return new Task<double>(() => source.Sum(selector));
		}

		public static Task<double> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double>> selector, CancellationToken cancellationToken)
		{
			return new Task<double>(() => source.Sum(selector), cancellationToken);
		}

		public static Task<double?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector)
		{
			return new Task<double?>(() => source.Sum(selector));
		}

		public static Task<double?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector, CancellationToken cancellationToken)
		{
			return new Task<double?>(() => source.Sum(selector), cancellationToken);
		}

		public static Task<decimal> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector)
		{
			return new Task<decimal>(() => source.Sum(selector));
		}

		public static Task<decimal> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector, CancellationToken cancellationToken)
		{
			return new Task<decimal>(() => source.Sum(selector), cancellationToken);
		}

		public static Task<decimal?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector)
		{
			return new Task<decimal?>(() => source.Sum(selector));
		}

		public static Task<decimal?> SumAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector, CancellationToken cancellationToken)
		{
			return new Task<decimal?>(() => source.Sum(selector), cancellationToken);
		}

		#endregion

		#region AverageAsync

		public static Task<double> AverageAsync(this IQueryable<int> source)
		{
			return new Task<double>(source.Average);
		}

		public static Task<double> AverageAsync(this IQueryable<int> source, CancellationToken cancellationToken)
		{
			return new Task<double>(source.Average, cancellationToken);
		}

		public static Task<double?> AverageAsync(this IQueryable<int?> source)
		{
			return new Task<double?>(source.Average);
		}

		public static Task<double?> AverageAsync(this IQueryable<int?> source, CancellationToken cancellationToken)
		{
			return new Task<double?>(source.Average, cancellationToken);
		}

		public static Task<double> AverageAsync(this IQueryable<long> source)
		{
			return new Task<double>(source.Average);
		}

		public static Task<double> AverageAsync(this IQueryable<long> source, CancellationToken cancellationToken)
		{
			return new Task<double>(source.Average, cancellationToken);
		}

		public static Task<double?> AverageAsync(this IQueryable<long?> source)
		{
			return new Task<double?>(source.Average);
		}

		public static Task<double?> AverageAsync(this IQueryable<long?> source, CancellationToken cancellationToken)
		{
			return new Task<double?>(source.Average, cancellationToken);
		}

		public static Task<float> AverageAsync(this IQueryable<float> source)
		{
			return new Task<float>(source.Average);
		}

		public static Task<float> AverageAsync(this IQueryable<float> source, CancellationToken cancellationToken)
		{
			return new Task<float>(source.Average, cancellationToken);
		}

		public static Task<float?> AverageAsync(this IQueryable<float?> source)
		{
			return new Task<float?>(source.Average);
		}

		public static Task<float?> AverageAsync(this IQueryable<float?> source, CancellationToken cancellationToken)
		{
			return new Task<float?>(source.Average, cancellationToken);
		}

		public static Task<double> AverageAsync(this IQueryable<double> source)
		{
			return new Task<double>(source.Average);
		}

		public static Task<double> AverageAsync(this IQueryable<double> source, CancellationToken cancellationToken)
		{
			return new Task<double>(source.Average, cancellationToken);
		}

		public static Task<double?> AverageAsync(this IQueryable<double?> source)
		{
			return new Task<double?>(source.Average);
		}

		public static Task<double?> AverageAsync(this IQueryable<double?> source, CancellationToken cancellationToken)
		{
			return new Task<double?>(source.Average, cancellationToken);
		}

		public static Task<decimal> AverageAsync(this IQueryable<decimal> source)
		{
			return new Task<decimal>(source.Average);
		}

		public static Task<decimal> AverageAsync(this IQueryable<decimal> source, CancellationToken cancellationToken)
		{
			return new Task<decimal>(source.Average, cancellationToken);
		}

		public static Task<decimal?> AverageAsync(this IQueryable<decimal?> source)
		{
			return new Task<decimal?>(source.Average);
		}

		public static Task<decimal?> AverageAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken)
		{
			return new Task<decimal?>(source.Average, cancellationToken);
		}

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int>> selector)
		{
			return new Task<double>(() => source.Average(selector));
		}

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int>> selector, CancellationToken cancellationToken)
		{
			return new Task<double>(() => source.Average(selector), cancellationToken);
		}

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector)
		{
			return new Task<double?>(() => source.Average(selector));
		}

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,int?>> selector, CancellationToken cancellationToken)
		{
			return new Task<double?>(() => source.Average(selector), cancellationToken);
		}

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long>> selector)
		{
			return new Task<double>(() => source.Average(selector));
		}

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long>> selector, CancellationToken cancellationToken)
		{
			return new Task<double>(() => source.Average(selector), cancellationToken);
		}

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector)
		{
			return new Task<double?>(() => source.Average(selector));
		}

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,long?>> selector, CancellationToken cancellationToken)
		{
			return new Task<double?>(() => source.Average(selector), cancellationToken);
		}

		public static Task<float> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float>> selector)
		{
			return new Task<float>(() => source.Average(selector));
		}

		public static Task<float> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float>> selector, CancellationToken cancellationToken)
		{
			return new Task<float>(() => source.Average(selector), cancellationToken);
		}

		public static Task<float?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float?>> selector)
		{
			return new Task<float?>(() => source.Average(selector));
		}

		public static Task<float?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,float?>> selector, CancellationToken cancellationToken)
		{
			return new Task<float?>(() => source.Average(selector), cancellationToken);
		}

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double>> selector)
		{
			return new Task<double>(() => source.Average(selector));
		}

		public static Task<double> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double>> selector, CancellationToken cancellationToken)
		{
			return new Task<double>(() => source.Average(selector), cancellationToken);
		}

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector)
		{
			return new Task<double?>(() => source.Average(selector));
		}

		public static Task<double?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,double?>> selector, CancellationToken cancellationToken)
		{
			return new Task<double?>(() => source.Average(selector), cancellationToken);
		}

		public static Task<decimal> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector)
		{
			return new Task<decimal>(() => source.Average(selector));
		}

		public static Task<decimal> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal>> selector, CancellationToken cancellationToken)
		{
			return new Task<decimal>(() => source.Average(selector), cancellationToken);
		}

		public static Task<decimal?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector)
		{
			return new Task<decimal?>(() => source.Average(selector));
		}

		public static Task<decimal?> AverageAsync<TSource>(
			this IQueryable<TSource> source, Expression<Func<TSource,decimal?>> selector, CancellationToken cancellationToken)
		{
			return new Task<decimal?>(() => source.Average(selector), cancellationToken);
		}

		#endregion
	}
}
