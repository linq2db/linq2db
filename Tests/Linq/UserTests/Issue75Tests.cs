using System.Linq;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue75Tests : TestBase
	{
		[Test, DataContextSource(), Ignore("Not currently supported")]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var childs = db.Child.Select(c => new
				{
					Child = c,
					HasChildren = db.Child.Any(c2 => c2.ParentID == c.ChildID)
				});
				childs =
					from child in childs
					join parent in db.Parent on child.Child.ParentID equals parent.ParentID
					where parent.Value1 == 1
					select child;

				var sql = childs.ToString();
				Assert.That(sql, Contains.Substring("HasChildren"));
				Assert.That(childs.ToList().Count, Is.GreaterThan(0));
			}
		}
	}
}
