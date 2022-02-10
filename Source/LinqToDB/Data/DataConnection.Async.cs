using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data
{
	using Async;
	using Common;
	using RetryPolicy;

	public partial class DataConnection
	{
		/// <summary>
		/// Starts new transaction asynchronously for current connection with default isolation level. If connection already has transaction, it will be rolled back.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Database transaction object.</returns>
		public virtual async Task<DataConnectionTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
		{
			await EnsureConnectionAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

			// If transaction is open, we dispose it, it will rollback all changes.
			//
			if (TransactionAsync != null) await TransactionAsync.DisposeAsync().ConfigureAwait(Configuration.ContinueOnCapturedContext);

			// Create new transaction object.
			//
			TransactionAsync = await _connection!.BeginTransactionAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

			_closeTransaction = true;

			// If the active command exists.
			//
			if (_command != null)
				_command.Transaction = Transaction;

			return new DataConnectionTransaction(this);
		}

		/// <summary>
		/// Starts new transaction asynchronously for current connection with specified isolation level. If connection already have transaction, it will be rolled back.
		/// </summary>
		/// <param name="isolationLevel">Transaction isolation level.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Database transaction object.</returns>
		public virtual async Task<DataConnectionTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
		{
			await EnsureConnectionAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

			// If transaction is open, we dispose it, it will rollback all changes.
			//
			if (TransactionAsync != null) await TransactionAsync.DisposeAsync().ConfigureAwait(Configuration.ContinueOnCapturedContext);

			// Create new transaction object.
			//
			TransactionAsync = await _connection!.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

			_closeTransaction = true;

			// If the active command exists.
			//
			if (_command != null)
				_command.Transaction = Transaction;

			return new DataConnectionTransaction(this);
		}

		/// <summary>
		/// Ensure that database connection opened. If opened connection missing, it will be opened asynchronously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Async operation task.</returns>
		public async Task EnsureConnectionAsync(CancellationToken cancellationToken = default)
		{
			CheckAndThrowOnDisposed();

			if (_connection == null)
			{
				DbConnection connection;
				if (_connectionFactory != null)
					connection = _connectionFactory();
				else
					connection = DataProvider.CreateConnection(ConnectionString!);

				_connection = AsyncFactory.Create(connection);

				if (RetryPolicy != null)
					_connection = new RetryingDbConnection(this, _connection, RetryPolicy);
			}

			if (_connection.State == ConnectionState.Closed)
			{
				try
				{
					if (_connectionInterceptor != null)
						await _connectionInterceptor.ConnectionOpeningAsync(new (this), _connection.Connection, cancellationToken)
							.ConfigureAwait(Configuration.ContinueOnCapturedContext);

					await _connection.OpenAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

					_closeConnection = true;

					if (_connectionInterceptor != null)
						await _connectionInterceptor.ConnectionOpenedAsync(new (this), _connection.Connection, cancellationToken)
							.ConfigureAwait(Configuration.ContinueOnCapturedContext);
				}
				catch (Exception ex)
				{
					if (TraceSwitchConnection.TraceError)
					{
						OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.Open, true)
						{
							TraceLevel     = TraceLevel.Error,
							StartTime      = DateTime.UtcNow,
							Exception      = ex,
						});
					}

					throw;
				}
			}
		}

		/// <summary>
		/// Commits started (if any) transaction, associated with connection.
		/// If underlying provider doesn't support asynchronous commit, it will be performed synchronously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		public virtual async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
		{
			if (TransactionAsync != null)
			{
				await TransactionAsync.CommitAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				if (_closeTransaction)
				{
					await TransactionAsync.DisposeAsync().ConfigureAwait(Configuration.ContinueOnCapturedContext);
					TransactionAsync = null;

					if (_command != null)
						_command.Transaction = null;
				}
			}
		}

		/// <summary>
		/// Rollbacks started (if any) transaction, associated with connection.
		/// If underlying provider doesn't support asynchonous commit, it will be performed synchonously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		public virtual async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
		{
			if (TransactionAsync != null)
			{
				await TransactionAsync.RollbackAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				if (_closeTransaction)
				{
					await TransactionAsync.DisposeAsync().ConfigureAwait(Configuration.ContinueOnCapturedContext);
					TransactionAsync = null;

					if (_command != null)
						_command.Transaction = null;
				}
			}
		}

		/// <summary>
		/// Closes and dispose associated underlying database transaction/connection asynchronously.
		/// </summary>
		/// <returns>Asynchronous operation completion task.</returns>
		public virtual async Task CloseAsync()
		{
			if (_dataContextInterceptor != null)
				await _dataContextInterceptor.OnClosingAsync(new (this)).ConfigureAwait(Configuration.ContinueOnCapturedContext);

			DisposeCommand();

			if (TransactionAsync != null && _closeTransaction)
			{
				await TransactionAsync.DisposeAsync().ConfigureAwait(Configuration.ContinueOnCapturedContext);
				TransactionAsync = null;
			}

			if (_connection != null)
			{
				if (_disposeConnection)
				{
					await _connection.DisposeAsync().ConfigureAwait(Configuration.ContinueOnCapturedContext);
					_connection = null;
				}
				else if (_closeConnection)
					await _connection.CloseAsync().ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}

			if (_dataContextInterceptor != null)
				await _dataContextInterceptor.OnClosedAsync(new (this)).ConfigureAwait(Configuration.ContinueOnCapturedContext);
		}

		/// <summary>
		/// Disposes connection asynchronously.
		/// </summary>
		/// <returns>Asynchronous operation completion task.</returns>
#if NATIVE_ASYNC
		public async ValueTask DisposeAsync()
#else
		public async Task DisposeAsync()
#endif
		{
			Disposed = true;
			await CloseAsync().ConfigureAwait(Configuration.ContinueOnCapturedContext);
		}

		#region ExecuteNonQueryAsync

		protected virtual async Task<int> ExecuteNonQueryAsync(DbCommand command, CancellationToken cancellationToken)
		{
			var result = Option<int>.None;

			if (_commandInterceptor != null)
				result = await _commandInterceptor.ExecuteNonQueryAsync(new (this), CurrentCommand!, result, cancellationToken)
					.ConfigureAwait(Configuration.ContinueOnCapturedContext);

			return result.HasValue
				? result.Value
				: await CurrentCommand!.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
		}

		internal async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			if (TraceSwitchConnection.Level == TraceLevel.Off)
				using (DataProvider.ExecuteScope(this))
					return await ExecuteNonQueryAsync(CurrentCommand!, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitchConnection.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(this, TraceInfoStep.BeforeExecute, TraceOperation.ExecuteNonQuery, true)
				{
					TraceLevel     = TraceLevel.Info,
					StartTime      = now,
					Command        = CurrentCommand,
				});
			}

			try
			{
				int ret;
				using (DataProvider.ExecuteScope(this))
					ret = await ExecuteNonQueryAsync(CurrentCommand!, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				if (TraceSwitchConnection.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.AfterExecute, TraceOperation.ExecuteNonQuery, true)
					{
						TraceLevel      = TraceLevel.Info,
						Command         = CurrentCommand,
						StartTime       = now,
						ExecutionTime   = sw.Elapsed,
						RecordsAffected = ret,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitchConnection.TraceError)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.ExecuteNonQuery, true)
					{
						TraceLevel     = TraceLevel.Error,
						Command        = CurrentCommand,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
					});
				}

				throw;
			}
		}

		#endregion

		#region ExecuteScalarAsync

		protected virtual async Task<object?> ExecuteScalarAsync(DbCommand command, CancellationToken cancellationToken)
		{
			var result = Option<object?>.None;

			if (_commandInterceptor != null)
				result = await _commandInterceptor.ExecuteScalarAsync(new (this), CurrentCommand!, result, cancellationToken)
					.ConfigureAwait(Configuration.ContinueOnCapturedContext);

			return result.HasValue
				? result.Value
				: await CurrentCommand!.ExecuteScalarAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
		}

		internal async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
		{
			if (TraceSwitchConnection.Level == TraceLevel.Off)
				using (DataProvider.ExecuteScope(this))
					return await ExecuteScalarAsync(CurrentCommand!, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitchConnection.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(this, TraceInfoStep.BeforeExecute, TraceOperation.ExecuteScalar, true)
				{
					TraceLevel     = TraceLevel.Info,
					Command        = CurrentCommand,
					StartTime      = now,
				});
			}

			try
			{
				object? ret;
				using (DataProvider.ExecuteScope(this))
					ret = await ExecuteScalarAsync(CurrentCommand!, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				if (TraceSwitchConnection.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.AfterExecute, TraceOperation.ExecuteScalar, true)
					{
						TraceLevel      = TraceLevel.Info,
						Command         = CurrentCommand,
						StartTime       = now,
						ExecutionTime   = sw.Elapsed,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitchConnection.TraceError)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.ExecuteScalar, true)
					{
						TraceLevel     = TraceLevel.Error,
						Command        = CurrentCommand,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
					});
				}

				throw;
			}
		}

		#endregion

		#region ExecuteReaderAsync

		protected virtual async Task<DataReaderWrapper> ExecuteReaderAsync(
			DbCommand         command,
			CommandBehavior   commandBehavior,
			CancellationToken cancellationToken)
		{
			var result = Option<DbDataReader>.None;

			if (_commandInterceptor != null)
				result = await _commandInterceptor.ExecuteReaderAsync(new (this), CurrentCommand!, commandBehavior, result, cancellationToken)
					.ConfigureAwait(Configuration.ContinueOnCapturedContext);

			var dr = result.HasValue
				? result.Value
				: await CurrentCommand!.ExecuteReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

			var wrapper = new DataReaderWrapper(this, dr, CurrentCommand);
			_command    = null;

			return wrapper;
		}

		internal async Task<DataReaderWrapper> ExecuteDataReaderAsync(
			CommandBehavior commandBehavior,
			CancellationToken cancellationToken)
		{
			if (TraceSwitchConnection.Level == TraceLevel.Off)
				using (DataProvider.ExecuteScope(this))
					return await ExecuteReaderAsync(CurrentCommand!, commandBehavior, cancellationToken)
						.ConfigureAwait(Configuration.ContinueOnCapturedContext);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitchConnection.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(this, TraceInfoStep.BeforeExecute, TraceOperation.ExecuteReader, true)
				{
					TraceLevel     = TraceLevel.Info,
					Command        = CurrentCommand,
					StartTime      = now,
				});
			}

			try
			{
				DataReaderWrapper ret;

				using (DataProvider.ExecuteScope(this))
					ret = await ExecuteReaderAsync(CurrentCommand!, commandBehavior, cancellationToken)
						.ConfigureAwait(Configuration.ContinueOnCapturedContext);

				if (TraceSwitchConnection.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.AfterExecute, TraceOperation.ExecuteReader, true)
					{
						TraceLevel     = TraceLevel.Info,
						Command        = ret.Command,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitchConnection.TraceError)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.ExecuteReader, true)
					{
						TraceLevel     = TraceLevel.Error,
						Command        = CurrentCommand,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
					});
				}

				throw;
			}
		}

		#endregion
	}
}
