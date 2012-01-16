using System;
using System.Data.Linq.Mapping;

using LinqToDB_Temp.Metadata;

using NUnit.Framework;

namespace Tests.Metadata
{
	[TestFixture]
	public class AttributeReaderTest : TestBase
	{
		[Test]
		public void TypeAttribute()
		{
			var rd    = new AttributeReader();
			var attrs = rd.GetAttributes<TestFixtureAttribute>(typeof(AttributeReaderTest));

			Assert.NotNull (attrs);
			Assert.AreEqual(1, attrs.Length);
			Assert.AreEqual(null, attrs[0].Description);
		}

		public int Field1;

		[Test]
		public void FieldAttribute()
		{
			var rd    = new AttributeReader();
			var attrs = rd.GetAttributes<ColumnAttribute>(typeof(AttributeReaderTest), "Field1");

			Assert.IsNull(attrs);
		}

		[Column(Name = "TestName")]
		public int Property1 { get; set; }

		[Test]
		public void PropertyAttribute()
		{
			var rd    = new AttributeReader();
			var attrs = rd.GetAttributes<ColumnAttribute>(typeof(AttributeReaderTest), "Property1");

			Assert.NotNull (attrs);
			Assert.AreEqual(1, attrs.Length);
			Assert.AreEqual("TestName", attrs[0].Name);
		}
	}
}
