using System;
using System.Linq;
using LinqToDB;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue928Tests : TestBase
	{
		[Test, IncludeDataContextSource(ProviderName.SqlServer2012, ProviderName.PostgreSQL, ProviderName.SQLite)]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var subquery =
					from p in db.Parent
					where db.Child.Select(ch => ch.ParentID).Contains(p.ParentID)
					group p by p.ParentID into g
					select new { ParentID = g.Key, Sum = g.Sum(x => x.ParentID) };

				var q3 =
					from p1 in subquery
					from p2 in subquery.Where(o => o.ParentID == p1.ParentID).DefaultIfEmpty()
					select new { p1, p2 };

				q3.ToList();
			}
		}
	}
}
