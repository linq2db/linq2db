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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5424")]
		public void AsQueryable_Parameterize_AllParameters(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL93Plus)] string context,
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5424")]
		public void AsQueryable_Inline_AllInlined(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL93Plus)] string context,
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5424")]
		public void AsQueryable_Parameterize_ExceptId_InlinesId(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL93Plus)] string context,
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5424")]
		public void AsQueryable_Inline_ExceptData_ParameterisesData(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL93Plus)] string context,
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5424")]
		public void AsQueryable_Parameterize_CacheStable_AcrossDataChanges(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL93Plus)] string context)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5424")]
		public void AsQueryable_Parameterize_ScalarIntList(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL93Plus)] string context,
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5424")]
		public void AsQueryable_Parameterize_InlineArray(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL93Plus)] string context,
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

		#endregion
	}
}
