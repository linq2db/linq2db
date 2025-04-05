using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Async;
using LinqToDB.Common;
using LinqToDB.Compatibility.System;
using LinqToDB.Data.RetryPolicy;
using LinqToDB.Interceptors;
using LinqToDB.Tools;

namespace LinqToDB.Data
{
	public partial class DataConnection
	{
#if NET6_0_OR_GREATER
		// TODO: Mark private in v7 and remove warning suppressions from callers
		[Obsolete("This API scheduled for removal in v7"), EditorBrowsable(EditorBrowsableState.Never)]
		public async ValueTask DisposeCommandAsync()
		{
			if (_command != null)
			{
				await DataProvider.DisposeCommandAsync(_command).ConfigureAwait(false);
				_command = null;
			}
		}

		/// <summary>
		/// Sets command timeout to default connection value.
		/// </summary>
		public ValueTask ResetCommandTimeoutAsync()
		{
			_commandTimeout = null;

#pragma warning disable CS0618 // Type or member is obsolete
			return DisposeCommandAsync();
#pragma warning restore CS0618 // Type or member is obsolete
		}
#endif

		/// <summary>
		/// Starts new transaction asynchronously for current connection with default isolation level.
		/// If connection already has transaction, it will throw <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Database transaction object.</returns>
		public virtual async Task<DataConnectionTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
		{
			CheckAndThrowOnDisposed();

			if (!DataProvider.TransactionsSupported)
				return new(this);

			if (TransactionAsync != null) throw new InvalidOperationException("Data connection already has transaction");

			var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

			var dataConnectionTransaction = await TraceActionAsync(
				this,
				TraceOperation.BeginTransaction,
				static _ => "BeginTransactionAsync",
				connection,
				static async (dataConnection, connection, cancellationToken) =>
				{
					// Create new transaction object.
					//
					dataConnection.TransactionAsync = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

					dataConnection._closeTransaction = true;

					// If the active command exists.
					//
					if (dataConnection._command != null)
						dataConnection._command.Transaction = dataConnection.Transaction;

					return new DataConnectionTransaction(dataConnection);
				}, cancellationToken)
				.ConfigureAwait(false);

			return dataConnectionTransaction;
		}

		/// <summary>
		/// Starts new transaction asynchronously for current connection with specified isolation level.
		/// If connection already has transaction, it will throw <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <param name="isolationLevel">Transaction isolation level.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Database transaction object.</returns>
		public virtual async Task<DataConnectionTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
		{
			CheckAndThrowOnDisposed();

			if (!DataProvider.TransactionsSupported)
				return new(this);

			if (TransactionAsync != null) throw new InvalidOperationException("Data connection already has transaction");

			var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

			var dataConnectionTransaction = await TraceActionAsync(
				this,
				TraceOperation.BeginTransaction,
				static ctx => $"BeginTransactionAsync({ctx.isolationLevel})",
				(isolationLevel, connection),
				static async (dataConnection, ctx, cancellationToken) =>
				{
					// Create new transaction object.
					//
					dataConnection.TransactionAsync = await ctx.connection.BeginTransactionAsync(ctx.isolationLevel, cancellationToken).ConfigureAwait(false);

					dataConnection._closeTransaction = true;

					// If the active command exists.
					//
					if (dataConnection._command != null)
						dataConnection._command.Transaction = dataConnection.Transaction;

					return new DataConnectionTransaction(dataConnection);
				}, cancellationToken)
				.ConfigureAwait(false);

			return dataConnectionTransaction;
		}

		/// <summary>
		/// Returns underlying <see cref="DbConnection"/> instance. If connection is not open yet - it will be opened.
		/// </summary>
		public async Task<DbConnection> OpenDbConnectionAsync(CancellationToken cancellationToken)
		{
			return (await OpenConnectionAsync(cancellationToken).ConfigureAwait(false)).Connection;
		}

		/// <summary>
		/// Ensure that database connection opened. If opened connection missing, it will be opened asynchronously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Async operation task.</returns>
		// TODO: Remove in v7
		[Obsolete("This API scheduled for removal in v7"), EditorBrowsable(EditorBrowsableState.Never)]
		public async Task EnsureConnectionAsync(CancellationToken cancellationToken = default)
		{
			await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Ensure that database connection opened. If opened connection missing, it will be opened asynchronously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Async operation task.</returns>
		internal async Task<IAsyncDbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
		{
			CheckAndThrowOnDisposed();

			var connection = GetOrCreateConnection();

			try
			{
				if (connection.State == ConnectionState.Closed)
				{
					var interceptor = ((IInterceptable<IConnectionInterceptor>)this).Interceptor;

					if (interceptor is not null)
					{
						await using (ActivityService.StartAndConfigureAwait(ActivityID.ConnectionInterceptorConnectionOpeningAsync))
							await interceptor.ConnectionOpeningAsync(new(this), connection.Connection, cancellationToken)
								.ConfigureAwait(false);
					}

					var activity = ActivityService.StartAndConfigureAwait(ActivityID.ConnectionOpenAsync);

					if (activity is null)
					{
						await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
					}
					else
					{
						await using (activity)
						{
							await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
							activity.AddQueryInfo(this, connection.Connection, null);
						}
					}

					_closeConnection = true;

					if (interceptor is not null)
					{
						await using (ActivityService.StartAndConfigureAwait(ActivityID.ConnectionInterceptorConnectionOpenedAsync))
							await interceptor.ConnectionOpenedAsync(new (this), connection.Connection, cancellationToken)
								.ConfigureAwait(false);
					}
				}

				return connection;
			}
			catch (Exception ex)
			{
				if (TraceSwitchConnection.TraceError)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.Open, true)
					{
						TraceLevel = TraceLevel.Error,
						StartTime  = DateTime.UtcNow,
						Exception  = ex,
					});
				}

				throw;
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
			CheckAndThrowOnDisposed();

			if (TransactionAsync != null)
			{
				await TraceActionAsync(
					this,
					TraceOperation.CommitTransaction,
					static _ => "CommitTransactionAsync",
					TransactionAsync,
					static async (dataConnection, transaction, cancellationToken) =>
					{
						await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

						if (dataConnection._closeTransaction)
						{
							await transaction.DisposeAsync().ConfigureAwait(false);
							dataConnection.TransactionAsync = null;

							if (dataConnection._command != null)
								dataConnection._command.Transaction = null;
						}

						return (object?)null;
					}, cancellationToken)
					.ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Rollbacks started (if any) transaction, associated with connection.
		/// If underlying provider doesn't support asynchronous commit, it will be performed synchronously.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Asynchronous operation completion task.</returns>
		public virtual async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
		{
			CheckAndThrowOnDisposed();

			if (TransactionAsync != null)
			{
				await TraceActionAsync(
					this,
					TraceOperation.RollbackTransaction,
					static _ => "RollbackTransactionAsync",
					TransactionAsync,
					static async (dataConnection, transaction, cancellationToken) =>
					{
						await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

						if (dataConnection._closeTransaction)
						{
							await transaction.DisposeAsync().ConfigureAwait(false);
							dataConnection.TransactionAsync = null;

							if (dataConnection._command != null)
								dataConnection._command.Transaction = null;
						}

						return (object?)null;
					},
					cancellationToken
				)
					.ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Dispose started (if any) transaction, associated with connection.
		/// If underlying provider doesn't support asynchonous disposal, it will be performed synchonously.
		/// </summary>
		/// <returns>Asynchronous operation completion task.</returns>
		public virtual async Task DisposeTransactionAsync()
		{
			CheckAndThrowOnDisposed();

			if (TransactionAsync != null)
			{
				await TraceActionAsync(
					this,
					TraceOperation.DisposeTransaction,
					static _ => "DisposeTransactionAsync",
					TransactionAsync,
					static async (dataConnection, transaction, cancellationToken) =>
					{
						await transaction.DisposeAsync().ConfigureAwait(false);
						dataConnection.TransactionAsync = null;

						if (dataConnection._command != null)
							dataConnection._command.Transaction = null;

						return (object?)null;
					},
					default
				)
					.ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Closes and dispose associated underlying database transaction/connection asynchronously.
		/// </summary>
		/// <returns>Asynchronous operation completion task.</returns>
		public virtual async Task CloseAsync()
		{
			var interceptor = ((IInterceptable<IDataContextInterceptor>)this).Interceptor;

			if (interceptor != null)
			{
				await using (ActivityService.StartAndConfigureAwait(ActivityID.DataContextInterceptorOnClosingAsync))
					await interceptor.OnClosingAsync(new (this)).ConfigureAwait(false);
			}

#pragma warning disable CS0618 // Type or member is obsolete
#if NET6_0_OR_GREATER
			await DisposeCommandAsync().ConfigureAwait(false);
#else
			DisposeCommand();
#endif
#pragma warning restore CS0618 // Type or member is obsolete

			if (TransactionAsync != null && _closeTransaction)
			{
				await TransactionAsync.DisposeAsync().ConfigureAwait(false);
				TransactionAsync = null;
			}

			if (_connection != null)
			{
				if (_disposeConnection)
				{
					var a = ActivityService.StartAndConfigureAwait(ActivityID.ConnectionDisposeAsync)?.AddQueryInfo(this, _connection.Connection, null);

					if (a is null)
						await _connection.DisposeAsync().ConfigureAwait(false);
					else
						await using (a)
							await _connection.DisposeAsync().ConfigureAwait(false);

					_connection = null;
				}
				else if (_closeConnection)
				{
					var a = ActivityService.StartAndConfigureAwait(ActivityID.ConnectionCloseAsync)?.AddQueryInfo(this, _connection.Connection, null);

					if (a is null)
						await _connection.CloseAsync().ConfigureAwait(false);
					else
						await using (a)
							await _connection.CloseAsync().ConfigureAwait(false);
				}
			}

			if (interceptor != null)
			{
				await using (ActivityService.StartAndConfigureAwait(ActivityID.DataContextInterceptorOnClosedAsync))
					await interceptor.OnClosedAsync(new(this)).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Disposes connection asynchronously.
		/// </summary>
		/// <returns>Asynchronous operation completion task.</returns>
		public async ValueTask DisposeAsync()
		{
			await CloseAsync().ConfigureAwait(false);

			Disposed = true;
		}

		protected static async Task<TResult> TraceActionAsync<TContext, TResult>(
			DataConnection                                                   dataConnection,
			TraceOperation                                                   traceOperation,
			Func<TContext, string?>?                                         commandText,
			TContext                                                         context,
			Func<DataConnection, TContext, CancellationToken, Task<TResult>> action,
			CancellationToken                                                cancellationToken)
		{
			var now       = DateTime.UtcNow;
			Stopwatch? sw = null;
			var sql       = dataConnection.TraceSwitchConnection.TraceInfo ? commandText?.Invoke(context) : null;

			if (dataConnection.TraceSwitchConnection.TraceInfo)
			{
				sw = Stopwatch.StartNew();
				dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.BeforeExecute, traceOperation, true)
				{
					TraceLevel  = TraceLevel.Info,
					CommandText = sql,
					StartTime   = now,
				});
			}

			try
			{
				var actionResult = await action(dataConnection, context, cancellationToken).ConfigureAwait(false);

				if (dataConnection.TraceSwitchConnection.TraceInfo)
				{
					dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.AfterExecute, traceOperation, true)
					{
						TraceLevel    = TraceLevel.Info,
						CommandText   = sql,
						StartTime     = now,
						ExecutionTime = sw?.Elapsed
					});
				}

				return actionResult;
			}
			catch (Exception ex)
			{
				if (dataConnection.TraceSwitchConnection.TraceError)
				{
					dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.Error, traceOperation, true)
					{
						TraceLevel    = TraceLevel.Error,
						CommandText   = dataConnection.TraceSwitchConnection.TraceInfo ? sql : commandText?.Invoke(context),
						StartTime     = now,
						ExecutionTime = sw?.Elapsed,
						Exception     = ex,
					});
				}

				throw;
			}
		}

		#region ExecuteNonQueryAsync

		protected virtual async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			CheckAndThrowOnDisposed();

			try
			{
				await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

				if (((IInterceptable<ICommandInterceptor>)this).Interceptor is { } cInterceptor)
				{
					Option<int> result;

					await using (ActivityService.StartAndConfigureAwait(ActivityID.CommandInterceptorExecuteNonQueryAsync))
					{
						result = await cInterceptor.ExecuteNonQueryAsync(new(this), _command!, Option<int>.None, cancellationToken)
							.ConfigureAwait(false);
					}

					if (result.HasValue)
						return result.Value;
				}

				await using (ActivityService.StartAndConfigureAwait(ActivityID.CommandExecuteNonQueryAsync)?.AddQueryInfo(this, _command!.Connection, _command))
				{
					return await _command!.ExecuteNonQueryAsync(cancellationToken)
						.ConfigureAwait(false);
				}
			}
			catch (Exception ex) when (((IInterceptable<IExceptionInterceptor>)this).Interceptor is { } eInterceptor)
			{
				await using (ActivityService.StartAndConfigureAwait(ActivityID.ExceptionInterceptorProcessException))
					eInterceptor.ProcessException(new(this), ex);
				throw;
			}
		}

		internal async Task<int> ExecuteNonQueryDataAsync(CancellationToken cancellationToken)
		{
			CheckAndThrowOnDisposed();

			if (TraceSwitchConnection.Level == TraceLevel.Off)
				await using ((DataProvider.ExecuteScope(this) ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false))
					return await ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

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
				await using ((DataProvider.ExecuteScope(this) ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false))
					ret = await ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

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

		protected virtual async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
		{
			CheckAndThrowOnDisposed();

			try
			{
				await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

				if (((IInterceptable<ICommandInterceptor>)this).Interceptor is { } cInterceptor)
				{
					Option<object?> result;

					await using (ActivityService.StartAndConfigureAwait(ActivityID.CommandInterceptorExecuteScalarAsync))
					{
						result = await cInterceptor.ExecuteScalarAsync(new(this), _command!, Option<object?>.None, cancellationToken)
							.ConfigureAwait(false);
					}

					if (result.HasValue)
						return result.Value;
				}

				await using (ActivityService.StartAndConfigureAwait(ActivityID.CommandExecuteNonQueryAsync)?.AddQueryInfo(this, _command!.Connection, _command))
				{
					return await _command!.ExecuteScalarAsync(cancellationToken)
						.ConfigureAwait(false);
				}
			}
			catch (Exception ex) when (((IInterceptable<IExceptionInterceptor>)this).Interceptor is { } eInterceptor)
			{
				await using (ActivityService.StartAndConfigureAwait(ActivityID.ExceptionInterceptorProcessException))
					eInterceptor.ProcessException(new(this), ex);
				throw;
			}
		}

		internal async Task<object?> ExecuteScalarDataAsync(CancellationToken cancellationToken)
		{
			CheckAndThrowOnDisposed();

			if (TraceSwitchConnection.Level == TraceLevel.Off)
				await using ((DataProvider.ExecuteScope(this) ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false))
					return await ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

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
				await using ((DataProvider.ExecuteScope(this) ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false))
					ret = await ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

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
			CommandBehavior   commandBehavior,
			CancellationToken cancellationToken)
		{
			CheckAndThrowOnDisposed();

			try
			{
				await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

				DbDataReader reader;

				if (((IInterceptable<ICommandInterceptor>)this).Interceptor is { } cInterceptor)
				{
					Option<DbDataReader> result;

					await using (ActivityService.StartAndConfigureAwait(ActivityID.CommandInterceptorExecuteReader))
					{
						result = await cInterceptor.ExecuteReaderAsync(new(this), _command!, commandBehavior, Option<DbDataReader>.None, cancellationToken)
							.ConfigureAwait(false);
					}

					if (!result.HasValue)
					{
						await using (ActivityService.StartAndConfigureAwait(ActivityID.CommandExecuteReaderAsync)?.AddQueryInfo(this, _command!.Connection, _command))
						{
							reader = await _command!.ExecuteReaderAsync(commandBehavior, cancellationToken)
								.ConfigureAwait(false);
						}
					}
					else
					{
						reader = result.Value;
					}

					await using (ActivityService.StartAndConfigureAwait(ActivityID.CommandInterceptorAfterExecuteReader))
						cInterceptor.AfterExecuteReader(new(this), _command!, commandBehavior, reader);
				}
				else
				{
					await using (ActivityService.StartAndConfigureAwait(ActivityID.CommandExecuteReaderAsync)?.AddQueryInfo(this, _command!.Connection, _command))
					{
						reader = await _command!.ExecuteReaderAsync(commandBehavior, cancellationToken)
							.ConfigureAwait(false);
					}
				}

				var wrapper = new DataReaderWrapper(this, reader, _command!);
				_command = null;

				return wrapper;
			}
			catch (Exception ex) when (((IInterceptable<IExceptionInterceptor>)this).Interceptor is { } eInterceptor)
			{
				await using (ActivityService.StartAndConfigureAwait(ActivityID.ExceptionInterceptorProcessException))
					eInterceptor.ProcessException(new(this), ex);
				throw;
			}
		}

		internal async Task<DataReaderWrapper> ExecuteDataReaderAsync(
			CommandBehavior commandBehavior,
			CancellationToken cancellationToken)
		{
			CheckAndThrowOnDisposed();

			if (TraceSwitchConnection.Level == TraceLevel.Off)
				await using ((DataProvider.ExecuteScope(this) ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false))
					return await ExecuteReaderAsync(commandBehavior, cancellationToken)
						.ConfigureAwait(false);

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

				await using ((DataProvider.ExecuteScope(this) ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false))
					ret = await ExecuteReaderAsync(commandBehavior, cancellationToken)
						.ConfigureAwait(false);

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
