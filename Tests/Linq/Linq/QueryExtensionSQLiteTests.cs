using System;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.SQLite;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public partial class QueryExtensionSQLiteTests : TestBase
	{
		[Test]
		public void IndexedByTest([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person.TableHint(SQLiteHints.TableHint.IndexedBy("IX_PersonDesc"))
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
				from p in db.Person.IndexedBy("IX_PersonDesc")
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
				from p in db.Person.TableHint(SQLiteHints.TableHint.NotIndexed)
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
				from p in db.Person.NotIndexed()
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
				from p in db.Person.TableHint(SQLiteHints.TableHint.IndexedBy("IX_PersonDesc"))
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
				from p in db.Person.TableHint(SQLiteHints.TableHint.IndexedBy("IX_PersonDesc"))
				where p.ID > 1000000
				select p
			)
			.Set(p => p.FirstName, "")
			.Update();

			Assert.That(LastQuery, Contains.Substring("[Person] INDEXED BY IX_PersonDesc"));
		}
	}
}
