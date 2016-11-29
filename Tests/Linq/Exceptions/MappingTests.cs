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
	public class MappingTests : TestBase
	{
		[Test, DataContextSource]
		public void MapIgnore1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.Name == "123" select p;
				Assert.Throws(typeof(LinqException), () => q.ToList());
			}
		}

		[Table(Name="Person")]
		public class TestPerson1
		{
			[Column] public int    PersonID;
			         public string FirstName;
		}

		[Test, DataContextSource]
		public void MapIgnore2(string context)
		{
			using (var db = GetDataContext(context))
				Assert.Throws(typeof(LinqException), () => db.GetTable<TestPerson1>().FirstOrDefault(_ => _.FirstName == null));
		}

		enum Enum4
		{
			[MapValue(15)]
			Value1,
			Value2,
		}

		[Test]
		public void ConvertFromEnum()
		{
			Assert.Throws(
				typeof(LinqToDBConvertException),
				() => ConvertTo<int>.From(Enum4.Value1),
				"Inconsistent mapping. 'Tests.Exceptions.Mapping+Enum4.Value2' does not have MapValue(<System.Int32>) attribute.");

			Assert.Throws(
				typeof(LinqToDBConvertException),
				() => ConvertTo<int>.From(Enum4.Value2),
				"Inconsistent mapping. 'Tests.Exceptions.Mapping+Enum4.Value2' does not have MapValue(<System.Int32>) attribute.");
		}
	}
}
