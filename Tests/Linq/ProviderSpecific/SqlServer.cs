using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Microsoft.SqlServer.Types;

using NUnit.Framework;

namespace Tests.ProviderSpecific
{
	[TestFixture]
	public class SqlServer : TestBase
	{
		static void TestNumerics<T>(DataConnection conn, T expectedValue)
		{
			foreach (var sqlType in new[]
				{
					"bigint",
					"bit",
					"decimal",
					"int",
					"money",
					"numeric",
					"smallint",
					"smallmoney",
					"tinyint",

					"float",
					"real"
				})
			{
				var sql = string.Format("SELECT Cast(1 as {0})", sqlType);

				Debug.WriteLine(sql + " -> " + typeof(T));

				Assert.That(conn.Query<T>(sql).First(), Is.EqualTo(expectedValue));
			}
		}

		[Test]
		public void TestNumerics([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				TestNumerics(conn, (bool)   true);

				TestNumerics(conn, (sbyte)  1);
				TestNumerics(conn, (short)  1);
				TestNumerics(conn, (int)    1);
				TestNumerics(conn, (long)   1l);

				TestNumerics(conn, (byte)   1);
				TestNumerics(conn, (ushort) 1);
				TestNumerics(conn, (uint)   1u);
				TestNumerics(conn, (ulong)  1ul);

				TestNumerics(conn, (float)  1);
				TestNumerics(conn, (double) 1d);
				TestNumerics(conn, (decimal)1m);
			}
		}

		[Test]
		public void TestDateTime([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<DateTimeOffset>(
					"SELECT Cast('2012-12-12 12:12:12.012 +05:00' as datetimeoffset)").First(),
					Is.EqualTo(new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, new TimeSpan(5, 0, 0))));

				Assert.That(conn.Query<DateTime>(
					"SELECT Cast('2012-12-12 13:12:12.012 -04:00' as datetimeoffset)").First(),
					Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));

				Assert.That(conn.Query<DateTimeOffset>(
					"SELECT Cast('2012-12-12 12:12:12.012' as datetime2)").First(),
					Is.EqualTo(new DateTimeOffset(2012, 12, 12, 12, 12, 12, 12, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2012, 12, 12, 12, 12, 12)))));

				Assert.That(conn.Query<DateTime>(
					"SELECT Cast(NULL as datetimeoffset)").First(),
					Is.EqualTo(default(DateTime)));

				Assert.That(conn.Query<DateTime>("SELECT Cast('2012-12-12' as date)").First(),                   Is.EqualTo(new DateTime(2012, 12, 12)));
				Assert.That(conn.Query<DateTime>("SELECT Cast('2012-12-12 12:12:00' as smalldatetime)").First(), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 00)));
				Assert.That(conn.Query<DateTime>("SELECT Cast('2012-12-12 12:12:12' as datetime)").First(),      Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(conn.Query<DateTime>("SELECT Cast('2012-12-12 12:12:12.012' as datetime2)").First(), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12, 12)));
				Assert.That(conn.Query<TimeSpan>("SELECT Cast('12:12:12' as time)").First(),                     Is.EqualTo(new TimeSpan(12, 12, 12)));
			}
		}

		[Test]
		public void TestChar([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<char>("SELECT Cast('1' as char)").        First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char>("SELECT Cast('1' as char(1))").     First(), Is.EqualTo('1'));

				Assert.That(conn.Query<char>("SELECT Cast('1' as varchar)").     First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char>("SELECT Cast('1' as varchar(20))"). First(), Is.EqualTo('1'));

				Assert.That(conn.Query<char>("SELECT Cast('1' as nchar)").       First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char>("SELECT Cast('1' as nchar(20))").   First(), Is.EqualTo('1'));

				Assert.That(conn.Query<char>("SELECT Cast('1' as nvarchar)").    First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char>("SELECT Cast('1' as nvarchar(20))").First(), Is.EqualTo('1'));

				Assert.That(conn.Query<char>("SELECT @p",                  DataParameter.Char("p",  '1')).First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char>("SELECT Cast(@p as char)",    DataParameter.Char("p",  '1')).First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char>("SELECT Cast(@p as char(1))", DataParameter.Char("@p", '1')).First(), Is.EqualTo('1'));

				Assert.That(conn.Query<char>("SELECT @p", DataParameter.VarChar ("p", '1')).First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char>("SELECT @p", DataParameter.NChar   ("p", '1')).First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char>("SELECT @p", DataParameter.NVarChar("p", '1')).First(), Is.EqualTo('1'));
				Assert.That(conn.Query<char>("SELECT @p", DataParameter.Create  ("p", '1')).First(), Is.EqualTo('1'));

				Assert.That(conn.Query<char>("SELECT @p", new DataParameter { Name = "p", Value = '1' }).First(), Is.EqualTo('1'));
			}
		}

		[Test]
		public void TestString([IncludeDataContexts(ProviderName.SqlServer)] string context)
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

				Assert.That(conn.Query<string>("SELECT @p", DataParameter.Create("p", null)).First(), Is.EqualTo(null));
				Assert.That(conn.Query<string>("SELECT @p", new DataParameter { Name = "p", Value = "1" }).First(), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestBinary([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<byte[]>("SELECT Cast(12345 as binary(2))").     First(), Is.EqualTo(           new byte[] {       48, 57 }));
				Assert.That(conn.Query<Binary>("SELECT Cast(12345 as binary(4))").     First(), Is.EqualTo(new Binary(new byte[] { 0, 0, 48, 57 })));

				Assert.That(conn.Query<byte[]>("SELECT Cast(12345 as varbinary(2))").  First(), Is.EqualTo(           new byte[] {       48, 57 }));
				Assert.That(conn.Query<Binary>("SELECT Cast(12345 as varbinary(4))").  First(), Is.EqualTo(new Binary(new byte[] { 0, 0, 48, 57 })));

				Assert.That(conn.Query<byte[]>("SELECT Cast(NULL as image)").          First(), Is.EqualTo(null));
				Assert.That(conn.Query<byte[]>("SELECT Cast(12345 as varbinary(max))").First(), Is.EqualTo(           new byte[] { 0, 0, 48, 57 }));
			}
		}

		[Test]
		public void TestSqlTypes([IncludeDataContexts(ProviderName.SqlServer)] string context)
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
				Assert.That(conn.Query<SqlInt64>   ("SELECT Cast(1        as bigint)").   First().Value, Is.EqualTo(1l));
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
		public void TestGuid([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(
					conn.Query<Guid>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uniqueidentifier)").First(),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));
			}
		}

		[Test]
		public void TestTimestamp([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<byte[]>("SELECT Cast(1 as timestamp)"). First(), Is.EqualTo(new byte[] { 0,0,0,0,0,0,0,1 }));
				Assert.That(conn.Query<byte[]>("SELECT Cast(1 as rowversion)").First(), Is.EqualTo(new byte[] { 0,0,0,0,0,0,0,1 }));
			}
		}

		[Test]
		public void TestSqlVariant([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<object>("SELECT Cast(1 as sql_variant)"). First(), Is.EqualTo(1));
				Assert.That(conn.Query<int>   ("SELECT Cast(1 as sql_variant)"). First(), Is.EqualTo(1));
				Assert.That(conn.Query<string>("SELECT Cast(1 as sql_variant)"). First(), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestHierarchyID([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var id = SqlHierarchyId.Parse("/1/3/");
				Assert.That(conn.Query<SqlHierarchyId>("SELECT Cast('/1/3/' as hierarchyid)"). First(), Is.EqualTo(id));
			}
		}

		[Test]
		public void TestXml([IncludeDataContexts(ProviderName.SqlServer)] string context)
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
			[MapValue("B")] BB,
		}

		[Test]
		public void TestEnum1([IncludeDataContexts(ProviderName.SqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Query<TestEnum>("SELECT 'A'").First(), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Query<TestEnum>("SELECT 'B'").First(), Is.EqualTo(TestEnum.BB));
			}
		}
	}
}
