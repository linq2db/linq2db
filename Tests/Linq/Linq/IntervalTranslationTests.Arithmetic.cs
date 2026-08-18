using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class IntervalTranslationTests
	{
		/// <summary>
		/// Two durations that were declared in different units combine as the durations they are, not as the numbers
		/// they are stored as.
		/// </summary>
		/// <remarks>
		/// The unit a stored duration counts in lives in its column descriptor, so both operands reach the
		/// translation as bare numbers - ninety minutes is 5400 in one column and 54000000000 in the other. Combined
		/// as they stand the answer is the sum of two unrelated counts, read back through whichever descriptor is
		/// reached first, and nothing raises: 5400 + 54000000000 comes back as 625000.01:30:00.
		/// <para>
		/// Asked in both forms. Plain, the value is what a caller reads however it was arrived at; through
		/// <c>Sql.AsSql</c>, the arithmetic has to be the database's, so a fallback to .NET fails the case rather
		/// than answering it. The assertions are the same either way - what changes is what a pass means.
		/// </para>
		/// <para>
		/// Subtraction is asked in both directions because the two fail differently: one overshoots by the whole tick
		/// count, the other lands at 01:29:59.9994600, close enough to the right answer to pass a careless eye. The
		/// same-unit pairs are asked alongside them - they are already correct, and they are what would break if
		/// reconciling reached further than the mixed case.
		/// </para>
		/// <para>
		/// Access refuses in both forms rather than answering: a duration is kept as money there, since it has no
		/// 64-bit integer, and a tick count in money is not one the reader can turn back into a duration.
		/// </para>
		/// </remarks>
		[Test]
		[ThrowsCannotBeConverted(NoTickTotalProviders)]
		public void DurationsInDifferentUnitsCombineAsDurations([DataSources] string context, [Values] bool inSql)
		{
			// Direct and remote fold the operand casts differently - remote folds them into the enclosing cast,
			// direct keeps them - so the two traces differ by cast placement alone while denoting the same
			// arithmetic. The values are asserted in both contexts, which is what this case is here to hold.
			using var noBaseline = new DisableBaseline("Direct and remote differ by redundant cast placement only.");

			var value = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			// The two shapes differ only in whether the arithmetic is forced into SQL. A bare column on each side
			// states the rule; the rest are the shapes a query actually takes - an operand that is itself computed,
			// one that arrives negated, one chosen by a condition, and both sides computed at once. The nested case
			// additionally says that what this produces is usable as an operand again, and the lopsided subtraction
			// that two sides of equal magnitude cannot cancel a wrong scaling into a right answer.
			var row = inSql
				? t.Select(r => new Combination
					{
						SameSeconds     = Sql.AsSql(r.InSeconds + r.InSeconds),
						SameTicks       = Sql.AsSql(r.InTicks   + r.InTicks),
						MixedAdd        = Sql.AsSql(r.InSeconds + r.InTicks),
						MixedSub        = Sql.AsSql(r.InSeconds - r.InTicks),
						MixedSubRev     = Sql.AsSql(r.InTicks   - r.InSeconds),
						Nested          = Sql.AsSql((r.InSeconds + r.InTicks) + r.InSeconds),
						Negated         = Sql.AsSql(-r.InSeconds + r.InTicks),
						Conditional     = Sql.AsSql((r.Id > 0 ? r.InSeconds : r.InSeconds) + r.InTicks),
						BothComputed    = Sql.AsSql((r.InSeconds + r.InTicks) + (r.InTicks + r.InTicks)),
						BothComputedSub = Sql.AsSql((r.InSeconds + r.InSeconds + r.InTicks) - (r.InTicks + r.InTicks)),
						NegatedSub      = Sql.AsSql(-r.InSeconds - r.InTicks),
					}).Single()
				: t.Select(r => new Combination
					{
						SameSeconds     = r.InSeconds + r.InSeconds,
						SameTicks       = r.InTicks   + r.InTicks,
						MixedAdd        = r.InSeconds + r.InTicks,
						MixedSub        = r.InSeconds - r.InTicks,
						MixedSubRev     = r.InTicks   - r.InSeconds,
						Nested          = (r.InSeconds + r.InTicks) + r.InSeconds,
						Negated         = -r.InSeconds + r.InTicks,
						Conditional     = (r.Id > 0 ? r.InSeconds : r.InSeconds) + r.InTicks,
						BothComputed    = (r.InSeconds + r.InTicks) + (r.InTicks + r.InTicks),
						BothComputedSub = (r.InSeconds + r.InSeconds + r.InTicks) - (r.InTicks + r.InTicks),
						NegatedSub      = -r.InSeconds - r.InTicks,
					}).Single();

			row.SameSeconds.ShouldBe(value + value);
			row.SameTicks.ShouldBe(value + value);
			row.MixedAdd.ShouldBe(value + value);
			row.MixedSub.ShouldBe(TimeSpan.Zero);
			row.MixedSubRev.ShouldBe(TimeSpan.Zero);

			row.Nested.ShouldBe(value + value + value);
			row.Negated.ShouldBe(TimeSpan.Zero);
			row.Conditional.ShouldBe(value + value);

			row.BothComputed.ShouldBe(value + value + value + value);
			row.BothComputedSub.ShouldBe(value);
			row.NegatedSub.ShouldBe(-(value + value));

			// The operands carried through a projection, where each side reaches the arithmetic as a reference into
			// the anonymous type rather than as the column itself.
			var projected = inSql
				? t.Select(r => new { Seconds = r.InSeconds, Ticks = r.InTicks }).Select(x => Sql.AsSql(x.Seconds + x.Ticks)).Single()
				: t.Select(r => new { Seconds = r.InSeconds, Ticks = r.InTicks }).Select(x => x.Seconds + x.Ticks).Single();

			projected.ShouldBe(value + value);
		}

		/// <summary>
		/// Carries the combinations <see cref="DurationsInDifferentUnitsCombineAsDurations"/> asserts, so its two
		/// query shapes share one set of assertions.
		/// </summary>
		sealed class Combination
		{
			public TimeSpan SameSeconds     { get; set; }
			public TimeSpan SameTicks       { get; set; }
			public TimeSpan MixedAdd        { get; set; }
			public TimeSpan MixedSub        { get; set; }
			public TimeSpan MixedSubRev     { get; set; }
			public TimeSpan Nested          { get; set; }
			public TimeSpan Negated         { get; set; }
			public TimeSpan Conditional     { get; set; }
			public TimeSpan BothComputed    { get; set; }
			public TimeSpan BothComputedSub { get; set; }
			public TimeSpan NegatedSub      { get; set; }
		}

		/// <summary>
		/// A computed difference and a declared duration combine as the durations they are, in either order.
		/// </summary>
		/// <remarks>
		/// The mixed-unit case with the operand that carries no declaration of its own: a difference is a tick count
		/// by construction, while the budget beside it is declared in seconds.
		/// <para>
		/// Plain, with the forced half below rather than a parameter on this one: the two do not refuse on the same
		/// providers. A provider that cannot measure a difference answers this case from .NET and refuses that one,
		/// so they cannot share a set of gates.
		/// </para>
		/// <para>
		/// Two differences added together take the other path - they already agree on a unit, so nothing reconciles
		/// them. Asserted so the reconciliation cannot start reaching cases it should leave alone.
		/// </para>
		/// </remarks>
		[Test]
		[ThrowsCannotBeConverted(NoTickTotalProviders)]
		public void ADifferenceAndADeclaredDurationCombineAsDurations([DataSources(false)] string context)
		{
			using var noBaseline = new DisableBaseline("Direct and remote differ by redundant cast placement only.");

			var taken  = TimeSpan.FromHours(1);
			var budget = TimeSpan.FromHours(3);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();
			SeedTasks(db, (taken, budget));

			var row = t
				.Select(r => new
				{
					DiffPlusBudgets  = (r.FinishedOn - r.StartedOn) + (r.Budget + r.Budget),
					BudgetsMinusDiff = (r.Budget + r.Budget) - (r.FinishedOn - r.StartedOn),
					DiffPlusDiff     = (r.FinishedOn - r.StartedOn) + (r.FinishedOn - r.StartedOn),
					DiffMinusBudget  = (r.FinishedOn - r.StartedOn) - r.Budget,
				})
				.Single();

			row.DiffPlusBudgets.ShouldBe(taken + budget + budget);
			row.BudgetsMinusDiff.ShouldBe(budget + budget - taken);
			row.DiffPlusDiff.ShouldBe(taken + taken);
			row.DiffMinusBudget.ShouldBe(taken - budget);
		}

		/// <summary>
		/// The same combinations asked in SQL, where a provider that cannot measure a difference has nothing to fall
		/// back to.
		/// </summary>
		/// <remarks>
		/// The forced half of the case above, and the reason it is worth its own method: plain, a difference the
		/// provider cannot measure is computed in .NET and the values still come out right, so the case would pass on
		/// a provider that translated none of it. Informix reaches exactly that, and so does SQL Server before 2016.
		/// <para>
		/// The subtraction is lopsided and one case is negative, for the reason the sibling above gives.
		/// </para>
		/// </remarks>
		[Test]
		[ThrowsCannotBeConverted(NoTickTotalProviders)]
		[ThrowsCannotBeConverted(UnsupportedDifferenceProviders)]
		public void ADifferenceAndADeclaredDurationCombineInSql([DataSources(false)] string context)
		{
			using var noBaseline = new DisableBaseline("Direct and remote differ by redundant cast placement only.");

			var taken  = TimeSpan.FromHours(1);
			var budget = TimeSpan.FromHours(3);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();
			SeedTasks(db, (taken, budget));

			var row = t
				.Select(r => new
				{
					DiffPlusBudgets  = Sql.AsSql((r.FinishedOn - r.StartedOn) + (r.Budget + r.Budget)),
					BudgetsMinusDiff = Sql.AsSql((r.Budget + r.Budget) - (r.FinishedOn - r.StartedOn)),
					DiffPlusDiff     = Sql.AsSql((r.FinishedOn - r.StartedOn) + (r.FinishedOn - r.StartedOn)),
					DiffMinusBudget  = Sql.AsSql((r.FinishedOn - r.StartedOn) - r.Budget),
				})
				.Single();

			row.DiffPlusBudgets.ShouldBe(taken + budget + budget);
			row.BudgetsMinusDiff.ShouldBe(budget + budget - taken);
			row.DiffPlusDiff.ShouldBe(taken + taken);
			row.DiffMinusBudget.ShouldBe(taken - budget);
		}

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
		/// Two dates that may be absent, so a difference taken between them can be absent too.
		/// </summary>
		/// <remarks>
		/// Seeded so that each endpoint is the missing one in turn: which endpoint is missing decides which of the
		/// two cancelling forms goes wrong, and a row missing both would come out right by accident either way.
		/// </remarks>
		[Table]
		sealed class OptionalEventRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7, CanBeNull = true)]
			[Column(Configuration = ProviderName.Access,     CanBeNull = true)]
			[Column(Configuration = ProviderName.ClickHouse, CanBeNull = true)]
			public DateTime? StartedOn  { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7, CanBeNull = true)]
			[Column(Configuration = ProviderName.Access,     CanBeNull = true)]
			[Column(Configuration = ProviderName.ClickHouse, CanBeNull = true)]
			public DateTime? FinishedOn { get; set; }
		}

		[Test]
		public void CancellingAShiftKeepsTheAbsenceItCarried([DataSources] string context)
		{
			// The term that cancels is also the one whose absence the whole expression propagated, so dropping it
			// turns a row that has no answer into one that does - a date appears where the CLR says nothing. The
			// two forms fail on opposite rows, because each discards a different endpoint, which is why one row
			// is missing its start and the next is missing its end.
			var started  = new DateTime(2026, 1, 1, 10,  0, 0);
			var finished = new DateTime(2026, 1, 3, 13, 30, 0);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<OptionalEventRow>();

			db.Insert(new OptionalEventRow { Id = 1, StartedOn = null,    FinishedOn = finished });
			db.Insert(new OptionalEventRow { Id = 2, StartedOn = started, FinishedOn = null     });
			db.Insert(new OptionalEventRow { Id = 3, StartedOn = started, FinishedOn = finished });

			var rows = t
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					r.Id,
					BackToEnd   = r.StartedOn  + (r.FinishedOn - r.StartedOn),
					BackToStart = r.FinishedOn - (r.FinishedOn - r.StartedOn),
				})
				.ToList();

			rows.Select(r => r.BackToEnd).ShouldBe([null, null, finished]);
			rows.Select(r => r.BackToStart).ShouldBe([null, null, started]);
		}

		[Table]
		sealed class PlainDateRow
		{
			[PrimaryKey] public int Id { get; set; }

			// Deliberately no DataType: this is what a date column looks like when nobody asks for anything, and
			// on SQL Server that is DATETIME rather than DATETIME2.
			[Column] public DateTime When { get; set; }

			[Column(DataType = DataType.Int64)]
			[Duration(DurationUnit.Second)]
			public TimeSpan Elapsed { get; set; }
		}

		/// <summary>
		/// A date column that asked for no particular type is still shiftable.
		/// </summary>
		/// <remarks>
		/// Every other shift here runs over a column that pins <see cref="DataType.DateTime2"/>, which is not what a
		/// model looks like by default - and the shift ends in the finest unit the provider counts, which on SQL
		/// Server is the nanosecond. Whether a date type that stores less than that accepts being moved by one is a
		/// property of the column, not of the interval, so it is asked of a column that declares nothing.
		/// </remarks>
		[Test]
		public void ADateColumnWithNoDeclaredTypeShifts([IncludeDataSources(false, TestProvName.AllSqlServer2016Plus)] string context)
		{
			var when    = new DateTime(2026, 3, 1, 0, 0, 0);
			var elapsed = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<PlainDateRow>();

			db.Insert(new PlainDateRow { Id = 1, When = when, Elapsed = elapsed });

			var shifted = t.Select(r => r.When + r.Elapsed).Single();

			shifted.ShouldBe(when + elapsed);
		}

		/// <remarks>
		/// Ungated although several providers cannot express the shift at all. The refusal is raised while the
		/// expression is still being built, so a projection - which is what this is - falls back to .NET and answers
		/// exactly. Asserting the value on every provider is what proves that fallback: a refusal assertion would
		/// only have proved the refusal.
		/// </remarks>
		[Test]
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

		/// <remarks>
		/// Ungated for the reason its non-nullable sibling above is: the shift is declined while the expression is
		/// built, so this projection falls back to .NET everywhere and the absence is asserted on every provider
		/// rather than only on those that can express the arithmetic.
		/// </remarks>
		[Test]
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
		[ThrowsCannotBeConverted(ShiftRefusedWhileBuildingProviders + "," + UnsupportedDifferenceProviders)]
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

		/// <summary>
		/// A shift that survives to the SQL layer, over a remote context as well as a local one.
		/// </summary>
		/// <remarks>
		/// The shift node is the one of the four that no other test can hand to the serializer: the local-only test
		/// above cannot, and the cancelling forms are resolved by the optimizer before anything is written down. So
		/// this names the providers that lower a shift rather than declaring the ones that refuse, which is the only
		/// way to keep a remote context in scope - a refusal comes back wrapped in a transport exception there and
		/// would say nothing about the translation.
		/// </remarks>
		[Test]
		public void AShiftTravelsToARemoteContext(
			[IncludeDataSources(true, TestProvName.AllSqlServer2016Plus, TestProvName.AllPostgreSQL, TestProvName.AllMySql, TestProvName.AllDuckDB)] string context)
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

			// Every provider but one lands on the same double as .NET, so that is what they are held to - a
			// tolerance granted to all of them would stop anyone noticing the day one starts drifting.
			//
			// MySQL 5.7 is the exception, and not because of the order anything is done in: the lowering divides
			// the tick count as a double, but MySQL has no DOUBLE to cast to before 8.0.17, so the cast lands on
			// DECIMAL and the division is decimal. 5.7 rounds that a ulp away from where 8.0 does, on a value 8.0
			// gets exactly.
			if (context.IsAnyOf(TestProvName.AllMySql57))
				row.TotalHours.ShouldBe(value.TotalHours, Tolerance(value.TotalHours));
			else
				row.TotalHours.ShouldBe(value.TotalHours);

			row.Hours.ShouldBe(value.Hours);

			// Two providers cannot match the last bit here, and they are named rather than covered by a blanket
			// tolerance. Both reach it the same way - the division ends up decimal rather than binary - but for
			// different reasons: Access has no 64-bit integer so the count is held as DECIMAL to begin with, and
			// MySQL 5.7 has no DOUBLE to cast to. Everyone else divides a double and lands on .NET's exactly.
			if (context.IsAnyOf(TestProvName.AllAccess, TestProvName.AllMySql57))
				row.TotalMinutes.ShouldBe(value.TotalMinutes, Tolerance(value.TotalMinutes));
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
