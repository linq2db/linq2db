using System;
using System.Data.Linq.Mapping;

using LinqToDB.Expressions;
using LinqToDB.Metadata;

using NUnit.Framework;

namespace Tests.Metadata
{
	[TestFixture]
	public class AttributeReaderTests : TestBase
	{
		[Test]
		public void TypeAttribute()
		{
			var rd    = new AttributeReader();
			var attrs = rd.GetAttributes<TestFixtureAttribute>(typeof(AttributeReaderTests));

			Assert.NotNull (attrs);
			Assert.AreEqual(1, attrs.Length);
			Assert.AreEqual(null, attrs[0].Description);
		}

		public int Field1;

		[Test]
		public void FieldAttribute()
		{
			var rd    = new AttributeReader();
			var attrs = rd.GetAttributes<ColumnAttribute>(typeof(AttributeReaderTests), MemberHelper.MemberOf<AttributeReaderTests>(a => a.Field1));

			Assert.AreEqual(0, attrs.Length);
		}

		[Column(Name = "TestName")]
		public int Property1 { get; set; }

		[Test]
		public void PropertyAttribute()
		{
			var rd    = new AttributeReader();
			var attrs = rd.GetAttributes<ColumnAttribute>(typeof(AttributeReaderTests), MemberHelper.MemberOf<AttributeReaderTests>(a => a.Property1));

			Assert.NotNull (attrs);
			Assert.AreEqual(1, attrs.Length);
			Assert.AreEqual("TestName", attrs[0].Name);
		}
	}
}
