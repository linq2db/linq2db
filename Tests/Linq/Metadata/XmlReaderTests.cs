using System.IO;
using System.Linq;
using System.Text;

using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.Metadata;

using NUnit.Framework;

namespace Tests.Metadata
{
	public class XmlReaderTests
	{
		const string Data =
			@"<?xml version='1.0' encoding='utf-8' ?>
			<Types>
				<Type Name='MyType'>
					<Member Name='Field1'>
						<!-- 12345 -->
						<Attr1>
							<Value1 Value='2' Type='System.Int32' />
						</Attr1>
						<Attr2>
							<Value1 Value='3' />
						</Attr2>
					</Member>
					<Attr3><Value1 Value='4' Type='System.Int32' /></Attr3>
				</Type>

				<Type Name='XmlReaderTests'>
					<Table>
						<Name Value='TestName' />
					</Table>
					<Member Name='Field1'>
						<ColumnAttribute>
							<Name Value='TestName' />
						</ColumnAttribute>
					</Member>
					<Member Name='Property1'>
						<LinqToDB.Mapping.ColumnAttribute>
							<Name Value='TestName' />
						</LinqToDB.Mapping.ColumnAttribute>
					</Member>
				</Type>
			</Types>";

		[Test]
		public void Parse()
		{
			new XmlAttributeReader(new MemoryStream(Encoding.UTF8.GetBytes(Data)));
		}

		[Test]
		public void TypeAttribute()
		{
			var rd    = new XmlAttributeReader(new MemoryStream(Encoding.UTF8.GetBytes(Data)));
			var attrs = rd.GetAttributes(typeof(XmlReaderTests)).OfType<TableAttribute>().ToArray();

			Assert.That(attrs, Is.Not.Null);
			Assert.That(attrs, Has.Length.EqualTo(1));
			Assert.That(attrs[0].Name, Is.EqualTo("TestName"));
		}

		public int Field1;

		[Test]
		public void FieldAttribute()
		{
			var rd    = new XmlAttributeReader(new MemoryStream(Encoding.UTF8.GetBytes(Data)));
			var attrs = rd.GetAttributes(typeof(XmlReaderTests), MemberHelper.MemberOf<XmlReaderTests>(a => a.Field1))
				.OfType<ColumnAttribute>().ToArray();

			Assert.That(attrs, Is.Not.Null);
			Assert.That(attrs, Has.Length.EqualTo(1));
			Assert.That(attrs[0].Name, Is.EqualTo("TestName"));
		}

		public int Property1 { get; set; }

		[Test]
		public void PropertyAttribute()
		{
			var rd    = new XmlAttributeReader(new MemoryStream(Encoding.UTF8.GetBytes(Data)));
			var attrs = rd.GetAttributes(typeof(XmlReaderTests), MemberHelper.MemberOf<XmlReaderTests>(a => a.Property1))
				.OfType<ColumnAttribute>().ToArray();

			Assert.That(attrs, Is.Not.Null);
			Assert.That(attrs, Has.Length.EqualTo(1));
			Assert.That(attrs[0].Name, Is.EqualTo("TestName"));
		}
	}
}
