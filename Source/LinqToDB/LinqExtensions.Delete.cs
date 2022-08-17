﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Async;
	using Linq;

	public static partial class LinqExtensions
	{
		/// <summary>
		/// Deletes records from source query and returns deleted records.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query, that returns data for delete operation.</param>
		/// <returns>Enumeration of records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (doesn't support more than one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.0+ (doesn't support multi-table statements; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IEnumerable<TSource> DeleteWithOutput<TSource>(this IQueryable<TSource> source)
		{
			if (source == null) ThrowHelper.ThrowArgumentNullException(nameof(source));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.CreateQuery<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(DeleteWithOutput, source),
						currentSource.Expression))
				.AsEnumerable();
		}

		/// <summary>
		/// Deletes records from source query into target table asynchronously and returns deleted records.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query, that returns data for delete operation.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Array of records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (doesn't support more than one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.0+ (doesn't support multi-table statements; database limitation)</item>
		/// </list>
		/// </remarks>
		public static Task<TSource[]> DeleteWithOutputAsync<TSource>(
			this IQueryable<TSource> source,
			CancellationToken        token = default)
		{
			if (source == null) ThrowHelper.ThrowArgumentNullException(nameof(source));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.CreateQuery<TSource>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(DeleteWithOutput, source),
						currentSource.Expression))
				.ToArrayAsync(token);
		}

		/// <summary>
		/// Deletes records from source query into target table and returns deleted records.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source query, that returns data for delete operation.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Enumeration of records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (doesn't support more than one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.0+ (doesn't support multi-table statements; database limitation)</item>
		/// </list>
		/// </remarks>
		[Pure]
		public static IEnumerable<TOutput> DeleteWithOutput<TSource,TOutput>(
			this IQueryable<TSource>           source,
			Expression<Func<TSource, TOutput>> outputExpression)
		{
			if (source           == null) ThrowHelper.ThrowArgumentNullException(nameof(source));
			if (outputExpression == null) ThrowHelper.ThrowArgumentNullException(nameof(outputExpression));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.CreateQuery<TOutput>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(DeleteWithOutput, source, outputExpression),
						currentSource.Expression,
						Expression.Quote(outputExpression)))
				.AsEnumerable();
		}

		/// <summary>
		/// Deletes records from source query into target table asynchronously and returns deleted records.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source query, that returns data for delete operation.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Array of records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (doesn't support more than one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.0+ (doesn't support multi-table statements; database limitation)</item>
		/// </list>
		/// </remarks>
		public static Task<TOutput[]> DeleteWithOutputAsync<TSource,TOutput>(
			this IQueryable<TSource>           source,
			Expression<Func<TSource, TOutput>> outputExpression,
			CancellationToken                  token = default)
		{
			if (source           == null) ThrowHelper.ThrowArgumentNullException(nameof(source));
			if (outputExpression == null) ThrowHelper.ThrowArgumentNullException(nameof(outputExpression));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.CreateQuery<TOutput>(
					Expression.Call(
						null,
						MethodHelper.GetMethodInfo(DeleteWithOutput, source, outputExpression),
						currentSource.Expression,
						Expression.Quote(outputExpression)))
				.ToArrayAsync(token);
		}

		/// <summary>
		/// Deletes records from source query into target table and outputs deleted records into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source query, that returns data for delete operation.</param>
		/// <param name="outputTable">Output table.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int DeleteWithOutputInto<TSource,TOutput>(
			this IQueryable<TSource> source,
			ITable<TOutput>          outputTable)
			where TOutput : notnull
		{
			if (source      == null) ThrowHelper.ThrowArgumentNullException(nameof(source));
			if (outputTable == null) ThrowHelper.ThrowArgumentNullException(nameof(outputTable));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<int>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(DeleteWithOutputInto, source, outputTable),
					currentSource.Expression,
					((IQueryable<TOutput>)outputTable).Expression));
		}

		/// <summary>
		/// Deletes records from source query into target table asynchronously and outputs deleted records into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source query, that returns data for delete operation.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> DeleteWithOutputIntoAsync<TSource,TOutput>(
			this IQueryable<TSource> source,
			ITable<TOutput>          outputTable,
			CancellationToken        token = default)
			where TOutput : notnull
		{
			if (source      == null) ThrowHelper.ThrowArgumentNullException(nameof(source));
			if (outputTable == null) ThrowHelper.ThrowArgumentNullException(nameof(outputTable));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr =
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(DeleteWithOutputInto, source, outputTable),
					currentSource.Expression,
					((IQueryable<TOutput>)outputTable).Expression);

			if (source is IQueryProviderAsync queryAsync)
				return queryAsync.ExecuteAsync<int>(expr, token);

			return Task.Run(() => source.Provider.Execute<int>(expr), token);
		}

		/// <summary>
		/// Deletes records from source query into target table and outputs deleted records into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source query, that returns data for delete operation.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static int DeleteWithOutputInto<TSource,TOutput>(
			this IQueryable<TSource>          source,
			ITable<TOutput>                   outputTable,
			Expression<Func<TSource,TOutput>> outputExpression)
			where TOutput : notnull
		{
			if (source           == null) ThrowHelper.ThrowArgumentNullException(nameof(source));
			if (outputTable      == null) ThrowHelper.ThrowArgumentNullException(nameof(outputTable));
			if (outputExpression == null) ThrowHelper.ThrowArgumentNullException(nameof(outputExpression));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return source.Provider.Execute<int>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(DeleteWithOutputInto, source, outputTable, outputExpression),
					currentSource.Expression,
					((IQueryable<TOutput>)outputTable).Expression,
					Expression.Quote(outputExpression)));
		}

		/// <summary>
		/// Deletes records from source query into target table asynchronously and outputs deleted records into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source query, that returns data for delete operation.</param>
		/// <param name="outputTable">Output table.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// </remarks>
		public static Task<int> DeleteWithOutputIntoAsync<TSource,TOutput>(
			this IQueryable<TSource>          source,
			ITable<TOutput>                   outputTable,
			Expression<Func<TSource,TOutput>> outputExpression,
			CancellationToken                 token = default)
			where TOutput : notnull
		{
			if (source           == null) ThrowHelper.ThrowArgumentNullException(nameof(source));
			if (outputTable      == null) ThrowHelper.ThrowArgumentNullException(nameof(outputTable));
			if (outputExpression == null) ThrowHelper.ThrowArgumentNullException(nameof(outputExpression));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr =
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(DeleteWithOutputInto, source, outputTable, outputExpression),
					currentSource.Expression,
					((IQueryable<TOutput>)outputTable).Expression,
					Expression.Quote(outputExpression));

			if (currentSource is IQueryProviderAsync queryAsync)
				return queryAsync.ExecuteAsync<int>(expr, token);

			return Task.Run(() => currentSource.Provider.Execute<int>(expr), token);
		}
	}
}
