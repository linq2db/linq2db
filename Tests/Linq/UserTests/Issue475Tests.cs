using System.Linq;

using NUnit.Framework;
using Tests.Model;

namespace Tests.UserTests
{

	[TestFixture]
	public class Issue475Tests : TestBase
	{

		[Test, NorthwindDataContext]
		public void Test(string context)
		{
			using (var db = new NorthwindDB())
			{
				var zz =
					from e in db.Employee
					select e;

				var lst = zz.ToList();
				var item1 = lst.Take(1).Single();
				var item2 = zz.Take(1).Single();


				Assert.AreEqual(item1.EmployeeID, item2.EmployeeID);
			}
		}
	}
}
