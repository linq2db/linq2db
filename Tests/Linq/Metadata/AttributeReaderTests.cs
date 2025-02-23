using System.Linq;

using LinqToDB.Internal.Expressions;
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

			Assert.That(attrs, Is.Not.Null);
			Assert.That(attrs, Has.Length.EqualTo(1));
			Assert.That(attrs[0].Name, Is.EqualTo("TestTable"));
		}

		[Test]
		public void FieldAttribute()
		{
			var rd    = new AttributeReader();
			var attrs = rd.GetAttributes(typeof(TestEntity), MemberHelper.MemberOf<TestEntity>(a => a.Field1))
				.OfType<ColumnAttribute>().ToArray();

			Assert.That(attrs, Is.Empty);
		}

		[Test]
		public void PropertyAttribute()
		{
			var rd    = new AttributeReader();
			var attrs = rd.GetAttributes(typeof(TestEntity), MemberHelper.MemberOf<TestEntity>(a => a.Property1))
				.OfType<ColumnAttribute>().ToArray();

			Assert.That(attrs, Is.Not.Null);
			Assert.That(attrs, Has.Length.EqualTo(1));
			Assert.That(attrs[0].Name, Is.EqualTo("TestName"));
		}
	}
}
