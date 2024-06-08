using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue4300Tests : TestBase
	{
		[Table("TABLE1")]
		public class Table1
		{
			[Column("ID1"), PrimaryKey, NotNull] public int     Id1   { get; set; }
			[Column("NAME1"), Nullable]          public string? Name1 { get; set; }
		}

		[Table("TABLE2")]
		public class Table2
		{
			[Column("ID2"), PrimaryKey, NotNull] public int Id2      { get; set; }
			[Column("TABLE1ID"), NotNull]        public int Table1Id { get; set; }
		}

		[Test]
		public void ExpressionCompileTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new Table1 { Id1 = 1, Name1 = "Some1" },
				new Table1 { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new Table2 { Id2 = 10, Table1Id = 1 },
				new Table2 { Id2 = 20, Table1Id = 2 },
			});

			TestMe("Some1", 10); // works correctly
			TestMe("Some2", 20); // fails with error that 10 was received instead of 20 (unless above line is commented out)
			
			void TestMe(string str, int expectedTable2Id)
			{
				Expression<Func<Table1, bool>> expr = t => t.Name1 == str;
				//var query1 = tbl1.Where(expr).Select(row1 => row1.Id1); //works
				var query1 = tbl1.Where(row1 => expr.Compile()(row1)).Select(row1 => row1.Id1); //doesn't work
				var query2 = tbl2.Where(row2 => query1.Contains(row2.Table1Id)).Select(row2 => row2.Id2);
				var ret    = query2.ToList();
				Assert.That(ret, Has.Count.EqualTo(1));
				Assert.That(ret[0], Is.EqualTo(expectedTable2Id));
			}
		}
	}
}
