﻿using System;
using System.Linq;
using System.Threading.Tasks;

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
			[Values(TableOptions.CheckExistence, TableOptions.NotSet)] TableOptions tableOptions)
		{
			using var db = (DataConnection)GetDataContext(context);
			using var t1 = new[] { new { ID = 1, ValueColumn = 2 } }.IntoTempTable(db, "temp_table1", tableOptions : TableOptions.IsTemporary   | tableOptions);
			using var t2 = t1.IntoTempTable("temp_table2", tableOptions : TableOptions.IsTemporary                                              | tableOptions);

			var l1 = t1.ToArray();
			var l2 = t2.ToArray();

			Assert.That(l1, Is.EquivalentTo(l2));

			t1.BulkCopy(new BulkCopyOptions { BulkCopyType = BulkCopyType.MultipleRows     }, new[] { new { ID = 2, ValueColumn = 3 } });
			t1.BulkCopy(new BulkCopyOptions { BulkCopyType = BulkCopyType.RowByRow         }, new[] { new { ID = 3, ValueColumn = 3 } });
			t1.BulkCopy(new BulkCopyOptions { BulkCopyType = BulkCopyType.ProviderSpecific }, new[] { new { ID = 4, ValueColumn = 5 } });

			t1.Truncate();
			t2.Truncate();
		}

		[Test]
		public async Task IsTemporaryOptionAsyncTest(
			[DataSources(false)] string context,
			[Values(TableOptions.CheckExistence, TableOptions.NotSet)] TableOptions tableOptions)
		{
			using var db = (DataConnection)GetDataContext(context);
			using var t1 = await new[] { new { ID = 1, ValueColumn = 2 } }.IntoTempTableAsync(db, "temp_table1", tableOptions : TableOptions.IsTemporary   | tableOptions);
			using var t2 = await t1.IntoTempTableAsync("temp_table2", tableOptions : TableOptions.IsTemporary                                              | tableOptions);

			var l1 = t1.ToArray();
			var l2 = t2.ToArray();

			Assert.That(l1, Is.EquivalentTo(l2));

			await t1.BulkCopyAsync(new BulkCopyOptions { BulkCopyType = BulkCopyType.MultipleRows     }, new[] { new { ID = 2, ValueColumn = 3 } });
			await t1.BulkCopyAsync(new BulkCopyOptions { BulkCopyType = BulkCopyType.RowByRow         }, new[] { new { ID = 3, ValueColumn = 3 } });
			await t1.BulkCopyAsync(new BulkCopyOptions { BulkCopyType = BulkCopyType.ProviderSpecific }, new[] { new { ID = 4, ValueColumn = 5 } });

			await t1.TruncateAsync();
			await t2.TruncateAsync();
		}

		[UsedImplicitly]
		class DisposableTable
		{
			public int ID;
		}

		[Test]
		public void CheckExistenceTest([DataSources(
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllSapHana)] string context)
		{
			using var db = GetDataContext(context);

			Assert.That(db.SupportedTableOptions & TableOptions.CheckExistence, Is.EqualTo(TableOptions.CheckExistence));

			using var tbl = db.CreateTempTable<DisposableTable>(tableOptions:TableOptions.CheckExistence);
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
			[Column] public int Id          { get; set; }
			[Column] public int ValueColumn { get; set; }
		}

		[Test]
		public void IsTemporaryFlagTest([DataSources(false)] string context, [Values(true)] bool firstCall)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateTempTable<IsTemporaryTable>(tableOptions:TableOptions.NotSet);
			_ = table.ToArray();
		}

		[Table(TableOptions = TableOptions.IsGlobalTemporaryStructure)]
		[Table(TableOptions = TableOptions.IsGlobalTemporaryStructure, Configuration = ProviderName.DB2, Schema = "SESSION")]
		[UsedImplicitly]
		class IsGlobalTemporaryTable
		{
			[Column] public int Id          { get; set; }
			[Column] public int ValueColumn { get; set; }
		}

		[Test]
		public void IsGlobalTemporaryTest([IncludeDataSources(
			ProviderName.DB2,
			TestProvName.AllFirebird,
			TestProvName.AllOracle,
			TestProvName.AllSqlServer2005Plus,
			TestProvName.AllSybase)] string context,
			[Values(true)] bool firstCall)
		{
			using var db = (DataConnection)GetDataContext(context);
			using var table = db.CreateTempTable<IsGlobalTemporaryTable>(tableOptions:TableOptions.NotSet);
			_ = table.ToArray();
		}

		[Table(TableOptions = TableOptions.CreateIfNotExists)]
		[Table(TableOptions = TableOptions.CreateIfNotExists | TableOptions.IsTemporary, Configuration = ProviderName.SqlServer2008)]
		[Table("##temp_table", TableOptions = TableOptions.CreateIfNotExists, Configuration = ProviderName.SqlServer2012)]
		[UsedImplicitly]
		class CreateIfNotExistsTable
		{
			[Column] public int Id          { get; set; }
			[Column] public int ValueColumn { get; set; }
		}

		[Test]
		public void CreateIfNotExistsTest([IncludeDataSources(
			true,
			ProviderName.DB2,
			TestProvName.AllInformix,
			TestProvName.AllFirebird,
			TestProvName.AllMySql,
			TestProvName.AllOracle,
			TestProvName.AllPostgreSQL,
			TestProvName.AllSQLite,
			TestProvName.AllSqlServer2005Plus,
			TestProvName.AllSybase)] string context)
		{
			if (context.StartsWith("SqlServer.20") && context.EndsWith(".LinqService"))
				return;

			using var db = GetDataContext(context);

			db.DropTable<CreateIfNotExistsTable>(throwExceptionIfNotExists:false);

			using var table = db.CreateTempTable<CreateIfNotExistsTable>(tableOptions:TableOptions.NotSet);

			table.Insert(() => new CreateIfNotExistsTable { Id = 1, ValueColumn = 2 });

			_ = table.ToArray();
			_ = db.CreateTempTable<CreateIfNotExistsTable>(tableOptions:TableOptions.NotSet);
		}

		[Test]
		public void CreateTempIfNotExistsTest([IncludeDataSources(
			false,
			ProviderName.DB2,
			TestProvName.AllInformix,
			TestProvName.AllFirebird,
			TestProvName.AllMySql,
			TestProvName.AllOracle,
			TestProvName.AllPostgreSQL,
			TestProvName.AllSQLite,
			TestProvName.AllSqlServer2005Plus,
			TestProvName.AllSybase)] string context)
		{
			if (context.StartsWith("SqlServer.20") && context.EndsWith(".LinqService"))
				return;

			using var db = GetDataContext(context);

			db.DropTable<CreateIfNotExistsTable>(throwExceptionIfNotExists:false);

			using var table = db.CreateTempTable<CreateIfNotExistsTable>();

			_ = table.ToArray();
			_ = db.CreateTempTable<CreateIfNotExistsTable>(tableOptions:TableOptions.NotSet);
		}

		[UsedImplicitly]
		class TestTable
		{
			[Column] public int Id          { get; set; }
			[Column] public int ValueColumn { get; set; }
		}

		[Test]
		public void IsTemporaryMethodTest([DataSources(false, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			db.DropTable<TestTable>(tableOptions:TableOptions.IsTemporary | TableOptions.DropIfExists);

			using var table = db.CreateTempTable<TestTable>();

			_ =
			(
				from t1 in db.GetTable<TestTable>().IsTemporary()
				join t2 in db.GetTable<TestTable>().IsTemporary() on t1.Id equals t2.Id
				join t3 in table on t2.Id equals t3.Id
				select new { t1, t2, t3 }
			)
			.ToList();
		}

		[Test]
		public void IsTemporaryMethodTest2([DataSources(false, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			db.DropTable<TestTable>(tableOptions:TableOptions.IsTemporary | TableOptions.DropIfExists);

			using var table = db.CreateTempTable<TestTable>();

			_ =
			(
				from t1 in db.GetTable<TestTable>().IsTemporary()
				from t2 in db.GetTable<TestTable>().IsTemporary()
				join t3 in table on t2.Id equals t3.Id
				where t1.Id == t2.Id
				select new { t1, t2, t3 }
			)
			.ToList();
		}

		[Test]
		public void IsTemporaryMethodTest3([DataSources(false, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			db.DropTable<TestTable>(tableOptions:TableOptions.IsTemporary | TableOptions.DropIfExists);

			using var table = db.CreateTempTable<TestTable>();

			_ =
			(
				from t1 in db.GetTable<TestTable>().IsTemporary(true)
				from t2 in db.GetTable<TestTable>().IsTemporary(true)
				join t3 in table on t2.Id equals t3.Id
				where t1.Id == t2.Id
				select new { t1, t2, t3 }
			)
			.ToList();
		}

		[Test]
		public void TableOptionsMethodTest([DataSources(false, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			db.DropTable<TestTable>(tableOptions:TableOptions.IsTemporary | TableOptions.DropIfExists);

			using var table = db.CreateTempTable<TestTable>();

			_ =
			(
				from t1 in db.GetTable<TestTable>().TableOptions(TableOptions.IsTemporary)
				from t2 in db.GetTable<TestTable>().TableOptions(TableOptions.IsTemporary)
				join t3 in table on t2.Id equals t3.Id
				where t1.Id == t2.Id
				select new { t1, t2, t3 }
			)
			.ToList();
		}

		[Test]
		public void FluentMappingTest([DataSources(false, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);

			db.MappingSchema.GetFluentMappingBuilder()
				.Entity<TestTable>()
				.HasIsTemporary()
				.HasTableOptions(TableOptions.DropIfExists);

			db.DropTable<TestTable>();

			using var table = db.CreateTempTable<TestTable>();

			_ =
			(
				from t1 in db.GetTable<TestTable>()
				from t2 in db.GetTable<TestTable>()
				join t3 in table on t2.Id equals t3.Id
				where t1.Id == t2.Id
				select new { t1, t2, t3 }
			)
			.ToList();
		}

		void TestTableOptions(string context, TableOptions tableOptions)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateTempTable<TestTable>(tableOptions:tableOptions);
		}

		[Test]
		public void DB2TableOptionsTest(
			[IncludeDataSources(ProviderName.DB2)] string context,
			[Values(
				TableOptions.IsTemporary,
				TableOptions.IsTemporary |                                           TableOptions.IsLocalTemporaryData,
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure,
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure  | TableOptions.IsLocalTemporaryData,
				                                                                     TableOptions.IsLocalTemporaryData,
				                           TableOptions.IsLocalTemporaryStructure,
				                           TableOptions.IsLocalTemporaryStructure  | TableOptions.IsLocalTemporaryData,
				                           TableOptions.IsGlobalTemporaryStructure,
				                           TableOptions.IsGlobalTemporaryStructure | TableOptions.IsLocalTemporaryData)]
			TableOptions tableOptions)
		{
			TestTableOptions(context, tableOptions);
		}

		[Test]
		public void FirebirdTableOptionsTest(
			[IncludeDataSources(TestProvName.AllFirebird)] string context,
			[Values(
				TableOptions.IsTemporary,
				TableOptions.IsTemporary |                                           TableOptions.IsLocalTemporaryData,
				TableOptions.IsTemporary | TableOptions.IsGlobalTemporaryStructure,
				TableOptions.IsTemporary | TableOptions.IsGlobalTemporaryStructure | TableOptions.IsLocalTemporaryData,
				                                                                     TableOptions.IsLocalTemporaryData,
				                                                                     TableOptions.IsTransactionTemporaryData,
				                           TableOptions.IsGlobalTemporaryStructure,
				                           TableOptions.IsGlobalTemporaryStructure | TableOptions.IsLocalTemporaryData,
				                           TableOptions.IsGlobalTemporaryStructure | TableOptions.IsTransactionTemporaryData)]
			TableOptions tableOptions)
		{
			TestTableOptions(context, tableOptions);
		}

		[Test]
		public void InformixTableOptionsTest(
			[IncludeDataSources(TestProvName.AllInformix)] string context,
			[Values(
				TableOptions.IsTemporary,
				TableOptions.IsTemporary |                                          TableOptions.IsLocalTemporaryData,
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure,
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData,
				                                                                    TableOptions.IsLocalTemporaryData,
				                           TableOptions.IsLocalTemporaryStructure,
				                           TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData)]
			TableOptions tableOptions)
		{
			TestTableOptions(context, tableOptions);
		}

		[Test]
		public void MySqlTableOptionsTest(
			[IncludeDataSources(TestProvName.AllMySql)] string context,
			[Values(
				TableOptions.IsTemporary,
				TableOptions.IsTemporary |                                          TableOptions.IsLocalTemporaryData,
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure,
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData,
				                                                                    TableOptions.IsLocalTemporaryData,
				                           TableOptions.IsLocalTemporaryStructure,
				                           TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData)]
			TableOptions tableOptions)
		{
			TestTableOptions(context, tableOptions);
		}

		[Test]
		public void OracleTableOptionsTest(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values(
				TableOptions.IsTemporary,
				TableOptions.IsTemporary |                                           TableOptions.IsLocalTemporaryData,
				TableOptions.IsTemporary | TableOptions.IsGlobalTemporaryStructure,
				TableOptions.IsTemporary | TableOptions.IsGlobalTemporaryStructure | TableOptions.IsLocalTemporaryData,
				                                                                     TableOptions.IsLocalTemporaryData,
				                                                                     TableOptions.IsTransactionTemporaryData,
				                           TableOptions.IsGlobalTemporaryStructure,
				                           TableOptions.IsGlobalTemporaryStructure | TableOptions.IsLocalTemporaryData,
				                           TableOptions.IsGlobalTemporaryStructure | TableOptions.IsTransactionTemporaryData)]
			TableOptions tableOptions)
		{
			TestTableOptions(context, tableOptions);
		}

		[Test]
		public void PostgreSQLTableOptionsTest(
			[IncludeDataSources(TestProvName.AllPostgreSQL)] string context,
			[Values(
				TableOptions.IsTemporary,
				TableOptions.IsTemporary |                                          TableOptions.IsLocalTemporaryData,
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure,
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData,
				                                                                    TableOptions.IsLocalTemporaryData,
				                                                                    TableOptions.IsTransactionTemporaryData,
				                           TableOptions.IsLocalTemporaryStructure,
				                           TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData,
				                           TableOptions.IsLocalTemporaryStructure | TableOptions.IsTransactionTemporaryData)]
			TableOptions tableOptions)
		{
			TestTableOptions(context, tableOptions);
		}

		[Test]
		public void SapHanaTableOptionsTest(
			[IncludeDataSources(TestProvName.AllSapHana)] string context,
			[Values(
				TableOptions.IsTemporary,
				TableOptions.IsTemporary |                                           TableOptions.IsLocalTemporaryData,
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure,
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure  | TableOptions.IsLocalTemporaryData,
				                                                                     TableOptions.IsLocalTemporaryData,
				                           TableOptions.IsLocalTemporaryStructure,
				                           TableOptions.IsLocalTemporaryStructure  | TableOptions.IsLocalTemporaryData,
				                           TableOptions.IsGlobalTemporaryStructure,
				                           TableOptions.IsGlobalTemporaryStructure | TableOptions.IsLocalTemporaryData)]
			TableOptions tableOptions)
		{
			TestTableOptions(context, tableOptions);
		}

		[Test]
		public void SQLiteTableOptionsTest(
			[IncludeDataSources(TestProvName.AllSQLite)] string context,
			[Values(
				TableOptions.IsTemporary,
				TableOptions.IsTemporary |                                          TableOptions.IsLocalTemporaryData,
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure,
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData,
				                                                                    TableOptions.IsLocalTemporaryData,
				                           TableOptions.IsLocalTemporaryStructure,
				                           TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData)]
			TableOptions tableOptions)
		{
			TestTableOptions(context, tableOptions);
		}

		[Test]
		public void SqlServerTableOptionsTest(
			[IncludeDataSources(TestProvName.AllSqlServer)] string context,
			[Values(
				TableOptions.IsTemporary,
				TableOptions.IsTemporary |                                           TableOptions.IsLocalTemporaryData,
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure,
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure  | TableOptions.IsLocalTemporaryData,
				                                                                     TableOptions.IsLocalTemporaryData,
				                           TableOptions.IsLocalTemporaryStructure,
				                           TableOptions.IsLocalTemporaryStructure  | TableOptions.IsLocalTemporaryData,
				                           TableOptions.IsGlobalTemporaryStructure,
				                           TableOptions.IsGlobalTemporaryStructure | TableOptions.IsGlobalTemporaryData)]
			TableOptions tableOptions)
		{
			TestTableOptions(context, tableOptions);
		}

		[Test]
		public void SybaseTableOptionsTest(
			[IncludeDataSources(TestProvName.AllSybase)] string context,
			[Values(
				TableOptions.IsTemporary,
				TableOptions.IsTemporary |                                           TableOptions.IsLocalTemporaryData,
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure,
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure  | TableOptions.IsLocalTemporaryData,
				                                                                     TableOptions.IsLocalTemporaryData,
				                           TableOptions.IsLocalTemporaryStructure,
				                           TableOptions.IsLocalTemporaryStructure  | TableOptions.IsLocalTemporaryData,
				                           TableOptions.IsGlobalTemporaryStructure,
				                           TableOptions.IsGlobalTemporaryStructure | TableOptions.IsGlobalTemporaryData)]
			TableOptions tableOptions)
		{
			TestTableOptions(context, tableOptions);
		}
	}
}
