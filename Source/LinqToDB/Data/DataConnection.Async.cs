﻿using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data
{
	using Async;
	using DbCommandProcessor;
	using LinqToDB.DataProvider;
	using LinqToDB.Interceptors;
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
			await EnsureConnectionAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			// If transaction is open, we dispose it, it will rollback all changes.
			//
			if (TransactionAsync != null) await TransactionAsync.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			// Create new transaction object.
			//
			TransactionAsync = await _connection!.BeginTransactionAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

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
			await EnsureConnectionAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			// If transaction is open, we dispose it, it will rollback all changes.
			//
			if (TransactionAsync != null) await TransactionAsync.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			// Create new transaction object.
			//
			TransactionAsync = await _connection!.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

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
					if (_connectionInterceptors != null)
						await _connectionInterceptors.Apply((interceptor, arg1, arg2, ct) => interceptor.ConnectionOpeningAsync(arg1, arg2, ct), new ConnectionOpeningEventData(this), _connection.Connection, cancellationToken)
							.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

					await _connection.OpenAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

					_closeConnection = true;

					if (_connectionInterceptors != null)
						await _connectionInterceptors.Apply((interceptor, arg1, arg2, ct) => interceptor.ConnectionOpenedAsync(arg1, arg2, ct), new ConnectionOpenedEventData(this), _connection.Connection, cancellationToken)
							.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
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
		/// If underlying provider doesn't support asynchonous commit, it will be performed synchonously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		public virtual async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
		{
			if (TransactionAsync != null)
			{
				await TransactionAsync.CommitAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				if (_closeTransaction)
				{
					await TransactionAsync.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
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
				await TransactionAsync.RollbackAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				if (_closeTransaction)
				{
					await TransactionAsync.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
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
			OnClosing?.Invoke(this, EventArgs.Empty);

			DisposeCommand();

			if (TransactionAsync != null && _closeTransaction)
			{
				await TransactionAsync.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				TransactionAsync = null;
			}

			if (_connection != null)
			{
				if (_disposeConnection)
				{
					await _connection.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
					_connection = null;
				}
				else if (_closeConnection)
					await _connection.CloseAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}

			OnClosed?.Invoke(this, EventArgs.Empty);
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
			await CloseAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		#region ExecuteNonQueryAsync

		protected virtual Task<int> ExecuteNonQueryAsync(DbCommand command, CancellationToken cancellationToken)
		{
			return CurrentCommand!.ExecuteNonQueryExtAsync(cancellationToken);
		}

		internal async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			if (TraceSwitchConnection.Level == TraceLevel.Off)
				using (DataProvider.ExecuteScope(this))
					return await ExecuteNonQueryAsync(CurrentCommand!, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

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
					ret = await ExecuteNonQueryAsync(CurrentCommand!, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

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

		protected virtual Task<object?> ExecuteScalarAsync(DbCommand command, CancellationToken cancellationToken)
		{
			return CurrentCommand!.ExecuteScalarExtAsync(cancellationToken);
		}

		internal async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
		{
			if (TraceSwitchConnection.Level == TraceLevel.Off)
				using (DataProvider.ExecuteScope(this))
					return await ExecuteScalarAsync(CurrentCommand!, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

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
					ret = await ExecuteScalarAsync(CurrentCommand!, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

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
			var dr      = await CurrentCommand!.ExecuteReaderExtAsync(commandBehavior, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
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
						.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

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
						.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

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
