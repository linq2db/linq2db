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
							<Name Value='TestName1' />
						</ColumnAttribute>
					</Member>
					<Member Name='Field2'>
						<ColumnAttribute>
							<Name Value='TestName2' />
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
			[LinqToDB_Temp.Column(Config = "Test1", Name = "FieldName31")]
			[LinqToDB_Temp.Column(Config = "Test2", Name = "FieldName32")]
			[LinqToDB_Temp.Column(                  Name = "FieldName33")]
			public int Field3 = 0;
		}

		[Test, Sequential]
		public void MetadataTest1(
			[Values("Field1",    "Field2")]    string field,
			[Values("TestName1", "TestName2")] string fieldName)
		{
			var ms   = new MappingSchema { MetadataReader = new XmlAttributeReader(new MemoryStream(Encoding.UTF8.GetBytes(Data))) };
			var attr = ms.GetAttribute<ColumnAttribute>(typeof(TestClass), field);

			Assert.NotNull (attr);
			Assert.AreEqual(fieldName, attr.Name);
		}

		[Test]
		public void MetadataTest2()
		{
			var ms   = new MappingSchema();
			var attr = ms.GetAttribute<ColumnAttribute>(typeof(TestClass), "Field2");

			Assert.NotNull(attr);
			Assert.AreEqual("FieldName2", attr.Name);
		}

		[Test, Sequential]
		public void ConfigTest1(
			[Values("Test1",       "Test2",       null)]          string config,
			[Values("FieldName31", "FieldName32", "FieldName33")] string fieldName)
		{
			var ms1   = new MappingSchema(config);
			var attr1 = ms1.GetAttribute<LinqToDB_Temp.ColumnAttribute>(typeof(TestClass), "Field3", c => c.Config);

			Assert.NotNull(attr1);
			Assert.AreEqual(fieldName, attr1.Name);
		}
	}
}
