using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	[TestFixture]
	[Order(10000)]
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

			using (var db = GetDataContext(context, ms))
			{
				db.DropTable<TestTable>(throwExceptionIfNotExists:false);

				var table = db.CreateTable<TestTable>();
				var list = table.ToList();

				db.DropTable<TestTable>();
			}
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

			using (var db = GetDataContext(context, ms))
			{
				await db.DropTableAsync<TestTable>(throwExceptionIfNotExists:false);

				var table = await db.CreateTableAsync<TestTable>();
				var list  = await table.ToListAsync();

				await db.DropTableAsync<TestTable>();
			}
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

			using (var db = GetDataContext(context, ms))
			{
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

				ITable<TestTable> table;

				switch (context)
				{
					case string when context.IsAnyOf(TestProvName.AllSqlServer2008Plus):
						table = db.CreateTable<TestTable>("#" + tableName);
						break;
					case ProviderName.DB2                                 :
						table = db.CreateTable<TestTable>(statementHeader :"DECLARE GLOBAL TEMPORARY TABLE SESSION.{0}");
						break;
					default                                               :
						throw new InvalidOperationException();
				}

				var list = table.ToList();

				table.Drop();
			}
		}

		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/58", Configuration = ProviderName.ClickHouseOctonica)]
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

			using (var db = GetDataContext(context, ms))
			{
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

				ITable<TestTable> table;

				switch (context)
				{
					case string when context.IsAnyOf(TestProvName.AllSqlServer2008Plus):
						table = await db.CreateTableAsync<TestTable>("#" + tableName);
						break;
					case ProviderName.DB2                                           :
						table = await db.CreateTableAsync<TestTable>(statementHeader:"DECLARE GLOBAL TEMPORARY TABLE SESSION.{0}");
						break;
					default                                                         :
						table = await db.CreateTableAsync<TestTable>(tableName);
						break;
				}

				var list = await table.ToListAsync();

				await table.DropAsync();
			}
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

		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/58", Configuration = ProviderName.ClickHouseOctonica)]
		[Test]
		public void CreateTableWithEnum([IncludeDataSources(TestProvName.AllSqlServer2012, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
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
					Field1  = FieldType1.Value1,
					Field11 = FieldType1.Value1,
					Field2  = FieldType2.Value1,
					Field21 = FieldType2.Value1,
					Field3  = FieldType3.Value1,
				});

				var list = table.ToList();

				db.DropTable<TestEnumTable>();
			}
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

		[Test]
		public void TestIssue160([DataSources] string context)
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

			using (var conn = GetDataContext(context, ms))
			{
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
		}

		[Test]
		public void CreateTable2([IncludeDataSources(TestProvName.AllSqlServer2012)] string context)
		{
			using (var db = GetDataContext(context))
			{
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
			using (var db = GetDataContext(context))
			{
				var table = db.CreateTable<TestCreateFormat>("#TestTable");
				table.DropTable();
			}
		}

		#region Issue 3223
		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3223")]
		public void Issue3223Test([DataSources] string context)
		{
			var ms = new MappingSchema();
			ms.SetDataType(typeof(Enum), DataType.VarChar);

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable(Issue3223Table.Data);

			var res = db.GetTable<Issue3223Raw>().OrderBy(r => r.Id).ToArray();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Value, Is.EqualTo("Value1"));
				Assert.That(res[1].Value, Is.EqualTo("Value2"));
				Assert.That(res[2].Value, Is.Null);
			}
		}

		[Table("Issue3223Table")]
		sealed class Issue3223Raw
		{
			[Column] public int Id { get; set; }
			[Column] public string? Value { get; set; }
		}

		[Table("Issue3223Table")]
		sealed class Issue3223Table
		{
			[Column] public int Id { get; set; }
			[Column] public Issue3223Enum? Value { get; set; }

			public static readonly Issue3223Table[] Data =
			[
				new Issue3223Table() { Id = 1, Value = Issue3223Enum.Value1 },
				new Issue3223Table() { Id = 2, Value = Issue3223Enum.Value2 },
				new Issue3223Table() { Id = 3 }
			];
		}

		enum Issue3223Enum
		{
			Value1,
			Value2
		}
		#endregion

		[Table(nameof(Issue4671Entity))]
		public class Issue4671Entity
		{
			[Identity, PrimaryKey]
			public int Id { get; set; }

			[Column]
			public int Value { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4671")]
		public void Issue4671Test([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(Issue4671Entity));
			var column = ed.Columns.Single(c => c.ColumnName == nameof(Issue4671Entity.Id));

			Assert.That(column.IsIdentity);

			using var t1 = db.CreateLocalTable<Issue4671Entity>();
			using var t2 = db.CreateTempTable<Issue4671Entity>($"{nameof(Issue4671Entity)}TMP");

			t1.Insert(() => new Issue4671Entity() { Value = 1 });
			t2.Insert(() => new Issue4671Entity() { Value = 2 });

			var res1 = t1.Single();
			var res2 = t2.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res1.Id, Is.EqualTo(1));
				Assert.That(res2.Id, Is.EqualTo(1));
			}
		}

	}
}
