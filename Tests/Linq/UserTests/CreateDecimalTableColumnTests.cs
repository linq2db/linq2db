using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public sealed class CreateDecimalTableColumnTests
	{
		[Test]
		public void Test()
		{
			var schema = new MappingSchema();
			schema.SetDataType(typeof (decimal), new SqlDataType(DataType.Decimal, typeof(decimal), 19, 4));

			var table = new SqlTable(schema.GetEntityDescriptor(typeof(Foo)));

			Assert.Multiple(() =>
			{
				Assert.That(table.Fields.Single().Type!.Precision, Is.EqualTo(19));
				Assert.That(table.Fields.Single().Type!.Scale, Is.EqualTo(4));
			});
		}

		sealed class Foo
		{
			public decimal Field { get; set; }
		}
	}
}
