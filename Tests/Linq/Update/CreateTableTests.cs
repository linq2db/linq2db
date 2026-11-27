using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	[TestFixture]
	public class CreateTableTests : TestBase
	{
		sealed class TestTable
		{
			public int       ID        { get; set; }
			public string?   Field1    { get; set; }
			public string?   Field2    { get; set; }
			public DateTime? CreatedOn { get; set; }
		}

		[Test]
		public void CreateTable1([DataSources] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<TestTable>()
					.Property(t => t.ID)
						.IsIdentity()
						.IsPrimaryKey()
					.Property(t => t.Field1)
						.HasLength(50)
				.Build();

			using var db = GetDataContext(context, ms);
			db.DropTable<TestTable>(throwExceptionIfNotExists: false);

			var table = db.CreateTable<TestTable>();
			var list = table.ToList();

			db.DropTable<TestTable>();
		}

		[Test]
		public async Task CreateTable1Async([DataSources] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<TestTable>()
					.Property(t => t.ID)
						.IsIdentity()
						.IsPrimaryKey()
					.Property(t => t.Field1)
						.HasLength(50)
				.Build();

			using var db = GetDataContext(context, ms);
			await db.DropTableAsync<TestTable>(throwExceptionIfNotExists: false);

			var table = await db.CreateTableAsync<TestTable>();
			var list  = await table.ToListAsync();

			await db.DropTableAsync<TestTable>();
		}

		[Test]
		public void CreateLocalTempTable1([IncludeDataSources(TestProvName.AllSqlServer2008Plus/*, ProviderName.DB2*/)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<TestTable>()
					.Property(t => t.Field1)
						.HasLength(50)
				.Build();

			using var db = GetDataContext(context, ms);

			const string tableName = "TestTable";

			try
			{
				switch (context)
				{
					case string when context.IsAnyOf(TestProvName.AllSqlServer2008Plus) : db.DropTable<TestTable>("#" + tableName); break;
					default                                                             : db.DropTable<TestTable>(tableName);       break;
				}
			}
			catch
			{
			}

			var table = context switch
			{
				string when context.IsAnyOf(TestProvName.AllSqlServer2008Plus) =>
					db.CreateTable<TestTable>("#" + tableName),

				ProviderName.DB2 =>
					db.CreateTable<TestTable>(statementHeader: "DECLARE GLOBAL TEMPORARY TABLE SESSION.{0}"),

				_ => throw new InvalidOperationException(),
			};

			var list = table.ToList();

			table.Drop();
		}

		[Test]
		public async Task CreateLocalTempTable1Async([IncludeDataSources(
			TestProvName.AllSQLite,
			TestProvName.AllClickHouse,
			TestProvName.AllSqlServer2008Plus /*, ProviderName.DB2*/)]
			string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<TestTable>()
					.Property(t => t.Field1)
						.HasLength(50)
				.Build();

			using var db = GetDataContext(context, ms);

			const string tableName = "TestTable";

			try
			{
				switch (context)
				{
					case string when context.IsAnyOf(TestProvName.AllSqlServer2008Plus): await db.DropTableAsync<TestTable>("#" + tableName); break;
					default                                                            : await db.DropTableAsync<TestTable>(tableName);       break;
				}
			}
			catch
			{
			}

			var table = context switch
			{
				string when context.IsAnyOf(TestProvName.AllSqlServer2008Plus) =>
					await db.CreateTableAsync<TestTable>("#" + tableName),

				ProviderName.DB2 =>
					await db.CreateTableAsync<TestTable>(statementHeader: "DECLARE GLOBAL TEMPORARY TABLE SESSION.{0}"),

				_ => await db.CreateTableAsync<TestTable>(tableName),
			};

			var list = await table.ToListAsync();

			await table.DropAsync();
		}

		[Test]
		public void CreateLocalTempTable2([IncludeDataSources(TestProvName.AllSqlServer2008Plus/*, ProviderName.DB2*/)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<TestTable>()
					.Property(t => t.Field1)
						.HasLength(50)
				.Build();

			using var db = GetDataContext(context, ms);

			const string tableName = "TestTable";

			try
			{
				switch (context)
				{
					case string when context.IsAnyOf(TestProvName.AllSqlServer2008Plus): db.DropTable<TestTable>("#" + tableName); break;
					default: db.DropTable<TestTable>(tableName); break;
				}
			}
			catch
			{
			}

			var table = context switch
			{
				string when context.IsAnyOf(TestProvName.AllSqlServer2008Plus) =>
					db.CreateTable<TestTable>(new CreateTableOptions(TableName: "#" + tableName)),

				ProviderName.DB2 =>
					db.CreateTable<TestTable>(new CreateTableOptions(StatementHeader: "DECLARE GLOBAL TEMPORARY TABLE SESSION.{0}")),

				_ => throw new InvalidOperationException(),
			};
			var list = table.ToList();

			table.Drop();
		}

		[Test]
		public async Task CreateLocalTempTable2Async([IncludeDataSources(
			TestProvName.AllSQLite,
			TestProvName.AllClickHouse,
			TestProvName.AllSqlServer2008Plus /*, ProviderName.DB2*/)]
			string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<TestTable>()
					.Property(t => t.Field1)
						.HasLength(50)
				.Build();

			using var db = GetDataContext(context, ms);

			const string tableName = "TestTable";

			try
			{
				switch (context)
				{
					case string when context.IsAnyOf(TestProvName.AllSqlServer2008Plus): await db.DropTableAsync<TestTable>("#" + tableName); break;
					default: await db.DropTableAsync<TestTable>(tableName); break;
				}
			}
			catch
			{
			}

			var table = context switch
			{
				string when context.IsAnyOf(TestProvName.AllSqlServer2008Plus) =>
					await db.CreateTableAsync<TestTable>(new CreateTableOptions(TableName: "#" + tableName)),

				ProviderName.DB2 =>
					await db.CreateTableAsync<TestTable>(new CreateTableOptions(StatementHeader: "DECLARE GLOBAL TEMPORARY TABLE SESSION.{0}")),

				_ => await db.CreateTableAsync<TestTable>(new CreateTableOptions(TableName: tableName)),
			};

			var list = await table.ToListAsync();

			await table.DropAsync();
		}

		enum FieldType1
		{
			[MapValue(1)] Value1,
			[MapValue(2)] Value2,
		}

		enum FieldType2
		{
			[MapValue("A")]  Value1,
			[MapValue("AA")] Value2,
		}

		enum FieldType3 : short
		{
			Value1,
			Value2,
		}

		sealed class TestEnumTable
		{
			public FieldType1 Field1;
			[Column(DataType=DataType.Int32)]
			public FieldType1? Field11;
			public FieldType2? Field2;
			[Column(DataType=DataType.VarChar, Configuration = ProviderName.ClickHouse)]
			[Column(DataType=DataType.Char, Length=2)]
			public FieldType2 Field21;
			public FieldType3 Field3;
		}

		[Test]
		public void CreateTableWithEnum([IncludeDataSources(TestProvName.AllSqlServer2012, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			try
			{
				db.DropTable<TestEnumTable>();
			}
			catch
			{
			}

			var table = db.CreateTable<TestEnumTable>();

			table.Insert(() => new TestEnumTable
			{
				Field1 = FieldType1.Value1,
				Field11 = FieldType1.Value1,
				Field2 = FieldType2.Value1,
				Field21 = FieldType2.Value1,
				Field3 = FieldType3.Value1,
			});

			var list = table.ToList();

			db.DropTable<TestEnumTable>();
		}

		public enum Jjj
		{
			aa,
			bb,
		}
		public class base_aa
		{
			public Jjj dd { get; set; }
		}
		public class Aa : base_aa
		{
			public int     bb { get; set; }
			public string? cc { get; set; }
		}

		public class Qq
		{
			public int     bb { get; set; }
			public string? cc { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/160")]
		public void FluentEntitiesSharingTableName([DataSources] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<Aa>()
					.HasTableName("aa")
					.Property(t => t.bb).IsPrimaryKey()
					.Property(t => t.cc)
					.Property(t => t.dd).IsNotColumn()

				.Entity<Qq>()
					.HasTableName("aa")
					.Property(t => t.bb).IsPrimaryKey()
					.Property(t => t.cc)

				.Build();

			using var conn = GetDataContext(context, ms);
			try
			{
				conn.CreateTable<Qq>();
			}
			catch
			{
				conn.DropTable<Qq>();
				conn.CreateTable<Qq>();
			}

			conn.Insert(new Aa
			{
				bb = 99,
				cc = "hallo",
				dd = Jjj.aa
			});

			var qq = conn.GetTable<Aa>().ToList().First();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(qq.bb, Is.EqualTo(99));
				Assert.That(qq.cc, Is.EqualTo("hallo"));
			}

			conn.DropTable<Qq>();
		}

		[Test]
		public void CreateTable2([IncludeDataSources(TestProvName.AllSqlServer2012)] string context)
		{
			using var db = GetDataContext(context);
			var table = db.CreateTable<TestEnumTable>("#TestTable");
			table.BulkCopy(new BulkCopyOptions { BulkCopyType = BulkCopyType.ProviderSpecific },
				new[]
			{
					new TestEnumTable
					{
						Field1  = FieldType1.Value1,
						Field11 = FieldType1.Value1,
						Field2  = FieldType2.Value1,
						Field21 = FieldType2.Value1,
						Field3  = FieldType3.Value1,
					}
			});
			table.DropTable();
		}

		sealed class TestCreateFormat
		{
			[Column(CreateFormat = "{0}{1}{2}{3}/* test */"), NotNull]
			public int Field1;
			[Column]
			public int Field2;
		}

		[Test]
		public void CreateFormatTest([IncludeDataSources(TestProvName.AllSqlServer2012)] string context)
		{
			using var db = GetDataContext(context);
			var table = db.CreateTable<TestCreateFormat>("#TestTable");
			table.DropTable();
		}

		[ActiveIssue(3223, ErrorMessage = "Assert.That(res[0].Value, Is.EqualTo(\"Value1\"))",
			Details = "CreateTable maps the enum to a single character rather than varchar, so the value comes back truncated to one char - #3223's subject.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3223")]
		public void EnumMappedToVarCharWhenEnumsAreStrings([DataSources] string context)
		{
			var ms = new MappingSchema();
			ms.SetDataType(typeof(Enum), DataType.VarChar);

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable(VarCharEnumTable.Data);

			var res = db.GetTable<VarCharEnumRaw>().OrderBy(r => r.Id).ToArray();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Value, Is.EqualTo("Value1"));
				Assert.That(res[1].Value, Is.EqualTo("Value2"));
				Assert.That(res[2].Value, Is.Null);
			}
		}

		[Table("VarCharEnumTable")]
		sealed class VarCharEnumRaw
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public string? Value { get; set; }
		}

		[Table("VarCharEnumTable")]
		sealed class VarCharEnumTable
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public VarCharMappedEnum? Value { get; set; }

			public static readonly VarCharEnumTable[] Data =
			[
				new VarCharEnumTable() { Id = 1, Value = VarCharMappedEnum.Value1 },
				new VarCharEnumTable() { Id = 2, Value = VarCharMappedEnum.Value2 },
				new VarCharEnumTable() { Id = 3 }
			];
		}

		enum VarCharMappedEnum
		{
			Value1,
			Value2
		}

		[Table(nameof(TempIdentityRow))]
		public class TempIdentityRow
		{
			[Identity, PrimaryKey]
			public int Id { get; set; }

			[Column]
			public int Value { get; set; }
		}

		// Not #4671, which the [Test] description below points at: on YDB this fails before reaching that
		// issue's subject, because the provider has no CREATE TEMPORARY TABLE at all.
		[ActiveIssue(Configuration = TestProvName.AllYdb, ErrorTypeName = "Ydb.Sdk.Ado.YdbException", ErrorMessage = "Creating temporary table is not supported.",
			Details = "no-issue: YDB does not implement CREATE TEMPORARY TABLE (feature under development upstream)")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4671")]
		public void CreateTempTableKeepsIdentity([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(TempIdentityRow));
			var column = ed.Columns.Single(c => c.ColumnName == nameof(TempIdentityRow.Id));

			Assert.That(column.IsIdentity);

			using var t1 = db.CreateLocalTable<TempIdentityRow>();
			using var t2 = db.CreateTempTable<TempIdentityRow>($"{nameof(TempIdentityRow)}TMP");

			t1.Insert(() => new TempIdentityRow() { Value = 1 });
			t2.Insert(() => new TempIdentityRow() { Value = 2 });

			var res1 = t1.Single();
			var res2 = t2.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res1.Id, Is.EqualTo(1));
				Assert.That(res2.Id, Is.EqualTo(1));
			}
		}

		/// <summary>
		/// Confirm that tables creation uses the <see cref="ColumnAttribute.Order"/> field correctly.
		/// Before fix fields in derived tables were added first in the column order by <see cref="DataExtensions.CreateTable{T}(IDataContext, string?, string?, string?, string?, string?, LinqToDB.SqlQuery.DefaultNullable, string?, TableOptions)"/>.
		/// </summary>
		/// <param name="context">Configuration string for test context.</param>
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1305")]
		public void CreateTableHonorsAttributeColumnOrder([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			using var __ = db.CreateLocalTable<ColumnOrderTest>();
			// Get table schema
			var sp = db.DataProvider.GetSchemaProvider();
			var s = sp.GetSchema(db);
			var table = s.Tables.FirstOrDefault(_ => _.TableName!.Equals("ColumnOrderTest", StringComparison.OrdinalIgnoreCase))!;
			Assert.That(table, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				// Confirm order of specified fields only
				Assert.That(table.Columns[0].ColumnName.ToLowerInvariant(), Is.EqualTo("recordid"));
				Assert.That(table.Columns[1].ColumnName.ToLowerInvariant(), Is.EqualTo("effectivestart"));
				Assert.That(table.Columns[2].ColumnName.ToLowerInvariant(), Is.EqualTo("effectiveend"));
				Assert.That(table.Columns[3].ColumnName.ToLowerInvariant(), Is.EqualTo("key"));
				Assert.That(table.Columns[6].ColumnName.ToLowerInvariant(), Is.EqualTo("audit1id"));
				Assert.That(table.Columns[7].ColumnName.ToLowerInvariant(), Is.EqualTo("audit2id"));
			}

			// Confirm that unordered fields are in the right range of positions
			string[] unordered = new[] { "name", "code" };
			Assert.That(unordered, Does.Contain(table.Columns[4].ColumnName.ToLowerInvariant()));
			Assert.That(unordered, Does.Contain(table.Columns[5].ColumnName.ToLowerInvariant()));
		}

		/// <summary>
		/// Confirm that tables creation uses the <see cref="ColumnAttribute.Order"/> field correctly.
		/// </summary>
		/// <param name="context">Configuration string for test context.</param>
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1305")]
		public void CreateTableHonorsFluentColumnOrder([DataSources(false)] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<FluentColumnOrderTable>()
				.Property(t => t.Audit1ID).HasOrder(-10)
				.Property(t => t.Audit2ID).HasOrder(-1)
				.Property(t => t.RecordID).HasOrder(1).IsPrimaryKey()
				.Property(t => t.EffectiveEnd).HasOrder(3)
				.Property(t => t.EffectiveStart).HasOrder(2)
				.Property(t => t.Key).HasOrder(4)
				.Property(t => t.Unordered1)
				.Property(t => t.Unordered2)
				.Build();

			using var db = GetDataConnection(context, ms);
			using var tbl = db.CreateLocalTable<FluentColumnOrderTable>();
			// Get table schema
			var sp = db.DataProvider.GetSchemaProvider();
			var s = sp.GetSchema(db);
			var table = s.Tables.FirstOrDefault(_ => _.TableName!.Equals(nameof(FluentColumnOrderTable), StringComparison.OrdinalIgnoreCase))!;
			Assert.That(table, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				// Confirm order of specified fields only
				Assert.That(table.Columns[0].ColumnName.ToLowerInvariant(), Is.EqualTo("recordid"));
				Assert.That(table.Columns[1].ColumnName.ToLowerInvariant(), Is.EqualTo("effectivestart"));
				Assert.That(table.Columns[2].ColumnName.ToLowerInvariant(), Is.EqualTo("effectiveend"));
				Assert.That(table.Columns[3].ColumnName.ToLowerInvariant(), Is.EqualTo("key"));
				Assert.That(table.Columns[6].ColumnName.ToLowerInvariant(), Is.EqualTo("audit1id"));
				Assert.That(table.Columns[7].ColumnName.ToLowerInvariant(), Is.EqualTo("audit2id"));
			}

			// Confirm that unordered fields are in the right range of positions
			string[] unordered = new[] { "unordered1", "unordered2" };
			Assert.That(unordered, Does.Contain(table.Columns[4].ColumnName.ToLowerInvariant()));
			Assert.That(unordered, Does.Contain(table.Columns[5].ColumnName.ToLowerInvariant()));
		}

		public class FluentColumnOrderTable
		{
			public int       RecordID       { get; set; }
			public DateTime? EffectiveEnd   { get; set; }
			public DateTime  EffectiveStart { get; set; }
			public int       Key            { get; set; }
			public int       Audit2ID       { get; set; }
			public int       Audit1ID       { get; set; }
			public int       Unordered1     { get; set; }
			public int       Unordered2     { get; set; }
		}

		/// <summary>
		/// Base table class with column order specified.
		/// </summary>
		public abstract class VersionedRecord
		{
			[Column(IsPrimaryKey = true, SkipOnInsert = true, Order = 1)]
			public int RecordID             { get; set; }
			[Column(Order = 3)]
			public DateTime? EffectiveEnd   { get; set; }
			[Column(Order = 2)]
			public DateTime EffectiveStart  { get; set; }
			[Column(Order = 4)]
			public int Key                  { get; set; }
			[Column(Order = -1)]
			public int Audit2ID             { get; set; }
			[Column(Order = -10)]
			public int Audit1ID             { get; set; }
		}

		/// <summary>
		/// Derived table class, column order not specified.
		/// </summary>
		[Table("ColumnOrderTest")]
		public class ColumnOrderTest : VersionedRecord
		{
			[Column]
			public string? Name { get; set; }
			[Column]
			public string? Code { get; set; }
		}
	}
}
