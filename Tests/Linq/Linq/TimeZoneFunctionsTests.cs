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
		const string ZoneReadingProviders = ZonedProviders + "," + TestProvName.AllPostgreSQL;

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
