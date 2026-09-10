using System;
using System.Linq;

using LinqToDB;
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
			[Column] public int            Id  { get; set; }
			[Column] public DateTimeOffset Dto { get; set; }
		}

		// 12:00 +00:40 is 11:20 UTC. The forty-minute offset is deliberate and is the whole point of the fixture:
		// no real zone uses it, so an answer that quietly reads in the server's zone cannot coincide with the right
		// one. A row whose offset matches the container's would let every one of these tests pass while broken.
		static readonly DateTimeOffset Value = new (2020, 6, 15, 12, 00, 00, TimeSpan.FromMinutes(40));

		// SQL Server resolves zone names against its own registry-backed list; every other provider takes IANA.
		// There is no spelling that works everywhere - SQL Server rejects a bare UTC offset - so the identifier is
		// picked per provider, the way GetNepalTzId already does in DateTimeOffsetTests.
		static string PragueZone(string context)
			=> context.IsAnyOf(TestProvName.AllSqlServer) ? "Central European Standard Time" : "Europe/Prague";

		// Providers that can carry an offset in a column AND express a conversion to a named zone.
		const string ZonedProviders = TestProvName.AllSqlServer2016Plus + "," + TestProvName.AllOracleManaged;

		// Adds those that cannot carry an offset but can still answer a reading in a named zone.
		const string ZoneReadingProviders = ZonedProviders + "," + TestProvName.AllPostgreSQL + "," + TestProvName.AllDuckDB;

		[Test]
		public void AtTimeZoneKeepsInstantAndTakesTargetOffset([IncludeDataSources(false, ZonedProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = Value } });

			var result = table.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, PragueZone(context)))).Single();

			// Both halves are asserted separately on purpose: DateTimeOffset equality compares instants, so a wrong
			// offset passes silently unless the test names it.
			result!.Value.UtcDateTime.ShouldBe(Value.UtcDateTime);
			result!.Value.Offset.ShouldBe(TimeSpan.FromHours(2));
		}

		[Test]
		public void ComponentInNamedZone([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			var zone = PragueZone(context);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = Value } });

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
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = Value } });

			var viaBcl = table.Select(r => Sql.AsSql(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(r.Dto, zone).Hour)).Single();
			var viaSql = table.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value.Hour)).Single();

			viaBcl.ShouldBe(viaSql);
			viaBcl.ShouldBe(13);
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
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = Value } });

			var hour = table.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value.AddHours(1).Hour)).Single();

			hour.ShouldBe(14);
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
		public void ComponentMatchesRoundTrippedValue([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = Value } });

			var expected = table.Select(r => r.Dto).Single().Hour;
			var actual   = table.Select(r => Sql.AsSql(r.Dto.Hour)).Single();

			actual.ShouldBe(expected);
		}

		/// <summary>
		/// <c>UtcDateTime</c> names its own zone, so unlike a component it must answer the same thing on every
		/// provider - the expectation is the source value's, not the round-trip's.
		/// </summary>
		[Test]
		public void UtcDateTimeIsProviderIndependent([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = Value } });

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
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = Value } });

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
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = Value } });

			var expected = table.Select(r => r.Dto).Single().DateTime;
			var actual   = table.Select(r => Sql.AsSql(r.Dto.DateTime)).Single();

			actual.ShouldBe(expected);
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
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = Value } });

			var hour = table.Select(r => Sql.AsSql(Sql.AtTimeZone(r.Dto, zone)!.Value.DateTime.Hour)).Single();

			// 11:20 UTC is 13:20 in Prague in June - the same answer ComponentInNamedZone asserts, reached through
			// an extra member that must contribute no second conversion.
			hour.ShouldBe(13);
		}

		/// <summary>
		/// <c>ToUniversalTime</c> keeps the instant and zeroes the offset. On a provider that already hands every
		/// value back at <c>+00:00</c> it is the identity, so the same assertion holds either way.
		/// </summary>
		[Test]
		public void ToUniversalTimeZeroesTheOffset([IncludeDataSources(false, ZoneReadingProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = Value } });

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
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = Value } });

			// Negative and not a whole hour: the sign and the minutes are both part of the spelling the provider is
			// handed, and a whole positive hour would let either get lost.
			var shifted = table.Select(r => Sql.AsSql(r.Dto.ToOffset(TimeSpan.FromMinutes(-90)))).Single();

			shifted.UtcDateTime.ShouldBe(Value.UtcDateTime);
			shifted.Offset.ShouldBe(TimeSpan.FromMinutes(-90));
		}

		/// <summary>
		/// The offset cannot be a bind, so it is spelled into the SQL - which makes it part of what the query cache
		/// must key on. Two offsets over one query shape is exactly the case that would break if it were not.
		/// </summary>
		[Test]
		public void TwoOffsetsDoNotShareACachedQuery([IncludeDataSources(false, ZonedProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = Value } });

			var first  = TimeSpan.FromHours(2);
			var second = TimeSpan.FromMinutes(-90);

			table.Select(r => Sql.AsSql(r.Dto.ToOffset(first))).Single().Offset.ShouldBe(first);
			table.Select(r => Sql.AsSql(r.Dto.ToOffset(second))).Single().Offset.ShouldBe(second);
		}

		/// <summary>
		/// The acceptance rule for the two instant-reading members on <em>every</em> provider, including those that
		/// decline to translate them.
		/// </summary>
		[Test]
		public void InstantMembersAgreeWithTheRoundTrip([SupportsDateTimeOffsetContext] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = Value } });

			var stored = table.Select(r => r.Dto).Single();

			table.Select(r => r.Dto.ToUniversalTime()).Single().ShouldBe(stored.ToUniversalTime());
			table.Select(r => r.Dto.ToOffset(TimeSpan.FromMinutes(-90))).Single().ShouldBe(stored.ToOffset(TimeSpan.FromMinutes(-90)));
		}

		/// <summary>
		/// The acceptance rule for the two new members on <em>every</em> provider, including those that decline to
		/// translate them: the answer is either the server's or .NET's, and both must equal what the CLR computes on
		/// the value as that provider hands it back.
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
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = Value } });

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
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = Value } });

			var stored = table.Select(r => r.Dto).Single();

			table.Select(r => r.Dto.LocalDateTime).Single().ShouldBe(stored.LocalDateTime);
			table.Select(r => r.Dto.ToLocalTime()).Single().ShouldBe(stored.ToLocalTime());
		}

		/// <summary>
		/// Oracle computes <c>TIMESTAMP WITH TIME ZONE</c> arithmetic in UTC, where this input lands on 31 February
		/// and raises ORA-01839. .NET adds to the local reading and keeps the offset.
		/// </summary>
		[Test]
		public void AddMonthsAddsToTheLocalReading([IncludeDataSources(false, ZonedProviders)] string context)
		{
			var value = new DateTimeOffset(2020, 2, 1, 0, 20, 0, TimeSpan.FromMinutes(40));

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = value } });

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
			using var table = db.CreateLocalTable(new[] { new ZonedRow { Id = 1, Dto = start }, new ZonedRow { Id = 2, Dto = end } });

			var hours = table
				.Where(r => r.Id == 1)
				.Select(a => Sql.AsSql((table.Where(b => b.Id == 2).Select(b => b.Dto).Single() - a.Dto).TotalHours))
				.Single();

			hours.ShouldBe(0d);
		}
	}
}
