using NUnit.Framework;
using System;
using System.Linq;

namespace Tests.UserTests
{
	public class Issue0420Tests : TestBase
	{
		[Test]
		public void Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent
					.SelectMany(p => p.Children.Select(c => Tuple.Create(c)))
					.ToArray();
			}
		}
	}
}
