using NUnit.Framework;
using System;
using System.Linq;

namespace Tests.UserTests
{
	public class Issue0420Tests : TestBase
	{
		[Test]
		public void TestFactory([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent
					.SelectMany(p => p.Children.Select(c => Tuple.Create(c)))
					.ToArray();
			}
		}

		[Test]
		public void TestConstructor([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent
					.SelectMany(p => p.Children.Select(c => new Tuple<Model.Child>(c)))
					.ToArray();
			}
		}
	}
}
