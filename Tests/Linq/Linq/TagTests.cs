using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.xUpdate;

namespace Tests.Linq
{
	[TestFixture]
	public class TagTests : TestBase
	{
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

				commandSql.ShouldContain(expected);
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

				commandSql.ShouldContain(expected);
			}
		}

		[Test]
		public void Test_FromVariable([DataSources(NOT_SUPPORTED)] string context, [Values("one", null, "two")] string? tag)
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
					commandSql.ShouldContain(expected);
				else
					commandSql.ShouldNotContain(expected);
			}
		}

		[Test]
		public void Test_MultilineCommentsSupport([DataSources(NOT_SUPPORTED)] string context)
		{
			var tag = "My custom\r\nwonderful multiline\nquery tag";
			var expected = @$"/* My custom{Environment.NewLine}wonderful multiline{Environment.NewLine}query tag */{Environment.NewLine}";

			using (var db = GetDataContext(context))
			{
				var query =
					from x in db.Person.TagQuery(tag)
					select x;

				query.ToList();

				var commandSql = GetCurrentBaselines();

				commandSql.ShouldContain(expected);
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

				commandSql.ShouldContain(expected);
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

				commandSql.ShouldContain(expected);
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

				commandSql.ShouldNotContain(tag);
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

				commandSql.ShouldContain(expected);
			}
		}

		[Test]
		public void Test_SqlMergeStatement([MergeDataContextSource] string context)
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

				commandSql.ShouldContain(expected);
			}
		}

		[Test]
		public void Test_SqlTruncateTableStatement([DataSources(NOT_SUPPORTED)] string context)
		{
			var tag = "My Test";
			var expected = $"/* {tag} */{Environment.NewLine}";

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestTable>())
			{
				table.TagQuery(tag).Truncate();

				var commandSql = GetCurrentBaselines();

				commandSql.ShouldContain(expected);
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

				commandSql.ShouldContain(expected);
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

				commandSql.ShouldContain(expected);
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
				table.TagQuery(tag).Update(_ => new TestTable() { Fd = 1 });

				var commandSql = GetCurrentBaselines();

				commandSql.ShouldContain(expected);
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

				commandSql.ShouldContain(expected);
			}
		}

		[Test]
		public void Test_SqlInsertOrUpdateStatement([DataSources(NOT_SUPPORTED, TestProvName.AllClickHouse)] string context)
		{
			var tag = "My Test";
			var expected = $"/* {tag} */{Environment.NewLine}";

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestTable>())
			{
				table.TagQuery(tag).InsertOrUpdate(() => new TestTable() { Id = 1, Fd = 2 }, _ => new TestTable() {  Fd = 2 });

				var commandSql = GetCurrentBaselines();

				commandSql.ShouldContain(expected);
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
