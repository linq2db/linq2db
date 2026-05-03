using System.Linq;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue475Tests : TestBase
	{
		[Test]
		public void Test([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var zz =
					from e in db.Employee
					select e;

			var lst = zz.ToList();
			var item1 = lst.Take(1).Single();
			var item2 = zz.Take(1).Single();

			Assert.That(item2.EmployeeID, Is.EqualTo(item1.EmployeeID));
		}
	}
}
