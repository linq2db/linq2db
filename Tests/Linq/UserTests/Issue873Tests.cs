using System.Linq;
using NUnit.Framework;
using Tests.Model;
using LinqToDB;

namespace Tests.UserTests
{
	public class Issue873Tests : TestBase
	{
		[Test, DataContextSource]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Child;

				var query = db.Parent
					.Select(e => new
					{
						Fields = new
						{
							Label = " " + e.Value1,
							Count = q.Where(_ => _.Parent == e).Count()
						},
					})
					.Where(_ => _.Fields.Label.Contains("1"));

				var array = query.ToArray();
			}
		}
	}
}