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

		/// <summary>
		/// The units the rest of the fixture does not store, kept in their own table so the shared one stays as it
		/// is.
		/// </summary>
		/// <remarks>
		/// Second and Tick are what everything else declares, and between them they never divide: a tick passes
		/// through and a second multiplies. The two ends of the scale behave differently and are what this covers -
		/// Nanosecond is the only unit finer than a tick, so reaching ticks from it is a division, while Day carries
		/// the largest factor there is and is the one most likely to overflow a provider's own arithmetic.
		/// </remarks>
		[Table]
		sealed class UnitSpreadRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.Int64)]
			[Column(Configuration = Wide, DataType = DataType.Money)]
			[Duration(DurationUnit.Day)]
			public TimeSpan InDays { get; set; }

			[Column(DataType = DataType.Int64)]
			[Column(Configuration = Wide, DataType = DataType.Money)]
			[Duration(DurationUnit.Millisecond)]
			public TimeSpan InMilliseconds { get; set; }

			[Column(DataType = DataType.Int64)]
			[Duration(DurationUnit.Nanosecond)]
			public TimeSpan InNanoseconds { get; set; }
		}

		/// <summary>
		/// A duration declared in each of the remaining units is written, read back and asked for a member.
		/// </summary>
		/// <remarks>
		/// The day column is seeded with something that is not a whole number of days, which pins the truncation the
		/// derived conversion documents: storage coarser than the value keeps what fits and drops the rest, and that
		/// belongs to the unit the user chose rather than to anything this could avoid.
		/// <para>
		/// Access is left out because it has no integer wide enough for the nanosecond column - a hundred times a
		/// tick count - not because anything about the units differs there.
		/// </para>
		/// </remarks>
		[Test]
		public void EachDeclaredUnitRoundTripsAndReads([DataSources(false, TestProvName.AllAccess)] string context)
		{
			var days         = TimeSpan.FromHours(60);
			var milliseconds = new TimeSpan(0, 1, 2, 3, 456);
			var nanoseconds  = TimeSpan.FromTicks(TimeSpan.TicksPerSecond * 7 + 1234);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<UnitSpreadRow>();

			db.Insert(new UnitSpreadRow
			{
				Id             = 1,
				InDays         = days,
				InMilliseconds = milliseconds,
				InNanoseconds  = nanoseconds,
			});

			var row = t.Single();

			// Two and a half days stored as whole days is two - the value the column can hold, not the one written.
			row.InDays.ShouldBe(TimeSpan.FromDays(2));
			row.InMilliseconds.ShouldBe(milliseconds);
			row.InNanoseconds.ShouldBe(nanoseconds);

			var members = t
				.Select(r => new
				{
					DayHours          = Sql.AsSql(r.InDays.TotalHours),
					MillisecondSecond = Sql.AsSql(r.InMilliseconds.Seconds),
				})
				.Single();

			members.DayHours.ShouldBe(48d);
			members.MillisecondSecond.ShouldBe(milliseconds.Seconds);
		}

		/// <summary>
		/// A duration stored in a unit finer than a tick round-trips, but its members do not translate anywhere.
		/// </summary>
		/// <remarks>
		/// Reaching a tick count from nanoseconds is a division, and the lowering takes only units that scale up -
		/// so the member is refused by name on every provider rather than answered by one and not another. The
		/// conversion itself is unaffected, which is why the value above comes back intact: what cannot be done is
		/// arithmetic on the stored number in SQL.
		/// <para>
		/// Pinned rather than left undiscovered because the unit is offered on the public enum: whoever declares it
		/// should meet a refusal that says so, and this goes red the day the lowering learns to divide.
		/// </para>
		/// </remarks>
		[Test]
		public void AUnitFinerThanATickHasNoMembers([DataSources(false, TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<UnitSpreadRow>();

			var value = TimeSpan.FromSeconds(7);

			db.Insert(new UnitSpreadRow { Id = 1, InNanoseconds = value });

			// Left untranslated rather than refused, so a projection reads the column and answers from .NET - which
			// is the whole of what the member means. Asserted first because it is the case a caller actually meets.
			var members = t
				.Select(r => new
				{
					r.InNanoseconds.Ticks,
					r.InNanoseconds.Hours,
					r.InNanoseconds.TotalSeconds,
				})
				.Single();

			members.Ticks.ShouldBe(value.Ticks);
			members.Hours.ShouldBe(value.Hours);
			members.TotalSeconds.ShouldBe(value.TotalSeconds);

			// Asked for in SQL, there is nothing to fall back to and it is refused. Asserted here rather than through
			// ThrowsForProvider because there is no provider to name: no lowering anywhere takes a unit below a tick,
			// so every one of them refuses. This goes red the day one learns to divide.
			Action act = () => t.Select(r => Sql.AsSql(r.InNanoseconds.Ticks)).Single();

			// The message as well as the type: LinqToDBException stands for every untranslatable query, so a
			// type-only assertion would also be satisfied by an unrelated refusal inside this same one. What is
			// raised here is the generic conversion failure - no refusal names a sub-tick unit - so that is what
			// is pinned, and pinning it is what would notice one being introduced.
			act.ShouldThrow<LinqToDBException>().Message.ShouldContain("could not be converted to SQL");

			// The conversion is untouched by that - only arithmetic on the stored number is refused.
			t.Single().InNanoseconds.ShouldBe(value);
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

#if NET8_0_OR_GREATER
		/// <summary>
		/// A sub-second component of a stored duration answers even where the provider measures elapsed time
		/// more coarsely.
		/// </summary>
		/// <remarks>
		/// The two are different questions. How finely a provider can measure the time between two dates says
		/// nothing about a number already sitting in a column: a duration declared in ticks carries its own
		/// microseconds exactly, and dividing that number needs no measurement at all. SQLite is the provider
		/// where the two come apart - it measures through <c>julianday</c> and resolves to the millisecond - so a
		/// gate applied to both would refuse this while the answer is sitting in the column.
		/// </remarks>
		[Test]
		public void SubSecondComponentsOfAStoredDurationAnswer([DataSources] string context)
		{
			var value = new TimeSpan(0, 0, 0, 1, 234) + TimeSpan.FromTicks(5670);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			// Sql.AsSql is what makes this a test: the refusal it guards against is an error expression, and one of
			// those only propagates where SQL is required. Left as a plain projection the member would fall back to
			// .NET and answer correctly whether or not the gate had fired.
			var row = t
				.Select(r => new
				{
					Milliseconds = Sql.AsSql(r.InTicks.Milliseconds),
					Microseconds = Sql.AsSql(r.InTicks.Microseconds),
				})
				.Single();

			row.Milliseconds.ShouldBe(value.Milliseconds);
			row.Microseconds.ShouldBe(value.Microseconds);
		}
#endif

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
