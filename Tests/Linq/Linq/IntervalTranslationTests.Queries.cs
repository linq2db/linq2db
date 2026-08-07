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
		/// A set operation with a difference on one side and a stored duration on the other.
		/// </summary>
		/// <remarks>
		/// The cross-unit case in a union: one branch produces a tick count from the lowering, the other reads a
		/// column of seconds. Both must denote the same duration by the time the reader sees them, and the rows
		/// are seeded so that they do - a branch that kept its raw number would come back off by a factor of ten
		/// million rather than slightly wrong.
		/// </remarks>
		[ActiveIssue(Details = "The two branches lower the same logical type to different SQL types, and PostgreSQL says so outright: \"UNION types interval and bigint cannot be matched\". One branch produces a native interval from the lowering, the other reads a bigint column of seconds. Where the union does succeed the answer is wrong instead - the first branch's conversion is applied to both. A duration needs one representation across the branches of a query, which is the same root as the other set-operation and comparison defects recorded here.")]
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), UnsupportedDifferenceProviders, ErrorMessage = ErrorHelper.Error_Interval_Difference)]
		public void ConcatMixesADifferenceAndAColumn([DataSources(false)] string context)
		{
			var value = TimeSpan.FromHours(3);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<BudgetedTaskRow>();

			SeedTasks(db, (value, value));

			var rows = t
				.Select(r => new { Source = 1, Duration = r.FinishedOn - r.StartedOn })
				.Concat(t.Select(r => new { Source = 2, Duration = r.Budget }))
				.OrderBy(r => r.Source)
				.ToList();

			rows.Count.ShouldBe(2);
			rows[0].Duration.ShouldBe(value);
			rows[1].Duration.ShouldBe(value);
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
		[ActiveIssue(Details = "Min and Max over a duration column return the stored number read as ticks - 1800 seconds comes back as 1800 ticks. The aggregate produces its result without the column's conversion. One of five defects found together: the duration unit declared by the mapping reaches the value only through the column read path, so every other route to a TimeSpan - comparison, aggregate, set operation - loses it or applies the wrong one. Recorded rather than fixed because reconciling units across those paths is a change in shared code, not a local repair.")]
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
		[ActiveIssue(Details = "A set operation applies the first branch's conversion to both. The ticks branch is read as seconds, so ninety minutes comes back as 625000 days - not a lost conversion but a foreign one. One of five defects found together: the duration unit declared by the mapping reaches the value only through the column read path, so every other route to a TimeSpan - comparison, aggregate, set operation - loses it or applies the wrong one. Recorded rather than fixed because reconciling units across those paths is a change in shared code, not a local repair.")]
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
