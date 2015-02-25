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

			InitCommand(commandType, sql, parameters);
		}

		internal async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			if (TraceSwitch.Level == TraceLevel.Off)
				return await ((DbCommand)Command).ExecuteNonQueryAsync(cancellationToken);

			if (TraceSwitch.TraceInfo)
			{
				OnTrace(new TraceInfo
				{
					BeforeExecute  = true,
					TraceLevel     = TraceLevel.Info,
					DataConnection = this,
					Command        = Command,
				});
			}

			try
			{
				var now = DateTime.Now;
				var ret = await ((DbCommand)Command).ExecuteNonQueryAsync(cancellationToken);

				if (TraceSwitch.TraceInfo)
				{
					OnTrace(new TraceInfo
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
					OnTrace(new TraceInfo
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

		internal async Task<DbDataReader> ExecuteReaderAsync(CommandBehavior commandBehavior, CancellationToken cancellationToken)
		{
			if (TraceSwitch.Level == TraceLevel.Off)
				return await ((DbCommand)Command).ExecuteReaderAsync(commandBehavior, cancellationToken);

			if (TraceSwitch.TraceInfo)
			{
				OnTrace(new TraceInfo
				{
					BeforeExecute  = true,
					TraceLevel     = TraceLevel.Info,
					DataConnection = this,
					Command        = Command,
				});
			}

			try
			{
				var now = DateTime.Now;
				var ret = await ((DbCommand)Command).ExecuteReaderAsync(cancellationToken);

				if (TraceSwitch.TraceInfo)
				{
					OnTrace(new TraceInfo
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
					OnTrace(new TraceInfo
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
	}

#endif
}
