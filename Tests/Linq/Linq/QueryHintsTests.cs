using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;

using NUnit.Framework;
using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class QueryHintsTests : TestBase
	{
		[Test]
		public void Comment([DataSources(TestProvName.AllAccess, TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.QueryHints.Add("---");
				db.NextQueryHints.Add("----");

				var q = db.Parent.Select(p => p);

				var str = q.ToString();

				TestContext.WriteLine(str);

				Assert.That(str, Contains.Substring("---"));
				Assert.That(str, Contains.Substring("----"));

				var list = q.ToList();

				var ctx = db as DataConnection;

				if (ctx != null)
				{
					Assert.That(ctx.LastQuery, Contains.Substring("---"));
					Assert.That(ctx.LastQuery, Contains.Substring("----"));
				}

				str = q.ToString();

				TestContext.WriteLine(str);

				Assert.That(str, Contains.Substring("---"));
				Assert.That(str, Is.Not.Contains("----"));

				list = q.ToList();

				if (ctx != null)
				{
					Assert.That(ctx.LastQuery, Contains.Substring("---"));
					Assert.That(ctx.LastQuery, Is.Not.Contains("----"));
				}
			}
		}

		[Test]
		public void Option1([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.QueryHints.Add(SqlServerTools.Sql.OptionRecompile);

				var q = db.Parent.Select(p => p);

				var list = q.ToList();

				var ctx = db as DataConnection;

				if (ctx != null)
				{
					Assert.That(ctx.LastQuery, Contains.Substring("OPTION"));
				}

				list = q.ToList();

				if (ctx != null)
				{
					Assert.That(ctx.LastQuery, Contains.Substring("OPTION"));
				}
			}
		}

		[Test]
		public void Option2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.NextQueryHints.Add(SqlServerTools.Sql.OptionRecompile);

				var q = db.Parent.Select(p => p);

				var list = q.ToList();

				var ctx = db as DataConnection;

				if (ctx != null)
				{
					Assert.That(ctx.LastQuery, Contains.Substring("OPTION"));
				}

				list = q.ToList();

				if (ctx != null)
				{
					Assert.That(ctx.LastQuery, Is.Not.Contains("OPTION"));
				}
			}
		}

		[Repeat(100)]
		[Test]
		public async Task Issue3137([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var _ = new DisableBaseline("multi-threading");

			const int runs = 10;

			var tasks = new Task[runs];

			for (int i = 0; i < tasks.Length; i++)
				tasks[i] = Task.Run(execute);

			await Task.WhenAll(tasks);

			async Task execute()
			{
				using (var db = new TestDataConnection(context))
				{
					db.QueryHints.Add("-- many");
					db.NextQueryHints.Add("-- once");

					await db.Parent.Where(r => r.ParentID == 11).SingleOrDefaultAsync();
					var sql = db.LastQuery!;

					Assert.True(sql.Contains("-- many"));
					Assert.True(sql.Contains("-- once"));

					await db.Parent.Where(r => r.ParentID == 11).SingleOrDefaultAsync();
					sql = db.LastQuery!;

					Assert.True(sql.Contains("-- many"));
					Assert.False(sql.Contains("-- once"));
				}
			}
		}
	}
}
