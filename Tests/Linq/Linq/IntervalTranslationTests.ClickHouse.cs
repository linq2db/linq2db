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
		/// Date/time columns whose ClickHouse type is not <c>DateTime64</c>.
		/// </summary>
		/// <remarks>
		/// <see cref="EventRow"/> lets ClickHouse fall back to whatever it maps a <see cref="DateTime"/> to, which
		/// is <c>DateTime64(7)</c> - so nothing in this fixture ever measured a difference over a column stored any
		/// other way. <c>toUnixTimestamp64Nano</c>, which the elapsed lowering is built on, accepts a
		/// <c>DateTime64</c> and refuses every other date type by name.
		/// </remarks>
		[Table]
		sealed class CoarseEventRow
		{
			[PrimaryKey] public int Id { get; set; }

			// Whole seconds, 32-bit, 1970-2106.
			[Column(DataType = DataType.DateTime)] public DateTime StartedOn  { get; set; }
			[Column(DataType = DataType.DateTime)] public DateTime FinishedOn { get; set; }

			// A date with no time at all, which the lowering meets both as an operand and as the thing being shifted.
			[Column(DataType = DataType.Date32)] public DateTime OpenedOn { get; set; }
			[Column(DataType = DataType.Date32)] public DateTime ClosedOn { get; set; }
		}

		/// <summary>
		/// Midsummer, because these columns carry a wall-clock reading that the server resolves in its own zone.
		/// </summary>
		/// <remarks>
		/// A span that crosses a daylight-saving transition is an hour shorter or longer in absolute time than the
		/// two readings suggest, so an exact expectation computed in the CLR would disagree with the server on any
		/// configuration that observes one - and agree on this workstation, which does not. June has no transition
		/// in either hemisphere.
		/// </remarks>
		static readonly DateTime CoarseStart = new(2026, 6, 1, 10, 0, 0);

		/// <summary>
		/// A base for the shift that is not the difference's own start - that form cancels in the optimizer and no
		/// shift survives to test - and that stays inside the same transition-free window.
		/// </summary>
		static readonly DateTime CoarseShiftOrigin = new(2026, 6, 20, 0, 0, 0);

		static TempTable<CoarseEventRow> SeedCoarse(IDataContext db, DateTime started, DateTime finished)
		{
			var t = db.CreateLocalTable<CoarseEventRow>();

			try
			{
				db.Insert(new CoarseEventRow
				{
					Id         = 1,
					StartedOn  = started,
					FinishedOn = finished,
					OpenedOn   = started.Date,
					ClosedOn   = finished.Date,
				});
			}
			catch
			{
				// The table is not the caller's to dispose until it is returned, so a failed insert would otherwise
				// leave it behind for the next test in the same database to collide with.
				t.Dispose();
				throw;
			}

			return t;
		}

		/// <summary>
		/// The shape from the issue: whole days spanned by a grouped minimum and maximum.
		/// </summary>
		/// <remarks>
		/// Kept close to what was reported rather than reduced to the smallest failing query, because the aggregate
		/// is load-bearing. An aggregate carries the mapping schema's default type rather than the column's, so a
		/// fix that asked the expression what it was would be told <c>DateTime64</c> here and would leave this
		/// exact query broken.
		/// </remarks>
		[Test]
		public void ReportedDayCountOverASecondPrecisionColumn([IncludeDataSources(TestProvName.AllClickHouse)] string context)
		{
			var earlier = CoarseStart.AddDays(-7);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<CoarseEventRow>();

			db.Insert(new CoarseEventRow { Id = 1, StartedOn = CoarseStart, FinishedOn = CoarseStart, OpenedOn = CoarseStart.Date, ClosedOn = CoarseStart.Date });
			db.Insert(new CoarseEventRow { Id = 2, StartedOn = earlier,     FinishedOn = earlier,     OpenedOn = earlier.Date,     ClosedOn = earlier.Date     });

			var query =
				from grp in t.GroupBy(_ => 1)
				let minStarted = grp.Min(x => x.StartedOn)
				let maxStarted = grp.Max(x => x.StartedOn)
				let dayCount   = (int)(maxStarted - minStarted).TotalDays + 1
				select new { dayCount };

			query.Single().dayCount.ShouldBe(8);
		}

		[Test]
		public void TickCountOverASecondPrecisionColumnMatchesClr([IncludeDataSources(TestProvName.AllClickHouse)] string context)
		{
			// Whole seconds, which is all the column can hold - a finer expectation would be testing the column type.
			var finished = CoarseStart.AddHours(5).AddMinutes(4).AddSeconds(3);

			using var db = GetDataContext(context);
			using var t  = SeedCoarse(db, CoarseStart, finished);

			t.Select(r => Sql.AsSql((r.FinishedOn - r.StartedOn).Ticks)).Single().ShouldBe((finished - CoarseStart).Ticks);
		}

		/// <summary>
		/// The same difference over a column stored as a date, which is a different refusal from the server and so
		/// a separate question from the second-precision one above.
		/// </summary>
		/// <remarks>
		/// The stored values are read back first. A date column that did not round-trip would fail the difference
		/// too, and blaming the lowering for that would be wrong.
		/// </remarks>
		[Test]
		public void DifferenceOverADate32ColumnMatchesClr([IncludeDataSources(TestProvName.AllClickHouse)] string context)
		{
			var finished = CoarseStart.AddDays(11);

			using var db = GetDataContext(context);
			using var t  = SeedCoarse(db, CoarseStart, finished);

			var stored = t.Select(r => new { r.OpenedOn, r.ClosedOn }).Single();

			stored.OpenedOn.ShouldBe(CoarseStart.Date);
			stored.ClosedOn.ShouldBe(finished.Date);

			t.Select(r => Sql.AsSql((r.ClosedOn - r.OpenedOn).Ticks)).Single().ShouldBe((finished.Date - CoarseStart.Date).Ticks);
		}

		[Test]
		public void ShiftOverASecondPrecisionColumnMatchesClr([IncludeDataSources(TestProvName.AllClickHouse)] string context)
		{
			var finished = CoarseStart.AddHours(5).AddMinutes(4).AddSeconds(3);

			using var db = GetDataContext(context);
			using var t  = SeedCoarse(db, CoarseStart, finished);

			t.Select(r => Sql.AsSql(CoarseShiftOrigin + (r.FinishedOn - r.StartedOn))).Single()
				.ShouldBe(CoarseShiftOrigin + (finished - CoarseStart));
		}

		/// <summary>
		/// The shift with a date-stored operand, which the interval addition refuses in its own right.
		/// </summary>
		/// <remarks>
		/// Separate from the difference test above because the failure is a different function's: the difference is
		/// refused while measuring, this one while adding the measured amount back to a date - ClickHouse answers
		/// <c>addNanoseconds cannot be used with Date32</c> even once the measurement succeeds.
		/// </remarks>
		[Test]
		public void ShiftOverADate32ColumnMatchesClr([IncludeDataSources(TestProvName.AllClickHouse)] string context)
		{
			var finished = CoarseStart.AddDays(11);

			using var db = GetDataContext(context);
			using var t  = SeedCoarse(db, CoarseStart, finished);

			t.Select(r => Sql.AsSql(CoarseShiftOrigin + (r.ClosedOn - r.OpenedOn))).Single()
				.ShouldBe(CoarseShiftOrigin + (finished.Date - CoarseStart.Date));
		}

		/// <summary>
		/// A difference taken against the date part of a column that declares nothing at all.
		/// </summary>
		/// <remarks>
		/// The one that says this is not a mapping question. <see cref="EventRow"/> takes ClickHouse's default
		/// mapping, so both sides look like <c>DateTime64</c> from inside the query - but the date part is
		/// translated to <c>toDate32</c>, and the server types that as a date. Nothing the user wrote is unusual.
		/// </remarks>
		[Test]
		public void DifferenceAgainstTheDatePartMatchesClr([IncludeDataSources(TestProvName.AllClickHouse)] string context)
		{
			var finished = CoarseStart.AddHours(5);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = CoarseStart, FinishedOn = finished });

			t.Select(r => Sql.AsSql((r.FinishedOn - r.FinishedOn.Date).TotalHours)).Single()
				.ShouldBe((finished - finished.Date).TotalHours);
		}

		/// <summary>
		/// A difference against the server clock, again over a column that declares nothing.
		/// </summary>
		/// <remarks>
		/// The second operand kind the server types more narrowly than linq2db does: <c>now()</c> is a whole-second
		/// timestamp. Asserted as a count rather than a value because the clock is not deterministic - what this
		/// pins is that the query runs at all, with two days of slack so no time-zone difference between this
		/// process and the server can move the answer.
		/// </remarks>
		[Test]
		public void DifferenceAgainstServerNowAnswers([IncludeDataSources(TestProvName.AllClickHouse)] string context)
		{
			var started = DateTime.UtcNow.AddDays(-2);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = started, FinishedOn = started });

			t.Count(r => (Sql.CurrentTimestamp - r.StartedOn).TotalDays > 1).ShouldBe(1);
		}
	}
}
