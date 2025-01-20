using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore
{
	// ReSharper disable InvokeAsExtensionMethod
	/// <summary>
	/// Provides conflict-less mappings to <see cref="EntityFrameworkQueryableExtensions"/> extensions.
	/// </summary>
	public static partial class EFForEFExtensions
	{
		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.ForEachAsync{T}(IQueryable{T}, Action{T}, CancellationToken)"/>
		public static Task ForEachAsyncEF<TSource>(
			this IQueryable<TSource> source,
			Action<TSource>          action,
			CancellationToken        cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.ForEachAsync(source, action, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.ToListAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<List<TSource>> ToListAsyncEF<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.ToListAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.ToDictionaryAsync{TSource, TKey}(IQueryable{TSource}, Func{TSource, TKey}, CancellationToken)"/>
		public static Task<TSource[]> ToArrayAsyncEF<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.ToArrayAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.ToDictionaryAsync{TSource, TKey}(IQueryable{TSource}, Func{TSource, TKey}, IEqualityComparer{TKey}, CancellationToken)"/>
		public static Task<Dictionary<TKey, TSource>> ToDictionaryAsyncEF<TSource, TKey>(
			this IQueryable<TSource> source,
			Func<TSource, TKey>      keySelector,
			CancellationToken        cancellationToken = default)
			where TKey: notnull
			=> EntityFrameworkQueryableExtensions.ToDictionaryAsync(source, keySelector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.ToDictionaryAsync{TSource, TKey, TElement}(IQueryable{TSource}, Func{TSource, TKey}, Func{TSource, TElement}, CancellationToken)"/>
		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsyncEF<TSource,TKey,TElement>(
			this IQueryable<TSource>      source,
			Func<TSource,TKey>            keySelector,
			Func<TSource,TElement>        elementSelector,
			CancellationToken             cancellationToken = default)
			where TKey : notnull
			=> EntityFrameworkQueryableExtensions.ToDictionaryAsync(source, keySelector, elementSelector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.ToDictionaryAsync{TSource, TKey, TElement}(IQueryable{TSource}, Func{TSource, TKey}, Func{TSource, TElement}, IEqualityComparer{TKey}, CancellationToken)"/>
		public static Task<Dictionary<TKey,TElement>> ToDictionaryAsyncEF<TSource,TKey,TElement>(
			this IQueryable<TSource>      source,
			Func<TSource,TKey>            keySelector,
			Func<TSource,TElement>        elementSelector,
			IEqualityComparer<TKey>       comparer,
			CancellationToken             cancellationToken = default)
			where TKey : notnull
			=> EntityFrameworkQueryableExtensions.ToDictionaryAsync(source, keySelector, elementSelector, comparer, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.FirstAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<TSource> FirstAsyncEF<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        token = default)
			=> EntityFrameworkQueryableExtensions.FirstAsync(source, token);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.FirstAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public static Task<TSource> FirstAsyncEF<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.FirstAsync(source, predicate, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.FirstOrDefaultAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<TSource?> FirstOrDefaultAsyncEF<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken = default)
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
			=> EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(source, cancellationToken);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.FirstOrDefaultAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public static Task<TSource?> FirstOrDefaultAsyncEF<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken = default)
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
			=> EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(source, predicate, cancellationToken);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SingleAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<TSource> SingleAsyncEF<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SingleAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SingleAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public static Task<TSource> SingleAsyncEF<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SingleAsync(source, predicate, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SingleOrDefaultAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<TSource?> SingleOrDefaultAsyncEF<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken = default)
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
			=> EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(source, cancellationToken);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SingleOrDefaultAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public static Task<TSource?> SingleOrDefaultAsyncEF<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken = default)
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
			=> EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(source, predicate, cancellationToken);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.ContainsAsync{TSource}(IQueryable{TSource}, TSource, CancellationToken)"/>
		public static Task<bool> ContainsAsyncEF<TSource>(
			this IQueryable<TSource> source,
			TSource                  item,
			CancellationToken        cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.ContainsAsync(source, item, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AnyAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<bool> AnyAsyncEF<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AnyAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AnyAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public static Task<bool> AnyAsyncEF<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AnyAsync(source, predicate, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AllAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public static Task<bool> AllAsyncEF<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AllAsync(source, predicate, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.CountAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<int> CountAsyncEF<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.CountAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.CountAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public static Task<int> CountAsyncEF<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.CountAsync(source, predicate, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.LongCountAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<long> LongCountAsyncEF<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.LongCountAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.LongCountAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}}, CancellationToken)"/>
		public static Task<long> LongCountAsyncEF<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,bool>> predicate,
			CancellationToken              cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.LongCountAsync(source, predicate, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.MinAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<TSource> MinAsyncEF<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.MinAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.MinAsync{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, TResult}}, CancellationToken)"/>
		public static Task<TResult> MinAsyncEF<TSource,TResult>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,TResult>> selector,
			CancellationToken                 cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.MinAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.MaxAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
		public static Task<TSource> MaxAsyncEF<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.MaxAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.MaxAsync{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, TResult}}, CancellationToken)"/>
		public static Task<TResult> MaxAsyncEF<TSource,TResult>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,TResult>> selector,
			CancellationToken                 cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.MaxAsync(source, selector, cancellationToken);

		#region SumAsyncEF

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{int}, CancellationToken)"/>
		public static Task<int> SumAsyncEF(
			this IQueryable<int> source,
			CancellationToken    cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{int?}, CancellationToken)"/>
		public static Task<int?> SumAsyncEF(
			this IQueryable<int?> source,
			CancellationToken     cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{long}, CancellationToken)"/>
		public static Task<long> SumAsyncEF(
			this IQueryable<long> source,
			CancellationToken     cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{long?}, CancellationToken)"/>
		public static Task<long?> SumAsyncEF(
			this IQueryable<long?> source,
			CancellationToken      cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{float}, CancellationToken)"/>
		public static Task<float> SumAsyncEF(
			this IQueryable<float> source,
			CancellationToken      cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{float?}, CancellationToken)"/>
		public static Task<float?> SumAsyncEF(
			this IQueryable<float?> source,
			CancellationToken       cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{double}, CancellationToken)"/>
		public static Task<double> SumAsyncEF(
			this IQueryable<double> source,
			CancellationToken       cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{double?}, CancellationToken)"/>
		public static Task<double?> SumAsyncEF(
			this IQueryable<double?> source,
			CancellationToken        cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{decimal}, CancellationToken)"/>
		public static Task<decimal> SumAsyncEF(
			this IQueryable<decimal> source,
			CancellationToken        cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync(IQueryable{decimal?}, CancellationToken)"/>
		public static Task<decimal?> SumAsyncEF(
			this IQueryable<decimal?> source,
			CancellationToken         cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, int}}, CancellationToken)"/>
		public static Task<int> SumAsyncEF<TSource>(
			this IQueryable<TSource>      source,
			Expression<Func<TSource,int>> selector,
			CancellationToken             cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, int?}}, CancellationToken)"/>
		public static Task<int?> SumAsyncEF<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,int?>> selector,
			CancellationToken              cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, long}}, CancellationToken)"/>
		public static Task<long> SumAsyncEF<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,long>> selector,
			CancellationToken              cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, long?}}, CancellationToken)"/>
		public static Task<long?> SumAsyncEF<TSource>(
			this IQueryable<TSource>        source,
			Expression<Func<TSource,long?>> selector,
			CancellationToken               cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, float}}, CancellationToken)"/>
		public static Task<float> SumAsyncEF<TSource>(
			this IQueryable<TSource>        source,
			Expression<Func<TSource,float>> selector,
			CancellationToken               cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, float?}}, CancellationToken)"/>
		public static Task<float?> SumAsyncEF<TSource>(
			this IQueryable<TSource>         source,
			Expression<Func<TSource,float?>> selector,
			CancellationToken                cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, double}}, CancellationToken)"/>
		public static Task<double> SumAsyncEF<TSource>(
			this IQueryable<TSource>         source,
			Expression<Func<TSource,double>> selector,
			CancellationToken                cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, double?}}, CancellationToken)"/>
		public static Task<double?> SumAsyncEF<TSource>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,double?>> selector,
			CancellationToken                 cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal}}, CancellationToken)"/>
		public static Task<decimal> SumAsyncEF<TSource>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,decimal>> selector,
			CancellationToken                 cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.SumAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal?}}, CancellationToken)"/>
		public static Task<decimal?> SumAsyncEF<TSource>(
			this IQueryable<TSource>           source,
			Expression<Func<TSource,decimal?>> selector,
			CancellationToken                  cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

		#endregion SumAsyncEF

		#region AverageAsyncEF

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{int}, CancellationToken)"/>
		public static Task<double> AverageAsyncEF(
			this IQueryable<int> source,
			CancellationToken    cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{int?}, CancellationToken)"/>
		public static Task<double?> AverageAsyncEF(
			this IQueryable<int?> source,
			CancellationToken     cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{long}, CancellationToken)"/>
		public static Task<double> AverageAsyncEF(
			this IQueryable<long> source,
			CancellationToken     cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{long?}, CancellationToken)"/>
		public static Task<double?> AverageAsyncEF(
			this IQueryable<long?> source,
			CancellationToken      cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{float}, CancellationToken)"/>
		public static Task<float> AverageAsyncEF(
			this IQueryable<float> source,
			CancellationToken      cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{float?}, CancellationToken)"/>
		public static Task<float?> AverageAsyncEF(
			this IQueryable<float?> source,
			CancellationToken       cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{double}, CancellationToken)"/>
		public static Task<double> AverageAsyncEF(
			this IQueryable<double> source,
			CancellationToken       cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{double?}, CancellationToken)"/>
		public static Task<double?> AverageAsyncEF(
			this IQueryable<double?> source,
			CancellationToken        cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{decimal}, CancellationToken)"/>
		public static Task<decimal> AverageAsyncEF(
			this IQueryable<decimal> source,
			CancellationToken        cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync(IQueryable{decimal?}, CancellationToken)"/>
		public static Task<decimal?> AverageAsyncEF(
			this IQueryable<decimal?> source,
			CancellationToken         cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, int}}, CancellationToken)"/>
		public static Task<double> AverageAsyncEF<TSource>(
			this IQueryable<TSource>      source,
			Expression<Func<TSource,int>> selector,
			CancellationToken             cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, int?}}, CancellationToken)"/>
		public static Task<double?> AverageAsyncEF<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,int?>> selector,
			CancellationToken              cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, long}}, CancellationToken)"/>
		public static Task<double> AverageAsyncEF<TSource>(
			this IQueryable<TSource>       source,
			Expression<Func<TSource,long>> selector,
			CancellationToken              cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, long?}}, CancellationToken)"/>
		public static Task<double?> AverageAsyncEF<TSource>(
			this IQueryable<TSource>        source,
			Expression<Func<TSource,long?>> selector,
			CancellationToken               cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, float}}, CancellationToken)"/>
		public static Task<float> AverageAsyncEF<TSource>(
			this IQueryable<TSource>        source,
			Expression<Func<TSource,float>> selector,
			CancellationToken               cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, float?}}, CancellationToken)"/>
		public static Task<float?> AverageAsyncEF<TSource>(
			this IQueryable<TSource>         source,
			Expression<Func<TSource,float?>> selector,
			CancellationToken                cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, double}}, CancellationToken)"/>
		public static Task<double> AverageAsyncEF<TSource>(
			this IQueryable<TSource>         source,
			Expression<Func<TSource,double>> selector,
			CancellationToken                cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, double?}}, CancellationToken)"/>
		public static Task<double?> AverageAsyncEF<TSource>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,double?>> selector,
			CancellationToken                 cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal}}, CancellationToken)"/>
		public static Task<decimal> AverageAsyncEF<TSource>(
			this IQueryable<TSource>          source,
			Expression<Func<TSource,decimal>> selector,
			CancellationToken                 cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

		/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AverageAsync{TSource}(IQueryable{TSource}, Expression{Func{TSource, decimal?}}, CancellationToken)"/>
		public static Task<decimal?> AverageAsyncEF<TSource>(
			this IQueryable<TSource>           source,
			Expression<Func<TSource,decimal?>> selector,
			CancellationToken                  cancellationToken = default)
			=> EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

		#endregion AverageAsyncEF
	}
}
