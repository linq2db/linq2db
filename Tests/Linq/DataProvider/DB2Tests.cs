using System;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using LinqToDB.Tools.Comparers;

#if NETFRAMEWORK
using IBM.Data.DB2;
#else
using IBM.Data.Db2;
#endif

using IBM.Data.DB2Types;

using NUnit.Framework;

using Tests.Model;

namespace Tests.DataProvider
{
	[TestFixture]
	public class DB2Tests : DataProviderTestBase
	{
		const string CurrentProvider = ProviderName.DB2;

		[Test]
		public void TestParameters([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT Cast(@p  as int)  FROM SYSIBM.SYSDUMMY1", new { p = 1 }), Is.EqualTo("1"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p  as char) FROM SYSIBM.SYSDUMMY1", new { p = "1" }), Is.EqualTo("1"));
					Assert.That(conn.Execute<int>("SELECT Cast(@p  as int)  FROM SYSIBM.SYSDUMMY1", new { p = new DataParameter { Value = 1 } }), Is.EqualTo(1));
					Assert.That(conn.Execute<string>("SELECT Cast(@p1 as char) FROM SYSIBM.SYSDUMMY1", new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
					Assert.That(conn.Execute<int>("SELECT Cast(@p1 as int) + Cast(@p2 as int) FROM SYSIBM.SYSDUMMY1", new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
					Assert.That(conn.Execute<int>("SELECT Cast(@p2 as int) + Cast(@p1 as int) FROM SYSIBM.SYSDUMMY1", new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
				});
			}
		}

		[Test]
		public void TestDataTypes([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(TestType<long?>(conn, "bigintDataType", DataType.Int64, "ALLTYPES"), Is.EqualTo(1000000L));
					Assert.That(TestType<DB2Int64?>(conn, "bigintDataType", DataType.Int64, "ALLTYPES"), Is.EqualTo(new DB2Int64(1000000L)));
					Assert.That(TestType<int?>(conn, "intDataType", DataType.Int32, "ALLTYPES"), Is.EqualTo(7777777));
					Assert.That(TestType<DB2Int32?>(conn, "intDataType", DataType.Int32, "ALLTYPES"), Is.EqualTo(new DB2Int32(7777777)));
					Assert.That(TestType<short?>(conn, "smallintDataType", DataType.Int16, "ALLTYPES"), Is.EqualTo(100));
					Assert.That(TestType<DB2Int16?>(conn, "smallintDataType", DataType.Int16, "ALLTYPES"), Is.EqualTo(new DB2Int16(100)));
					Assert.That(TestType<decimal?>(conn, "decimalDataType", DataType.Decimal, "ALLTYPES"), Is.EqualTo(9999999m));
					Assert.That(TestType<decimal?>(conn, "decfloatDataType", DataType.Decimal, "ALLTYPES"), Is.EqualTo(8888888m));
					Assert.That(TestType<float?>(conn, "realDataType", DataType.Single, "ALLTYPES"), Is.EqualTo(20.31f));
					Assert.That(TestType<DB2Real?>(conn, "realDataType", DataType.Single, "ALLTYPES"), Is.EqualTo(new DB2Real(20.31f)));
					Assert.That(TestType<double?>(conn, "doubleDataType", DataType.Double, "ALLTYPES"), Is.EqualTo(16.2d));
					Assert.That(TestType<DB2Double?>(conn, "doubleDataType", DataType.Double, "ALLTYPES"), Is.EqualTo(new DB2Double(16.2d)));

					Assert.That(TestType<string>(conn, "charDataType", DataType.Char, "ALLTYPES"), Is.EqualTo("1"));
					Assert.That(TestType<string>(conn, "charDataType", DataType.NChar, "ALLTYPES"), Is.EqualTo("1"));
					Assert.That(TestType<DB2String?>(conn, "charDataType", DataType.Char, "ALLTYPES"), Is.EqualTo(new DB2String("1")));
					Assert.That(TestType<string>(conn, "varcharDataType", DataType.VarChar, "ALLTYPES"), Is.EqualTo("234"));
					Assert.That(TestType<string>(conn, "varcharDataType", DataType.NVarChar, "ALLTYPES"), Is.EqualTo("234"));
					Assert.That(TestType<string>(conn, "clobDataType", DataType.Text, "ALLTYPES"), Is.EqualTo("55645"));
					Assert.That(TestType<string>(conn, "dbclobDataType", DataType.NText, "ALLTYPES"), Is.EqualTo("6687"));

					Assert.That(TestType<byte[]>(conn, "binaryDataType", DataType.Binary, "ALLTYPES"), Is.EqualTo(new byte[] { 49, 50, 51, 32, 32 }));
					Assert.That(TestType<byte[]>(conn, "varbinaryDataType", DataType.VarBinary, "ALLTYPES"), Is.EqualTo(new byte[] { 49, 50, 51, 52 }));
					Assert.That(TestType<byte[]>(conn, "blobDataType", DataType.Blob, "ALLTYPES", skipDefaultNull: true, skipUndefinedNull: true, skipDefault: true, skipUndefined: true), Is.EqualTo(new byte[] { 50, 51, 52 }));
					Assert.That(TestType<byte[]>(conn, "blobDataType", DataType.VarBinary, "ALLTYPES", skipDefaultNull: true, skipUndefinedNull: true, skipDefault: true, skipUndefined: true), Is.EqualTo(new byte[] { 50, 51, 52 }));
					Assert.That(TestType<string>(conn, "graphicDataType", DataType.VarChar, "ALLTYPES"), Is.EqualTo("23        "));

					Assert.That(TestType<DateTime?>(conn, "dateDataType", DataType.Date, "ALLTYPES"), Is.EqualTo(new DateTime(2012, 12, 12)));
					Assert.That(TestType<DB2Date?>(conn, "dateDataType", DataType.Date, "ALLTYPES"), Is.EqualTo(new DB2Date(new DateTime(2012, 12, 12))));
					Assert.That(TestType<TimeSpan?>(conn, "timeDataType", DataType.Time, "ALLTYPES"), Is.EqualTo(new TimeSpan(12, 12, 12)));
					Assert.That(TestType<DB2Time?>(conn, "timeDataType", DataType.Time, "ALLTYPES"), Is.EqualTo(new DB2Time(new TimeSpan(12, 12, 12))));
					Assert.That(TestType<DateTime?>(conn, "timestampDataType", DataType.DateTime2, "ALLTYPES"), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));
					Assert.That(TestType<DB2TimeStamp?>(conn, "timestampDataType", DataType.DateTime2, "ALLTYPES"), Is.EqualTo(new DB2TimeStamp(new DateTime(2012, 12, 12, 12, 12, 12, 12))));

					Assert.That(TestType<string>(conn, "xmlDataType", DataType.Xml, "ALLTYPES", skipPass: true), Is.EqualTo("<root><element strattr=\"strvalue\" intattr=\"12345\"/></root>"));

					Assert.That(conn.Execute<byte[]>("SELECT rowid FROM AllTypes WHERE ID = 2"), Is.Not.Empty);
				});
				//Assert.That(conn.Execute<DB2RowId>("SELECT rowid FROM AllTypes WHERE ID = 2").Value.Length, Is.Not.EqualTo(0));

				TestType<DB2Clob>      (conn, "clobDataType",      DataType.Text     , "ALLTYPES", skipNotNull:true);
				            TestType<DB2Blob>      (conn, "blobDataType",      DataType.VarBinary, "ALLTYPES", skipNotNull:true);
				            TestType<DB2Xml>       (conn, "xmlDataType",       DataType.Xml      , "ALLTYPES", skipPass:true);

				Assert.Multiple(() =>
				{
					Assert.That(TestType<DB2Decimal?>(conn, "decimalDataType", DataType.Decimal, "ALLTYPES").ToString(), Is.EqualTo(new DB2Decimal(9999999m).ToString()));
					Assert.That(TestType<DB2Binary>(conn, "varbinaryDataType", DataType.VarBinary, "ALLTYPES").ToString(), Is.EqualTo(new DB2Binary(new byte[] { 49, 50, 51, 52 }).ToString()));
					Assert.That(TestType<DB2DecimalFloat?>(conn, "decfloatDataType", DataType.Decimal, "ALLTYPES").ToString(), Is.EqualTo(new DB2DecimalFloat(8888888m).ToString()));
				});
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
				var sqlValue = expectedValue is bool ? (bool)(object)expectedValue? 1 : 0 : (object?)expectedValue;

				var sql = string.Format(CultureInfo.InvariantCulture, "SELECT Cast({0} as {1}) FROM SYSIBM.SYSDUMMY1", sqlValue ?? "NULL", sqlType);

				Assert.That(conn.Execute<T>(sql), Is.EqualTo(expectedValue));
			}

			// [IBM][DB2/LINUXX8664] SQL0418N  The statement was not processed because the statement contains an invalid use of one of the following: an untyped parameter marker, the DEFAULT keyword, or a null value.
//			Assert.That(conn.Execute<T>("SELECT @p FROM SYSIBM.SYSDUMMY1", new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
//			Assert.That(conn.Execute<T>("SELECT @p FROM SYSIBM.SYSDUMMY1", new DataParameter { Name = "p", Value = expectedValue }), Is.EqualTo(expectedValue));
//			Assert.That(conn.Execute<T>("SELECT @p FROM SYSIBM.SYSDUMMY1", new { p = expectedValue }), Is.EqualTo(expectedValue));
		}

		static void TestSimple<T>(DataConnection conn, T expectedValue, DataType dataType)
			where T : struct
		{
			TestNumeric<T> (conn, expectedValue, dataType);
			TestNumeric<T?>(conn, expectedValue, dataType);
			TestNumeric<T?>(conn, (T?)null,      dataType);
		}

		[Test]
		public void TestNumerics([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var conn = GetDataConnection(context))
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
		public void TestDate([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12);

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<DateTime>("SELECT Cast('2012-12-12' as date) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12' as date) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime>("SELECT Cast(@p as date) FROM SYSIBM.SYSDUMMY1", DataParameter.Date("p", dateTime)), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>("SELECT Cast(@p as date) FROM SYSIBM.SYSDUMMY1", new DataParameter("p", dateTime, DataType.Date)), Is.EqualTo(dateTime));
				});
			}
		}

		[Test]
		public void TestDateTime([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<DateTime>("SELECT Cast('2012-12-12 12:12:12' as timestamp) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12' as timestamp) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(dateTime));

					Assert.That(conn.Execute<DateTime>("SELECT Cast(@p as timestamp) FROM SYSIBM.SYSDUMMY1", DataParameter.DateTime("p", dateTime)), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>("SELECT Cast(@p as timestamp) FROM SYSIBM.SYSDUMMY1", new DataParameter("p", dateTime)), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>("SELECT Cast(@p as timestamp) FROM SYSIBM.SYSDUMMY1", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
				});
			}
		}

		[Test]
		public void TestTimeSpan([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var time = new TimeSpan(12, 12, 12);

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<TimeSpan>("SELECT Cast('12:12:12' as time) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(time));
					Assert.That(conn.Execute<TimeSpan?>("SELECT Cast('12:12:12' as time) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(time));

					Assert.That(conn.Execute<TimeSpan>("SELECT Cast(@p as time) FROM SYSIBM.SYSDUMMY1", DataParameter.Time("p", time)), Is.EqualTo(time));
					Assert.That(conn.Execute<TimeSpan>("SELECT Cast(@p as time) FROM SYSIBM.SYSDUMMY1", DataParameter.Create("p", time)), Is.EqualTo(time));
					Assert.That(conn.Execute<TimeSpan?>("SELECT Cast(@p as time) FROM SYSIBM.SYSDUMMY1", new DataParameter("p", time, DataType.Time)), Is.EqualTo(time));
					Assert.That(conn.Execute<TimeSpan?>("SELECT Cast(@p as time) FROM SYSIBM.SYSDUMMY1", new DataParameter("p", time)), Is.EqualTo(time));
				});
			}
		}

		[Test]
		public void TestChar([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<char>("SELECT Cast('1' as char) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast('1' as char) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast('1' as char(1)) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast('1' as char(1)) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo('1'));

					Assert.That(conn.Execute<char>("SELECT Cast('1' as varchar(1)) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar(1)) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast('1' as varchar(20)) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar(20)) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo('1'));

					// [IBM][DB2/LINUXX8664] SQL0418N  The statement was not processed because the statement contains an invalid use of one of the following: an untyped parameter marker, the DEFAULT keyword, or a null value.
					//Assert.That(conn.Execute<char> ("SELECT @p FROM SYSIBM.SYSDUMMY1",                  DataParameter.Char("p", '1')), Is.EqualTo('1'));
					//Assert.That(conn.Execute<char?>("SELECT @p FROM SYSIBM.SYSDUMMY1",                  DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast(@p as char) FROM SYSIBM.SYSDUMMY1", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as char) FROM SYSIBM.SYSDUMMY1", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast(@p as char(1)) FROM SYSIBM.SYSDUMMY1", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM SYSIBM.SYSDUMMY1", DataParameter.Char("p", '1')), Is.EqualTo('1'));

					Assert.That(conn.Execute<char>("SELECT Cast(@p as varchar) FROM SYSIBM.SYSDUMMY1", DataParameter.VarChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as varchar) FROM SYSIBM.SYSDUMMY1", DataParameter.VarChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast(@p as nchar) FROM SYSIBM.SYSDUMMY1", DataParameter.NChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as nchar) FROM SYSIBM.SYSDUMMY1", DataParameter.NChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast(@p as nvarchar) FROM SYSIBM.SYSDUMMY1", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as nvarchar) FROM SYSIBM.SYSDUMMY1", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast(@p as char) FROM SYSIBM.SYSDUMMY1", DataParameter.Create("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as char) FROM SYSIBM.SYSDUMMY1", DataParameter.Create("p", '1')), Is.EqualTo('1'));

					Assert.That(conn.Execute<char>("SELECT Cast(@p as char) FROM SYSIBM.SYSDUMMY1", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as char) FROM SYSIBM.SYSDUMMY1", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				});
			}
		}

		[Test]
		public void TestString([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(5)) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20)) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast(NULL    as char(20)) FROM SYSIBM.SYSDUMMY1"), Is.Null);

					Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(5)) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(20)) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast(NULL    as varchar(20)) FROM SYSIBM.SYSDUMMY1"), Is.Null);

					Assert.That(conn.Execute<string>("SELECT Cast('12345' as clob) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast(NULL    as clob) FROM SYSIBM.SYSDUMMY1"), Is.Null);

					Assert.That(conn.Execute<string>("SELECT Cast(@p as char(3))     FROM SYSIBM.SYSDUMMY1", DataParameter.Char("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3))  FROM SYSIBM.SYSDUMMY1", DataParameter.VarChar("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as char(3))     FROM SYSIBM.SYSDUMMY1", DataParameter.Text("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as nchar(3))    FROM SYSIBM.SYSDUMMY1", DataParameter.NChar("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as nvarchar(3)) FROM SYSIBM.SYSDUMMY1", DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as nchar(3))    FROM SYSIBM.SYSDUMMY1", DataParameter.NText("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as char(3))     FROM SYSIBM.SYSDUMMY1", DataParameter.Create("p", "123")), Is.EqualTo("123"));

					Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM SYSIBM.SYSDUMMY1", DataParameter.Create("p", (string?)null)), Is.Null);
					Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM SYSIBM.SYSDUMMY1", new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
				});
			}
		}

		[Test]
		public void TestBinary([IncludeDataSources(CurrentProvider)] string context)
		{
			var arr1 = new byte[] {         49, 50 };
			var arr2 = new byte[] { 49, 50, 51, 52 };

			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<byte[]>("SELECT Cast('12' as char(2) for bit data) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(arr1));
					Assert.That(conn.Execute<Binary>("SELECT Cast('1234' as char(4) for bit data) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(new Binary(arr2)));

					Assert.That(conn.Execute<byte[]>("SELECT Cast('12' as varchar(2) for bit data) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(arr1));
					Assert.That(conn.Execute<Binary>("SELECT Cast('1234' as varchar(4) for bit data) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(new Binary(arr2)));
				});
			}
		}

		[Test]
		public void TestGuid([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(
									conn.Execute<Guid>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as varchar(38))  FROM SYSIBM.SYSDUMMY1"),
									Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

					Assert.That(
						conn.Execute<Guid?>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as varchar(38)) FROM SYSIBM.SYSDUMMY1"),
						Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));
				});

				var guid = TestData.Guid1;

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<Guid>("SELECT Cast(@p as char(16) for bit data) FROM SYSIBM.SYSDUMMY1", DataParameter.Create("p", guid)), Is.EqualTo(guid));
					Assert.That(conn.Execute<Guid>("SELECT Cast(@p as char(16) for bit data) FROM SYSIBM.SYSDUMMY1", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
				});
			}
		}

		[Test]
		public void TestXml([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT Cast('<xml/>' as char(10)) FROM SYSIBM.SYSDUMMY1"), Is.EqualTo("<xml/>"));
					Assert.That(conn.Execute<XDocument>("SELECT Cast('<xml/>' as char(10)) FROM SYSIBM.SYSDUMMY1").ToString(), Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XmlDocument>("SELECT Cast('<xml/>' as char(10)) FROM SYSIBM.SYSDUMMY1").InnerXml, Is.EqualTo("<xml />"));
				});

				var xdoc = XDocument.Parse("<xml/>");
				var xml  = Convert<string,XmlDocument>.Lambda("<xml/>");

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT Cast(@p as char(10)) FROM SYSIBM.SYSDUMMY1", DataParameter.Xml("p", "<xml/>")), Is.EqualTo("<xml/>"));
					Assert.That(conn.Execute<XDocument>("SELECT Cast(@p as char(10)) FROM SYSIBM.SYSDUMMY1", DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XmlDocument>("SELECT Cast(@p as char(10)) FROM SYSIBM.SYSDUMMY1", DataParameter.Xml("p", xml)).InnerXml, Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XDocument>("SELECT Cast(@p as char(10)) FROM SYSIBM.SYSDUMMY1", new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XDocument>("SELECT Cast(@p as char(10)) FROM SYSIBM.SYSDUMMY1", new DataParameter("p", xml)).ToString(), Is.EqualTo("<xml />"));
				});
			}
		}

		enum TestEnum
		{
			[MapValue("A")] AA,
			[MapValue("B")] BB,
		}

		[Test]
		public void TestEnum1([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<TestEnum>("SELECT 'A' FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(TestEnum.AA));
					Assert.That(conn.Execute<TestEnum?>("SELECT 'A' FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(TestEnum.AA));
					Assert.That(conn.Execute<TestEnum>("SELECT 'B' FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(TestEnum.BB));
					Assert.That(conn.Execute<TestEnum?>("SELECT 'B' FROM SYSIBM.SYSDUMMY1"), Is.EqualTo(TestEnum.BB));
				});
			}
		}

		[Test]
		public void TestEnum2([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM SYSIBM.SYSDUMMY1", new { p = TestEnum.AA }), Is.EqualTo("A"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM SYSIBM.SYSDUMMY1", new { p = (TestEnum?)TestEnum.BB }), Is.EqualTo("B"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM SYSIBM.SYSDUMMY1", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM SYSIBM.SYSDUMMY1", new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM SYSIBM.SYSDUMMY1", new { p = conn.MappingSchema.GetConverter<TestEnum?, string>()!(TestEnum.AA) }), Is.EqualTo("A"));
				});
			}
		}

		void BulkCopyTest(string context, BulkCopyType bulkCopyType, int maxSize, int batchSize)
		{
			using (var conn = GetDataConnection(context))
			{
				try
				{
					conn.BulkCopy(
						new BulkCopyOptions
						{
							MaxBatchSize       = maxSize,
							BulkCopyType       = bulkCopyType,
							NotifyAfter        = 10000,
						},
						Enumerable.Range(0, batchSize).Select(n =>
							new ALLTYPE
							{
								ID                = 2000 + n,
								BIGINTDATATYPE    = 3000 + n,
								INTDATATYPE       = 4000 + n,
								SMALLINTDATATYPE  = (short)(5000 + n),
								DECIMALDATATYPE   = 6000 + n,
								DECFLOATDATATYPE  = 7000 + n,
								REALDATATYPE      = 8000 + n,
								DOUBLEDATATYPE    = 9000 + n,
								CHARDATATYPE      = 'A',
								VARCHARDATATYPE   = "",
								CLOBDATATYPE      = null,
								DBCLOBDATATYPE    = null,
								BINARYDATATYPE    = null,
								VARBINARYDATATYPE = null,
								BLOBDATATYPE      = new byte[] { 1, 2, 3 },
								GRAPHICDATATYPE   = null,
								DATEDATATYPE      = TestData.DateTime,
								TIMEDATATYPE      = null,
								TIMESTAMPDATATYPE = null,
								XMLDATATYPE       = null,
							}));

				}
				finally
				{
					conn.GetTable<ALLTYPE>().Delete(p => p.SMALLINTDATATYPE >= 5000);
				}
			}
		}

		async Task BulkCopyTestAsync(string context, BulkCopyType bulkCopyType, int maxSize, int batchSize)
		{
			using (var conn = GetDataConnection(context))
			{
				try
				{
					await conn.BulkCopyAsync(
						new BulkCopyOptions
						{
							MaxBatchSize       = maxSize,
							BulkCopyType       = bulkCopyType,
							NotifyAfter        = 10000,
						},
						Enumerable.Range(0, batchSize).Select(n =>
							new ALLTYPE
							{
								ID                = 2000 + n,
								BIGINTDATATYPE    = 3000 + n,
								INTDATATYPE       = 4000 + n,
								SMALLINTDATATYPE  = (short)(5000 + n),
								DECIMALDATATYPE   = 6000 + n,
								DECFLOATDATATYPE  = 7000 + n,
								REALDATATYPE      = 8000 + n,
								DOUBLEDATATYPE    = 9000 + n,
								CHARDATATYPE      = 'A',
								VARCHARDATATYPE   = "",
								CLOBDATATYPE      = null,
								DBCLOBDATATYPE    = null,
								BINARYDATATYPE    = null,
								VARBINARYDATATYPE = null,
								BLOBDATATYPE      = new byte[] { 1, 2, 3 },
								GRAPHICDATATYPE   = null,
								DATEDATATYPE      = TestData.DateTime,
								TIMEDATATYPE      = null,
								TIMESTAMPDATATYPE = null,
								XMLDATATYPE       = null,
							}));
				}
				finally
				{
					await conn.GetTable<ALLTYPE>().DeleteAsync(p => p.SMALLINTDATATYPE >= 5000);
				}
			}
		}

		[Test]
		public void BulkCopyMultipleRows([IncludeDataSources(CurrentProvider)] string context)
		{
			BulkCopyTest(context, BulkCopyType.MultipleRows, 5000, 10001);
		}

		[Test]
		public void BulkCopyProviderSpecific([IncludeDataSources(CurrentProvider)] string context)
		{
			BulkCopyTest(context, BulkCopyType.ProviderSpecific, 50000, 100001);
		}

		[Test]
		public async Task BulkCopyMultipleRowsAsync([IncludeDataSources(CurrentProvider)] string context)
		{
			await BulkCopyTestAsync(context, BulkCopyType.MultipleRows, 5000, 10001);
		}

		[Test]
		public async Task BulkCopyProviderSpecificAsync([IncludeDataSources(CurrentProvider)] string context)
		{
			await BulkCopyTestAsync(context, BulkCopyType.ProviderSpecific, 50000, 100001);
		}

		[Test]
		public void BulkCopyLinqTypes([IncludeDataSources(CurrentProvider)] string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using (var db = GetDataConnection(context))
				{
					try
					{
						db.BulkCopy(
							new BulkCopyOptions { BulkCopyType = bulkCopyType, },
							Enumerable.Range(0, 10).Select(n =>
								new LinqDataTypes
								{
									ID            = 4000 + n,
									MoneyValue    = 1000m + n,
									DateTimeValue = new DateTime(2001, 1, 11, 1, 11, 21, 100),
									BoolValue     = true,
									GuidValue     = TestData.SequentialGuid(n),
									SmallIntValue = (short)n
								}
							));
					}
					finally
					{
						db.GetTable<LinqDataTypes>().Delete(p => p.ID >= 4000);
					}
				}
			}
		}

		[Test]
		public async Task BulkCopyLinqTypesAsync([IncludeDataSources(CurrentProvider)] string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using (var db = GetDataConnection(context))
				{
					try
					{
						await db.BulkCopyAsync(
							new BulkCopyOptions { BulkCopyType = bulkCopyType, },
							Enumerable.Range(0, 10).Select(n =>
								new LinqDataTypes
								{
									ID            = 4000 + n,
									MoneyValue    = 1000m + n,
									DateTimeValue = new DateTime(2001, 1, 11, 1, 11, 21, 100),
									BoolValue     = true,
									GuidValue     = TestData.SequentialGuid(n),
									SmallIntValue = (short)n
								}
							));
					}
					finally
					{
						await db.GetTable<LinqDataTypes>().DeleteAsync(p => p.ID >= 4000);
					}
				}
			}
		}

		[Test]
		public void TestBinarySize([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				try
				{
					var data = new byte[500000];

					for (var i = 0; i < data.Length; i++)
						data[i] = (byte)(i % byte.MaxValue);

					conn.GetTable<ALLTYPE>().Insert(() => new ALLTYPE
					{
						INTDATATYPE  = 2000,
						BLOBDATATYPE = data,
					});

					var blob = conn.GetTable<ALLTYPE>().First(t => t.INTDATATYPE == 2000).BLOBDATATYPE;

					Assert.That(blob, Is.EqualTo(data));
				}
				finally
				{
					conn.GetTable<ALLTYPE>().Delete(p => p.INTDATATYPE == 2000);
				}
			}
		}

		[Test]
		public void TestClobSize([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				try
				{
					var sb = new StringBuilder();

					for (var i = 0; i < 100000; i++)
						sb.Append((char)((i % byte.MaxValue) + 32));

					var data = sb.ToString();

					conn.GetTable<ALLTYPE>().Insert(() => new ALLTYPE
					{
						INTDATATYPE  = 2000,
						CLOBDATATYPE = data,
					});

					var blob = conn.GetTable<ALLTYPE>()
						.Where (t => t.INTDATATYPE == 2000)
						.Select(t => t.CLOBDATATYPE)
						.First();

					Assert.That(blob, Is.EqualTo(data));
				}
				finally
				{
					conn.GetTable<ALLTYPE>().Delete(p => p.INTDATATYPE == 2000);
				}
			}
		}

		[Test]
		public void TestTypes([IncludeDataSources(CurrentProvider)] string context)
		{
			//IBM.Data.DB2.DB2Parameter p = null;
			//p.
			//new IBM.Data.DB2Types.DB2RowId();

			var int64Value = new DB2Int64(1);
			var int32Value = new DB2Int32(2);
			var int16Value = new DB2Int16(3);

			using (var conn = GetDataConnection(context))
			{
				conn.Select(() => 1);

				Assert.Multiple(() =>
				{
					var connection = (DB2Connection)conn.OpenDbConnection();
					Assert.That(new DB2Clob(connection).IsNull, Is.True);
					Assert.That(new DB2Blob(connection).IsNull, Is.True);
				});
			}

			Assert.Multiple(() =>
			{
				Assert.That(int64Value.Value, Is.TypeOf<long>().And.EqualTo(1));
				Assert.That(int32Value.Value, Is.TypeOf<int>().And.EqualTo(2));
				Assert.That(int16Value.Value, Is.TypeOf<short>().And.EqualTo(3));
			});

			var decimalValue          = new DB2Decimal     (4m);
			var decimalValueAsDecimal = new DB2DecimalFloat(5m);
			var decimalValueAsDouble  = new DB2DecimalFloat(6.0);
			var decimalValueAsLong    = new DB2DecimalFloat(7);
			var realValue             = new DB2Real        (8);
			var real370Value          = new DB2Real370     (9);
			var stringValue           = new DB2String      ("1");
			var clobValue             = new DB2Clob        ("2");
			var binaryValue           = new DB2Binary      (new byte[] { 1 });
			var blobValue             = new DB2Blob        (new byte[] { 2 });
			var dateValue             = new DB2Date        (new DateTime(2000, 1, 1));
			var timeValue             = new DB2Time        (new TimeSpan(1, 1, 1));

			//if (DB2Types.DB2DateTime.Type != null)
			{
				var dateTimeValue1 = new DB2DateTime(new DateTime(2000, 1, 2));
				var dateTimeValue2 = new DB2DateTime(new DateTime(2000, 1, 3).Ticks);
				var timeStampValue = new DB2DateTime(new DateTime(2000, 1, 4));

				Assert.Multiple(() =>
				{
					Assert.That(dateTimeValue1.Value, Is.TypeOf<DateTime>().And.EqualTo(new DateTime(2000, 1, 2)));
					Assert.That(dateTimeValue2.Value, Is.TypeOf<DateTime>().And.EqualTo(new DateTime(2000, 1, 3)));
					Assert.That(timeStampValue.Value, Is.TypeOf<DateTime>().And.EqualTo(new DateTime(2000, 1, 4)));
				});
			}

			Assert.Multiple(() =>
			{
				Assert.That(decimalValue.Value, Is.TypeOf<decimal>().And.EqualTo(4));
				Assert.That(decimalValueAsDecimal.Value, Is.TypeOf<decimal>().And.EqualTo(5));
				Assert.That(decimalValueAsDouble.Value, Is.TypeOf<decimal>().And.EqualTo(6));
				Assert.That(decimalValueAsLong.Value, Is.TypeOf<decimal>().And.EqualTo(7));
				Assert.That(realValue.Value, Is.TypeOf<float>().And.EqualTo(8));
				Assert.That(real370Value.Value, Is.TypeOf<double>().And.EqualTo(9));
				Assert.That(stringValue.Value, Is.TypeOf<string>().And.EqualTo("1"));
				Assert.That(clobValue.Value, Is.TypeOf<string>().And.EqualTo("2"));
				Assert.That(binaryValue.Value, Is.TypeOf<byte[]>().And.EqualTo(new byte[] { 1 }));
				Assert.That(blobValue.Value, Is.TypeOf<byte[]>().And.EqualTo(new byte[] { 2 }));
				Assert.That(dateValue.Value, Is.TypeOf<DateTime>().And.EqualTo(new DateTime(2000, 1, 1)));
				Assert.That(timeValue.Value, Is.TypeOf<TimeSpan>().And.EqualTo(new TimeSpan(1, 1, 1)));
			});

			int64Value = new DB2Int64();
			int32Value = new DB2Int32();
			int16Value = new DB2Int16();

			Assert.Multiple(() =>
			{
				Assert.That(int64Value.IsNull, Is.True);
				Assert.That(int32Value.IsNull, Is.True);
				Assert.That(int16Value.IsNull, Is.True);

				Assert.That(new DB2Decimal().IsNull, Is.True);
				Assert.That(new DB2DecimalFloat().IsNull, Is.False);
				Assert.That(new DB2Real().IsNull, Is.True);
				Assert.That(new DB2Real370().IsNull, Is.True);
				Assert.That(new DB2String().IsNull, Is.True);
				Assert.That(new DB2Binary().IsNull, Is.True);
				Assert.That(new DB2Date().IsNull, Is.True);
				Assert.That(new DB2Time().IsNull, Is.True);
				Assert.That(new DB2TimeStamp().IsNull, Is.True);
				Assert.That(new DB2RowId().IsNull, Is.True);
				Assert.That(new DB2DateTime().IsNull, Is.True);
			});
		}

		[Table]
		sealed class TestTimeTypes
		{
			[Column]
			public int Id { get; set; }

			[Column(DataType = DataType.Date)]
			public DateTime Date1 { get; set; }

			[Column(DbType = "Date")]
			public DateTime Date2 { get; set; }

			[Column]
			public TimeSpan Time { get; set; }

			[Column(Precision = 0)]
			public DateTime TimeStamp0 { get; set; }

			[Column(DbType = "timestamp(1)")]
			public DateTime TimeStamp1 { get; set; }

			[Column(Precision = 2)]
			public DateTime TimeStamp2 { get; set; }

			//[Column(DbType = "timestamp(3)")]
			[Column(Precision = 3)]
			public DateTime TimeStamp3 { get; set; }

			[Column(Precision = 4)]
			public DateTime TimeStamp4 { get; set; }

			//[Column(DbType = "TimeStamp(5)")]
			[Column(Precision = 5)]
			public DateTime TimeStamp5 { get; set; }

			[Column(Precision = 6)]
			public DateTime TimeStamp6 { get; set; }

			//[Column(DbType = "timestamp(7)")]
			[Column(Precision = 7)]
			public DateTime TimeStamp7 { get; set; }

			[Column(Precision = 8)]
			public DB2TimeStamp TimeStamp8 { get; set; }

			//[Column(DbType = "timestamp(9)")]
			[Column(Precision = 9)]
			public DB2TimeStamp TimeStamp9 { get; set; }

			[Column(Precision = 10)]
			public DB2TimeStamp TimeStamp10 { get; set; }

			//[Column(DbType = "timestamp(11)")]
			[Column(Precision = 11)]
			public DB2TimeStamp TimeStamp11 { get; set; }

			[Column(Precision = 12)]
			public DB2TimeStamp TimeStamp12 { get; set; }

			static TestTimeTypes()
			{
				Data = new[]
				{
					new TestTimeTypes() { Id = 1, Date1 = new DateTime(1234, 5, 6), Date2 = new DateTime(1234, 5, 7), Time = new TimeSpan(21, 2, 3) },
					new TestTimeTypes() { Id = 2, Date1 = new DateTime(6543, 2, 1), Date2 = new DateTime(1234, 5, 8), Time = new TimeSpan(23, 2, 1) }
				};

				for (var i = 1; i <= Data.Length; i++)
				{
					var idx = i - 1;
					Data[idx].TimeStamp0  = new     DateTime(1000, 1, 10, 2, 20, 30 + i, 0);
					Data[idx].TimeStamp1  = new     DateTime(1000, 1, 10, 2, 20, 30, i * 100);
					Data[idx].TimeStamp2  = new     DateTime(1000, 1, 10, 2, 20, 30, i * 10);
					Data[idx].TimeStamp3  = new     DateTime(1000, 1, 10, 2, 20, 30, i);
					Data[idx].TimeStamp4  = new     DateTime(1000, 1, 10, 2, 20, 30, 1).AddTicks(1000 * i);
					Data[idx].TimeStamp5  = new     DateTime(1000, 1, 10, 2, 20, 30, 1).AddTicks(100 * i);
					Data[idx].TimeStamp6  = new     DateTime(1000, 1, 10, 2, 20, 30, 1).AddTicks(10 * i);
					Data[idx].TimeStamp7  = new     DateTime(1000, 1, 10, 2, 20, 30, 1).AddTicks(1 * i);
					Data[idx].TimeStamp8  = new DB2TimeStamp(1000, 1, 10, 2, 20, 30, 10000 * i, 8);
					Data[idx].TimeStamp9  = new DB2TimeStamp(1000, 1, 10, 2, 20, 30, 1000 * i, 9);
					Data[idx].TimeStamp10 = new DB2TimeStamp(1000, 1, 10, 2, 20, 30, 100 * i, 10);
					Data[idx].TimeStamp11 = new DB2TimeStamp(1000, 1, 10, 2, 20, 30, 10 * i, 11);
					Data[idx].TimeStamp12 = new DB2TimeStamp(1000, 1, 10, 2, 20, 30, i, 12);
				}
			}

			public static TestTimeTypes[] Data;

			public static Func<TestTimeTypes, TestTimeTypes, bool> Comparer = ComparerBuilder.GetEqualsFunc<TestTimeTypes>();
		}

		[ActiveIssue(SkipForNonLinqService = true, Details = "RemoteContext miss provider-specific types mappings. Could be workarounded by explicit column mappings")]
		[Test]
		public void TestTimespanAndTimeValues([IncludeDataSources(true, ProviderName.DB2)] string context, [Values] bool useParameters)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(TestTimeTypes.Data))
			{
				db.InlineParameters = !useParameters;

				var record = table.Where(_ => _.Id == 1).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);

				record = table.Where(_ => _.Date1 == TestTimeTypes.Data[0].Date1).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);

				record = table.Where(_ => _.Date2 == TestTimeTypes.Data[0].Date2).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);

				record = table.Where(_ => _.Time == TestTimeTypes.Data[0].Time).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);

				record = table.Where(_ => _.TimeStamp0 == TestTimeTypes.Data[0].TimeStamp0).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);

				record = table.Where(_ => _.TimeStamp1 == TestTimeTypes.Data[0].TimeStamp1).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);

				record = table.Where(_ => _.TimeStamp2 == TestTimeTypes.Data[0].TimeStamp2).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);

				record = table.Where(_ => _.TimeStamp3 == TestTimeTypes.Data[0].TimeStamp3).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);

				record = table.Where(_ => _.TimeStamp4 == TestTimeTypes.Data[0].TimeStamp4).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);

				record = table.Where(_ => _.TimeStamp5 == TestTimeTypes.Data[0].TimeStamp5).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);

				record = table.Where(_ => _.TimeStamp6 == TestTimeTypes.Data[0].TimeStamp6).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);

				record = table.Where(_ => _.TimeStamp7 == TestTimeTypes.Data[0].TimeStamp7).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);

				record = table.Where(_ => Compare(_.TimeStamp8, TestTimeTypes.Data[0].TimeStamp8)).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);

				record = table.Where(_ => Compare(_.TimeStamp9, TestTimeTypes.Data[0].TimeStamp9)).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);

				record = table.Where(_ => Compare(_.TimeStamp10, TestTimeTypes.Data[0].TimeStamp10)).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);

				record = table.Where(_ => Compare(_.TimeStamp11, TestTimeTypes.Data[0].TimeStamp11)).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);

				record = table.Where(_ => Compare(_.TimeStamp12, TestTimeTypes.Data[0].TimeStamp12)).Single();
				Assert.That(TestTimeTypes.Comparer(record, TestTimeTypes.Data[0]), Is.True);
			}
		}

		[Sql.Expression("{0} = {1}", IsPredicate = true, ServerSideOnly = true, PreferServerSide = true)]
		private static bool Compare(DB2TimeStamp left, DB2TimeStamp right)
		{
			throw new InvalidOperationException();
		}

		[Table]
		sealed class TestParametersTable
		{
			[ Column] public int     Id   { get; set; }
			[ Column] public string? Text { get; set; }
		}
		// https://github.com/linq2db/linq2db/issues/2091
		[Test]
		public void TestParametersUsed([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var db    = GetDataConnection(context))
			using (var table = db.CreateLocalTable<TestParametersTable>())
			{
				var newText = new TestParametersTable() { Id = 12, Text = "Hallo Welt!" };
				db.Insert(newText);

				var text   = "bla";
				var query  = from f in table where f.Text == text select f;
				var result = query.ToArray();

				Assert.That(db.LastQuery!, Does.Contain("@"));
			}
		}

		[Test]
		public void Issue2763Test([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				// DB2 SYSCAT.COLUMNS.TABSCHEMA column is padded with spaces to max(schema.length) length despite it being of varchar type
				var schemas = db.Query<string>("SELECT SCHEMANAME FROM SYSCAT.SCHEMATA").AsEnumerable().Select(_ => _.TrimEnd(' ')).ToArray();

				if (schemas.Select(_ => _.Length).Distinct().Count() < 2)
					Assert.Inconclusive("Test requires at least two schemas with different name length");

				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions() { IncludedSchemas = schemas });

				var usedSchemas = new HashSet<string>();
				foreach (var table in schema.Tables)
				{
					Assert.Multiple(() =>
					{
						Assert.That(table.ID!, Does.Not.Contain(' '));
						Assert.That(table.SchemaName!, Does.Not.EndWith(" "));
						Assert.That(table.Columns, Is.Not.Empty);
					});
					usedSchemas.Add(table.SchemaName!);
				}

				Assert.That(usedSchemas.Select(_ => _.Length).Distinct().Count(), Is.GreaterThan(1));
			}
		}

		[Test]
		public void TestModule([IncludeDataSources(false, ProviderName.DB2)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var parameters = new []
				{
					new DataParameter("I", 1, DataType.Int32),
					new DataParameter("O", null, DataType.Int32)
					{
						Direction = ParameterDirection.Output
					}
				};

				Assert.Multiple(() =>
				{
					Assert.That(db.QueryProc<int>("TEST_PROCEDURE", new { i = 1 }).First(), Is.EqualTo(4));
					Assert.That(db.QueryProc<int>("TEST_MODULE1.TEST_PROCEDURE", new { i = 1 }).First(), Is.EqualTo(2));
					Assert.That(db.QueryProc<int>("TEST_MODULE2.TEST_PROCEDURE", new { i = 1 }).First(), Is.EqualTo(3));

					Assert.That(db.Person.Select(p => DB2ModuleFunctions.TestFunction(1)).First(), Is.EqualTo(4));
					Assert.That(db.Person.Select(p => DB2ModuleFunctions.TestFunctionP1(1)).First(), Is.EqualTo(2));
					Assert.That(db.Person.Select(p => DB2ModuleFunctions.TestFunctionP2(1)).First(), Is.EqualTo(3));

					Assert.That(DB2ModuleFunctions.TestTableFunction(db, 1).Select(r => r.O).First(), Is.EqualTo(4));
					Assert.That(DB2ModuleFunctions.TestTableFunctionP1(db, 1).Select(r => r.O).First(), Is.EqualTo(2));
					Assert.That(DB2ModuleFunctions.TestTableFunctionP2(db, 1).Select(r => r.O).First(), Is.EqualTo(3));
				});
			}
		}

		static class DB2ModuleFunctions
		{
			[Sql.Function("TEST_FUNCTION", ServerSideOnly = true)]
			public static int TestFunction(int param)
			{
				throw new InvalidOperationException("Scalar function cannot be called outside of query");
			}

			[Sql.Function("TEST_MODULE1.TEST_FUNCTION", ServerSideOnly = true)]
			public static int TestFunctionP1(int param)
			{
				throw new InvalidOperationException("Scalar function cannot be called outside of query");
			}

			[Sql.Function("TEST_MODULE2.TEST_FUNCTION", ServerSideOnly = true)]
			public static int TestFunctionP2(int param)
			{
				throw new InvalidOperationException("Scalar function cannot be called outside of query");
			}

			[Sql.TableFunction("TEST_TABLE_FUNCTION", argIndices: new[] { 1 })]
			public static LinqToDB.ITable<Record> TestTableFunction(IDataContext db, int param1)
			{
				return db.GetTable<Record>(null, (MethodInfo)MethodBase.GetCurrentMethod()!, db, param1);
			}

			[Sql.TableFunction("TEST_TABLE_FUNCTION", argIndices: new[] { 1 }, Package = "TEST_MODULE1")]
			public static LinqToDB.ITable<Record> TestTableFunctionP1(IDataContext db, int param1)
			{
				return db.GetTable<Record>(null, (MethodInfo)MethodBase.GetCurrentMethod()!, db, param1);
			}

			[Sql.TableFunction("TEST_TABLE_FUNCTION", argIndices: new[] { 1 }, Package = "TEST_MODULE2")]
			public static LinqToDB.ITable<Record> TestTableFunctionP2(IDataContext db, int param1)
			{
				return db.GetTable<Record>(null, (MethodInfo)MethodBase.GetCurrentMethod()!, db, param1);
			}

			public sealed class Record
			{
				public int O { get; set; }
			}
		}
	}
}
