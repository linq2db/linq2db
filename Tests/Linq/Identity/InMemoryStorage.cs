using System;
using Microsoft.Data.Sqlite;

namespace Tests.Identity
{
	public class InMemoryStorage : IDisposable
	{
		private static int _counter;
		private static readonly object _syncRoot = new object();
		private readonly SqliteConnection _connection;

		public InMemoryStorage()
		{
			lock (_syncRoot)
			{
				ConnectionString = $"Data Source=file:memdb{_counter}?mode=memory&cache=shared";
				//_connectionString = "Data Source=file:memdb?mode=memory&cache=shared";
				_counter++;
			}

			var connectionString = ConnectionString; //"Data Source=:memory:;";
			_connection = new SqliteConnection(connectionString);
			_connection.Open();
		}

		public string ConnectionString { get; }

		public void Dispose()
		{
			_connection.Dispose();
		}
	}

}
