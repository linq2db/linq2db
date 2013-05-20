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

using IBM.Data.DB2Types;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class DB2Test : DataProviderTestBase
	{
		const string CurrentProvider = ProviderName.DB2;

		[Test]
		public void TestParameters([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT @p FROM SYSIBM.SYSDUMMY1",        new { p =  1  }), Is.EqualTo("1"));
				Assert.That(conn.Execute<string>("SELECT @p FROM SYSIBM.SYSDUMMY1",        new { p = "1" }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT @p FROM SYSIBM.SYSDUMMY1",        new { p =  new DataParameter { Value = 1   } }), Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT @p1 FROM SYSIBM.SYSDUMMY1",       new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT @p1 + @p2 FROM SYSIBM.SYSDUMMY1", new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
				Assert.That(conn.Execute<int>   ("SELECT @p2 + @p1 FROM SYSIBM.SYSDUMMY1", new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
			}
		}

		[Test]
		public void TestDataTypes([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(TestType<long?>        (conn, "bigintDataType",    DataType.Int64),     Is.EqualTo(1000000L));
				Assert.That(TestType<DB2Int64?>    (conn, "bigintDataType",    DataType.Int64),     Is.EqualTo(new DB2Int64(1000000L)));
				Assert.That(TestType<int?>         (conn, "intDataType",       DataType.Int32),     Is.EqualTo(7777777));
				Assert.That(TestType<DB2Int32?>    (conn, "intDataType",       DataType.Int32),     Is.EqualTo(new DB2Int32(7777777)));
				Assert.That(TestType<short?>       (conn, "smallintDataType",  DataType.Int16),     Is.EqualTo(100));
				Assert.That(TestType<DB2Int16?>    (conn, "smallintDataType",  DataType.Int16),     Is.EqualTo(new DB2Int16(100)));
				Assert.That(TestType<decimal?>     (conn, "decimalDataType",   DataType.Decimal),   Is.EqualTo(9999999m));
				Assert.That(TestType<decimal?>     (conn, "decfloatDataType",  DataType.Decimal),   Is.EqualTo(8888888m));
				Assert.That(TestType<float?>       (conn, "realDataType",      DataType.Single),    Is.EqualTo(20.31f));
				Assert.That(TestType<DB2Real?>     (conn, "realDataType",      DataType.Single),    Is.EqualTo(new DB2Real(20.31f)));
				Assert.That(TestType<double?>      (conn, "doubleDataType",    DataType.Double),    Is.EqualTo(16.2d));
				Assert.That(TestType<DB2Double?>   (conn, "doubleDataType",    DataType.Double),    Is.EqualTo(new DB2Double(16.2d)));

				Assert.That(TestType<string>       (conn, "charDataType",      DataType.Char),      Is.EqualTo("1"));
				Assert.That(TestType<string>       (conn, "charDataType",      DataType.NChar),     Is.EqualTo("1"));
				Assert.That(TestType<DB2String?>   (conn, "charDataType",      DataType.Char),      Is.EqualTo(new DB2String("1")));
				Assert.That(TestType<string>       (conn, "varcharDataType",   DataType.VarChar),   Is.EqualTo("234"));
				Assert.That(TestType<string>       (conn, "varcharDataType",   DataType.NVarChar),  Is.EqualTo("234"));
				Assert.That(TestType<string>       (conn, "clobDataType",      DataType.Text),      Is.EqualTo("55645"));
				Assert.That(TestType<string>       (conn, "dbclobDataType",    DataType.NText),     Is.EqualTo("6687"));

				Assert.That(TestType<byte[]>       (conn, "binaryDataType",    DataType.Binary),    Is.EqualTo(new byte[] { 49, 50, 51, 32, 32 }));
				Assert.That(TestType<byte[]>       (conn, "varbinaryDataType", DataType.VarBinary), Is.EqualTo(new byte[] { 49, 50, 51, 52 }));
				Assert.That(TestType<byte[]>       (conn, "blobDataType",      DataType.Blob,      skipDefaultNull:true, skipUndefinedNull:true, skipDefault:true, skipUndefined:true), Is.EqualTo(new byte[] { 50, 51, 52 }));
				Assert.That(TestType<byte[]>       (conn, "blobDataType",      DataType.VarBinary, skipDefaultNull:true, skipUndefinedNull:true, skipDefault:true, skipUndefined:true), Is.EqualTo(new byte[] { 50, 51, 52 }));
				Assert.That(TestType<string>       (conn, "graphicDataType",   DataType.VarChar),   Is.EqualTo("23        "));

				Assert.That(TestType<DateTime?>    (conn, "dateDataType",      DataType.Date),      Is.EqualTo(new DateTime(2012, 12, 12)));
				Assert.That(TestType<DB2Date?>     (conn, "dateDataType",      DataType.Date),      Is.EqualTo(new DB2Date(new DateTime(2012, 12, 12))));
				Assert.That(TestType<TimeSpan?>    (conn, "timeDataType",      DataType.Time),      Is.EqualTo(new TimeSpan(12, 12, 12)));
				Assert.That(TestType<DB2Time?>     (conn, "timeDataType",      DataType.Time),      Is.EqualTo(new DB2Time(new TimeSpan(12, 12, 12))));
				Assert.That(TestType<DateTime?>    (conn, "timestampDataType", DataType.DateTime2), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));
				Assert.That(TestType<DB2TimeStamp?>(conn, "timestampDataType", DataType.DateTime2), Is.EqualTo(new DB2TimeStamp(new DateTime(2012, 12, 12, 12, 12, 12, 12))));

				Assert.That(TestType<string>       (conn, "xmlDataType",       DataType.Xml, skipPass:true), Is.EqualTo("<root><element strattr=\"strvalue\" intattr=\"12345\"/></root>"));

				Assert.That(conn.Execute<byte[]>("SELECT rowid FROM AllTypes WHERE ID = 2").Length, Is.Not.EqualTo(0));
				//Assert.That(conn.Execute<DB2RowId>("SELECT rowid FROM AllTypes WHERE ID = 2").Value.Length, Is.Not.EqualTo(0));

				            TestType<DB2Clob>    (conn, "clobDataType",      DataType.Text,      skipNotNull:true);
				            TestType<DB2Blob>    (conn, "blobDataType",      DataType.VarBinary, skipNotNull:true);
				            TestType<DB2Xml>     (conn, "xmlDataType",       DataType.Xml, skipPass:true);

				Assert.That(TestType<DB2Decimal?>     (conn, "decimalDataType",   DataType.Decimal).  ToString(),   Is.EqualTo(new DB2Decimal(9999999m).ToString()));
				Assert.That(TestType<DB2Binary>       (conn, "varbinaryDataType", DataType.VarBinary).ToString(), Is.EqualTo(new DB2Binary(new byte[] { 49, 50, 51, 52 }).ToString()));
				Assert.That(TestType<DB2DecimalFloat?>(conn, "decfloatDataType",  DataType.Decimal).  ToString(), Is.EqualTo(new DB2DecimalFloat(8888888m).ToString()));
			}
		}

		static void TestNumeric<T>(DataConnection conn, T expectedValue, DataType dataType, string skip = "")
		{
			var skipTypes = skip.Split(' ');

			foreach (var sqlType in new[]
				{
					"bigint",
					"int",
					"smallint",
					"decimal(31)",
					"decfloat",
					"double",
					"real"
				}.Except(skipTypes))
			{
				var sqlValue = expectedValue is bool ? (bool)(object)expectedValue? 1 : 0 : (object)expectedValue;

				var sql = string.Format("SELECT Cast({0} as {1}) FROM SYSIBM.SYSDUMMY1", sqlValue ?? "NULL", sqlType);

				Debug.WriteLine(sql + " -> " + typeof(T));

				Assert.That(conn.Execute<T>(sql), Is.EqualTo(expectedValue));
			}

			Debug.WriteLine("{0} -> DataType.{1}",  typeof(T), dataType);
			Assert.That(conn.Execute<T>("SELECT @p FROM SYSIBM.SYSDUMMY1", new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> auto", typeof(T));
			Assert.That(conn.Execute<T>("SELECT @p FROM SYSIBM.SYSDUMMY1", new DataParameter { Name = "p", Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> new",  typeof(T));
			Assert.That(conn.Execute<T>("SELECT @p FROM SYSIBM.SYSDUMMY1", new { p = expectedValue }), Is.EqualTo(expectedValue));
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
				TestSimple<sbyte>  (conn, 1,   DataType.SByte);
				TestSimple<short>  (conn, 1,   DataType.Int16);
				TestSimple<int>    (conn, 1,   DataType.Int32);
				TestSimple<long>   (conn, 1L,  DataType.Int64);
				TestSimple<byte>   (conn, 1,   DataType.Byte);
				TestSimple<ushort> (conn, 1,   DataType.UInt16);
				TestSimple<uint>   (conn, 1u,  DataType.UInt32);
				TestSimple<ulong>  (conn, 1ul, DataType.UInt64);
				TestSimple<float>  (conn, 1,   DataType.Single);
				TestSimple<double> (conn, 1d,  DataType.Double);
				TestSimple<decimal>(conn, 1m,  DataType.Decimal);
				TestSimple<decimal>(conn, 1m,  DataType.VarNumeric);
				TestSimple<decimal>(conn, 1m,  DataType.Money);
				TestSimple<decimal>(conn, 1m,  DataType.SmallMoney);

				TestNumeric(conn, sbyte.MinValue,    DataType.SByte);
				TestNumeric(conn, sbyte.MaxValue,    DataType.SByte);
				TestNumeric(conn, short.MinValue,    DataType.Int16);
				TestNumeric(conn, short.MaxValue,    DataType.Int16);
				TestNumeric(conn, int.MinValue,      DataType.Int32,      "smallint");
				TestNumeric(conn, int.MaxValue,      DataType.Int32,      "smallint real");
				TestNumeric(conn, long.MinValue,     DataType.Int64,      "smallint int double");
				TestNumeric(conn, long.MaxValue,     DataType.Int64,      "smallint int double real");

				TestNumeric(conn, byte.MaxValue,     DataType.Byte);
				TestNumeric(conn, ushort.MaxValue,   DataType.UInt16,     "smallint");
				TestNumeric(conn, uint.MaxValue,     DataType.UInt32,     "smallint int real");
				TestNumeric(conn, ulong.MaxValue,    DataType.UInt64,     "smallint int real bigint double");

				TestNumeric(conn, -3.40282306E+38f,  DataType.Single,     "bigint int smallint decimal(31) decfloat");
				TestNumeric(conn, 3.40282306E+38f,   DataType.Single,     "bigint int smallint decimal(31) decfloat");
				TestNumeric(conn, -1.79E+308d,       DataType.Double,     "bigint int smallint decimal(31) decfloat real");
				TestNumeric(conn,  1.79E+308d,       DataType.Double,     "bigint int smallint decimal(31) decfloat real");
				TestNumeric(conn, decimal.MinValue,  DataType.Decimal,    "bigint int smallint double real");
				TestNumeric(conn, decimal.MaxValue,  DataType.Decimal,    "bigint int smallint double real");
				TestNumeric(conn, decimal.MinValue,  DataType.VarNumeric, "bigint int smallint double real");
				TestNumeric(conn, decimal.MaxValue,  DataType.VarNumeric, "bigint int smallint double real");
				TestNumeric(conn, -922337203685477m, DataType.Money,      "int smallint real");
				TestNumeric(conn, +922337203685477m, DataType.Money,      "int smallint real");
				TestNumeric(conn, -214748m,          DataType.SmallMoney, "smallint");
				TestNumeric(conn, +214748m,          DataType.SmallMoney, "smallint");
			}
		}

		[Test]
		public void TestDate([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12' as date) FROM SYSIBM.SYSDUMMY1"),                          Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12' as date) FROM SYSIBM.SYSDUMMY1"),                          Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime> ("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.Date("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT @p FROM SYSIBM.SYSDUMMY1", new DataParameter("p", dateTime, DataType.Date)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestDateTime([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12 12:12:12' as timestamp) FROM SYSIBM.SYSDUMMY1"),                 Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12' as timestamp) FROM SYSIBM.SYSDUMMY1"),                 Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime> ("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.DateTime("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT @p FROM SYSIBM.SYSDUMMY1", new DataParameter("p", dateTime)),                    Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT @p FROM SYSIBM.SYSDUMMY1", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestTimeSpan([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var time = new TimeSpan(12, 12, 12);

				Assert.That(conn.Execute<TimeSpan> ("SELECT Cast('12:12:12' as time) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(time));
				Assert.That(conn.Execute<TimeSpan?>("SELECT Cast('12:12:12' as time) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(time));

				Assert.That(conn.Execute<TimeSpan> ("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.Time  ("p", time)),              Is.EqualTo(time));
				Assert.That(conn.Execute<TimeSpan> ("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.Create("p", time)),              Is.EqualTo(time));
				Assert.That(conn.Execute<TimeSpan?>("SELECT @p FROM SYSIBM.SYSDUMMY1", new DataParameter("p",  time, DataType.Time)), Is.EqualTo(time));
				Assert.That(conn.Execute<TimeSpan?>("SELECT @p FROM SYSIBM.SYSDUMMY1", new DataParameter("p",  time)),                Is.EqualTo(time));
			}
		}

		[Test]
		public void TestChar([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char) FROM SYSIBM.SYSDUMMY1"),         Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char) FROM SYSIBM.SYSDUMMY1"),         Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char(1)) FROM SYSIBM.SYSDUMMY1"),      Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char(1)) FROM SYSIBM.SYSDUMMY1"),      Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast('1' as varchar(1)) FROM SYSIBM.SYSDUMMY1"),   Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar(1)) FROM SYSIBM.SYSDUMMY1"),   Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as varchar(20)) FROM SYSIBM.SYSDUMMY1"),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar(20)) FROM SYSIBM.SYSDUMMY1"),  Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT @p FROM SYSIBM.SYSDUMMY1",                  DataParameter.Char("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p FROM SYSIBM.SYSDUMMY1",                  DataParameter.Char("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char) FROM SYSIBM.SYSDUMMY1",    DataParameter.Char("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char) FROM SYSIBM.SYSDUMMY1",    DataParameter.Char("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char(1)) FROM SYSIBM.SYSDUMMY1", DataParameter.Char("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM SYSIBM.SYSDUMMY1", DataParameter.Char("p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT @p FROM SYSIBM.SYSDUMMY1", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p FROM SYSIBM.SYSDUMMY1", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
			}
		}

		[Test]
		public void TestString([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(5)) FROM SYSIBM.SYSDUMMY1"),       Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20)) FROM SYSIBM.SYSDUMMY1"),      Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as char(20)) FROM SYSIBM.SYSDUMMY1"),      Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(5)) FROM SYSIBM.SYSDUMMY1"),    Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(20)) FROM SYSIBM.SYSDUMMY1"),   Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as varchar(20)) FROM SYSIBM.SYSDUMMY1"),   Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as clob) FROM SYSIBM.SYSDUMMY1"),          Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as clob) FROM SYSIBM.SYSDUMMY1"),          Is.Null);

				Assert.That(conn.Execute<string>("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.Char    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.VarChar ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.Text    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.NChar   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.NText   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.Create  ("p", "123")), Is.EqualTo("123"));

				Assert.That(conn.Execute<string>("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.Create("p", (string)null)), Is.EqualTo(null));
				Assert.That(conn.Execute<string>("SELECT @p FROM SYSIBM.SYSDUMMY1", new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestBinary([IncludeDataContexts(CurrentProvider)] string context)
		{
			var arr1 = new byte[] {         49, 50 };
			var arr2 = new byte[] { 49, 50, 51, 52 };

			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<byte[]>("SELECT Cast('12' as char(2) for bit data) FROM SYSIBM.SYSDUMMY1"),      Is.EqualTo(           arr1));
				Assert.That(conn.Execute<Binary>("SELECT Cast('1234' as char(4) for bit data) FROM SYSIBM.SYSDUMMY1"),    Is.EqualTo(new Binary(arr2)));

				Assert.That(conn.Execute<byte[]>("SELECT Cast('12' as varchar(2) for bit data) FROM SYSIBM.SYSDUMMY1"),   Is.EqualTo(           arr1));
				Assert.That(conn.Execute<Binary>("SELECT Cast('1234' as varchar(4) for bit data) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(new Binary(arr2)));
			}
		}

		[Test]
		public void TestGuid([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(
					conn.Execute<Guid>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as varchar(38))  FROM SYSIBM.SYSDUMMY1"),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				Assert.That(
					conn.Execute<Guid?>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as varchar(38)) FROM SYSIBM.SYSDUMMY1"),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				var guid = Guid.NewGuid();

				Assert.That(conn.Execute<Guid>("SELECT Cast(@p as char(16) for bit data) FROM SYSIBM.SYSDUMMY1", DataParameter.Create("p", guid)),                Is.EqualTo(guid));
				Assert.That(conn.Execute<Guid>("SELECT Cast(@p as char(16) for bit data) FROM SYSIBM.SYSDUMMY1", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test]
		public void TestXml([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>     ("SELECT '<xml/>' FROM SYSIBM.SYSDUMMY1"),            Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>  ("SELECT '<xml/>' FROM SYSIBM.SYSDUMMY1").ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT '<xml/>' FROM SYSIBM.SYSDUMMY1").InnerXml,   Is.EqualTo("<xml />"));

				var xdoc = XDocument.Parse("<xml/>");
				var xml  = Convert<string,XmlDocument>.Lambda("<xml/>");

				Assert.That(conn.Execute<string>     ("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.Xml("p", "<xml/>")),        Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>  ("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT @p FROM SYSIBM.SYSDUMMY1", DataParameter.Xml("p", xml)). InnerXml,   Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  ("SELECT @p FROM SYSIBM.SYSDUMMY1", new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  ("SELECT @p FROM SYSIBM.SYSDUMMY1", new DataParameter("p", xml)). ToString(), Is.EqualTo("<xml />"));
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
				Assert.That(conn.Execute<TestEnum> ("SELECT 'A' FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'A' FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum> ("SELECT 'B' FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(TestEnum.BB));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'B' FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(TestEnum.BB));
			}
		}

		[Test]
		public void TestEnum2([IncludeDataContexts(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT @p FROM SYSIBM.SYSDUMMY1", new { p = TestEnum.AA }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p FROM SYSIBM.SYSDUMMY1", new { p = (TestEnum?)TestEnum.BB }), Is.EqualTo("B"));
				Assert.That(conn.Execute<string>("SELECT @p FROM SYSIBM.SYSDUMMY1", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p FROM SYSIBM.SYSDUMMY1", new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p FROM SYSIBM.SYSDUMMY1", new { p = conn.MappingSchema.GetConverter<TestEnum?,string>()(TestEnum.AA) }), Is.EqualTo("A"));
			}
		}
	}
}
