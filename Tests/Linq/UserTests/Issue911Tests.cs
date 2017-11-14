using System.Linq;
using NUnit.Framework;
using Tests.Model;
using LinqToDB;
using System.Linq.Expressions;

namespace Tests.UserTests
{
	public class Issue911Tests : TestBase
	{
		[Test, DataContextSource]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Child
					.GroupBy(p => p.ParentID % 2 == 0)
					.Select(g => new
					{
						g.Key,
						X = g.OrderBy(_ => _.ParentID).First()
					});

				var array = query.ToArray();
			}
		}
	}
}
