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
		[Test]
		public void MapIgnore1([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.Name == "123" select p;
				Assert.Throws<LinqToDBException>(() => q.ToList());
			}
		}

		[Table(Name="Person")]
		public class TestPerson1
		{
			[Column] public int    PersonID;
			         public string FirstName = null!;
		}

		[Test]
		public void MapIgnore2([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.Throws<LinqToDBException>(() => db.GetTable<TestPerson1>().FirstOrDefault(_ => _.FirstName == null));
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
			Assert.Throws<LinqToDBConvertException>(
				() => ConvertTo<int>.From(Enum4.Value1),
				"Inconsistent mapping. 'Tests.Exceptions.Mapping+Enum4.Value2' does not have MapValue(<System.Int32>) attribute.");

			Assert.Throws<LinqToDBConvertException>(
				() => ConvertTo<int>.From(Enum4.Value2),
				"Inconsistent mapping. 'Tests.Exceptions.Mapping+Enum4.Value2' does not have MapValue(<System.Int32>) attribute.");
		}
	}
}
