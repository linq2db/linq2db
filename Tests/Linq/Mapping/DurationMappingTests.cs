using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Mapping
{
	[TestFixture]
	public class DurationMappingTests : TestBase
	{
		[Table]
		sealed class Attributed
		{
			[Column] public int Id { get; set; }

			// Two columns of the very same storage type holding durations in different units. Nothing about
			// DataType.Int64 says which, so the unit has to come from the mapping - this is the case a
			// translator that hardcodes "Int64 means ticks" gets silently wrong by a factor of 10000000.
			[Column(DataType = DataType.Int64), Duration(DurationUnit.Second)]
			public TimeSpan Seconds { get; set; }

			[Column(DataType = DataType.Int64), Duration(DurationUnit.Tick)]
			public TimeSpan Ticks { get; set; }

			[Column(DataType = DataType.Int64)]
			public TimeSpan Undeclared { get; set; }

			[Column, Duration(DurationUnit.Millisecond)]
			public TimeSpan? NullableMilliseconds { get; set; }
		}

		[Table]
		sealed class Fluent
		{
			[Column] public int      Id      { get; set; }
			[Column] public TimeSpan Elapsed { get; set; }
			[Column] public TimeSpan Other   { get; set; }
		}

		static ColumnDescriptor Column(MappingSchema ms, Type type, string memberName)
		{
			return ms.GetEntityDescriptor(type).Columns.Single(c => c.MemberName == memberName);
		}

		[Test]
		public void AttributeDeclaresUnit()
		{
			var ms = new MappingSchema();

			Column(ms, typeof(Attributed), nameof(Attributed.Seconds)).DurationUnit.ShouldBe(DurationUnit.Second);
			Column(ms, typeof(Attributed), nameof(Attributed.Ticks)).DurationUnit.ShouldBe(DurationUnit.Tick);
		}

		[Test]
		public void DistinctUnitsSurviveOnTheSameStorageType()
		{
			var ms      = new MappingSchema();
			var seconds = Column(ms, typeof(Attributed), nameof(Attributed.Seconds));
			var ticks   = Column(ms, typeof(Attributed), nameof(Attributed.Ticks));

			seconds.DataType.ShouldBe(DataType.Int64);
			ticks.DataType.ShouldBe(DataType.Int64);

			seconds.DurationUnit.ShouldNotBe(ticks.DurationUnit);
		}

		[Test]
		public void UndeclaredColumnKeepsExistingMeaning()
		{
			// The compatibility rule: a TimeSpan column that was never declared as a duration must not acquire
			// duration semantics, or every existing TIME/time-of-day mapping would be reinterpreted.
			Column(new MappingSchema(), typeof(Attributed), nameof(Attributed.Undeclared)).DurationUnit.ShouldBeNull();
		}

		[Test]
		public void NullableColumnCarriesUnit()
		{
			Column(new MappingSchema(), typeof(Attributed), nameof(Attributed.NullableMilliseconds))
				.DurationUnit.ShouldBe(DurationUnit.Millisecond);
		}

		[Test]
		public void FluentDeclaresUnit()
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<Fluent>()
					.Property(e => e.Elapsed).HasDataType(DataType.Int64).HasDuration(DurationUnit.Hour)
				.Build();

			Column(ms, typeof(Fluent), nameof(Fluent.Elapsed)).DurationUnit.ShouldBe(DurationUnit.Hour);
			Column(ms, typeof(Fluent), nameof(Fluent.Other)).DurationUnit.ShouldBeNull();
		}

		[Test]
		public void DeclarationIsScopedToItsMappingSchema()
		{
			var declared = new MappingSchema();

			new FluentMappingBuilder(declared)
				.Entity<Fluent>()
					.Property(e => e.Elapsed).HasDuration(DurationUnit.Day)
				.Build();

			Column(declared, typeof(Fluent), nameof(Fluent.Elapsed)).DurationUnit.ShouldBe(DurationUnit.Day);
			Column(new MappingSchema(), typeof(Fluent), nameof(Fluent.Elapsed)).DurationUnit.ShouldBeNull();
		}

		[Test]
		public void ForDurationCoversEveryUnit([Values] DurationUnit unit)
		{
			var type = SqlIntervalType.ForDuration(unit);

			type.Domain.ShouldBe(SqlIntervalDomain.Duration);
			type.IsSigned.ShouldBeTrue();
			type.Resolution.ShouldBe(SqlIntervalType.ToIntervalUnit(unit));
		}

		[Test]
		public void ClrTimeSpanIsSignedTickResolutionDuration()
		{
			SqlIntervalType.ClrTimeSpan.ShouldBe(
				new SqlIntervalType(SqlIntervalDomain.Duration, SqlIntervalUnit.Tick, true));
		}

		[TestCase(SqlIntervalUnit.Nanosecond,  1L,        100L)]
		[TestCase(SqlIntervalUnit.Tick,        1L,          1L)]
		[TestCase(SqlIntervalUnit.Microsecond, 10L,         1L)]
		[TestCase(SqlIntervalUnit.Millisecond, 10_000L,     1L)]
		[TestCase(SqlIntervalUnit.Second,      10_000_000L, 1L)]
		public void TicksRatioIsExact(SqlIntervalUnit unit, long numerator, long denominator)
		{
			SqlIntervalUnits.TryGetTicksRatio(unit, out var num, out var den).ShouldBeTrue();

			num.ShouldBe(numerator);
			den.ShouldBe(denominator);
		}

		[TestCase(SqlIntervalUnit.Month)]
		[TestCase(SqlIntervalUnit.Quarter)]
		[TestCase(SqlIntervalUnit.Year)]
		public void CalendarUnitsHaveNoTickCount(SqlIntervalUnit unit)
		{
			// Their elapsed length depends on when they are applied, so there is no honest answer - the caller
			// must reject the translation rather than pick an average.
			SqlIntervalUnits.TryGetTicksRatio(unit, out _, out _).ShouldBeFalse();
			SqlIntervalUnits.TryToTicks(1, unit, out _).ShouldBeFalse();
		}

		[TestCase(SqlIntervalUnit.Tick,        1L, 1L)]
		[TestCase(SqlIntervalUnit.Microsecond, 1L, 10L)]
		[TestCase(SqlIntervalUnit.Millisecond, 1L, TimeSpan.TicksPerMillisecond)]
		[TestCase(SqlIntervalUnit.Second,      1L, TimeSpan.TicksPerSecond)]
		[TestCase(SqlIntervalUnit.Minute,      1L, TimeSpan.TicksPerMinute)]
		[TestCase(SqlIntervalUnit.Hour,        1L, TimeSpan.TicksPerHour)]
		[TestCase(SqlIntervalUnit.Day,         1L, TimeSpan.TicksPerDay)]
		[TestCase(SqlIntervalUnit.Week,        1L, TimeSpan.TicksPerDay * 7)]
		[TestCase(SqlIntervalUnit.Second,     -3L, -3 * TimeSpan.TicksPerSecond)]
		public void ToTicksMatchesTimeSpan(SqlIntervalUnit unit, long amount, long expected)
		{
			SqlIntervalUnits.TryToTicks(amount, unit, out var ticks).ShouldBeTrue();

			ticks.ShouldBe(expected);
		}

		[TestCase(  99L,  0L)]
		[TestCase( 100L,  1L)]
		[TestCase( 199L,  1L)]
		[TestCase( -99L,  0L)]
		[TestCase(-100L, -1L)]
		[TestCase(-101L, -1L)]
		public void NanosecondsTruncateTowardZero(long nanoseconds, long expectedTicks)
		{
			// A tick is 100ns, so sub-tick amounts cannot round - and they must truncate toward zero the way CLR
			// integer division does, symmetrically for negatives, not floor.
			SqlIntervalUnits.TryToTicks(nanoseconds, SqlIntervalUnit.Nanosecond, out var ticks).ShouldBeTrue();

			ticks.ShouldBe(expectedTicks);
		}

		[Test]
		public void OverflowFailsInsteadOfWrapping()
		{
			// Silent wrapping would turn a too-large duration into a plausible small one. Refusing is the only
			// safe answer.
			SqlIntervalUnits.TryToTicks(long.MaxValue, SqlIntervalUnit.Day, out _).ShouldBeFalse();
			SqlIntervalUnits.TryToTicks(long.MinValue, SqlIntervalUnit.Hour, out _).ShouldBeFalse();
		}

		[Test]
		public void UnitMappingIsInjective()
		{
			// A collision here would make two different storage units indistinguishable downstream.
			var units = Enum.GetValues<DurationUnit>();

			units.Select(SqlIntervalType.ToIntervalUnit).Distinct().Count().ShouldBe(units.Length);
		}
	}
}
