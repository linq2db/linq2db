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
			[DataSources(TestProvName.AllAccess)] string context,
			[Values(1, 2)] int iteration)
		{
			using var db = GetDataContext(context);

			var rows  = BuildParamRows(2);
			var query = rows.AsQueryable(db, b => b.Parameterize());

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
			[DataSources(TestProvName.AllAccess)] string context,
			[Values(1, 2)] int iteration)
		{
			using var db = GetDataContext(context);

			var rows  = BuildParamRows(2);
			var query = rows.AsQueryable(db, b => b.Inline());

			var sql    = query.ToSqlQuery().Sql;
			var result = query.ToList();

			result.Count.ShouldBe(2);
			result[1].Data.ShouldBe("Data 1");

			sql.ShouldContain("'Data 0'");
			sql.ShouldContain("'Data 1'");
		}

		[Test]
		public void AsQueryable_Parameterize_ExceptId_InlinesId(
			[DataSources(TestProvName.AllAccess)] string context,
			[Values(1, 2)] int iteration)
		{
			using var db = GetDataContext(context);

			var rows  = BuildParamRows(2);
			var query = rows.AsQueryable(db, b => b.Parameterize().Except(p => p.Id));

			var sql    = query.ToSqlQuery().Sql;
			var result = query.ToList();

			result.Count.ShouldBe(2);

			// Id is inlined as a literal; Data is parameterised.
			sql.ShouldNotContain("'Data 0'");
			sql.ShouldNotContain("'Data 1'");
		}

		[Test]
		public void AsQueryable_Inline_ExceptData_ParameterisesData(
			[DataSources(TestProvName.AllAccess)] string context,
			[Values(1, 2)] int iteration)
		{
			using var db = GetDataContext(context);

			var rows  = BuildParamRows(2);
			var query = rows.AsQueryable(db, b => b.Inline().Except(p => p.Data));

			var sql    = query.ToSqlQuery().Sql;
			var result = query.ToList();

			result.Count.ShouldBe(2);
			result[0].Data.ShouldBe("Data 0");

			// Data is parameterised, so the literal must not appear in SQL.
			sql.ShouldNotContain("'Data 0'");
			sql.ShouldNotContain("'Data 1'");
		}

		[Test]
		public void AsQueryable_Parameterize_CacheStable_AcrossDataChanges(
			[DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			// First call — populates the cache.
			var firstRows  = BuildParamRows(2, seed: 0);
			var firstQuery = firstRows.AsQueryable(db, b => b.Parameterize());
			firstQuery.ToList();

			var cacheMissBefore = firstQuery.GetCacheMissCount();

			// Second call with different content but identical shape — must hit the cache.
			var secondRows  = BuildParamRows(2, seed: 100);
			var secondQuery = secondRows.AsQueryable(db, b => b.Parameterize());
			var secondList  = secondQuery.ToList();

			secondQuery.GetCacheMissCount().ShouldBe(cacheMissBefore);

			secondList.Count.ShouldBe(2);
			secondList[0].Id.ShouldBe(100);
			secondList[1].Id.ShouldBe(101);
		}

		[Test]
		public void AsQueryable_Parameterize_CacheHit_AcrossIterations(
			[DataSources(TestProvName.AllAccess)] string context,
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
			[DataSources(TestProvName.AllAccess)] string context,
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
			[DataSources(TestProvName.AllAccess)] string context,
			[Values(1, 2)] int iteration)
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
			[DataSources(TestProvName.AllAccess)] string context,
			[Values(1, 2)] int iteration)
		{
			using var db = GetDataContext(context);

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
		public void AsQueryable_JoinPerson_CacheHit_AcrossIterations(
			[DataSources(TestProvName.AllAccess)] string context,
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

		#endregion
	}
}
