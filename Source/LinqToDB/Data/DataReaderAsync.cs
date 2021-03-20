using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data
{
	public class DataReaderAsync : IDisposable,
#if NATIVE_ASYNC
		IAsyncDisposable
#else
		Async.IAsyncDisposable
#endif
	{
		public   CommandInfo?      CommandInfo       { get; set; }
		public   DbDataReader?     Reader            { get; set; }
		internal int               ReadNumber        { get; set; }
		internal CancellationToken CancellationToken { get; set; }
		private  DateTime          StartedOn         { get; }      = DateTime.UtcNow;
		private  Stopwatch         Stopwatch         { get; }      = Stopwatch.StartNew();
		internal Action?           OnDispose         { get; set; }
		public void Dispose()
		{
			if (Reader != null)
			{
				Reader.Dispose();

				if (CommandInfo?.DataConnection.TraceSwitchConnection.TraceInfo == true)
				{
					CommandInfo.DataConnection.OnTraceConnection(new TraceInfo(CommandInfo.DataConnection, TraceInfoStep.Completed, TraceOperation.ExecuteReader)
					{
						TraceLevel      = TraceLevel.Info,
						Command         = CommandInfo.DataConnection.GetCurrentCommand(),
						StartTime       = StartedOn,
						ExecutionTime   = Stopwatch.Elapsed,
						RecordsAffected = ReadNumber,
					});
				}
			}

			OnDispose?.Invoke();
		}

#if NETSTANDARD2_1PLUS
		public async ValueTask DisposeAsync()
		{
			if (Reader != null)
			{
				await Reader.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				if (CommandInfo?.DataConnection.TraceSwitchConnection.TraceInfo == true)
				{
					CommandInfo.DataConnection.OnTraceConnection(new TraceInfo(CommandInfo.DataConnection, TraceInfoStep.Completed, TraceOperation.ExecuteReaderAsync)
					{
						TraceLevel      = TraceLevel.Info,
						Command         = CommandInfo.DataConnection.GetCurrentCommand(),
						StartTime       = StartedOn,
						ExecutionTime   = Stopwatch.Elapsed,
						RecordsAffected = ReadNumber,
					});
				}
			}

			OnDispose?.Invoke();
		}
#elif NATIVE_ASYNC
		public ValueTask DisposeAsync()
		{
			Dispose();
			return default;
		}
#else
		public Task DisposeAsync()
		{
			Dispose();
			return TaskEx.CompletedTask;
		}
#endif

		#region Query with object reader

		public Task<List<T>> QueryToListAsync<T>(Func<IDataReader,T> objectReader)
		{
			return QueryToListAsync(objectReader, CancellationToken.None);
		}

		public async Task<List<T>> QueryToListAsync<T>(Func<IDataReader,T> objectReader, CancellationToken cancellationToken)
		{
			var list = new List<T>();
			await QueryForEachAsync(objectReader, list.Add, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			return list;
		}

		public Task<T[]> QueryToArrayAsync<T>(Func<IDataReader,T> objectReader)
		{
			return QueryToArrayAsync(objectReader, CancellationToken.None);
		}

		public async Task<T[]> QueryToArrayAsync<T>(Func<IDataReader,T> objectReader, CancellationToken cancellationToken)
		{
			var list = new List<T>();
			await QueryForEachAsync(objectReader, list.Add, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			return list.ToArray();
		}

		public Task QueryForEachAsync<T>(Func<IDataReader,T> objectReader, Action<T> action)
		{
			return QueryForEachAsync(objectReader, action, CancellationToken.None);
		}

		public async Task QueryForEachAsync<T>(Func<IDataReader,T> objectReader, Action<T> action, CancellationToken cancellationToken)
		{
			while (await Reader!.ReadAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
				action(objectReader(Reader));
		}

		#endregion

		#region Query

		public Task<List<T>> QueryToListAsync<T>()
		{
			return QueryToListAsync<T>(CancellationToken.None);
		}

		public async Task<List<T>> QueryToListAsync<T>(CancellationToken cancellationToken)
		{
			var list = new List<T>();
			await QueryForEachAsync<T>(list.Add, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			return list;
		}

		public Task<T[]> QueryToArrayAsync<T>()
		{
			return QueryToArrayAsync<T>(CancellationToken.None);
		}

		public async Task<T[]> QueryToArrayAsync<T>(CancellationToken cancellationToken)
		{
			var list = new List<T>();
			await QueryForEachAsync<T>(list.Add, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			return list.ToArray();
		}

		public Task QueryForEachAsync<T>(Action<T> action)
		{
			return QueryForEachAsync(action, CancellationToken.None);
		}

		public async Task QueryForEachAsync<T>(Action<T> action, CancellationToken cancellationToken)
		{
			if (ReadNumber != 0)
				if (!await Reader!.NextResultAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
					return;

			ReadNumber++;

			await CommandInfo!.ExecuteQueryAsync(Reader!, CommandInfo.DataConnection.Command.CommandText + "$$$" + ReadNumber, action, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		#endregion

		#region Query with template

		public Task<List<T>> QueryToListAsync<T>(T template)
		{
			return QueryToListAsync(template, CancellationToken.None);
		}

		public async Task<List<T>> QueryToListAsync<T>(T template, CancellationToken cancellationToken)
		{
			var list = new List<T>();
			await QueryForEachAsync(template, list.Add, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			return list;
		}

		public Task<T[]> QueryToArrayAsync<T>(T template)
		{
			return QueryToArrayAsync(template, CancellationToken.None);
		}

		public async Task<T[]> QueryToArrayAsync<T>(T template, CancellationToken cancellationToken)
		{
			var list = new List<T>();
			await QueryForEachAsync(template, list.Add, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			return list.ToArray();
		}

		public Task QueryForEachAsync<T>(T template, Action<T> action)
		{
			return QueryForEachAsync(template, action, CancellationToken.None);
		}

		public Task QueryForEachAsync<T>(T template, Action<T> action, CancellationToken cancellationToken)
		{
			return QueryForEachAsync(action, cancellationToken);
		}

		#endregion

		#region Execute scalar

		public Task<T> ExecuteForEachAsync<T>()
		{
			return ExecuteForEachAsync<T>(CancellationToken.None);
		}

		public async Task<T> ExecuteForEachAsync<T>(CancellationToken cancellationToken)
		{
			if (ReadNumber != 0)
				if (!await Reader!.NextResultAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
					return default(T)!;

			ReadNumber++;

			var sql = CommandInfo!.DataConnection.Command.CommandText + "$$$" + ReadNumber;

			return await CommandInfo.ExecuteScalarAsync<T>(Reader!, sql, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		#endregion
	}
}
