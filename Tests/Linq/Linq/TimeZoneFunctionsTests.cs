using System;
using System.Globalization;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class TimeZoneFunctionsTests : TestBase
	{
		[Table]
		sealed class ZonedRow
		{
			// YDB requires one on every table, and this fixture's gate does not exclude it.
			[PrimaryKey] public int        Id  { get; set; }
			[Column] public DateTimeOffset Dto { get; set; }
		}

		// 12:00 +00:40 is 11:20 UTC. The forty-minute offset is deliberate and is the whole point of the fixture:
		// no real zone uses it, so an answer that quietly reads in the server's zone cannot coincide with the right
		// one. A row whose offset matches the container's would let every one of these tests pass while broken.
		static readonly DateTimeOffset Value = new (2020, 6, 15, 12, 00, 00, TimeSpan.FromMinutes(40));

		[Table]
		sealed class DateRow
		{
			[PrimaryKey]                          public int      Id { get; set; }
			// A DATE on Oracle, which is how a DateTime is commonly stored there.
			[Column(DataType = DataType.DateTime)] public DateTime Dt { get; set; }
		}

		[Table]
		sealed class StampRow
		{
			[PrimaryKey]                           public int            Id  { get; set; }
			[Column(DataType = DataType.DateTime2)] public DateTimeOffset Dto { get; set; }
		}

		[Table]
		sealed class DatedRow
		{
			[PrimaryKey]                           public int            Id  { get; set; }
			// A DATE on Oracle: no offset and no fraction of a second.
			[Column(DataType = DataType.DateTime)] public DateTimeOffset Dto { get; set; }
		}

		// One value stored with an offset and one without, side by side, for the expressions that can answer either.
		[Table]
		sealed class MixedStampRow
		{
			[PrimaryKey]                            public int             Id     { get; set; }
			[Column]                                public bool            Flag   { get; set; }
			[Column]                                public DateTimeOffset  Dto    { get; set; }
			[Column]                                public DateTimeOffset  Dto2   { get; set; }
			[Column(DataType = DataType.DateTime2)] public DateTimeOffset  Stamp  { get; set; }
			[Column(DataType = DataType.DateTime2)] public DateTimeOffset? StampN { get; set; }
		}

		[Table]
		sealed class PreciseRow
		{
			[PrimaryKey]                                                public int            Id  { get; set; }
			[Column(DataType = DataType.DateTimeOffset, Precision = 7)] public DateTimeOffset Dto { get; set; }
		}

		static ZonedRow[] Rows(params DateTimeOffset[] values)
			=> values
				.Select((v, i) => new ZonedRow { Id = i + 1, Dto = v })
				.ToArray();

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

		/// <summary>
		/// The conversion as a value in its own right: same instant, the target zone's offset. It needs a result type
		/// able to carry an offset, which is why it is scoped to the providers that have one.
		/// </summary>
		[Test]
		public void AtTimeZoneKeepsInstantAndTakesTargetOffset([IncludeDataSources(true, ZonedProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var result = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, PragueZone(context))))
				.Single();

			// Both halves are asserted separately on purpose: DateTimeOffset equality compares instants, so a wrong
			// offset passes silently unless the test names it.
			result!.Value.UtcDateTime.ShouldBe(Value.UtcDateTime);
			result!.Value.Offset.ShouldBe(TimeSpan.FromHours(2));
		}

		/// <summary>
		/// The other direction, and the only way a caller reaches the attach form with a zone <em>name</em>: every
		/// other spelling in this fixture hands the conversion a fixed offset, which routes to a different function
		/// and leaves <c>AT TIME ZONE '&lt;name&gt;'</c> over a zone-less value unexercised.
		/// </summary>
		[Test]
		public void AtTimeZoneReadsAWallClockInTheNamedZone([IncludeDataSources(false, ZonedProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var result = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto.DateTime, zone)))
				.Single();

			// Same reason as above - and the wall clock is taken from the round-trip because it is the provider's own.
			result!.Value.Offset.ShouldBe(TimeSpan.FromHours(2));
			result!.Value.UtcDateTime.ShouldBe(new DateTimeOffset(stored.DateTime, TimeSpan.FromHours(2)).UtcDateTime);
		}

		/// <summary>
		/// The same attach on a provider with no type that can carry the zone's offset. <c>Sql.AtTimeZone</c> is
		/// server-side only, so the answer is the server's: the instant the wall clock denotes in the zone, read back
		/// at <c>+00:00</c> as every value these providers return is.
		/// </summary>
		[Test]
		public void AtTimeZoneWallClockReadsBackTheInstantWhereNoTypeCarriesAnOffset([IncludeDataSources(false, OffsetlessProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var result = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto.DateTime, zone)))
				.Single();

			var expected = new DateTimeOffset(stored.DateTime, TimeSpan.FromHours(2));

			result!.Value.UtcDateTime.ShouldBe(expected.UtcDateTime);
			result!.Value.Offset.ShouldBe(TimeSpan.Zero);
		}

		/// <summary>
		/// <c>Sql.AtTimeZone</c> is answered by the database's zone data, so it has no .NET evaluation.
		/// </summary>
		[Test]
		public void AtTimeZoneIsServerSideOnly()
		{
			Assert.That(() => Sql.AtTimeZone(Value,          "Europe/Prague"), Throws.InstanceOf<ServerSideOnlyException>());
			Assert.That(() => Sql.AtTimeZone(Value.DateTime, "Europe/Prague"), Throws.InstanceOf<ServerSideOnlyException>());
		}

		/// <summary>
		/// Where the provider cannot express the conversion there is nothing to fall back to, so even a plain
		/// projection - top-level or read through a member - refuses rather than answering on the client.
		/// </summary>
		[Test]
		public void AtTimeZoneRefusesInAProjectionWhereThereIsNoZoneSupport([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllMySql, TestProvName.AllClickHouse, TestProvName.AllYdb, TestProvName.AllSqlServer2014Minus)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			Assert.That(
				() => table
					.Select(r => Sql.AtTimeZone(r.Dto, "Europe/Prague"))
					.ToList(),
				Throws.InstanceOf<LinqToDBException>());

			Assert.That(
				() => table
					.Select(r => Sql.AtTimeZone(r.Dto.DateTime, "Europe/Prague"))
					.ToList(),
				Throws.InstanceOf<LinqToDBException>());

			Assert.That(
				() => table
					.Select(r => new { H = Sql.AtTimeZone(r.Dto, "Europe/Prague")!.Value.Hour })
					.ToList(),
				Throws.InstanceOf<LinqToDBException>());
		}

		/// <summary>
		/// The <see cref="DateTimeOffset"/> overload needs a type that carries an offset, so on a provider without one
		/// a plain projection refuses too; the wall-clock overload attaches a zone, which such a provider can do.
		/// </summary>
		[Test]
		public void AtTimeZoneInAProjectionWhereNoTypeCarriesAnOffset([IncludeDataSources(OffsetlessProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var stored = table
				.Select(r => r.Dto)
				.Single();

			Assert.That(
				() => table
					.Select(r => Sql.AtTimeZone(r.Dto, zone))
					.ToList(),
				Throws.InstanceOf<LinqToDBException>());

			var attached = table
				.Select(r => Sql.AtTimeZone(r.Dto.DateTime, zone))
				.Single();

			attached!.Value.UtcDateTime.ShouldBe(new DateTimeOffset(stored.DateTime, TimeSpan.FromHours(2)).UtcDateTime);
		}

		/// <summary>
		/// A refusal names the gap the provider actually has: one that cannot read a wall clock in a zone at all is
		/// not told to read a component instead.
		/// </summary>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSQLite, TestProvName.AllMySql, TestProvName.AllClickHouse, TestProvName.AllYdb, TestProvName.AllSqlServer2014Minus, ErrorMessage = ErrorHelper.Error_TimeZone_Conversion)]
		public void AtTimeZoneRefusalNamesTheMissingConversion([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllMySql, TestProvName.AllClickHouse, TestProvName.AllYdb, TestProvName.AllSqlServer2014Minus)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, "Europe/Prague")))
				.Single();
		}

		/// <summary>
		/// Reading a component through the conversion, which is the case that works on a provider with no type able
		/// to carry the target zone's offset - the value never has to be materialised there.
		/// </summary>
		[Test]
		public void ComponentInNamedZone([IncludeDataSources(true, ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var hour = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value.Hour))
				.Single();

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

			var viaBcl = table
				.Select(r => Sql.AsSql(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(r.Dto, zone).Hour))
				.Single();
			var viaSql = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value.Hour))
				.Single();

			viaBcl.ShouldBe(viaSql);
			viaBcl.ShouldBe(13);
		}

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
		[ActiveIssue(1855, Configurations = [TestProvName.AllSQLiteClassic], Details = "The storage keeps the offset - the round-trip returns 12:00 +00:40 - but the component is read off the UTC reading, so the hour answers 11 where the CLR says 12. A wrong number rather than a refusal, so it is recorded rather than skipped.")]
		public void ComponentMatchesRoundTrippedValue([SupportsDateTimeOffsetContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var expected = table
				.Select(r => r.Dto)
				.Single()
				.Hour;
			var actual   = table
				.Select(r => Sql.AsSql(r.Dto.Hour))
				.Single();

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

			var stored = table
				.Select(r => r.Dto)
				.Single();

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

			var hour = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value.DateTime.Hour))
				.Single();

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

			var stored = table
				.Select(r => r.Dto)
				.Single();

			table
				.Select(r => Sql.AsSql(r.Dto.TimeOfDay))
				.Single()
				.ShouldBe(stored.TimeOfDay);
		}

		/// <summary>
		/// The time half keeps the fraction of a second the column holds: 12:00:00.5, not 12:00:00.
		/// </summary>
		[Test]
		public void TimeOfDayKeepsTheFractionOfASecond([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value.AddMilliseconds(500)));

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var timeOfDay = table
				.Select(r => Sql.AsSql(r.Dto.TimeOfDay))
				.Single();

			timeOfDay.ShouldBe(stored.TimeOfDay);
		}

		/// <summary>
		/// The time half of an operand stored as a date and time without a fraction - a <see cref="DateTimeOffset"/>
		/// mapped onto one, or a wall clock read through <c>Sql.AtTimeZone</c> - which is an Oracle <c>DATE</c>.
		/// </summary>
		[Test]
		public void TimeOfDayOfADateOperand([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			var zone  = PragueZone(context);
			var local = new DateTime(2020, 6, 15, 12, 0, 0);

			using var db    = GetDataContext(context);
			using var dated = db.CreateLocalTable(new[] { new DatedRow { Id = 1, Dto = Value } });
			using var dates = db.CreateLocalTable(new[] { new DateRow  { Id = 1, Dt  = local } });

			var stored = dated
				.Select(r => r.Dto)
				.Single();

			var timeOfDay = dated
				.Select(r => Sql.AsSql(r.Dto.TimeOfDay))
				.Single();

			var wallClockTimeOfDay = dates
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dt, zone)!.Value.TimeOfDay))
				.Single();

			timeOfDay.ShouldBe(stored.TimeOfDay);
			wallClockTimeOfDay.ShouldBe(local.TimeOfDay);
		}

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

			var hour = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value.AddHours(1).Hour))
				.Single();

			hour.ShouldBe(14);
		}

		/// <summary>
		/// The same rule for the wall-clock overload. Reading a component straight back through it is the identity
		/// and needs no zone, but an operation whose own result is a <see cref="DateTimeOffset"/> leaves the frame
		/// and has to re-enter the zone the caller named rather than the provider's default.
		/// </summary>
		[Test]
		public void WallClockZoneSurvivesArithmetic([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var shifted = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto.DateTime, zone)!.Value.AddDays(1)))
				.Single();

			// The wall clock is taken from the round-trip because it is the provider's own - 12:00 where the offset
			// survives, 11:20 where the value came back as UTC. Prague is +02:00 on both 15 and 16 June, so the
			// shift cannot move the offset.
			var expected = new DateTimeOffset(stored.DateTime, TimeSpan.FromHours(2))
				.AddDays(1);

			shifted.ShouldBe(expected);
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

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var shifted = table
				.Select(r => Sql.AsSql(r.Dto.AddMonths(1)))
				.Single();

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

			var shifted = table
				.Select(r => Sql.AsSql(r.Dto.AddMonths(1)))
				.Single();

			// Both halves, for the reason the first test in this file gives: equality compares instants, so the
			// offset this test exists to check is the one half it would otherwise never look at.
			shifted.ShouldBe(value.AddMonths(1));
			shifted.Offset.ShouldBe(value.Offset);
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
			var premise = end - start;

			premise.ShouldBe(TimeSpan.Zero);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(start, end));

			var hours = table
				.Where(r => r.Id == 1)
				.Select(a => Sql.AsSql((table.Where(b => b.Id == 2).Select(b => b.Dto).Single() - a.Dto).TotalHours))
				.Single();

			hours.ShouldBe(0d);
		}

		/// <summary>
		/// A shift that crosses a daylight-saving change. <see cref="DateTimeOffset.AddMonths(int)"/> keeps the offset the
		/// value carries, so re-attaching the zone by name after the shift - which resolves the offset afresh at the new
		/// wall clock - lands on a different instant.
		/// </summary>
		[Test]
		public void ShiftAcrossDaylightSavingKeepsTheOffset([IncludeDataSources(true, ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var shifted = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value.AddMonths(6)))
				.Single();

			// 11:20 UTC is 13:20 +02:00 in Prague; six months on .NET answers 2020-12-15 13:20 +02:00, i.e. 11:20 UTC.
			shifted.UtcDateTime.ShouldBe(new DateTime(2020, 12, 15, 11, 20, 0, DateTimeKind.Utc));
		}

		/// <summary>
		/// The other half of the same shift, where a type can carry it: the offset .NET keeps is the zone's at the
		/// source, not at the shifted wall clock.
		/// </summary>
		[Test]
		public void ShiftAcrossDaylightSavingCarriesTheSourceOffset([IncludeDataSources(true, ZonedProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var shifted = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value.AddMonths(6)))
				.Single();

			shifted.Offset.ShouldBe(TimeSpan.FromHours(2));
		}

		/// <summary>
		/// The same shift through the wall-clock overload, whose frame is the wall clock itself.
		/// </summary>
		[Test]
		public void WallClockShiftAcrossDaylightSavingKeepsTheOffset([IncludeDataSources(true, ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var shifted = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto.DateTime, zone)!.Value.AddMonths(6)))
				.Single();

			// The wall clock is the provider's own; Prague is +02:00 in June, and .NET keeps that through the shift.
			var expected = new DateTimeOffset(stored.DateTime, TimeSpan.FromHours(2))
				.AddMonths(6);

			shifted.UtcDateTime.ShouldBe(expected.UtcDateTime);

			if (context.IsAnyOf(ZonedProviders))
				shifted.Offset.ShouldBe(expected.Offset);
		}

		/// <summary>
		/// A chain whose first link crosses the transition: the next link has to read the wall clock in the offset
		/// the first one kept, not in the zone's offset at the new instant.
		/// </summary>
		[Test]
		public void ChainAcrossDaylightSavingKeepsTheOffset([IncludeDataSources(true, ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var actual = table
				.Select(r => new
				{
					Hour = Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value
						.AddMonths(6)
						.AddHours(1)
						.Hour),
					Shifted = Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value
						.AddMonths(6)
						.AddHours(1)),
				})
				.Single();

			// 13:20 +02:00, six months on 13:20 +02:00, an hour on 14:20 +02:00 - i.e. 12:20 UTC.
			actual.Hour.ShouldBe(14);
			actual.Shifted.UtcDateTime.ShouldBe(new DateTime(2020, 12, 15, 12, 20, 0, DateTimeKind.Utc));

			if (context.IsAnyOf(ZonedProviders))
				actual.Shifted.Offset.ShouldBe(TimeSpan.FromHours(2));
		}

		/// <summary>
		/// A zoned value reached through a projected member rather than spelled in the operand: the conversion is then
		/// translated first and the shift reads its frame off the node.
		/// </summary>
		[Test]
		public void ShiftOfAProjectedWallClockInAZone([IncludeDataSources(ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var shifted = table
				.Select(r => new { Z = Sql.AtTimeZone(r.Dto.DateTime, zone) })
				.Select(x => Sql.AsSql(x.Z!.Value.AddMonths(6)))
				.Single();

			var expected = new DateTimeOffset(stored.DateTime, TimeSpan.FromHours(2))
				.AddMonths(6);

			shifted.UtcDateTime.ShouldBe(expected.UtcDateTime);

			if (context.IsAnyOf(ZonedProviders))
				shifted.Offset.ShouldBe(expected.Offset);
		}

		/// <summary>
		/// The same for an instant re-expressed in a zone, which only a provider with an offset-carrying type can
		/// produce as a value in the first place.
		/// </summary>
		[Test]
		[ThrowsCannotBeConverted(OffsetlessProviders)]
		public void ShiftOfAProjectedInstantInAZone([IncludeDataSources(ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var shifted = table
				.Select(r => new { Z = Sql.AtTimeZone(r.Dto, zone) })
				.Select(x => Sql.AsSql(x.Z!.Value.AddMonths(6)))
				.Single();

			shifted.UtcDateTime.ShouldBe(new DateTime(2020, 12, 15, 11, 20, 0, DateTimeKind.Utc));
			shifted.Offset.ShouldBe(TimeSpan.FromHours(2));
		}

		/// <summary>
		/// <see cref="Sql.DateAdd(Sql.DateParts, double?, DateTimeOffset?)"/> leaves the frame the same way the
		/// <c>AddX</c> members do.
		/// </summary>
		[Test]
		public void DateAddAcrossDaylightSavingKeepsTheOffset([IncludeDataSources(ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var shifted = table
				.Select(r => Sql.AsSql(Sql.DateAdd(Sql.DateParts.Month, 6, Sql.AtTimeZone(r.Dto, zone))))
				.Single();

			shifted!.Value.UtcDateTime.ShouldBe(new DateTime(2020, 12, 15, 11, 20, 0, DateTimeKind.Utc));

			if (context.IsAnyOf(ZonedProviders))
				shifted.Value.Offset.ShouldBe(TimeSpan.FromHours(2));
		}

		/// <summary>
		/// A <see cref="DateTimeOffset"/> mapped onto a column that carries no offset. The frame leaves such a value
		/// alone, so the way back out of it has to leave it alone too.
		/// </summary>
		[Test]
		public void ShiftOfAValueStoredWithoutAnOffset([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new StampRow { Id = 1, Dto = Value } });

			var stored  = table
				.Select(r => r.Dto)
				.Single();
			var shifted = table
				.Select(r => Sql.AsSql(r.Dto.AddDays(1)))
				.Single();

			shifted.ShouldBe(stored.AddDays(1));
		}

		/// <summary>
		/// <see cref="DateTimeOffset.Offset"/> of a value stored without an offset: SQL has none to read, so the answer
		/// is the round-tripped value's, as on master, where the member was not translated.
		/// </summary>
		[Test]
		public void OffsetOfAValueStoredWithoutAnOffset([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new StampRow { Id = 1, Dto = Value } });

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var offset = table
				.Select(r => r.Dto.Offset)
				.Single();

			offset.ShouldBe(stored.Offset);
		}

		/// <summary>
		/// The instant members of a value stored without an offset: SQL has no instant to read either, so they answer
		/// what .NET does on the round-tripped value.
		/// </summary>
		[Test]
		public void InstantOfAValueStoredWithoutAnOffset([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new StampRow { Id = 1, Dto = Value } });

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var utcDateTime = table
				.Select(r => r.Dto.UtcDateTime)
				.Single();

			var utc = table
				.Select(r => r.Dto.ToUniversalTime())
				.Single();

			var shifted = table
				.Select(r => r.Dto.ToOffset(TimeSpan.FromMinutes(-90)))
				.Single();

			utcDateTime.ShouldBe(stored.UtcDateTime);
			utc.ShouldBe(stored.ToUniversalTime());
			utc.Offset.ShouldBe(TimeSpan.Zero);
			shifted.ShouldBe(stored.ToOffset(TimeSpan.FromMinutes(-90)));
			shifted.Offset.ShouldBe(TimeSpan.FromMinutes(-90));

#if NET8_0_OR_GREATER
			var totalOffsetMinutes = table
				.Select(r => r.Dto.TotalOffsetMinutes)
				.Single();

			totalOffsetMinutes.ShouldBe(stored.TotalOffsetMinutes);
#endif
		}

		/// <summary>
		/// <see cref="DateTimeOffset.DateTime"/> of a value stored without an offset. The column alone would be read
		/// back through local time, which moves the kind - and on DuckDB the value.
		/// </summary>
		[Test]
		public void DateTimeOfAValueStoredWithoutAnOffset([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new StampRow { Id = 1, Dto = Value } });

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var dateTime = table
				.Select(r => r.Dto.DateTime)
				.Single();

			dateTime.ShouldBe(stored.DateTime);
			dateTime.Kind.ShouldBe(stored.DateTime.Kind);
		}

#if !NETFRAMEWORK
		/// <summary>
		/// A zone conversion of a value stored without an offset. Applied to such a value <c>AT TIME ZONE</c> attaches
		/// the zone instead of converting to it, so the conversion is left to .NET.
		/// </summary>
		/// <remarks>
		/// Not on .NET Framework, where <see cref="TimeZoneInfo"/> takes Windows identifiers only and the .NET
		/// evaluation cannot resolve the IANA one the other providers need.
		/// </remarks>
		[Test]
		public void ZoneConversionOfAValueStoredWithoutAnOffset([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new StampRow { Id = 1, Dto = Value } });

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var hour = table
				.Select(r => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(r.Dto, zone).Hour)
				.Single();

			var converted = table
				.Select(r => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(r.Dto, zone))
				.Single();

			// Prague is +02:00 in June.
			var expected = stored.ToOffset(TimeSpan.FromHours(2));

			hour.ShouldBe(expected.Hour);
			converted.ShouldBe(expected);
			converted.Offset.ShouldBe(expected.Offset);
		}

		/// <summary>
		/// A zone conversion whose value or zone only .NET can compute is left to .NET in a projection, as on a
		/// provider without zone support, rather than refused.
		/// </summary>
		/// <remarks>
		/// Not on .NET Framework, for the reason given at <see cref="ZoneConversionOfAValueStoredWithoutAnOffset"/>.
		/// </remarks>
		[Test]
		public void ZoneConversionOfAClientValueIsReadInDotNet([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var converted = table
				.Select(r => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(ClientOnly(r.Dto), zone))
				.Single();

			var hour = table
				.Select(r => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(ClientOnly(r.Dto), zone).Hour)
				.Single();

			var byRow = table
				.Select(r => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(r.Dto, ZoneOf(r.Id, zone)))
				.Single();

			var expected = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(stored, zone);

			converted.ShouldBe(expected);
			converted.Offset.ShouldBe(expected.Offset);
			hour.ShouldBe(expected.Hour);
			byRow.ShouldBe(expected);
			byRow.Offset.ShouldBe(expected.Offset);
		}

		static DateTimeOffset ClientOnly(DateTimeOffset value) => value;

		static string ZoneOf(int id, string zone) => id > 0 ? zone : "UTC";
#endif

		/// <summary>
		/// <c>Sql.AtTimeZone</c> of a value stored without an offset has nothing to convert and no .NET answer, so it
		/// is refused by name. In a filter the refusal keeps only the generic message.
		/// </summary>
		[Test]
		public void AtTimeZoneRefusesAValueStoredWithoutAnOffset([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new StampRow { Id = 1, Dto = Value } });

			Assert.That(
				() => table
					.Select(r => Sql.AtTimeZone(r.Dto, zone))
					.ToList(),
				Throws.InstanceOf<LinqToDBException>().With.Message.Contains(ErrorHelper.Error_TimeZone_OffsetlessValue));

			Assert.That(
				() => table
					.Select(r => new { H = Sql.AtTimeZone(r.Dto, zone)!.Value.Hour })
					.ToList(),
				Throws.InstanceOf<LinqToDBException>().With.Message.Contains(ErrorHelper.Error_TimeZone_OffsetlessValue));

			Assert.That(
				() => table
					.Count(r => Sql.AtTimeZone(r.Dto, zone)!.Value.Hour == 1),
				Throws.InstanceOf<LinqToDBException>());
		}

		public enum OffsetlessMember
		{
			Offset,
			UtcDateTime,
			ToUniversalTime,
			ToOffset,
			DateTime,
			ConvertedHour,
		}

		/// <summary>
		/// Every member of a value stored without an offset that reads its offset or its instant refuses where SQL is
		/// required, rather than reading the stored wall clock as one.
		/// </summary>
		[Test]
		[ThrowsCannotBeConverted(ZoneReadingProviders)]
		public void OffsetlessValueRefusesWhereSqlIsRequired([IncludeDataSources(false, ZoneReadingProviders)] string context, [Values] OffsetlessMember member)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new StampRow { Id = 1, Dto = Value } });

			switch (member)
			{
				case OffsetlessMember.Offset:
					table
						.Select(r => Sql.AsSql(r.Dto.Offset))
						.Single();
					break;

				case OffsetlessMember.UtcDateTime:
					table
						.Select(r => Sql.AsSql(r.Dto.UtcDateTime))
						.Single();
					break;

				case OffsetlessMember.ToUniversalTime:
					table
						.Select(r => Sql.AsSql(r.Dto.ToUniversalTime()))
						.Single();
					break;

				case OffsetlessMember.ToOffset:
					table
						.Select(r => Sql.AsSql(r.Dto.ToOffset(TimeSpan.FromMinutes(-90))))
						.Single();
					break;

				case OffsetlessMember.DateTime:
					table
						.Select(r => Sql.AsSql(r.Dto.DateTime))
						.Single();
					break;

				case OffsetlessMember.ConvertedHour:
					table
						.Select(r => Sql.AsSql(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(r.Dto, zone).Hour))
						.Single();
					break;
			}
		}

		public enum MixedStorage
		{
			Coalesce,
			Condition,
			CoalesceOffset,
			SubQuery,
			Cte,
			Concat,
			CoalesceHour,
			ConditionHour,
			CoalesceDate,
			CoalesceShift,
		}

		/// <summary>
		/// A value that can answer either a column stored with an offset or one stored without takes its type from
		/// one of them - also through a subquery, a CTE or a set operation - so it is treated as having none.
		/// </summary>
		[Test]
		[ThrowsCannotBeConverted(ZoneReadingProviders)]
		public void MixedOffsetStorageRefusesWhereSqlIsRequired([IncludeDataSources(false, ZoneReadingProviders)] string context, [Values] MixedStorage storage)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new MixedStampRow { Id = 1, Dto = Value, Dto2 = Value, Stamp = Value, StampN = Value } });

			switch (storage)
			{
				case MixedStorage.Coalesce:
					table
						.Select(r => Sql.AsSql((r.StampN ?? r.Dto).UtcDateTime))
						.Single();
					break;

				case MixedStorage.Condition:
					table
						.Select(r => Sql.AsSql((r.Flag ? r.Dto : r.Stamp).UtcDateTime))
						.Single();
					break;

				case MixedStorage.CoalesceOffset:
					table
						.Select(r => Sql.AsSql((r.StampN ?? r.Dto).Offset))
						.Single();
					break;

				case MixedStorage.SubQuery:
					table
						.Select(r => new { V = r.Flag ? r.Dto : r.Stamp })
						.AsSubQuery()
						.Select(x => Sql.AsSql(x.V.UtcDateTime))
						.Single();
					break;

				case MixedStorage.Cte:
					table
						.Select(r => new { V = r.Flag ? r.Dto : r.Stamp })
						.AsCte()
						.Select(x => Sql.AsSql(x.V.UtcDateTime))
						.Single();
					break;

				case MixedStorage.Concat:
					var withOffset = table
						.Where(r => r.Flag)
						.Select(r => new { V = r.Dto });

					var withoutOffset = table
						.Where(r => !r.Flag)
						.Select(r => new { V = r.Stamp });

					withOffset
						.Concat(withoutOffset)
						.Select(x => Sql.AsSql(x.V.UtcDateTime))
						.Single();
					break;

				// A component or a shift has no single frame that reads both kinds right.
				case MixedStorage.CoalesceHour:
					table
						.Select(r => Sql.AsSql((r.StampN ?? r.Dto).Hour))
						.Single();
					break;

				case MixedStorage.ConditionHour:
					table
						.Select(r => Sql.AsSql((r.Flag ? r.Dto : r.Stamp).Hour))
						.Single();
					break;

				case MixedStorage.CoalesceDate:
					table
						.Select(r => Sql.AsSql((r.StampN ?? r.Dto).Date))
						.Single();
					break;

				case MixedStorage.CoalesceShift:
					table
						.Select(r => Sql.AsSql((r.StampN ?? r.Dto).AddHours(1)))
						.Single();
					break;
			}
		}

		/// <summary>
		/// In a projection the components and shifts of such a value are read in .NET, on the round-tripped value,
		/// whichever column answers it.
		/// </summary>
		[Test]
		public void MixedOffsetComponentsAreReadInDotNet([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[]
			{
				new MixedStampRow { Id = 1, Flag = true,  Dto = Value, Dto2 = Value, Stamp = Value, StampN = null  },
				new MixedStampRow { Id = 2, Flag = false, Dto = Value, Dto2 = Value, Stamp = Value, StampN = Value },
			});

			// The round trip of the expression, not of its columns: the server converts the operands to one type.
			var stored = table
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					Coalesced = r.StampN ?? r.Dto,
					Chosen    = r.Flag ? r.Dto : r.Stamp,
				})
				.ToArray();

			var actual = table
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					Hour      = (r.StampN ?? r.Dto).Hour,
					Condition = (r.Flag ? r.Dto : r.Stamp).Hour,
					Date      = (r.StampN ?? r.Dto).Date,
					Shifted   = (r.StampN ?? r.Dto).AddHours(1),
				})
				.ToArray();

			actual.Length.ShouldBe(stored.Length);

			for (var i = 0; i < stored.Length; i++)
			{
				var value = stored[i].Coalesced;

				actual[i].Hour.ShouldBe(value.Hour);
				actual[i].Condition.ShouldBe(stored[i].Chosen.Hour);
				actual[i].Date.ShouldBe(value.Date);
				actual[i].Date.Kind.ShouldBe(value.Date.Kind);
				actual[i].Shifted.ShouldBe(value.AddHours(1));
				actual[i].Shifted.Offset.ShouldBe(value.Offset);
			}
		}

		/// <summary>
		/// Only the values a <c>CASE</c> can answer decide its offset: one stored without an offset in its condition
		/// does not stop it.
		/// </summary>
		[Test]
		public void MixedOffsetConditionStillTranslates([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new MixedStampRow { Id = 1, Dto = Value, Dto2 = Value, Stamp = Value, StampN = Value } });

			var stored = table
				.Single();

			var utc = table
				.Select(r => Sql.AsSql((r.Stamp < r.Dto ? r.Dto : r.Dto2).UtcDateTime))
				.Single();

			utc.ShouldBe((stored.Stamp < stored.Dto ? stored.Dto : stored.Dto2).UtcDateTime);
		}

		/// <summary>
		/// A shift through the frame keeps every digit the column holds. The expectation is the round-tripped value,
		/// so a provider that stores fewer digits is held only to what it stores.
		/// </summary>
		[Test]
		public void ShiftKeepsTheColumnsPrecision([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new PreciseRow { Id = 1, Dto = Value.AddTicks(1234567) } });

			var stored  = table
				.Select(r => r.Dto)
				.Single();
			var shifted = table
				.Select(r => Sql.AsSql(r.Dto.AddDays(1)))
				.Single();

			// Ticks rather than the value, which prints to the second and would show two equal-looking values.
			shifted.UtcTicks.ShouldBe(stored.AddDays(1).UtcTicks);
		}

		/// <summary>
		/// <c>UtcDateTime</c> names its own zone, so unlike a component it must answer the same thing on every
		/// provider - the expectation is the source value's, not the round-trip's.
		/// </summary>
		[Test]
		public void UtcDateTimeIsProviderIndependent([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var utc = table
				.Select(r => Sql.AsSql(r.Dto.UtcDateTime))
				.Single();

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

			var utc = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value.UtcDateTime))
				.Single();

			utc.ShouldBe(Value.UtcDateTime);
		}

		/// <summary>
		/// The instant a wall clock denotes in a zone is defined by the attach, so it is read straight off it.
		/// </summary>
		[Test]
		public void UtcDateTimeOfAWallClockInAZone([IncludeDataSources(ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var utc = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto.DateTime, zone)!.Value.UtcDateTime))
				.Single();

			utc.ShouldBe(new DateTimeOffset(stored.DateTime, TimeSpan.FromHours(2)).UtcDateTime);
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

			var expected = table
				.Select(r => r.Dto)
				.Single()
				.DateTime;
			var actual   = table
				.Select(r => Sql.AsSql(r.Dto.DateTime))
				.Single();

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

			var utc = table
				.Select(r => Sql.AsSql(r.Dto.ToUniversalTime()))
				.Single();

			utc.UtcDateTime.ShouldBe(Value.UtcDateTime);
			utc.Offset.ShouldBe(TimeSpan.Zero);
		}

		/// <summary>
		/// <c>ToOffset</c> keeps the instant and takes the offset asked for, so it needs a result type able to carry
		/// one - which is why it is scoped to the providers that have it.
		/// </summary>
		[Test]
		public void ToOffsetTakesTheOffsetAsked([IncludeDataSources(true, ZonedProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			// Negative and not a whole hour: the sign and the minutes are both part of the spelling the provider is
			// handed, and a whole positive hour would let either get lost.
			var shifted = table
				.Select(r => Sql.AsSql(r.Dto.ToOffset(TimeSpan.FromMinutes(-90))))
				.Single();

			shifted.UtcDateTime.ShouldBe(Value.UtcDateTime);
			shifted.Offset.ShouldBe(TimeSpan.FromMinutes(-90));
		}

		/// <summary>
		/// A component read <em>through</em> <c>ToOffset</c>. The frame is then a fixed offset rather than a named
		/// zone, which no dialect accepts where a zone name goes, so the member is left to .NET and still answers.
		/// </summary>
		[Test]
		public void ComponentThroughAFixedOffsetFallsBackToNet([IncludeDataSources(false, ZonedProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var hour = table
				.Select(r => r.Dto.ToOffset(TimeSpan.FromMinutes(-90)).Hour)
				.Single();

			hour.ShouldBe(stored.ToOffset(TimeSpan.FromMinutes(-90)).Hour);
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

			var stored = table
				.Select(r => r.Dto)
				.Single();

			table
				.Select(r => Sql.AsSql(r.Dto.Offset))
				.Single()
				.ShouldBe(stored.Offset);
#if NET8_0_OR_GREATER
			table
				.Select(r => Sql.AsSql(r.Dto.TotalOffsetMinutes))
				.Single()
				.ShouldBe(stored.TotalOffsetMinutes);
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

			var stored = table
				.Select(r => r.Dto)
				.Single();

#if NET8_0_OR_GREATER
			table
				.Select(r => Sql.AsSql(r.Dto.TotalOffsetMinutes))
				.Single()
				.ShouldBe(stored.TotalOffsetMinutes);
#endif
			table
				.Select(r => Sql.AsSql(r.Dto.Offset))
				.Single()
				.ShouldBe(stored.Offset);
		}

		/// <summary>
		/// A filter on the offset against a captured <see cref="TimeSpan"/>: the offset is a tick count on the
		/// server, so the captured value has to be sent as one.
		/// </summary>
		[Test]
		public void OffsetComparesWithACapturedTimeSpan([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllOracle)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var offset = Value.Offset;

			var captured = table.Count(r => r.Dto.Offset == offset);
			var constant = table.Count(r => r.Dto.Offset == TimeSpan.Zero);

			captured.ShouldBe(1);
			constant.ShouldBe(0);
		}

		/// <summary>
		/// The offset as a duration in its own right, through the members and operators that take one.
		/// </summary>
		[Test]
		public void OffsetIsADuration([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllOracle)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var offset = stored.Offset + TimeSpan.FromMinutes(20);

			var actual = table
				.Select(r => new
				{
					Minutes = Sql.AsSql(r.Dto.Offset.TotalMinutes),
					Negated = Sql.AsSql(-r.Dto.Offset),
				})
				.Single();

			var sum = table.Count(r => r.Dto.Offset + TimeSpan.FromMinutes(20) == offset);

			actual.Minutes.ShouldBe(stored.Offset.TotalMinutes);
			actual.Negated.ShouldBe(-stored.Offset);
			sum.ShouldBe(1);
		}

		/// <summary>
		/// The offset compared after it has been carried somewhere first - a subquery column, a grouped aggregate, the
		/// nullable side of an outer join - where it no longer arrives as the member it was read from.
		/// </summary>
		[Test]
		public void OffsetComparesThroughWhatCarriesIt([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllOracle)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var offset = Value.Offset;

			var throughSubquery = table
				.Select(r => new { r.Id, Offset = r.Dto.Offset })
				.AsSubQuery()
				.Count(x => x.Offset == offset);

			var throughAggregate = table
				.GroupBy(r => r.Id)
				.Count(g => g.Max(r => r.Dto.Offset) == offset);

			var outerJoin =
				from a in table
				join b in table on a.Id + 1 equals b.Id into next
				from b in next.DefaultIfEmpty()
				where (b == null ? a.Dto.Offset : b.Dto.Offset) == offset
				select a;

			var throughOuterJoin = outerJoin.Count();

			throughSubquery.ShouldBe(1);
			throughAggregate.ShouldBe(1);
			throughOuterJoin.ShouldBe(1);
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
			var stored = table
				.Select(r => r.Dto)
				.Single();

			var built = table
				.Select(r => Sql.AsSql(new DateTimeOffset(r.Dto.DateTime, TimeSpan.FromMinutes(-90))))
				.Single();

			built.ShouldBe(new DateTimeOffset(stored.DateTime, TimeSpan.FromMinutes(-90)));
			built.Offset.ShouldBe(TimeSpan.FromMinutes(-90));
		}

		/// <summary>
		/// The same constructor over a column whose storage type is not a timestamp. A plain projection - no
		/// <c>AsSql</c> - because before the constructor was translated this shape evaluated in .NET and answered.
		/// </summary>
		[Test]
		public void ConstructorOverADateColumn([IncludeDataSources(false, ZonedProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new DateRow { Id = 1, Dt = new DateTime(2020, 6, 15, 12, 0, 0) } });

			var stored = table
				.Select(r => r.Dt)
				.Single();
			var built  = table
				.Select(r => new DateTimeOffset(r.Dt, TimeSpan.FromMinutes(-90)))
				.Single();

			built.ShouldBe(new DateTimeOffset(stored, TimeSpan.FromMinutes(-90)));
			built.Offset.ShouldBe(TimeSpan.FromMinutes(-90));
		}

		/// <summary>
		/// The constructor in a plain projection on every provider: translated where the offset survives, left to
		/// .NET where it does not - either way the value .NET builds.
		/// </summary>
		[Test]
		public void ConstructorInAProjectionAgreesWithNet([SupportsDateTimeOffsetContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			// From the stored value's wall clock, whose kind is always unspecified: a DateTime column read back as UTC,
			// as ClickHouse and YDB do, cannot be given another offset in .NET at all.
			var stored = table
				.Select(r => r.Dto)
				.Single();

			var built = table
				.Select(r => new DateTimeOffset(r.Dto.DateTime, TimeSpan.FromHours(2)))
				.Single();

			built.ShouldBe(new DateTimeOffset(stored.DateTime, TimeSpan.FromHours(2)));
			built.Offset.ShouldBe(TimeSpan.FromHours(2));
		}

		/// <summary>
		/// A constructor over captured values only is a value, built on the client and compared as a parameter.
		/// </summary>
		[Test]
		[ActiveIssue(1855, Configurations = [TestProvName.AllSQLiteClassic], ErrorType = typeof(ShouldAssertException), Details = "strftime over the stored text and over the parameter compare unequal for the same value - the DateTimeOffset comparison Issue1855Test records.")]
		public void ConstructorOverCapturedValuesIsAParameter([SupportsDateTimeOffsetContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			// The stored value's own wall clock and offset: a provider that compares the stored text, as SQLite.Classic
			// does, then sees the same spelling.
			var stored = table
				.Select(r => r.Dto)
				.Single();

			var local  = stored.DateTime;
			var offset = stored.Offset;

			var count = table.Count(r => r.Dto == new DateTimeOffset(local, offset));

			count.ShouldBe(1);
		}

		/// <summary>
		/// A wall clock in a zone from a column whose storage type is not a timestamp.
		/// </summary>
		[Test]
		public void AtTimeZoneOverADateColumn([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new DateRow { Id = 1, Dt = new DateTime(2020, 6, 15, 12, 0, 0) } });

			var stored = table
				.Select(r => r.Dt)
				.Single();

			var actual = table
				.Select(r => new
				{
					Zoned  = Sql.AsSql(Sql.AtTimeZone(r.Dt, "Europe/Prague")),
					Day    = Sql.AsSql(Sql.AtTimeZone(r.Dt, "Europe/Prague")!.Value.AddDays(1)),
					Month  = Sql.AsSql(Sql.AtTimeZone(r.Dt, "Europe/Prague")!.Value.AddMonths(1)),
				})
				.Single();

			var expected = new DateTimeOffset(stored, TimeSpan.FromHours(2));

			actual.Zoned.ShouldBe(expected);
			actual.Day.ShouldBe(expected.AddDays(1));
			actual.Month.ShouldBe(expected.AddMonths(1));

			actual.Zoned!.Value.Offset.ShouldBe(expected.Offset);
			actual.Day.Offset.ShouldBe(expected.Offset);
			actual.Month.Offset.ShouldBe(expected.Offset);
		}

		/// <summary>
		/// The offset cannot be a bind, so it is spelled into the SQL - which makes it part of what the query cache
		/// must key on. Two offsets over one query shape is exactly the case that would break if it were not.
		/// </summary>
		[Test]
		public void TwoOffsetsDoNotShareACachedQuery([IncludeDataSources(true, ZonedProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			// One local, re-assigned. Two separate locals would give the two lambdas different closure fields, so
			// they could not share a cache entry whatever the mechanism did - and the test would pass without
			// having exercised it.
			var offset = TimeSpan.FromHours(2);

			table
				.Select(r => Sql.AsSql(r.Dto.ToOffset(offset)))
				.Single()
				.Offset
				.ShouldBe(offset);

			offset = TimeSpan.FromMinutes(-90);

			table
				.Select(r => Sql.AsSql(r.Dto.ToOffset(offset)))
				.Single()
				.Offset
				.ShouldBe(offset);
		}

		/// <summary>
		/// An offset a <see cref="DateTimeOffset"/> cannot carry - not whole minutes, or more than 14 hours - is
		/// refused by name, as .NET refuses it, instead of being rounded into one the server accepts. A captured offset
		/// is checked on every execution, since its SQL is rebuilt for each value.
		/// </summary>
		[Test]
		public void OffsetADateTimeOffsetCannotCarryIsRefused([IncludeDataSources(true, ZonedProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			Assert.That(
				() => table
					.Select(r => Sql.AsSql(r.Dto.ToOffset(TimeSpan.FromSeconds(90))))
					.Single(),
				Throws.InstanceOf<LinqToDBException>().With.Message.Contains(InvalidOffsetMessage(TimeSpan.FromSeconds(90))));

			Assert.That(
				() => table
					.Select(r => Sql.AsSql(new DateTimeOffset(r.Dto.DateTime, TimeSpan.FromSeconds(90))))
					.Single(),
				Throws.InstanceOf<LinqToDBException>().With.Message.Contains(InvalidOffsetMessage(TimeSpan.FromSeconds(90))));

			Assert.That(
				() => table
					.Select(r => Sql.AsSql(r.Dto.ToOffset(TimeSpan.FromHours(15))))
					.Single(),
				Throws.InstanceOf<LinqToDBException>().With.Message.Contains(InvalidOffsetMessage(TimeSpan.FromHours(15))));

			// One local, re-assigned, so the three executions share one cached query.
			var offset = TimeSpan.FromMinutes(-90);

			DateTimeOffset Shift() => table
				.Select(r => Sql.AsSql(r.Dto.ToOffset(offset)))
				.Single();

			var first = Shift();

			first.Offset.ShouldBe(offset);

			offset = TimeSpan.FromSeconds(90);

			Assert.That(Shift, Throws.InstanceOf<LinqToDBException>().With.Message.Contains(InvalidOffsetMessage(offset)));

			offset = TimeSpan.FromMinutes(-90);

			var again = Shift();

			again.Offset.ShouldBe(offset);
		}

		static string InvalidOffsetMessage(TimeSpan offset)
			=> string.Format(CultureInfo.InvariantCulture, ErrorHelper.Error_TimeZone_InvalidOffset, offset);

		/// <summary>
		/// A zone conversion over <see cref="DateTimeOffset.UtcNow"/>, whose own SQL already names a zone on some
		/// providers: the two must nest into valid SQL.
		/// </summary>
		[Test]
		public void UtcNowReadsThroughAZoneConversion([IncludeDataSources(true, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var forced = table
				.Select(r => Sql.AsSql(DateTimeOffset.UtcNow.UtcDateTime))
				.Single();

			var projected = table
				.Select(r => new { Now = DateTimeOffset.UtcNow.UtcDateTime })
				.Single();

			var forcedDrift    = forced        - DateTime.UtcNow;
			var projectedDrift = projected.Now - DateTime.UtcNow;

			forcedDrift.ShouldBeInRange(TimeSpan.FromMinutes(-5), TimeSpan.FromMinutes(5));
			projectedDrift.ShouldBeInRange(TimeSpan.FromMinutes(-5), TimeSpan.FromMinutes(5));

			// Only a provider whose type carries an offset hands one back; the others answer every value at +00:00.
			if (context.IsAnyOf(ZonedProviders))
			{
				var shifted = table
					.Select(r => Sql.AsSql(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(2))))
					.Single();

				var shiftedDrift = shifted.UtcDateTime - DateTime.UtcNow;

				shifted.Offset.ShouldBe(TimeSpan.FromHours(2));
				shiftedDrift.ShouldBeInRange(TimeSpan.FromMinutes(-5), TimeSpan.FromMinutes(5));
			}
		}

		/// <summary>
		/// The acceptance rule for the two instant-reading members on <em>every</em> provider, including those that
		/// decline to translate them.
		/// </summary>
		[Test]
		public void InstantMembersAgreeWithTheRoundTrip([SupportsDateTimeOffsetContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var utc = table
				.Select(r => r.Dto.ToUniversalTime())
				.Single();

			var shifted = table
				.Select(r => r.Dto.ToOffset(TimeSpan.FromMinutes(-90)))
				.Single();

			// Equality compares instants, which both members keep by definition - the offset is what they change.
			utc.ShouldBe(stored.ToUniversalTime());
			utc.Offset.ShouldBe(TimeSpan.Zero);
			shifted.ShouldBe(stored.ToOffset(TimeSpan.FromMinutes(-90)));
			shifted.Offset.ShouldBe(TimeSpan.FromMinutes(-90));
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

			var stored = table
				.Select(r => r.Dto)
				.Single();

			table
				.Select(r => r.Dto.DateTime)
				.Single()
				.ShouldBe(stored.DateTime);

			// UtcDateTime names its own zone, so it is the one member whose answer does not depend on the round-trip.
			table
				.Select(r => r.Dto.UtcDateTime)
				.Single()
				.ShouldBe(Value.UtcDateTime);
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

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var localDateTime = table
				.Select(r => r.Dto.LocalDateTime)
				.Single();

			var localTime = table
				.Select(r => r.Dto.ToLocalTime())
				.Single();

			localDateTime.ShouldBe(stored.LocalDateTime);
			localTime.ShouldBe(stored.ToLocalTime());
			localTime.Offset.ShouldBe(stored.ToLocalTime().Offset);
		}

		/// <summary>
		/// The date and time-of-day halves at a day boundary, where reading them in the wrong frame changes the day.
		/// </summary>
		/// <remarks>
		/// On MySQL and YDB the kind is asserted as well: their date was read back through local time, which is right
		/// on a UTC host and off by the host's offset everywhere else, so the value alone passes on CI. SQL Server
		/// reads the right value back as a local one, as it always has.
		/// </remarks>
		[Test]
		public void DateAndTimeOfDayAtADayBoundaryAgreeWithTheRoundTrip([SupportsDateTimeOffsetContext] string context)
		{
			var value = new DateTimeOffset(2021, 1, 1, 0, 20, 0, TimeSpan.FromMinutes(40));

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(value));

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var actual = table
				.Select(r => new { r.Dto.Date, r.Dto.TimeOfDay })
				.Single();

			actual.Date.ShouldBe(stored.Date);
			actual.TimeOfDay.ShouldBe(stored.TimeOfDay);

			if (context.IsAnyOf(TestProvName.AllMySql, TestProvName.AllYdb))
				actual.Date.Kind.ShouldNotBe(DateTimeKind.Local);
		}

		/// <summary>
		/// The same where SQL is required - a forced projection and a grouping key. SQLite reads a stored offset as a
		/// shift to UTC, so it leaves both members to .NET and refuses here, as it always did.
		/// </summary>
		[Test]
		[ThrowsCannotBeConverted(TestProvName.AllSQLite)]
		public void DateAtADayBoundaryOnTheServer([SupportsDateTimeOffsetContext] string context)
		{
			var value = new DateTimeOffset(2021, 1, 1, 0, 20, 0, TimeSpan.FromMinutes(40));

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(value));

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var date = table
				.Select(r => Sql.AsSql(r.Dto.Date))
				.Single();

			var key = table
				.GroupBy(r => r.Dto.Date)
				.Select(g => g.Key)
				.Single();

			date.ShouldBe(stored.Date);
			key.ShouldBe(stored.Date);

			if (context.IsAnyOf(TestProvName.AllMySql, TestProvName.AllYdb))
			{
				date.Kind.ShouldNotBe(DateTimeKind.Local);
				key.Kind.ShouldNotBe(DateTimeKind.Local);
			}
		}

		/// <summary>
		/// The time-of-day half where SQL is required. SQLite leaves a stored offset's to .NET and refuses here, and
		/// SQL Server 2005 has no <c>time</c> type to answer it with.
		/// </summary>
		[Test]
		[ThrowsCannotBeConverted(TestProvName.AllSQLite, TestProvName.AllSqlServer2005)]
		public void TimeOfDayAtADayBoundaryOnTheServer([SupportsDateTimeOffsetContext] string context)
		{
			var value = new DateTimeOffset(2021, 1, 1, 0, 20, 0, TimeSpan.FromMinutes(40));

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(value));

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var timeOfDay = table
				.Select(r => Sql.AsSql(r.Dto.TimeOfDay))
				.Single();

			timeOfDay.ShouldBe(stored.TimeOfDay);
		}

		/// <summary>
		/// A filter on the date: the round-tripped value's midnight matches, an hour past it does not.
		/// </summary>
		[Test]
		[ThrowsCannotBeConverted(TestProvName.AllSQLite)]
		public void DateFilterAtADayBoundary([SupportsDateTimeOffsetContext] string context)
		{
			var value = new DateTimeOffset(2021, 1, 1, 0, 20, 0, TimeSpan.FromMinutes(40));

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(value));

			var stored = table
				.Select(r => r.Dto)
				.Single();

			var date  = stored.Date;
			var later = date.AddHours(1);

			var atMidnight = table.Count(r => r.Dto.Date == date);
			var hourLater  = table.Count(r => r.Dto.Date == later);

			atMidnight.ShouldBe(1);
			hourLater.ShouldBe(0);
		}

		/// <summary>
		/// The same filter on the date of a value read in a named zone, whose frame is a wall clock rather than the
		/// stored value - a different operand for the truncation to type its result from.
		/// </summary>
		[Test]
		public void DateFilterInANamedZone([IncludeDataSources(ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			// 11:20 UTC is 13:20 on 15 June in Prague.
			var date  = new DateTime(2020, 6, 15);
			var later = date.AddHours(1);

			var atMidnight = table.Count(r => Sql.AtTimeZone(r.Dto, zone)!.Value.Date == date);
			var hourLater  = table.Count(r => Sql.AtTimeZone(r.Dto, zone)!.Value.Date == later);
			var shifted    = table.Count(r => Sql.AtTimeZone(r.Dto, zone)!.Value.AddDays(1).Date == date.AddDays(1));
			var shiftedOff = table.Count(r => Sql.AtTimeZone(r.Dto, zone)!.Value.AddDays(1).Date == later.AddDays(1));

			atMidnight.ShouldBe(1);
			hourLater.ShouldBe(0);
			shifted.ShouldBe(1);
			shiftedOff.ShouldBe(0);
		}

		/// <summary>
		/// A wall clock in a zone as a filter, sort and update operand on every provider that reads zones: the
		/// instant is right everywhere, whatever offset the provider can carry.
		/// </summary>
		[Test]
		public void WallClockInAZoneAsAnOperand([IncludeDataSources(ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value, Value.AddHours(5)));

			var earlier = table.Count(r => Sql.AtTimeZone(r.Dto.DateTime, zone) < r.Dto);

			var order = table
				.OrderByDescending(r => Sql.AtTimeZone(r.Dto.DateTime, zone))
				.Select(r => r.Id)
				.ToList();

			earlier.ShouldBe(2);
			order.ShouldBe(new[] { 2, 1 });

			var stored = table
				.Where(r => r.Id == 1)
				.Select(r => r.Dto)
				.Single();

			table
				.Where(r => r.Id == 1)
				.Set(r => r.Dto, r => Sql.AtTimeZone(r.Dto.DateTime, zone)!.Value)
				.Update();

			var updated = table
				.Where(r => r.Id == 1)
				.Select(r => r.Dto)
				.Single();

			updated.UtcDateTime.ShouldBe(new DateTimeOffset(stored.DateTime, TimeSpan.FromHours(2)).UtcDateTime);
		}

		/// <summary>
		/// Captured arguments are not folded on the client - there is no .NET answer - so they reach SQL as
		/// parameters, and must be typed as themselves rather than through the column they are compared with.
		/// </summary>
		[Test]
		public void CapturedWallClockInAZone([IncludeDataSources(ZoneReadingProviders)] string context)
		{
			var zone  = PragueZone(context);
			var local = new DateTime(2020, 6, 15, 12, 0, 0);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var later = table.Count(r => r.Dto > Sql.AtTimeZone(local, zone));

			// 12:00 in Prague is 10:00 UTC, before the row's 11:20.
			later.ShouldBe(1);
		}

		/// <summary>
		/// The same for an instant re-expressed in a zone, which needs a type that carries an offset.
		/// </summary>
		[Test]
		[ThrowsCannotBeConverted(OffsetlessProviders)]
		public void CapturedInstantInAZone([IncludeDataSources(ZoneReadingProviders)] string context)
		{
			var zone    = PragueZone(context);
			var instant = new DateTimeOffset(2020, 6, 15, 9, 0, 0, TimeSpan.Zero);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var later = table.Count(r => r.Dto > Sql.AtTimeZone(instant, zone));

			later.ShouldBe(1);
		}

		/// <summary>
		/// A component and a shift of a captured wall clock in a zone. The frame is the bare parameter.
		/// </summary>
		[Test]
		[ActiveIssue(6484, Configuration = TestProvName.AllDuckDB, ErrorTypeName = "DuckDB.NET.Data.DuckDBException", ErrorMessage = "Binder Error")]
		public void CapturedWallClockInAZoneThroughTheFrame([IncludeDataSources(ZoneReadingProviders)] string context)
		{
			var zone   = PragueZone(context);
			var local  = new DateTime(2020, 6, 15, 12, 0, 0);
			var months = 6;

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			var expected = new DateTimeOffset(local, TimeSpan.FromHours(2))
				.AddMonths(6);

			var hour = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(local, zone)!.Value.Hour))
				.Single();

			var byLiteral = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(local, zone)!.Value.AddMonths(6)))
				.Single();

			var byCaptured = table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(local, zone)!.Value.AddMonths(months)))
				.Single();

			hour.ShouldBe(12);
			byLiteral.UtcDateTime.ShouldBe(expected.UtcDateTime);
			byCaptured.UtcDateTime.ShouldBe(expected.UtcDateTime);
		}

		/// <summary>
		/// A component of a wall clock, captured or stored, needs no zone support at all: reading it back in the zone
		/// it was declared in is the identity.
		/// </summary>
		[Test]
		public void WallClockComponentWithoutZoneSupport([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var local = new DateTime(2020, 6, 15, 12, 0, 0);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new DateRow { Id = 1, Dt = local } });

			var actual = table
				.Select(r => new
				{
					Hour            = Sql.AsSql(Sql.AtTimeZone(local, "Europe/Prague")!.Value.Hour),
					Date            = Sql.AsSql(Sql.AtTimeZone(local, "Europe/Prague")!.Value.Date),
					TimeOfDay       = Sql.AsSql(Sql.AtTimeZone(local, "Europe/Prague")!.Value.TimeOfDay),
					ColumnHour      = Sql.AsSql(Sql.AtTimeZone(r.Dt, "Europe/Prague")!.Value.Hour),
					ColumnDate      = Sql.AsSql(Sql.AtTimeZone(r.Dt, "Europe/Prague")!.Value.Date),
					ColumnTimeOfDay = Sql.AsSql(Sql.AtTimeZone(r.Dt, "Europe/Prague")!.Value.TimeOfDay),
				})
				.Single();

			actual.Hour.ShouldBe(12);
			actual.Date.ShouldBe(local.Date);
			actual.TimeOfDay.ShouldBe(local.TimeOfDay);
			actual.ColumnHour.ShouldBe(12);
			actual.ColumnDate.ShouldBe(local.Date);
			actual.ColumnTimeOfDay.ShouldBe(local.TimeOfDay);
		}

		/// <summary>
		/// The seven-argument constructor shipped before the zone capabilities were added keeps binding: compiled
		/// code calls it by its parameter types.
		/// </summary>
		[Test]
		public void TranslationProviderFlagsKeepsItsShippedConstructor()
		{
			var ctor = typeof(TranslationProviderFlags).GetConstructor(
			[
				typeof(NullsDefaultOrdering), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(SqlIntervalUnit), typeof(bool),
			]);

			ctor.ShouldNotBeNull();

			var flags = (TranslationProviderFlags)ctor.Invoke([NullsDefaultOrdering.Smallest, true, true, false, true, SqlIntervalUnit.Millisecond, false]);

			flags.CanLowerIntervalDifference.ShouldBeTrue();
			flags.CanLowerIntervalPart.ShouldBeFalse();
			flags.IntervalResolution.ShouldBe(SqlIntervalUnit.Millisecond);
			flags.CanMeasureDifferenceInTicks.ShouldBeFalse();
			var attach    = flags.CanLowerTimeZoneConversion(SqlTimeZoneConversionKind.AttachZone);
			var convert   = flags.CanLowerTimeZoneConversion(SqlTimeZoneConversionKind.ConvertZone);
			var wallClock = flags.CanLowerTimeZoneConversion(SqlTimeZoneConversionKind.ToWallTime);

			attach.ShouldBeFalse();
			convert.ShouldBeFalse();
			wallClock.ShouldBeFalse();
			flags.RequiresConstantTimeZone.ShouldBeFalse();

			// Each arity has to bind to exactly one constructor, or this does not compile.
			_ = new TranslationProviderFlags(NullsDefaultOrdering.Smallest, true, false, false, false);
			_ = new TranslationProviderFlags(NullsDefaultOrdering.Smallest, true, false, false, false, SqlIntervalUnit.Tick);
			_ = new TranslationProviderFlags(NullsDefaultOrdering.Smallest, true, false, false, false, SqlIntervalUnit.Tick, true);
		}

		/// <summary>
		/// The third acceptance outcome: where the value cannot leave the server and the provider genuinely cannot
		/// express the operation, the refusal is by name rather than a wrong number or a generic failure.
		/// </summary>
		/// <remarks>
		/// <c>Sql.AtTimeZone</c> has no .NET evaluation, so a plain projection refuses too
		/// (<see cref="AtTimeZoneInAProjectionWhereNoTypeCarriesAnOffset"/>); <see cref="Sql.AsSql{T}(T)"/> is the
		/// position that keeps the reason in the message.
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

			table
				.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)))
				.Single();
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

			table
				.Count(r => r.Dto.ToOffset(TimeSpan.FromHours(2)) == Value)
				.ShouldBe(1);
		}

		/// <summary>
		/// Reading a component through a fixed offset is refused by name where SQL is required. The offset would have
		/// to go where a zone name goes, and neither dialect that carries an offset accepts one there for this
		/// direction - so the alternative is not a wrong number but SQL no server parses.
		/// </summary>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), ZonedProviders, ErrorMessage = ErrorHelper.Error_TimeZone_OffsetFrame)]
		public void ComponentThroughAFixedOffsetRefusedWhereSqlIsRequired([IncludeDataSources(false, ZonedProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Rows(Value));

			table
				.Select(r => Sql.AsSql(r.Dto.ToOffset(TimeSpan.FromMinutes(-90)).Hour))
				.Single();
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

			table
				.Count(r => r.Dto.DateTime == Value.DateTime)
				.ShouldBe(1);
		}
	}
}
