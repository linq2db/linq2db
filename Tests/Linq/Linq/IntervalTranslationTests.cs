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
			// Declared a key because YDB requires one on every table.
			[PrimaryKey] public int Id { get; set; }

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
		public void NegationIsTranslatedWhenConsumed([DataSources] string context)
		{
			var value = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			var row = t
				.Select(r => new
				{
					(-r.InSeconds).TotalHours,
					(-r.InSeconds).Hours,
				})
				.Single();

			row.TotalHours.ShouldBe((-value).TotalHours);
			row.Hours.ShouldBe((-value).Hours);
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

			var row = t
				.Select(r => new
				{
					Seconds = -r.InSeconds,
					Ticks   = -r.InTicks,
				})
				.Single();

			row.Seconds.ShouldBe(-value);
			row.Ticks.ShouldBe(-value);
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

		[Table]
		sealed class EventRow
		{
			// Declared a key because YDB requires one on every table.
			[PrimaryKey] public int Id { get; set; }

			// The tick-precise type is asked for because one test measures a sub-millisecond difference, and it is
			// asked for only where it exists: Access ODBC has no mapping for DateTime2 at all, and ClickHouse
			// rejects it too, so those fall back to whatever the provider maps DateTime to.
			[Column(DataType = DataType.DateTime2, Precision = 7)]
			[Column(Configuration = ProviderName.Access)]
			[Column(Configuration = ProviderName.ClickHouse)]
			public DateTime StartedOn  { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			[Column(Configuration = ProviderName.Access)]
			[Column(Configuration = ProviderName.ClickHouse)]
			public DateTime FinishedOn { get; set; }
		}

		[Test]
		public void DateDifferenceComponentsMatchClr(
			[IncludeDataSources(TestProvName.AllSqlServer2016Plus, TestProvName.AllPostgreSQL)] string context,
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
		public void DifferenceAddedBackToADate([DataSources] string context)
		{
			// A difference is not only read for its parts - it gets used. Adding it back to its own start must
			// land on the end, and adding it to a third date must move that one by the same amount.
			var started  = new DateTime(2026, 1, 1, 10,  0, 0);
			var finished = new DateTime(2026, 1, 3, 13, 30, 0);
			var other    = new DateTime(2026, 6, 15, 8, 45, 0);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = started, FinishedOn = finished });

			var elapsed = finished - started;

			var row = t
				.Select(r => new
				{
					BackToEnd = r.StartedOn + (r.FinishedOn - r.StartedOn),
					Shifted   = other + (r.FinishedOn - r.StartedOn),

					// The shifted date is a date like any other, so a part of it still has to read.
					Hour      = (r.StartedOn + (r.FinishedOn - r.StartedOn)).Hour,
				})
				.Single();

			row.BackToEnd.ShouldBe(finished);
			row.Shifted.ShouldBe(other + elapsed);
			row.Hour.ShouldBe(finished.Hour);
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

		[Test]
		public void DateDifferenceKeepsSubSecondPrecision([IncludeDataSources(true, TestProvName.AllSqlServer2016Plus, TestProvName.AllPostgreSQL)] string context)
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

			var row = t
				.Select(r => new
				{
					TotalHours   = Sql.AsSql(r.InSeconds.TotalHours),
					Hours        = Sql.AsSql(r.InSeconds.Hours),
					TotalMinutes = Sql.AsSql(r.InTicks.TotalMinutes),
				})
				.Single();

			row.TotalHours.ShouldBe(value.TotalHours);
			row.Hours.ShouldBe(value.Hours);

			// Access stores the tick count as DECIMAL - it has no 64-bit integer type - so dividing it happens in
			// decimal arithmetic and the last bit of the resulting double need not match .NET's binary division.
			// Every provider that holds the count in BIGINT does match exactly, so the tolerance is granted only
			// where the storage makes exactness impossible, not everywhere.
			if (context.IsAnyOf(TestProvName.AllAccess))
				row.TotalMinutes.ShouldBe(value.TotalMinutes, tolerance: 1e-9);
			else
				row.TotalMinutes.ShouldBe(value.TotalMinutes);
		}
	}
}
