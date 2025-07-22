using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data
{
	public class DataReaderAsync : IDisposable, IAsyncDisposable
	{
		internal DataReaderWrapper? ReaderWrapper     { get; private set; }
		public   DbDataReader?      Reader            => ReaderWrapper?.DataReader;
		public   CommandInfo?       CommandInfo       { get; }
		internal int                ReadNumber        { get; set; }
		internal CancellationToken  CancellationToken { get; set; }
		private  DateTime           StartedOn         { get; }      = DateTime.UtcNow;
		private  Stopwatch          Stopwatch         { get; }      = Stopwatch.StartNew();

		public DataReaderAsync(CommandInfo commandInfo, DataReaderWrapper dataReader)
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

		public async ValueTask DisposeAsync()
		{
			if (ReaderWrapper != null)
			{
				if (CommandInfo?.DataConnection.TraceSwitchConnection.TraceInfo == true)
				{
					CommandInfo.DataConnection.OnTraceConnection(new TraceInfo(CommandInfo.DataConnection, TraceInfoStep.Completed, TraceOperation.ExecuteReader, true)
					{
						TraceLevel      = TraceLevel.Info,
						Command         = ReaderWrapper.Command,
						StartTime       = StartedOn,
						ExecutionTime   = Stopwatch.Elapsed,
						RecordsAffected = ReadNumber,
					});
				}

				await ReaderWrapper.DisposeAsync().ConfigureAwait(false);
				ReaderWrapper = null;
			}
		}

		#region Query with object reader

		public Task<List<T>> QueryToListAsync<T>(Func<DbDataReader, T> objectReader)
		{
			return QueryToListAsync(objectReader, CancellationToken.None);
		}

		public async Task<List<T>> QueryToListAsync<T>(Func<DbDataReader, T> objectReader, CancellationToken cancellationToken)
		{
			var list = new List<T>();
			await QueryForEachAsync(objectReader, list.Add, cancellationToken).ConfigureAwait(false);
			return list;
		}

		public Task<T[]> QueryToArrayAsync<T>(Func<DbDataReader, T> objectReader)
		{
			return QueryToArrayAsync(objectReader, CancellationToken.None);
		}

		public async Task<T[]> QueryToArrayAsync<T>(Func<DbDataReader, T> objectReader, CancellationToken cancellationToken)
		{
			var list = new List<T>();
			await QueryForEachAsync(objectReader, list.Add, cancellationToken).ConfigureAwait(false);
			return list.ToArray();
		}

		public Task QueryForEachAsync<T>(Func<DbDataReader, T> objectReader, Action<T> action)
		{
			return QueryForEachAsync(objectReader, action, CancellationToken.None);
		}

		public async Task QueryForEachAsync<T>(Func<DbDataReader, T> objectReader, Action<T> action, CancellationToken cancellationToken)
		{
			while (await Reader!.ReadAsync(cancellationToken).ConfigureAwait(false))
				action(objectReader(Reader));
		}

		public IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(Func<DbDataReader, T> objectReader)
		{
			return Impl(objectReader);

			async IAsyncEnumerable<T> Impl(Func<DbDataReader, T> objectReader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				while (await Reader!.ReadAsync(cancellationToken).ConfigureAwait(false))
					yield return objectReader(Reader);
			}
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
			await QueryForEachAsync<T>(list.Add, cancellationToken).ConfigureAwait(false);
			return list;
		}

		public Task<T[]> QueryToArrayAsync<T>()
		{
			return QueryToArrayAsync<T>(CancellationToken.None);
		}

		public async Task<T[]> QueryToArrayAsync<T>(CancellationToken cancellationToken)
		{
			var list = new List<T>();
			await QueryForEachAsync<T>(list.Add, cancellationToken).ConfigureAwait(false);
			return list.ToArray();
		}

		public Task QueryForEachAsync<T>(Action<T> action)
		{
			return QueryForEachAsync(action, CancellationToken.None);
		}

		public async Task QueryForEachAsync<T>(Action<T> action, CancellationToken cancellationToken)
		{
			if (ReadNumber != 0)
				if (!await Reader!.NextResultAsync(cancellationToken).ConfigureAwait(false))
					return;

			ReadNumber++;

			await CommandInfo!.ExecuteQueryAsync(Reader!, FormattableString.Invariant($"{CommandInfo.CommandText}$$${ReadNumber}"), action, cancellationToken).ConfigureAwait(false);
		}

		public IAsyncEnumerable<T> QueryToAsyncEnumerable<T>()
		{
			return Impl();

			async IAsyncEnumerable<T> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				if (ReadNumber != 0
					&& !await Reader!.NextResultAsync(cancellationToken).ConfigureAwait(false))
				{
					yield break;
				}

				ReadNumber++;

				await foreach (var element in CommandInfo!.ExecuteQueryAsync<T>(Reader!, FormattableString.Invariant($"{CommandInfo.CommandText}$$${ReadNumber}"))
						.WithCancellation(cancellationToken)
						.ConfigureAwait(false))
				{
					yield return element;
				}
			}
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
			await QueryForEachAsync(template, list.Add, cancellationToken).ConfigureAwait(false);
			return list;
		}

		public Task<T[]> QueryToArrayAsync<T>(T template)
		{
			return QueryToArrayAsync(template, CancellationToken.None);
		}

		public async Task<T[]> QueryToArrayAsync<T>(T template, CancellationToken cancellationToken)
		{
			var list = new List<T>();
			await QueryForEachAsync(template, list.Add, cancellationToken).ConfigureAwait(false);
			return list.ToArray();
		}

		public Task QueryForEachAsync<T>(T template, Action<T> action)
		{
			return QueryForEachAsync(template, action, CancellationToken.None);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "template param used to provide T generic argument")]
		public Task QueryForEachAsync<T>(T template, Action<T> action, CancellationToken cancellationToken)
		{
			return QueryForEachAsync(action, cancellationToken);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "template param used to provide T generic argument")]
		public IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(T template)
		{
			return QueryToAsyncEnumerable<T>();
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
				if (!await Reader!.NextResultAsync(cancellationToken).ConfigureAwait(false))
					return default(T)!;

			ReadNumber++;

			var sql = FormattableString.Invariant($"{CommandInfo!.CommandText}$$${ReadNumber}");

			return await CommandInfo.ExecuteScalarAsync<T>(Reader!, sql, cancellationToken).ConfigureAwait(false);
		}

		#endregion
	}
}
