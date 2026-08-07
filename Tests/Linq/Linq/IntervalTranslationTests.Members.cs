using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class IntervalTranslationTests
	{
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
