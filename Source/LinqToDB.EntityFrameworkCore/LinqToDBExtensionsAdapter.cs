using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore
{
	/// <summary>
	/// LINQ To DB async extensions adapter to call EF.Core functionality instead of default implementation.
	/// </summary>
	public sealed class LinqToDBExtensionsAdapter : IExtensionsAdapter
	{
		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AsAsyncEnumerable{TSource}(IQueryable{TSource})"/>
		public IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(IQueryable<TSource> source)
			=> EntityFrameworkQueryableExtensions.AsAsyncEnumerable(source);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.ForEachAsync{T}(IQueryable{T}, Action{T}, CancellationToken)"/>
		public Task ForEachAsync<TSource>(
			IQueryable<TSource> source,
			Action<TSource>     action,
			CancellationToken   token)
			=> EntityFrameworkQueryableExtensions.ForEachAsync(source, action, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.ToListAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public Task<List<TSource>> ToListAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token)
			=> EntityFrameworkQueryableExtensions.ToListAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.ToArrayAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public Task<TSource[]> ToArrayAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token)
			=> EntityFrameworkQueryableExtensions.ToArrayAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.ToDictionaryAsync{TSource, TKey}(IQueryable{TSource}, Func{TSource, TKey}, CancellationToken)"/>
		public Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(
			IQueryable<TSource> source,
			Func<TSource, TKey> keySelector,
			CancellationToken   token)
			where TKey : notnull
			=> EntityFrameworkQueryableExtensions.ToDictionaryAsync(source, keySelector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.ToDictionaryAsync{TSource, TKey}(IQueryable{TSource}, Func{TSource, TKey}, IEqualityComparer{TKey}, CancellationToken)"/>
		public Task<Dictionary<TKey,TSource>> ToDictionaryAsync<TSource,TKey>(
			IQueryable<TSource>      source,
			Func<TSource,TKey>       keySelector,
			IEqualityComparer<TKey>  comparer,
			CancellationToken        token)
			where TKey : notnull
			=> EntityFrameworkQueryableExtensions.ToDictionaryAsync(source, keySelector, comparer, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.ToDictionaryAsync{TSource, TKey, TElement}(IQueryable{TSource}, Func{TSource, TKey}, Func{TSource, TElement}, CancellationToken)"/>
		public Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			IQueryable<TSource>      source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			CancellationToken        token)
			where TKey : notnull
			=> EntityFrameworkQueryableExtensions.ToDictionaryAsync(source, keySelector, elementSelector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.ToDictionaryAsync{TSource, TKey, TElement}(IQueryable{TSource}, Func{TSource, TKey}, Func{TSource, TElement}, IEqualityComparer{TKey}, CancellationToken)"/>
		public Task<Dictionary<TKey,TElement>> ToDictionaryAsync<TSource,TKey,TElement>(
			IQueryable<TSource>      source,
			Func<TSource,TKey>       keySelector,
			Func<TSource,TElement>   elementSelector,
			IEqualityComparer<TKey>  comparer,
			CancellationToken        token)
			where TKey : notnull
			=> EntityFrameworkQueryableExtensions.ToDictionaryAsync(source, keySelector, elementSelector, comparer, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.FirstAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public Task<TSource> FirstAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token)
			=> EntityFrameworkQueryableExtensions.FirstAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.FirstAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public Task<TSource> FirstAsync<TSource>(
			IQueryable<TSource>            source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token)
			=> EntityFrameworkQueryableExtensions.FirstAsync(source, predicate, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.FirstOrDefaultAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public Task<TSource?> FirstOrDefaultAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token)
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
			=> EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(source, token);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.FirstOrDefaultAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public Task<TSource?> FirstOrDefaultAsync<TSource>(
			IQueryable<TSource>            source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token)
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
			=> EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(source, predicate, token);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SingleAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public Task<TSource> SingleAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token)
			=> EntityFrameworkQueryableExtensions.SingleAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SingleAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public Task<TSource> SingleAsync<TSource>(
			IQueryable<TSource>            source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token)
			=> EntityFrameworkQueryableExtensions.SingleAsync(source, predicate, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SingleOrDefaultAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public Task<TSource?> SingleOrDefaultAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token)
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
			=> EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(source, token);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SingleOrDefaultAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public Task<TSource?> SingleOrDefaultAsync<TSource>(
			IQueryable<TSource>            source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token)
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
			=> EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(source, predicate, token);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.ContainsAsync{TSource}(IQueryable{TSource}, TSource, CancellationToken)"/>
		public Task<bool> ContainsAsync<TSource>(
			IQueryable<TSource> source,
			TSource             item,
			CancellationToken   token)
			=> EntityFrameworkQueryableExtensions.ContainsAsync(source, item, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AnyAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public Task<bool> AnyAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token)
			=> EntityFrameworkQueryableExtensions.AnyAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AnyAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public Task<bool> AnyAsync<TSource>(
			IQueryable<TSource>            source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token)
			=> EntityFrameworkQueryableExtensions.AnyAsync(source, predicate, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AllAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public Task<bool> AllAsync<TSource>(
			IQueryable<TSource>            source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token)
			=> EntityFrameworkQueryableExtensions.AllAsync(source, predicate, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.CountAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public Task<int> CountAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token)
			=> EntityFrameworkQueryableExtensions.CountAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.CountAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public Task<int> CountAsync<TSource>(
			IQueryable<TSource>            source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token)
			=> EntityFrameworkQueryableExtensions.CountAsync(source, predicate, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.LongCountAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public Task<long> LongCountAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token)
			=> EntityFrameworkQueryableExtensions.LongCountAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.LongCountAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public Task<long> LongCountAsync<TSource>(
			IQueryable<TSource>            source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token)
			=> EntityFrameworkQueryableExtensions.LongCountAsync(source, predicate, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.MinAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public Task<TSource?> MinAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token)
			=> EntityFrameworkQueryableExtensions.MinAsync(source, token)!;

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.MinAsync{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, TResult}}, CancellationToken)"/>
		public Task<TResult?> MinAsync<TSource,TResult>(
			IQueryable<TSource>               source,
			Expression<Func<TSource,TResult>> selector,
			CancellationToken                 token)
			=> EntityFrameworkQueryableExtensions.MinAsync(source, selector, token)!;

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.MaxAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public Task<TSource?> MaxAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken   token)
			=> EntityFrameworkQueryableExtensions.MaxAsync(source, token)!;

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.MaxAsync{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, TResult}}, CancellationToken)"/>
		public Task<TResult?> MaxAsync<TSource,TResult>(
			IQueryable<TSource>               source,
			Expression<Func<TSource,TResult>> selector,
			CancellationToken                 token)
			=> EntityFrameworkQueryableExtensions.MaxAsync(source, selector, token)!;

		#region SumAsync

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{int}, CancellationToken)"/>
		public Task<int> SumAsync(
			IQueryable<int>   source,
			CancellationToken token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{int?}, CancellationToken)"/>
		public Task<int?> SumAsync(
			IQueryable<int?>  source,
			CancellationToken token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{long}, CancellationToken)"/>
		public Task<long> SumAsync(
			IQueryable<long>  source,
			CancellationToken token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{long?}, CancellationToken)"/>
		public Task<long?> SumAsync(
			IQueryable<long?> source,
			CancellationToken token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{float}, CancellationToken)"/>
		public Task<float> SumAsync(
			IQueryable<float> source,
			CancellationToken token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{float?}, CancellationToken)"/>
		public Task<float?> SumAsync(
			IQueryable<float?> source,
			CancellationToken  token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{double}, CancellationToken)"/>
		public Task<double> SumAsync(
			IQueryable<double> source,
			CancellationToken  token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{double?}, CancellationToken)"/>
		public Task<double?> SumAsync(
			IQueryable<double?> source,
			CancellationToken   token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{decimal}, CancellationToken)"/>
		public Task<decimal> SumAsync(
			IQueryable<decimal> source,
			CancellationToken   token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{decimal?}, CancellationToken)"/>
		public Task<decimal?> SumAsync(
			IQueryable<decimal?> source,
			CancellationToken    token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, int}}, CancellationToken)"/>
		public Task<int> SumAsync<TSource>(
			IQueryable<TSource>           source,
			Expression<Func<TSource,int>> selector,
			CancellationToken             token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, int?}}, CancellationToken)"/>
		public Task<int?> SumAsync<TSource>(
			IQueryable<TSource>            source,
			Expression<Func<TSource,int?>> selector,
			CancellationToken              token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, long}}, CancellationToken)"/>
		public Task<long> SumAsync<TSource>(
			IQueryable<TSource>            source,
			Expression<Func<TSource,long>> selector,
			CancellationToken              token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, long?}}, CancellationToken)"/>
		public Task<long?> SumAsync<TSource>(
			IQueryable<TSource>             source,
			Expression<Func<TSource,long?>> selector,
			CancellationToken               token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, float}}, CancellationToken)"/>
		public Task<float> SumAsync<TSource>(
			IQueryable<TSource>             source,
			Expression<Func<TSource,float>> selector,
			CancellationToken               token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, float?}}, CancellationToken)"/>
		public Task<float?> SumAsync<TSource>(
			IQueryable<TSource>              source,
			Expression<Func<TSource,float?>> selector,
			CancellationToken                token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, double}}, CancellationToken)"/>
		public Task<double> SumAsync<TSource>(
			IQueryable<TSource>              source,
			Expression<Func<TSource,double>> selector,
			CancellationToken                token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, double?}}, CancellationToken)"/>
		public Task<double?> SumAsync<TSource>(
			IQueryable<TSource>               source,
			Expression<Func<TSource,double?>> selector,
			CancellationToken                 token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal}}, CancellationToken)"/>
		public Task<decimal> SumAsync<TSource>(
			IQueryable<TSource>               source,
			Expression<Func<TSource,decimal>> selector,
			CancellationToken                 token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal?}}, CancellationToken)"/>
		public Task<decimal?> SumAsync<TSource>(
			IQueryable<TSource>                source,
			Expression<Func<TSource,decimal?>> selector,
			CancellationToken                  token)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, token);

		#endregion SumAsync

		#region AverageAsync

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{int}, CancellationToken)"/>
		public Task<double> AverageAsync(
			IQueryable<int>   source,
			CancellationToken token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{int?}, CancellationToken)"/>
		public Task<double?> AverageAsync(
			IQueryable<int?>  source,
			CancellationToken token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{long}, CancellationToken)"/>
		public Task<double> AverageAsync(
			IQueryable<long>  source,
			CancellationToken token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{long?}, CancellationToken)"/>
		public Task<double?> AverageAsync(
			IQueryable<long?> source,
			CancellationToken token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{float}, CancellationToken)"/>
		public Task<float> AverageAsync(
			IQueryable<float> source,
			CancellationToken token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{float?}, CancellationToken)"/>
		public Task<float?> AverageAsync(
			IQueryable<float?> source,
			CancellationToken  token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{double}, CancellationToken)"/>
		public Task<double> AverageAsync(
			IQueryable<double> source,
			CancellationToken  token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{double?}, CancellationToken)"/>
		public Task<double?> AverageAsync(
			IQueryable<double?> source,
			CancellationToken   token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{decimal}, CancellationToken)"/>
		public Task<decimal> AverageAsync(
			IQueryable<decimal> source,
			CancellationToken   token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{decimal?}, CancellationToken)"/>
		public Task<decimal?> AverageAsync(
			IQueryable<decimal?> source,
			CancellationToken    token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, int}}, CancellationToken)"/>
		public Task<double> AverageAsync<TSource>(
			IQueryable<TSource>           source,
			Expression<Func<TSource,int>> selector,
			CancellationToken             token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, int?}}, CancellationToken)"/>
		public Task<double?> AverageAsync<TSource>(
			IQueryable<TSource>            source,
			Expression<Func<TSource,int?>> selector,
			CancellationToken              token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, long}}, CancellationToken)"/>
		public Task<double> AverageAsync<TSource>(
			IQueryable<TSource>            source,
			Expression<Func<TSource,long>> selector,
			CancellationToken              token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, long?}}, CancellationToken)"/>
		public Task<double?> AverageAsync<TSource>(
			IQueryable<TSource>             source,
			Expression<Func<TSource,long?>> selector,
			CancellationToken               token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, float}}, CancellationToken)"/>
		public Task<float> AverageAsync<TSource>(
			IQueryable<TSource>             source,
			Expression<Func<TSource,float>> selector,
			CancellationToken               token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, float?}}, CancellationToken)"/>
		public Task<float?> AverageAsync<TSource>(
			IQueryable<TSource>              source,
			Expression<Func<TSource,float?>> selector,
			CancellationToken                token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, double}}, CancellationToken)"/>
		public Task<double> AverageAsync<TSource>(
			IQueryable<TSource>              source,
			Expression<Func<TSource,double>> selector,
			CancellationToken                token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, double?}}, CancellationToken)"/>
		public Task<double?> AverageAsync<TSource>(
			IQueryable<TSource>               source,
			Expression<Func<TSource,double?>> selector,
			CancellationToken                 token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal}}, CancellationToken)"/>
		public Task<decimal> AverageAsync<TSource>(
			IQueryable<TSource>               source,
			Expression<Func<TSource,decimal>> selector,
			CancellationToken                 token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal?}}, CancellationToken)"/>
		public Task<decimal?> AverageAsync<TSource>(
			IQueryable<TSource>                source,
			Expression<Func<TSource,decimal?>> selector,
			CancellationToken                  token)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, token);

		#endregion AverageAsync
	}
}
