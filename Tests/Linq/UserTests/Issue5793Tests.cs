using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	/// <summary>
	/// Regression: a <c>LoadWith</c> filter lambda that closes over an <b>optional</b> value
	/// (<c>ledgers == null || ledgers.Contains(...)</c>) is not keyed on that closure state, so the
	/// first call to compile the query wins and every later call with different closure state
	/// silently re-executes the first call's plan.
	/// <para>
	/// Order-dependent, and wrong in both directions:
	/// unfiltered first -> the filtered call returns unfiltered rows (the filter is missing);
	/// filtered first   -> the unfiltered call gets <c>WHERE 1 = 0</c> and returns nothing.
	/// Both the eager-load command and the main query's <c>EXISTS</c> are affected.
	/// </para>
	/// <para>
	/// The same idiom in a plain top-level <c>Where</c> is handled correctly - see
	/// <see cref="OptionalFilterInTopLevelWhere"/>, which reuses one cached plan and re-renders the
	/// filter per execution. Correct in 6.1.0, broken since 6.2.0.
	/// </para>
	/// <see href="https://github.com/linq2db/linq2db/issues/5793"/>
	/// </summary>
	[TestFixture]
	public class Issue5793Tests : TestBase
	{
		[Table]
		sealed class Header
		{
			[PrimaryKey, Column(Length = 20, CanBeNull = false)] public string No { get; set; } = null!;

			[Association(ThisKey = nameof(No), OtherKey = nameof(Line.HeaderNo))]
			public List<Line>? Lines { get; set; }
		}

		[Table]
		sealed class Line
		{
			[PrimaryKey]                             public int    Id       { get; set; }
			[Column(Length = 20, CanBeNull = false)] public string HeaderNo { get; set; } = null!;
			[Column(Length = 20, CanBeNull = false)] public string Ledger   { get; set; } = null!;
		}

		static TempTable<Header> SetUpData(IDataContext db, out TempTable<Line> lines)
		{
			lines = db.CreateLocalTable(
			[
				new Line { Id = 1, HeaderNo = "H1", Ledger = "L1" },
				new Line { Id = 2, HeaderNo = "H1", Ledger = "L2" },
			]);

			return db.CreateLocalTable([new Header { No = "H1" }]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5793"), QueryCacheTest]
		public void OptionalFilterInLoadWith([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool filteredFirst)
		{
			using var db      = GetDataContext(context);
			using var headers = SetUpData(db, out var lines);
			using var _       = lines;

			// The two cases share the query shape, so the sibling case's cache entry would decide this one.
			Query.ClearCaches();

			if (filteredFirst)
			{
				Ledgers(["L1"]).ShouldBe(new[] { "L1" });
				Ledgers(null).ShouldBe(new[] { "L1", "L2" });
			}
			else
			{
				Ledgers(null).ShouldBe(new[] { "L1", "L2" });
				Ledgers(["L1"]).ShouldBe(new[] { "L1" });
			}

			// The captured set is optional: null means "no ledger filter".
			List<string> Ledgers(HashSet<string>? ledgers)
			{
				return db.GetTable<Header>()
					.LoadWith(h => h.Lines, qb => qb.Where(l => ledgers == null || ledgers.Contains(l.Ledger)))
					.Where(h => h.Lines!.Any())
					.ToList()
					.SelectMany(h => h.Lines ?? [])
					.Select(l => l.Ledger)
					.OrderBy(l => l)
					.ToList();
			}
		}

		/// <summary>
		/// Control for <see cref="OptionalFilterInLoadWith"/>: the same optional-filter idiom applied
		/// as a plain top-level <c>Where</c>. Both calls share one cached plan (the miss count does not
		/// move on the second call) and both are still correct, because the filter is re-rendered from
		/// the current closure on every execution.
		/// </summary>
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5793"), QueryCacheTest]
		public void OptionalFilterInTopLevelWhere([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool filteredFirst)
		{
			using var db      = GetDataContext(context);
			using var headers = SetUpData(db, out var lines);
			using var _       = lines;

			Query.ClearCaches();

			long missAfterFirst;

			if (filteredFirst)
			{
				Ledgers(["L1"]).ShouldBe(new[] { "L1" });
				missAfterFirst = lines.GetCacheMissCount();

				Ledgers(null).ShouldBe(new[] { "L1", "L2" });
			}
			else
			{
				Ledgers(null).ShouldBe(new[] { "L1", "L2" });
				missAfterFirst = lines.GetCacheMissCount();

				Ledgers(["L1"]).ShouldBe(new[] { "L1" });
			}

			// One compiled plan serves both closure states - correctness here does not come from
			// recompiling, which is what makes the LoadWith case above a genuine defect.
			lines.GetCacheMissCount().ShouldBe(missAfterFirst);
			missAfterFirst.ShouldBeGreaterThan(0);

			List<string> Ledgers(HashSet<string>? ledgers)
			{
				return db.GetTable<Line>()
					.Where(l => ledgers == null || ledgers.Contains(l.Ledger))
					.ToList()
					.Select(l => l.Ledger)
					.OrderBy(l => l)
					.ToList();
			}
		}
	}
}
