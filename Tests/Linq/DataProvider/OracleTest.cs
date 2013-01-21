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

using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;

namespace Tests.DataProvider
{
	using Model;

	[TestFixture]
	public class OracleTest : TestBase
	{
		const string CurrentProvider = ProviderName.Oracle;

		string _pathThroughSql = "SELECT :p FROM sys.dual";
		string  PathThroughSql
		{
			get
			{
				_pathThroughSql += " ";
				return _pathThroughSql;
			}
		}

		[Test]
		public void TestParameters([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.VarBinary("p", null)), Is.EqualTo(null));
				Assert.That(conn.Execute<char>  (PathThroughSql, DataParameter.Char     ("p", '1')),  Is.EqualTo('1'));

				Assert.That(conn.Execute<string>(PathThroughSql,                   new { p =  1  }), Is.EqualTo("1"));
				Assert.That(conn.Execute<string>(PathThroughSql,                   new { p = "1" }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT :p FROM sys.dual",        new { p =  new DataParameter { Value = 1   } }), Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT :p1 FROM sys.dual",       new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT :p1 + :p2 FROM sys.dual", new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
				Assert.That(conn.Execute<int>   ("SELECT :p2 + :p1 FROM sys.dual", new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
			}
		}

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
		public void TestDataTypes([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				TestType(conn, "bigintDataType",           1000000L);
				TestType(conn, "numericDataType",          9999999m);
				TestType(conn, "bitDataType",              true);
				TestType(conn, "smallintDataType",         (short)25555);
				TestType(conn, "decimalDataType",          2222222m);
				TestType(conn, "smallmoneyDataType",       100000m);
				TestType(conn, "intDataType",              7777777);
				TestType(conn, "tinyintDataType",          (sbyte)100);
				TestType(conn, "moneyDataType",            100000m);
				TestType(conn, "floatDataType",            20.31d);
				TestType(conn, "realDataType",             16.2f);

				TestType(conn, "datetimeDataType",         new DateTime(2012, 12, 12, 12, 12, 12));
				TestType(conn, "datetime2DataType",        new DateTime(2012, 12, 12, 12, 12, 12, 012));
				TestType(conn, "datetimeoffsetDataType",   new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, new TimeSpan(-5, 0, 0)));
				TestType(conn, "localZoneDataType",        new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, new TimeSpan(-5, 0, 0)));

				TestType(conn, "charDataType",             '1');
				TestType(conn, "varcharDataType",          "234");
				TestType(conn, "textDataType",             "567");
				TestType(conn, "ncharDataType",            "23233");
				TestType(conn, "nvarcharDataType",         "3323");
				TestType(conn, "ntextDataType",            "111");

				TestType(conn, "binaryDataType",           new byte[] { 0, 170 });
				TestType(conn, "bfileDataType",            new byte[] { 49, 50, 51, 52, 53 });

//				TestType(conn, "uriDataType",              "");
				TestType(conn, "xmlDataType",              "<root>\n  <element strattr=\"strvalue\" intattr=\"12345\"/>\n</root>\n");
			}
		}

		void TestNumeric<T>(DataConnection conn, T expectedValue, DataType dataType, string skip = "")
		{
			var skipTypes = skip.Split(' ');

			foreach (var sqlType in new[]
				{
					"number",
					"number(10,0)",
					"number(20,0)",
					"binary_float",
					"binary_double"
				}.Except(skipTypes))
			{
				var sqlValue = expectedValue is bool ? (bool)(object)expectedValue? 1 : 0 : (object)expectedValue;

				var sql = string.Format("SELECT Cast({0} as {1}) FROM sys.dual", sqlValue ?? "NULL", sqlType);

				Debug.WriteLine(sql + " -> " + typeof(T));

				Assert.That(conn.Execute<T>(sql), Is.EqualTo(expectedValue));
			}

			Debug.WriteLine("{0} -> DataType.{1}",  typeof(T), dataType);
			Assert.That(conn.Execute<T>(PathThroughSql, new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> auto", typeof(T));
			Assert.That(conn.Execute<T>(PathThroughSql, new DataParameter { Name = "p", Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> new",  typeof(T));
			Assert.That(conn.Execute<T>(PathThroughSql, new { p = expectedValue }), Is.EqualTo(expectedValue));
		}

		void TestSimple<T>(DataConnection conn, T expectedValue, DataType dataType)
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

				TestNumeric(conn, sbyte.MinValue,    DataType.SByte);
				TestNumeric(conn, sbyte.MaxValue,    DataType.SByte);
				TestNumeric(conn, short.MinValue,    DataType.Int16);
				TestNumeric(conn, short.MaxValue,    DataType.Int16);
				TestNumeric(conn, int.MinValue,      DataType.Int32);
				TestNumeric(conn, int.MaxValue,      DataType.Int32,      "binary_float");
				TestNumeric(conn, long.MinValue,     DataType.Int64,      "number(10,0)");
				TestNumeric(conn, long.MaxValue,     DataType.Int64,      "number(10,0) binary_float binary_double");

				TestNumeric(conn, byte.MaxValue,     DataType.Byte,       "");
				TestNumeric(conn, ushort.MaxValue,   DataType.UInt16,     "");
				TestNumeric(conn, uint.MaxValue,     DataType.UInt32,     "binary_float");
				TestNumeric(conn, ulong.MaxValue,    DataType.UInt64,     "number(10,0) binary_float binary_double");

				TestNumeric(conn, -3.4E+28f,         DataType.Single,     "number number(10,0) number(20,0)");
				TestNumeric(conn, 3.4E+28f,          DataType.Single,     "number number(10,0) number(20,0)");
				TestNumeric(conn, decimal.MinValue,  DataType.Decimal,    "number(10,0) number(20,0) binary_float binary_double");
				TestNumeric(conn, decimal.MaxValue,  DataType.Decimal,    "number(10,0) number(20,0) binary_float binary_double");
				TestNumeric(conn, decimal.MinValue,  DataType.VarNumeric, "number(10,0) number(20,0) binary_float binary_double");
				TestNumeric(conn, decimal.MaxValue,  DataType.VarNumeric, "number(10,0) number(20,0) binary_float binary_double");
				TestNumeric(conn, -922337203685477m, DataType.Money,      "number(10,0) binary_float");
				TestNumeric(conn, +922337203685477m, DataType.Money,      "number(10,0) binary_float");
				TestNumeric(conn, -214748m,          DataType.SmallMoney);
				TestNumeric(conn, +214748m,          DataType.SmallMoney);
			}
		}

		[Test]
		public void TestDate([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12);

				Assert.That(conn.Execute<DateTime> (PathThroughSql, DataParameter.Date("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>(PathThroughSql, new DataParameter("p", dateTime, DataType.Date)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestSmallDateTime([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 00);

				Assert.That(conn.Execute<DateTime> (PathThroughSql, DataParameter.SmallDateTime("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>(PathThroughSql, new DataParameter("p", dateTime, DataType.SmallDateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestDateTime([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT to_date('2012-12-12 12:12:12', 'YYYY-MM-DD HH:MI:SS') FROM sys.dual"), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT to_date('2012-12-12 12:12:12', 'YYYY-MM-DD HH:MI:SS') FROM sys.dual"), Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime> (PathThroughSql, DataParameter.DateTime("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>(PathThroughSql, new DataParameter("p", dateTime)),                    Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>(PathThroughSql, new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestDateTime2([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime1 = new DateTime(2012, 12, 12, 12, 12, 12);
				var dateTime2 = new DateTime(2012, 12, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime?>("SELECT timestamp '2012-12-12 12:12:12.012' FROM sys.dual"), Is.EqualTo(dateTime2));

				Assert.That(conn.Execute<DateTime> (PathThroughSql, DataParameter.DateTime2("p", dateTime2)),               Is.EqualTo(dateTime2));
				Assert.That(conn.Execute<DateTime> (PathThroughSql, DataParameter.Create   ("p", dateTime2)),               Is.EqualTo(dateTime2));
				Assert.That(conn.Execute<DateTime?>(PathThroughSql, new DataParameter("p", dateTime2, DataType.DateTime2)), Is.EqualTo(dateTime2));
			}
		}

		[Test]
		public void TestDateTimeOffset([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dto = new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, new TimeSpan(5, 0, 0));

				Assert.That(conn.Execute<DateTimeOffset>(
					"SELECT timestamp '2012-12-12 12:12:12.012' FROM sys.dual"),
					Is.EqualTo(new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2012, 12, 12, 12, 12, 12)))));

				Assert.That(conn.Execute<DateTimeOffset?>(
					"SELECT timestamp '2012-12-12 12:12:12.012' FROM sys.dual"),
					Is.EqualTo(new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2012, 12, 12, 12, 12, 12)))));

				Assert.That(conn.Execute<DateTime>(
					"SELECT timestamp '2012-12-12 12:12:12.012 -04:00' FROM sys.dual"),
					Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));

				Assert.That(conn.Execute<DateTime?>(
					"SELECT timestamp '2012-12-12 12:12:12.012 -04:00' FROM sys.dual"),
					Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));

				Assert.That(conn.Execute<DateTimeOffset>(
					"SELECT timestamp '2012-12-12 12:12:12.012 +05:00' FROM sys.dual"),
					Is.EqualTo(dto));

				Assert.That(conn.Execute<DateTimeOffset?>(
					"SELECT timestamp '2012-12-12 12:12:12.012 +05:00' FROM sys.dual"),
					Is.EqualTo(dto));

				Assert.That(conn.Execute<DateTime> ("SELECT datetimeoffsetDataType FROM AllTypes WHERE ID = 1"), Is.EqualTo(default(DateTime)));
				Assert.That(conn.Execute<DateTime?>("SELECT datetimeoffsetDataType FROM AllTypes WHERE ID = 1"), Is.EqualTo(default(DateTime?)));

				Assert.That(conn.Execute<DateTimeOffset?>(PathThroughSql, new DataParameter("p", dto)).                         ToString(), Is.EqualTo(dto.ToString()));
				Assert.That(conn.Execute<DateTimeOffset?>(PathThroughSql, new DataParameter("p", dto, DataType.DateTimeOffset)).ToString(), Is.EqualTo(dto.ToString()));
			}
		}

		[Test]
		public void TestChar([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char)    FROM sys.dual"),       Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char)    FROM sys.dual"),       Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char(1)) FROM sys.dual"),       Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char(1)) FROM sys.dual"),       Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast('1' as varchar2(20)) FROM sys.dual"),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar2(20)) FROM sys.dual"),  Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast('1' as nchar)     FROM sys.dual"),     Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nchar)     FROM sys.dual"),     Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as nchar(20)) FROM sys.dual"),     Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nchar(20)) FROM sys.dual"),     Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast('1' as nvarchar2(20)) FROM sys.dual"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nvarchar2(20)) FROM sys.dual"), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> (PathThroughSql, DataParameter.Char    ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, DataParameter.Char    ("p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> (PathThroughSql, DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> (PathThroughSql, DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> (PathThroughSql, DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> (PathThroughSql, DataParameter.Create  ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, DataParameter.Create  ("p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> (PathThroughSql, new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
			}
		}

		[Test]
		public void TestString([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20)) FROM sys.dual"),     Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as char(20)) FROM sys.dual"),     Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar2(20)) FROM sys.dual"),  Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as varchar2(20)) FROM sys.dual"),  Is.Null);

				Assert.That(conn.Execute<string>("SELECT textDataType FROM AllTypes WHERE ID = 2"),      Is.EqualTo("567"));
				Assert.That(conn.Execute<string>("SELECT textDataType FROM AllTypes WHERE ID = 1"),      Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nchar(20)) FROM sys.dual"),     Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as nchar(20)) FROM sys.dual"),     Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nvarchar2(20)) FROM sys.dual"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as nvarchar2(20)) FROM sys.dual"), Is.Null);

				Assert.That(conn.Execute<string>("SELECT ntextDataType FROM AllTypes WHERE ID = 2"),     Is.EqualTo("111"));
				Assert.That(conn.Execute<string>("SELECT ntextDataType FROM AllTypes WHERE ID = 1"),     Is.Null);

				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.Char    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.VarChar ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.Text    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.NChar   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.NText   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.Create  ("p", "123")), Is.EqualTo("123"));

				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.Create("p", (string)null)), Is.EqualTo(null));
				Assert.That(conn.Execute<string>(PathThroughSql, new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestBinary([IncludeDataContexts(CurrentProvider)] string context)
		{
			var arr1 = new byte[] {       0x30, 0x39 };
			var arr2 = new byte[] { 0, 0, 0x30, 0x39 };

			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<byte[]>("SELECT to_blob('3039')     FROM sys.dual"), Is.EqualTo(           arr1));
				Assert.That(conn.Execute<Binary>("SELECT to_blob('00003039') FROM sys.dual"), Is.EqualTo(new Binary(arr2)));

				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.VarBinary("p", null)), Is.EqualTo(null));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.Binary   ("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.VarBinary("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.Create   ("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.VarBinary("p", new byte[0])), Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.Image    ("p", new byte[0])), Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.Create   ("p", new Binary(arr1))), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, new DataParameter("p", new Binary(arr1))), Is.EqualTo(arr1));
			}
		}

		[Test]
		public void TestOracleTypes([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var arr = new byte[] { 0x30, 0x39 };

				Assert.That(conn.Execute<OracleBinary>   ("SELECT to_blob('3039')           FROM sys.dual").Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<OracleBlob>     ("SELECT to_blob('3039')           FROM sys.dual").Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<OracleDecimal>  ("SELECT Cast(1        as decimal) FROM sys.dual").Value, Is.EqualTo(1));
				Assert.That(conn.Execute<OracleString>   ("SELECT Cast('12345' as char(6))  FROM sys.dual").Value, Is.EqualTo("12345 "));
				Assert.That(conn.Execute<OracleClob>     ("SELECT ntextDataType     FROM AllTypes WHERE ID = 2").Value, Is.EqualTo("111"));
				Assert.That(conn.Execute<OracleDate>     ("SELECT datetimeDataType  FROM AllTypes WHERE ID = 2").Value, Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(conn.Execute<OracleTimeStamp>("SELECT datetime2DataType FROM AllTypes WHERE ID = 2").Value, Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));
				Assert.That(conn.Execute<OracleXmlType>  ("SELECT XMLTYPE('<xml/>')         FROM sys.dual").Value, Is.EqualTo("<xml/>\n"));

				var xmlType = new OracleXmlType((OracleConnection)conn.Connection, "<xml/>");

				Assert.That(conn.Execute<OracleXmlType>(PathThroughSql, new DataParameter("p", xmlType)).              Value, Is.EqualTo("<xml/>\n"));
				Assert.That(conn.Execute<OracleXmlType>(PathThroughSql, new DataParameter("p", xmlType, DataType.Xml)).Value, Is.EqualTo("<xml/>\n"));
			}
		}

		[Test]
		public void TestGuid([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var guid = conn.Execute<Guid>("SELECT guidDataType FROM AllTypes WHERE ID = 2");

				Assert.That(conn.Execute<Guid?>("SELECT guidDataType FROM AllTypes WHERE ID = 1"), Is.EqualTo(null));
				Assert.That(conn.Execute<Guid?>("SELECT guidDataType FROM AllTypes WHERE ID = 2"), Is.EqualTo(guid));

				Assert.That(conn.Execute<Guid>(PathThroughSql, DataParameter.Create("p", guid)),                Is.EqualTo(guid));
				Assert.That(conn.Execute<Guid>(PathThroughSql, new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test]
		public void TestXml([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>     ("SELECT XMLTYPE('<xml/>') FROM sys.dual"),            Is.EqualTo("<xml/>\n"));
				Assert.That(conn.Execute<XDocument>  ("SELECT XMLTYPE('<xml/>') FROM sys.dual").ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT XMLTYPE('<xml/>') FROM sys.dual").InnerXml,   Is.EqualTo("<xml />"));

				var xdoc = XDocument.Parse("<xml/>");
				var xml  = Convert<string,XmlDocument>.Lambda("<xml/>");

				Assert.That(conn.Execute<string>     (PathThroughSql, DataParameter.Xml("p", "<xml/>")),        Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>  (PathThroughSql, DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>(PathThroughSql, DataParameter.Xml("p", xml)). InnerXml,   Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  (PathThroughSql, new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  (PathThroughSql, new DataParameter("p", xml)). ToString(), Is.EqualTo("<xml />"));
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
				Assert.That(conn.Execute<TestEnum> ("SELECT 'A' FROM sys.dual"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'A' FROM sys.dual"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum> ("SELECT 'B' FROM sys.dual"), Is.EqualTo(TestEnum.BB));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'B' FROM sys.dual"), Is.EqualTo(TestEnum.BB));
			}
		}

		[Test]
		public void TestEnum2([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>(PathThroughSql, new { p = TestEnum.AA }),            Is.EqualTo("A"));
				Assert.That(conn.Execute<string>(PathThroughSql, new { p = (TestEnum?)TestEnum.BB }), Is.EqualTo("B"));

				Assert.That(conn.Execute<string>(PathThroughSql, new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>(PathThroughSql, new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>(PathThroughSql, new { p = conn.MappingSchema.GetConverter<TestEnum?,string>()(TestEnum.AA) }), Is.EqualTo("A"));
			}
		}

		#region Sequence

		[Test]
		public void SequenceInsert([IncludeDataContexts(ProviderName.Oracle)] string context)
		{
			using (var db = new TestDbManager(context))
			{
				db.GetTable<OracleSpecific.SequenceTest>().Where(_ => _.Value == "SeqValue").Delete();
				db.Insert(new OracleSpecific.SequenceTest { Value = "SeqValue" });

				var id = db.GetTable<OracleSpecific.SequenceTest>().Single(_ => _.Value == "SeqValue").ID;

				db.GetTable<OracleSpecific.SequenceTest>().Where(_ => _.ID == id).Delete();

				Assert.AreEqual(0, db.GetTable<OracleSpecific.SequenceTest>().Count(_ => _.Value == "SeqValue"));
			}
		}

		[Test]
		public void SequenceInsertWithIdentity([IncludeDataContexts(ProviderName.Oracle)] string context)
		{
			using (var db = new TestDbManager(context))
			{
				db.GetTable<OracleSpecific.SequenceTest>().Where(_ => _.Value == "SeqValue").Delete();

				var id1 = Convert.ToInt32(db.InsertWithIdentity(new OracleSpecific.SequenceTest { Value = "SeqValue" }));
				var id2 = db.GetTable<OracleSpecific.SequenceTest>().Single(_ => _.Value == "SeqValue").ID;

				Assert.AreEqual(id1, id2);

				db.GetTable<OracleSpecific.SequenceTest>().Where(_ => _.ID == id1).Delete();

				Assert.AreEqual(0, db.GetTable<OracleSpecific.SequenceTest>().Count(_ => _.Value == "SeqValue"));
			}
		}

		#endregion

		#region InsertBatch

		[System.Data.Linq.Mapping.Table(Name = "stg_trade_information")]
		public class Trade
		{
			[MapField("STG_TRADE_ID")]          public int       ID             { get; set; }
			[MapField("STG_TRADE_VERSION")]     public int       Version        { get; set; }
			[MapField("INFORMATION_TYPE_ID")]   public int       TypeID         { get; set; }
			[MapField("INFORMATION_TYPE_NAME")] public string    TypeName       { get; set; }
			[MapField("value")]                 public string    Value          { get; set; }
			[MapField("value_as_integer")]      public int?      ValueAsInteger { get; set; }
			[MapField("value_as_date")]         public DateTime? ValueAsDate    { get; set; }
		}

		[Test]
		public void InsertBatch1([IncludeDataContexts(ProviderName.Oracle)] string context)
		{
			var data = new[]
			{
				new Trade { ID = 375, Version = 1, TypeID = 20224, TypeName = "Gas Month",     },
				new Trade { ID = 328, Version = 1, TypeID = 20224, TypeName = "Gas Month",     },
				new Trade { ID = 348, Version = 1, TypeID = 20224, TypeName = "Gas Month",     },
				new Trade { ID = 357, Version = 1, TypeID = 20224, TypeName = "Gas Month",     },
				new Trade { ID = 371, Version = 1, TypeID = 20224, TypeName = "Gas Month",     },
				new Trade { ID = 333, Version = 1, TypeID = 20224, TypeName = "Gas Month",     ValueAsInteger = 1,          ValueAsDate = new DateTime(2011, 1, 5) },
				new Trade { ID = 353, Version = 1, TypeID = 20224, TypeName = "Gas Month",     ValueAsInteger = 1000000000,                                        },
				new Trade { ID = 973, Version = 1, TypeID = 20160, TypeName = "EU Allowances", },
			};

			using (var db = new TestDbManager(context))
			{
				db.InsertBatch(5, data);
			}
		}

		[Test]
		public void InsertBatch2([IncludeDataContexts(ProviderName.Oracle)] string context)
		{
			using (var db = new TestDbManager(context))
			{
				db.Types2.Delete(_ => _.ID > 1000);

				db.InsertBatch(2, new[]
				{
					new LinqDataTypes2 { ID = 1003, MoneyValue = 0m, DateTimeValue = null,         BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue = null, IntValue = null },
					new LinqDataTypes2 { ID = 1004, MoneyValue = 0m, DateTimeValue = DateTime.Now, BoolValue = false, GuidValue = null,                                             SmallIntValue = 2,    IntValue = 1532334 },
					new LinqDataTypes2 { ID = 1005, MoneyValue = 1m, DateTimeValue = DateTime.Now, BoolValue = false, GuidValue = null,                                             SmallIntValue = 5,    IntValue = null },
					new LinqDataTypes2 { ID = 1006, MoneyValue = 2m, DateTimeValue = DateTime.Now, BoolValue = false, GuidValue = null,                                             SmallIntValue = 6,    IntValue = 153     },
				});

				db.Types2.Delete(_ => _.ID > 1000);
			}
		}

		#endregion
	}
}
