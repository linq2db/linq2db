using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;

using NUnit.Framework;

namespace Tests.Extensions
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
#pragma warning disable CS0618
				db.QueryHints.Add(SqlServerTools.Sql.OptionRecompile);
#pragma warning restore CS0618

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
#pragma warning disable CS0618
				db.NextQueryHints.Add(SqlServerTools.Sql.OptionRecompile);
#pragma warning restore CS0618

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
		public async Task Issue3137([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var _ = new DisableBaseline("multi-threading");
			var rnd = new Random();

			const int runs = 10;

			var tasks = new Task[runs];

			for (int i = 0; i < tasks.Length; i++)
				tasks[i] = Task.Run(execute);

			await Task.WhenAll(tasks);

			async Task execute()
			{
				// add uniqueness to hints to ensure no hints spilling between contexts through query cache
				using (var db = GetDataContext(context))
				{
					var corr = rnd.Next();
					var sharedHint  = $"-- many {corr}!";
					var oneTimeHint = $"-- once {corr}!";
					db.QueryHints.Add(sharedHint);
					db.NextQueryHints.Add(oneTimeHint);

					var query = db.Parent.Where(r => r.ParentID == 11);
					var sql = db is DataConnection ? null : query.ToString();
					await query.ToListAsync();
					if (db is DataConnection dc) sql = dc.LastQuery!;

					Assert.True(sql!.Contains(sharedHint), $"(1) expected {sharedHint}. Has alien hint: {sql.Contains("many")}");
					Assert.True(sql.Contains(oneTimeHint), $"(1) expected {oneTimeHint}. Has alien hint: {sql.Contains("once")}");

					query = db.Parent.Where(r => r.ParentID == 11);
					sql = db is DataConnection ? null : query.ToString();
					await query.ToListAsync();
					if (db is DataConnection dc2) sql = dc2.LastQuery!;

					Assert.True(sql!.Contains(sharedHint), $"(2) expected {sharedHint}. Has alien hint: {sql.Contains("many")}");
					Assert.False(sql.Contains(oneTimeHint), $"(2) expected no {oneTimeHint}");
					Assert.False(sql.Contains("once"), $"(2) alien one-time hint found");
				}
			}
		}
	}
}
