using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	/// <summary>
	/// Extension points a <see cref="DataConnection"/> subclass owns have to keep working when the combined-command
	/// engine is on. The combined path builds its own plan from the query's statement instead of going through
	/// <c>DataConnection.QueryRunner.GetCommand</c>, so everything <c>GetCommand</c> does on the way to the render is
	/// either reproduced there or explicitly opted out of - silently skipping it is the failure this fixture pins.
	/// </summary>
	[TestFixture]
	public class CombinedCommandsExtensibilityTests : TestBase
	{
		[Table]
		sealed class CeParent
		{
			[PrimaryKey] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(CeChild.ParentId))]
			public List<CeChild> Children { get; set; } = null!;
		}

		[Table]
		sealed class CeChild
		{
			[PrimaryKey] public int Id       { get; set; }
			[Column    ] public int ParentId { get; set; }
		}

		sealed class ProcessQueryCountingConnection : DataConnection
		{
			public ProcessQueryCountingConnection(DataOptions options) : base(options)
			{
			}

			public int ProcessQueryCount { get; set; }

			protected override SqlStatement ProcessQuery(SqlStatement statement, EvaluationContext context)
			{
				ProcessQueryCount++;
				return base.ProcessQuery(statement, context);
			}
		}

		sealed class SelectCounter : CommandInterceptor
		{
			public int Count { get; set; }

			public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
			{
				var sql = command.CommandText;

				if (sql.Contains("SELECT", StringComparison.OrdinalIgnoreCase)
					&& !sql.Contains("CREATE", StringComparison.OrdinalIgnoreCase)
					&& !sql.Contains("DROP",   StringComparison.OrdinalIgnoreCase)
					&& !sql.Contains("INSERT", StringComparison.OrdinalIgnoreCase))
				{
					Count++;
				}

				return command;
			}
		}

		// Anti-trivial guard for the test below: proves the option really does collapse this eager load into one
		// command on this provider, so a later "the override still saw everything" result cannot be satisfied by a
		// combined path that never engaged in the first place.
		[Test]
		public void CombinedEagerLoad_CollapsesToOneCommand([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db = new DataConnection(new DataOptions()
				.UseConfiguration(context)
				.UseCombinedCommands(true));

			var counter = new SelectCounter();
			db.AddInterceptor(counter);

			using var parents  = db.CreateLocalTable<CeParent>();
			using var children = db.CreateLocalTable<CeChild>();

			Seed(db);
			counter.Count = 0;

			parents.LoadWith(p => p.Children).ToList();

			counter.Count.ShouldBe(1);
		}

		// ProcessQuery is a shipped protected virtual extension point whose only caller is GetCommand. The combined
		// path renders from QueryInfo.Statement and each child's GetCombinableStatement() directly, so without a guard
		// a subclass's rewrite silently stops applying to the main query and every combinable child while still firing
		// for the non-combinable ones - a per-harvester split in behaviour with no diagnostic.
		[Test]
		public void ProcessQueryOverride_SeesEveryStatement_WhenCombining([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var sequential = RunAndCountProcessQuery(context, combined: false);
			var combined   = RunAndCountProcessQuery(context, combined: true);

			sequential.ShouldBeGreaterThan(1);  // the eager load really is more than one statement
			combined.ShouldBe(sequential);
		}

		static int RunAndCountProcessQuery(string context, bool combined)
		{
			using var db = new ProcessQueryCountingConnection(new DataOptions()
				.UseConfiguration(context)
				.UseCombinedCommands(combined));

			using var parents  = db.CreateLocalTable<CeParent>();
			using var children = db.CreateLocalTable<CeChild>();

			Seed(db);
			db.ProcessQueryCount = 0;

			var loaded = parents.LoadWith(p => p.Children).ToList();

			loaded.ShouldHaveSingleItem();
			loaded[0].Children.Count.ShouldBe(2);

			return db.ProcessQueryCount;
		}

		// A DataContext runs the combined eager load on the connection it holds. Inside a TransactionScope no implicit load
		// transaction holds that connection, so the enumeration has to: the nested runners of the harvesters and of the
		// main query would otherwise release it while the shared reader is still being read.
		[Test]
		public void DataContextCombinedEagerLoad_KeepsItsConnection([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var setup    = new DataConnection(context);
			using var parents  = setup.CreateLocalTable<CeParent>();
			using var children = setup.CreateLocalTable<CeChild>();

			Seed(setup);

			var counter = new SelectCounter();

			using var db = new DataContext(new DataOptions()
				.UseConfiguration(context)
				.UseCombinedCommands(true)
				.UseInterceptor(counter));

			List<CeParent> loaded;

			using (new TransactionScope())
				loaded = db.GetTable<CeParent>().LoadWith(p => p.Children).ToList();

			loaded.ShouldHaveSingleItem().Children.Count.ShouldBe(2);
			counter.Count.ShouldBe(1);
		}

		[Test]
		public async Task DataContextCombinedEagerLoad_KeepsItsConnectionAsync([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var setup    = new DataConnection(context);
			using var parents  = setup.CreateLocalTable<CeParent>();
			using var children = setup.CreateLocalTable<CeChild>();

			Seed(setup);

			var counter = new SelectCounter();

			await using var db = new DataContext(new DataOptions()
				.UseConfiguration(context)
				.UseCombinedCommands(true)
				.UseInterceptor(counter));

			List<CeParent> loaded;

			using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
				loaded = await db.GetTable<CeParent>().LoadWith(p => p.Children).ToListAsync();

			loaded.ShouldHaveSingleItem().Children.Count.ShouldBe(2);
			counter.Count.ShouldBe(1);
		}

		[Table]
		sealed class RcParent
		{
			[PrimaryKey] public int     Id    { get; set; }
			[Column    ] public string? Name  { get; set; }
			[Column    ] public int?    Value { get; set; }
		}

		[Table]
		sealed class RcChild
		{
			[PrimaryKey] public int     Id       { get; set; }
			[Column    ] public int     ParentId { get; set; }
			[Column    ] public string? Name     { get; set; }
			[Column    ] public bool?   Flag     { get; set; }
		}

		// Its own type gives it its own query-cache entry, so it renders the query from untouched statements.
		sealed class SeparateCacheConnection : DataConnection
		{
			public SeparateCacheConnection(DataOptions options) : base(options)
			{
			}
		}

		static IQueryable<RcResult> RcQuery(IDataContext db, int minValue, string pattern)
		{
			return
				from p in db.GetTable<RcParent>()
				where p.Value >= minValue || p.Name == null
				orderby p.Id
				select new RcResult
				{
					Id       = p.Id,
					IsNamed  = p.Name != null,
					Matching = db.GetTable<RcChild>()
						.Where(c => c.ParentId == p.Id && (c.Flag == true || c.Name!.Contains(pattern)))
						.OrderBy(c => c.Id)
						.Select(c => c.Id)
						.ToList(),
					Flagged  = db.GetTable<RcChild>()
						.Where(c => c.ParentId == p.Id && c.Flag != false)
						.OrderBy(c => c.Id)
						.Select(c => c.Id)
						.ToList(),
				};
		}

		static IQueryable<RcResult> RcGatedQuery(IDataContext db, Func<int, int> gate)
		{
			return
				from p in db.GetTable<RcParent>()
				where p.Value >= gate(1) || p.Name == null
				orderby p.Id
				select new RcResult
				{
					Id       = p.Id,
					IsNamed  = p.Name != null,
					Matching = db.GetTable<RcChild>()
						.Where(c => c.ParentId == p.Id && c.Flag == true)
						.OrderBy(c => c.Id)
						.Select(c => c.Id)
						.ToList(),
					Flagged  = db.GetTable<RcChild>()
						.Where(c => c.ParentId == p.Id && c.Flag != false)
						.OrderBy(c => c.Id)
						.Select(c => c.Id)
						.ToList(),
				};
		}

		sealed class RcResult
		{
			public int       Id       { get; set; }
			public bool      IsNamed  { get; set; }
			public List<int> Matching { get; set; } = null!;
			public List<int> Flagged  { get; set; } = null!;
		}

		static readonly RcParent[] RcParents =
		[
			new() { Id = 1, Name = "a",  Value = 5    },
			new() { Id = 2, Name = null, Value = null },
			new() { Id = 3, Name = "c",  Value = 0    },
		];

		static readonly RcChild[] RcChildren =
		[
			new() { Id = 1, ParentId = 1, Name = "xa", Flag = true  },
			new() { Id = 2, ParentId = 1, Name = "yy", Flag = null  },
			new() { Id = 3, ParentId = 2, Name = null, Flag = false },
			new() { Id = 4, ParentId = 2, Name = "za", Flag = null  },
		];

		static string Rows(IEnumerable<RcResult> rows)
		{
			return string.Join(
				"; ",
				rows.Select(r => $"{r.Id} {r.IsNamed} [{string.Join(",", r.Matching)}] [{string.Join(",", r.Flagged)}]"));
		}

		// A sequential run prepares the statements of the main query and of the children it runs unhinted in place; a
		// combined render of the same cached query then converts them a second time. That has to render what a combined
		// render of untouched statements renders.
		[Test]
		public void CombinedRenderAfterSequentialRunMatchesFreshRender(
			[IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllSqlServer, TestProvName.AllPostgreSQL, TestProvName.AllMySql)] string context)
		{
			var traces  = new List<string>();
			var options = new DataOptions()
				.UseConfiguration(context)
				.UseCombinedCommands(true)
				.UseTracing(TraceLevel.Info, info =>
				{
					if (info.TraceInfoStep == TraceInfoStep.BeforeExecute && info.Operation == TraceOperation.ExecuteReader)
						traces.Add(info.SqlText);
				});

			using var setup    = new DataConnection(context);
			using var parents  = setup.CreateLocalTable(RcParents);
			using var children = setup.CreateLocalTable(RcChildren);

			List<RcResult> expected;
			string         fresh;

			using (var db = new SeparateCacheConnection(options))
			{
				traces.Clear();

				expected = RcQuery(db, 1, "a")
					.ToList();

				fresh = traces.ShouldHaveSingleItem();
			}

			using (var db = new DataConnection(options))
			{
				db.NextQueryHints.Add("-- sequential run");

				traces.Clear();

				var sequential = RcQuery(db, 1, "a")
					.ToList();

				// The first command consumes the one-shot hint, so the second child and the main run unhinted and are
				// prepared in place; the first child, Matching, is parameter-dependent and is not.
				traces.Count.ShouldBe(3);
				traces[0].ShouldContain("-- sequential run");
				traces[1].ShouldNotContain("-- sequential run");
				traces[2].ShouldNotContain("-- sequential run");

				Rows(sequential)
					.ShouldBe(Rows(expected));

				traces.Clear();

				var combined = RcQuery(db, 1, "a")
					.ToList();

				traces
					.ShouldHaveSingleItem()
					.ShouldBe(fresh);

				Rows(combined)
					.ShouldBe(Rows(expected));
			}
		}

		// A combined execution parked inside BuildPlan - a client-side call in the main query's filter waits there - holds
		// the monitors of the main query and of its combinable children. A sequential execution of the same cached query on
		// another thread therefore runs no command until the combined one moves on: it waits at its first child's
		// GetCommand instead of preparing that child's statement while the combined render reads it. Each execution runs
		// in a TransactionScope so that no implicit load transaction holds a database lock the other would wait on.
		[Test]
		public void CombinedRenderHoldsChildMonitors([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var executedOn = new ConcurrentQueue<int>();
			var options    = new DataOptions()
				.UseConfiguration(context)
				.UseCombinedCommands(true)
				.UseTracing(TraceLevel.Info, info =>
				{
					if (info.TraceInfoStep == TraceInfoStep.BeforeExecute && info.Operation == TraceOperation.ExecuteReader)
						executedOn.Enqueue(Environment.CurrentManagedThreadId);
				});

			using var setup    = new DataConnection(context);
			using var parents  = setup.CreateLocalTable(RcParents);
			using var children = setup.CreateLocalTable(RcChildren);

			using var parked            = new ManualResetEventSlim();
			using var release           = new ManualResetEventSlim();
			using var sequentialStarted = new ManualResetEventSlim();

			var parkOnThread     = 0;
			var parkedInPlan     = false;
			var sequentialThread = 0;

			int Gate(int value)
			{
				if (Environment.CurrentManagedThreadId == Volatile.Read(ref parkOnThread))
				{
					Volatile.Write(ref parkOnThread, 0);

					parkedInPlan = new StackTrace()
						.GetFrames()
						.Any(f => f.GetMethod()?.Name == "BuildPlan");

					parked.Set();
					release.Wait();
				}

				return value;
			}

			List<RcResult> warmRows;

			using (var warm = new DataConnection(options))
			{
				warmRows = RcGatedQuery(warm, Gate)
					.ToList();
			}

			var expected = Rows(warmRows);

			Task<List<RcResult>>? combined   = null;
			Task<List<RcResult>>? sequential = null;

			try
			{
				combined = Task.Factory.StartNew(
					() =>
					{
						using var scope = new TransactionScope();
						using var db    = new DataConnection(options);

						Volatile.Write(ref parkOnThread, Environment.CurrentManagedThreadId);

						return RcGatedQuery(db, Gate)
							.ToList();
					},
					CancellationToken.None,
					TaskCreationOptions.LongRunning,
					TaskScheduler.Default);

				parked
					.Wait(TimeSpan.FromSeconds(30))
					.ShouldBeTrue();

				parkedInPlan.ShouldBeTrue();

				sequential = Task.Factory.StartNew(
					() =>
					{
						Volatile.Write(ref sequentialThread, Environment.CurrentManagedThreadId);
						sequentialStarted.Set();

						using var scope = new TransactionScope();
						using var db    = new DataConnection(options);

						db.NextQueryHints.Add("-- sequential run");

						return RcGatedQuery(db, Gate)
							.ToList();
					},
					CancellationToken.None,
					TaskCreationOptions.LongRunning,
					TaskScheduler.Default);

				sequentialStarted
					.Wait(TimeSpan.FromSeconds(30))
					.ShouldBeTrue();

				sequential
					.Wait(TimeSpan.FromSeconds(2))
					.ShouldBeFalse();

				executedOn
					.Count(id => id == Volatile.Read(ref sequentialThread))
					.ShouldBe(0);
			}
			finally
			{
				release.Set();

				foreach (var task in new[] { combined, sequential })
					task?.Wait(TimeSpan.FromSeconds(30));
			}

			Rows(combined!.Result)
				.ShouldBe(expected);

			Rows(sequential!.Result)
				.ShouldBe(expected);
		}

		static void Seed(DataConnection db)
		{
			db.Insert(new CeParent { Id = 1 });
			db.Insert(new CeChild { Id = 1, ParentId = 1 });
			db.Insert(new CeChild { Id = 2, ParentId = 1 });
		}
	}
}
