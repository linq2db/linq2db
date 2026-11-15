using System.Linq;

using LinqToDB;
using LinqToDB.Common;
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
			using var db = GetDataContext(context);
			var q = from p in db.Person where p.Name == "123" select p;
			Assert.That(() => q.ToList(), Throws.TypeOf<LinqToDBException>());
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
			using var db = GetDataContext(context);
			Assert.That(() => db.GetTable<TestPerson1>().FirstOrDefault(_ => _.FirstName == null), Throws.TypeOf<LinqToDBException>());
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
			Assert.That(
				() => ConvertTo<int>.From(Enum4.Value1),
				Throws.TypeOf<LinqToDBConvertException>().With.Message.EqualTo("Inconsistent mapping. 'Tests.Exceptions.MappingTests+Enum4.Value2' does not have MapValue(<System.Int32>) attribute."));

			Assert.That(
				() => ConvertTo<int>.From(Enum4.Value2),
				Throws.TypeOf<LinqToDBConvertException>().With.Message.EqualTo("Inconsistent mapping. 'Tests.Exceptions.MappingTests+Enum4.Value2' does not have MapValue(<System.Int32>) attribute."));
		}
	}
}
