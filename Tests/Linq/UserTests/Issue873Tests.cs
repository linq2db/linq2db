using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class Issue873Tests : TestBase
	{
		[Test]
		[ActiveIssue(873, Details = "Also check WCF test error for Access")]
		public void Test([DataSources] string context)
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
