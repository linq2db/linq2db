using System;
using System.Linq;

using IBM.Data.DB2;

#if !NET46
using IBM.Data.DB2.Core;
#endif

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class TestTemplate : TestBase
	{
		[Table(IsTemporary = true)]
		[Table(IsTemporary = true, Configuration = ProviderName.SqlServer,  Database = "TestData", Schema = "TestSchema")]
		[Table(IsTemporary = true, Configuration = ProviderName.Sybase,     Database = "TestData")]
		[Table(IsTemporary = true, Configuration = ProviderName.SQLite)]
		[Table(IsTemporary = true, Configuration = ProviderName.PostgreSQL, Database = "TestData", Schema = "test_schema")]
		[Table(IsTemporary = true, Configuration = ProviderName.DB2, Schema = "SESSION")]
		class IsTemporaryTable
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void IsTemporaryTest([DataSources(false)] string context, [Values(true)] bool firstCall)
		{
			using var db = (DataConnection)GetDataContext(context);

			try
			{
				using var table = db.CreateTempTable<IsTemporaryTable>();

				var result = table.ToArray();
			}
			catch (DB2Exception ex) when (firstCall && ex.ErrorCode == -2147467259)
			{
//				db.Execute("DROP TABLESPACE DBHOSTTEMPU_32K;");
//				db.Execute("DROP TABLESPACE DBHOSTTEMPS_32K;");
//				db.Execute("DROP TABLESPACE DBHOST_32K;");
//				db.Execute("DROP BUFFERPOOL DBHOST_32K;");

				db.Execute("CREATE BUFFERPOOL DBHOST_32K IMMEDIATE SIZE 250 AUTOMATIC PAGESIZE 32K;");
				db.Execute("CREATE LARGE TABLESPACE DBHOST_32K PAGESIZE 32K MANAGED BY AUTOMATIC STORAGE EXTENTSIZE 32 PREFETCHSIZE 32 BUFFERPOOL DBHOST_32K;");
				db.Execute("CREATE USER TEMPORARY TABLESPACE DBHOSTTEMPU_32K PAGESIZE 32K MANAGED BY AUTOMATIC STORAGE BUFFERPOOL DBHOST_32K;");
				db.Execute("CREATE SYSTEM TEMPORARY TABLESPACE DBHOSTTEMPS_32K PAGESIZE 32K MANAGED BY AUTOMATIC STORAGE BUFFERPOOL DBHOST_32K;");

				IsGlobalTemporaryTest(context, false);
			}

		}

		[Table(TableOptions = TableOptions.IsGlobalTemporary)]
		[Table(TableOptions = TableOptions.IsGlobalTemporary, Configuration = ProviderName.DB2, Schema = "SESSION")]
		class IsGlobalTemporaryTable
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void IsGlobalTemporaryTest([IncludeDataSources(
			ProviderName.DB2,
			ProviderName.Firebird,
			ProviderName.SqlServer2005,
			ProviderName.SqlServer2008,
			ProviderName.SqlServer2012,
			ProviderName.SqlServer2014,
			ProviderName.Sybase,
			ProviderName.SybaseManaged)] string context,
			[Values(true)] bool firstCall)
		{
			using var db = (DataConnection)GetDataContext(context);

			try
			{
				using var table = db.CreateTempTable<IsGlobalTemporaryTable>();

				var result = table.ToArray();
			}
			catch (DB2Exception ex) when (firstCall && ex.ErrorCode == -2147467259)
			{
//				db.Execute("DROP TABLESPACE DBHOSTTEMPU_32K;");
//				db.Execute("DROP TABLESPACE DBHOSTTEMPS_32K;");
//				db.Execute("DROP TABLESPACE DBHOST_32K;");
//				db.Execute("DROP BUFFERPOOL DBHOST_32K;");

				db.Execute("CREATE BUFFERPOOL DBHOST_32K IMMEDIATE SIZE 250 AUTOMATIC PAGESIZE 32K;");
				db.Execute("CREATE LARGE TABLESPACE DBHOST_32K PAGESIZE 32K MANAGED BY AUTOMATIC STORAGE EXTENTSIZE 32 PREFETCHSIZE 32 BUFFERPOOL DBHOST_32K;");
				db.Execute("CREATE USER TEMPORARY TABLESPACE DBHOSTTEMPU_32K PAGESIZE 32K MANAGED BY AUTOMATIC STORAGE BUFFERPOOL DBHOST_32K;");
				db.Execute("CREATE SYSTEM TEMPORARY TABLESPACE DBHOSTTEMPS_32K PAGESIZE 32K MANAGED BY AUTOMATIC STORAGE BUFFERPOOL DBHOST_32K;");

				IsGlobalTemporaryTest(context, false);
			}
		}

		[Table(TableOptions = TableOptions.CreateIfNotExists)]
		class CreateIfNotExistsTable
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void CreateIfNotExistsTest([IncludeDataSources(
			ProviderName.DB2,
			ProviderName.Firebird,
			ProviderName.PostgreSQL,
			ProviderName.SQLiteClassic,
			ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);

			db.DropTable<CreateIfNotExistsTable>(throwExceptionIfNotExists:false);

			using var table = db.CreateTempTable<CreateIfNotExistsTable>();

			var result = table.ToArray();

			var table1 = db.CreateTempTable<CreateIfNotExistsTable>();
		}
	}
}
