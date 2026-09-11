using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class TimeZoneFunctionsTests : TestBase
	{
		#region Fixture

		[Table]
		sealed class ZonedRow
		{
			[Column] public int            Id  { get; set; }
			[Column] public DateTimeOffset Dto { get; set; }
		}

		// 12:00 +00:40 is 11:20 UTC. The forty-minute offset is deliberate and is the whole point of the fixture:
		// no real zone uses it, so an answer that quietly reads in the server's zone cannot coincide with the right
		// one. A row whose offset matches the container's would let every one of these tests pass while broken.
		static readonly DateTimeOffset Value = new (2020, 6, 15, 12, 00, 00, TimeSpan.FromMinutes(40));

		static ZonedRow[] Rows(params DateTimeOffset[] values)
			=> values.Select((v, i) => new ZonedRow { Id = i + 1, Dto = v }).ToArray();

		// SQL Server resolves zone names against its own registry-backed list; every other provider takes IANA.
		// There is no spelling that works everywhere - SQL Server rejects a bare UTC offset - so the identifier is
		// picked per provider, the way GetNepalTzId already does in DateTimeOffsetTests.
		static string PragueZone(string context)
			=> context.IsAnyOf(TestProvName.AllSqlServer) ? "Central European Standard Time" : "Europe/Prague";

		// Providers that can carry an offset in a column AND express a conversion to a named zone.
		const string ZonedProviders = TestProvName.AllSqlServer2016Plus + "," + TestProvName.AllOracleManaged;

		// Adds those that cannot carry an offset but can still answer a reading in a named zone.
		const string ZoneReadingProviders = ZonedProviders + "," + TestProvName.AllPostgreSQL + "," + TestProvName.AllDuckDB;

		// The complement of ZonedProviders within ZoneReadingProviders: they read in a named zone but have no type
		// that can hand the converted value back, so anything materialising one is refused.
		const string OffsetlessProviders = TestProvName.AllPostgreSQL + "," + TestProvName.AllDuckDB;

		#endregion

		#region Sql.AtTimeZone

		/// <summary>
		/// The conversion as a value in its own right: same instant, the target zone's offset. It needs a result type
		/// able to carry an offset, which is why it is scoped to the providers that have one.
		/// </summary>
		[Test]
		public void AtTimeZoneKeepsInstantAndTakesTargetOffset([IncludeDataSources(false, ZonedProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var result = table.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, PragueZone(context)))).Single();

			// Both halves are asserted separately on purpose: DateTimeOffset equality compares instants, so a wrong
			// offset passes silently unless the test names it.
			result!.Value.UtcDateTime.ShouldBe(Value.UtcDateTime);
			result!.Value.Offset.ShouldBe(TimeSpan.FromHours(2));
		}

		/// <summary>
		/// Reading a component through the conversion, which is the case that works on a provider with no type able
		/// to carry the target zone's offset - the value never has to be materialised there.
		/// </summary>
		[Test]
		public void ComponentInNamedZone([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var hour = table.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value.Hour)).Single();

			// 11:20 UTC is 13:20 in Prague in June.
			hour.ShouldBe(13);
		}

		/// <summary>
		/// The BCL spelling of the same conversion has to reach the same SQL, not fall back to .NET on the providers
		/// where the explicit one works.
		/// </summary>
		[Test]
		public void TimeZoneInfoConversionMatchesAtTimeZone([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var viaBcl = table.Select(r => Sql.AsSql(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(r.Dto, zone).Hour)).Single();
			var viaSql = table.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value.Hour)).Single();

			viaBcl.ShouldBe(viaSql);
			viaBcl.ShouldBe(13);
		}

		#endregion

		#region The reading frame

		/// <summary>
		/// The frame rule with no <c>AtTimeZone</c> in sight: a component of a stored value has to agree with what
		/// the CLR answers for the value this provider hands back.
		/// </summary>
		/// <remarks>
		/// The expectation is taken from a round-trip rather than from <see cref="Value"/>, because a provider that
		/// normalises to UTC on write returns a different value than it was given - comparing against the source
		/// would test the storage instead of the translation.
		/// </remarks>
		[Test]
		public void ComponentMatchesRoundTrippedValue([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var expected = table.Select(r => r.Dto).Single().Hour;
			var actual   = table.Select(r => Sql.AsSql(r.Dto.Hour)).Single();

			actual.ShouldBe(expected);
		}

		/// <summary>
		/// The same rule where it decides a <em>date</em> rather than a time. Every other row in this fixture sits at
		/// midday, so a frame that is wrong by the forty-minute offset shows up as a wrong hour; only a row whose UTC
		/// date differs from its own can make the same error come out as a wrong day.
		/// </summary>
		[Test]
		public void DatePartsAtADayBoundaryMatchTheRoundTrip([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			// 00:20 +00:40 is 23:40 UTC on the previous day - and the previous month, and in a leap year the
			// previous day-of-year is 366.
			var value = new DateTimeOffset(2021, 1, 1, 0, 20, 0, TimeSpan.FromMinutes(40));

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(value));

			var stored = table.Select(r => r.Dto).Single();

			var actual = table
				.Select(r => new
				{
					Day       = Sql.AsSql(r.Dto.Day),
					Month     = Sql.AsSql(r.Dto.Month),
					Year      = Sql.AsSql(r.Dto.Year),
					DayOfYear = Sql.AsSql(r.Dto.DayOfYear),
				})
				.Single();

			actual.Day.ShouldBe(stored.Day);
			actual.Month.ShouldBe(stored.Month);
			actual.Year.ShouldBe(stored.Year);
			actual.DayOfYear.ShouldBe(stored.DayOfYear);
		}

		/// <summary>
		/// The shortest expression in which the frame could be applied twice: the zone is consumed by the
		/// <see cref="DateTimeOffset"/> half, and the <see cref="DateTime"/> read off it must not be framed again.
		/// </summary>
		[Test]
		public void DateTimeInNamedZoneIsFramedOnce([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var hour = table.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value.DateTime.Hour)).Single();

			// 11:20 UTC is 13:20 in Prague in June - the same answer ComponentInNamedZone asserts, reached through
			// an extra member that must contribute no second conversion.
			hour.ShouldBe(13);
		}

		/// <summary>
		/// The frame belongs to the operation, not to the spelling: <see cref="Sql.DatePart(Sql.DateParts, DateTimeOffset?)"/>
		/// and <see cref="Sql.DateAdd(Sql.DateParts, double?, DateTimeOffset?)"/> have to answer what the member
		/// spellings of the same operations answer.
		/// </summary>
		/// <remarks>
		/// Oracle is what makes this measurable rather than a tautology: its frame is a real conversion, so an
		/// unframed <c>EXTRACT</c> reads UTC while the framed one reads the stored offset's local value, and the two
		/// spellings differ by the offset whatever zone the container runs in. On PostgreSQL a UTC session hides the
		/// same divergence, which is why a green run there proves nothing on its own.
		/// </remarks>
		[Test]
		public void SqlSpellingsReadTheSameFrameAsTheMembers([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var actual = table
				.Select(r => new
				{
					MemberHour  = Sql.AsSql(r.Dto.Hour),
					SqlHour     = Sql.AsSql(Sql.DatePart(Sql.DateParts.Hour, r.Dto)),
					MemberShift = Sql.AsSql(r.Dto.AddMonths(1)),
					SqlShift    = Sql.AsSql(Sql.DateAdd(Sql.DateParts.Month, 1, r.Dto)),
				})
				.Single();

			actual.SqlHour.ShouldBe(actual.MemberHour);
			actual.SqlShift!.Value.UtcDateTime.ShouldBe(actual.MemberShift.UtcDateTime);
		}

		/// <summary>
		/// <see cref="DateTimeOffset.TimeOfDay"/> is the time half of the reading whose date half <c>.Date</c> takes,
		/// so the two have to come out of the same frame or they describe different readings.
		/// </summary>
		[Test]
		public void TimeOfDayIsReadInTheFrame([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var stored = table.Select(r => r.Dto).Single();

			table.Select(r => Sql.AsSql(r.Dto.TimeOfDay)).Single().ShouldBe(stored.TimeOfDay);
		}

		#endregion

		#region Arithmetic inside the frame

		/// <summary>
		/// The zone has to survive an intervening shift, or a component read after it silently falls back to the
		/// provider's default frame.
		/// </summary>
		[Test]
		public void ZoneSurvivesArithmetic([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var hour = table.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value.AddHours(1).Hour)).Single();

			hour.ShouldBe(14);
		}

		/// <summary>
		/// The same rule over a chain rather than a single shift: every link has to carry the zone forward, and the
		/// reading at the end has to happen in it.
		/// </summary>
		/// <remarks>
		/// One link can pass while a chain does not. Each shift leaves the frame to answer a
		/// <see cref="DateTimeOffset"/> and the next one enters it again, so a chain is where a zone gets dropped -
		/// and a wrong offset here is a wrong hour, which is what this reads.
		/// </remarks>
		[Test]
		public void ZoneSurvivesAChainOfShifts([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var hour = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value.AddDays(10).AddMonths(1).AddHours(1).Hour))
				.Single();

			// 11:20 UTC is 13:20 in Prague in June. Ten days on is 25 June, a month after that is 25 July - both
			// still inside summer time, so the zone's offset never changes and only the added hour moves the clock.
			hour.ShouldBe(14);
		}

		/// <summary>
		/// Month arithmetic is the one shift that is not frame-invariant: adding to the local reading and adding to
		/// the UTC one land on different days whenever the two disagree about the date, and the end-of-month clamp
		/// then makes the difference permanent rather than an hour's drift.
		/// </summary>
		[Test]
		public void MonthArithmeticStaysInTheFrame([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			// 2020-01-31 00:20 +00:40 is 2020-01-30 23:40 UTC. Adding a month to the local reading clamps to
			// February 29; adding it to the UTC one gives February 29 as well but a day earlier in date terms, and
			// the two answers differ. Taking the expectation from the round-trip is what makes that measurable.
			//
			// The 123 milliseconds are load-bearing, not decoration. A month shift that clamps has to go through a
			// function whose result may carry whole seconds only - Oracle's ADD_MONTHS answers a DATE - so a value
			// with nothing below the second would let that loss pass unnoticed.
			var value = new DateTimeOffset(2020, 1, 31, 0, 20, 0, 123, TimeSpan.FromMinutes(40));

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(value));

			var stored = table.Select(r => r.Dto).Single();

			var shifted = table.Select(r => Sql.AsSql(r.Dto.AddMonths(1))).Single();

			shifted.UtcDateTime.ShouldBe(stored.AddMonths(1).UtcDateTime);
		}

		/// <summary>
		/// The same shift where its result is materialised rather than read: the offset the value carries has to come
		/// back with it, which only a provider with an offset-carrying type can do.
		/// </summary>
		/// <remarks>
		/// The input deliberately does not clamp - 1 February plus a month is 1 March - so this isolates re-attaching
		/// the offset from the end-of-month question <see cref="MonthArithmeticStaysInTheFrame"/> covers.
		/// </remarks>
		[Test]
		public void AddMonthsAddsToTheLocalReading([IncludeDataSources(false, ZonedProviders)] string context)
		{
			var value = new DateTimeOffset(2020, 2, 1, 0, 20, 0, TimeSpan.FromMinutes(40));

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(value));

			var shifted = table.Select(r => Sql.AsSql(r.Dto.AddMonths(1))).Single();

			shifted.ShouldBe(value.AddMonths(1));
		}

		/// <summary>
		/// Two marks for the same instant written in different zones must subtract to zero, as the CLR does.
		/// </summary>
		[Test]
		public void ZonedDifferenceMeasuresInstants([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			var start = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
			var end   = new DateTimeOffset(2026, 1, 1, 14, 0, 0, TimeSpan.FromHours(2));

			// The premise, so a failure below cannot be blamed on the fixture data.
			(end - start).ShouldBe(TimeSpan.Zero);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(start, end));

			var hours = table
				.Where(r => r.Id == 1)
				.Select(a => Sql.AsSql((table.Where(b => b.Id == 2).Select(b => b.Dto).Single() - a.Dto).TotalHours))
				.Single();

			hours.ShouldBe(0d);
		}

		#endregion

		#region Members whose zone is their own

		/// <summary>
		/// <c>UtcDateTime</c> names its own zone, so unlike a component it must answer the same thing on every
		/// provider - the expectation is the source value's, not the round-trip's.
		/// </summary>
		[Test]
		public void UtcDateTimeIsProviderIndependent([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var utc = table.Select(r => Sql.AsSql(r.Dto.UtcDateTime)).Single();

			utc.ShouldBe(Value.UtcDateTime);
		}

		/// <summary>
		/// An intervening conversion changes only the offset the value carries, so a reader of the instant must
		/// ignore it - and must not be refused on a provider that cannot express that conversion at all.
		/// </summary>
		[Test]
		public void UtcDateTimeIgnoresAnIntermediateZone([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var utc = table.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value.UtcDateTime)).Single();

			utc.ShouldBe(Value.UtcDateTime);
		}

		/// <summary>
		/// <c>DateTime</c> is the wall clock of the value as the provider hands it back, so its expectation comes
		/// from the round-trip - the same rule as a component.
		/// </summary>
		[Test]
		public void DateTimeMatchesRoundTrippedValue([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var expected = table.Select(r => r.Dto).Single().DateTime;
			var actual   = table.Select(r => Sql.AsSql(r.Dto.DateTime)).Single();

			actual.ShouldBe(expected);
		}

		/// <summary>
		/// <c>ToUniversalTime</c> keeps the instant and zeroes the offset. On a provider that already hands every
		/// value back at <c>+00:00</c> it is the identity, so the same assertion holds either way.
		/// </summary>
		[Test]
		public void ToUniversalTimeZeroesTheOffset([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var utc = table.Select(r => Sql.AsSql(r.Dto.ToUniversalTime())).Single();

			utc.UtcDateTime.ShouldBe(Value.UtcDateTime);
			utc.Offset.ShouldBe(TimeSpan.Zero);
		}

		/// <summary>
		/// <c>ToOffset</c> keeps the instant and takes the offset asked for, so it needs a result type able to carry
		/// one - which is why it is scoped to the providers that have it.
		/// </summary>
		[Test]
		public void ToOffsetTakesTheOffsetAsked([IncludeDataSources(false, ZonedProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			// Negative and not a whole hour: the sign and the minutes are both part of the spelling the provider is
			// handed, and a whole positive hour would let either get lost.
			var shifted = table.Select(r => Sql.AsSql(r.Dto.ToOffset(TimeSpan.FromMinutes(-90)))).Single();

			shifted.UtcDateTime.ShouldBe(Value.UtcDateTime);
			shifted.Offset.ShouldBe(TimeSpan.FromMinutes(-90));
		}

		/// <summary>
		/// The offset the value carries, read on the server. Unlike a component this is not a reading in some frame -
		/// it is the frame - so its expectation comes from the round-trip, which is where the offset actually lives.
		/// </summary>
		/// <remarks>
		/// The fixture's <c>+00:40</c> is what makes this worth asserting: a provider answering in whole hours, or
		/// dropping the minutes half of the offset, would still look right against any offset that has none.
		/// </remarks>
		[Test]
		public void OffsetMatchesTheRoundTrip([IncludeDataSources(false, ZonedProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var stored = table.Select(r => r.Dto).Single();

			table.Select(r => Sql.AsSql(r.Dto.Offset)).Single().ShouldBe(stored.Offset);
#if NET8_0_OR_GREATER
			table.Select(r => Sql.AsSql(r.Dto.TotalOffsetMinutes)).Single().ShouldBe(stored.TotalOffsetMinutes);
#endif
		}

		/// <summary>
		/// A negative offset, because the two halves are signed separately on at least one provider and adding them
		/// wrongly is invisible while every offset in the fixture is positive.
		/// </summary>
		[Test]
		public void NegativeOffsetIsReadWithItsSign([IncludeDataSources(false, ZonedProviders)] string context)
		{
			var value = new DateTimeOffset(2020, 6, 15, 12, 0, 0, TimeSpan.FromMinutes(-90));

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(value));

			var stored = table.Select(r => r.Dto).Single();

#if NET8_0_OR_GREATER
			table.Select(r => Sql.AsSql(r.Dto.TotalOffsetMinutes)).Single().ShouldBe(stored.TotalOffsetMinutes);
#endif
			table.Select(r => Sql.AsSql(r.Dto.Offset)).Single().ShouldBe(stored.Offset);
		}

		/// <summary>
		/// The constructor is the inverse of <see cref="DateTimeOffset.ToOffset(TimeSpan)"/>: that one re-expresses an
		/// instant at a given offset, this one declares a zone-less reading to be at one, which is what decides the
		/// instant.
		/// </summary>
		[Test]
		public void ConstructorAttachesTheOffsetGiven([IncludeDataSources(false, ZonedProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			// Built from the stored value's own wall clock, so the answer does not depend on the round-trip: the same
			// reading declared at the same offset is the same instant on any provider.
			var stored = table.Select(r => r.Dto).Single();

			var built = table
				.Select(r => Sql.AsSql(new DateTimeOffset(r.Dto.DateTime, TimeSpan.FromMinutes(-90))))
				.Single();

			built.ShouldBe(new DateTimeOffset(stored.DateTime, TimeSpan.FromMinutes(-90)));
			built.Offset.ShouldBe(TimeSpan.FromMinutes(-90));
		}

		/// <summary>
		/// The offset cannot be a bind, so it is spelled into the SQL - which makes it part of what the query cache
		/// must key on. Two offsets over one query shape is exactly the case that would break if it were not.
		/// </summary>
		[Test]
		public void TwoOffsetsDoNotShareACachedQuery([IncludeDataSources(false, ZonedProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			// One local, re-assigned. Two separate locals would give the two lambdas different closure fields, so
			// they could not share a cache entry whatever the mechanism did - and the test would pass without
			// having exercised it.
			var offset = TimeSpan.FromHours(2);

			table.Select(r => Sql.AsSql(r.Dto.ToOffset(offset))).Single().Offset.ShouldBe(offset);

			offset = TimeSpan.FromMinutes(-90);

			table.Select(r => Sql.AsSql(r.Dto.ToOffset(offset))).Single().Offset.ShouldBe(offset);
		}

		#endregion

		#region The acceptance rule, on every provider that stores an offset

		/// <summary>
		/// The acceptance rule for the two instant-reading members on <em>every</em> provider, including those that
		/// decline to translate them.
		/// </summary>
		[Test]
		public void InstantMembersAgreeWithTheRoundTrip([SupportsDateTimeOffsetContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var stored = table.Select(r => r.Dto).Single();

			table.Select(r => r.Dto.ToUniversalTime()).Single().ShouldBe(stored.ToUniversalTime());
			table.Select(r => r.Dto.ToOffset(TimeSpan.FromMinutes(-90))).Single().ShouldBe(stored.ToOffset(TimeSpan.FromMinutes(-90)));
		}

		/// <summary>
		/// The same rule for the two wall-clock members: the answer is either the server's or .NET's, and both must
		/// equal what the CLR computes on the value as that provider hands it back.
		/// </summary>
		/// <remarks>
		/// Without <see cref="Sql.AsSql{T}(T)"/> on purpose. The tests above force these server-side and so only
		/// cover providers that can express them; this one covers the refusal path, where a provider with no way to
		/// read a wall clock out of an instant must fall back rather than emit a cast to a type it does not have.
		/// </remarks>
		[Test]
		public void WallClockMembersAgreeWithTheRoundTrip([SupportsDateTimeOffsetContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var stored = table.Select(r => r.Dto).Single();

			table.Select(r => r.Dto.DateTime).Single().ShouldBe(stored.DateTime);

			// UtcDateTime names its own zone, so it is the one member whose answer does not depend on the round-trip.
			table.Select(r => r.Dto.UtcDateTime).Single().ShouldBe(Value.UtcDateTime);
		}

		/// <summary>
		/// The two members whose zone is the evaluating machine's must stay in .NET on every provider: a server-side
		/// answer would use the <em>server's</em> zone, which is a different question with the same spelling.
		/// </summary>
		/// <remarks>
		/// Deliberately without <see cref="Sql.AsSql{T}(T)"/> - forcing these server-side is what must never happen,
		/// so the test asserts the value rather than the refusal.
		/// </remarks>
		[Test]
		public void MachineZoneMembersStayClientSide([SupportsDateTimeOffsetContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var stored = table.Select(r => r.Dto).Single();

			table.Select(r => r.Dto.LocalDateTime).Single().ShouldBe(stored.LocalDateTime);
			table.Select(r => r.Dto.ToLocalTime()).Single().ShouldBe(stored.ToLocalTime());
		}

		#endregion

		#region Refusals

		/// <summary>
		/// The third acceptance outcome: where the value cannot leave the server and the provider genuinely cannot
		/// express the operation, the refusal is by name rather than a wrong number or a generic failure.
		/// </summary>
		/// <remarks>
		/// A plain projection would be allowed to fall back to .NET - that is the second outcome, and
		/// <see cref="WallClockMembersAgreeWithTheRoundTrip"/> covers it - so the refusal is only visible where
		/// falling back is forbidden, which is what <see cref="Sql.AsSql{T}(T)"/> does here.
		/// <para>
		/// The same refusal inside a comparison - <c>Where(r =&gt; AtTimeZone(...) == value)</c> - reaches the caller
		/// as the generic "could not be converted to SQL" with no reason attached. Measured by raising the named
		/// error unconditionally and reading the whole message rather than its first line: the error is built and its
		/// text is dropped somewhere on the comparison path, not withheld by the translator. Its own fix, and until
		/// then this is the position that shows the name.
		/// </para>
		/// </remarks>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), OffsetlessProviders, ErrorMessage = ErrorHelper.Error_TimeZone_ZonedResult)]
		public void AtTimeZoneRefusesToMaterialiseWhereNoTypeCarriesAnOffset([IncludeDataSources(false, OffsetlessProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			table.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone))).Single();
		}

		/// <summary>
		/// <c>ToOffset</c> is the same gap reached through a BCL member rather than through <c>Sql.AtTimeZone</c>: it
		/// answers an offset-carrying value, which these providers have no type for.
		/// </summary>
		[Test]
		[ThrowsCannotBeConverted(OffsetlessProviders)]
		public void ToOffsetRefusesWhereNoTypeCarriesAnOffset([IncludeDataSources(false, OffsetlessProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			table.Count(r => r.Dto.ToOffset(TimeSpan.FromHours(2)) == Value).ShouldBe(1);
		}

		/// <summary>
		/// A provider with no way to read a wall clock out of an instant declines <c>DateTime</c> rather than casting
		/// to a type its dialect does not have - which on SQLite would answer the UTC reading instead of the value's
		/// own, and pass for a right answer.
		/// </summary>
		[Test]
		[ThrowsCannotBeConverted(TestProvName.AllSQLite)]
		public void WallClockRefusedWhereNoZoneSupportExists([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			table.Count(r => r.Dto.DateTime == Value.DateTime).ShouldBe(1);
		}

		#endregion
	}
}
