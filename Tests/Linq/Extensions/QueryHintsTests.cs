using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Linq;
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

				TestContext.Out.WriteLine(str);

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

				TestContext.Out.WriteLine(str);

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

		[Repeat(100)]
		[Test]
		public void Issue3137([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var _ = new DisableBaseline("multi-threading");
			var rnd = new Random();

			const int runs = 10;

			var tasks = new Task[runs];

			for (int i = 0; i < tasks.Length; i++)
				tasks[i] = Task.Run(execute);

			Task.WaitAll(tasks);

			async Task execute()
			{
				// add uniqueness to hints to ensure no hints spilling between contexts through query cache
				using var db = GetDataContext(context);
				var corr = rnd.Next();
				var sharedHint  = $"-- many {corr}!";
				var oneTimeHint = $"-- once {corr}!";
				db.QueryHints.Add(sharedHint);
				db.NextQueryHints.Add(oneTimeHint);

				var query = db.Parent.Where(r => r.ParentID == 11);
				var sql = db is DataConnection ? null : query.ToString();
				await query.ToListAsync();
				if (db is DataConnection dc) sql = dc.LastQuery!;

				Assert.That(sql, Is.Not.Null);
				Assert.That(sql, Does.Contain(sharedHint), $"(1) expected {sharedHint}. Has alien hint: {sql.Contains("many")}");
				Assert.That(sql, Does.Contain(oneTimeHint), $"(1) expected {oneTimeHint}. Has alien hint: {sql.Contains("once")}");

				query = db.Parent.Where(r => r.ParentID == 11);
				sql = db is DataConnection ? null : query.ToString();
				await query.ToListAsync();
				if (db is DataConnection dc2) sql = dc2.LastQuery!;

				Assert.That(sql, Is.Not.Null);
				Assert.That(sql, Does.Contain(sharedHint), $"(2) expected {sharedHint}. Has alien hint: {sql.Contains("many")}");
				Assert.That(sql, Does.Not.Contain(oneTimeHint), $"(2) expected no {oneTimeHint}");
				Assert.That(sql, Does.Not.Contain("once"), $"(2) alien one-time hint found");
			}
		}
	}
}
