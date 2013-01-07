using System;
using System.Data.Linq;
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
	public class AccessTest : TestBase
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
		public void TestDataTypes([IncludeDataContexts(ProviderName.Access)] string context)
		{
			using (var conn = new DataConnection(context))
			{
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

				TestType(conn, "binaryDataType",           new byte[] { 1, 2, 3, 4, 0, 0, 0, 0, 0, 0 });
				TestType(conn, "varbinaryDataType",        new byte[] { 1, 2, 3, 5 });
				TestType(conn, "imageDataType",            new byte[] { 3, 4, 5, 6 });
				TestType(conn, "oleobjectDataType",        new byte[] { 5, 6, 7, 8 });

				TestType(conn, "uniqueidentifierDataType", new Guid("{6F9619FF-8B86-D011-B42D-00C04FC964FF}"));
			}
		}

		static void TestNumerics<T>(DataConnection conn, T expectedValue, DataType dataType, string skip = "cbool")
		{
			var skipTypes = skip.Split(' ');

			foreach (var sqlType in new[]
				{
					"cbool",
					//"cdec",
					"cbyte",
					"clng",
					"cint",
					"ccur",
					"cdbl",
					"csng",
				}.Except(skipTypes))
			{
				var sqlValue = expectedValue is bool ? (bool)(object)expectedValue? 1 : 0 : (object)expectedValue;

				var sql = string.Format("SELECT {0}({1})", sqlType, sqlValue);

				Debug.WriteLine(sql + " -> " + typeof(T));

				Assert.That(conn.Execute<T>(sql), Is.EqualTo(expectedValue));
			}

			Assert.That(conn.Execute<T>("SELECT @p", new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
			Assert.That(conn.Execute<T>("SELECT @p", new DataParameter { Name = "p", Value = expectedValue }), Is.EqualTo(expectedValue));
			Assert.That(conn.Execute<T>("SELECT @p", new { p = expectedValue }), Is.EqualTo(expectedValue));
		}

		[Test]
		public void TestNumerics([IncludeDataContexts(ProviderName.Access)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				TestNumerics(conn, (bool)    false,   DataType.Boolean, "");
				TestNumerics(conn, (bool)    true,    DataType.Boolean, "");
				TestNumerics(conn, (bool?)   true,    DataType.Boolean, "");

				TestNumerics(conn, (sbyte)   1,       DataType.SByte);
				TestNumerics(conn, (sbyte?)  1,       DataType.SByte);
				TestNumerics(conn, sbyte.MinValue,    DataType.SByte,   "cbool cbyte");
				TestNumerics(conn, sbyte.MaxValue,    DataType.SByte);
				TestNumerics(conn, (short)   1,       DataType.Int16);
				TestNumerics(conn, (short?)  1,       DataType.Int16);
				TestNumerics(conn, short.MinValue,    DataType.Int16,   "cbool cbyte");
				TestNumerics(conn, short.MaxValue,    DataType.Int16,   "cbool cbyte");
				TestNumerics(conn, (int)     1,       DataType.Int32);
				TestNumerics(conn, (int?)    1,       DataType.Int32);
				TestNumerics(conn, int.MinValue,      DataType.Int32,   "cbool cbyte cint");
				TestNumerics(conn, int.MaxValue,      DataType.Int32,   "cbool cbyte cint csng");
				TestNumerics(conn, (long)    1L,      DataType.Int64);
				TestNumerics(conn, (long?)   1L,      DataType.Int64);
				TestNumerics(conn, long.MinValue,     DataType.Int64,   "cbool cbyte cint clng ccur");
				TestNumerics(conn, long.MaxValue,     DataType.Int64,   "cbool cbyte cint clng ccur cdbl csng");

				TestNumerics(conn, (byte)    1,       DataType.Byte);
				TestNumerics(conn, (byte?)   1,       DataType.Byte);
				TestNumerics(conn, byte.MaxValue,     DataType.Byte);
				TestNumerics(conn, (ushort)  1,       DataType.UInt16);
				TestNumerics(conn, (ushort?) 1,       DataType.UInt16);
				TestNumerics(conn, ushort.MaxValue,   DataType.UInt16,  "cbool cbyte cint");
				TestNumerics(conn, (uint)    1u,      DataType.UInt32);
				TestNumerics(conn, (uint?)   1u,      DataType.UInt32);
				TestNumerics(conn, uint.MaxValue,     DataType.UInt32,  "cbool cbyte cint clng csng");
				TestNumerics(conn, (ulong)   1ul,     DataType.UInt64);
				TestNumerics(conn, (ulong?)  1ul,     DataType.UInt64);
				TestNumerics(conn, ulong.MaxValue,    DataType.UInt64,  "cbool cbyte cint clng csng ccur cdbl");

				TestNumerics(conn, (float)   1,       DataType.Single);
				TestNumerics(conn, (float?)  1,       DataType.Single);
				TestNumerics(conn, -3.40282306E+38f,  DataType.Single,  "cbool cbyte clng cint ccur");
				TestNumerics(conn, 3.40282306E+38f,   DataType.Single,  "cbool cbyte clng cint ccur");
				TestNumerics(conn, (double)  1d,      DataType.Double);
				TestNumerics(conn, (double?) 1d,      DataType.Double);
				TestNumerics(conn, -1.79E+308d,       DataType.Double,  "cbool cbyte clng cint ccur csng");
				TestNumerics(conn,  1.79E+308d,       DataType.Double,  "cbool cbyte clng cint ccur csng");
				TestNumerics(conn, (decimal) 1m,      DataType.Decimal);
				TestNumerics(conn, (decimal?)1m,      DataType.Decimal);
				TestNumerics(conn, decimal.MinValue,  DataType.Decimal, "cbool cbyte clng cint ccur cdbl csng");
				TestNumerics(conn, decimal.MaxValue,  DataType.Decimal, "cbool cbyte clng cint ccur cdbl csng");
				TestNumerics(conn, (decimal) 1m,      DataType.Money);
				TestNumerics(conn, (decimal?)1m,      DataType.Money);
				TestNumerics(conn, -922337203685477m, DataType.Money,   "cbool cbyte clng cint csng");
				TestNumerics(conn, +922337203685477m, DataType.Money,   "cbool cbyte clng cint csng");
				TestNumerics(conn, (decimal) 1m,      DataType.SmallMoney);
				TestNumerics(conn, (decimal?)1m,      DataType.SmallMoney);
				TestNumerics(conn, -214748m,          DataType.SmallMoney, "cbool cbyte cint");
				TestNumerics(conn, +214748m,          DataType.SmallMoney, "cbool cbyte cint");
			}
		}

		[Test]
		public void TestDateTime([IncludeDataContexts(ProviderName.Access)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT cdate('2012-12-12 12:12:12')"), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT CDate('2012-12-12 12:12:12')"), Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime> ("SELECT @p", DataParameter.DateTime("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime)),                    Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestChar([IncludeDataContexts(ProviderName.Access)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<char> ("SELECT CStr('1')"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT CStr('1')"), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT @p",       DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p",       DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT CStr(@p)", DataParameter.Char("p",  '1')), Is.EqualTo('1'));

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
		public void TestString([IncludeDataContexts(ProviderName.Access)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT CStr('12345')"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT NULL"),          Is.Null);

				Assert.That(conn.Execute<string>("SELECT @p & 1", DataParameter.Char    ("p", "123")), Is.EqualTo("1231"));
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
		public void TestBinary([IncludeDataContexts(ProviderName.Access)] string context)
		{
			var arr1 = new byte[] { 48, 57 };

			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Binary   ("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create   ("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", null)), Is.EqualTo(null));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", new byte[0])), Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Image    ("p", new byte[0])), Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create   ("p", new Binary(arr1))), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter("p", new Binary(arr1))), Is.EqualTo(arr1));
			}
		}

		[Test]
		public void TestGuid([IncludeDataContexts(ProviderName.Access)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var guid = Guid.NewGuid();

				Assert.That(conn.Execute<Guid>("SELECT @p", DataParameter.Create("p", guid)),                Is.EqualTo(guid));
				Assert.That(conn.Execute<Guid>("SELECT @p", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test]
		public void TestSqlVariant([IncludeDataContexts(ProviderName.Access)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<object>("SELECT CVar(1)"), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT CVar(1)"), Is.EqualTo(1));
				Assert.That(conn.Execute<int?>  ("SELECT CVar(1)"), Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT CVar(1)"), Is.EqualTo("1"));

				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Variant("p", 1)), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestXml([IncludeDataContexts(ProviderName.Access)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>     ("SELECT '<xml/>'"),            Is.EqualTo("<xml/>"));
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
		public void TestEnum1([IncludeDataContexts(ProviderName.Access)] string context)
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
		public void TestEnum2([IncludeDataContexts(ProviderName.Access)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<string>("SELECT @p", new { p = TestEnum.AA }).           First(), Is.EqualTo("A"));
				Assert.That(conn.Query<string>("SELECT @p", new { p = (TestEnum?)TestEnum.BB }).First(), Is.EqualTo("B"));
				Assert.That(conn.Query<string>("SELECT @p", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }).First(), Is.EqualTo("A"));
				Assert.That(conn.Query<string>("SELECT @p", new { p = ConvertTo<string>.From(TestEnum.AA) }).First(), Is.EqualTo("A"));
				Assert.That(conn.Query<string>("SELECT @p", new { p = conn.MappingSchema.GetConverter<TestEnum?,string>()(TestEnum.AA) }).First(), Is.EqualTo("A"));
			}
		}

		[Test]
		public void TestCast([IncludeDataContexts(ProviderName.Access)] string context)
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
