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
	/// <c>.AsQueryable()</c>, where the element is a multi-member class and one member
	/// flows into decimal arithmetic in the projection, emits a spurious whole-object
	/// <c>[item]</c> column in the generated <c>VALUES</c> clause. The element is bound as a
	/// single parameter, producing <c>InvalidCastException: Failed to convert parameter value
	/// from a &lt;Type&gt; to a Decimal</c> at execution.
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

		sealed class Counts
		{
			public Guid Key   { get; set; }
			public int  Count { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5576")]
		public void LeftJoinLocalClassWithDecimalArithmetic([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<Campaign>();

			var g1 = TestData.Guid1;
			var g2 = TestData.Guid2;

			db.Insert(new Campaign { Guid = g1, Sold = 100 });
			db.Insert(new Campaign { Guid = g2, Sold = 200 });

			var counts = new[]
			{
				new Counts { Key = g1, Count = 5  },
				new Counts { Key = g2, Count = 10 },
			};

			var query = table
				.LeftJoin(
					counts.AsQueryable(),
					(c, lc) => c.Guid == lc.Key,
					(c, lc) => new
					{
						c.Guid,
						Count = lc.Count,
						Rate  = lc.Count / (decimal)c.Sold * 100
					});

			var result = query.OrderBy(r => r.Rate).ToList();

			result.Count.ShouldBe(2);

			result[0].Guid.ShouldBe(g1);
			result[0].Count.ShouldBe(5);
			result[0].Rate.ShouldBe(5m / 100 * 100);

			result[1].Guid.ShouldBe(g2);
			result[1].Count.ShouldBe(10);
			result[1].Rate.ShouldBe(10m / 200 * 100);
		}
	}
}
