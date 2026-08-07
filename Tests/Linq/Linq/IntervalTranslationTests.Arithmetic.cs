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
	}
}
