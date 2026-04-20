using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Internal.Async;
using LinqToDB.Internal.Linq;
using LinqToDB.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.xUpdate
{
	/// <summary>
	/// Tests for the fluent <c>Upsert</c> API (issue #2558) — Phase 1 (SQLite single-entity).
	///
	/// The API surface was finalised in Phase 0 (see git history). Phase 1 implements:
	/// - single-entity upsert (generic arity 1) against SQLite's <c>ON CONFLICT</c> path,
	/// - <c>.Match</c> (content currently ignored — PK is always used as keys),
	/// - root and per-branch <c>.Set</c> / <c>.Ignore</c>.
	///
	/// Rejected with <see cref="LinqToDBException"/> (Phase 2+ features):
	/// <c>.When</c>, <c>.DoNothing</c>, <c>.SkipInsert</c>, <c>.SkipUpdate</c>, bulk sources.
	/// </summary>
	[TestFixture]
	public partial class UpsertTests : TestBase
	{
		#region Test-only model

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

		#endregion

		#region Entry-method null-argument validation (no database needed)

		public static IEnumerable<TestCaseData> NullParameterCases
		{
			get
			{
				var table         = new FakeTable<UpsertRow>();
				var iqsource      = (IQueryable<UpsertRow>)table;
				var item          = new UpsertRow();
				var enumItems     = new[] { new UpsertRow() };
				Expression<Func<IUpsertable<UpsertRow, UpsertRow>, IUpsertable<UpsertRow, UpsertRow>>> cfgIdentity = u => u;
				Expression<Func<IUpsertable<UpsertRow, UpsertRow>, IUpsertable<UpsertRow, UpsertRow>>> cfgIdentityTS = u => u;

				var cases = new TestDelegate[]
				{
					// ------------ single entity ------------
					() => LinqExtensions.Upsert(null!, item),
					() => LinqExtensions.Upsert<UpsertRow>(table, null!),
					() => LinqExtensions.Upsert(null!, item, cfgIdentity),
					() => LinqExtensions.Upsert<UpsertRow>(table, null!, cfgIdentity),
					() => LinqExtensions.Upsert(table, item, configure: null!),

					() => LinqExtensions.UpsertAsync(null!, item),
					() => LinqExtensions.UpsertAsync<UpsertRow>(table, null!),
					() => LinqExtensions.UpsertAsync(null!, item, cfgIdentity),
					() => LinqExtensions.UpsertAsync<UpsertRow>(table, null!, cfgIdentity),
					() => LinqExtensions.UpsertAsync(table, item, configure: null!),

					// ------------ IEnumerable<TSource> ------------
					() => LinqExtensions.Upsert<UpsertRow, UpsertRow>(null!, enumItems, cfgIdentityTS),
					() => LinqExtensions.Upsert<UpsertRow, UpsertRow>(table, (IEnumerable<UpsertRow>)null!, cfgIdentityTS),
					() => LinqExtensions.Upsert<UpsertRow, UpsertRow>(table, enumItems, configure: null!),

					() => LinqExtensions.UpsertAsync<UpsertRow, UpsertRow>(null!, enumItems, cfgIdentityTS),
					() => LinqExtensions.UpsertAsync<UpsertRow, UpsertRow>(table, (IEnumerable<UpsertRow>)null!, cfgIdentityTS),
					() => LinqExtensions.UpsertAsync<UpsertRow, UpsertRow>(table, enumItems, configure: null!),

					// ------------ IQueryable<TSource> ------------
					() => LinqExtensions.Upsert<UpsertRow, UpsertRow>(null!, iqsource, cfgIdentityTS),
					() => LinqExtensions.Upsert<UpsertRow, UpsertRow>(table, (IQueryable<UpsertRow>)null!, cfgIdentityTS),
					() => LinqExtensions.Upsert<UpsertRow, UpsertRow>(table, iqsource, configure: null!),

					() => LinqExtensions.UpsertAsync<UpsertRow, UpsertRow>(null!, iqsource, cfgIdentityTS),
					() => LinqExtensions.UpsertAsync<UpsertRow, UpsertRow>(table, (IQueryable<UpsertRow>)null!, cfgIdentityTS),
					() => LinqExtensions.UpsertAsync<UpsertRow, UpsertRow>(table, iqsource, configure: null!),

					// ------------ mirror (IQueryable receiver, ITable arg) ------------
					() => LinqExtensions.Upsert<UpsertRow, UpsertRow>((IQueryable<UpsertRow>)null!, table, cfgIdentityTS),
					() => LinqExtensions.Upsert(iqsource, target: null!, cfgIdentityTS),
					() => LinqExtensions.Upsert(iqsource, table, configure: null!),

					() => LinqExtensions.UpsertAsync<UpsertRow, UpsertRow>((IQueryable<UpsertRow>)null!, table, cfgIdentityTS),
					() => LinqExtensions.UpsertAsync(iqsource, target: null!, cfgIdentityTS),
					() => LinqExtensions.UpsertAsync(iqsource, table, configure: null!),
				};

				return cases.Select((d, i) => new TestCaseData(d).SetName($"Upsert.API.NullParameter.{i:D2}"));
			}
		}

		[TestCaseSource(nameof(NullParameterCases))]
		public void UpsertApiNullParameter(TestDelegate action)
		{
			Action act = () => action();
			act.ShouldThrow<ArgumentNullException>();
		}

		#endregion

		#region Chain-method direct-invocation — must throw NotSupportedException

		[Test]
		public void Match_DirectInvocation_Throws()
		{
			var upsertable = default(IUpsertable<UpsertRow, UpsertRow>)!;
			Action act = () => upsertable.Match((t, s) => t.Id == s.Id);
			act.ShouldThrow<NotSupportedException>();
		}

		[Test]
		public void Set_DirectInvocation_Throws()
		{
			var upsertable = default(IUpsertable<UpsertRow, UpsertRow>)!;
			Action act = () => upsertable.Set(x => x.Version, () => 1);
			act.ShouldThrow<NotSupportedException>();
		}

		[Test]
		public void InsertBranch_Set_DirectInvocation_Throws()
		{
			var builder = default(IUpsertInsertBuilder<UpsertRow, UpsertRow>)!;
			Action act = () => builder.Set(x => x.Version, () => 1);
			act.ShouldThrow<NotSupportedException>();
		}

		[Test]
		public void SkipInsert_DirectInvocation_Throws()
		{
			var upsertable = default(IUpsertable<UpsertRow, UpsertRow>)!;
			Action act = () => upsertable.SkipInsert();
			act.ShouldThrow<NotSupportedException>();
		}

		#endregion

		#region E2E — Phase 1 scenarios against SQLite

		[Test]
		public void E2E_BareSingleEntityUpsert([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			// Insert
			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1, Name = "a", Version = 1 });
			db.GetTable<UpsertRow>().Single(r => r.Id == 1).Name.ShouldBe("a");

			// Update
			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1, Name = "b", Version = 2 });
			var row = db.GetTable<UpsertRow>().Single(r => r.Id == 1);
			row.Name   .ShouldBe("b");
			row.Version.ShouldBe(2);
		}

		[Test]
		public void E2E_WithMatch_SameAsPK([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			// .Match on PK — equivalent to bare upsert in Phase 1.
			db.GetTable<UpsertRow>().Upsert(
				new UpsertRow { Id = 1, Name = "m1", Version = 1 },
				u => u.Match((t, s) => t.Id == s.Id));

			db.GetTable<UpsertRow>().Upsert(
				new UpsertRow { Id = 1, Name = "m2", Version = 2 },
				u => u.Match((t, s) => t.Id == s.Id));

			db.GetTable<UpsertRow>().Single().Name.ShouldBe("m2");
		}

		[Test]
		public void E2E_PerBranch_Set_Ignore([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			var insertTime = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
			var updateTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

			// First call: INSERT. CreatedAt/By get captured values; UpdatedAt/By ignored.
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

			// Second call: UPDATE. CreatedAt/By must be preserved; UpdatedAt/By written.
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
		public void E2E_Root_Set_Ignore([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			var modified = new DateTime(2026, 2, 2, 9, 0, 0, DateTimeKind.Utc);

			// Root-level .Set / .Ignore apply to BOTH branches.
			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1, Name = "root-ins", Version = 1 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Set(x => x.UpdatedAt, () => modified)
				.Set(x => x.UpdatedBy, s => "sys-" + s.Name)
				.Ignore(x => x.CreatedBy));

			var afterInsert = db.GetTable<UpsertRow>().Single();
			afterInsert.Name     .ShouldBe("root-ins");
			afterInsert.UpdatedAt.ShouldBe(modified);
			afterInsert.UpdatedBy.ShouldBe("sys-root-ins");
			afterInsert.CreatedBy.ShouldBeNull(); // ignored in both branches

			// Update path: same root setters still apply.
			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1, Name = "root-upd", Version = 2 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Set(x => x.UpdatedAt, () => modified)
				.Set(x => x.UpdatedBy, s => "sys-" + s.Name)
				.Ignore(x => x.CreatedBy));

			db.GetTable<UpsertRow>().Single().UpdatedBy.ShouldBe("sys-root-upd");
		}

		#endregion

		#region Phase-1-rejected features (must throw LinqToDBException)

		[Test]
		public void Phase1_SkipInsert_Rejected([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			Action act = () => db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1 }, u => u.SkipInsert());
			act.ShouldThrow<LinqToDBException>();
		}

		[Test]
		public void Phase1_SkipUpdate_Rejected([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			Action act = () => db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1 }, u => u.SkipUpdate());
			act.ShouldThrow<LinqToDBException>();
		}

		[Test]
		public void Phase1_InsertWhen_Rejected([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			Action act = () => db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1 }, u => u.Insert(i => i.When(s => true)));
			act.ShouldThrow<LinqToDBException>();
		}

		[Test]
		public void Phase1_UpdateDoNothing_Rejected([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			Action act = () => db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1 }, u => u.Update(v => v.DoNothing()));
			act.ShouldThrow<LinqToDBException>();
		}

		[Test]
		public void Phase1_BulkEnumerable_Rejected([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			var items = new[] { new UpsertRow { Id = 1 } };

			Action act = () => db.GetTable<UpsertRow>().Upsert(items, u => u.Match((t, s) => t.Id == s.Id));
			act.ShouldThrow<LinqToDBException>();
		}

		#endregion

		#region Deferred Phase 2+ E2E scenarios (kept as documentation)

		[Explicit("Pending Phase 2 — requires .When / annotations")]
		[Test]
		public void E2E_InsertIfNotExists_SkipUpdate([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			db.GetTable<UpsertRow>().Insert(() => new UpsertRow { Id = 1, Name = "original", Version = 1 });

			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1, Name = "replaced", Version = 99 },
				u => u.Match((t, s) => t.Id == s.Id).SkipUpdate());

			var row = db.GetTable<UpsertRow>().Single(r => r.Id == 1);
			row.Name   .ShouldBe("original");
			row.Version.ShouldBe(1);
		}

		[Explicit("Pending Phase 3 — requires MERGE provider")]
		[Test]
		public void E2E_UpdateIfExists_SkipInsert([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1, Name = "x", Version = 1 },
				u => u.Match((t, s) => t.Id == s.Id).SkipInsert().Update(v => v.Set(x => x.Name, s => s.Name)));

			db.GetTable<UpsertRow>().Any(r => r.Id == 1).ShouldBeFalse();
		}

		[Explicit("Pending Phase 2 — requires .When")]
		[Test]
		public void E2E_ConditionalUpdate_When([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			db.GetTable<UpsertRow>().Insert(() => new UpsertRow { Id = 1, Name = "a", Version = 5 });

			db.GetTable<UpsertRow>().Upsert(new UpsertRow { Id = 1, Name = "stale", Version = 3 }, u => u
				.Match((t, s) => t.Id == s.Id)
				.Update(v => v.When((t, s) => s.Version > t.Version).Set(x => x.Name, s => s.Name)));

			db.GetTable<UpsertRow>().Single(r => r.Id == 1).Name.ShouldBe("a");
		}

		[Explicit("Pending Phase 4 — bulk IEnumerable source")]
		[Test]
		public void E2E_BulkEnumerable_Upsert([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var _  = db.CreateLocalTable<UpsertRow>();

			db.GetTable<UpsertRow>().Insert(() => new UpsertRow { Id = 1, Name = "old", Version = 1 });

			var payload = new[]
			{
				new UpsertRow { Id = 1, Name = "one", Version = 2 },
				new UpsertRow { Id = 2, Name = "two", Version = 1 },
			};

			db.GetTable<UpsertRow>().Upsert(payload, u => u.Match((t, s) => t.Id == s.Id));

			var rows = db.GetTable<UpsertRow>().OrderBy(r => r.Id).ToArray();
			rows.Length .ShouldBe(2);
			rows[0].Name.ShouldBe("one");
			rows[1].Name.ShouldBe("two");
		}

		#endregion

		#region Fakes (for null-arg validation only)

		private sealed class FakeQueryProvider : IQueryProvider
		{
			IQueryable IQueryProvider.CreateQuery(Expression expression) => throw new NotImplementedException();
			IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression) => throw new NotImplementedException();
			object IQueryProvider.Execute(Expression expression) => throw new NotImplementedException();
			TResult IQueryProvider.Execute<TResult>(Expression expression) => throw new NotImplementedException();
		}

		private sealed class FakeTable<TEntity> : ITable<TEntity>
			where TEntity : notnull
		{
			IDataContext            IExpressionQuery.DataContext                                  => throw new NotImplementedException();
			Expression              IExpressionQuery.Expression                                   => throw new NotImplementedException();
			IReadOnlyList<QuerySql> IExpressionQuery.GetSqlQueries(SqlGenerationOptions? options) => throw new NotImplementedException();

			Type                    IQueryable.         ElementType                               => throw new NotImplementedException();
			Expression              IQueryable.         Expression                                => throw new NotImplementedException();
			IQueryProvider          IQueryable.         Provider                                  => new FakeQueryProvider();
			Expression              IQueryProviderAsync.Expression                                => throw new NotImplementedException();

			Expression IExpressionQuery<TEntity>.Expression => Expression.Constant((ITable<TEntity>)this);

			public QueryDebugView DebugView => throw new NotImplementedException();

			IQueryable IQueryProvider.CreateQuery(Expression expression) => throw new NotImplementedException();
			IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression) => throw new NotImplementedException();
			object IQueryProvider.Execute(Expression expression) => throw new NotImplementedException();
			TResult IQueryProvider.Execute<TResult>(Expression expression) => throw new NotImplementedException();

			Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) => throw new NotImplementedException();
			Task<IAsyncEnumerable<TResult>> IQueryProviderAsync.ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken) => throw new NotImplementedException();

			IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
			IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator() => throw new NotImplementedException();

			public string?      ServerName   { get; }
			public string?      DatabaseName { get; }
			public string?      SchemaName   { get; }
			public string       TableName    { get; } = null!;
			public TableOptions TableOptions { get; }
			public string?      TableID      { get; }

			public string GetTableName() => throw new NotImplementedException();
		}

		#endregion
	}
}
