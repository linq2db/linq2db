using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class SQLiteTest : TestBase
	{
		static void TestType<T>(DataConnection connection, string dataTypeName, T value, string tableName = "AllTypes", bool convertToString = false)
		{
			Assert.That(connection.Execute<T>(string.Format("SELECT {0} FROM {1} WHERE ID = 1", dataTypeName, tableName)),
				Is.EqualTo(connection.MappingSchema.GetDefaultValue(typeof(T))));

			object actualValue   = connection.Execute<T>(string.Format("SELECT {0} FROM {1} WHERE ID = 2", dataTypeName, tableName));
			object expectedValue = value;

			if (convertToString)
			{
				actualValue   = actualValue.  ToString();
				expectedValue = expectedValue.ToString();
			}

			Assert.That(actualValue, Is.EqualTo(expectedValue));
		}

		[Test]
		public void TestDataTypes([IncludeDataContexts(ProviderName.SQLite)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				TestType(conn, "bigintDataType",           1000000L);
				TestType(conn, "numericDataType",          9999999m);
				TestType(conn, "bitDataType",              true);
				TestType(conn, "smallintDataType",         (short)25555);
				TestType(conn, "decimalDataType",          2222222m);
				TestType(conn, "intDataType",              7777777);
				TestType(conn, "tinyintDataType",          (sbyte)100);
				TestType(conn, "moneyDataType",            100000m);
				TestType(conn, "floatDataType",            20.31d);
				TestType(conn, "realDataType",             16.2f);

				TestType(conn, "datetimeDataType",         new DateTime(2012, 12, 12, 12, 12, 12));

				TestType(conn, "charDataType",             '1');
				TestType(conn, "varcharDataType",          "234");
				TestType(conn, "textDataType",             "567");
				TestType(conn, "ncharDataType",            "23233");
				TestType(conn, "nvarcharDataType",         "3323");
				TestType(conn, "ntextDataType",            "111");

				TestType(conn, "binaryDataType",           new byte[] { 1 });
				TestType(conn, "varbinaryDataType",        new byte[] { 2 });
				TestType(conn, "imageDataType",            new byte[] { 0, 0, 0, 3 });

				TestType(conn, "uniqueidentifierDataType", new Guid("{6F9619FF-8B86-D011-B42D-00C04FC964FF}"));
				TestType(conn, "objectDataType",           (object)10);
			}
		}

		static void TestNumerics<T>(DataConnection conn, T expectedValue, DataType dataType, string skip = "")
		{
			var skipTypes = skip.Split(' ');

			foreach (var sqlType in new[]
				{
					"bigint",
					"bit",
					"decimal",
					"int",
					"money",
					"numeric",
					"smallint",
					"tinyint",

					"float",
					"real"
				}.Except(skipTypes))
			{
				var sqlValue = expectedValue is bool ? (bool)(object)expectedValue? 1 : 0 : (object)expectedValue;

				var sql = string.Format("SELECT Cast({0} as {1})", sqlValue, sqlType);

				Debug.WriteLine(sql + " -> " + typeof(T));

				Assert.That(conn.Execute<T>(sql), Is.EqualTo(expectedValue));
			}

			Assert.That(conn.Execute<T>("SELECT @p", new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
			Assert.That(conn.Execute<T>("SELECT @p", new DataParameter { Name = "p", Value = expectedValue }), Is.EqualTo(expectedValue));
			Assert.That(conn.Execute<T>("SELECT @p", new { p = expectedValue }), Is.EqualTo(expectedValue));
		}

		[Test]
		public void TestNumerics([IncludeDataContexts(ProviderName.SQLite)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				TestNumerics(conn, (bool)    true,    DataType.Boolean);
				TestNumerics(conn, (bool?)   true,    DataType.Boolean);

				TestNumerics(conn, (sbyte)   1,       DataType.SByte);
				TestNumerics(conn, (sbyte?)  1,       DataType.SByte);
				TestNumerics(conn, sbyte.MinValue,    DataType.SByte);
				TestNumerics(conn, sbyte.MaxValue,    DataType.SByte);
				TestNumerics(conn, (short)   1,       DataType.Int16);
				TestNumerics(conn, (short?)  1,       DataType.Int16);
				TestNumerics(conn, short.MinValue,    DataType.Int16);
				TestNumerics(conn, short.MaxValue,    DataType.Int16);
				TestNumerics(conn, (int)     1,       DataType.Int32);
				TestNumerics(conn, (int?)    1,       DataType.Int32);
				TestNumerics(conn, int.MinValue,      DataType.Int32);
				TestNumerics(conn, int.MaxValue,      DataType.Int32);
				TestNumerics(conn, (long)    1L,      DataType.Int64);
				TestNumerics(conn, (long?)   1L,      DataType.Int64);
				TestNumerics(conn, long.MinValue,     DataType.Int64);
				TestNumerics(conn, long.MaxValue,     DataType.Int64,     "float real");

				TestNumerics(conn, (byte)    1,       DataType.Byte);
				TestNumerics(conn, (byte?)   1,       DataType.Byte);
				TestNumerics(conn, byte.MaxValue,     DataType.Byte);
				TestNumerics(conn, (ushort)  1,       DataType.UInt16);
				TestNumerics(conn, (ushort?) 1,       DataType.UInt16);
				TestNumerics(conn, ushort.MaxValue,   DataType.UInt16);
				TestNumerics(conn, (uint)    1u,      DataType.UInt32);
				TestNumerics(conn, (uint?)   1u,      DataType.UInt32);
				TestNumerics(conn, uint.MaxValue,     DataType.UInt32);
				TestNumerics(conn, (ulong)   1ul,     DataType.UInt64);
				TestNumerics(conn, (ulong?)  1ul,     DataType.UInt64);
				TestNumerics(conn, ulong.MaxValue,    DataType.UInt64,     "bigint bit decimal int money numeric smallint tinyint float real");

				TestNumerics(conn, (float)   1,       DataType.Single);
				TestNumerics(conn, (float?)  1,       DataType.Single);
				TestNumerics(conn, -3.40282306E+38f,  DataType.Single,     "bigint int smallint tinyint");
				TestNumerics(conn, 3.40282306E+38f,   DataType.Single,     "bigint int smallint tinyint");
				TestNumerics(conn, (double)  1d,      DataType.Double);
				TestNumerics(conn, (double?) 1d,      DataType.Double);
				TestNumerics(conn, -1.7900000000000008E+308d, DataType.Double, "bigint int smallint tinyint");
				TestNumerics(conn,  1.7900000000000008E+308d, DataType.Double, "bigint int smallint tinyint");
				TestNumerics(conn, (decimal) 1m,      DataType.Decimal);
				TestNumerics(conn, (decimal?)1m,      DataType.Decimal);
				TestNumerics(conn, decimal.MinValue,  DataType.Decimal,    "bigint bit decimal int money numeric smallint tinyint float real");
				TestNumerics(conn, decimal.MaxValue,  DataType.Decimal,    "bigint bit decimal int money numeric smallint tinyint float real");
				TestNumerics(conn, (decimal) 1m,      DataType.VarNumeric);
				TestNumerics(conn, (decimal?)1m,      DataType.VarNumeric);
				TestNumerics(conn, decimal.MinValue,  DataType.VarNumeric, "bigint bit decimal int money numeric smallint tinyint float real");
				TestNumerics(conn, decimal.MaxValue,  DataType.VarNumeric, "bigint bit decimal int money numeric smallint tinyint float real");
				TestNumerics(conn, (decimal) 1m,      DataType.Money);
				TestNumerics(conn, (decimal?)1m,      DataType.Money);
				TestNumerics(conn, -922337203685477m, DataType.Money);
				TestNumerics(conn, +922337203685477m, DataType.Money);
				TestNumerics(conn, (decimal) 1m,      DataType.SmallMoney);
				TestNumerics(conn, (decimal?)1m,      DataType.SmallMoney);
				TestNumerics(conn, -214748m,          DataType.SmallMoney);
				TestNumerics(conn, +214748m,          DataType.SmallMoney);
			}
		}

		[Test]
		public void TestDateTime([IncludeDataContexts(ProviderName.SQLite)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT '2012-12-12 12:12:12'"), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT '2012-12-12 12:12:12'"), Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime> ("SELECT @p", DataParameter.DateTime("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime)),                    Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestChar([IncludeDataContexts(ProviderName.SQLite)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char)"),         Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char)"),         Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char(1))"),      Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char(1))"),      Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast('1' as varchar)"),      Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar)"),      Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as varchar(20))"),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar(20))"),  Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast('1' as nchar)"),        Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nchar)"),        Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as nchar(20))"),    Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nchar(20))"),    Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast('1' as nvarchar)"),     Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nvarchar)"),     Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as nvarchar(20))"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nvarchar(20))"), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT @p",                  DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p",                  DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char)",    DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char)",    DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char(1))", DataParameter.Char("@p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1))", DataParameter.Char("@p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT @p", DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p", DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT @p", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT @p", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT @p", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT @p", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
			}
		}

		[Test]
		public void TestString([IncludeDataContexts(ProviderName.SQLite)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char)"),          Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20))"),      Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as char(20))"),      Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar)"),       Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(20))"),   Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as varchar(20))"),   Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as text)"),          Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as text)"),          Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nchar)"),         Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nchar(20))"),     Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as nchar(20))"),     Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nvarchar)"),      Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nvarchar(20))"),  Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as nvarchar(20))"),  Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as ntext)"),         Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as ntext)"),         Is.Null);

				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Char    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.VarChar ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Text    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NChar   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NText   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Create  ("p", "123")), Is.EqualTo("123"));

				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Create("p", (string)null)), Is.EqualTo(null));
				Assert.That(conn.Execute<string>("SELECT @p", new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestBinary([IncludeDataContexts(ProviderName.SQLite)] string context)
		{
			var arr1 = new byte[] { 1 };
			var arr2 = new byte[] { 2 };

			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<byte[]>("SELECT    binaryDataType FROM AllTypes WHERE ID = 2"), Is.EqualTo(           arr1));
				Assert.That(conn.Execute<Binary>("SELECT varbinaryDataType FROM AllTypes WHERE ID = 2"), Is.EqualTo(new Binary(arr2)));

				Assert.That(conn.Execute<byte[]>("SELECT    binaryDataType FROM AllTypes WHERE ID = 2"), Is.EqualTo(           arr1));
				Assert.That(conn.Execute<Binary>("SELECT varbinaryDataType FROM AllTypes WHERE ID = 2"), Is.EqualTo(new Binary(arr2)));

				Assert.That(conn.Execute<byte[]>("SELECT Cast(NULL as image)"),           Is.EqualTo(null));

				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Binary   ("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create   ("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", null)), Is.EqualTo(null));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Binary   ("p", new byte[0])), Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", new byte[0])), Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Image    ("p", new byte[0])), Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create   ("p", new Binary(arr1))), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter("p", new Binary(arr1))), Is.EqualTo(arr1));
			}
		}

		[Test]
		public void TestGuid([IncludeDataContexts(ProviderName.SQLite)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(
					conn.Execute<Guid>("SELECT uniqueidentifierDataType FROM AllTypes WHERE ID = 2"),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				Assert.That(
					conn.Execute<Guid?>("SELECT '6F9619FF-8B86-D011-B42D-00C04FC964FF'"),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				var guid = Guid.NewGuid();

				Assert.That(conn.Execute<Guid>("SELECT @p", DataParameter.Create("p", guid)),                Is.EqualTo(guid));
				Assert.That(conn.Execute<Guid>("SELECT @p", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test]
		public void TestObject([IncludeDataContexts(ProviderName.SQLite)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<object>("SELECT Cast(1 as Object)"), Is.EqualTo(1));
				Assert.That(conn.Execute<int>   ("SELECT Cast(1 as Object)"), Is.EqualTo(1));
				Assert.That(conn.Execute<int?>  ("SELECT Cast(1 as Object)"), Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT Cast(1 as Object)"), Is.EqualTo("1"));

				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Variant("p", 1)), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestXml([IncludeDataContexts(ProviderName.SQLite)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<XDocument>  ("SELECT '<xml/>'").ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT '<xml/>'").InnerXml,   Is.EqualTo("<xml />"));

				var xdoc = XDocument.Parse("<xml/>");
				var xml  = Convert<string,XmlDocument>.Lambda("<xml/>");

				Assert.That(conn.Execute<string>     ("SELECT @p", DataParameter.Xml("p", "<xml/>")),        Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>  ("SELECT @p", DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT @p", DataParameter.Xml("p", xml)). InnerXml,   Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  ("SELECT @p", new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  ("SELECT @p", new DataParameter("p", xml)). ToString(), Is.EqualTo("<xml />"));
			}
		}

		enum TestEnum
		{
			[MapValue("A")] AA,
			[MapValue("B")] BB,
		}

		[Test]
		public void TestEnum1([IncludeDataContexts(ProviderName.SQLite)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<TestEnum> ("SELECT 'A'"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'A'"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum> ("SELECT 'B'"), Is.EqualTo(TestEnum.BB));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'B'"), Is.EqualTo(TestEnum.BB));
			}
		}

		[Test]
		public void TestEnum2([IncludeDataContexts(ProviderName.SQLite)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT @p", new { p = TestEnum.AA }),            Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = (TestEnum?)TestEnum.BB }), Is.EqualTo("B"));

				Assert.That(conn.Execute<string>("SELECT @p", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = conn.MappingSchema.GetConverter<TestEnum?,string>()(TestEnum.AA) }), Is.EqualTo("A"));
			}
		}

		[Test]
		public void TestCast([IncludeDataContexts(ProviderName.SQLite)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT @p", new { p =  1  }), Is.EqualTo("1"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = "1" }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT @p", new { p =  new DataParameter { Value = 1 } }), Is.EqualTo(1));
			}
		}
	}
}
