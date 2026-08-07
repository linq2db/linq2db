using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class IntervalTranslationTests
	{
		[Test]
		public void TotalMatchesClr([DataSources] string context)
		{
			var value = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			var row = t
				.Select(r => new
				{
					SecondsHours   = r.InSeconds.TotalHours,
					SecondsMinutes = r.InSeconds.TotalMinutes,
					TicksHours     = r.InTicks.TotalHours,
				})
				.Single();

			row.SecondsHours.ShouldBe(value.TotalHours);
			row.SecondsMinutes.ShouldBe(value.TotalMinutes);
			row.TicksHours.ShouldBe(value.TotalHours);
		}

		[Test]
		public void ComponentMatchesClr([DataSources] string context)
		{
			var value = new TimeSpan(2, 3, 4, 5);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			var row = t
				.Select(r => new
				{
					r.InSeconds.Days,
					r.InSeconds.Hours,
					r.InSeconds.Minutes,
					r.InSeconds.Seconds,
				})
				.Single();

			row.Days.ShouldBe(value.Days);
			row.Hours.ShouldBe(value.Hours);
			row.Minutes.ShouldBe(value.Minutes);
			row.Seconds.ShouldBe(value.Seconds);
		}

		[Test]
		public void NegativeComponentsTruncateTowardZero([DataSources] string context)
		{
			// The case provider division and modulo disagree on. CLR truncates toward zero, so -25h is
			// Days == -1 and Hours == -1, not -2 / +23 as a flooring provider would give.
			var value = TimeSpan.FromHours(-25);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			value.Days.ShouldBe(-1);
			value.Hours.ShouldBe(-1);

			var row = t
				.Select(r => new
				{
					r.InSeconds.Days,
					r.InSeconds.Hours,
					r.InSeconds.TotalHours,
				})
				.Single();

			row.Days.ShouldBe(value.Days);
			row.Hours.ShouldBe(value.Hours);
			row.TotalHours.ShouldBe(value.TotalHours);
		}

		[Test]
		public void UnitIsTakenFromTheDeclarationNotTheStorageType([DataSources] string context)
		{
			// Both columns are Int64 and hold the same duration, but in different units. If the unit were
			// inferred from the storage type instead of the declaration, one of these would be wrong.
			var value = TimeSpan.FromHours(3);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			var row = t
				.Select(r => new
				{
					Seconds = r.InSeconds.TotalHours,
					Ticks   = r.InTicks.TotalHours,
				})
				.Single();

			row.Seconds.ShouldBe(3d);
			row.Ticks.ShouldBe(3d);
		}

		[Test]
		public void ValueRoundTripsThroughTheDeclaredUnit([DataSources] string context)
		{
			// No HasConversion anywhere in this fixture for the declared columns - the conversion is derived from
			// the unit, so writing and reading back has to work on the declaration alone.
			var value = TimeSpan.FromSeconds(4567);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			var row = t.Single();

			row.InSeconds.ShouldBe(value);
			row.InTicks.ShouldBe(value);
		}

		[Test]
		public void NegatedConvertedColumnKeepsItsConverter([DataSources] string context)
		{
			// Undeclared, so no interval node is involved - this exercises the plain converted-column path and
			// answers whether GetColumnDescriptor's unary branch is fixing a pre-existing defect or only serving
			// the interval path. Whatever the query does, negating must not change which converter applies.
			var value = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			t
				.Select(r => Sql.AsSql(-r.UndeclaredSeconds))
				.Single()
				.ShouldBe(-value);
		}
	}
}
