using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

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
		[ActiveIssue(Configurations = [TestProvName.AllSQLiteClassic, TestProvName.AllOracle], Details = "The storage keeps the instant - the round-trip guard inside the test passes - but the difference is measured on the local reading: SQLite's julianday ignores the offset suffix, and Oracle 11 loses the zone in the CAST(x AS DATE) that the elapsed lowering uses. A wrong number rather than a refusal, so it is recorded rather than skipped.")]
		[Test]
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
		[ActiveIssue(Configurations = [TestProvName.AllSQLiteClassic, TestProvName.AllOracle], Details = "The storage keeps the instant - the round-trip guard inside the test passes - but the difference is measured on the local reading: SQLite's julianday ignores the offset suffix, and Oracle 11 loses the zone in the CAST(x AS DATE) that the elapsed lowering uses. A wrong number rather than a refusal, so it is recorded rather than skipped.")]
		[Test]
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
		/// PostgreSQL is the one that refuses rather than answers: it extracts only <c>day</c>, <c>hour</c>,
		/// <c>minute</c> and <c>second</c> from a native interval, so anything below a second has no expression.
		/// </para>
		/// </remarks>
		[ActiveIssue(Configurations = [TestProvName.AllOracle], Details = "Oracle 11 measures the difference on the local reading although the storage kept the instant - the zone is lost in the CAST(x AS DATE) that the elapsed lowering uses.")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSQLite, ErrorMessage = ErrorHelper.Error_Interval_ComponentBelowResolution)]
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllPostgreSQL, ErrorMessage = ErrorHelper.Error_Interval_Member)]
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

		[Test]
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
	}
}
