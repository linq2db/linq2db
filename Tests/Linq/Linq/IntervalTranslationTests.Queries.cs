using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	// A duration column reads back through a conversion derived from its declared unit, and an elapsed difference
	// reads back through whatever the lowering produces. Projecting one of those is the obvious place either can go
	// wrong and is covered elsewhere - these are the shapes where the value takes another route: through a
	// comparison, a grouping key, an aggregate, a sort key it is never read from, a membership test, a local
	// collection, a set operation, or the choice between being written into the statement and being passed to it.
	public partial class IntervalTranslationTests
	{
		/// <summary>
		/// Carries a date pair and a declared duration in one row, so a difference and a stored duration can be
		/// compared against each other - which is the case neither model alone can express.
		/// </summary>
		[Table]
		sealed class BudgetedTaskRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			[Column(Configuration = ProviderName.Access)]
			[Column(Configuration = ProviderName.ClickHouse)]
			public DateTime StartedOn  { get; set; }

			[Column(DataType = DataType.DateTime2, Precision = 7)]
			[Column(Configuration = ProviderName.Access)]
			[Column(Configuration = ProviderName.ClickHouse)]
			public DateTime FinishedOn { get; set; }

			[Column(DataType = DataType.Int64)]
			[Column(Configuration = Wide, DataType = DataType.Money)]
			[Duration(DurationUnit.Second)]
			public TimeSpan Budget { get; set; }
		}

		static void SeedTasks(IDataContext db, params (TimeSpan Taken, TimeSpan Budget)[] rows)
		{
			var started = new DateTime(2026, 1, 1, 10, 0, 0);

			for (var i = 0; i < rows.Length; i++)
			{
				db.Insert(new BudgetedTaskRow
				{
					Id         = i + 1,
					StartedOn  = started,
					FinishedOn = started + rows[i].Taken,
					Budget     = rows[i].Budget,
				});
			}
		}

		/// <summary>
		/// Why a branch holding a difference cannot meet one holding a declared duration on some providers.
		/// </summary>
		/// <remarks>
		/// A member the branches store differently is given a column each, so each is read the way its own branch
		/// stores it. That keeps the reader right, but it does not make the two storages meet: where the provider
		/// has a type of its own for elapsed time, a difference arrives in that type and a declared duration
		/// arrives as an integer, and the two still have to occupy one position of the set operation.
		/// <para>
		/// From there each provider fails its own way, which is why this is one gate rather than one fix. DB2, YDB
		/// and DuckDB refuse the statement - DuckDB says it plainly, <em>Unimplemented type for cast (BIGINT -&gt;
		/// INTERVAL)</em>, and DB2 (SQL0415N) and YDB (<em>Uncompatible member ... Optional&lt;Interval&gt; and
		/// Int64</em>) say the same in their own words. MySQL and MariaDB accept it and break on the way back
		/// instead: the column becomes a <c>TIME</c>, the tick count goes into it, and the driver cannot read
		/// 36000000000 back out as a <see cref="TimeSpan"/>.
		/// </para>
		/// <para>
		/// Not a defect in the splitting. Providers that hold elapsed time as a plain number run these shapes green
		/// - every Firebird version, ClickHouse, SQLite, SQL Server, PostgreSQL, Oracle - and on the refusing
		/// providers the same shape with two stored durations passes, because there both sides are integers.
		/// Answering it needs the branches brought to one representation before they meet, which is separate work.
		/// </para>
		/// </remarks>
		const string MixedStorageInASetOperation =
			"Where a provider has its own type for elapsed time, a difference and a declared duration reach it as "
			+ "different SQL types and cannot share one position of a set operation. DB2, YDB and DuckDB refuse the "
			+ "statement (DuckDB: 'Unimplemented type for cast (BIGINT -> INTERVAL)'); MySQL and MariaDB accept it "
			+ "and then cannot read the tick count back out of a TIME column. Giving each branch its own column "
			+ "keeps the reader right but cannot reconcile the two storages - that needs both branches projected to "
			+ "one representation first. Providers that hold elapsed time as a plain number run these shapes green.";

		/// <summary>
		/// Why a membership test still loses the unit where a comparison keeps it.
		/// </summary>
		/// <remarks>
		/// Units are reconciled while an expression is translated: a declared duration is wrapped so that it carries
		/// its unit with it, and two such wrapped values can then be brought to common terms. A comparison goes
		/// through that translation, which is why <c>InSeconds == InTicks</c> answers correctly. <c>Contains</c>
		/// builds its predicate itself, straight from the two SQL expressions, and never passes through it - so two
		/// columns are compared as the raw numbers they store, and a value taken from an expression with no declared
		/// unit at all reaches the provider in its CLR form.
		/// <para>
		/// The value-against-a-projected-column case is different and is fixed: there the sequence names one column,
		/// so the value can be built on that column's terms before any translation is needed. These two cannot be
		/// answered the same way - one has no constant to convert, the other has no unit to convert to.
		/// </para>
		/// </remarks>
		const string ContainsSkipsIntervalTranslation =
			"Contains builds its predicate from the two SQL expressions directly rather than through the translation "
			+ "that reconciles declared duration units, so two columns held in different units are compared as the raw "
			+ "numbers they store, and a candidate taken from an expression with no declared unit reaches the provider "
			+ "as a CLR TimeSpan. A comparison of the same two values answers correctly, which is where the reconciliation "
			+ "already lives.";

		/// <summary>
		/// Why a duration compared against a value longer than about three and a half minutes fails on Access.
		/// </summary>
		/// <remarks>
		/// Access has no 64-bit integer parameter, so its provider maps one to a 32-bit parameter and narrows the
		/// value with a checked cast. Anything above <see cref="int.MaxValue"/> throws while the parameter is being
		/// bound - before the statement executes, and before it reaches the trace, so the log says nothing about
		/// which query failed. A comparison is reconciled in ticks, and 2147483647 ticks is three minutes and
		/// thirty-four seconds, so every bound in these tests is far over the line.
		/// <para>
		/// Not an interval defect, and gated rather than declared for that reason: a plain captured <c>long</c>
		/// compared against an ordinary integer column throws exactly the same way, with no duration anywhere. It
		/// is also not declarable as a refusal - what escapes is a raw <see cref="OverflowException"/> rather than
		/// one of ours, and the failure follows the <em>value</em>, not the shape of the query: the same comparison
		/// with a hundred-second bound produces the same SQL and passes.
		/// </para>
		/// </remarks>
		const string AccessLongParameterOverflow =
			"Access maps a 64-bit integer parameter to a 32-bit one and narrows it with a checked cast, so any value "
			+ "above int.MaxValue throws OverflowException as the parameter is bound. A duration comparison travels in "
			+ "ticks, where int.MaxValue is three and a half minutes, so any longer bound fails. Not an interval "
			+ "defect - a plain long compared against an int column throws identically. See issue 5748.";

		// Here the duration is the difference between two dates. Most of these shapes are asked again in the block
		// that follows, over a column whose unit comes from its declaration, and the repetition is deliberate rather
		// than one parameterised pass: a column reaches the query as itself while a difference reaches it as lowered
		// arithmetic, so a grouping key, an aggregate or a sort key is applied to a computed expression here, and a
		// provider may refuse it in one of those positions even though the projection works. A shape only one of the
		// two models can express is asked once, in the block that can express it.

		/// <summary>
		/// A date difference compared against a duration value.
		/// </summary>
		/// <remarks>
		/// The difference lowers to ticks, so the bound has to arrive in ticks - a different conversion from the
		/// one a declared column needs, and reached by a different path.
		/// </remarks>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), NoTickTotalProviders, ErrorMessage = ErrorHelper.Error_Interval_Member)]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		public void DifferenceComparedToAValue([DataSources(false)] string context)
		{
			var bound = TimeSpan.FromHours(2);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();

			SeedTasks(db,
				(TimeSpan.FromHours(1), TimeSpan.FromHours(3)),
				(TimeSpan.FromHours(2), TimeSpan.FromHours(3)),
				(TimeSpan.FromHours(3), TimeSpan.FromHours(3)));

			var over = t
				.Where(r => r.FinishedOn - r.StartedOn > bound)
				.Select(r => r.Id)
				.ToArray();

			var atLeast = t
				.Where(r => r.FinishedOn - r.StartedOn >= bound)
				.Select(r => r.Id)
				.OrderBy(id => id)
				.ToArray();

			over.ShouldBe([3]);
			atLeast.ShouldBe([2, 3]);
		}

		/// <summary>
		/// A bound compared against a difference travels as a parameter unless it is asked to stand in the
		/// statement.
		/// </summary>
		/// <remarks>
		/// The bound is not sent as the duration it was written as: a difference arrives in ticks, so what reaches
		/// the statement is the bound's tick count. That is what makes this the discriminating place for either
		/// request - <c>Sql.Constant</c> and <c>Sql.Parameter</c> are written around the duration, and unless the
		/// request travels with the value onto the member actually sent, it is left sitting on an argument nothing
		/// reads any more and the caller's choice is dropped in silence.
		/// <para>
		/// Which form a bound takes on its own is not one answer but two, so both requests have somewhere to bite. A
		/// duration the query has to look up - a captured local, a call - is a parameter, and asking it to stand in
		/// the statement is what changes that. One written as an expression that cannot vary is already standing
		/// there, and asking it to travel is what changes that instead. Each request is therefore asked over the
		/// form that would otherwise go the other way, with that form asked unmarked beside it.
		/// </para>
		/// <para>
		/// Every query is also run. A bound that reaches the statement in the right form and means the wrong
		/// duration would pass the counts alone. The values are checked everywhere; only the counts stand aside on
		/// ClickHouse, which has no query parameters at all and answers zero to every form alike.
		/// </para>
		/// </remarks>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), NoTickTotalProviders, ErrorMessage = ErrorHelper.Error_Interval_Member)]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		public void DifferenceBoundFollowsTheRequestForHowItTravels([DataSources(false)] string context)
		{
			var bound = TimeSpan.FromHours(2);

			using var db = GetDataConnection(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();

			SeedTasks(db,
				(TimeSpan.FromHours(1), TimeSpan.FromHours(3)),
				(TimeSpan.FromHours(2), TimeSpan.FromHours(3)),
				(TimeSpan.FromHours(3), TimeSpan.FromHours(3)));

			var lookedUp    = t.Where(r => r.FinishedOn - r.StartedOn > bound).Select(r => r.Id);
			var askedToStay = t.Where(r => r.FinishedOn - r.StartedOn > Sql.Constant(bound)).Select(r => r.Id);
			var cannotVary  = t.Where(r => r.FinishedOn - r.StartedOn > TimeSpan.Zero).Select(r => r.Id);
			var askedToGo   = t.Where(r => r.FinishedOn - r.StartedOn > Sql.Parameter(TimeSpan.Zero)).Select(r => r.Id);

			var lookedUpSql    = lookedUp.ToSqlQuery();
			var askedToStaySql = askedToStay.ToSqlQuery();
			var cannotVarySql  = cannotVary.ToSqlQuery();
			var askedToGoSql   = askedToGo.ToSqlQuery();

			lookedUp.OrderBy(id => id).ToArray().ShouldBe([3]);
			askedToStay.OrderBy(id => id).ToArray().ShouldBe([3]);
			cannotVary.OrderBy(id => id).ToArray().ShouldBe([1, 2, 3]);
			askedToGo.OrderBy(id => id).ToArray().ShouldBe([1, 2, 3]);

			askedToStaySql.Parameters.ShouldBeEmpty();
			cannotVarySql.Parameters.ShouldBeEmpty();

			if (!context.IsAnyOf(TestProvName.AllClickHouse))
			{
				lookedUpSql.Parameters.Count.ShouldBe(1);
				askedToGoSql.Parameters.Count.ShouldBe(1);
			}
		}

		/// <summary>
		/// A date difference compared against a declared duration column.
		/// </summary>
		/// <remarks>
		/// The cross-unit case, and the one most likely to be wrong quietly: the left side is a tick count produced
		/// by the lowering, the right side is a number of seconds sitting in a column, and comparing them as they
		/// stand puts every row on the same side. The rows are chosen so that answer - all or nothing - differs
		/// from the correct one.
		/// </remarks>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), NoTickTotalProviders, ErrorMessage = ErrorHelper.Error_Interval_Member)]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		public void DifferenceComparedToADeclaredColumn([DataSources(false)] string context)
		{
			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();

			SeedTasks(db,
				(TimeSpan.FromHours(1), TimeSpan.FromHours(3)),   // under budget
				(TimeSpan.FromHours(4), TimeSpan.FromHours(3)),   // over
				(TimeSpan.FromHours(3), TimeSpan.FromHours(3)));  // exactly on it

			var overBudget = t
				.Where(r => r.FinishedOn - r.StartedOn > r.Budget)
				.Select(r => r.Id)
				.ToArray();

			var onBudget = t
				.Where(r => r.FinishedOn - r.StartedOn == r.Budget)
				.Select(r => r.Id)
				.ToArray();

			overBudget.ShouldBe([2]);
			onBudget.ShouldBe([3]);
		}

		/// <summary>
		/// A difference used as a grouping key comes back as the duration it denotes.
		/// </summary>
		[Test]
		public void GroupByDifferenceReturnsTheDuration([DataSources(false)] string context)
		{
			var shorter = TimeSpan.FromHours(1);
			var longer  = TimeSpan.FromHours(3);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();

			SeedTasks(db,
				(shorter, longer),
				(longer,  longer),
				(shorter, longer));

			var groups = t
				.GroupBy(r => r.FinishedOn - r.StartedOn)
				.Select(g => new { Taken = g.Key, Count = g.Count() })
				.OrderBy(g => g.Taken)
				.ToList();

			groups.Count.ShouldBe(2);

			groups[0].Taken.ShouldBe(shorter);
			groups[0].Count.ShouldBe(2);

			groups[1].Taken.ShouldBe(longer);
			groups[1].Count.ShouldBe(1);
		}

		/// <summary>
		/// Aggregates over a difference.
		/// </summary>
		/// <remarks>
		/// <c>Min</c> and <c>Max</c> put the lowered arithmetic inside an aggregate, which is where a provider that
		/// renders the difference as a multi-part expression is most likely to object.
		/// </remarks>
		[ActiveIssue(Configuration = NoTickTotalProviders, Details = "An aggregate whose body cannot be translated falls back to client evaluation, and the fallback builds a LinqExtensions.AggregateExecute call that EnumerableQuery cannot rewrite: 'There is no method AggregateExecute ... that matches the specified arguments'. Not an interval defect - reproduced on SQLite with Min(r => r.Stamp.ToBinary()), no interval code on the path - so the refusal Access should report is masked by a pre-existing core failure.")]
		[Test]
		// An aggregate whose selector could not be translated is refused by the aggregate machinery rather than by
		// the interval code, and it throws its own exception type - so the refusal is declared by that type here
		// instead of by one of the interval messages. It is still a refusal, and still loud.
		[ThrowsForProvider(typeof(InvalidOperationException), UnsupportedDifferenceProviders)]
		public void AggregatesOverADifference([DataSources(false)] string context)
		{
			var shorter = TimeSpan.FromHours(1);
			var longer  = TimeSpan.FromHours(3);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();

			SeedTasks(db, (longer, longer), (shorter, longer));

			var row = t
				.Select(_ => new
				{
					Min      = t.Min(r => r.FinishedOn - r.StartedOn),
					Max      = t.Max(r => r.FinishedOn - r.StartedOn),
					TotalMin = t.Sum(r => (r.FinishedOn - r.StartedOn).TotalMinutes),
				})
				.First();

			row.Min.ShouldBe(shorter);
			row.Max.ShouldBe(longer);
			row.TotalMin.ShouldBe(shorter.TotalMinutes + longer.TotalMinutes);
		}

		/// <summary>
		/// Ordering by a difference orders by the duration.
		/// </summary>
		[Test]
		public void OrderByDifferenceOrdersByTheDuration([DataSources(false)] string context)
		{
			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();

			// Not seeded in sorted order, so a dropped ORDER BY returns the insertion order and fails.
			SeedTasks(db,
				(TimeSpan.FromHours(3), TimeSpan.FromHours(3)),
				(TimeSpan.FromHours(1), TimeSpan.FromHours(3)),
				(TimeSpan.FromHours(2), TimeSpan.FromHours(3)));

			var ascending = t
				.OrderBy(r => r.FinishedOn - r.StartedOn)
				.Select(r => r.Id)
				.ToArray();

			var descending = t
				.OrderByDescending(r => r.FinishedOn - r.StartedOn)
				.Select(r => r.Id)
				.ToArray();

			ascending.ShouldBe([2, 3, 1]);
			descending.ShouldBe([1, 3, 2]);
		}

		/// <summary>
		/// Membership tests over an elapsed difference convert the candidates into the unit the lowering produces.
		/// </summary>
		/// <remarks>
		/// A difference is not a column and has no declared unit of its own - it arrives in whatever the lowering
		/// yields - so the candidates cannot be converted by asking the mapping. The rows are seeded so that the
		/// wanted and the unwanted durations are both present, which is what tells a working conversion from one
		/// that matched everything or nothing.
		/// </remarks>
		[ActiveIssue(Details = ContainsSkipsIntervalTranslation)]
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), NoTickTotalProviders, ErrorMessage = ErrorHelper.Error_Interval_Member)]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		public void ContainsOverADifference([DataSources(false)] string context)
		{
			var wanted = new[] { TimeSpan.FromHours(1), TimeSpan.FromHours(3) };

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();

			SeedTasks(db,
				(TimeSpan.FromHours(1), TimeSpan.FromHours(3)),
				(TimeSpan.FromHours(2), TimeSpan.FromHours(3)),
				(TimeSpan.FromHours(3), TimeSpan.FromHours(3)));

			var matching = t
				.Where(r => wanted.Contains(r.FinishedOn - r.StartedOn))
				.Select(r => r.Id)
				.OrderBy(id => id)
				.ToArray();

			var present = t.Select(r => r.FinishedOn - r.StartedOn).Contains(TimeSpan.FromHours(2));
			var absent  = t.Select(r => r.FinishedOn - r.StartedOn).Contains(TimeSpan.FromHours(5));

			matching.ShouldBe([1, 3]);
			present.ShouldBeTrue();
			absent.ShouldBeFalse();
		}

		/// <summary>
		/// Three branches - a stored duration, a difference, and the stored duration again.
		/// </summary>
		/// <remarks>
		/// A set operation is a list, not a pair, so a branch in the middle is reached by walking it rather than by
		/// looking at the other side. A two-branch shape cannot tell a walk from a look, because there every branch
		/// is both first and last. Here the difference sits where neither end is, and the column branches around it
		/// must still agree with each other across it.
		/// <para>
		/// The difference is seeded shorter than the stored duration so the three branches answer two distinct
		/// values. Making them equal would let a branch that took another branch's conversion pass unnoticed.
		/// </para>
		/// </remarks>
		[ActiveIssue(Configurations = new[] { TestProvName.AllDB2, TestProvName.AllYdb, TestProvName.AllDuckDB, TestProvName.AllMySql }, Details = MixedStorageInASetOperation)]
		[Test]
		public void ConcatSurroundsADifferenceWithColumns([DataSources(false)] string context)
		{
			var elapsed = TimeSpan.FromHours(1);
			var budget  = TimeSpan.FromHours(3);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();

			SeedTasks(db, (elapsed, budget));

			var rows = t
				.Select(r => new { Source = 1, Duration = r.Budget })
				.Concat(t.Select(r => new { Source = 2, Duration = r.FinishedOn - r.StartedOn }))
				.Concat(t.Select(r => new { Source = 3, Duration = r.Budget }))
				.OrderBy(r => r.Source)
				.ToList();

			rows.Count.ShouldBe(3);

			rows[0].Duration.ShouldBe(budget);
			rows[1].Duration.ShouldBe(elapsed);
			rows[2].Duration.ShouldBe(budget);
		}

		/// <summary>
		/// Two durations in one projection, mixed the opposite way round in each branch.
		/// </summary>
		/// <remarks>
		/// Every shape above carries a single duration, so they cannot tell whether the reconciliation is decided
		/// per column or once for the query. Here the first column is a stored duration meeting a difference and the
		/// second is a difference meeting a stored duration, in the same union - a decision that leaked from one
		/// column to the other would put the wrong conversion on one of them.
		/// <para>
		/// The two durations are deliberately unequal, and they swap places between the branches. Seeding them the
		/// same would make a column mix-up look identical to a correct answer.
		/// </para>
		/// </remarks>
		[ActiveIssue(Configurations = new[] { TestProvName.AllDB2, TestProvName.AllYdb, TestProvName.AllDuckDB, TestProvName.AllMySql }, Details = MixedStorageInASetOperation)]
		[Test]
		public void ConcatMixesTwoDurationsPerRow([DataSources(false)] string context)
		{
			var elapsed = TimeSpan.FromHours(1);
			var budget  = TimeSpan.FromHours(3);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();

			SeedTasks(db, (elapsed, budget));

			var rows = t
				.Select(r => new { Source = 1, First = r.Budget,                        Second = r.FinishedOn - r.StartedOn })
				.Concat(t.Select(r => new { Source = 2, First = r.FinishedOn - r.StartedOn, Second = r.Budget }))
				.OrderBy(r => r.Source)
				.ToList();

			rows.Count.ShouldBe(2);

			rows[0].First.ShouldBe(budget);
			rows[0].Second.ShouldBe(elapsed);

			rows[1].First.ShouldBe(elapsed);
			rows[1].Second.ShouldBe(budget);
		}

		/// <summary>
		/// A union of two differences, with no stored duration on either side.
		/// </summary>
		/// <remarks>
		/// The control for the two mixed shapes above. Both branches reach the reader by the same lowering, so there
		/// is nothing to reconcile and this must pass - if it did not, the mixed cases would not be about mixing at
		/// all and the diagnosis would be wrong.
		/// </remarks>
		[Test]
		public void ConcatOfTwoDifferences([DataSources(false)] string context)
		{
			var elapsed = TimeSpan.FromHours(1);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();

			SeedTasks(db, (elapsed, TimeSpan.FromHours(3)));

			var rows = t
				.Select(r => new { Source = 1, Duration = r.FinishedOn - r.StartedOn })
				.Concat(t.Select(r => new { Source = 2, Duration = r.FinishedOn - r.StartedOn }))
				.OrderBy(r => r.Source)
				.ToList();

			rows.Count.ShouldBe(2);

			rows[0].Duration.ShouldBe(elapsed);
			rows[1].Duration.ShouldBe(elapsed);
		}

		/// <summary>
		/// A common table expression carrying both a difference and a stored duration reads each on its own terms.
		/// </summary>
		/// <remarks>
		/// The two reach the expression by different routes - one is a column with a declared unit, the other is
		/// lowered arithmetic with no declaration of its own - and the CTE puts the same hop in front of both: what
		/// the outer query names is a field of the expression, not the column, so each conversion has to be found
		/// through it.
		/// <para>
		/// The two durations are seeded unequal, so a hop that handed one of them the other's conversion shows up
		/// as a wrong value rather than as an equal one.
		/// </para>
		/// </remarks>
		[Test]
		public void CteCarriesADifferenceAndAStoredDuration([CteContextSource] string context)
		{
			var elapsed = TimeSpan.FromHours(1);
			var budget  = TimeSpan.FromHours(3);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();

			SeedTasks(db, (elapsed, budget));

			var cte = t
				.Select(r => new { r.Id, Taken = r.FinishedOn - r.StartedOn, r.Budget })
				.AsCte();

			var row = cte.OrderBy(r => r.Id).Single();

			row.Taken.ShouldBe(elapsed);
			row.Budget.ShouldBe(budget);
		}

		// The same shapes again over a column whose unit comes from its declaration. What is at stake changes with
		// them: the number in the database is not the duration - 1800 and 18000000000 are the same ninety minutes -
		// so the question is no longer whether the provider allows the expression but whether the right conversion
		// reaches the value, and a wrong one answers plausibly instead of failing.

		/// <summary>
		/// Comparing a duration column against a duration value uses the column's declared unit.
		/// </summary>
		/// <remarks>
		/// The value has to be converted into the column's unit before the comparison, not after: against
		/// <c>InSeconds</c> the bound is 1800 and against <c>InTicks</c> it is 18000000000, for the same half hour.
		/// Sending the tick count to both would put every row on the wrong side of the boundary.
		/// <para>
		/// Both a strict and a non-strict bound are asked, and one row sits exactly on it, so a comparison that is
		/// converted correctly but rendered with the wrong operator is still caught.
		/// </para>
		/// </remarks>
		[ActiveIssue(5748, Configuration = TestProvName.AllAccess, Details = AccessLongParameterOverflow)]
		[Test]
		public void ComparisonAgainstAValueUsesTheDeclaredUnit([DataSources] string context)
		{
			var bound = TimeSpan.FromMinutes(30);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();

			Seed(db, TimeSpan.FromMinutes(15), bound, TimeSpan.FromMinutes(45));

			var overInSeconds = t
				.Where(r => r.InSeconds > bound)
				.Select(r => r.Id)
				.ToArray();

			var atLeastInSeconds = t
				.Where(r => r.InSeconds >= bound)
				.Select(r => r.Id)
				.OrderBy(id => id)
				.ToArray();

			var overInTicks = t
				.Where(r => r.InTicks > bound)
				.Select(r => r.Id)
				.ToArray();

			var equalInTicks = t
				.Where(r => r.InTicks == bound)
				.Select(r => r.Id)
				.ToArray();

			overInSeconds.ShouldBe([3]);
			atLeastInSeconds.ShouldBe([2, 3]);
			overInTicks.ShouldBe([3]);
			equalInTicks.ShouldBe([2]);
		}

		/// <summary>
		/// A duration that may be absent is compared on the column's declared unit when it is there, and matches
		/// nothing when it is not.
		/// </summary>
		/// <remarks>
		/// The bound arrives as a <see cref="Nullable{T}"/>, which is a different thing to convert from the value
		/// itself: the unit has to be found through the wrapper. Both units are asked, so a conversion that was
		/// lost rather than applied shows up as the wrong rows rather than as none - against <c>InSeconds</c> the
		/// bound is 1800 and against <c>InTicks</c> 18000000000, for the same half hour.
		/// <para>
		/// The absent bound is the half that has nowhere to take a type from. It still has to reach the statement
		/// as a duration, because a parameter with no type is what a provider refuses outright rather than
		/// answering; and once there, the comparison must match nothing rather than everything. Both SQL and the
		/// CLR agree that nothing is greater than an absent bound - that is the answer being pinned.
		/// </para>
		/// </remarks>
		[Test]
		public void ComparisonAgainstAnOptionalValueUsesTheDeclaredUnit([DataSources] string context)
		{
			TimeSpan? present = TimeSpan.FromMinutes(30);
			TimeSpan? absent  = null;

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();

			Seed(db, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(45));

			var overInSeconds = t
				.Where(r => r.InSeconds > present)
				.Select(r => r.Id)
				.OrderBy(id => id)
				.ToArray();

			var overInTicks = t
				.Where(r => r.InTicks > present)
				.Select(r => r.Id)
				.OrderBy(id => id)
				.ToArray();

			var equalInSeconds = t
				.Where(r => r.InSeconds == present)
				.Select(r => r.Id)
				.ToArray();

			var overAbsent = t
				.Where(r => r.InSeconds > absent)
				.Select(r => r.Id)
				.ToArray();

			var equalAbsent = t
				.Where(r => r.InSeconds == absent)
				.Select(r => r.Id)
				.ToArray();

			overInSeconds.ShouldBe([3]);
			overInTicks.ShouldBe([3]);
			equalInSeconds.ShouldBe([2]);
			overAbsent.ShouldBeEmpty();
			equalAbsent.ShouldBeEmpty();
		}

		/// <summary>
		/// A bound the column cannot represent selects the same rows the CLR would.
		/// </summary>
		/// <remarks>
		/// The comparison is asked of the bare column, with the bound converted into the column's unit instead - so
		/// a bound that falls between two storable values has to move to one of them, and which one is what the
		/// operator decides. A column counting whole seconds is above one and a half exactly when it is above one,
		/// but reaches one and a half only when it reaches two. Rounding the same way for both would answer a
		/// different question for one of them, and it would answer it in the shape of an ordinary result.
		/// <para>
		/// Equality is the case with no single answer to round to: a duration between two storable values is equal
		/// to neither, so it is asked as the pair of bounds closing on it, which meet on a storable duration and
		/// cross over on one that is not. Inequality is the complement of that pair and is asked in ticks instead,
		/// so it is here to pin that the two still answer as each other's opposite.
		/// </para>
		/// <para>
		/// Every case is checked against the same predicate applied in .NET rather than against a written-down row
		/// list, so the test states the property instead of restating the arithmetic. The bound is also put on the
		/// left in some cases, since that turns the operator before the rounding is chosen from it.
		/// </para>
		/// </remarks>
		[Test]
		public void ABoundBetweenTwoStorableValuesMatchesClr([DataSources] string context)
		{
			var stored = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3) };

			// One and a half seconds is not a whole number of them, so the column can hold neither it nor anything
			// between its neighbours. Two seconds is, and is checked beside it so the exact case cannot regress
			// while the rounded one is being got right.
			foreach (var bound in new[] { TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(2) })
			{
				using var db = GetDataContext(context, BuildSchema());
				using var t  = db.CreateLocalTable<DurationRow>();

				Seed(db, stored);

				Ids(t.Where(r => r.InSeconds >  bound)).ShouldBe(Expected(v => v >  bound), $"greater than {bound}");
				Ids(t.Where(r => r.InSeconds >= bound)).ShouldBe(Expected(v => v >= bound), $"at least {bound}");
				Ids(t.Where(r => r.InSeconds <  bound)).ShouldBe(Expected(v => v <  bound), $"less than {bound}");
				Ids(t.Where(r => r.InSeconds <= bound)).ShouldBe(Expected(v => v <= bound), $"at most {bound}");
				Ids(t.Where(r => r.InSeconds == bound)).ShouldBe(Expected(v => v == bound), $"equal to {bound}");
				Ids(t.Where(r => r.InSeconds != bound)).ShouldBe(Expected(v => v != bound), $"other than {bound}");

				// The same questions written the other way round.
				Ids(t.Where(r => bound <  r.InSeconds)).ShouldBe(Expected(v => bound <  v), $"{bound} below");
				Ids(t.Where(r => bound >= r.InSeconds)).ShouldBe(Expected(v => bound >= v), $"{bound} at or above");
				Ids(t.Where(r => bound == r.InSeconds)).ShouldBe(Expected(v => bound == v), $"{bound} equals");

				int[] Expected(Func<TimeSpan, bool> matches) =>
					[.. stored.Select((v, i) => (Value: v, Id: i + 1)).Where(x => matches(x.Value)).Select(x => x.Id)];
			}

			static int[] Ids(IQueryable<DurationRow> rows) => [.. rows.Select(r => r.Id).OrderBy(id => id)];
		}

		/// <summary>
		/// A duration that is absent answers every comparison the way the CLR answers it.
		/// </summary>
		/// <remarks>
		/// Asking the comparison of the bare column puts the absence somewhere it was not before: the column is what
		/// carries it, and the column is now what the comparison is written around. What the answer for an absent
		/// duration should be is not a matter of taste - <c>Grace &gt;= x</c> is false for a row that has no grace
		/// period, and <c>Grace != x</c> is true - so each operator is checked against the same predicate applied in
		/// .NET, over a row that has the duration and a row that does not.
		/// <para>
		/// Equality carries the weight here, because it is the one asked as two comparisons rather than one. Read as
		/// an ordering, a bound comparison lets an absent operand answer in the affirmative; read as the half of an
		/// equality it must not, or absence would equal everything - and a single row is enough to tell those two
		/// readings apart.
		/// </para>
		/// <para>
		/// The bound that no column of whole seconds can hold is here for the same reason as in the test above, but
		/// against the absent row it asks something further: crossing the bounds over must leave the row out for the
		/// reason it is absent, not because the range is empty.
		/// </para>
		/// <para>
		/// The last round asks the same six of a bound that may itself be absent. That one cannot be converted into
		/// the column's unit - there may be nothing to convert - so both sides go to ticks instead, which is a
		/// second place the same question about the column has to be answered the same way.
		/// </para>
		/// </remarks>
		[Test]
		public void ComparingAnAbsentDurationMatchesClr([DataSources] string context)
		{
			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable(OptionalDurationRow.Data);

			// A duration one of the rows has, and one falling half a second short of it, which a column counting
			// whole seconds cannot hold.
			foreach (var bound in new[] { TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15) - TimeSpan.FromMilliseconds(500) })
			{
				Ids(t.Where(r => r.Grace >  bound)).ShouldBe(Expected(v => v >  bound), $"greater than {bound}");
				Ids(t.Where(r => r.Grace >= bound)).ShouldBe(Expected(v => v >= bound), $"at least {bound}");
				Ids(t.Where(r => r.Grace <  bound)).ShouldBe(Expected(v => v <  bound), $"less than {bound}");
				Ids(t.Where(r => r.Grace <= bound)).ShouldBe(Expected(v => v <= bound), $"at most {bound}");
				Ids(t.Where(r => r.Grace == bound)).ShouldBe(Expected(v => v == bound), $"equal to {bound}");
				Ids(t.Where(r => r.Grace != bound)).ShouldBe(Expected(v => v != bound), $"other than {bound}");
			}

			TimeSpan? optional = TimeSpan.FromMinutes(15);
			TimeSpan? absent   = null;

			Ids(t.Where(r => r.Grace >  optional)).ShouldBe(Expected(v => v >  optional), "greater than an optional bound");
			Ids(t.Where(r => r.Grace >= optional)).ShouldBe(Expected(v => v >= optional), "at least an optional bound");
			Ids(t.Where(r => r.Grace <  optional)).ShouldBe(Expected(v => v <  optional), "less than an optional bound");
			Ids(t.Where(r => r.Grace <= optional)).ShouldBe(Expected(v => v <= optional), "at most an optional bound");
			Ids(t.Where(r => r.Grace == optional)).ShouldBe(Expected(v => v == optional), "equal to an optional bound");
			Ids(t.Where(r => r.Grace != optional)).ShouldBe(Expected(v => v != optional), "other than an optional bound");

			// A bound that is absent, against a column that can be. Two absences are equal to each other, which is
			// the one thing a pair of bounds cannot say - nothing lies within a range that has no ends - so this is
			// where a comparison rewritten into such a pair stops meaning what it was written as.
			Ids(t.Where(r => r.Grace == absent)).ShouldBe(Expected(v => v == absent), "equal to an absent bound");
			Ids(t.Where(r => r.Grace != absent)).ShouldBe(Expected(v => v != absent), "other than an absent bound");

			static int[] Expected(Func<TimeSpan?, bool> matches) =>
				[.. OptionalDurationRow.Data.Where(r => matches(r.Grace)).Select(r => r.Id)];

			static int[] Ids(IQueryable<OptionalDurationRow> rows) => [.. rows.Select(r => r.Id).OrderBy(id => id)];
		}

		/// <summary>
		/// How a duration reaches the statement follows the expression it was written as, and either request
		/// overrides that.
		/// </summary>
		/// <remarks>
		/// A <see cref="TimeSpan"/> has no literal form of its own in SQL - what reaches the statement is the number
		/// its declaration says it is - so nothing forces the choice either way, and the choice matters: a value
		/// written into the statement makes a query that differs only by its duration a different query, which the
		/// cache then keeps apart, and which a shape running one query per value cannot vary at all.
		/// <para>
		/// Left alone the choice follows whether the expression could have been anything else. A captured local is
		/// looked up, and so is a call - <c>TimeSpan.FromMinutes(30)</c> is written in place but still has to be
		/// evaluated - and both become parameters. <c>TimeSpan.Zero</c> cannot be anything else, and is written into
		/// the statement. So each request is asked over the form that would otherwise go the other way, and that
		/// form is asked unmarked beside it; asked the other way round, either would pass without being honoured.
		/// </para>
		/// <para>
		/// The statement is read rather than its text searched, and every query is also run, because a value that
		/// travels correctly and means the wrong duration would pass the first check alone. The values are checked
		/// everywhere; only the counts stand aside where there is nothing to count, since ClickHouse has no query
		/// parameters at all - it refuses to name one - and answers zero to every form alike.
		/// </para>
		/// <para>
		/// Both units are asked of the constant, because the number differs between them while the duration does
		/// not. The number itself is not pinned: which side of the comparison carries the reconciliation - the
		/// column lifted to ticks, or the value lowered to seconds - is the provider's business.
		/// </para>
		/// </remarks>
		[ActiveIssue(5748, Configuration = TestProvName.AllAccess, Details = AccessLongParameterOverflow)]
		[Test]
		public void DeclaredDurationFollowsTheRequestForHowItTravels([DataSources(false)] string context)
		{
			var bound = TimeSpan.FromMinutes(30);

			using var db = GetDataConnection(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();

			Seed(db, TimeSpan.FromMinutes(15), bound, TimeSpan.FromMinutes(45));

			var lookedUp         = t.Where(r => r.InSeconds > bound).Select(r => r.Id);
			var calledInPlace    = t.Where(r => r.InSeconds > TimeSpan.FromMinutes(30)).Select(r => r.Id);
			var askedToStay      = t.Where(r => r.InSeconds > Sql.Constant(bound)).Select(r => r.Id);
			var askedToStayTicks = t.Where(r => r.InTicks   > Sql.Constant(bound)).Select(r => r.Id);
			var cannotVary       = t.Where(r => r.InSeconds > TimeSpan.Zero).Select(r => r.Id);
			var askedToGo        = t.Where(r => r.InSeconds > Sql.Parameter(TimeSpan.Zero)).Select(r => r.Id);

			var lookedUpSql         = lookedUp.ToSqlQuery();
			var calledInPlaceSql    = calledInPlace.ToSqlQuery();
			var askedToStaySql      = askedToStay.ToSqlQuery();
			var askedToStayTicksSql = askedToStayTicks.ToSqlQuery();
			var cannotVarySql       = cannotVary.ToSqlQuery();
			var askedToGoSql        = askedToGo.ToSqlQuery();

			lookedUp.OrderBy(id => id).ToArray().ShouldBe([3]);
			calledInPlace.OrderBy(id => id).ToArray().ShouldBe([3]);
			askedToStay.OrderBy(id => id).ToArray().ShouldBe([3]);
			askedToStayTicks.OrderBy(id => id).ToArray().ShouldBe([3]);
			cannotVary.OrderBy(id => id).ToArray().ShouldBe([1, 2, 3]);
			askedToGo.OrderBy(id => id).ToArray().ShouldBe([1, 2, 3]);

			askedToStaySql.Parameters.ShouldBeEmpty();
			askedToStayTicksSql.Parameters.ShouldBeEmpty();
			cannotVarySql.Parameters.ShouldBeEmpty();

			if (!context.IsAnyOf(TestProvName.AllClickHouse))
			{
				lookedUpSql.Parameters.Count.ShouldBe(1);
				calledInPlaceSql.Parameters.Count.ShouldBe(1);
				askedToGoSql.Parameters.Count.ShouldBe(1);
			}
		}

		/// <summary>
		/// Two columns holding the same duration in different units compare equal.
		/// </summary>
		/// <remarks>
		/// The discriminating case for comparing durations across units, and the reason it is worth a test of its
		/// own: the stored numbers are 1800 and 18000000000 for the same ninety minutes, so a comparison left to
		/// the raw values answers "not equal" - a plausible-looking answer that is simply wrong.
		/// </remarks>
		[Test]
		public void EqualDurationsInDifferentUnitsCompareEqual([DataSources] string context)
		{
			var value = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			// Seeded from one value, so the two columns denote the same duration and differ only in storage.
			var equal = t
				.Where(r => r.InSeconds == r.InTicks)
				.Select(r => r.Id)
				.ToArray();

			var greater = t
				.Where(r => r.InSeconds > r.InTicks)
				.Select(r => r.Id)
				.ToArray();

			equal.ShouldBe([1]);
			greater.ShouldBeEmpty();
		}

		/// <summary>
		/// A duration used as a grouping key comes back as the duration, not as the number underneath it.
		/// </summary>
		/// <remarks>
		/// The key travels a different path from a projected column - it is produced by the GROUP BY and read from
		/// the key position - so a conversion applied on the projection path alone would leave this one raw. With
		/// <c>InSeconds</c> the raw value is 1800 against a expected 30 minutes, which is not a subtle difference.
		/// </remarks>
		[Test]
		public void GroupByDurationKeepsTheDeclaredUnit([DataSources] string context)
		{
			var shorter = TimeSpan.FromMinutes(30);
			var longer  = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, shorter, longer, shorter);

			var groups = t
				.GroupBy(r => r.InSeconds)
				.Select(g => new { Duration = g.Key, Count = g.Count() })
				.OrderBy(g => g.Duration)
				.ToList();

			groups.Count.ShouldBe(2);

			groups[0].Duration.ShouldBe(shorter);
			groups[0].Count.ShouldBe(2);

			groups[1].Duration.ShouldBe(longer);
			groups[1].Count.ShouldBe(1);
		}

		/// <summary>
		/// Aggregates over a duration column keep the declared unit.
		/// </summary>
		/// <remarks>
		/// <c>Min</c> and <c>Max</c> return a duration and so must be converted back, while a sum is taken over a
		/// member rather than the column - there is no numeric sum of a <see cref="TimeSpan"/> - which makes it the
		/// case where the unit has to be applied before the addition rather than after.
		/// </remarks>
		[Test]
		public void AggregatesKeepTheDeclaredUnit([DataSources] string context)
		{
			var shorter = TimeSpan.FromMinutes(30);
			var longer  = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, longer, shorter);

			var row = t
				.Select(_ => new
				{
					Min      = t.Min(r => r.InSeconds),
					Max      = t.Max(r => r.InSeconds),
					TotalMin = t.Sum(r => r.InSeconds.TotalMinutes),
				})
				.First();

			row.Min.ShouldBe(shorter);
			row.Max.ShouldBe(longer);
			row.TotalMin.ShouldBe(shorter.TotalMinutes + longer.TotalMinutes);
		}

		/// <summary>
		/// Ordering by a duration orders by the duration.
		/// </summary>
		/// <remarks>
		/// The one shape where the value is never read, so nothing about the conversion is exercised - what is
		/// pinned instead is that the sort happens on the stored amount and stays monotone in the duration. The
		/// seeded order is deliberately not the sorted one, so a query that dropped the ORDER BY would return the
		/// rows as inserted and fail rather than pass by coincidence.
		/// </remarks>
		[Test]
		public void OrderByDurationOrdersByTheDuration([DataSources] string context)
		{
			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();

			Seed(db, TimeSpan.FromMinutes(90), TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(60));

			var ascending = t
				.OrderBy(r => r.InSeconds)
				.Select(r => r.Id)
				.ToArray();

			var descending = t
				.OrderByDescending(r => r.InTicks)
				.Select(r => r.Id)
				.ToArray();

			ascending.ShouldBe([2, 3, 1]);
			descending.ShouldBe([1, 3, 2]);
		}

		/// <summary>
		/// Membership tests convert each candidate into the column's declared unit.
		/// </summary>
		/// <remarks>
		/// An <c>IN</c> list is a comparison repeated, so it needs the same conversion a single comparison needs -
		/// against <c>InSeconds</c> the candidates are 900 and 2700, against <c>InTicks</c> nine and twenty-seven
		/// billion, for the same two durations. Sending one set of numbers to both columns matches nothing, which is
		/// why the expected answer here is a subset rather than everything: an empty result and a full one would
		/// both be indistinguishable from a plausible mistake.
		/// <para>
		/// Three routes reach that conversion, each arriving differently - the list may stand in the predicate or be
		/// projected as a value, and the set may be the local list or a subquery with the candidate outside it. Both
		/// units are asked of each, because a conversion taken from the wrong column would still answer one of them.
		/// </para>
		/// <para>
		/// The fourth route, where both sides are columns and neither number is known while the query is built, is
		/// <c>ContainsAcrossUnitsReconcilesTheColumns</c> below - it does not work yet.
		/// </para>
		/// </remarks>
		[Test]
		public void ContainsUsesTheDeclaredUnit([DataSources] string context)
		{
			var wanted = new[] { TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(45) };

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();

			Seed(db, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(45));

			var inSeconds = t
				.Where(r => wanted.Contains(r.InSeconds))
				.Select(r => r.Id)
				.OrderBy(id => id)
				.ToArray();

			var inTicks = t
				.Where(r => wanted.Contains(r.InTicks))
				.Select(r => r.Id)
				.OrderBy(id => id)
				.ToArray();

			var projected = t
				.OrderBy(r => r.Id)
				.Select(r => wanted.Contains(r.InSeconds))
				.ToArray();

			var present = t.Select(r => r.InSeconds).Contains(TimeSpan.FromMinutes(30));
			var absent  = t.Select(r => r.InTicks).Contains(TimeSpan.FromMinutes(90));

			inSeconds.ShouldBe([1, 3]);
			inTicks.ShouldBe([1, 3]);
			projected.ShouldBe([true, false, true]);
			present.ShouldBeTrue();
			absent.ShouldBeFalse();
		}

		/// <summary>
		/// A membership test between two columns held in different units brings them to common terms.
		/// </summary>
		/// <remarks>
		/// The case no conversion of a constant can carry: both sides are columns, so neither number is known while
		/// the query is being built, and 1800 against 18000000000 is the same ninety minutes written two ways. The
		/// answer is every row, because each row's two columns hold the same duration - an empty result means the
		/// raw numbers were compared.
		/// </remarks>
		[Test]
		public void ContainsAcrossUnitsReconcilesTheColumns([DataSources] string context)
		{
			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();

			Seed(db, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(45));

			var acrossUnits = t
				.Where(r => t.Select(x => x.InTicks).Contains(r.InSeconds))
				.Select(r => r.Id)
				.OrderBy(id => id)
				.ToArray();

			acrossUnits.ShouldBe([1, 2, 3]);
		}

		/// <summary>
		/// A local collection used as a source carries its durations into the query on the column's terms.
		/// </summary>
		/// <remarks>
		/// A collection joined to a table is not a predicate but a source: its values become rows the query selects
		/// from, so each one is written into the statement rather than bound to a comparison. The unit still has to
		/// come from whatever the value meets - a row holding ninety minutes matches <c>InSeconds</c> only if it is
		/// written as 1800, and <c>InTicks</c> only if it is written as eighteen billion, and the same collection is
		/// used against both here.
		/// </remarks>
		[Test]
		public void LocalCollectionAsASourceKeepsTheDeclaredUnit([DataSources] string context)
		{
			var wanted = new[] { TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(45) };

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();

			Seed(db, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(45));

			var joinedToSeconds =
				(from d in wanted
				 join r in t on d equals r.InSeconds
				 orderby r.Id
				 select r.Id)
				.ToArray();

			var joinedToTicks =
				(from d in wanted
				 join r in t on d equals r.InTicks
				 orderby r.Id
				 select r.Id)
				.ToArray();

			joinedToSeconds.ShouldBe([1, 3]);
			joinedToTicks.ShouldBe([1, 3]);
		}

		/// <summary>
		/// Each value of a local collection driving a per-value query reaches its own query.
		/// </summary>
		/// <remarks>
		/// This shape is not one statement over a values source: the collection is walked on the client and a query
		/// runs per value. Each of those has to carry its own value, and the two here are chosen to match different
		/// rows, so a value that failed to travel shows up as the same row twice rather than as no rows at all.
		/// <para>
		/// Which is what it did for as long as a duration was written into the statement rather than passed to it:
		/// every value after the first found the first one's query already built and asked its question again. The
		/// same shape over a plain integer column always answered correctly, and that difference is what showed the
		/// fault was in how the duration travelled rather than in the shape. What settled it is one test below.
		/// </para>
		/// <para>
		/// A duration is also projected back out, which is the other direction through the same seam: the value has
		/// to survive the round trip into the statement and back into a <see cref="TimeSpan"/>.
		/// </para>
		/// </remarks>
		[ActiveIssue(5748, Configuration = TestProvName.AllAccess, Details = AccessLongParameterOverflow)]
		[Test]
		public void LocalCollectionDrivingAQueryPerValueKeepsEachValue([DataSources] string context)
		{
			var wanted = new[] { TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(45) };

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();

			Seed(db, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(45));

			var correlated =
				(from d in wanted
				 from r in t.Where(x => x.InSeconds == d)
				 orderby r.Id
				 select r.Id)
				.ToArray();

			var roundTripped =
				(from d in wanted
				 from r in t.Where(x => x.InSeconds == d)
				 orderby r.Id
				 select new { Local = d, Stored = r.InSeconds })
				.ToArray();

			correlated.ShouldBe([1, 3]);

			roundTripped.Select(x => x.Local).ShouldBe([TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(45)]);
			roundTripped.Select(x => x.Stored).ShouldBe([TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(45)]);
		}

		/// <summary>
		/// A duration survives a set operation, from both sides of it.
		/// </summary>
		/// <remarks>
		/// Both branches of a <c>UNION ALL</c> must agree on the column's storage before the reader sees it. The
		/// two sides here read different columns holding the same duration in different units, so a branch that
		/// dropped its conversion would come back off by a factor of ten million rather than not at all.
		/// </remarks>
		[Test]
		public void ConcatKeepsTheDeclaredUnitOnBothSides([DataSources] string context)
		{
			var value = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			var rows = t
				.Select(r => new { Source = 1, Duration = r.InSeconds })
				.Concat(t.Select(r => new { Source = 2, Duration = r.InTicks }))
				.OrderBy(r => r.Source)
				.ToList();

			rows.Count.ShouldBe(2);
			rows[0].Duration.ShouldBe(value);
			rows[1].Duration.ShouldBe(value);
		}

		/// <summary>
		/// A duration the branches store differently is read a branch at a time, and refused where no branch is
		/// known yet.
		/// </summary>
		/// <remarks>
		/// Which conversion a value needs is settled when the row is materialised, because only then is it known
		/// which branch the row came from. That is enough to read it, whatever shape it is projected in - the shape
		/// above carries the member inside an object, this one projects it alone, and the two reach the reader by
		/// different routes.
		/// <para>
		/// Asking for the same value in SQL has no row and no branch yet, so the choice cannot be made: the two
		/// columns would have to become one, and one column carries one conversion, which is right for at most one
		/// of the branches. The query is refused rather than answered - the answer would look ordinary and be wrong
		/// by a factor of ten million.
		/// </para>
		/// </remarks>
		[Test]
		public void ConcatReadsADivergentMemberButRefusesItInSql([DataSources] string context)
		{
			var value = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			var union = t
				.Select(r => new { Source = 1, Duration = r.InSeconds })
				.Concat(t.Select(r => new { Source = 2, Duration = r.InTicks }));

			var durations = union
				.OrderBy(r => r.Source)
				.Select(r => r.Duration)
				.ToList();

			durations.ShouldBe([value, value]);

			var act = () => union.Where(r => r.Duration > TimeSpan.Zero).Select(r => r.Source).ToList();

			act.ShouldThrow<LinqToDBException>();
		}

		/// <summary>
		/// A set operation that compares rows refuses branches that store a duration differently.
		/// </summary>
		/// <remarks>
		/// <c>UNION ALL</c> hands every row back, so each can be read the way its own branch stores it. Every other
		/// set operation decides which rows survive by comparing the values in the database, where a duration in
		/// seconds and one in ticks are two different numbers - the comparison would not mean what it says, and no
		/// per-branch reading afterwards can repair a row that was already dropped or kept wrongly.
		/// <para>
		/// So the answer is a refusal rather than an answer. The message is checked, not just the exception type:
		/// the same exception stands for every untranslatable query, and what makes this one useful is that it says
		/// which operation and what is wrong with it.
		/// </para>
		/// </remarks>
		[Test]
		public void UnionRefusesBranchesThatStoreADurationDifferently([DataSources] string context)
		{
			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, TimeSpan.FromMinutes(90));

			var act = () => t
				.Select(r => new { Duration = r.InSeconds })
				.Union(t.Select(r => new { Duration = r.InTicks }))
				.ToList();

			var message = act.ShouldThrow<LinqToDBException>().Message;

			message.ShouldContain("Union");
			message.ShouldContain("in different terms");
		}

		/// <summary>
		/// Choosing between two columns converted differently keeps each one's own conversion.
		/// </summary>
		/// <remarks>
		/// Nothing about this is particular to set operations: a conditional hands the reader one value, and a
		/// server-side <c>CASE</c> would hand it one conversion with it, which is right for at most one of the two
		/// columns. The choice therefore stays where the row is, and each answer is read the way its own column
		/// stores it.
		/// <para>
		/// The two columns here carry hand-written converters rather than a declared unit, so this also covers the
		/// half of the comparison that the declared-unit tests never reach. Two rows are seeded with different
		/// values and the condition sends them down different arms, so a single conversion applied to both shows up
		/// as a wrong value rather than as an equal one.
		/// </para>
		/// </remarks>
		[Test]
		public void ConditionalKeepsEachColumnsOwnConversion([DataSources] string context)
		{
			var longer  = TimeSpan.FromMinutes(90);
			var shorter = TimeSpan.FromMinutes(30);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, longer, shorter);

			var durations = t
				.OrderBy(r => r.Id)
				.Select(r => r.Id == 1 ? r.Undeclared : r.UndeclaredSeconds)
				.ToList();

			durations.ShouldBe([longer, shorter]);
		}

		/// <summary>
		/// A duration read through a common table expression keeps the conversion its own column carries.
		/// </summary>
		/// <remarks>
		/// A CTE puts a hop between the column and the reader: what the outer query names is a field of the
		/// expression rather than the column itself, so the conversion has to be found through that field. The three
		/// columns asked here are stored three different ways - two declared units and one hand-written converter -
		/// and each would come back as a different wrong number if the hop lost it, so a single conversion applied
		/// to all three could not pass either.
		/// </remarks>
		[Test]
		public void CteCarriesADurationHoweverItIsStored([CteContextSource] string context)
		{
			var value = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			var cte = t
				.Select(r => new { r.Id, r.InSeconds, r.InTicks, r.UndeclaredSeconds })
				.AsCte();

			var row = cte.OrderBy(r => r.Id).Single();

			row.InSeconds.ShouldBe(value);
			row.InTicks.ShouldBe(value);
			row.UndeclaredSeconds.ShouldBe(value);
		}

		/// <summary>
		/// A duration produced by a scalar sub-query keeps its conversion, including where the provider can only
		/// express the sub-query by lifting it into a common table expression.
		/// </summary>
		/// <remarks>
		/// A single-column sub-query in a projection is a value rather than a row, and a provider with no inline
		/// form for one rewrites it into a named expression and refers to that name instead. The reader is built
		/// from the rewritten statement, so it has to find the conversion through the expression rather than through
		/// the sub-query - a different walk, and the one that was not answered: a duration stored as 1800 seconds
		/// came back as 1800 ticks, which is the same value the reader would produce with no conversion at all.
		/// <para>
		/// Three storages are asked, and the third is what widens the case beyond durations: <c>UndeclaredSeconds</c>
		/// has no declared unit and only a hand-written converter, so what is pinned here is that any value
		/// converter survives this route, not that this one does.
		/// </para>
		/// </remarks>
		[Test]
		public void ScalarSubqueryKeepsTheConversion([DataSources] string context)
		{
			var shorter = TimeSpan.FromMinutes(30);
			var longer  = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, longer, shorter);

			var row = t
				.Select(_ => new
				{
					Seconds   = t.Min(r => r.InSeconds),
					Ticks     = t.Min(r => r.InTicks),
					Converted = t.Min(r => r.UndeclaredSeconds),
					Largest   = t.Max(r => r.InSeconds),
				})
				.First();

			row.Seconds.ShouldBe(shorter);
			row.Ticks.ShouldBe(shorter);
			row.Converted.ShouldBe(shorter);
			row.Largest.ShouldBe(longer);
		}

		/// <summary>
		/// Carries a duration through the levels of a recursive expression, where it is a projected value rather
		/// than a column of its own.
		/// </summary>
		sealed class DurationChain
		{
			public int      Id        { get; set; }
			public int      Level     { get; set; }
			public TimeSpan Duration  { get; set; }
			public TimeSpan Converted { get; set; }
		}

		/// <summary>
		/// A duration carried through a recursive common table expression keeps its conversion at every level.
		/// </summary>
		/// <remarks>
		/// A recursive expression names itself, so a walk looking for the conversion can arrive back where it
		/// started. The scalar-expression rule therefore refuses a recursive one outright instead of trying to
		/// answer for it, and this is the shape that says whether refusing cost anything: the value still has to
		/// reach the reader, by the ordinary route through the expression's own fields.
		/// <para>
		/// Each level carries a different duration, and a second column carries a hand-written converter beside the
		/// declared unit, so a conversion lost after the first level - or taken once and reused for both columns -
		/// shows up as a wrong value rather than as an equal one.
		/// </para>
		/// </remarks>
		[Test]
		public void RecursiveCteCarriesADurationThroughEveryLevel([RecursiveCteContextSource] string context)
		{
			var first  = TimeSpan.FromMinutes(15);
			var second = TimeSpan.FromMinutes(30);
			var third  = TimeSpan.FromMinutes(45);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();

			Seed(db, first, second, third);

			var chain = db.GetCte<DurationChain>(self =>
				(
					from r in t
					where r.Id == 1
					select new DurationChain { Id = r.Id, Level = 1, Duration = r.InSeconds, Converted = r.UndeclaredSeconds }
				)
				.Concat
				(
					from r in t
					from c in self.InnerJoin(c => r.Id == c.Id + 1)
					select new DurationChain { Id = r.Id, Level = c.Level + 1, Duration = r.InSeconds, Converted = r.UndeclaredSeconds }
				));

			var rows = chain.OrderBy(r => r.Id).ToList();

			rows.Select(r => r.Level).ShouldBe([1, 2, 3]);
			rows.Select(r => r.Duration).ShouldBe([first, second, third]);
			rows.Select(r => r.Converted).ShouldBe([first, second, third]);
		}
	}
}
