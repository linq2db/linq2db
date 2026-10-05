using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class IntervalTranslationTests
	{
		/// <summary>
		/// The coercion is emitted only where the operand is not already a <c>DateTime64</c>.
		/// </summary>
		/// <remarks>
		/// Both halves are needed and neither implies the other. Dropping the coercion where it is not needed is
		/// what the operand types are read for, and it is invisible in a result assertion - every test in
		/// <see cref="CoarseEventRow"/>'s set passes with the conversion applied to everything. Keeping it where the
		/// server needs it is what #5955 was; a type that stopped describing the SQL would silently take this branch too.
		/// </remarks>
		[Test]
		public void TheCoercionIsEmittedOnlyWhereTheOperandNeedsIt([IncludeDataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataConnection(context);
			using var t  = db.CreateLocalTable<EventRow>();

			// EventRow declares nothing, so ClickHouse stores DateTime64(7) - already what the epoch functions take.
			_ = t.Select(r => (r.FinishedOn - r.StartedOn).TotalHours).ToList();
			db.LastQuery!.ShouldNotContain("toDateTime64");

			// now() is a whole-second timestamp whatever the mapping schema makes of a CLR DateTime.
			_ = t.Select(r => (Sql.CurrentTimestamp - r.StartedOn).TotalHours).ToList();
			db.LastQuery!.ShouldContain("toDateTime64(now()");

			// makeDateTime answers a whole-second timestamp too.
			_ = t.Select(r => (r.FinishedOn - Sql.MakeDateTime(2026, 6, 1, 10, 0, 0)!.Value).TotalHours).ToList();
			db.LastQuery!.ShouldContain("toDateTime64(makeDateTime(");

			using var coarse = db.CreateLocalTable<CoarseEventRow>();

			// A declared DataType.DateTime column is a 32-bit whole-second timestamp on the server.
			_ = coarse.Select(r => (r.FinishedOn - r.StartedOn).TotalHours).ToList();
			db.LastQuery!.ShouldContain("toDateTime64(");
		}

		[Table]
		sealed class CoarseNullableEventRow
		{
			[PrimaryKey]                           public int       Id         { get; set; }
			[Column(DataType = DataType.DateTime)] public DateTime  StartedOn  { get; set; }
			[Column(DataType = DataType.DateTime)] public DateTime? FinishedOn { get; set; }
		}

		static TempTable<CoarseNullableEventRow> SeedCoarseNullable(IDataContext db)
		{
			var t = db.CreateLocalTable<CoarseNullableEventRow>();

			db.Insert(new CoarseNullableEventRow { Id = 1, StartedOn = CoarseStart,              FinishedOn = null                         });
			db.Insert(new CoarseNullableEventRow { Id = 2, StartedOn = CoarseStart.AddHours(1), FinishedOn = CoarseStart.AddHours(3) });

			return t;
		}

		[Test]
		public void CoalesceOverASecondPrecisionColumnIsCoerced([IncludeDataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = SeedCoarseNullable(db);

			t.OrderBy(r => r.Id).Select(r => Sql.AsSql((r.FinishedOn ?? r.StartedOn).TimeOfDay)).ToArray()
				.ShouldBe([CoarseStart.TimeOfDay, CoarseStart.AddHours(3).TimeOfDay]);

			t.OrderBy(r => r.Id).Select(r => Sql.AsSql(((r.FinishedOn ?? r.StartedOn) - r.StartedOn).TotalMilliseconds)).ToArray()
				.ShouldBe([0d, 2 * 3600_000d]);
		}

		[Test]
		public void ValueWindowFunctionOverASecondPrecisionColumnIsCoerced([IncludeDataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = SeedCoarseNullable(db);

			t.Select(r => new
				{
					r.Id,
					First = Sql.AsSql(Sql.Window.FirstValue(r.StartedOn, w => w.OrderBy(r.Id).RowsBetween.Unbounded.And.Unbounded).TimeOfDay),
					Last  = Sql.AsSql(Sql.Window.LastValue(r.StartedOn, w => w.OrderBy(r.Id).RowsBetween.Unbounded.And.Unbounded).TimeOfDay),
				})
				.OrderBy(r => r.Id)
				.Select(r => new { r.First, r.Last })
				.ToArray()
				.ShouldAllBe(r => r.First == CoarseStart.TimeOfDay && r.Last == CoarseStart.AddHours(1).TimeOfDay);

			var lags = t.Select(r => new
				{
					r.Id,
					Lag       = Sql.AsSql((r.StartedOn - Sql.Window.Lag(r.StartedOn, w => w.OrderBy(r.Id))).TotalMilliseconds),
					LagOffset = Sql.AsSql((r.StartedOn - Sql.Window.Lag(r.StartedOn, 1, r.StartedOn, w => w.OrderBy(r.Id))).TotalMilliseconds),
				})
				.ToArray()
				.OrderBy(r => r.Id)
				.ToArray();

			// The first row has no predecessor, and ClickHouse answers its type's default rather than NULL.
			lags[1].Lag.ShouldBe(3600_000d);
			lags.Select(r => r.LagOffset).ShouldBe([0d, 3600_000d]);
		}

		// Spelled out rather than derived from CoarseStart: static initializers in separate partial-class files run in
		// no defined order.
		static readonly DateTime CoarseSubSecond = new(2026, 6, 1, 10, 0, 0, 500);

		/// <summary>
		/// A coalesce or a value window function over a whole-second column is a computed value: the literal beside it
		/// keeps its sub-second part.
		/// </summary>
		[Test]
		public void ComputedSecondPrecisionValueComparedWithSubSecondLiteral([IncludeDataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = SeedCoarseNullable(db);

			t.Count(r => (r.FinishedOn ?? r.StartedOn) <  CoarseSubSecond).ShouldBe(1);
			t.Count(r => CoarseSubSecond > (r.FinishedOn ?? r.StartedOn)).ShouldBe(1);
			t.Count(r => (r.FinishedOn ?? r.StartedOn) == CoarseSubSecond).ShouldBe(0);

			var windowed = t
				.Select(r => new
				{
					First = Sql.Window.FirstValue(r.StartedOn, w => w.OrderBy(r.Id).RowsBetween.Unbounded.And.Unbounded),
					Lag   = Sql.Window.Lag(r.StartedOn, 1, r.StartedOn, w => w.OrderBy(r.Id)),
				})
				.AsSubQuery();

			var boundary = CoarseSubSecond;

			windowed.Count(r => r.First < CoarseSubSecond).ShouldBe(2);
			windowed.Count(r => r.Lag   < CoarseSubSecond).ShouldBe(2);
			windowed.Count(r => r.Lag   < boundary).ShouldBe(2);
		}
	}
}
