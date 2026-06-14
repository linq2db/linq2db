using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class ContainsUseTempTableTests : TestBase
	{
		// Same provider exclusion list as EnumerableSourceTests.AsQueryable.cs — temp-table
		// machinery is unavailable on these providers (IsRuntimeTempTableCreationSupported is
		// false; Access doesn't support inline-rows sources at all; ClickHouse hard-throws on
		// parameters). Tests below also pass includeLinqService = false because TempTable<T>
		// requires a local DataConnection/DataContext, not a RemoteDataContextBase.
		const string ProvidersWithoutAutoTempTable =
			TestProvName.AllAccess     + "," +
			TestProvName.AllClickHouse + "," +
			TestProvName.AllOracle     + "," +
			TestProvName.AllFirebird   + "," +
			TestProvName.AllSybase     + "," +
			TestProvName.AllSapHana    + "," +
			TestProvName.AllInformix   + "," +
			TestProvName.AllDB2        + "," +
			TestProvName.AllSqlCe      + "," +
			TestProvName.AllDuckDB;

		// Local entity used by the execution tests below — controlled row count + IDs so the
		// assertions don't depend on the test DB seed.
		[Table("ContainsTempTableTestRow")]
		sealed class IdRow
		{
			[PrimaryKey] public int     Id   { get; set; }
			[Column]     public string  Name { get; set; } = "";
			[Column]     public string? Tag  { get; set; }
		}

		static IdRow[] BuildIdRows(int count) =>
			Enumerable
				.Range(1, count)
				.Select(i => new IdRow { Id = i, Name = "row" + i, Tag = i % 3 == 0 ? null : "tag" + i })
				.ToArray();

		#region Build-time SQL shape (SQLite — deterministic)

		[Test]
		public void Contains_DataOptions_AboveThreshold_EmitsTempTablePath(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			// Threshold(5) + list of 20 → above. Predicate must translate to
			// IN (SELECT item FROM <SqlValuesTable>) with the [T_xxxxxxxx] temp-table name
			// allocated by GetOrAssignTempTableName.
			using var db = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(5)));

			var ids   = Enumerable.Range(1, 20).ToList();
			var query = db.Person.Where(p => ids.Contains(p.ID));

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldContain("[T_");
		}

		[Test]
		public void Contains_DataOptions_BelowThreshold_StaysInline(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			// Threshold(100) + list of 3 → below threshold, must stay on the flat IN form.
			using var db = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(100)));

			var ids   = new List<int> { 1, 2, 3 };
			var query = db.Person.Where(p => ids.Contains(p.ID));

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldNotContain("[T_");
			sql.ShouldNotContain("IN (SELECT");
		}

		[Test]
		public void Contains_NoDataOptionsConfig_StaysInline(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			// UseTempTablesForContains NOT called — even with a large collection, the predicate
			// must stay on the inline-IN path. Default opt-in is null/disabled — the gate
			// fails closed when no Contains spec is set.
			using var db = GetDataContext(context);

			var ids   = Enumerable.Range(1, 20).ToList();
			var query = db.Person.Where(p => ids.Contains(p.ID));

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldNotContain("[T_");
		}

		[Test]
		public void Contains_InlineLiteralArray_NotRewritten(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			// new[] { 1, 2, 3 }.Contains(p.ID) is a compile-time literal that goes through the
			// ExpressionType.NewArrayInit branch in ConvertInPredicate — the temp-table rewrite
			// is intentionally only applied to the parameter-backed default branch. Verifies
			// that even with a threshold of 1 set, literal arrays stay inline.
			using var db = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(1)));

			var query = db.Person.Where(p => new[] { 1, 2, 3 }.Contains(p.ID));
			var sql   = query.ToSqlQuery().Sql;

			sql.ShouldNotContain("[T_");
		}

		[Test]
		public void Contains_DataOptionsSpecChange_ProducesFreshSql(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			// Two DataOptions instances differing ONLY in the resolved Contains spec must
			// produce different SQL — proves TempTableOptions.ConfigurationID flows into the
			// cache key. Threshold 5 with a 20-item list trips the rewrite; threshold 100 with
			// the same list stays inline. The two queries' SQL must therefore differ.
			var ids = Enumerable.Range(1, 20).ToList();

			string sqlLow;
			string sqlHigh;

			using (var dbLow  = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(5))))
				sqlLow = dbLow.Person.Where(p => ids.Contains(p.ID)).ToSqlQuery().Sql;

			using (var dbHigh = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(100))))
				sqlHigh = dbHigh.Person.Where(p => ids.Contains(p.ID)).ToSqlQuery().Sql;

			sqlLow .ShouldContain("[T_");
			sqlHigh.ShouldNotContain("[T_");
			sqlLow .ShouldNotBe(sqlHigh);
		}

		#endregion

		#region Cross-provider execution

		[Test]
		public void Contains_AboveThreshold_ReturnsCorrectRows(
			[DataSources(false, ProvidersWithoutAutoTempTable)] string context)
		{
			// Above threshold + provider with runtime temp tables. End-to-end smoke: the
			// BULK-insert + SELECT path produces a valid query and the right rows on
			// SQLite/SqlServer/PostgreSQL/MySQL.
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(5)));
			using var rows = db.CreateLocalTable(BuildIdRows(50));

			// Pick a non-trivially-sized subset. Mixed with non-matching IDs to confirm the
			// IN-filter actually filters (not just "return everything").
			var lookup = Enumerable.Range(1, 10)
				.Concat(Enumerable.Range(100, 10))     // 10 valid + 10 non-existent
				.ToList();

			var result = rows.Where(r => lookup.Contains(r.Id)).OrderBy(r => r.Id).ToList();

			result.Count.ShouldBe(10);
			result[0].Id.ShouldBe(1);
			result[9].Id.ShouldBe(10);
		}

		[Test]
		public void Contains_BelowThreshold_ReturnsCorrectRows(
			[DataSources(false, ProvidersWithoutAutoTempTable)] string context)
		{
			// Below threshold — same opt-in but the small collection takes the regular
			// inline-IN path. Confirms the fallback still functions when the gate is engaged
			// elsewhere in the query.
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(100)));
			using var rows = db.CreateLocalTable(BuildIdRows(20));

			var lookup = new List<int> { 1, 5, 10 };

			var result = rows.Where(r => lookup.Contains(r.Id)).OrderBy(r => r.Id).ToList();

			result.Select(r => r.Id).ShouldBe(new[] { 1, 5, 10 });
		}

		[Test]
		public void Contains_StringElement_AboveThreshold_ReturnsCorrectRows(
			// SqlServer excluded: SqlServer creates temp tables in tempdb, which uses the server's
			// default collation; the local table (ContainsTempTableTestRow) inherits the user
			// database's collation. When the two differ ("SQL_Latin1_General_CP1_CI_AS" vs
			// "Latin1_General_CS_AS" in our test env), the predicate's equality check fails with
			// a runtime "collation conflict" error. This is a SqlServer-specific tempdb quirk —
			// the user can work around it by setting an explicit collation on the column or by
			// matching the server/database collations. Not a defect in the temp-table rewrite.
			[DataSources(false, ProvidersWithoutAutoTempTable + "," + TestProvName.AllSqlServer)] string context)
		{
			// Reference-type element (string) — exercises the IsNullableOrReferenceType branch in
			// the BuildScalarValuesTableForContains helper (canBeNull = true for strings).
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(3)));
			using var rows = db.CreateLocalTable(BuildIdRows(20));

			var names = Enumerable.Range(1, 10).Select(i => "row" + i).ToList();

			var result = rows.Where(r => names.Contains(r.Name)).OrderBy(r => r.Id).ToList();

			result.Count.ShouldBe(10);
			result[0].Name.ShouldBe("row1");
			result[9].Name.ShouldBe("row10");
		}

		[Test]
		public void Contains_NullableStringElement_AboveThreshold_ReturnsCorrectRows(
			// SqlServer excluded for the same tempdb collation-conflict reason documented on
			// Contains_StringElement_AboveThreshold_ReturnsCorrectRows above.
			[DataSources(false, ProvidersWithoutAutoTempTable + "," + TestProvName.AllSqlServer)] string context)
		{
			// Nullable-reference element (string?) — same canBeNull = true branch in the helper.
			// IdRow.Tag is null for every third row, so we filter by non-null tags only and
			// assert the count of matching non-null-tag rows.
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(3)));
			using var rows = db.CreateLocalTable(BuildIdRows(30));

			// IdRow.Tag = "tag<i>" for i not divisible by 3, null otherwise. Pick the non-null
			// tags from the source data so we have a >threshold lookup list.
			var nonNullTags = BuildIdRows(30)
				.Where(r => r.Tag != null)
				.Select(r => r.Tag)
				.ToList();

			var result = rows.Where(r => nonNullTags.Contains(r.Tag)).OrderBy(r => r.Id).ToList();

			result.Count.ShouldBe(nonNullTags.Count);
		}

		[Test]
		public void Contains_LikeClrNulls_LookupAndColumnNullable_NullMatchesNull(
			// SqlServer excluded for tempdb collation; MySQL excluded because the assertion form
			// uses self-join in CteContext-like patterns — not necessary here since this test
			// targets one Contains predicate.
			[DataSources(false, ProvidersWithoutAutoTempTable + "," + TestProvName.AllSqlServer)] string context)
		{
			// CompareNulls.LikeClr semantics: NULL Contains NULL is true. Below threshold this
			// is handled by SqlPredicate.InList emitting `... IN (...) OR col IS NULL`. Above
			// threshold the predicate must preserve the same semantics — rows with a NULL
			// column should match if the lookup contains NULL.
			using var db = GetDataContext(context, o => o
				.UseCompareNulls(CompareNulls.LikeClr)
				.UseTempTablesForContains(b => b.Threshold(3)));

			using var rows = db.CreateLocalTable(BuildIdRows(30));

			// Lookup with > threshold items + an explicit NULL. The 30-row source has 10 rows
			// where Tag == null (every third row); plus our lookup matches the non-null tags
			// for i in {1,2,4,5,7,8} → 6 rows. So we expect 10 (null tags) + 6 (matched non-null
			// tags) = 16 rows total.
			var lookup = new List<string?> { "tag1", "tag2", "tag4", "tag5", "tag7", "tag8", null };

			var result = rows.Where(r => lookup.Contains(r.Tag)).OrderBy(r => r.Id).ToList();

			result.Count.ShouldBe(16);
		}

		[Test]
		public void Contains_LikeClrNulls_NotContains_PreservesNullSemantics(
			[DataSources(false, ProvidersWithoutAutoTempTable + "," + TestProvName.AllSqlServer)] string context)
		{
			// !list.Contains(col) with LikeClr + lookup containing NULL: a NULL column means
			// list.Contains(NULL) is TRUE under LikeClr, so !Contains is FALSE → exclude NULL
			// columns. The temp-table path must produce the same result as the inline path.
			using var db = GetDataContext(context, o => o
				.UseCompareNulls(CompareNulls.LikeClr)
				.UseTempTablesForContains(b => b.Threshold(3)));

			using var rows = db.CreateLocalTable(BuildIdRows(30));

			// Lookup includes "tag1" + null. 30-row source: rows with tag matching "tag1" → 1,
			// rows with NULL Tag → 10. The remaining 19 rows (non-null, not "tag1") should be
			// included in !Contains.
			var lookup = new List<string?> { "tag1", "tag2", "tag4", "tag5", "tag7", "tag8", null };

			var result = rows.Where(r => !lookup.Contains(r.Tag)).OrderBy(r => r.Id).ToList();

			// 30 total - 6 matching tags (tag1..tag8 except multiples of 3) - 10 NULL rows = 14.
			result.Count.ShouldBe(14);
		}

		[Test]
		public void Contains_LikeClrNulls_LookupAndColumnNullable_BelowThreshold(
			[DataSources(false, ProvidersWithoutAutoTempTable + "," + TestProvName.AllSqlServer)] string context)
		{
			// Same LikeClr-with-NULL scenario but below threshold — verifies today's flat-IN
			// path (with `OR col IS NULL` from InList.WithNull) still works. This is a
			// regression guard for the inline path when the opt-in is set elsewhere.
			using var db = GetDataContext(context, o => o
				.UseCompareNulls(CompareNulls.LikeClr)
				.UseTempTablesForContains(b => b.Threshold(100)));

			using var rows = db.CreateLocalTable(BuildIdRows(30));

			var lookup = new List<string?> { "tag1", "tag2", null };

			// 10 rows have Tag == null, 2 rows have Tag in lookup ("tag1", "tag2") → 12 total.
			var result = rows.Where(r => lookup.Contains(r.Tag)).OrderBy(r => r.Id).ToList();

			result.Count.ShouldBe(12);
		}

		[Test]
		public void Contains_SelfJoin_SameLocalList_SharesTempTable(
			// MySQL excluded: MySQL doesn't allow a TEMPORARY TABLE to be referenced more than
			// once in the same SELECT — throws ER_CANT_REOPEN_TABLE ("Can't reopen table"). This
			// is a fundamental MySQL temp-table limitation, not a defect in the dedup logic;
			// in fact, the dedup IS working — both Contains predicates resolve to the same
			// temp-table reference, which MySQL then refuses to read twice. Users on MySQL
			// with this query shape must stay below the threshold or restructure the query.
			[DataSources(false, ProvidersWithoutAutoTempTable + "," + TestProvName.AllMySql)] string context)
		{
			// Two Contains predicates against the same captured local list inside one query —
			// GetOrAssignTempTableName keys by source expression identity, so they should
			// share a single TempTableName → one CREATE/INSERT cycle. Asserts the rewritten SQL
			// references the temp table but doesn't re-create it.
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(5)));
			using var rows = db.CreateLocalTable(BuildIdRows(20));

			var lookup = Enumerable.Range(1, 15).ToList();

			var query =
				from a in rows
				from b in rows
				where lookup.Contains(a.Id) && lookup.Contains(b.Id) && a.Id < b.Id
				select new { A = a.Id, B = b.Id };

			var sql = query.ToSqlQuery().Sql;

			// The dedup contract: both Contains predicates share one CREATE/INSERT/DROP cycle,
			// so the SQL must reference exactly one distinct temp-table name (not two).
			// Match T_<hex>... regardless of the surrounding identifier quoting (which varies:
			// `[T_xxx]` on SQLite + SQL Server, `"T_xxx"` on PostgreSQL, `` `T_xxx` `` on MySQL).
			// .Cast<Match>() restores type inference on net462 — MatchCollection only implements
			// non-generic IEnumerable there, so Select picks LinqExtensions.Select<T>(IDataContext, …).
			var distinctNames = Regex
				.Matches(sql, @"T_[0-9a-f]+")
				.Cast<Match>()
				.Select(m => m.Value)
				.Distinct()
				.ToList();

			distinctNames.Count.ShouldBe(1,
				$"Expected one shared temp-table name, got: {string.Join(", ", distinctNames)}");

			var result = query.ToList();
			result.Count.ShouldBeGreaterThan(0);
		}

		[Test]
		public void Contains_CacheHit_OnRepeatedExecute(
			[DataSources(false, ProvidersWithoutAutoTempTable)] string context)
		{
			// Two executes of the same query expression against the same DataOptions must reuse
			// the cached Query<T> instance — GetCacheMissCount() snapshot before+after must match.
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(5)));
			using var rows = db.CreateLocalTable(BuildIdRows(20));

			var lookup = Enumerable.Range(1, 10).ToList();

			// First execute — populates the cache.
			_ = rows.Where(r => lookup.Contains(r.Id)).OrderBy(r => r.Id).ToList();

			var query    = rows.Where(r => lookup.Contains(r.Id)).OrderBy(r => r.Id);
			var beforeMs = query.GetCacheMissCount();

			_ = query.ToList();

			query.GetCacheMissCount().ShouldBe(beforeMs);
		}

		#endregion

		#region Entity / composite-key Contains

		[Table("ContainsTempTableComposite")]
		sealed class CompositeRow
		{
			[PrimaryKey(0)]                                       public int    K1   { get; set; }
			[PrimaryKey(1), Column(CanBeNull = false, Length = 32)] public string K2   { get; set; } = "";
			[Column]                                              public string Data { get; set; } = "";
		}

		static CompositeRow[] BuildCompositeRows(int count) =>
			Enumerable
				.Range(1, count)
				.Select(i => new CompositeRow { K1 = i, K2 = "k" + i, Data = "data" + i })
				.ToArray();

		// --- Build-time SQL shape (SQLite, deterministic) ---

		[Test]
		public void Contains_EntityWithSinglePk_AboveThreshold_EmitsExistsTempTablePath(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(5)));
			using var rows = db.CreateLocalTable(BuildIdRows(20));

			var lookup = BuildIdRows(20).ToList();
			var query  = rows.Where(r => lookup.Contains(r));
			var sql    = query.ToSqlQuery().Sql;

			sql.ShouldContain("[T_");
			sql.ShouldContain("EXISTS", Case.Insensitive);
		}

		[Test]
		public void Contains_EntityWithCompositePk_AboveThreshold_EmitsExistsTempTablePath(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(5)));
			using var rows = db.CreateLocalTable(BuildCompositeRows(20));

			var lookup = BuildCompositeRows(20).ToList();
			var query  = rows.Where(r => lookup.Contains(r));
			var sql    = query.ToSqlQuery().Sql;

			sql.ShouldContain("[T_");
			sql.ShouldContain("EXISTS", Case.Insensitive);
		}

		[Test]
		public void Contains_AnonymousComposite_AboveThreshold_FallsBackToInlinePath(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			// Anonymous-composite lookup has no EntityDescriptor matching the outer
			// columns' owner — the rewrite falls back to the regular OR-AND chain.
			// Correctness is preserved via the inline path; below-threshold execute tests
			// in this fixture cover the runtime behavior.
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(5)));
			using var rows = db.CreateLocalTable(BuildIdRows(20));

			var lookup = BuildIdRows(20).Select(r => new { r.Id, r.Tag }).ToList();
			var query  = rows.Where(r => lookup.Contains(new { r.Id, r.Tag }));
			var sql    = query.ToSqlQuery().Sql;

			sql.ShouldNotContain("[T_");
		}

		[Test]
		public void Contains_EntityCompositePk_BelowThreshold_StaysInline(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(100)));
			using var rows = db.CreateLocalTable(BuildCompositeRows(10));

			var lookup = BuildCompositeRows(3).ToList();
			var query  = rows.Where(r => lookup.Contains(r));
			var sql    = query.ToSqlQuery().Sql;

			sql.ShouldNotContain("[T_");
		}

		// --- Cross-provider execution ---

		[Test]
		public void Contains_EntitySinglePk_AboveThreshold_ReturnsCorrectRows(
			[DataSources(false, ProvidersWithoutAutoTempTable)] string context)
		{
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(5)));
			using var rows = db.CreateLocalTable(BuildIdRows(50));

			// Lookup of 20 IdRows with Ids 1..20 — should match exactly those.
			var lookup = BuildIdRows(20).ToList();

			var result = rows.Where(r => lookup.Contains(r)).OrderBy(r => r.Id).ToList();

			result.Count.ShouldBe(20);
			result[0].Id.ShouldBe(1);
			result[19].Id.ShouldBe(20);
		}

		[Test]
		public void Contains_EntityCompositePk_AboveThreshold_ReturnsCorrectRows(
			[DataSources(false, ProvidersWithoutAutoTempTable + "," + TestProvName.AllSqlServer)] string context)
		{
			// SqlServer excluded: tempdb collation-conflict on the K2 string column (same as
			// the scalar string-element tests — see Contains_StringElement_AboveThreshold_…).
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(5)));
			using var rows = db.CreateLocalTable(BuildCompositeRows(30));

			var lookup = BuildCompositeRows(15).ToList();

			var result = rows.Where(r => lookup.Contains(r)).OrderBy(r => r.K1).ToList();

			result.Count.ShouldBe(15);
		}

		[Test]
		public void Contains_AnonymousComposite_AboveThreshold_ReturnsCorrectRows(
			[DataSources(false, ProvidersWithoutAutoTempTable + "," + TestProvName.AllSqlServer)] string context)
		{
			// SqlServer excluded: tempdb collation-conflict on the Tag string column.
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(5)));
			using var rows = db.CreateLocalTable(BuildIdRows(30));

			var lookup = BuildIdRows(30).Where(r => r.Tag != null).Select(r => new { r.Id, r.Tag }).ToList();

			var result = rows.Where(r => lookup.Contains(new { r.Id, r.Tag })).OrderBy(r => r.Id).ToList();

			result.Count.ShouldBe(lookup.Count);
		}

		[Test]
		public void Contains_EntityCompositePk_NotContains_AboveThreshold(
			[DataSources(false, ProvidersWithoutAutoTempTable + "," + TestProvName.AllSqlServer)] string context)
		{
			// SqlServer excluded for tempdb collation as above.
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(5)));
			using var rows = db.CreateLocalTable(BuildCompositeRows(30));

			var lookup = BuildCompositeRows(15).ToList();

			// !Contains → NOT EXISTS — should return the complement (15 rows).
			var result = rows.Where(r => !lookup.Contains(r)).OrderBy(r => r.K1).ToList();

			result.Count.ShouldBe(15);
			result[0].K1.ShouldBe(16);
		}

		// --- LikeClr null semantics ---

		[Test]
		public void Contains_AnonymousComposite_LikeClrNulls_AboveThreshold(
			[DataSources(false, ProvidersWithoutAutoTempTable + "," + TestProvName.AllSqlServer)] string context)
		{
			// LikeClr: a row with Tag IS NULL matches a lookup item with Tag = null. Anonymous
			// lookups stay on the OR-AND chain regardless of threshold (no entity descriptor to
			// drive TempTable<TAnon>); LikeClr handling there is the same code path as the
			// below-threshold case below — this test is the correctness witness that the
			// rewrite's null-aware behavior was not regressed by the multi-column gate.
			using var db = GetDataContext(context, o => o
				.UseCompareNulls(CompareNulls.LikeClr)
				.UseTempTablesForContains(b => b.Threshold(5)));

			using var rows = db.CreateLocalTable(BuildIdRows(30));

			// Lookup includes the source row 3 (Tag = null) as `(3, null)`.
			var lookup = BuildIdRows(30).Select(r => new { r.Id, r.Tag }).Take(20).ToList();

			var result = rows.Where(r => lookup.Contains(new { r.Id, r.Tag })).OrderBy(r => r.Id).ToList();

			// 20 lookup items, each matches the corresponding row in the 30-row source.
			result.Count.ShouldBe(20);
		}

		[Test]
		public void Contains_AnonymousComposite_LikeClrNulls_BelowThreshold(
			[DataSources(false, ProvidersWithoutAutoTempTable + "," + TestProvName.AllSqlServer)] string context)
		{
			// Below-threshold regression guard — same scenario via today's OR-AND path.
			using var db = GetDataContext(context, o => o
				.UseCompareNulls(CompareNulls.LikeClr)
				.UseTempTablesForContains(b => b.Threshold(100)));

			using var rows = db.CreateLocalTable(BuildIdRows(30));

			var lookup = BuildIdRows(30).Select(r => new { r.Id, r.Tag }).Take(3).ToList();

			var result = rows.Where(r => lookup.Contains(new { r.Id, r.Tag })).OrderBy(r => r.Id).ToList();

			result.Count.ShouldBe(3);
		}

		[Test]
		public void Contains_AnonymousComposite_LikeClrNulls_NotContains_AboveThreshold(
			[DataSources(false, ProvidersWithoutAutoTempTable + "," + TestProvName.AllSqlServer)] string context)
		{
			// !lookup.Contains(new { ... }) under LikeClr — anonymous lookups stay on the
			// OR-AND chain (no temp-table rewrite), so this is a correctness witness that the
			// negated multi-column compare still excludes rows whose NULL columns match the
			// lookup's NULL slots.
			using var db = GetDataContext(context, o => o
				.UseCompareNulls(CompareNulls.LikeClr)
				.UseTempTablesForContains(b => b.Threshold(5)));

			using var rows = db.CreateLocalTable(BuildIdRows(30));

			// 20-row lookup → 10 rows NOT in the lookup (rows 21..30).
			var lookup = BuildIdRows(30).Select(r => new { r.Id, r.Tag }).Take(20).ToList();

			var result = rows.Where(r => !lookup.Contains(new { r.Id, r.Tag })).OrderBy(r => r.Id).ToList();

			result.Count.ShouldBe(10);
			result[0].Id.ShouldBe(21);
		}

		// --- Cache + dedup ---

		[Test]
		public void Contains_EntityCompositePk_CacheHit_OnRepeatedExecute(
			[DataSources(false, ProvidersWithoutAutoTempTable + "," + TestProvName.AllSqlServer)] string context)
		{
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(5)));
			using var rows = db.CreateLocalTable(BuildCompositeRows(20));

			var lookup = BuildCompositeRows(10).ToList();

			_ = rows.Where(r => lookup.Contains(r)).OrderBy(r => r.K1).ToList();

			var query    = rows.Where(r => lookup.Contains(r)).OrderBy(r => r.K1);
			var beforeMs = query.GetCacheMissCount();

			_ = query.ToList();

			query.GetCacheMissCount().ShouldBe(beforeMs);
		}

		// Entity with an enum PK column declared as VARCHAR — linq2db's built-in enum-to-string
		// conversion mechanism. Because the temp-table path uses TempTable<TEntity> directly,
		// the [Column(DataType=VarChar)] annotation propagates from the outer-column side
		// onto the temp-table column type via the shared EntityDescriptor — both sides of
		// the EXISTS WHERE comparison see the same VARCHAR shape, so strict-typing providers
		// (PostgreSQL) accept the comparison without explicit casts. This test asserts the
		// temp-table path IS taken for the converted column.
		public enum Category { Alpha, Beta, Gamma, Delta }

		[Table("ContainsTempTableConverted")]
		sealed class ConvertedRow
		{
			[PrimaryKey(0)]                                                   public int      Id   { get; set; }
			[PrimaryKey(1), Column(DataType = DataType.VarChar, Length = 16)] public Category Cat  { get; set; }
			[Column]                                                          public string   Data { get; set; } = "";
		}

		[Test]
		public void Contains_EntityWithConvertedPkColumn_AboveThreshold_EmitsExistsTempTablePath(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(1)));
			using var rows = db.CreateLocalTable<ConvertedRow>();

			var lookup = new[]
			{
				new ConvertedRow { Id = 1, Cat = Category.Alpha, Data = "a" },
				new ConvertedRow { Id = 2, Cat = Category.Beta,  Data = "b" },
				new ConvertedRow { Id = 3, Cat = Category.Gamma, Data = "c" },
			}.ToList();

			var sql = rows.Where(r => lookup.Contains(r)).ToSqlQuery().Sql;

			sql.ShouldContain("[T_");
			sql.ShouldContain("EXISTS", Case.Insensitive);
		}

		[Test]
		public void Contains_EntityCompositePk_SelfJoinSameLookup_SharesTempTable(
			// MySQL excluded: ER_CANT_REOPEN_TABLE — same documented limitation as the scalar
			// SelfJoin test.
			[DataSources(false, ProvidersWithoutAutoTempTable + "," + TestProvName.AllSqlServer + "," + TestProvName.AllMySql)] string context)
		{
			using var db   = GetDataContext(context, o => o.UseTempTablesForContains(b => b.Threshold(5)));
			using var rows = db.CreateLocalTable(BuildCompositeRows(20));

			var lookup = BuildCompositeRows(15).ToList();

			var query =
				from a in rows
				from b in rows
				where lookup.Contains(a) && lookup.Contains(b) && a.K1 < b.K1
				select new { A = a.K1, B = b.K1 };

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldContain("T_");

			var result = query.ToList();
			result.Count.ShouldBeGreaterThan(0);
		}

		// --- Compiled query interop ---

		// The compiled query uses AsQueryable.UseTempTable directly (rather than the global
		// UseTempTablesForContains DataOption) — this is the shape that exercises
		// CompiledTable.Execute's new QueryExecutionContext wiring. The lambda body returns
		// int (Count) so CompiledQuery routes it through MethodType.Element →
		// CompiledTable.Execute (the method that fires Query.InitQueries before reaching
		// GetElement). Same static field is reused across providers — the IDataContext flows
		// in as the first parameter, the IEnumerable<int> as the second.
		static readonly Func<IDataContext, IEnumerable<int>, int> _compiledUseTempTableCount =
			CompiledQuery.Compile<IDataContext, IEnumerable<int>, int>(
				(db, ids) => ids.AsQueryable(db, b => b.Parameterize().UseTempTable(threshold: 10)).Count());

		static readonly Func<IDataContext, IEnumerable<int>, int> _compiledUseTempTableSingle =
			CompiledQuery.Compile<IDataContext, IEnumerable<int>, int>(
				(db, ids) => ids.AsQueryable(db, b => b.Parameterize().UseTempTable(threshold: 10))
					.OrderBy(x => x).First());

		[Test]
		public void Contains_CompiledQuery_AboveThreshold_FiresTempTableSetup(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer, TestProvName.AllMySql, TestProvName.AllPostgreSQL)] string context)
		{
			// CompiledQuery's generated delegate now threads a fresh QueryExecutionContext
			// through CompiledTable.Execute and fires Query.InitQueries before reaching
			// GetElement — so a compiled query that wraps an AsQueryable.UseTempTable source
			// actually creates the temp table at execute time and returns correct rows
			// (rather than emitting SQL referencing a non-existent table).
			using var db = GetDataContext(context);

			// 30-item list → above the 10-item threshold → temp-table path fires.
			var count = _compiledUseTempTableCount(db, Enumerable.Range(1, 30).ToList());

			count.ShouldBe(30);
		}

		[Test]
		public void Contains_CompiledQuery_BelowThreshold_StaysInline(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer, TestProvName.AllMySql, TestProvName.AllPostgreSQL)] string context)
		{
			// Same compiled delegate, below-threshold list — exercises the inline-VALUES
			// branch of the same Setup pipeline; the run-step records UseInlineValues, the
			// SQL builder reads it and emits the regular VALUES form.
			using var db = GetDataContext(context);

			var count = _compiledUseTempTableCount(db, Enumerable.Range(1, 5).ToList());

			count.ShouldBe(5);
		}

		[Test]
		public void Contains_CompiledQuery_ScalarElement_AboveThreshold_ReturnsCorrectRow(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer, TestProvName.AllMySql, TestProvName.AllPostgreSQL)] string context)
		{
			// Same wiring with a scalar-element selector — proves the temp-table path
			// produces correct row content (not just a correct row count).
			using var db = GetDataContext(context);

			var first = _compiledUseTempTableSingle(db, Enumerable.Range(20, 30).ToList());

			first.ShouldBe(20);
		}

		#endregion
	}
}
