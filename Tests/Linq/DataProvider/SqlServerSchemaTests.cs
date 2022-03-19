using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Tools.DataProvider.SqlServer.Schemas;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class SqlServerSchemaTests : TestBase
	{
		[Test]
		public void DataLengthTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.DataLength("123"));
			Assert.That(result, Is.EqualTo(6));
		}

		[Test]
		public void DataLengthLTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.DataLengthL("123"));
			Assert.That(result, Is.EqualTo(6));
		}

		[Test]
		public void IdentityCurrentTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.IdentityCurrent("Person"));
			Assert.That(result, Is.GreaterThanOrEqualTo(0m));
		}

		[Test]
		public void IdentityIncrementTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.IdentityIncrement("Person"));
			Assert.That(result, Is.EqualTo(1m));
		}

		[Test]
		public void IdentitySeedTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.IdentitySeed("Person"));
			Assert.That(result, Is.EqualTo(1m));
		}

		[Test]
		public void AppNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.AppName());
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void ColumnLengthTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			var result = db.Select(() => Fn.ColumnLength("Person", "PersonID"));
			Assert.That(result, Is.EqualTo(4));

			result = db.Select(() => Fn.ColumnLength("Person", "ID"));
			Assert.That(result, Is.Null);
		}

		[Test]
		public void ColumnNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.ColumnName(Fn.ObjectID("dbo.Person", "U"), 1));
			Assert.That(result, Is.EqualTo("PersonID"));
		}

		[Test]
		public void ColumnPropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			foreach (var item in new[]
			{
				new { Parameter = Fn.ColumnPropertyName.AllowsNull, Result =  0 },
				new { Parameter = Fn.ColumnPropertyName.IsIdentity, Result =  1 },
				new { Parameter = Fn.ColumnPropertyName.Precision,  Result = 10 },
				new { Parameter = Fn.ColumnPropertyName.Scale,      Result =  0 },
			})
			{
				var result = db.Select(() => Fn.ColumnProperty(Fn.ObjectID("dbo.Person"), "PersonID", item.Parameter));
				Assert.That(result, Is.EqualTo(item.Result));
			}
		}

		[Test]
		public void DatabasePropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.DatabasePropertyEx(Fn.DbName(), Fn.DatabasePropertyName.Version));
			Assert.That(result, Is.GreaterThan(600));
		}

		[Test]
		public void DbIDTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.DbID("TestData"));
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void DbIDTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.DbID());
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void DbNameTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.DbName(Fn.DbID()));
			Assert.That(result, Is.EqualTo("TestData"));
		}

		[Test]
		public void DbNameTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.DbName());
			Assert.That(result, Is.EqualTo("TestData"));
		}

		[Test]
		public void FileIDTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			var file   = db.DatabasesAndFiles.DatabaseFiles.First();
			var result = db.Select(() => Fn.FileID(file.Name));

			Assert.That(result, Is.EqualTo(file.FileID));
		}

		[Test]
		public void FileIDExTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			var file   = db.DatabasesAndFiles.DatabaseFiles.First();
			var result = db.Select(() => Fn.FileIDEx(file.Name));

			Assert.That(result, Is.EqualTo(file.FileID));
		}

		[Test]
		public void FileNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			var file   = db.DatabasesAndFiles.DatabaseFiles.First();
			var result = db.Select(() => Fn.FileName(file.FileID));

			Assert.That(result, Is.EqualTo(file.Name));
		}

		[Test]
		public void FileGroupIDTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.FileGroupID("PRIMARY"));
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void FileGroupNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.FileGroupName(1));
			Assert.That(result, Is.EqualTo("PRIMARY"));
		}

		[Test]
		public void FileGroupPropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.FileGroupProperty("PRIMARY", Fn.FileGroupPropertyName.IsReadOnly));
			Assert.That(result, Is.EqualTo(0));
		}

		[Test]
		public void FilePropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			var file   = db.DatabasesAndFiles.DatabaseFiles.First();
			var result = db.Select(() => Fn.FileProperty(file.Name, Fn.FilePropertyName.IsPrimaryFile));

			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void FilePropertyExTest([IncludeDataSources(TestProvName.SqlAzure)] string context)
		{
			using var db = new SystemDB(context);

			var file   = db.DatabasesAndFiles.DatabaseFiles.First();
			var result = db.Select(() => Fn.FilePropertyEx(file.Name, Fn.FilePropertyExName.AccountType));

			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void FullTextServicePropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.FullTextServiceProperty(Fn.FullTextServicePropertyName.IsFulltextInstalled));
			Assert.That(result, Is.EqualTo(0).Or.EqualTo(1));
		}

		[Test]
		public void IndexColumnTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.IndexColumn("Person", 1, 1));
			Assert.That(result, Is.EqualTo("PersonID"));
		}

		[Test]
		public void IndexKeyPropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.IndexKeyProperty(Fn.ObjectID("Person", "U"), 1, 1, Fn.IndexKeyPropertyName.ColumnId));
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void IndexPropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => Fn.IndexProperty(Fn.ObjectID("dbo.Person"), "PK_Person", Fn.IndexPropertyName.IsClustered));
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void NextValueForTest([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => Fn.NextValueFor("dbo.TestSequence"));
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
					Sequence = Fn.NextValueForOver("dbo.TestSequence").OrderBy(p.ID).ThenByDesc(p.FirstName).ToValue(),
					p.ID
				};

			var l = q.ToList();

			Assert.That(l.Count, Is.GreaterThan(0));
		}
	}
}
