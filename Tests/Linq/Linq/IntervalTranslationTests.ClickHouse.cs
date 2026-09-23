using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class IntervalTranslationTests
	{
		/// <summary>
		/// The coercion is emitted only where the operand is not already a <c>DateTime64</c>.
		/// </summary>
		/// <remarks>
		/// Both halves are needed and neither implies the other. Dropping the coercion where it is not needed is
		/// what the operand types are read for, and it is invisible in a result assertion - every test in
		/// <see cref="CoarseEventRow"/>'s set passes with the conversion applied to everything. Keeping it where the
		/// server needs it is what #5955 was; a type that stopped describing the SQL would silently take this branch too.
		/// </remarks>
		[Test]
		public void TheCoercionIsEmittedOnlyWhereTheOperandNeedsIt([IncludeDataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataConnection(context);
			using var t  = db.CreateLocalTable<EventRow>();

			// EventRow declares nothing, so ClickHouse stores DateTime64(7) - already what the epoch functions take.
			_ = t.Select(r => (r.FinishedOn - r.StartedOn).TotalHours).ToList();
			db.LastQuery!.ShouldNotContain("toDateTime64");

			// now() is a whole-second timestamp whatever the mapping schema makes of a CLR DateTime.
			_ = t.Select(r => (Sql.CurrentTimestamp - r.StartedOn).TotalHours).ToList();
			db.LastQuery!.ShouldContain("toDateTime64(now()");

			// makeDateTime answers a whole-second timestamp too.
			_ = t.Select(r => (r.FinishedOn - Sql.MakeDateTime(2026, 6, 1, 10, 0, 0)!.Value).TotalHours).ToList();
			db.LastQuery!.ShouldContain("toDateTime64(makeDateTime(");

			using var coarse = db.CreateLocalTable<CoarseEventRow>();

			// A declared DataType.DateTime column is a 32-bit whole-second timestamp on the server.
			_ = coarse.Select(r => (r.FinishedOn - r.StartedOn).TotalHours).ToList();
			db.LastQuery!.ShouldContain("toDateTime64(");
		}
	}
}
