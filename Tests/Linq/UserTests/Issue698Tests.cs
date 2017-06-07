using System;
using System.Linq;
using LinqToDB;
using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue698Tests : TestBase
	{
		[Test]
		public void Test(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var zz =
					from e in db.Employee
					where Sql.RegExp(e.FirstName, "Na.*")
					select e;

				var res2 = zz.ToList();
				
				Assert.That(res2.Count, Is.EqualTo(2));
			}
		}
	}
}
