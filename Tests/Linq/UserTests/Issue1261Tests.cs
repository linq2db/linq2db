using System.Linq;
using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1261Tests : TestBase
	{
		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/56 + https://github.com/ClickHouse/ClickHouse/issues/37999", Configurations = new[] { ProviderName.ClickHouseMySql, ProviderName.ClickHouseOctonica })]
		[Test]
		public void TestLinqAll([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q  = db.GrandChild.Where(x => x.ParentID == 1);
				var q1 = q.All(x => x.ChildID == 11 && x.GrandChildID == 777);
				var q2 = q.All(x => x.GrandChildID == 777 && x.ChildID == 11);

				Assert.AreEqual(q1, q2);
			}
		}
	}
}
