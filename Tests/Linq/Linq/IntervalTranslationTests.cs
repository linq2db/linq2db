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
					InSeconds  = values[i],
					InTicks    = values[i],
					Undeclared = values[i],
				});
			}
		}

		[Test]
		public void TotalMatchesClr([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
		public void ComponentMatchesClr([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
		public void NegativeComponentsTruncateTowardZero([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
		public void UnitIsTakenFromTheDeclarationNotTheStorageType([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
		public void ValueRoundTripsThroughTheDeclaredUnit([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
		public void ArithmeticHappensOnTheServer([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			// Without this the whole fixture proves nothing: if translation returned null, linq2db would evaluate
			// the members client-side and every value assertion would still pass.
			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();

			var totalSql     = t.Select(r => r.InSeconds.TotalHours).ToSqlQuery().Sql;
			var componentSql = t.Select(r => r.InSeconds.Hours).ToSqlQuery().Sql;

			// seconds -> ticks -> hours
			totalSql.ShouldContain("10000000");
			totalSql.ShouldContain("36000000000");

			// truncation toward zero is stated explicitly rather than left to the provider
			componentSql.ShouldContain("FLOOR");
			componentSql.ShouldContain("CEILING");
		}

		[Test]
		public void UndeclaredColumnIsNotTranslated([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			// The opt-in rule. Without a declaration there is no unit, so the member cannot be translated -
			// it must fall back to client-side evaluation rather than guess ticks.
			var value = TimeSpan.FromHours(3);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			var sql = t.Select(r => r.Undeclared.TotalHours).ToSqlQuery().Sql;

			sql.ShouldNotContain("10000000");
		}
	}
}
