using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Linq;
using LinqToDB.Mapping;

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

		[Table(Name="Person")]
		public class TestPerson1
		{
			[Column] public int    PersonID;
			         public string FirstName;
		}

		[Test, ExpectedException(typeof(LinqException))]
		public void MapIgnore2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<TestPerson1>().FirstOrDefault(_ => _.FirstName == null);
		}

		enum Enum4
		{
			[MapValue(15)]
			Value1,
			Value2,
		}

		[Test, ExpectedException(
			typeof(LinqToDBException),
			ExpectedMessage = "Inconsistent mapping. 'Tests.Exceptions.Mapping+Enum4.Value2' does not have MapValue(<System.Int32>) attribute.")]
		public void ConvertFromEnum()
		{
			Assert.AreEqual(15, ConvertTo<int>.From(Enum4.Value1));
			Assert.AreEqual(25, ConvertTo<int>.From(Enum4.Value2));
		}
	}
}
