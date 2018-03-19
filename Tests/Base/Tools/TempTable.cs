using System;
using System.Collections.Generic;
using LinqToDB;
using LinqToDB.Data;

namespace Tests.Tools
{
	public static class TempTable
	{
		public static TempTable<T> Create<T>(IDataContext db, string tableName)
		{
			return new TempTable<T>(db, tableName);
		}

		public static TempTable<T> Create<T>(IDataContext db, IEnumerable<T> data, string tableName)
		{
			var table = new TempTable<T>(db, tableName);
			table.Table.BulkCopy(data);
			return table;
		}
	}

	public class TempTable<T> : IDisposable
	{
		public ITable<T> Table { get; }

		public TempTable(IDataContext db, string tableName)
		{
			try
			{
				Table = db.CreateTable<T>(tableName);
			}
			catch
			{
				db.DropTable<T>(tableName, throwExceptionIfNotExists: false);
				Table = db.CreateTable<T>(tableName);
			}
		}

		public void Dispose()
		{
			Table.DropTable();
		}
	}
}
