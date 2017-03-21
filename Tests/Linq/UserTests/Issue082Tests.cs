using System;
using System.Linq;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue082Tests : TestBase
	{
		[Test, NorthwindDataContext]
		public void Test(string context)
		{
			using (var db = new NorthwindDB(context))
			{

				var query = from o in db.Order
							join od in db.OrderDetail on o.OrderID equals od.OrderID into irc
							select new
							{
								Order = o,
								CountResources = irc.Count(),
								CountInventory = irc.Sum(x => x.Quantity)
							};

				var lst = query.ToList();
				var resMemory = lst.Where(x => x.CountResources > 0).ToList();

				var res = query.Where(x => x.CountResources > 0);


				// ok
				Assert.That(resMemory.Count(), Is.EqualTo(res.Count()));

				// failing
				Assert.That(resMemory.Count(), Is.EqualTo(res.ToList().Count()));
			}
		}
	}
}
