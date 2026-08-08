using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class IntervalTranslationTests
	{
		[Test]
		public void DifferenceAddedBackToADate([DataSources] string context)
		{
			// A difference is not only read for its parts - it gets used. Adding it back to its own start must
			// land on the end, and adding it to a third date must move that one by the same amount.
			var started  = new DateTime(2026, 1, 1, 10,  0, 0);
			var finished = new DateTime(2026, 1, 3, 13, 30, 0);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = started, FinishedOn = finished });

			// Only the cancelling forms here, which the optimizer resolves before any provider is asked - so this
			// runs everywhere. A shift off an unrelated base needs real lowering and is tested separately.
			var row = t
				.Select(r => new
				{
					BackToEnd = r.StartedOn + (r.FinishedOn - r.StartedOn),
					BackToStart = r.FinishedOn - (r.FinishedOn - r.StartedOn),

					// The result is a date like any other, so a part of it still has to read.
					Hour = (r.StartedOn + (r.FinishedOn - r.StartedOn)).Hour,
				})
				.Single();

			row.BackToEnd.ShouldBe(finished);
			row.BackToStart.ShouldBe(started);
			row.Hour.ShouldBe(finished.Hour);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDeclaredShiftProviders, ErrorMessage = ErrorHelper.Error_Interval_Shift)]
		public void ADeclaredDurationShiftsADate([DataSources(false)] string context)
		{
			// A shift whose interval is a declared column rather than a computed difference. The two reach the
			// provider as the same node but carry the amount differently - a difference lowers to ticks, a
			// declaration keeps the number the column holds - so the seconds column is what tells them apart.
			// The tick column is the control: its stored number already is a tick count, so it reads correctly
			// either way and a failure on it alone would mean something else broke.
			var value = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			var row = t
				.Select(r => new
				{
					AddedSeconds      = ShiftOrigin + r.InSeconds,
					SubtractedSeconds = ShiftOrigin - r.InSeconds,
					AddedTicks        = ShiftOrigin + r.InTicks,
				})
				.Single();

			// Subtraction is its own branch in every lowering - the shared one negates the amount, the providers
			// with a native interval spell a different operator - so it is asked for beside the addition rather
			// than assumed to follow from it.
			row.AddedSeconds.ShouldBe(ShiftOrigin + value);
			row.SubtractedSeconds.ShouldBe(ShiftOrigin - value);
			row.AddedTicks.ShouldBe(ShiftOrigin + value);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDeclaredShiftProviders, ErrorMessage = ErrorHelper.Error_Interval_Shift)]
		public void ADurationThatMayBeAbsentShiftsADate([DataSources(false)] string context)
		{
			// A shift by a nullable duration is a registration of its own, and the result is nullable with it: the
			// row that holds no duration must come back holding no date, which is what the CLR's lifted operator
			// says. A zero-length shift landing on the origin would read as an answer and is the failure to catch.
			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable(OptionalDurationRow.Data);

			var rows = t
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					r.Id,
					Shifted  = ShiftOrigin + r.Grace,
					Required = ShiftOrigin + r.Required,
				})
				.ToList();

			rows.Select(r => r.Shifted).ShouldBe(
			[
				ShiftOrigin + TimeSpan.FromMinutes(15),
				null,
				ShiftOrigin + TimeSpan.FromMinutes(45),
			]);

			// The column that is never absent rides along, so a run that answered nothing for every row would fail
			// here rather than pass on the nulls.
			rows.Select(r => r.Required).ShouldBe(
			[
				ShiftOrigin + TimeSpan.FromMinutes(15),
				ShiftOrigin + TimeSpan.FromMinutes(30),
				ShiftOrigin + TimeSpan.FromMinutes(45),
			]);
		}

		/// <summary>
		/// A date shifted by an interval, in a predicate - answered where the provider can lower it, refused by
		/// name where it cannot.
		/// </summary>
		/// <remarks>
		/// A predicate is the right shape for this: there is no falling back to .NET for any provider, because the
		/// rows have to be chosen by the database. What the refusal side pins is loudness, not incapability - until
		/// a provider gains the lowering, the attempt must fail by name rather than produce SQL the database
		/// accepts and answers wrongly, which is what a plain plus between a date and a tick count gives.
		/// <para>
		/// Local contexts only: a remote one wraps the refusal in a transport exception, which says nothing about
		/// the translation.
		/// </para>
		/// </remarks>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedShiftProviders, ErrorMessage = ErrorHelper.Error_Interval_Shift)]
		[ThrowsCannotBeConverted(ShiftRefusedWhileBuildingProviders)]
		public void ShiftIsExpressedInAPredicate([DataSources(false)] string context)
		{
			var started = new DateTime(2026, 1, 1, 10, 0, 0);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = started, FinishedOn = started.AddHours(5) });

			ShiftedInAPredicate(t)
				.ToArray()
				.ShouldBe([1]);
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
	}
}
