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
		protected IDbConnection Connection { get; private set; }

		internal protected AsyncDbConnection(IDbConnection connection)
		{
			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		public virtual string ConnectionString
		{
			get => Connection.ConnectionString;
			set => Connection.ConnectionString = value;
		}

		public virtual int ConnectionTimeout => Connection.ConnectionTimeout;

		public virtual string Database => Connection.Database;

		public virtual ConnectionState State => Connection.State;

		public IDbConnection Unwrap => Connection is IAsyncDbConnection async ? async.Unwrap : Connection;

		public virtual Task<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(AsyncFactory.Create(BeginTransaction()));
		}

		public virtual Task<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(AsyncFactory.Create(BeginTransaction(isolationLevel)));
		}

		public virtual ValueTask CloseAsync(CancellationToken cancellationToken = default)
		{
			Close();

			return default;
		}

		public virtual Task OpenAsync(CancellationToken cancellationToken = default)
		{
			if (Unwrap is DbConnection dbConnection)
				return dbConnection.OpenAsync();

			return TaskEx.CompletedTask;
		}

		public virtual IDbTransaction BeginTransaction()
		{
			return AsyncFactory.Create(Connection.BeginTransaction());
		}

		public virtual IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			return AsyncFactory.Create(Connection.BeginTransaction(isolationLevel));
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

		public virtual Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default)
		{
			ChangeDatabase(databaseName);

			return TaskEx.CompletedTask;
		}

		public virtual ValueTask DisposeAsync()
		{
			Dispose();

			return default;
		}

		public virtual IAsyncDbConnection TryClone()
		{
			return Unwrap is ICloneable cloneable
				? AsyncFactory.Create((IDbConnection)cloneable.Clone())
				: null;
		}
	}
}
