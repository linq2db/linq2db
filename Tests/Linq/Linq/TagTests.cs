using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using NUnit.Framework;
using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class TagTests : TestBase
	{
		class TagTestTable
		{
			public int ID { get; set; }
			public string? Name { get; set; }
		}

		[Test]
		public void Test_OneLineComment([DataSources(false)] string context)
		{
			var tag = "My Test";
			var expected = "-- " + tag + Environment.NewLine;

			using (var db = GetDataContext(context))
			{
				var query =
					from x in db.Person.TagQuery(tag)
					select x;

				query.ToList();

				var commandSql = ((DataConnection)db).LastQuery!;

				Assert.That(commandSql!.IndexOf(expected), Is.EqualTo(0));
			}
		}

		[Test]
		public void Test_MultilineCommentsSupport([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var tag = "My custom\r\nwonderful multiline\nquery tag";
			var expected = "/* My custom\r\nwonderful multiline\nquery tag */";

			using (var db = GetDataContext(context))
			{
				var query =
					from x in db.Person.TagQuery(tag)
					select x;

				query.ToList();

				var commandSql = ((DataConnection)db).LastQuery!;

				Assert.That(commandSql!.IndexOf(expected), Is.EqualTo(0));
			}
		}

		[Test]
		public void Test_Null([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				Assert.Throws<ArgumentNullException>(() =>
				{
					var query =
					from x in db.Person.TagQuery(null!)
					select x;
				});
			}
		}

		[Test]
		public void Test_MultipleTags([DataSources(false)] string context)
		{
			var tag1 = "query 1";
			var tag2 = "query 2";
			var expected = "-- " + tag1 + Environment.NewLine +
						   "-- " + tag2 + Environment.NewLine;

			using (var db = GetDataContext(context))
			{
				var query =
					from x in db.Person.TagQuery(tag1).TagQuery(tag2)
					select x;

				query.ToList();

				var commandSql = ((DataConnection)db).LastQuery!;

				Assert.That(commandSql!.IndexOf(expected), Is.EqualTo(0));
			}
		}

		[Test]
		public void Test_CombinedQuery([DataSources(false)] string context)
		{
			var tag1 = "query 1";
			var tag2 = "query 2";
			var expected = "-- " + tag1 + Environment.NewLine +
						   "-- " + tag2 + Environment.NewLine;

			using (var db = GetDataContext(context))
			{
				var query1 =
					from x in db.Person.Where(p => p.LastName == "a").TagQuery(tag1)
					select x;

				var query = query1.Where(p => p.FirstName == "a").TagQuery(tag2);

				query.ToList();

				var commandSql = ((DataConnection)db).LastQuery!;

				Assert.That(commandSql!.IndexOf(expected), Is.EqualTo(0));
			}
		}

		[Test]
		public void Test_NoCommentsSupport([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			var tag = "My Test";

			using (var db = GetDataContext(context))
			{
				var query =
					from x in db.Person.TagQuery(tag)
					select x;

				query.ToList();

				var commandSql = ((DataConnection)db).LastQuery!;

				Assert.That(commandSql!.IndexOf(tag), Is.EqualTo(-1));
			}
		}

		[Test]
		public void Test_TagInsertUpdateDeleteFlow([DataSources(false)] string context)
		{
			var tag = "Wonderful tag";
			var expected = "-- " + tag + Environment.NewLine;

			using (var db = GetDataContext(context))
			{
				db.DropTable<TagTestTable>(throwExceptionIfNotExists: false);
				var table = db.CreateTable<TagTestTable>();

				var insertQuery = db.GetTable<TagTestTable>().TagQuery(tag)
								.Value(p => p.ID, 100)
								.Value(p => p.Name, "name");
				insertQuery.Insert();
				var insertCommnadSql = ((DataConnection)db).LastQuery!;

				var updateQuery = db.GetTable<TagTestTable>().TagQuery(tag)
								.Where(p => p.ID == 100)
								.Set(p => p.Name, "updated");
				updateQuery.Update();
				var updateCommnadSql = ((DataConnection)db).LastQuery!;

				var deleteQuery = db.GetTable<TagTestTable>().Where(p => p.ID == 100).TagQuery(tag);
				deleteQuery.Delete();
				var deleteCommnadSql = ((DataConnection)db).LastQuery!;

				var truncateQuery = db.GetTable<TagTestTable>().TagQuery(tag);
				truncateQuery.Truncate();
				var truncateCommnadSql = ((DataConnection)db).LastQuery!;

				Assert.That(insertCommnadSql!.IndexOf(expected), Is.EqualTo(0));
				Assert.That(updateCommnadSql!.IndexOf(expected), Is.EqualTo(0));
				Assert.That(deleteCommnadSql!.IndexOf(expected), Is.EqualTo(0));
				Assert.That(truncateCommnadSql!.IndexOf(expected), Is.EqualTo(0));

				db.DropTable<TagTestTable>();
			}
		}
	}
}
