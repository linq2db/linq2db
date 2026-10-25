using System;

using LinqToDB;
using LinqToDB.DataProvider.SQLite;

using Microsoft.Data.Sqlite;

namespace Tests.Scaffold.Harness
{
	/// <summary>
	/// Private in-memory SQLite database. Additional schemas are attached in-memory databases, mapped through
	/// <see cref="LinqToDB.SqlQuery.SqlObjectName.Database"/> (SQLite renders database name, not schema).
	/// </summary>
	public sealed class SqliteTestDb : IDisposable
	{
		readonly SqliteConnection _connection = new("Data Source=:memory:");

		public SqliteTestDb(params string[] attachedDatabases)
		{
			_connection.Open();

			foreach (var name in attachedDatabases)
				Execute($"ATTACH DATABASE ':memory:' AS \"{name}\"");
		}

		public SqliteConnection Connection => _connection;

		/// <summary>
		/// Options for context over this database. Connection is not disposed by context.
		/// </summary>
		public DataOptions Options => new DataOptions().UseConnection(SQLiteTools.GetDataProvider(SQLiteProvider.Microsoft), _connection, false);

		public void Execute(string sql)
		{
			using var command = _connection.CreateCommand();
			command.CommandText = sql;
			command.ExecuteNonQuery();
		}

		public void Dispose() => _connection.Dispose();
	}
}
