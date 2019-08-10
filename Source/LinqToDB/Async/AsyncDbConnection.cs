#nullable disable
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

		internal protected AsyncDbConnection(IDbConnection connection)
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

		public virtual Task<IAsyncDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(AsyncFactory.Create(BeginTransaction()));
		}

		public virtual Task<IAsyncDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(AsyncFactory.Create(BeginTransaction(isolationLevel)));
		}

		public virtual Task CloseAsync(CancellationToken cancellationToken = default)
		{
			Close();

			return TaskEx.CompletedTask;
		}

		public virtual Task OpenAsync(CancellationToken cancellationToken = default)
		{
			if (Connection is DbConnection dbConnection)
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

		public virtual IAsyncDbConnection TryClone()
		{
			return Connection is ICloneable cloneable
				? AsyncFactory.Create((IDbConnection)cloneable.Clone())
				: null;
		}
	}
}
