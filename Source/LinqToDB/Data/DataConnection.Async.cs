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
				}
				catch (Exception ex)
				{
					if (TraceSwitch.TraceError)
					{
						OnTraceConnection(new TraceInfo(TraceInfoStep.Error)
						{
							TraceLevel     = TraceLevel.Error,
							DataConnection = this,
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

			if (TraceSwitch.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(TraceInfoStep.BeforeExecute)
				{
					TraceLevel     = TraceLevel.Info,
					DataConnection = this,
					Command        = Command,
					IsAsync        = true,
				});
			}

			try
			{
				var now = DateTime.Now;
				var ret = await ((DbCommand)Command).ExecuteNonQueryAsync(cancellationToken);

				if (TraceSwitch.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.AfterExecute)
					{
						TraceLevel      = TraceLevel.Info,
						DataConnection  = this,
						Command         = Command,
						ExecutionTime   = DateTime.Now - now,
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

			if (TraceSwitch.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(TraceInfoStep.BeforeExecute)
				{
					TraceLevel     = TraceLevel.Info,
					DataConnection = this,
					Command        = Command,
					IsAsync        = true,
				});
			}

			try
			{
				var now = DateTime.Now;
				var ret = await ((DbCommand)Command).ExecuteScalarAsync(cancellationToken);

				if (TraceSwitch.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(TraceInfoStep.AfterExecute)
					{
						TraceLevel      = TraceLevel.Info,
						DataConnection  = this,
						Command         = Command,
						ExecutionTime   = DateTime.Now - now,
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

			if (TraceSwitch.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(TraceInfoStep.BeforeExecute)
				{
					TraceLevel     = TraceLevel.Info,
					DataConnection = this,
					Command        = Command,
					IsAsync        = true,
				});
			}

			var now = DateTime.Now;

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
						ExecutionTime  = DateTime.Now - now,
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
						ExecutionTime  = DateTime.Now - now,
						Exception      = ex,
						IsAsync        = true,
					});
				}

				throw;
			}
		}
	}
}
