﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Data.RetryPolicy;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Interceptors;
using LinqToDB.Linq;
using LinqToDB.Linq.Internal;
using LinqToDB.SchemaProvider;
using LinqToDB.Tools;
using LinqToDB.Mapping;

using NUnit.Framework;

using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

using DA = Devart.Data.Oracle;

namespace Tests.DataProvider
{
	using Model;

	[TestFixture]
	public class OracleTests : TestBase
	{
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
		public void TestParameters([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
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

		static void TestType<T>(
			DataConnection   connection,
			string           dataTypeName,
			T                value,
			string           tableName       = "\"AllTypes\"",
			bool             convertToString = false,
			bool             throwException  = false)
			where T : notnull
		{
			Assert.That(connection.Execute<T>($"SELECT {dataTypeName} FROM {tableName} WHERE ID = 1"),
				Is.EqualTo(connection.MappingSchema.GetDefaultValue(typeof(T))));

			object actualValue   = connection.Execute<T>($"SELECT {dataTypeName} FROM {tableName} WHERE ID = 2")!;
			object expectedValue = value;

			if (convertToString)
			{
				actualValue   = actualValue.  ToString()!;
				expectedValue = expectedValue.ToString()!;
			}

			// fix version-specific formatting
			if (dataTypeName.Contains("xmlDataType") && actualValue is string strVal)
				actualValue = strVal.Replace("\n", string.Empty).Replace(">  <", "><");

			if (throwException)
			{
				if (!EqualityComparer<T>.Default.Equals((T)actualValue, (T)expectedValue))
					throw new AssertionException($"Expected: {expectedValue} But was: {actualValue}");
			}
			else
			{
				Assert.That(actualValue, Is.EqualTo(expectedValue));
			}
		}

		/* If this test fails for you with

		 "ORA-22288: file or LOB operation FILEOPEN failed
		 The system cannot find the path specified."

			Copy file Data\Oracle\bfile.txt to C:\DataFiles on machine with oracle server
			(of course only if it is Windows machine)

		*/
		[ActiveIssue(Configuration = TestProvName.Oracle21DevartDirect)]
		[Test]
		public void TestDataTypes([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				TestType(conn, "\"bigintDataType\"",         1000000L);
				TestType(conn, "\"numericDataType\"",        9999999m);
				TestType(conn, "\"bitDataType\"",            true);
				TestType(conn, "\"smallintDataType\"",       (short)25555);
				TestType(conn, "\"decimalDataType\"",        2222222m);
				TestType(conn, "\"smallmoneyDataType\"",     100000m);
				TestType(conn, "\"intDataType\"",            7777777);
				TestType(conn, "\"tinyintDataType\"",        (sbyte)100);
				TestType(conn, "\"moneyDataType\"",          100000m);
				TestType(conn, "\"floatDataType\"",          20.31d);
				TestType(conn, "\"realDataType\"",           16.2f);

				TestType(conn, "\"datetimeDataType\"",       new DateTime(2012, 12, 12, 12, 12, 12));
				TestType(conn, "\"datetime2DataType\"",      new DateTime(2012, 12, 12, 12, 12, 12, 012));
				TestType(conn, "\"datetimeoffsetDataType\"", new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, new TimeSpan(-5, 0, 0)));

				var dt = new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, TimeSpan.Zero);
				TestType(conn, "\"localZoneDataType\"", new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, TimeZoneInfo.Local.GetUtcOffset(dt) /* new TimeSpan(-4, 0, 0)*/), throwException:true);

				TestType(conn, "\"charDataType\"",           '1');
				TestType(conn, "\"varcharDataType\"",        "234");
				TestType(conn, "\"textDataType\"",           "567");
				TestType(conn, "\"ncharDataType\"",          "23233");
				TestType(conn, "\"nvarcharDataType\"",       "3323");
				TestType(conn, "\"ntextDataType\"",          "111");

				TestType(conn, "\"binaryDataType\"",         new byte[] { 0, 170 });
				// TODO: configure test file in docker image
				TestType(conn, "\"bfileDataType\"",          new byte[] { 49, 50, 51, 52, 53 });

				var res = "<root><element strattr=\"strvalue\" intattr=\"12345\"/></root>";

				TestType(conn, "XMLSERIALIZE(DOCUMENT \"xmlDataType\" AS CLOB NO INDENT)", res);
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
				var sqlValue = expectedValue is bool ? (bool)(object)expectedValue? 1 : 0 : (object?)expectedValue;

				var sql = string.Format(CultureInfo.InvariantCulture, "SELECT Cast({0} as {1}) FROM sys.dual", sqlValue ?? "NULL", sqlType);

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
		public void TestNumerics([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
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
				TestNumeric(conn, +3.4E+28f,         DataType.Single,     "number number(10,0) number(20,0)");
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
		public void TestDate([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12);

				Assert.That(conn.Execute<DateTime> (PathThroughSql, DataParameter.Date("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>(PathThroughSql, new DataParameter("p", dateTime, DataType.Date)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestSmallDateTime([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 00);

				Assert.That(conn.Execute<DateTime> (PathThroughSql, DataParameter.SmallDateTime("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>(PathThroughSql, new DataParameter("p", dateTime, DataType.SmallDateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestDateTime([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
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
		public void TestDateTime2([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var dateTime1 = new DateTime(2012, 12, 12, 12, 12, 12);
				var dateTime2 = new DateTime(2012, 12, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime?>("SELECT timestamp '2012-12-12 12:12:12.012' FROM sys.dual"), Is.EqualTo(dateTime2));

				Assert.That(conn.Execute<DateTime> (PathThroughSql, DataParameter.DateTime2("p", dateTime2)),               Is.EqualTo(dateTime2));
				Assert.That(conn.Execute<DateTime> (PathThroughSql, DataParameter.Create   ("p", dateTime2)),               Is.EqualTo(dateTime2));
				Assert.That(conn.Execute<DateTime?>(PathThroughSql, new DataParameter("p", dateTime2, DataType.DateTime2)), Is.EqualTo(dateTime2));
			}
		}

		[ActiveIssue(Configuration = TestProvName.Oracle21DevartDirect)]
		[Test]
		public void TestDateTimeOffset([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
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

				Assert.That(conn.Execute<DateTime> ("SELECT \"datetimeoffsetDataType\" FROM \"AllTypes\" WHERE ID = 1"), Is.EqualTo(default(DateTime)));
				Assert.That(conn.Execute<DateTime?>("SELECT \"datetimeoffsetDataType\" FROM \"AllTypes\" WHERE ID = 1"), Is.EqualTo(default(DateTime?)));

				Assert.That(conn.Execute<DateTimeOffset?>(PathThroughSql, new DataParameter("p", dto)).                         ToString(), Is.EqualTo(dto.ToString()));
				Assert.That(conn.Execute<DateTimeOffset?>(PathThroughSql, new DataParameter("p", dto, DataType.DateTimeOffset)).ToString(), Is.EqualTo(dto.ToString()));
			}
		}

		[Test]
		public void TestChar([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
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

		[Test]
		public void TestString([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20)) FROM sys.dual"),     Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as char(20)) FROM sys.dual"),     Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar2(20)) FROM sys.dual"),  Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as varchar2(20)) FROM sys.dual"),  Is.Null);

				Assert.That(conn.Execute<string>("SELECT \"textDataType\" FROM \"AllTypes\" WHERE ID = 2"),      Is.EqualTo("567"));
				Assert.That(conn.Execute<string>("SELECT \"textDataType\" FROM \"AllTypes\" WHERE ID = 1"),      Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nchar(20)) FROM sys.dual"),     Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as nchar(20)) FROM sys.dual"),     Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nvarchar2(20)) FROM sys.dual"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as nvarchar2(20)) FROM sys.dual"), Is.Null);

				Assert.That(conn.Execute<string>("SELECT \"ntextDataType\" FROM \"AllTypes\" WHERE ID = 2"),     Is.EqualTo("111"));
				Assert.That(conn.Execute<string>("SELECT \"ntextDataType\" FROM \"AllTypes\" WHERE ID = 1"),     Is.Null);

				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.Char    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.VarChar ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.Text    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.NChar   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.NText   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.Create  ("p", "123")), Is.EqualTo("123"));

				Assert.That(conn.Execute<string>(PathThroughSql, DataParameter.Create("p", (string?)null)), Is.EqualTo(null));
				Assert.That(conn.Execute<string>(PathThroughSql, new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestBinary([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var arr1 = new byte[] {       0x30, 0x39 };
			var arr2 = new byte[] { 0, 0, 0x30, 0x39 };

			using (var conn = GetDataConnection(context))
			{
				Assert.That(conn.Execute<byte[]>("SELECT to_blob('3039')     FROM sys.dual"), Is.EqualTo(           arr1));
				Assert.That(conn.Execute<Binary>("SELECT to_blob('00003039') FROM sys.dual"), Is.EqualTo(new Binary(arr2)));

				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.VarBinary("p", null)), Is.EqualTo(null));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.Binary   ("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.VarBinary("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.Create   ("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.VarBinary("p", Array<byte>.Empty)), Is.EqualTo(Array<byte>.Empty));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.Image    ("p", Array<byte>.Empty)), Is.EqualTo(Array<byte>.Empty));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, DataParameter.Create   ("p", new Binary(arr1))), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>(PathThroughSql, new DataParameter("p", new Binary(arr1))), Is.EqualTo(arr1));
			}
		}

		[Test]
		public void TestOracleManagedTypes([IncludeDataSources(TestProvName.AllOracleManaged)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var arr = new byte[] { 0x30, 0x39 };

				Assert.That(conn.Execute<Oracle.ManagedDataAccess.Types.OracleBinary>   ("SELECT to_blob('3039')          FROM sys.dual").      Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<Oracle.ManagedDataAccess.Types.OracleBlob>     ("SELECT to_blob('3039')          FROM sys.dual").      Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<Oracle.ManagedDataAccess.Types.OracleDecimal>  ("SELECT Cast(1       as decimal) FROM sys.dual").      Value, Is.EqualTo(1));
				Assert.That(conn.Execute<Oracle.ManagedDataAccess.Types.OracleString>   ("SELECT Cast('12345' as char(6)) FROM sys.dual").      Value, Is.EqualTo("12345 "));
				Assert.That(conn.Execute<Oracle.ManagedDataAccess.Types.OracleClob>     ("SELECT \"ntextDataType\"     FROM \"AllTypes\" WHERE ID = 2").Value, Is.EqualTo("111"));
				Assert.That(conn.Execute<Oracle.ManagedDataAccess.Types.OracleDate>     ("SELECT \"datetimeDataType\"  FROM \"AllTypes\" WHERE ID = 2").Value, Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(conn.Execute<Oracle.ManagedDataAccess.Types.OracleTimeStamp>("SELECT \"datetime2DataType\" FROM \"AllTypes\" WHERE ID = 2").Value, Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));
			}
		}

		[Test]
		public void TestOracleDevartTypes([IncludeDataSources(TestProvName.AllOracleDevart)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var arr = new byte[] { 0x30, 0x39 };

				Assert.That(conn.Execute<DA.OracleBinary   >("SELECT to_blob('3039')          FROM sys.dual").Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<DA.OracleLob      >("SELECT to_blob('3039')          FROM sys.dual").Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<DA.OracleNumber   >("SELECT Cast(1       as decimal) FROM sys.dual").Value, Is.EqualTo(1));
				// note Devart provider trims char from OracleString even with trimming disabled in connection string...
				Assert.That(conn.Execute<DA.OracleString   >("SELECT Cast('12345' as char(6)) FROM sys.dual").Value, Is.EqualTo("12345"));
				Assert.That(conn.Execute<DA.OracleLob      >("SELECT \"ntextDataType\"     FROM \"AllTypes\" WHERE ID = 2").Value, Is.EqualTo("111"));
				Assert.That(conn.Execute<DA.OracleDate     >("SELECT \"datetimeDataType\"  FROM \"AllTypes\" WHERE ID = 2").Value, Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(conn.Execute<DA.OracleTimeStamp>("SELECT \"datetime2DataType\" FROM \"AllTypes\" WHERE ID = 2").Value, Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));
			}
		}

#if NET472

		[Test]
		public void TestOracleNativeTypes([IncludeDataSources(TestProvName.AllOracleNative)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var arr = new byte[] { 0x30, 0x39 };

				Assert.That(conn.Execute<Oracle.DataAccess.Types.OracleBinary>   ("SELECT to_blob('3039')          FROM sys.dual").      Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<Oracle.DataAccess.Types.OracleBlob>     ("SELECT to_blob('3039')          FROM sys.dual").      Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<Oracle.DataAccess.Types.OracleDecimal>  ("SELECT Cast(1       as decimal) FROM sys.dual").      Value, Is.EqualTo(1));
				Assert.That(conn.Execute<Oracle.DataAccess.Types.OracleString>   ("SELECT Cast('12345' as char(6)) FROM sys.dual").      Value, Is.EqualTo("12345 "));
				Assert.That(conn.Execute<Oracle.DataAccess.Types.OracleClob>     ("SELECT \"ntextDataType\"     FROM \"AllTypes\" WHERE ID = 2").Value, Is.EqualTo("111"));
				Assert.That(conn.Execute<Oracle.DataAccess.Types.OracleDate>     ("SELECT \"datetimeDataType\"  FROM \"AllTypes\" WHERE ID = 2").Value, Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(conn.Execute<Oracle.DataAccess.Types.OracleTimeStamp>("SELECT \"datetime2DataType\" FROM \"AllTypes\" WHERE ID = 2").Value, Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));
			}
		}

#endif

		[Test]
		public void TestGuid([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (new DisableBaseline("Server-side guid generation test"))
			using (var conn = GetDataConnection(context))
			{
				var guid = conn.Execute<Guid>("SELECT \"guidDataType\" FROM \"AllTypes\" WHERE ID = 2");

				Assert.That(conn.Execute<Guid?>("SELECT \"guidDataType\" FROM \"AllTypes\" WHERE ID = 1"), Is.EqualTo(null));
				Assert.That(conn.Execute<Guid?>("SELECT \"guidDataType\" FROM \"AllTypes\" WHERE ID = 2"), Is.EqualTo(guid));

				Assert.That(conn.Execute<Guid>(PathThroughSql, DataParameter.Create("p", guid)),                Is.EqualTo(guid));
				Assert.That(conn.Execute<Guid>(PathThroughSql, new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test]
		public void TestXml([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.That(conn.Execute<string>     ("SELECT XMLTYPE('<xml/>') FROM sys.dual").TrimEnd(),  Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>  ("SELECT XMLTYPE('<xml/>') FROM sys.dual").ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT XMLTYPE('<xml/>') FROM sys.dual").InnerXml,   Is.EqualTo("<xml />"));

				var xdoc = XDocument.Parse("<xml/>");
				var xml  = Convert<string,XmlDocument>.Lambda("<xml/>");

				var xmlExpected = context.IsAnyOf(TestProvName.AllOracleNative) ? "<xml/>\n" : "<xml/>";
				Assert.That(conn.Execute<string>     (PathThroughSql, DataParameter.Xml("p", "<xml/>")),        Is.EqualTo(xmlExpected));
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
		public void TestEnum1([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.That(conn.Execute<TestEnum> ("SELECT 'A' FROM sys.dual"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'A' FROM sys.dual"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum> ("SELECT 'B' FROM sys.dual"), Is.EqualTo(TestEnum.BB));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'B' FROM sys.dual"), Is.EqualTo(TestEnum.BB));
			}
		}

		[Test]
		public void TestEnum2([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.That(conn.Execute<string>(PathThroughSql, new { p = TestEnum.AA }),            Is.EqualTo("A"));
				Assert.That(conn.Execute<string>(PathThroughSql, new { p = (TestEnum?)TestEnum.BB }), Is.EqualTo("B"));

				Assert.That(conn.Execute<string>(PathThroughSql, new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>(PathThroughSql, new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>(PathThroughSql, new { p = conn.MappingSchema.GetConverter<TestEnum?,string>()!(TestEnum.AA) }), Is.EqualTo("A"));
			}
		}

		[Test]
		public void TestTreatEmptyStringsAsNulls([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var table    = db.GetTable<OracleSpecific.StringTest>();
				var expected = table.Where(_ => _.KeyValue == "NullValues").ToList();


				AreEqual(expected, table.Where(_ => string.IsNullOrEmpty(_.StringValue1)));
				AreEqual(expected, table.Where(_ => string.IsNullOrEmpty(_.StringValue2)));

				AreEqual(expected, table.Where(_ => _.StringValue1 == ""));
				AreEqual(expected, table.Where(_ => _.StringValue2 == ""));

				AreEqual(expected, table.Where(_ => _.StringValue1 == null));
				AreEqual(expected, table.Where(_ => _.StringValue2 == null));

				string  emptyString = string.Empty;
				string? nullString  = null;

				AreEqual(expected, table.Where(_ => _.StringValue1 == emptyString));
				AreEqual(expected, table.Where(_ => _.StringValue2 == emptyString));

				AreEqual(expected, table.Where(_ => _.StringValue1 == nullString));
				AreEqual(expected, table.Where(_ => _.StringValue2 == nullString));

				AreEqual(expected, GetStringTest1(db, emptyString));
				AreEqual(expected, GetStringTest1(db, emptyString));

				AreEqual(expected, GetStringTest2(db, emptyString));
				AreEqual(expected, GetStringTest2(db, emptyString));

				AreEqual(expected, GetStringTest1(db, nullString));
				AreEqual(expected, GetStringTest1(db, nullString));

				AreEqual(expected, GetStringTest2(db, nullString));
				AreEqual(expected, GetStringTest2(db, nullString));
			}
		}

		private IEnumerable<OracleSpecific.StringTest> GetStringTest1(IDataContext db, string? value)
		{
			return db.GetTable<OracleSpecific.StringTest>()
				.Where(_ => value == _.StringValue1);
		}

		private IEnumerable<OracleSpecific.StringTest> GetStringTest2(IDataContext db, string? value)
		{
			return db.GetTable<OracleSpecific.StringTest>()
				.Where(_ => value == _.StringValue2);
		}

#region DateTime Tests

		[Table]
		public partial class AllTypes
		{
			[Column(DataType = DataType.Decimal       , Length = 22   , Scale = 0)    , PrimaryKey          ] public decimal         ID                     { get; set; } // NUMBER
			[Column(DataType = DataType.Decimal       , Length = 22   , Precision = 20, Scale = 0), Nullable] public decimal?        bigintDataType         { get; set; } // NUMBER (20,0)
			[Column(DataType = DataType.Decimal       , Length = 22   , Scale = 0)    , Nullable            ] public decimal?        numericDataType        { get; set; } // NUMBER
			[Column(DataType = DataType.Decimal       , Length = 22   , Precision = 1 , Scale = 0), Nullable] public sbyte?          bitDataType            { get; set; } // NUMBER (1,0)
			[Column(DataType = DataType.Decimal       , Length = 22   , Precision = 5 , Scale = 0), Nullable] public int?            smallintDataType       { get; set; } // NUMBER (5,0)
			[Column(DataType = DataType.Decimal       , Length = 22   , Scale = 6)    , Nullable            ] public decimal?        decimalDataType        { get; set; } // NUMBER
			[Column(DataType = DataType.Decimal       , Length = 22   , Precision = 10, Scale = 4), Nullable] public decimal?        smallmoneyDataType     { get; set; } // NUMBER (10,4)
			[Column(DataType = DataType.Decimal       , Length = 22   , Precision = 10, Scale = 0), Nullable] public long?           intDataType            { get; set; } // NUMBER (10,0)
			[Column(DataType = DataType.Decimal       , Length = 22   , Precision = 3 , Scale = 0), Nullable] public short?          tinyintDataType        { get; set; } // NUMBER (3,0)
			[Column(DataType = DataType.Decimal       , Length = 22)  , Nullable                            ] public decimal?        moneyDataType          { get; set; } // NUMBER
			[Column(DataType = DataType.Double        , Length = 8)   , Nullable                            ] public double?         floatDataType          { get; set; } // BINARY_DOUBLE
			[Column(DataType = DataType.Single        , Length = 4)   , Nullable                            ] public float?          realDataType           { get; set; } // BINARY_FLOAT
			[Column(DataType = DataType.Date)         , Nullable                                            ] public DateTime?       datetimeDataType       { get; set; } // DATE
			[Column(DataType = DataType.DateTime2     , Length = 11   , Scale = 6)    , Nullable            ] public DateTime?       datetime2DataType      { get; set; } // TIMESTAMP(6)
			[Column(DataType = DataType.DateTimeOffset, Length = 13   , Scale = 6)    , Nullable            ] public DateTimeOffset? datetimeoffsetDataType { get; set; } // TIMESTAMP(6) WITH TIME ZONE
			[Column(DataType = DataType.DateTimeOffset, Length = 11   , Scale = 6)    , Nullable            ] public DateTimeOffset? localZoneDataType      { get; set; } // TIMESTAMP(6) WITH LOCAL TIME ZONE
			[Column(DataType = DataType.Char          , Length = 1)   , Nullable                            ] public char?           charDataType           { get; set; } // CHAR(1)
			[Column(DataType = DataType.VarChar       , Length = 20)  , Nullable                            ] public string?         varcharDataType        { get; set; } // VARCHAR2(20)
			[Column(DataType = DataType.Text          , Length = 4000), Nullable                            ] public string?         textDataType           { get; set; } // CLOB
			[Column(DataType = DataType.NChar         , Length = 40)  , Nullable                            ] public string?         ncharDataType          { get; set; } // NCHAR(40)
			[Column(DataType = DataType.NVarChar      , Length = 40)  , Nullable                            ] public string?         nvarcharDataType       { get; set; } // NVARCHAR2(40)
			[Column(DataType = DataType.NText         , Length = 4000), Nullable                            ] public string?         ntextDataType          { get; set; } // NCLOB
			[Column(DataType = DataType.Blob          , Length = 4000), Nullable                            ] public byte[]?         binaryDataType         { get; set; } // BLOB
			[Column(DataType = DataType.VarBinary     , Length = 530) , Nullable                            ] public byte[]?         bfileDataType          { get; set; } // BFILE
			[Column(DataType = DataType.Binary        , Length = 16)  , Nullable                            ] public byte[]?         guidDataType           { get; set; } // RAW(16)
			[Column(DataType = DataType.Long)         , Nullable                                            ] public string?         longDataType           { get; set; } // LONG
			[Column(DataType = DataType.Xml           , Length = 2000), Nullable                            ] public string?         xmlDataType            { get; set; } // XMLTYPE
		}

		[Table("t_entity")]
		public sealed class Entity
		{
			[PrimaryKey, Identity]
			[Column("entity_id")] public long Id           { get; set; }
			[Column("time")]      public DateTime Time     { get; set; }
			[Column("duration")]  public TimeSpan Duration { get; set; }
		}

		[Test]
		public void TestTimeSpan([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.BeginTransaction();

				long id = 1;

				db.GetTable<Entity>().Insert(() => new Entity { Id = id + 1, Duration = TimeSpan.FromHours(1) });
			}
		}

		[Test]
		public void DateTimeTest1([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.GetTable<AllTypes>().Delete(t => t.ID >= 1000);

				using (db.BeginTransaction())
				{
					db.BulkCopy(
						new BulkCopyOptions() { BulkCopyType = BulkCopyType.MultipleRows },
						new[]
						{
							new AllTypes
							{
								ID                = 1000,
								datetimeDataType  = TestData.DateTime,
								datetime2DataType = TestData.DateTime
							}
						});
				}
			}
		}

		[Test]
		public async Task DateTimeTest1Async([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.GetTable<AllTypes>().Delete(t => t.ID >= 1000);

				using (db.BeginTransaction())
				{
					await db.BulkCopyAsync(
						new BulkCopyOptions() { BulkCopyType = BulkCopyType.MultipleRows },
						new[]
						{
							new AllTypes
							{
								ID                = 1000,
								datetimeDataType  = TestData.DateTime,
								datetime2DataType = TestData.DateTime
							}
						});
				}
			}
		}

		[Test]
		public void NVarchar2InsertTest([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			ResetAllTypesIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				db.InlineParameters = false;

				var value   = "致我们最爱的母亲";

				var id = db.GetTable<AllTypes>()
					.InsertWithInt32Identity(() => new AllTypes
					{
						nvarcharDataType = value
					});

				var query = from p in db.GetTable<AllTypes>()
							where p.ID == id
							select new { p.nvarcharDataType };

				var res = query.Single();
				Assert.That(res.nvarcharDataType, Is.EqualTo(value));
			}
		}

		[Test]
		public void NVarchar2UpdateTest([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			ResetAllTypesIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				db.InlineParameters = false;

				var value = "致我们最爱的母亲";

				var id = db.GetTable<AllTypes>()
					.InsertWithInt32Identity(() => new AllTypes
					{
						intDataType = 123
					});

				db.GetTable<AllTypes>()
					.Set(e => e.nvarcharDataType, () => value)
					.Update();

				var query = from p in db.GetTable<AllTypes>()
							where p.ID == id
							select new { p.nvarcharDataType };

				var res = query.Single();
				Assert.That(res.nvarcharDataType, Is.EqualTo(value));
			}
		}

		[Test]
		public void SelectDateTime([IncludeDataSources(TestProvName.AllOracleNative)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var ms = new MappingSchema();

				// Set custom DateTime to SQL converter.
				//
				ms.SetValueToSqlConverter(
					typeof(DateTime),
					(stringBuilder, dataType, val) =>
					{
						var value = (DateTime)val;
						Assert.That(dataType.Type.DataType, Is.Not.EqualTo(DataType.Undefined));

						var format =
							dataType.Type.DataType == DataType.DateTime2 ?
								"TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')" :
								"TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.fffffff}', 'YYYY-MM-DD HH24:MI:SS.FF7')";

						stringBuilder.AppendFormat(format, value);
					});

				db.AddMappingSchema(ms);

				var res = db.GetTable<AllTypes>().Where(e => e.datetime2DataType == TestData.DateTime).ToList();
				Debug.WriteLine(res.Count);
			}
		}

		[Test]
		public void DateTimeTest2([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			// Set custom DateTime to SQL converter.
			//
			var ms = new MappingSchema();
			ms.SetValueToSqlConverter(
				typeof(DateTime),
				(stringBuilder,dataType,val) =>
				{
					var value  = (DateTime)val;
					var format =
						dataType.Type.DataType == DataType.DateTime ?
							"TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')" :
							"TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.fffffff}', 'YYYY-MM-DD HH24:MI:SS.FF7')";

					stringBuilder.AppendFormat(format, value);
				});

			using (var db = GetDataConnection(context, ms))
			{
				db.GetTable<AllTypes>().Delete(t => t.ID >= 1000);

				using (db.BeginTransaction())
				{
					db.BulkCopy(
						new BulkCopyOptions() { BulkCopyType = BulkCopyType.MultipleRows },
						new[]
						{
							new AllTypes
							{
								ID                = 1000,
								datetimeDataType  = TestData.DateTime,
								datetime2DataType = TestData.DateTime
							}
						});
				}
			}
		}

		[Test]
		public async Task DateTimeTest2Async([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			// Set custom DateTime to SQL converter.
			//
			var ms = new MappingSchema();
			ms.SetValueToSqlConverter(
				typeof(DateTime),
				(stringBuilder, dataType, val) =>
				{
					var value = (DateTime)val;
					var format =
						dataType.Type.DataType == DataType.DateTime ?
							"TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')" :
							"TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.fffffff}', 'YYYY-MM-DD HH24:MI:SS.FF7')";

					stringBuilder.AppendFormat(format, value);
				});

			using (var db = GetDataConnection(context, ms))
			{
				db.GetTable<AllTypes>().Delete(t => t.ID >= 1000);

				using (db.BeginTransaction())
				{
					await db.BulkCopyAsync(
						new BulkCopyOptions() { BulkCopyType = BulkCopyType.MultipleRows },
						new[]
						{
							new AllTypes
							{
								ID                = 1000,
								datetimeDataType  = TestData.DateTime,
								datetime2DataType = TestData.DateTime
							}
						});
				}
			}
		}

		[Test]
		public void ClauseDateTimeWithoutJointure([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var date = TestData.Date;
			using (var db = GetDataConnection(context))
			{
				DbParameter[] parameters = null!;
				db.OnNextCommandInitialized((args, cmd) =>
				{
					parameters = cmd.Parameters.Cast<DbParameter>().ToArray();
					return cmd;
				});

				var query = from a in db.GetTable<AllTypes>()
							where a.datetimeDataType == date
							select a;

				query.FirstOrDefault();

				Assert.That(parameters.Length, Is.EqualTo(2));

				if (context.IsAnyOf(TestProvName.AllOracleDevart))
					// another case of sloppy implementation by devart...
					Assert.True(parameters.Any(p => p.DbType == DbType.DateTime));
				else
					Assert.True(parameters.Any(p => p.DbType == DbType.Date));
			}
		}

		[Test]
		public void ClauseDateTimeWithJointure([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var date = TestData.Date;
			using (var db = GetDataConnection(context))
			{
				DbParameter[] parameters = null!;
				db.OnNextCommandInitialized((args, cmd) =>
				{
					parameters = cmd.Parameters.Cast<DbParameter>().ToArray();
					return cmd;
				});

				var query = from a in db.GetTable<AllTypes>()
							join b in db.GetTable<AllTypes>() on a.ID equals b.ID
							where a.datetimeDataType == date
							select a;

				query.FirstOrDefault();

				Assert.That(parameters.Length, Is.EqualTo(2));

				if (context.IsAnyOf(TestProvName.AllOracleDevart))
					// another case of sloppy implementation by devart...
					Assert.True(parameters.Any(p => p.DbType == DbType.DateTime));
				else
					Assert.True(parameters.Any(p => p.DbType == DbType.Date));
			}
		}

#endregion

#region Sequence

		[Test]
		public void SequenceInsert([IncludeDataSources(TestProvName.AllOracle)] string context)
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

		[Test]
		public void SequenceInsertWithIdentity([IncludeDataSources(TestProvName.AllOracle)] string context)
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

		void BulkCopyLinqTypes(string context, BulkCopyType bulkCopyType)
		{
			using (var db = GetDataConnection(context))
			{
				if (bulkCopyType == BulkCopyType.ProviderSpecific)
				{
					var ms = new MappingSchema();

					ms.GetFluentMappingBuilder()
						.Entity<LinqDataTypes>()
							.Property(e => e.GuidValue)
								.IsNotColumn()
						;

					if (context.IsAnyOf(TestProvName.AllOracleNative))
					{
						ms.GetFluentMappingBuilder()
							.Entity<LinqDataTypes>()
								.Property(e => e.BoolValue)
									.HasDataType(DataType.Int16)
							;
					}

					db.AddMappingSchema(ms);
				}

				try
				{
					db.BulkCopy(
						new BulkCopyOptions { BulkCopyType = bulkCopyType },
						Enumerable.Range(0, 10).Select(n =>
							new LinqDataTypes
							{
								ID            = 4000 + n,
								MoneyValue    = 1000m + n,
								DateTimeValue = new DateTime(2001,  1,  11,  1, 11, 21, 100),
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

		async Task BulkCopyLinqTypesAsync(string context, BulkCopyType bulkCopyType)
		{
			using (var db = GetDataConnection(context))
			{
				if (bulkCopyType == BulkCopyType.ProviderSpecific)
				{
					var ms = new MappingSchema();

					ms.GetFluentMappingBuilder()
						.Entity<LinqDataTypes>()
							.Property(e => e.GuidValue)
								.IsNotColumn()
						;

					if (context.IsAnyOf(TestProvName.AllOracleNative))
					{
						ms.GetFluentMappingBuilder()
							.Entity<LinqDataTypes>()
								.Property(e => e.BoolValue)
									.HasDataType(DataType.Int16)
							;
					}

					db.AddMappingSchema(ms);
				}

				try
				{
					await db.BulkCopyAsync(
						new BulkCopyOptions { BulkCopyType = bulkCopyType },
						Enumerable.Range(0, 10).Select(n =>
							new LinqDataTypes
							{
								ID            = 4000 + n,
								MoneyValue    = 1000m + n,
								DateTimeValue = new DateTime(2001,  1,  11,  1, 11, 21, 100),
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

		[Test]
		public void BulkCopyLinqTypesMultipleRows(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
		{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				BulkCopyLinqTypes(context, BulkCopyType.MultipleRows);
			}

		[Test]
		public async Task BulkCopyLinqTypesMultipleRowsAsync(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
			{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				await BulkCopyLinqTypesAsync(context, BulkCopyType.MultipleRows);
			}

		[Test]
		public void BulkCopyLinqTypesProviderSpecific(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
			{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				BulkCopyLinqTypes(context, BulkCopyType.ProviderSpecific);
			}

		[Test]
		public async Task BulkCopyLinqTypesProviderSpecificAsync(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
		{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				await BulkCopyLinqTypesAsync(context, BulkCopyType.ProviderSpecific);
			}

		[Test]
		public void BulkCopyRetrieveSequencesProviderSpecific(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
			{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				BulkCopyRetrieveSequence(context, BulkCopyType.ProviderSpecific);
			}

		[Test]
		public async Task BulkCopyRetrieveSequencesProviderSpecificAsync(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
			{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				await BulkCopyRetrieveSequenceAsync(context, BulkCopyType.ProviderSpecific);
			}

		[Test]
		public void BulkCopyRetrieveSequencesMultipleRows(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
			{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				BulkCopyRetrieveSequence(context, BulkCopyType.MultipleRows);
			}

		[Test]
		public async Task BulkCopyRetrieveSequencesMultipleRowsAsync(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
			{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				await BulkCopyRetrieveSequenceAsync(context, BulkCopyType.MultipleRows);
			}

		[Test]
		public void BulkCopyRetrieveSequencesRowByRow(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
		{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				BulkCopyRetrieveSequence(context, BulkCopyType.RowByRow);
			}

		[Test]
		public async Task BulkCopyRetrieveSequencesRowByRowAsync(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
			{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				await BulkCopyRetrieveSequenceAsync(context, BulkCopyType.RowByRow);
			}

		void BulkCopyRetrieveSequence(string context, BulkCopyType bulkCopyType)
		{
			var data = new[]
			{
				new OracleSpecific.SequenceTest { Value = "Value"},
				new OracleSpecific.SequenceTest { Value = "Value"},
				new OracleSpecific.SequenceTest { Value = "Value"},
				new OracleSpecific.SequenceTest { Value = "Value"},
			};

			using (var db = GetDataConnection(context))
			{
				db.GetTable<OracleSpecific.SequenceTest>().Where(_ => _.Value == "SeqValue").Delete();

				var options = new BulkCopyOptions
				{
					MaxBatchSize       = 5,
					//RetrieveSequence   = true,
					KeepIdentity       = bulkCopyType != BulkCopyType.RowByRow,
					BulkCopyType       = bulkCopyType,
					NotifyAfter        = 3,
					RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied)
				};

				db.BulkCopy(options, data.RetrieveIdentity(db));

				foreach (var d in data)
				{
					Assert.That(d.ID, Is.GreaterThan(0));
				}

				//Assert.That(options.BulkCopyType, Is.EqualTo(bulkCopyType));
			}
		}

		async Task BulkCopyRetrieveSequenceAsync(string context, BulkCopyType bulkCopyType)
		{
			var data = new[]
			{
				new OracleSpecific.SequenceTest { Value = "Value"},
				new OracleSpecific.SequenceTest { Value = "Value"},
				new OracleSpecific.SequenceTest { Value = "Value"},
				new OracleSpecific.SequenceTest { Value = "Value"},
			};

			using (var db = GetDataConnection(context))
			{
				db.GetTable<OracleSpecific.SequenceTest>().Where(_ => _.Value == "SeqValue").Delete();

				var options = new BulkCopyOptions
				{
					MaxBatchSize       = 5,
					//RetrieveSequence   = true,
					KeepIdentity       = bulkCopyType != BulkCopyType.RowByRow,
					BulkCopyType       = bulkCopyType,
					NotifyAfter        = 3,
					RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied)
				};

				await db.BulkCopyAsync(options, data.RetrieveIdentity(db));

				foreach (var d in data)
				{
					Assert.That(d.ID, Is.GreaterThan(0));
				}

				//Assert.That(options.BulkCopyType, Is.EqualTo(bulkCopyType));
			}
		}

		[Table(Name = "STG_TRADE_INFORMATION")]
		public class Trade
		{
			[Column("STG_TRADE_ID")]          public int       ID             { get; set; }
			[Column("STG_TRADE_VERSION")]     public int       Version        { get; set; }
			[Column("INFORMATION_TYPE_ID")]   public int       TypeID         { get; set; }
			[Column("INFORMATION_TYPE_NAME")] public string?   TypeName       { get; set; }
			[Column("VALUE")]                 public string?   Value          { get; set; }
			[Column("VALUE_AS_INTEGER")]      public int?      ValueAsInteger { get; set; }
			[Column("VALUE_AS_DATE")]         public DateTime? ValueAsDate    { get; set; }
		}

		void BulkCopy1(string context, BulkCopyType bulkCopyType)
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

			using (var db = GetDataConnection(context))
			{
				var options = new BulkCopyOptions
				{
					MaxBatchSize = 5,
					BulkCopyType = bulkCopyType,
					NotifyAfter  = 3,
					RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied)
				};

				db.BulkCopy(options, data);

				//Assert.That(options.BulkCopyType, Is.EqualTo(bulkCopyType));
			}
		}

		async Task BulkCopy1Async(string context, BulkCopyType bulkCopyType)
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

			using (var db = GetDataConnection(context))
			{
				var options = new BulkCopyOptions
				{
					MaxBatchSize = 5,
					BulkCopyType = bulkCopyType,
					NotifyAfter  = 3,
					RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied)
				};

				await db.BulkCopyAsync(options, data);

				//Assert.That(options.BulkCopyType, Is.EqualTo(bulkCopyType));
			}
		}

		[Test]
		public void BulkCopy1MultipleRows(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
		{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				BulkCopy1(context, BulkCopyType.MultipleRows);
		}

		[Test]
		public async Task BulkCopy1MultipleRowsAsync(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
		{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				await BulkCopy1Async(context, BulkCopyType.MultipleRows);
		}

		[Test]
		public void BulkCopy1ProviderSpecific(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
		{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				BulkCopy1(context, BulkCopyType.ProviderSpecific);
		}

		[Test]
		public async Task BulkCopy1ProviderSpecificAsync(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
		{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				await BulkCopy1Async(context, BulkCopyType.ProviderSpecific);
		}

		// we use copy of table with all-uppercase names to be able to use it with native
		// bulk copy with ODP.NET provider
		[Table("LINQDATATYPESBC")]
		public class LinqDataTypesBC
		{
			[PrimaryKey]                                             public int       ID;
			[Column("MONEYVALUE")]                                   public decimal   MoneyValue;
			[Column("DATETIMEVALUE", DataType = DataType.DateTime2)] public DateTime? DateTimeValue;
			[Column("DATETIMEVALUE2")]                               public DateTime? DateTimeValue2;
			[Column("BOOLVALUE", DataType = DataType.Int16)]         public bool?     BoolValue;
			[Column("GUIDVALUE")]                                    public Guid?     GuidValue;
			[Column("SMALLINTVALUE")]                                public short?    SmallIntValue;
			[Column("INTVALUE")]                                     public int?      IntValue;
			[Column("BIGINTVALUE")]                                  public long?     BigIntValue;
			[Column("STRINGVALUE")]                                  public string?   StringValue;
		}

		void BulkCopy21(string context, BulkCopyType bulkCopyType)
		{
			using (var db = GetDataConnection(context))
			{
				db.GetTable<LinqDataTypesBC>().Delete();

				if (context.IsAnyOf(TestProvName.AllOracleNative) && bulkCopyType == BulkCopyType.ProviderSpecific)
				{
					var ms = new MappingSchema();

					db.AddMappingSchema(ms);

					ms.GetFluentMappingBuilder()
						.Entity<LinqDataTypesBC>()
							.Property(e => e.GuidValue)
								.IsNotColumn()
						;
				}

				try
				{
					db.BulkCopy(
						new BulkCopyOptions { MaxBatchSize = 2, BulkCopyType = bulkCopyType },
						new[]
						{
							new LinqDataTypesBC { ID = 1003, MoneyValue = 0m, DateTimeValue = null,              BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue = null, IntValue = null    },
							new LinqDataTypesBC { ID = 1004, MoneyValue = 0m, DateTimeValue = TestData.DateTime, BoolValue = false, GuidValue = null,                                             SmallIntValue = 2,    IntValue = 1532334 },
							new LinqDataTypesBC { ID = 1005, MoneyValue = 1m, DateTimeValue = TestData.DateTime, BoolValue = false, GuidValue = null,                                             SmallIntValue = 5,    IntValue = null    },
							new LinqDataTypesBC { ID = 1006, MoneyValue = 2m, DateTimeValue = TestData.DateTime, BoolValue = false, GuidValue = null,                                             SmallIntValue = 6,    IntValue = 153     }
						});
				}
				finally
				{
					db.GetTable<LinqDataTypesBC>().Delete();
				}
			}
		}

		async Task BulkCopy21Async(string context, BulkCopyType bulkCopyType)
		{
			using (var db = GetDataConnection(context))
			{
				db.GetTable<LinqDataTypesBC>().Delete();

				if (context.IsAnyOf(TestProvName.AllOracleNative) && bulkCopyType == BulkCopyType.ProviderSpecific)
				{
					var ms = new MappingSchema();

					db.AddMappingSchema(ms);

					ms.GetFluentMappingBuilder()
						.Entity<LinqDataTypesBC>()
							.Property(e => e.GuidValue)
								.IsNotColumn()
						;
				}

				try
				{
					await db.BulkCopyAsync(
						new BulkCopyOptions { MaxBatchSize = 2, BulkCopyType = bulkCopyType },
						new[]
						{
							new LinqDataTypesBC { ID = 1003, MoneyValue = 0m, DateTimeValue = null,              BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue = null, IntValue = null    },
							new LinqDataTypesBC { ID = 1004, MoneyValue = 0m, DateTimeValue = TestData.DateTime, BoolValue = false, GuidValue = null,                                             SmallIntValue = 2,    IntValue = 1532334 },
							new LinqDataTypesBC { ID = 1005, MoneyValue = 1m, DateTimeValue = TestData.DateTime, BoolValue = false, GuidValue = null,                                             SmallIntValue = 5,    IntValue = null    },
							new LinqDataTypesBC { ID = 1006, MoneyValue = 2m, DateTimeValue = TestData.DateTime, BoolValue = false, GuidValue = null,                                             SmallIntValue = 6,    IntValue = 153     }
						});
				}
				finally
				{
					db.GetTable<LinqDataTypesBC>().Delete();
				}
			}
		}

		[Test]
		public void BulkCopy21MultipleRows(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
		{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				BulkCopy21(context, BulkCopyType.MultipleRows);
		}

		[Test]
		public async Task BulkCopy21MultipleRowsAsync(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
		{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				await BulkCopy21Async(context, BulkCopyType.MultipleRows);
		}

		// ORA-38910: BATCH ERROR mode is not supported for this operation
		[ActiveIssue(Configuration = TestProvName.AllOracleDevartOCI)]
		[Test]
		public void BulkCopy21ProviderSpecific(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
		{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				BulkCopy21(context, BulkCopyType.ProviderSpecific);
		}

		// ORA-38910: BATCH ERROR mode is not supported for this operation
		[ActiveIssue(Configuration = TestProvName.AllOracleDevartOCI)]
		[Test]
		public async Task BulkCopy21ProviderSpecificAsync(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
			{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				await BulkCopy21Async(context, BulkCopyType.ProviderSpecific);
			}

		void BulkCopy22(string context, BulkCopyType bulkCopyType)
		{
			using (var db = GetDataConnection(context))
			{
				db.Types2.Delete(_ => _.ID > 1000);

				var ms = new MappingSchema();

				db.AddMappingSchema(ms);

				ms.GetFluentMappingBuilder()
					.Entity<LinqDataTypes2>()
						.Property(e => e.GuidValue)
							.IsNotColumn()
					;

				try
				{
					db.BulkCopy(
						new BulkCopyOptions { MaxBatchSize = 2, BulkCopyType = bulkCopyType },
						new[]
						{
							new LinqDataTypes2 { ID = 1003, MoneyValue = 0m, DateTimeValue = TestData.DateTime, BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue = null, IntValue = null    },
							new LinqDataTypes2 { ID = 1004, MoneyValue = 0m, DateTimeValue = null,              BoolValue = false, GuidValue = null,                                             SmallIntValue = 2,    IntValue = 1532334 },
							new LinqDataTypes2 { ID = 1005, MoneyValue = 1m, DateTimeValue = TestData.DateTime, BoolValue = false, GuidValue = null,                                             SmallIntValue = 5,    IntValue = null    },
							new LinqDataTypes2 { ID = 1006, MoneyValue = 2m, DateTimeValue = TestData.DateTime, BoolValue = false, GuidValue = null,                                             SmallIntValue = 6,    IntValue = 153     }
						});
				}
				finally
				{
					db.Types2.Delete(_ => _.ID > 1000);
				}
			}
		}

		async Task BulkCopy22Async(string context, BulkCopyType bulkCopyType)
		{
			using (var db = GetDataConnection(context))
			{
				db.Types2.Delete(_ => _.ID > 1000);

				var ms = new MappingSchema();

				db.AddMappingSchema(ms);

				ms.GetFluentMappingBuilder()
					.Entity<LinqDataTypes2>()
						.Property(e => e.GuidValue)
							.IsNotColumn()
					;

				try
				{
					await db.BulkCopyAsync(
						new BulkCopyOptions { MaxBatchSize = 2, BulkCopyType = bulkCopyType },
						new[]
						{
							new LinqDataTypes2 { ID = 1003, MoneyValue = 0m, DateTimeValue = TestData.DateTime, BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue = null, IntValue = null    },
							new LinqDataTypes2 { ID = 1004, MoneyValue = 0m, DateTimeValue = null,              BoolValue = false, GuidValue = null,                                             SmallIntValue = 2,    IntValue = 1532334 },
							new LinqDataTypes2 { ID = 1005, MoneyValue = 1m, DateTimeValue = TestData.DateTime, BoolValue = false, GuidValue = null,                                             SmallIntValue = 5,    IntValue = null    },
							new LinqDataTypes2 { ID = 1006, MoneyValue = 2m, DateTimeValue = TestData.DateTime, BoolValue = false, GuidValue = null,                                             SmallIntValue = 6,    IntValue = 153     }
						});
				}
				finally
				{
					await db.Types2.DeleteAsync(_ => _.ID > 1000);
				}
			}
		}

		[Test]
		public void BulkCopy22MultipleRows(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
			{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				BulkCopy22(context, BulkCopyType.MultipleRows);
			}

		[Test]
		public async Task BulkCopy22MultipleRowsAsync(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
			{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				await BulkCopy22Async(context, BulkCopyType.MultipleRows);
			}

		[Test]
		public void BulkCopy22ProviderSpecific(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
		{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				BulkCopy22(context, BulkCopyType.ProviderSpecific);
			}

		[Test]
		public async Task BulkCopy22ProviderSpecificAsync(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
			{
			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
				await BulkCopy22Async(context, BulkCopyType.ProviderSpecific);
			}

#endregion

#region CreateTest

		[Table]
		sealed class TempTestTable
		{
			// column name length = 30 char (maximum for Oracle)
			[Column(Name = "AAAAAAAAAAAAAAAAAAAAAAAAAAAABC")]
			public long Id { get; set; }
		}

		[Test]
		public void LongAliasTest([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataConnection(context))
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

#region XmlTable

		[Test]
		public void XmlTableTest1([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var list = conn.OracleXmlTable(new[]
					{
						new { field1 = 1, field2 = "11" },
						new { field1 = 2, field2 = "22" },
					})
					.Select(t => new { t.field1, t.field2 })
					.ToList();

				Assert.That(list.Count, Is.EqualTo(2));
				Assert.That(list[0].field1, Is.EqualTo(1));
				Assert.That(list[1].field1, Is.EqualTo(2));
				Assert.That(list[0].field2, Is.EqualTo("11"));
				Assert.That(list[1].field2, Is.EqualTo("22"));
			}
		}

		[Test]
		public void XmlTableTest2([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataContext(context))
			{
				var list =
				(
					from t1 in conn.Parent
					join t2 in conn.OracleXmlTable(new[]
					{
						new { field1 = 1, field2 = "11" },
						new { field1 = 2, field2 = "22" },
					})
					on t1.ParentID equals t2.field1
					select new { t2.field1, t2.field2 }
				).ToList();

				Assert.That(list.Count, Is.EqualTo(2));
				Assert.That(list[0].field1, Is.EqualTo(1));
				Assert.That(list[1].field1, Is.EqualTo(2));
				Assert.That(list[0].field2, Is.EqualTo("11"));
				Assert.That(list[1].field2, Is.EqualTo("22"));
			}
		}

		[Test]
		public void XmlTableTest3([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var data = new[]
				{
					new { field1 = 1, field2 = "11" },
					new { field1 = 2, field2 = "22" },
				};

				var list = conn.OracleXmlTable(data)
					.Select(t => new { t.field1, t.field2 })
					.ToList();

				Assert.That(list.Count, Is.EqualTo(2));
				Assert.That(list[0].field1, Is.EqualTo(1));
				Assert.That(list[1].field1, Is.EqualTo(2));
				Assert.That(list[0].field2, Is.EqualTo("11"));
				Assert.That(list[1].field2, Is.EqualTo("22"));
			}
		}

		sealed class XmlData
		{
			public int     Field1;
			[Column(Length = 2)]
			public string? Field2;
		}

		[Test]
		public void XmlTableTest4([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var list = conn.OracleXmlTable<XmlData>("<t><r><c0>1</c0><c1>11</c1></r><r><c0>2</c0><c1>22</c1></r></t>")
					.Select(t => new { t.Field1, t.Field2 })
					.ToList();

				Assert.That(list.Count, Is.EqualTo(2));
				Assert.That(list[0].Field1, Is.EqualTo(1));
				Assert.That(list[1].Field1, Is.EqualTo(2));
				Assert.That(list[0].Field2, Is.EqualTo("11"));
				Assert.That(list[1].Field2, Is.EqualTo("22"));
			}
		}

		static string? _data;

		[Test]
		public void XmlTableTest5([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataContext(context))
			{
				_data = "<t><r><c0>1</c0><c1>11</c1></r><r><c0>2</c0><c1>22</c1></r></t>";

				var list = conn.OracleXmlTable<XmlData>(_data)
					.Select(t => new { t.Field1, t.Field2 })
					.ToList();

				Assert.That(list.Count, Is.EqualTo(2));
				Assert.That(list[0].Field1, Is.EqualTo(1));
				Assert.That(list[1].Field1, Is.EqualTo(2));
				Assert.That(list[0].Field2, Is.EqualTo("11"));
				Assert.That(list[1].Field2, Is.EqualTo("22"));

				_data = "<t><r><c0>1</c0><c1>11</c1></r></t>";

				list =
				(
					from t1 in conn.Parent
					join t2 in conn.OracleXmlTable<XmlData>(_data)
					on t1.ParentID equals t2.Field1
					select new { t2.Field1, t2.Field2 }
				).ToList();

				Assert.That(list.Count, Is.EqualTo(1));
				Assert.That(list[0].Field1, Is.EqualTo(1));
				Assert.That(list[0].Field2, Is.EqualTo("11"));
			}
		}

		[Test]
		public void XmlTableTest6([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var data = new[]
				{
					new { field1 = 1, field2 = "11" },
					new { field1 = 2, field2 = "22" },
				};

				var xmlData = OracleTools.GetXmlData(conn.MappingSchema, data);

				var list = conn.OracleXmlTable<XmlData>(xmlData)
					.Select(t => new { t.Field1, t.Field2 })
					.ToList();

				Assert.That(list.Count, Is.EqualTo(2));
				Assert.That(list[0].Field1, Is.EqualTo(1));
				Assert.That(list[1].Field1, Is.EqualTo(2));
				Assert.That(list[0].Field2, Is.EqualTo("11"));
				Assert.That(list[1].Field2, Is.EqualTo("22"));
			}
		}

		[Test]
		public void XmlTableTest7([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataContext(context))
			{
				var data = new[]
				{
					new { field1 = 1, field2 = "11" },
					new { field1 = 2, field2 = "22" },
				};

				var xmlData = OracleTools.GetXmlData(conn.MappingSchema, data);

				var list = conn.OracleXmlTable<XmlData>(() => xmlData)
					.Select(t => new { t.Field1, t.Field2 })
					.ToList();

				Assert.That(list.Count, Is.EqualTo(2));
				Assert.That(list[0].Field1, Is.EqualTo(1));
				Assert.That(list[1].Field1, Is.EqualTo(2));
				Assert.That(list[0].Field2, Is.EqualTo("11"));
				Assert.That(list[1].Field2, Is.EqualTo("22"));

				xmlData = "<t><r><c0>1</c0><c1>11</c1></r></t>";

				list = conn.OracleXmlTable<XmlData>(() => xmlData)
					.Select(t => new { t.Field1, t.Field2 })
					.ToList();

				Assert.That(list.Count, Is.EqualTo(1));
				Assert.That(list[0].Field1, Is.EqualTo(1));
				Assert.That(list[0].Field2, Is.EqualTo("11"));
			}
		}

		[Test]
		public void XmlTableTest8([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataContext(context))
			{
				var data = "<t><r><c0>1</c0><c1>11</c1></r></t>";

				var list =
				(
					from p in conn.Parent
					where conn.OracleXmlTable<XmlData>(data).Count(t => t.Field1 == p.ParentID) > 0
					select p
				).ToList();

				Assert.That(list[0].ParentID, Is.EqualTo(1));

				data = "<t><r><c0>2</c0><c1>22</c1></r></t>";

				list =
				(
					from p in conn.Parent
					where conn.OracleXmlTable<XmlData>(data).Count(t => t.Field1 == p.ParentID) > 0
					select p
				).ToList();

				Assert.That(list[0].ParentID, Is.EqualTo(2));
			}
		}

		[Test]
		public void XmlTableTest9([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var conn = GetDataContext(context))
			{
				var data = "<t><r><c0>1</c0><c1>11</c1></r></t>";

				var list =
				(
					from p in conn.Parent
					where conn.OracleXmlTable<XmlData>(() => data).Count(t => t.Field1 == p.ParentID) > 0
					select p
				).ToList();

				Assert.That(list[0].ParentID, Is.EqualTo(1));

				data = "<t><r><c0>2</c0><c1>22</c1></r></t>";

				list =
				(
					from p in conn.Parent
					where conn.OracleXmlTable<XmlData>(() => data).Count(t => t.Field1 == p.ParentID) > 0
					select p
				).ToList();

				Assert.That(list[0].ParentID, Is.EqualTo(2));
			}
		}

#endregion

		[Test]
		public void TestOrderByFirst1([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var q =
					from x in db.Parent
					where x.Value1 == 1
					orderby x.ParentID descending
					select x;

				var row = q.First();

				var start = 0;
				var n     = 0;

				while ((start = db.LastQuery!.IndexOf("FROM", start) + 1) > 0)
					n++;

				Assert.That(n, Is.EqualTo(context.Contains("11") ? 2 : 1));
			}
		}

		[Test]
		public void TestOrderByFirst2([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var q =
					from x in db.Parent
					where x.Value1 == 1
					select x;

				var row = q.First();

				var start = 0;
				var n     = 0;

				while ((start = db.LastQuery!.IndexOf("FROM", start) + 1) > 0)
					n++;

				Assert.That(n, Is.EqualTo(1));
			}
		}

		[Test]
		public void TestOrderByFirst3([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var q =
					from x in db.Parent
					where x.Value1 == 1
					orderby x.ParentID descending
					select x;

				var row = q.Skip(1).First();

				var start = 0;
				var n     = 0;

				while ((start = db.LastQuery!.IndexOf("FROM", start) + 1) > 0)
					n++;

				Assert.That(n, Is.EqualTo(context.Contains("11") ? 3 : 1));
			}
		}

		[Table("DecimalOverflow")]
		sealed class DecimalOverflow
		{
			[Column] public decimal Decimal1;
			[Column] public decimal Decimal2;
			[Column] public decimal Decimal3;
		}

		internal sealed class TestOracleDataProvider : OracleDataProvider
		{
			public TestOracleDataProvider(string providerName, OracleProvider provider, OracleVersion version)
				: base(providerName, provider, version)
			{
			}
		}

		[Test]
		public void OverflowTest([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			OracleDataProvider provider;

			using (var db = GetDataConnection(context))
			{
				provider = new TestOracleDataProvider(db.DataProvider.Name, ((OracleDataProvider)db.DataProvider).Provider, ((OracleDataProvider)db.DataProvider).Version);
			}

			provider.ReaderExpressions[new ReaderInfo { FieldType = typeof(decimal) }] = (Expression<Func<DbDataReader, int, decimal>>)((r,i) => GetDecimal(r, i));

			using (var db = new DataConnection(provider, DataConnection.GetConnectionString(context)))
			{
				var list = db.GetTable<DecimalOverflow>().ToList();
			}
		}

		const int ClrPrecision = 29;

		[ColumnReader(1)]
		static decimal GetDecimal(DbDataReader rd, int idx)
		{
			if (rd is Oracle.ManagedDataAccess.Client.OracleDataReader reader)
			{
				var value  = reader.GetOracleDecimal(idx);
				var newval = OracleDecimal.SetPrecision(value, value > 0 ? ClrPrecision : (ClrPrecision - 1));

				return newval.Value;
			}
			else if (rd is DA.OracleDataReader daReader)
			{
				return (decimal)daReader.GetOracleNumber(idx);
			}
			else
			{
				var value  = ((OracleDataReader)rd).GetOracleDecimal(idx);
				var newval = OracleDecimal.SetPrecision(value, value > 0 ? ClrPrecision : (ClrPrecision - 1));

				return newval.Value;
			}
		}

		[Table("DecimalOverflow")]
		sealed class DecimalOverflow2
		{
			[Column] public OracleDecimal Decimal1;
			[Column] public OracleDecimal Decimal2;
			[Column] public OracleDecimal Decimal3;
		}

		[Test]
		public void OverflowTest2([IncludeDataSources(TestProvName.AllOracleManaged, TestProvName.AllOracleDevart)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var list = db.GetTable<DecimalOverflow2>().ToList();
			}
		}

		public class UseAlternativeBulkCopy
		{
			public int Id;
			public int Value;

			public override int GetHashCode()
			{
				return Id;
			}

			public override bool Equals(object? obj)
			{
				return obj is UseAlternativeBulkCopy e
					&& e.Id == Id && e.Value == Value;
			}
		}

		[Test]
		public void UseAlternativeBulkCopyInsertIntoTest([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var data = new List<UseAlternativeBulkCopy>(100);
			for (var i = 0; i < 100; i++)
				data.Add(new UseAlternativeBulkCopy() { Id = i, Value = i });

			using var _ = new OracleAlternativeBulkCopyMode(AlternativeBulkCopy.InsertInto);
			using var db = GetDataConnection(context);
			using var t = db.CreateLocalTable<UseAlternativeBulkCopy>();
			db.BulkCopy(25, data);

			var selected = db.GetTable<UseAlternativeBulkCopy>().ToList();
			AreEqual(data, selected);
		}

		[Test]
		public async Task UseAlternativeBulkCopyInsertIntoTestAsync([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var data = new List<UseAlternativeBulkCopy>(100);
			for (var i = 0; i < 100; i++)
				data.Add(new UseAlternativeBulkCopy() { Id = i, Value = i });

			using (new OracleAlternativeBulkCopyMode(AlternativeBulkCopy.InsertInto))
			using (var db = GetDataConnection(context))
			{
				await db.CreateTableAsync<UseAlternativeBulkCopy>();
				try
				{
					await db.BulkCopyAsync(25, data);

					var selected = await db.GetTable<UseAlternativeBulkCopy>().ToListAsync();
					AreEqual(data, selected);
				}
				finally
				{
					await db.DropTableAsync<UseAlternativeBulkCopy>();
				}
			}
		}

		[Test]
		public void UseAlternativeBulkCopyInsertDualTest([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var data = new List<UseAlternativeBulkCopy>(100);
			for (var i = 0; i < 100; i++)
				data.Add(new UseAlternativeBulkCopy() { Id = i, Value = i });

			using (new OracleAlternativeBulkCopyMode(AlternativeBulkCopy.InsertDual))
			using (var db = GetDataConnection(context))
			{
				db.CreateTable<UseAlternativeBulkCopy>();
				try
				{
					db.BulkCopy(25, data);

					var selected = db.GetTable<UseAlternativeBulkCopy>().ToList();
					AreEqual(data, selected);
				}
				finally
				{
					db.DropTable<UseAlternativeBulkCopy>();
				}
			}
		}

		[Test]
		public async Task UseAlternativeBulkCopyInsertDualTestAsync([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var data = new List<UseAlternativeBulkCopy>(100);
			for (var i = 0; i < 100; i++)
				data.Add(new UseAlternativeBulkCopy() { Id = i, Value = i });

			using (new OracleAlternativeBulkCopyMode(AlternativeBulkCopy.InsertDual))
			using (var db = GetDataConnection(context))
			{
				await db.CreateTableAsync<UseAlternativeBulkCopy>();
				try
				{
					await db.BulkCopyAsync(25, data);

					var selected = await db.GetTable<UseAlternativeBulkCopy>().ToListAsync();
					AreEqual(data, selected);
				}
				finally
				{
					await db.DropTableAsync<UseAlternativeBulkCopy>();
				}
			}
		}

		public class ClobEntity
		{
			public ClobEntity()
			{ }

			public ClobEntity(int id)
			{
				Id         = id;
				ClobValue  = "Clob" .PadRight(4001, id.ToString()[0]);
				NClobValue = "NClob".PadRight(4001, id.ToString()[0]);
			}
			public int Id;

			[Column(DataType = DataType.Text)]
			public string? ClobValue;

			[Column(DataType = DataType.NText)]
			public string? NClobValue;

			public override int GetHashCode()
			{
				return Id;
			}

			public override bool Equals(object? obj)
			{
				return obj is ClobEntity clob
					   && clob.Id         == Id
					   && clob.ClobValue  == ClobValue
					   && clob.NClobValue == NClobValue;
			}
		}

		[Test]
		public void ClobTest1([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				try
				{
					db.CreateTable<ClobEntity>();
					var obj = new ClobEntity(1);
					db.Insert(obj);

					var selected = db.GetTable<ClobEntity>().First(_ => _.Id == 1);
					Assert.AreEqual(obj, selected);
				}
				finally
				{
					db.DropTable<ClobEntity>();
				}
			}
		}

		[Test]
		public void ClobBulkCopyTest(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
		{
			var data = new List<ClobEntity>(new[] { new ClobEntity(1), new ClobEntity(2) });

			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
			using (var db = GetDataConnection(context))
			{
				try
				{
					db.CreateTable<ClobEntity>();
					db.BulkCopy(data);

					var selected = db.GetTable<ClobEntity>().ToList();
					AreEqual(data, selected);
				}
				finally
				{
					db.DropTable<ClobEntity>();
				}

			}
		}

		[Test]
		public async Task ClobBulkCopyTestAsync(
			[IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values]                                     AlternativeBulkCopy useAlternativeBulkCopy)
		{
			var data = new List<ClobEntity>(new[] { new ClobEntity(1), new ClobEntity(2) });

			using (new OracleAlternativeBulkCopyMode(useAlternativeBulkCopy))
			using (var db = GetDataConnection(context))
			{
				try
				{
					await db.CreateTableAsync<ClobEntity>();
					await db.BulkCopyAsync(data);

					var selected = await db.GetTable<ClobEntity>().ToListAsync();
					AreEqual(data, selected);
				}
				finally
				{
					await db.DropTableAsync<ClobEntity>();
				}

			}
		}

		[Table(IsColumnAttributeRequired = false)]
		public class DateTimeOffsetTable
		{
			public DateTimeOffset DateTimeOffsetValue;
		}

		[ActiveIssue(Configuration = TestProvName.Oracle21DevartDirect)]
		[Test]
		public void Issue515Test([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var now = new DateTimeOffset(2000, 1, 1, 10, 11, 12, TimeSpan.FromHours(5));
					db.CreateTable<DateTimeOffsetTable>();
					db.Insert(new DateTimeOffsetTable() {DateTimeOffsetValue = now});
					Assert.AreEqual(now, db.GetTable<DateTimeOffsetTable>().Select(_ => _.DateTimeOffsetValue).Single());
				}
				finally
				{
					db.DropTable<DateTimeOffsetTable>();
				}
			}
		}

		[ActiveIssue(Configuration = TestProvName.Oracle21DevartDirect)]
		[Test]
		public void Issue612Test([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					// initialize with ticks with default oracle timestamp presicion (6 fractional seconds)
					var expected = new DateTimeOffset(636264847785126550, TimeSpan.FromHours(3));

					db.CreateTable<DateTimeOffsetTable>();

					db.Insert(new DateTimeOffsetTable { DateTimeOffsetValue = expected });

					var actual = db.GetTable<DateTimeOffsetTable>().Select(x => x.DateTimeOffsetValue).Single();

					Assert.That(actual, Is.EqualTo(expected));
				}
				finally
				{
					db.DropTable<DateTimeOffsetTable>();
				}
			}

		}

		[ActiveIssue(Configuration = TestProvName.Oracle21DevartDirect)]
		[Test]
		public void Issue612TestDefaultTSTZPrecisonCanDiffersOfUpTo9Ticks([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					// initialize with ticks with default DateTimeOffset presicion (7 fractional seconds for Oracle TSTZ)
					var expected = new DateTimeOffset(636264847785126559, TimeSpan.FromHours(3));

					db.CreateTable<DateTimeOffsetTable>();

					db.Insert(new DateTimeOffsetTable { DateTimeOffsetValue = expected });

					var actual = db.GetTable<DateTimeOffsetTable>().Select(x => x.DateTimeOffsetValue).Single();

					Assert.That(actual, Is.EqualTo(expected).Within(9).Ticks);
				}
				finally
				{
					db.DropTable<DateTimeOffsetTable>();
				}
			}

		}

		public static IEnumerable<Person> PersonSelectByKey(DataConnection dataConnection, int id)
		{
			return dataConnection.QueryProc<Person>("PERSON_SELECTBYKEY",
				new DataParameter("pID", @id),
				new DataParameter { Name = "retCursor", DataType = DataType.Cursor, Direction = ParameterDirection.ReturnValue });
		}

		[Test]
		public void PersonSelectByKey([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				AreEqual(Person.Where(_ => _.ID == 1), PersonSelectByKey(db, 1));
			}
		}

		[Table(Name = "AllTypes")]
		public partial class ALLTYPE2
		{
			[Column                          , PrimaryKey, Identity] public decimal ID             { get; set; } // NUMBER
			[Column                          ,             Nullable] public byte[]? binaryDataType { get; set; } // BLOB
			[Column                          ,             Nullable] public byte[]? bfileDataType  { get; set; } // BFILE
			[Column(DataType = DataType.Guid),             Nullable] public byte[]? guidDataType   { get; set; } // RAW(16)
		}

		[Test]
		public void Issue539([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			ResetAllTypesIdentity(context);

			using (var db = GetDataContext(context))
			{
				var n = 0;
				try
				{
					var val = new byte[] { 1, 2, 3 };

					n = Convert.ToInt32(db.GetTable<ALLTYPE2>()
						.InsertWithIdentity(() => new ALLTYPE2 { ID = 1000, binaryDataType = val, guidDataType = val }));

					var qry = db.GetTable<ALLTYPE2>().Where(_ => _.ID == 1000 && _.guidDataType == val);

					var data = db.GetTable<ALLTYPE2>()
						.Where(_ => _.ID == n)
						.Select(_ => new
						{
							_.binaryDataType,
							Count = qry.Count()
						}).First();

					AreEqual(val, data.binaryDataType!);

				}
				finally
				{
					db.GetTable<ALLTYPE2>().Delete(_ => _.ID == n);
				}
			}
		}

		[Table("ISSUE723TABLE")]
		public class Issue723Table
		{
			[PrimaryKey, Identity]
			public int Id;

			public string? StringValue;
		}

		// if this test fails with "ORA-12828: Can't start parallel transaction at a remote site"
		// for devart provider in oci mode, add this to connection string:
		// Enlist=false;Transaction Scope Local=true;
		[Test]
		public void Issue723Test1([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			// v12 fix: ORA-65096: invalid common user or role name
			// http://www.dba-oracle.com/t_ora_65096_create_user_12c_without_c_prefix.htm

			var ms = new MappingSchema();
			using (var db = (DataConnection)GetDataContext(context, ms))
			{
				var currentUser = db.Execute<string>("SELECT user FROM dual");
				db.Execute("GRANT CREATE ANY TRIGGER TO " + currentUser);
				db.Execute("GRANT CREATE ANY SEQUENCE TO " + currentUser);
				db.Execute("GRANT DROP ANY TRIGGER TO " + currentUser);
				db.Execute("GRANT DROP ANY SEQUENCE TO " + currentUser);

				try {db.Execute("DROP USER C##ISSUE723SCHEMA CASCADE");} catch { }

				db.Execute("CREATE USER C##ISSUE723SCHEMA IDENTIFIED BY password");

				try
				{

					var tableSpace = db.Execute<string>("SELECT default_tablespace FROM sys.dba_users WHERE username = 'C##ISSUE723SCHEMA'");
					db.Execute($"ALTER USER C##ISSUE723SCHEMA quota unlimited on {tableSpace}");

					db.CreateTable<Issue723Table>(schemaName: "C##ISSUE723SCHEMA");
					Assert.That(db.LastQuery!.Contains("C##ISSUE723SCHEMA.ISSUE723TABLE"));

					try
					{

						db.MappingSchema.GetFluentMappingBuilder()
							.Entity<Issue723Table>()
							.HasSchemaName("C##ISSUE723SCHEMA");

						for (var i = 1; i < 3; i++)
						{
							var id = Convert.ToInt32(db.InsertWithIdentity(new Issue723Table() { StringValue = i.ToString() }));
							Assert.AreEqual(i, id);
						}
						Assert.That(db.LastQuery.Contains("C##ISSUE723SCHEMA.ISSUE723TABLE"));
					}
					finally
					{
						db.DropTable<Issue723Table>(schemaName: "C##ISSUE723SCHEMA");
					}
				}
				finally
				{
					db.Execute("DROP USER C##ISSUE723SCHEMA CASCADE");
				}
			}
		}

		[Test]
		public void Issue723Test2([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Issue723Table>())
			{
				Assert.True(true);
			}
		}

		public class Issue731Table
		{
			public int     Id;
			public Guid    Guid;
			[Column(DataType = DataType.Binary)]
			public Guid    BinaryGuid;
			public byte[]? BlobValue;
			[Column(Length = 5)]
			public byte[]? RawValue;

		}

		[Test]
		public void Issue731Test([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Issue731Table>())
			{
				var origin = new Issue731Table()
				{
					Id         = 1,
					Guid       = TestData.Guid1,
					BinaryGuid = TestData.Guid2,
					BlobValue  = new byte[] { 1, 2, 3 },
					RawValue   = new byte[] { 4, 5, 6 }
				};

				db.Insert(origin);

				var result = db.GetTable<Issue731Table>().First(_ => _.Id == 1);

				Assert.AreEqual(origin.Id,         result.Id);
				Assert.AreEqual(origin.Guid,       result.Guid);
				Assert.AreEqual(origin.BinaryGuid, result.BinaryGuid);
				Assert.AreEqual(origin.BlobValue,  result.BlobValue);
				Assert.AreEqual(origin.RawValue,   result.RawValue);
			}
		}

		sealed class MyDate
		{
			public int    Year;
			public int    Month;
			public int    Day;
			public int    Hour;
			public int    Minute;
			public int    Second;
			public int    Nanosecond;
			public string TimeZone = null!;
		}

		static MyDate OracleTimeStampTZToMyDate(OracleTimeStampTZ tz)
		{
			return new MyDate
			{
				Year       = tz.Year,
				Month      = tz.Month,
				Day        = tz.Day,
				Hour       = tz.Hour,
				Minute     = tz.Minute,
				Second     = tz.Second,
				Nanosecond = tz.Nanosecond,
				TimeZone   = tz.TimeZone,
			};
		}

		static OracleTimeStampTZ MyDateToOracleTimeStampTZ(MyDate dt)
		{
			return dt == null ?
				OracleTimeStampTZ.Null :
				new OracleTimeStampTZ(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Nanosecond, dt.TimeZone);
		}

		[Table("AllTypes")]
		sealed class MappingTest
		{
			[Column] public int    ID;
			[Column("datetimeoffsetDataType")] public MyDate? MyDate;
		}

		[Test]
		public void CustomMappingNonstandardTypeTest([IncludeDataSources(TestProvName.AllOracleManaged)] string context)
		{
			var ms = new MappingSchema();
			var dataProvider = (DataProviderBase)DataConnection.GetDataProvider(context);

			// Expression to read column value from data reader.
			//
			dataProvider.ReaderExpressions[new ReaderInfo
			{
				ToType            = typeof(MyDate),
				ProviderFieldType = typeof(OracleTimeStampTZ),
			}] = (Expression<Func<OracleDataReader,int,MyDate>>)((rd, idx) => OracleTimeStampTZToMyDate(rd.GetOracleTimeStampTZ(idx)));

			// Converts object property value to data reader parameter.
			//
			ms.SetConverter<MyDate,DataParameter>(
				dt => new DataParameter { Value = MyDateToOracleTimeStampTZ(dt) });

			// Converts object property value to SQL.
			//
			ms.SetValueToSqlConverter(typeof(MyDate), (sb,tp,v) =>
			{
				if (!(v is MyDate value)) sb.Append("NULL");
				else sb.Append($"DATE '{value.Year}-{value.Month}-{value.Day}'");
			});

			// Converts object property value to SQL.
			//
			ms.SetValueToSqlConverter(typeof(OracleTimeStampTZ), (sb,tp,v) =>
			{
				var value = (OracleTimeStampTZ)v;
				if (value.IsNull) sb.Append("NULL");
				else              sb.Append($"DATE '{value.Year}-{value.Month}-{value.Day}'");
			});

			// Maps OracleTimeStampTZ to MyDate and the other way around.
			//
			ms.SetConverter<OracleTimeStampTZ,MyDate>(OracleTimeStampTZToMyDate);
			ms.SetConverter<MyDate,OracleTimeStampTZ>(MyDateToOracleTimeStampTZ);

			using (var db = GetDataContext(context, ms))
			{
				var table = db.GetTable<MappingTest>();
				var list  = table.ToList();

				table.Update(
					mt => mt.ID == list[0].ID,
					mt => new MappingTest
					{
						MyDate = list[0].MyDate
					});

				db.InlineParameters = true;

				table.Update(
					mt => mt.ID == list[0].ID,
					mt => new MappingTest
					{
						MyDate = list[0].MyDate
					});
			}
		}

		static MyDate OracleTimeStampToMyDate(DA.OracleTimeStamp tz)
		{
			return new MyDate
			{
				Year       = tz.Year,
				Month      = tz.Month,
				Day        = tz.Day,
				Hour       = tz.Hour,
				Minute     = tz.Minute,
				Second     = tz.Second,
				Nanosecond = tz.Nanosecond,
				TimeZone   = tz.TimeZone,
			};
		}

		static DA.OracleTimeStamp MyDateToOracleTimeStamp(MyDate dt)
		{
			return dt == null ?
				DA.OracleTimeStamp.Null :
				new DA.OracleTimeStamp(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Nanosecond, dt.TimeZone);
		}

		[Test]
		public void CustomMappingNonstandardTypeTestDevart([IncludeDataSources(TestProvName.AllOracleDevart)] string context)
		{
			var ms = new MappingSchema();
			var dataProvider = (DataProviderBase)DataConnection.GetDataProvider(context);

			// Expression to read column value from data reader.
			//
			dataProvider.ReaderExpressions[new ReaderInfo
			{
				ToType            = typeof(MyDate),
				ProviderFieldType = typeof(DA.OracleTimeStamp),
			}] = (Expression<Func<DA.OracleDataReader,int,MyDate>>)((rd, idx) => OracleTimeStampToMyDate(rd.GetOracleTimeStamp(idx)));

			// Converts object property value to data reader parameter.
			//
			ms.SetConverter<MyDate,DataParameter>(
				dt => new DataParameter { Value = MyDateToOracleTimeStamp(dt) });

			// Converts object property value to SQL.
			//
			ms.SetValueToSqlConverter(typeof(MyDate), (sb,tp,v) =>
			{
				if (!(v is MyDate value)) sb.Append("NULL");
				else sb.Append($"DATE '{value.Year}-{value.Month}-{value.Day}'");
			});

			// Converts object property value to SQL.
			//
			ms.SetValueToSqlConverter(typeof(DA.OracleTimeStamp), (sb,tp,v) =>
			{
				var value = (DA.OracleTimeStamp)v;
				if (value.IsNull) sb.Append("NULL");
				else              sb.Append($"DATE '{value.Year}-{value.Month}-{value.Day}'");
			});

			// Maps OracleTimeStamp to MyDate and the other way around.
			//
			ms.SetConverter<DA.OracleTimeStamp, MyDate>(OracleTimeStampToMyDate);
			ms.SetConverter<MyDate, DA.OracleTimeStamp>(MyDateToOracleTimeStamp);

			using (var db = GetDataContext(context, ms))
			{
				var table = db.GetTable<MappingTest>();
				var list  = table.ToList();

				table.Update(
					mt => mt.ID == list[0].ID,
					mt => new MappingTest
					{
						MyDate = list[0].MyDate
					});

				db.InlineParameters = true;

				table.Update(
					mt => mt.ID == list[0].ID,
					mt => new MappingTest
					{
						MyDate = list[0].MyDate
					});
			}
		}

		sealed class BooleanMapping
		{
			private sealed class EqualityComparer : IEqualityComparer<BooleanMapping>
			{
				public bool Equals(BooleanMapping? x, BooleanMapping? y)
				{
					if (ReferenceEquals(x, y)) return true;
					if (ReferenceEquals(x, null)) return false;
					if (ReferenceEquals(y, null)) return false;
					if (x.GetType() != y.GetType()) return false;
					return x.Id == y.Id && x.BoolProp == y.BoolProp && x.NullableBoolProp == y.NullableBoolProp;
				}

				public int GetHashCode(BooleanMapping obj)
				{
					unchecked
					{
						var hashCode = obj.Id;
						hashCode = (hashCode * 397) ^ obj.BoolProp.GetHashCode();
						hashCode = (hashCode * 397) ^ obj.NullableBoolProp.GetHashCode();
						return hashCode;
					}
				}
			}

			public static IEqualityComparer<BooleanMapping> Comparer { get; } = new EqualityComparer();

			[PrimaryKey]
			public int Id { get; set; }
			[Column]
			public bool BoolProp { get; set; }
			[Column]
			public bool? NullableBoolProp { get; set; }
		}

		[Test]
		public void BooleanMappingTests([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var ms = new MappingSchema();

			ms.SetConvertExpression<bool?, DataParameter>(_ =>
				_ != null
					? DataParameter.Char(null, _.HasValue && _.Value ? 'Y' : 'N')
					: new DataParameter(null, DBNull.Value));

			var testData = new[]
			{
				new BooleanMapping { Id = 1, BoolProp = true,  NullableBoolProp = true  },
				new BooleanMapping { Id = 2, BoolProp = false, NullableBoolProp = false },
				new BooleanMapping { Id = 3, BoolProp = true,  NullableBoolProp = null  }
			};

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<BooleanMapping>())
			{
				table.BulkCopy(testData);
				var values = table.ToArray();

				AreEqual(testData, values, BooleanMapping.Comparer);
			}
		}

		[Test]
		public async Task BooleanMappingTestsAsync([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			var ms = new MappingSchema();

			ms.SetConvertExpression<bool?, DataParameter>(_ =>
				_ != null
					? DataParameter.Char(null, _.HasValue && _.Value ? 'Y' : 'N')
					: new DataParameter(null, DBNull.Value));

			var testData = new[]
			{
				new BooleanMapping { Id = 1, BoolProp = true,  NullableBoolProp = true  },
				new BooleanMapping { Id = 2, BoolProp = false, NullableBoolProp = false },
				new BooleanMapping { Id = 3, BoolProp = true,  NullableBoolProp = null  }
			};

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<BooleanMapping>())
			{
				await table.BulkCopyAsync(testData);
				var values = await table.ToArrayAsync();

				AreEqual(testData, values, BooleanMapping.Comparer);
			}
		}

		[Table("BinaryData")]
		public class TestIdentifiersTable1
		{
			[Column]
			public int BinaryDataID { get; set; }
		}

		[Table("BINARYDATA")]
		public class TestIdentifiersTable2
		{
			[Column("BINARYDATAID")]
			public int BinaryDataID { get; set; }
		}

		[Test]
		public void TestLowercaseIdentifiersQuotation([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var initial = OracleTools.DontEscapeLowercaseIdentifiers;
				try
				{
					OracleTools.DontEscapeLowercaseIdentifiers = true;
					db.GetTable<TestIdentifiersTable1>().ToList();
					db.GetTable<TestIdentifiersTable2>().ToList();

					Query.ClearCaches();
					OracleTools.DontEscapeLowercaseIdentifiers = false;

					// no specific exception type as it differ for managed and native providers
					Assert.That(() => db.GetTable<TestIdentifiersTable1>().ToList(), Throws.Exception.With.Message.Contains("ORA-00942"));

					db.GetTable<TestIdentifiersTable2>().ToList();
				}
				finally
				{
					OracleTools.DontEscapeLowercaseIdentifiers = initial;
					Query.ClearCaches();
				}
			}
		}

		// this is a sad test which shows that we need better support for parameter value bindings
		[ActiveIssue(Configuration = TestProvName.Oracle21DevartDirect)]
		[Test]
		public void ProcedureOutParameters([IncludeDataSources(false, TestProvName.AllOracle)] string context)
		{
			var isNative = context.IsAnyOf(TestProvName.AllOracleNative);
			// Devart generate OCI-21500 error for xml parameter and I currently have no idea how to fix it
			var skipXml = context.IsAnyOf(TestProvName.AllOracleDevartOCI);
			// ORA-22922: nonexistent LOB value
			var skipClob = context.IsAnyOf(TestProvName.AllOracleDevartDirect);
			// binary_ types handling broken too
			var borkenBinaryFloats = context.IsAnyOf(TestProvName.AllOracleDevart);

			using (var db = (DataConnection)GetDataContext(context))
			{
				var pms = new[]
				{
					new DataParameter {Name = "ID"                    , Direction = ParameterDirection.InputOutput, DataType = DataType.Decimal,        Value = 1},

					new DataParameter {Name = "bigintDataType"        , Direction = ParameterDirection.InputOutput, DataType = DataType.Decimal,        Value = 1},
					new DataParameter {Name = "numericDataType"       , Direction = ParameterDirection.InputOutput, DataType = DataType.Decimal,        Value = 1},
					new DataParameter {Name = "bitDataType"           , Direction = ParameterDirection.InputOutput, DataType = DataType.Decimal,        Value = 1},
					new DataParameter {Name = "smallintDataType"      , Direction = ParameterDirection.InputOutput, DataType = DataType.Decimal,        Value = 1},
					new DataParameter {Name = "decimalDataType"       , Direction = ParameterDirection.InputOutput, DataType = DataType.Decimal,        Value = 1},
					new DataParameter {Name = "smallmoneyDataType"    , Direction = ParameterDirection.InputOutput, DataType = DataType.Decimal,        Value = 1},
					new DataParameter {Name = "intDataType"           , Direction = ParameterDirection.InputOutput, DataType = DataType.Decimal,        Value = 1},
					new DataParameter {Name = "tinyintDataType"       , Direction = ParameterDirection.InputOutput, DataType = DataType.Decimal,        Value = 1},
					new DataParameter {Name = "moneyDataType"         , Direction = ParameterDirection.InputOutput, DataType = DataType.Decimal,        Value = 1},
					new DataParameter {Name = "floatDataType"         , Direction = ParameterDirection.InputOutput, DataType = DataType.Double,         Value = 1},
					new DataParameter {Name = "realDataType"          , Direction = ParameterDirection.InputOutput, DataType = DataType.Single,         Value = 1},

					new DataParameter {Name = "datetimeDataType"      , Direction = ParameterDirection.InputOutput, DataType = DataType.DateTime,       Value = TestData.DateTime},
					new DataParameter {Name = "datetime2DataType"     , Direction = ParameterDirection.InputOutput, DataType = DataType.DateTime2,      Value = TestData.DateTime},
					new DataParameter {Name = "datetimeoffsetDataType", Direction = ParameterDirection.InputOutput, DataType = DataType.DateTimeOffset, Value = TestData.DateTimeOffset},
					new DataParameter {Name = "localZoneDataType"     , Direction = ParameterDirection.InputOutput, DataType = DataType.DateTimeOffset, Value = TestData.DateTimeOffset},

					new DataParameter {Name = "charDataType"          , Direction = ParameterDirection.InputOutput, DataType = DataType.Char,           Value = 'A'},
					new DataParameter {Name = "char20DataType"        , Direction = ParameterDirection.InputOutput, DataType = DataType.Char,           Value = 'B'},
					new DataParameter {Name = "varcharDataType"       , Direction = ParameterDirection.InputOutput, DataType = DataType.VarChar,        Value = "VarChar"},
					new DataParameter {Name = "textDataType"          , Direction = ParameterDirection.InputOutput, DataType = DataType.Text,           Value = skipClob ? null : "Text"},
					new DataParameter {Name = "ncharDataType"         , Direction = ParameterDirection.InputOutput, DataType = DataType.NChar,          Value = "NChar"},
					new DataParameter {Name = "nvarcharDataType"      , Direction = ParameterDirection.InputOutput, DataType = DataType.NVarChar,       Value = "NVarChar"},
					new DataParameter {Name = "ntextDataType"         , Direction = ParameterDirection.InputOutput, DataType = DataType.NText,          Value = skipClob ? null : "NText"},

					new DataParameter {Name = "binaryDataType"        , Direction = ParameterDirection.InputOutput, DataType = DataType.Blob,           Value = new byte []{ 1,2,3 }},

					new DataParameter {Name = "bfileDataType"         , Direction = ParameterDirection.InputOutput, DataType = DataType.BFile,          Value = new byte []{ 1,2,3 }},

					new DataParameter {Name = "guidDataType"          , Direction = ParameterDirection.InputOutput, DataType = DataType.Guid,           Value = TestData.Guid1},

					new DataParameter {Name = "xmlDataType"           , Direction = ParameterDirection.InputOutput, DataType = DataType.Xml,            Value = skipXml ? null : "<?xml version=\"1.0\" encoding=\"UTF-8\"?><test>hi</test>"},
				};

				db.ExecuteProc("ALLOUTPUTPARAMETERS", pms);

				// assert types converted
				Assert.AreEqual(typeof(decimal)       , pms[0] .Value!.GetType());
				Assert.AreEqual(typeof(decimal)       , pms[1] .Value!.GetType());
				Assert.AreEqual(typeof(decimal)       , pms[2] .Value!.GetType());
				Assert.AreEqual(typeof(decimal)       , pms[3] .Value!.GetType());
				Assert.AreEqual(typeof(decimal)       , pms[4] .Value!.GetType());
				Assert.AreEqual(typeof(decimal)       , pms[5] .Value!.GetType());
				Assert.AreEqual(typeof(decimal)       , pms[6] .Value!.GetType());
				Assert.AreEqual(typeof(decimal)       , pms[7] .Value!.GetType());
				Assert.AreEqual(typeof(decimal)       , pms[8] .Value!.GetType());
				Assert.AreEqual(typeof(decimal)       , pms[9] .Value!.GetType());
				Assert.AreEqual(borkenBinaryFloats ? typeof(int) : typeof(decimal), pms[10].Value!.GetType());
				Assert.AreEqual(borkenBinaryFloats ? typeof(int) : typeof(decimal), pms[11].Value!.GetType());
				Assert.AreEqual(typeof(DateTime)      , pms[12].Value!.GetType());
				Assert.AreEqual(typeof(DateTime)      , pms[13].Value!.GetType());
				Assert.That(pms[14].Value, Is.TypeOf<DateTime>().Or.TypeOf<DateTimeOffset>());
				Assert.That(pms[15].Value, Is.TypeOf<DateTime>().Or.TypeOf<DateTimeOffset>());
				Assert.AreEqual(typeof(string)        , pms[16].Value!.GetType());
				// [17] is char20 which is not set now for some reason
				Assert.AreEqual(typeof(string)        , pms[18].Value!.GetType());
				Assert.AreEqual(typeof(string)        , pms[19].Value!.GetType());
				Assert.AreEqual(typeof(string)        , pms[20].Value!.GetType());
				Assert.AreEqual(typeof(string)        , pms[21].Value!.GetType());
				Assert.AreEqual(typeof(string)        , pms[22].Value!.GetType());
				Assert.AreEqual(typeof(byte[])        , pms[23].Value!.GetType());
				Assert.That(pms[24].Value, Is.TypeOf<byte[]>().Or.TypeOf(((OracleDataProvider)db.DataProvider).Adapter.OracleBFileType));
				Assert.AreEqual(typeof(byte[])        , pms[25].Value!.GetType());
				Assert.AreEqual(typeof(string)        , pms[26].Value!.GetType());

				// assert values
				Assert.AreEqual(2                     , pms[0].Value);
				Assert.AreEqual(1000000               , pms[1].Value);
				Assert.AreEqual(9999999               , pms[2].Value);
				Assert.AreEqual(1                     , pms[3].Value);
				Assert.AreEqual(25555                 , pms[4].Value);
				Assert.AreEqual(2222222               , pms[5].Value);
				Assert.AreEqual(100000                , pms[6].Value);
				Assert.AreEqual(7777777               , pms[7].Value);
				Assert.AreEqual(100                   , pms[8].Value);
				Assert.AreEqual(100000                , pms[9].Value);
				Assert.AreEqual(borkenBinaryFloats ? 20 : 20.31, pms[10].Value);
				Assert.AreEqual(borkenBinaryFloats ? 16 : 16.2 , pms[11].Value);
				Assert.AreEqual(new DateTime(2012, 12, 12, 12, 12, 12), pms[12].Value);
				Assert.AreEqual(new DateTime(2012, 12, 12, 12, 12, 12, 12), pms[13].Value);
				if (context.IsAnyOf(TestProvName.AllOracleDevart))
					Assert.AreEqual(new DateTime(2012, 12, 12, 12, 12, 12, 12), pms[14].Value);
				else
					Assert.AreEqual(new DateTimeOffset(2012, 12, 12, 12, 12, 12, isNative ? 0 : 12, TimeSpan.FromHours(-5)), pms[14].Value);

				if (context.IsAnyOf(TestProvName.AllOracleDevart))
					Assert.AreEqual(new DateTime(2012, 12, 12, 12, 12, 12, 12), pms[15].Value);
				// TODO: fix timezones handling
				//if (!context.IsAnyOf(TestProvName.AllOracleNative))
				else
					Assert.That(pms[15].Value,
						Is.EqualTo(new DateTimeOffset(2012, 12, 12, 11, 12, 12, isNative ? 0 : 12, TimeSpan.Zero)).
						Or.EqualTo(new DateTimeOffset(2012, 12, 12, 11, 12, 12, isNative ? 0 : 12, TestData.DateTimeOffset.Offset)).
						Or.EqualTo(new DateTimeOffset(2012, 12, 12, 12, 12, 12, isNative ? 0 : 12, TestData.DateTimeOffset.Offset.Add(new TimeSpan(-1, 0, 0)))).
						Or.EqualTo(new DateTimeOffset(2012, 12, 12, 12, 12, 12, isNative ? 0 : 12, new TimeSpan(2, 0, 0))).
						Or.EqualTo(new DateTimeOffset(2012, 12, 12, 12, 12, 12, isNative ? 0 : 12, new TimeSpan(-5, 0, 0))));

				if (context.IsAnyOf(TestProvName.AllOracleDevart))
					Assert.AreEqual("1"               , pms[16].Value?.ToString()?.TrimEnd());
				else
					Assert.AreEqual("1"               , pms[16].Value);
				Assert.IsNull(pms[17].Value);
				Assert.AreEqual("234"                 , pms[18].Value);
				Assert.AreEqual("567"                 , pms[19].Value);
				if (context.IsAnyOf(TestProvName.AllOracleDevart))
					Assert.AreEqual("23233"           , pms[20].Value?.ToString()?.TrimEnd());
				else
					Assert.AreEqual("23233"           , pms[20].Value);
				Assert.AreEqual("3323"                , pms[21].Value);
				Assert.AreEqual("111"                 , pms[22].Value);
				if (context.IsAnyOf(TestProvName.AllOracleDevartDirect))
					// this is not correct
					Assert.AreEqual(new byte[] { 1, 2, 3 }, pms[23].Value);
				else
					Assert.AreEqual(new byte[] { 0, 0xAA }, pms[23].Value);

				// default converter for BFile missing intentionally
				var bfile = pms[24].Output!.Value!;
				if (isNative)
				{
#if NET472
					using (var file = (Oracle.DataAccess.Types.OracleBFile)bfile)
					{
						file.OpenFile();
						Assert.AreEqual(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35 }, file.Value);
					}
#endif
				}
				else if (context.IsAnyOf(TestProvName.AllOracleDevart))
					Assert.AreEqual(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35 }, bfile);
				else
				{
					using (var file = (Oracle.ManagedDataAccess.Types.OracleBFile)bfile)
					{
						file.OpenFile();
						Assert.AreEqual(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35 }, file.Value);
					}
				}

				// guid is autogenerated
				Assert.AreEqual(16                    , ((byte[])pms[25].Value!).Length);

				Assert.AreEqual(
					"<root><elementstrattr=\"strvalue\"intattr=\"12345\"/></root>",
					pms[26].Value!.ToString()!.Replace(" ", "").Replace("\n", ""));
			}
		}

		sealed class MyTestDataConnectionInterceptor : CommandInterceptor
		{
			public override void AfterExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, DbDataReader dataReader)
			{
				if (dataReader is OracleDataReader or1 && command is OracleCommand oc1)
				{
					or1.FetchSize = oc1.RowSize * 10000;
				}
			}
		}

		[Test]
		public void OverrideExecuteReaderTest([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.AddInterceptor(new MyTestDataConnectionInterceptor());

				_ = db.Person.ToList();
			}
		}

		[Test]
		public void LongDataTypeTest([IncludeDataSources(false, TestProvName.AllOracle)] string context)
		{
			ResetAllTypesIdentity(context);

			using (var db = GetDataContext(context))
			{
				db.GetTable<AllTypes>()
					.Where(t => t.ID > 2)
					.Delete();

				try
				{
					var items = db.GetTable<AllTypes>()
						.Select(t => new { t.longDataType })
						.ToArray();

					Assert.That(items.Length, Is.GreaterThanOrEqualTo(2));
					Assert.That(items[0].longDataType, Is.Null);
					Assert.That(items[1].longDataType, Is.EqualTo("LONG"));

					var str = new string('A', 10000);

					var id = db.GetTable<AllTypes>().InsertWithDecimalIdentity(() => new AllTypes
					{
						longDataType = str,
					});

					var insertedItems = db.GetTable<AllTypes>()
						.Where(t => t.ID == id)
						.Select(t => new { t.longDataType })
						.ToArray();

					Assert.That(insertedItems[0].longDataType, Is.EqualTo(str));

					var str2 = new string('B', 4000);

					var id2 = db.GetTable<AllTypes>().InsertWithDecimalIdentity(() => new AllTypes
					{
						longDataType = Sql.ToSql(str2),
					});

					var insertedItems2 = db.GetTable<AllTypes>()
						.Where(t => t.ID == id2)
						.Select(t => new { t.longDataType })
						.ToArray();

					Assert.That(insertedItems2[0].longDataType, Is.EqualTo(str2));
				}
				finally
				{
					db.GetTable<AllTypes>()
						.Where(t => t.ID > 2)
						.Delete();
				}
			}
		}

		sealed class LongRawTable
		{
			[Column(Name =  "ID")] public int Id { get; set; }
			[Column(Name = "longRawDataType", DataType=DataType.LongRaw), Nullable] public byte[]? LONGRAWDATATYPE { get; set; } // LONG RAW
		}

		[Test]
		public void LongRawDataTypeTest([IncludeDataSources(false, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<LongRawTable>()
					.Where(t => t.Id > 2)
					.Delete();

				var items = db.GetTable<LongRawTable>()
					.Select(t => new { t.LONGRAWDATATYPE })
					.ToArray();

				Assert.That(items.Length, Is.EqualTo(2));
				Assert.That(items[0].LONGRAWDATATYPE, Is.Null);
				Assert.That(items[1].LONGRAWDATATYPE, Is.Not.Null);

				var bytes1 = Encoding.UTF8.GetBytes(new string('A', 10000));

				db.GetTable<LongRawTable>().Insert(() => new LongRawTable
				{
					Id = 3,
					LONGRAWDATATYPE = bytes1,
				});

				var bytes2 = Encoding.UTF8.GetBytes(new string('B', 10000));

				db.GetTable<LongRawTable>().Insert(() => new LongRawTable
				{
					Id = 4,
					LONGRAWDATATYPE = bytes2,
				});

				var insertedItems = db.GetTable<LongRawTable>()
					.Where(t => t.Id.In(3, 4))
					.Select(t => new { t.LONGRAWDATATYPE })
					.ToArray();

				Assert.That(insertedItems.Length, Is.EqualTo(2));
				Assert.That(insertedItems[0].LONGRAWDATATYPE, Is.EqualTo(bytes1));
				Assert.That(insertedItems[1].LONGRAWDATATYPE, Is.EqualTo(bytes2));
			}
		}

		[Test]
		public void TestUpdateAliases([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = from child in db.Child
					from parent in db.Parent.InnerJoin(parent => parent.ParentID == child.ParentID && parent.ParentID < 5)
					select child;

				var countRecords = query.Count();

				var recordsAffected = query.Set(child => child.ParentID, child => child.ParentID)
					.Update();

				Assert.That(recordsAffected, Is.EqualTo(countRecords));
			}
		}

		[Table("TEST_IDENTITY_SCHEMA")]
		public class ItentityColumnTable
		{
			[Column("ID", IsIdentity = true, DbType = "NUMBER GENERATED BY DEFAULT AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 NOCACHE ORDER NOCYCLE")]
			public int Id { get; set; }

			[Column("NOT_ID")]
			public int NotId { get; set; }
		}

		[Test]
		public void TestIdentityColumnRead([IncludeDataSources(false, TestProvName.AllOracle12Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable<ItentityColumnTable>())
			{
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions()
				{
					GetProcedures = false
				});

				var table = schema.Tables.Single(t => t.TableName == "TEST_IDENTITY_SCHEMA");

				Assert.AreEqual(2, table.Columns.Count);

				var id    = table.Columns.Single(c => c.ColumnName == "ID");
				var notid = table.Columns.Single(c => c.ColumnName == "NOT_ID");

				Assert.True(id    .IsIdentity);
				Assert.False(notid.IsIdentity);
			}
		}

#region DateTime

		[Table("Test0431")]
		public partial class TestDateTimeTypes
		{
			[Column(DataType =  DataType.Date)]                     public DateTime       Date             { get; set; }
			[Column]                                                public DateTime       DateTime         { get; set; }
			[Column(DataType =  DataType.DateTime)]                 public DateTime       DateTime_        { get; set; }
			[Column(DataType =  DataType.DateTime2)]                public DateTime       DateTime2        { get; set; }
			[Column(DataType =  DataType.DateTime2, Precision = 0)] public DateTime       DateTime2_0      { get; set; }
			[Column(DataType =  DataType.DateTime2, Precision = 1)] public DateTime       DateTime2_1      { get; set; }
			[Column(DataType =  DataType.DateTime2, Precision = 9)] public DateTime       DateTime2_9      { get; set; }
			[Column]                                                public DateTimeOffset DateTimeOffset_  { get; set; }
			[Column(Precision = 0)]                                 public DateTimeOffset DateTimeOffset_0 { get; set; }
			[Column(Precision = 1)]                                 public DateTimeOffset DateTimeOffset_1 { get; set; }
			[Column(Precision = 9)]                                 public DateTimeOffset DateTimeOffset_9 { get; set; }

			public static readonly TestDateTimeTypes[] Data = new[]
			{
				new TestDateTimeTypes()
				{
					// for DataType.Date we currently don't trim parameter values of time part
					Date             = new DateTime(2020, 1, 3),
					DateTime         = new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1234),
					DateTime_        = new DateTime(2020, 1, 3, 4, 5, 6),
					DateTime2        = new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1234),
					DateTime2_0      = new DateTime(2020, 1, 3, 4, 5, 6, 189).AddTicks(1234),
					DateTime2_1      = new DateTime(2020, 1, 3, 4, 5, 6, 719).AddTicks(1234),
					DateTime2_9      = new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1234),
					DateTimeOffset_  = new DateTimeOffset(2020, 1, 3, 4, 5, 6, 789, TimeSpan.FromMinutes(45)).AddTicks(1234),
					DateTimeOffset_0 = new DateTimeOffset(2020, 1, 3, 4, 5, 6, 189, TimeSpan.FromMinutes(45)).AddTicks(1234),
					DateTimeOffset_1 = new DateTimeOffset(2020, 1, 3, 4, 5, 6, 719, TimeSpan.FromMinutes(45)).AddTicks(1234),
					DateTimeOffset_9 = new DateTimeOffset(2020, 1, 3, 4, 5, 6, 789, TimeSpan.FromMinutes(45)).AddTicks(1234)
				}
			};
		}

		// ActiveIssue: provider reads TimeStampTZ incorrectly (offset applied twice)
		[ActiveIssue(Configuration = TestProvName.Oracle21DevartDirect)]
		[Test]
		public void TestDateTimeRoundtrip([IncludeDataSources(true, TestProvName.AllOracle)] string context, [Values] bool inlineParameters)
		{
			using (var db    = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestDateTimeTypes>())
			{
				db.Insert(TestDateTimeTypes.Data[0]);

				db.InlineParameters = inlineParameters;

				var pDate               = new DateTime(2020, 1, 3);
				var pDateTime           = new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1234);
				var pDateTimeOffset     = new DateTimeOffset(2020, 1, 3, 4, 5, 6, 789, TimeSpan.FromMinutes(45)).AddTicks(1234);

				var real = table.ToArray();

				var results = table.Where(r => r.Date             == pDate              ).ToArray(); assert();
				results     = table.Where(r => r.DateTime         == pDateTime          ).ToArray(); assert();
				results     = table.Where(r => r.DateTime_        == pDateTime          ).ToArray(); assert();
				results     = table.Where(r => r.DateTime2        == pDateTime          ).ToArray(); assert();
				results     = table.Where(r => r.DateTime2_0      == pDateTime          ).ToArray(); assert();
				results     = table.Where(r => r.DateTime2_1      == pDateTime          ).ToArray(); assert();
				results     = table.Where(r => r.DateTime2_9      == pDateTime          ).ToArray(); assert();
				results     = table.Where(r => r.DateTimeOffset_  == pDateTimeOffset    ).ToArray(); assert();
				results     = table.Where(r => r.DateTimeOffset_0 == pDateTimeOffset    ).ToArray(); assert();
				results     = table.Where(r => r.DateTimeOffset_1 == pDateTimeOffset    ).ToArray(); assert();
				results     = table.Where(r => r.DateTimeOffset_9 == pDateTimeOffset    ).ToArray(); assert();

				void assert()
				{
					Assert.AreEqual(1, results.Length);

					Assert.AreEqual(new DateTime(2020, 1, 3), results[0].Date);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1230), results[0].DateTime);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6), results[0].DateTime_);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1230), results[0].DateTime2);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 0), results[0].DateTime2_0);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 700), results[0].DateTime2_1);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1234), results[0].DateTime2_9);
					Assert.AreEqual(new DateTimeOffset(2020, 1, 3, 4, 5, 6, 789, TimeSpan.FromMinutes(45)).AddTicks(1230), results[0].DateTimeOffset_);
					Assert.AreEqual(new DateTimeOffset(2020, 1, 3, 4, 5, 6, 0, TimeSpan.FromMinutes(45)), results[0].DateTimeOffset_0);
					Assert.AreEqual(new DateTimeOffset(2020, 1, 3, 4, 5, 6, 700, TimeSpan.FromMinutes(45)), results[0].DateTimeOffset_1);
					Assert.AreEqual(new DateTimeOffset(2020, 1, 3, 4, 5, 6, 789, TimeSpan.FromMinutes(45)).AddTicks(1234), results[0].DateTimeOffset_9);
				}
			}
		}

		// ActiveIssue: provider reads TimeStampTZ incorrectly (offset applied twice)
		[ActiveIssue(Configuration = TestProvName.Oracle21DevartDirect)]
		[Test]
		public void TestDateTimeSQL([IncludeDataSources(false, TestProvName.AllOracle)] string context, [Values] bool inlineParameters)
		{
			using (var db    = GetDataConnection(context))
			using (var table = db.CreateLocalTable<TestDateTimeTypes>())
			{
				Assert.True(db.LastQuery!.Contains("\"Date\"             date                        NOT NULL"));
				Assert.True(db.LastQuery.Contains("\"DateTime\"         timestamp                   NOT NULL"));
				Assert.True(db.LastQuery.Contains("\"DateTime_\"        date                        NOT NULL"));
				Assert.True(db.LastQuery.Contains("\"DateTime2\"        timestamp                   NOT NULL"));
				Assert.True(db.LastQuery.Contains("\"DateTime2_0\"      timestamp(0)                NOT NULL"));
				Assert.True(db.LastQuery.Contains("\"DateTime2_1\"      timestamp(1)                NOT NULL"));
				Assert.True(db.LastQuery.Contains("\"DateTime2_9\"      timestamp(9)                NOT NULL"));
				Assert.True(db.LastQuery.Contains("\"DateTimeOffset_\"  timestamp with time zone    NOT NULL"));
				Assert.True(db.LastQuery.Contains("\"DateTimeOffset_0\" timestamp(0) with time zone NOT NULL"));
				Assert.True(db.LastQuery.Contains("\"DateTimeOffset_1\" timestamp(1) with time zone NOT NULL"));
				Assert.True(db.LastQuery.Contains("\"DateTimeOffset_9\" timestamp(9) with time zone NOT NULL"));

				db.Insert(TestDateTimeTypes.Data[0]);

				db.InlineParameters = inlineParameters;

				var pDate           = new DateTime(2020, 1, 3);
				var pDateTime       = new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1234);
				var pDateTimeOffset = new DateTimeOffset(2020, 1, 3, 4, 5, 6, 789, TimeSpan.FromMinutes(45)).AddTicks(1234);

				var results = table.Where(r => r.Date == pDate).ToArray();
				assert("DATE '2020-01-03'");

				results = table.Where(r => r.DateTime == pDateTime).ToArray();
				assert("TIMESTAMP '2020-01-03 04:05:06.789123'");

				results = table.Where(r => r.DateTime_ == pDateTime).ToArray();
				assert("TO_DATE('2020-01-03 04:05:06', 'YYYY-MM-DD HH24:MI:SS')");

				results = table.Where(r => r.DateTime2 == pDateTime).ToArray();
				assert("TIMESTAMP '2020-01-03 04:05:06.789123'");

				results = table.Where(r => r.DateTime2_0 == pDateTime).ToArray();
				assert("TIMESTAMP '2020-01-03 04:05:06'");

				results = table.Where(r => r.DateTime2_1 == pDateTime).ToArray();
				assert("TIMESTAMP '2020-01-03 04:05:06.7'");

				results = table.Where(r => r.DateTime2_9 == pDateTime).ToArray();
				assert("TIMESTAMP '2020-01-03 04:05:06.7891234'");

				results = table.Where(r => r.DateTimeOffset_ == pDateTimeOffset).ToArray();
				assert("TIMESTAMP '2020-01-03 03:20:06.789123 +00:00'");

				results = table.Where(r => r.DateTimeOffset_0 == pDateTimeOffset).ToArray();
				assert("TIMESTAMP '2020-01-03 03:20:06 +00:00'");

				results = table.Where(r => r.DateTimeOffset_1 == pDateTimeOffset).ToArray();
				assert("TIMESTAMP '2020-01-03 03:20:06.7 +00:00'");

				results = table.Where(r => r.DateTimeOffset_9 == pDateTimeOffset).ToArray();
				assert("TIMESTAMP '2020-01-03 03:20:06.7891234 +00:00'");

				void assert(string function)
				{
					Assert.AreEqual(1, results.Length);

					Assert.AreEqual(new DateTime(2020, 1, 3), results[0].Date);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1230), results[0].DateTime);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6), results[0].DateTime_);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1230), results[0].DateTime2);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 0), results[0].DateTime2_0);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 700), results[0].DateTime2_1);
					Assert.AreEqual(new DateTime(2020, 1, 3, 4, 5, 6, 789).AddTicks(1234), results[0].DateTime2_9);
					Assert.AreEqual(new DateTimeOffset(2020, 1, 3, 4, 5, 6, 789, TimeSpan.FromMinutes(45)).AddTicks(1230), results[0].DateTimeOffset_);
					Assert.AreEqual(new DateTimeOffset(2020, 1, 3, 4, 5, 6, 0, TimeSpan.FromMinutes(45)), results[0].DateTimeOffset_0);
					Assert.AreEqual(new DateTimeOffset(2020, 1, 3, 4, 5, 6, 700, TimeSpan.FromMinutes(45)), results[0].DateTimeOffset_1);
					Assert.AreEqual(new DateTimeOffset(2020, 1, 3, 4, 5, 6, 789, TimeSpan.FromMinutes(45)).AddTicks(1234), results[0].DateTimeOffset_9);

					if (inlineParameters)
						Assert.True(db.LastQuery.Contains(function));
				}
			}
		}

#endregion

		[ActiveIssue(399)]
		[Test]
		public void Issue399Test([IncludeDataSources(false, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions()
				{
					GetTables     = false,
					GetProcedures = true
				});

				Assert.AreEqual(11, schema.Procedures.Count);

				// This filter used by T4 generator
				Assert.AreEqual(11, schema.Procedures.Where(
					proc => proc.IsLoaded
					|| proc.IsFunction && !proc.IsTableFunction
					|| proc.IsTableFunction && proc.ResultException != null).Count());
			}
		}

		[Table("TYPESTEST")]
		public class TypesTest
		{
			[Column(DbType = "CHAR(10)")     ] public string Char10       { get; set; } = null!;
			[Column(DbType = "NCHAR(10)")    ] public string NChar10      { get; set; } = null!;
			[Column(DbType = "VARCHAR(10)")  ] public string VarChar10    { get; set; } = null!;
			[Column(DbType = "VARCHAR2(10)") ] public string VarChar2_10  { get; set; } = null!;
			[Column(DbType = "NVARCHAR2(10)")] public string NVarChar2_10 { get; set; } = null!;
		}

		// TODO: add more types and assertions
		[Test]
		public void TestSchemaTypes([IncludeDataSources(false, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable<TypesTest>())
			{
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions()
				{
					GetTables = true,
					GetProcedures = false
				});

				var table = schema.Tables.SingleOrDefault(t => t.TableName == nameof(TypesTest).ToUpperInvariant())!;
				Assert.IsNotNull(table);
				Assert.AreEqual(5, table.Columns.Count);

				AssertColumn(nameof(TypesTest.Char10)      , "CHAR(10)"     , 10);
				AssertColumn(nameof(TypesTest.NChar10)     , "NCHAR(10)"    , 10);
				AssertColumn(nameof(TypesTest.VarChar10)   , "VARCHAR2(10)" , 10); // VARCHAR is alias to VARCHAR2
				AssertColumn(nameof(TypesTest.VarChar2_10) , "VARCHAR2(10)" , 10);
				AssertColumn(nameof(TypesTest.NVarChar2_10), "NVARCHAR2(10)", 10);

				void AssertColumn(string name, string dbType, int? length)
				{
					var column = table.Columns.SingleOrDefault(c => c.ColumnName == name)!;

					Assert.IsNotNull(column);
					Assert.AreEqual(dbType, column.ColumnType);
					Assert.AreEqual(length, column.Length);
				}
			}
		}

		[Table("BULKCOPYTABLE")]
		sealed class BulkCopyTable
		{
			[Column("ID")] public int Id { get; set; }
		}

		[Table("BULKCOPYTABLE2")]
		sealed class BulkCopyTable2
		{
			[Column("id")] public int Id { get; set; }
		}

		[Test]
		public void BulkCopyWithSchemaName(
			[IncludeDataSources(false, TestProvName.AllOracle)] string context, [Values] bool withSchema)
		{
			using var db    = GetDataConnection(context);
			using var table = db.CreateLocalTable<BulkCopyTable>();
			{
				var schemaName = TestUtils.GetSchemaName(db, context);

				var trace = string.Empty;
				db.OnTraceConnection += ti =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				table.BulkCopy(
						new BulkCopyOptions() { BulkCopyType = BulkCopyType.ProviderSpecific, SchemaName = withSchema ? schemaName : null },
						Enumerable.Range(1, 10).Select(id => new BulkCopyTable { Id = id }));

				if (withSchema)
					Assert.True(trace.Contains($"INSERT BULK {schemaName}.BULKCOPYTABLE"));
				else
					Assert.True(trace.Contains("INSERT BULK BULKCOPYTABLE"));
			}
		}

		[Test]
		public void BulkCopyWithServerName(
			[IncludeDataSources(false, TestProvName.AllOracle)] string context, [Values] bool withServer)
		{
			using var db    = GetDataConnection(context);
			using var table = db.CreateLocalTable<BulkCopyTable>();
			{
				var serverName = TestUtils.GetServerName(db, context);

				var trace = string.Empty;
				db.OnTraceConnection += ti =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				table.BulkCopy(
						new BulkCopyOptions() { BulkCopyType = BulkCopyType.ProviderSpecific, ServerName = withServer ? serverName : null },
						Enumerable.Range(1, 10).Select(id => new BulkCopyTable { Id = id }));

				if (withServer)
					Assert.False(trace.Contains($"INSERT BULK"));
				else
					Assert.True(trace.Contains("INSERT BULK BULKCOPYTABLE"));
			}
		}

		[Test]
		public void BulkCopyWithEscapedColumn(
			[IncludeDataSources(false, TestProvName.AllOracle)] string context)
		{
			using var db    = GetDataConnection(context);
			using var table = db.CreateLocalTable<BulkCopyTable2>();
			{
				var serverName = TestUtils.GetServerName(db, context);

				var trace = string.Empty;
				db.OnTraceConnection += ti =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						trace = ti.SqlText;
				};

				table.BulkCopy(
						new BulkCopyOptions() { BulkCopyType = BulkCopyType.ProviderSpecific },
						Enumerable.Range(1, 10).Select(id => new BulkCopyTable2 { Id = id }));

				Assert.False(trace.Contains($"INSERT BULK"));
			}
		}

		[Test]
		public void BulkCopyTransactionTest(
			[IncludeDataSources(false, TestProvName.AllOracle)] string context, [Values] bool withTransaction, [Values] bool withInternalTransaction)
		{
			using var db    = GetDataConnection(context);
			using var table = db.CreateLocalTable<BulkCopyTable>();
			{
				IDisposable? tr = null;
				if (withTransaction)
					tr = db.BeginTransaction();

				try
				{
					var trace = string.Empty;
					db.OnTraceConnection += ti =>
					{
						if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
							trace = ti.SqlText;
					};

					// Another devart bug: explicit + internal transaction doesn't produce error...
					if (withTransaction && withInternalTransaction && !context.IsAnyOf(TestProvName.AllOracleDevart))
						Assert.Throws<InvalidOperationException>(() =>
						{
							table.BulkCopy(
								new BulkCopyOptions() { BulkCopyType = BulkCopyType.ProviderSpecific, UseInternalTransaction = withInternalTransaction },
								Enumerable.Range(1, 10).Select(id => new BulkCopyTable { Id = id }));
						});
					else
					{
						table.BulkCopy(
							new BulkCopyOptions() { BulkCopyType = BulkCopyType.ProviderSpecific, UseInternalTransaction = withInternalTransaction },
							Enumerable.Range(1, 10).Select(id => new BulkCopyTable { Id = id }));

						Assert.True(trace.Contains($"INSERT BULK"));
					}
				}
				finally
				{
					tr?.Dispose();
				}
			}
		}

#region Issue 2342
		[Test]
		public void Issue2342Test([IncludeDataSources(false, TestProvName.AllOracle)] string context)
		{
			using (new OracleAlternativeBulkCopyMode(AlternativeBulkCopy.InsertInto))
			{
			try
			{
				Configuration.RetryPolicy.Factory  = connection => new DummyRetryPolicy();

				using var db    = GetDataConnection(context);
				using var table = db.CreateLocalTable<Issue2342Entity>();

				using (db.BeginTransaction())
				{
					table.BulkCopy(Enumerable.Range(1, 10).Select(id => new Issue2342Entity { Id = id, Name = $"Name_{id}" }));
				}

				table.Truncate();
			}
			finally
			{
				Configuration.RetryPolicy.Factory  = null;
			}
		}
		}

		sealed class DummyRetryPolicy : IRetryPolicy
		{
			public TResult       Execute<TResult>(Func<TResult> operation) => operation();
			public void          Execute(Action operation) => operation();
			public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = new CancellationToken()) => operation(cancellationToken);
			public Task          ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = new CancellationToken()) => operation(cancellationToken);
		}

		[Table]
		sealed class Issue2342Entity
		{
			[Column]                        public long   Id   { get; set; }
			[Column(Length = 256, CanBeNull = false)] public string Name { get; set; } = null!;
		}
#endregion

		[Test]
		public void TestTablesAndViewsLoad([IncludeDataSources(false, TestProvName.AllOracle)] string context, [Values] bool withFilter)
		{
			using (var db = GetDataConnection(context))
			{
				var options = withFilter
					? new GetSchemaOptions() { ExcludedSchemas = new string[] { "fake" } }
					: null;

				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, options);

				var table        = schema.Tables.FirstOrDefault(t => t.TableName == "SchemaTestTable")!;
				var view         = schema.Tables.FirstOrDefault(t => t.TableName == "SchemaTestView")!;
				var matView      = schema.Tables.FirstOrDefault(t => t.TableName == "SchemaTestMatView" && t.IsView)!;
				var matViewTable = schema.Tables.FirstOrDefault(t => t.TableName == "SchemaTestMatView" && !t.IsView);

				Assert.IsNotNull(table);
				Assert.AreEqual("This is table", table.Description);
				Assert.IsFalse(table.IsView);

				Assert.AreEqual(1, table.Columns.Count);
				Assert.AreEqual("Id", table.Columns[0].ColumnName);
				Assert.AreEqual("This is column", table.Columns[0].Description);

				Assert.IsNotNull(view);
				Assert.IsNull(view.Description);
				Assert.IsTrue(view.IsView);

				Assert.AreEqual(1, view.Columns.Count);
				Assert.AreEqual("Id", view.Columns[0].ColumnName);
				Assert.AreEqual("This is view column", view.Columns[0].Description);

				Assert.IsNotNull(matView);
				Assert.AreEqual("This is matview", matView.Description);

				Assert.AreEqual(1, matView.Columns.Count);
				Assert.AreEqual("Id", matView.Columns[0].ColumnName);
				Assert.AreEqual("This is matview column", matView.Columns[0].Description);

				Assert.IsNull(matViewTable);
			}
		}

#region Issue 2504
		[Test]
		public async Task Issue2504Test([IncludeDataSources(false, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				try
				{
					db.Execute("CREATE SEQUENCE SEQ_A START WITH 1 MINVALUE 0");
					db.Execute(@"
CREATE TABLE ""TABLE_A""(
	""COLUMN_A"" NUMBER(20, 0) NOT NULL,
	""COLUMN_B"" NUMBER(6, 0) NOT NULL,
	""COLUMN_C"" NUMBER(6, 0) NOT NULL,
	CONSTRAINT ""PK_TABLE_A"" PRIMARY KEY(""COLUMN_A"", ""COLUMN_B"", ""COLUMN_C"")
)");

					var id = await db.InsertWithInt64IdentityAsync(new Issue2504Table1
					{
						COLUMNA = 1,
						COLUMNB = 2
					});

					Assert.AreEqual(1, id);

					id = await db.InsertWithInt64IdentityAsync(new Issue2504Table2()
					{
						COLUMNA = 1,
						COLUMNB = 2
					});

					Assert.AreEqual(2, id);
				}
				finally
				{
					try { db.Execute("DROP SEQUENCE SEQ_A");    } catch { }
					try { db.Execute("DROP TABLE \"TABLE_A\""); } catch { }
				}
			}
		}

		[Table(Name = "TABLE_A")]
		public sealed class Issue2504Table1
		{
			[PrimaryKey]
			[Column(Name = "COLUMN_A")]
			public long COLUMNA { get; set; }

			[PrimaryKey]
			[Column(Name = "COLUMN_B")]
			public int COLUMNB { get; set; }

			[PrimaryKey]
			[Column(Name = "COLUMN_C"), SequenceName("SEQ_A")]
			public int COLUMNC { get; set; }
		}

		[Table(Name = "TABLE_A")]
		public sealed class Issue2504Table2
		{
			[PrimaryKey]
			[Column(Name = "COLUMN_A")]
			public long COLUMNA { get; set; }

			[PrimaryKey]
			[Column(Name = "COLUMN_B")]
			public int COLUMNB { get; set; }

			[PrimaryKey]
			[Column(Name = "COLUMN_C"), SequenceName("SEQ_A")]
			public int COLUMNC { get; set; }
		}
#endregion

		[Test]
		public void TestDateTimeNAddTimeSpan([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			var ts = TimeSpan.FromHours(1);

			using (var db = GetDataContext(context))
			{
				db.GetTable<AllTypes>()
					.Where(_ =>
						 Sql.CurrentTimestamp > _.datetime2DataType + TimeSpan.FromHours(1)
					).Select(x => x.ID).ToArray();
			}
		}

		[Table("LinqDataTypes", IsColumnAttributeRequired = false)]
		sealed class LinqDataTypesBlobs
		{
			public int ID { get; set; }
			// Implicit OracleBlob support, no attribute
			public OracleBlob? BinaryValue { get; set; }
			// Explicit attribute with DataType = Blob
			[Column("BinaryValue", DataType = DataType.Blob)]
			public OracleBlob? Blob { get; set; }
		}

		[Test]
		public void TestBlob([IncludeDataSources(TestProvName.AllOracleManaged)] string context)
		{
			using var db = GetDataContext(context);
			if (db is not DataConnection dc) return;

			using var tx = dc.BeginTransaction();

			using var blob = new OracleBlob((OracleConnection)dc.Connection);
			blob.WriteByte(1);

			db.GetTable<LinqDataTypesBlobs>().Insert(() => new LinqDataTypesBlobs { ID = -10, BinaryValue = blob });
			db.GetTable<LinqDataTypesBlobs>().Insert(() => new LinqDataTypesBlobs { ID = -20, Blob = blob });

			var inserted = db
				.GetTable<LinqDataTypesBlobs>()
				.Where(x => x.ID.In(-10, -20))
				.Select(x => Sql.Expr<int>("LENGTH(\"BinaryValue\")"))
				.ToList();

			tx.Rollback();

			inserted.Should().Equal(1, 1);
		}

		[Table("LinqDataTypes", IsColumnAttributeRequired = false)]
		sealed class LinqDataTypesBlobsDevart
		{
			public int ID { get; set; }
			// Implicit OracleBlob support, no attribute
			public DA.OracleLob? BinaryValue { get; set; }
			// Explicit attribute with DataType = Blob
			[Column("BinaryValue", DataType = DataType.Blob)]
			public DA.OracleLob? Blob { get; set; }
		}

		[Test]
		public void TestBlobDevart([IncludeDataSources(TestProvName.AllOracleDevart)] string context)
		{
			using var db = GetDataContext(context);
			if (db is not DataConnection dc) return;

			using var tx = dc.BeginTransaction();

			using var blob = new DA.OracleLob((DA.OracleConnection)dc.Connection, DA.OracleDbType.Blob);
			blob.WriteByte(1);

			db.GetTable<LinqDataTypesBlobsDevart>().Insert(() => new LinqDataTypesBlobsDevart { ID = -10, BinaryValue = blob });
			db.GetTable<LinqDataTypesBlobsDevart>().Insert(() => new LinqDataTypesBlobsDevart { ID = -20, Blob = blob });

			var inserted = db
				.GetTable<LinqDataTypesBlobsDevart>()
				.Where(x => x.ID.In(-10, -20))
				.Select(x => Sql.Expr<int>("LENGTH(\"BinaryValue\")"))
				.ToList();

			tx.Rollback();

			inserted.Should().Equal(1, 1);
		}

#if NETFRAMEWORK
		[Table("LinqDataTypes", IsColumnAttributeRequired = false)]
		sealed class LinqDataTypesBlobsNative
		{
			public int ID { get; set; }
			// Implicit OracleBlob support, no attribute
			public Oracle.DataAccess.Types.OracleBlob? BinaryValue { get; set; }
			// Explicit attribute with DataType = Blob
			[Column("BinaryValue", DataType = DataType.Blob)]
			public Oracle.DataAccess.Types.OracleBlob? Blob { get; set; }
		}

		[Test]
		public void TestBlobNative([IncludeDataSources(TestProvName.AllOracleNative)] string context)
		{
			using var db = GetDataContext(context);
			if (db is not DataConnection dc) return;

			using var tx = dc.BeginTransaction();

			using var blob = new Oracle.DataAccess.Types.OracleBlob((Oracle.DataAccess.Client.OracleConnection)dc.Connection);
			blob.WriteByte(1);

			db.GetTable<LinqDataTypesBlobsNative>().Insert(() => new LinqDataTypesBlobsNative { ID = -10, BinaryValue = blob });
			db.GetTable<LinqDataTypesBlobsNative>().Insert(() => new LinqDataTypesBlobsNative { ID = -20, Blob = blob });

			var inserted = db
				.GetTable<LinqDataTypesBlobsNative>()
				.Where(x => x.ID.In(-10, -20))
				.Select(x => Sql.Expr<int>("LENGTH(\"BinaryValue\")"))
				.ToList();

			tx.Rollback();

			inserted.Should().Equal(1, 1);
		}
#endif

		[Table]
		sealed class NativeIdentity
		{
			// TODO: we need to add identity support for create table API for oracle12+ dialect
			[Column(DbType = "NUMBER GENERATED BY DEFAULT AS IDENTITY")]
			public int Id    { get; set; }
			[Column]
			public int Field { get; set; }
		}

		[Test]
		public void TestNativeIdentityBulkCopy(
			[IncludeDataSources(TestProvName.AllOracle12Plus)] string context,
			[Values] BulkCopyType        copyType,
			[Values] bool                keepIdentity,
			[Values] AlternativeBulkCopy multipeRowsMode)
		{
			if (copyType == BulkCopyType.RowByRow && keepIdentity)
				Assert.Inconclusive($"{nameof(BulkCopyType.RowByRow)} doesn't support {nameof(BulkCopyOptions.KeepIdentity)} = true mode");
			if (copyType == BulkCopyType.MultipleRows && multipeRowsMode == AlternativeBulkCopy.InsertAll && !keepIdentity)
				Assert.Inconclusive($"{nameof(BulkCopyType.MultipleRows)} doesn't support {nameof(BulkCopyOptions.KeepIdentity)} = false with {nameof(AlternativeBulkCopy.InsertAll)} mode");
			if ((copyType == BulkCopyType.ProviderSpecific || copyType == BulkCopyType.Default) && !keepIdentity)
				Assert.Inconclusive($"{nameof(BulkCopyType.ProviderSpecific)} doesn't support {nameof(BulkCopyOptions.KeepIdentity)} = false mode");

			using (new OracleAlternativeBulkCopyMode(multipeRowsMode))
			using (var db    = new DataConnection(context))
			using (var table = db.CreateLocalTable<NativeIdentity>())
			{
				var ms = new MappingSchema();
				ms.GetFluentMappingBuilder().Entity<NativeIdentity>()
					.HasColumn(e => e.Id)
					.HasColumn(e => e.Field)
					.HasIdentity(e => e.Id);
				db.AddMappingSchema(ms);

				var initialData = new []
				{
					new NativeIdentity() { Id = 4, Field = 11 },
					new NativeIdentity() { Id = 8, Field = 12 },
				};

				db.BulkCopy(new BulkCopyOptions()
				{
					BulkCopyType = copyType,
					KeepIdentity = keepIdentity,
				}, initialData);

				var insertedData = table.OrderBy(_ => _.Field).ToArray();

				Assert.AreEqual(2, insertedData.Length);
				Assert.AreEqual(11, insertedData[0].Field);
				Assert.AreEqual(12, insertedData[1].Field);

				if (keepIdentity)
				{
					Assert.AreEqual(4, insertedData[0].Id);
					Assert.AreEqual(8, insertedData[1].Id);
				}
				else
				{
					Assert.AreEqual(1, insertedData[0].Id);
					Assert.AreEqual(2, insertedData[1].Id);
				}
			}
		}

		[Test]
		public void TestModule([IncludeDataSources(false, TestProvName.AllOracle)] string context)
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

				db.ExecuteProc("TEST_PROCEDURE", parameters);
				Assert.AreEqual(4, parameters[1].Value);
				db.ExecuteProc("TEST_PACKAGE1.TEST_PROCEDURE", parameters);
				Assert.AreEqual(2, parameters[1].Value);
				db.ExecuteProc("TEST_PACKAGE2.TEST_PROCEDURE", parameters);
				Assert.AreEqual(3, parameters[1].Value);

				Assert.AreEqual(4, db.Person.Select(p => OracleModuleFunctions.TestFunction(1)).First());
				Assert.AreEqual(2, db.Person.Select(p => OracleModuleFunctions.TestFunctionP1(1)).First());
				Assert.AreEqual(3, db.Person.Select(p => OracleModuleFunctions.TestFunctionP2(1)).First());

				Assert.AreEqual(4, OracleModuleFunctions.TestTableFunction(db, 1).Select(r => r.O).First());
				Assert.AreEqual(2, OracleModuleFunctions.TestTableFunctionP1(db, 1).Select(r => r.O).First());
				Assert.AreEqual(3, OracleModuleFunctions.TestTableFunctionP2(db, 1).Select(r => r.O).First());
			}
		}

		static class OracleModuleFunctions
		{
			[Sql.Function("TEST_FUNCTION", ServerSideOnly = true)]
			public static int TestFunction(int param)
			{
				throw new InvalidOperationException("Scalar function cannot be called outside of query");
			}

			[Sql.Function("TEST_PACKAGE1.TEST_FUNCTION", ServerSideOnly = true)]
			public static int TestFunctionP1(int param)
			{
				throw new InvalidOperationException("Scalar function cannot be called outside of query");
			}

			[Sql.Function("TEST_PACKAGE2.TEST_FUNCTION", ServerSideOnly = true)]
			public static int TestFunctionP2(int param)
			{
				throw new InvalidOperationException("Scalar function cannot be called outside of query");
			}

			[Sql.TableFunction("TEST_TABLE_FUNCTION", argIndices: new[] { 1 })]
			public static LinqToDB.ITable<Record> TestTableFunction(IDataContext db, int param1)
			{
				return db.GetTable<Record>(null, (MethodInfo)MethodBase.GetCurrentMethod()!, db, param1);
			}

			[Sql.TableFunction("TEST_TABLE_FUNCTION", argIndices: new[] { 1 }, Package = "TEST_PACKAGE1")]
			public static LinqToDB.ITable<Record> TestTableFunctionP1(IDataContext db, int param1)
			{
				return db.GetTable<Record>(null, (MethodInfo)MethodBase.GetCurrentMethod()!, db, param1);
			}

			[Sql.TableFunction("TEST_TABLE_FUNCTION", argIndices: new[] { 1 }, Package = "TEST_PACKAGE2")]
			public static LinqToDB.ITable<Record> TestTableFunctionP2(IDataContext db, int param1)
			{
				return db.GetTable<Record>(null, (MethodInfo)MethodBase.GetCurrentMethod()!, db, param1);
			}

			public sealed class Record
			{
				public int O { get; set; }
			}
		}

		[Test]
		public void Issue3732Test([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using var db    = GetDataConnection(context);
			using var table = db.CreateLocalTable<BulkCopyTable>();
			using var _     = new OracleAlternativeBulkCopyMode(AlternativeBulkCopy.InsertInto);

			db.BulkCopy(Enumerable.Range(1, 10).Select(x => new BulkCopyTable() { Id = x }));

			var id = 4;
			table.Where(x => x.Id != id).ToArray();
		}

		[Test]
		public void Issue3740Test1([IncludeDataSources(TestProvName.AllOracle12Plus)] string context)
		{
			using var db = GetDataConnection(context);

			db.Execute("CREATE OR REPLACE FUNCTION ISSUE3742(myParameter IN VARCHAR2) RETURN BOOLEAN AS BEGIN RETURN TRUE; END;");
			try
			{
				db.Execute(@"WITH
FUNCTION convert_bool(i IN VARCHAR2) RETURN NUMBER AS
BEGIN
	RETURN CASE ISSUE3742(i) WHEN TRUE THEN 1 WHEN FALSE THEN 0 END;
END convert_bool;
SELECT convert_bool(:p) FROM SYS.DUAL", new { p = "test" });
			}
			finally
			{
				db.Execute("DROP FUNCTION ISSUE3742");
			}
		}

		[Test]
		public void Issue3740Test2([IncludeDataSources(TestProvName.AllOracle12Plus)] string context)
		{
			using var db = GetDataConnection(context);

			db.Execute("CREATE OR REPLACE FUNCTION ISSUE3742(myParameter IN VARCHAR2) RETURN BOOLEAN AS BEGIN RETURN TRUE; END;");
			try
			{
				db.NextQueryHints.Add(@"**WITH
FUNCTION convert_bool(i IN VARCHAR2) RETURN NUMBER AS
BEGIN
	RETURN CASE ISSUE3742(i) WHEN TRUE THEN 1 WHEN FALSE THEN 0 END;
END convert_bool;");
				db.Select(() => Issue3742Function("test"));
			}
			finally
			{
				db.Execute("DROP FUNCTION ISSUE3742");
			}
		}

		[Sql.Expression("convert_bool({0})", ServerSideOnly = true)]
		public static bool Issue3742Function(string parameter) => throw new InvalidOperationException();
	}
}
