using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Extensions
{
	[TestFixture]
	public class SQLiteTests : TestBase
	{
		[Test]
		public void IndexedByTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person.TableHint(SQLiteHints.Hint.IndexedBy("IX_PersonDesc"))
				where p.ID > 0
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[Person] [p] INDEXED BY IX_PersonDesc"));
		}

		[Test]
		public void IndexedByTest2([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person.AsSQLite().IndexedByHint("IX_PersonDesc")
				where p.ID > 0
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[Person] [p] INDEXED BY IX_PersonDesc"));
		}

		[Test]
		public void NotIndexedTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person.TableHint(SQLiteHints.Hint.NotIndexed)
				where p.ID > 0
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[Person] [p] NOT INDEXED"));
		}

		[Test]
		public void NotIndexedTest2([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person.AsSQLite().NotIndexedHint()
				where p.ID > 0
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[Person] [p] NOT INDEXED"));
		}

		[Test]
		public void DeleteIndexedByTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			(
				from p in db.Person.TableHint(SQLiteHints.Hint.IndexedBy("IX_PersonDesc"))
				where p.ID > 1000000
				select p
			)
			.Delete();

			Assert.That(LastQuery, Contains.Substring("[Person] INDEXED BY IX_PersonDesc"));
		}

		[Test]
		public void UpdateIndexedByTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			(
				from p in db.Person.TableHint(SQLiteHints.Hint.IndexedBy("IX_PersonDesc"))
				where p.ID > 1000000
				select p
			)
			.Set(p => p.FirstName, "")
			.Update();

			Assert.That(LastQuery, Contains.Substring("[Person] INDEXED BY IX_PersonDesc"));
		}

		class GuidMapping
		{
			[Column]
			public Guid BlobGuid { get; set; }

			[Column(DbType = "TEXT")]
			public Guid TextGuid { get; set; }
		}

		[Test]
		public void GuidMappingTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			using var table = db.CreateLocalTable<GuidMapping>();

			var data = new []
			{
				new GuidMapping{BlobGuid = TestData.Guid1, TextGuid = TestData.Guid1}, 
				new GuidMapping{BlobGuid = TestData.Guid2, TextGuid = TestData.Guid2}, 
			};
			table.BulkCopy(data);

			var result = table.ToArray();

			AreEqual(data, result);
		}


	}
}
