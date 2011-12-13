using System;
using System.Linq;

using NUnit.Framework;

using LinqToDB.Data.Linq;

namespace Data.Exceptions
{
	using Linq;
	using Linq.Model;

	[TestFixture]
	public class JoinTest : TestBase
	{
		[Test, ExpectedException(typeof(LinqException))]
		public void InnerJoin()
		{
			ForEachProvider(typeof(NotSupportedException), db =>
			{
				var q =
					from p1 in db.Person
						join p2 in db.Person on new Person { FirstName = "", ID = p1.ID } equals new Person { ID = p2.ID }
					where p1.ID == 1
					select new Person { ID = p1.ID, FirstName = p2.FirstName };
				q.ToList();
			});
		}
	}
}
