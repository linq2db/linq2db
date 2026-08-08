using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	// A duration column reads back through a conversion derived from its declared unit. Projecting one is the
	// obvious place that can go wrong, and it is covered elsewhere - these are the shapes where the value reaches
	// the reader by another route: as a grouping key, as an aggregate result, through a set operation, or as a
	// sort key where it is never read at all and only has to order correctly.
	public partial class IntervalTranslationTests
	{
		/// <summary>
		/// Why a branch holding a difference cannot meet one holding a declared duration on some providers.
		/// </summary>
		/// <remarks>
		/// A member the branches store differently is given a column each, so that each is read the way its own
		/// branch stores it. That keeps the reader right, but it does not make the two storages meet: DB2 and YDB
		/// require corresponding columns of a set operation to have compatible types, and on them a difference is a
		/// native interval while a declared duration is an integer. Neither is convertible to the other by anything
		/// the set operation can express, so the driver refuses the whole statement - DB2 with SQL0415N, YDB with
		/// "Uncompatible member ... types: Optional&lt;Interval&gt; and Int64".
		/// <para>
		/// Not a defect in the splitting: the same shape with two stored durations passes on both, because both
		/// sides are integers there. Answering it needs the two branches brought to one representation before they
		/// meet, which is a separate piece of work.
		/// </para>
		/// </remarks>
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
		/// Why a duration carried by a per-value query arrives as the first value every time.
		/// </summary>
		/// <remarks>
		/// The unit itself is reconciled correctly - the column is raised to ticks and the candidate is written in
		/// ticks - but the converted candidate ends up in the statement as a literal rather than as a parameter.
		/// Every value after the first then reuses the first one's statement, so the query asks the same question
		/// repeatedly and answers it consistently and wrongly. An integer column in the same shape keeps its value
		/// a parameter and answers correctly.
		/// <para>
		/// Silent by nature: the rows come back, they are simply the wrong ones, and the number of them is right.
		/// </para>
		/// </remarks>
		const string ConvertedValueIsInlinedPerQuery =
			"A converted duration reaches the statement as a literal rather than a parameter, so a shape that runs "
			+ "one query per local value reuses the first value's statement for all of them - the same rows come back "
			+ "for every value. The unit is reconciled correctly; it is the value that fails to travel. The same shape "
			+ "over a plain integer column answers correctly.";

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
			[Column(Configuration = Wide, DataType = DataType.Decimal, Precision = 18, Scale = 0)]
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
		/// A date difference compared against a duration value.
		/// </summary>
		/// <remarks>
		/// The difference lowers to ticks, so the bound has to arrive in ticks - a different conversion from the
		/// one a declared column needs, and reached by a different path.
		/// </remarks>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		[ThrowsForProvider(typeof(LinqToDBException), NoTickTotalProviders,          ErrorMessage = ErrorHelper.Error_Interval_Member)]
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
		/// A date difference compared against a declared duration column.
		/// </summary>
		/// <remarks>
		/// The cross-unit case, and the one most likely to be wrong quietly: the left side is a tick count produced
		/// by the lowering, the right side is a number of seconds sitting in a column, and comparing them as they
		/// stand puts every row on the same side. The rows are chosen so that answer - all or nothing - differs
		/// from the correct one.
		/// </remarks>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		[ThrowsForProvider(typeof(LinqToDBException), NoTickTotalProviders,          ErrorMessage = ErrorHelper.Error_Interval_Member)]
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

		// The same four shapes again, over a date difference rather than a stored column. Worth repeating rather
		// than parameterising: a column reaches the query as itself, while a difference reaches it as lowered
		// arithmetic, so GROUP BY, MIN and ORDER BY are applied to a computed expression - and a provider may
		// place one of those where the expression is not allowed even though the projection works.

		/// <summary>
		/// A difference used as a grouping key comes back as the duration it denotes.
		/// </summary>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
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
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		[ActiveIssue(Configuration = NoTickTotalProviders, Details = "An aggregate whose body cannot be translated falls back to client evaluation, and the fallback builds a LinqExtensions.AggregateExecute call that EnumerableQuery cannot rewrite: 'There is no method AggregateExecute ... that matches the specified arguments'. Not an interval defect - reproduced on SQLite with Min(r => r.Stamp.ToBinary()), no interval code on the path - so the refusal Access should report is masked by a pre-existing core failure.")]
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
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
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
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
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
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
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
		/// Ordering by a difference orders by the duration.
		/// </summary>
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
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
		/// Membership tests convert each candidate into the column's declared unit.
		/// </summary>
		/// <remarks>
		/// An <c>IN</c> list is a comparison repeated, so it needs the same conversion a single comparison needs -
		/// against <c>InSeconds</c> the candidates are 900 and 2700, against <c>InTicks</c> they are nine and
		/// twenty-seven billion, for the same two durations. Sending one set of numbers to both columns matches
		/// nothing, which is why the expected answer here is a subset rather than everything: an empty result and a
		/// full one would both be indistinguishable from a plausible mistake.
		/// <para>
		/// Four routes to the same conversion, and each reaches it differently: the list may sit in the predicate or
		/// be projected as a value, and the set may be a local collection or a subquery. The last case is the
		/// discriminating one - both sides are columns, in different units, so neither can be converted at build
		/// time and they have to meet on terms the query itself establishes.
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
		[ActiveIssue(Details = ContainsSkipsIntervalTranslation)]
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
		/// <para>
		/// A duration is also projected back out, which is the other direction through the same seam: the value has
		/// to survive the round trip into the statement and back into a <see cref="TimeSpan"/>.
		/// </para>
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
		/// The same shape over a plain integer column answers correctly, which is what makes this about the
		/// conversion rather than about the shape.
		/// </para>
		/// </remarks>
		[ActiveIssue(Details = ConvertedValueIsInlinedPerQuery)]
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
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		[ThrowsForProvider(typeof(LinqToDBException), NoTickTotalProviders,          ErrorMessage = ErrorHelper.Error_Interval_Member)]
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
	}
}
