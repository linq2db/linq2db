using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using LinqToDB.Linq;
using JetBrains.Annotations;
using NHibernate;

namespace LinqToDB.NHibernate
{
	public static partial class LinqToDBForNHibernateTools
	{
		#region BulkCopy

		/// <summary>
		/// Performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="session">NHibernate session.</param>
		/// <param name="options">Operation options.</param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>(this ISession session, BulkCopyOptions options, IEnumerable<T> source) where T : class
		{
			ArgumentNullException.ThrowIfNull(session);

			using var dc = session.CreateLinqToDbConnection();
			return dc.BulkCopy(options, source);
		}

		/// <summary>
		/// Performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="session">NHibernate session.</param>
		/// <param name="maxBatchSize">Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server. </param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>(this ISession session, int maxBatchSize, IEnumerable<T> source) where T : class
		{
			ArgumentNullException.ThrowIfNull(session);

			using var dc = session.CreateLinqToDbConnection();
			return dc.BulkCopy(
				new BulkCopyOptions { MaxBatchSize = maxBatchSize },
				source);
		}

		/// <summary>
		/// Performs bulk insert operation.
		/// </summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="session">NHibernate session.</param>
		/// <param name="source">Records to insert.</param>
		/// <returns>Bulk insert operation status.</returns>
		public static BulkCopyRowsCopied BulkCopy<T>(this ISession session, IEnumerable<T> source) where T : class
		{
			ArgumentNullException.ThrowIfNull(session);

			using var dc = session.CreateLinqToDbConnection();
			return dc.BulkCopy(
				new BulkCopyOptions(),
				source);
		}

		#endregion

		#region BulkCopyAsync

		/// <summary>Asynchronously performs bulk insert operation.</summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="session">NHibernate session.</param>
		/// <param name="options">Operation options.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			this ISession session,
			BulkCopyOptions options,
			IEnumerable<T> source,
			CancellationToken cancellationToken = default)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(session);
			ArgumentNullException.ThrowIfNull(source);

			using var dc = session.CreateLinqToDbConnection();
			return await dc.BulkCopyAsync(options, source, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>Asynchronously performs bulk insert operation.</summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="session">NHibernate session.</param>
		/// <param name="maxBatchSize">
		///     Number of rows in each batch. At the end of each batch, the rows in the batch are sent to
		///     the server.
		/// </param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			this ISession session,
			int maxBatchSize,
			IEnumerable<T> source,
			CancellationToken cancellationToken = default)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(session);
			ArgumentNullException.ThrowIfNull(source);

			using var dc = session.CreateLinqToDbConnection();
			return await dc.BulkCopyAsync(maxBatchSize, source, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>Asynchronously performs bulk insert operation.</summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="session">NHibernate session.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			this ISession session,
			IEnumerable<T> source,
			CancellationToken cancellationToken = default)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(session);
			ArgumentNullException.ThrowIfNull(source);

			using var dc = session.CreateLinqToDbConnection();
			return await dc.BulkCopyAsync(source, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>Asynchronously performs bulk insert operation.</summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="session">NHibernate session.</param>
		/// <param name="options">Operation options.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			this ISession session,
			BulkCopyOptions options,
			IAsyncEnumerable<T> source,
			CancellationToken cancellationToken = default)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(session);
			ArgumentNullException.ThrowIfNull(source);

			using var dc = session.CreateLinqToDbConnection();
			return await dc.BulkCopyAsync(options, source, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>Asynchronously performs bulk insert operation.</summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="session">NHibernate session.</param>
		/// <param name="maxBatchSize">Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			this ISession session,
			int maxBatchSize,
			IAsyncEnumerable<T> source,
			CancellationToken cancellationToken = default)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(session);
			ArgumentNullException.ThrowIfNull(source);

			using var dc = session.CreateLinqToDbConnection();
			return await dc.BulkCopyAsync(maxBatchSize, source, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>Asynchronously performs bulk insert operation.</summary>
		/// <typeparam name="T">Mapping type of inserted record.</typeparam>
		/// <param name="session">NHibernate session.</param>
		/// <param name="source">Records to insert.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with bulk insert operation status.</returns>
		public static async Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			this ISession session,
			IAsyncEnumerable<T> source,
			CancellationToken cancellationToken = default)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(session);
			ArgumentNullException.ThrowIfNull(source);

			using var dc = session.CreateLinqToDbConnection();
			return await dc.BulkCopyAsync(source, cancellationToken).ConfigureAwait(false);
		}

		#endregion

		#region Value Insertable

		/// <summary>
		/// Starts LINQ query definition for insert operation.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="session">NHibernate session.</param>
		/// <param name="target">Target table.</param>
		/// <returns>Insertable source query.</returns>
		[LinqTunnel]
		[Pure]
		public static IValueInsertable<T> Into<T>(this ISession session, ITable<T> target) where T : notnull
		{
			ArgumentNullException.ThrowIfNull(session);
			ArgumentNullException.ThrowIfNull(target);

			return session.CreateLinqToDbConnection().Into(target);
		}

		#endregion

		#region GetTable

		/// <summary>
		/// Returns a queryable linq2db source for the mapping class <typeparamref name="T"/> over the NHibernate
		/// session, mapped to its database table or view.
		/// </summary>
		/// <typeparam name="T">Mapping class type.</typeparam>
		/// <returns>Queryable source.</returns>
		public static ITable<T> GetTable<T>(this ISession session)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(session);

			return session.CreateLinqToDbContext().GetTable<T>();
		}

		/// <summary>
		/// Returns a queryable linq2db source for the mapping class <typeparamref name="T"/> over the NHibernate
		/// stateless session (materialised entities are not tracked), mapped to its database table or view.
		/// </summary>
		/// <typeparam name="T">Mapping class type.</typeparam>
		/// <returns>Queryable source.</returns>
		public static ITable<T> GetTable<T>(this IStatelessSession session)
			where T : class
		{
			ArgumentNullException.ThrowIfNull(session);

			return session.CreateLinqToDbContext().GetTable<T>();
		}

		#endregion
	}
}
