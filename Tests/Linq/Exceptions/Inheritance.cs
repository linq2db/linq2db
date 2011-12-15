using System;
using System.Linq;

using LinqToDB.Data.Linq;

using NUnit.Framework;

namespace Tests.Exceptions
{
	[TestFixture]
	public class Inheritance : TestBase
	{
		[Test, ExpectedException(typeof(LinqException))]
		public void Test1()
		{
			ForEachProvider(typeof(LinqException), db =>
			{
				var q = from p in db.ParentInheritance2 select p;
				q.ToList();
			});
		}
	}
}
