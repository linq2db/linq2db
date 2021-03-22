using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class TagTests : TestBase
	{
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
	}
}
