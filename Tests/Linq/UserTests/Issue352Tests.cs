using System;
using System.Linq;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue352Tests : TestBase
	{
		[Test, NorthwindDataContext]
		public void Test(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var zz =
					from e in db.Employee
					from et in db.EmployeeTerritory
					where et.EmployeeID == e.EmployeeID
					group e by new { e.EmployeeID }
					into g
					select new
					{
						g.Key.EmployeeID,
						//g.FirstOrDefault().FirstName,
						db.Employee.FirstOrDefault(em => em.EmployeeID == g.Key.EmployeeID).FirstName,
					};

				//    zz = zz.OrderBy(a => a.FirstName);

				var res = zz.ToList();
			}
		}
	}
}
