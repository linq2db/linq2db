using System;
using System.Globalization;
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
		/// A declared duration paired with one stored through a hand-written converter, which never said what its
		/// number counts.
		/// </summary>
		/// <remarks>
		/// The two stored numbers are not commensurable and nothing in the model can make them so: <c>InSeconds</c>
		/// holds 5400 and <c>InTicks</c> holds 54000000000 for the same duration, and only the declared one says
		/// which it is. Adding them as they stand answered 01:30:00.0005400 for a sum that is three hours - the
		/// converted column's 5400 taken as ticks - which is wrong by a factor of ten million and silent.
		/// <para>
		/// Refused whether or not SQL is demanded, which is asserted rather than assumed because the other refusals
		/// in this fixture behave the other way: a member that cannot be lowered leaves the column readable and the
		/// projection answers it in .NET. Here the error is the combination itself, so there is no half of it left
		/// to read the two sides through, and a bare projection is refused too. A named refusal for a wrong duration
		/// is the trade, and the message names both ways out - declare the column, or combine the two in .NET.
		/// </para>
		/// <para>
		/// The pairing where <em>neither</em> side declares a unit is refused too, one layer down and by its own
		/// message - see <see cref="TwoDisagreeingConvertersRefuseToCombine"/>.
		/// </para>
		/// </remarks>
		[Test]
		public void ADeclaredDurationRefusesAnUndeclaredOne([DataSources] string context)
		{
			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, TimeSpan.FromMinutes(90));

			var forced = () => t
				.Select(r => Sql.AsSql(r.InTicks + r.UndeclaredSeconds))
				.ToArray();

			var bare = () => t
				.Select(r => r.InTicks + r.UndeclaredSeconds)
				.ToArray();

			forced.ShouldThrow<LinqToDBException>().Message.ShouldContain(ErrorHelper.Error_Interval_UndeclaredOperand);
			bare  .ShouldThrow<LinqToDBException>().Message.ShouldContain(ErrorHelper.Error_Interval_UndeclaredOperand);
		}

		/// <summary>
		/// The same pairing, compared rather than combined.
		/// </summary>
		/// <remarks>
		/// A comparison is where the two stored numbers meet most quietly - nothing about
		/// <c>InSeconds == Undeclared</c> reads as arithmetic - and the generic handling compared 5400 against
		/// 54000000000 for the same ninety minutes, answering no rows where the CLR answers every one. The
		/// arithmetic refused this pairing while the comparison registered beside it, over the same two type pairs
		/// and the same operand shapes, let it through.
		/// <para>
		/// Refused whether or not SQL is demanded, as the combination is and for the reason it gives: the error is
		/// the pairing itself, so there is no half of it left for a projection to read the two sides through.
		/// </para>
		/// </remarks>
		[Test]
		public void ADeclaredDurationRefusesComparisonWithAnUndeclaredOne([DataSources] string context)
		{
			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, TimeSpan.FromMinutes(90));

			var filtered  = () => t.Where (r => r.InSeconds == r.Undeclared).ToArray();
			var projected = () => t.Select(r => r.InSeconds == r.Undeclared).ToArray();

			filtered .ShouldThrow<LinqToDBException>().Message.ShouldContain(ErrorHelper.Error_Interval_UndeclaredOperand);
			projected.ShouldThrow<LinqToDBException>().Message.ShouldContain(ErrorHelper.Error_Interval_UndeclaredOperand);
		}

		/// <summary>
		/// Both refused pairings above, projected from an operand of a set operation rather than from a terminal
		/// <c>Select</c>.
		/// </summary>
		/// <remarks>
		/// A set operand's projection is built asking translators for strict SQL, so it is the one place where a
		/// refusal raised regardless of the flags can be mistaken for one raised only because SQL was demanded.
		/// Read that way it is swallowed and the generic handling answers instead - the comparison came back
		/// <c>False</c> where the CLR answers <c>True</c>, and the sum came back 01:30:00.0005400 for three hours -
		/// which is exactly the silent wrong duration the refusals exist to stop, now inside a query shape that
		/// looks no different from any other.
		/// </remarks>
		[Test]
		public void ARefusedPairingStaysRefusedInsideASetOperation([DataSources] string context)
		{
			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, TimeSpan.FromMinutes(90));

			var compared = () => t.Where(r => r.Id >  0).Select(r => r.InSeconds == r.Undeclared)
				.Concat  (t.Where(r => r.Id <= 0).Select(r => r.InSeconds == r.Undeclared))
				.ToArray();

			var combined = () => t.Where(r => r.Id >  0).Select(r => r.InTicks + r.UndeclaredSeconds)
				.Concat  (t.Where(r => r.Id <= 0).Select(r => r.InTicks + r.UndeclaredSeconds))
				.ToArray();

			compared.ShouldThrow<LinqToDBException>().Message.ShouldContain(ErrorHelper.Error_Interval_UndeclaredOperand);
			combined.ShouldThrow<LinqToDBException>().Message.ShouldContain(ErrorHelper.Error_Interval_UndeclaredOperand);
		}

		/// <summary>
		/// The pairing the one above left alone: two durations stored through hand-written converters that do not
		/// agree about what their numbers count.
		/// </summary>
		/// <remarks>
		/// Neither column declares a unit, so nothing in the model says that <c>Undeclared</c> holds 54000000000 and
		/// <c>UndeclaredSeconds</c> holds 5400 for the same ninety minutes. Combined as they stand the sum is read
		/// back through whichever descriptor the walk reaches first, which answered 01:30:00.0005400 for three hours
		/// - the seconds column's 5400 taken as ticks. Compared as they stand, 54000000000 against 5400 answered no
		/// rows where the CLR answers one.
		/// <para>
		/// Both operands are named in the message, because the query text shows two durations being added and gives
		/// no hint that the two columns are stored differently: the mistake is in the model.
		/// </para>
		/// </remarks>
		[Test]
		public void TwoDisagreeingConvertersRefuseToCombine([DataSources] string context)
		{
			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, TimeSpan.FromMinutes(90));

			var expected = string.Format(
				CultureInfo.InvariantCulture,
				ErrorHelper.Error_ValueConverter_DivergentOperands,
				nameof(DurationRow.Undeclared),
				nameof(DurationRow.UndeclaredSeconds));

			var combined = () => t
				.Select(r => Sql.AsSql(r.Undeclared + r.UndeclaredSeconds))
				.ToArray();

			var compared = () => t
				.Where(r => r.Undeclared == r.UndeclaredSeconds)
				.ToArray();

			combined.ShouldThrow<LinqToDBException>().Message.ShouldContain(expected);
			compared.ShouldThrow<LinqToDBException>().Message.ShouldContain(expected);
		}

		/// <summary>
		/// The same two columns where the combination need not be expressed in SQL, and the three pairings that are
		/// not refused at all.
		/// </summary>
		/// <remarks>
		/// A projection is the one position with somewhere else to do the work: each column is read on its own terms
		/// and the two durations are added by the reader, which is exact. So the refusal is scoped to where SQL is
		/// genuinely required rather than raised at the operator - which is where this differs from the
		/// declared/undeclared pairing above, refused in both positions because its error is raised by the translator,
		/// ahead of the client-side fallback.
		/// <para>
		/// The three controls are the shapes the check must not touch: one column paired with itself, where the two
		/// descriptors are the same object; two columns declaring the same unit, whose converters are derived rather
		/// than written; and a plain value, which is written through the column's own converter and therefore arrives
		/// in that column's terms.
		/// </para>
		/// </remarks>
		[Test]
		public void TwoDisagreeingConvertersStillCombineInDotNet([DataSources] string context)
		{
			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, TimeSpan.FromMinutes(90));

			var row = t
				.Select(r => new
				{
					Divergent  = r.Undeclared + r.UndeclaredSeconds,
					SameColumn = Sql.AsSql(r.Undeclared + r.Undeclared),
					SameUnit   = Sql.AsSql(r.InSeconds  + r.InSeconds),
					PlainValue = Sql.AsSql(r.UndeclaredSeconds + TimeSpan.FromMinutes(30)),
				})
				.Single();

			row.Divergent.ShouldBe(TimeSpan.FromHours(3));
			row.SameColumn.ShouldBe(TimeSpan.FromHours(3));
			row.SameUnit.ShouldBe(TimeSpan.FromHours(3));
			row.PlainValue.ShouldBe(TimeSpan.FromHours(2));
		}

		// TimeSpan / TimeSpan arrived in .NET Core 3.0; on net462 the operator does not exist to be translated.
#if !NETFRAMEWORK
		[Table]
		sealed class RatioRow
		{
			[Column]                            public int      Id      { get; set; }
			[Column(DataType = DataType.Int64)]
			[Duration(DurationUnit.Second)]     public TimeSpan Half    { get; set; }
			[Column(DataType = DataType.Int64)]
			[Duration(DurationUnit.Second)]     public TimeSpan Whole   { get; set; }
			[Column(DataType = DataType.Int64)]
			[Duration(DurationUnit.Millisecond)] public TimeSpan WholeMs { get; set; }
		}

		/// <summary>
		/// One duration divided by another, which answers how many of the second fit in the first.
		/// </summary>
		/// <remarks>
		/// Two ways to get this wrong, and the storage supplies both. Declared in different units the stored numbers
		/// count different things, so 1800 seconds over 1800000 milliseconds answered a thousandth of the truth -
		/// and because both are integral the provider divided them as integers, so what came back was not 0.001 but
		/// 0. Declared in the <em>same</em> unit the numbers are commensurable and it is still wrong for the second
		/// reason alone: fifteen minutes over thirty is a half, which integer division reports as none.
		/// <para>
		/// Scoped to the two providers whose duration storage is a 64-bit integer. Where a provider has none - Access
		/// keeps a duration as money - a tick count is not a whole number and the ratio is refused rather than
		/// answered, which is the trade the sum already makes.
		/// </para>
		/// </remarks>
		[Test]
		public void OneDurationDividedByAnother([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<RatioRow>();

			db.Insert(new RatioRow
			{
				Id      = 1,
				Half    = TimeSpan.FromMinutes(15),
				Whole   = TimeSpan.FromMinutes(30),
				WholeMs = TimeSpan.FromMinutes(30),
			});

			var row = t.Select(r => new
			{
				CrossUnit = Sql.AsSql(r.Whole / r.WholeMs),
				SameUnit  = Sql.AsSql(r.Half  / r.Whole),
			}).Single();

			row.CrossUnit.ShouldBe(1d);
			row.SameUnit .ShouldBe(0.5d);
		}
#endif

		[Table]
		sealed class NativeIntervalRow
		{
			[Column] public int      Id         { get; set; }
			[Column] public TimeSpan Span       { get; set; }
			[Column] public DateTime StartedOn  { get; set; }
			[Column] public DateTime FinishedOn { get; set; }
		}

		/// <summary>
		/// A <see cref="TimeSpan"/> column that declared no unit and carries no converter either, paired with a
		/// computed difference.
		/// </summary>
		/// <remarks>
		/// The refusal above is for a stored number whose meaning only a hand-written converter knows. A column with
		/// no converter is a different case: on a provider with a native interval type it holds an interval, and
		/// adding an elapsed difference to it is <c>interval + interval</c> - a question the database answers. Under
		/// the refusal it was declined by a message naming a converter the model does not have, and the projection
		/// lost the .NET fallback with it.
		/// <para>
		/// PostgreSQL, because the assertion is that the query <em>answers</em>, and that is where a bare
		/// <see cref="TimeSpan"/> maps to a native interval. Both a demanded projection and a predicate are asserted:
		/// the refusal reached them by different routes, one falling back to .NET and one failing the query whole.
		/// </para>
		/// </remarks>
		[Test]
		public void AnUndeclaredDurationWithNoConverterKeepsItsMeaning([IncludeDataSources(false, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[]
			{
				new NativeIntervalRow
				{
					Id         = 1,
					Span       = TimeSpan.FromMinutes(30),
					StartedOn  = new DateTime(2020, 1, 1, 10, 0, 0),
					FinishedOn = new DateTime(2020, 1, 1, 11, 0, 0),
				},
			});

			t.Select(r => Sql.AsSql(r.Span + (r.FinishedOn - r.StartedOn))).Single().ShouldBe(TimeSpan.FromMinutes(90));
			t.Count(r => r.Span + (r.FinishedOn - r.StartedOn) > TimeSpan.FromMinutes(60)).ShouldBe(1);
		}

		/// <summary>
		/// A duration paired with an ordinary <see cref="TimeSpan"/> value rather than with another stored one.
		/// </summary>
		/// <remarks>
		/// The two pairings reach the value differently, and only one of them had anywhere to reach.
		/// <para>
		/// Beside a <em>declared column</em> the value is written through that column's own converter - it goes into
		/// the column's unit and the column stays bare, which is both correct and the shape an index can still be
		/// walked by. Nothing here changes that, and it is asserted so that nothing starts to.
		/// </para>
		/// <para>
		/// Beside a <em>computed difference</em> there is no column and no converter, so the value used to reach the
		/// statement as whatever the provider maps a bare <see cref="TimeSpan"/> to, set against a tick count. That
		/// was loud rather than wrong - SQL Server called it an operand type clash, SQLite refused the cast - but it
		/// refused an ordinary query, and on a provider with a native interval type the same query answered. It is
		/// counted in ticks now, the way the comparison path counts one.
		/// </para>
		/// <para>
		/// Both orders and both operators, since the value sits on the near side in one and the far side in the
		/// other, and subtraction is where a sign can go missing.
		/// </para>
		/// </remarks>
		[Test]
		// Access refuses by name rather than generically: the difference is asked for its tick total, which is the
		// one thing it has no way to produce, so the refusal comes from the member rather than from the conversion.
		[ThrowsForProvider(typeof(LinqToDBException), NoTickTotalProviders, ErrorMessage = ErrorHelper.Error_Interval_Member)]
		[ThrowsCannotBeConverted(UnsupportedDifferenceProviders)]
		public void ADurationCombinesWithAPlainValue([DataSources(false)] string context)
		{
			var taken  = TimeSpan.FromHours(1);
			var budget = TimeSpan.FromHours(3);
			var extra  = TimeSpan.FromMinutes(5);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();
			SeedTasks(db, (taken, budget));

			var row = t
				.Select(r => new
				{
					ColumnPlusValue     = Sql.AsSql(r.Budget + extra),
					DifferencePlusValue = Sql.AsSql((r.FinishedOn - r.StartedOn) + extra),
					ValuePlusDifference = Sql.AsSql(extra + (r.FinishedOn - r.StartedOn)),
					DifferenceLessValue = Sql.AsSql((r.FinishedOn - r.StartedOn) - extra),
				})
				.Single();

			row.ColumnPlusValue.ShouldBe(budget + extra);
			row.DifferencePlusValue.ShouldBe(taken + extra);
			row.ValuePlusDifference.ShouldBe(taken + extra);
			row.DifferenceLessValue.ShouldBe(taken - extra);
		}

		/// <summary>
		/// A date shifted by a duration that was computed rather than stored.
		/// </summary>
		/// <remarks>
		/// The composition the two cases above do not reach: their result is read, here it is spent. A shift takes
		/// its amount from a declared column everywhere else in this fixture, so nothing said whether an amount that
		/// was reconciled from two units is still one a shift can take.
		/// <para>
		/// It is, and the refusals line up with the ones a shift already declares - no provider answers this wrongly,
		/// and the ones that decline are exactly those that decline any shift. Asked in both forms for the reason the
		/// combination cases give: plain, the value is what a caller reads however it was arrived at.
		/// </para>
		/// <para>
		/// Subtracting the computed amount is asked alongside adding it, since the sign is applied to a value the
		/// shift lowering never saw built.
		/// </para>
		/// </remarks>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedShiftProviders, ErrorMessage = ErrorHelper.Error_Interval_Shift)]
		[ThrowsCannotBeConverted(ShiftRefusedWhileBuildingProviders)]
		public void ADateShiftsByAComputedDuration([DataSources(false)] string context)
		{
			var taken  = TimeSpan.FromHours(1);
			var budget = TimeSpan.FromHours(3);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();
			SeedTasks(db, (taken, budget));

			// Read rather than assumed, so the case does not depend on how the seed picks its start.
			var started = t.Select(r => r.StartedOn).Single();

			var row = t
				.Select(r => new Shifted
				{
					Forward  = r.StartedOn + ((r.FinishedOn - r.StartedOn) + r.Budget),
					Lopsided = r.StartedOn + ((r.Budget + r.Budget) - (r.FinishedOn - r.StartedOn)),
					Backward = r.StartedOn - ((r.FinishedOn - r.StartedOn) + r.Budget),
				})
				.Single();

			row.Forward.ShouldBe(started + taken + budget);
			row.Lopsided.ShouldBe(started + budget + budget - taken);
			row.Backward.ShouldBe(started - taken - budget);
		}

		/// <summary>
		/// The same shift asked in SQL, where a provider that cannot measure a difference has nothing to fall back
		/// to.
		/// </summary>
		/// <remarks>
		/// The forced half of the case above, and its own method for the reason the combination cases give: the two
		/// do not refuse on the same providers. Informix answers the plain form from .NET and refuses this one, so a
		/// shared set of gates would be wrong about one of them.
		/// </remarks>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedShiftProviders, ErrorMessage = ErrorHelper.Error_Interval_Shift)]
		[ThrowsCannotBeConverted(ShiftRefusedWhileBuildingProviders + "," + UnsupportedDifferenceProviders)]
		public void ADateShiftsByAComputedDurationInSql([DataSources(false)] string context)
		{
			var taken  = TimeSpan.FromHours(1);
			var budget = TimeSpan.FromHours(3);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();
			SeedTasks(db, (taken, budget));

			var started = t.Select(r => r.StartedOn).Single();

			var row = t
				.Select(r => new Shifted
				{
					Forward  = Sql.AsSql(r.StartedOn + ((r.FinishedOn - r.StartedOn) + r.Budget)),
					Lopsided = Sql.AsSql(r.StartedOn + ((r.Budget + r.Budget) - (r.FinishedOn - r.StartedOn))),
					Backward = Sql.AsSql(r.StartedOn - ((r.FinishedOn - r.StartedOn) + r.Budget)),
				})
				.Single();

			row.Forward.ShouldBe(started + taken + budget);
			row.Lopsided.ShouldBe(started + budget + budget - taken);
			row.Backward.ShouldBe(started - taken - budget);
		}

		/// <summary>
		/// Carries what the two shift cases above assert, so both share one set of assertions.
		/// </summary>
		sealed class Shifted
		{
			public DateTime Forward  { get; set; }
			public DateTime Lopsided { get; set; }
			public DateTime Backward { get; set; }
		}

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
		[ThrowsForProvider(typeof(LinqToDBException), NoTickTotalProviders, ErrorMessage = ErrorHelper.Error_Interval_Operation)]
		public void DurationsInDifferentUnitsCombineAsDurations([DataSources] string context, [Values] bool inSql)
		{
			var value = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			// The two shapes differ only in whether the arithmetic is forced into SQL. A bare column on each side
			// states the rule; the rest are the shapes a query actually takes - an operand that is itself computed,
			// one that arrives negated, one chosen by a condition, and both sides computed at once. The nested case
			// additionally says that what this produces is usable as an operand again, and the lopsided subtraction
			// that two sides of equal magnitude cannot cancel a wrong scaling into a right answer.
			// Conditional's two arms are the same column on purpose: the shape under test is a conditional
			// whose arms carry the same interval unit, so differing arms would be a different test
#pragma warning disable MA0140 // Both if and else branch have identical code
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
#pragma warning restore MA0140

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
		[ThrowsForProvider(typeof(LinqToDBException), NoTickTotalProviders, ErrorMessage = ErrorHelper.Error_Interval_Operation)]
		public void ADifferenceAndADeclaredDurationCombineAsDurations([DataSources(false)] string context)
		{
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
		/// a provider that translated none of it. Informix reaches exactly that.
		/// <para>
		/// The subtraction is lopsided and one case is negative, for the reason the sibling above gives.
		/// </para>
		/// </remarks>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), NoTickTotalProviders, ErrorMessage = ErrorHelper.Error_Interval_Operation)]
		[ThrowsCannotBeConverted(UnsupportedDifferenceProviders)]
		public void ADifferenceAndADeclaredDurationCombineInSql([DataSources(false)] string context)
		{
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

			// Only the cancelling forms here, and every provider answers them - by one of two routes. Where the
			// difference can be lowered, the optimizer folds start + (end - start) back to end, so the statement
			// carries neither the difference nor the shift and the provider is asked for nothing: SQLite selects
			// the two columns and reads the hour off one of them. Where it cannot - Access, which leaves
			// CanLowerIntervalDifference false - the difference is never built, so there is nothing to fold and
			// the projection is answered in .NET from the two columns.
			//
			// A shift off an unrelated base needs real lowering and is tested separately.
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
		/// The same shift, taking its amount from a declared column rather than from a computed difference.
		/// </summary>
		/// <remarks>
		/// The sibling above carries the amount as a tick count reconciled from a difference, which is the other
		/// branch of the shift entirely - <see cref="LinqToDB.Linq.Translation.TranslationProviderFlags.CanLowerIntervalShift"/> is read for a
		/// shift by a <em>declared</em> duration only, and nothing asked for that in SQL. Every other declared-shift
		/// case is a plain projection, which falls back to .NET and answers correctly on every provider whether the
		/// lowering happened or not: they cannot tell "lowered" from "quietly not lowered".
		/// <para>
		/// So this is where the two halves become visible. The providers named decline by name, and the rest have to
		/// answer from SQL - and answer with the column's own unit, since ninety minutes read as ninety million
		/// ticks is half a millisecond and would not clear the bound.
		/// </para>
		/// <para>
		/// Local contexts only, for the reason the sibling gives: a remote context wraps the refusal in a transport
		/// exception that says nothing about the translation.
		/// </para>
		/// </remarks>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDeclaredShiftProviders, ErrorMessage = ErrorHelper.Error_Interval_Shift)]
		public void ADeclaredDurationShiftIsExpressedInAPredicate([DataSources(false)] string context)
		{
			var value = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			t
				.Where(r => ShiftOrigin + r.InSeconds > ShiftOrigin.AddHours(1))
				.Select(r => r.Id)
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
			[IncludeDataSources(true,
				TestProvName.AllSqlServer, TestProvName.AllPostgreSQL, TestProvName.AllMySql, TestProvName.AllDuckDB,
				TestProvName.AllSQLite, TestProvName.AllFirebird, TestProvName.AllYdb, TestProvName.AllOracle)] string context)
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
		/// The providers whose own lowering spends a computed difference on a date, pinned by the cases below.
		/// </summary>
		const string ComputedShiftProviders =
			TestProvName.AllSQLite   + "," +
			TestProvName.AllFirebird + "," +
			TestProvName.AllOracle   + "," +
			TestProvName.AllYdb;

		/// <summary>
		/// A shift by a computed difference keeps an amount below a millisecond wherever the storage holds one.
		/// </summary>
		/// <remarks>
		/// Firebird stores a tenth of a millisecond, Oracle and YDB a microsecond, so a millisecond and a half has to
		/// arrive intact. Truncated to a whole millisecond it lands on the bound of the predicate and drops the row.
		/// SQLite and Firebird 2.5 measure the difference in whole milliseconds to begin with and are not asked.
		/// </remarks>
		[Test]
		public void AComputedShiftKeepsASubMillisecondAmount(
			[IncludeDataSources(TestProvName.AllFirebird3Plus, TestProvName.AllOracle, TestProvName.AllYdb)] string context)
		{
			var started = new DateTime(2026, 1, 1, 10, 0, 0);
			var amount  = TimeSpan.FromTicks(15_000);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = started, FinishedOn = started + amount });

			t
				.Select(r => Sql.AsSql(ShiftOrigin + (r.FinishedOn - r.StartedOn)))
				.Single()
				.ShouldBe(ShiftOrigin + amount);

			t
				.Where(r => ShiftOrigin + (r.FinishedOn - r.StartedOn) > ShiftOrigin.AddMilliseconds(1))
				.Select(r => r.Id)
				.ToArray()
				.ShouldBe([1]);
		}

		/// <summary>
		/// A shift by a computed difference longer than 2<sup>31</sup> seconds, a little over 68 years, in both
		/// directions.
		/// </summary>
		/// <remarks>
		/// A second count of that size no longer fits a 32-bit amount, which is what an interval built from seconds
		/// alone runs into. The dates stay inside every provider's range: YDB's timestamp starts in 1970 and ends
		/// before 2106.
		/// </remarks>
		[Test]
		public void AComputedShiftSpansMoreThanSixtyEightYears([IncludeDataSources(ComputedShiftProviders)] string context)
		{
			var started  = new DateTime(1980, 1, 1,  0, 0, 0);
			var finished = new DateTime(2060, 1, 1, 12, 0, 0);
			var early    = new DateTime(1971, 1, 1);
			var late     = new DateTime(2100, 1, 1);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = started, FinishedOn = finished });

			var row = t
				.Select(r => new
				{
					Forward  = Sql.AsSql(early + (r.FinishedOn - r.StartedOn)),
					Backward = Sql.AsSql(late  - (r.FinishedOn - r.StartedOn)),
				})
				.Single();

			row.Forward.ShouldBe(early + (finished - started));
			row.Backward.ShouldBe(late - (finished - started));
		}

		/// <summary>
		/// A shift by a computed difference of three thousand years, in both directions.
		/// </summary>
		/// <remarks>
		/// Far past what an amount of ticks can be widened by before it overflows a fixed-point type of eighteen
		/// digits. YDB is not asked: its timestamp covers 1970 to 2105 only. The dates stay after 1582, before which
		/// Oracle counts in the Julian calendar and .NET does not.
		/// </remarks>
		[Test]
		public void AComputedShiftSpansMillennia(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllFirebird, TestProvName.AllOracle)] string context)
		{
			var started  = new DateTime(1600, 1, 1);
			var finished = new DateTime(4700, 1, 1, 12, 0, 0);
			var early    = new DateTime(1650, 1, 1);
			var late     = new DateTime(8000, 1, 1);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = started, FinishedOn = finished });

			var row = t
				.Select(r => new
				{
					Forward  = Sql.AsSql(early + (r.FinishedOn - r.StartedOn)),
					Backward = Sql.AsSql(late  - (r.FinishedOn - r.StartedOn)),
				})
				.Single();

			row.Forward.ShouldBe(early + (finished - started));
			row.Backward.ShouldBe(late - (finished - started));
		}

		[Table]
		sealed class ShiftTargetRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			public DateTime StartedOn  { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			public DateTime FinishedOn { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			public DateTime Due        { get; set; }
		}

		/// <summary>
		/// A shift by a computed difference written to a column that cannot be null.
		/// </summary>
		/// <remarks>
		/// YQL types the sum of a timestamp and an interval as optional whatever its operands, and refuses to write an
		/// optional to a column declared not null, so the value has to arrive in the column's own type.
		/// </remarks>
		[Test]
		public void AComputedShiftIsWrittenByAnUpdate([IncludeDataSources(ComputedShiftProviders)] string context)
		{
			var started = new DateTime(2026, 1, 1, 10, 0, 0);
			var amount  = new TimeSpan(0, 5, 30, 0, 250);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<ShiftTargetRow>();

			db.Insert(new ShiftTargetRow { Id = 1, StartedOn = started, FinishedOn = started + amount, Due = started });

			t
				.Where(r => r.Id == 1)
				.Set(r => r.Due, r => ShiftOrigin + (r.FinishedOn - r.StartedOn))
				.Update();

			t.Select(r => r.Due).Single().ShouldBe(ShiftOrigin + amount);
		}

		[Table]
		sealed class DatedEventRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.Date)]
			public DateTime Day { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			public DateTime StartedOn  { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			public DateTime FinishedOn { get; set; }
		}

		/// <summary>
		/// A date column shifted by a computed difference keeps the time of day the difference adds.
		/// </summary>
		/// <remarks>
		/// A date type has no time part, so a shift that stays in it drops the hours - and, where it keeps seconds
		/// but no fraction of one, the milliseconds. The amount carries both.
		/// </remarks>
		[Test]
		public void AComputedShiftOfADateColumnKeepsTheTime([IncludeDataSources(ComputedShiftProviders)] string context)
		{
			var day     = new DateTime(2026, 3, 1);
			var started = new DateTime(2026, 1, 1, 10, 0, 0);
			var amount  = new TimeSpan(0, 5, 30, 0, 250);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<DatedEventRow>();

			db.Insert(new DatedEventRow { Id = 1, Day = day, StartedOn = started, FinishedOn = started + amount });

			t
				.Select(r => Sql.AsSql(r.Day + (r.FinishedOn - r.StartedOn)))
				.Single()
				.ShouldBe(day + amount);

			// Compared, the shifted value has to keep the time as well: read back as a date on either side, it
			// equals the date it started from and the row is dropped.
			t
				.Where(r => r.Day + (r.FinishedOn - r.StartedOn) > r.Day)
				.Select(r => r.Id)
				.ToArray()
				.ShouldBe([1]);
		}

		[Table]
		sealed class DatedEventAsDateTimeRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column]
			public DateTime Day { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			public DateTime StartedOn  { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			public DateTime FinishedOn { get; set; }
		}

		[Table]
		sealed class DatedEventWithDbTypeRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.Date, DbType = "Date")]
			public DateTime Day { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			public DateTime StartedOn  { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			public DateTime FinishedOn { get; set; }
		}

		/// <summary>
		/// The date column of <see cref="AComputedShiftOfADateColumnKeepsTheTime"/>, read through the two mappings
		/// that do not declare it as a date alone.
		/// </summary>
		/// <remarks>
		/// A plain <see cref="DateTime"/> - what a scaffolder writes - declares nothing, so a widening keyed on the
		/// declared type never sees a date; and a declared <c>DbType</c>, carried into a cast built from the declared
		/// type, renders that cast as the date it was meant to leave. Either way the shift stays in the date type and
		/// drops the time it adds.
		/// </remarks>
		[Test]
		public void AComputedShiftOfADateColumnKeepsTheTimeWhateverTheMapping(
			[IncludeDataSources(ComputedShiftProviders + "," + TestProvName.AllSqlServer2008Plus)] string context)
		{
			var day     = new DateTime(2026, 3, 1);
			var started = new DateTime(2026, 1, 1, 10, 0, 0);
			var amount  = new TimeSpan(0, 5, 30, 0, 250);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<DatedEventRow>();

			db.Insert(new DatedEventRow { Id = 1, Day = day, StartedOn = started, FinishedOn = started + amount });

			var plain = db.GetTable<DatedEventAsDateTimeRow>().TableName(t.TableName);

			plain
				.Select(r => Sql.AsSql(r.Day + (r.FinishedOn - r.StartedOn)))
				.Single()
				.ShouldBe(day + amount);

			plain
				.Where(r => r.Day + (r.FinishedOn - r.StartedOn) > r.Day)
				.Select(r => r.Id)
				.ToArray()
				.ShouldBe([1]);

			var withDbType = db.GetTable<DatedEventWithDbTypeRow>().TableName(t.TableName);

			withDbType
				.Select(r => Sql.AsSql(r.Day + (r.FinishedOn - r.StartedOn)))
				.Single()
				.ShouldBe(day + amount);

			withDbType
				.Where(r => r.Day + (r.FinishedOn - r.StartedOn) > r.Day)
				.Select(r => r.Id)
				.ToArray()
				.ShouldBe([1]);
		}

		[Table]
		sealed class CoarseShiftRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.DateTime)]
			public DateTime OnDateTime { get; set; }

			[Column(DataType = DataType.SmallDateTime)]
			public DateTime OnSmall { get; set; }

			[Column(DataType = DataType.Date)]
			public DateTime OnDate { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			public DateTime StartedOn  { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			public DateTime FinishedOn { get; set; }
		}

		/// <summary>
		/// A SQL Server <c>datetime</c>, <c>smalldatetime</c> and <c>date</c> shifted by a computed difference with a
		/// part below the millisecond.
		/// </summary>
		/// <remarks>
		/// The shift spends the part below a second through <c>DATEADD(nanosecond, ...)</c>, which SQL Server refuses
		/// on all three types, and the last two could not hold the time of day it adds anyway. Each starting value is
		/// one the type stores exactly, so the answer is the CLR one to the tick.
		/// </remarks>
		[Test]
		public void AComputedShiftOfACoarseSqlServerType([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var on      = new DateTime(2026, 3, 1, 10, 0, 0);
			var started = new DateTime(2026, 1, 1, 10, 0, 0);
			var amount  = new TimeSpan(0, 5, 30, 0, 250) + TimeSpan.FromTicks(1234);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<CoarseShiftRow>();

			db.Insert(new CoarseShiftRow { Id = 1, OnDateTime = on, OnSmall = on, OnDate = on.Date, StartedOn = started, FinishedOn = started + amount });

			var row = t
				.Select(r => new
				{
					FromDateTime = Sql.AsSql(r.OnDateTime + (r.FinishedOn - r.StartedOn)),
					FromSmall    = Sql.AsSql(r.OnSmall    + (r.FinishedOn - r.StartedOn)),
					FromDate     = Sql.AsSql(r.OnDate     + (r.FinishedOn - r.StartedOn)),
				})
				.Single();

			row.FromDateTime.ShouldBe(on + amount);
			row.FromSmall.ShouldBe(on + amount);
			row.FromDate.ShouldBe(on.Date + amount);

			t
				.Where(r => r.OnDateTime + (r.FinishedOn - r.StartedOn) > r.OnDateTime.AddHours(5))
				.Select(r => r.Id)
				.ToArray()
				.ShouldBe([1]);
		}

		/// <summary>
		/// A column shifted by the difference of two client values, in SQL and in a predicate, both ways.
		/// </summary>
		/// <remarks>
		/// The difference itself is the client's to compute, but it has to reach the shift as a duration. Handed over as
		/// a bare <see cref="TimeSpan"/> it carries no unit, and the shift became a plain <c>+</c> between a date and a
		/// time, which SQL Server refuses. The providers that cannot shift a date by an amount at all are not asked.
		/// </remarks>
		[Test]
		public void AShiftOfAColumnByADifferenceOfClientValues([DataSources(UnsupportedDeclaredShiftProviders)] string context)
		{
			var earlier = new DateTime(2026, 1, 3, 13, 30, 0);
			var later   = earlier.AddHours(1).AddMilliseconds(250);
			var started = new DateTime(2026, 1, 1, 10, 0, 0);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<EventRow>();

			db.Insert(new EventRow { Id = 1, StartedOn = started, FinishedOn = started.AddHours(2) });

			t.Select(r => Sql.AsSql(r.StartedOn + (later - earlier))).Single().ShouldBe(started + (later - earlier));
			t.Select(r => Sql.AsSql(r.FinishedOn - (later - earlier))).Single().ShouldBe(started.AddHours(2) - (later - earlier));

			t.Where(r => r.StartedOn + (later - earlier) < r.FinishedOn).Select(r => r.Id).ToArray().ShouldBe([1]);
			t.Where(r => r.FinishedOn - (later - earlier) > r.StartedOn.AddHours(1)).Select(r => r.Id).ToArray().ShouldBeEmpty();
		}

		[Table]
		sealed class WideTimestampDeclaredRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.Timestamp64)]
			public DateTime On { get; set; }

			[Column(DataType = DataType.Date32)]
			public DateTime Day32 { get; set; }

			[Column(DataType = DataType.DateTime64)]
			public DateTime On64 { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			public DateTime StartedOn  { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			public DateTime FinishedOn { get; set; }
		}

		[Table]
		sealed class WideTimestampRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DbType = "Timestamp64")]
			public DateTime On { get; set; }

			[Column(DbType = "Date32")]
			public DateTime Day32 { get; set; }

			[Column(DbType = "Datetime64")]
			public DateTime On64 { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			public DateTime StartedOn  { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			public DateTime FinishedOn { get; set; }
		}

		/// <summary>
		/// YDB's 64-bit date types declared through their <c>DbType</c> alone, shifted by a computed difference.
		/// </summary>
		/// <remarks>
		/// Such a column is typed as a plain timestamp, and widening it like one would cast it to a
		/// <c>Timestamp</c>, which starts in 1970 and cannot hold the value. Left as it is, a <c>Date32</c> or a
		/// <c>Datetime64</c> would drop the time of day or the fraction of a second the shift adds; all three go to
		/// <c>Timestamp64</c>. The row is written through a mapping that
		/// declares the data type: the DbType-only one would bind the 1960 parameter as a <c>Timestamp</c> too.
		/// </remarks>
		[Test]
		public void AComputedShiftOfAWideTimestampKeepsItsRange([IncludeDataSources(TestProvName.AllYdb)] string context)
		{
			var on      = new DateTime(1960, 3, 1, 8, 0, 0);
			var started = new DateTime(2026, 1, 1, 10, 0, 0);
			var amount  = new TimeSpan(0, 5, 30, 0, 250);

			using var db = GetDataContext(context);
			using var declared = db.CreateLocalTable<WideTimestampDeclaredRow>();

			db.Insert(new WideTimestampDeclaredRow { Id = 1, On = on, Day32 = on.Date, On64 = on, StartedOn = started, FinishedOn = started + amount });

			var t = db.GetTable<WideTimestampRow>().TableName(declared.TableName);

			var row = t
				.Select(r => new
				{
					On    = Sql.AsSql(r.On    + (r.FinishedOn - r.StartedOn)),
					Day32 = Sql.AsSql(r.Day32 + (r.FinishedOn - r.StartedOn)),
					On64  = Sql.AsSql(r.On64  + (r.FinishedOn - r.StartedOn)),
				})
				.Single();

			row.On.ShouldBe(on + amount);
			row.Day32.ShouldBe(on.Date + amount);
			row.On64.ShouldBe(on + amount);

			t.Where(r => r.On    + (r.FinishedOn - r.StartedOn) > r.On).Select(r => r.Id).ToArray().ShouldBe([1]);
			t.Where(r => r.Day32 + (r.FinishedOn - r.StartedOn) > r.Day32).Select(r => r.Id).ToArray().ShouldBe([1]);
			t.Where(r => r.On64  + (r.FinishedOn - r.StartedOn) > r.On64.AddHours(5).AddMinutes(30)).Select(r => r.Id).ToArray().ShouldBe([1]);
		}

		[Table]
		sealed class SmallDateTimeShiftRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.SmallDateTime)]
			public DateTime OnSmall { get; set; }

			[Column(DataType = DataType.DateTime)]
			public DateTime StartedOn  { get; set; }

			[Column(DataType = DataType.DateTime)]
			public DateTime FinishedOn { get; set; }
		}

		/// <summary>
		/// A SQL Server <c>smalldatetime</c> shifted by a computed difference keeps the seconds the shift adds, on every
		/// version.
		/// </summary>
		/// <remarks>
		/// <c>DATEADD</c> returns a <c>smalldatetime</c> for one, which keeps no seconds. 2005 has no <c>datetime2</c>
		/// to widen to, so it widens to <c>datetime</c>; every value here is one a <c>datetime</c> stores exactly.
		/// </remarks>
		[Test]
		public void AComputedShiftOfASmallDateTimeKeepsItsSeconds([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			var on      = new DateTime(2020, 1, 1, 3, 0, 0);
			var started = new DateTime(2026, 1, 1, 10, 0, 0);
			var amount  = new TimeSpan(0, 5, 30, 15, 250);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<SmallDateTimeShiftRow>();

			db.Insert(new SmallDateTimeShiftRow { Id = 1, OnSmall = on, StartedOn = started, FinishedOn = started + amount });

			t.Select(r => Sql.AsSql(r.OnSmall + (r.FinishedOn - r.StartedOn))).Single().ShouldBe(on + amount);

			var bound = on + amount - TimeSpan.FromSeconds(1);

			t.Where(r => r.OnSmall + (r.FinishedOn - r.StartedOn) > bound).Select(r => r.Id).ToArray().ShouldBe([1]);
		}

		[Table]
		sealed class OptionalDueRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			[Column(Configuration = ProviderName.ClickHouse)]
			public DateTime? DueOn { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			[Column(Configuration = ProviderName.ClickHouse)]
			public DateTime StartedOn { get; set; }
		}

		/// <summary>
		/// A column shifted by the difference of two client values, written in its nullable spellings.
		/// </summary>
		/// <remarks>
		/// Over a nullable column the addition is lifted and the difference converted to a nullable
		/// <see cref="TimeSpan"/>; between two nullable locals the difference is one itself. Both are the shift
		/// <see cref="AShiftOfAColumnByADifferenceOfClientValues"/> asks, and an absent local makes the result absent.
		/// </remarks>
		[Test]
		public void AShiftByADifferenceOfClientValuesInANullableSpelling([DataSources(UnsupportedDeclaredShiftProviders)] string context)
		{
			var earlier = new DateTime(2026, 1, 3, 13, 30, 0);
			var later   = earlier.AddHours(1).AddMilliseconds(250);
			var started = new DateTime(2026, 1, 1, 10, 0, 0);

			DateTime? earlierN = earlier;
			DateTime? laterN   = later;
			DateTime? absent   = null;

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<OptionalDueRow>();

			db.Insert(new OptionalDueRow { Id = 1, DueOn = started, StartedOn = started });

			t.Select(r => Sql.AsSql(r.DueOn + (later - earlier))).Single().ShouldBe(started + (later - earlier));
			t.Select(r => Sql.AsSql(r.StartedOn + (laterN - earlierN))).Single().ShouldBe(started + (later - earlier));
			t.Select(r => Sql.AsSql(r.DueOn - (laterN - earlierN))).Single().ShouldBe(started - (later - earlier));
			t.Select(r => Sql.AsSql(r.StartedOn + (laterN - absent))).Single().ShouldBeNull();

			t.Where(r => r.DueOn + (later - earlier) > r.StartedOn.AddHours(1)).Select(r => r.Id).ToArray().ShouldBe([1]);
			t.Where(r => r.StartedOn + (laterN - earlierN) < r.StartedOn.AddHours(1)).Select(r => r.Id).ToArray().ShouldBeEmpty();
		}

		/// <summary>
		/// A <see cref="DateTimeOffset"/> shifted by a computed difference, answered as the same instant or refused.
		/// </summary>
		/// <remarks>
		/// SQLite refuses: its date functions work in UTC and write the result back without an offset, which one of
		/// its providers reads as local time and the other cannot read at all. Both offsets are the same here, so the
		/// difference itself is not what is being asked. Firebird is not asked, for the reason
		/// <see cref="SupportsDateTimeOffsetContextAttribute"/> gives: its client refuses the offset on write.
		/// </remarks>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSQLite, ErrorMessage = ErrorHelper.Error_Interval_Shift)]
		public void AComputedShiftOfADateTimeOffset(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle, TestProvName.AllYdb)] string context)
		{
			var started  = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.FromHours(2));
			var finished = started + new TimeSpan(0, 5, 30, 0, 250);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<ZonedEventRow>();

			db.Insert(new ZonedEventRow { Id = 1, StartedOn = started, FinishedOn = finished });

			t
				.Select(r => Sql.AsSql(r.FinishedOn + (r.FinishedOn - r.StartedOn)))
				.Single()
				.ShouldBe(finished + (finished - started));
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
