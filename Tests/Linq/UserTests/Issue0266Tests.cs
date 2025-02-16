using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class Issue0266Tests : TestBase
	{
		[Test]
		public void TestFactory([DataSources] string context)
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

		[Test]
		public void TestConstructor([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person
					.Select(p => new Tuple<int, string>(p.ID, p.Name))
					.Where(_ => _.Item1 == 1)
					.Select(_ => _.Item2)
					.SingleOrDefault();
			}
		}
	}
}
