using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LinqToDB.Data
{
	public class DataReader : IDisposable
	{
		internal DataReaderWrapper?  ReaderWrapper { get; private set;  }
		public   DbDataReader?       Reader        => ReaderWrapper?.DataReader;
		public   CommandInfo?        CommandInfo   { get; }
		internal int                 ReadNumber    { get; set; }
		private  DateTime            StartedOn     { get; }      = DateTime.UtcNow;
		private  Stopwatch           Stopwatch     { get; }      = Stopwatch.StartNew();

		public DataReader(CommandInfo commandInfo, DataReaderWrapper dataReader)
		{
			CommandInfo   = commandInfo;
			ReaderWrapper = dataReader;
		}

		public void Dispose()
		{
			if (ReaderWrapper != null)
			{
				if (CommandInfo?.DataConnection.TraceSwitchConnection.TraceInfo == true)
				{
					CommandInfo.DataConnection.OnTraceConnection(new TraceInfo(CommandInfo.DataConnection, TraceInfoStep.Completed, TraceOperation.ExecuteReader, false)
					{
						TraceLevel      = TraceLevel.Info,
						Command         = ReaderWrapper.Command,
						StartTime       = StartedOn,
						ExecutionTime   = Stopwatch.Elapsed,
						RecordsAffected = ReadNumber,
					});
				}

				ReaderWrapper.Dispose();
				ReaderWrapper = null;
			}
		}

		#region Query with object reader

		public IEnumerable<T> Query<T>(Func<DbDataReader, T> objectReader)
		{
			while (Reader!.Read())
				yield return objectReader(Reader);
		}

		#endregion

		#region Query

		public IEnumerable<T> Query<T>()
		{
			if (ReadNumber != 0)
				if (!Reader!.NextResult())
					return Enumerable.Empty<T>();

			ReadNumber++;

			return CommandInfo!.ExecuteQuery<T>(Reader!, FormattableString.Invariant($"{CommandInfo.CommandText}$$${ReadNumber}"));
		}

		#endregion

		#region Query with template

		public IEnumerable<T> Query<T>(T template)
		{
			return Query<T>();
		}

		#endregion

		#region Execute scalar

		[return: MaybeNull]
		public T Execute<T>()
		{
			if (ReadNumber != 0)
				if (!Reader!.NextResult())
					return default(T);

			ReadNumber++;

			var sql = FormattableString.Invariant($"{CommandInfo!.CommandText}$$${ReadNumber}");

			return CommandInfo.ExecuteScalar<T>(Reader!, sql);
		}

		#endregion
	}
}
