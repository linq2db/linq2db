using System;
using System.Linq;

using LinqToDB.Data.Linq;
using LinqToDB.DataAccess;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Data.Exceptions
{
	using Linq;

	[TestFixture]
	public class Mapping : TestBase
	{
		[Test, ExpectedException(typeof(LinqException))]
		public void MapIgnore1()
		{
			ForEachProvider(typeof(LinqException), db =>
			{
				var q = from p in db.Person where p.Name == "123" select p;
				q.ToList();
			});
		}

		[TableName("Person")]
		public class TestPerson1
		{
			            public int    PersonID;
			[MapIgnore] public string FirstName;
		}

		[Test, ExpectedException(typeof(LinqException))]
		public void MapIgnore2()
		{
			ForEachProvider(typeof(LinqException), db =>
				db.GetTable<TestPerson1>()
					.Where (_ => _.PersonID == 1)
					.Select(_ => _.FirstName)
					.FirstOrDefault());
		}
	}
}
