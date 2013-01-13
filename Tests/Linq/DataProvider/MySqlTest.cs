using System;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using MySql.Data.Types;

namespace Tests.DataProvider
{
	[TestFixture]
	public class MySqlTest : DataProviderTestBase
	{
		const string CurrentProvider = ProviderName.MySql;

		[Test]
		public void TestParameters([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT @p",        new { p =  1  }), Is.EqualTo("1"));
				Assert.That(conn.Execute<string>("SELECT @p",        new { p = "1" }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT @p",        new { p =  new DataParameter { Value = 1   } }), Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT @p1",       new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT @p1 + ?p2", new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
				Assert.That(conn.Execute<int>   ("SELECT @p2 + ?p1", new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
			}
		}

		[Test]
		public void TestDataTypes([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(TestType<long?>         (conn, "bigintDataType",    DataType.Int64),               Is.EqualTo(1000000));
				Assert.That(TestType<short?>        (conn, "smallintDataType",  DataType.Int16),               Is.EqualTo(25555));
				Assert.That(TestType<sbyte?>        (conn, "tinyintDataType",   DataType.SByte),               Is.EqualTo(111));
				Assert.That(TestType<int?>          (conn, "mediumintDataType", DataType.Int32),               Is.EqualTo(5555));
				Assert.That(TestType<int?>          (conn, "intDataType",       DataType.Int32),               Is.EqualTo(7777777));
				Assert.That(TestType<decimal?>      (conn, "numericDataType",   DataType.Decimal),             Is.EqualTo(9999999m));
				Assert.That(TestType<decimal?>      (conn, "decimalDataType",   DataType.Decimal),             Is.EqualTo(8888888m));
				            TestType<MySqlDecimal?> (conn, "decimalDataType",   DataType.Decimal);
				Assert.That(TestType<double?>       (conn, "doubleDataType",    DataType.Double),              Is.EqualTo(20.31d));
				Assert.That(TestType<float?>        (conn, "floatDataType",     DataType.Single),              Is.EqualTo(16.0f));

				Assert.That(TestType<DateTime?>     (conn, "dateDataType",      DataType.Date),                Is.EqualTo(new DateTime(2012, 12, 12)));
				Assert.That(TestType<DateTime?>     (conn, "datetimeDataType",  DataType.DateTime),            Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(TestType<DateTime?>     (conn, "datetimeDataType",  DataType.DateTime2),           Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(TestType<MySqlDateTime?>(conn, "datetimeDataType",  DataType.DateTime),            Is.EqualTo(new MySqlDateTime(2012, 12, 12, 12, 12, 12, 0)));
				Assert.That(TestType<DateTime?>     (conn, "timestampDataType", DataType.Timestamp),           Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(TestType<TimeSpan?>     (conn, "timeDataType",      DataType.Time),                Is.EqualTo(new TimeSpan(12, 12, 12)));
				Assert.That(TestType<int?>          (conn, "yearDataType",      DataType.Int32),               Is.EqualTo(1998));
				Assert.That(TestType<int?>          (conn, "year2DataType",     DataType.Int32),               Is.EqualTo(97));
				Assert.That(TestType<int?>          (conn, "year4DataType",     DataType.Int32),               Is.EqualTo(2012));

				Assert.That(TestType<char?>         (conn, "charDataType",      DataType.Char),                Is.EqualTo('1'));
				Assert.That(TestType<string>        (conn, "charDataType",      DataType.Char),                Is.EqualTo("1"));
				Assert.That(TestType<string>        (conn, "charDataType",      DataType.NChar),               Is.EqualTo("1"));
				Assert.That(TestType<string>        (conn, "varcharDataType",   DataType.VarChar),             Is.EqualTo("234"));
				Assert.That(TestType<string>        (conn, "varcharDataType",   DataType.NVarChar),            Is.EqualTo("234"));
				Assert.That(TestType<string>        (conn, "textDataType",      DataType.Text),                Is.EqualTo("567"));

				Assert.That(TestType<byte[]>        (conn, "binaryDataType",    DataType.Binary),              Is.EqualTo(new byte[] {  97,  98,  99 }));
				Assert.That(TestType<byte[]>        (conn, "binaryDataType",    DataType.VarBinary),           Is.EqualTo(new byte[] {  97,  98,  99 }));
				Assert.That(TestType<byte[]>        (conn, "varbinaryDataType", DataType.Binary),              Is.EqualTo(new byte[] {  99, 100, 101 }));
				Assert.That(TestType<byte[]>        (conn, "varbinaryDataType", DataType.VarBinary),           Is.EqualTo(new byte[] {  99, 100, 101 }));
				Assert.That(TestType<Binary>        (conn, "varbinaryDataType", DataType.VarBinary).ToArray(), Is.EqualTo(new byte[] {  99, 100, 101 }));
				Assert.That(TestType<byte[]>        (conn, "blobDataType",      DataType.Binary),              Is.EqualTo(new byte[] { 100, 101, 102 }));
				Assert.That(TestType<byte[]>        (conn, "blobDataType",      DataType.VarBinary),           Is.EqualTo(new byte[] { 100, 101, 102 }));

				Assert.That(TestType<ulong?>        (conn, "bitDataType"),                                     Is.EqualTo(5));
				Assert.That(TestType<string>        (conn, "enumDataType"),                                    Is.EqualTo("Green"));
				Assert.That(TestType<string>        (conn, "setDataType"),                                     Is.EqualTo("one"));
			}
		}

		static void TestNumeric<T>(DataConnection conn, T expectedValue, DataType dataType, string skip = "")
		{
			var skipTypes = skip.Split(' ');

			foreach (var sqlType in new[]
				{
					"bigint",
					"smallint",
					"tinyint",
					"mediumint",
					"int",
					"money",
					"decimal",
					"decimal(38)",

					"float",
					"real"
				}.Except(skipTypes))
			{
				var sqlValue = (object)expectedValue;

				var sql = string.Format("SELECT Cast({0} as {1})", sqlValue ?? "NULL", sqlType);

				Debug.WriteLine(sql + " -> " + typeof(T));

				Assert.That(conn.Execute<T>(sql), Is.EqualTo(expectedValue));
			}

			Debug.WriteLine("{0} -> DataType.{1}",  typeof(T), dataType);
			Assert.That(conn.Execute<T>("SELECT :p", new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> auto", typeof(T));
			Assert.That(conn.Execute<T>("SELECT :p", new DataParameter { Name = "p", Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> new",  typeof(T));
			Assert.That(conn.Execute<T>("SELECT :p", new { p = expectedValue }), Is.EqualTo(expectedValue));
		}

		static void TestSimple<T>(DataConnection conn, T expectedValue, DataType dataType)
			where T : struct
		{
			TestNumeric<T> (conn, expectedValue, dataType);
			TestNumeric<T?>(conn, expectedValue, dataType);
			TestNumeric<T?>(conn, (T?)null,      dataType);
		}

		[Test]
		public void TestNumerics([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
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

				TestNumeric(conn, sbyte.MinValue,    DataType.SByte,      "money");
				TestNumeric(conn, sbyte.MaxValue,    DataType.SByte);
				TestNumeric(conn, short.MinValue,    DataType.Int16,      "money");
				TestNumeric(conn, short.MaxValue,    DataType.Int16);
				TestNumeric(conn, int.MinValue,      DataType.Int32,      "money smallint");
				TestNumeric(conn, int.MaxValue,      DataType.Int32,      "smallint real");
				TestNumeric(conn, long.MinValue,     DataType.Int64,      "int money smallint");
				TestNumeric(conn, long.MaxValue,     DataType.Int64,      "int money smallint float real");

				TestNumeric(conn, byte.MaxValue,     DataType.Byte);
				TestNumeric(conn, ushort.MaxValue,   DataType.UInt16,     "int smallint");
				TestNumeric(conn, uint.MaxValue,     DataType.UInt32,     "int smallint real");
				TestNumeric(conn, ulong.MaxValue,    DataType.UInt64,     "bigint int money smallint float real");

				TestNumeric(conn, -3.40282306E+38f,  DataType.Single,     "bigint int money smallint numeric numeric(38)");
				TestNumeric(conn, 3.40282306E+38f,   DataType.Single,     "bigint int money numeric numeric(38) smallint");
				TestNumeric(conn, -1.79E+308d,       DataType.Double,     "bigint int money numeric numeric(38) smallint real");
				TestNumeric(conn,  1.79E+308d,       DataType.Double,     "bigint int money numeric numeric(38) smallint real");
				TestNumeric(conn, decimal.MinValue,  DataType.Decimal,    "bigint int money numeric numeric(38) smallint float real");
				TestNumeric(conn, decimal.MaxValue,  DataType.Decimal,    "bigint int money numeric numeric(38) smallint float real");
				TestNumeric(conn, decimal.MinValue,  DataType.VarNumeric, "bigint int money numeric numeric(38) smallint float real");
				TestNumeric(conn, decimal.MaxValue,  DataType.VarNumeric, "bigint int money numeric numeric(38) smallint float real");
				TestNumeric(conn, -922337203685477m, DataType.Money,      "int money smallint real");
				TestNumeric(conn, +922337203685477m, DataType.Money,      "int smallint real");
				TestNumeric(conn, -214748m,          DataType.SmallMoney, "money smallint smallint");
				TestNumeric(conn, +214748m,          DataType.SmallMoney, "smallint");
			}
		}

		[Test]
		public void TestDate([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12' as date)"),                          Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12' as date)"),                          Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime> ("SELECT :p", DataParameter.Date("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT :p", new DataParameter("p", dateTime, DataType.Date)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestDateTime([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12 12:12:12' as timestamp)"),                     Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12' as timestamp)"),                     Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime> ("SELECT :p", DataParameter.DateTime("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT :p", new DataParameter("p", dateTime)),                    Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT :p", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestChar([IncludeDataContexts(CurrentProvider)] string context)
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

				Assert.That(conn.Execute<char> ("SELECT :p",                  DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p",                  DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(:p as char)",    DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(:p as char)",    DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(:p as char(1))", DataParameter.Char("@p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(:p as char(1))", DataParameter.Char("@p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT :p", DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p", DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT :p", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT :p", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT :p", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT :p", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
			}
		}

		[Test]
		public void TestString([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20))"),      Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as char(20))"),      Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(20))"),   Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as varchar(20))"),   Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as text)"),          Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as text)"),          Is.Null);

				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.Char    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.VarChar ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.Text    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.NChar   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.NText   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.Create  ("p", "123")), Is.EqualTo("123"));

				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.Create("p", (string)null)), Is.EqualTo(null));
				Assert.That(conn.Execute<string>("SELECT :p", new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestBinary([IncludeDataContexts(CurrentProvider)] string context)
		{
			var arr1 = new byte[] { 48, 57 };

			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<byte[]>("SELECT E'\\060\\071'::bytea"), Is.EqualTo(arr1));

				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Binary   ("p", arr1)),             Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", arr1)),             Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create   ("p", arr1)),             Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", null)),             Is.EqualTo(null));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", new byte[0])),      Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Image    ("p", new byte[0])),      Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create   ("p", new Binary(arr1))), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter("p", new Binary(arr1))),       Is.EqualTo(arr1));
			}
		}

		[Test]
		public void TestGuid([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(
					conn.Execute<Guid>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uuid)"),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				Assert.That(
					conn.Execute<Guid?>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uuid)"),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				var guid = Guid.NewGuid();

				Assert.That(conn.Execute<Guid>("SELECT :p", DataParameter.Create("p", guid)),                Is.EqualTo(guid));
				Assert.That(conn.Execute<Guid>("SELECT :p", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test]
		public void TestXml([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>     ("SELECT XMLPARSE (DOCUMENT'<xml/>')"),            Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>  ("SELECT XMLPARSE (DOCUMENT'<xml/>')").ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT XMLPARSE (DOCUMENT'<xml/>')").InnerXml,   Is.EqualTo("<xml />"));

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
		public void TestEnum1([IncludeDataContexts(CurrentProvider)] string context)
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
		public void TestEnum2([IncludeDataContexts(CurrentProvider)] string context)
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
	}
}
