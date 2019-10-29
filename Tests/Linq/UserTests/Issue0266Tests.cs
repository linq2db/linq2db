using NUnit.Framework;
using System;
using System.Linq;

namespace Tests.UserTests
{
	public class Issue0266Tests : TestBase
	{
		[ActiveIssue(266)]
		[Test]
		public void Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person
					.Select(p => Tuple.Create(p.ID, p.Name))
					.Where(_ => _.Item1 == 1)
					.Select(_ => _.Item2)
					.SingleOrDefault();
			}
		}
	}
}
