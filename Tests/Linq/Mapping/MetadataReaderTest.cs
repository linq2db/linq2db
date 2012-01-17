using System;
using System.Data.Linq.Mapping;
using System.IO;
using System.Text;
using LinqToDB_Temp.Mapping;
using LinqToDB_Temp.Metadata;

using NUnit.Framework;

namespace Tests.Mapping
{
	[TestFixture]
	public class MetadataReaderTest : TestBase
	{
		const string Data =
			@"<?xml version='1.0' encoding='utf-8' ?>
			<Types xmlns='urn:schemas-bltoolkit-net:typeext'>
				<Type Name='TestClass'>
					<Table>
						<Name Value='TestName' />
					</Table>
					<Member Name='Field1'>
						<ColumnAttribute>
							<Name Value='TestName' />
						</ColumnAttribute>
					</Member>
					<Member Name='Field2'>
						<ColumnAttribute>
							<Name Value='TestName' />
						</ColumnAttribute>
					</Member>
					<Member Name='Property1'>
						<System.Data.Linq.Mapping.ColumnAttribute>
							<Name Value='TestName' />
						</System.Data.Linq.Mapping.ColumnAttribute>
					</Member>
				</Type>
			</Types>";

		class TestClass
		{
			public int Field1 = 0;
			[Column(Name = "FieldName2")]
			public int Field2 = 0;
		}

		[Test]
		public void MetadataTest1()
		{
			var ms   = new MappingSchema { MetadataReader = new XmlAttributeReader(new MemoryStream(Encoding.UTF8.GetBytes(Data))) };
			var attr = ms.GetAttribute<ColumnAttribute>(typeof(TestClass), "Field1");

			Assert.NotNull (attr);
			Assert.AreEqual("TestName", attr.Name);
		}

		[Test]
		public void MetadataTest2()
		{
			var ms = new MappingSchema { MetadataReader = new XmlAttributeReader(new MemoryStream(Encoding.UTF8.GetBytes(Data))) };
			var attr = ms.GetAttribute<ColumnAttribute>(typeof(TestClass), "Field2");

			Assert.NotNull(attr);
			Assert.AreEqual("TestName", attr.Name);
		}

		[Test]
		public void MetadataTest3()
		{
			var ms = new MappingSchema();
			var attr = ms.GetAttribute<ColumnAttribute>(typeof(TestClass), "Field2");

			Assert.NotNull(attr);
			Assert.AreEqual("FieldName2", attr.Name);
		}
	}
}
