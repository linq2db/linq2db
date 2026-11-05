using System;
using System.Globalization;
using System.Linq;
using System.Threading;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	// Probes for the folding rule: a value is folded into the SQL only when it is a function of the query
	// expression itself. Everything else - a clock, the ambient culture, a member of a mutable object - is a
	// parameter and is read on every execution through the same cached query.
	[TestFixture]
	public class CachePrincipleProbeTests : TestBase
	{
		static void Dump(string label, IQueryable<int> q)
		{
			var sql = q.ToSqlQuery();
			TestContext.Out.WriteLine($"PROBE {label}: miss={q.GetCacheMissCount()} params={sql.Parameters.Count} sql={sql.Sql.Replace("\n", " ")}");
		}

		sealed class Counter
		{
			public int Value;
		}

		static readonly Counter _counter = new();

		// A mutable field read through a static readonly object: the object is fixed, its field is not.
		[Test, QueryCacheTest]
		public void MutableFieldOfStaticReadonlyObjectIsParameter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query() { return db.Person.Where(p => p.ID == _counter.Value).Select(p => p.ID); }

			lock (_counter)
			{
				_counter.Value = 1;
				Query().ToArray().ShouldBe([1]);
				var missCount = Query().GetCacheMissCount();
				Dump("counter#1", Query());

				_counter.Value = 2;
				Query().ToArray().ShouldBe([2]);
				Dump("counter#2", Query());
				Query().GetCacheMissCount().ShouldBe(missCount);
			}
		}

		// The clock behind a static property: never folded, always the current value.
		[Test, QueryCacheTest]
		public void ClockIsParameter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query() { return db.Types.Where(t => t.ID < DateTime.Now.Millisecond + 100000).Select(t => t.ID); }

			Query().ToArray();
			var missCount = Query().GetCacheMissCount();
			var sql1      = Query().ToSqlQuery();
			Dump("clock#1", Query());

			Thread.Sleep(50);

			Query().ToArray();
			var sql2 = Query().ToSqlQuery();
			Dump("clock#2", Query());

			Query().GetCacheMissCount().ShouldBe(missCount);
			sql1.Parameters.Count.ShouldBe(1);
			sql2.Parameters.Count.ShouldBe(1);
		}

		// Culture-sensitive formatting over literals: the result depends on the ambient culture, not on the
		// expression. It stays a parameter, so a culture change is seen without a rebuild.
		[Test, QueryCacheTest]
		public void CultureSensitiveFormattingIsParameter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<string> Query()
			{
				return db.Person.Where(p => p.ID == 1).Select(p => Sql.AsSql((1234.5).ToString()));
			}

			var culture = CultureInfo.CurrentCulture;

			try
			{
				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
				Query().Single().ShouldBe("1234.5");
				var missCount = Query().GetCacheMissCount();

				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
				Query().Single().ShouldBe("1234,5");
				Query().GetCacheMissCount().ShouldBe(missCount);

				// An explicit culture makes the value a function of the expression: folded.
				db.Person.Where(p => p.ID == 1).Select(p => Sql.AsSql((1234.5).ToString(CultureInfo.InvariantCulture))).Single().ShouldBe("1234.5");
			}
			finally
			{
				CultureInfo.CurrentCulture = culture;
			}
		}
	}
}
