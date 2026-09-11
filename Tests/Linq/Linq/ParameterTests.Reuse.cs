using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class ParameterTests
	{
		sealed class ReuseCounter
		{
			public int Value;
			public int Next() => Value++;
		}

		sealed class ReuseHolder
		{
			public static ReuseHolder Shared = new() { ValueProperty = "str" };

			public string? ValueField;
			public string? ValueProperty { get; set; }

			public static ReuseHolder Get() => Shared;
		}

		sealed class ReuseOuter
		{
			public ReuseHolder? Inner;
		}

		sealed class ReuseIdSource
		{
			public int Value;

			public List<int> Next()
			{
				return new List<int> { Value++ };
			}
		}

		#region A repeated expression is shared only when both occurrences produce the same value

		[Test]
		public void ParameterReuse_ImpureExpression_EvaluatedPerOccurrence([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var counter = new ReuseCounter();

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.Int1 == counter.Next() || t.Int2 == counter.Next())
				.ToSqlQuery();

			// The two calls return different values, so they cannot share a parameter and each
			// occurrence must carry its own.
			sql.Parameters.Count.ShouldBe(2);
			sql.Parameters[0].Value.ShouldNotBe(sql.Parameters[1].Value);
		}

		[Test]
		public void ParameterReuse_ImpureExpression_CostDoesNotGrowPerBuild([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var counter    = new ReuseCounter();
			var perBuild   = new List<int>();
			var previous   = 0;

			for (var i = 0; i < 4; i++)
			{
				var sql = db.GetTable<ParameterDeduplication>()
					.Where(t => t.Int1 == counter.Next() || t.Int2 == counter.Next())
					.ToSqlQuery();

				sql.Parameters.Count.ShouldBe(2);

				perBuild.Add(counter.Value - previous);
				previous = counter.Value;
			}

			// Sharing a parameter whose occurrences disagree used to leave the registered duplicate check
			// permanently unsatisfied: the plan stayed at one parameter and the accessors were re-evaluated
			// more times on every rebuild (1, 5, 9, 13 ... evaluations). The first build is the expensive
			// one (cache miss); pin that the cost then reaches a steady state instead of growing.
			perBuild[^1].ShouldBe(perBuild[^2]);
		}

		[Test]
		public void ParameterReuse_MethodCallWithEqualValues_Reused([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			// The contract is about the values, not about the shape of the expression: both calls return the
			// same value, so one parameter carries it. ParameterTests.Caching pins the same contract.
			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == ReuseHolder.Get().ValueProperty || t.String3 == ReuseHolder.Get().ValueProperty)
				.ToSqlQuery();

			sql.Parameters.Count.ShouldBe(1);
		}

		[Test]
		public void ParameterReuse_ConversionCallWithEqualValues_Reused([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			int? value = 1;

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.Int1 == value.GetValueOrDefault() || t.Int2 == value.GetValueOrDefault())
				.ToSqlQuery();

			sql.Parameters.Count.ShouldBe(1);
		}

		[Test]
		public void ParameterReuse_ImpureExpression_EachOccurrenceFiltersItsOwnValue([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Enumerable.Range(0, 20)
				.Select(v => new ParameterDeduplication { Id = v + 1, Int1 = v, Int2 = v })
				.ToArray());

			var counter = new ReuseCounter();

			// Every row repeats its value in both columns, so a predicate that binds two different values
			// selects two rows while one that reuses a single value for both sides selects one - whatever
			// the counter happens to stand at. Binding one value to both predicates is what the shared
			// parameter did, and no assertion on the generated SQL alone would catch the wrong rows.
			var ids = table
				.Where(t => t.Int1 == counter.Next() || t.Int2 == counter.Next())
				.Select(t => t.Id)
				.ToArray();

			ids.Distinct().Count().ShouldBe(2);
		}

		[Test]
		public void ParameterReuse_ImpureCollection_EachOccurrenceFiltersItsOwnValues([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Enumerable.Range(0, 20)
				.Select(v => new ParameterDeduplication { Id = v + 1, Int1 = v, Int2 = v })
				.ToArray());

			var source = new ReuseIdSource();

			// Collection parameters of an IN predicate take the same path: two calls return two different
			// collections, so the predicates must not share one. Every row repeats its value in both columns,
			// so two distinct collections select two rows while a shared one selects a single row.
			var ids = table
				.Where(t => source.Next().Contains(t.Int1) || source.Next().Contains(t.Int2))
				.Select(t => t.Id)
				.ToArray();

			ids.Distinct().Count().ShouldBe(2);
		}

		[Test]
		public void ParameterReuse_CapturedCollection_Reused([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var values = new List<int> { 1, 2, 3 };

			// The same captured collection on both sides is one value, so it stays one occurrence.
			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => values.Contains(t.Int1) || values.Contains(t.Int2))
				.ToSqlQuery();

			// Cast first: on net462 MatchCollection implements only the non-generic IEnumerable, so Select
			// would not bind to Enumerable.Select there.
			var lists = Regex.Matches(sql.Sql, @"IN \(([^)]*)\)")
				.Cast<Match>()
				.Select(m => m.Groups[1].Value)
				.ToArray();

			lists.Length.ShouldBe(2);
			lists[0].ShouldBe(lists[1]);
		}

		[Test]
		public void ParameterReuse_ThrowingExpression_KeepsOriginalException([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var empty = new List<int>();

			// Comparing both occurrences means evaluating them at build time, which runs the user's own
			// code. When that throws, the exception must reach the caller as-is: ParametersContext
			// .BuildParameter treats any throw as "not a parameter", which would otherwise turn this into
			// "the LINQ expression could not be converted to SQL" and hide the real cause.
			Shouldly.Should.Throw<InvalidOperationException>(() => db.GetTable<ParameterDeduplication>()
				.Where(t => t.Int1 == empty.First() || t.Int2 == empty.First())
				.ToSqlQuery());
		}

		#endregion

		#region Captured member access keeps being reused

		[Test]
		public void ParameterReuse_CapturedLocal_Reused([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var value = 1;

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.Int1 == value || t.Int2 == value)
				.ToSqlQuery();

			sql.Parameters.Count.ShouldBe(1);
		}

		[Test]
		public void ParameterReuse_CapturedField_Reused([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var holder = new ReuseHolder { ValueField = "str" };

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == holder.ValueField || t.String3 == holder.ValueField)
				.ToSqlQuery();

			sql.Parameters.Count.ShouldBe(1);
		}

		[Test]
		public void ParameterReuse_CapturedProperty_Reused([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var holder = new ReuseHolder { ValueProperty = "str" };

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == holder.ValueProperty || t.String3 == holder.ValueProperty)
				.ToSqlQuery();

			sql.Parameters.Count.ShouldBe(1);
		}

		[Test]
		public void ParameterReuse_CapturedNestedChain_Reused([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var outer = new ReuseOuter { Inner = new ReuseHolder { ValueProperty = "str" } };

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == outer.Inner!.ValueProperty || t.String3 == outer.Inner!.ValueProperty)
				.ToSqlQuery();

			sql.Parameters.Count.ShouldBe(1);
		}

		[Test]
		public void ParameterReuse_StaticMemberChain_Reused([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == ReuseHolder.Shared.ValueProperty || t.String3 == ReuseHolder.Shared.ValueProperty)
				.ToSqlQuery();

			sql.Parameters.Count.ShouldBe(1);
		}

		[Test]
		public void ParameterReuse_NullableValue_Reused([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			int? value = 1;

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.Int1 == value!.Value || t.Int2 == value!.Value)
				.ToSqlQuery();

			sql.Parameters.Count.ShouldBe(1);
		}

		#endregion

		#region One value threaded through nested filter helpers collapses to one parameter

		// Each helper captures the value in its own closure, so the parameter expressions are not
		// structurally equal - they are matched by parameter path name plus evaluated value instead.

		static IQueryable<ParameterDeduplication> WhereInt1(IQueryable<ParameterDeduplication> query, int value)
		{
			return query.Where(t => t.Int1 == value);
		}

		static IQueryable<ParameterDeduplication> WhereInt2(IQueryable<ParameterDeduplication> query, int value)
		{
			return WhereInt1(query, value).Where(t => t.Int2 == value);
		}

		static IQueryable<ParameterDeduplication> WhereIntN1(IQueryable<ParameterDeduplication> query, int value)
		{
			return WhereInt2(query, value).Where(t => t.IntN1 == value);
		}

		static IQueryable<ParameterDeduplication> WhereIntN2(IQueryable<ParameterDeduplication> query, int value)
		{
			return WhereIntN1(query, value).Where(t => t.IntN2 == value);
		}

		static IQueryable<ParameterDeduplication> WhereRenamedInner(IQueryable<ParameterDeduplication> query, int cutoff)
		{
			return query.Where(t => t.Int1 == cutoff);
		}

		static IQueryable<ParameterDeduplication> WhereRenamedOuter(IQueryable<ParameterDeduplication> query, int value)
		{
			return WhereRenamedInner(query, value).Where(t => t.Int2 == value);
		}

		static IQueryable<ParameterDeduplication> WhereVarChar(IQueryable<ParameterDeduplication> query, string value)
		{
			return query.Where(t => t.String1 == value);
		}

		static IQueryable<ParameterDeduplication> WhereNVarChar(IQueryable<ParameterDeduplication> query, string value)
		{
			return WhereVarChar(query, value).Where(t => t.String2 == value);
		}

		[Test]
		public void ParameterReuse_NestedFilters_SameName_OneParameter([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var sql = WhereInt2(db.GetTable<ParameterDeduplication>(), 1).ToSqlQuery();

			sql.Parameters.Count.ShouldBe(1);
			sql.Parameters[0].Value.ShouldBe(1);
		}

		[Test]
		public void ParameterReuse_DeeplyNestedFilters_SameName_OneParameter([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var sql = WhereIntN2(db.GetTable<ParameterDeduplication>(), 1).ToSqlQuery();

			sql.Parameters.Count.ShouldBe(1);
			sql.Parameters[0].Value.ShouldBe(1);
		}

		[Test]
		public void ParameterReuse_NestedFilters_DifferentNames([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			// The parameter path name is part of the reuse identity on purpose: two differently named
			// values that merely happen to be equal on this run stay separate parameters.
			var sql = WhereRenamedOuter(db.GetTable<ParameterDeduplication>(), 1).ToSqlQuery();

			sql.Parameters.Count.ShouldBe(2);
		}

		[Test]
		public void ParameterReuse_NestedFilters_DifferentColumnTypes([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			// String1 is VarChar, String2 is NVarChar. Reuse requires the two occurrences to agree on
			// DataType / DbType / Length / Precision / Scale, and a single DbParameter can only carry one
			// of them, so the value is passed twice. Unlike the differing-name case above this is a
			// limitation rather than a deliberate distinction: nothing here decides whether two DB types
			// are compatible enough to share one widened parameter.
			var sql = WhereNVarChar(db.GetTable<ParameterDeduplication>(), "str").ToSqlQuery();

			sql.Parameters.Count.ShouldBe(2);
		}

		#endregion

		#region Many independently filtered sources joined into one query share the parameter

		// The realistic reporting shape: several helpers each filter their own source by the same
		// value, and the results are joined. Every helper closes over the value separately, so this
		// leans entirely on the by-name-and-value match.

		static IQueryable<ParameterDeduplication> ByInt1(IQueryable<ParameterDeduplication> query, int value)
		{
			return query.Where(t => t.Int1 == value);
		}

		static IQueryable<ParameterDeduplication> ByInt2(IQueryable<ParameterDeduplication> query, int value)
		{
			return query.Where(t => t.Int2 == value);
		}

		static IQueryable<ParameterDeduplication> ByIntN1(IQueryable<ParameterDeduplication> query, int value)
		{
			return query.Where(t => t.IntN1 == value);
		}

		static IQueryable<ParameterDeduplication> ByIntN2(IQueryable<ParameterDeduplication> query, int value)
		{
			return query.Where(t => t.IntN2 == value);
		}

		static IQueryable<ParameterDeduplication> ExistsByInt1(IQueryable<ParameterDeduplication> query, IQueryable<ParameterDeduplication> probe, int value)
		{
			return query.Where(t => probe.Any(p => p.Id == t.Id && p.Int1 == value));
		}

		[Test]
		public void ParameterReuse_JoinedFilteredSources_SameName_OneParameter([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var value = 1;
			var table = db.GetTable<ParameterDeduplication>();

			var query =
				from a in ByInt1(table, value)
				join b in ByInt2(table, value)  on a.Id equals b.Id
				join c in ByIntN1(table, value) on a.Id equals c.Id
				join d in ByIntN2(table, value) on a.Id equals d.Id
				select new { a.Id, b.Int2, c.IntN1, d.IntN2 };

			var sql = query.ToSqlQuery();

			sql.Parameters.Count.ShouldBe(1);
			sql.Parameters[0].Value.ShouldBe(value);
		}

		[Test]
		public void ParameterReuse_JoinedFilteredSubqueries_SameName_OneParameter([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var value = 1;
			var table = db.GetTable<ParameterDeduplication>();

			// AsSubQuery keeps each filtered source a separate derived table, so the four WHERE clauses
			// are not merged into one scan and the parameter really is shared across four query blocks.
			var query =
				from a in ByInt1(table, value).AsSubQuery()
				join b in ByInt2(table, value).AsSubQuery()  on a.Id equals b.Id
				join c in ByIntN1(table, value).AsSubQuery() on a.Id equals c.Id
				join d in ByIntN2(table, value).AsSubQuery() on a.Id equals d.Id
				select new { a.Id, b.Int2, c.IntN1, d.IntN2 };

			var sql = query.ToSqlQuery();

			sql.Parameters.Count.ShouldBe(1);
			sql.Parameters[0].Value.ShouldBe(value);
		}

		[Test]
		public void ParameterReuse_JoinedFilteredSources_WithCorrelatedExists_OneParameter([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var value = 1;
			var table = db.GetTable<ParameterDeduplication>();

			var query =
				from a in ByInt1(table, value)
				join b in ByInt2(table, value) on a.Id equals b.Id
				select new { a.Id, b.Int2 };

			var filtered =
				from x in ExistsByInt1(table, ByIntN1(table, value), value)
				join y in query on x.Id equals y.Id
				where x.IntN2 == value
				select new { x.Id, y.Int2 };

			var sql = filtered.ToSqlQuery();

			sql.Parameters.Count.ShouldBe(1);
			sql.Parameters[0].Value.ShouldBe(value);
		}

		#endregion
	}
}
