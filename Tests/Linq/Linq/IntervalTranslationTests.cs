using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public partial class IntervalTranslationTests : TestBase
	{
		// Access has no 64-bit integer column type, so it stores the same durations as CURRENCY - a 64-bit integer
		// scaled by ten thousand, which carries a tick count exactly and with room to spare. Declaring that per
		// configuration keeps one model and one set of tests: the storage type is a provider detail, and the
		// feature under test - that the unit comes from the declaration - is exactly what should not vary with it.
		//
		// Not DECIMAL, which is the obvious choice and was the first one: the ACE ODBC driver cannot fetch a
		// DECIMAL column produced by a scalar sub-query in the projection. The statement executes and then the
		// read fails inside the driver with "Reserved error (|)". It is the pairing that breaks - the same column
		// reads fine without the sub-query, and through a derived table, and INTEGER, DOUBLE, CURRENCY and VARCHAR
		// all read fine through one - and it reproduces on a plain decimal property with no linq2db conversion on
		// the path at all. So it is the driver's, not ours, and the model sidesteps it rather than the SQL builder
		// working around it for every Access query.
		const string Wide = ProviderName.Access;

		[Table]
		sealed class DurationRow
		{
			// Declared a key because YDB requires one on every table.
			[PrimaryKey] public int Id { get; set; }

			// Same CLR type, same storage type, different units. Nothing about the storage says which - only the
			// declaration does, and getting it wrong is a silent factor-of-10000000 error.
			[Column(DataType = DataType.Int64)]
			[Column(Configuration = Wide, DataType = DataType.Money)]
			[Duration(DurationUnit.Second)]
			public TimeSpan InSeconds { get; set; }

			[Column(DataType = DataType.Int64)]
			[Column(Configuration = Wide, DataType = DataType.Money)]
			[Duration(DurationUnit.Tick)]
			public TimeSpan InTicks { get; set; }

			[Column(DataType = DataType.Int64)]
			[Column(Configuration = Wide, DataType = DataType.Money)]
			public TimeSpan Undeclared { get; set; }

			// No duration declaration, and a converter that is NOT the identity in ticks - so reading it without
			// the converter gives a visibly different value.
			[Column(DataType = DataType.Int64)]
			[Column(Configuration = Wide, DataType = DataType.Money)]
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

		/// <summary>
		/// Relative tolerance for a <c>Total*</c> member, which is a double the server divides in its own order of
		/// operations. Relative rather than fixed because the members span days to milliseconds.
		/// </summary>
		static double Tolerance(double expected)
		{
			return Math.Abs(expected) * 1e-12;
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

		// The write paths below matter because the conversion on the declared columns is *derived* from the unit
		// rather than written by hand. Anything that builds its own parameters, or copies values without asking
		// the column descriptor, would store a number in the wrong unit - and store it silently.

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

		[Table]
		sealed class ZonedEventRow
		{
			[PrimaryKey] public int Id { get; set; }

			// [Column] is not decoration here: [Table] requires it, so a property without one is silently left out
			// of the model - the table is created without the column and the query never resolves it.
			//
			// The precision is asked for because several providers default to whole seconds for a timestamp -
			// MySQL among them - and a difference measured on a column that dropped its fractional part would be
			// testing the column type rather than the translation.
			[Column(Precision = 6)] public DateTimeOffset StartedOn  { get; set; }
			[Column(Precision = 6)] public DateTimeOffset FinishedOn { get; set; }
		}

		/// <summary>
		/// Providers with no lowering for a date shifted by an interval yet, so the attempt is refused by name.
		/// </summary>
		/// <remarks>
		/// Declared as the unsupported side rather than the supported one so a single test can run everywhere and
		/// assert both outcomes: the providers listed here must fail with <em>our</em> refusal, and every other
		/// provider must answer. Naming the supported side instead would need two tests, and the excluded one
		/// would only ever check that <em>something</em> was thrown.
		/// <para>
		/// This list shrinks as the lowering spreads - most of these could shift a date perfectly well and simply
		/// have no implementation yet.
		/// </para>
		/// </remarks>
		const string UnsupportedShiftProviders =
			TestProvName.AllSQLite            + "," +
			TestProvName.AllOracle            + "," +
			TestProvName.AllFirebird          + "," +
			TestProvName.AllSapHana           + "," +
			TestProvName.AllDB2               + "," +
			TestProvName.AllInformix          + "," +
			TestProvName.AllYdb               + "," +
			TestProvName.AllSqlServer2014Minus;

		/// <summary>
		/// Providers that refuse the shift one step earlier, while the expression is still being built.
		/// </summary>
		/// <remarks>
		/// Access counts elapsed units but cannot turn a difference into a value at all, so the shift node is never
		/// created and the refusal comes from expression building rather than from the SQL builder. Both are
		/// refusals and both are correct - they are listed apart because the message differs, and a test that
		/// accepted either would stop proving which one happened.
		/// </remarks>
		const string ShiftRefusedWhileBuildingProviders = TestProvName.AllAccess;

		/// <summary>
		/// Providers that cannot express an elapsed difference as a value at all, so any comparison or member
		/// taken from one is refused rather than answered.
		/// </summary>
		const string UnsupportedDifferenceProviders =
			TestProvName.AllInformix          + "," +
			TestProvName.AllSqlServer2014Minus;

		/// <summary>
		/// Providers that measure an elapsed difference but cannot express one as a tick count.
		/// </summary>
		/// <remarks>
		/// Access is the only one: <c>DateDiff</c> hands back a 32-bit count, and scaling seconds to ticks overflows
		/// it after about three and a half minutes, so its <c>ElapsedTicks</c> refuses by design. That splits the
		/// query shapes in two. Grouping and ordering compare a difference against itself, so any single unit does
		/// and Access answers them; comparing one against a duration that came from somewhere else needs a unit both
		/// sides share, and ticks are the only one - so those are refused instead of answered approximately.
		/// </remarks>
		const string NoTickTotalProviders = TestProvName.AllAccess;

		/// <summary>
		/// The base is deliberately not the difference's own start: that form cancels in the optimizer and no
		/// shift survives to test.
		/// </summary>
		static readonly DateTime ShiftOrigin = new(2026, 3, 1, 0, 0, 0);

		static IQueryable<int> ShiftedInAPredicate(ITable<EventRow> t)
		{
			return t
				.Where(r => ShiftOrigin + (r.FinishedOn - r.StartedOn) > ShiftOrigin.AddHours(4))
				.Select(r => r.Id);
		}

	}
}
