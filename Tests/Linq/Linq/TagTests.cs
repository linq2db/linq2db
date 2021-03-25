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
		class TestTable
		{
			public int ID { get; set; }
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
		public void Test_MultipleTags([DataSources] string context)
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
		public void Test_CombinedQuery([DataSources] string context)
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
		public void Test_TagDelete([DataSources] string context)
		{
			var tag = "My Test";
			var expected = "-- " + tag + Environment.NewLine;

			using (var db = GetDataContext(context))
			{
				var query = db.Patient.Where(p => p.PersonID == -100).TagQuery(tag);
				query.Delete();

				var commandSql = ((DataConnection)db).LastQuery!;

				Assert.That(commandSql!.IndexOf(expected), Is.EqualTo(0));
			}
		}

		[Test]
		public void Test_TagUpdate([DataSources] string context)
		{
			var tag = "My Test";
			var expected = "-- " + tag + Environment.NewLine;

			using (var db = GetDataContext(context))
			{
				var query = db.Person.TagQuery(tag)
								.Where(p => p.LastName == "tag update test")
								.Set(p => p.LastName, "other name");

				query.Update();

				var commandSql = ((DataConnection)db).LastQuery!;

				Assert.That(commandSql!.IndexOf(expected), Is.EqualTo(0));
			}
		}

		[Test]
		public void Test_TagInsert([DataSources] string context)
		{
			var tag = "My Test";
			var expected = "-- " + tag + Environment.NewLine;

			using (var db = GetDataContext(context))
			{
				var query = db.Person.TagQuery(tag)
								.Value(p => p.LastName, "tag insert test")
								.Value(p => p.FirstName, "first name")
								.Value(p => p.Gender, Gender.Male);

				query.Insert();

				var commandSql = ((DataConnection)db).LastQuery!;

				// clean
				db.Person.Where(p => p.LastName == "tag insert test").Delete();

				Assert.That(commandSql!.IndexOf(expected), Is.EqualTo(0));
			}
		}

		[Test]
		public void Test_TagTruncate([DataSources] string context)
		{
			var tag = "My Test";
			var expected = "-- " + tag + Environment.NewLine;

			using (var db = GetDataContext(context))
			{
				db.DropTable<TestTable>(throwExceptionIfNotExists: false);

				var table = db.CreateTable<TestTable>();

				var query = db.GetTable<TestTable>().TagQuery(tag);

				query.Truncate();

				var commandSql = ((DataConnection)db).LastQuery!;

				Assert.That(commandSql!.IndexOf(expected), Is.EqualTo(0));
			}
		}
	}
}
