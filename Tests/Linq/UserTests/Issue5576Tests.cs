using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	/// <summary>
	/// Regression: a LEFT JOIN of a DB table to an in-memory collection exposed via
	/// <c>.AsQueryable()</c>, where the element is a multi-member class and one member flows into
	/// decimal arithmetic in a <b>later</b> projection, emitted a spurious whole-object <c>[item]</c>
	/// column in the generated <c>VALUES</c> clause. The element was bound as a single parameter
	/// typed by the leaked <c>Decimal</c> descriptor, producing
	/// <c>InvalidCastException: Failed to convert parameter value from a &lt;Type&gt; to a Decimal</c>
	/// at execution.
	/// <para>
	/// The trigger is the three-stage shape (join → decimal-rate select → re-mapping select): the
	/// re-mapping keeps the decimal projection as an inner query, so the enumerable element source is
	/// re-resolved while a <c>Decimal</c> descriptor is active. Collapsing it to a single join
	/// selector does not reproduce.
	/// </para>
	/// <see href="https://github.com/linq2db/linq2db/issues/5576"/>
	/// </summary>
	[TestFixture]
	public class Issue5576Tests : TestBase
	{
		[Table]
		sealed class Campaign
		{
			[Column] public Guid Guid { get; set; }
			[Column] public int  Sold { get; set; }
		}

		// Multi-member class. Must be a class — an unmapped struct is treated as scalar (separate path).
		sealed class Counts
		{
			public Guid Key   { get; set; }
			public int  Count { get; set; }
		}

		// Stage 1: result of the LEFT JOIN, carrying the (nullable) joined member.
		sealed class Stat
		{
			public Guid CampaignGuid { get; set; }
			public int? LeadCount    { get; set; }
			public int  Sold         { get; set; }
		}

		// Stage 2: adds the decimal rate computed from the joined member.
		sealed class WithRate
		{
			public Guid     CampaignGuid { get; set; }
			public int?     LeadCount    { get; set; }
			public decimal? Rate         { get; set; }
		}

		// Stage 3: a re-mapping projection (data-model -> domain-model).
		sealed class Result
		{
			public Guid     CampaignGuid { get; set; }
			public int?     Leads        { get; set; }
			public decimal? Rate         { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5576")]
		public void LeftJoinLocalClassWithDecimalArithmetic([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<Campaign>();

			var g1 = TestData.Guid1;
			var g2 = TestData.Guid2;
			var g3 = TestData.Guid3;

			db.Insert(new Campaign { Guid = g1, Sold = 100 });
			db.Insert(new Campaign { Guid = g2, Sold = 40  });
			db.Insert(new Campaign { Guid = g3, Sold = 200 });

			var counts = new[]
			{
				new Counts { Key = g1, Count = 5  },
				new Counts { Key = g2, Count = 10 },
			};

			// Stage 1 — LEFT JOIN the in-memory multi-member source.
			var stage1 = table
				.LeftJoin(
					counts.AsQueryable(),
					(c, lc) => c.Guid == lc.Key,
					(c, lc) => new Stat
					{
						CampaignGuid = c.Guid,
						LeadCount    = lc.Count,
						Sold         = c.Sold
					});

			// Stage 2 — decimal arithmetic on the (nullable) joined member -> Decimal descriptor.
			var stage2 = stage1.Select(s => new WithRate
			{
				CampaignGuid = s.CampaignGuid,
				LeadCount    = s.LeadCount,
				Rate         = s.LeadCount.HasValue ? s.LeadCount.Value / (decimal)s.Sold * 100 : (decimal?)null
			});

			// Stage 3 — re-mapping projection (data-model -> domain-model). This layering triggers the bug.
			var query = stage2.Select(r => new Result
			{
				CampaignGuid = r.CampaignGuid,
				Leads        = r.LeadCount,
				Rate         = r.Rate
			});

			var result = query.ToList();

			result.Count.ShouldBe(3);

			var r1 = result.Single(r => r.CampaignGuid == g1);
			r1.Leads.ShouldBe(5);
			r1.Rate.ShouldBe(5m);

			var r2 = result.Single(r => r.CampaignGuid == g2);
			r2.Leads.ShouldBe(10);
			r2.Rate.ShouldBe(25m);

			var r3 = result.Single(r => r.CampaignGuid == g3);
			r3.Leads.ShouldBeNull();
			r3.Rate.ShouldBeNull();
		}
	}
}
