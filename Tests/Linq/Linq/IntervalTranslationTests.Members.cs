using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class IntervalTranslationTests
	{
		/// <summary>
		/// A duration that may be absent, beside one that may not, so the same member can be asked of both.
		/// </summary>
		/// <remarks>
		/// The pairing is the point. A member read from a column that is never null proves the translation; the
		/// same member read from one that can be null proves it survives the nullability, and the two side by side
		/// in one row mean a mistake cannot hide behind "everything came back null".
		/// </remarks>
		[Table]
		sealed class OptionalDurationRow
		{
			[PrimaryKey] public int Id { get; set; }

			// Named for a grace period rather than for what it demonstrates: "Optional" is how YQL spells a
			// nullable type, so a column of that name makes YDB reject the CREATE TABLE outright.
			[Column(DataType = DataType.Int64, CanBeNull = true)]
			[Column(Configuration = Wide, DataType = DataType.Money, CanBeNull = true)]
			[Duration(DurationUnit.Second)]
			public TimeSpan? Grace { get; set; }

			[Column(DataType = DataType.Int64)]
			[Column(Configuration = Wide, DataType = DataType.Money)]
			[Duration(DurationUnit.Second)]
			public TimeSpan Required { get; set; }

			public static readonly OptionalDurationRow[] Data =
			[
				new() { Id = 1, Grace = TimeSpan.FromMinutes(15), Required = TimeSpan.FromMinutes(15) },
				new() { Id = 2, Grace = null,                     Required = TimeSpan.FromMinutes(30) },
				new() { Id = 3, Grace = TimeSpan.FromMinutes(45), Required = TimeSpan.FromMinutes(45) },
			];
		}

		/// <summary>
		/// A member of a duration that may be absent answers for the row that has one and answers nothing for the
		/// row that does not.
		/// </summary>
		/// <remarks>
		/// A member is read by converting the stored number into a duration and then asking it, so a column that
		/// may be null puts a step in front of that which the conversion has to survive: there is nothing to
		/// convert. What must not happen is the absence turning into a value - a zero, or the raw stored number
		/// read as though it were ticks.
		/// <para>
		/// The required column carries the same member in the same row, so a row that answered nothing for both
		/// would be caught. The two are seeded to the same duration wherever both are present, which means an
		/// answer that took one column's value for the other still reads correctly here - that mix-up is not what
		/// this test is for, and <c>Optional</c> is deliberately absent where <c>Required</c> is 30 minutes so the
		/// two can never be confused on the row where it would matter.
		/// </para>
		/// </remarks>
		[Test]
		public void MembersOfAnOptionalDurationSurviveItsAbsence([DataSources] string context)
		{
			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable(OptionalDurationRow.Data);

			var rows = t
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					r.Id,
					Minutes         = (int?)   r.Grace!.Value.Minutes,
					TotalMinutes    = (double?)r.Grace!.Value.TotalMinutes,
					RequiredMinutes = r.Required.TotalMinutes,
				})
				.ToList();

			rows.Count.ShouldBe(3);

			rows[0].Minutes.ShouldBe(15);
			rows[0].TotalMinutes.ShouldNotBeNull();
			rows[0].TotalMinutes!.Value.ShouldBe(15d, Tolerance(15d));

			rows[1].Minutes.ShouldBeNull();
			rows[1].TotalMinutes.ShouldBeNull();

			rows[2].Minutes.ShouldBe(45);
			rows[2].TotalMinutes.ShouldNotBeNull();
			rows[2].TotalMinutes!.Value.ShouldBe(45d, Tolerance(45d));

			rows.Select(r => r.RequiredMinutes).ShouldBe([15d, 30d, 45d]);
		}

		/// <summary>
		/// Filtering on a member of a duration keeps the member, rather than comparing the number underneath it.
		/// </summary>
		/// <remarks>
		/// The shape behind issue 4308: a member access on a duration was dropped on the way into SQL, so the
		/// comparison was made against the stored number instead. That reads as an ordinary query and answers
		/// wrongly, which is why the bound is chosen to tell the two apart - the durations are stored as 900 and
		/// 2700 seconds, and both are greater than thirty, so a dropped member matches every row while the correct
		/// answer is one of them.
		/// <para>
		/// The absent row must fall out of the filter rather than fail it: a comparison against nothing is not
		/// true, which is what SQL and the CLR both say. Asked of the required column as well, because the two
		/// reach the comparison by different routes and only one of them has the nullability to lose.
		/// </para>
		/// </remarks>
		[Test]
		public void FilteringOnADurationMemberKeepsTheMember([DataSources] string context)
		{
			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable(OptionalDurationRow.Data);

			var optional = t
				.Where(r => r.Grace!.Value.TotalMinutes > 30)
				.Select(r => r.Id)
				.OrderBy(id => id)
				.ToArray();

			var required = t
				.Where(r => r.Required.TotalMinutes > 30)
				.Select(r => r.Id)
				.OrderBy(id => id)
				.ToArray();

			optional.ShouldBe([3]);
			required.ShouldBe([3]);
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
	}
}
