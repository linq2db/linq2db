﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

#if !NETSTANDARD1_6
using Microsoft.SqlServer.Types;
using SqlServerTypes;
#endif

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class SqlServerTests : DataProviderTestBase
	{
#if !NETSTANDARD1_6 && !MONO
		[OneTimeSetUp]
		protected void InitializeFixture()
		{
			// load spatial types support
			//Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
		}
#endif

		[Test]
		public void TestParameters([IncludeDataSources(TestProvName.AllSqlServer)] string context)
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
		public void TestDataTypes([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(TestType<long?>    (conn, "bigintDataType",           DataType.Int64),         Is.EqualTo(1000000L));
				Assert.That(TestType<decimal?> (conn, "numericDataType",          DataType.Decimal),       Is.EqualTo(9999999m));
				Assert.That(TestType<bool?>    (conn, "bitDataType",              DataType.Boolean),       Is.EqualTo(true));
				Assert.That(TestType<short?>   (conn, "smallintDataType",         DataType.Int16),         Is.EqualTo(25555));
				Assert.That(TestType<decimal?> (conn, "decimalDataType",          DataType.Decimal),       Is.EqualTo(2222222m));
				Assert.That(TestType<decimal?> (conn, "smallmoneyDataType",       DataType.SmallMoney,
					skipUndefinedNull : context == ProviderName.SqlServer2000),                            Is.EqualTo(100000m));
				Assert.That(TestType<int?>     (conn, "intDataType",              DataType.Int32),         Is.EqualTo(7777777));
				Assert.That(TestType<sbyte?>   (conn, "tinyintDataType",          DataType.SByte),         Is.EqualTo(100));
				Assert.That(TestType<decimal?> (conn, "moneyDataType",            DataType.Money,
					skipUndefinedNull : context == ProviderName.SqlServer2000),                            Is.EqualTo(100000m));
				Assert.That(TestType<double?>  (conn, "floatDataType",            DataType.Double),        Is.EqualTo(20.31d));
				Assert.That(TestType<float?>   (conn, "realDataType",             DataType.Single),        Is.EqualTo(16.2f));

				Assert.That(TestType<DateTime?>(conn, "datetimeDataType",         DataType.DateTime),      Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(TestType<DateTime?>(conn, "smalldatetimeDataType",    DataType.SmallDateTime), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 00)));

				Assert.That(TestType<char?>    (conn, "charDataType",             DataType.Char),                   Is.EqualTo('1'));
				Assert.That(TestType<string>   (conn, "varcharDataType",          DataType.VarChar),                Is.EqualTo("234"));
				Assert.That(TestType<string>   (conn, "ncharDataType",            DataType.NVarChar),               Is.EqualTo("23233"));
				Assert.That(TestType<string>   (conn, "nvarcharDataType",         DataType.NVarChar),               Is.EqualTo("3323"));
				Assert.That(TestType<string>   (conn, "textDataType",             DataType.Text,  skipPass:true),   Is.EqualTo("567"));
				Assert.That(TestType<string>   (conn, "ntextDataType",            DataType.NText, skipPass:true),   Is.EqualTo("111"));

				Assert.That(TestType<byte[]>   (conn, "binaryDataType",           DataType.Binary),                 Is.EqualTo(new byte[] { 1 }));
				Assert.That(TestType<byte[]>   (conn, "varbinaryDataType",        DataType.VarBinary),              Is.EqualTo(new byte[] { 2 }));
				Assert.That(TestType<byte[]>   (conn, "imageDataType",            DataType.Image, skipPass:true),   Is.EqualTo(new byte[] { 0, 0, 0, 3 }));

				Assert.That(TestType<Guid?>    (conn, "uniqueidentifierDataType", DataType.Guid),                   Is.EqualTo(new Guid("{6F9619FF-8B86-D011-B42D-00C04FC964FF}")));
				Assert.That(TestType<object>   (conn, "sql_variantDataType",      DataType.Variant),                Is.EqualTo(10));

				Assert.That(TestType<string>   (conn, "nvarchar_max_DataType",    DataType.NVarChar),               Is.EqualTo("22322"));
				Assert.That(TestType<string>   (conn, "varchar_max_DataType",     DataType.VarChar),                Is.EqualTo("3333"));
				Assert.That(TestType<byte[]>   (conn, "varbinary_max_DataType",   DataType.VarBinary),              Is.EqualTo(new byte[] { 0, 0, 9, 41 }));

				Assert.That(TestType<string>   (conn, "xmlDataType",              DataType.Xml, skipPass:true),
					Is.EqualTo(context == ProviderName.SqlServer2000 ?
						"<root><element strattr=\"strvalue\" intattr=\"12345\"/></root>" :
						"<root><element strattr=\"strvalue\" intattr=\"12345\" /></root>"));

				Assert.That(conn.Execute<byte[]>("SELECT timestampDataType FROM AllTypes WHERE ID = 1").Length, Is.EqualTo(8));
			}
		}

		[Test]
		public void TestDataTypes2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(TestType<DateTime?>      (conn, "dateDataType",           DataType.Date,           "AllTypes2"), Is.EqualTo(new DateTime(2012, 12, 12)));
				Assert.That(TestType<DateTimeOffset?>(conn, "datetimeoffsetDataType", DataType.DateTimeOffset, "AllTypes2"), Is.EqualTo(new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, new TimeSpan(5, 0, 0))));
				Assert.That(TestType<DateTime?>      (conn, "datetime2DataType",      DataType.DateTime2,      "AllTypes2"), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));
				Assert.That(TestType<TimeSpan?>      (conn, "timeDataType",           DataType.Time,           "AllTypes2"), Is.EqualTo(new TimeSpan(0, 12, 12, 12, 12)));

#if !NETSTANDARD1_6
				Assert.That(TestType<SqlHierarchyId?>(conn, "hierarchyidDataType",              tableName:"AllTypes2"),            Is.EqualTo(SqlHierarchyId.Parse("/1/3/")));
				Assert.That(TestType<SqlGeography>   (conn, "geographyDataType", skipPass:true, tableName:"AllTypes2").ToString(), Is.EqualTo("LINESTRING (-122.36 47.656, -122.343 47.656)"));
				Assert.That(TestType<SqlGeometry>    (conn, "geometryDataType",  skipPass:true, tableName:"AllTypes2").ToString(), Is.EqualTo("LINESTRING (100 100, 20 180, 180 180)"));
#endif
			}
		}

		static void TestNumeric<T>(DataConnection conn, T expectedValue, DataType dataType, string skip = "")
		{
			var skipTypes = skip.Split(' ');

			foreach (var sqlType in new[]
				{
					"bigint",
					"bit",
					"decimal",
					"decimal(38)",
					"int",
					"money",
					"numeric",
					"numeric(38)",
					"smallint",
					"smallmoney",
					"tinyint",

					"float",
					"real"
				}.Except(skipTypes))
			{
				var sqlValue = expectedValue is bool ? (bool)(object)expectedValue? 1 : 0 : (object)expectedValue;

				var sql = string.Format(CultureInfo.InvariantCulture, "SELECT Cast({0} as {1})", sqlValue ?? "NULL", sqlType);

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
		public void TestNumerics([IncludeDataSources(TestProvName.AllSqlServer)] string context)
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

				TestNumeric(conn, sbyte.MinValue,    DataType.SByte,      "bit tinyint");
				TestNumeric(conn, sbyte.MaxValue,    DataType.SByte,      "bit");
				TestNumeric(conn, short.MinValue,    DataType.Int16,      "bit tinyint");
				TestNumeric(conn, short.MaxValue,    DataType.Int16,      "bit tinyint");
				TestNumeric(conn, int.MinValue,      DataType.Int32,      "bit smallint smallmoney tinyint");
				TestNumeric(conn, int.MaxValue,      DataType.Int32,      "bit smallint smallmoney tinyint real");
				TestNumeric(conn, long.MinValue,     DataType.Int64,      "bit decimal int money numeric smallint smallmoney tinyint");
				TestNumeric(conn, long.MaxValue,     DataType.Int64,      "bit decimal int money numeric smallint smallmoney tinyint float real");

				TestNumeric(conn, byte.MaxValue,     DataType.Byte,       "bit");
				TestNumeric(conn, ushort.MaxValue,   DataType.UInt16,     "bit smallint tinyint");
				TestNumeric(conn, uint.MaxValue,     DataType.UInt32,     "bit int smallint smallmoney tinyint real");
				TestNumeric(conn, ulong.MaxValue,    DataType.UInt64,     "bigint bit decimal int money numeric smallint smallmoney tinyint float real");

				TestNumeric(conn, -3.40282306E+38f,  DataType.Single,     "bigint bit decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint");
				TestNumeric(conn,  3.40282306E+38f,  DataType.Single,     "bigint bit decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint");
				TestNumeric(conn, -1.79E+308d,       DataType.Double,     "bigint bit decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint real");
				TestNumeric(conn,  1.79E+308d,       DataType.Double,     "bigint bit decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint real");
				TestNumeric(conn, decimal.MinValue,  DataType.Decimal,    "bigint bit decimal int money numeric smallint smallmoney tinyint float real");
				TestNumeric(conn, decimal.MaxValue,  DataType.Decimal,    "bigint bit decimal int money numeric smallint smallmoney tinyint float real");
				TestNumeric(conn, decimal.MinValue,  DataType.VarNumeric, "bigint bit decimal int money numeric smallint smallmoney tinyint float real");
				TestNumeric(conn, decimal.MaxValue,  DataType.VarNumeric, "bigint bit decimal int money numeric smallint smallmoney tinyint float real");
				TestNumeric(conn, -922337203685477m, DataType.Money,      "bit int smallint smallmoney tinyint real");
				TestNumeric(conn, +922337203685477m, DataType.Money,      "bit int smallint smallmoney tinyint real");
				TestNumeric(conn, -214748m,          DataType.SmallMoney, "bit smallint tinyint");
				TestNumeric(conn, +214748m,          DataType.SmallMoney, "bit smallint tinyint");
			}
		}

		[Test]
		public void TestDate([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12' as date)"),                          Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12' as date)"),                          Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime> ("SELECT @p", DataParameter.Date("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime, DataType.Date)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestSmallDateTime([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 00);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12 12:12:00' as smalldatetime)"),                 Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:00' as smalldatetime)"),                 Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime> ("SELECT @p", DataParameter.SmallDateTime("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime, DataType.SmallDateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestDateTime([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12 12:12:12' as datetime)"),                 Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12' as datetime)"),                 Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime> ("SELECT @p", DataParameter.DateTime("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime)),                    Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestDateTime2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime2 = new DateTime(2012, 12, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12 12:12:12.012' as datetime2)"), Is.EqualTo(dateTime2));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12.012' as datetime2)"), Is.EqualTo(dateTime2));

				Assert.That(conn.Execute<DateTime> ("SELECT @p", DataParameter.DateTime2("p", dateTime2)),               Is.EqualTo(dateTime2));
				Assert.That(conn.Execute<DateTime> ("SELECT @p", DataParameter.Create   ("p", dateTime2)),               Is.EqualTo(dateTime2));
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime2, DataType.DateTime2)), Is.EqualTo(dateTime2));
			}
		}

		[Test]
		public void TestDateTimeOffset([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dto = new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, new TimeSpan( 5, 0, 0));
				var lto = new DateTimeOffset(2012, 12, 12, 13, 12, 12, 12, new TimeSpan(-4, 0, 0));

				Assert.That(conn.Execute<DateTimeOffset>(
					"SELECT Cast('2012-12-12 12:12:12.012' as datetime2)"),
					Is.EqualTo(new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2012, 12, 12, 12, 12, 12)))));

				Assert.That(conn.Execute<DateTimeOffset?>(
					"SELECT Cast('2012-12-12 12:12:12.012' as datetime2)"),
					Is.EqualTo(new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2012, 12, 12, 12, 12, 12)))));

				Assert.That(conn.Execute<DateTime>(
					"SELECT Cast('2012-12-12 13:12:12.012 -04:00' as datetimeoffset)"),
					Is.EqualTo(lto.LocalDateTime));

				Assert.That(conn.Execute<DateTime?>(
					"SELECT Cast('2012-12-12 13:12:12.012 -04:00' as datetimeoffset)"),
					Is.EqualTo(lto.LocalDateTime));

				Assert.That(conn.Execute<DateTimeOffset>(
					"SELECT Cast('2012-12-12 12:12:12.012 +05:00' as datetimeoffset)"),
					Is.EqualTo(dto));

				Assert.That(conn.Execute<DateTimeOffset?>(
					"SELECT Cast('2012-12-12 12:12:12.012 +05:00' as datetimeoffset)"),
					Is.EqualTo(dto));

				Assert.That(conn.Execute<DateTime>(
					"SELECT Cast(NULL as datetimeoffset)"),
					Is.EqualTo(default(DateTime)));

				Assert.That(conn.Execute<DateTime?>(
					"SELECT Cast(NULL as datetimeoffset)"),
					Is.EqualTo(default(DateTime?)));

				Assert.That(conn.Execute<DateTimeOffset> ("SELECT @p", DataParameter.DateTimeOffset("p", dto)),               Is.EqualTo(dto));
				Assert.That(conn.Execute<DateTimeOffset> ("SELECT @p", DataParameter.Create        ("p", dto)),               Is.EqualTo(dto));
				Assert.That(conn.Execute<DateTimeOffset?>("SELECT @p", new DataParameter("p", dto)),                          Is.EqualTo(dto));
				Assert.That(conn.Execute<DateTimeOffset?>("SELECT @p", new DataParameter("p", dto, DataType.DateTimeOffset)), Is.EqualTo(dto));
			}
		}

		[Test]
		public void TestTimeSpan([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var time = new TimeSpan(12, 12, 12);

				Assert.That(conn.Execute<TimeSpan> ("SELECT Cast('12:12:12' as time)"), Is.EqualTo(time));
				Assert.That(conn.Execute<TimeSpan?>("SELECT Cast('12:12:12' as time)"), Is.EqualTo(time));

				Assert.That(conn.Execute<TimeSpan> ("SELECT @p", DataParameter.Time  ("p", time)),              Is.EqualTo(time));
				Assert.That(conn.Execute<TimeSpan> ("SELECT @p", DataParameter.Create("p", time)),              Is.EqualTo(time));
				Assert.That(conn.Execute<TimeSpan?>("SELECT @p", new DataParameter("p",  time, DataType.Time)), Is.EqualTo(time));
				Assert.That(conn.Execute<TimeSpan?>("SELECT @p", new DataParameter("p",  time)),                Is.EqualTo(time));
			}
		}

		[Test]
		public void TestChar([IncludeDataSources(TestProvName.AllSqlServer)] string context)
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
		public void TestString([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char)"),          Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20))"),      Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as char(20))"),      Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar)"),       Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(20))"),   Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as varchar(20))"),   Is.Null);

				var isScCollation = conn.Execute<int>("SELECT COUNT(*) FROM sys.Databases WHERE database_id = DB_ID() AND collation_name LIKE '%_SC'") > 0;
				if (isScCollation)
				{
					// explicit collation set for legacy text types as they doesn't support *_SC collations
					Assert.That(conn.Execute<string>("SELECT Cast('12345' COLLATE Latin1_General_CI_AS as text)"),                Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast(CAST(NULL as nvarchar) COLLATE Latin1_General_CI_AS as text)"), Is.Null);
				}
				else
				{
					Assert.That(conn.Execute<string>("SELECT Cast('12345' as text)"),     Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast(NULL    as text)"),     Is.Null);
				}

				if (context != ProviderName.SqlServer2000)
				{
					Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(max))"),  Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast(NULL    as varchar(max))"),  Is.Null);
				}

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nchar)"),         Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nchar(20))"),     Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as nchar(20))"),     Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nvarchar)"),      Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nvarchar(20))"),  Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as nvarchar(20))"),  Is.Null);

				if (isScCollation)
				{
					// explicit collation set for legacy text types as they doesn't support *_SC collations
					Assert.That(conn.Execute<string>("SELECT Cast('12345' COLLATE Latin1_General_CI_AS as ntext)"),                Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast(CAST(NULL as nvarchar) COLLATE Latin1_General_CI_AS as ntext)"), Is.Null);
				}
				else
				{
					Assert.That(conn.Execute<string>("SELECT Cast('12345' as ntext)"), Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast(NULL    as ntext)"), Is.Null);
				}

				if (context != ProviderName.SqlServer2000)
				{
					Assert.That(conn.Execute<string>("SELECT Cast('12345' as nvarchar(max))"), Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast(NULL    as nvarchar(max))"), Is.Null);
				}

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
		public void TestBinary([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var arr1 = new byte[] {       48, 57 };
			var arr2 = new byte[] { 0, 0, 48, 57 };

			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<byte[]>("SELECT Cast(12345 as binary(2))"),    Is.EqualTo(           arr1));
				Assert.That(conn.Execute<Binary>("SELECT Cast(12345 as binary(4))"),    Is.EqualTo(new Binary(arr2)));

				Assert.That(conn.Execute<byte[]>("SELECT Cast(12345 as varbinary(2))"), Is.EqualTo(           arr1));
				Assert.That(conn.Execute<Binary>("SELECT Cast(12345 as varbinary(4))"), Is.EqualTo(new Binary(arr2)));

				Assert.That(conn.Execute<byte[]>("SELECT Cast(NULL as image)"),         Is.EqualTo(null));

				Assert.That(conn.Execute<byte[]>(
					context == ProviderName.SqlServer2000 ? "SELECT Cast(12345 as varbinary(4000))" : "SELECT Cast(12345 as varbinary(max))"),
					Is.EqualTo(arr2));

				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Binary   ("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create   ("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", null)), Is.EqualTo(null));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as binary(1))", DataParameter.Binary("p", new byte[0])), Is.EqualTo(new byte[] {0}));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Binary   ("p", new byte[0])), Is.EqualTo(new byte[8000]));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", new byte[0])), Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Image    ("p", new byte[0])), Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create   ("p", new Binary(arr1))), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter("p", new Binary(arr1))), Is.EqualTo(arr1));
			}
		}

		[Test]
		public void TestSqlTypes([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var arr = new byte[] { 48, 57 };

				Assert.That(conn.Execute<SqlBinary> ("SELECT Cast(12345    as binary(2))").Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<SqlBoolean>("SELECT Cast(1        as bit)").      Value, Is.EqualTo(true));
				Assert.That(conn.Execute<SqlByte>   ("SELECT Cast(1        as tinyint)").  Value, Is.EqualTo((byte)1));
				Assert.That(conn.Execute<SqlDecimal>("SELECT Cast(1        as decimal)").  Value, Is.EqualTo(1));
				Assert.That(conn.Execute<SqlDouble> ("SELECT Cast(1        as float)").    Value, Is.EqualTo(1.0));
				Assert.That(conn.Execute<SqlInt16>  ("SELECT Cast(1        as smallint)"). Value, Is.EqualTo((short)1));
				Assert.That(conn.Execute<SqlInt32>  ("SELECT Cast(1        as int)").      Value, Is.EqualTo((int)1));
				Assert.That(conn.Execute<SqlInt64>  ("SELECT Cast(1        as bigint)").   Value, Is.EqualTo(1L));
				Assert.That(conn.Execute<SqlMoney>  ("SELECT Cast(1        as money)").    Value, Is.EqualTo(1m));
				Assert.That(conn.Execute<SqlSingle> ("SELECT Cast(1        as real)").     Value, Is.EqualTo((float)1));
				Assert.That(conn.Execute<SqlString> ("SELECT Cast('12345'  as char(6))").  Value, Is.EqualTo("12345 "));

				if (context != ProviderName.SqlServer2000)
					Assert.That(conn.Execute<SqlXml>("SELECT Cast('<xml/>' as xml)").      Value, Is.EqualTo("<xml />"));

				Assert.That(
					conn.Execute<SqlDateTime>("SELECT Cast('2012-12-12 12:12:12' as datetime)").Value,
					Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));

				Assert.That(
					conn.Execute<SqlGuid>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uniqueidentifier)").Value,
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				Assert.That(conn.Execute<SqlBinary> ("SELECT @p", new DataParameter("p", new SqlBinary(arr))).                    Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<SqlBinary> ("SELECT @p", new DataParameter("p", new SqlBinary(arr), DataType.VarBinary)).Value, Is.EqualTo(arr));

				Assert.That(conn.Execute<SqlBoolean>("SELECT @p", new DataParameter("p", true)).                  Value, Is.EqualTo(true));
				Assert.That(conn.Execute<SqlBoolean>("SELECT @p", new DataParameter("p", true, DataType.Boolean)).Value, Is.EqualTo(true));

				if (context != ProviderName.SqlServer2000)
				{
					var conv = conn.MappingSchema.GetConverter<string,SqlXml>();

					Assert.That(conn.Execute<SqlXml>("SELECT @p", new DataParameter("p", conv("<xml/>"))).              Value, Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<SqlXml>("SELECT @p", new DataParameter("p", conv("<xml/>"), DataType.Xml)).Value, Is.EqualTo("<xml />"));
				}
			}
		}

		[Test]
		public void TestGuid([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(
					conn.Execute<Guid>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uniqueidentifier)"),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				Assert.That(
					conn.Execute<Guid?>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uniqueidentifier)"),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				var guid = Guid.NewGuid();

				Assert.That(conn.Execute<Guid>("SELECT @p", DataParameter.Create("p", guid)),                Is.EqualTo(guid));
				Assert.That(conn.Execute<Guid>("SELECT @p", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test]
		public void TestTimestamp([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var arr = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };

				Assert.That(conn.Execute<byte[]>("SELECT Cast(1 as timestamp)"),  Is.EqualTo(arr));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(1 as rowversion)"), Is.EqualTo(arr));

				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Timestamp("p", arr)),               Is.EqualTo(arr));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter("p", arr, DataType.Timestamp)), Is.EqualTo(arr));
			}
		}

		[Test]
		public void TestSqlVariant([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<object>("SELECT Cast(1 as sql_variant)"), Is.EqualTo(1));
				Assert.That(conn.Execute<int>   ("SELECT Cast(1 as sql_variant)"), Is.EqualTo(1));
				Assert.That(conn.Execute<int?>  ("SELECT Cast(1 as sql_variant)"), Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT Cast(1 as sql_variant)"), Is.EqualTo("1"));

				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Variant("p", 1)), Is.EqualTo("1"));
			}
		}

#if !NETSTANDARD1_6
		[Test]
		public void TestHierarchyID([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var id = SqlHierarchyId.Parse("/1/3/");

				Assert.That(conn.Execute<SqlHierarchyId> ("SELECT Cast('/1/3/' as hierarchyid)"),  Is.EqualTo(id));
				Assert.That(conn.Execute<SqlHierarchyId?>("SELECT Cast('/1/3/' as hierarchyid)"),  Is.EqualTo(id));
				Assert.That(conn.Execute<SqlHierarchyId> ("SELECT Cast(NULL as hierarchyid)"),     Is.EqualTo(SqlHierarchyId.Null));
				Assert.That(conn.Execute<SqlHierarchyId?>("SELECT Cast(NULL as hierarchyid)"),     Is.EqualTo(null));

				Assert.That(conn.Execute<SqlHierarchyId>("SELECT @p", new DataParameter("p", id)), Is.EqualTo(id));
			}
		}

		[Test]
		public void TestGeometry([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var id = SqlGeometry.Parse("LINESTRING (100 100, 20 180, 180 180)");

				Assert.That(conn.Execute<SqlGeometry>("SELECT Cast(geometry::STGeomFromText('LINESTRING (100 100, 20 180, 180 180)', 0) as geometry)")
					.ToString(), Is.EqualTo(id.ToString()));

				Assert.That(conn.Execute<SqlGeometry>("SELECT Cast(NULL as geometry)").ToString(),
					Is.EqualTo(SqlGeometry.Null.ToString()));

				Assert.That(conn.Execute<SqlGeometry>("SELECT @p", new DataParameter("p", id)).ToString(),               Is.EqualTo(id.ToString()));
				Assert.That(conn.Execute<SqlGeometry>("SELECT @p", new DataParameter("p", id, DataType.Udt)).ToString(), Is.EqualTo(id.ToString()));
				Assert.That(conn.Execute<SqlGeometry>("SELECT @p", DataParameter.Udt("p", id)).ToString(),               Is.EqualTo(id.ToString()));
			}
		}

		[Test]
		public void TestGeography([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var id = SqlGeography.Parse("LINESTRING (-122.36 47.656, -122.343 47.656)");

				Assert.That(conn.Execute<SqlGeography>("SELECT Cast(geography::STGeomFromText('LINESTRING(-122.360 47.656, -122.343 47.656)', 4326) as geography)")
					.ToString(), Is.EqualTo(id.ToString()));

				Assert.That(conn.Execute<SqlGeography>("SELECT Cast(NULL as geography)").ToString(),
					Is.EqualTo(SqlGeography.Null.ToString()));

				Assert.That(conn.Execute<SqlGeography>("SELECT @p", new DataParameter("p", id)).ToString(),               Is.EqualTo(id.ToString()));
				Assert.That(conn.Execute<SqlGeography>("SELECT @p", new DataParameter("p", id, DataType.Udt)).ToString(), Is.EqualTo(id.ToString()));
				Assert.That(conn.Execute<SqlGeography>("SELECT @p", DataParameter.Udt("p", id)).ToString(),               Is.EqualTo(id.ToString()));
			}
		}
#endif

		[Test]
		public void TestXml([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				if (context != ProviderName.SqlServer2000)
				{
					Assert.That(conn.Execute<string>     ("SELECT Cast('<xml/>' as xml)"),            Is.EqualTo("<xml/>").Or.EqualTo("<xml />"));
					Assert.That(conn.Execute<XDocument>  ("SELECT Cast('<xml/>' as xml)").ToString(), Is.EqualTo("<xml/>").Or.EqualTo("<xml />"));
					Assert.That(conn.Execute<XmlDocument>("SELECT Cast('<xml/>' as xml)").InnerXml,   Is.EqualTo("<xml/>").Or.EqualTo("<xml />"));
				}

				var xdoc = XDocument.Parse("<xml/>");
				var xml  = Convert<string,XmlDocument>.Lambda("<xml/>");

				Assert.That(conn.Execute<string>     ("SELECT @p", DataParameter.Xml("p", "<xml/>")),        Is.EqualTo("<xml/>").Or.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  ("SELECT @p", DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml/>").Or.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT @p", DataParameter.Xml("p", xml)). InnerXml,   Is.EqualTo("<xml/>").Or.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  ("SELECT @p", new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml/>").Or.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  ("SELECT @p", new DataParameter("p", xml)). ToString(), Is.EqualTo("<xml/>").Or.EqualTo("<xml />"));
			}
		}

		enum TestEnum
		{
			[MapValue("A")] AA,
			[MapValue(ProviderName.SqlServer2008, "C")]
			[MapValue("B")] BB,
		}

		[Test]
		public void TestEnum1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<TestEnum> ("SELECT 'A'"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'A'"), Is.EqualTo(TestEnum.AA));

				var sql = context == ProviderName.SqlServer2008 ? "SELECT 'C'" : "SELECT 'B'";

				Assert.That(conn.Execute<TestEnum> (sql), Is.EqualTo(TestEnum.BB));
				Assert.That(conn.Execute<TestEnum?>(sql), Is.EqualTo(TestEnum.BB));
			}
		}

		[Test]
		public void TestEnum2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT @p", new { p = TestEnum.AA }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = (TestEnum?)TestEnum.BB }),
					Is.EqualTo(context == ProviderName.SqlServer2008 ? "C" : "B"));

				Assert.That(conn.Execute<string>("SELECT @p", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = conn.MappingSchema.GetConverter<TestEnum?,string>()(TestEnum.AA) }), Is.EqualTo("A"));
			}
		}

		[Table(Schema = "dbo", Name = "LinqDataTypes")]
		class DataTypes
		{
			[Column] public int      ID;
			[Column] public decimal  MoneyValue;
			[Column] public DateTime DateTimeValue;
			[Column] public bool     BoolValue;
			[Column] public Guid     GuidValue;
			[Column] public Binary   BinaryValue;
			[Column] public short    SmallIntValue;
		}

		[Test]
		public void BulkCopyLinqTypesMultipleRows([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = new DataConnection(context))
			{
				db.BulkCopy(
					new BulkCopyOptions
					{
						BulkCopyType       = BulkCopyType.MultipleRows,
						RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied)
					},
					Enumerable.Range(0, 10).Select(n =>
						new DataTypes
						{
							ID            = 4000 + n,
							MoneyValue    = 1000m + n,
							DateTimeValue = new DateTime(2001,  1,  11,  1, 11, 21, 100),
							BoolValue     = true,
							GuidValue     = Guid.NewGuid(),
							SmallIntValue = (short)n
						}
					));

				db.GetTable<DataTypes>().Delete(p => p.ID >= 4000);
			}
		}

		[Test]
		public void BulkCopyLinqTypesProviderSpecific([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = new DataConnection(context))
			{
				db.BulkCopy(
					new BulkCopyOptions
					{
						BulkCopyType       = BulkCopyType.ProviderSpecific,
						RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied)
					},
					Enumerable.Range(0, 10).Select(n =>
						new DataTypes
						{
							ID            = 4000 + n,
							MoneyValue    = 1000m + n,
							DateTimeValue = new DateTime(2001,  1,  11,  1, 11, 21, 100),
							BoolValue     = true,
							GuidValue     = Guid.NewGuid(),
							SmallIntValue = (short)n
						}
					));

				db.GetTable<DataTypes>().Delete(p => p.ID >= 4000);
			}
		}

		[Table]
		class AllTypes
		{
			[Identity]
			[Column(DataType=DataType.Int32),                          NotNull]  public int             ID                       { get; set; }
			[Column(DataType=DataType.Int64),                          Nullable] public long?           bigintDataType           { get; set; }
			[Column(DataType=DataType.Decimal),                        Nullable] public decimal?        numericDataType          { get; set; }
			[Column(DataType=DataType.Boolean),                        Nullable] public bool?           bitDataType              { get; set; }
			[Column(DataType=DataType.Int16),                          Nullable] public short?          smallintDataType         { get; set; }
			[Column(DataType=DataType.Decimal),                        Nullable] public decimal?        decimalDataType          { get; set; }
			[Column(DataType=DataType.SmallMoney),                     Nullable] public decimal?        smallmoneyDataType       { get; set; }
			[Column(DataType=DataType.Int32),                          Nullable] public int?            intDataType              { get; set; }
			[Column(DataType=DataType.Byte),                           Nullable] public byte?           tinyintDataType          { get; set; }
			[Column(DataType=DataType.Money),                          Nullable] public decimal?        moneyDataType            { get; set; }
			[Column(DataType=DataType.Double),                         Nullable] public double?         floatDataType            { get; set; }
			[Column(DataType=DataType.Single),                         Nullable] public float?          realDataType             { get; set; }
			[Column(DataType=DataType.DateTime),                       Nullable] public DateTime?       datetimeDataType         { get; set; }
			[Column(DataType=DataType.SmallDateTime),                  Nullable] public DateTime?       smalldatetimeDataType    { get; set; }
			[Column(DataType=DataType.Char,      Length=1),            Nullable] public char?           charDataType             { get; set; }
			[Column(DataType=DataType.VarChar,   Length=20),           Nullable] public string          varcharDataType          { get; set; }
			[Column(DataType=DataType.Text),                           Nullable] public string          textDataType             { get; set; }
			[Column(DataType=DataType.NChar,     Length=20),           Nullable] public string          ncharDataType            { get; set; }
			[Column(DataType=DataType.NVarChar,  Length=20),           Nullable] public string          nvarcharDataType         { get; set; }
			[Column(DataType=DataType.NText),                          Nullable] public string          ntextDataType            { get; set; }
			[Column(DataType=DataType.Binary),                         Nullable] public byte[]          binaryDataType           { get; set; }
			[Column(DataType=DataType.VarBinary),                      Nullable] public byte[]          varbinaryDataType        { get; set; }
			[Column(DataType=DataType.Image),                          Nullable] public byte[]          imageDataType            { get; set; }
			[Column(DataType=DataType.Timestamp,SkipOnInsert=true),    Nullable] public byte[]          timestampDataType        { get; set; }
			[Column(DataType=DataType.Guid),                           Nullable] public Guid?           uniqueidentifierDataType { get; set; }
			[Column(DataType=DataType.Variant),                        Nullable] public object          sql_variantDataType      { get; set; }
			[Column(DataType=DataType.NVarChar,  Length=int.MaxValue), Nullable] public string          nvarchar_max_DataType    { get; set; }
			[Column(DataType=DataType.VarChar,   Length=int.MaxValue), Nullable] public string          varchar_max_DataType     { get; set; }
			[Column(DataType=DataType.VarBinary, Length=int.MaxValue), Nullable] public byte[]          varbinary_max_DataType   { get; set; }
			[Column(DataType=DataType.Xml),                            Nullable] public string          xmlDataType              { get; set; }
			[Column(Configuration=ProviderName.SqlServer2000, DataType=DataType.VarChar)]
			[Column(Configuration=ProviderName.SqlServer2005, DataType=DataType.VarChar)]
			[Column(DataType=DataType.DateTime2),                      Nullable] public DateTime?       datetime2DataType        { get; set; }
			[Column(Configuration=ProviderName.SqlServer2000, DataType=DataType.VarChar)]
			[Column(Configuration=ProviderName.SqlServer2005, DataType=DataType.VarChar)]
			[Column(DataType=DataType.DateTimeOffset),                 Nullable] public DateTimeOffset? datetimeoffsetDataType   { get; set; }
			[Column(Configuration=ProviderName.SqlServer2000, DataType=DataType.VarChar)]
			[Column(Configuration=ProviderName.SqlServer2005, DataType=DataType.VarChar)]
			[Column(DataType=DataType.DateTimeOffset,Scale=0),         Nullable] public DateTimeOffset? datetimeoffset0DataType   { get; set; }
			[Column(Configuration=ProviderName.SqlServer2000, DataType=DataType.VarChar)]
			[Column(Configuration=ProviderName.SqlServer2005, DataType=DataType.VarChar)]
			[Column(DataType=DataType.DateTimeOffset,Scale=1),         Nullable] public DateTimeOffset? datetimeoffset1DataType   { get; set; }
			[Column(Configuration=ProviderName.SqlServer2000, DataType=DataType.VarChar)]
			[Column(Configuration=ProviderName.SqlServer2005, DataType=DataType.VarChar)]
			[Column(DataType=DataType.DateTimeOffset,Scale=2),         Nullable] public DateTimeOffset? datetimeoffset2DataType   { get; set; }
			[Column(Configuration=ProviderName.SqlServer2000, DataType=DataType.VarChar)]
			[Column(Configuration=ProviderName.SqlServer2005, DataType=DataType.VarChar)]
			[Column(DataType=DataType.DateTimeOffset,Scale=3),         Nullable] public DateTimeOffset? datetimeoffset3DataType   { get; set; }
			[Column(Configuration=ProviderName.SqlServer2000, DataType=DataType.VarChar)]
			[Column(Configuration=ProviderName.SqlServer2005, DataType=DataType.VarChar)]
			[Column(DataType=DataType.DateTimeOffset,Scale=4),         Nullable] public DateTimeOffset? datetimeoffset4DataType   { get; set; }
			[Column(Configuration=ProviderName.SqlServer2000, DataType=DataType.VarChar)]
			[Column(Configuration=ProviderName.SqlServer2005, DataType=DataType.VarChar)]
			[Column(DataType=DataType.DateTimeOffset,Scale=5),         Nullable] public DateTimeOffset? datetimeoffset5DataType   { get; set; }
			[Column(Configuration=ProviderName.SqlServer2000, DataType=DataType.VarChar)]
			[Column(Configuration=ProviderName.SqlServer2005, DataType=DataType.VarChar)]
			[Column(DataType=DataType.DateTimeOffset,Scale=6),         Nullable] public DateTimeOffset? datetimeoffset6DataType   { get; set; }
			[Column(Configuration=ProviderName.SqlServer2000, DataType=DataType.VarChar)]
			[Column(Configuration=ProviderName.SqlServer2005, DataType=DataType.VarChar)]
			[Column(DataType=DataType.DateTimeOffset,Scale=7),         Nullable] public DateTimeOffset? datetimeoffset7DataType   { get; set; }
			[Column(Configuration=ProviderName.SqlServer2000, DataType=DataType.VarChar)]
			[Column(Configuration=ProviderName.SqlServer2005, DataType=DataType.VarChar)]
			[Column(DataType=DataType.Date),                           Nullable] public DateTime?       dateDataType             { get; set; }
			[Column(Configuration=ProviderName.SqlServer2000, DataType=DataType.VarChar)]
			[Column(Configuration=ProviderName.SqlServer2005, DataType=DataType.VarChar)]
			[Column(DataType=DataType.Time),                           Nullable] public TimeSpan?       timeDataType             { get; set; }
		}

		static readonly AllTypes[] _allTypeses =
		{
#region data
			new AllTypes
			{
				ID                       = 700,
				bigintDataType           = 1,
				numericDataType          = 1.1m,
				bitDataType              = true,
				smallintDataType         = 1,
				decimalDataType          = 1.1m,
				smallmoneyDataType       = 1.1m,
				intDataType              = 1,
				tinyintDataType          = 1,
				moneyDataType            = 1.1m,
				floatDataType            = 1.1d,
				realDataType             = 1.1f,
				datetimeDataType         = new DateTime(2014, 12, 17, 21, 2, 58, 123),
				smalldatetimeDataType    = new DateTime(2014, 12, 17, 21, 3, 0),
				charDataType             = 'E',
				varcharDataType          = "E",
				textDataType             = "E",
				ncharDataType            = "Ё",
				nvarcharDataType         = "Ё",
				ntextDataType            = "Ё",
				binaryDataType           = new byte[] { 1 },
				varbinaryDataType        = new byte[] { 1 },
				imageDataType            = new byte[] { 1, 2, 3, 4, 5 },
				uniqueidentifierDataType = new Guid(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
				sql_variantDataType      = "1",
				nvarchar_max_DataType    = "1",
				varchar_max_DataType     = "1",
				varbinary_max_DataType   = new byte[] { 1, 2, 3, 4, 50 },
				xmlDataType              = "<xml />",
				datetime2DataType        = new DateTime(2014, 12, 17, 21, 2, 58, 123),
				datetimeoffsetDataType   = new DateTimeOffset(2014, 12, 17, 21, 2, 58, 123, new TimeSpan(5, 0, 0)),
				datetimeoffset0DataType  = new DateTimeOffset(2014, 12, 17, 21, 2, 58,   0, new TimeSpan(5, 0, 0)),
				datetimeoffset1DataType  = new DateTimeOffset(2014, 12, 17, 21, 2, 58, 100, new TimeSpan(5, 0, 0)),
				datetimeoffset2DataType  = new DateTimeOffset(2014, 12, 17, 21, 2, 58, 120, new TimeSpan(5, 0, 0)),
				datetimeoffset3DataType  = new DateTimeOffset(2014, 12, 17, 21, 2, 58, 123, new TimeSpan(5, 0, 0)),
				datetimeoffset4DataType  = new DateTimeOffset(2014, 12, 17, 21, 2, 58, 123, new TimeSpan(5, 0, 0)),
				datetimeoffset5DataType  = new DateTimeOffset(2014, 12, 17, 21, 2, 58, 123, new TimeSpan(5, 0, 0)),
				datetimeoffset6DataType  = new DateTimeOffset(2014, 12, 17, 21, 2, 58, 123, new TimeSpan(5, 0, 0)),
				datetimeoffset7DataType  = new DateTimeOffset(2014, 12, 17, 21, 2, 58, 123, new TimeSpan(5, 0, 0)),
				dateDataType             = new DateTime(2014, 12, 17),
				timeDataType             = new TimeSpan(0, 10, 11, 12, 567),
			},
			new AllTypes
			{
				ID                       = 701,
			},
#endregion
		};

		void BulkCopyAllTypes(string context, BulkCopyType bulkCopyType)
		{
			using (var db = new DataConnection(context))
			{
				db.CommandTimeout = 60;

				db.GetTable<AllTypes>().Delete(p => p.ID >= _allTypeses[0].ID);

				db.BulkCopy(
					new BulkCopyOptions
					{
						BulkCopyType       = bulkCopyType,
						RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied),
						KeepIdentity       = true,
					},
					_allTypeses);

				var ids = _allTypeses.Select(at => at.ID).ToArray();

				var list = db.GetTable<AllTypes>().Where(t => ids.Contains(t.ID)).OrderBy(t => t.ID).ToList();

				db.GetTable<AllTypes>().Delete(p => p.ID >= _allTypeses[0].ID);

				Assert.That(list.Count, Is.EqualTo(_allTypeses.Length));

				for (var i = 0; i < list.Count; i++)
					CompareObject(db.MappingSchema, list[i], _allTypeses[i]);
			}
		}

		[Test]
		public void BulkCopyAllTypesMultipleRows([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			BulkCopyAllTypes(context, BulkCopyType.MultipleRows);
		}

		[Test]
		public void BulkCopyAllTypesProviderSpecific([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			BulkCopyAllTypes(context, BulkCopyType.ProviderSpecific);
		}

		void CompareObject<T>(MappingSchema mappingSchema, T actual, T test)
		{
			var ed = mappingSchema.GetEntityDescriptor(typeof(T));

			foreach (var column in ed.Columns)
			{
				var actualValue = column.GetValue(mappingSchema, actual);
				var testValue   = column.GetValue(mappingSchema, test);

				// timestampDataType autogenerated
				if (column.MemberName == "timestampDataType")
					continue;

#if !NETSTANDARD1_6
				if (actualValue is SqlGeometry)
				{
					Assert.That(actualValue == null  || ((SqlGeometry) actualValue).IsNull ? null : actualValue.ToString(),
						Is.EqualTo(testValue == null || ((SqlGeometry) testValue).IsNull   ? null : testValue.ToString()),
						"Column  : {0}", column.MemberName);
				}
				else if (actualValue is SqlGeography)
				{
					Assert.That(actualValue == null  || ((SqlGeography) actualValue).IsNull ? null : actualValue.ToString(),
						Is.EqualTo(testValue == null || ((SqlGeography) testValue).IsNull   ? null : testValue.ToString()),
						"Column  : {0}", column.MemberName);
				}
				else
#endif
					Assert.That(actualValue, Is.EqualTo(testValue),
						actualValue is DateTimeOffset
							? "Column  : {0} {1:yyyy-MM-dd HH:mm:ss.fffffff zzz} {2:yyyy-MM-dd HH:mm:ss.fffffff zzz}"
							: "Column  : {0}",
						column.MemberName,
						actualValue,
						testValue);
			}
		}

		[Table(Name="AllTypes2")]
		class AllTypes2
		{
			[Column(DbType="int"),   PrimaryKey, Identity] public int             ID                     { get; set; } // int
			[Column(DbType="date"),              Nullable] public DateTime?       dateDataType           { get; set; } // date
			[Column(DbType="datetimeoffset(7)"), Nullable] public DateTimeOffset? datetimeoffsetDataType { get; set; } // datetimeoffset(7)
			[Column(DbType="datetime2(7)"),      Nullable] public DateTime?       datetime2DataType      { get; set; } // datetime2(7)
			[Column(DbType="time(7)"),           Nullable] public TimeSpan?       timeDataType           { get; set; } // time(7)
#if !NETSTANDARD1_6
			[Column(DbType="hierarchyid"),       Nullable] public SqlHierarchyId  hierarchyidDataType    { get; set; } // hierarchyid
			[Column(DbType="geography"),         Nullable] public SqlGeography    geographyDataType      { get; set; } // geography
			[Column(DbType="geometry"),          Nullable] public SqlGeometry     geometryDataType       { get; set; } // geometry
#endif
		}

		IEnumerable<AllTypes2> GenerateAllTypes2(int startId, int count)
		{
			for (int i = 0; i < count; i++)
			{
				yield return new AllTypes2
				{
					ID                     = startId + i,
					dateDataType           = DateTime.Today.AddDays(i),
					datetimeoffsetDataType = DateTime.Now.AddMinutes(i),
					datetime2DataType      = DateTime.Today.AddDays(i),
					timeDataType           = TimeSpan.FromSeconds(i),
#if !NETSTANDARD1_6
					hierarchyidDataType    = SqlHierarchyId.Parse("/1/3/"),
					geographyDataType      = SqlGeography.Parse("LINESTRING (-122.36 47.656, -122.343 47.656)"),
					geometryDataType       = SqlGeometry.Parse("LINESTRING (100 100, 20 180, 180 180)"),
#endif
				};
			}
		}

		void BulkCopyAllTypes2(string context, BulkCopyType bulkCopyType)
		{
			using (var db = new DataConnection(context))
			{
				db.CommandTimeout = 60;

				db.GetTable<AllTypes2>().Delete(p => p.ID >= 3);

				var allTypes2 = GenerateAllTypes2(3, 10).ToArray();
				db.BulkCopy(
					new BulkCopyOptions
					{
						BulkCopyType       = bulkCopyType,
						RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied),
						KeepIdentity       = true,
					},
					allTypes2);

				var loaded = db.GetTable<AllTypes2>().Where(p => p.ID >= 3).OrderBy(p=> p.ID).ToArray();

				Assert.That(loaded.Count, Is.EqualTo(allTypes2.Length));

				for (var i = 0; i < loaded.Length; i++)
					CompareObject(db.MappingSchema, loaded[i], allTypes2[i]);
			}
		}

#if !NETSTANDARD1_6
		[Test]
		public void BulkCopyAllTypes2MultipleRows([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			BulkCopyAllTypes2(context, BulkCopyType.MultipleRows);
		}

		[Test]
		public void BulkCopyAllTypes2ProviderSpecific([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			BulkCopyAllTypes2(context, BulkCopyType.ProviderSpecific);
		}
#endif

		[Test]
		public void CreateAllTypes([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var ms = new MappingSchema();

				db.AddMappingSchema(ms);

				ms.GetFluentMappingBuilder()
					.Entity<AllTypes>()
						.HasTableName("AllTypeCreateTest");

				try
				{
					db.DropTable<AllTypes>();
				}
				catch
				{
				}

				var table = db.CreateTable<AllTypes>();

				var list = table.ToList();

				db.DropTable<AllTypes>();
			}
		}

#if !NETSTANDARD1_6
		[Test]
		public void CreateAllTypes2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var ms = new MappingSchema();

				db.AddMappingSchema(ms);

				ms.GetFluentMappingBuilder()
					.Entity<AllTypes2>()
					.HasTableName("AllType2CreateTest");

				try
				{
					db.DropTable<AllTypes2>();
				}
				catch
				{
				}

				var table = db.CreateTable<AllTypes2>();
				var list = table.ToList();

				db.DropTable<AllTypes2>();
			}
		}
#endif

		[Table("#TempTable")]
		class TempTable
		{
			[PrimaryKey] public int ID;
		}

		[Test]
		public void CreateTempTable([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = new DataConnection(context))
			{
				db.CreateTable<TempTable>();
				db.DropTable<TempTable>();
				db.CreateTable<TempTable>();
			}
		}

		[Test]
		public void CreateTempTable2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db1 = new DataConnection(context))
			using (var db2 = new DataConnection(context))
			{
				db1.CreateTable<TempTable>();
				db2.CreateTable<TempTable>();
			}
		}

		[Table("DecimalOverflow")]
		class DecimalOverflow
		{
			[Column] public decimal Decimal1;
			[Column] public decimal Decimal2;
			[Column] public decimal Decimal3;
		}

		[Test]
		public void OverflowTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var func = SqlServerTools.DataReaderGetDecimal;
			try
			{
				SqlServerTools.DataReaderGetDecimal = GetDecimal;

				using (var db = new DataConnection(context))
				{
					var list = db.GetTable<DecimalOverflow>().ToList();
				}
			}
			finally
			{
				SqlServerTools.DataReaderGetDecimal = func;
			}
		}

		const int ClrPrecision = 29;

		static decimal GetDecimal(IDataReader rd, int idx)
		{
			try
			{
				var value = ((SqlDataReader)rd).GetSqlDecimal(idx);

				if (value.Precision > ClrPrecision)
				{
					var str = value.ToString();
					var val = decimal.Parse(str, CultureInfo.InvariantCulture);

					return val;
				}

				return value.Value;
			}
			catch (Exception)
			{
				var vvv=  rd.GetValue(idx);

				throw;
			}
		}

		[Table("DecimalOverflow")]
		class DecimalOverflow2
		{
			[Column] public SqlDecimal Decimal1;
			[Column] public SqlDecimal Decimal2;
			[Column] public SqlDecimal Decimal3;
		}

		[Test]
		public void OverflowTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var func = SqlServerTools.DataReaderGetDecimal;

			try
			{
				SqlServerTools.DataReaderGetDecimal = (rd, idx) => { throw new Exception(); };

				using (var db = new DataConnection(context))
				{
					var list = db.GetTable<DecimalOverflow2>().ToList();
				}
			}
			finally
			{
				SqlServerTools.DataReaderGetDecimal = func;
			}
		}

		[Test]
		public void SelectTableWithHintTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(Person, db.Person.With("TABLOCK"));
			}
		}

		[Test]
		public void UpdateTableWithHintTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				Assert.AreEqual(Person.Count(), db.Person.Set(_ => _.FirstName, _ => _.FirstName).Update());
				Assert.AreEqual(Person.Count(), db.Person.With("TABLOCK").Set(_ => _.FirstName, _ => _.FirstName).Update());
			}
		}

		[Test]
		public void InOutProcedureTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			{
				var dbName            = TestUtils.GetDatabaseName(db);
				var    inputID        = 1234;
				var    inputStr       = "InputStr";
				int?   outputID       = 5678;
				int?   inputOutputID  = 9012;
				string outputStr      = "OuputStr";
				string inputOutputStr = "InputOutputStr";

				var parameters = new []
				{
					new DataParameter("@ID",             inputID,        DataType.Int32),
					new DataParameter("@outputID",       outputID,       DataType.Int32)   { Direction = ParameterDirection.InputOutput },
					new DataParameter("@inputOutputID",  inputOutputID,  DataType.Int32)   { Direction = ParameterDirection.InputOutput },
					new DataParameter("@str",            inputStr,       DataType.VarChar),
					new DataParameter("@outputStr",      outputStr,      DataType.VarChar) { Direction = ParameterDirection.InputOutput, Size = 50 },
					new DataParameter("@inputOutputStr", inputOutputStr, DataType.VarChar) { Direction = ParameterDirection.InputOutput, Size = 50 }
				};

				var ret = db.ExecuteProc($"[{dbName}]..[OutRefTest]", parameters);

				outputID       = Converter.ChangeTypeTo<int?>  (parameters[1].Value);
				inputOutputID  = Converter.ChangeTypeTo<int?>  (parameters[2].Value);
				outputStr      = Converter.ChangeTypeTo<string>(parameters[4].Value);
				inputOutputStr = Converter.ChangeTypeTo<string>(parameters[5].Value);

				Assert.That(outputID,       Is.EqualTo(inputID));
				Assert.That(inputOutputID,  Is.EqualTo(9012 + inputID));
				Assert.That(outputStr,      Is.EqualTo(inputStr));
				Assert.That(inputOutputStr, Is.EqualTo(inputStr + "InputOutputStr"));
			}
		}

		[Test]
		public async Task InOutProcedureTestAsync([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			{
				var dbName            = TestUtils.GetDatabaseName(db);
				var    inputID        = 1234;
				var    inputStr       = "InputStr";
				int?   outputID       = 5678;
				int?   inputOutputID  = 9012;
				string outputStr      = "OuputStr";
				string inputOutputStr = "InputOutputStr";

				var parameters = new []
				{
					new DataParameter("@ID",             inputID,        DataType.Int32),
					new DataParameter("@outputID",       outputID,       DataType.Int32)   { Direction = ParameterDirection.InputOutput },
					new DataParameter("@inputOutputID",  inputOutputID,  DataType.Int32)   { Direction = ParameterDirection.InputOutput },
					new DataParameter("@str",            inputStr,       DataType.VarChar),
					new DataParameter("@outputStr",      outputStr,      DataType.VarChar) { Direction = ParameterDirection.InputOutput, Size = 50 },
					new DataParameter("@inputOutputStr", inputOutputStr, DataType.VarChar) { Direction = ParameterDirection.InputOutput, Size = 50 }
				};

				var ret = await db.ExecuteProcAsync($"[{dbName}]..[OutRefTest]", parameters);

				outputID       = Converter.ChangeTypeTo<int?>  (parameters[1].Value);
				inputOutputID  = Converter.ChangeTypeTo<int?>  (parameters[2].Value);
				outputStr      = Converter.ChangeTypeTo<string>(parameters[4].Value);
				inputOutputStr = Converter.ChangeTypeTo<string>(parameters[5].Value);

				Assert.That(outputID,       Is.EqualTo(inputID));
				Assert.That(inputOutputID,  Is.EqualTo(9012 + inputID));
				Assert.That(outputStr,      Is.EqualTo(inputStr));
				Assert.That(inputOutputStr, Is.EqualTo(inputStr + "InputOutputStr"));
			}
		}

#if !NETSTANDARD1_6
		[Test]
		public void TestIssue1144([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			{
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, TestUtils.GetDefaultSchemaOptions(context));

				var table = schema.Tables.Where(_ => _.TableName == "Issue1144").Single();

				Assert.AreEqual(1, table.Columns.Count);
			}
		}
#endif
		[Table("Issue1613")]
		private class Issue1613Table
		{
			[Column("dt"), Nullable] 
			public DateTimeOffset? DateTimeOffset { get; set; }
		}

		private static Issue1613Table[] GenerateData()
		{
			var sampleData = new[]
			{
				new Issue1613Table { DateTimeOffset = null },
				new Issue1613Table { DateTimeOffset = DateTimeOffset.Now.AddDays(1) },
				new Issue1613Table { DateTimeOffset = DateTimeOffset.Now.AddDays(2) },
				new Issue1613Table { DateTimeOffset = DateTimeOffset.Now.AddDays(3) },
				new Issue1613Table { DateTimeOffset = DateTimeOffset.Now.AddDays(4) }
			};
			return sampleData;
		}

		[Test]
		public void Issue1613Test1([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateData()))
			{ 

				var query1 = table.GroupBy(x => x.DateTimeOffset).Select(g => g.Key).ToList();
				var query2 = table.Select(r => r.DateTimeOffset).ToList();

				Assert.AreEqual(5, query1.Count);
				Assert.AreEqual(5, query2.Count);
				Assert.AreEqual(query1, query2);
			}
		}
		
		[Test, ActiveIssue(1666)]
		public void Issue1613Test2([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateData()))
			{ 

				var query1 = table.GroupBy(x => x.DateTimeOffset.Value.Date).Select(g => g.Key).ToList();
				var query2 = table.Select(r => r.DateTimeOffset.Value.Date).ToList();

				Assert.AreEqual(5, query1.Count);
				Assert.AreEqual(5, query2.Count);
				Assert.AreEqual(query1, query2);
			}
		}

		[Test, ActiveIssue(1666)]
		public void Issue1613Test3([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateData()))
			{ 

				var query1 = table.GroupBy(x => x.DateTimeOffset.Value.TimeOfDay).Select(g => g.Key).ToList();
				var query2 = table.Select(r => r.DateTimeOffset.Value.TimeOfDay).ToList();

				Assert.AreEqual(5, query1.Count);
				Assert.AreEqual(5, query2.Count);
				Assert.AreEqual(query1, query2);
			}
		}
	}
}
