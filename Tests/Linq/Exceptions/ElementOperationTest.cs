using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.Exceptions
{
	[TestFixture]
	public class ElementOperationTest : TestBase
	{
		[Test, DataContextSource]
		public void First(string context)
		{
			using (var db = GetDataContext(context))
				Assert.Throws(typeof(InvalidOperationException), () => db.Parent.First(p => p.ParentID == 100));
		}

		[Test, DataContextSource]
		public void Single(string context)
		{
			using (var db = GetDataContext(context))
				Assert.Throws(typeof(InvalidOperationException), () => db.Parent.Single());
		}
	}
}
