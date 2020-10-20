using System;
using System.Linq;

using JetBrains.Annotations;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class TableOptionsTests : TestBase
	{
		[Test]
		public void IsTemporaryOptionTest(
			[DataSources(false)] string context,
			[Values(TableOptions.CreateIfNotExists | TableOptions.DropIfExists, TableOptions.NotSet)] TableOptions tableOptions)
		{
			using var db = (DataConnection)GetDataContext(context);
			using var t1 = db.CreateTempTable("temp_table1", TableOptions.IsTemporary | tableOptions, new[] { new { ID = 1, Value = 2 } });
			using var t2 = db.CreateTempTable("temp_table2", TableOptions.IsTemporary | tableOptions, t1);

			var l1 = t1.ToArray();
			var l2 = t2.ToArray();

			Assert.That(l1, Is.EquivalentTo(l2));

			t1.Truncate();
			t2.Truncate();
		}

		[Table(IsTemporary = true)]
		[Table(IsTemporary = true, Configuration = ProviderName.SqlServer,  Database = "TestData", Schema = "TestSchema")]
		[Table(IsTemporary = true, Configuration = ProviderName.Sybase,     Database = "TestData")]
		[Table(IsTemporary = true, Configuration = ProviderName.SQLite)]
		[Table(IsTemporary = true, Configuration = ProviderName.PostgreSQL, Database = "TestData", Schema = "test_schema")]
		[Table(IsTemporary = true, Configuration = ProviderName.DB2,                               Schema = "SESSION")]
		[UsedImplicitly]
		class IsTemporaryTable
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void IsTemporaryFlagTest([DataSources(false)] string context, [Values(true)] bool firstCall)
		{
			using var db = (DataConnection)GetDataContext(context);
			using var table = db.CreateTempTable<IsTemporaryTable>();
			_ = table.ToArray();
		}

		[Table(TableOptions = TableOptions.IsGlobalTemporaryStructure)]
		[Table(TableOptions = TableOptions.IsGlobalTemporaryStructure, Configuration = ProviderName.DB2, Schema = "SESSION")]
		[UsedImplicitly]
		class IsGlobalTemporaryTable
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void IsGlobalTemporaryTest([IncludeDataSources(
			ProviderName.DB2,
			ProviderName.Firebird,
			TestProvName.AllSqlServer2005Plus,
			TestProvName.AllSybase)] string context,
			[Values(true)] bool firstCall)
		{
			using var db = (DataConnection)GetDataContext(context);
			using var table = db.CreateTempTable<IsGlobalTemporaryTable>();
			_ = table.ToArray();
		}

		[Table(TableOptions = TableOptions.CreateIfNotExists)]
		[Table(TableOptions = TableOptions.CreateIfNotExists | TableOptions.IsTemporary, Configuration = ProviderName.SqlServer2008)]
		[Table("##temp_table", TableOptions = TableOptions.CreateIfNotExists, Configuration = ProviderName.SqlServer2012)]
		[UsedImplicitly]
		class CreateIfNotExistsTable
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void CreateIfNotExistsTest([IncludeDataSources(
			true,
			ProviderName.DB2,
			ProviderName.Informix,
			ProviderName.Firebird,
			TestProvName.AllMySql,
			TestProvName.AllOracle,
			ProviderName.PostgreSQL,
			TestProvName.AllSQLite,
			TestProvName.AllSqlServer2005Plus,
			TestProvName.AllSybase)] string context)
		{
			if (context.StartsWith("SqlServer.20") && context.EndsWith(".LinqService"))
				return;

			using var db = GetDataContext(context);

			db.DropTable<CreateIfNotExistsTable>(throwExceptionIfNotExists:false);

			using var table = db.CreateTempTable<CreateIfNotExistsTable>();

			table.Insert(() => new CreateIfNotExistsTable { Id = 1, Value = 2 });

			_ = table.ToArray();
			_ = db.CreateTempTable<CreateIfNotExistsTable>();
		}

		[Test]
		public void CreateTempIfNotExistsTest([IncludeDataSources(
			false,
			ProviderName.DB2,
			ProviderName.Informix,
			ProviderName.Firebird,
			TestProvName.AllMySql,
			TestProvName.AllOracle,
			ProviderName.PostgreSQL,
			TestProvName.AllSQLite,
			TestProvName.AllSqlServer2005Plus,
			TestProvName.AllSybase)] string context)
		{
			if (context.StartsWith("SqlServer.20") && context.EndsWith(".LinqService"))
				return;

			using var db = GetDataContext(context);

			db.DropTable<CreateIfNotExistsTable>(throwExceptionIfNotExists:false);

			using var table = db.CreateTempTable<CreateIfNotExistsTable>(tableOptions:TableOptions.IsTemporary);

			_ = table.ToArray();
			_ = db.CreateTempTable<CreateIfNotExistsTable>();
		}

		[UsedImplicitly]
		class TestTable
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void IsTemporaryMethodTest([DataSources(false, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			db.DropTable<TestTable>(tableOptions:TableOptions.IsTemporary | TableOptions.DropIfExists);

			using var table = db.CreateTempTable<TestTable>(tableOptions:TableOptions.IsTemporary);

			_ =
			(
				from t1 in db.GetTable<TestTable>().IsTemporary()
				join t2 in db.GetTable<TestTable>().IsTemporary() on t1.Id equals t2.Id
				join t3 in table on t2.Id equals t3.Id
				select new { t1, t2, t3 }
			)
			.ToList();
		}
	}
}
