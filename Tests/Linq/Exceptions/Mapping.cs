using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Linq;

using NUnit.Framework;

namespace Tests.Exceptions
{
	[TestFixture]
	public class Mapping : TestBase
	{
		[Test, ExpectedException(typeof(LinqException))]
		public void MapIgnore1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.Name == "123" select p;
				q.ToList();
			}
		}

		[TableName("Person")]
		public class TestPerson1
		{
			            public int    PersonID;
			[MapIgnore] public string FirstName;
		}

		[Test, ExpectedException(typeof(LinqException))]
		public void MapIgnore2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<TestPerson1>().FirstOrDefault(_ => _.FirstName == null);
		}
	}
}
