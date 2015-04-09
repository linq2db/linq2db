using System;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Mapping;

using NUnit.Framework;

#if MANAGED_ORACLE
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
#else
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;
#endif

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

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestParameters(string context)
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

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestDataTypes(string context)
		{
			using (var conn = new DataConnection(context))
			{
				TestType(conn, "bigintDataType",         1000000L);
				TestType(conn, "numericDataType",        9999999m);
				TestType(conn, "bitDataType",            true);
				TestType(conn, "smallintDataType",       (short)25555);
				TestType(conn, "decimalDataType",        2222222m);
				TestType(conn, "smallmoneyDataType",     100000m);
				TestType(conn, "intDataType",            7777777);
				TestType(conn, "tinyintDataType",        (sbyte)100);
				TestType(conn, "moneyDataType",          100000m);
				TestType(conn, "floatDataType",          20.31d);
				TestType(conn, "realDataType",           16.2f);

				TestType(conn, "datetimeDataType",       new DateTime(2012, 12, 12, 12, 12, 12));
				TestType(conn, "datetime2DataType",      new DateTime(2012, 12, 12, 12, 12, 12, 012));
				TestType(conn, "datetimeoffsetDataType", new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, new TimeSpan(-5, 0, 0)));
				TestType(conn, "localZoneDataType",      new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, new TimeSpan(-4, 0, 0)));

				TestType(conn, "charDataType",           '1');
				TestType(conn, "varcharDataType",        "234");
				TestType(conn, "textDataType",           "567");
				TestType(conn, "ncharDataType",          "23233");
				TestType(conn, "nvarcharDataType",       "3323");
				TestType(conn, "ntextDataType",          "111");

				TestType(conn, "binaryDataType",         new byte[] { 0, 170 });
				TestType(conn, "bfileDataType",          new byte[] { 49, 50, 51, 52, 53 });

				if (OracleTools.IsXmlTypeSupported)
					TestType(conn, "xmlDataType",        "<root>\n  <element strattr=\"strvalue\" intattr=\"12345\"/>\n</root>\n");
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

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestNumerics(string context)
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

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestDate(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12);

				Assert.That(conn.Execute<DateTime> (PathThroughSql, DataParameter.Date("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>(PathThroughSql, new DataParameter("p", dateTime, DataType.Date)), Is.EqualTo(dateTime));
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestSmallDateTime(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 00);

				Assert.That(conn.Execute<DateTime> (PathThroughSql, DataParameter.SmallDateTime("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>(PathThroughSql, new DataParameter("p", dateTime, DataType.SmallDateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestDateTime(string context)
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

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestDateTime2(string context)
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

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestDateTimeOffset(string context)
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

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestChar(string context)
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

				Assert.That(conn.Execute<char> (PathThroughSql, DataParameter.Char    ("p", '1')),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, DataParameter.Char    ("p", '1')),  Is.EqualTo('1'));

				Assert.That(conn.Execute<char> (PathThroughSql, DataParameter.VarChar ("p", '1')),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, DataParameter.VarChar ("p", '1')),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char> (PathThroughSql, DataParameter.NChar   ("p", '1')),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, DataParameter.NChar   ("p", '1')),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char> (PathThroughSql, DataParameter.NVarChar("p", '1')),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, DataParameter.NVarChar("p", '1')),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char> (PathThroughSql, DataParameter.Create  ("p", '1')),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, DataParameter.Create  ("p", '1')),  Is.EqualTo('1'));

				Assert.That(conn.Execute<char> (PathThroughSql, new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>(PathThroughSql, new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestString(string context)
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

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestBinary(string context)
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

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestOracleTypes(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var arr = new byte[] { 0x30, 0x39 };

				Assert.That(conn.Execute<OracleBinary>   ("SELECT to_blob('3039')           FROM sys.dual").     Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<OracleBlob>     ("SELECT to_blob('3039')           FROM sys.dual").     Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<OracleDecimal>  ("SELECT Cast(1        as decimal) FROM sys.dual").     Value, Is.EqualTo(1));
				Assert.That(conn.Execute<OracleString>   ("SELECT Cast('12345' as char(6))  FROM sys.dual").     Value, Is.EqualTo("12345 "));
				Assert.That(conn.Execute<OracleClob>     ("SELECT ntextDataType     FROM AllTypes WHERE ID = 2").Value, Is.EqualTo("111"));
				Assert.That(conn.Execute<OracleDate>     ("SELECT datetimeDataType  FROM AllTypes WHERE ID = 2").Value, Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(conn.Execute<OracleTimeStamp>("SELECT datetime2DataType FROM AllTypes WHERE ID = 2").Value, Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));

#if !MANAGED_ORACLE
				Assert.That(conn.Execute<OracleXmlType>  ("SELECT XMLTYPE('<xml/>')         FROM sys.dual").Value, Is.EqualTo("<xml/>\n"));

				var xmlType = new OracleXmlType((OracleConnection)conn.Connection, "<xml/>");

				Assert.That(conn.Execute<OracleXmlType>(PathThroughSql, new DataParameter("p", xmlType)).              Value, Is.EqualTo("<xml/>\n"));
				Assert.That(conn.Execute<OracleXmlType>(PathThroughSql, new DataParameter("p", xmlType, DataType.Xml)).Value, Is.EqualTo("<xml/>\n"));
#endif
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestGuid(string context)
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

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestXml(string context)
		{
			if (OracleTools.IsXmlTypeSupported)
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
		}

		enum TestEnum
		{
			[MapValue("A")] AA,
			[MapValue("B")] BB,
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestEnum1(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<TestEnum> ("SELECT 'A' FROM sys.dual"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'A' FROM sys.dual"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum> ("SELECT 'B' FROM sys.dual"), Is.EqualTo(TestEnum.BB));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'B' FROM sys.dual"), Is.EqualTo(TestEnum.BB));
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestEnum2(string context)
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

		#region DateTime Tests

		[Table(Schema="TESTUSER", Name="ALLTYPES")]
		public partial class ALLTYPE
		{
			[Column(DataType=DataType.Decimal,        Length=22, Scale=0),               PrimaryKey,  NotNull] public decimal         ID                     { get; set; } // NUMBER
			[Column(DataType=DataType.Decimal,        Length=22, Precision=20, Scale=0),    Nullable         ] public decimal?        BIGINTDATATYPE         { get; set; } // NUMBER (20,0)
			[Column(DataType=DataType.Decimal,        Length=22, Scale=0),                  Nullable         ] public decimal?        NUMERICDATATYPE        { get; set; } // NUMBER
			[Column(DataType=DataType.Decimal,        Length=22, Precision=1, Scale=0),     Nullable         ] public sbyte?          BITDATATYPE            { get; set; } // NUMBER (1,0)
			[Column(DataType=DataType.Decimal,        Length=22, Precision=5, Scale=0),     Nullable         ] public int?            SMALLINTDATATYPE       { get; set; } // NUMBER (5,0)
			[Column(DataType=DataType.Decimal,        Length=22, Scale=6),                  Nullable         ] public decimal?        DECIMALDATATYPE        { get; set; } // NUMBER
			[Column(DataType=DataType.Decimal,        Length=22, Precision=10, Scale=4),    Nullable         ] public decimal?        SMALLMONEYDATATYPE     { get; set; } // NUMBER (10,4)
			[Column(DataType=DataType.Decimal,        Length=22, Precision=10, Scale=0),    Nullable         ] public long?           INTDATATYPE            { get; set; } // NUMBER (10,0)
			[Column(DataType=DataType.Decimal,        Length=22, Precision=3, Scale=0),     Nullable         ] public short?          TINYINTDATATYPE        { get; set; } // NUMBER (3,0)
			[Column(DataType=DataType.Decimal,        Length=22),                           Nullable         ] public decimal?        MONEYDATATYPE          { get; set; } // NUMBER
			[Column(DataType=DataType.Double,         Length=8),                            Nullable         ] public double?         FLOATDATATYPE          { get; set; } // BINARY_DOUBLE
			[Column(DataType=DataType.Single,         Length=4),                            Nullable         ] public float?          REALDATATYPE           { get; set; } // BINARY_FLOAT
			[Column(/*DataType=DataType.DateTime,       Length=7*/),                            Nullable         ] public DateTime?       DATETIMEDATATYPE       { get; set; } // DATE
			[Column(DataType=DataType.DateTime2,      Length=11, Scale=6),                  Nullable         ] public DateTime?       DATETIME2DATATYPE      { get; set; } // TIMESTAMP(6)
			[Column(DataType=DataType.DateTimeOffset, Length=13, Scale=6),                  Nullable         ] public DateTimeOffset? DATETIMEOFFSETDATATYPE { get; set; } // TIMESTAMP(6) WITH TIME ZONE
			[Column(DataType=DataType.DateTimeOffset, Length=11, Scale=6),                  Nullable         ] public DateTimeOffset? LOCALZONEDATATYPE      { get; set; } // TIMESTAMP(6) WITH LOCAL TIME ZONE
			[Column(DataType=DataType.Char,           Length=1),                            Nullable         ] public char?           CHARDATATYPE           { get; set; } // CHAR(1)
			[Column(DataType=DataType.VarChar,        Length=20),                           Nullable         ] public string          VARCHARDATATYPE        { get; set; } // VARCHAR2(20)
			[Column(DataType=DataType.Text,           Length=4000),                         Nullable         ] public string          TEXTDATATYPE           { get; set; } // CLOB
			[Column(DataType=DataType.NChar,          Length=40),                           Nullable         ] public string          NCHARDATATYPE          { get; set; } // NCHAR(40)
			[Column(DataType=DataType.NVarChar,       Length=40),                           Nullable         ] public string          NVARCHARDATATYPE       { get; set; } // NVARCHAR2(40)
			[Column(DataType=DataType.NText,          Length=4000),                         Nullable         ] public string          NTEXTDATATYPE          { get; set; } // NCLOB
			[Column(DataType=DataType.Blob,           Length=4000),                         Nullable         ] public byte[]          BINARYDATATYPE         { get; set; } // BLOB
			[Column(DataType=DataType.VarBinary,      Length=530),                          Nullable         ] public byte[]          BFILEDATATYPE          { get; set; } // BFILE
			[Column(DataType=DataType.Binary,         Length=16),                           Nullable         ] public byte[]          GUIDDATATYPE           { get; set; } // RAW(16)
			[Column(DataType=DataType.Undefined,      Length=256),                          Nullable         ] public object          URIDATATYPE            { get; set; } // URITYPE
			[Column(DataType=DataType.Xml,            Length=2000),                         Nullable         ] public string          XMLDATATYPE            { get; set; } // XMLTYPE
		}

		[Table("t_entity")]
		public sealed class Entity
		{
			[PrimaryKey, Identity]
			[NotNull, Column("entity_id")] public long Id           { get; set; }
			[NotNull, Column("time")]      public DateTime Time     { get; set; }
			[NotNull, Column("duration")]  public TimeSpan Duration { get; set; }
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestTimeSpan(string context)
		{
			using (var db = new DataConnection(context))
			{
				db.BeginTransaction();

				long id = 1;

				db.GetTable<Entity>().Insert(() => new Entity { Id = id + 1, Duration = TimeSpan.FromHours(1) });
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void DateTimeTest1(string context)
		{
			using (var db = new DataConnection(context))
			{
				db.GetTable<ALLTYPE>().Delete(t => t.ID >= 1000);

				db.BeginTransaction();

				db.MultipleRowsCopy(new[]
				{
					new ALLTYPE
					{
						ID                = 1000,
						DATETIMEDATATYPE  = DateTime.Now,
						DATETIME2DATATYPE = DateTime.Now
					}
				});
			}
		}

		//[Test, IncludeDataContextSource(CurrentProvider)]
		public void SelectDateTime(string context)
		{
			using (var db = new DataConnection(context))
			{
				var ms = new MappingSchema();

				// Set custom DateTime to SQL converter.
				//
				ms.SetValueToSqlConverter(
					typeof(DateTime),
					(stringBuilder, dataType, val) =>
					{
						var value = (DateTime)val;
						Assert.That(dataType.DataType, Is.Not.EqualTo(DataType.Undefined));

						var format =
							dataType.DataType == DataType.DateTime2 ?
								"TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')" :
								"TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.fffffff}', 'YYYY-MM-DD HH24:MI:SS.FF7')";

						stringBuilder.AppendFormat(format, value);
					});

				db.AddMappingSchema(ms);

				var res = (db.GetTable<ALLTYPE>().Where(e => e.DATETIME2DATATYPE == DateTime.Now)).ToList();
				Debug.WriteLine(res.Count);
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void DateTimeTest2(string context)
		{
			// Set custom DateTime to SQL converter.
			//
			OracleTools.GetDataProvider().MappingSchema.SetValueToSqlConverter(
				typeof(DateTime),
				(stringBuilder,dataType,val) =>
				{
					var value  = (DateTime)val;
					var format =
						dataType.DataType == DataType.DateTime ?
							"TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')" :
							"TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.fffffff}', 'YYYY-MM-DD HH24:MI:SS.FF7')";

					stringBuilder.AppendFormat(format, value);
				});

			using (var db = new DataConnection(context))
			{
				db.GetTable<ALLTYPE>().Delete(t => t.ID >= 1000);

				db.BeginTransaction();

				db.MultipleRowsCopy(new[]
				{
					new ALLTYPE
					{
						ID                = 1000,
						DATETIMEDATATYPE  = DateTime.Now,
						DATETIME2DATATYPE = DateTime.Now
					}
				});
			}

			// Reset converter to default.
			//
			OracleTools.GetDataProvider().MappingSchema.SetValueToSqlConverter(
				typeof(DateTime),
				(stringBuilder,dataType,val) =>
				{
					var value  = (DateTime)val;
					var format =
						dataType.DataType == DataType.DateTime ?
							"TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')" :
							"TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.fffffff}', 'YYYY-MM-DD HH24:MI:SS.FF7')";

					if (value.Millisecond == 0)
					{
						format = value.Hour == 0 && value.Minute == 0 && value.Second == 0 ?
							"TO_DATE('{0:yyyy-MM-dd}', 'YYYY-MM-DD')" :
							"TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')";
					}

					stringBuilder.AppendFormat(format, value);
				});
		}

		#endregion

		#region Sequence

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void SequenceInsert(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<OracleSpecific.SequenceTest>().Where(_ => _.Value == "SeqValue").Delete();
				db.Insert(new OracleSpecific.SequenceTest { Value = "SeqValue" });

				var id = db.GetTable<OracleSpecific.SequenceTest>().Single(_ => _.Value == "SeqValue").ID;

				db.GetTable<OracleSpecific.SequenceTest>().Where(_ => _.ID == id).Delete();

				Assert.AreEqual(0, db.GetTable<OracleSpecific.SequenceTest>().Count(_ => _.Value == "SeqValue"));
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void SequenceInsertWithIdentity(string context)
		{
			using (var db = GetDataContext(context))
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

		#region BulkCopy

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void BulkCopyLinqTypes(string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using (var db = new DataConnection(context))
				{
					if (bulkCopyType == BulkCopyType.ProviderSpecific)
					{
						var ms = new MappingSchema();

						ms.GetFluentMappingBuilder()
							.Entity<LinqDataTypes>()
								.Property(e => e.GuidValue)
									.IsNotColumn()
							;

						db.AddMappingSchema(ms);
					}

					db.BulkCopy(
						new BulkCopyOptions { BulkCopyType = bulkCopyType },
						Enumerable.Range(0, 10).Select(n =>
							new LinqDataTypes
							{
								ID            = 4000 + n,
								MoneyValue    = 1000m + n,
								DateTimeValue = new DateTime(2001,  1,  11,  1, 11, 21, 100),
								BoolValue     = true,
								GuidValue     = Guid.NewGuid(),
								SmallIntValue = (short)n
							}
						));

					db.GetTable<LinqDataTypes>().Delete(p => p.ID >= 4000);
				}
			}
		}

		[System.Data.Linq.Mapping.Table(Name = "stg_trade_information")]
		public class Trade
		{
			[Column("STG_TRADE_ID")]          public int       ID             { get; set; }
			[Column("STG_TRADE_VERSION")]     public int       Version        { get; set; }
			[Column("INFORMATION_TYPE_ID")]   public int       TypeID         { get; set; }
			[Column("INFORMATION_TYPE_NAME")] public string    TypeName       { get; set; }
			[Column("value")]                 public string    Value          { get; set; }
			[Column("value_as_integer")]      public int?      ValueAsInteger { get; set; }
			[Column("value_as_date")]         public DateTime? ValueAsDate    { get; set; }
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void BulkCopy1(string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
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

				using (var db = new TestDataConnection(context))
				{
					db.BulkCopy(
						new BulkCopyOptions
						{
							MaxBatchSize = 5,
							BulkCopyType = bulkCopyType,
							NotifyAfter  = 3,
							RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied)
						},
						data);
				}
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void BulkCopy2(string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using (var db = new TestDataConnection(context))
				{
					db.Types2.Delete(_ => _.ID > 1000);

					if (bulkCopyType == BulkCopyType.ProviderSpecific)
					{
						var ms = new MappingSchema();

						db.AddMappingSchema(ms);

						ms.GetFluentMappingBuilder()
							.Entity<LinqDataTypes2>()
								.Property(e => e.GuidValue)
									.IsNotColumn()
							;
					}

					db.BulkCopy(
						new BulkCopyOptions { MaxBatchSize = 2, BulkCopyType = bulkCopyType },
						new[]
						{
							new LinqDataTypes2 { ID = 1003, MoneyValue = 0m, DateTimeValue = null,         BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue = null, IntValue = null    },
							new LinqDataTypes2 { ID = 1004, MoneyValue = 0m, DateTimeValue = DateTime.Now, BoolValue = false, GuidValue = null,                                             SmallIntValue = 2,    IntValue = 1532334 },
							new LinqDataTypes2 { ID = 1005, MoneyValue = 1m, DateTimeValue = DateTime.Now, BoolValue = false, GuidValue = null,                                             SmallIntValue = 5,    IntValue = null    },
							new LinqDataTypes2 { ID = 1006, MoneyValue = 2m, DateTimeValue = DateTime.Now, BoolValue = false, GuidValue = null,                                             SmallIntValue = 6,    IntValue = 153     }
						});

					db.Types2.Delete(_ => _.ID > 1000);
				}
			}
		}

		#endregion

		#region CreateTest

		[Table]
		class TempTestTable
		{
			// column name length = 30 char (maximum for Oracle)
			[Column(Name = "AAAAAAAAAAAAAAAAAAAAAAAAAAAABC")]
			public long Id { get; set; }
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void LongAliasTest(string context)
		{
			using (var db = new DataConnection(context))
			{
				try { db.DropTable<TempTestTable>(); } catch {}

				var table = db.CreateTable<TempTestTable>();

				var query =
				(
					from t in table.Distinct()
					select new { t.Id }
				).ToList();

				db.DropTable<TempTestTable>();
			}
		}

		#endregion
	}
}
