using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB
{
	/// <summary>
	/// Provides a way to handle custom extensions
	/// </summary>
	public interface IExtensionsAdapter
	{
		Task ForEachAsync<TSource>(
			IQueryable<TSource> source, 
			Action<TSource>     action, 
			CancellationToken   token);

		Task<List<TSource>> ToListAsync<TSource>(
			IQueryable<TSource> source, 
			CancellationToken   token);

		Task<TSource[]> ToArrayAsync<TSource>(
			IQueryable<TSource> source, 
			CancellationToken   token);

		Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(
			IQueryable<TSource> source,
			Func<TSource, TKey> keySelector, 
			CancellationToken   token);

		Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			IQueryable<TSource>      source,
			Func<TSource,TKey>       keySelector,
			IEqualityComparer<TKey>  comparer,
			CancellationToken        token);

		Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			IQueryable<TSource>      source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			CancellationToken        token);

		Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			IQueryable<TSource>      source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			IEqualityComparer<TKey>  comparer,
			CancellationToken        token);

		Task<TSource> FirstAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token);

		Task<TSource> FirstAsync<TSource>(
			IQueryable<TSource>            source, 
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token);

		Task<TSource> FirstOrDefaultAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token);

		Task<TSource> FirstOrDefaultAsync<TSource>(
			IQueryable<TSource>            source, 
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token);

		Task<TSource> SingleAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token);

		Task<TSource> SingleAsync<TSource>(
			IQueryable<TSource>            source, 
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token);

		Task<TSource> SingleOrDefaultAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token);

		Task<TSource> SingleOrDefaultAsync<TSource>(
			IQueryable<TSource>            source, 
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token);

		Task<bool> ContainsAsync<TSource>(
			IQueryable<TSource> source, 
			TSource             item,
			CancellationToken   token);

		Task<bool> AnyAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token);

		Task<bool> AnyAsync<TSource>(
			IQueryable<TSource>            source, 
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token);

		Task<bool> AllAsync<TSource>(
			IQueryable<TSource>            source, 
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token);

		Task<int> CountAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token);

		Task<int> CountAsync<TSource>(
			IQueryable<TSource>            source, 
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token);

		Task<long> LongCountAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token);

		Task<long> LongCountAsync<TSource>(
			IQueryable<TSource>            source, 
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token);

		Task<TSource> MinAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token);

		Task<TResult> MinAsync<TSource,TResult>(
			IQueryable<TSource>               source, 
			Expression<Func<TSource,TResult>> selector,
			CancellationToken                 token);

		Task<TSource> MaxAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token);

		Task<TResult> MaxAsync<TSource,TResult>(
			IQueryable<TSource>               source, 
			Expression<Func<TSource,TResult>> selector,
			CancellationToken                 token);

		#region SumAsync

		Task<int> SumAsync(
			IQueryable<int>   source,
			CancellationToken token);

		Task<int?> SumAsync(
			IQueryable<int?>  source,
			CancellationToken token);

		Task<long> SumAsync(
			IQueryable<long>  source,
			CancellationToken token);

		Task<long?> SumAsync(
			IQueryable<long?> source,
			CancellationToken token);

		Task<float> SumAsync(
			IQueryable<float> source,
			CancellationToken token);

		Task<float?> SumAsync(
			IQueryable<float?> source,
			CancellationToken  token);

		Task<double> SumAsync(
			IQueryable<double> source,
			CancellationToken  token);

		Task<double?> SumAsync(
			IQueryable<double?> source,
			CancellationToken   token);

		Task<decimal> SumAsync(
			IQueryable<decimal> source,
			CancellationToken   token);

		Task<decimal?> SumAsync(
			IQueryable<decimal?> source,
			CancellationToken    token);

		Task<int> SumAsync<TSource>(
			IQueryable<TSource>           source,
			Expression<Func<TSource,int>> selector,
			CancellationToken             token);

		Task<int?> SumAsync<TSource>(
			IQueryable<TSource>            source,
			Expression<Func<TSource,int?>> selector,
			CancellationToken              token);

		Task<long> SumAsync<TSource>(
			IQueryable<TSource>            source,
			Expression<Func<TSource,long>> selector,
			CancellationToken              token);

		Task<long?> SumAsync<TSource>(
			IQueryable<TSource>             source,
			Expression<Func<TSource,long?>> selector,
			CancellationToken               token);

		Task<float> SumAsync<TSource>(
			IQueryable<TSource>             source,
			Expression<Func<TSource,float>> selector,
			CancellationToken               token);

		Task<float?> SumAsync<TSource>(
			IQueryable<TSource>              source,
			Expression<Func<TSource,float?>> selector,
			CancellationToken                token);

		Task<double> SumAsync<TSource>(
			IQueryable<TSource>              source,
			Expression<Func<TSource,double>> selector,
			CancellationToken                token);

		Task<double?> SumAsync<TSource>(
			IQueryable<TSource>               source,
			Expression<Func<TSource,double?>> selector,
			CancellationToken                 token);

		Task<decimal> SumAsync<TSource>(
			IQueryable<TSource>               source,
			Expression<Func<TSource,decimal>> selector,
			CancellationToken                 token);

		Task<decimal?> SumAsync<TSource>(
			IQueryable<TSource>                source,
			Expression<Func<TSource,decimal?>> selector,
			CancellationToken                  token);

		#endregion SumAsync

		#region AverageAsync

		Task<double> AverageAsync(
			IQueryable<int>   source,
			CancellationToken token);

		Task<double?> AverageAsync(
			IQueryable<int?>  source,
			CancellationToken token);

		Task<double> AverageAsync(
			IQueryable<long>  source,
			CancellationToken token);

		Task<double?> AverageAsync(
			IQueryable<long?> source,
			CancellationToken token);

		Task<float> AverageAsync(
			IQueryable<float> source,
			CancellationToken token);

		Task<float?> AverageAsync(
			IQueryable<float?> source,
			CancellationToken  token);

		Task<double> AverageAsync(
			IQueryable<double> source,
			CancellationToken  token);

		Task<double?> AverageAsync(
			IQueryable<double?> source,
			CancellationToken   token);

		Task<decimal> AverageAsync(
			IQueryable<decimal> source,
			CancellationToken   token);

		Task<decimal?> AverageAsync(
			IQueryable<decimal?> source,
			CancellationToken    token);

		Task<double> AverageAsync<TSource>(
			IQueryable<TSource>           source,
			Expression<Func<TSource,int>> selector,
			CancellationToken             token);

		Task<double?> AverageAsync<TSource>(
			IQueryable<TSource>            source,
			Expression<Func<TSource,int?>> selector,
			CancellationToken              token);

		Task<double> AverageAsync<TSource>(
			IQueryable<TSource>            source,
			Expression<Func<TSource,long>> selector,
			CancellationToken              token);

		Task<double?> AverageAsync<TSource>(
			IQueryable<TSource>             source,
			Expression<Func<TSource,long?>> selector,
			CancellationToken               token);

		Task<float> AverageAsync<TSource>(
			IQueryable<TSource>             source,
			Expression<Func<TSource,float>> selector,
			CancellationToken               token);

		Task<float?> AverageAsync<TSource>(
			IQueryable<TSource>              source,
			Expression<Func<TSource,float?>> selector,
			CancellationToken                token);

		Task<double> AverageAsync<TSource>(
			IQueryable<TSource>              source,
			Expression<Func<TSource,double>> selector,
			CancellationToken                token);

		Task<double?> AverageAsync<TSource>(
			IQueryable<TSource>               source,
			Expression<Func<TSource,double?>> selector,
			CancellationToken                 token);

		Task<decimal> AverageAsync<TSource>(
			IQueryable<TSource>               source,
			Expression<Func<TSource,decimal>> selector,
			CancellationToken                 token);

		Task<decimal?> AverageAsync<TSource>(
			IQueryable<TSource>                source,
			Expression<Func<TSource,decimal?>> selector,
			CancellationToken                  token);

		#endregion AverageAsync
	}
}
