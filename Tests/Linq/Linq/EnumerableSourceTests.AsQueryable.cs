using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class EnumerableSourceTests : TestBase
	{
		#region AsQueryable parameterisation (issue 5424)

		sealed class ParamRow
		{
			public int     Id   { get; set; }
			public string? Data { get; set; }
		}

		static List<ParamRow> BuildParamRows(int count, int seed = 0)
		{
			var list = new List<ParamRow>(count);
			for (var i = 0; i < count; i++)
				list.Add(new ParamRow { Id = i + seed, Data = "Data " + (i + seed) });
			return list;
		}

		[Test]
		public void AsQueryable_Parameterize_AllParameters(
			[DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var rows  = BuildParamRows(2);
			var query = rows.AsQueryable(db, b => b.Parameterize()).OrderBy(r => r.Id);

			var sql    = query.ToSqlQuery().Sql;
			var result = query.ToList();

			result.Count.ShouldBe(2);
			result[0].Id.ShouldBe(0);
			result[0].Data.ShouldBe("Data 0");

			sql.ShouldNotContain("'Data 0'");
			sql.ShouldNotContain("'Data 1'");
		}

		[Test]
		public void AsQueryable_Inline_AllInlined(
			[DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			var rows  = BuildParamRows(2);
			var query = rows.AsQueryable(db, b => b.Inline()).OrderBy(r => r.Id);

			var sql    = query.ToSqlQuery().Sql;
			var result = query.ToList();

			result.Count.ShouldBe(2);
			result[1].Data.ShouldBe("Data 1");

			sql.ShouldContain("'Data 0'");
			sql.ShouldContain("'Data 1'");
		}

		[Test]
		public void AsQueryable_Parameterize_ExceptId_InlinesId(
			[DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			// Distinctive seed → Ids 7777, 7778 — unlikely to occur incidentally in generated SQL.
			var rows  = BuildParamRows(2, seed: 7777);
			var query = rows.AsQueryable(db, b => b.Parameterize().Except(p => p.Id));

			var sql    = query.ToSqlQuery().Sql;
			var result = query.ToList();

			result.Count.ShouldBe(2);

			// Id is inlined as a literal — must appear directly in the SQL.
			sql.ShouldContain("7777");
			sql.ShouldContain("7778");

			// Data is parameterised — literal must NOT appear in the SQL.
			sql.ShouldNotContain("'Data 7777'");
			sql.ShouldNotContain("'Data 7778'");
		}

		[Test]
		public void AsQueryable_Inline_ExceptData_ParameterisesData(
			[DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			// Distinctive seed → Ids 7777, 7778 — unlikely to occur incidentally in generated SQL.
			var rows  = BuildParamRows(2, seed: 7777);
			var query = rows.AsQueryable(db, b => b.Inline().Except(p => p.Data)).OrderBy(r => r.Id);

			var sql    = query.ToSqlQuery().Sql;
			var result = query.ToList();

			result.Count.ShouldBe(2);
			result[0].Data.ShouldBe("Data 7777");

			// Id is inlined as a literal — must appear directly in the SQL.
			sql.ShouldContain("7777");
			sql.ShouldContain("7778");

			// Data is parameterised — literal must NOT appear in the SQL.
			sql.ShouldNotContain("'Data 7777'");
			sql.ShouldNotContain("'Data 7778'");
		}

		[Test]
		public void AsQueryable_Parameterize_CacheStable_AcrossDataChanges(
			[DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			// First call — populates the cache.
			var firstRows  = BuildParamRows(2, seed: 0);
			var firstQuery = firstRows.AsQueryable(db, b => b.Parameterize()).OrderBy(r => r.Id);
			firstQuery.ToList();

			var cacheMissBefore = firstQuery.GetCacheMissCount();

			// Second call with different content but identical shape — must hit the cache.
			var secondRows  = BuildParamRows(2, seed: 100);
			var secondQuery = secondRows.AsQueryable(db, b => b.Parameterize()).OrderBy(r => r.Id);
			var secondList  = secondQuery.ToList();

			secondQuery.GetCacheMissCount().ShouldBe(cacheMissBefore);

			secondList.Count.ShouldBe(2);
			secondList[0].Id.ShouldBe(100);
			secondList[1].Id.ShouldBe(101);
		}

		[Test]
		public void AsQueryable_Parameterize_CacheHit_AcrossIterations(
			[DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context,
			[Values(1, 2)] int iteration)
		{
			using var db = GetDataContext(context);

			// Vary both the row count and the values across iterations — the IR shape stays the
			// same so the compiled query must be reused.
			var rows = BuildParamRows(iteration + 1, seed: iteration * 10);

			var query     = rows.AsQueryable(db, b => b.Parameterize());
			var cacheMiss = query.GetCacheMissCount();

			_ = query.ToArray();

			if (iteration > 1)
				query.GetCacheMissCount().ShouldBe(cacheMiss);
		}

		[Test]
		public void AsQueryable_Inline_CacheHit_AcrossIterations(
			[DataSources(TestProvName.AllAccess)] string context,
			[Values(1, 2)] int iteration)
		{
			using var db = GetDataContext(context);

			// Even in Inline mode, the LINQ expression tree shape doesn't change with row count or
			// values — the per-row SqlValues are produced from the materialised source at execution
			// time. The compiled query is reused across iterations.
			var rows = BuildParamRows(iteration + 1, seed: iteration * 10);

			var query     = rows.AsQueryable(db, b => b.Inline());
			var cacheMiss = query.GetCacheMissCount();

			_ = query.ToArray();

			if (iteration > 1)
				query.GetCacheMissCount().ShouldBe(cacheMiss);
		}

		[Test]
		public void AsQueryable_Parameterize_ExceptId_CacheHit_AcrossIterations(
			[DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context,
			[Values(1, 2)] int iteration)
		{
			using var db = GetDataContext(context);

			// Id is inlined (Except flips it from parameter to literal); Data is parameterised.
			// The IR shape is the same across iterations regardless of row count or values, so the cache hits.
			var rows = BuildParamRows(iteration + 1, seed: iteration * 10);

			var query     = rows.AsQueryable(db, b => b.Parameterize().Except(p => p.Id));
			var cacheMiss = query.GetCacheMissCount();

			_ = query.ToArray();

			if (iteration > 1)
				query.GetCacheMissCount().ShouldBe(cacheMiss);
		}

		[Test]
		public void AsQueryable_Parameterize_ScalarIntList(
			[DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var values = new[] { 10, 20, 30 };
			var query  = values.AsQueryable(db, b => b.Parameterize());

			var sql    = query.ToSqlQuery().Sql;
			var result = query.ToList();

			result.OrderBy(x => x).ShouldBe(new[] { 10, 20, 30 });

			// Scalar values are parameterised — assert the SQL is non-empty.
			sql.ShouldNotBeNullOrEmpty();
		}

		[Test]
		public void AsQueryable_Parameterize_InlineArray(
			[DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			// Standalone inline-array source: expression-tree preprocessing folds the array literal to a
			// ConstantExpression before EnumerableBuilder sees it, so this is functionally equivalent to
			// passing a materialised IEnumerable<T>. The configured form's NewArrayExpression reject only
			// fires when the array literal is captured inside an outer lambda (e.g. SelectMany).
			var query = new[]
			{
				new ParamRow { Id = 0, Data = "Data 0" },
				new ParamRow { Id = 1, Data = "Data 1" },
			}.AsQueryable(db, b => b.Parameterize());

			var sql    = query.ToSqlQuery().Sql;
			var result = query.ToList();

			result.Count.ShouldBe(2);

			sql.ShouldNotContain("'Data 0'");
			sql.ShouldNotContain("'Data 1'");
		}

		[Test]
		public void AsQueryable_Parameterize_InlineArrayInSelectMany_Throws(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// Inline array referencing the outer table (`t.ID`, `t.FirstName`) — the configured overload
			// can't materialise this client-side and has no per-element expression machinery. Reject
			// up-front; user should use the 2-arg AsQueryable(IDataContext) overload, which routes
			// through EnumerableContextDynamic for outer-referencing element expressions.
			var query =
				from t in db.Person
				from v in new[] { new ParamRow { Id = t.ID, Data = t.FirstName } }.AsQueryable(db, b => b.Parameterize())
				select t;

			var act = () => query.ToArray();

			act.ShouldThrow<LinqToDBException>().Message.ShouldContain("AsQueryable configure");
		}

		[Test]
		public void AsQueryable_JoinPerson_CacheHit_AcrossIterations(
			[DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context,
			[Values(1, 2)] int iteration)
		{
			using var db = GetDataContext(context);

			// Vary both the row count and the values across iterations — the IR shape stays
			// stable, so the compiled query must be reused.
			var rows = BuildParamRows(iteration + 1, seed: iteration * 10);

			var query =
				from p in db.Person
				join r in rows.AsQueryable(db, b => b.Parameterize()) on p.ID equals r.Id
				select p;

			var cacheMiss = query.GetCacheMissCount();
			_ = query.ToArray();

			if (iteration > 1)
				query.GetCacheMissCount().ShouldBe(cacheMiss);
		}

		[Test]
		public void AsQueryable_CrossApply_CacheHit_AcrossIterations(
			[IncludeDataSources(
				TestProvName.AllSqlServer2008Plus,
				TestProvName.AllPostgreSQL93Plus,
				TestProvName.AllOracle12Plus,
				TestProvName.AllMySqlWithApply)] string context,
			[Values(1, 2)] int iteration)
		{
			using var db = GetDataContext(context);

			var rows = BuildParamRows(iteration + 1, seed: iteration * 10);

			// SelectMany with Where on the outer row → CROSS APPLY shape.
			var query =
				from p in db.Person
				from r in rows.AsQueryable(db, b => b.Parameterize()).Where(r => r.Id == p.ID)
				select p;

			var cacheMiss = query.GetCacheMissCount();
			_ = query.ToArray();

			if (iteration > 1)
				query.GetCacheMissCount().ShouldBe(cacheMiss);
		}

		sealed class NestedAddr
		{
			public string? Zip { get; set; }
		}

		sealed class NestedRow
		{
			public int         Id      { get; set; }
			public NestedAddr? Address { get; set; }
		}

		[Test]
		public void AsQueryable_Parameterize_ExceptNestedMember_InlinesNested(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var rows = new List<NestedRow>
			{
				new() { Id = 7777, Address = new NestedAddr { Zip = "Z7777" } },
				new() { Id = 7778, Address = new NestedAddr { Zip = "Z7778" } },
			};

			// Projection accesses p.Address.Zip so the builder actually visits the nested member —
			// without it the builder only flattens top-level scalars and never asks the parameterization
			// config about the nested path.
			var query = from p in rows.AsQueryable(db, b => b.Parameterize().Except(p => p.Address!.Zip))
						select new { p.Id, Zip = p.Address!.Zip };

			var sql    = query.ToSqlQuery().Sql;
			var result = query.ToList();

			result.Count.ShouldBe(2);

			// Address.Zip is inlined as a literal (Except flips it from parameter to literal);
			// Id stays parameterised under the Parameterize default.
			sql.ShouldContain("'Z7777'");
			sql.ShouldContain("'Z7778'");
		}

		[Test]
		public void AsQueryable_Except_NonMemberSelector_Throws(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var rows = BuildParamRows(2);
			var act  = () => rows.AsQueryable(db, b => b.Parameterize().Except(p => p.Id + 1)).ToSqlQuery();

			act.ShouldThrow<LinqToDBException>().Message.ShouldContain("AsQueryable configure");
		}

		[Test]
		public void AsQueryable_Except_BareParameter_Throws(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var rows = BuildParamRows(2);
			var act  = () => rows.AsQueryable(db, b => b.Parameterize().Except(p => p)).ToSqlQuery();

			act.ShouldThrow<LinqToDBException>().Message.ShouldContain("AsQueryable configure");
		}

		[Test]
		public void AsQueryable_Except_CapturedExternalMember_Throws(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var rows  = BuildParamRows(2);
			var other = new ParamRow { Id = 99, Data = "X" };

			var act = () => rows.AsQueryable(db, b => b.Parameterize().Except(p => other.Id)).ToSqlQuery();

			act.ShouldThrow<LinqToDBException>().Message.ShouldContain("AsQueryable configure");
		}

		// Regression coverage for the configured 3-arg overload inside CompiledQuery.Compile with the
		// IEnumerable<T> source supplied as a compiled-query parameter. The configured path's
		// BuildTraverseExpression on sourceArg lets a free ParameterExpression flow through
		// CanBeEvaluatedOnClient, so the chain compiles and executes for any per-iteration enumerable.
		static readonly Func<IDataContext, IEnumerable<ParamRow>, IEnumerable<ParamRow>> _compiledConfiguredAsQueryable =
			CompiledQuery.Compile<IDataContext, IEnumerable<ParamRow>, IEnumerable<ParamRow>>(
				(db, rows) => rows.AsQueryable(db, b => b.Parameterize()).OrderBy(r => r.Id));

		[Test]
		public void AsQueryable_Configured_CompiledQuery_WithEnumerableParam(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var ctx = GetDataContext(context);

			// First invocation: 2 rows starting at id 0.
			var rows1   = BuildParamRows(2);
			var result1 = _compiledConfiguredAsQueryable(ctx, rows1).ToList();

			result1.Count.ShouldBe(2);
			result1[0].Id.ShouldBe(0);
			result1[0].Data.ShouldBe("Data 0");
			result1[1].Id.ShouldBe(1);
			result1[1].Data.ShouldBe("Data 1");

			// Second invocation through the same compiled delegate, with a different row count and seed.
			// Proves the compiled query is reusable across distinct enumerable arguments — the static
			// field is built once and the rows[] payload flows through as a runtime parameter on each call.
			var rows2   = BuildParamRows(3, seed: 100);
			var result2 = _compiledConfiguredAsQueryable(ctx, rows2).ToList();

			result2.Count.ShouldBe(3);
			result2[0].Id.ShouldBe(100);
			result2[0].Data.ShouldBe("Data 100");
			result2[2].Id.ShouldBe(102);
			result2[2].Data.ShouldBe("Data 102");
		}

		#endregion

		#region AsQueryable UseTempTable threshold

		// Provider families excluded from the AsQueryable.UseTempTable matrix: either they don't
		// support runtime session-scoped temp-table creation
		// (SqlProviderFlags.IsRuntimeTempTableCreationSupported is false — Oracle, Firebird, Sybase,
		// SAP HANA, Informix, DB2, SqlCe, DuckDB), don't support inline-rows sources at all
		// (Access — IsInlineRowsSourceSupported is false), or hard-throw on parameters which the
		// new chain produces (ClickHouse — BuildParameter throws). Tests below also pass
		// includeLinqService = false to DataSources because TempTable<T> requires a local
		// DataConnection/DataContext — the LinqService remote proxy isn't supported by the
		// auto-temp-table feature (temp tables can't span the client/server gRPC boundary).
		// The four supported providers — SQLite, SQL Server, PostgreSQL, MySQL — get exercised
		// by every test below.
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

		[Test]
		public void AsQueryable_UseTempTable_BelowThreshold_UsesInlineValues(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// Distinct threshold value (10000) so this test gets its own cache entry — the build-
			// time threshold is part of the expression tree (a ConstantExpression) and therefore
			// part of the Query<T> cache key. AboveThreshold uses 10; using 10000 here means the
			// two tests don't share a cached compiled query, which is what we want when asserting
			// the build-time decision (a cached temp-table-form query from another test would
			// otherwise mask the inline branch we're verifying).
			var rows  = BuildParamRows(3);
			var query = rows.AsQueryable(db, b => b.Parameterize().UseTempTable(threshold: 10000));

			var sql    = query.ToSqlQuery().Sql;
			var result = query.OrderBy(r => r.Id).ToList();

			result.Count.ShouldBe(3);
			// Below threshold: should not have materialized a real temp table (no generated T_xxxxxxxx name).
			sql.ShouldNotContain("[T_");
			sql.ShouldNotContain("CREATE TEMPORARY TABLE");
		}

		[Test]
		public void AsQueryable_UseTempTable_AboveThreshold_UsesTempTable(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var rows  = BuildParamRows(50);
			var query = rows.AsQueryable(db, b => b.Parameterize().UseTempTable(threshold: 10));

			var sql    = query.ToSqlQuery().Sql;
			var result = query.OrderBy(r => r.Id).ToList();

			result.Count.ShouldBe(50);
			result[0].Id.ShouldBe(0);
			result[49].Id.ShouldBe(49);
			// Above threshold: the main SELECT references a generated temp table, not inline VALUES.
			sql.ShouldContain("[T_");
			sql.ShouldNotContain("VALUES");
		}

		[Test]
		public void AsQueryable_UseTempTable_ComposesWithFilter(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var rows = BuildParamRows(30);

			// Compose into a non-trivial query: filter by Id range against the materialized temp table.
			var result = rows
				.AsQueryable(db, b => b.Parameterize().UseTempTable(threshold: 5))
				.Where(r => r.Id >= 10 && r.Id < 20)
				.OrderBy(r => r.Id)
				.ToList();

			result.Count.ShouldBe(10);
			result[0].Id.ShouldBe(10);
			result[9].Id.ShouldBe(19);
		}

		[Test]
		public void AsQueryable_UseTempTable_ScalarInt_AboveThreshold(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var items  = Enumerable.Range(1, 30).ToArray();
			var query  = items.AsQueryable(db, b => b.Parameterize().UseTempTable(threshold: 5))
				.Where(x => x > 20)
				.OrderBy(x => x);

			var sql    = query.ToSqlQuery().Sql;
			var result = query.ToList();

			result.Count.ShouldBe(10);
			result[0].ShouldBe(21);
			result[9].ShouldBe(30);

			// Scalar element type wraps in ValueHolder<int> internally; the column is [item].
			sql.ShouldContain("[T_");
			sql.ShouldContain("[item]");
		}

		[Test]
		public void AsQueryable_UseTempTable_ScalarString_AboveThreshold(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var items = new[] { "alpha", "beta", "gamma", "delta", "epsilon", "zeta", "eta", "theta", "iota", "kappa" };

			// Equality on the scalar value itself (no string method calls) — keeps the scope of this
			// test focused on the temp-table wrap, not on member translation for string operations.
			var result = items.AsQueryable(db, b => b.Parameterize().UseTempTable(threshold: 3))
				.Where(s => s == "alpha" || s == "iota" || s == "zeta")
				.OrderBy(s => s)
				.ToList();

			result.ShouldBe(new[] { "alpha", "iota", "zeta" });
		}

		[Test]
		public void AsQueryable_UseTempTable_ScalarInt_SelfJoin(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var items = Enumerable.Range(1, 15).ToArray();
			var q     = items.AsQueryable(db, b => b.Parameterize().UseTempTable(threshold: 5));

			var result = (from a in q
			              from b in q
			              where a < b
			              select new { a, b })
			            .OrderBy(p => p.a).ThenBy(p => p.b)
			            .ToList();

			// 15*14/2 = 105 pairs
			result.Count.ShouldBe(105);
			result[0].a.ShouldBe(1);
			result[0].b.ShouldBe(2);
		}

		[Test]
		public void AsQueryable_UseTempTable_CacheHit_AcrossIterations(
			[IncludeDataSources(TestProvName.AllSQLite)] string context,
			[Values(1, 2)] int iteration)
		{
			using var db = GetDataContext(context);

			// Different rows + different count per iteration; LINQ shape is identical so the cached
			// compiled query should be reused on the second iteration.
			var rows = BuildParamRows(iteration * 30, seed: iteration * 1000);

			var query     = rows.AsQueryable(db, b => b.Parameterize().UseTempTable(threshold: 5));
			var cacheMiss = query.GetCacheMissCount();

			var result = query.OrderBy(r => r.Id).ToArray();

			result.Length.ShouldBe(iteration * 30);

			if (iteration > 1)
				query.GetCacheMissCount().ShouldBe(cacheMiss);
		}

		[Test]
		public void AsQueryable_UseTempTable_DroppedAfterQueryExecution(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			var       infra = db as LinqToDB.Internal.Infrastructure.IInfrastructure<IDisposableTracker>;

			infra.ShouldNotBeNull();
			infra!.Instance.ActiveDisposables.Count.ShouldBe(0);

			var rows = BuildParamRows(30);

			// Default mode — no DisposeWithConnection. The temp table is created during query
			// execution and dropped right after the query completes (via the run-step's Teardown,
			// which unregisters from the tracker before dropping).
			var result = rows
				.AsQueryable(db, b => b.Parameterize().UseTempTable(threshold: 5))
				.OrderBy(r => r.Id)
				.ToList();

			result.Count.ShouldBe(30);

			// After query execution: tracker drained → temp table dropped, no leak.
			infra.Instance.ActiveDisposables.Count.ShouldBe(0);
		}

		[Test]
		public void AsQueryable_UseTempTable_DisposeWithConnection_RegistersWithTracker(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db   = GetDataContext(context);
			var       infra = db as LinqToDB.Internal.Infrastructure.IInfrastructure<IDisposableTracker>;

			infra.ShouldNotBeNull("Test provider must expose IInfrastructure<IDisposableTracker>");
			infra!.Instance.ActiveDisposables.Count.ShouldBe(0);

			var rows = BuildParamRows(30);

			var result = rows
				.AsQueryable(db, b => b.Parameterize().UseTempTable(c => c.Threshold(5).DisposeWithConnection()))
				.OrderBy(r => r.Id)
				.ToList();

			result.Count.ShouldBe(30);

			// DisposeWithConnection: temp table stays alive on the context until DC dispose.
			infra.Instance.ActiveDisposables.Count.ShouldBe(1);
		}

		[Test]
		public void TempTable_CtorAutoRegistersWithTracker(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			var       infra = db as LinqToDB.Internal.Infrastructure.IInfrastructure<IDisposableTracker>;

			infra.ShouldNotBeNull("Test provider must expose IInfrastructure<IDisposableTracker>");
			infra!.Instance.ActiveDisposables.Count.ShouldBe(0);

			using (var tt = new TempTable<ParamRow>(db, BuildParamRows(5)))
			{
				// Direct ctor (not via AsQueryable): tracker should still see the temp table as a safety net.
				infra.Instance.ActiveDisposables.Count.ShouldBe(1);
				tt.ToList().Count.ShouldBe(5);
			}

			// Manual Dispose unregisters so the tracker doesn't keep a stale reference.
			infra.Instance.ActiveDisposables.Count.ShouldBe(0);
		}

		[Test]
		public void TempTable_LeakedInstance_DroppedOnContextClose(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			IDisposableTracker tracker;

			using (var db = GetDataContext(context))
			{
				var infra = db as LinqToDB.Internal.Infrastructure.IInfrastructure<IDisposableTracker>;
				infra.ShouldNotBeNull();

				// Capture the tracker reference now — after Dispose, `Instance` throws ObjectDisposedException.
				tracker = infra!.Instance;

				// Intentionally do NOT dispose: simulate a forgotten using-block.
				var tt = new TempTable<ParamRow>(db, BuildParamRows(5));
				tt.ToList().Count.ShouldBe(5);

				tracker.ActiveDisposables.Count.ShouldBe(1);
			}

			// After context Dispose, the tracker drained its entries (drop ran on close).
			tracker.ActiveDisposables.Count.ShouldBe(0);
		}

		[Test]
		public void AsQueryable_UseTempTable_SelfJoin_DroppedAfterQueryExecution(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			var       infra = db as LinqToDB.Internal.Infrastructure.IInfrastructure<IDisposableTracker>;

			infra.ShouldNotBeNull();
			infra!.Instance.ActiveDisposables.Count.ShouldBe(0);

			var rows = BuildParamRows(20);
			var q    = rows.AsQueryable(db, b => b.Parameterize().UseTempTable(threshold: 5));

			// Same IQueryable used twice (self-join). Dedup -> one shared temp table + one run-step.
			// No DisposeWithConnection -> the single shared temp table is dropped after the query.
			var result = (from a in q
			              from b in q
			              where a.Id < b.Id
			              select new { aId = a.Id, bId = b.Id })
			            .OrderBy(p => p.aId).ThenBy(p => p.bId)
			            .ToList();

			result.Count.ShouldBe(190);          // 20*19/2 pairs
			result[0].aId.ShouldBe(0);
			result[0].bId.ShouldBe(1);

			// After query execution: tracker drained -> the one shared temp table was dropped exactly once.
			infra.Instance.ActiveDisposables.Count.ShouldBe(0);
		}

		[Test]
		public void AsQueryable_UseTempTable_SelfJoin_SharesSingleTempTable(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var rows = BuildParamRows(20);
			var q    = rows.AsQueryable(db, b => b.Parameterize().UseTempTable(threshold: 5));

			// Same IQueryable used twice in one query: should produce one CREATE TEMPORARY TABLE
			// and two FROM-clause references over the same name (CTE-style sharing).
			var query = from a in q
			            from b in q
			            where a.Id < b.Id
			            select new { aId = a.Id, bId = b.Id };

			var sql    = query.ToSqlQuery().Sql;
			var result = query.OrderBy(p => p.aId).ThenBy(p => p.bId).ToList();

			// Cartesian filtered by a.Id < b.Id over 20 rows → 20*19/2 = 190 pairs.
			result.Count.ShouldBe(190);
			result[0].aId.ShouldBe(0);
			result[0].bId.ShouldBe(1);

			// The SQL should mention the temp-table name twice (one per usage) — no two distinct names.
			var firstNameMatch = System.Text.RegularExpressions.Regex.Match(sql, @"\[T_[0-9a-f]+\]");
			firstNameMatch.Success.ShouldBeTrue();
			var distinctNames = System.Text.RegularExpressions.Regex
				.Matches(sql, @"\[T_[0-9a-f]+\]")
				.Cast<System.Text.RegularExpressions.Match>()
				.Select(m => m.Value)
				.Distinct()
				.ToList();
			distinctNames.Count.ShouldBe(1, $"Expected one shared temp-table name, got: {string.Join(", ", distinctNames)}");
		}

		// Access has IsValuesSyntaxSupported = false AND no FakeTable, so neither native VALUES nor
		// the SELECT … UNION ALL fallback produce runnable SQL (every Access SELECT requires FROM
		// <table>). It opts out of SqlProviderFlags.IsInlineRowsSourceSupported, and EnumerableBuilder
		// must refuse to translate the sequence at build time with a clear LinqToDBException rather
		// than letting the provider surface a cryptic ODBC / parser error. Covers both 2-arg and
		// 3-arg AsQueryable forms in two tests so ThrowsForProvider can match the in-flight exception
		// from the specific call shape under test (a single body would short-circuit on the first
		// throw).

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ErrorMessage = ErrorHelper.Error_AsQueryable_InlineRowsSourceNotSupported)]
		public void AsQueryable_TwoArg_OnInlineRowsUnsupportedProvider_Throws([DataSources] string context)
		{
			using var db   = GetDataContext(context);
			var       rows = BuildParamRows(3);

			_ = rows.AsQueryable(db).ToList();
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ErrorMessage = ErrorHelper.Error_AsQueryable_InlineRowsSourceNotSupported)]
		// Excludes ClickHouse because b.Parameterize() emits parameters, and ClickHouse provider
		// doesn't accept parameters in inline-VALUES rows (ClickHouseSqlBuilder.BuildParameter throws
		// "Parameters not supported for ClickHouse provider"). Same exclusion as
		// AsQueryable_Parameterize_CacheHit_AcrossIterations above.
		public void AsQueryable_Configured_OnInlineRowsUnsupportedProvider_Throws([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db   = GetDataContext(context);
			var       rows = BuildParamRows(3);

			_ = rows.AsQueryable(db, b => b.Parameterize()).ToList();
		}

		[Test]
		public void AsQueryable_UseTempTable_WithEagerLoading(
			[IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			var       infra = db as LinqToDB.Internal.Infrastructure.IInfrastructure<IDisposableTracker>;
			infra.ShouldNotBeNull();
			infra!.Instance.ActiveDisposables.Count.ShouldBe(0);

			// Pull the existing parent ids from the test database, then drive a query that joins
			// db.Parent through an AsQueryable temp-table filter and eager-loads Children. The
			// eager-loader emits a preamble query that re-uses the parent-id projection — which in
			// turn references the temp table — so this test exercises the
			// Setup-before-InitPreambles ordering: if Setup ran after the preamble fired, the
			// preamble's SELECT would hit a "no such table" error.
			var allParentIds = db.Parent.Select(p => p.ParentID).OrderBy(id => id).ToArray();
			allParentIds.Length.ShouldBeGreaterThan(5, "Test fixture must have enough parents to exceed the threshold.");

			// Wrap each id so the source is an entity-shaped IEnumerable<T>, not a scalar — the
			// non-scalar UseTempTable path is the one Setup-before-InitPreambles protects.
			var filter = allParentIds.Select(id => new ParamRow { Id = id, Data = "p" + id }).ToArray();

			var query = from row in filter.AsQueryable(db, b => b.Parameterize().UseTempTable(threshold: 3))
			            join p   in db.Parent.LoadWith(p => p.Children) on row.Id equals p.ParentID
			            orderby p.ParentID
			            select p;

			var result = query.ToList();

			result.Count.ShouldBe(allParentIds.Length);
			result[0].Children.ShouldNotBeNull();

			// Sanity: across the whole result, at least some parent has children — otherwise we
			// haven't really verified eager loading worked.
			result.Sum(p => p.Children.Count).ShouldBeGreaterThan(0);

			// Temp table dropped after both the main query and the eager-load preamble completed.
			infra.Instance.ActiveDisposables.Count.ShouldBe(0);
		}

		[Test]
		public void AsQueryable_UseTempTable_ConfigureBulkCopy_ChainCompilesAndExecutes(
			[DataSources(false, ProvidersWithoutAutoTempTable)] string context)
		{
			// Smoke-test that the full new chain shape — UseTempTable(b => b.Threshold(N)
			// .ConfigureBulkCopy(bc => bc.UseMultiRows(t => t.WithMaxBatchSize(M)))
			// .DisposeWithConnection()) — compiles, parses, executes, and returns the right rows.
			using var db    = GetDataContext(context);
			var       infra = db as LinqToDB.Internal.Infrastructure.IInfrastructure<IDisposableTracker>;
			infra.ShouldNotBeNull();
			infra!.Instance.ActiveDisposables.Count.ShouldBe(0);

			var rows = BuildParamRows(20);

			var result = rows
				.AsQueryable(db, b => b.Parameterize().UseTempTable(c => c
					.Threshold(5)
					.ConfigureBulkCopy(bc => bc.UseMultiRows(t => t.WithMaxBatchSize(5).WithBulkCopyTimeout(60)))
					.DisposeWithConnection()))
				.OrderBy(r => r.Id)
				.ToList();

			result.Count.ShouldBe(20);
			// DisposeWithConnection → table stays alive on the context.
			infra.Instance.ActiveDisposables.Count.ShouldBe(1);
		}

		[Test]
		public void AsQueryable_UseTempTable_DataOptionsDefault_AppliesTo3ArgAsQueryable(
			[DataSources(false, ProvidersWithoutAutoTempTable)] string context)
		{
			// DataOptions sets a LocalCollections default (Threshold 5). The configure lambda omits
			// UseTempTable entirely. Expected: the DataOptions default fills in and the temp-table
			// path fires when source.Count > 5.
			using var db = GetDataContext(context, o => o.UseTempTablesForLocalCollections(b => b.Threshold(5)));

			var rows = BuildParamRows(20);

			var sql = rows
				.AsQueryable(db, b => b.Parameterize())   // no UseTempTable in the chain
				.OrderBy(r => r.Id)
				.ToSqlQuery().Sql;

			// Temp-table path emitted a CREATE TABLE somewhere — the per-emission name token starts
			// with the EnumerableBuilder.GetOrAssignTempTableName prefix "T_".
			sql.ShouldContain("T_");
		}

		[Test]
		public void AsQueryable_UseTempTable_PerCallOverridesDataOptions(
			[DataSources(false, ProvidersWithoutAutoTempTable)] string context)
		{
			// DataOptions sets a low threshold (5); per-call overrides with a high threshold (10000).
			// Source has 20 rows — would trip the DataOptions threshold but not the per-call one.
			// Expected: inline VALUES (per-call wins).
			using var db = GetDataContext(context, o => o.UseTempTablesForLocalCollections(b => b.Threshold(5)));

			var rows = BuildParamRows(20);

			var sql = rows
				.AsQueryable(db, b => b.Parameterize().UseTempTable(c => c.Threshold(10000)))
				.OrderBy(r => r.Id)
				.ToSqlQuery().Sql;

			sql.ShouldNotContain("T_");                  // no temp-table token (inline-rows form
			                                             // varies per provider — VALUES, UNION ALL,
			                                             // SELECT ... FROM dual, ... — so we only
			                                             // assert the absence of the temp-table prefix)
		}

		[Test]
		public void AsQueryable_UseTempTable_PartialMerge_BulkCopyFromDataOptions(
			[DataSources(false, ProvidersWithoutAutoTempTable)] string context)
		{
			// DataOptions sets BulkCopy options + Threshold. Per-call overrides only Threshold.
			// Expected resolved spec: Threshold from per-call, BulkCopy from DataOptions
			// (per-property merge). Executes successfully — the merged BulkCopyOptions reach the
			// TempTable<T> bulk-copy without throwing.
			using var db = GetDataContext(context, o => o.UseTempTablesForLocalCollections(b => b
				.Threshold(100)
				.ConfigureBulkCopy(bc => bc.UseMultiRows(t => t.WithMaxBatchSize(5).WithBulkCopyTimeout(60)))));

			var rows = BuildParamRows(20);

			// Per-call sets only Threshold; the DataOptions BulkCopyOptions must fill in.
			var result = rows
				.AsQueryable(db, b => b.Parameterize().UseTempTable(c => c.Threshold(5)))
				.OrderBy(r => r.Id)
				.ToList();

			result.Count.ShouldBe(20);
		}

		[Test]
		public void AsQueryable_UseTempTable_DataOptionsConfigurationID_VariesWithSpec()
		{
			// Pure-DataOptions cache-key test — two DataOptions instances differing only in their
			// TempTableOptions sub-record must produce different ConfigurationIDs, so the LINQ
			// query cache invalidates when the user changes the global temp-table default. Two
			// identical sub-records must produce the same ID (cache hit).
			var optionsA = new DataOptions().UseConfiguration("SQLite.Classic")
				.UseTempTablesForLocalCollections(b => b.Threshold(10));

			var optionsB = new DataOptions().UseConfiguration("SQLite.Classic")
				.UseTempTablesForLocalCollections(b => b.Threshold(50));

			var optionsC = new DataOptions().UseConfiguration("SQLite.Classic")
				.UseTempTablesForLocalCollections(b => b.Threshold(10));

			var idA = ((LinqToDB.Internal.Common.IConfigurationID)optionsA).ConfigurationID;
			var idB = ((LinqToDB.Internal.Common.IConfigurationID)optionsB).ConfigurationID;
			var idC = ((LinqToDB.Internal.Common.IConfigurationID)optionsC).ConfigurationID;

			idA.ShouldNotBe(idB);   // different threshold → different ID
			idA.ShouldBe(idC);      // same spec → same ID (cache hit)
		}

		[Test]
		public void AsQueryable_UseTempTable_DataOptions_CacheHit_OnRepeatedExecute(
			[DataSources(false, ProvidersWithoutAutoTempTable)] string context)
		{
			// Same DataOptions, same query expression shape — second execute must hit the cache.
			// Asserts the GetCacheMissCount counter does NOT increment between two identical
			// executes (proving the cache key is stable when nothing relevant changes).
			using var db = GetDataContext(context, o => o.UseTempTablesForLocalCollections(b => b.Threshold(5)));

			var rows = BuildParamRows(20);

			// First execute — populates the cache (miss is expected and uncounted by this test).
			_ = rows.AsQueryable(db).OrderBy(r => r.Id).ToList();

			// Second execute — same expression, same DataOptions: snapshot miss count, execute,
			// assert the count didn't budge.
			var query    = rows.AsQueryable(db).OrderBy(r => r.Id);
			var beforeMs = query.GetCacheMissCount();

			_ = query.ToList();

			query.GetCacheMissCount().ShouldBe(beforeMs);
		}

		[Test]
		public void AsQueryable_UseTempTable_PerCall_CacheHit_OnRepeatedExecute_Final(
			[DataSources(false, ProvidersWithoutAutoTempTable)] string context,
			[Values(1, 2)] int iteration)
		{
			// Per-call UseTempTable chain — same configure across iterations must hit the cache.
			// Iteration 2 reuses the cached Query<T> produced by iteration 1; miss count snapshot
			// before+after must match.
			using var db = GetDataContext(context);

			var rows = BuildParamRows(iteration + 1, seed: iteration * 10);

			var query =
				rows.AsQueryable(db, c => c.Parameterize().UseTempTable(b => b.Threshold(2)))
				    .OrderBy(r => r.Id);

			var cacheMiss = query.GetCacheMissCount();
			_ = query.ToList();

			if (iteration > 1)
				query.GetCacheMissCount().ShouldBe(cacheMiss);
		}

		[Test]
		public void AsQueryable_UseTempTable_PerCall_SpecChange_ProducesFreshSql_Final(
			[DataSources(false, ProvidersWithoutAutoTempTable)] string context)
		{
			// Per-call UseTempTable chain — two queries that differ ONLY in the literal threshold
			// must produce different SQL. If the cache key didn't pick up the resolved
			// TempTableSpec, the second call would reuse the first translation and emit identical
			// SQL — silently ignoring the new threshold. The threshold-100 case stays inline
			// (source < 100); the threshold-5 case promotes to a temp table.
			using var db = GetDataContext(context);

			var rows = BuildParamRows(20);

			var sqlInline = rows
				.AsQueryable(db, c => c.Parameterize().UseTempTable(b => b.Threshold(100)))
				.OrderBy(r => r.Id)
				.ToSqlQuery().Sql;

			sqlInline.ShouldNotContain("T_");

			var sqlTemp = rows
				.AsQueryable(db, c => c.Parameterize().UseTempTable(b => b.Threshold(5)))
				.OrderBy(r => r.Id)
				.ToSqlQuery().Sql;

			sqlTemp.ShouldContain("T_");
		}

		[Test]
		public void AsQueryable_UseTempTable_PerCall_CacheHit_OnRepeatedExecute_InJoin(
			[DataSources(false, ProvidersWithoutAutoTempTable)] string context,
			[Values(1, 2)] int iteration)
		{
			// AsQueryable used inside a LINQ join expression — same configure across iterations
			// must hit the cache. Mirrors AsQueryable_JoinPerson_CacheHit_AcrossIterations's
			// pattern but with UseTempTable in the chain.
			using var db = GetDataContext(context);

			var rows = BuildParamRows(iteration + 1, seed: iteration * 10);

			var query =
				from p in db.Person
				join r in rows.AsQueryable(db, c => c.Parameterize().UseTempTable(b => b.Threshold(2))) on p.ID equals r.Id
				select p;

			var cacheMiss = query.GetCacheMissCount();
			_ = query.ToArray();

			if (iteration > 1)
				query.GetCacheMissCount().ShouldBe(cacheMiss);
		}

		[Test]
		public void AsQueryable_UseTempTable_PerCall_SpecChange_ProducesFreshSql_InJoin(
			[DataSources(false, ProvidersWithoutAutoTempTable)] string context)
		{
			// AsQueryable inside a LINQ join — same correctness property as the Final variant
			// above, just exercises the in-LINQ-expression position. The join is the same; only
			// the inner UseTempTable spec changes.
			using var db = GetDataContext(context);

			var rows = BuildParamRows(20);

			var sqlInline = (
				from p in db.Person
				join r in rows.AsQueryable(db, c => c.Parameterize().UseTempTable(b => b.Threshold(100))) on p.ID equals r.Id
				select p
			).ToSqlQuery().Sql;

			sqlInline.ShouldNotContain("T_");

			var sqlTemp = (
				from p in db.Person
				join r in rows.AsQueryable(db, c => c.Parameterize().UseTempTable(b => b.Threshold(5))) on p.ID equals r.Id
				select p
			).ToSqlQuery().Sql;

			sqlTemp.ShouldContain("T_");
		}

		[Test]
		public void AsQueryable_UseTempTable_DataOptions_SpecChange_ProducesFreshSql(
			[DataSources(false, ProvidersWithoutAutoTempTable)] string context)
		{
			// End-to-end cache-correctness check — two DataOptions differing only in
			// UseTempTablesForLocalCollections threshold must produce different SQL for the
			// same LINQ expression. If the cache key didn't incorporate TempTableOptions, the
			// second call would reuse the first translation and emit the same SQL, silently
			// ignoring the new spec — exactly the bug ConfigurationID participation prevents.
			var rows = BuildParamRows(20);

			// Threshold above row count → inline VALUES.
			using (var dbInline = new DataConnection(new DataOptions()
				.UseConfiguration(context)
				.UseTempTablesForLocalCollections(b => b.Threshold(100))))
			{
				var sqlInline = rows.AsQueryable(dbInline).OrderBy(r => r.Id).ToSqlQuery().Sql;
				sqlInline.ShouldNotContain("T_");
			}

			// Threshold below row count → temp table.
			using (var dbTemp = new DataConnection(new DataOptions()
				.UseConfiguration(context)
				.UseTempTablesForLocalCollections(b => b.Threshold(5))))
			{
				var sqlTemp = rows.AsQueryable(dbTemp).OrderBy(r => r.Id).ToSqlQuery().Sql;
				sqlTemp.ShouldContain("T_");   // temp-table name token
			}

			// Repeat the inline scenario one more time — different DataConnection but same spec —
			// to confirm the cache key for the inline-VALUES translation is stable and we didn't
			// accidentally tie it to the DataConnection instance identity.
			using var dbInline2 = new DataConnection(new DataOptions()
				.UseConfiguration(context)
				.UseTempTablesForLocalCollections(b => b.Threshold(100)));

			var sqlInline2 = rows.AsQueryable(dbInline2).OrderBy(r => r.Id).ToSqlQuery().Sql;
			sqlInline2.ShouldNotContain("T_");
		}

		#endregion
	}
}
