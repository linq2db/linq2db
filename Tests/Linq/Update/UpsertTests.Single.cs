using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

namespace Tests.xUpdate
{
	/// <summary>
	/// End-to-end Upsert scenarios for the single-entity overload
	/// <c>Upsert&lt;T&gt;(ITable&lt;T&gt;, T, configure)</c>.
	/// </summary>
	public partial class UpsertTests
	{
		#region Phase 1 — whole-object upsert, root & per-branch Set/Ignore

		[Test]
		public void BareSingleEntityUpsert([InsertOrUpdateDataSources] string context)
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
		public void WithMatch_SameAsPK([InsertOrUpdateDataSources] string context)
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
		public void PerBranch_Set_Ignore([InsertOrUpdateDataSources] string context)
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
		public void Root_Set_Ignore([InsertOrUpdateDataSources] string context)
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

		#endregion

		#region Phase 2 — SkipUpdate / Update.DoNothing / Update.When (native path)

		[Test]
		public void InsertIfNotExists_SkipUpdate([InsertOrUpdateDataSources] string context)
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
		public void InsertIfNotExists_UpdateDoNothing([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new UpsertRow { Id = 1, Name = "original", Version = 1 } });

			table.Upsert(new UpsertRow { Id = 1, Name = "replaced", Version = 99 },
				u => u.Match((t, s) => t.Id == s.Id).Update(v => v.DoNothing()));

			table.Single(r => r.Id == 1).Name.ShouldBe("original");
		}

		[Test]
		public void ConditionalUpdate_When([InsertOrUpdateDataSources] string context)
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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSapHana, ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public void UpdateIfExists_SkipInsert_EmptyTable([InsertOrUpdateDataSources(
				TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus, TestProvName.AllMySql,
				TestProvName.AllMariaDB, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllAccess,
				TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1, Name = "x", Version = 1 },
				u => u.Match((t, s) => t.Id == s.Id).SkipInsert().Update(v => v.Set(x => x.Name, s => s.Name)));

			db.GetTable<UpsertRow>().Any(r => r.Id == 1).ShouldBeFalse();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSapHana, ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public void UpdateIfExists_SkipInsert_Existing([InsertOrUpdateDataSources(
				TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus, TestProvName.AllMySql,
				TestProvName.AllMariaDB, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllAccess,
				TestProvName.AllInformix)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new UpsertRow { Id = 1, Name = "seed", Version = 1 } });

			table.Upsert(new UpsertRow { Id = 1, Name = "updated", Version = 2 },
				u => u.Match((t, s) => t.Id == s.Id).SkipInsert().Update(v => v.Set(x => x.Name, s => s.Name)));

			table.Single(r => r.Id == 1).Name.ShouldBe("updated");
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSapHana, ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public void InsertWhen_Merge([InsertOrUpdateDataSources(
				TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus, TestProvName.AllMySql,
				TestProvName.AllMariaDB, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllAccess,
				TestProvName.AllInformix)] string context)
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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSapHana, ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public void NonPK_Match_Merge([InsertOrUpdateDataSources(
				TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus, TestProvName.AllMySql,
				TestProvName.AllMariaDB, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllAccess,
				TestProvName.AllInformix)] string context)
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

		#endregion

		#region Query-cache parameterisation smoke tests

		[Test]
		public void QueryCache_Parameterises_ItemValues_NativePath([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<UpsertRow>();

			// Prime the cache (first invocation = miss).
			table.Upsert(new UpsertRow { Id = 1, Name = "first", Version = 10 });

			var missBefore = table.GetCacheMissCount();

			// Second Upsert with DIFFERENT values on the SAME DataContext → same cache slot.
			// If the item is inlined instead of parameterised, this is a cache miss.
			table.Upsert(new UpsertRow { Id = 2, Name = "second", Version = 20 });

			var rows = table.OrderBy(r => r.Id).ToArray();
			rows.Length   .ShouldBe(2);
			rows[0].Name  .ShouldBe("first");
			rows[0].Version.ShouldBe(10);
			rows[1].Name  .ShouldBe("second");
			rows[1].Version.ShouldBe(20);

			table.GetCacheMissCount().ShouldBe(missBefore);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSapHana, ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public void QueryCache_Parameterises_ItemValues_MergePath([InsertOrUpdateDataSources(
				// MERGE-only features → MERGE providers only.
				TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus, TestProvName.AllMySql,
				TestProvName.AllMariaDB, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllAccess,
				TestProvName.AllInformix, TestProvName.AllOracle)] string context)
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

			table.Single(r => r.Id == 42).Name.ShouldBe("second-update");

			table.GetCacheMissCount().ShouldBe(missBefore);
		}

		#endregion

		#region Malformed-match guard

		[Test]
		public void Phase1_MalformedMatch_Rejected([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			Action act = () => db.GetTable<UpsertRow>().Upsert(
				new UpsertRow { Id = 1 },
				u => u.Match((t, s) => t.Id == s.Id && t.Version > 0));
			act.ShouldThrow<LinqToDBException>();
		}

		#endregion
	}
}
