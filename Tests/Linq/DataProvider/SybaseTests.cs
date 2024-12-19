using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

namespace Tests.DataProvider
{
	using Model;

	[TestFixture]
	public class SybaseTests : TestBase
	{
		[Test]
		public void TestParameters([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT @p", new { p = 1 }), Is.EqualTo("1"));
					Assert.That(conn.Execute<string>("SELECT @p", new { p = "1" }), Is.EqualTo("1"));
					Assert.That(conn.Execute<int>("SELECT @p", new { p = new DataParameter { Value = 1 } }), Is.EqualTo(1));
					Assert.That(conn.Execute<string>("SELECT @p1", new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
					Assert.That(conn.Execute<int>("SELECT @p1 + @p2", new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
					Assert.That(conn.Execute<int>("SELECT @p2 + @p1", new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
				});
			}
		}

		static void TestType<T>(DataConnection connection, string dataTypeName, T value, string tableName = "AllTypes", bool convertToString = false)
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
		public void TestDataTypes([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				TestType(conn, "bigintDataType",        1000000L);
				TestType(conn, "uBigintDataType",       2233332UL);
				TestType(conn, "numericDataType",       9999999m);
				TestType(conn, "bitDataType",           true);
				TestType(conn, "smallintDataType",      (short)25555);
				TestType(conn, "uSmallintDataType",     (ushort)33333);
				TestType(conn, "decimalDataType",       2222222m);
				TestType(conn, "smallmoneyDataType",    100000m);
				TestType(conn, "intDataType",           7777777);
				TestType(conn, "uIntDataType",          3333333U);
				TestType(conn, "tinyintDataType",       (sbyte)100);
				TestType(conn, "moneyDataType",         100000m);
				TestType(conn, "floatDataType",         20.31d);
				TestType(conn, "realDataType",          16.2f);

				TestType(conn, "datetimeDataType",      new DateTime(2012, 12, 12, 12, 12, 12));
				TestType(conn, "smalldatetimeDataType", new DateTime(2012, 12, 12, 12, 12, 00));
				TestType(conn, "dateDataType",          new DateTime(2012, 12, 12));
				TestType(conn, "timeDataType",          new TimeSpan(0, 12, 12, 12, 10));

				TestType(conn, "charDataType",          '1');
				TestType(conn, "varcharDataType",       "234");
				TestType(conn, "textDataType",          "567");
				TestType(conn, "ncharDataType",         "23233");
				TestType(conn, "nvarcharDataType",      "3323");
				TestType(conn, "ntextDataType",         "111");

				TestType(conn, "binaryDataType",        new byte[] { 1 });
				TestType(conn, "varbinaryDataType",     new byte[] { 2 });
				TestType(conn, "imageDataType",         new byte[] { 3, 0, 0, 0 });

				Assert.That(conn.Execute<byte[]>("SELECT timestampDataType FROM AllTypes WHERE ID = 1"), Has.Length.EqualTo(8));
			}
		}

		static void TestNumeric<T>(DataConnection conn, T expectedValue, DataType dataType, string skip = "")
		{
			var skipTypes = skip.Split(' ').Select(s => s.Replace("u.", "unsigned "));

			foreach (var sqlType in new[]
				{
					"bigint",
					"unsigned bigint",
					"bit",
					"decimal",
					"decimal(38)",
					"int",
					"unsigned int",
					"money",
					"numeric",
					"numeric(38)",
					"smallint",
					"unsigned smallint",
					"smallmoney",
					"tinyint",

					"float",
					"real"
				}.Except(skipTypes))
			{
				var sqlValue = expectedValue is bool ? (bool)(object)expectedValue? 1 : 0 : (object?)expectedValue;

				var sql = string.Format(CultureInfo.InvariantCulture, "SELECT Cast({0} as {1})", sqlValue ?? "NULL", sqlType);

				Assert.That(conn.Execute<T>(sql), Is.EqualTo(expectedValue));
			}

			{
				var sql = typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?) ?
					"SELECT Cast(@p as decimal(38))" :
					"SELECT @p";

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<T>(sql, new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
					Assert.That(conn.Execute<T>(sql, new DataParameter { Name = "p", Value = expectedValue }), Is.EqualTo(expectedValue));
					Assert.That(conn.Execute<T>(sql, new { p = expectedValue }), Is.EqualTo(expectedValue));
				});
			}
		}

		static void TestSimple<T>(DataConnection conn, T expectedValue, DataType dataType)
			where T : struct
		{
			TestNumeric<T> (conn, expectedValue, dataType);
			TestNumeric<T?>(conn, expectedValue, dataType);
			TestNumeric<T?>(conn, (T?)null,      dataType, "bit");
		}

		[Test]
		public void TestNumerics([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				TestNumeric<bool>  (conn, true, DataType.Boolean);
				TestNumeric<bool?> (conn, true, DataType.Boolean);
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

				TestNumeric(conn, sbyte.MinValue,   DataType.SByte,      "bit u.bigint u.int u.smallint tinyint");
				TestNumeric(conn, sbyte.MaxValue,   DataType.SByte,      "bit");
				TestNumeric(conn, short.MinValue,   DataType.Int16,      "bit u.bigint u.int u.smallint tinyint");
				TestNumeric(conn, short.MaxValue,   DataType.Int16,      "bit tinyint");
				TestNumeric(conn, int.MinValue,     DataType.Int32,      "bit u.bigint u.int u.smallint smallint smallmoney tinyint");
				TestNumeric(conn, int.MaxValue,     DataType.Int32,      "bit u.smallint smallint smallmoney tinyint real");
				TestNumeric(conn, long.MinValue,    DataType.Int64,      "bit u.bigint u.int u.smallint decimal int money numeric smallint smallmoney tinyint");
				TestNumeric(conn, long.MaxValue,    DataType.Int64,      "bit u.int u.smallint decimal int money numeric smallint smallmoney tinyint float real");

				TestNumeric(conn, byte.MaxValue,    DataType.Byte,       "bit");
				TestNumeric(conn, ushort.MaxValue,  DataType.UInt16,     "bit smallint tinyint");
				TestNumeric(conn, uint.MaxValue,    DataType.UInt32,     "bit int smallint smallmoney tinyint real u.smallint");
				TestNumeric(conn, ulong.MaxValue,   DataType.UInt64,     "bigint bit decimal int money numeric smallint smallmoney tinyint float real u.int u.smallint");

				TestNumeric(conn, -3.40282306E+38f, DataType.Single,     "bigint bit u.bigint u.int u.smallint decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint");
				TestNumeric(conn, 3.40282306E+38f,  DataType.Single,     "bigint bit u.bigint u.int u.smallint decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint");
				TestNumeric(conn, -1.79E+308d,      DataType.Double,     "bigint bit u.bigint u.int u.smallint decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint real");
				TestNumeric(conn,  1.79E+308d,      DataType.Double,     "bigint bit u.bigint u.int u.smallint decimal decimal(38) int money numeric numeric(38) smallint smallmoney tinyint real");

				const decimal decmax = 79228162514264337593543950m;

				TestNumeric(conn, -decmax,          DataType.Decimal,    "bigint bit u.bigint u.int u.smallint decimal int money numeric smallint smallmoney tinyint float real");
				TestNumeric(conn, +decmax,          DataType.Decimal,    "bigint bit u.bigint u.int u.smallint decimal int money numeric smallint smallmoney tinyint float real");
				TestNumeric(conn, -decmax,          DataType.VarNumeric, "bigint bit u.bigint u.int u.smallint decimal int money numeric smallint smallmoney tinyint float real");
				TestNumeric(conn, +decmax,          DataType.VarNumeric, "bigint bit u.bigint u.int u.smallint decimal int money numeric smallint smallmoney tinyint float real");
				TestNumeric(conn, -9223372036854m,  DataType.Money,      "bit u.bigint u.int u.smallint int smallint smallmoney tinyint real");
				TestNumeric(conn, +9223372036854m,  DataType.Money,      "bit u.int u.smallint int smallint smallmoney tinyint real");
				TestNumeric(conn, -214748m,         DataType.SmallMoney, "bit u.bigint u.int u.smallint smallint tinyint");
				TestNumeric(conn, +214748m,         DataType.SmallMoney, "bit u.smallint smallint tinyint");
			}
		}

		[Test]
		public void TestDate([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12);

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<DateTime>("SELECT Cast('2012-12-12' as date)"), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12' as date)"), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime>("SELECT @p", DataParameter.Date("p", dateTime)), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime, DataType.Date)), Is.EqualTo(dateTime));
				});
			}
		}

		[Test]
		public void TestSmallDateTime([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 00);

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<DateTime>("SELECT Cast('2012-12-12 12:12:00' as smalldatetime)"), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:00' as smalldatetime)"), Is.EqualTo(dateTime));

					Assert.That(conn.Execute<DateTime>("SELECT @p", DataParameter.SmallDateTime("p", dateTime)), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime, DataType.SmallDateTime)), Is.EqualTo(dateTime));
				});
			}
		}

		[Test]
		public void TestDateTime([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<DateTime>("SELECT Cast('2012-12-12 12:12:12' as datetime)"), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12' as datetime)"), Is.EqualTo(dateTime));

					Assert.That(conn.Execute<DateTime>("SELECT @p", DataParameter.DateTime("p", dateTime)), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime)), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
				});
			}
		}

		[Test]
		public void TestTimeSpan([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var time = new TimeSpan(12, 12, 12);

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<TimeSpan>("SELECT Cast('12:12:12' as time)"), Is.EqualTo(time));
					Assert.That(conn.Execute<TimeSpan?>("SELECT Cast('12:12:12' as time)"), Is.EqualTo(time));

					Assert.That(conn.Execute<TimeSpan>("SELECT @p", DataParameter.Time("p", time)), Is.EqualTo(time));
					Assert.That(conn.Execute<TimeSpan>("SELECT @p", DataParameter.Create("p", time)), Is.EqualTo(time));
					Assert.That(conn.Execute<TimeSpan?>("SELECT @p", new DataParameter("p", time, DataType.Time)), Is.EqualTo(time));
					Assert.That(conn.Execute<TimeSpan?>("SELECT @p", new DataParameter("p", time)), Is.EqualTo(time));
				});
			}
		}

		[Test]
		public void TestChar([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
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

					Assert.That(conn.Execute<char>("SELECT @p", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT @p", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast(@p as char)", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as char)", DataParameter.Char("p", '1')), Is.EqualTo('1'));

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
				});
			}
		}

		[Test]
		public void TestString([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT Cast('12345' as char)"), Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20))"), Is.EqualTo("12345"));
				});
				Assert.Multiple(() =>
				{
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

					Assert.That(conn.Execute<string>("SELECT Cast('12345' as unitext)"), Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast(NULL    as unitext)"), Is.Null);

					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Char("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.VarChar("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Text("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NChar("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NText("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Create("p", "123")), Is.EqualTo("123"));

					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Create("p", (string?)null)), Is.EqualTo(null));
					Assert.That(conn.Execute<string>("SELECT @p", new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
				});
			}
		}

		public static IEnumerable<StringTestCase> StringTestCases
		{
			get
			{
				yield return new StringTestCase("'\u2000\u2001\u2002\u2003\uabab\u03bctst тест", "u&'''\\2000\\2001\\2002\\2003\\abab\\03bctst тест'", "Test case 1");
				// this case fails for parameters, because driver terminates parameter value at \0 character
				//yield return Tuple.Create("\0test", "char(0) + 'test'");
			}
		}

		public class StringTestCase
		{
			private readonly string _caseName;

			public StringTestCase(string value, string literal, string caseName)
			{
				_caseName = caseName;
				Value     = value;
				Literal   = literal;
			}

			public string Value   { get; }
			public string Literal { get; }

			public override string ToString()
			{
				return _caseName;
			}
		}

		[Test]
		public void TestUnicodeString(
			[IncludeDataSources(TestProvName.AllSybase)] string context,
			[ValueSource(nameof(StringTestCases))] StringTestCase testCase)
		{
			using (var conn = GetDataConnection(context))
			{
				var value   = testCase.Value;
				var literal = testCase.Literal;

				Assert.Multiple(() =>
				{
					// test raw literals queries
					Assert.That(conn.Execute<string>($"SELECT Cast({literal} as char)"), Is.EqualTo(value));
					Assert.That(conn.Execute<string>($"SELECT Cast({literal} as varchar)"), Is.EqualTo(value));
					Assert.That(conn.Execute<string>($"SELECT Cast({literal} as text)"), Is.EqualTo(value));
					Assert.That(conn.Execute<string>($"SELECT Cast({literal} as nchar)"), Is.EqualTo(value));
					Assert.That(conn.Execute<string>($"SELECT Cast({literal} as nvarchar)"), Is.EqualTo(value));
					Assert.That(conn.Execute<string>($"SELECT Cast({literal} as unitext)"), Is.EqualTo(value));

					// test parameters
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Char("p", value)), Is.EqualTo(value));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.VarChar("p", value)), Is.EqualTo(value));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Text("p", value)), Is.EqualTo(value));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NChar("p", value)), Is.EqualTo(value));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NVarChar("p", value)), Is.EqualTo(value));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NText("p", value)), Is.EqualTo(value));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Create("p", value)), Is.EqualTo(value));

					// test default linq2db behavior for parameter and literal
					Assert.That(conn.Select(() => value), Is.EqualTo(value));
				});
				conn.InlineParameters = true;
				Assert.That(conn.Select(() => value), Is.EqualTo(value));
			}
		}

		[Test]
		public void TestBinary([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			var arr1 = new byte[] { 57, 48        };
			var arr2 = new byte[] { 57, 48, 0, 0  };

			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<byte[]>("SELECT Cast(12345 as binary(2))"), Is.EqualTo(arr1));
					Assert.That(conn.Execute<Binary>("SELECT Cast(12345 as binary(4))"), Is.EqualTo(new Binary(arr2)));

					Assert.That(conn.Execute<byte[]>("SELECT Cast(12345 as varbinary(2))"), Is.EqualTo(arr1));
					Assert.That(conn.Execute<Binary>("SELECT Cast(12345 as varbinary(4))"), Is.EqualTo(new Binary(arr2)));

					Assert.That(conn.Execute<byte[]>("SELECT Cast(NULL as image)"), Is.EqualTo(null));

					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Binary("p", arr1)), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", arr1)), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create("p", arr1)), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", null)), Is.EqualTo(null));
					Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as binary(1))", DataParameter.Binary("p", Array.Empty<byte>())), Is.EqualTo(new byte[] { 0 }));
					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Binary("p", Array.Empty<byte>())), Is.EqualTo(new byte[1]));
					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", Array.Empty<byte>())), Is.EqualTo(new byte[1]));
					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Image("p", Array.Empty<byte>())), Is.EqualTo(null));
					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Image("p", arr2)), Is.EqualTo(arr2));
					Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create("p", new Binary(arr1))), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter("p", new Binary(arr1))), Is.EqualTo(arr1));
				});
			}
		}

		[Test]
		public void TestGuid([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(
									conn.Execute<Guid>("SELECT '6F9619FF-8B86-D011-B42D-00C04FC964FF'"),
									Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

					Assert.That(
						conn.Execute<Guid?>("SELECT '6F9619FF-8B86-D011-B42D-00C04FC964FF'"),
						Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));
				});

				var guid = TestData.Guid1;

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<Guid>("SELECT @p", DataParameter.Create("p", guid)), Is.EqualTo(guid));
					Assert.That(conn.Execute<Guid>("SELECT @p", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
				});
			}
		}

		[Test]
		public void TestTimestamp([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var arr = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Timestamp("p", arr)), Is.EqualTo(arr));
					Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter("p", arr, DataType.Timestamp)), Is.EqualTo(arr));
				});
			}
		}

		[Test]
		public void TestXml([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT '<xml/>'"), Is.EqualTo("<xml/>"));
					Assert.That(conn.Execute<XDocument>("SELECT '<xml/>'").ToString(), Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XmlDocument>("SELECT '<xml/>'").InnerXml, Is.EqualTo("<xml />"));
				});

				var xdoc = XDocument.Parse("<xml/>");
				var xml  = Convert<string,XmlDocument>.Lambda("<xml/>");

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Xml("p", "<xml/>")), Is.EqualTo("<xml/>"));
					Assert.That(conn.Execute<XDocument>("SELECT @p", DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XmlDocument>("SELECT @p", DataParameter.Xml("p", xml)).InnerXml, Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XDocument>("SELECT @p", new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XDocument>("SELECT @p", new DataParameter("p", xml)).ToString(), Is.EqualTo("<xml />"));
				});
			}
		}

		enum TestEnum
		{
			[MapValue("A")] AA,
			[MapValue("B")] BB,
		}

		[Test]
		public void TestEnum1([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<TestEnum>("SELECT 'A'"), Is.EqualTo(TestEnum.AA));
					Assert.That(conn.Execute<TestEnum?>("SELECT 'A'"), Is.EqualTo(TestEnum.AA));
					Assert.That(conn.Execute<TestEnum>("SELECT 'B'"), Is.EqualTo(TestEnum.BB));
					Assert.That(conn.Execute<TestEnum?>("SELECT 'B'"), Is.EqualTo(TestEnum.BB));
				});
			}
		}

		[Test]
		public void TestEnum2([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT @p", new { p = TestEnum.AA }), Is.EqualTo("A"));
					Assert.That(conn.Execute<string>("SELECT @p", new { p = (TestEnum?)TestEnum.BB }), Is.EqualTo("B"));

					Assert.That(conn.Execute<string>("SELECT @p", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
					Assert.That(conn.Execute<string>("SELECT @p", new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
					Assert.That(conn.Execute<string>("SELECT @p", new { p = conn.MappingSchema.GetConverter<TestEnum?, string>()!(TestEnum.AA) }), Is.EqualTo("A"));
				});
			}
		}

		[Test]
		public void BulkCopyLinqTypes([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using (var db = GetDataConnection(context))
				{
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
		}

		[Test]
		public async Task BulkCopyLinqTypesAsync([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using (var db = GetDataConnection(context))
				{
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
		}

		[Table]
		sealed class Issue1707
		{
			public static IEqualityComparer<Issue1707> Comparer = ComparerBuilder.GetEqualityComparer<Issue1707>();
			[Column]
			public int Id { get; set; }

			[Column]
			public TimeSpan Time { get; set; }

			[Column(DataType = DataType.Time)]
			public DateTime Time2 { get; set; }

			[Column]
			public DateTime DateTime { get; set; }

			[Column]
			public TimeSpan? TimeN { get; set; }

			[Column(DataType = DataType.Time)]
			public DateTime? Time2N { get; set; }

			[Column]
			public DateTime? DateTimeN { get; set; }
		}

		[Test]
		public void Issue1707Test([IncludeDataSources(true, TestProvName.AllSybase)] string context, [Values] bool useParameters)
		{
			var testIntervals = new[]
			{
				TimeSpan.Zero,
				TimeSpan.FromMinutes(123),
				TimeSpan.FromMinutes(-123),
				TimeSpan.FromMinutes(1567),
				TimeSpan.FromMinutes(-1567)
			};

			var start    = new DateTime(1900, 1, 1);
			var testData = new List<Issue1707>();

			for (var i = 0; i < testIntervals.Length; i++)
			{
				var ts = testIntervals[i];

				testData.Add(new Issue1707()
				{
					Id        = i + 1,
					Time      = ts,
					TimeN     = ts,
					Time2     = start + ts,
					Time2N    = start + ts,
					DateTime  = start + ts,
					DateTimeN = start + ts
				});
			}

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<Issue1707>())
			{
				db.InlineParameters = !useParameters;

				foreach (var record in testData)
				{
					table.Insert(() => new Issue1707()
					{
						Id        = record.Id,
						Time      = record.Time,
						TimeN     = record.TimeN,
						Time2     = record.Time2,
						Time2N    = record.Time2N,
						DateTime  = record.DateTime,
						DateTimeN = record.DateTimeN,
					});
				}

				var results = table.OrderBy(_ => _.Id);

				AreEqual(testData.Select(fixRecord), results, Issue1707.Comparer);
			}

			Issue1707 fixRecord(Issue1707 record)
			{
				record.Time   = fixTime(record.Time);
				record.TimeN  = fixTime(record.TimeN!.Value);
				record.Time2  = new DateTime() + fixTime(record.Time2 - start);
				record.Time2N = new DateTime() + fixTime(record.Time2N!.Value - start);

				return record;
			}

			TimeSpan fixTime(TimeSpan time)
			{
				if (time < TimeSpan.Zero)
					time = TimeSpan.FromDays(1 - time.Days) + time;
				return time - TimeSpan.FromDays(time.Days);
			}
		}

		[Table]
		sealed class Issue3902
		{
			[PrimaryKey, NotNull] public char    TPPSLT_TYPE                 { get; set; }
			[PrimaryKey, NotNull] public string  TPPSLT_KIND_ID              { get; set; } = null!;
			[Column]              public decimal TPPSLT_QUOTE_DURATION_MULTI { get; set; }
			[Column]              public string  TPPSLT_USER_ID              { get; set; } = null!;
		}

		[Test]
		public void Issue3902Test([IncludeDataSources(true, TestProvName.AllSybase)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<Issue3902>();

			table
				.Where(c => c.TPPSLT_TYPE == '4' && c.TPPSLT_KIND_ID == "AAA")
				.Set(c => c.TPPSLT_QUOTE_DURATION_MULTI, 10)
				.Set(c => c.TPPSLT_USER_ID,              "IamHandsome")
				.Update();
		}

		#region BulkCopy

		[Table("AllTypes")]
		public partial class AllType
		{
			[PrimaryKey, Identity] public int ID { get; set; }

			[Column] public long?     bigintDataType         { get; set; }
			[Column] public ulong?    uBigintDataType        { get; set; }
			[Column] public decimal?  numericDataType        { get; set; }
			[Column] public bool      bitDataType            { get; set; }
			[Column] public short?    smallintDataType       { get; set; }
			[Column] public ushort?   uSmallintDataType      { get; set; }
			[Column] public decimal?  decimalDataType        { get; set; }
			[Column] public decimal?  moneyDataType          { get; set; }
			[Column] public decimal?  smallmoneyDataType     { get; set; }
			[Column] public int?      intDataType            { get; set; }
			[Column] public uint?     uIntDataType           { get; set; }
			[Column] public byte?     tinyintDataType        { get; set; }
			[Column] public double?   floatDataType          { get; set; }
			[Column] public float?    realDataType           { get; set; }
			[Column] public DateTime? datetimeDataType       { get; set; }
			[Column] public DateTime? smalldatetimeDataType  { get; set; }
			[Column] public DateTime? dateDataType           { get; set; }
			[Column] public TimeSpan? timeDataType           { get; set; }
			[Column] public char?     charDataType           { get; set; }
			[Column] public string?   char20DataType         { get; set; }
			[Column] public string?   varcharDataType        { get; set; }
			[Column] public string?   textDataType           { get; set; }
			[Column] public char?     ncharDataType          { get; set; }
			[Column] public string?   nvarcharDataType       { get; set; }
			[Column] public string?   ntextDataType          { get; set; }
			[Column] public byte[]?   binaryDataType         { get; set; }
			[Column] public byte[]?   varbinaryDataType      { get; set; }
			[Column] public byte[]?   imageDataType          { get; set; }
			[Column] public byte[]?   timestampDataType      { get; set; }
		}

		static readonly AllType[] _allTypeses =
		{
			#region data

			new AllType
			{
				ID                       = 700,
				bigintDataType           = 1,
				uBigintDataType          = 2,
				numericDataType          = 1.6m,
				bitDataType              = true,
				smallintDataType         = 1,
				uSmallintDataType        = 2,
				decimalDataType          = 1.1m,
				moneyDataType            = 1.2m,
				smallmoneyDataType       = 1.3m,
				intDataType              = 1,
				uIntDataType             = 2,
				tinyintDataType          = 1,
				floatDataType            = 1.4d,
				realDataType             = 1.5f,
				datetimeDataType         = new DateTime(2014, 12, 17, 21, 2, 58, 123),
				smalldatetimeDataType    = new DateTime(2014, 12, 17, 21, 3, 0),
				dateDataType             = new DateTime(2014, 12, 17),
				timeDataType             = new TimeSpan(0, 10, 11, 12),
				charDataType             = 'E',
				char20DataType           = "Eboi",
				varcharDataType          = "E",
				textDataType             = "E",
				ncharDataType            = 'Ё',
				nvarcharDataType         = "Ё",
				ntextDataType            = "Ё",
				binaryDataType           = new byte[] { 1 },
				varbinaryDataType        = new byte[] { 1 },
				imageDataType            = new byte[] { 1, 2, 3, 4, 5 },
				timestampDataType        = new byte[] { 5, 4, 3, 2, 1 },
			},
			new AllType
			{
				ID                       = 701,
			},

			#endregion
		};

		[Table("LinqDataTypes")]
		sealed class DataTypes
		{
			[Column] public int       ID;
			[Column] public decimal?  MoneyValue;
			[Column] public DateTime? DateTimeValue;
			[Column] public DateTime? DateTimeValue2;
			[Column] public bool      BoolValue;
			[Column] public Guid?     GuidValue;
			[Column] public Binary?   BinaryValue;
			[Column] public short?    SmallIntValue;
			[Column] public int?      IntValue;
			[Column] public long?     BigIntValue;
			[Column] public string?   StringValue;
		}

		[Test]
		public void BulkCopyLinqTypesMultipleRows([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				try
				{
					db.BulkCopy(
						new BulkCopyOptions
						{
							BulkCopyType = BulkCopyType.MultipleRows,
						},
						Enumerable.Range(0, 10).Select(n =>
							new DataTypes
							{
								ID             = 4000 + n,
								MoneyValue     = 1000m + n,
								DateTimeValue  = new DateTime(2001, 1, 11, 1, 11, 21, 100),
								DateTimeValue2 = new DateTime(2001, 1, 10, 1, 11, 21, 100),
								BoolValue      = true,
								GuidValue      = TestData.SequentialGuid(n),
								BinaryValue    = new byte[] { (byte)n },
								SmallIntValue  = (short)n,
								IntValue       = n,
								BigIntValue    = n,
								StringValue    = n.ToString(),
							}
						));
				}
				finally
				{
					db.GetTable<DataTypes>().Delete(p => p.ID >= 4000);
				}
			}
		}

		[Test]
		public async Task BulkCopyLinqTypesMultipleRowsAsync([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				try
				{
					await db.BulkCopyAsync(
						new BulkCopyOptions
						{
							BulkCopyType = BulkCopyType.MultipleRows,
						},
						Enumerable.Range(0, 10).Select(n =>
							new DataTypes
							{
								ID             = 4000 + n,
								MoneyValue     = 1000m + n,
								DateTimeValue  = new DateTime(2001, 1, 11, 1, 11, 21, 100),
								DateTimeValue2 = new DateTime(2001, 1, 10, 1, 11, 21, 100),
								BoolValue      = true,
								GuidValue      = TestData.SequentialGuid(n),
								BinaryValue    = new byte[] { (byte)n },
								SmallIntValue  = (short)n,
								IntValue       = n,
								BigIntValue    = n,
								StringValue    = n.ToString(),
							}
						));
				}
				finally
				{
					await db.GetTable<DataTypes>().DeleteAsync(p => p.ID >= 4000);
				}
			}
		}

		[Test]
		public void BulkCopyLinqTypesProviderSpecific([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				try
				{
					db.BulkCopy(
						new BulkCopyOptions
						{
							BulkCopyType = BulkCopyType.ProviderSpecific,
						},
						Enumerable.Range(0, 10).Select(n =>
							new DataTypes
							{
								ID             = 4000 + n,
								MoneyValue     = 1000m + n,
								DateTimeValue  = new DateTime(2001, 1, 11, 1, 11, 21, 100),
								DateTimeValue2 = new DateTime(2001, 1, 10, 1, 11, 21, 100),
								BoolValue      = true,
								GuidValue      = TestData.SequentialGuid(n),
								BinaryValue    = new byte[] { (byte)n },
								SmallIntValue  = (short)n,
								IntValue       = n,
								BigIntValue    = n,
								StringValue    = n.ToString(),
							}
						));
				}
				finally
				{
					db.GetTable<DataTypes>().Delete(p => p.ID >= 4000);
				}
			}
		}

		[Test]
		public async Task BulkCopyLinqTypesProviderSpecificAsync([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				try
				{
					await db.BulkCopyAsync(
						new BulkCopyOptions
						{
							BulkCopyType = BulkCopyType.ProviderSpecific,
						},
						Enumerable.Range(0, 10).Select(n =>
							new DataTypes
							{
								ID             = 4000 + n,
								MoneyValue     = 1000m + n,
								DateTimeValue  = new DateTime(2001, 1, 11, 1, 11, 21, 100),
								DateTimeValue2 = new DateTime(2001, 1, 10, 1, 11, 21, 100),
								BoolValue      = true,
								GuidValue      = TestData.SequentialGuid(n),
								BinaryValue    = new byte[] { (byte)n },
								SmallIntValue  = (short)n,
								IntValue       = n,
								BigIntValue    = n,
								StringValue    = n.ToString(),
							}
						));
				}
				finally
				{
					await db.GetTable<DataTypes>().DeleteAsync(p => p.ID >= 4000);
				}
			}
		}

		void BulkCopyAllTypes(string context, BulkCopyType bulkCopyType)
		{
			var hasBitBug = context == ProviderName.Sybase && bulkCopyType == BulkCopyType.ProviderSpecific;
			using (var db = GetDataConnection(context))
			{
				db.CommandTimeout = 60;

				db.GetTable<AllType>().Delete(p => p.ID >= _allTypeses[0].ID);

				try
				{
					db.BulkCopy(
						new BulkCopyOptions
						{
							BulkCopyType       = bulkCopyType,
							KeepIdentity       = true,
						},
						_allTypeses);

					var ids = _allTypeses.Select(at => at.ID).ToArray();

					var list = db.GetTable<AllType>().Where(t => ids.Contains(t.ID)).OrderBy(t => t.ID).ToList();

					Assert.That(list, Has.Count.EqualTo(_allTypeses.Length));

					for (var i = 0; i < list.Count; i++)
						CompareObject(db.MappingSchema, list[i], _allTypeses[i], hasBitBug);
				}
				finally
				{
					db.GetTable<AllType>().Delete(p => p.ID >= _allTypeses[0].ID);
				}
			}
		}

		async Task BulkCopyAllTypesAsync(string context, BulkCopyType bulkCopyType)
		{
			var hasBitBug = context == ProviderName.Sybase && bulkCopyType == BulkCopyType.ProviderSpecific;
			using (var db = GetDataConnection(context))
			{
				db.CommandTimeout = 60;

				db.GetTable<AllType>().Delete(p => p.ID >= _allTypeses[0].ID);

				try
				{
					await db.BulkCopyAsync(
						new BulkCopyOptions
						{
							BulkCopyType = bulkCopyType,
							KeepIdentity = true,
						},
						_allTypeses);

					var ids = _allTypeses.Select(at => at.ID).ToArray();

					var list = db.GetTable<AllType>().Where(t => ids.Contains(t.ID)).OrderBy(t => t.ID).ToList();

					Assert.That(list, Has.Count.EqualTo(_allTypeses.Length));

					for (var i = 0; i < list.Count; i++)
						CompareObject(db.MappingSchema, list[i], _allTypeses[i], hasBitBug);
				}
				finally
				{
					await db.GetTable<AllType>().DeleteAsync(p => p.ID >= _allTypeses[0].ID);
				}
			}
		}

		void CompareObject<T>(MappingSchema mappingSchema, T actual, T test, bool hasBitBug)
			where T: notnull
		{
			var ed = mappingSchema.GetEntityDescriptor(typeof(T));

			foreach (var column in ed.Columns)
			{
				var actualValue = column.GetProviderValue(actual);
				var testValue   = column.GetProviderValue(test);

				// timestampDataType autogenerated
				if (column.MemberName == "timestampDataType")
					continue;

				if (hasBitBug && column.MemberName == "bitDataType")
				{
					// this is a bug in ASE bulk copy implementation:
					// for first record it inserts false into bit field
					// assert it so we will know when it fixed
					Assert.That(actualValue, Is.EqualTo(false));
					continue;
				}

				Assert.That(actualValue, Is.EqualTo(testValue),
					actualValue is DateTimeOffset
						? $"Column  : {column.MemberName} {actualValue:yyyy-MM-dd HH:mm:ss.fffffff zzz} {testValue:yyyy-MM-dd HH:mm:ss.fffffff zzz}"
						: $"Column  : {column.MemberName}");
			}
		}

		[Test]
		public void BulkCopyAllTypesMultipleRows([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			BulkCopyAllTypes(context, BulkCopyType.MultipleRows);
		}

		[Test]
		public void BulkCopyAllTypesProviderSpecific([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			BulkCopyAllTypes(context, BulkCopyType.ProviderSpecific);
		}

		[Test]
		public async Task BulkCopyAllTypesMultipleRowsAsync([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			await BulkCopyAllTypesAsync(context, BulkCopyType.MultipleRows);
		}

		[Test]
		public async Task BulkCopyAllTypesProviderSpecificAsync([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			await BulkCopyAllTypesAsync(context, BulkCopyType.ProviderSpecific);
		}

		[Test]
		public void CreateAllTypes([IncludeDataSources(TestProvName.AllSybase)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<AllType>()
				.HasTableName("AllTypeCreateTest")
				.Build();

			using var db = GetDataConnection(context, ms);

			new FluentMappingBuilder(db.MappingSchema)
				.Entity<AllType>()
					.HasTableName("AllTypeCreateTest")
				.Build();

			try
			{
				db.DropTable<AllType>();
			}
			catch
			{
			}

			var table = db.CreateTable<AllType>();

			var list = table.ToList();

			db.DropTable<AllType>();
		}

		#endregion
	}
}
