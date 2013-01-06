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

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class SqlCeTest : TestBase
	{
		static void TestType<T>(DataConnection connection, string dataTypeName, T value, string tableName = "AllTypes", bool convertToString = false)
		{
			connection.Command.Parameters.Clear();
			Assert.That(connection.Execute<T>(string.Format("SELECT {0} FROM {1} WHERE ID = 1", dataTypeName, tableName)),
				Is.EqualTo(connection.MappingSchema.GetDefaultValue(typeof(T))));

			connection.Command.Parameters.Clear();

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
		public void TestDataTypes([IncludeDataContexts(ProviderName.SqlCe)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				TestType(conn, "bigintDataType",           1000000L);
				TestType(conn, "numericDataType",          9999999m);
				TestType(conn, "bitDataType",              true);
				TestType(conn, "smallintDataType",         (short)25555);
				TestType(conn, "decimalDataType",          2222222m);
				TestType(conn, "intDataType",              7777777);
				TestType(conn, "tinyintDataType",          (sbyte)100);
				TestType(conn, "moneyDataType",            100000m);
				TestType(conn, "floatDataType",            20.31d);
				TestType(conn, "realDataType",             16.2f);

				TestType(conn, "datetimeDataType",         new DateTime(2012, 12, 12, 12, 12, 12));

				TestType(conn, "ncharDataType",            "23233");
				TestType(conn, "nvarcharDataType",         "3323");
				TestType(conn, "ntextDataType",            "111");

				TestType(conn, "binaryDataType",           new byte[] { 1 });
				TestType(conn, "varbinaryDataType",        new byte[] { 2 });
				TestType(conn, "imageDataType",            new byte[] { 0, 0, 0, 3 });

				TestType(conn, "uniqueidentifierDataType", new Guid("{6F9619FF-8B86-D011-B42D-00C04FC964FF}"));

				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<byte[]>("SELECT timestampDataType FROM AllTypes WHERE ID = 1").Length, Is.EqualTo(8));
			}
		}

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
					"tinyint",

					"float",
					"real"
				}.Except(skipTypes))
			{
				var sqlValue = expectedValue is bool ? (bool)(object)expectedValue? 1 : 0 : (object)expectedValue;

				var sql = string.Format("SELECT Cast({0} as {1})", sqlValue, sqlType);

				Debug.WriteLine(sql + " -> " + typeof(T));

				Assert.That(conn.Execute<T>(sql), Is.EqualTo(expectedValue));
			}

			conn.Command.Parameters.Clear();
			Assert.That(conn.Execute<T>("SELECT @p + 0", new DataParameter { Name = "@p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
			conn.Command.Parameters.Clear();
			Assert.That(conn.Execute<T>("SELECT @p + 0", new DataParameter { Name = "@p", Value = expectedValue }), Is.EqualTo(expectedValue));
			conn.Command.Parameters.Clear();
			Assert.That(conn.Execute<T>("SELECT @p + 0", new { p = expectedValue }), Is.EqualTo(expectedValue));
		}

		[Test]
		public void TestNumerics([IncludeDataContexts(ProviderName.SqlCe)] string context)
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
		public void TestDateTime([IncludeDataContexts(ProviderName.SqlCe)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12 12:12:12' as datetime)"),                 Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12' as datetime)"),                 Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime> ("SELECT DateAdd(day, 0, @p)", DataParameter.DateTime("p", dateTime)),               Is.EqualTo(dateTime));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<DateTime?>("SELECT DateAdd(day, 0, @p)", new DataParameter("p", dateTime)),                    Is.EqualTo(dateTime));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<DateTime?>("SELECT DateAdd(day, 0, @p)", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestChar([IncludeDataContexts(ProviderName.SqlCe)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as nchar)"),        Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nchar)"),        Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as nchar(20))"),    Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nchar(20))"),    Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast('1' as nvarchar)"),     Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nvarchar)"),     Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as nvarchar(20))"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nvarchar(20))"), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT RTRIM(@p)",            DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<char?>("SELECT RTRIM(@p)",            DataParameter.NChar("p",  '1')), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<char> ("SELECT Cast(@p as nchar)",    DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as nchar)",    DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<char> ("SELECT Cast(@p as nchar(1))", DataParameter.Char("@p", '1')), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as nchar(1))", DataParameter.Char("@p", '1')), Is.EqualTo('1'));

				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<char> ("SELECT @p + ''",   DataParameter.VarChar ("p", 'A')), Is.EqualTo('A'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<char?>("SELECT RTRIM(@p)", DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<char> ("SELECT RTRIM(@p)", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<char?>("SELECT RTRIM(@p)", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<char> ("SELECT RTRIM(@p)", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<char?>("SELECT RTRIM(@p)", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<char> ("SELECT RTRIM(@p)", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<char?>("SELECT RTRIM(@p)", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));

				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<char> ("SELECT RTRIM(@p)", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<char?>("SELECT RTRIM(@p)", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
			}
		}

		[Test]
		public void TestString([IncludeDataContexts(ProviderName.SqlCe)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nchar)"),         Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nchar(20))"),     Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as nchar(20))"),     Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nvarchar)"),      Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nvarchar(20))"),  Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as nvarchar(20))"),  Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as ntext)"),         Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as ntext)"),         Is.Null);

				Assert.That(conn.Execute<string>("SELECT RTRIM(@p)",         DataParameter.Char    ("p", "123")), Is.EqualTo("123"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<string>("SELECT @p + ''",           DataParameter.VarChar ("p", "123")), Is.EqualTo("123"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<string>("SELECT Cast(@p as ntext)", DataParameter.Text    ("p", "123")), Is.EqualTo("123"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<string>("SELECT Cast(@p as nchar)", DataParameter.NChar   ("p", "123")), Is.EqualTo("123"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<string>("SELECT @p + ''",           DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<string>("SELECT Cast(@p as ntext)", DataParameter.NText   ("p", "123")), Is.EqualTo("123"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<string>("SELECT @p + ''",           DataParameter.Create  ("p", "123")), Is.EqualTo("123"));

				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<string>("SELECT @p + ''",           DataParameter.Create("p", (string)null)), Is.EqualTo(null));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<string>("SELECT @p + ''",           new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestBinary([IncludeDataContexts(ProviderName.SqlCe)] string context)
		{
			var arr1 = new byte[] {       48, 57 };
			var arr2 = new byte[] { 0, 0, 48, 57 };

			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<byte[]>("SELECT Cast(12345 as binary(2))"),      Is.EqualTo(           arr1));
				Assert.That(conn.Execute<Binary>("SELECT Cast(12345 as binary(4))"),      Is.EqualTo(new Binary(arr2)));

				Assert.That(conn.Execute<byte[]>("SELECT Cast(12345 as varbinary(2))"),   Is.EqualTo(           arr1));
				Assert.That(conn.Execute<Binary>("SELECT Cast(12345 as varbinary(4))"),   Is.EqualTo(new Binary(arr2)));

				Assert.That(conn.Execute<byte[]>("SELECT Cast(NULL as image)"),           Is.EqualTo(null));

				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as binary(2))",    DataParameter.Binary   ("p", arr1)), Is.EqualTo(arr1));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as varbinary(2))", DataParameter.VarBinary("p", arr1)), Is.EqualTo(arr1));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as varbinary(2))", DataParameter.Create   ("p", arr1)), Is.EqualTo(arr1));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as varbinary)",    DataParameter.VarBinary("p", null)), Is.EqualTo(null));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as binary(1))",    DataParameter.Binary   ("p", new byte[0])), Is.EqualTo(new byte[] {0}));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as binary)",       DataParameter.Binary   ("p", new byte[0])), Is.EqualTo(new byte[8000]));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as varbinary)",    DataParameter.VarBinary("p", new byte[0])), Is.EqualTo(new byte[0]));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as image)",        DataParameter.Image    ("p", new byte[0])), Is.EqualTo(new byte[0]));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as varbinary)",    new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as varbinary)",    DataParameter.Create   ("p", new Binary(arr1))), Is.EqualTo(arr1));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as varbinary)",    new DataParameter("p", new Binary(arr1))), Is.EqualTo(arr1));
			}
		}

		[Test]
		public void TestSqlTypes([IncludeDataContexts(ProviderName.SqlCe)] string context)
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
				Assert.That(conn.Execute<SqlString> ("SELECT Cast('12345'  as nchar(6))"). Value, Is.EqualTo("12345"));
				Assert.That(conn.Execute<SqlXml>    ("SELECT Cast('<xml/>' as nvarchar)"). Value, Is.EqualTo("<xml />"));

				Assert.That(
					conn.Execute<SqlDateTime>("SELECT Cast('2012-12-12 12:12:12' as datetime)").Value,
					Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));

				Assert.That(
					conn.Execute<SqlGuid>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uniqueidentifier)").Value,
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<SqlBinary> ("SELECT Cast(@p as varbinary)", new DataParameter("p", new SqlBinary(arr))).                    Value, Is.EqualTo(arr));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<SqlBinary> ("SELECT Cast(@p as varbinary)", new DataParameter("p", new SqlBinary(arr), DataType.VarBinary)).Value, Is.EqualTo(arr));

				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<SqlBoolean>("SELECT Cast(@p as bit)",       new DataParameter("p", true)).                  Value, Is.EqualTo(true));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<SqlBoolean>("SELECT Cast(@p as bit)",       new DataParameter("p", true, DataType.Boolean)).Value, Is.EqualTo(true));

				var conv = conn.MappingSchema.GetConverter<string,SqlXml>();

				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<SqlXml>("SELECT Cast(@p as nvarchar)",      new DataParameter("p", conv("<xml/>"))).              Value, Is.EqualTo("<xml />"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<SqlXml>("SELECT Cast(@p as nvarchar)",      new DataParameter("p", conv("<xml/>"), DataType.Xml)).Value, Is.EqualTo("<xml />"));
			}
		}

		[Test]
		public void TestGuid([IncludeDataContexts(ProviderName.SqlCe)] string context)
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

				Assert.That(conn.Execute<Guid>("SELECT Cast(@p as uniqueidentifier)", DataParameter.Create("p", guid)),                Is.EqualTo(guid));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<Guid>("SELECT Cast(@p as uniqueidentifier)", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test]
		public void TestTimestamp([IncludeDataContexts(ProviderName.SqlCe)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var arr = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };

				Assert.That(conn.Execute<byte[]>("SELECT Cast(1 as timestamp)"),  Is.EqualTo(arr));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(1 as rowversion)"), Is.EqualTo(arr));

				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as timestamp)", DataParameter.Timestamp("p", arr)),               Is.EqualTo(arr));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as timestamp)", new DataParameter("p", arr, DataType.Timestamp)), Is.EqualTo(arr));
			}
		}

		[Test]
		public void TestXml([IncludeDataContexts(ProviderName.SqlCe)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>     ("SELECT Cast('<xml/>' as nvarchar)"),            Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast('<xml/>' as nvarchar)").ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT Cast('<xml/>' as nvarchar)").InnerXml,   Is.EqualTo("<xml />"));

				var xdoc = XDocument.Parse("<xml/>");
				var xml  = Convert<string,XmlDocument>.Lambda("<xml/>");

				Assert.That(conn.Execute<string>     ("SELECT Cast(@p as nvarchar)", DataParameter.Xml("p", "<xml/>")),        Is.EqualTo("<xml/>"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast(@p as nvarchar)", DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<XmlDocument>("SELECT Cast(@p as nvarchar)", DataParameter.Xml("p", xml)). InnerXml,   Is.EqualTo("<xml />"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast(@p as nvarchar)", new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast(@p as nvarchar)", new DataParameter("p", xml)). ToString(), Is.EqualTo("<xml />"));
			}
		}

		enum TestEnum
		{
			[MapValue("A")] AA,
			[MapValue("B")] BB,
		}

		[Test]
		public void TestEnum1([IncludeDataContexts(ProviderName.SqlCe)] string context)
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
		public void TestEnum2([IncludeDataContexts(ProviderName.SqlCe)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast(@p as nvarchar)", new { p = TestEnum.AA }), Is.EqualTo("A"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<string>("SELECT Cast(@p as nvarchar)", new { p = (TestEnum?)TestEnum.BB }), Is.EqualTo("B"));

				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<string>("SELECT Cast(@p as nvarchar)", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<string>("SELECT Cast(@p as nvarchar)", new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<string>("SELECT Cast(@p as nvarchar)", new { p = conn.MappingSchema.GetConverter<TestEnum?,string>()(TestEnum.AA) }), Is.EqualTo("A"));
			}
		}

		[Test]
		public void TestCast([IncludeDataContexts(ProviderName.SqlCe)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast(@p as int)",      new { p =  1  }), Is.EqualTo("1"));
				conn.Command.Parameters.Clear();
				Assert.That(conn.Execute<string>("SELECT Cast(@p as nvarchar)", new { p = "1" }), Is.EqualTo("1"));
			}
		}
	}
}
