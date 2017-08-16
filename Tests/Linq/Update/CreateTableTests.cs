﻿using System;
using System.Linq;

#if !NOASYNC
using System.Threading.Tasks;
#endif

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	[TestFixture]
	public class CreateTableTests : TestBase
	{
		class TestTable
		{
			public int       ID;
			public string    Field1;
			public string    Field2;
			public DateTime? CreatedOn;
		}

		[Test, DataContextSource(ProviderName.OracleNative)]
		public void CreateTable1(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<TestTable>()
						.Property(t => t.ID)
							.IsIdentity()
							.IsPrimaryKey()
						.Property(t => t.Field1)
							.HasLength(50);

				db.DropTable<TestTable>(throwExceptionIfNotExists:false);

				var table = db.CreateTable<TestTable>();
				var list = table.ToList();

				db.DropTable<TestTable>();
			}
		}

#if !NOASYNC

		[Test, DataContextSource(ProviderName.OracleNative)]
		public async Task CreateTable1Async(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<TestTable>()
						.Property(t => t.ID)
							.IsIdentity()
							.IsPrimaryKey()
						.Property(t => t.Field1)
							.HasLength(50);

				await db.DropTableAsync<TestTable>(throwExceptionIfNotExists:false);

				var table = await db.CreateTableAsync<TestTable>();
				var list  = await table.ToListAsync();

				await db.DropTableAsync<TestTable>();
			}
		}

#endif

		[Test, IncludeDataContextSource(false, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014 /*, ProviderName.DB2*/)]
		public void CreateLocalTempTable1(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<TestTable>()
						.Property(t => t.Field1)
							.HasLength(50);

				const string tableName = "TestTable";

				try
				{
					switch (context)
					{
						case ProviderName.SqlServer2008 : 
						case ProviderName.SqlServer2012 : 
						case ProviderName.SqlServer2014 : db.DropTable<TestTable>("#" + tableName); break;
						default                         : db.DropTable<TestTable>(tableName);       break;
					}
				}
				catch
				{
				}

				ITable<TestTable> table;

				switch (context)
				{
					case ProviderName.SqlServer2008 : 
					case ProviderName.SqlServer2012 : 
					case ProviderName.SqlServer2014 :
						table = db.CreateTable<TestTable>("#" + tableName);
						break;
					case ProviderName.DB2 :
						table = db.CreateTable<TestTable>(statementHeader:"DECLARE GLOBAL TEMPORARY TABLE SESSION.{0}");
						break;
					default:
						throw new InvalidOperationException();
				}

				var list = table.ToList();

				table.Drop();
			}
		}

#if !NOASYNC

		[Test, IncludeDataContextSource(false, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014 /*, ProviderName.DB2*/)]
		public async Task CreateLocalTempTable1Async(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.GetFluentMappingBuilder()
					.Entity<TestTable>()
						.Property(t => t.Field1)
							.HasLength(50);

				const string tableName = "TestTable";

				try
				{
					switch (context)
					{
						case ProviderName.SqlServer2008 : 
						case ProviderName.SqlServer2012 : 
						case ProviderName.SqlServer2014 : await db.DropTableAsync<TestTable>("#" + tableName); break;
						default                         : await db.DropTableAsync<TestTable>(tableName);       break;
					}
				}
				catch
				{
				}

				ITable<TestTable> table;

				switch (context)
				{
					case ProviderName.SqlServer2008 : 
					case ProviderName.SqlServer2012 : 
					case ProviderName.SqlServer2014 :
						table = await db.CreateTableAsync<TestTable>("#" + tableName);
						break;
					case ProviderName.DB2 :
						table = await db.CreateTableAsync<TestTable>(statementHeader:"DECLARE GLOBAL TEMPORARY TABLE SESSION.{0}");
						break;
					default:
						throw new InvalidOperationException();
				}

				var list = await table.ToListAsync();

				await table.DropAsync();
			}
		}

#endif

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

		class TestEnumTable
		{
			public FieldType1 Field1;
			[Column(DataType=DataType.Int32)]
			public FieldType1? Field11;
			public FieldType2? Field2;
			[Column(DataType=DataType.Char, Length=2)]
			public FieldType2 Field21;
			public FieldType3 Field3;
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2012)]
		public void CreateTableWithEnum(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.DropTable<TestEnumTable>();
				}
				catch (Exception)
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

		public enum jjj
		{
			aa,
			bb,
		}
		public class base_aa
		{
			public jjj dd { get; set; }
		}
		public class aa : base_aa
		{
			public int    bb { get; set; }
			public string cc { get; set; }
		}
		
		public class qq
		{
			public int bb { get; set; }
			public string cc { get; set; }
		}

		[Test, DataContextSource]
		public void TestIssue160(string context)
		{
			using (var conn = GetDataContext(context))
			{
				conn.MappingSchema.GetFluentMappingBuilder()
					.Entity<aa>()
						.HasTableName("aa")
						.Property(t => t.bb).IsPrimaryKey()
						.Property(t => t.cc)
						.Property(t => t.dd).IsNotColumn()
					
					.Entity<qq>()
						.HasTableName("aa")
						.Property(t => t.bb).IsPrimaryKey()
						.Property(t => t.cc)
					;

				try
				{
					conn.CreateTable<qq>();
				}
				catch
				{
					conn.DropTable<qq>();
					conn.CreateTable<qq>();
				}

				conn.Insert(new aa
				{
					bb = 99,
					cc = "hallo",
					dd = jjj.aa
				});

				var qq = conn.GetTable<aa>().ToList().First();

				Assert.That(qq.bb, Is.EqualTo(99));
				Assert.That(qq.cc, Is.EqualTo("hallo"));

				conn.DropTable<qq>();
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2012)]
		public void CreateTable2(string context)
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

		class TestCreateFormat
		{
			[Column(CreateFormat = "{0}{1}{2}{3}/* test */"), NotNull]
			public int Field1;
			[Column]
			public int Field2;
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2012)]
		public void CreateFormatTest(string context)
		{
			using (var db = GetDataContext(context))
			{
				var table = db.CreateTable<TestCreateFormat>("#TestTable");
				table.DropTable();
			}
		}
	}
}
