using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.Exceptions
{
	[TestFixture]
	public class ElementOperationTest : TestBase
	{
		[Test, DataContextSource, ExpectedException(typeof(InvalidOperationException))]
		public void First(string context)
		{
			using (var db = GetDataContext(context))
				db.Parent.First(p => p.ParentID == 100);
		}

		[Test, DataContextSource, ExpectedException(typeof(InvalidOperationException))]
		public void Single(string context)
		{
			using (var db = GetDataContext(context))
				db.Parent.Single();
		}
	}
}
