using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Async
{
	/// <summary>
	/// Implements <see cref="IAsyncDbConnection"/> wrapper over <see cref="IDbConnection"/> instance with
	/// synchronous implementation of asynchronous methods.
	/// Providers with async operations support could override its methods with asynchronous implementations.
	/// </summary>
	[PublicAPI]
	public class AsyncDbConnection : IAsyncDbConnection
	{
		private readonly IDbConnection _connection;

		protected internal AsyncDbConnection(IDbConnection connection)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		public virtual string ConnectionString
		{
			get => Connection.ConnectionString;
			set => Connection.ConnectionString = value;
		}

		public virtual int ConnectionTimeout => Connection.ConnectionTimeout;

		public virtual string Database => Connection.Database;

		public virtual ConnectionState State => Connection.State;

		public virtual IDbConnection Connection => _connection;

#if !NATIVE_ASYNC
		public virtual Task<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
			=> Task.FromResult(AsyncFactory.Create(BeginTransaction()));
#elif !NETSTANDARD2_1PLUS
		public virtual ValueTask<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
			=> new ValueTask<IAsyncDbTransaction>(AsyncFactory.Create(BeginTransaction()));
#else
		public virtual async ValueTask<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
		{
			if (Connection is DbConnection dbConnection)
			{
				var transaction = await dbConnection.BeginTransactionAsync(cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				return new AsyncDbTransaction(transaction);
			}
			return AsyncFactory.Create(BeginTransaction());
		}
#endif

#if NETSTANDARD2_1PLUS
		public virtual async ValueTask<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
		{
			if (Connection is DbConnection dbConnection)
			{
				var transaction = await dbConnection.BeginTransactionAsync(isolationLevel, cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				return new AsyncDbTransaction(transaction);
			}
			return AsyncFactory.Create(BeginTransaction(isolationLevel));
		}
#elif NATIVE_ASYNC
		public virtual ValueTask<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
			=> new ValueTask<IAsyncDbTransaction>(AsyncFactory.Create(BeginTransaction(isolationLevel)));
#else
		public virtual Task<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
			=> Task.FromResult(AsyncFactory.Create(BeginTransaction(isolationLevel)));
#endif

		public virtual Task CloseAsync()
		{
#if NETSTANDARD2_1PLUS
			if (Connection is DbConnection dbConnection)
				return dbConnection.CloseAsync();
#endif
			Close();

			return TaskEx.CompletedTask;
		}

		public virtual Task OpenAsync(CancellationToken cancellationToken = default)
		{
			if (Connection is DbConnection dbConnection)
				return dbConnection.OpenAsync(cancellationToken);

			return TaskEx.CompletedTask;
		}

		public virtual IDbTransaction BeginTransaction()
		{
			return AsyncFactory.Create(Connection.BeginTransaction());
		}

		public virtual IDbTransaction BeginTransaction(IsolationLevel il)
		{
			return AsyncFactory.Create(Connection.BeginTransaction(il));
		}

		public virtual void Close()
		{
			Connection.Close();
		}

		public virtual void ChangeDatabase(string databaseName)
		{
			Connection.ChangeDatabase(databaseName);
		}

		public virtual IDbCommand CreateCommand()
		{
			return Connection.CreateCommand();
		}

		public virtual void Open()
		{
			Connection.Open();
		}

		public virtual void Dispose()
		{
			Connection.Dispose();
		}

#if !NATIVE_ASYNC
		public virtual Task DisposeAsync()
		{
			Dispose();

			return TaskEx.CompletedTask;
		}
#else
		public virtual ValueTask DisposeAsync()
		{
			if (Connection is IAsyncDisposable asyncDisposable)
				return asyncDisposable.DisposeAsync();

			Dispose();
			return default;
		}
#endif

		public virtual IAsyncDbConnection? TryClone()
		{
			try
			{
				return Connection is ICloneable cloneable
					? AsyncFactory.Create((IDbConnection)cloneable.Clone())
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
	}
}
