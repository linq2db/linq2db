using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Async;

using NH = NHibernate.Linq.LinqExtensionMethods;

namespace LinqToDB.NHibernate
{
	/// <summary>
	/// LINQ To DB async extensions adapter that routes async operations over a native NHibernate query to
	/// NHibernate's own async API (<c>NHibernate.Linq.LinqExtensionMethods</c>). Methods NHibernate
	/// does not provide (<c>ToArray</c>/<c>ToDictionary</c>/<c>Contains</c>/<c>ForEach</c>/<c>AsAsyncEnumerable</c>)
	/// fall back to an async <c>ToListAsync</c> followed by a client-side projection.
	/// </summary>
	public class LinqToDBExtensionsAdapter : IExtensionsAdapter
	{
		/// <inheritdoc />
		public IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(IQueryable<TSource> source)
			=> AsAsyncEnumerableImpl(source);

		static async IAsyncEnumerable<TSource> AsAsyncEnumerableImpl<TSource>(
			IQueryable<TSource>                        source,
			[EnumeratorCancellation] CancellationToken token = default)
		{
			foreach (var item in await NH.ToListAsync(source, token).ConfigureAwait(false))
				yield return item;
		}

		/// <inheritdoc />
		public async Task ForEachAsync<TSource>(IQueryable<TSource> source, Action<TSource> action, CancellationToken token)
		{
			foreach (var item in await NH.ToListAsync(source, token).ConfigureAwait(false))
				action(item);
		}

		/// <inheritdoc />
		public Task<List<TSource>> ToListAsync<TSource>(IQueryable<TSource> source, CancellationToken token)
			=> NH.ToListAsync(source, token);

		/// <inheritdoc />
		public async Task<TSource[]> ToArrayAsync<TSource>(IQueryable<TSource> source, CancellationToken token)
			=> (await NH.ToListAsync(source, token).ConfigureAwait(false)).ToArray();

		/// <inheritdoc />
		public async Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(
			IQueryable<TSource> source,
			Func<TSource, TKey> keySelector,
			CancellationToken   token)
			where TKey : notnull
			=> (await NH.ToListAsync(source, token).ConfigureAwait(false)).ToDictionary(keySelector);

		/// <inheritdoc />
		public async Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(
			IQueryable<TSource>     source,
			Func<TSource, TKey>     keySelector,
			IEqualityComparer<TKey> comparer,
			CancellationToken       token)
			where TKey : notnull
			=> (await NH.ToListAsync(source, token).ConfigureAwait(false)).ToDictionary(keySelector, comparer);

		/// <inheritdoc />
		public async Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(
			IQueryable<TSource>    source,
			Func<TSource, TKey>    keySelector,
			Func<TSource, TElement> elementSelector,
			CancellationToken      token)
			where TKey : notnull
			=> (await NH.ToListAsync(source, token).ConfigureAwait(false)).ToDictionary(keySelector, elementSelector);

		/// <inheritdoc />
		public async Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(
			IQueryable<TSource>     source,
			Func<TSource, TKey>     keySelector,
			Func<TSource, TElement> elementSelector,
			IEqualityComparer<TKey> comparer,
			CancellationToken       token)
			where TKey : notnull
			=> (await NH.ToListAsync(source, token).ConfigureAwait(false)).ToDictionary(keySelector, elementSelector, comparer);

		/// <inheritdoc />
		public Task<TSource> FirstAsync<TSource>(IQueryable<TSource> source, CancellationToken token)
			=> NH.FirstAsync(source, token);

		/// <inheritdoc />
		public Task<TSource> FirstAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token)
			=> NH.FirstAsync(source, predicate, token);

		/// <inheritdoc />
		public Task<TSource?> FirstOrDefaultAsync<TSource>(IQueryable<TSource> source, CancellationToken token)
			=> NH.FirstOrDefaultAsync(source, token)!;

		/// <inheritdoc />
		public Task<TSource?> FirstOrDefaultAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token)
			=> NH.FirstOrDefaultAsync(source, predicate, token)!;

		/// <inheritdoc />
		public Task<TSource> SingleAsync<TSource>(IQueryable<TSource> source, CancellationToken token)
			=> NH.SingleAsync(source, token);

		/// <inheritdoc />
		public Task<TSource> SingleAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token)
			=> NH.SingleAsync(source, predicate, token);

		/// <inheritdoc />
		public Task<TSource?> SingleOrDefaultAsync<TSource>(IQueryable<TSource> source, CancellationToken token)
			=> NH.SingleOrDefaultAsync(source, token)!;

		/// <inheritdoc />
		public Task<TSource?> SingleOrDefaultAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token)
			=> NH.SingleOrDefaultAsync(source, predicate, token)!;

		/// <inheritdoc />
		public async Task<bool> ContainsAsync<TSource>(IQueryable<TSource> source, TSource item, CancellationToken token)
			=> (await NH.ToListAsync(source, token).ConfigureAwait(false)).Contains(item);

		/// <inheritdoc />
		public Task<bool> AnyAsync<TSource>(IQueryable<TSource> source, CancellationToken token)
			=> NH.AnyAsync(source, token);

		/// <inheritdoc />
		public Task<bool> AnyAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token)
			=> NH.AnyAsync(source, predicate, token);

		/// <inheritdoc />
		public Task<bool> AllAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token)
			=> NH.AllAsync(source, predicate, token);

		/// <inheritdoc />
		public Task<int> CountAsync<TSource>(IQueryable<TSource> source, CancellationToken token)
			=> NH.CountAsync(source, token);

		/// <inheritdoc />
		public Task<int> CountAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token)
			=> NH.CountAsync(source, predicate, token);

		/// <inheritdoc />
		public Task<long> LongCountAsync<TSource>(IQueryable<TSource> source, CancellationToken token)
			=> NH.LongCountAsync(source, token);

		/// <inheritdoc />
		public Task<long> LongCountAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token)
			=> NH.LongCountAsync(source, predicate, token);

		/// <inheritdoc />
		public Task<TSource?> MinAsync<TSource>(IQueryable<TSource> source, CancellationToken token)
			=> NH.MinAsync(source, token)!;

		/// <inheritdoc />
		public Task<TResult?> MinAsync<TSource, TResult>(IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken token)
			=> NH.MinAsync(source, selector, token)!;

		/// <inheritdoc />
		public Task<TSource?> MaxAsync<TSource>(IQueryable<TSource> source, CancellationToken token)
			=> NH.MaxAsync(source, token)!;

		/// <inheritdoc />
		public Task<TResult?> MaxAsync<TSource, TResult>(IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken token)
			=> NH.MaxAsync(source, selector, token)!;

		#region SumAsync

		/// <inheritdoc />
		public Task<int> SumAsync(IQueryable<int> source, CancellationToken token)
			=> NH.SumAsync(source, token);

		/// <inheritdoc />
		public Task<int?> SumAsync(IQueryable<int?> source, CancellationToken token)
			=> NH.SumAsync(source, token);

		/// <inheritdoc />
		public Task<long> SumAsync(IQueryable<long> source, CancellationToken token)
			=> NH.SumAsync(source, token);

		/// <inheritdoc />
		public Task<long?> SumAsync(IQueryable<long?> source, CancellationToken token)
			=> NH.SumAsync(source, token);

		/// <inheritdoc />
		public Task<float> SumAsync(IQueryable<float> source, CancellationToken token)
			=> NH.SumAsync(source, token);

		/// <inheritdoc />
		public Task<float?> SumAsync(IQueryable<float?> source, CancellationToken token)
			=> NH.SumAsync(source, token);

		/// <inheritdoc />
		public Task<double> SumAsync(IQueryable<double> source, CancellationToken token)
			=> NH.SumAsync(source, token);

		/// <inheritdoc />
		public Task<double?> SumAsync(IQueryable<double?> source, CancellationToken token)
			=> NH.SumAsync(source, token);

		/// <inheritdoc />
		public Task<decimal> SumAsync(IQueryable<decimal> source, CancellationToken token)
			=> NH.SumAsync(source, token);

		/// <inheritdoc />
		public Task<decimal?> SumAsync(IQueryable<decimal?> source, CancellationToken token)
			=> NH.SumAsync(source, token);

		/// <inheritdoc />
		public Task<int> SumAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken token)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc />
		public Task<int?> SumAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken token)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc />
		public Task<long> SumAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken token)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc />
		public Task<long?> SumAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken token)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc />
		public Task<float> SumAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken token)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc />
		public Task<float?> SumAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken token)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc />
		public Task<double> SumAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken token)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc />
		public Task<double?> SumAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken token)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc />
		public Task<decimal> SumAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken token)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc />
		public Task<decimal?> SumAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken token)
			=> NH.SumAsync(source, selector, token);

		#endregion SumAsync

		#region AverageAsync

		/// <inheritdoc />
		public Task<double> AverageAsync(IQueryable<int> source, CancellationToken token)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc />
		public Task<double?> AverageAsync(IQueryable<int?> source, CancellationToken token)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc />
		public Task<double> AverageAsync(IQueryable<long> source, CancellationToken token)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc />
		public Task<double?> AverageAsync(IQueryable<long?> source, CancellationToken token)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc />
		public Task<float> AverageAsync(IQueryable<float> source, CancellationToken token)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc />
		public Task<float?> AverageAsync(IQueryable<float?> source, CancellationToken token)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc />
		public Task<double> AverageAsync(IQueryable<double> source, CancellationToken token)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc />
		public Task<double?> AverageAsync(IQueryable<double?> source, CancellationToken token)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc />
		public Task<decimal> AverageAsync(IQueryable<decimal> source, CancellationToken token)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc />
		public Task<decimal?> AverageAsync(IQueryable<decimal?> source, CancellationToken token)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc />
		public Task<double> AverageAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken token)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc />
		public Task<double?> AverageAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken token)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc />
		public Task<double> AverageAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken token)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc />
		public Task<double?> AverageAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken token)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc />
		public Task<float> AverageAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken token)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc />
		public Task<float?> AverageAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken token)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc />
		public Task<double> AverageAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken token)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc />
		public Task<double?> AverageAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken token)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc />
		public Task<decimal> AverageAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken token)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc />
		public Task<decimal?> AverageAsync<TSource>(IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken token)
			=> NH.AverageAsync(source, selector, token);

		#endregion AverageAsync
	}
}
