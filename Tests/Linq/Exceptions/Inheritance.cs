using System;
using System.Linq;

using LinqToDB.Linq;

using NUnit.Framework;

namespace Tests.Exceptions
{
	[TestFixture]
	public class Inheritance : TestBase
	{
		[Test, DataContextSource, ExpectedException(typeof(LinqException))]
		public void Test1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.ParentInheritance2 select p;
				q.ToList();
			}
		}
	}
}
