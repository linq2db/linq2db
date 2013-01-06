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

using Microsoft.SqlServer.Types;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class SqlServer : TestBase
	{
		static void TestNumerics<T>(DataConnection conn, T expectedValue, DataType dataType, string skip = "")
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

				var sql = string.Format("SELECT Cast({0} as {1})", sqlValue, sqlType);

				Debug.WriteLine(sql + " -> " + typeof(T));

				Assert.That(conn.Query<T>(sql).First(), Is.EqualTo(expectedValue));
			}

			conn.Command.Parameters.Clear();
			Assert.That(conn.Execute<T>("SELECT @p", new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
			conn.Command.Parameters.Clear();
			Assert.That(conn.Execute<T>("SELECT @p", new DataParameter { Name = "p", Value = expectedValue }), Is.EqualTo(expectedValue));
			conn.Command.Parameters.Clear();
			Assert.That(conn.Execute<T>("SELECT @p", new { p = expectedValue }), Is.EqualTo(expectedValue));
		}

		[Test]
		public void TestNumerics([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				TestNumerics(conn, (bool)    true,    DataType.Boolean);
				TestNumerics(conn, (bool?)   true,    DataType.Boolean);

				TestNumerics(conn, (sbyte)   1,       DataType.SByte);
				TestNumerics(conn, (sbyte?)  1,       DataType.SByte);
				TestNumerics(conn, sbyte.MinValue,    DataType.SByte,   "bit tinyint");
				TestNumerics(conn, sbyte.MaxValue,    DataType.SByte,   "bit");
				TestNumerics(conn, (short)   1,       DataType.Int16);
				TestNumerics(conn, (short?)  1,       DataType.Int16);
				TestNumerics(conn, short.MinValue,    DataType.Int16,   "bit tinyint");
				TestNumerics(conn, short.MaxValue,    DataType.Int16,   "bit tinyint");
				TestNumerics(conn, (int)     1,       DataType.Int32);
				TestNumerics(conn, (int?)    1,       DataType.Int32);
				TestNumerics(conn, int.MinValue,      DataType.Int32,   "bit smallint smallmoney tinyint");
				TestNumerics(conn, int.MaxValue,      DataType.Int32,   "bit smallint smallmoney tinyint real");
				TestNumerics(conn, (long)    1L,      DataType.Int64);
				TestNumerics(conn, (long?)   1L,      DataType.Int64);
				TestNumerics(conn, long.MinValue,     DataType.Int64,   "bit decimal int money numeric smallint smallmoney tinyint");
				TestNumerics(conn, long.MaxValue,     DataType.Int64,   "bit decimal int money numeric smallint smallmoney tinyint float real");

				TestNumerics(conn, (byte)    1,       DataType.Byte);
				TestNumerics(conn, (byte?)   1,       DataType.Byte);
				TestNumerics(conn, byte.MaxValue,     DataType.Byte,    "bit");
				TestNumerics(conn, (ushort)  1,       DataType.UInt16);
				TestNumerics(conn, (ushort?) 1,       DataType.UInt16);
				TestNumerics(conn, ushort.MaxValue,   DataType.UInt16,  "bit smallint tinyint");
				TestNumerics(conn, (uint)    1u,      DataType.UInt32);
				TestNumerics(conn, (uint?)   1u,      DataType.UInt32);
				TestNumerics(conn, uint.MaxValue,     DataType.UInt32,  "bit int smallint smallmoney tinyint real");
				TestNumerics(conn, (ulong)   1ul,     DataType.UInt64);
				TestNumerics(conn, (ulong?)  1ul,     DataType.UInt64);
				TestNumerics(conn, ulong.MaxValue,    DataType.UInt64,  "bigint bit decimal int money numeric smallint smallmoney tinyint float real");

				TestNumerics(conn, (float)   1,       DataType.Single);
				TestNumerics(conn, (float?)  1,       DataType.Single);
				TestNumerics(conn, -3.40282306E+38f,  DataType.Single,  "bigint bit decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint");
				TestNumerics(conn, 3.40282306E+38f,   DataType.Single,  "bigint bit decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint");
				TestNumerics(conn, (double)  1d,      DataType.Double);
				TestNumerics(conn, (double?) 1d,      DataType.Double);
				TestNumerics(conn, -1.79E+308d,       DataType.Double,  "bigint bit decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint real");
				TestNumerics(conn,  1.79E+308d,       DataType.Double,  "bigint bit decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint real");
				TestNumerics(conn, (decimal) 1m,      DataType.Decimal);
				TestNumerics(conn, (decimal?)1m,      DataType.Decimal);
				TestNumerics(conn, decimal.MinValue,  DataType.Decimal, "bigint bit decimal int money numeric smallint smallmoney tinyint float real");
				TestNumerics(conn, decimal.MaxValue,  DataType.Decimal, "bigint bit decimal int money numeric smallint smallmoney tinyint float real");
				TestNumerics(conn, (decimal) 1m,      DataType.VarNumeric);
				TestNumerics(conn, (decimal?)1m,      DataType.VarNumeric);
				TestNumerics(conn, decimal.MinValue,  DataType.VarNumeric, "bigint bit decimal int money numeric smallint smallmoney tinyint float real");
				TestNumerics(conn, decimal.MaxValue,  DataType.VarNumeric, "bigint bit decimal int money numeric smallint smallmoney tinyint float real");
				TestNumerics(conn, (decimal) 1m,      DataType.Money);
				TestNumerics(conn, (decimal?)1m,      DataType.Money);
				TestNumerics(conn, -922337203685477m, DataType.Money,   "bit int smallint smallmoney tinyint real");
				TestNumerics(conn, +922337203685477m, DataType.Money,   "bit int smallint smallmoney tinyint real");
				TestNumerics(conn, (decimal) 1m,      DataType.SmallMoney);
				TestNumerics(conn, (decimal?)1m,      DataType.SmallMoney);
				TestNumerics(conn, -214748m,          DataType.SmallMoney, "bit smallint tinyint");
				TestNumerics(conn, +214748m,          DataType.SmallMoney, "bit smallint tinyint");
			}
		}

		[Test]
		public void TestDate([IncludeDataContexts(ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12' as date)"),                          Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12' as date)"),                          Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime> ("SELECT @p", DataParameter.Date("p", dateTime)),               Is.EqualTo(dateTime));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime, DataType.Date)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestSmallDateTime([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 00);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12 12:12:00' as smalldatetime)"),                 Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:00' as smalldatetime)"),                 Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime> ("SELECT @p", DataParameter.SmallDateTime("p", dateTime)),               Is.EqualTo(dateTime));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime, DataType.SmallDateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestDateTime([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12 12:12:12' as datetime)"),                 Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12' as datetime)"),                 Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime> ("SELECT @p", DataParameter.DateTime("p", dateTime)),               Is.EqualTo(dateTime));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime)),                    Is.EqualTo(dateTime));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestDateTime2([IncludeDataContexts(ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime2 = new DateTime(2012, 12, 12, 12, 12, 12, 12);

				Assert.That(conn.Query<DateTime> ("SELECT Cast('2012-12-12 12:12:12.012' as datetime2)").First(), Is.EqualTo(dateTime2));
				Assert.That(conn.Query<DateTime?>("SELECT Cast('2012-12-12 12:12:12.012' as datetime2)").First(), Is.EqualTo(dateTime2));

				Assert.That(conn.Execute<DateTime> ("SELECT @p", DataParameter.DateTime2("p", dateTime2)),               Is.EqualTo(dateTime2));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<DateTime> ("SELECT @p", DataParameter.Create   ("p", dateTime2)),               Is.EqualTo(dateTime2));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime2, DataType.DateTime2)), Is.EqualTo(dateTime2));
			}
		}

		[Test]
		public void TestDateTimeOffset([IncludeDataContexts(ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dto = new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, new TimeSpan(5, 0, 0));

				Assert.That(conn.Query<DateTimeOffset>(
					"SELECT Cast('2012-12-12 12:12:12.012' as datetime2)").First(),
					Is.EqualTo(new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2012, 12, 12, 12, 12, 12)))));

				Assert.That(conn.Query<DateTimeOffset?>(
					"SELECT Cast('2012-12-12 12:12:12.012' as datetime2)").First(),
					Is.EqualTo(new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2012, 12, 12, 12, 12, 12)))));

				Assert.That(conn.Query<DateTime>(
					"SELECT Cast('2012-12-12 13:12:12.012 -04:00' as datetimeoffset)").First(),
					Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));

				Assert.That(conn.Query<DateTime?>(
					"SELECT Cast('2012-12-12 13:12:12.012 -04:00' as datetimeoffset)").First(),
					Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));

				Assert.That(conn.Query<DateTimeOffset>(
					"SELECT Cast('2012-12-12 12:12:12.012 +05:00' as datetimeoffset)").First(),
					Is.EqualTo(dto));

				Assert.That(conn.Query<DateTimeOffset?>(
					"SELECT Cast('2012-12-12 12:12:12.012 +05:00' as datetimeoffset)").First(),
					Is.EqualTo(dto));

				Assert.That(conn.Query<DateTime>(
					"SELECT Cast(NULL as datetimeoffset)").First(),
					Is.EqualTo(default(DateTime)));

				Assert.That(conn.Query<DateTime?>(
					"SELECT Cast(NULL as datetimeoffset)").First(),
					Is.EqualTo(default(DateTime?)));

				Assert.That(conn.Execute<DateTimeOffset> ("SELECT @p", DataParameter.DateTimeOffset("p", dto)),               Is.EqualTo(dto));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<DateTimeOffset> ("SELECT @p", DataParameter.Create        ("p", dto)),               Is.EqualTo(dto));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<DateTimeOffset?>("SELECT @p", new DataParameter("p", dto)),                          Is.EqualTo(dto));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<DateTimeOffset?>("SELECT @p", new DataParameter("p", dto, DataType.DateTimeOffset)), Is.EqualTo(dto));
			}
		}

		[Test]
		public void TestTimeSpan([IncludeDataContexts(ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var time = new TimeSpan(12, 12, 12);

				Assert.That(conn.Execute<TimeSpan> ("SELECT Cast('12:12:12' as time)"), Is.EqualTo(time));
				Assert.That(conn.Execute<TimeSpan?>("SELECT Cast('12:12:12' as time)"), Is.EqualTo(time));

				Assert.That(conn.Execute<TimeSpan> ("SELECT @p", DataParameter.Time  ("p", time)),              Is.EqualTo(time));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<TimeSpan> ("SELECT @p", DataParameter.Create("p", time)),              Is.EqualTo(time));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<TimeSpan?>("SELECT @p", new DataParameter("p",  time, DataType.Time)), Is.EqualTo(time));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<TimeSpan?>("SELECT @p", new DataParameter("p",  time)),                Is.EqualTo(time));
			}
		}

		[Test]
		public void TestChar([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<char> ("SELECT Cast('1' as char)").        First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char?>("SELECT Cast('1' as char)").        First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char> ("SELECT Cast('1' as char(1))").     First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char?>("SELECT Cast('1' as char(1))").     First(), Is.EqualTo('1'));

				Assert.That(conn.Query<char> ("SELECT Cast('1' as varchar)").     First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char?>("SELECT Cast('1' as varchar)").     First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char> ("SELECT Cast('1' as varchar(20))"). First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char?>("SELECT Cast('1' as varchar(20))"). First(), Is.EqualTo('1'));

				Assert.That(conn.Query<char> ("SELECT Cast('1' as nchar)").       First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char?>("SELECT Cast('1' as nchar)").       First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char> ("SELECT Cast('1' as nchar(20))").   First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char?>("SELECT Cast('1' as nchar(20))").   First(), Is.EqualTo('1'));

				Assert.That(conn.Query<char> ("SELECT Cast('1' as nvarchar)").    First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char?>("SELECT Cast('1' as nvarchar)").    First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char> ("SELECT Cast('1' as nvarchar(20))").First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char?>("SELECT Cast('1' as nvarchar(20))").First(), Is.EqualTo('1'));

				Assert.That(conn.Query<char> ("SELECT @p",                  DataParameter.Char("p",  '1')).First(), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<char?>("SELECT @p",                  DataParameter.Char("p",  '1')).First(), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<char> ("SELECT Cast(@p as char)",    DataParameter.Char("p",  '1')).First(), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<char?>("SELECT Cast(@p as char)",    DataParameter.Char("p",  '1')).First(), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<char> ("SELECT Cast(@p as char(1))", DataParameter.Char("@p", '1')).First(), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<char?>("SELECT Cast(@p as char(1))", DataParameter.Char("@p", '1')).First(), Is.EqualTo('1'));

				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<char> ("SELECT @p", DataParameter.VarChar ("p", '1')).First(), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<char?>("SELECT @p", DataParameter.VarChar ("p", '1')).First(), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<char> ("SELECT @p", DataParameter.NChar   ("p", '1')).First(), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<char?>("SELECT @p", DataParameter.NChar   ("p", '1')).First(), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<char> ("SELECT @p", DataParameter.NVarChar("p", '1')).First(), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<char?>("SELECT @p", DataParameter.NVarChar("p", '1')).First(), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<char> ("SELECT @p", DataParameter.Create  ("p", '1')).First(), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<char?>("SELECT @p", DataParameter.Create  ("p", '1')).First(), Is.EqualTo('1'));

				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<char> ("SELECT @p", new DataParameter { Name = "p", Value = '1' }).First(), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<char?>("SELECT @p", new DataParameter { Name = "p", Value = '1' }).First(), Is.EqualTo('1'));
			}
		}

		[Test]
		public void TestString([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<string>("SELECT Cast('12345' as char)").         First(), Is.EqualTo("12345"));
				Assert.That(conn.Query<string>("SELECT Cast('12345' as char(20))").     First(), Is.EqualTo("12345"));
				Assert.That(conn.Query<string>("SELECT Cast(NULL    as char(20))").     First(), Is.Null);

				Assert.That(conn.Query<string>("SELECT Cast('12345' as varchar)").      First(), Is.EqualTo("12345"));
				Assert.That(conn.Query<string>("SELECT Cast('12345' as varchar(20))").  First(), Is.EqualTo("12345"));
				Assert.That(conn.Query<string>("SELECT Cast(NULL    as varchar(20))").  First(), Is.Null);

				Assert.That(conn.Query<string>("SELECT Cast('12345' as text)").         First(), Is.EqualTo("12345"));
				Assert.That(conn.Query<string>("SELECT Cast(NULL    as text)").         First(), Is.Null);

				Assert.That(conn.Query<string>("SELECT Cast('12345' as varchar(max))"). First(), Is.EqualTo("12345"));
				Assert.That(conn.Query<string>("SELECT Cast(NULL    as varchar(max))"). First(), Is.Null);

				Assert.That(conn.Query<string>("SELECT Cast('12345' as nchar)").        First(), Is.EqualTo("12345"));
				Assert.That(conn.Query<string>("SELECT Cast('12345' as nchar(20))").    First(), Is.EqualTo("12345"));
				Assert.That(conn.Query<string>("SELECT Cast(NULL    as nchar(20))").    First(), Is.Null);

				Assert.That(conn.Query<string>("SELECT Cast('12345' as nvarchar)").     First(), Is.EqualTo("12345"));
				Assert.That(conn.Query<string>("SELECT Cast('12345' as nvarchar(20))"). First(), Is.EqualTo("12345"));
				Assert.That(conn.Query<string>("SELECT Cast(NULL    as nvarchar(20))"). First(), Is.Null);

				Assert.That(conn.Query<string>("SELECT Cast('12345' as ntext)").        First(), Is.EqualTo("12345"));
				Assert.That(conn.Query<string>("SELECT Cast(NULL    as ntext)").        First(), Is.Null);

				Assert.That(conn.Query<string>("SELECT Cast('12345' as nvarchar(max))").First(), Is.EqualTo("12345"));
				Assert.That(conn.Query<string>("SELECT Cast(NULL    as nvarchar(max))").First(), Is.Null);

				Assert.That(conn.Query<string>("SELECT @p", DataParameter.Char    ("p", "123")).First(), Is.EqualTo("123"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<string>("SELECT @p", DataParameter.VarChar ("p", "123")).First(), Is.EqualTo("123"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<string>("SELECT @p", DataParameter.Text    ("p", "123")).First(), Is.EqualTo("123"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<string>("SELECT @p", DataParameter.NChar   ("p", "123")).First(), Is.EqualTo("123"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<string>("SELECT @p", DataParameter.NVarChar("p", "123")).First(), Is.EqualTo("123"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<string>("SELECT @p", DataParameter.NText   ("p", "123")).First(), Is.EqualTo("123"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<string>("SELECT @p", DataParameter.Create  ("p", "123")).First(), Is.EqualTo("123"));

				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<string>("SELECT @p", DataParameter.Create("p", (string)null)).First(), Is.EqualTo(null));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<string>("SELECT @p", new DataParameter { Name = "p", Value = "1" }).First(), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestBinary([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			var arr1 = new byte[] {       48, 57 };
			var arr2 = new byte[] { 0, 0, 48, 57 };

			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<byte[]>("SELECT Cast(12345 as binary(2))").     First(), Is.EqualTo(           arr1));
				Assert.That(conn.Query<Binary>("SELECT Cast(12345 as binary(4))").     First(), Is.EqualTo(new Binary(arr2)));

				Assert.That(conn.Query<byte[]>("SELECT Cast(12345 as varbinary(2))").  First(), Is.EqualTo(           arr1));
				Assert.That(conn.Query<Binary>("SELECT Cast(12345 as varbinary(4))").  First(), Is.EqualTo(new Binary(arr2)));

				Assert.That(conn.Query<byte[]>("SELECT Cast(NULL as image)").          First(), Is.EqualTo(null));
				Assert.That(conn.Query<byte[]>("SELECT Cast(12345 as varbinary(max))").First(), Is.EqualTo(           arr2));

				Assert.That(conn.Query<byte[]>("SELECT @p", DataParameter.Binary   ("p", arr1)).First(), Is.EqualTo(arr1));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<byte[]>("SELECT @p", DataParameter.VarBinary("p", arr1)).First(), Is.EqualTo(arr1));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<byte[]>("SELECT @p", DataParameter.Create   ("p", arr1)).First(), Is.EqualTo(arr1));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<byte[]>("SELECT @p", DataParameter.VarBinary("p", null)).First(), Is.EqualTo(null));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<byte[]>("SELECT Cast(@p as binary(1))", DataParameter.Binary("p", new byte[0])).First(), Is.EqualTo(new byte[] {0}));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<byte[]>("SELECT @p", DataParameter.Binary   ("p", new byte[0])).First(), Is.EqualTo(new byte[8000]));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<byte[]>("SELECT @p", DataParameter.VarBinary("p", new byte[0])).First(), Is.EqualTo(new byte[0]));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<byte[]>("SELECT @p", DataParameter.Image    ("p", new byte[0])).First(), Is.EqualTo(new byte[0]));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<byte[]>("SELECT @p", new DataParameter { Name = "p", Value = arr1 }).First(), Is.EqualTo(arr1));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<byte[]>("SELECT @p", DataParameter.Create   ("p", new Binary(arr1))).First(), Is.EqualTo(arr1));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<byte[]>("SELECT @p", new DataParameter("p", new Binary(arr1))).First(), Is.EqualTo(arr1));
			}
		}

		[Test]
		public void TestSqlTypes([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
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
				Assert.That(conn.Execute<SqlString> ("SELECT Cast('12345'  as char(6))").  Value, Is.EqualTo("12345"));
				Assert.That(conn.Execute<SqlXml>    ("SELECT Cast('<xml/>' as xml)").      Value, Is.EqualTo("<xml />"));

				Assert.That(
					conn.Execute<SqlDateTime>("SELECT Cast('2012-12-12 12:12:12' as datetime)").Value,
					Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));

				Assert.That(
					conn.Execute<SqlGuid>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uniqueidentifier)").Value,
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<SqlBinary> ("SELECT @p", new DataParameter("p", new SqlBinary(arr))).                    Value, Is.EqualTo(arr));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<SqlBinary> ("SELECT @p", new DataParameter("p", new SqlBinary(arr), DataType.VarBinary)).Value, Is.EqualTo(arr));

				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<SqlBoolean>("SELECT @p", new DataParameter("p", true)).                  Value, Is.EqualTo(true));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<SqlBoolean>("SELECT @p", new DataParameter("p", true, DataType.Boolean)).Value, Is.EqualTo(true));

				var conv = conn.MappingSchema.GetConverter<string,SqlXml>();

				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<SqlXml>("SELECT @p", new DataParameter("p", conv("<xml/>"))).              Value, Is.EqualTo("<xml />"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<SqlXml>("SELECT @p", new DataParameter("p", conv("<xml/>"), DataType.Xml)).Value, Is.EqualTo("<xml />"));
			}
		}

		[Test]
		public void TestGuid([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(
					conn.Query<Guid>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uniqueidentifier)").First(),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				Assert.That(
					conn.Query<Guid?>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uniqueidentifier)").First(),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				var guid = Guid.NewGuid();

				Assert.That(conn.Execute<Guid>("SELECT @p", DataParameter.Create("p", guid)),                Is.EqualTo(guid));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<Guid>("SELECT @p", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test]
		public void TestTimestamp([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var arr = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };

				Assert.That(conn.Execute<byte[]>("SELECT Cast(1 as timestamp)"),  Is.EqualTo(arr));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(1 as rowversion)"), Is.EqualTo(arr));

				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Timestamp("p", arr)),               Is.EqualTo(arr));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter("p", arr, DataType.Timestamp)), Is.EqualTo(arr));
			}
		}

		[Test]
		public void TestSqlVariant([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
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

		[Test]
		public void TestHierarchyID([IncludeDataContexts(ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var id = SqlHierarchyId.Parse("/1/3/");

				Assert.That(conn.Query<SqlHierarchyId> ("SELECT Cast('/1/3/' as hierarchyid)").First(), Is.EqualTo(id));
				Assert.That(conn.Query<SqlHierarchyId?>("SELECT Cast('/1/3/' as hierarchyid)").First(), Is.EqualTo(id));
				Assert.That(conn.Query<SqlHierarchyId> ("SELECT Cast(NULL as hierarchyid)").   First(), Is.EqualTo(SqlHierarchyId.Null));
				Assert.That(conn.Query<SqlHierarchyId?>("SELECT Cast(NULL as hierarchyid)").   First(), Is.EqualTo(null));

				Assert.That(conn.Execute<SqlHierarchyId>("SELECT @p", new DataParameter("p", id)), Is.EqualTo(id));
			}
		}

		[Test]
		public void TestGeometry([IncludeDataContexts(ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var id = SqlGeometry.Parse("LINESTRING (100 100, 20 180, 180 180)");

				Assert.That(conn.Execute<SqlGeometry>("SELECT Cast(geometry::STGeomFromText('LINESTRING (100 100, 20 180, 180 180)', 0) as geometry)")
					.ToString(), Is.EqualTo(id.ToString()));

				Assert.That(conn.Execute<SqlGeometry>("SELECT Cast(NULL as geometry)").ToString(),
					Is.EqualTo(SqlGeometry.Null.ToString()));

				Assert.That(conn.Execute<SqlGeometry>("SELECT @p", new DataParameter("p", id)).ToString(),               Is.EqualTo(id.ToString()));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<SqlGeometry>("SELECT @p", new DataParameter("p", id, DataType.Udt)).ToString(), Is.EqualTo(id.ToString()));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<SqlGeometry>("SELECT @p", DataParameter.Udt("p", id)).ToString(),               Is.EqualTo(id.ToString()));
			}
		}

		[Test]
		public void TestGeography([IncludeDataContexts(ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var id = SqlGeography.Parse("LINESTRING (-122.36 47.656, -122.343 47.656)");

				Assert.That(conn.Execute<SqlGeography>("SELECT Cast(geography::STGeomFromText('LINESTRING(-122.360 47.656, -122.343 47.656)', 4326) as geography)")
					.ToString(), Is.EqualTo(id.ToString()));

				Assert.That(conn.Execute<SqlGeography>("SELECT Cast(NULL as geography)").ToString(),
					Is.EqualTo(SqlGeography.Null.ToString()));

				Assert.That(conn.Execute<SqlGeography>("SELECT @p", new DataParameter("p", id)).ToString(),               Is.EqualTo(id.ToString()));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<SqlGeography>("SELECT @p", new DataParameter("p", id, DataType.Udt)).ToString(), Is.EqualTo(id.ToString()));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<SqlGeography>("SELECT @p", DataParameter.Udt("p", id)).ToString(),               Is.EqualTo(id.ToString()));
			}
		}

		[Test]
		public void TestXml([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>     ("SELECT Cast('<xml/>' as xml)"),            Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast('<xml/>' as xml)").ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT Cast('<xml/>' as xml)").InnerXml,   Is.EqualTo("<xml />"));

				var xdoc = XDocument.Parse("<xml/>");
				var xml  = Convert<string,XmlDocument>.Lambda("<xml/>");

				Assert.That(conn.Execute<string>     ("SELECT @p", DataParameter.Xml("p", "<xml/>")),        Is.EqualTo("<xml/>"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<XDocument>  ("SELECT @p", DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<XmlDocument>("SELECT @p", DataParameter.Xml("p", xml)). InnerXml,   Is.EqualTo("<xml />"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<XDocument>  ("SELECT @p", new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<XDocument>  ("SELECT @p", new DataParameter("p", xml)). ToString(), Is.EqualTo("<xml />"));
			}
		}

		enum TestEnum
		{
			[MapValue("A")] AA,
			[MapValue(ProviderName.SqlServer2008, "C")] 
			[MapValue("B")] BB,
		}

		[Test]
		public void TestEnum1([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<TestEnum> ("SELECT 'A'").First(), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Query<TestEnum?>("SELECT 'A'").First(), Is.EqualTo(TestEnum.AA));

				var sql = context == ProviderName.SqlServer2008 ? "SELECT 'C'" : "SELECT 'B'";

				Assert.That(conn.Query<TestEnum> (sql).First(), Is.EqualTo(TestEnum.BB));
				Assert.That(conn.Query<TestEnum?>(sql).First(), Is.EqualTo(TestEnum.BB));
			}
		}

		[Test]
		public void TestEnum2([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<string>("SELECT @p", new { p = TestEnum.AA }).           First(), Is.EqualTo("A"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<string>("SELECT @p", new { p = (TestEnum?)TestEnum.BB }).First(),
					Is.EqualTo(context == ProviderName.SqlServer2008 ? "C" : "B"));

				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<string>("SELECT @p", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }).First(), Is.EqualTo("A"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<string>("SELECT @p", new { p = ConvertTo<string>.From(TestEnum.AA) }).First(), Is.EqualTo("A"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<string>("SELECT @p", new { p = conn.MappingSchema.GetConverter<TestEnum?,string>()(TestEnum.AA) }).First(), Is.EqualTo("A"));
			}
		}

		[Test]
		public void TestCast([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<string>("SELECT @p", new { p =  1  }).First(), Is.EqualTo("1"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Query<string>("SELECT @p", new { p = "1" }).First(), Is.EqualTo("1"));
			}
		}

		static void TestStruct<T>(DataConnection connection, string dataTypeName, T value)
			where T : struct
		{
			connection.Command.Parameters.Clear();
			Assert.That(connection.Execute<T?>(string.Format("SELECT {0} FROM AllTypes WHERE ID = 1", dataTypeName)), Is.Null);

			connection.Command.Parameters.Clear();
			Assert.That(connection.Execute<T>(string.Format("SELECT {0} FROM AllTypes WHERE ID = 2", dataTypeName)), Is.EqualTo(value));
		}

		static void TestClass<T>(DataConnection connection, string dataTypeName, T value)
		{
			connection.Command.Parameters.Clear();
			Assert.That(connection.Execute<T>(string.Format("SELECT {0} FROM AllTypes WHERE ID = 1", dataTypeName)), Is.Null);

			connection.Command.Parameters.Clear();
			Assert.That(connection.Execute<T>(string.Format("SELECT {0} FROM AllTypes WHERE ID = 2", dataTypeName)), Is.EqualTo(value));
		}

		[Test]
		public void TestDataTypes([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				TestStruct(conn, "bigintDataType",           1000000L);
				TestStruct(conn, "numericDataType",          9999999m);
				TestStruct(conn, "bitDataType",              true);
				TestStruct(conn, "smallintDataType",         (short)25555);
				TestStruct(conn, "decimalDataType",          2222222m);
				TestStruct(conn, "smallmoneyDataType",       100000m);
				TestStruct(conn, "intDataType",              7777777);
				TestStruct(conn, "tinyintDataType",          (sbyte)100);
				TestStruct(conn, "moneyDataType",            100000m);
				TestStruct(conn, "floatDataType",            20.31d);
				TestStruct(conn, "realDataType",             16.2f);

				TestStruct(conn, "datetimeDataType",         new DateTime(2012, 12, 12, 12, 12, 12));
				TestStruct(conn, "smalldatetimeDataType",    new DateTime(2012, 12, 12, 12, 12, 00));

				TestStruct(conn, "charDataType",             '1');
				TestClass (conn, "varcharDataType",          "234");
				TestClass (conn, "textDataType",             "567");
				TestClass (conn, "ncharDataType",            "23233");
				TestClass (conn, "nvarcharDataType",         "3323");
				TestClass (conn, "ntextDataType",            "111");

				TestClass (conn, "binaryDataType",           new byte[] { 1 });
				TestClass (conn, "varbinaryDataType",        new byte[] { 2 });
				TestClass (conn, "imageDataType",            new byte[] { 0, 0, 0, 3 });

				TestStruct(conn, "uniqueidentifierDataType", new Guid("{6F9619FF-8B86-D011-B42D-00C04FC964FF}"));
				TestClass (conn, "sql_variantDataType",      (object)10);

				TestClass (conn, "nvarchar_max_DataType",    "22322");
				TestClass (conn, "varchar_max_DataType",     "3333");
				TestClass (conn, "varbinary_max_DataType",   new byte[] { 0, 0, 9, 41 });

				TestClass (conn, "xmlDataType",              "<root><element strattr=\"strvalue\" intattr=\"12345\" /></root>");

				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<byte[]>("SELECT timestampDataType FROM AllTypes WHERE ID = 1").Length, Is.EqualTo(8));
			}
		}
	}
}
