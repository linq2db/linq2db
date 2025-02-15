using System.Collections.Generic;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3056Tests : TestBase
	{
		[Test]
		public void DataModelDynamicTableTest2([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var mappingSchema = new MappingSchema();
			var fm            = new FluentMappingBuilder(mappingSchema);

			fm.Entity<DynamicTableRow>()
				//.HasIdentity(x => Sql.Property<int>(x, "Id"))
				.Property(x => Sql.Property<string>(x, "Name"))
				.Property(x => Sql.Property<string>(x, "Description"))
				.Build();

			var rows = new List<DynamicTableRow>();
			var drow = new DynamicTableRow();
			drow.Properties.Add("Id", 1);
			drow.Properties.Add("Name", "n1");
			drow.Properties.Add("Description", "d0");
			rows.Add(drow);
			drow = new DynamicTableRow();
			drow.Properties.Add("Id", 2);
			drow.Properties.Add("Name", "n2");
			drow.Properties.Add("Description", "d00");
			rows.Add(drow);
			using (var db = (DataConnection)GetDataContext(context, mappingSchema))
			using (db.CreateLocalTable<TestRow>())
			{
				var options = GetDefaultBulkCopyOptions(context) with
				{
					TableName    = TestTableName,
					SchemaName   = "dbo",
					MaxBatchSize = 100
				};

				db.BulkCopy(options, rows);
			}
		}

		private const string TestTableName = "Table_3056";
		[Table(TestTableName, Schema = "dbo")]
		sealed class TestRow
		{
			[PrimaryKey, Identity]
			public int Id;

			[Column(DataType = DataType.VarChar, Length = 100)]
			public string? Name;

			[Column(DataType = DataType.VarChar, Length = 200)]
			public string? Description;

		}

		[Table]
		public class DynamicTableRow
		{
			[DynamicColumnsStore]
			public Dictionary<string, object> Properties = new Dictionary<string, object>();
		}
	}
}
