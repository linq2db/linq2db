using System;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Internal.DataProvider.SQLite;
using LinqToDB.Internal.Logging;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using LinqToDB.Tools.Activity;

using NUnit.Framework;

using Tests.Model;

using Binary = System.Data.Linq.Binary;

namespace Tests.DataProvider
{
	[TestFixture]
	public class SQLiteTests : TestBase
	{
		[Test]
		public void TestParameters([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var conn = GetDataContext(context);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<string>("SELECT @p", new { p = 1 }), Is.EqualTo("1"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = "1" }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>("SELECT @p", new { p = new DataParameter { Value = 1 } }), Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT @p1", new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>("SELECT @p1 + @p2", new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
				Assert.That(conn.Execute<int>("SELECT @p2 + @p1", new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
			}
		}

		static void TestType<T>(IDataContext connection, string dataTypeName, T value, string tableName = "AllTypes", bool convertToString = false)
			where T : notnull
		{
			Assert.That(connection.Execute<T>(string.Format("SELECT {0} FROM {1} WHERE ID = 1", dataTypeName, tableName)),
				Is.EqualTo(connection.MappingSchema.GetDefaultValue(typeof(T))));

			object actualValue   = connection.Execute<T>(string.Format("SELECT {0} FROM {1} WHERE ID = 2", dataTypeName, tableName))!;
			object expectedValue = value;

			if (convertToString)
			{
				actualValue   = actualValue.  ToString()!;
				expectedValue = expectedValue.ToString()!;
			}

			Assert.That(actualValue, Is.EqualTo(expectedValue));
		}

		[Test]
		public void TestDataTypes([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var conn = GetDataContext(context);
			TestType(conn, "bigintDataType", 1000000L);
			TestType(conn, "numericDataType", 9999999m);
			TestType(conn, "bitDataType", true);
			TestType(conn, "smallintDataType", (short)25555);
			TestType(conn, "decimalDataType", 2222222m);
			TestType(conn, "intDataType", 7777777);
			TestType(conn, "tinyintDataType", (sbyte)100);
			TestType(conn, "moneyDataType", 100000m);
			TestType(conn, "floatDataType", 20.31d);
			TestType(conn, "realDataType", 16.2f);

			TestType(conn, "datetimeDataType", new DateTime(2012, 12, 12, 12, 12, 12));

			TestType(conn, "charDataType", '1');
			TestType(conn, "varcharDataType", "234");
			TestType(conn, "textDataType", "567");
			TestType(conn, "ncharDataType", "23233");
			TestType(conn, "nvarcharDataType", "3323");
			TestType(conn, "ntextDataType", "111");

			TestType(conn, "binaryDataType", new byte[] { 1 });
			TestType(conn, "varbinaryDataType", new byte[] { 2 });
			TestType(conn, "imageDataType", new byte[] { 0, 0, 0, 3 });

			TestType(conn, "uniqueidentifierDataType", new Guid("{6F9619FF-8B86-D011-B42D-00C04FC964FF}"));
			TestType(conn, "objectDataType", (object)10);
		}

		// this adds type as string using single quotes, but sqlite understands such syntax
		[Sql.Expression("CAST({0} as {1})", ServerSideOnly = true)]
		static TValue Cast<TValue>(TValue value, string type)
		{
			throw new InvalidOperationException();
		}

		static void TestNumeric<T>(IDataContext conn, T expectedValue, DataType dataType, string skip = "")
		{
			var skipTypes = skip.Split(' ');

			conn.InlineParameters = true;

			foreach (var sqlType in new[]
				{
					"bigint",
					"bit",
					"decimal",
					"int",
					"money",
					"numeric",
					"smallint",
					"tinyint",

					"float",
					"real"
				}.Except(skipTypes))
			{
				var result = conn.Select(() => Cast(expectedValue, sqlType))!;

				// sqlite floating point parser doesn't restore roundtrip values properly
				// and also deviation could differ for different versions of engine
				// see https://system.data.sqlite.org/index.html/tktview/fb9e4b30874d83042e09c2f791d6065fc5e73a4b
				// and TestDoubleRoundTrip test
				if (expectedValue is double doubleValue)
					Assert.That((double)(object)result, Is.EqualTo(doubleValue).Within(Math.Abs(doubleValue) * 1e-15));
				else if (expectedValue is float floatValue)
					Assert.That((float)(object)result, Is.EqualTo(floatValue).Within(Math.Abs(floatValue) * 1e-9));
				else
					Assert.That(result, Is.EqualTo(expectedValue));
			}

			conn.InlineParameters = false;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<T>("SELECT @p", new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
				Assert.That(conn.Execute<T>("SELECT @p", new DataParameter { Name = "p", Value = expectedValue }), Is.EqualTo(expectedValue));
				Assert.That(conn.Execute<T>("SELECT @p", new { p = expectedValue }), Is.EqualTo(expectedValue));
			}
		}

		static void TestSimple<T>(IDataContext conn, T expectedValue, DataType dataType, string skip = "")
			where T : struct
		{
			TestNumeric<T> (conn, expectedValue, dataType, skip);
			TestNumeric<T?>(conn, expectedValue, dataType, skip);
			TestNumeric<T?>(conn, (T?)null,      dataType, skip);
		}

		[Test]
		public void TestNumerics([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			// culture region needed if tests run on system with non-dot decimal separator, e.g. nl-NL
			using (new InvariantCultureRegion())
			using (var conn = GetDataContext(context))
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
				TestSimple<float>  (conn, 1f,   DataType.Single);
				TestSimple<float>  (conn, 1.1f, DataType.Single,      "bigint int smallint tinyint");
				TestSimple<double> (conn, 1d,   DataType.Double);
				TestSimple<double> (conn, 1.1d, DataType.Double,      "bigint int smallint tinyint");
				TestSimple<decimal>(conn, 1m,   DataType.Decimal);
				//TestSimple<decimal>(conn, 1.1m, DataType.Decimal,     "bigint int smallint tinyint");
				TestSimple<decimal>(conn, 1m,   DataType.VarNumeric);
				//TestSimple<decimal>(conn, 1.1m, DataType.VarNumeric,  "bigint int smallint tinyint");
				TestSimple<decimal>(conn, 1m,   DataType.Money);
				//TestSimple<decimal>(conn, 1.1m, DataType.Money,       "bigint int smallint tinyint");
				TestSimple<decimal>(conn, 1m,   DataType.SmallMoney);
				//TestSimple<decimal>(conn, 1.1m, DataType.SmallMoney,  "bigint int smallint tinyint");

				TestNumeric(conn, sbyte.MinValue,    DataType.SByte);
				TestNumeric(conn, sbyte.MaxValue,    DataType.SByte);
				TestNumeric(conn, short.MinValue,    DataType.Int16);
				TestNumeric(conn, short.MaxValue,    DataType.Int16);
				TestNumeric(conn, int.  MinValue,    DataType.Int32);
				TestNumeric(conn, int.  MaxValue,    DataType.Int32);
				TestNumeric(conn, long. MinValue,    DataType.Int64);
				TestNumeric(conn, long. MaxValue,    DataType.Int64,      "float real");

				TestNumeric(conn, byte.MaxValue,     DataType.Byte);
				TestNumeric(conn, ushort.MaxValue,   DataType.UInt16);
				TestNumeric(conn, uint.MaxValue,     DataType.UInt32);

				if (context != ProviderName.SQLiteMS)
					TestNumeric(conn, ulong.MaxValue,    DataType.UInt64,     "bigint bit decimal int money numeric smallint tinyint float real");

				TestNumeric(conn, -3.40282306E+38f,  DataType.Single,     "bigint int smallint tinyint");
				TestNumeric(conn,  3.40282306E+38f,  DataType.Single,     "bigint int smallint tinyint");
				TestNumeric(conn, decimal.MinValue,  DataType.Decimal,    "bigint bit decimal int money numeric smallint tinyint float real");
				TestNumeric(conn, decimal.MaxValue,  DataType.Decimal,    "bigint bit decimal int money numeric smallint tinyint float real");
				TestNumeric(conn, decimal.MinValue,  DataType.VarNumeric, "bigint bit decimal int money numeric smallint tinyint float real");
				TestNumeric(conn, decimal.MaxValue,  DataType.VarNumeric, "bigint bit decimal int money numeric smallint tinyint float real");
				TestNumeric(conn, -922337203685477m, DataType.Money);
				TestNumeric(conn, +922337203685477m, DataType.Money);
				TestNumeric(conn, -214748m,          DataType.SmallMoney);
				TestNumeric(conn, +214748m,          DataType.SmallMoney);
			}
		}

		[ActiveIssue("https://system.data.sqlite.org/index.html/tktview/fb9e4b30874d83042e09c2f791d6065fc5e73a4b")]
		[Test]
		public void TestDoubleRoundTrip([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var conn = GetDataContext(context);
			var value = -1.7900000000000002E+308;

			// SELECT CAST(-1.7900000000000002E+308 as real)
			using var rd = conn.ExecuteReader($"SELECT CAST({value:G17} as real)");
			rd.Reader!.Read();
			var valueFromDB = rd.Reader.GetDouble(0);

			// -1.790000000000001E+308d != -1.7900000000000002E+308
			Assert.That(valueFromDB, Is.EqualTo(value));
		}

		[Test]
		public void TestNumericsDouble([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var conn = GetDataContext(context);
			TestNumeric(conn, -1.7900000000000002E+308d, DataType.Double, "bigint int smallint tinyint");
			TestNumeric(conn, -1.7900000000000008E+308d, DataType.Double, "bigint int smallint tinyint");
			TestNumeric(conn, 1.7900000000000002E+308d, DataType.Double, "bigint int smallint tinyint");
			TestNumeric(conn, 1.7900000000000008E+308d, DataType.Double, "bigint int smallint tinyint");
		}

		[Test]
		public void TestDateTime([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var conn = GetDataContext(context);
			var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<DateTime>("SELECT '2012-12-12 12:12:12'"), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT '2012-12-12 12:12:12'"), Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime>("SELECT @p", DataParameter.DateTime("p", dateTime)), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime)), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestChar([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var conn = GetDataContext(context);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<char>("SELECT Cast('1' as char)"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char)"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char>("SELECT Cast('1' as char(1))"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char(1))"), Is.EqualTo('1'));

				Assert.That(conn.Execute<char>("SELECT Cast('1' as varchar)"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar)"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char>("SELECT Cast('1' as varchar(20))"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar(20))"), Is.EqualTo('1'));

				Assert.That(conn.Execute<char>("SELECT Cast('1' as nchar)"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nchar)"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char>("SELECT Cast('1' as nchar(20))"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nchar(20))"), Is.EqualTo('1'));

				Assert.That(conn.Execute<char>("SELECT Cast('1' as nvarchar)"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nvarchar)"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char>("SELECT Cast('1' as nvarchar(20))"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as nvarchar(20))"), Is.EqualTo('1'));
			}

			if (context != ProviderName.SQLiteMS)
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(conn.Execute<char>("SELECT @p", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT @p", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast(@p as char)", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as char)", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast(@p as char(1))", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1))", DataParameter.Char("p", '1')), Is.EqualTo('1'));
				}
			}

			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<char>("SELECT @p", DataParameter.VarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p", DataParameter.VarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char>("SELECT @p", DataParameter.NChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p", DataParameter.NChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char>("SELECT @p", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char>("SELECT @p", DataParameter.Create("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p", DataParameter.Create("p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char>("SELECT @p", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
			}
		}

		[Test]
		public void TestString([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var conn = GetDataContext(context);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char)"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20))"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as char(20))"), Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar)"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(20))"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as varchar(20))"), Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as text)"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as text)"), Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nchar)"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nchar(20))"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as nchar(20))"), Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nvarchar)"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as nvarchar(20))"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as nvarchar(20))"), Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as ntext)"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as ntext)"), Is.Null);

				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Char("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.VarChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Text("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NText("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Create("p", "123")), Is.EqualTo("123"));

				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Create("p", (string?)null)), Is.Null);
				Assert.That(conn.Execute<string>("SELECT @p", new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestBinary([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var arr1 = new byte[] { 1 };
			var arr2 = new byte[] { 2 };

			using var conn = GetDataContext(context);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<byte[]>("SELECT    binaryDataType FROM AllTypes WHERE ID = 2"), Is.EqualTo(arr1));
				Assert.That(conn.Execute<Binary>("SELECT varbinaryDataType FROM AllTypes WHERE ID = 2"), Is.EqualTo(new Binary(arr2)));
				Assert.That(conn.Execute<byte[]>("SELECT    binaryDataType FROM AllTypes WHERE ID = 2"), Is.EqualTo(arr1));
				Assert.That(conn.Execute<Binary>("SELECT varbinaryDataType FROM AllTypes WHERE ID = 2"), Is.EqualTo(new Binary(arr2)));

				Assert.That(conn.Execute<byte[]>("SELECT Cast(NULL as image)"), Is.Null);

				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Binary("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", null)), Is.Null);
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Binary("p", Array.Empty<byte>())), Is.EqualTo(Array.Empty<byte>()));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", Array.Empty<byte>())), Is.EqualTo(Array.Empty<byte>()));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Image("p", Array.Empty<byte>())), Is.EqualTo(Array.Empty<byte>()));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create("p", new Binary(arr1))), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter("p", new Binary(arr1))), Is.EqualTo(arr1));
			}
		}

		[Test]
		public void TestGuid([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var conn = GetDataContext(context);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(
								conn.Execute<Guid>("SELECT uniqueidentifierDataType FROM AllTypes WHERE ID = 2"),
								Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				Assert.That(
					conn.Execute<Guid?>("SELECT '6F9619FF-8B86-D011-B42D-00C04FC964FF'"),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));
			}

			var guid = TestData.Guid1;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<Guid>("SELECT @p", DataParameter.Create("p", guid)), Is.EqualTo(guid));
				Assert.That(conn.Execute<Guid>("SELECT @p", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test]
		public void TestObject([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var conn = GetDataContext(context);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<object>("SELECT Cast(1 as Object)"), Is.EqualTo(1));
				Assert.That(conn.Execute<int>("SELECT Cast(1 as Object)"), Is.EqualTo(1));
				Assert.That(conn.Execute<int?>("SELECT Cast(1 as Object)"), Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT Cast(1 as Object)"), Is.EqualTo("1"));

				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Variant("p", 1)), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestXml([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var conn = GetDataContext(context);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<XDocument>("SELECT '<xml/>'").ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT '<xml/>'").InnerXml, Is.EqualTo("<xml />"));
			}

			var xdoc = XDocument.Parse("<xml/>");
			var xml  = Convert<string,XmlDocument>.Lambda("<xml/>");
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<string>("SELECT  @p", DataParameter.Xml("p", "<xml/>")), Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>("SELECT  @p", DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT  @p", DataParameter.Xml("p", xml)).InnerXml, Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>("SELECT  @p", new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>("SELECT  @p", new DataParameter("p", xml)).ToString(), Is.EqualTo("<xml />"));
			}
		}

		/// <summary>
		/// Ensure we can pass data as Json parameter type and get
		/// same value back out equivalent in value
		/// </summary>
		[Test]
		public void TestJson([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var conn = GetDataContext(context);
			var testJson = /*lang=json,strict*/ "{\"name\":\"bob\", \"age\":10}";

			Assert.That(conn.Execute<string>("SELECT @p", new DataParameter("p", testJson, DataType.Json)), Is.EqualTo(testJson));
		}

		enum TestEnum
		{
			[MapValue("A")] AA,
			[MapValue("B")] BB,
		}

		[Test]
		public void TestEnum1([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var conn = GetDataContext(context);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<TestEnum>("SELECT 'A'"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'A'"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum>("SELECT 'B'"), Is.EqualTo(TestEnum.BB));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'B'"), Is.EqualTo(TestEnum.BB));
			}
		}

		[Test]
		public void TestEnum2([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var conn = GetDataContext(context);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<string>("SELECT @p", new { p = TestEnum.AA }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = (TestEnum?)TestEnum.BB }), Is.EqualTo("B"));

				Assert.That(conn.Execute<string>("SELECT @p", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = conn.MappingSchema.GetConverter<TestEnum?, string>()!(TestEnum.AA) }), Is.EqualTo("A"));
			}
		}

		[Table(Name = "CreateTableTest", Schema = "IgnoreSchema")]
		public class CreateTableTest
		{
			[PrimaryKey, Identity]
			public int Id;
		}

		[Test]
		public void CreateDatabase([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			try
			{
				SQLiteTools.DropDatabase("TestDatabase");
			}
			catch
			{
			}

			SQLiteTools.CreateDatabase("TestDatabase");
			Assert.That(File.Exists ("TestDatabase.sqlite"), Is.True);

			var provider = context.IsAnyOf(TestProvName.AllSQLiteClassic) ? SQLiteProvider.System : SQLiteProvider.Microsoft;
			using (var db = new DataConnection(new DataOptions().UseConnectionString(SQLiteTools.GetDataProvider(provider), "Data Source=TestDatabase.sqlite")))
			{
				db.CreateTable<CreateTableTest>();
				db.DropTable  <CreateTableTest>();
			}

			SQLiteTools.ClearAllPools(provider);
			SQLiteTools.DropDatabase ("TestDatabase");
			Assert.That(File.Exists  ("TestDatabase.sqlite"), Is.False);
		}

		[Test]
		public void BulkCopyLinqTypes([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using var db = GetDataContext(context);
				try
				{
					db.BulkCopy(
						new BulkCopyOptions { BulkCopyType = bulkCopyType },
						Enumerable.Range(0, 10).Select(n =>
							new LinqDataTypes
							{
								ID = 4000 + n,
								MoneyValue = 1000m + n,
								DateTimeValue = new DateTime(2001, 1, 11, 1, 11, 21, 100),
								BoolValue = true,
								GuidValue = TestData.SequentialGuid(n),
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

		[Test]
		public async Task BulkCopyLinqTypesAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using var db = GetDataContext(context);
				try
				{
					await db.BulkCopyAsync(
						new BulkCopyOptions { BulkCopyType = bulkCopyType },
						Enumerable.Range(0, 10).Select(n =>
							new LinqDataTypes
							{
								ID = 4000 + n,
								MoneyValue = 1000m + n,
								DateTimeValue = new DateTime(2001, 1, 11, 1, 11, 21, 100),
								BoolValue = true,
								GuidValue = TestData.SequentialGuid(n),
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
		public void Issue784Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			var sp = db.DataProvider.GetSchemaProvider();
			var s  = sp.GetSchema(db);

			var table = s.Tables.FirstOrDefault(_ => _.TableName!.Equals("ForeignKeyTable", StringComparison.OrdinalIgnoreCase))!;
			Assert.That(table, Is.Not.Null);

			Assert.That(table.ForeignKeys, Has.Count.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(table.ForeignKeys[0].OtherTable.TableName, Is.EqualTo("PrimaryKeyTable"));
				Assert.That(table.ForeignKeys[0].OtherColumns[0].ColumnName, Is.EqualTo("ID"));
				Assert.That(table.ForeignKeys[0].ThisColumns[0].ColumnName, Is.EqualTo("PrimaryKeyTableID"));
			}
		}

		// there is no date type in sqlite and one of three other types could be used as storage:
		// INTEGER: unixtime (not sure if it supports full 64 bits) without fractional seconds
		// DOUBLE : "the number of days since noon in Greenwich on November 24, 4714 B.C. according to the proleptic Gregorian calendar." whatever it means O_O
		// TEXT   : ISO8601 string ("YYYY-MM-DD HH:MM:SS.SSS"). Not sure why SQLite documentation specify this specific format, maybe they don't support other qualifiers from ISO8601
		public class DateTimeTable
		{
			public DateTime DateTime { get; set; }
		}

		private MappingSchema ConfigureMapping(string columnType)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<DateTimeTable>()
					.Property(_ => _.DateTime)
						.HasDbType(columnType)
				.Build();

			return ms;
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2107")]
		public void DateTimeRoundtrip_Insert(
			[IncludeDataSources(TestProvName.AllSQLite)] string       context,
			[Values]                                     bool         inline,
			[Values]                                     DateTimeKind kind,
			[Values("TEXT", "REAL", "INTEGER")]          string       columnType)
		{
			// TODO: retest in V3 with newer provider version
			// in v108 it:
			// - cannot read data from int/double values into DateTime
			// - custom converter could be registered, but it still will not work because provider will return
			// year number instead of full date value (not sure if it is read bug or it is written into db incorrectly)
			if (context.Contains("Classic") && columnType != "TEXT")
				Assert.Inconclusive("System.Data.SQLite doesn't supports only ISO8601 dates as of v1.0.108");

			using var db    = new DataConnection(new DataOptions().UseConfiguration(context, ConfigureMapping(columnType)));
			using var table = db.CreateLocalTable<DateTimeTable>();

			db.InlineParameters = inline;
			// use 2040 to test unixtime don't overflow
			var dt              = new DateTime(2040, 2, 29, 11, 12, 13, 456, kind);

			table
				.Insert(() => new DateTimeTable()
				{
					DateTime = dt
				});

			var sql = db.LastQuery!;

			var result = table.Single();

			var resultDt = result.DateTime;
			if (kind == DateTimeKind.Utc)

				Assert.That(sql.Contains("@"), Is.EqualTo(!inline));

			if (kind == DateTimeKind.Utc)
			{
				// utc values returned as local
				Assert.That(result.DateTime, Is.EqualTo(dt.ToLocalTime()));
				// local/unspecified values returned as unspecified (makes sense)
				Assert.That(result.DateTime.Kind, Is.EqualTo(DateTimeKind.Local));
			}
			else
			{
				Assert.That(result.DateTime, Is.EqualTo(dt));
				// local/unspecified values returned as unspecified (makes sense)
				Assert.That(result.DateTime.Kind, Is.EqualTo(DateTimeKind.Unspecified));
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2107")]
		public void DateTimeRoundtrip_BulkCopy(
			[IncludeDataSources(TestProvName.AllSQLite)] string       context,
			[Values]                                     bool         inline,
			[Values]                                     DateTimeKind kind,
			[Values]                                     BulkCopyType copyType,
			[Values("TEXT", "REAL", "INTEGER")]          string       columnType)
		{
			if (context.Contains("Classic") && columnType != "TEXT")
				Assert.Inconclusive("System.Data.SQLite doesn't supports only ISO8601 dates as of v1.0.108");

			using var db    = new DataConnection(new DataOptions().UseConfiguration(context, ConfigureMapping(columnType)));
			using var table = db.CreateLocalTable<DateTimeTable>();

			db.InlineParameters = inline;
			var dt              = new DateTime(2040, 2, 29, 11, 12, 13, 456, kind);

			db.BulkCopy(
					new BulkCopyOptions { BulkCopyType = copyType },
					new[]
					{
						new DateTimeTable()
						{
							DateTime = dt
						}
					});

			var result = table.Single();

			// don't assert sql, as InlineParameters ignored for some copy types

			if (kind == DateTimeKind.Utc)
			{
				// utc values returned as local
				Assert.That(result.DateTime, Is.EqualTo(dt.ToLocalTime()));
				// local/unspecified values returned as unspecified (makes sense)
				Assert.That(result.DateTime.Kind, Is.EqualTo(DateTimeKind.Local));
			}
			else
			{
				Assert.That(result.DateTime, Is.EqualTo(dt));
				// local/unspecified values returned as unspecified (makes sense)
				Assert.That(result.DateTime.Kind, Is.EqualTo(DateTimeKind.Unspecified));
			}
		}

		// test to make sure our tests work with expected version of sqlite
		// should be updated when we bump dependency
		// also test matrix document should be updated too in that case (Build/Azure/README.md)
		[Explicit("disabled till MDS migration to new runtimes nuget")]
		[Test]
		public void TestDbVersion([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var expectedVersion = context switch
			{
				ProviderName.SQLiteClassic or TestProvName.SQLiteClassicMiniProfilerMapped or TestProvName.SQLiteClassicMiniProfilerUnmapped => "3.50.4",
				ProviderName.SQLiteMS => "3.46.1",

				_ => throw new InvalidOperationException(),
			};

			using var db = GetDataContext(context);
			using var rd = db.ExecuteReader("select sqlite_version()");
		
			rd.Reader!.Read();
			var version = rd.Reader.GetString(0);

			Assert.That(version, Is.EqualTo(expectedVersion));
		}

		[Explicit("disabled till MDS migration to new runtimes nuget")]
		[Test]
		public void TestDbVersion2([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			// second copy of test to ensure that with TestsInitialization runtime resolve code loads right module
			TestDbVersion(context);
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3899")]
		public void Issue3899Test([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3899Table>();

			db.Insert(new Issue3899Table()
			{
				Value1 = ulong.MinValue,
				Value2 = ulong.MaxValue,
			});

			var res = tb.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.Value1, Is.EqualTo(ulong.MinValue));
				Assert.That(res.Value2, Is.EqualTo(ulong.MaxValue));
			}
		}

		sealed class Issue3899Table
		{
			public ulong  Value1 { get; set; }
			public ulong? Value2 { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3766")]
		public void Issue3766Test1([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3766Table>();
			db.InlineParameters = inline;

			var record = new Issue3766Table() { Id = TestData.DateTimeOffsetUtc };
			db.InsertOrReplace(record);
			db.InsertOrReplace(record);
		}

		[ActiveIssue(Configuration = TestProvName.AllSQLiteClassic)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3766")]
		public void Issue3766Test2([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3766Table>();
			db.InlineParameters = inline;

			var record = new Issue3766Table() { Id = TestData.DateTimeOffsetUtc };
			db.Insert(record);
			var cnt = db.Update(record);

			Assert.That(cnt, Is.EqualTo(1));
		}

		[Table]
		sealed class Issue3766Table
		{
			[PrimaryKey] public DateTimeOffset Id { get; set; }
			[Column] public int Value { get; set; }
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2432")]
		public void Issue2432Test1([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool inline)
		{
			var ms = new MappingSchema();

			ms.SetDataType(typeof(DateTime), DataType.Int64);
			ms.SetConvertExpression(
				(long ticks) => new DateTime(ticks, DateTimeKind.Unspecified)
			);
			ms.SetConvertExpression(
				(DateTime dt) => dt.Ticks
			);
			ms.SetValueToSqlConverter(
				typeof(DateTime),
				(sb, dType, v) => sb.Append(((DateTime)v).Ticks.ToString(CultureInfo.InvariantCulture))
			);

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<Issue2432Table>();
			db.InlineParameters = inline;

			var record = new Issue2432Table() { DateTime = TestData.DateTime };

			db.Insert(record);
			var cnt = db.Update(record);
			var result = tb.Where(r => r.DateTime == TestData.DateTime).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(cnt, Is.EqualTo(1));
				Assert.That(result.DateTime, Is.EqualTo(TestData.DateTime));
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2432")]
		public void Issue2432Test2([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool inline)
		{
			var ms = new MappingSchema();

			ms.SetDataType(typeof(DateTime), DataType.Int64);
			ms.SetConvertExpression(
				(long ticks) => new DateTime(ticks, DateTimeKind.Unspecified)
			);
			ms.SetConvertExpression(
				(DateTime dt) => dt.Ticks
			);
			ms.SetValueToSqlConverter(
				typeof(DateTime),
				(sb, dType, v) => sb.Append(((DateTime)v).Ticks.ToString(CultureInfo.InvariantCulture))
			);

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<Issue2432Table>();
			db.InlineParameters = inline;

			var record = new Issue2432Table() { DateTime = TestData.DateTime };

			db.Insert(record);

			var result = tb.Where(r => r.DateTime == TestData.DateTime).Single();
			Assert.That(result.DateTime, Is.EqualTo(TestData.DateTime));
		}

		[Table]
		sealed class Issue2432Table
		{
			[PrimaryKey] public DateTime DateTime { get; set; }
			[Column] public int Field { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2099")]
		public void Issue2099Test([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue2099Table>();

			db.Insert(new Issue2099Table());
			var result = tb.Single();

			Assert.That(result.Id, Is.EqualTo(1));
		}

		sealed class Issue2099Table
		{
			[PrimaryKey, Identity] public long Id { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/934")]
		public void Issue934Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);

			try
			{
			db.Execute(@"
CREATE TABLE withid_1(x INTEGER PRIMARY KEY ASC, y, z);
CREATE TABLE withid_2(x INTEGER, y, z, PRIMARY KEY(x ASC));
CREATE TABLE withid_3(x INTEGER, y, z, PRIMARY KEY(x DESC));
CREATE TABLE withoutid_1(x INTEGER PRIMARY KEY DESC, y, z);
CREATE TABLE withoutid_2(x INTEGER PRIMARY KEY ASC, y, z) without rowid;
CREATE TABLE withoutid_3(x INTEGER, y, z, PRIMARY KEY(x ASC)) without rowid;
CREATE TABLE withoutid_4(x INTEGER, y, z, PRIMARY KEY(x DESC)) without rowid;
");

				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);

				AssertIdentity(schema.Tables.FirstOrDefault(t => t.TableName == "withid_1"), true);
				AssertIdentity(schema.Tables.FirstOrDefault(t => t.TableName == "withid_2"), true);
				AssertIdentity(schema.Tables.FirstOrDefault(t => t.TableName == "withid_3"), true);
				AssertIdentity(schema.Tables.FirstOrDefault(t => t.TableName == "withoutid_1"), false);
				AssertIdentity(schema.Tables.FirstOrDefault(t => t.TableName == "withoutid_2"), false);
				AssertIdentity(schema.Tables.FirstOrDefault(t => t.TableName == "withoutid_3"), false);
				AssertIdentity(schema.Tables.FirstOrDefault(t => t.TableName == "withoutid_4"), false);

				static void AssertIdentity(TableSchema? table, bool hasIdentity)
				{
					Assert.That(table, Is.Not.Null);

					var pk = table.Columns.Where(c => c.IsPrimaryKey).ToArray();

					Assert.That(pk, Has.Length.EqualTo(1));
					using (Assert.EnterMultipleScope())
					{
						Assert.That(pk[0].ColumnName, Is.EqualTo("x"));
						Assert.That(pk[0].IsIdentity, Is.EqualTo(hasIdentity));
					}
				}
			}
			finally
			{
				db.Execute(@"
DROP TABLE withid_1;
DROP TABLE withid_2;
DROP TABLE withid_3;
DROP TABLE withoutid_1;
DROP TABLE withoutid_2;
DROP TABLE withoutid_3;
DROP TABLE withoutid_4;
");
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4736")]
		public void Issue4736Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);

			try
			{
				db.Execute(@"
CREATE TABLE FirstTable (PkField1 INT, PkField2 INT, AdditionalField INTEGER, PRIMARY KEY (PkField1, PkField2));
CREATE TABLE SecondTable (FkField1 INT, FkField2 INT, FOREIGN KEY (FkField1, FkField2) REFERENCES FirstTable (PkField1, PkField2));
");

				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);

				var firstTable = schema.Tables.FirstOrDefault(t => t.TableName == "FirstTable");
				var secondTable = schema.Tables.FirstOrDefault(t => t.TableName == "SecondTable");

				Assert.That(firstTable, Is.Not.Null);

				using (Assert.EnterMultipleScope())
				{
					Assert.That(firstTable.Columns.Count(c => c.IsPrimaryKey), Is.EqualTo(2));

					Assert.That(secondTable, Is.Not.Null);
				}

				Assert.That(secondTable.ForeignKeys, Has.Count.EqualTo(1));

				using (Assert.EnterMultipleScope())
				{
					Assert.That(secondTable.ForeignKeys[0].ThisColumns, Has.Count.EqualTo(2));
					Assert.That(secondTable.ForeignKeys[0].OtherColumns, Has.Count.EqualTo(2));
				}
			}
			finally
			{
				db.Execute(@"
DROP TABLE FirstTable;
DROP TABLE SecondTable;
");
			}
		}

		#region issue 4808
		[Table(nameof(Issue4808Table))]
		sealed class Issue4808TableRaw
		{
			[Column(DataType = DataType.Text)  ] public Guid? ValueText { get; set; }
			[Column(DataType = DataType.Binary)] public Guid? ValueBinary { get; set; }

			public static readonly Issue4808TableRaw[] Data =
			[
				new Issue4808TableRaw()
				{
					ValueText = TestData.Guid1,
					ValueBinary = TestData.Guid2,
				}
			];
		}

		[Table]
		sealed class Issue4808Table
		{
			[Column(DbType = "TEXT")] public Guid? ValueText { get; set; }
			[Column(DbType = "BINARY")] public Guid? ValueBinary { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4808")]
		public void Issue4808Test([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4808TableRaw.Data);
			db.InlineParameters = inline;

			var listText = new Guid[] { TestData.Guid1 };
			var listBinary = new Guid[] {TestData.Guid2 };

			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.GetTable<Issue4808TableRaw>().Where(r => listText.Contains(r.ValueText.GetValueOrDefault())).Count(), Is.EqualTo(1));
				Assert.That(db.GetTable<Issue4808TableRaw>().Where(r => listBinary.Contains(r.ValueBinary.GetValueOrDefault())).Count(), Is.EqualTo(1));
				Assert.That(db.GetTable<Issue4808TableRaw>().Where(r => r.ValueText.HasValue && listText.Contains(r.ValueText.Value)).Count(), Is.EqualTo(1));
				Assert.That(db.GetTable<Issue4808TableRaw>().Where(r => r.ValueBinary.HasValue && listBinary.Contains(r.ValueBinary.Value)).Count(), Is.EqualTo(1));

				Assert.That(db.GetTable<Issue4808Table>().Where(r => listText.Contains(r.ValueText.GetValueOrDefault())).Count(), Is.EqualTo(1));
				Assert.That(db.GetTable<Issue4808Table>().Where(r => listBinary.Contains(r.ValueBinary.GetValueOrDefault())).Count(), Is.EqualTo(1));
				Assert.That(db.GetTable<Issue4808Table>().Where(r => r.ValueText.HasValue && listText.Contains(r.ValueText.Value)).Count(), Is.EqualTo(1));
				Assert.That(db.GetTable<Issue4808Table>().Where(r => r.ValueBinary.HasValue && listBinary.Contains(r.ValueBinary.Value)).Count(), Is.EqualTo(1));
			}
		}
		#endregion

		[Sql.TableFunction("pragma_table_info")]
		static ITable<PragmaTableInfoTable> PragmaTableInfo(string tableName) => throw new InvalidOperationException();

		sealed class PragmaTableInfoTable
		{
			public string Name { get; set; } = null!;
		}

		[Table("sqlite_master")]
		sealed class SQLiteMaster
		{
			[Column] public string Name { get; set; } = null!;
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/discussions/4985")]
		public void CrossApplyJoin([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query = from sqliteMaster in db.GetTable<SQLiteMaster>()
						from pragmaTableInfo in PragmaTableInfo(sqliteMaster.Name)
						select new
						{
							TableName = sqliteMaster.Name,
							PrimaryKeyColumn = pragmaTableInfo.Name
						};

			_ = query.ToArray();
		}

		sealed record Issue4904Table
		{
			public DateTime RecordDate { get; init; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4904")]
		public void Issue4904Test([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue4904Table>();

			db.Insert(new Issue4904Table { RecordDate = TestData.DateTime.AddDays(-1) });
			db.Insert(new Issue4904Table { RecordDate = TestData.DateTime });
			db.Insert(new Issue4904Table { RecordDate = TestData.DateTime.AddDays(1) });

			db.InlineParameters = true;

			var records1 = tb.Where(r => r.RecordDate <= DateTime.MaxValue).ToList();
			var records2 = tb.Where(r => r.RecordDate <= DateTime.MaxValue.AddMilliseconds(-1)).ToList();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(records1, Has.Count.EqualTo(3));
				Assert.That(records2, Has.Count.EqualTo(3));
			}
		}

		// TODO: Enable AllSQLite when MSSQLite provider is supported. Related: #4117
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5014")]
		public void Issue5014Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);

			db.Execute("DROP TABLE IF EXISTS Issue5014TestTable");
			db.Execute("""
				CREATE TABLE "Issue5014TestTable" (
					"Id" INTEGER NOT NULL PRIMARY KEY,
					"Name" TEXT NOT NULL
				) without rowid;
				""");

			try
			{
				var provider = new SQLiteSchemaProvider();
				var schema   = provider.GetSchema(db);

				var table = schema.Tables.First(x => x.TableName == "Issue5014TestTable");

				Assert.That(table.Columns.Count(x => x.IsPrimaryKey), Is.EqualTo(1));
			}
			finally
			{
				db.Execute("DROP TABLE IF EXISTS Issue5014TestTable");
			}
		}
	}
}
