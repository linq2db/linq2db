using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Tools;
using LinqToDB.Tools.DataProvider.SqlServer.Schemas;

using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Tests.DataProvider
{
	[TestFixture]
	public class SqlServerSchemaTests : TestBase
	{
		#region Data type

		[Test]
		public void DataLengthTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DataLength("123"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(6));
		}

		[Test]
		public void DataLengthLTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DataLengthL("123"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(6));
		}

		[Test]
		public void IdentityCurrentTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.IdentityCurrent("Person"));
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThanOrEqualTo(0m));
		}

		[Test]
		public void IdentityIncrementTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.IdentityIncrement("Person"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1m));
		}

		[Test]
		public void IdentitySeedTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.IdentitySeed("Person"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1m));
		}

		#endregion

		#region Logical

		[Test]
		public void ChooseTest([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			var b = "B";

			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Choose(2, "A", b, "C"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("B"));
		}

		[Test]
		public void IifTest([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Iif(Sql.AsSql(1) > 2, "A", "B"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("B"));
		}

		#endregion

		#region Metadata

		[Test]
		public void AppNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.AppName());
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void ColumnLengthTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			var result = db.Select(() => SqlFn.ColumnLength("Person", "PersonID"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(4));

			result = db.Select(() => SqlFn.ColumnLength("Person", "ID"));
			Console.WriteLine(result);
			Assert.That(result, Is.Null);
		}

		[Test]
		public void ColumnNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ColumnName(SqlFn.ObjectID("dbo.Person", "U"), 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("PersonID"));
		}

		[Test]
		public void ColumnPropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			foreach (var item in new[]
			{
				new { Parameter = SqlFn.ColumnPropertyName.AllowsNull, Result =  0 },
				new { Parameter = SqlFn.ColumnPropertyName.IsIdentity, Result =  1 },
				new { Parameter = SqlFn.ColumnPropertyName.Precision,  Result = 10 },
				new { Parameter = SqlFn.ColumnPropertyName.Scale,      Result =  0 },
			})
			{
				var result = db.Select(() => SqlFn.ColumnProperty(SqlFn.ObjectID("dbo.Person"), "PersonID", item.Parameter));
				Console.WriteLine(result);
				Assert.That(result, Is.EqualTo(item.Result));
			}
		}

		[Test]
		public void DatabasePropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DatabasePropertyEx(SqlFn.DbName(), SqlFn.DatabasePropertyName.Version));
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(600));
		}

		[Test]
		public void DbIDTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DbID("TestData"));
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void DbIDTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DbID());
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void DbNameTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DbName(SqlFn.DbID()));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("TestData"));
		}

		[Test]
		public void DbNameTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DbName());
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("TestData"));
		}

		[Test]
		public void FileIDTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			var file   = db.DatabasesAndFiles.DatabaseFiles.First();
			var result = db.Select(() => SqlFn.FileID(file.Name));

			Console.WriteLine(result);

			Assert.That(result, Is.EqualTo(file.FileID));
		}

		[Test]
		public void FileIDExTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			var file   = db.DatabasesAndFiles.DatabaseFiles.First();
			var result = db.Select(() => SqlFn.FileIDEx(file.Name));

			Console.WriteLine(result);

			Assert.That(result, Is.EqualTo(file.FileID));
		}

		[Test]
		public void FileNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			var file   = db.DatabasesAndFiles.DatabaseFiles.First();
			var result = db.Select(() => SqlFn.FileName(file.FileID));

			Console.WriteLine(result);

			Assert.That(result, Is.EqualTo(file.Name));
		}

		[Test]
		public void FileGroupIDTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.FileGroupID("PRIMARY"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void FileGroupNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.FileGroupName(1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("PRIMARY"));
		}

		[Test]
		public void FileGroupPropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.FileGroupProperty("PRIMARY", SqlFn.FileGroupPropertyName.IsReadOnly));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(0));
		}

		[Test]
		public void FilePropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			var file   = db.DatabasesAndFiles.DatabaseFiles.First();
			var result = db.Select(() => SqlFn.FileProperty(file.Name, SqlFn.FilePropertyName.IsPrimaryFile));

			Console.WriteLine(result);

			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void FilePropertyExTest([IncludeDataSources(TestProvName.SqlAzure)] string context)
		{
			using var db = new SystemDB(context);

			var file   = db.DatabasesAndFiles.DatabaseFiles.First();
			var result = db.Select(() => SqlFn.FilePropertyEx(file.Name, SqlFn.FilePropertyExName.AccountType));

			Console.WriteLine(result);

			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void FullTextServicePropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.FullTextServiceProperty(SqlFn.FullTextServicePropertyName.IsFulltextInstalled));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(0).Or.EqualTo(1));
		}

		[Test]
		public void IndexColumnTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.IndexColumn("Person", 1, 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("PersonID"));
		}

		[Test]
		public void IndexKeyPropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.IndexKeyProperty(SqlFn.ObjectID("Person", "U"), 1, 1, SqlFn.IndexKeyPropertyName.ColumnId));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void IndexPropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.IndexProperty(SqlFn.ObjectID("dbo.Person"), "PK_Person", SqlFn.IndexPropertyName.IsClustered));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void NextValueForTest([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.NextValueFor("dbo.TestSequence"));
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void NextValueForOverTest([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person
				select new
				{
					Sequence = SqlFn.NextValueForOver("dbo.TestSequence").OrderBy(p.ID).ThenByDesc(p.FirstName).ToValue(),
					p.ID
				};

			var l = q.ToList();

			Assert.That(l.Count, Is.GreaterThan(0));
		}

		[Test]
		public void ObjectDefinitionTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.ObjectDefinition(SqlFn.ObjectID("PersonSearch")));
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void ObjectNameTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ObjectName(SqlFn.ObjectID("dbo.Person")));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("Person"));
		}

		[Test]
		public void ObjectNameTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ObjectName(SqlFn.ObjectID("dbo.Person"), SqlFn.DbID("TestData")));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("Person"));
		}

		[Test]
		public void ObjectSchemaNameTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ObjectSchemaName(SqlFn.ObjectID("dbo.Person")));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("dbo"));
		}

		[Test]
		public void ObjectSchemaNameTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ObjectSchemaName(SqlFn.ObjectID("dbo.Person"), SqlFn.DbID("TestData")));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("dbo"));
		}

		[Test]
		public void ObjectPropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ObjectProperty(SqlFn.ObjectID("dbo.Person"), SqlFn.ObjectPropertyName.HasDeleteTrigger));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(0));
		}

		[Test]
		public void ObjectPropertyExTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ObjectPropertyEx(SqlFn.ObjectID("dbo.Person"), SqlFn.ObjectPropertyExName.IsTable));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void OriginalDbNameTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.OriginalDbName());
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("TestData"));
		}

		[Test]
		public void ParseNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ParseName("dbo.Person", 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("Person"));
		}

		[Test]
		public void SchemaIDNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.SchemaName(SqlFn.SchemaID("sys")));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("sys"));
		}

		[Test]
		public void SchemaNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.SchemaName());
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("dbo"));
		}

		[Test]
		public void SchemaIDTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.SchemaName(SqlFn.SchemaID()));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("dbo"));
		}

		[Test]
		public void ScopeIdentityTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ScopeIdentity());
			Assert.That(result, Is.EqualTo(0));
		}

		[Test]
		public void ServerPropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ServerProperty(SqlFn.ServerPropertyName.Edition));
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void StatsDateTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.StatsDate(SqlFn.ObjectID("dbo.Person"), 1));
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null.Or.Null);
		}

		[Test]
		public void TypeNameIDTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TypeName(SqlFn.TypeID("int")));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("int"));
		}

		[Test]
		public void TypePropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TypeProperty("int", SqlFn.TypePropertyName.Precision));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(10));
		}

		#endregion

		#region System

		[Test]
		public void IdentityTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Identity());
			Console.WriteLine(result);
			Assert.That(result, Is.Null);
		}

		[Test]
		public void PackReceivedTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.PackReceived());
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void TransactionCountTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TransactionCount());
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(0));
		}

		[Test]
		public void BinaryCheckSumTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person
				select SqlFn.BinaryCheckSum();

			var result = q.First();

			Console.WriteLine(result);
			Assert.That(result, Is.Not.EqualTo(0));
		}

		[Test]
		public void BinaryCheckSumTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person
				where p.ID == 1
				select SqlFn.BinaryCheckSum(p.ID, p.FirstName);

			var result = q.First();

			Console.WriteLine(result);
			Assert.That(result, Is.Not.EqualTo(0));
		}

		[Test]
		public void CheckSumTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person
				select SqlFn.CheckSum();

			var result = q.First();

			Console.WriteLine(result);
			Assert.That(result, Is.Not.EqualTo(0));
		}

		[Test]
		public void CheckSumTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person
				where p.ID == 1
				select SqlFn.CheckSum(p.ID, p.FirstName);

			var result = q.First();

			Console.WriteLine(result);
			Assert.That(result, Is.Not.EqualTo(0));
		}

		[Test]
		public void CompressTest1([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.Compress("ABC"));
			Console.WriteLine(result.ToDiagnosticString());
			Assert.That(result[0], Is.EqualTo(31));
		}

		[Test]
		public void CompressTest2([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.Compress(new byte[] { 1, 2, 3 }));
			Console.WriteLine(result.ToDiagnosticString());
			Assert.That(result[0], Is.EqualTo(31));
		}

		[Test]
		public void ConnectionPropertyTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.ConnectionProperty(SqlFn.ConnectionPropertyName.Net_Transport));
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void CurrentRequestIDTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.CurrentRequestID());
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void CurrentTransactionIDTest([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.CurrentTransactionID());
			Console.WriteLine(result);
			Assert.That(result, Is.Not.EqualTo(0));
		}

		[Test]
		public void DecompressTest([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => Sql.ConvertTo<string>.From(SqlFn.Decompress(new byte[]
			{
				31, 139, 8, 0, 0, 0, 0, 0, 4, 0, 115, 100, 112, 98, 112, 102, 0, 0, 26, 244, 143, 159, 6, 0, 0, 0

			})));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("ABC"));
		}

		[Test]
		public void FormatMessageTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.FormatMessage(20009, "ABC", "CBA"));
			Console.WriteLine(result);
			Assert.That(result, Contains.Substring("ABC").And.Contains("CBA"));
		}

		#endregion
	}
}
