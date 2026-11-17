using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.Exceptions
{
	[TestFixture]
	public class ElementOperationTests : TestBase
	{
		[Test]
		public void First([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(() => db.Parent.First(p => p.ParentID == 100), Throws.TypeOf<InvalidOperationException>());
		}

		[Test]
		public void Single([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(() => db.Parent.Single(), Throws.TypeOf<InvalidOperationException>());
		}
	}
}
