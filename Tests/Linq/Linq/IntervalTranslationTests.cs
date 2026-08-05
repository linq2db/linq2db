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
		// Access has no 64-bit integer column type, so it stores the same durations as DECIMAL. Declaring that per
		// configuration keeps one model and one set of tests: the storage type is a provider detail, and the
		// feature under test - that the unit comes from the declaration - is exactly what should not vary with it.
		const string Wide = ProviderName.Access;

		[Table]
		sealed class DurationRow
		{
			[Column] public int Id { get; set; }

			// Same CLR type, same storage type, different units. Nothing about the storage says which - only the
			// declaration does, and getting it wrong is a silent factor-of-10000000 error.
			[Column(DataType = DataType.Int64)]
			[Column(Configuration = Wide, DataType = DataType.Decimal, Precision = 18, Scale = 0)]
			[Duration(DurationUnit.Second)]
			public TimeSpan InSeconds { get; set; }

			[Column(DataType = DataType.Int64)]
			[Column(Configuration = Wide, DataType = DataType.Decimal, Precision = 18, Scale = 0)]
			[Duration(DurationUnit.Tick)]
			public TimeSpan InTicks { get; set; }

			[Column(DataType = DataType.Int64)]
			[Column(Configuration = Wide, DataType = DataType.Decimal, Precision = 18, Scale = 0)]
			public TimeSpan Undeclared { get; set; }

			// No duration declaration, and a converter that is NOT the identity in ticks - so reading it without
			// the converter gives a visibly different value.
			[Column(DataType = DataType.Int64)]
			[Column(Configuration = Wide, DataType = DataType.Decimal, Precision = 18, Scale = 0)]
			public TimeSpan UndeclaredSeconds { get; set; }
		}

		static MappingSchema BuildSchema()
		{
			var ms = new MappingSchema();

			// The declared columns need nothing here - the value conversion is derived from their unit. Only the
			// two undeclared ones carry hand-written converters, and that is the point of them.
			new FluentMappingBuilder(ms)
				.Entity<DurationRow>()
					.Property(e => e.Undeclared)
						.HasConversion(ts => ts.Ticks, v => TimeSpan.FromTicks(v))
					.Property(e => e.UndeclaredSeconds)
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

		[Table]
		sealed class EventRow
		{
			[Column] public int      Id        { get; set; }
			[Column(DataType = DataType.DateTime2, Precision = 7)] public DateTime StartedOn  { get; set; }
			[Column(DataType = DataType.DateTime2, Precision = 7)] public DateTime FinishedOn { get; set; }
		}

		[Test]
		public void DateDifferenceIsElapsedTime([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			// Elapsed, not a boundary count. 10:59 -> 11:01 is two minutes; Sql.DateDiff(hour, ...) would say one,
			// and that difference is the whole reason this does not reuse the DateDiff builders.
			var started  = new DateTime(2026, 1, 1, 10, 59, 0);
			var finished = new DateTime(2026, 1, 1, 11,  1, 0);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = started, FinishedOn = finished });

			var elapsed = finished - started;

			t.Select(r => Sql.AsSql((r.FinishedOn - r.StartedOn).TotalMinutes)).Single().ShouldBe(elapsed.TotalMinutes);
			t.Select(r => Sql.AsSql((r.FinishedOn - r.StartedOn).Minutes)).Single().ShouldBe(elapsed.Minutes);
		}

		[Test]
		public void DateDifferenceKeepsSubSecondPrecision([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			// datetime2 stores 100ns, so the difference must too - a millisecond-resolution DATEDIFF would report
			// zero here. This is the case the review of #5739 called out as translator-induced precision loss.
			var started = new DateTime(2026, 1, 1);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = started, FinishedOn = started.AddTicks(9999) });

			t.Select(r => Sql.AsSql((r.FinishedOn - r.StartedOn).Ticks)).Single().ShouldBe(9999L);
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

			// Access stores the tick count as DECIMAL - it has no 64-bit integer type - so dividing it happens in
			// decimal arithmetic and the last bit of the resulting double need not match .NET's binary division.
			// Every provider that holds the count in BIGINT does match exactly, so the tolerance is granted only
			// where the storage makes exactness impossible, not everywhere.
			var totalMinutes = t.Select(r => Sql.AsSql(r.InTicks.TotalMinutes)).Single();

			if (context.IsAnyOf(TestProvName.AllAccess))
				totalMinutes.ShouldBe(value.TotalMinutes, tolerance: 1e-9);
			else
				totalMinutes.ShouldBe(value.TotalMinutes);
		}
	}
}
