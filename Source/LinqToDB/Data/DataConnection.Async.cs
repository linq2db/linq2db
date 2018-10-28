using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data
{
	using RetryPolicy;

	public partial class DataConnection
	{
		public async Task EnsureConnectionAsync(CancellationToken cancellationToken = default)
		{
			if (_connection == null)
			{
				_connection = DataProvider.CreateConnection(ConnectionString);

				if (RetryPolicy != null)
					_connection = new RetryingDbConnection(this, (DbConnection)_connection, RetryPolicy);
			}

			if (_connection.State == ConnectionState.Closed)
			{
				try
				{
					if (_connection is RetryingDbConnection retrying)
						await retrying.OpenAsync(cancellationToken);
					else
						await ((DbConnection)_connection).OpenAsync(cancellationToken);

					_closeConnection = true;
					await OnConnectionAsyncOpened?.Invoke(this, _connection, cancellationToken);
				}
				catch (Exception ex)
				{
					if (TraceSwitch.TraceError)
					{
						OnTraceConnection(new TraceInfo(TraceInfoStep.Error)
						{
							TraceLevel     = TraceLevel.Error,
							DataConnection = this,
							StartTime      = DateTime.UtcNow,
							Exception      = ex,
							IsAsync        = true,
						});
					}

					throw;
				}
			}
		}

		internal async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			if (TraceSwitch.Level == TraceLevel.Off || OnTraceConnection == null)
				return await ((DbCommand)Command).ExecuteNonQueryAsync(cancellationToken);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitch.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(TraceInfoStep.BeforeExecute)
				{
					TraceLevel     = TraceLevel.Info,
					StartTime      = now,
					DataConnection = this,
					Command        = Command,
					IsAsync        = true,
				});
			}

			try
			{
				var ret = await ((DbCommand)Command).ExecuteNonQueryAsync(cancellationToken);

				if (TraceSwitch.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.AfterExecute)
					{
						TraceLevel      = TraceLevel.Info,
						DataConnection  = this,
						Command         = Command,
						StartTime       = now,
						ExecutionTime   = sw.Elapsed,
						RecordsAffected = ret,
						IsAsync         = true,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitch.TraceError)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.Error)
					{
						TraceLevel     = TraceLevel.Error,
						DataConnection = this,
						Command        = Command,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
						IsAsync        = true,
					});
				}

				throw;
			}
		}

		internal async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
		{
			if (TraceSwitch.Level == TraceLevel.Off || OnTraceConnection == null)
				return await ((DbCommand)Command).ExecuteScalarAsync(cancellationToken);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitch.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(TraceInfoStep.BeforeExecute)
				{
					TraceLevel     = TraceLevel.Info,
					DataConnection = this,
					Command        = Command,
					StartTime      = now,
					IsAsync        = true,
				});
			}

			try
			{
				var ret = await ((DbCommand)Command).ExecuteScalarAsync(cancellationToken);

				if (TraceSwitch.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.AfterExecute)
					{
						TraceLevel      = TraceLevel.Info,
						DataConnection  = this,
						Command         = Command,
						StartTime       = now,
						ExecutionTime   = sw.Elapsed,
						IsAsync         = true,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitch.TraceError)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.Error)
					{
						TraceLevel     = TraceLevel.Error,
						DataConnection = this,
						Command        = Command,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
						IsAsync        = true,
					});
				}

				throw;
			}
		}

		internal async Task<DbDataReader> ExecuteReaderAsync(
			CommandBehavior commandBehavior,
			CancellationToken cancellationToken)
		{
			if (TraceSwitch.Level == TraceLevel.Off || OnTraceConnection == null)
				return await ((DbCommand)Command).ExecuteReaderAsync(commandBehavior, cancellationToken);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitch.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(TraceInfoStep.BeforeExecute)
				{
					TraceLevel     = TraceLevel.Info,
					DataConnection = this,
					Command        = Command,
					StartTime      = now,
					IsAsync        = true,
				});
			}

			try
			{
				var ret = await ((DbCommand)Command).ExecuteReaderAsync(commandBehavior, cancellationToken);

				if (TraceSwitch.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.AfterExecute)
					{
						TraceLevel     = TraceLevel.Info,
						DataConnection = this,
						Command        = Command,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						IsAsync        = true,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitch.TraceError)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.Error)
					{
						TraceLevel     = TraceLevel.Error,
						DataConnection = this,
						Command        = Command,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
						IsAsync        = true,
					});
				}

				throw;
			}
		}
	}
}
