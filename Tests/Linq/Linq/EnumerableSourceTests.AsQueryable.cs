using System.Collections.Generic;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
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
			[DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
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

		#endregion
	}
}
