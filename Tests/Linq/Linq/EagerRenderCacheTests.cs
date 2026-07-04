#if BUGCHECK
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	// BUGCHECK-only (mirrors QueryCacheEvictionTests): proves the per-command eager render cache. Reads two hooks:
	// Query<T>.CacheMissCount (the compiled query was reused - the precondition; if it missed, the whole query rebuilds and
	// a render-count delta is meaningless) and RenderDiagnostics.BuildSqlCount (top-level statements rendered this run).
	[TestFixture]
	public class EagerRenderCacheTests : TestBase
	{
		[Table]
		sealed class RcParent
		{
			[PrimaryKey] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(RcChild.ParentId))]
			public List<RcChild> Children { get; set; } = null!;
		}

		[Table]
		sealed class RcChild
		{
			[PrimaryKey] public int     Id       { get; set; }
			[Column    ] public int     ParentId { get; set; }
			[Column    ] public string? Name     { get; set; }
		}

		static void Seed(IDataContext db)
		{
			db.Insert(new RcParent { Id = 1 });
			db.Insert(new RcChild { Id = 1, ParentId = 1, Name = "alpha" });
			db.Insert(new RcChild { Id = 2, ParentId = 1, Name = "beta"  });
		}

		// A parameter-dependent detail (string.Contains -> LIKE) re-renders on reuse; caching never renders MORE than the
		// first run. Combined with StableEagerLoadFullyCached (stable => 0 re-renders), this proves per-command caching:
		// the stable main is cached while the volatile detail re-renders.
		[Test]
		public void ParameterDependentDetailReRenders([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db       = GetDataContext(context);
			using var parents  = db.CreateLocalTable<RcParent>();
			using var children = db.CreateLocalTable<RcChild>();

			Seed(db);

			var pattern = "al";

			// pattern is a captured variable => a SqlParameter, so Contains lowers to a parameter-dependent SearchString
			// (BasicSqlOptimizer treats a SearchString as parameter-dependent unless its argument is a literal SqlValue).
			List<RcParent> Run() => parents
				.LoadWith(p => p.Children, c => c.Where(x => x.Name!.Contains(pattern)))
				.ToList();

			var missStart = parents.GetCacheMissCount();
			var baseline  = RenderDiagnostics.BuildSqlCount;

			Run();                                                                    // run 1: builds the query + renders all
			var afterFirstMiss = parents.GetCacheMissCount();
			var run1Renders    = RenderDiagnostics.BuildSqlCount - baseline;

			pattern = "be";                                                           // change ONLY the volatile parameter value
			var run2        = Run();                                                  // run 2: query-cache hit + partial render
			var run2Renders = RenderDiagnostics.BuildSqlCount - baseline - run1Renders;

			afterFirstMiss.ShouldBeGreaterThan(missStart);                            // run 1 compiled the query (a cache miss)
			parents.GetCacheMissCount().ShouldBe(afterFirstMiss);                     // run 2 reused it (a cache hit) - precondition
			run1Renders.ShouldBeGreaterThanOrEqualTo(2);                              // run 1 rendered the main + the detail
			run2Renders.ShouldBeGreaterThanOrEqualTo(1);                              // the parameter-dependent detail re-rendered
			run2Renders.ShouldBeLessThanOrEqualTo(run1Renders);                      // caching never renders more

			run2.ShouldHaveSingleItem();
			run2[0].Children.Select(c => c.Name).ShouldBe(new[] { "beta" });          // the LIKE filter actually applied
		}

		// A fully stable eager load renders once and is fully reused - nothing re-renders on the second run. This is the
		// core invariant: a non-parameter-dependent statement is never re-rendered (holds on every backend).
		[Test]
		public void StableEagerLoadFullyCached([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db       = GetDataContext(context);
			using var parents  = db.CreateLocalTable<RcParent>();
			using var children = db.CreateLocalTable<RcChild>();

			Seed(db);

			List<RcParent> Run() => parents.LoadWith(p => p.Children).ToList();

			Run();                                                                    // warm-up: render + cache

			var afterMiss = parents.GetCacheMissCount();
			var baseline  = RenderDiagnostics.BuildSqlCount;

			Run();                                                                    // second run

			parents.GetCacheMissCount().ShouldBe(afterMiss);                          // query-cache hit
			(RenderDiagnostics.BuildSqlCount - baseline).ShouldBe(0);                 // fully stable => nothing re-rendered
		}
	}
}
#endif
