#if BUGCHECK
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Interceptors;
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

		sealed class CommandCounter : CommandInterceptor
		{
			public int Count { get; set; }

			public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
			{
				Count++;
				return command;
			}
		}

		static void Seed(IDataContext db)
		{
			db.Insert(new RcParent { Id = 1 });
			db.Insert(new RcChild { Id = 1, ParentId = 1, Name = "alpha" });
			db.Insert(new RcChild { Id = 2, ParentId = 1, Name = "beta"  });
		}

		// A parameter-dependent detail (string.Contains -> LIKE) forces a re-render on reuse. SQLite renders the combined
		// eager load as ONE concat command whose cache is all-or-nothing, so a single volatile step re-renders the whole
		// command - run 2 renders the SAME count as run 1 (hence <=, not <). What this pins: (a) a volatile step DOES
		// re-render (>= 1) and (b) caching never renders MORE than the first run; its companion StableEagerLoadFullyCached
		// shows a fully-stable load re-renders nothing (0). Statement-level granularity - a stable main staying cached while
		// only a volatile sibling re-renders - is a DbBatch-backend property and is not observable on this concat path.
		// QueryCacheTest: this asserts an exact GetCacheMissCount() delta, so it needs the globally-exclusive lane and the
		// entry-cap lifted. Without it a suite that has already filled the cache evicts the entry run 2 expects to hit,
		// which surfaces as one extra compilation.
		[Test, QueryCacheTest]
		public void ParameterDependentDetailReRenders([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			// Forced on: this fixture's whole subject is the COMBINED per-command render cache. With the policy switch off
			// (the 6.x default) the eager load runs sequentially and the render-count assertions still hold for unrelated
			// reasons, so Run() also checks that each load went out as one command.
			using var db       = GetDataContext(context, o => o.UseCombinedCommands(true));
			using var parents  = db.CreateLocalTable<RcParent>();
			using var children = db.CreateLocalTable<RcChild>();

			Seed(db);

			var counter = new CommandCounter();
			db.AddInterceptor(counter);

			var pattern = "al";

			// pattern is a captured variable => a SqlParameter, so Contains lowers to a parameter-dependent SearchString
			// (BasicSqlOptimizer treats a SearchString as parameter-dependent unless its argument is a literal SqlValue).
			List<RcParent> Run()
			{
				counter.Count = 0;

				var result = parents
					.LoadWith(p => p.Children, c => c.Where(x => x.Name!.Contains(pattern)))
					.ToList();

				counter.Count.ShouldBe(1);

				return result;
			}

			var missStart = parents.GetCacheMissCount();
			var baseline  = RenderDiagnostics.BuildSqlCount;

			Run();                                                                    // run 1: builds the query + renders all
			var afterFirstMiss = parents.GetCacheMissCount();
			var run1Renders    = RenderDiagnostics.BuildSqlCount - baseline;

			pattern = "be";                                                           // change ONLY the volatile parameter value
			var run2        = Run();                                                  // run 2: query-cache hit + partial render
			var run2Renders = RenderDiagnostics.BuildSqlCount - baseline - run1Renders;
			var run2Misses  = parents.GetCacheMissCount();

			afterFirstMiss.ShouldBeGreaterThan(missStart);                            // run 1 compiled the query (a cache miss)
			run2Misses.ShouldBe(afterFirstMiss);                                      // run 2 reused it (a cache hit) - precondition
			run1Renders.ShouldBeGreaterThanOrEqualTo(2);                              // run 1 rendered the main + the detail
			run2Renders.ShouldBeGreaterThanOrEqualTo(1);                              // the parameter-dependent detail re-rendered
			run2Renders.ShouldBeLessThanOrEqualTo(run1Renders);                      // concat cache is all-or-nothing: a volatile step re-renders the whole command (== run 1)

			run2.ShouldHaveSingleItem();
			run2[0].Children
				.Select(c => c.Name)
				.ShouldBe(new[] { "beta" });                                          // the LIKE filter actually applied
		}

#if SUPPORTS_DBBATCH
		// The DbBatch counterpart of ParameterDependentDetailReRenders: a batch caches each statement on its own, so run 2
		// re-renders only the volatile detail, where the concatenated command re-renders whole. No command interceptor here,
		// since one forces the concatenated backend; the command is read from tracing.
		[Test, QueryCacheTest]
		public void ParameterDependentDetailReRendersAlone(
			[IncludeDataSources(false, TestProvName.AllPostgreSQL, TestProvName.AllSqlServerMS)] string context,
			[Values] bool useDbBatch)
		{
			var commands = new List<string>();

			using var db = GetDataContext(context, o => o
				.UseCombinedCommands(true)
				.UseDbBatch(useDbBatch)
				.UseTracing(TraceLevel.Info, info =>
				{
					if (info.TraceInfoStep == TraceInfoStep.BeforeExecute && info.Operation == TraceOperation.ExecuteReader)
						commands.Add(info.SqlText);
				}));

			using var parents  = db.CreateLocalTable<RcParent>();
			using var children = db.CreateLocalTable<RcChild>();

			Seed(db);

			// The harness registers a command interceptor on the sequential-access configurations, which forces the
			// concatenated backend.
			var batch = useDbBatch
				&& !context.IsAnyOf(TestProvName.AllSqlServerSequentialAccess)
				&& ((DataConnection)db).OpenDbConnection().CanCreateBatch;

			var pattern = "al";

			List<RcParent> Run()
			{
				commands.Clear();

				var result = parents
					.LoadWith(p => p.Children, c => c.Where(x => x.Name!.Contains(pattern)))
					.ToList();

				commands
					.ShouldHaveSingleItem()
					.Contains("-- Command 2", StringComparison.Ordinal)
					.ShouldBe(batch);

				return result;
			}

			var missStart = parents.GetCacheMissCount();
			var baseline  = RenderDiagnostics.BuildSqlCount;

			Run();
			var afterFirstMiss = parents.GetCacheMissCount();
			var run1Renders    = RenderDiagnostics.BuildSqlCount - baseline;

			pattern = "be";
			var run2        = Run();
			var run2Renders = RenderDiagnostics.BuildSqlCount - baseline - run1Renders;
			var run2Misses  = parents.GetCacheMissCount();

			afterFirstMiss.ShouldBeGreaterThan(missStart);
			run2Misses.ShouldBe(afterFirstMiss);
			run1Renders.ShouldBeGreaterThanOrEqualTo(2);
			run2Renders.ShouldBe(batch ? 1 : run1Renders);

			run2.ShouldHaveSingleItem();
			run2[0].Children
				.Select(c => c.Name)
				.ShouldBe(new[] { "beta" });
		}
#endif

		// A fully stable eager load renders once and is fully reused - nothing re-renders on the second run. This is the
		// core invariant: a non-parameter-dependent statement is never re-rendered (holds on every backend).
		[Test, QueryCacheTest]
		public void StableEagerLoadFullyCached([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			// Forced on: this fixture's whole subject is the COMBINED per-command render cache. With the policy switch off
			// (the 6.x default) the eager load runs sequentially and the render-count assertions still hold for unrelated
			// reasons, so Run() also checks that each load went out as one command.
			using var db       = GetDataContext(context, o => o.UseCombinedCommands(true));
			using var parents  = db.CreateLocalTable<RcParent>();
			using var children = db.CreateLocalTable<RcChild>();

			Seed(db);

			var counter = new CommandCounter();
			db.AddInterceptor(counter);

			List<RcParent> Run()
			{
				counter.Count = 0;

				var result = parents
					.LoadWith(p => p.Children)
					.ToList();

				counter.Count.ShouldBe(1);

				return result;
			}

			Run();                                                                    // warm-up: render + cache

			var afterMiss = parents.GetCacheMissCount();
			var baseline  = RenderDiagnostics.BuildSqlCount;

			Run();                                                                    // second run

			var afterSecondRun = parents.GetCacheMissCount();

			afterSecondRun.ShouldBe(afterMiss);                                       // query-cache hit
			(RenderDiagnostics.BuildSqlCount - baseline).ShouldBe(0);                 // fully stable => nothing re-rendered
		}
	}
}
#endif
