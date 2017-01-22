using System.Linq;

using LinqToDB;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue358Tests : TestBase
	{
		enum TestIssue358Enum
		{
			Value1,
			Value2
		}

		class TestIssue358Class
		{
			public TestIssue358Enum? MyEnum;
		}

		[Test, Ignore("Not currently supported"), DataContextSource]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var qry =
					from p in db.GetTable<TestIssue358Class>()
					where p.MyEnum != TestIssue358Enum.Value1
					select p;

				var sql = qry.ToString();

				Assert.That(sql.IndexOf("NULL"), Is.GreaterThan(0));
			} 
		}
	}
}
