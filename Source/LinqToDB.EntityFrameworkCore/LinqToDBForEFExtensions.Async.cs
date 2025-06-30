using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Async;

namespace LinqToDB.EntityFrameworkCore
{
	// ReSharper disable InvokeAsExtensionMethod
	/// <summary>
	/// Provides conflict-less mappings to <see cref="AsyncExtensions"/>.
	/// </summary>
	[PublicAPI]
	public static partial class LinqToDBForEFExtensions
	{
		/// <inheritdoc cref="AsyncExtensions.ForEachAsync{TSource}(IQueryable{TSource}, Action{TSource}, CancellationToken)"/>
		public static Task ForEachAsyncLinqToDB<TSource>(
			this IQueryable<TSource> source,
			Action<TSource>          action,
			CancellationToken        token = default)
			=> AsyncExtensions.ForEachAsync(source.ToLinqToDB(), action, token);

		/// <inheritdoc cref="AsyncExtensions.ToListAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<List<TSource>> ToListAsyncLinqToDB<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        token = default)
			=> AsyncExtensions.ToListAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.ToArrayAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<TSource[]> ToArrayAsyncLinqToDB<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        token = default)
			=> AsyncExtensions.ToArrayAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.ToDictionaryAsync{TSource, TKey}(IQueryable{TSource}, Func{TSource, TKey}, CancellationToken)"/>
		public static Task<Dictionary<TKey, TSource>> ToDictionaryAsyncLinqToDB<TSource, TKey>(
			this IQueryable<TSource> source,
			Func<TSource, TKey>      keySelector,
			CancellationToken        token = default)
			where TKey: notnull
			=> AsyncExtensions.ToDictionaryAsync(source.ToLinqToDB(), keySelector, token);

		/// <inheritdoc cref="AsyncExtensions.ToDictionaryAsync{TSource, TKey, TElement}(IQueryable{TSource}, Func{TSource, TKey}, Func{TSource, TElement}, CancellationToken)"/>
		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsyncLinqToDB<TSource,TKey,TElement>(
			this IQueryable<TSource>      source,
			Func<TSource,TKey>            keySelector,
			Func<TSource,TElement>        elementSelector,
			CancellationToken             token = default)
			where TKey : notnull
			=> AsyncExtensions.ToDictionaryAsync(source.ToLinqToDB(), keySelector, elementSelector, token);

		/// <inheritdoc cref="AsyncExtensions.ToDictionaryAsync{TSource, TKey, TElement}(IQueryable{TSource}, Func{TSource, TKey}, Func{TSource, TElement}, IEqualityComparer{TKey}, CancellationToken)"/>
		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsyncLinqToDB<TSource,TKey,TElement>(
			this IQueryable<TSource>      source,
			Func<TSource,TKey>            keySelector,
			Func<TSource,TElement>        elementSelector,
			IEqualityComparer<TKey>       comparer,
			CancellationToken             token = default)
			where TKey : notnull
			=> AsyncExtensions.ToDictionaryAsync(source.ToLinqToDB(), keySelector, elementSelector, comparer, token);

		/// <inheritdoc cref="AsyncExtensions.FirstAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<TSource> FirstAsyncLinqToDB<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        token = default)
			=> AsyncExtensions.FirstAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.FirstAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public static Task<TSource> FirstAsyncLinqToDB<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token = default)
			=> AsyncExtensions.FirstAsync(source.ToLinqToDB(), predicate, token);

		/// <inheritdoc cref="AsyncExtensions.FirstOrDefaultAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<TSource?> FirstOrDefaultAsyncLinqToDB<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        token = default)
			=> AsyncExtensions.FirstOrDefaultAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.FirstOrDefaultAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public static Task<TSource?> FirstOrDefaultAsyncLinqToDB<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token = default)
			=> AsyncExtensions.FirstOrDefaultAsync(source.ToLinqToDB(), predicate, token);

		/// <inheritdoc cref="AsyncExtensions.SingleAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<TSource> SingleAsyncLinqToDB<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        token = default)
			=> AsyncExtensions.SingleAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.SingleAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public static Task<TSource> SingleAsyncLinqToDB<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token = default)
			=> AsyncExtensions.SingleAsync(source.ToLinqToDB(), predicate, token);

		/// <inheritdoc cref="AsyncExtensions.SingleOrDefaultAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<TSource?> SingleOrDefaultAsyncLinqToDB<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        token = default)
			=> AsyncExtensions.SingleOrDefaultAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.SingleOrDefaultAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public static Task<TSource?> SingleOrDefaultAsyncLinqToDB<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token = default)
			=> AsyncExtensions.SingleOrDefaultAsync(source.ToLinqToDB(), predicate, token);

		/// <inheritdoc cref="AsyncExtensions.ContainsAsync{TSource}(IQueryable{TSource}, TSource, CancellationToken)"/>
		public static Task<bool> ContainsAsyncLinqToDB<TSource>(
			this IQueryable<TSource> source,
			TSource                  item,
			CancellationToken        token = default)
			=> AsyncExtensions.ContainsAsync(source.ToLinqToDB(), item, token);

		/// <inheritdoc cref="AsyncExtensions.AnyAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<bool> AnyAsyncLinqToDB<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        token = default)
			=> AsyncExtensions.AnyAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.AnyAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public static Task<bool> AnyAsyncLinqToDB<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token = default)
			=> AsyncExtensions.AnyAsync(source.ToLinqToDB(), predicate, token);

		/// <inheritdoc cref="AsyncExtensions.AllAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public static Task<bool> AllAsyncLinqToDB<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token = default)
			=> AsyncExtensions.AllAsync(source.ToLinqToDB(), predicate, token);

		/// <inheritdoc cref="AsyncExtensions.CountAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<int> CountAsyncLinqToDB<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        token = default)
			=> AsyncExtensions.CountAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.CountAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public static Task<int> CountAsyncLinqToDB<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token = default)
			=> AsyncExtensions.CountAsync(source.ToLinqToDB(), predicate, token);

		/// <inheritdoc cref="AsyncExtensions.LongCountAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<long> LongCountAsyncLinqToDB<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        token = default)
			=> AsyncExtensions.LongCountAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.LongCountAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public static Task<long> LongCountAsyncLinqToDB<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              token = default)
			=> AsyncExtensions.LongCountAsync(source.ToLinqToDB(), predicate, token);

		/// <inheritdoc cref="AsyncExtensions.MinAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<TSource?> MinAsyncLinqToDB<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        token = default)
			=> AsyncExtensions.MinAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.MinAsync{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, TResult}}, CancellationToken)"/>
		public static Task<TResult?> MinAsyncLinqToDB<TSource,TResult>(
			this IQueryable<TSource>         source,
			Expression<Func<TSource,TResult>> selector,
			CancellationToken                 token = default)
			=> AsyncExtensions.MinAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.MaxAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<TSource?> MaxAsyncLinqToDB<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        token = default)
			=> AsyncExtensions.MaxAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.MaxAsync{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, TResult}}, CancellationToken)"/>
		public static Task<TResult?> MaxAsyncLinqToDB<TSource,TResult>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,TResult>> selector,
			CancellationToken                 token = default)
			=> AsyncExtensions.MaxAsync(source.ToLinqToDB(), selector, token);

		#region SumAsyncLinqToDB

		/// <inheritdoc cref="AsyncExtensions.SumAsync(IQueryable{int}, CancellationToken)"/>
		public static Task<int> SumAsyncLinqToDB(
			this IQueryable<int>   source,
			CancellationToken token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync(IQueryable{int?}, CancellationToken)"/>
		public static Task<int?> SumAsyncLinqToDB(
			this IQueryable<int?> source,
			CancellationToken     token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync(IQueryable{long}, CancellationToken)"/>
		public static Task<long> SumAsyncLinqToDB(
			this IQueryable<long> source,
			CancellationToken     token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync(IQueryable{long?}, CancellationToken)"/>
		public static Task<long?> SumAsyncLinqToDB(
			this IQueryable<long?> source,
			CancellationToken      token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync(IQueryable{float}, CancellationToken)"/>
		public static Task<float> SumAsyncLinqToDB(
			this IQueryable<float> source,
			CancellationToken      token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync(IQueryable{float?}, CancellationToken)"/>
		public static Task<float?> SumAsyncLinqToDB(
			this IQueryable<float?> source,
			CancellationToken       token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync(IQueryable{double}, CancellationToken)"/>
		public static Task<double> SumAsyncLinqToDB(
			this IQueryable<double> source,
			CancellationToken       token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync(IQueryable{double?}, CancellationToken)"/>
		public static Task<double?> SumAsyncLinqToDB(
			this IQueryable<double?> source,
			CancellationToken        token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync(IQueryable{decimal}, CancellationToken)"/>
		public static Task<decimal> SumAsyncLinqToDB(
			this IQueryable<decimal> source,
			CancellationToken        token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync(IQueryable{decimal?}, CancellationToken)"/>
		public static Task<decimal?> SumAsyncLinqToDB(
			this IQueryable<decimal?> source,
			CancellationToken         token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, int}}, CancellationToken)"/>
		public static Task<int> SumAsyncLinqToDB<TSource>(
			this IQueryable<TSource>      source,
			Expression<Func<TSource,int>> selector,
			CancellationToken             token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, int?}}, CancellationToken)"/>
		public static Task<int?> SumAsyncLinqToDB<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,int?>> selector,
			CancellationToken              token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, long}}, CancellationToken)"/>
		public static Task<long> SumAsyncLinqToDB<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,long>> selector,
			CancellationToken              token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, long?}}, CancellationToken)"/>
		public static Task<long?> SumAsyncLinqToDB<TSource>(
			this IQueryable<TSource>        source,
			Expression<Func<TSource,long?>> selector,
			CancellationToken               token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, float}}, CancellationToken)"/>
		public static Task<float> SumAsyncLinqToDB<TSource>(
			this IQueryable<TSource>             source,
			Expression<Func<TSource,float>> selector,
			CancellationToken               token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, float?}}, CancellationToken)"/>
		public static Task<float?> SumAsyncLinqToDB<TSource>(
			this IQueryable<TSource>         source,
			Expression<Func<TSource,float?>> selector,
			CancellationToken                token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, double}}, CancellationToken)"/>
		public static Task<double> SumAsyncLinqToDB<TSource>(
			this IQueryable<TSource>         source,
			Expression<Func<TSource,double>> selector,
			CancellationToken                token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, double?}}, CancellationToken)"/>
		public static Task<double?> SumAsyncLinqToDB<TSource>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,double?>> selector,
			CancellationToken                 token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal}}, CancellationToken)"/>
		public static Task<decimal> SumAsyncLinqToDB<TSource>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,decimal>> selector,
			CancellationToken                 token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal?}}, CancellationToken)"/>
		public static Task<decimal?> SumAsyncLinqToDB<TSource>(
			this IQueryable<TSource>           source,
			Expression<Func<TSource,decimal?>> selector,
			CancellationToken                  token = default)
			=> AsyncExtensions.SumAsync(source.ToLinqToDB(), selector, token);

		#endregion SumAsyncLinqToDB

		#region AverageAsyncLinqToDB

		/// <inheritdoc cref="AsyncExtensions.AverageAsync(IQueryable{int}, CancellationToken)"/>
		public static Task<double> AverageAsyncLinqToDB(
			this IQueryable<int> source,
			CancellationToken    token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync(IQueryable{int?}, CancellationToken)"/>
		public static Task<double?> AverageAsyncLinqToDB(
			this IQueryable<int?> source,
			CancellationToken     token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync(IQueryable{long}, CancellationToken)"/>
		public static Task<double> AverageAsyncLinqToDB(
			this IQueryable<long> source,
			CancellationToken     token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync(IQueryable{long?}, CancellationToken)"/>
		public static Task<double?> AverageAsyncLinqToDB(
			this IQueryable<long?> source,
			CancellationToken      token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync(IQueryable{float}, CancellationToken)"/>
		public static Task<float> AverageAsyncLinqToDB(
			this IQueryable<float> source,
			CancellationToken      token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync(IQueryable{float?}, CancellationToken)"/>
		public static Task<float?> AverageAsyncLinqToDB(
			this IQueryable<float?> source,
			CancellationToken       token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync(IQueryable{double}, CancellationToken)"/>
		public static Task<double> AverageAsyncLinqToDB(
			this IQueryable<double> source,
			CancellationToken       token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync(IQueryable{double?}, CancellationToken)"/>
		public static Task<double?> AverageAsyncLinqToDB(
			this IQueryable<double?> source,
			CancellationToken        token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync(IQueryable{decimal}, CancellationToken)"/>
		public static Task<decimal> AverageAsyncLinqToDB(
			this IQueryable<decimal> source,
			CancellationToken        token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync(IQueryable{decimal?}, CancellationToken)"/>
		public static Task<decimal?> AverageAsyncLinqToDB(
			this IQueryable<decimal?> source,
			CancellationToken         token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, int}}, CancellationToken)"/>
		public static Task<double> AverageAsyncLinqToDB<TSource>(
			this IQueryable<TSource>      source,
			Expression<Func<TSource,int>> selector,
			CancellationToken             token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, int?}}, CancellationToken)"/>
		public static Task<double?> AverageAsyncLinqToDB<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,int?>> selector,
			CancellationToken              token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, long}}, CancellationToken)"/>
		public static Task<double> AverageAsyncLinqToDB<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,long>> selector,
			CancellationToken              token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, long?}}, CancellationToken)"/>
		public static Task<double?> AverageAsyncLinqToDB<TSource>(
			this IQueryable<TSource>        source,
			Expression<Func<TSource,long?>> selector,
			CancellationToken               token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, float}}, CancellationToken)"/>
		public static Task<float> AverageAsyncLinqToDB<TSource>(
			this IQueryable<TSource>        source,
			Expression<Func<TSource,float>> selector,
			CancellationToken               token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, float?}}, CancellationToken)"/>
		public static Task<float?> AverageAsyncLinqToDB<TSource>(
			this IQueryable<TSource>         source,
			Expression<Func<TSource,float?>> selector,
			CancellationToken                token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, double}}, CancellationToken)"/>
		public static Task<double> AverageAsyncLinqToDB<TSource>(
			this IQueryable<TSource>         source,
			Expression<Func<TSource,double>> selector,
			CancellationToken                token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, double?}}, CancellationToken)"/>
		public static Task<double?> AverageAsyncLinqToDB<TSource>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,double?>> selector,
			CancellationToken                 token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal}}, CancellationToken)"/>
		public static Task<decimal> AverageAsyncLinqToDB<TSource>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,decimal>> selector,
			CancellationToken                 token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), selector, token);

		/// <inheritdoc cref="AsyncExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal?}}, CancellationToken)"/>
		public static Task<decimal?> AverageAsyncLinqToDB<TSource>(
			this IQueryable<TSource>           source,
			Expression<Func<TSource,decimal?>> selector,
			CancellationToken                  token = default)
			=> AsyncExtensions.AverageAsync(source.ToLinqToDB(), selector, token);

		#endregion AverageAsyncLinqToDB
	}
}
