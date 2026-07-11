using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class AggregationNullabilityTests : TestBase
	{
		#region Model

		[Table]
		sealed class Outer
		{
			[PrimaryKey] public int     Id      { get; set; }
			[Column]     public int     Group   { get; set; }

			[Column(Configuration = ProviderName.Access, DataType = DataType.Money)]
			[Column]     public decimal Anchor  { get; set; }

			public static readonly Outer[] Data =
			{
				// Group 1 has matching rows in Inner; Group 99 has none (empty subquery).
				new() { Id = 1, Group = 1,  Anchor = 100m },
				new() { Id = 2, Group = 99, Anchor = 200m },
			};
		}

		[Table]
		sealed class Inner
		{
			[PrimaryKey] public int     Id          { get; set; }
			[Column]     public int     Group       { get; set; }

			[Column] public int      IntValue       { get; set; }
			[Column] public int?     IntValueN      { get; set; }

			[Column(Configuration = ProviderName.Access, DataType = DataType.Int32)]
			[Column] public long     LongValue      { get; set; }

			[Column(Configuration = ProviderName.Access, DataType = DataType.Int32)]
			[Column] public long?    LongValueN     { get; set; }

			[Column] public float    FloatValue     { get; set; }
			[Column] public float?   FloatValueN    { get; set; }
			[Column] public double   DoubleValue    { get; set; }
			[Column] public double?  DoubleValueN   { get; set; }

			[Column(Configuration = ProviderName.Access, DataType = DataType.Money)]
			[Column] public decimal  DecimalValue   { get; set; }

			[Column(Configuration = ProviderName.Access, DataType = DataType.Money)]
			[Column] public decimal? DecimalValueN  { get; set; }

			public static readonly Inner[] Data =
			{
				new() { Id = 1, Group = 1,
					IntValue = 3, IntValueN = 3,
					LongValue = 3L, LongValueN = 3L,
					FloatValue = 3f, FloatValueN = 3f,
					DoubleValue = 3d, DoubleValueN = 3d,
					DecimalValue = 3m, DecimalValueN = 3m,
				},
				new() { Id = 2, Group = 1,
					IntValue = 7, IntValueN = 7,
					LongValue = 7L, LongValueN = 7L,
					FloatValue = 7f, FloatValueN = 7f,
					DoubleValue = 7d, DoubleValueN = 7d,
					DecimalValue = 7m, DecimalValueN = 7m,
				},
			};
		}

		#endregion

		#region Subquery arithmetic with aggregate (#5404 shape)

		// Reproduces the shape from #5404: outer arithmetic minus a subquery aggregate.
		// Empty inner must yield outer anchor unchanged (Sum semantics: empty -> 0).
		[Test]
		public void DecimalSumSubqueryArithmeticEmpty([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable(Outer.Data);
			using var inner = db.CreateLocalTable(Inner.Data);

			var query =
				from o in outer
				where o.Id == 2 // Group=99, no matching inner rows
				select o.Anchor - inner.Where(i => i.Group == o.Group).Sum(i => i.DecimalValue);

			query.First().ShouldBe(200m);
		}

		[Test]
		public void DecimalSumSubqueryArithmeticNonEmpty([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable(Outer.Data);
			using var inner = db.CreateLocalTable(Inner.Data);

			var query =
				from o in outer
				where o.Id == 1 // Group=1, has 3+7=10 in inner
				select o.Anchor - inner.Where(i => i.Group == o.Group).Sum(i => i.DecimalValue);

			query.First().ShouldBe(90m);
		}

		#endregion

		#region Sum overloads — non-nullable in subquery (must wrap with COALESCE)

		[Test]
		public void SumIntSubqueryEmpty([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable(Outer.Data);
			using var inner = db.CreateLocalTable(Inner.Data);

			var query =
				from o in outer
				where o.Id == 2
				select 1000 - inner.Where(i => i.Group == o.Group).Sum(i => i.IntValue);

			query.First().ShouldBe(1000);
		}

		[Test]
		public void SumLongSubqueryEmpty([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable(Outer.Data);
			using var inner = db.CreateLocalTable(Inner.Data);

			var query =
				from o in outer
				where o.Id == 2
				select 1000L - inner.Where(i => i.Group == o.Group).Sum(i => i.LongValue);

			query.First().ShouldBe(1000L);
		}

		[Test]
		public void SumFloatSubqueryEmpty([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable(Outer.Data);
			using var inner = db.CreateLocalTable(Inner.Data);

			var query =
				from o in outer
				where o.Id == 2
				select 1000f - inner.Where(i => i.Group == o.Group).Sum(i => i.FloatValue);

			query.First().ShouldBe(1000f);
		}

		[Test]
		public void SumDoubleSubqueryEmpty([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable(Outer.Data);
			using var inner = db.CreateLocalTable(Inner.Data);

			var query =
				from o in outer
				where o.Id == 2
				select 1000d - inner.Where(i => i.Group == o.Group).Sum(i => i.DoubleValue);

			query.First().ShouldBe(1000d);
		}

		[Test]
		public void SumDecimalSubqueryEmpty([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable(Outer.Data);
			using var inner = db.CreateLocalTable(Inner.Data);

			var query =
				from o in outer
				where o.Id == 2
				select 1000m - inner.Where(i => i.Group == o.Group).Sum(i => i.DecimalValue);

			query.First().ShouldBe(1000m);
		}

		#endregion

		#region Sum overloads — nullable in subquery (must NOT wrap with COALESCE)

		[Test]
		public void SumNullableIntSubqueryEmpty_NoCoalesce([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable(Outer.Data);
			using var inner = db.CreateLocalTable(Inner.Data);

			var query =
				from o in outer
				where o.Id == 2
				select inner.Where(i => i.Group == o.Group).Sum(i => i.IntValueN);

			// nullable return — translator should not wrap with COALESCE
			query.ToSqlQuery().Sql.ToUpperInvariant().ShouldNotContain("COALESCE");
			query.First().ShouldBeNull();
		}

		[Test]
		public void SumNullableDecimalSubqueryEmpty_NoCoalesce([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable(Outer.Data);
			using var inner = db.CreateLocalTable(Inner.Data);

			var query =
				from o in outer
				where o.Id == 2
				select inner.Where(i => i.Group == o.Group).Sum(i => i.DecimalValueN);

			query.ToSqlQuery().Sql.ToUpperInvariant().ShouldNotContain("COALESCE");
			query.First().ShouldBeNull();
		}

		#endregion

		#region Min / Max / Avg in subquery — non-nullable keeps validator (LINQ throws on empty)

		// COALESCE is intentionally NOT applied to Min/Max/Avg: LINQ defines empty -> throw
		// InvalidOperationException for those, and silently substituting a default would
		// contradict both LINQ semantics and the existing Tests.Exceptions.AggregationTests.
		[Test]
		public void MinIntSubqueryNonEmpty([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable(Outer.Data);
			using var inner = db.CreateLocalTable(Inner.Data);

			var query =
				from o in outer
				where o.Id == 1
				select inner.Where(i => i.Group == o.Group).Min(i => i.IntValue);

			query.ToSqlQuery().Sql.ToUpperInvariant().ShouldNotContain("COALESCE");
			query.First().ShouldBe(3);
		}

		[Test]
		public void MaxIntSubqueryNonEmpty([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable(Outer.Data);
			using var inner = db.CreateLocalTable(Inner.Data);

			var query =
				from o in outer
				where o.Id == 1
				select inner.Where(i => i.Group == o.Group).Max(i => i.IntValue);

			query.ToSqlQuery().Sql.ToUpperInvariant().ShouldNotContain("COALESCE");
			query.First().ShouldBe(7);
		}

		[Test]
		public void AverageIntSubqueryNonEmpty([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable(Outer.Data);
			using var inner = db.CreateLocalTable(Inner.Data);

			var query =
				from o in outer
				where o.Id == 1
				select inner.Where(i => i.Group == o.Group).Average(i => i.IntValue);

			query.ToSqlQuery().Sql.ToUpperInvariant().ShouldNotContain("COALESCE");
			query.First().ShouldBe(5d); // (3+7)/2 = 5
		}

		// Empty Max in projection-as-aggregate must throw via the runtime validator
		// (matches Enumerable.Max contract and Tests.Exceptions.AggregationTests.NonNullableMax2).
		[Test]
		public void MaxIntSubqueryEmpty_Throws([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable(Outer.Data);
			using var inner = db.CreateLocalTable(Inner.Data);

			var query =
				from o in outer
				where o.Id == 2
				select inner.Where(i => i.Group == o.Group).Max(i => i.IntValue);

			Action act = () => query.First();
			act.ShouldThrow<InvalidOperationException>();
		}

		#endregion

		#region #5699 — nullable Sum(lambda) in a subquery keeps NULL semantics

		// Coverage for #5699 (reported symptom could NOT be reproduced). The report showed a
		// nullable subquery Sum wrapped in COALESCE(...,0), which drops an outer `!= null` guard.
		// We could not reproduce that on 6.3.0 or master, SQLite or SQL Server — the reporter's
		// posted repro was incomplete (custom fluent mapping + a `For<T>()` helper not shown).
		// These tests pin the correct current behavior: a nullable `Sum(x => (decimal?)x.Col)` over
		// a non-nullable column, summed in a subquery, keeps SQL NULL semantics (no COALESCE), so an
		// outer `== null` / `!= null` guard stays meaningful. (Note the cast is the Sum lambda
		// argument — `.Select(x => (decimal?)x).Sum()` takes a different, always-nullable path.)

		// `where billTotal != null` must keep the predicate (COALESCE would make it always-true).
		[Test]
		public void Issue5699_NullableSubquerySum_NotNullGuardKept([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable(Outer.Data);
			using var inner = db.CreateLocalTable(Inner.Data);

			var query =
				from o in outer
				let billTotal = inner.Where(i => i.Group == o.Group).Sum(i => (decimal?)i.DecimalValue)
				where billTotal != null
				select o.Id;

			// Only Group 1 (Id = 1) has inner rows -> billTotal non-null; Group 99 -> NULL, excluded.
			// If billTotal were COALESCE'd to 0 (the reported #5699 symptom), `!= null` would be
			// always-true, the WHERE would be dropped, and both rows would come back ({ 1, 2 }).
			query.ToArray().ShouldBe(new[] { 1 });
		}

		// `where billTotal == null` selects the empty-group row, and no COALESCE is emitted.
		[Test]
		public void Issue5699_NullableSubquerySum_IsNullOnEmpty([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db    = GetDataContext(context);
			using var outer = db.CreateLocalTable(Outer.Data);
			using var inner = db.CreateLocalTable(Inner.Data);

			var query =
				from o in outer
				let billTotal = inner.Where(i => i.Group == o.Group).Sum(i => (decimal?)i.DecimalValue)
				where billTotal == null
				select o.Id;

			// Group 99 (Id = 2) has no inner rows -> nullable Sum -> NULL. If it were COALESCE'd to 0
			// (the reported #5699 symptom) this row would be dropped (result would be empty).
			query.ToArray().ShouldBe(new[] { 2 });

			// nullable result -> no COALESCE wrapping at the SQL level
			query.ToSqlQuery().Sql.ToUpperInvariant().ShouldNotContain("COALESCE");
		}

		#endregion

		#region Top-level non-nullable Sum — unchanged (must NOT wrap)

		[Test]
		public void SumDecimalTopLevel_NoCoalesce([DataSources(false)] string context)
		{
			using var db    = GetDataConnection(context);
			using var inner = db.CreateLocalTable(Inner.Data);

			var query = inner.Where(i => i.Group == 1).Select(i => i.DecimalValue);
			// non-empty top-level: returns 10
			query.Sum().ShouldBe(10m);

			// per plan: top-level Sum keeps current behavior; no COALESCE wrapping at SQL level
			db.LastQuery!.ToUpperInvariant().ShouldNotContain("COALESCE");
		}

		#endregion
	}
}
