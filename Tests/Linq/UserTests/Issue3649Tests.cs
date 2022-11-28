﻿using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3649Tests : TestBase
	{
		[Table]
		sealed class Total
		{
			[Column]              public int     Id    { get; set; }
			[Column]              public int     Sum   { get; set; }
			[Column(Length = 50)] public string? Label { get; set; }
		}

		[Table]
		sealed class Entry
		{
			[Column] public int Id    { get; set; }
			[Column] public int TotalId    { get; set; }
			[Column] public int Sum   { get; set; }
		}

		[Test]
		public async Task UpdateWithoutJoin([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db      = (DataConnection)GetDataContext(context);
			using var totals  = db.CreateLocalTable<Total>();
			using var entries = db.CreateLocalTable<Entry>();

			await totals.InnerJoin(
					entries
						.GroupBy(e => e.TotalId, (totalId, en) => new { TotalId = totalId, SumAggr = en.Sum(i => i.Sum) }),
					(t, eg) => t.Id == eg.TotalId, (t, eg) => new { t, eg }
				)
				.Set(r => r.t.Sum, r => r.t.Sum + r.eg.SumAggr)
				.UpdateAsync();

			db.LastQuery.Should().NotContain("JOIN");
		}

		[Test]
		public async Task UpdateByOtherTable([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db      = (DataConnection)GetDataContext(context);
			using var totals  = db.CreateLocalTable<Total>();
			using var entries = db.CreateLocalTable<Entry>();

			await totals.InnerJoin(
					entries
						.GroupBy(e => e.TotalId,
							(totalId, en) => new { TotalId = totalId, SumAggr = en.Sum(i => i.Sum) }),
					(t, eg) => t.Id == eg.TotalId, (t, eg) => new { OldSum = t.Sum, eg.SumAggr, t.Label }
				)
				.Where(r => r.Label == "spendings")
				.UpdateAsync(totals, g =>
					new Total { Sum = g.OldSum + g.SumAggr });

				db.LastQuery.Should().NotContain("JOIN");
		}

		[Test]
		public async Task UpdateByOtherTableWithJoin([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db      = (DataConnection)GetDataContext(context);
			using var totals  = db.CreateLocalTable<Total>();
			using var entries = db.CreateLocalTable<Entry>();

			await
				entries
					.GroupBy(e => e.TotalId, (totalId, en) => new { TotalId = totalId, SumAggr = en.Sum(i => i.Sum) })
					.InnerJoin(totals, (eg, t) =>
						t.Id == eg.TotalId, (eg, t) => new {  OldSum = t.Sum, eg.SumAggr, t.Label })
					.Where(r => r.Label == "spendings")
					.UpdateAsync(totals, g =>
						new Total
						{
							Sum = g.OldSum + g.SumAggr
						});

			db.LastQuery.Should().NotContain("JOIN");
		}
	}
}
