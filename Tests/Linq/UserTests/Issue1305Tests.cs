using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	/// <summary>
	/// Test fixes to Issue #1305.
	/// Before fix fields in derived tables were added first in the column order by <see cref="DataExtensions.CreateTable{T}(IDataContext, string, string, string, string, string, LinqToDB.SqlQuery.DefaultNullable)"/>.
	/// </summary>
	[TestFixture]
    public class Issue1305Tests : TestBase
    {
		/// <summary>
		/// Base table class with column order specified.
		/// </summary>
		public abstract class VersionedRecord
		{
			[Column(IsPrimaryKey = true, SkipOnInsert = true, Order = 1)]
			public int RecordID { get; set; }
			[Column(Order = 3)]
			public DateTime? EffectiveEnd { get; set; }
			[Column(Order = 2)]
			public DateTime EffectiveStart { get; set; }
			[Column(Order = 4)]
			public int Key { get; set; }
			[Column(Order = -1)]
			public int AuditID { get; set; }
		}

		/// <summary>
		/// Derived table class, column order not specified.
		/// </summary>
		[Table("ColumnOrderTest")]
		public class ColumnOrderTest : VersionedRecord
		{
			[Column]
			public string Name { get; set; }
			[Column]
			public string Code { get; set; }
		}

#if !NETSTANDARD1_6
		/// <summary>
		/// Confirm that tables creation uses the <see cref="ColumnAttribute.Order"/> field correctly.
		/// </summary>
		/// <param name="context">Configuration string for test context.</param>
		[Test, DataContextSource(false, "SQLite.MS")]
		public void Test1(string context)
		{
			using (var db = new DataConnection(context))
			{
				// Force recreate table
				db.DropTable<ColumnOrderTest>(throwExceptionIfNotExists: false);
				db.CreateTable<ColumnOrderTest>();

				// Get table schema
				var sp = db.DataProvider.GetSchemaProvider();
				var s = sp.GetSchema(db);
				var table = s.Tables.FirstOrDefault(_ => _.TableName.Equals("ColumnOrderTest", StringComparison.OrdinalIgnoreCase));
				Assert.IsNotNull(table);

				// Confirm order of specified fields only
				Assert.AreEqual("RecordID",			table.Columns[0].ColumnName);
				Assert.AreEqual("EffectiveStart",   table.Columns[1].ColumnName);
				Assert.AreEqual("EffectiveEnd",     table.Columns[2].ColumnName);
				Assert.AreEqual("Key",              table.Columns[3].ColumnName);
				Assert.AreEqual("AuditID",          table.Columns[6].ColumnName);

				// Confirm that unordered fields are in the right range of positions
				string[] unordered = new[] { "Name", "Code" };
				Assert.Contains(table.Columns[4].ColumnName, unordered);
				Assert.Contains(table.Columns[5].ColumnName, unordered);
			}
		}
#endif
	}
}
