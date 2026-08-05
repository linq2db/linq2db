using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class IntervalTranslationTests : TestBase
	{
		[Table]
		sealed class DurationRow
		{
			[Column] public int Id { get; set; }

			// Same CLR type, same storage type, different units. Nothing about Int64 says which - only the
			// declaration does, and getting it wrong is a silent factor-of-10000000 error.
			[Column] public TimeSpan InSeconds { get; set; }
			[Column] public TimeSpan InTicks   { get; set; }

			[Column] public TimeSpan Undeclared { get; set; }

			// No duration declaration, and a converter that is NOT the identity in ticks - so reading it without
			// the converter gives a visibly different value.
			[Column] public TimeSpan UndeclaredSeconds { get; set; }
		}

		static MappingSchema BuildSchema()
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<DurationRow>()
					// One declaration per column. The value conversion is derived from the unit, so it cannot
					// disagree with what the translator assumes.
					.Property(e => e.InSeconds)
						.HasDataType(DataType.Int64)
						.HasDuration(DurationUnit.Second)
					.Property(e => e.InTicks)
						.HasDataType(DataType.Int64)
						.HasDuration(DurationUnit.Tick)
					.Property(e => e.Undeclared)
						.HasDataType(DataType.Int64)
						.HasConversion(ts => ts.Ticks, v => TimeSpan.FromTicks(v))
					.Property(e => e.UndeclaredSeconds)
						.HasDataType(DataType.Int64)
						.HasConversion(ts => ts.Ticks / TimeSpan.TicksPerSecond, v => TimeSpan.FromTicks(v * TimeSpan.TicksPerSecond))
				.Build();

			return ms;
		}

		static void Seed(IDataContext db, params TimeSpan[] values)
		{
			for (var i = 0; i < values.Length; i++)
			{
				db.Insert(new DurationRow
				{
					Id         = i + 1,
					InSeconds         = values[i],
					InTicks           = values[i],
					Undeclared        = values[i],
					UndeclaredSeconds = values[i],
				});
			}
		}

		[Test]
		public void TotalMatchesClr([DataSources] string context)
		{
			var value = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			t.Select(r => r.InSeconds.TotalHours).Single().ShouldBe(value.TotalHours);
			t.Select(r => r.InSeconds.TotalMinutes).Single().ShouldBe(value.TotalMinutes);
			t.Select(r => r.InTicks.TotalHours).Single().ShouldBe(value.TotalHours);
		}

		[Test]
		public void ComponentMatchesClr([DataSources] string context)
		{
			var value = new TimeSpan(2, 3, 4, 5);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			t.Select(r => r.InSeconds.Days).Single().ShouldBe(value.Days);
			t.Select(r => r.InSeconds.Hours).Single().ShouldBe(value.Hours);
			t.Select(r => r.InSeconds.Minutes).Single().ShouldBe(value.Minutes);
			t.Select(r => r.InSeconds.Seconds).Single().ShouldBe(value.Seconds);
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

			t.Select(r => r.InSeconds.Days).Single().ShouldBe(value.Days);
			t.Select(r => r.InSeconds.Hours).Single().ShouldBe(value.Hours);
			t.Select(r => r.InSeconds.TotalHours).Single().ShouldBe(value.TotalHours);
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

			t.Select(r => r.InSeconds.TotalHours).Single().ShouldBe(3d);
			t.Select(r => r.InTicks.TotalHours).Single().ShouldBe(3d);
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
		public void NegationIsTranslatedWhenConsumed([DataSources] string context)
		{
			var value = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			t.Select(r => (-r.InSeconds).TotalHours).Single().ShouldBe((-value).TotalHours);
			t.Select(r => (-r.InSeconds).Hours).Single().ShouldBe((-value).Hours);
		}

		[Test]
		public void ComputedIntervalProjectsAndMaterializes([DataSources] string context)
		{
			// Nothing carries a converter on the expression. QueryHelper.GetColumnDescriptor looks through the
			// interval node back to the operand's column, and ToReadExpression uses that column's converter -
			// the same path an ordinary column projection takes.
			//
			// This only works because the interval node carries the model type: were it typed by its storage,
			// the descriptor lookup would drop it and the amount would come back read as raw ticks.
			var value = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			t.Select(r => -r.InSeconds).Single().ShouldBe(-value);
			t.Select(r => -r.InTicks).Single().ShouldBe(-value);
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

			t.Select(r => Sql.AsSql(-r.UndeclaredSeconds)).Single().ShouldBe(-value);
		}

		[Test]
		public void ArithmeticHappensOnTheServer([DataSources] string context)
		{
			// Without this the fixture would prove much less: had translation returned null, linq2db would
			// evaluate the members client-side and every value assertion above would still pass.
			//
			// Sql.AsSql forces server evaluation, so a provider that cannot translate the member fails here
			// instead of quietly computing it in .NET. Matching the generated SQL text would not work across
			// providers - the constants and the truncation function differ from one to the next.
			var value = new TimeSpan(2, 3, 4, 5);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			t.Select(r => Sql.AsSql(r.InSeconds.TotalHours)).Single().ShouldBe(value.TotalHours);
			t.Select(r => Sql.AsSql(r.InSeconds.Hours)).Single().ShouldBe(value.Hours);
			t.Select(r => Sql.AsSql(r.InTicks.TotalMinutes)).Single().ShouldBe(value.TotalMinutes);
		}
	}
}
