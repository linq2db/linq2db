using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using AsyncDisposableWrapper = LinqToDB.Tools.ActivityService.AsyncDisposableWrapper;

namespace LinqToDB.Async
{
	using Tools;

	/// <summary>
	/// Implements <see cref="IAsyncDbConnection"/> wrapper over <see cref="DbConnection"/> instance with
	/// synchronous implementation of asynchronous methods.
	/// Providers with async operations support could override its methods with asynchronous implementations.
	/// </summary>
	[PublicAPI]
	public class AsyncDbConnection : IAsyncDbConnection
	{
		protected internal AsyncDbConnection(DbConnection connection)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		public virtual DbConnection Connection { get; }

		public virtual DbConnection? TryClone()
		{
			try
			{
				return Connection is ICloneable cloneable
					? (DbConnection)cloneable.Clone()
					: null;
			}
			catch
			{
				// this try-catch added to handle errors like this one from MiniProfiler's ProfiledDbConnection
				// "NotSupportedException : Underlying SqliteConnection is not cloneable"
				// because wrapper implements ICloneable but wrapped connection doesn't
				// exception-less solution will be always return null for wrapped connections which is also meh
				return null;
			}
		}

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
			using var _ = ActivityService.Start(ActivityID.ConnectionOpen);
			Connection.Open();
		}

		public virtual Task OpenAsync(CancellationToken cancellationToken)
		{
			var a = ActivityService.StartAndConfigureAwait(ActivityID.ConnectionOpenAsync);

			if (a is null)
				return Connection.OpenAsync(cancellationToken);

			return CallAwaitUsing(a, Connection, cancellationToken);

			static async Task CallAwaitUsing(AsyncDisposableWrapper activity, DbConnection connection, CancellationToken token)
			{
				await using (activity)
					await connection.OpenAsync(token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
		}

		public virtual void Close()
		{
			using var _ = ActivityService.Start(ActivityID.ConnectionClose);
			Connection.Close();
		}

		public virtual Task CloseAsync()
		{
#if NET6_0_OR_GREATER
			var a = ActivityService.StartAndConfigureAwait(ActivityID.ConnectionCloseAsync);

			if (a is null)
				return Connection.CloseAsync();

			return CallAwaitUsing(a, Connection);

			static async Task CallAwaitUsing(AsyncDisposableWrapper activity, DbConnection connection)
			{
				await using (activity)
					await connection.CloseAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
#else
			using var _ = ActivityService.Start(ActivityID.ConnectionCloseAsync);

			Close();
			return Task.CompletedTask;
#endif
		}

		public virtual IAsyncDbTransaction BeginTransaction()
		{
			using var a = ActivityService.Start(ActivityID.ConnectionBeginTransaction);
			return AsyncFactory.Create(Connection.BeginTransaction());
		}

		public virtual IAsyncDbTransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			using var a = ActivityService.Start(ActivityID.ConnectionBeginTransaction);
			return AsyncFactory.Create(Connection.BeginTransaction(isolationLevel));
		}

#if !NET6_0_OR_GREATER

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
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ConnectionBeginTransactionAsync))
			{
				var transaction = await Connection.BeginTransactionAsync(cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				return AsyncFactory.Create(transaction);
			}
		}

		public virtual async ValueTask<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ConnectionBeginTransactionAsync))
			{
				var transaction = await Connection.BeginTransactionAsync(isolationLevel, cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				return AsyncFactory.Create(transaction);
			}
		}

#endif

		#region IDisposable

		public virtual void Dispose()
		{
			using var _ = ActivityService.Start(ActivityID.ConnectionDispose);
			Connection.Dispose();
		}

		#endregion

		#region IAsyncDisposable
		public virtual ValueTask DisposeAsync()
		{
			if (Connection is IAsyncDisposable asyncDisposable)
			{
				var a = ActivityService.StartAndConfigureAwait(ActivityID.ConnectionDisposeAsync);

				if (a is null)
					return asyncDisposable.DisposeAsync();

				return CallAwaitUsing(a, asyncDisposable);

				static async ValueTask CallAwaitUsing(AsyncDisposableWrapper activity, IAsyncDisposable disposable)
				{
					await using (activity)
						await disposable.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				}
			}

			using var _ = ActivityService.Start(ActivityID.ConnectionDisposeAsync);

			Connection.Dispose();
			return default;
		}
		#endregion
	}
}
