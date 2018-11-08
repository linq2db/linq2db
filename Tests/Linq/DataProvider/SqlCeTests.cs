using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlCe;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.DataProvider
{
	using System.Globalization;
	using LinqToDB.Linq;
	using Model;

	[TestFixture]
	public class SqlCeTests : DataProviderTestBase
	{
		[Test]
		public void TestParameters([IncludeDataSources(ProviderName.SqlCe)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast(@p as int)",       new { p =  1  }), Is.EqualTo("1"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as nvarchar)",  new { p = "1" }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT Cast(@p as int)",       new { p =  new DataParameter { Value = 1   } }), Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT Cast(@p1 as nvarchar)", new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT @p1 + @p2",             new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
				Assert.That(conn.Execute<int>   ("SELECT @p2 + @p1",             new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
			}
		}

		[Test]
		public void TestDataTypes([IncludeDataSources(ProviderName.SqlCe)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(TestType<long?>    (conn, "bigintDataType",    DataType.UInt64,    skipUndefinedNull:true), Is.EqualTo(1000000L));
				Assert.That(TestType<decimal?> (conn, "numericDataType",   DataType.Decimal,   skipUndefinedNull:true), Is.EqualTo(9999999m));
				Assert.That(TestType<bool?>    (conn, "bitDataType",       DataType.Boolean,   skipUndefinedNull:true), Is.EqualTo(true));
				Assert.That(TestType<short?>   (conn, "smallintDataType",  DataType.Int16,     skipUndefinedNull:true), Is.EqualTo(25555));
				Assert.That(TestType<decimal?> (conn, "decimalDataType",   DataType.Decimal,   skipUndefinedNull:true), Is.EqualTo(2222222m));
				Assert.That(TestType<int?>     (conn, "intDataType",       DataType.Int32,     skipUndefinedNull:true), Is.EqualTo(7777777));
				Assert.That(TestType<sbyte?>   (conn, "tinyintDataType",   DataType.SByte,     skipUndefinedNull:true), Is.EqualTo(100));
				Assert.That(TestType<decimal?> (conn, "moneyDataType",     DataType.Money,     skipUndefinedNull:true), Is.EqualTo(100000m));
				Assert.That(TestType<double?>  (conn, "floatDataType",     DataType.Double,    skipUndefinedNull:true), Is.EqualTo(20.31d));
				Assert.That(TestType<float?>   (conn, "realDataType",      DataType.Single,    skipUndefinedNull:true), Is.EqualTo(16.2f));

				Assert.That(TestType<DateTime?>(conn, "datetimeDataType",  DataType.DateTime,  skipUndefinedNull:true), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));

				Assert.That(TestType<string>   (conn, "ncharDataType",     DataType.NChar,     skipUndefinedNull:true), Is.EqualTo("23233"));
				Assert.That(TestType<string>   (conn, "nvarcharDataType",  DataType.NVarChar,  skipUndefinedNull:true), Is.EqualTo("3323"));
				Assert.That(TestType<string>   (conn, "ntextDataType",     DataType.NText,     skipPass:true),          Is.EqualTo("111"));

				Assert.That(TestType<byte[]>   (conn, "binaryDataType",    DataType.Binary,    skipUndefinedNull:true), Is.EqualTo(new byte[] { 1 }));
				Assert.That(TestType<byte[]>   (conn, "varbinaryDataType", DataType.VarBinary, skipUndefinedNull:true), Is.EqualTo(new byte[] { 2 }));
				Assert.That(TestType<byte[]>   (conn, "imageDataType",     DataType.Image,     skipPass:true),          Is.EqualTo(new byte[] { 0, 0, 0, 3 }));

				Assert.That(TestType<Guid?>    (conn, "uniqueidentifierDataType", DataType.Guid, skipUndefinedNull:true), Is.EqualTo(new Guid("{6F9619FF-8B86-D011-B42D-00C04FC964FF}")));

				Assert.That(conn.Execute<byte[]>("SELECT timestampDataType FROM AllTypes WHERE ID = 1").Length, Is.EqualTo(8));
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
			Assert.That(conn.Execute<T>("SELECT @p + 0", new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> auto", typeof(T));
			Assert.That(conn.Execute<T>("SELECT @p + 0", new DataParameter { Name = "p", Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> new",  typeof(T));
			Assert.That(conn.Execute<T>("SELECT @p + 0", new { p = expectedValue }), Is.EqualTo(expectedValue));
		}

		static void TestSimple<T>(DataConnection conn, T expectedValue, DataType dataType)
			where T : struct
		{
			TestNumeric<T> (conn, expectedValue, dataType);
			TestNumeric<T?>(conn, expectedValue, dataType);
			TestNumeric<T?>(conn, (T?)null,      dataType);
		}

		[Test]
		public void TestNumerics([IncludeDataSources(ProviderName.SqlCe)] string context)
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
				TestNumeric(conn, 3.40282306E+38f,   DataType.Single,     "bigint bit decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint");
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
		public void TestDateTime([IncludeDataSources(ProviderName.SqlCe)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12 12:12:12' as datetime)"),                                   Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12' as datetime)"),                                   Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime> ("SELECT DateAdd(day, 0, @p)", DataParameter.DateTime("p", dateTime)),                Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT DateAdd(day, 0, @p)", new DataParameter("p", dateTime)),                     Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT DateAdd(day, 0, @p)", new DataParameter("p", dateTime, DataType.DateTime)),  Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT DateAdd(day, 0, @p)", new DataParameter("p", dateTime, DataType.DateTime2)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestChar([IncludeDataSources(ProviderName.SqlCe)] string context)
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

				Assert.That(conn.Execute<char> ("SELECT RTRIM(@p)",            DataParameter.Char ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT RTRIM(@p)",            DataParameter.NChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(@p as nchar)",    DataParameter.Char ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as nchar)",    DataParameter.Char ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(@p as nchar(1))", DataParameter.Char ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as nchar(1))", DataParameter.Char ("p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT @p + ''",   DataParameter.VarChar ("p", 'A')), Is.EqualTo('A'));
				Assert.That(conn.Execute<char?>("SELECT RTRIM(@p)", DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT RTRIM(@p)", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT RTRIM(@p)", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT RTRIM(@p)", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT RTRIM(@p)", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT RTRIM(@p)", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT RTRIM(@p)", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT RTRIM(@p)", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT RTRIM(@p)", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
			}
		}

		[Test]
		public void TestString([IncludeDataSources(ProviderName.SqlCe)] string context)
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
				Assert.That(conn.Execute<string>("SELECT @p + ''",           DataParameter.VarChar ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as ntext)", DataParameter.Text    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as nchar)", DataParameter.NChar   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p + ''",           DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as ntext)", DataParameter.NText   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p + ''",           DataParameter.Create  ("p", "123")), Is.EqualTo("123"));

				Assert.That(conn.Execute<string>("SELECT @p + ''",           DataParameter.Create("p", (string)null)), Is.EqualTo(null));
				Assert.That(conn.Execute<string>("SELECT @p + ''",           new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestBinary([IncludeDataSources(ProviderName.SqlCe)] string context)
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

				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as binary(2))",    DataParameter.Binary   ("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as varbinary(2))", DataParameter.VarBinary("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as varbinary(2))", DataParameter.Create   ("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as varbinary)",    DataParameter.VarBinary("p", null)), Is.EqualTo(null));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as binary(1))",    DataParameter.Binary   ("p", new byte[0])), Is.EqualTo(new byte[] {0}));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as binary)",       DataParameter.Binary   ("p", new byte[0])), Is.EqualTo(new byte[8000]));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as varbinary)",    DataParameter.VarBinary("p", new byte[0])), Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as image)",        DataParameter.Image    ("p", new byte[0])), Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as varbinary)",    new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as varbinary)",    DataParameter.Create   ("p", new Binary(arr1))), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as varbinary)",    new DataParameter("p", new Binary(arr1))), Is.EqualTo(arr1));
			}
		}

		[Test]
		public void TestSqlTypes([IncludeDataSources(ProviderName.SqlCe)] string context)
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

				Assert.That(conn.Execute<SqlBinary> ("SELECT Cast(@p as varbinary)", new DataParameter("p", new SqlBinary(arr))).                    Value, Is.EqualTo(arr));
				Assert.That(conn.Execute<SqlBinary> ("SELECT Cast(@p as varbinary)", new DataParameter("p", new SqlBinary(arr), DataType.VarBinary)).Value, Is.EqualTo(arr));

				Assert.That(conn.Execute<SqlBoolean>("SELECT Cast(@p as bit)",       new DataParameter("p", true)).                  Value, Is.EqualTo(true));
				Assert.That(conn.Execute<SqlBoolean>("SELECT Cast(@p as bit)",       new DataParameter("p", true, DataType.Boolean)).Value, Is.EqualTo(true));

				var conv = conn.MappingSchema.GetConverter<string,SqlXml>();

				Assert.That(conn.Execute<SqlXml>("SELECT Cast(@p as nvarchar)",      new DataParameter("p", conv("<xml/>"))).              Value, Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<SqlXml>("SELECT Cast(@p as nvarchar)",      new DataParameter("p", conv("<xml/>"), DataType.Xml)).Value, Is.EqualTo("<xml />"));
			}
		}

		[Test]
		public void TestGuid([IncludeDataSources(ProviderName.SqlCe)] string context)
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
				Assert.That(conn.Execute<Guid>("SELECT Cast(@p as uniqueidentifier)", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test]
		public void TestTimestamp([IncludeDataSources(ProviderName.SqlCe)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var arr = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };

				Assert.That(conn.Execute<byte[]>("SELECT Cast(1 as timestamp)"),  Is.EqualTo(arr));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(1 as rowversion)"), Is.EqualTo(arr));

				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as timestamp)", DataParameter.Timestamp("p", arr)),               Is.EqualTo(arr));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as timestamp)", new DataParameter("p", arr, DataType.Timestamp)), Is.EqualTo(arr));
			}
		}

		[Test]
		public void TestXml([IncludeDataSources(ProviderName.SqlCe)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>     ("SELECT Cast('<xml/>' as nvarchar)"),            Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast('<xml/>' as nvarchar)").ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT Cast('<xml/>' as nvarchar)").InnerXml,   Is.EqualTo("<xml />"));

				var xdoc = XDocument.Parse("<xml/>");
				var xml  = Convert<string,XmlDocument>.Lambda("<xml/>");

				Assert.That(conn.Execute<string>     ("SELECT Cast(@p as nvarchar)", DataParameter.Xml("p", "<xml/>")),        Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast(@p as nvarchar)", DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT Cast(@p as nvarchar)", DataParameter.Xml("p", xml)). InnerXml,   Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast(@p as nvarchar)", new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast(@p as nvarchar)", new DataParameter("p", xml)). ToString(), Is.EqualTo("<xml />"));
			}
		}

		enum TestEnum
		{
			[MapValue("A")] AA,
			[MapValue("B")] BB,
		}

		[Test]
		public void TestEnum1([IncludeDataSources(ProviderName.SqlCe)] string context)
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
		public void TestEnum2([IncludeDataSources(ProviderName.SqlCe)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast(@p as nvarchar)", new { p = TestEnum.AA }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as nvarchar)", new { p = (TestEnum?)TestEnum.BB }), Is.EqualTo("B"));

				Assert.That(conn.Execute<string>("SELECT Cast(@p as nvarchar)", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as nvarchar)", new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as nvarchar)", new { p = conn.MappingSchema.GetConverter<TestEnum?,string>()(TestEnum.AA) }), Is.EqualTo("A"));
			}
		}

		[Table(Name = "CreateTableTest", Schema = "IgnoreSchema", Database = "TestDatabase")]
		public class CreateTableTest
		{
			[PrimaryKey, Identity]
			public int Id;
		}

		[Test]
		public void CreateDatabase([IncludeDataSources(ProviderName.SqlCe)] string context)
		{
			SqlCeTools.CreateDatabase ("TestDatabase");
			Assert.IsTrue(File.Exists ("TestDatabase.sdf"));

			using (var db = new DataConnection(SqlCeTools.GetDataProvider(), "Data Source=TestDatabase.sdf"))
			{
				db.CreateTable<CreateTableTest>();
				db.DropTable  <CreateTableTest>();
			}

			SqlCeTools.DropDatabase   ("TestDatabase");
			Assert.IsFalse(File.Exists("TestDatabase.sdf"));
		}

		[Test]
		public void BulkCopyLinqTypes([IncludeDataSources(ProviderName.SqlCe)] string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using (var db = new DataConnection(context))
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
								GuidValue     = Guid.NewGuid(),
								SmallIntValue = (short)n
							}
						));

					db.GetTable<LinqDataTypes>().Delete(p => p.ID >= 4000);
				}
			}
		}

#if !NETSTANDARD1_6
		[Test]
		public void Issue695Test([IncludeDataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var sp = db.DataProvider.GetSchemaProvider();
				var sh = sp.GetSchema(db, TestUtils.GetDefaultSchemaOptions(context));
				var t  = sh.Tables.Single(_ => _.TableName.Equals("Issue695", StringComparison.OrdinalIgnoreCase));

				Assert.AreEqual(2, t.Columns.Count);
				Assert.AreEqual(1, t.Columns.Count(_ => _.IsPrimaryKey));
				Assert.AreEqual(1, t.ForeignKeys.Count);
			}
		}
#endif

		[Test]
		public void SelectTableWithHintTest([IncludeDataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(Person, db.Person.With("TABLOCK"));
			}
		}

		[Test]
		public void UpdateTableWithHintTest([IncludeDataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
			{
				Assert.AreEqual(Person.Count(),  db.Person.                Set(_ => _.FirstName, _ => _.FirstName).Update());
				Assert.AreEqual(Person.Count(),  db.Person.With("TABLOCK").Set(_ => _.FirstName, _ => _.FirstName).Update());
			}
		}

		[Table("AllTypes")]
		public class TestInline
		{
			[Column("datetimeDataType")]
			public DateTime? DateTimeValue { get; set; }
		}

		[Test]
		public void ParametersInlining(
			[IncludeDataSources(false, ProviderName.SqlCe)] string context,
			[Values] bool inline)
		{
			Query.ClearCaches();
			var defaultValue = SqlCeConfiguration.InlineFunctionParameters;
			try
			{
				SqlCeConfiguration.InlineFunctionParameters = inline;
				using (var db = new DataConnection(context))
				{
					var values = db.GetTable<TestInline>()
						.Where(_ => (_.DateTimeValue ?? SqlDateTime.MinValue.Value) <= DateTime.Now)
						.ToList();

					Assert.True(db.LastQuery.Contains(", @p") != inline);
				}
			}
			finally
			{
				SqlCeConfiguration.InlineFunctionParameters = defaultValue;
			}
		}
	}
}
