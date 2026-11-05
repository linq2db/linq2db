using System;
using System.Globalization;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class CriticProbe2Tests : TestBase
	{
		static readonly int    _fixedId   = 2;
		static readonly string _fixedName = "John";

		static void Dump(string label, IQueryable<int> q)
		{
			var sql = q.ToSqlQuery();
			TestContext.Out.WriteLine($"PROBE {label}: miss={q.GetCacheMissCount()} params={sql.Parameters.Count} sql={sql.Sql.Replace("\n", " ")}");
		}

		[Test, QueryCacheTest]
		public void CultureFormat([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			IQueryable<int> Q() { return db.Person.Where(p => p.FirstName == string.Format("{0:N1}", 1234.5)).Select(p => p.ID); }
			IQueryable<int> Q2() { return db.Person.Where(p => p.FirstName == 1.5.ToString()).Select(p => p.ID); }
			IQueryable<int> Q3() { return db.Person.Where(p => p.FirstName == "v" + 1.5).Select(p => p.ID); }
			IQueryable<int> Q4() { return db.Person.Where(p => p.FirstName == (-5).ToString()).Select(p => p.ID); }
			var saved = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
				Q().ToArray(); Dump("fmt-en", Q()); Q2().ToArray(); Dump("tostr-en", Q2()); Q3().ToArray(); Dump("concat-en", Q3()); Q4().ToArray(); Dump("neg-en", Q4());
				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
				Q().ToArray(); Dump("fmt-de", Q()); Q2().ToArray(); Dump("tostr-de", Q2()); Q3().ToArray(); Dump("concat-de", Q3());
				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");
				Q4().ToArray(); Dump("neg-sv", Q4()); TestContext.Out.WriteLine("PROBE sv neg text=" + (-5).ToString() + " U+" + ((int)(-5).ToString()[0]).ToString("X4"));
			}
			finally { CultureInfo.CurrentCulture = saved; }
		}

		[Test, QueryCacheTest]
		public void CultureFirstSv([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			IQueryable<int> Q() { return db.Person.Where(p => p.FirstName == (-6).ToString()).Select(p => p.ID); }
			IQueryable<int> Q3() { return db.Person.Where(p => p.FirstName == "w" + 2.5).Select(p => p.ID); }
			var saved = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");
				Q().ToArray(); Dump("neg6-sv-first", Q()); Q3().ToArray(); Dump("concat25-sv-first", Q3());
				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
				Q().ToArray(); Dump("neg6-en-second", Q()); Q3().ToArray(); Dump("concat25-en-second", Q3());
			}
			finally { CultureInfo.CurrentCulture = saved; }
		}

		[Test, QueryCacheTest]
		public void StaticReadonlyScalar([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			IQueryable<int> Q() { return db.Person.Where(p => p.ID == _fixedId && p.FirstName == _fixedName).Select(p => p.ID); }
			Q().ToArray(); Dump("static-ro", Q());
		}
	}
}
