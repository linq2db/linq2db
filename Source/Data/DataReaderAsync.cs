using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data
{
	using Linq;

#if !NOASYNC

	public class DataReaderAsync : IDisposable
	{
		public   CommandInfo       CommandInfo       { get; set; }
		public   DbDataReader      Reader            { get; set; }
		internal int               ReadNumber        { get; set; }
		internal CancellationToken CancellationToken { get; set; }

		public void Dispose()
		{
			if (Reader != null)
				Reader.Dispose();
		}

		#region Query with object reader

		public Task<List<T>> QueryToListAsync<T>(Func<IDataReader,T> objectReader)
		{
			return QueryToListAsync(objectReader, CancellationToken.None);
		}

		public async Task<List<T>> QueryToListAsync<T>(Func<IDataReader,T> objectReader, CancellationToken cancellationToken)
		{
			var list = new List<T>();
			await QueryForEachAsync(objectReader, list.Add, cancellationToken);
			return list;
		}

		public Task<T[]> QueryToArrayAsync<T>(Func<IDataReader,T> objectReader)
		{
			return QueryToArrayAsync(objectReader, CancellationToken.None);
		}

		public async Task<T[]> QueryToArrayAsync<T>(Func<IDataReader,T> objectReader, CancellationToken cancellationToken)
		{
			var list = new List<T>();
			await QueryForEachAsync(objectReader, list.Add, cancellationToken);
			return list.ToArray();
		}

		public Task QueryForEachAsync<T>(Func<IDataReader,T> objectReader, Action<T> action)
		{
			return QueryForEachAsync(objectReader, action, CancellationToken.None);
		}

		public async Task QueryForEachAsync<T>(Func<IDataReader,T> objectReader, Action<T> action, CancellationToken cancellationToken)
		{
			while (await Reader.ReadAsync(cancellationToken))
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
			await QueryForEachAsync<T>(list.Add, cancellationToken);
			return list;
		}

		public Task<T[]> QueryToArrayAsync<T>()
		{
			return QueryToArrayAsync<T>(CancellationToken.None);
		}

		public async Task<T[]> QueryToArrayAsync<T>(CancellationToken cancellationToken)
		{
			var list = new List<T>();
			await QueryForEachAsync<T>(list.Add, cancellationToken);
			return list.ToArray();
		}

		public Task QueryForEachAsync<T>(Action<T> action)
		{
			return QueryForEachAsync(action, CancellationToken.None);
		}

		public async Task QueryForEachAsync<T>(Action<T> action, CancellationToken cancellationToken)
		{
			if (ReadNumber != 0)
				if (!await Reader.NextResultAsync(cancellationToken))
					return;

			ReadNumber++;

			await CommandInfo.ExecuteQueryAsync(Reader, CommandInfo.DataConnection.Command.CommandText + "$$$" + ReadNumber, action, cancellationToken);
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
			await QueryForEachAsync(template, list.Add, cancellationToken);
			return list;
		}

		public Task<T[]> QueryToArrayAsync<T>(T template)
		{
			return QueryToArrayAsync(template, CancellationToken.None);
		}

		public async Task<T[]> QueryToArrayAsync<T>(T template, CancellationToken cancellationToken)
		{
			var list = new List<T>();
			await QueryForEachAsync(template, list.Add, cancellationToken);
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
				if (!await Reader.NextResultAsync(cancellationToken))
					return default(T);

			ReadNumber++;

			var sql = CommandInfo.DataConnection.Command.CommandText + "$$$" + ReadNumber;

			return await CommandInfo.ExecuteScalarAsync<T>(Reader, sql, cancellationToken);
		}

		#endregion
	}

#endif
}
