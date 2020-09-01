using System;
using System.Linq;

using LinqToDB;
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
		class IsTemporaryTable
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void IsTemporaryTest([DataSources(false)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateTempTable<IsTemporaryTable>();

			var result = table.ToArray();
		}

		[Table(TableOptions = TableOptions.IsGlobalTemporary)]
		class IsGlobalTemporaryTable
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void IsGlobalTemporaryTest([IncludeDataSources(
			ProviderName.Firebird,
			ProviderName.SqlServer2005,
			ProviderName.SqlServer2008,
			ProviderName.SqlServer2012,
			ProviderName.SqlServer2014,
			ProviderName.Sybase,
			ProviderName.SybaseManaged)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateTempTable<IsGlobalTemporaryTable>();

			var result = table.ToArray();
		}

		[Table(TableOptions = TableOptions.CreateIfNotExists)]
		class CreateIfNotExistsTable
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void CreateIfNotExistsTest([IncludeDataSources(
			ProviderName.Firebird,
			ProviderName.PostgreSQL,
			ProviderName.SQLiteClassic,
			ProviderName.SQLiteMS)] string context)
		{
			using var db    = GetDataContext(context);

			db.DropTable<CreateIfNotExistsTable>(throwExceptionIfNotExists:false);

			using var table = db.CreateTempTable<CreateIfNotExistsTable>();

			var result = table.ToArray();

			var table1 = db.CreateTempTable<CreateIfNotExistsTable>();
		}
	}
}
