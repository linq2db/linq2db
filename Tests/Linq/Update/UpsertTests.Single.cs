using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.xUpdate
{
	/// <summary>
	/// End-to-end Upsert scenarios for the single-entity overload
	/// <c>Upsert&lt;T&gt;(ITable&lt;T&gt;, T, configure)</c>.
	/// </summary>
	[TestFixture]
	public partial class UpsertTests : TestBase
	{
		[Table("UpsertTest")]
		public sealed class UpsertRow
		{
			[PrimaryKey]                     public int       Id         { get; set; }
			[Column]                         public string    Name       { get; set; } = null!;
			[Column]                         public int       Version    { get; set; }
			[Column]                         public DateTime? CreatedAt  { get; set; }
			[Column]                         public string?   CreatedBy  { get; set; }
			[Column]                         public DateTime? UpdatedAt  { get; set; }
			[Column]                         public string?   UpdatedBy  { get; set; }
		}

		#region Phase 1 — whole-object upsert, root & per-branch Set/Ignore

		[Test]
		public void Single_Bare_Upsert([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1, Name = "a", Version = 1 });
			db.GetTable<UpsertRow>().Single(r => r.Id == 1).Name.ShouldBe("a");

			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1, Name = "b", Version = 2 });
			var row = db.GetTable<UpsertRow>().Single(r => r.Id == 1);
			row.Name   .ShouldBe("b");
			row.Version.ShouldBe(2);
		}

		[Test]
		public void Single_WithMatch_SameAsPK([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			db.GetTable<UpsertRow>().Upsert(
				new UpsertRow { Id = 1, Name = "m1", Version = 1 },
				u => u.Match((t, s) => t.Id == s.Id));

			db.GetTable<UpsertRow>().Upsert(
				new UpsertRow { Id = 1, Name = "m2", Version = 2 },
				u => u.Match((t, s) => t.Id == s.Id));

			db.GetTable<UpsertRow>().Single().Name.ShouldBe("m2");
		}

		[Test]
		public void Single_PerBranch_Set_Ignore([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			var insertTime = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
			var updateTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1, Name = "first", Version = 1 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Insert(i => i
					.Set(x => x.CreatedAt, () => insertTime)
					.Set(x => x.CreatedBy, _ => "system")
					.Ignore(x => x.UpdatedAt)
					.Ignore(x => x.UpdatedBy))
				.Update(v => v
					.Set(x => x.UpdatedAt, () => updateTime)
					.Set(x => x.UpdatedBy, _ => "system")
					.Ignore(x => x.CreatedAt)
					.Ignore(x => x.CreatedBy)));

			var afterInsert = db.GetTable<UpsertRow>().Single(r => r.Id == 1);
			afterInsert.Name     .ShouldBe("first");
			afterInsert.CreatedAt.ShouldBe(insertTime);
			afterInsert.CreatedBy.ShouldBe("system");
			afterInsert.UpdatedAt.ShouldBeNull();
			afterInsert.UpdatedBy.ShouldBeNull();

			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1, Name = "second", Version = 2 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Insert(i => i
					.Set(x => x.CreatedAt, () => insertTime)
					.Set(x => x.CreatedBy, _ => "system")
					.Ignore(x => x.UpdatedAt)
					.Ignore(x => x.UpdatedBy))
				.Update(v => v
					.Set(x => x.UpdatedAt, () => updateTime)
					.Set(x => x.UpdatedBy, _ => "system")
					.Ignore(x => x.CreatedAt)
					.Ignore(x => x.CreatedBy)));

			var afterUpdate = db.GetTable<UpsertRow>().Single(r => r.Id == 1);
			afterUpdate.Name     .ShouldBe("second");
			afterUpdate.CreatedAt.ShouldBe(insertTime, "CreatedAt must survive UPDATE");
			afterUpdate.CreatedBy.ShouldBe("system");
			afterUpdate.UpdatedAt.ShouldBe(updateTime);
			afterUpdate.UpdatedBy.ShouldBe("system");
		}

		[Test]
		public void Single_Root_Set_Ignore([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			var modified = new DateTime(2026, 2, 2, 9, 0, 0, DateTimeKind.Utc);

			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1, Name = "root-ins", Version = 1 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Set(x => x.UpdatedAt, () => modified)
				.Set(x => x.UpdatedBy, s => "sys-" + s.Name)
				.Ignore(x => x.CreatedBy));

			var afterInsert = db.GetTable<UpsertRow>().Single();
			afterInsert.Name     .ShouldBe("root-ins");
			afterInsert.UpdatedAt.ShouldBe(modified);
			afterInsert.UpdatedBy.ShouldBe("sys-root-ins");
			afterInsert.CreatedBy.ShouldBeNull();

			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1, Name = "root-upd", Version = 2 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Set(x => x.UpdatedAt, () => modified)
				.Set(x => x.UpdatedBy, s => "sys-" + s.Name)
				.Ignore(x => x.CreatedBy));

			db.GetTable<UpsertRow>().Single().UpdatedBy.ShouldBe("sys-root-upd");
		}

		[Test]
		public void Single_Set_AcceptsSqlServerSideExpression([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<UpsertRow>();

			// .Set accepts any SQL-translatable expression on the setter body — not just constants.
			// Sql.CurrentTimestamp throws ServerSideOnlyException if evaluated client-side, so a
			// non-null DateTime after the round-trip proves the expression was emitted as SQL.

			// Insert branch (no-source overload).
			table.Upsert(new UpsertRow { Id = 1, Name = "ts-ins", Version = 1 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Insert(i => i.Set(x => x.CreatedAt, () => Sql.CurrentTimestamp)));

			table.Single(r => r.Id == 1).CreatedAt.ShouldNotBeNull();

			// Update branch (no-source overload).
			table.Upsert(new UpsertRow { Id = 1, Name = "ts-upd", Version = 2 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Update(v => v.Set(x => x.UpdatedAt, () => Sql.CurrentTimestamp)));

			table.Single(r => r.Id == 1).UpdatedAt.ShouldNotBeNull();

			// Root-level .Set (applies to both branches; the INSERT branch already happened above,
			// so this second root-Set pass exercises the UPDATE path on a fresh column).
			table.Upsert(new UpsertRow { Id = 2, Name = "root-ts", Version = 1 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Set(x => x.CreatedAt, () => Sql.CurrentTimestamp));

			table.Single(r => r.Id == 2).CreatedAt.ShouldNotBeNull();
		}

		[Test]
		public void Single_Set_ServerSideDateTimeNow([IncludeDataSources(TestProvName.AllDuckDB)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new UpsertRow { Id = 1, Name = "seed", Version = 1 } });

			// Server-side DateTime.Now (DuckDB LOCALTIMESTAMP / current_localtimestamp()) lands in the
			// ON CONFLICT ... DO UPDATE SET branch — regression for the bare-keyword binder bug.
			table.Upsert(new UpsertRow { Id = 1, Name = "upd", Version = 2 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Update(v => v.Set(x => x.UpdatedAt, () => Sql.AsSql(DateTime.Now))));

			table.Single(r => r.Id == 1).UpdatedAt.ShouldNotBeNull();
		}

		[Test]
		public void Single_Update_Set_UsesBothTargetAndSource([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new UpsertRow { Id = 1, Name = "seed", Version = 10 } });

			// UPDATE setter references BOTH the existing target row and the incoming source row —
			// exercises the (t, s) => ... overload of IEntityUpdateSpec.Set.
			table.Upsert(new UpsertRow { Id = 1, Name = "inc", Version = 3 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Update(v => v.Set(x => x.Version, (t, s) => t.Version + s.Version)));

			table.Single(r => r.Id == 1).Version.ShouldBe(13); // 10 + 3
		}

		[Test]
		public void Single_FluentChain_LastSetWins([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<UpsertRow>();

			// Regression test for fluent-chain order: chaining Set on the same column at root level
			// AND inside a per-branch Insert(...) configure must preserve the user's call order —
			// the LAST call wins, matching normal fluent semantics. Both root and per-branch paths
			// go through different walkers (UpsertBuilder.WalkRoot vs EntityBuilderParser.Parse),
			// so this test exercises both at once.
			table.Upsert(new UpsertRow { Id = 1, Name = "row", Version = 1 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Set   (x => x.CreatedBy, () => "first-root")
				.Set   (x => x.CreatedBy, () => "second-root")          // root-chain duplicate; second-root wins
				.Insert(b => b
					.Set(x => x.UpdatedBy, () => "first-branch")
					.Set(x => x.UpdatedBy, () => "second-branch")));    // branch-chain duplicate; second-branch wins

			var row = table.Single(r => r.Id == 1);
			row.CreatedBy.ShouldBe("second-root");
			row.UpdatedBy.ShouldBe("second-branch");
		}

		#endregion

		#region Phase 2 — SkipUpdate / Update.DoNothing / Update.When (native path)

		[Test]
		public void Single_InsertIfNotExists_SkipUpdate([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new UpsertRow { Id = 1, Name = "original", Version = 1 } });

			table.Upsert(new UpsertRow { Id = 1, Name = "replaced", Version = 99 },
				u => u.Match((t, s) => t.Id == s.Id).SkipUpdate());

			var row = table.Single(r => r.Id == 1);
			row.Name   .ShouldBe("original");
			row.Version.ShouldBe(1);

			table.Upsert(new UpsertRow { Id = 2, Name = "fresh", Version = 1 },
				u => u.Match((t, s) => t.Id == s.Id).SkipUpdate());

			table.Count().ShouldBe(2);
			table.Single(r => r.Id == 2).Name.ShouldBe("fresh");
		}

		[Test]
		public void Single_InsertIfNotExists_UpdateDoNothing([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new UpsertRow { Id = 1, Name = "original", Version = 1 } });

			table.Upsert(new UpsertRow { Id = 1, Name = "replaced", Version = 99 },
				u => u.Match((t, s) => t.Id == s.Id).Update(v => v.DoNothing()));

			table.Single(r => r.Id == 1).Name.ShouldBe("original");
		}

		[Test]
		public void Single_ConditionalUpdate_When([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new UpsertRow { Id = 1, Name = "a", Version = 5 } });

			// Older source: .When guards the update — row stays "a".
			table.Upsert(new UpsertRow { Id = 1, Name = "stale", Version = 3 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Update(v => v.When((t, s) => s.Version > t.Version).Set(x => x.Name, s => s.Name)));

			table.Single(r => r.Id == 1).Name.ShouldBe("a");

			// Newer source: .When holds — row becomes "fresh".
			table.Upsert(new UpsertRow { Id = 1, Name = "fresh", Version = 10 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Update(v => v.When((t, s) => s.Version > t.Version).Set(x => x.Name, s => s.Name)));

			table.Single(r => r.Id == 1).Name.ShouldBe("fresh");
		}

		#endregion

		#region Phase 3 — MERGE lowering (SkipInsert / InsertWhen / non-PK match)

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSapHana, TestProvName.AllSqlServer2005, TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus,
			TestProvName.AllMySql, TestProvName.AllSqlCe, TestProvName.AllAccess,
			ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public void Single_UpdateIfExists_SkipInsert_EmptyTable([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1, Name = "x", Version = 1 },
				u => u.Match((t, s) => t.Id == s.Id).SkipInsert().Update(v => v.Set(x => x.Name, s => s.Name)));

			db.GetTable<UpsertRow>().Any(r => r.Id == 1).ShouldBeFalse();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSapHana, TestProvName.AllSqlServer2005, TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus,
			TestProvName.AllMySql, TestProvName.AllSqlCe, TestProvName.AllAccess,
			ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public void Single_UpdateIfExists_SkipInsert_Existing([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new UpsertRow { Id = 1, Name = "seed", Version = 1 } });

			table.Upsert(new UpsertRow { Id = 1, Name = "updated", Version = 2 },
				u => u.Match((t, s) => t.Id == s.Id).SkipInsert().Update(v => v.Set(x => x.Name, s => s.Name)));

			table.Single(r => r.Id == 1).Name.ShouldBe("updated");
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSapHana, TestProvName.AllSqlServer2005, TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus,
			TestProvName.AllMySql, TestProvName.AllSqlCe, TestProvName.AllAccess,
			ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		[ThrowsForProvider(typeof(LinqToDBException), ProviderName.Firebird25, TestProvName.AllInformix,
			ErrorMessage = ErrorHelper.Error_Upsert_MergeWithPredicate_NotSupported)]
		public void Single_ConditionalInsert_When([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1, Name = "skip", Version = 0 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Insert(i => i.When(s => s.Version > 0)));

			db.GetTable<UpsertRow>().Any().ShouldBeFalse();

			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 2, Name = "keep", Version = 5 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Insert(i => i.When(s => s.Version > 0)));

			db.GetTable<UpsertRow>().Single().Name.ShouldBe("keep");
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSapHana, TestProvName.AllSqlServer2005, TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus,
			TestProvName.AllMySql, TestProvName.AllSqlCe, TestProvName.AllAccess,
			ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public void Single_Match_OnNonPKColumn([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[]
			{
				new UpsertRow { Id = 1, Name = "alice", Version = 1 },
				new UpsertRow { Id = 2, Name = "bob",   Version = 1 },
			});

			table.Upsert(new UpsertRow { Id = 99, Name = "alice", Version = 42 },
				u => u.Match((t, s) => t.Name == s.Name).Update(v => v.Set(x => x.Version, s => s.Version)));

			table.Single(r => r.Name == "alice").Version.ShouldBe(42);
			table.Single(r => r.Name == "bob").Version.ShouldBe(1);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSapHana, TestProvName.AllSqlServer2005, TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus,
			TestProvName.AllMySql, TestProvName.AllSqlCe, TestProvName.AllAccess,
			ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public void Single_Insert_DoNothing([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new UpsertRow { Id = 1, Name = "seed", Version = 1 } });

			// .Insert(i => i.DoNothing()) — update-only, same effect as root .SkipInsert() but via the
			// branch-level builder. No-op when the row is missing.
			table.Upsert(new UpsertRow { Id = 2, Name = "ignored", Version = 1 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Insert(i => i.DoNothing())
				.Update(v => v.Set(x => x.Name, s => s.Name)));

			table.Count().ShouldBe(1);
			table.Single(r => r.Id == 1).Name.ShouldBe("seed");

			// Existing row → update runs.
			table.Upsert(new UpsertRow { Id = 1, Name = "updated", Version = 2 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Insert(i => i.DoNothing())
				.Update(v => v.Set(x => x.Name, s => s.Name)));

			table.Single(r => r.Id == 1).Name.ShouldBe("updated");
		}

		#endregion

		#region Async entry methods

		[Test]
		public async Task Single_Async_Bare_Upsert([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<UpsertRow>();

			await table.UpsertAsync(new UpsertRow { Id = 1, Name = "a", Version = 1 });
			(await table.SingleAsync(r => r.Id == 1)).Name.ShouldBe("a");

			await table.UpsertAsync(new UpsertRow { Id = 1, Name = "b", Version = 2 });
			(await table.SingleAsync(r => r.Id == 1)).Name.ShouldBe("b");
		}

		[Test]
		public async Task Single_Async_Upsert([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<UpsertRow>();

			await table.UpsertAsync(new UpsertRow { Id = 1, Name = "a", Version = 1 },
				u => u.Match((t, s) => t.Id == s.Id));
			(await table.SingleAsync(r => r.Id == 1)).Name.ShouldBe("a");

			await table.UpsertAsync(new UpsertRow { Id = 1, Name = "b", Version = 2 },
				u => u.Match((t, s) => t.Id == s.Id));
			(await table.SingleAsync(r => r.Id == 1)).Name.ShouldBe("b");
		}

		#endregion

		#region LinqOptions.UpsertEmulationPolicy

		[Test]
		public void Single_UpsertEmulationPolicyThrow_ForcesException(
			// MySQL / MariaDB cannot carry an UPDATE predicate natively (ON DUPLICATE KEY UPDATE
			// has no WHERE), so .Update(v => v.When(…)) routes to the emulation path — perfect
			// fixture to prove the opt-in throw works.
			[IncludeDataSources(TestProvName.AllMySql, TestProvName.AllMariaDB)] string context)
		{
			using var db = GetDataContext(context,
				o => o.WithOptions<LinqOptions>(lo => lo with { UpsertEmulationPolicy = UpsertEmulationPolicy.Throw }));
			using var table = db.CreateLocalTable(new[] { new UpsertRow { Id = 1, Name = "seed", Version = 5 } });

			Action act = () =>
				table.Upsert(new UpsertRow { Id = 1, Name = "x", Version = 3 }, u => u
					.Match((t, s) => t.Id == s.Id)
					.Update(v => v.When((t, s) => s.Version > t.Version).Set(x => x.Name, s => s.Name)));

			act.ShouldThrow<LinqToDBException>();
		}

		#endregion

		#region Query-cache parameterisation smoke tests

		[Test]
		public void Single_QueryCache_Parameterises_ItemValues_NativePath([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<UpsertRow>();

			// Prime the cache (first invocation = miss).
			table.Upsert(new UpsertRow { Id = 1, Name = "first", Version = 10 });

			var missBefore = table.GetCacheMissCount();

			// Second Upsert with DIFFERENT values on the SAME DataContext → same cache slot.
			// If the item is inlined instead of parameterised, this is a cache miss.
			table.Upsert(new UpsertRow { Id = 2, Name = "second", Version = 20 });

			table.GetCacheMissCount().ShouldBe(missBefore);

			var rows = table.OrderBy(r => r.Id).ToArray();
			rows.Length   .ShouldBe(2);
			rows[0].Name  .ShouldBe("first");
			rows[0].Version.ShouldBe(10);
			rows[1].Name  .ShouldBe("second");
			rows[1].Version.ShouldBe(20);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSapHana, TestProvName.AllSqlServer2005, TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus,
			TestProvName.AllMySql, TestProvName.AllSqlCe, TestProvName.AllAccess,
			ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public void Single_QueryCache_Parameterises_ItemValues_MergePath([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<UpsertRow>();

			// Seed a row via a plain insert path.
			db.Insert(new UpsertRow { Id = 42, Name = "seed", Version = 1 });

			// Prime the cache — SkipInsert forces MERGE lowering.
			table.Upsert(new UpsertRow { Id = 42, Name = "first-update", Version = 50 },
				u => u.Match((t, s) => t.Id == s.Id).SkipInsert().Update(v => v.Set(x => x.Name, s => s.Name)));

			var missBefore = table.GetCacheMissCount();

			// Second Upsert with DIFFERENT item values should hit the cache (no recompile).
			table.Upsert(new UpsertRow { Id = 42, Name = "second-update", Version = 99 },
				u => u.Match((t, s) => t.Id == s.Id).SkipInsert().Update(v => v.Set(x => x.Name, s => s.Name)));

			table.GetCacheMissCount().ShouldBe(missBefore);

			table.Single(r => r.Id == 42).Name.ShouldBe("second-update");
		}

		#endregion

		#region Malformed-match guard

		[Test]
		public void Single_MalformedMatch_Rejected([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			// Name + Version explicitly set so the test outcome depends only on the
			// match-validation path. If validation regresses and the query executes,
			// it must NOT also fail on a NOT NULL / constraint check first (otherwise
			// the assertion would pass for the wrong reason).
			Action act = () => db.GetTable<UpsertRow>().Upsert(
				new UpsertRow { Id = 1, Name = "x", Version = 1 },
				u => u.Match((t, s) => t.Id == s.Id && t.Version > 0));
			act.ShouldThrow<LinqToDBException>();
		}

		#endregion

		#region Contradictory-chain guard

		[Test]
		public void Single_SkipInsert_With_Insert_Rejected([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			Action act = () => db.GetTable<UpsertRow>().Upsert(
				new UpsertRow { Id = 1, Name = "x", Version = 1 },
				u => u.Match((t, s) => t.Id == s.Id).SkipInsert().Insert(i => i.Set(x => x.CreatedBy, _ => "sys")));
			act.ShouldThrow<LinqToDBException>();
		}

		[Test]
		public void Single_SkipUpdate_With_Update_Rejected([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			Action act = () => db.GetTable<UpsertRow>().Upsert(
				new UpsertRow { Id = 1, Name = "x", Version = 1 },
				u => u.Match((t, s) => t.Id == s.Id).SkipUpdate().Update(v => v.Set(x => x.Name, s => s.Name)));
			act.ShouldThrow<LinqToDBException>();
		}

		[Test]
		public void Single_InsertBranch_DoNothing_With_Ops_Rejected([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			Action act = () => db.GetTable<UpsertRow>().Upsert(
				new UpsertRow { Id = 1, Name = "x", Version = 1 },
				u => u.Match((t, s) => t.Id == s.Id).Insert(i => i.DoNothing().Set(x => x.CreatedBy, _ => "sys")));
			act.ShouldThrow<LinqToDBException>();
		}

		[Test]
		public void Single_UpdateBranch_DoNothing_With_Ops_Rejected([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			Action act = () => db.GetTable<UpsertRow>().Upsert(
				new UpsertRow { Id = 1, Name = "x", Version = 1 },
				u => u.Match((t, s) => t.Id == s.Id).Update(v => v.DoNothing().When((t, s) => s.Version > t.Version)));
			act.ShouldThrow<LinqToDBException>();
		}

		[Test]
		public void Single_InsertBranch_DoNothing_AcrossCalls_Rejected([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			// DoNothing in one Insert branch and Set in a second Insert branch is the same contradiction
			// as the within-branch form and must be rejected (MIN001) rather than silently letting the
			// DoNothing-derived skip win and dropping the Set.
			Action act = () => db.GetTable<UpsertRow>().Upsert(
				new UpsertRow { Id = 1, Name = "x", Version = 1 },
				u => u.Match((t, s) => t.Id == s.Id).Insert(i => i.DoNothing()).Insert(i => i.Set(x => x.CreatedBy, _ => "sys")));
			act.ShouldThrow<LinqToDBException>();
		}

		[Test]
		public void Single_UpdateBranch_DoNothing_AcrossCalls_Rejected([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			Action act = () => db.GetTable<UpsertRow>().Upsert(
				new UpsertRow { Id = 1, Name = "x", Version = 1 },
				u => u.Match((t, s) => t.Id == s.Id).Update(v => v.DoNothing()).Update(v => v.Set(x => x.Name, s => s.Name)));
			act.ShouldThrow<LinqToDBException>();
		}

		#endregion

		#region #3721 — dynamic / Sql.Property columns via .Set

		// Dynamic-column store mapped onto the physical UpsertDynamicTest table; the "Name" column is
		// declared as a dynamic (non-POCO) property and is reachable only through Sql.Property.
		[Table("UpsertDynamicTest")]
		sealed class UpsertDynamicStore
		{
			[PrimaryKey] public int Id { get; set; }

			[DynamicColumnsStore] public Dictionary<string, object> Values { get; set; } = new();
		}

		// Fixed-schema twin (same table name) used to materialise the physical table via CreateLocalTable.
		[Table("UpsertDynamicTest")]
		sealed class UpsertDynamicFullTable
		{
			[Column] public int     Id   { get; set; }
			[Column] public string? Name { get; set; }
		}

		static MappingSchema ConfigureUpsertDynamicSchema()
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<UpsertDynamicStore>()
					.HasPrimaryKey(e => e.Id)
					.DynamicColumnsStore(e => e.Values)
					.Property(x => Sql.Property<string?>(x, "Name"))
				.Build();

			return ms;
		}

		// #3721: set a dynamic / Sql.Property (non-POCO) column through the fluent Upsert .Set — covering
		// both the INSERT (unmatched) and UPDATE (matched) branches.
		// Dynamic-column field resolution + override matching now work (ColumnAccess emits
		// Sql.Property), but extracting the column *value* as a parameter from the entity instance
		// still hits DynamicColumnInfo.DummyGetter (real values live in the DynamicColumnsStore, not
		// the property getter) — the documented "dynamic target setters not supported" root. Needs
		// store-based value extraction. Gated until #3721 is fully implemented.
		[ActiveIssue(3721)]
		[Test]
		public void Single_Set_DynamicColumn([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context, ConfigureUpsertDynamicSchema());
			using var _  = db.CreateLocalTable<UpsertDynamicFullTable>();

			var table = db.GetTable<UpsertDynamicStore>();

			// INSERT branch — dynamic "Name" supplied via Sql.Property.
			table.Upsert(
				new UpsertDynamicStore { Id = 1 },
				u => u.Set(x => Sql.Property<string?>(x, "Name"), () => "dyn-insert"));

			table.Single(r => r.Id == 1).Values["Name"].ShouldBe("dyn-insert");

			// UPDATE branch — same PK, new dynamic value.
			table.Upsert(
				new UpsertDynamicStore { Id = 1 },
				u => u.Set(x => Sql.Property<string?>(x, "Name"), () => "dyn-update"));

			table.Single(r => r.Id == 1).Values["Name"].ShouldBe("dyn-update");
		}

		#endregion

		#region Nested complex-column setters

		sealed class UpsertNestedName
		{
			public string? First { get; set; }
			public string? Last  { get; set; }
		}

		[Table("UpsertNestedTest")]
		[Column("First", "Name.First")]
		[Column("Last",  "Name.Last")]
		sealed class UpsertNestedRow
		{
			[PrimaryKey] public int             Id   { get; set; }
			             public UpsertNestedName Name { get; set; } = null!;
		}

		// Sets a nested complex-column member (Name.First) through the fluent Upsert .Set, across the
		// INSERT (unmatched) and UPDATE (matched) branches.
		[Test]
		public void Single_Set_NestedColumn([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertNestedRow>();

			var table = db.GetTable<UpsertNestedRow>();

			// INSERT branch — Name.Last from item, Name.First overridden via .Set.
			table.Upsert(
				new UpsertNestedRow { Id = 1, Name = new UpsertNestedName { First = "ins-first", Last = "seed-last" } },
				u => u.Set(x => x.Name.First, () => "set-first"));

			var inserted = table.Single(r => r.Id == 1);
			inserted.Name.First.ShouldBe("set-first");
			inserted.Name.Last .ShouldBe("seed-last");

			// UPDATE branch — same PK, new nested value via .Set.
			table.Upsert(
				new UpsertNestedRow { Id = 1, Name = new UpsertNestedName { First = "ins-first", Last = "upd-last" } },
				u => u.Set(x => x.Name.First, () => "upd-first"));

			var updated = table.Single(r => r.Id == 1);
			updated.Name.First.ShouldBe("upd-first");
			updated.Name.Last .ShouldBe("upd-last");
		}

		#endregion
	}
}
