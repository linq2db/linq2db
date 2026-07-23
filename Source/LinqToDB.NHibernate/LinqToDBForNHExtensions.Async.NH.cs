using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using NH = NHibernate.Linq.LinqExtensionMethods;

namespace LinqToDB.NHibernate
{
	// ReSharper disable InvokeAsExtensionMethod
	/// <summary>
	/// Provides conflict-less mappings to NHibernate's own async LINQ extensions
	/// (<see cref="NHibernate.Linq.LinqExtensionMethods"/>). These run the query through NHibernate (not linq2db);
	/// the <c>*NH</c> suffix avoids the ambiguity between NHibernate's and linq2db's <c>.ToListAsync()</c> etc.
	/// Operations NHibernate does not provide fall back to an async <c>ToListAsync</c> plus a client-side projection.
	/// </summary>
	public static partial class LinqToDBForNHibernateExtensions
	{
		/// <inheritdoc cref="NH.ToListAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static async Task ForEachAsyncNH<TSource>(this IQueryable<TSource> source, Action<TSource> action, CancellationToken token = default)
		{
			foreach (var item in await NH.ToListAsync(source, token).ConfigureAwait(false))
				action(item);
		}

		/// <inheritdoc cref="NH.ToListAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static Task<List<TSource>> ToListAsyncNH<TSource>(this IQueryable<TSource> source, CancellationToken token = default)
			=> NH.ToListAsync(source, token);

		/// <inheritdoc cref="NH.ToListAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static async Task<TSource[]> ToArrayAsyncNH<TSource>(this IQueryable<TSource> source, CancellationToken token = default)
			=> (await NH.ToListAsync(source, token).ConfigureAwait(false)).ToArray();

		/// <inheritdoc cref="NH.ToListAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static async Task<Dictionary<TKey, TSource>> ToDictionaryAsyncNH<TSource, TKey>(
			this IQueryable<TSource> source, Func<TSource, TKey> keySelector, CancellationToken token = default)
			where TKey : notnull
			=> (await NH.ToListAsync(source, token).ConfigureAwait(false)).ToDictionary(keySelector);

		/// <inheritdoc cref="NH.ToListAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static async Task<Dictionary<TKey, TSource>> ToDictionaryAsyncNH<TSource, TKey>(
			this IQueryable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer, CancellationToken token = default)
			where TKey : notnull
			=> (await NH.ToListAsync(source, token).ConfigureAwait(false)).ToDictionary(keySelector, comparer);

		/// <inheritdoc cref="NH.ToListAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static async Task<Dictionary<TKey, TElement>> ToDictionaryAsyncNH<TSource, TKey, TElement>(
			this IQueryable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, CancellationToken token = default)
			where TKey : notnull
			=> (await NH.ToListAsync(source, token).ConfigureAwait(false)).ToDictionary(keySelector, elementSelector);

		/// <inheritdoc cref="NH.ToListAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static async Task<Dictionary<TKey, TElement>> ToDictionaryAsyncNH<TSource, TKey, TElement>(
			this IQueryable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer, CancellationToken token = default)
			where TKey : notnull
			=> (await NH.ToListAsync(source, token).ConfigureAwait(false)).ToDictionary(keySelector, elementSelector, comparer);

		/// <inheritdoc cref="NH.FirstAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static Task<TSource> FirstAsyncNH<TSource>(this IQueryable<TSource> source, CancellationToken token = default)
			=> NH.FirstAsync(source, token);

		/// <inheritdoc cref="NH.FirstAsync{T}(IQueryable{T}, Expression{Func{T, bool}}, CancellationToken)"/>
		public static Task<TSource> FirstAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token = default)
			=> NH.FirstAsync(source, predicate, token);

		/// <inheritdoc cref="NH.FirstOrDefaultAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static Task<TSource?> FirstOrDefaultAsyncNH<TSource>(this IQueryable<TSource> source, CancellationToken token = default)
			=> NH.FirstOrDefaultAsync(source, token)!;

		/// <inheritdoc cref="NH.FirstOrDefaultAsync{T}(IQueryable{T}, Expression{Func{T, bool}}, CancellationToken)"/>
		public static Task<TSource?> FirstOrDefaultAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token = default)
			=> NH.FirstOrDefaultAsync(source, predicate, token)!;

		/// <inheritdoc cref="NH.SingleAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static Task<TSource> SingleAsyncNH<TSource>(this IQueryable<TSource> source, CancellationToken token = default)
			=> NH.SingleAsync(source, token);

		/// <inheritdoc cref="NH.SingleAsync{T}(IQueryable{T}, Expression{Func{T, bool}}, CancellationToken)"/>
		public static Task<TSource> SingleAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token = default)
			=> NH.SingleAsync(source, predicate, token);

		/// <inheritdoc cref="NH.SingleOrDefaultAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static Task<TSource?> SingleOrDefaultAsyncNH<TSource>(this IQueryable<TSource> source, CancellationToken token = default)
			=> NH.SingleOrDefaultAsync(source, token)!;

		/// <inheritdoc cref="NH.SingleOrDefaultAsync{T}(IQueryable{T}, Expression{Func{T, bool}}, CancellationToken)"/>
		public static Task<TSource?> SingleOrDefaultAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token = default)
			=> NH.SingleOrDefaultAsync(source, predicate, token)!;

		/// <inheritdoc cref="NH.ToListAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static async Task<bool> ContainsAsyncNH<TSource>(this IQueryable<TSource> source, TSource item, CancellationToken token = default)
			=> (await NH.ToListAsync(source, token).ConfigureAwait(false)).Contains(item);

		/// <inheritdoc cref="NH.AnyAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static Task<bool> AnyAsyncNH<TSource>(this IQueryable<TSource> source, CancellationToken token = default)
			=> NH.AnyAsync(source, token);

		/// <inheritdoc cref="NH.AnyAsync{T}(IQueryable{T}, Expression{Func{T, bool}}, CancellationToken)"/>
		public static Task<bool> AnyAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token = default)
			=> NH.AnyAsync(source, predicate, token);

		/// <inheritdoc cref="NH.AllAsync{T}(IQueryable{T}, Expression{Func{T, bool}}, CancellationToken)"/>
		public static Task<bool> AllAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token = default)
			=> NH.AllAsync(source, predicate, token);

		/// <inheritdoc cref="NH.CountAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static Task<int> CountAsyncNH<TSource>(this IQueryable<TSource> source, CancellationToken token = default)
			=> NH.CountAsync(source, token);

		/// <inheritdoc cref="NH.CountAsync{T}(IQueryable{T}, Expression{Func{T, bool}}, CancellationToken)"/>
		public static Task<int> CountAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token = default)
			=> NH.CountAsync(source, predicate, token);

		/// <inheritdoc cref="NH.LongCountAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static Task<long> LongCountAsyncNH<TSource>(this IQueryable<TSource> source, CancellationToken token = default)
			=> NH.LongCountAsync(source, token);

		/// <inheritdoc cref="NH.LongCountAsync{T}(IQueryable{T}, Expression{Func{T, bool}}, CancellationToken)"/>
		public static Task<long> LongCountAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token = default)
			=> NH.LongCountAsync(source, predicate, token);

		/// <inheritdoc cref="NH.MinAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static Task<TSource?> MinAsyncNH<TSource>(this IQueryable<TSource> source, CancellationToken token = default)
			=> NH.MinAsync(source, token)!;

		/// <inheritdoc cref="NH.MinAsync{T, TResult}(IQueryable{T}, Expression{Func{T, TResult}}, CancellationToken)"/>
		public static Task<TResult?> MinAsyncNH<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken token = default)
			=> NH.MinAsync(source, selector, token)!;

		/// <inheritdoc cref="NH.MaxAsync{T}(IQueryable{T}, CancellationToken)"/>
		public static Task<TSource?> MaxAsyncNH<TSource>(this IQueryable<TSource> source, CancellationToken token = default)
			=> NH.MaxAsync(source, token)!;

		/// <inheritdoc cref="NH.MaxAsync{T, TResult}(IQueryable{T}, Expression{Func{T, TResult}}, CancellationToken)"/>
		public static Task<TResult?> MaxAsyncNH<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken token = default)
			=> NH.MaxAsync(source, selector, token)!;

		#region SumAsyncNH

		/// <inheritdoc cref="NH.SumAsync(IQueryable{int}, CancellationToken)"/>
		public static Task<int> SumAsyncNH(this IQueryable<int> source, CancellationToken token = default)
			=> NH.SumAsync(source, token);

		/// <inheritdoc cref="NH.SumAsync(IQueryable{int?}, CancellationToken)"/>
		public static Task<int?> SumAsyncNH(this IQueryable<int?> source, CancellationToken token = default)
			=> NH.SumAsync(source, token);

		/// <inheritdoc cref="NH.SumAsync(IQueryable{long}, CancellationToken)"/>
		public static Task<long> SumAsyncNH(this IQueryable<long> source, CancellationToken token = default)
			=> NH.SumAsync(source, token);

		/// <inheritdoc cref="NH.SumAsync(IQueryable{long?}, CancellationToken)"/>
		public static Task<long?> SumAsyncNH(this IQueryable<long?> source, CancellationToken token = default)
			=> NH.SumAsync(source, token);

		/// <inheritdoc cref="NH.SumAsync(IQueryable{float}, CancellationToken)"/>
		public static Task<float> SumAsyncNH(this IQueryable<float> source, CancellationToken token = default)
			=> NH.SumAsync(source, token);

		/// <inheritdoc cref="NH.SumAsync(IQueryable{float?}, CancellationToken)"/>
		public static Task<float?> SumAsyncNH(this IQueryable<float?> source, CancellationToken token = default)
			=> NH.SumAsync(source, token);

		/// <inheritdoc cref="NH.SumAsync(IQueryable{double}, CancellationToken)"/>
		public static Task<double> SumAsyncNH(this IQueryable<double> source, CancellationToken token = default)
			=> NH.SumAsync(source, token);

		/// <inheritdoc cref="NH.SumAsync(IQueryable{double?}, CancellationToken)"/>
		public static Task<double?> SumAsyncNH(this IQueryable<double?> source, CancellationToken token = default)
			=> NH.SumAsync(source, token);

		/// <inheritdoc cref="NH.SumAsync(IQueryable{decimal}, CancellationToken)"/>
		public static Task<decimal> SumAsyncNH(this IQueryable<decimal> source, CancellationToken token = default)
			=> NH.SumAsync(source, token);

		/// <inheritdoc cref="NH.SumAsync(IQueryable{decimal?}, CancellationToken)"/>
		public static Task<decimal?> SumAsyncNH(this IQueryable<decimal?> source, CancellationToken token = default)
			=> NH.SumAsync(source, token);

		/// <inheritdoc cref="NH.SumAsync{T}(IQueryable{T}, Expression{Func{T, int}}, CancellationToken)"/>
		public static Task<int> SumAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken token = default)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc cref="NH.SumAsync{T}(IQueryable{T}, Expression{Func{T, int?}}, CancellationToken)"/>
		public static Task<int?> SumAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken token = default)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc cref="NH.SumAsync{T}(IQueryable{T}, Expression{Func{T, long}}, CancellationToken)"/>
		public static Task<long> SumAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken token = default)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc cref="NH.SumAsync{T}(IQueryable{T}, Expression{Func{T, long?}}, CancellationToken)"/>
		public static Task<long?> SumAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken token = default)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc cref="NH.SumAsync{T}(IQueryable{T}, Expression{Func{T, float}}, CancellationToken)"/>
		public static Task<float> SumAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken token = default)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc cref="NH.SumAsync{T}(IQueryable{T}, Expression{Func{T, float?}}, CancellationToken)"/>
		public static Task<float?> SumAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken token = default)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc cref="NH.SumAsync{T}(IQueryable{T}, Expression{Func{T, double}}, CancellationToken)"/>
		public static Task<double> SumAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken token = default)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc cref="NH.SumAsync{T}(IQueryable{T}, Expression{Func{T, double?}}, CancellationToken)"/>
		public static Task<double?> SumAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken token = default)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc cref="NH.SumAsync{T}(IQueryable{T}, Expression{Func{T, decimal}}, CancellationToken)"/>
		public static Task<decimal> SumAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken token = default)
			=> NH.SumAsync(source, selector, token);

		/// <inheritdoc cref="NH.SumAsync{T}(IQueryable{T}, Expression{Func{T, decimal?}}, CancellationToken)"/>
		public static Task<decimal?> SumAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken token = default)
			=> NH.SumAsync(source, selector, token);

		#endregion SumAsyncNH

		#region AverageAsyncNH

		/// <inheritdoc cref="NH.AverageAsync(IQueryable{int}, CancellationToken)"/>
		public static Task<double> AverageAsyncNH(this IQueryable<int> source, CancellationToken token = default)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc cref="NH.AverageAsync(IQueryable{int?}, CancellationToken)"/>
		public static Task<double?> AverageAsyncNH(this IQueryable<int?> source, CancellationToken token = default)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc cref="NH.AverageAsync(IQueryable{long}, CancellationToken)"/>
		public static Task<double> AverageAsyncNH(this IQueryable<long> source, CancellationToken token = default)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc cref="NH.AverageAsync(IQueryable{long?}, CancellationToken)"/>
		public static Task<double?> AverageAsyncNH(this IQueryable<long?> source, CancellationToken token = default)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc cref="NH.AverageAsync(IQueryable{float}, CancellationToken)"/>
		public static Task<float> AverageAsyncNH(this IQueryable<float> source, CancellationToken token = default)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc cref="NH.AverageAsync(IQueryable{float?}, CancellationToken)"/>
		public static Task<float?> AverageAsyncNH(this IQueryable<float?> source, CancellationToken token = default)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc cref="NH.AverageAsync(IQueryable{double}, CancellationToken)"/>
		public static Task<double> AverageAsyncNH(this IQueryable<double> source, CancellationToken token = default)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc cref="NH.AverageAsync(IQueryable{double?}, CancellationToken)"/>
		public static Task<double?> AverageAsyncNH(this IQueryable<double?> source, CancellationToken token = default)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc cref="NH.AverageAsync(IQueryable{decimal}, CancellationToken)"/>
		public static Task<decimal> AverageAsyncNH(this IQueryable<decimal> source, CancellationToken token = default)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc cref="NH.AverageAsync(IQueryable{decimal?}, CancellationToken)"/>
		public static Task<decimal?> AverageAsyncNH(this IQueryable<decimal?> source, CancellationToken token = default)
			=> NH.AverageAsync(source, token);

		/// <inheritdoc cref="NH.AverageAsync{T}(IQueryable{T}, Expression{Func{T, int}}, CancellationToken)"/>
		public static Task<double> AverageAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken token = default)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc cref="NH.AverageAsync{T}(IQueryable{T}, Expression{Func{T, int?}}, CancellationToken)"/>
		public static Task<double?> AverageAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken token = default)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc cref="NH.AverageAsync{T}(IQueryable{T}, Expression{Func{T, long}}, CancellationToken)"/>
		public static Task<double> AverageAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken token = default)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc cref="NH.AverageAsync{T}(IQueryable{T}, Expression{Func{T, long?}}, CancellationToken)"/>
		public static Task<double?> AverageAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken token = default)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc cref="NH.AverageAsync{T}(IQueryable{T}, Expression{Func{T, float}}, CancellationToken)"/>
		public static Task<float> AverageAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken token = default)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc cref="NH.AverageAsync{T}(IQueryable{T}, Expression{Func{T, float?}}, CancellationToken)"/>
		public static Task<float?> AverageAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken token = default)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc cref="NH.AverageAsync{T}(IQueryable{T}, Expression{Func{T, double}}, CancellationToken)"/>
		public static Task<double> AverageAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken token = default)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc cref="NH.AverageAsync{T}(IQueryable{T}, Expression{Func{T, double?}}, CancellationToken)"/>
		public static Task<double?> AverageAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken token = default)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc cref="NH.AverageAsync{T}(IQueryable{T}, Expression{Func{T, decimal}}, CancellationToken)"/>
		public static Task<decimal> AverageAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken token = default)
			=> NH.AverageAsync(source, selector, token);

		/// <inheritdoc cref="NH.AverageAsync{T}(IQueryable{T}, Expression{Func{T, decimal?}}, CancellationToken)"/>
		public static Task<decimal?> AverageAsyncNH<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken token = default)
			=> NH.AverageAsync(source, selector, token);

		#endregion AverageAsyncNH
	}
}
