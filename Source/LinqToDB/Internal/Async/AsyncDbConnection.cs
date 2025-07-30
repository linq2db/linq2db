using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Metrics;

using AsyncDisposableWrapper = LinqToDB.Metrics.ActivityService.AsyncDisposableWrapper;

namespace LinqToDB.Internal.Async
{
	/// <summary>
	/// Implements <see cref="IAsyncDbConnection"/> wrapper over <see cref="DbConnection"/> instance with
	/// synchronous implementation of asynchronous methods.
	/// Providers with async operations support could override its methods with asynchronous implementations.
	/// </summary>
	public class AsyncDbConnection : IAsyncDbConnection
	{
		protected internal AsyncDbConnection(DbConnection connection)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		internal DataConnection? DataConnection { get; set; }

		public virtual DbConnection Connection { get; }

		[AllowNull]
		public virtual string ConnectionString
		{
			get => Connection.ConnectionString;
			set => Connection.ConnectionString = value;
		}

		public virtual ConnectionState State => Connection.State;

		public virtual DbCommand CreateCommand() => Connection.CreateCommand();

		public virtual void Open()
		{
			using var a = ActivityService.Start(ActivityID.ConnectionOpen);
			Connection.Open();
			a?.AddQueryInfo(DataConnection, Connection, null);
		}

		public virtual Task OpenAsync(CancellationToken cancellationToken)
		{
			var a = ActivityService.StartAndConfigureAwait(ActivityID.ConnectionOpenAsync);

			if (a is null)
				return Connection.OpenAsync(cancellationToken);

			return CallAwaitUsing(a, DataConnection, Connection, cancellationToken);

			static async Task CallAwaitUsing(AsyncDisposableWrapper activity, DataConnection? dataConnection, DbConnection connection, CancellationToken token)
			{
				await using (activity)
				{
					await connection.OpenAsync(token).ConfigureAwait(false);
					activity.AddQueryInfo(dataConnection, connection, null);
				}
			}
		}

		public virtual void Close()
		{
			using var _ = ActivityService.Start(ActivityID.ConnectionClose)?.AddQueryInfo(DataConnection, Connection, null);
			Connection.Close();
		}

		public virtual Task CloseAsync()
		{
#if ADO_ASYNC
			var a = ActivityService.StartAndConfigureAwait(ActivityID.ConnectionCloseAsync)?.AddQueryInfo(DataConnection, Connection, null);

			if (a is null)
				return Connection.CloseAsync();

			return CallAwaitUsing(a, Connection);

			static async Task CallAwaitUsing(AsyncDisposableWrapper activity, DbConnection connection)
			{
				await using (activity)
					await connection.CloseAsync().ConfigureAwait(false);
			}
#else
			using var _ = ActivityService.Start(ActivityID.ConnectionCloseAsync)?.AddQueryInfo(DataConnection, Connection, null);

			Close();
			return Task.CompletedTask;
#endif
		}

		public virtual IAsyncDbTransaction BeginTransaction()
		{
			using var a = ActivityService.Start(ActivityID.ConnectionBeginTransaction)?.AddQueryInfo(DataConnection, Connection, null);
			return AsyncFactory.CreateAndSetDataContext(DataConnection, Connection.BeginTransaction());
		}

		public virtual IAsyncDbTransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			using var a = ActivityService.Start(ActivityID.ConnectionBeginTransaction)?.AddQueryInfo(DataConnection, Connection, null);
			return AsyncFactory.CreateAndSetDataContext(DataConnection, Connection.BeginTransaction(isolationLevel));
		}

#if !ADO_ASYNC
		public virtual ValueTask<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
		{
			return new(BeginTransaction());
		}

		public virtual ValueTask<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
		{
			return new(BeginTransaction(isolationLevel));
		}
#else
		public virtual async ValueTask<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ConnectionBeginTransactionAsync)?.AddQueryInfo(DataConnection, Connection, null))
			{
				var transaction = await Connection.BeginTransactionAsync(cancellationToken)
					.ConfigureAwait(false);

				return AsyncFactory.CreateAndSetDataContext(DataConnection, transaction);
			}
		}

		public virtual async ValueTask<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ConnectionBeginTransactionAsync)?.AddQueryInfo(DataConnection, Connection, null))
			{
				var transaction = await Connection.BeginTransactionAsync(isolationLevel, cancellationToken)
					.ConfigureAwait(false);

				return AsyncFactory.CreateAndSetDataContext(DataConnection, transaction);
			}
		}

#endif

		#region IDisposable

		public virtual void Dispose()
		{
			using var _ = ActivityService.Start(ActivityID.ConnectionDispose)?.AddQueryInfo(DataConnection, Connection, null);
#pragma warning disable RS0030 // Do not use banned APIs
			Connection.Dispose();
#pragma warning restore RS0030 // Do not use banned APIs
		}

		#endregion

		#region IAsyncDisposable
		public virtual ValueTask DisposeAsync()
		{
			var a = ActivityService.StartAndConfigureAwait(ActivityID.ConnectionDisposeAsync)?.AddQueryInfo(DataConnection, Connection, null);

			if (a is null)
				return Connection.DisposeAsync();

			return CallAwaitUsing(a, Connection);

			static async ValueTask CallAwaitUsing(AsyncDisposableWrapper activity, DbConnection connection)
			{
				await using (activity)
					await connection.DisposeAsync().ConfigureAwait(false);
			}
		}
		#endregion
	}
}
