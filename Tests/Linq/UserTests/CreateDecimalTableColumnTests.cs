using System;
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
			schema.SetDataType(typeof (decimal), new SqlDataType(DataType.Decimal, 19, 4));

			var table = new SqlTable<Foo>(schema);

			Assert.That(table.Fields.Single().Value.Precision, Is.EqualTo(19));
			Assert.That(table.Fields.Single().Value.Scale, Is.EqualTo(4));
		}

		class Foo
		{
			public decimal Field { get; set; }
		}
	}
}
