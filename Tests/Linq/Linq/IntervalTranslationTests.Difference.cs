using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class IntervalTranslationTests
	{
		/// <summary>
		/// A difference between two <see cref="DateTimeOffset"/> values measures the instants, not the local
		/// readings - as CLR subtraction does, which compares <c>UtcDateTime</c>.
		/// </summary>
		/// <remarks>
		/// Worth pinning separately from the <see cref="DateTime"/> case because the failure is silent: a provider
		/// that measures the stored local representation returns a plausible number for two marks that are actually
		/// the same instant, and every total or component taken from that difference inherits the error. The two
		/// rows are chosen so that reading the offset and ignoring it give different answers in both directions -
		/// one is zero only if offsets are honoured, the other is non-zero only if they are.
		/// </remarks>
		[ActiveIssue(Configurations = [TestProvName.AllSQLiteClassic, TestProvName.AllOracle], Details = "The storage keeps the instant - the round-trip guard inside the test passes - but the difference is measured on the local reading: SQLite's julianday ignores the offset suffix, and every Oracle version loses the zone in the CAST(x AS timestamp) that the elapsed lowering uses. A wrong number rather than a refusal, so it is recorded rather than skipped.")]
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		public void ZonedDifferenceMeasuresInstantsNotLocalTime(
			[SupportsDateTimeOffsetContext] string context)
		{
			// Same instant, written down in two zones: 12:00Z and 14:00+02:00.
			var sameInstantStart = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
			var sameInstantEnd   = new DateTimeOffset(2026, 1, 1, 14, 0, 0, TimeSpan.FromHours(2));

			// Same local reading in two zones: the +02:00 one is the earlier instant by two hours.
			var sameLocalStart = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));
			var sameLocalEnd   = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

			// The CLR contract this pins, asserted before the database is asked at all.
			(sameInstantEnd - sameInstantStart).ShouldBe(TimeSpan.Zero);
			(sameLocalEnd   - sameLocalStart).ShouldBe(TimeSpan.FromHours(2));

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<ZonedEventRow>();

			db.Insert(new ZonedEventRow { Id = 1, StartedOn = sameInstantStart, FinishedOn = sameInstantEnd });
			db.Insert(new ZonedEventRow { Id = 2, StartedOn = sameLocalStart,   FinishedOn = sameLocalEnd   });

			// Asked first, and deliberately: if the storage did not keep the instant, every difference below is
			// measuring something other than what was written, and blaming the interval translation for it would
			// be wrong. Compared with UtcDateTime because a provider may normalise the offset it hands back while
			// still denoting the same moment - that is faithful storage, a changed instant is not.
			var stored = t
				.OrderBy(r => r.Id)
				.Select(r => new { r.StartedOn, r.FinishedOn })
				.ToList();

			stored[0].StartedOn.UtcDateTime.ShouldBe(sameInstantStart.UtcDateTime);
			stored[0].FinishedOn.UtcDateTime.ShouldBe(sameInstantEnd.UtcDateTime);
			stored[1].StartedOn.UtcDateTime.ShouldBe(sameLocalStart.UtcDateTime);
			stored[1].FinishedOn.UtcDateTime.ShouldBe(sameLocalEnd.UtcDateTime);

			var rows = t
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql((r.FinishedOn - r.StartedOn).TotalHours))
				.ToList();

			rows[0].ShouldBe(0d);
			rows[1].ShouldBe(2d);
		}

		/// <summary>
		/// Every <see cref="TimeSpan"/> member of a difference between two <see cref="DateTimeOffset"/> values,
		/// against the CLR.
		/// </summary>
		/// <remarks>
		/// The two marks are the same instant written in different zones - <see cref="DateTimeOffset.ToOffset"/>
		/// changes the local reading and keeps the instant - so the expected difference is exactly the one built
		/// here. That makes one test carry both questions: a provider that measured the local readings would be
		/// two hours out on every member at once, and a member that lowers wrongly is off on its own.
		/// <para>
		/// The amount is exact at one microsecond, which both a SQL Server <c>datetimeoffset</c> and a PostgreSQL
		/// <c>timestamptz</c> can hold, so the expectation needs no per-provider tolerance.
		/// </para>
		/// </remarks>
		[ActiveIssue(Configurations = [TestProvName.AllSQLiteClassic, TestProvName.AllOracle], Details = "The storage keeps the instant - the round-trip guard inside the test passes - but the difference is measured on the local reading: SQLite's julianday ignores the offset suffix, and every Oracle version loses the zone in the CAST(x AS timestamp) that the elapsed lowering uses. A wrong number rather than a refusal, so it is recorded rather than skipped.")]
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		public void ZonedDifferenceMembersMatchClr(
			[SupportsDateTimeOffsetContext] string context)
		{
			// 2 days 3 hours 4 minutes 5 seconds 6 milliseconds, exact at one millisecond. Members finer than that
			// have their own test, which derives its expectation from the round trip because a storage that truncates
			// and a member that lies both show up as a zero.
			var expected = new TimeSpan(2, 3, 4, 5, 6);

			var start = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
			var end   = (start + expected).ToOffset(TimeSpan.FromHours(2));

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<ZonedEventRow>();

			db.Insert(new ZonedEventRow { Id = 1, StartedOn = start, FinishedOn = end });

			var row = t
				.Select(r => new
				{
					Days              = Sql.AsSql((r.FinishedOn - r.StartedOn).Days),
					Hours             = Sql.AsSql((r.FinishedOn - r.StartedOn).Hours),
					Minutes           = Sql.AsSql((r.FinishedOn - r.StartedOn).Minutes),
					Seconds           = Sql.AsSql((r.FinishedOn - r.StartedOn).Seconds),

					Ticks             = Sql.AsSql((r.FinishedOn - r.StartedOn).Ticks),
					TotalDays         = Sql.AsSql((r.FinishedOn - r.StartedOn).TotalDays),
					TotalHours        = Sql.AsSql((r.FinishedOn - r.StartedOn).TotalHours),
					TotalMinutes      = Sql.AsSql((r.FinishedOn - r.StartedOn).TotalMinutes),
					TotalSeconds      = Sql.AsSql((r.FinishedOn - r.StartedOn).TotalSeconds),
					TotalMilliseconds = Sql.AsSql((r.FinishedOn - r.StartedOn).TotalMilliseconds),
				})
				.Single();

			row.Days.ShouldBe(expected.Days);
			row.Hours.ShouldBe(expected.Hours);
			row.Minutes.ShouldBe(expected.Minutes);
			row.Seconds.ShouldBe(expected.Seconds);

			// Ticks is integral and must match exactly. Every Total* is a double the server divides in its own
			// order of operations, so the last bit need not agree with the CLR's - PostgreSQL returns
			// 51.06805734888889 where .NET gives 51.068057348888885. The tolerance is relative because the
			// members span eight orders of magnitude, from days to milliseconds.
			row.Ticks.ShouldBe(expected.Ticks);
			row.TotalDays.ShouldBe(expected.TotalDays, Tolerance(expected.TotalDays));
			row.TotalHours.ShouldBe(expected.TotalHours, Tolerance(expected.TotalHours));
			row.TotalMinutes.ShouldBe(expected.TotalMinutes, Tolerance(expected.TotalMinutes));
			row.TotalSeconds.ShouldBe(expected.TotalSeconds, Tolerance(expected.TotalSeconds));
			row.TotalMilliseconds.ShouldBe(expected.TotalMilliseconds, Tolerance(expected.TotalMilliseconds));
		}

#if NET8_0_OR_GREATER
		/// <summary>
		/// The sub-second components of a zoned difference, where a provider can express them at all.
		/// </summary>
		/// <remarks>
		/// The expectation is taken from what the storage actually kept, not from what was written. That is the
		/// difference between a test and a green light: a provider whose timestamp stops at the second truly has
		/// no microseconds to report, and answering zero there is correct - while a provider that <em>does</em>
		/// hold them and still answers zero is lying, and only an expectation derived from the stored value can
		/// tell those two apart. Excluding the coarse storages instead would have hidden the second case.
		/// <para>
		/// PostgreSQL answers these away from the <c>EXTRACT</c> shortcut: that names no field below the second, so
		/// the member falls to the shared decomposition, which is exact here because this provider sums the
		/// interval's own fields into a tick count rather than dividing an epoch.
		/// </para>
		/// </remarks>
		[ActiveIssue(Configurations = [TestProvName.AllOracle], Details = "Every Oracle version measures the difference on the local reading although the storage kept the instant - the zone is lost in the CAST(x AS timestamp) that the elapsed lowering uses.")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSQLite, ErrorMessage = ErrorHelper.Error_Interval_ComponentBelowResolution)]
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		public void ZonedDifferenceSubMillisecondMembersMatchClr(
			[SupportsDateTimeOffsetContext] string context)
		{
			var written = TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond * 6 + 4560);

			var start = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
			var end   = (start + written).ToOffset(TimeSpan.FromHours(2));

			// 456 microseconds within the millisecond, and nothing finer - no storage in the matrix goes below the
			// microsecond, so a value that did could never round-trip anywhere.
			written.Microseconds.ShouldBe(456);
			written.Nanoseconds.ShouldBe(0);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<ZonedEventRow>();

			db.Insert(new ZonedEventRow { Id = 1, StartedOn = start, FinishedOn = end });

			var stored = t
				.Select(r => new { r.StartedOn, r.FinishedOn })
				.Single();

			// What the database really holds. Where the storage truncated, this is coarser than `written`, and the
			// members below are expected to agree with the truncation rather than with the original.
			var expected = stored.FinishedOn - stored.StartedOn;

			var row = t
				.Select(r => new
				{
					Microseconds      = Sql.AsSql((r.FinishedOn - r.StartedOn).Microseconds),
					Nanoseconds       = Sql.AsSql((r.FinishedOn - r.StartedOn).Nanoseconds),
					TotalMicroseconds = Sql.AsSql((r.FinishedOn - r.StartedOn).TotalMicroseconds),
					TotalNanoseconds  = Sql.AsSql((r.FinishedOn - r.StartedOn).TotalNanoseconds),
				})
				.Single();

			row.Microseconds.ShouldBe(expected.Microseconds);
			row.Nanoseconds.ShouldBe(expected.Nanoseconds);
			row.TotalMicroseconds.ShouldBe(expected.TotalMicroseconds, Tolerance(expected.TotalMicroseconds));
			row.TotalNanoseconds.ShouldBe(expected.TotalNanoseconds, Tolerance(expected.TotalNanoseconds));
		}
#endif

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		public void DateDifferenceComponentsMatchClr(
			[DataSources] string context,
			[Values(1, -1)] int direction)
		{
			// 2 days 3 hours 30 minutes, taken in both directions. The negative case is where a native interval
			// type is most likely to disagree with the CLR - PostgreSQL reports it as "-2 days -03:30:00", so the
			// components come back negative as .NET gives them, but that has to be verified, not assumed.
			var earlier = new DateTime(2026, 1, 1, 10,  0, 0);
			var later   = new DateTime(2026, 1, 3, 13, 30, 0);

			var start = direction > 0 ? earlier : later;
			var end   = direction > 0 ? later   : earlier;

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = start, FinishedOn = end });

			var elapsed = end - start;

			elapsed.Days.ShouldBe(2 * direction);
			elapsed.Hours.ShouldBe(3 * direction);

			var row = t.Select(r => new
			{
				Days       = Sql.AsSql((r.FinishedOn - r.StartedOn).Days),
				Hours      = Sql.AsSql((r.FinishedOn - r.StartedOn).Hours),
				Minutes    = Sql.AsSql((r.FinishedOn - r.StartedOn).Minutes),
				TotalHours = Sql.AsSql((r.FinishedOn - r.StartedOn).TotalHours),
			}).Single();

			row.Days.ShouldBe(elapsed.Days);
			row.Hours.ShouldBe(elapsed.Hours);
			row.Minutes.ShouldBe(elapsed.Minutes);
			row.TotalHours.ShouldBe(elapsed.TotalHours, 1e-9);
		}

		/// <summary>
		/// A span longer than sixty-eight years measures what it measures in the CLR.
		/// </summary>
		/// <remarks>
		/// Sixty-eight years is not an arbitrary length to pick: it is two to the thirty-first seconds, and it was
		/// the ceiling. The decomposition anchors the start by shifting it forward by the whole units it counted,
		/// and a shift takes a 32-bit amount on the providers that lower a difference this way - so counting those
		/// whole units in seconds capped the whole thing at that many. Past it SQL Server answered <em>arithmetic
		/// overflow error converting expression to data type int</em>, and the age of anyone born before about
		/// 1958 is already past it. Counting the whole part in days instead puts the ceiling beyond what a date
		/// can hold at all, which is what makes the range match the CLR's rather than merely being large.
		/// <para>
		/// The span deliberately ends part-way through a day. A whole number of days would answer correctly even
		/// if the remainder were dropped entirely, and the remainder is the half of the decomposition the anchor
		/// exists for. It is a whole number of seconds, so that a provider keeping less than tick resolution still
		/// answers exactly rather than approximately.
		/// </para>
		/// <para>
		/// Taken in both directions, and the reverse is not symmetry for its own sake. A tick count reached by
		/// dividing rather than by decomposing carries a relative error, and where the result is then floored the
		/// error stops cancelling and starts costing a whole tick - on PostgreSQL that made every negative span
		/// wrong, down to one of a single second, while the positive half only went wrong past sixty-three years.
		/// A test that asked one direction would have found half of that.
		/// </para>
		/// <para>
		/// The dates sit inside the narrowest window any tested provider offers: YDB stores a timestamp as
		/// microseconds after the Unix epoch, so nothing earlier than 1970 can be written there at all, which is
		/// a limit of the storage rather than of the arithmetic - see the companion test on a wider column.
		/// </para>
		/// </remarks>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), NoTickTotalProviders, ErrorMessage = ErrorHelper.Error_Interval_Member)]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		public void LongSpanMatchesClr([DataSources] string context, [Values(1, -1)] int direction)
		{
			var earlier = new DateTime(1970, 1, 2, 0, 0, 0);
			var later   = new DateTime(2045, 6, 5, 4, 3, 2);

			var start = direction > 0 ? earlier : later;
			var end   = direction > 0 ? later   : earlier;

			var expected = end - start;

			// The guard against the test quietly ceasing to test anything: if these dates are ever brought closer
			// together, the shape stops crossing the boundary it exists for.
			Math.Abs(expected.TotalSeconds).ShouldBeGreaterThan((double)int.MaxValue);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = start, FinishedOn = end });

			var row = t.Select(r => new
			{
				Ticks     = Sql.AsSql((r.FinishedOn - r.StartedOn).Ticks),
				TotalDays = Sql.AsSql((r.FinishedOn - r.StartedOn).TotalDays),
			}).Single();

			row.Ticks.ShouldBe(expected.Ticks);
			row.TotalDays.ShouldBe(expected.TotalDays, Tolerance(expected.TotalDays));
		}

		/// <summary>
		/// A span past the range of a second counter, asked only for members that counting can answer.
		/// </summary>
		/// <remarks>
		/// Holds the totals and components of a seventy-five year span against the CLR. Access is what the unit list
		/// is chosen for: it is the one provider that answers members by counting units rather than by dividing a tick
		/// count, so its totals are bounded by the width of the counter rather than by the width of a tick. A count of
		/// seconds is 32-bit and runs out after about sixty-eight years, which is inside a human lifetime, and a
		/// difference between two columns has no .NET fallback to reach for.
		/// <para>
		/// <c>Ticks</c> is left out, which is what separates this from <see cref="LongSpanMatchesClr"/>: Access has no
		/// tick count to give and declares that refusal by name, so a case opening on <c>Ticks</c> stops there and
		/// says nothing about the totals behind it.
		/// </para>
		/// <para>
		/// Every provider runs it, not Access alone: the members are ordinary ones, and a total that agrees with the
		/// CLR over a span this long is worth holding everywhere.
		/// </para>
		/// </remarks>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		public void ALongSpanIsMeasuredWhereCountingAnswersIt([DataSources] string context, [Values(1, -1)] int direction)
		{
			var earlier = new DateTime(1970, 1, 2, 0, 0, 0);
			var later   = new DateTime(2045, 6, 5, 4, 3, 2);

			var start = direction > 0 ? earlier : later;
			var end   = direction > 0 ? later   : earlier;

			var expected = end - start;

			// The same guard the test above carries: brought closer together, these dates stop crossing the boundary
			// the case exists for.
			Math.Abs(expected.TotalSeconds).ShouldBeGreaterThan((double)int.MaxValue);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = start, FinishedOn = end });

			var row = t.Select(r => new
			{
				TotalDays    = Sql.AsSql((r.FinishedOn - r.StartedOn).TotalDays),
				TotalHours   = Sql.AsSql((r.FinishedOn - r.StartedOn).TotalHours),
				TotalMinutes = Sql.AsSql((r.FinishedOn - r.StartedOn).TotalMinutes),
				TotalSeconds = Sql.AsSql((r.FinishedOn - r.StartedOn).TotalSeconds),
				Days         = Sql.AsSql((r.FinishedOn - r.StartedOn).Days),
				Hours        = Sql.AsSql((r.FinishedOn - r.StartedOn).Hours),
				Minutes      = Sql.AsSql((r.FinishedOn - r.StartedOn).Minutes),
			}).Single();

			row.TotalDays.ShouldBe(expected.TotalDays, Tolerance(expected.TotalDays));
			row.TotalHours.ShouldBe(expected.TotalHours, Tolerance(expected.TotalHours));
			row.TotalMinutes.ShouldBe(expected.TotalMinutes, Tolerance(expected.TotalMinutes));
			row.TotalSeconds.ShouldBe(expected.TotalSeconds, Tolerance(expected.TotalSeconds));

			row.Days.ShouldBe(expected.Days);
			row.Hours.ShouldBe(expected.Hours);
			row.Minutes.ShouldBe(expected.Minutes);
		}

		/// <summary>
		/// Where a second component stops answering on Access, and that a shorter span still does.
		/// </summary>
		/// <remarks>
		/// Access forms the component as an elapsed count of seconds reduced by <c>MOD</c>, and every step of that is
		/// 32-bit: <c>DateDiff</c> produces the count, <c>DateAdd</c> carries it as the anchor of the correction, and
		/// <c>MOD</c> coerces both operands before dividing. The first two act on a value that can be split into whole
		/// days and a sub-day rest, but <c>MOD</c> takes the elapsed count whole - so the reach of the component is the
		/// reach of that count, about sixty-eight years.
		/// <para>
		/// Reaching further means forming the component over a day-anchored window instead, where a day is a whole
		/// number of the unit and the modulo cannot see it. That needs the corrected elapsed day count rather than the
		/// boundary one: an anchor that overshoots makes the remainder negative, and the sign survives the modulo.
		/// </para>
		/// <para>
		/// Totals are not bounded this way, and the companion test above holds them over the same span. Pinned rather
		/// than left undiscovered, and it goes red the day the component reaches further - which is when this is worth
		/// revisiting.
		/// </para>
		/// <para>
		/// The short-span row is asked first and through the same expression, so this cannot pass by the component
		/// being broken outright rather than only beyond its range.
		/// </para>
		/// </remarks>
		[Test]
		public void ASecondComponentPastItsCountersRangeIsRefused([IncludeDataSources(false, TestProvName.AllAccess)] string context)
		{
			var start = new DateTime(1970, 1, 2, 0, 0, 0);

			var shortSpanEnd = start.AddSeconds(125);
			var longSpanEnd  = new DateTime(2045, 6, 5, 4, 3, 2);

			Math.Abs((longSpanEnd - start).TotalSeconds).ShouldBeGreaterThan((double)int.MaxValue);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = start, FinishedOn = shortSpanEnd });
			db.Insert(new EventRow { Id = 2, StartedOn = start, FinishedOn = longSpanEnd  });

			int SecondsOf(int id) => t
				.Where (r => r.Id == id)
				.Select(r => Sql.AsSql((r.FinishedOn - r.StartedOn).Seconds))
				.Single();

			SecondsOf(1).ShouldBe((shortSpanEnd - start).Seconds);

			Shouldly.Should.Throw<Exception>(() => SecondsOf(2));
		}

		/// <summary>
		/// Carries dates on a column wide enough for the whole CLR range, which on YDB is not the default.
		/// </summary>
		[Table]
		sealed class WideEventRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.Timestamp64)] public DateTime StartedOn  { get; set; }
			[Column(DataType = DataType.Timestamp64)] public DateTime FinishedOn { get; set; }
		}

		/// <summary>
		/// Given a column that can hold them, the whole CLR range measures what it measures in the CLR.
		/// </summary>
		/// <remarks>
		/// The companion to the span test above, and the point is to separate two limits that look like one. YDB
		/// maps a <see cref="DateTime"/> to <c>Timestamp</c>, which counts microseconds after the Unix epoch and
		/// is unsigned, so a date before 1970 cannot be written and one after 2105 is refused - and the failure
		/// arrives from the driver while binding the insert, before any query is built. That is easy to mistake
		/// for the difference arithmetic being unable to reach far, which it is not.
		/// <para>
		/// Declaring the columns <c>Timestamp64</c> - signed, and spanning years 1 through 9999 - the same
		/// untouched lowering answers exactly across the entire CLR range, in both directions. So the boundary
		/// belongs to the storage type alone, and this pins that: should YDB's default ever widen, the arithmetic
		/// is already known to be ready for it.
		/// </para>
		/// <para>
		/// Whole seconds, because <c>Timestamp64</c> keeps microseconds rather than ticks.
		/// </para>
		/// </remarks>
		[Test]
		public void FullClrRangeMatchesClrOnAWideColumn(
			[IncludeDataSources(TestProvName.AllYdb)] string context,
			[Values(1, -1)] int direction)
		{
			var earliest = DateTime.MinValue;
			var latest   = new DateTime(9999, 12, 31, 23, 59, 59);

			var start = direction > 0 ? earliest : latest;
			var end   = direction > 0 ? latest   : earliest;

			var expected = end - start;

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<WideEventRow>();

			db.Insert(new WideEventRow { Id = 1, StartedOn = start, FinishedOn = end });

			var ticks = t.Select(r => Sql.AsSql((r.FinishedOn - r.StartedOn).Ticks)).Single();

			ticks.ShouldBe(expected.Ticks);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		public void DifferenceSurvivesAsASubqueryColumn([DataSources] string context)
		{
			// AsSubQuery keeps the nesting, so the difference really becomes a column of an inner SELECT and the
			// outer query meets a column reference rather than the difference node itself. Without it the
			// optimizer folds the projection away and the lowering never sees that shape at all.
			var started = new DateTime(2026, 1, 1, 10, 0, 0);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = started, FinishedOn = started.AddHours(5) });
			db.Insert(new EventRow { Id = 2, StartedOn = started, FinishedOn = started.AddHours(1) });

			var inner =
				(from r in t
				 select new { r.Id, Taken = r.FinishedOn - r.StartedOn })
				.AsSubQuery();

			var rows = inner
				.Where(x => x.Taken.TotalHours > 3)
				.OrderBy(x => x.Id)
				.ToArray();

			rows.Length.ShouldBe(1);
			rows[0].Id.ShouldBe(1);
			rows[0].Taken.ShouldBe(TimeSpan.FromHours(5));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		public void DifferenceFromASubqueryFiltersOnItsParts([DataSources] string context)
		{
			// The difference is computed in one query and a part of it is taken in the enclosing one, so the
			// lowering meets a column reference where it usually meets the difference node itself.
			var started  = new DateTime(2026, 1, 1, 10, 0, 0);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = started, FinishedOn = started.AddHours(5) });
			db.Insert(new EventRow { Id = 2, StartedOn = started, FinishedOn = started.AddHours(1) });

			var elapsed =
				from r in t
				select new { r.Id, Taken = r.FinishedOn - r.StartedOn };

			elapsed
				.Where(x => x.Taken.TotalHours > 3)
				.Select(x => x.Id)
				.ToArray()
				.ShouldBe([1]);
			elapsed
				.Where(x => x.Taken.Hours == 1)
				.Select(x => x.Id)
				.ToArray()
				.ShouldBe([2]);
			elapsed
				.OrderByDescending(x => x.Taken)
				.Select(x => x.Id)
				.ToArray()
				.ShouldBe([1, 2]);

			// And the interval itself comes back, not only the rows it selected: filtering on a part says nothing
			// about whether the value survives the trip out of the subquery.
			elapsed
				.OrderBy(x => x.Id)
				.Select(x => x.Taken)
				.ToArray()
				.ShouldBe([TimeSpan.FromHours(5), TimeSpan.FromHours(1)]);

			var whole = elapsed
				.OrderBy(x => x.Id)
				.ToArray();

			whole[0].Taken.ShouldBe(TimeSpan.FromHours(5));
			whole[1].Taken.ShouldBe(TimeSpan.FromHours(1));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		public void DateDifferenceIsElapsedTime([DataSources] string context)
		{
			// Elapsed, not a boundary count. 10:59 -> 11:01 is two minutes; Sql.DateDiff(hour, ...) would say one,
			// and that difference is the whole reason this does not reuse the DateDiff builders.
			var started  = new DateTime(2026, 1, 1, 10, 59, 0);
			var finished = new DateTime(2026, 1, 1, 11,  1, 0);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = started, FinishedOn = finished });

			var elapsed = finished - started;

			var row = t
				.Select(r => new
				{
					TotalMinutes = Sql.AsSql((r.FinishedOn - r.StartedOn).TotalMinutes),
					Minutes      = Sql.AsSql((r.FinishedOn - r.StartedOn).Minutes),
				})
				.Single();

			row.TotalMinutes.ShouldBe(elapsed.TotalMinutes);
			row.Minutes.ShouldBe(elapsed.Minutes);
		}

		/// <summary>
		/// A difference finer than a millisecond survives the round trip and the lowering.
		/// </summary>
		/// <remarks>
		/// Scoped by what the storage can hold, which is the whole point of the test - a provider whose timestamp
		/// stops at the second or the millisecond has nothing sub-millisecond to measure, and asserting against it
		/// would be testing the column type. The excluded set was read off a run rather than assumed: SQLite and
		/// MySQL return whole seconds here, DuckDB and DB2 stop at ten microseconds, ClickHouse and Oracle round,
		/// and Access refuses the member outright.
		/// </remarks>
		[Test]
		public void DateDifferenceKeepsSubSecondPrecision(
			[IncludeDataSources(true, TestProvName.AllSqlServer2016Plus, TestProvName.AllPostgreSQL)] string context)
		{
			// A millisecond-resolution DATEDIFF would report zero here. This is the case the review of #5739 called
			// out as translator-induced precision loss.
			var started = new DateTime(2026, 1, 1);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = started, FinishedOn = started.AddTicks(9999) });

			// Read against the stored value, not against 9999: the difference cannot be finer than what the column
			// holds, and the storage quantum differs - datetime2 keeps 100ns where a PostgreSQL timestamp keeps a
			// microsecond. What is being pinned is that the difference loses nothing beyond that.
			var row = t
				.Select(r => new
				{
					Ticks  = Sql.AsSql((r.FinishedOn - r.StartedOn).Ticks),
					Stored = r.FinishedOn,
				})
				.Single();

			var ticks = row.Ticks;

			ticks.ShouldBe((row.Stored - started).Ticks);

			// And that what remains is genuinely sub-millisecond, so the assertion above cannot be satisfied by a
			// provider that rounded the stored value to a whole millisecond in the first place.
			(ticks % TimeSpan.TicksPerMillisecond).ShouldNotBe(0L);
		}

		/// <summary>
		/// Sub-second components of a plain <see cref="DateTime"/> difference agree with what the storage kept.
		/// </summary>
		/// <remarks>
		/// The detector for a component that reports zero while the column holds a value. Runs everywhere and needs
		/// no per-provider expectation, because the expectation is read back rather than assumed: a storage that
		/// truncates to the second genuinely has no milliseconds and answering zero is right, while a storage that
		/// kept them and still answers zero is wrong. Excluding the coarse providers - the obvious way to make this
		/// green - would remove exactly the second case from view.
		/// <para>
		/// The <see cref="DateTimeOffset"/> twin cannot cover these providers: several have no offset-carrying
		/// column type at all, so they are out of its scope entirely.
		/// </para>
		/// <para>
		/// Two providers refuse rather than answer, and the difference between them is worth keeping in mind.
		/// Access has no sub-second date part to extract at all. PostgreSQL does - <c>EXTRACT(MILLISECONDS ...)</c>
		/// exists - but it returns the whole seconds field scaled, not the part within the second, so mapping it
		/// would need a truncation and a modulus rather than a name. That is unimplemented, not impossible.
		/// </para>
		/// </remarks>
		[Test]
		// PostgreSQL is absent here although EXTRACT names no field below the second: the components are reached
		// from its own exact tick count instead, so it answers them like everyone else.
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ErrorMessage = ErrorHelper.Error_Interval_ComponentBelowResolution)]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		public void DateDifferenceSubSecondComponentsAgreeWithStorage([DataSources] string context)
		{
			var started = new DateTime(2026, 1, 1, 10, 20, 30);

			// Milliseconds, microseconds and sub-microsecond ticks all non-zero, so a component that survives the
			// storage cannot be confused with one that is legitimately zero.
			var written = started.AddTicks(TimeSpan.TicksPerMillisecond * 123 + 4567);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = started, FinishedOn = written });

			var stored   = t.Select(r => r.FinishedOn).Single();
			var expected = stored - started;

			var row = t
				.Select(r => new
				{
					Milliseconds = Sql.AsSql((r.FinishedOn - r.StartedOn).Milliseconds),
					Seconds      = Sql.AsSql((r.FinishedOn - r.StartedOn).Seconds),
				})
				.Single();

			row.Seconds.ShouldBe(expected.Seconds);
			row.Milliseconds.ShouldBe(expected.Milliseconds);
		}
	}
}
