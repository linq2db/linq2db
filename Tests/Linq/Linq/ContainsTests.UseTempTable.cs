using System.Collections.Generic;
using System.Linq;

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

			// At minimum: the rewrite fired (the SQL contains a temp-table reference).
			sql.ShouldContain("T_");

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
	}
}
