using System.Linq;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.Metadata;

using NUnit.Framework;

namespace Tests.Metadata
{
	[TestFixture]
	public class AttributeReaderTests : TestBase
	{
		[Table("TestTable")]
		public sealed class TestEntity
		{
			public int Field1;
			[Column(Name = "TestName")]
			public int Property1 { get; set; }

		}
		[Test]
		public void TypeAttribute()
		{
			var rd    = new AttributeReader();
			var attrs = rd.GetAttributes(typeof(TestEntity))
				.OfType<TableAttribute>().ToArray();

			Assert.NotNull (attrs);
			Assert.AreEqual(1, attrs.Length);
			Assert.AreEqual("TestTable", attrs[0].Name);
		}

		[Test]
		public void FieldAttribute()
		{
			var rd    = new AttributeReader();
			var attrs = rd.GetAttributes(typeof(TestEntity), MemberHelper.MemberOf<TestEntity>(a => a.Field1))
				.OfType<ColumnAttribute>().ToArray();

			Assert.AreEqual(0, attrs.Length);
		}

		[Test]
		public void PropertyAttribute()
		{
			var rd    = new AttributeReader();
			var attrs = rd.GetAttributes(typeof(TestEntity), MemberHelper.MemberOf<TestEntity>(a => a.Property1))
				.OfType<ColumnAttribute>().ToArray();

			Assert.NotNull (attrs);
			Assert.AreEqual(1, attrs.Length);
			Assert.AreEqual("TestName", attrs[0].Name);
		}
	}
}
