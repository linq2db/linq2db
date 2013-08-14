using System;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.Access;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class AccessTest : DataProviderTestBase
	{
		[Test]
		public void TestParameters([IncludeDataContexts(ProviderName.Access)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT @p",        new { p =  1  }), Is.EqualTo("1"));
				Assert.That(conn.Execute<string>("SELECT @p",        new { p = "1" }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT @p",        new { p =  new DataParameter { Value = 1   } }), Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT @p1",       new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT @p1 + @p2", new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
				Assert.That(conn.Execute<int>   ("SELECT @p2 + @p1", new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
			}
		}

		[Test]
		public void TestDataTypes([IncludeDataContexts(ProviderName.Access)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(TestType<bool>     (conn, "bitDataType",              DataType.Boolean),   Is.EqualTo(true));
				Assert.That(TestType<short?>   (conn, "smallintDataType",         DataType.Int16),     Is.EqualTo(25555));
				Assert.That(TestType<decimal?> (conn, "decimalDataType",          DataType.Decimal),   Is.EqualTo(2222222m));
				Assert.That(TestType<int?>     (conn, "intDataType",              DataType.Int32),     Is.EqualTo(7777777));
				Assert.That(TestType<sbyte?>   (conn, "tinyintDataType",          DataType.SByte),     Is.EqualTo(100));
				Assert.That(TestType<decimal?> (conn, "moneyDataType",            DataType.Money),     Is.EqualTo(100000m));
				Assert.That(TestType<double?>  (conn, "floatDataType",            DataType.Double),    Is.EqualTo(20.31d));
				Assert.That(TestType<float?>   (conn, "realDataType",             DataType.Single),    Is.EqualTo(16.2f));

				Assert.That(TestType<DateTime?>(conn, "datetimeDataType",         DataType.DateTime),  Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));

				Assert.That(TestType<char?>    (conn, "charDataType",             DataType.Char),      Is.EqualTo('1'));
				Assert.That(TestType<string>   (conn, "varcharDataType",          DataType.VarChar),   Is.EqualTo("234"));
				Assert.That(TestType<string>   (conn, "textDataType",             DataType.Text),      Is.EqualTo("567"));
				Assert.That(TestType<string>   (conn, "ncharDataType",            DataType.NChar),     Is.EqualTo("23233"));
				Assert.That(TestType<string>   (conn, "nvarcharDataType",         DataType.NVarChar),  Is.EqualTo("3323"));
				Assert.That(TestType<string>   (conn, "ntextDataType",            DataType.NText),     Is.EqualTo("111"));

				Assert.That(TestType<byte[]>   (conn, "binaryDataType",           DataType.Binary),    Is.EqualTo(new byte[] { 1, 2, 3, 4, 0, 0, 0, 0, 0, 0 }));
				Assert.That(TestType<byte[]>   (conn, "varbinaryDataType",        DataType.VarBinary), Is.EqualTo(new byte[] { 1, 2, 3, 5 }));
				Assert.That(TestType<byte[]>   (conn, "imageDataType",            DataType.Image),     Is.EqualTo(new byte[] { 3, 4, 5, 6 }));
				Assert.That(TestType<byte[]>   (conn, "oleobjectDataType",        DataType.Variant, skipDefined:true), Is.EqualTo(new byte[] { 5, 6, 7, 8 }));

				Assert.That(TestType<Guid?>    (conn, "uniqueidentifierDataType", DataType.Guid),      Is.EqualTo(new Guid("{6F9619FF-8B86-D011-B42D-00C04FC964FF}")));
			}
		}

		static void TestNumeric<T>(DataConnection conn, T expectedValue, DataType dataType, string skip = "cbool")
		{
			var skipTypes = skip.Split(' ');

			if (expectedValue != null)
				foreach (var sqlType in new[]
					{
						"cbool",
						"cbyte",
						"clng",
						"cint",
						"ccur",
						"cdbl",
						"csng"
					}.Except(skipTypes))
				{
					var sqlValue = expectedValue is bool ? (bool)(object)expectedValue? 1 : 0 : (object)expectedValue;

					var sql = string.Format("SELECT {0}({1})", sqlType, sqlValue ?? "NULL");

					Debug.WriteLine(sql + " -> " + typeof(T));

					Assert.That(conn.Execute<T>(sql), Is.EqualTo(expectedValue));
				}

			Debug.WriteLine("{0} -> DataType.{1}",  typeof(T), dataType);
			Assert.That(conn.Execute<T>("SELECT @p", new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> auto", typeof(T));
			Assert.That(conn.Execute<T>("SELECT @p", new DataParameter { Name = "p", Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> new",  typeof(T));
			Assert.That(conn.Execute<T>("SELECT @p", new { p = expectedValue }), Is.EqualTo(expectedValue));
		}

		static void TestSimple<T>(DataConnection conn, T expectedValue, DataType dataType)
			where T : struct
		{
			TestNumeric<T> (conn, expectedValue, dataType);
			TestNumeric<T?>(conn, expectedValue, dataType);
			TestNumeric<T?>(conn, (T?)null,      dataType);
		}

		[Test]
		public void TestNumerics([IncludeDataContexts(ProviderName.Access)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				TestSimple<bool>   (conn, true, DataType.Boolean);
				TestSimple<sbyte>  (conn, 1,    DataType.SByte);
				TestSimple<short>  (conn, 1,    DataType.Int16);
				TestSimple<int>    (conn, 1,    DataType.Int32);
				TestSimple<long>   (conn, 1L,   DataType.Int64);
				TestSimple<byte>   (conn, 1,    DataType.Byte);
				TestSimple<ushort> (conn, 1,    DataType.UInt16);
				TestSimple<uint>   (conn, 1u,   DataType.UInt32);
				TestSimple<ulong>  (conn, 1ul,  DataType.UInt64);
				TestSimple<float>  (conn, 1,    DataType.Single);
				TestSimple<double> (conn, 1d,   DataType.Double);
				TestSimple<decimal>(conn, 1m,   DataType.Decimal);
				TestSimple<decimal>(conn, 1m,   DataType.VarNumeric);
				TestSimple<decimal>(conn, 1m,   DataType.Money);
				TestSimple<decimal>(conn, 1m,   DataType.SmallMoney);

				TestNumeric(conn, sbyte.MinValue,    DataType.SByte,      "cbool cbyte");
				TestNumeric(conn, sbyte.MaxValue,    DataType.SByte);
				TestNumeric(conn, short.MinValue,    DataType.Int16,      "cbool cbyte");
				TestNumeric(conn, short.MaxValue,    DataType.Int16,      "cbool cbyte");
				TestNumeric(conn, int.MinValue,      DataType.Int32,      "cbool cbyte cint");
				TestNumeric(conn, int.MaxValue,      DataType.Int32,      "cbool cbyte cint csng");
				TestNumeric(conn, long.MinValue,     DataType.Int64,      "cbool cbyte cint clng ccur");
				TestNumeric(conn, long.MaxValue,     DataType.Int64,      "cbool cbyte cint clng ccur cdbl csng");

				TestNumeric(conn, byte.MaxValue,     DataType.Byte);
				TestNumeric(conn, ushort.MaxValue,   DataType.UInt16,     "cbool cbyte cint");
				TestNumeric(conn, uint.MaxValue,     DataType.UInt32,     "cbool cbyte cint clng csng");
				TestNumeric(conn, ulong.MaxValue,    DataType.UInt64,     "cbool cbyte cint clng csng ccur cdbl");

				TestNumeric(conn, -3.40282306E+38f,  DataType.Single,     "cbool cbyte clng cint ccur");
				TestNumeric(conn, 3.40282306E+38f,   DataType.Single,     "cbool cbyte clng cint ccur");
				TestNumeric(conn, -1.79E+308d,       DataType.Double,     "cbool cbyte clng cint ccur csng");
				TestNumeric(conn,  1.79E+308d,       DataType.Double,     "cbool cbyte clng cint ccur csng");
				TestNumeric(conn, decimal.MinValue,  DataType.Decimal,    "cbool cbyte clng cint ccur cdbl csng");
				TestNumeric(conn, decimal.MaxValue,  DataType.Decimal,    "cbool cbyte clng cint ccur cdbl csng");
				TestNumeric(conn, -922337203685477m, DataType.Money,      "cbool cbyte clng cint csng");
				TestNumeric(conn, +922337203685477m, DataType.Money,      "cbool cbyte clng cint csng");
				TestNumeric(conn, -214748m,          DataType.SmallMoney, "cbool cbyte cint");
				TestNumeric(conn, +214748m,          DataType.SmallMoney, "cbool cbyte cint");
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
		public void CreateDatabase([IncludeDataContexts(ProviderName.Access)] string context)
		{
			AccessTools.CreateDatabase("TestDatabase", deleteIfExists:true);
			Assert.IsTrue(File.Exists("TestDatabase.mdb"));
			AccessTools.DropDatabase  ("TestDatabase");
		}
	}
}
