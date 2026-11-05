using System;
using System.Linq;
using System.Threading;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class CriticProbeTests : TestBase
	{
		static void Dump(string label, IQueryable<int> q)
		{
			var sql = q.ToSqlQuery();
			TestContext.Out.WriteLine($"PROBE {label}: miss={q.GetCacheMissCount()} params={sql.Parameters.Count} sql={sql.Sql.Replace("\n", " ")}");
		}

		[Test, QueryCacheTest]
		public void BclStaticClock([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Today()  { return db.Types.Where(t => t.DateTimeValue < DateTime.Today).Select(t => t.ID); }
			IQueryable<int> Now()    { return db.Types.Where(t => t.DateTimeValue < DateTime.Now.AddSeconds(0)).Select(t => t.ID); }
			IQueryable<int> TodayAdd(){ return db.Types.Where(t => t.DateTimeValue < DateTime.Today.AddDays(1)).Select(t => t.ID); }
			IQueryable<int> NowProp(){ return db.Types.Where(t => t.DateTimeValue < DateTime.Now.Date).Select(t => t.ID); }
			IQueryable<int> Ticks()  { return db.Types.Where(t => t.ID < DateTime.Now.Millisecond + 100000).Select(t => t.ID); }
			IQueryable<int> Offset() { return db.Types.Where(t => t.DateTimeValue < DateTimeOffset.Now.DateTime).Select(t => t.ID); }

			foreach (var (name, f) in new (string, Func<IQueryable<int>>)[] { ("Today", Today), ("NowAdd0", Now), ("TodayAdd", TodayAdd), ("NowDate", NowProp), ("NowMs", Ticks), ("OffsetNowDateTime", Offset) })
			{
				f().ToArray();
				Dump(name + "#1", f());
				Thread.Sleep(1100);
				f().ToArray();
				Dump(name + "#2", f());
			}
		}

		[Test, QueryCacheTest]
		public void ClosureConditionalInExpose([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Q(bool flag) { return db.Person.Where(p => flag ? p.ID == 1 : p.ID == 2).Select(p => p.ID); }

			var r1 = Q(true).ToArray();
			Dump("cond-true", Q(true));
			var r2 = Q(false).ToArray();
			Dump("cond-false", Q(false));
			TestContext.Out.WriteLine($"PROBE cond rows true=[{string.Join(",", r1)}] false=[{string.Join(",", r2)}]");
			Assert.That(r1, Is.EqualTo(new[] { 1 }));
			Assert.That(r2, Is.EqualTo(new[] { 2 }));
		}
	}
}
