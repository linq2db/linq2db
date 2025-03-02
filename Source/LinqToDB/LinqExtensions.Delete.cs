using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Internal.Async;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Reflection;

namespace LinqToDB
{
	public static partial class LinqExtensions
	{
		#region DeleteWithOutput

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
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.0+ (doesn't support multi-table statements; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IEnumerable<TSource> DeleteWithOutput<TSource>(this IQueryable<TSource> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(DeleteWithOutput, source),
				currentSource.Expression);

			return currentSource.CreateQuery<TSource>(expr).AsEnumerable();
		}

		/// <summary>
		/// Deletes records from source query into target table asynchronously and returns deleted records.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query, that returns data for delete operation.</param>
		/// <returns>Async sequence of records returned by output.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.0+ (doesn't support multi-table statements; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IAsyncEnumerable<TSource> DeleteWithOutputAsync<TSource>(
			this IQueryable<TSource> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(DeleteWithOutput, source),
				currentSource.Expression);

			return currentSource.CreateQuery<TSource>(expr).AsAsyncEnumerable();
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
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.0+ (doesn't support multi-table statements; database limitation)</item>
		/// </list>
		/// </remarks>
		// TODO: Remove in v7
		[Obsolete("Use overload with IAsyncEnumerable return type. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public static Task<TSource[]> DeleteWithOutputAsync<TSource>(
			IQueryable<TSource> source,
			CancellationToken token)
		{
			return DeleteWithOutputAsync(source)
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
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
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
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(DeleteWithOutput, source, outputExpression),
				currentSource.Expression,
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsEnumerable();
		}

		/// <summary>
		/// Deletes records from source query into target table asynchronously and returns deleted records.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source query, that returns data for delete operation.</param>
		/// <param name="outputExpression">Output record constructor expression.
		/// Expression supports only record new expression with field initializers.</param>
		/// <returns>Async sequence of records returned by output.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL</item>
		/// <item>SQLite 3.35+</item>
		/// <item>MariaDB 10.0+ (doesn't support multi-table statements; database limitation)</item>
		/// </list>
		/// </remarks>
		public static IAsyncEnumerable<TOutput> DeleteWithOutputAsync<TSource,TOutput>(
			this IQueryable<TSource>           source,
			Expression<Func<TSource, TOutput>> outputExpression)
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(DeleteWithOutput, source, outputExpression),
				currentSource.Expression,
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsAsyncEnumerable();
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
		// TODO: Remove in v7
		[Obsolete("Use overload with IAsyncEnumerable return type. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public static Task<TOutput[]> DeleteWithOutputAsync<TSource, TOutput>(
			IQueryable<TSource> source,
			Expression<Func<TSource, TOutput>> outputExpression,
			CancellationToken token)
		{
			return DeleteWithOutputAsync(source, outputExpression)
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
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(DeleteWithOutputInto, source, outputTable),
				currentSource.Expression,
				((IQueryable<TOutput>)outputTable).Expression);

			return currentSource.Execute<int>(expr);
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
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(DeleteWithOutputInto, source, outputTable),
				currentSource.Expression,
				((IQueryable<TOutput>)outputTable).Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
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
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(DeleteWithOutputInto, source, outputTable, outputExpression),
				currentSource.Expression,
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.Execute<int>(expr);
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
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(DeleteWithOutputInto, source, outputTable, outputExpression),
				currentSource.Expression,
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion

		#region Delete

		/// <summary>
		/// Executes delete operation, using source query as filter for records, that should be deleted.
		/// </summary>
		/// <typeparam name="T">Mapping class for delete operation target table.</typeparam>
		/// <param name="source">Query that returns records to delete.</param>
		/// <returns>Number of deleted records.</returns>
		public static int Delete<T>(this IQueryable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Delete.DeleteQueryable.MakeGenericMethod(typeof(T)),
				currentSource.Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes delete operation asynchronously, using source query as filter for records, that should be deleted.
		/// </summary>
		/// <typeparam name="T">Mapping class for delete operation target table.</typeparam>
		/// <param name="source">Query that returns records to delete.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of deleted records.</returns>
		public static Task<int> DeleteAsync<T>(this IQueryable<T> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Delete.DeleteQueryable.MakeGenericMethod(typeof(T)),
				currentSource.Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes delete operation, using source query as initial filter for records, that should be deleted, and predicate expression as additional filter.
		/// </summary>
		/// <typeparam name="T">Mapping class for delete operation target table.</typeparam>
		/// <param name="source">Query that returns records to delete.</param>
		/// <param name="predicate">Filter expression, to specify what records from source should be deleted.</param>
		/// <returns>Number of deleted records.</returns>
		public static int Delete<T>(
							this IQueryable<T> source,
			[InstantHandle] Expression<Func<T, bool>> predicate)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Delete.DeleteQueryablePredicate.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(predicate));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes delete operation asynchronously, using source query as initial filter for records, that should be deleted, and predicate expression as additional filter.
		/// </summary>
		/// <typeparam name="T">Mapping class for delete operation target table.</typeparam>
		/// <param name="source">Query that returns records to delete.</param>
		/// <param name="predicate">Filter expression, to specify what records from source should be deleted.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of deleted records.</returns>
		public static Task<int> DeleteAsync<T>(
					   this IQueryable<T> source,
			[InstantHandle] Expression<Func<T, bool>> predicate,
			CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Delete.DeleteQueryablePredicate.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(predicate));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion
	}
}
