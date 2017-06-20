using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data
{
#if !NOASYNC

	public partial class DataConnection
	{
		internal async Task InitCommandAsync(CommandType commandType, string sql, DataParameter[] parameters, CancellationToken cancellationToken)
		{
			if (_connection == null)
				_connection = DataProvider.CreateConnection(ConnectionString);

			if (_connection.State == ConnectionState.Closed)
			{
				await ((DbConnection)_connection).OpenAsync(cancellationToken);
				_closeConnection = true;
			}

			InitCommand(commandType, sql, parameters, null);
		}

		private Task<int> ExecuteNonQueryAsyncInternal(CancellationToken cancellationToken)
		{
			return
				RetryPolicy == null
					?                                ((DbCommand)Command).ExecuteNonQueryAsync(cancellationToken)
					: RetryPolicy.ExecuteAsync(ct => ((DbCommand)Command).ExecuteNonQueryAsync(ct), cancellationToken);
		}

		internal async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			if (TraceSwitch.Level == TraceLevel.Off || OnTraceConnection == null)
				return await ExecuteNonQueryAsyncInternal(cancellationToken);

			if (TraceSwitch.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(TraceInfoStep.BeforeExecute)
				{
					TraceLevel     = TraceLevel.Info,
					DataConnection = this,
					Command        = Command,
				});
			}

			try
			{
				var now = DateTime.Now;
				var ret = await ExecuteNonQueryAsyncInternal(cancellationToken);

				if (TraceSwitch.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.AfterExecute)
					{
						TraceLevel      = TraceLevel.Info,
						DataConnection  = this,
						Command         = Command,
						ExecutionTime   = DateTime.Now - now,
						RecordsAffected = ret,
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
						Exception      = ex,
					});
				}

				throw;
			}
		}

		internal Task<DbDataReader> ExecuteReaderAsyncInternal(
			CommandBehavior commandBehavior,
			CancellationToken cancellationToken)
		{
			return
				RetryPolicy == null
					?                                ((DbCommand) Command).ExecuteReaderAsync(commandBehavior, cancellationToken)
					: RetryPolicy.ExecuteAsync(ct => ((DbCommand) Command).ExecuteReaderAsync(commandBehavior, ct), cancellationToken);
		}

		internal async Task<DbDataReader> ExecuteReaderAsync(
			CommandBehavior commandBehavior,
			CancellationToken cancellationToken)
		{
			if (TraceSwitch.Level == TraceLevel.Off || OnTraceConnection == null)
				return await ExecuteReaderAsyncInternal(commandBehavior, cancellationToken);

			if (TraceSwitch.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(TraceInfoStep.BeforeExecute)
				{
					TraceLevel     = TraceLevel.Info,
					DataConnection = this,
					Command        = Command,
				});
			}

			var now = DateTime.Now;

			try
			{
				var ret = await ExecuteReaderAsyncInternal(commandBehavior, cancellationToken);

				if (TraceSwitch.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.AfterExecute)
					{
						TraceLevel     = TraceLevel.Info,
						DataConnection = this,
						Command        = Command,
						ExecutionTime  = DateTime.Now - now,
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
						ExecutionTime  = DateTime.Now - now,
						Exception      = ex,
					});
				}

				throw;
			}
		}
	}

#endif
}
