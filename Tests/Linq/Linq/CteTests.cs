using LinqToDB;
using System.Linq;
using NUnit.Framework;
using Tests.Model;

namespace Tests.Linq
{
	public class CteTests : TestBase
	{
		[Test, DataContextSource]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var cte1 = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCTE();
				var query = from p in db.Parent
					join c in cte1 on p.ParentID equals c.ParentID
					join c2 in cte1 on p.ParentID equals c2.ParentID
					select p;

				var str = query.ToString();
			}
		}
	}
}
