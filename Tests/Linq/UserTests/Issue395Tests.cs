using System;
using System.Linq;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue395Tests : TestBase
	{
		[Test]
		public void Test([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var first  = db.Order.Where(x => x.ShipVia == 1).Select(x => new {x.ShipCountry, Via1 = x.Freight, Via2 = 0M,        Via3 = 0M});
				var second = db.Order.Where(x => x.ShipVia == 2).Select(x => new {x.ShipCountry, Via1 = 0M,        Via2 = x.Freight, Via3 = 0M});
				var third  = db.Order.Where(x => x.ShipVia == 3).Select(x => new {x.ShipCountry, Via1 = 0M,        Via2 = 0M,        Via3 = x.Freight});

				var allData = first.Union(second).Union(third)
					.GroupBy(x => x.ShipCountry)
					.Select(x => new
					{
						ShipCountry = x.Key,
						Via1 = x.Select(y => y.Via1).Sum(),
						Via2 = x.Select(y => y.Via2).Sum(),
						Via3 = x.Select(y => y.Via3).Sum(),
					});

				var data = allData.First();

				// ok
				Assert.That(data.Via1,
							Is.EqualTo(first.Where(x => x.ShipCountry == data.ShipCountry).Sum(x => x.Via1)),
							"first aggregation sum mismatch");

				// ok
				Assert.That(data.Via2,
							Is.EqualTo(second.Where(x => x.ShipCountry == data.ShipCountry).Sum(x => x.Via2)),
							"second aggregation sum mismatch");

				// asserts
				Assert.That(data.Via3,
							Is.EqualTo(third.Where(x => x.ShipCountry == data.ShipCountry).Sum(x => x.Via3)),
							"third aggregation sum mismatch");
			}
		}
	}
}
