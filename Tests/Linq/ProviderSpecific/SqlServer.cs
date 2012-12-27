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

namespace Tests.ProviderSpecific
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

			Assert.That(conn.Execute<T>("SELECT @p", new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
			Assert.That(conn.Execute<T>("SELECT @p", new { p = expectedValue }), Is.EqualTo(expectedValue));
		}

		[Test]
		public void TestNumerics([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				TestNumerics(conn, (bool)    true,   DataType.Boolean);
				TestNumerics(conn, (bool?)   true,   DataType.Boolean);

				TestNumerics(conn, (sbyte)   1,      DataType.SByte);
				TestNumerics(conn, (sbyte?)  1,      DataType.SByte);
				TestNumerics(conn, sbyte.MinValue,   DataType.SByte,   "bit tinyint");
				TestNumerics(conn, sbyte.MaxValue,   DataType.SByte,   "bit");
				TestNumerics(conn, (short)   1,      DataType.Int16);
				TestNumerics(conn, (short?)  1,      DataType.Int16);
				TestNumerics(conn, short.MinValue,   DataType.Int16,   "bit tinyint");
				TestNumerics(conn, short.MaxValue,   DataType.Int16,   "bit tinyint");
				TestNumerics(conn, (int)     1,      DataType.Int32);
				TestNumerics(conn, (int?)    1,      DataType.Int32);
				TestNumerics(conn, int.MinValue,     DataType.Int32,   "bit smallint smallmoney tinyint");
				TestNumerics(conn, int.MaxValue,     DataType.Int32,   "bit smallint smallmoney tinyint real");
				TestNumerics(conn, (long)    1L,     DataType.Int64);
				TestNumerics(conn, (long?)   1L,     DataType.Int64);
				TestNumerics(conn, long.MinValue,    DataType.Int64,   "bit decimal int money numeric smallint smallmoney tinyint");
				TestNumerics(conn, long.MaxValue,    DataType.Int64,   "bit decimal int money numeric smallint smallmoney tinyint float real");

				TestNumerics(conn, (byte)    1,      DataType.Byte);
				TestNumerics(conn, (byte?)   1,      DataType.Byte);
				TestNumerics(conn, byte.MaxValue,    DataType.Byte,    "bit");
				TestNumerics(conn, (ushort)  1,      DataType.UInt16);
				TestNumerics(conn, (ushort?) 1,      DataType.UInt16);
				TestNumerics(conn, ushort.MaxValue,  DataType.UInt16,  "bit smallint tinyint");
				TestNumerics(conn, (uint)    1u,     DataType.UInt32);
				TestNumerics(conn, (uint?)   1u,     DataType.UInt32);
				TestNumerics(conn, uint.MaxValue,    DataType.UInt32,  "bit int smallint smallmoney tinyint real");
				TestNumerics(conn, (ulong)   1ul,    DataType.UInt64);
				TestNumerics(conn, (ulong?)  1ul,    DataType.UInt64);
				TestNumerics(conn, ulong.MaxValue,   DataType.UInt64,  "bigint bit decimal int money numeric smallint smallmoney tinyint float real");

				TestNumerics(conn, (float)   1,      DataType.Single);
				TestNumerics(conn, (float?)  1,      DataType.Single);
				TestNumerics(conn, -3.40282306E+38f, DataType.Single,  "bigint bit decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint");
				TestNumerics(conn, 3.40282306E+38f,  DataType.Single,  "bigint bit decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint");
				TestNumerics(conn, (double)  1d,     DataType.Double);
				TestNumerics(conn, (double?) 1d,     DataType.Double);
				TestNumerics(conn, -1.79E+308d,      DataType.Double,  "bigint bit decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint real");
				TestNumerics(conn,  1.79E+308d,      DataType.Double,  "bigint bit decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint real");
				TestNumerics(conn, (decimal) 1m,     DataType.Decimal);
				TestNumerics(conn, (decimal?)1m,     DataType.Decimal);
				TestNumerics(conn, decimal.MinValue, DataType.Decimal, "bigint bit decimal int money numeric smallint smallmoney tinyint float real");
				TestNumerics(conn, decimal.MaxValue, DataType.Decimal, "bigint bit decimal int money numeric smallint smallmoney tinyint float real");
			}
		}

		[Test]
		public void TestDateTime([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<DateTime> ("SELECT Cast('2012-12-12 12:12:00' as smalldatetime)").First(), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 00)));
				Assert.That(conn.Query<DateTime?>("SELECT Cast('2012-12-12 12:12:00' as smalldatetime)").First(), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 00)));
				Assert.That(conn.Query<DateTime> ("SELECT Cast('2012-12-12 12:12:12' as datetime)").First(),      Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(conn.Query<DateTime?>("SELECT Cast('2012-12-12 12:12:12' as datetime)").First(),      Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
			}
		}

		[Test]
		public void TestDateTimeOffset([IncludeDataContexts(ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
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
					Is.EqualTo(new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, new TimeSpan(5, 0, 0))));

				Assert.That(conn.Query<DateTimeOffset?>(
					"SELECT Cast('2012-12-12 12:12:12.012 +05:00' as datetimeoffset)").First(),
					Is.EqualTo(new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, new TimeSpan(5, 0, 0))));

				Assert.That(conn.Query<DateTime>(
					"SELECT Cast(NULL as datetimeoffset)").First(),
					Is.EqualTo(default(DateTime)));

				Assert.That(conn.Query<DateTime?>(
					"SELECT Cast(NULL as datetimeoffset)").First(),
					Is.EqualTo(default(DateTime?)));

				Assert.That(conn.Query<DateTime> ("SELECT Cast('2012-12-12' as date)").First(),                   Is.EqualTo(new DateTime(2012, 12, 12)));
				Assert.That(conn.Query<DateTime?>("SELECT Cast('2012-12-12' as date)").First(),                   Is.EqualTo(new DateTime(2012, 12, 12)));
				Assert.That(conn.Query<DateTime> ("SELECT Cast('2012-12-12 12:12:12.012' as datetime2)").First(), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));
				Assert.That(conn.Query<DateTime?>("SELECT Cast('2012-12-12 12:12:12.012' as datetime2)").First(), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));
				Assert.That(conn.Query<TimeSpan> ("SELECT Cast('12:12:12' as time)").First(),                     Is.EqualTo(new TimeSpan(12, 12, 12)));
				Assert.That(conn.Query<TimeSpan?>("SELECT Cast('12:12:12' as time)").First(),                     Is.EqualTo(new TimeSpan(12, 12, 12)));
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
				Assert.That(conn.Query<char?>("SELECT @p",                  DataParameter.Char("p",  '1')).First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char> ("SELECT Cast(@p as char)",    DataParameter.Char("p",  '1')).First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char?>("SELECT Cast(@p as char)",    DataParameter.Char("p",  '1')).First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char> ("SELECT Cast(@p as char(1))", DataParameter.Char("@p", '1')).First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char?>("SELECT Cast(@p as char(1))", DataParameter.Char("@p", '1')).First(), Is.EqualTo('1'));

				Assert.That(conn.Query<char> ("SELECT @p", DataParameter.VarChar ("p", '1')).First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char?>("SELECT @p", DataParameter.VarChar ("p", '1')).First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char> ("SELECT @p", DataParameter.NChar   ("p", '1')).First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char?>("SELECT @p", DataParameter.NChar   ("p", '1')).First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char> ("SELECT @p", DataParameter.NVarChar("p", '1')).First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char?>("SELECT @p", DataParameter.NVarChar("p", '1')).First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char> ("SELECT @p", DataParameter.Create  ("p", '1')).First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char?>("SELECT @p", DataParameter.Create  ("p", '1')).First(), Is.EqualTo('1'));

				Assert.That(conn.Query<char> ("SELECT @p", new DataParameter { Name = "p", Value = '1' }).First(), Is.EqualTo('1'));
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
				Assert.That(conn.Query<string>("SELECT @p", DataParameter.VarChar ("p", "123")).First(), Is.EqualTo("123"));
				Assert.That(conn.Query<string>("SELECT @p", DataParameter.Text    ("p", "123")).First(), Is.EqualTo("123"));
				Assert.That(conn.Query<string>("SELECT @p", DataParameter.NChar   ("p", "123")).First(), Is.EqualTo("123"));
				Assert.That(conn.Query<string>("SELECT @p", DataParameter.NVarChar("p", "123")).First(), Is.EqualTo("123"));
				Assert.That(conn.Query<string>("SELECT @p", DataParameter.NText   ("p", "123")).First(), Is.EqualTo("123"));
				Assert.That(conn.Query<string>("SELECT @p", DataParameter.Create  ("p", "123")).First(), Is.EqualTo("123"));

				Assert.That(conn.Query<string>("SELECT @p", DataParameter.Create("p", (string)null)).First(), Is.EqualTo(null));
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
				Assert.That(conn.Query<byte[]>("SELECT @p", DataParameter.VarBinary("p", arr1)).First(), Is.EqualTo(arr1));
				Assert.That(conn.Query<byte[]>("SELECT @p", DataParameter.Create   ("p", arr1)).First(), Is.EqualTo(arr1));
				Assert.That(conn.Query<byte[]>("SELECT @p", DataParameter.VarBinary("p", null)).First(), Is.EqualTo(null));
				Assert.That(conn.Query<byte[]>("SELECT Cast(@p as binary(1))", DataParameter.Binary("p", new byte[0])).First(), Is.EqualTo(new byte[] {0}));
				Assert.That(conn.Query<byte[]>("SELECT @p", DataParameter.Binary("p", new byte[0])).First(), Is.EqualTo(new byte[8000]));
				Assert.That(conn.Query<byte[]>("SELECT @p", DataParameter.VarBinary("p", new byte[0])).First(), Is.EqualTo(new byte[0]));
				Assert.That(conn.Query<byte[]>("SELECT @p", new DataParameter { Name = "p", Value = arr1 }).First(), Is.EqualTo(arr1));
			}
		}

		[Test]
		public void TestSqlTypes([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<SqlBinary>  ("SELECT Cast(12345    as binary(2))").First().Value, Is.EqualTo(new byte[] { 48, 57 }));
				Assert.That(conn.Query<SqlBoolean> ("SELECT Cast(1        as bit)").      First().Value, Is.EqualTo(true));
				Assert.That(conn.Query<SqlByte>    ("SELECT Cast(1        as tinyint)").  First().Value, Is.EqualTo((byte)1));
				Assert.That(conn.Query<SqlDecimal> ("SELECT Cast(1        as decimal)").  First().Value, Is.EqualTo(1));
				Assert.That(conn.Query<SqlDouble>  ("SELECT Cast(1        as float)").    First().Value, Is.EqualTo(1.0));
				Assert.That(conn.Query<SqlInt16>   ("SELECT Cast(1        as smallint)"). First().Value, Is.EqualTo((short)1));
				Assert.That(conn.Query<SqlInt32>   ("SELECT Cast(1        as int)").      First().Value, Is.EqualTo((int)1));
				Assert.That(conn.Query<SqlInt64>   ("SELECT Cast(1        as bigint)").   First().Value, Is.EqualTo(1L));
				Assert.That(conn.Query<SqlMoney>   ("SELECT Cast(1        as money)").    First().Value, Is.EqualTo(1m));
				Assert.That(conn.Query<SqlSingle>  ("SELECT Cast(1        as real)").     First().Value, Is.EqualTo((float)1));
				Assert.That(conn.Query<SqlString>  ("SELECT Cast('12345'  as char(6))").  First().Value, Is.EqualTo("12345 "));
				Assert.That(conn.Query<SqlXml>     ("SELECT Cast('<xml/>' as xml)").      First().Value, Is.EqualTo("<xml />"));

				Assert.That(
					conn.Query<SqlDateTime>("SELECT Cast('2012-12-12 12:12:12' as datetime)").First().Value,
					Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));

				Assert.That(
					conn.Query<SqlGuid>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uniqueidentifier)").First().Value,
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));
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
			}
		}

		[Test]
		public void TestTimestamp([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<byte[]>("SELECT Cast(1 as timestamp)"). First(), Is.EqualTo(new byte[] { 0,0,0,0,0,0,0,1 }));
				Assert.That(conn.Query<byte[]>("SELECT Cast(1 as rowversion)").First(), Is.EqualTo(new byte[] { 0,0,0,0,0,0,0,1 }));
			}
		}

		[Test]
		public void TestSqlVariant([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<object>("SELECT Cast(1 as sql_variant)"). First(), Is.EqualTo(1));
				Assert.That(conn.Query<int>   ("SELECT Cast(1 as sql_variant)"). First(), Is.EqualTo(1));
				Assert.That(conn.Query<int?>  ("SELECT Cast(1 as sql_variant)"). First(), Is.EqualTo(1));
				Assert.That(conn.Query<string>("SELECT Cast(1 as sql_variant)"). First(), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestHierarchyID([IncludeDataContexts(ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var id = SqlHierarchyId.Parse("/1/3/");
				Assert.That(conn.Query<SqlHierarchyId> ("SELECT Cast('/1/3/' as hierarchyid)"). First(), Is.EqualTo(id));
				Assert.That(conn.Query<SqlHierarchyId?>("SELECT Cast('/1/3/' as hierarchyid)"). First(), Is.EqualTo(id));
			}
		}

		[Test]
		public void TestXml([IncludeDataContexts(ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<XDocument>  ("SELECT Cast('<xml/>' as xml)").First().ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Query<XmlDocument>("SELECT Cast('<xml/>' as xml)").First().InnerXml,   Is.EqualTo("<xml />"));
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
				Assert.That(conn.Query<string>("SELECT @p", new { p = (TestEnum?)TestEnum.BB }).First(),
					Is.EqualTo(context == ProviderName.SqlServer2008 ? "C" : "B"));

				Assert.That(conn.Query<string>("SELECT @p", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }).First(), Is.EqualTo("A"));
				Assert.That(conn.Query<string>("SELECT @p", new { p = ConvertTo<string>.From(TestEnum.AA) }).First(), Is.EqualTo("A"));
				Assert.That(conn.Query<string>("SELECT @p", new { p = conn.MappingSchema.GetConverter<TestEnum?,string>()(TestEnum.AA) }).First(), Is.EqualTo("A"));
			}
		}
	}
}
