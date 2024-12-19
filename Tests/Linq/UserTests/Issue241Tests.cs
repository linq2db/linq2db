using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue241Tests : TestBase
	{
		[Test]
		public void Test([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(new DataOptions().UseConfiguration(context).UseGuardGrouping(false)))
			{
				var jj  = from o in db.Customer select o;
				jj      = jj.Where(x => x.CompanyName.Contains("t"));
				var t1g = jj.GroupBy(_ => _).ToDictionary(_ => _.Key, _ => _.ToList());

				Assert.That(t1g, Is.Not.Empty);
			}
		}
	}
}
