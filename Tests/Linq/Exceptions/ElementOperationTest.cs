using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.Exceptions
{
	[TestFixture]
	public class ElementOperationTest : TestBase
	{
		[Test, ExpectedException(typeof(InvalidOperationException))]
		public void First([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				db.Parent.First(p => p.ParentID == 100);
		}

		[Test, ExpectedException(typeof(InvalidOperationException))]
		public void Single([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				db.Parent.Single();
		}
	}
}
