using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Model;
using Tests.xUpdate;

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

		public const string NOT_SUPPORTED = TestProvName.AllAccess;

		[Test]
		public void Test_OneLineComment([DataSources(NOT_SUPPORTED)] string context)
		{
			var tag = "My Test";
			var expected = $"/* {tag} */{Environment.NewLine}";

			using (var db = GetDataContext(context))
			{
				var query =
					from x in db.Person.TagQuery(tag)
					select x;

				query.ToList();

				var commandSql = GetCurrentBaselines();

				Assert.That(commandSql.IndexOf(expected), Is.Not.EqualTo(-1));
			}
		}

		[Test]
		public void Test_CommentSanitation([DataSources(NOT_SUPPORTED)] string context)
		{
			var tag = "My */bad/* Test";
			var expected = $"/* {tag.Replace("/*", "").Replace("*/", "")} */{Environment.NewLine}";

			using (var db = GetDataContext(context))
			{
				var query =
					from x in db.Person.TagQuery(tag)
					select x;

				query.ToList();

				var commandSql = GetCurrentBaselines();

				Assert.That(commandSql.IndexOf(expected), Is.Not.EqualTo(-1));
			}
		}

		[Test]
		public void Test_FromVariable([DataSources(NOT_SUPPORTED)] string context, [Values("one", null, "two")] string tag)
		{
			var expected = $"/* {tag} */{Environment.NewLine}";

			using (var db = GetDataContext(context))
			{
				var query =
					from x in db.Person.TagQuery(tag ?? string.Empty)
					select x;

				query.ToList();

				var commandSql = GetCurrentBaselines();

				if (tag != null)
					Assert.That(commandSql.IndexOf(expected), Is.Not.EqualTo(-1));
				else
					Assert.That(commandSql.IndexOf(expected), Is.EqualTo(-1));
			}
		}

		[Test]
		public void Test_MultilineCommentsSupport([DataSources(NOT_SUPPORTED)] string context)
		{
			var tag = "My custom\r\nwonderful multiline\nquery tag";
			var expected = @$"/* My custom
wonderful multiline
query tag */{Environment.NewLine}";

			using (var db = GetDataContext(context))
			{
				var query =
					from x in db.Person.TagQuery(tag)
					select x;

				query.ToList();

				var commandSql = GetCurrentBaselines();

				Assert.That(commandSql.IndexOf(expected), Is.Not.EqualTo(-1));
			}
		}

		[Test]
		public void Test_Null([DataSources(NOT_SUPPORTED)] string context)
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
		public void Test_MultipleTags([DataSources(NOT_SUPPORTED)] string context)
		{
			var tag1 = "query 1";
			var tag2 = "query 2";
			var expected = $"/* {tag1}{Environment.NewLine}{tag2} */{Environment.NewLine}";

			using (var db = GetDataContext(context))
			{
				var query =
					from x in db.Person.TagQuery(tag1).TagQuery(tag2)
					select x;

				query.ToList();

				var commandSql = GetCurrentBaselines();

				Assert.That(commandSql.IndexOf(expected), Is.Not.EqualTo(-1));
			}
		}

		[Test]
		public void Test_CombinedQuery([DataSources(NOT_SUPPORTED)] string context)
		{
			var tag1 = "query 1";
			var tag2 = "query 2";
			var expected = $"/* {tag1}{Environment.NewLine}{tag2} */{Environment.NewLine}";

			using (var db = GetDataContext(context))
			{
				var query1 =
					from x in db.Person.Where(p => p.LastName == "a").TagQuery(tag1)
					select x;

				var query = query1.Where(p => p.FirstName == "a").TagQuery(tag2);

				query.ToList();

				var commandSql = GetCurrentBaselines();

				Assert.That(commandSql.IndexOf(expected), Is.Not.EqualTo(-1));
			}
		}

		[Test]
		public void Test_NoCommentsSupport([IncludeDataSources(true, NOT_SUPPORTED)] string context)
		{
			var tag = "My Test";

			using (var db = GetDataContext(context))
			{
				var query =
					from x in db.Person.TagQuery(tag)
					select x;

				query.ToList();

				var commandSql = GetCurrentBaselines();

				Assert.That(commandSql.IndexOf(tag), Is.EqualTo(-1));
			}
		}

		[Test]
		public void Test_TagInsertUpdateDeleteFlow([DataSources(false, NOT_SUPPORTED)] string context)
		{
			var tag = "Wonderful tag";
			var expected = $"/* {tag} */{Environment.NewLine}";

			using (var db = new TestDataConnection(context))
			{
				db.DropTable<TagTestTable>(throwExceptionIfNotExists: false);
				var table = db.CreateTable<TagTestTable>();

				var insertQuery = db.GetTable<TagTestTable>().TagQuery(tag)
								.Value(p => p.ID, 100)
								.Value(p => p.Name, "name");
				insertQuery.Insert();
				var insertCommandSql = db.LastQuery!;

				var updateQuery = db.GetTable<TagTestTable>().TagQuery(tag)
								.Where(p => p.ID == 100)
								.Set(p => p.Name, "updated");
				updateQuery.Update();
				var updateCommandSql = db.LastQuery!;

				var deleteQuery = db.GetTable<TagTestTable>().Where(p => p.ID == 100).TagQuery(tag);
				deleteQuery.Delete();
				var deleteCommandSql = db.LastQuery!;

				var truncateQuery = db.GetTable<TagTestTable>().TagQuery(tag);
				truncateQuery.Truncate();
				var truncateCommandSql = db.LastQuery!;

				Assert.That(insertCommandSql.IndexOf(expected), Is.EqualTo(0));
				Assert.That(updateCommandSql.IndexOf(expected), Is.EqualTo(0));
				Assert.That(deleteCommandSql.IndexOf(expected), Is.EqualTo(0));
				Assert.That(truncateCommandSql.IndexOf(expected), Is.EqualTo(0));

				db.DropTable<TagTestTable>();
			}
		}

		[Test]
		public void Test_SqlDropTableStatement([DataSources(NOT_SUPPORTED)] string context)
		{
			var tag = "My Test";
			var expected = $"/* {tag} */{Environment.NewLine}";

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestTable>())
			{
				table.TagQuery(tag).Drop();

				var commandSql = GetCurrentBaselines();

				Assert.That(commandSql.IndexOf(expected), Is.Not.EqualTo(-1));
			}
		}

		[Test]
		public void Test_SqlMergeStatement([MergeTests.MergeDataContextSource] string context)
		{
			var tag = "My Test";
			var expected = $"/* {tag} */{Environment.NewLine}";

			using (var db = GetDataContext(context))
			{
				db.GetTable<MergeTests.TestMapping1>().TableName("TestMerge1")
					.TagQuery(tag)
					.Merge()
					.Using(db.GetTable<MergeTests.TestMapping1>().TableName("TestMerge2"))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var commandSql = GetCurrentBaselines();

				Assert.That(commandSql.IndexOf(expected), Is.Not.EqualTo(-1));
			}
		}

		[Test]
		public void Test_SqlTruncateTableStatement([DataSources(NOT_SUPPORTED)] string context)
		{
			var tag = "My Test";
			var expected = $"/* {tag} */{Environment.NewLine}";

			using (var db    = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestTable>())
			{
				table.TagQuery(tag).Truncate();

				var commandSql = GetCurrentBaselines();

				Assert.That(commandSql.IndexOf(expected), Is.Not.EqualTo(-1));
			}
		}

		[Test]
		public void Test_SqlDeleteStatement([DataSources(NOT_SUPPORTED)] string context)
		{
			var tag = "My Test";
			var expected = $"/* {tag} */{Environment.NewLine}";

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestTable>())
			{
				table.TagQuery(tag).Delete();

				var commandSql = GetCurrentBaselines();

				Assert.That(commandSql.IndexOf(expected), Is.Not.EqualTo(-1));
			}
		}

		[Test]
		public void Test_SqlInsertStatement([DataSources(NOT_SUPPORTED)] string context)
		{
			var tag = "My Test";
			var expected = $"/* {tag} */{Environment.NewLine}";

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestTable>())
			{
				table.TagQuery(tag).Insert(() => new TestTable() { Id = 1 });

				var commandSql = GetCurrentBaselines();

				Assert.That(commandSql.IndexOf(expected), Is.Not.EqualTo(-1));
			}
		}

		[Test]
		public void Test_SqlUpdateStatement([DataSources(NOT_SUPPORTED)] string context)
		{
			var tag = "My Test";
			var expected = $"/* {tag} */{Environment.NewLine}";

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestTable>())
			{
				table.TagQuery(tag).Update(_ => new TestTable() { Id = 1 });

				var commandSql = GetCurrentBaselines();

				Assert.That(commandSql.IndexOf(expected), Is.Not.EqualTo(-1));
			}
		}

		[Test]
		public void Test_SqlSelectStatement([DataSources(NOT_SUPPORTED)] string context)
		{
			var tag = "My Test";
			var expected = $"/* {tag} */{Environment.NewLine}";

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestTable>())
			{
				table.TagQuery(tag).ToArray();

				var commandSql = GetCurrentBaselines();

				Assert.That(commandSql.IndexOf(expected), Is.Not.EqualTo(-1));
			}
		}

		[Test]
		public void Test_SqlInsertOrUpdateStatement([DataSources(NOT_SUPPORTED)] string context)
		{
			var tag = "My Test";
			var expected = $"/* {tag} */{Environment.NewLine}";

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestTable>())
			{
				table.TagQuery(tag).InsertOrUpdate(() => new TestTable() { Id = 1 }, _ => new TestTable() { Id = 1, Fd = 2 });

				var commandSql = GetCurrentBaselines();

				Assert.That(commandSql.IndexOf(expected), Is.Not.EqualTo(-1));
			}
		}

		[Table]
		public class TestTable
		{
			[PrimaryKey] public int  Id { get; set; }
			[Column    ] public int? Fd { get; set; }
		}

		// unteseted statements:
		// SqlCreateTableStatement : no API to attach tag
	}
}
