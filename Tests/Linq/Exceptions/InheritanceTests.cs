using System;
using System.Linq;

using LinqToDB.Linq;

using NUnit.Framework;

namespace Tests.Exceptions
{
	[TestFixture]
	public class InheritanceTests : TestBase
	{
		[Test, DataContextSource]
		public void Test1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.ParentInheritance2 select p;
				Assert.Throws(typeof(LinqException), () => q.ToList());
			}
		}
	}
}
