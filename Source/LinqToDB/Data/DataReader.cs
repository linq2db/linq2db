using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace LinqToDB.Data
{
	public class DataReader : IDisposable
	{
		public   CommandInfo CommandInfo { get; set; }
		public   IDataReader Reader      { get; set; }
		internal int         ReadNumber  { get; set; }
		internal DateTime    StartedOn   { get; }      = DateTime.Now;

		public void Dispose()
		{
			if (Reader != null)
			{
				Reader.Dispose();

				if (DataConnection.TraceSwitch.TraceInfo && CommandInfo?.DataConnection?.OnTraceConnection != null)
				{
					CommandInfo.DataConnection.OnTraceConnection(new TraceInfo(TraceInfoStep.Completed)
					{
						TraceLevel = TraceLevel.Info,
						DataConnection = CommandInfo.DataConnection,
						ExecutionTime = DateTime.Now - StartedOn,
						RecordsAffected = ReadNumber,
					});
				}
			}
		}

		#region Query with object reader

		public IEnumerable<T> Query<T>(Func<IDataReader,T> objectReader)
		{
			while (Reader.Read())
				yield return objectReader(Reader);
		}

		#endregion

		#region Query

		public IEnumerable<T> Query<T>()
		{
			if (ReadNumber != 0)
				if (!Reader.NextResult())
					return Enumerable.Empty<T>();

			ReadNumber++;

			return CommandInfo.ExecuteQuery<T>(Reader, CommandInfo.DataConnection.Command.CommandText + "$$$" + ReadNumber);
		}

		#endregion

		#region Query with template

		public IEnumerable<T> Query<T>(T template)
		{
			return Query<T>();
		}

		#endregion

		#region Execute scalar

		public T Execute<T>()
		{
			if (ReadNumber != 0)
				if (!Reader.NextResult())
					return default(T);

			ReadNumber++;

			var sql = CommandInfo.DataConnection.Command.CommandText + "$$$" + ReadNumber;

			return CommandInfo.ExecuteScalar<T>(Reader, sql);
		}

		#endregion
	}
}
