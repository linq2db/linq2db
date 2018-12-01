using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Xml;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Mapping;

using NpgsqlTypes;

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tests.DataProvider
{
	using LinqToDB.SchemaProvider;
	using Model;
	using System.Linq.Expressions;

	[TestFixture]
	public class PostgreSQLTests : DataProviderTestBase
	{
		public PostgreSQLTests()
		{
			PassNullSql  = "SELECT \"ID\" FROM \"AllTypes\" WHERE :p IS NULL AND \"{0}\" IS NULL OR :p IS NOT NULL AND \"{0}\" = :p";
			PassValueSql = "SELECT \"ID\" FROM \"AllTypes\" WHERE \"{0}\" = :p";
			GetNullSql   = "SELECT \"{0}\" FROM \"{1}\" WHERE \"ID\" = 1";
			GetValueSql  = "SELECT \"{0}\" FROM \"{1}\" WHERE \"ID\" = 2";
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void TestParameters(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT :p"       , new { p = "1" }),                                Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT :p"       , new { p = new DataParameter { Value = 1 } }),    Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT :p1"      , new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT :p1 + :p2", new { p1 = 2, p2 = 3 }),                         Is.EqualTo(5));
				Assert.That(conn.Execute<int>   ("SELECT :p2 + :p1", new { p2 = 2, p1 = 3 }),                         Is.EqualTo(5));
				Assert.That(conn.Execute<string>("SELECT :p"       , new { p = 1 }),                                  Is.EqualTo("1"));
			}
		}

		T TestTypeEx<T>(DataConnection conn, string fieldName,
			DataType dataType          = DataType.Undefined,
			string   tableName         = "AllTypes",
			bool     skipPass          = false,
			bool     skipNull          = false,
			bool     skipDefinedNull   = false, //true,
			bool     skipDefaultNull   = false, //true,
			bool     skipUndefinedNull = true,
			bool     skipNotNull       = false,
			bool     skipDefined       = false,
			bool     skipDefault       = false,
			bool     skipUndefined     = false)
		{
			return TestType<T>(conn, fieldName, dataType, tableName, skipPass, skipNull, skipDefinedNull, skipDefaultNull,
				skipUndefinedNull, skipNotNull, skipDefined, skipDefault, skipUndefined);
		}

		public class TypeTestData
		{
			public TypeTestData(string name, Func<string, PostgreSQLTests, DataConnection, object> func, object result)
			{
				Name   = name;
				Func   = func;
				Result = result;
			}

			public TypeTestData(string name, int id, Func<string, PostgreSQLTests, DataConnection, object> func, object result)
			{
				Name   = name;
				ID     = id;
				Func   = func;
				Result = result;
			}

			public string Name                                                { get; set; }
			public int    ID                                                  { get; set; }
			public Func<string, PostgreSQLTests, DataConnection, object> Func { get; set; }
			public object Result                                              { get; set; }
		}

		class TestDataTypeAttribute : NUnitAttribute, ITestBuilder, IImplyFixture
		{
			public TestDataTypeAttribute(string providerName)
			{
				_providerName = providerName;
			}

			readonly string _providerName;

			public IEnumerable<TestMethod> BuildFrom(IMethodInfo method, Test suite)
			{
				var tests = UserProviders.Contains(_providerName) ?
					new[]
					{
						new TypeTestData("bigintDataType", 0,   (n,t,c) => t.TestTypeEx<long?>             (c, n, DataType.Int64),   1000000),
						new TypeTestData("bigintDataType", 1,   (n,t,c) => t.TestTypeEx<long?>             (c, n, DataType.Int64),   1000000),
						new TypeTestData("numericDataType",     (n,t,c) => t.TestTypeEx<decimal?>          (c, n, DataType.Decimal), 9999999m),
						new TypeTestData("smallintDataType",    (n,t,c) => t.TestTypeEx<short?>            (c, n, DataType.Int16),   25555),
						new TypeTestData("intDataType",         (n,t,c) => t.TestTypeEx<int?>              (c, n, DataType.Int32),   7777777),
//						new TypeTestData("moneyDataType",       (n,t,c) => t.TestTypeEx<decimal?>          (c, n, DataType.Money),   100000m),
						new TypeTestData("doubleDataType",      (n,t,c) => t.TestTypeEx<double?>           (c, n, DataType.Double),  20.31d),
						new TypeTestData("realDataType",        (n,t,c) => t.TestTypeEx<float?>            (c, n, DataType.Single),  16.2f),

#if NPG2
						new TypeTestData("timestampDataType",   (n,t,c) => t.TestTypeEx<NpgsqlTimeStamp?>  (c, n), new NpgsqlTimeStamp(2012, 12, 12, 12, 12, 12)),
						new TypeTestData("timestampTZDataType", (n,t,c) => t.TestTypeEx<NpgsqlTimeStampTZ?>(c, n), new NpgsqlTimeStampTZ(2012, 12, 12, 11, 12, 12, new NpgsqlTimeZone(-5, 0))),
						new TypeTestData("timeDataType",        (n,t,c) => t.TestTypeEx<NpgsqlTime?>       (c, n), new NpgsqlTime(12, 12, 12)),
						new TypeTestData("timeTZDataType",      (n,t,c) => t.TestTypeEx<NpgsqlTimeTZ?>     (c, n), new NpgsqlTimeTZ(12, 12, 12)),
						new TypeTestData("intervalDataType",    (n,t,c) => t.TestTypeEx<NpgsqlInterval?>   (c, n), new NpgsqlInterval(1, 3, 5, 20)),
						new TypeTestData("bitDataType",         (n,t,c) => t.TestTypeEx<BitString?>        (c, n), new BitString(new[] { true, false, true })),
						new TypeTestData("macaddrDataType",     (n,t,c) => t.TestTypeEx<NpgsqlMacAddress?> (c, n), new NpgsqlMacAddress("01:02:03:04:05:06")),
#else
//						new TypeTestData("timestampDataType",   (n,t,c) => t.TestTypeEx<NpgsqlTimeStamp?>  (c, n),                       new NpgsqlTimeStamp(2012, 12, 12, 12, 12, 12)),
//						new TypeTestData("timestampTZDataType", (n,t,c) => t.TestTypeEx<NpgsqlTimeStampTZ?>(c, n),                       new NpgsqlTimeStampTZ(2012, 12, 12, 11, 12, 12, new NpgsqlTimeZone(-5, 0))),
						new TypeTestData("timeDataType",        (n,t,c) => t.TestTypeEx<TimeSpan?>         (c, n),                       new TimeSpan(12, 12, 12)),
//						new TypeTestData("timeTZDataType",      (n,t,c) => t.TestTypeEx<NpgsqlTimeTZ?>     (c, n),                       new NpgsqlTimeTZ(12, 12, 12)),
//						new TypeTestData("intervalDataType",    (n,t,c) => t.TestTypeEx<TimeSpan?>         (c, n),                       new TimeSpan(1, 3, 5, 20)),
						new TypeTestData("bitDataType",         (n,t,c) => t.TestTypeEx<BitArray>          (c, n),                       new BitArray(new[] { true, false, true })),
						new TypeTestData("varBitDataType",      (n,t,c) => t.TestTypeEx<BitArray>          (c, n),                       new BitArray(new[] { true, false, true, true })),
						new TypeTestData("macaddrDataType",     (n,t,c) => t.TestTypeEx<PhysicalAddress>   (c, n, skipDefaultNull:true), new PhysicalAddress(new byte[] { 1, 2, 3, 4, 5, 6 })),
#endif

						new TypeTestData("timestampDataType",   (n,t,c) => t.TestTypeEx<DateTime?>         (c, n, DataType.DateTime2),      new DateTime(2012, 12, 12, 12, 12, 12)),
						new TypeTestData("timestampTZDataType", (n,t,c) => t.TestTypeEx<DateTimeOffset?>   (c, n, DataType.DateTimeOffset), new DateTimeOffset(2012, 12, 12, 11, 12, 12, new TimeSpan(-5, 0, 0))),
						new TypeTestData("dateDataType",    0,  (n,t,c) => t.TestTypeEx<NpgsqlDate?>       (c, n, skipDefaultNull:true),    new NpgsqlDate(2012, 12, 12)),
						new TypeTestData("dateDataType",    1,  (n,t,c) => t.TestTypeEx<DateTime?>         (c, n, DataType.Date),           new DateTime(2012, 12, 12)),

						new TypeTestData("charDataType",    0,  (n,t,c) => t.TestTypeEx<char?>             (c, n, DataType.Char),                           '1'),
						new TypeTestData("charDataType",    1,  (n,t,c) => t.TestTypeEx<string>            (c, n, DataType.Char,     skipDefaultNull:true), "1"),
						new TypeTestData("charDataType",    2,  (n,t,c) => t.TestTypeEx<string>            (c, n, DataType.NChar,    skipDefaultNull:true), "1"),
						new TypeTestData("varcharDataType", 0,  (n,t,c) => t.TestTypeEx<string>            (c, n, DataType.VarChar,  skipDefaultNull:true), "234"),
						new TypeTestData("varcharDataType", 1,  (n,t,c) => t.TestTypeEx<string>            (c, n, DataType.NVarChar, skipDefaultNull:true), "234"),
						new TypeTestData("textDataType",        (n,t,c) => t.TestTypeEx<string>            (c, n, DataType.Text,     skipDefaultNull:true), "567"),

						new TypeTestData("binaryDataType",  0,  (n,t,c) => t.TestTypeEx<byte[]>            (c, n, DataType.Binary),              new byte[] { 42 }),
						new TypeTestData("binaryDataType",  1,  (n,t,c) => t.TestTypeEx<byte[]>            (c, n, DataType.VarBinary),           new byte[] { 42 }),
						new TypeTestData("binaryDataType",  2,  (n,t,c) => t.TestTypeEx<Binary>            (c, n, DataType.VarBinary).ToArray(), new byte[] { 42 }),

						new TypeTestData("uuidDataType",        (n,t,c) => t.TestTypeEx<Guid?>             (c, n, DataType.Guid),        new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")),
						new TypeTestData("booleanDataType",     (n,t,c) => t.TestTypeEx<bool?>             (c, n, DataType.Boolean),     true),
//						new TypeTestData("colorDataType",       (n,t,c) => t.TestTypeEx<string>            (c, n, skipDefaultNull:true, skipDefault:true,skipUndefined:true), "Green"),

						new TypeTestData("pointDataType",       (n,t,c) => t.TestTypeEx<NpgsqlPoint?>      (c, n, skipNull:true, skipNotNull:true), new NpgsqlPoint(1, 2)),
						new TypeTestData("lsegDataType",        (n,t,c) => t.TestTypeEx<NpgsqlLSeg?>       (c, n, skipDefaultNull:true),            new NpgsqlLSeg   (new NpgsqlPoint(1, 2), new NpgsqlPoint(3, 4))),
						new TypeTestData("boxDataType",         (n,t,c) => t.TestTypeEx<NpgsqlBox?>        (c, n, skipDefaultNull:true).ToString(), new NpgsqlBox    (new NpgsqlPoint(3, 4), new NpgsqlPoint(1, 2)).ToString()),
#if !NPGSQL226
						new TypeTestData("pathDataType",        (n,t,c) => t.TestTypeEx<NpgsqlPath?>       (c, n, skipDefaultNull:true),            new NpgsqlPath   (new NpgsqlPoint(1, 2), new NpgsqlPoint(3, 4))),
						new TypeTestData("polygonDataType",     (n,t,c) => t.TestTypeEx<NpgsqlPolygon?>    (c, n, skipNull:true, skipNotNull:true), new NpgsqlPolygon(new NpgsqlPoint(1, 2), new NpgsqlPoint(3, 4))),
#endif
						new TypeTestData("circleDataType",      (n,t,c) => t.TestTypeEx<NpgsqlCircle?>     (c, n, skipDefaultNull:true),            new NpgsqlCircle (new NpgsqlPoint(1, 2), 3)),

						new TypeTestData("inetDataType",        (n,t,c) => t.TestTypeEx<NpgsqlInet?>       (c, n, skipDefaultNull:true),            new NpgsqlInet(new IPAddress(new byte[] { 192, 168, 1, 1 }))),

						new TypeTestData("xmlDataType",     0,  (n,t,c) => t.TestTypeEx<string>            (c, n, DataType.Xml, skipNull:true, skipNotNull:true),
							"<root><element strattr=\"strvalue\" intattr=\"12345\"/></root>"),
						new TypeTestData("xmlDataType",     1,  (n,t,c) => t.TestTypeEx<XDocument>         (c, n, DataType.Xml, skipNull:true, skipNotNull:true).ToString(),
							XDocument.Parse("<root><element strattr=\"strvalue\" intattr=\"12345\"/></root>").ToString()),
						new TypeTestData("xmlDataType",     2,  (n,t,c) => t.TestTypeEx<XmlDocument>       (c, n, DataType.Xml, skipNull:true, skipNotNull:true).InnerXml,
							ConvertTo<XmlDocument>.From("<root><element strattr=\"strvalue\" intattr=\"12345\"/></root>").InnerXml),
					}
					:
					new[]
					{
						new TypeTestData("ignore", (n,t,c) => t.TestTypeEx<long?>(c, n, DataType.Int64), 1000000)
					};

				var builder = new NUnitTestCaseBuilder();

				var data = tests.Select(t => new TestCaseParameters(new object[] { t.Name, t.ID, t, _providerName }));

				foreach (var item in data)
				{
					var test = builder.BuildTestMethod(method, suite, item);

					test.Properties.Set(PropertyNames.Category, _providerName);

					if (!UserProviders.Contains(_providerName))
					{
						test.RunState = RunState.Ignored;
						test.Properties.Set(PropertyNames.SkipReason, "Provider is disabled. See DataProviders.json");
					}

					yield return test;
				}
			}
		}

		[Test, TestDataType(ProviderName.PostgreSQL)]
		public void TestDataTypes(string typeName, int id, TypeTestData data, string context)
		{
			using (var conn = new DataConnection(context))
			{
				var value = data.Func(typeName, this, conn);
				if (data.Result is NpgsqlPoint)
				{
					Assert.IsTrue(object.Equals(value, data.Result));
				}
				else
				{
					Assert.AreEqual(value, data.Result);
				}
			}
		}

		static void TestNumeric<T>(DataConnection conn, T expectedValue, DataType dataType, string skip = "")
		{
			var skipTypes = skip.Split(' ');

			foreach (var sqlType in new[]
				{
					"bigint",
					"int",
					"money",
					"numeric",
					"numeric(38)",
					"smallint",

					"float",
					"real"
				}.Except(skipTypes))
			{
				var sqlValue = (object)expectedValue;

				var sql = string.Format("SELECT Cast({0} as {1})", sqlValue ?? "NULL", sqlType);

				Debug.WriteLine(sql + " -> " + typeof(T));

				Assert.That(conn.Execute<T>(sql), Is.EqualTo(expectedValue));
			}

			Debug.WriteLine("{0} -> DataType.{1}", typeof(T), dataType);
			Assert.That(conn.Execute<T>("SELECT :p", new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> auto", typeof(T));
			Assert.That(conn.Execute<T>("SELECT :p", new DataParameter { Name = "p", Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> new", typeof(T));
			Assert.That(conn.Execute<T>("SELECT :p", new { p = expectedValue }), Is.EqualTo(expectedValue));
		}

		static void TestSimple<T>(DataConnection conn, T expectedValue, DataType dataType)
			where T : struct
		{
			TestNumeric(conn, expectedValue, dataType);
			TestNumeric<T?>(conn, expectedValue, dataType);
			TestNumeric(conn, (T?)null, dataType);
		}

		//[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.Npgsql4)]
		public void TestNumerics(string context)
		{
			using (var conn = new DataConnection(context))
			{
				TestSimple<short> (conn, 1,   DataType.Int16);
				TestSimple        (conn, 1,   DataType.Int32);
				TestSimple        (conn, 1L,  DataType.Int64);
				TestSimple<byte>  (conn, 1,   DataType.Byte);
				TestSimple<ushort>(conn, 1,   DataType.UInt16);
				TestSimple        (conn, 1u,  DataType.UInt32);
				TestSimple        (conn, 1ul, DataType.UInt64);
				TestSimple<float> (conn, 1,   DataType.Single);
				TestSimple        (conn, 1d,  DataType.Double);
				TestSimple        (conn, 1m,  DataType.Decimal);
				TestSimple        (conn, 1m,  DataType.VarNumeric);
				TestSimple        (conn, 1m,  DataType.Money);
				TestSimple        (conn, 1m,  DataType.SmallMoney);
				TestSimple<sbyte> (conn, 1,   DataType.SByte);

				TestNumeric(conn, sbyte.MinValue, DataType.SByte, "money");
				TestNumeric(conn, sbyte.MaxValue, DataType.SByte);
				TestNumeric(conn, short.MinValue, DataType.Int16, "money");
				TestNumeric(conn, short.MaxValue, DataType.Int16);
				TestNumeric(conn, int.MinValue,   DataType.Int32, "money smallint");
				TestNumeric(conn, int.MaxValue,   DataType.Int32, "smallint real");
				TestNumeric(conn, long.MinValue,  DataType.Int64, "int money smallint");
				TestNumeric(conn, long.MaxValue,  DataType.Int64, "int money smallint float real");

				TestNumeric(conn, byte.MaxValue,   DataType.Byte);
				TestNumeric(conn, ushort.MaxValue, DataType.UInt16, "int smallint");
				TestNumeric(conn, uint.MaxValue,   DataType.UInt32, "int smallint real");
				TestNumeric(conn, ulong.MaxValue,  DataType.UInt64, "bigint int money smallint float real");

				TestNumeric(conn, -3.40282306E+38f,  DataType.Single, "bigint int money smallint numeric numeric(38)");
				TestNumeric(conn, 3.40282306E+38f,   DataType.Single, "bigint int money numeric numeric(38) smallint");
				TestNumeric(conn, -1.79E+308d,       DataType.Double, "bigint int money numeric numeric(38) smallint real");
				TestNumeric(conn, 1.79E+308d,        DataType.Double, "bigint int money numeric numeric(38) smallint real");
				TestNumeric(conn, decimal.MinValue,  DataType.Decimal, "bigint int money numeric numeric(38) smallint float real");
				TestNumeric(conn, decimal.MaxValue,  DataType.Decimal, "bigint int money numeric numeric(38) smallint float real");
				TestNumeric(conn, decimal.MinValue,  DataType.VarNumeric, "bigint int money numeric numeric(38) smallint float real");
				TestNumeric(conn, decimal.MaxValue,  DataType.VarNumeric, "bigint int money numeric numeric(38) smallint float real");
				TestNumeric(conn, -922337203685477m, DataType.Money, "int money smallint real");
				TestNumeric(conn, +922337203685477m, DataType.Money, "int smallint real");
				TestNumeric(conn, -214748m,          DataType.SmallMoney, "money smallint smallint");
				TestNumeric(conn, +214748m,          DataType.SmallMoney, "smallint");
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void TestDate(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12);

				Assert.That(conn.Execute<DateTime>("SELECT Cast('2012-12-12' as date)"), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12' as date)"), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime>("SELECT :p", DataParameter.Date("p", dateTime)), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT :p", new DataParameter("p", dateTime, DataType.Date)), Is.EqualTo(dateTime));
			}
		}

		/// <summary>
		/// Ensure we can pass data as Json parameter type and get
		/// same value back out equivalent in value
		/// </summary>
		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void TestJson(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var testJson = "{\"name\":\"bob\", \"age\":10}";

				Assert.That(conn.Execute<string>("SELECT :p", new DataParameter("p", testJson, DataType.Json)), Is.EqualTo(testJson));
			}
		}

		/// <summary>
		/// Ensure we can pass data as binary json and have things handled
		/// with values coming back as being equivalent in value
		/// </summary>
		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void TestJsonb(string context)
		{
			var json = new { name = "bob", age = 10 };
			using (var conn = new DataConnection(context))
			{
				//properties come back out in potentially diff order as its being
				//converted between a binary json format and the string representation
				var raw = conn.Execute<string>("SELECT :p", new DataParameter("p", JsonConvert.SerializeObject(json), DataType.BinaryJson));
				var obj = JObject.Parse(raw);

				Assert.That(obj.Value<string>("name"), Is.EqualTo(json.name));
				Assert.That(obj.Value<int>("age"), Is.EqualTo(json.age));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void TestDateTime(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime>("SELECT Cast('2012-12-12 12:12:12' as timestamp)"), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12' as timestamp)"), Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime>("SELECT :p", DataParameter.DateTime("p", dateTime)), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT :p", new DataParameter("p", dateTime)), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT :p", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void TestChar(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<char>("SELECT Cast('1' as char)"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char)"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char>("SELECT Cast('1' as char(1))"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char(1))"), Is.EqualTo('1'));

				Assert.That(conn.Execute<char>("SELECT Cast('1' as varchar)"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar)"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char>("SELECT Cast('1' as varchar(20))"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar(20))"), Is.EqualTo('1'));

				Assert.That(conn.Execute<char>("SELECT :p", DataParameter.Char("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p", DataParameter.Char("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char>("SELECT Cast(:p as char)", DataParameter.Char("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(:p as char)", DataParameter.Char("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char>("SELECT Cast(:p as char(1))", DataParameter.Char("@p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(:p as char(1))", DataParameter.Char("@p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char>("SELECT :p", DataParameter.VarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p", DataParameter.VarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char>("SELECT :p", DataParameter.NChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p", DataParameter.NChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char>("SELECT :p", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char>("SELECT :p", DataParameter.Create("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p", DataParameter.Create("p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char>("SELECT :p", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void TestString(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20))"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as char(20))"), Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(20))"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as varchar(20))"), Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as text)"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as text)"), Is.Null);

				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.Char("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.VarChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.Text("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.NChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.NText("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.Create("p", "123")), Is.EqualTo("123"));

				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.Create("p", (string)null)), Is.EqualTo(null));
				Assert.That(conn.Execute<string>("SELECT :p", new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void TestBinary(string context)
		{
			var arr1 = new byte[] { 48, 57 };

			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<byte[]>("SELECT E'\\060\\071'::bytea"), Is.EqualTo(arr1));

				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Binary("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", null)), Is.EqualTo(null));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", new byte[0])), Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Image("p", new byte[0])), Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create("p", new Binary(arr1))), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter("p", new Binary(arr1))), Is.EqualTo(arr1));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void TestGuid(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(
					conn.Execute<Guid>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uuid)"),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				Assert.That(
					conn.Execute<Guid?>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uuid)"),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				var guid = Guid.NewGuid();

				Assert.That(conn.Execute<Guid>("SELECT :p", DataParameter.Create("p", guid)), Is.EqualTo(guid));
				Assert.That(conn.Execute<Guid>("SELECT :p", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void TestXml(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT XMLPARSE (DOCUMENT'<xml/>')"), Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>("SELECT XMLPARSE (DOCUMENT'<xml/>')").ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT XMLPARSE (DOCUMENT'<xml/>')").InnerXml, Is.EqualTo("<xml />"));

				var xdoc = XDocument.Parse("<xml/>");
				var xml = Convert<string, XmlDocument>.Lambda("<xml/>");

				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Xml("p", "<xml/>")), Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>("SELECT @p", DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT @p", DataParameter.Xml("p", xml)).InnerXml, Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>("SELECT @p", new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>("SELECT @p", new DataParameter("p", xml)).ToString(), Is.EqualTo("<xml />"));
			}
		}

		enum TestEnum
		{
			[MapValue("A")] AA,
			[MapValue("B")] BB
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void TestEnum1(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<TestEnum>("SELECT 'A'"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'A'"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum>("SELECT 'B'"), Is.EqualTo(TestEnum.BB));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'B'"), Is.EqualTo(TestEnum.BB));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void TestEnum2(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT @p", new { p = TestEnum.AA }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = (TestEnum?)TestEnum.BB }), Is.EqualTo("B"));

				Assert.That(conn.Execute<string>("SELECT @p", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = conn.MappingSchema.GetConverter<TestEnum?, string>()(TestEnum.AA) }), Is.EqualTo("A"));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void SequenceInsert1(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<PostgreSQLSpecific.SequenceTest1>().Where(_ => _.Value == "SeqValue").Delete();
				db.Insert(new PostgreSQLSpecific.SequenceTest1 { Value = "SeqValue" });

				var id = db.GetTable<PostgreSQLSpecific.SequenceTest1>().Single(_ => _.Value == "SeqValue").ID;

				db.GetTable<PostgreSQLSpecific.SequenceTest1>().Where(_ => _.ID == id).Delete();

				Assert.AreEqual(0, db.GetTable<PostgreSQLSpecific.SequenceTest1>().Count(_ => _.Value == "SeqValue"));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void SequenceInsert2(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<PostgreSQLSpecific.SequenceTest2>().Where(_ => _.Value == "SeqValue").Delete();
				db.Insert(new PostgreSQLSpecific.SequenceTest2 { Value = "SeqValue" });

				var id = db.GetTable<PostgreSQLSpecific.SequenceTest2>().Single(_ => _.Value == "SeqValue").ID;

				db.GetTable<PostgreSQLSpecific.SequenceTest2>().Where(_ => _.ID == id).Delete();

				Assert.AreEqual(0, db.GetTable<PostgreSQLSpecific.SequenceTest2>().Count(_ => _.Value == "SeqValue"));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void SequenceInsert3(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<PostgreSQLSpecific.SequenceTest3>().Where(_ => _.Value == "SeqValue").Delete();
				db.Insert(new PostgreSQLSpecific.SequenceTest3 { Value = "SeqValue" });

				var id = db.GetTable<PostgreSQLSpecific.SequenceTest3>().Single(_ => _.Value == "SeqValue").ID;

				db.GetTable<PostgreSQLSpecific.SequenceTest3>().Where(_ => _.ID == id).Delete();

				Assert.AreEqual(0, db.GetTable<PostgreSQLSpecific.SequenceTest3>().Count(_ => _.Value == "SeqValue"));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void SequenceInsertWithIdentity1(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<PostgreSQLSpecific.SequenceTest1>().Where(_ => _.Value == "SeqValue").Delete();

				var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.SequenceTest1 { Value = "SeqValue" }));
				var id2 = db.GetTable<PostgreSQLSpecific.SequenceTest1>().Single(_ => _.Value == "SeqValue").ID;

				Assert.AreEqual(id1, id2);

				db.GetTable<PostgreSQLSpecific.SequenceTest1>().Where(_ => _.ID == id1).Delete();

				Assert.AreEqual(0, db.GetTable<PostgreSQLSpecific.SequenceTest1>().Count(_ => _.Value == "SeqValue"));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void SequenceInsertWithIdentity2(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<PostgreSQLSpecific.SequenceTest2>().Where(_ => _.Value == "SeqValue").Delete();

				var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.SequenceTest2 { Value = "SeqValue" }));
				var id2 = db.GetTable<PostgreSQLSpecific.SequenceTest2>().Single(_ => _.Value == "SeqValue").ID;

				Assert.AreEqual(id1, id2);

				db.GetTable<PostgreSQLSpecific.SequenceTest2>().Where(_ => _.ID == id1).Delete();

				Assert.AreEqual(0, db.GetTable<PostgreSQLSpecific.SequenceTest2>().Count(_ => _.Value == "SeqValue"));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void SequenceInsertWithIdentity3(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<PostgreSQLSpecific.SequenceTest3>().Where(_ => _.Value == "SeqValue").Delete();

				var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.SequenceTest3 { Value = "SeqValue" }));
				var id2 = db.GetTable<PostgreSQLSpecific.SequenceTest3>().Single(_ => _.Value == "SeqValue").ID;

				Assert.AreEqual(id1, id2);

				db.GetTable<PostgreSQLSpecific.SequenceTest3>().Where(_ => _.ID == id1).Delete();

				Assert.AreEqual(0, db.GetTable<PostgreSQLSpecific.SequenceTest3>().Count(_ => _.Value == "SeqValue"));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void SequenceInsertWithIdentity4(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<PostgreSQLSpecific.TestSchemaIdentity>().Delete();

				var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.TestSchemaIdentity()));
				var id2 = db.GetTable<PostgreSQLSpecific.TestSchemaIdentity>().Single().ID;

				Assert.AreEqual(id1, id2);

				db.GetTable<PostgreSQLSpecific.TestSchemaIdentity>().Delete();
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void SequenceInsertWithIdentity5(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<PostgreSQLSpecific.TestSerialIdentity>().Delete();

				var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.TestSerialIdentity()));
				var id2 = db.GetTable<PostgreSQLSpecific.TestSerialIdentity>().Single().ID;

				Assert.AreEqual(id1, id2);

				db.GetTable<PostgreSQLSpecific.TestSerialIdentity>().Delete();
			}
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void BulkCopyLinqTypes(string context)
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
								ID = 4000 + n,
								MoneyValue = 1000m + n,
								DateTimeValue = new DateTime(2001, 1, 11, 1, 11, 21, 100),
								BoolValue = true,
								GuidValue = Guid.NewGuid(),
								SmallIntValue = (short)n
							}
						));

					db.GetTable<LinqDataTypes>().Delete(p => p.ID >= 4000);
				}
			}
		}

		public class TestTeamplate
		{
			public string cdni_cd_cod_numero_item1;
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void Issue140(string context)
		{
			using (var db = new DataConnection(context))
			{
				var list = db.Query<TestTeamplate>("select 1 as cdni_cd_cod_numero_item1").ToList();

				Assert.That(list.Count, Is.EqualTo(1));
				Assert.That(list[0].cdni_cd_cod_numero_item1, Is.EqualTo("1"));
			}
		}

		public class CreateTableTestClass
		{
			public DateTimeOffset TimeOffset;
			public Guid Guid;
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void CreateTableTest(string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<CreateTableTestClass>())
			{
				var e = new CreateTableTestClass
				{
					Guid = Guid.NewGuid(),
					TimeOffset = new DateTimeOffset(2017, 06, 17, 16, 40, 33, 0, TimeSpan.FromHours(-3))
				};
				db.Insert(e);

				var e2 = db.GetTable<CreateTableTestClass>()
					.FirstOrDefault(_ => _.Guid == e.Guid);

				Assert.IsNotNull(e2);
				Assert.AreEqual(e.Guid, e2.Guid);
				Assert.AreEqual(e.TimeOffset, e2.TimeOffset);
			}
		}

#if !NPGSQL226
		[Test, IncludeDataContextSource(ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)]
		public void NpgsqlDateTimeTest(string context)
		{
			PostgreSQLTools.GetDataProvider().CreateConnection(DataConnection.GetConnectionString(context));

			var d = new NpgsqlDateTime(DateTime.Today);
			var o = new DateTimeOffset(DateTime.Today);
			var c1 = PostgreSQLTools.GetDataProvider().MappingSchema.GetConvertExpression<NpgsqlDateTime, DateTimeOffset>();
			var c2 = PostgreSQLTools.GetDataProvider().MappingSchema.GetConvertExpression<NpgsqlDateTime, DateTimeOffset?>();

			Assert.IsNotNull(c1);
			Assert.IsNotNull(c2);

			Assert.AreEqual(o, c1.Compile()(d));
			Assert.AreEqual(o, c2.Compile()(d).Value);
		}
#endif

		[Table]
		public class AllTypes
		{
			[Column, PrimaryKey, Identity]             public int ID                                    { get; set; }
			// numeric/monetary
			[Column]                                   public long?    bigintDataType                   { get; set; }
			[Column]                                   public decimal? numericDataType                  { get; set; }
			[Column]                                   public short?   smallintDataType                 { get; set; }
			[Column]                                   public int?     intDataType                      { get; set; }
			[Column  (DataType = DataType.Money)]      public decimal? moneyDataType                    { get; set; }
			[Column]                                   public double?  doubleDataType                   { get; set; }
			[Column]                                   public float?   realDataType                     { get; set; }
			// time/date/intertval
			[Column]                                   public NpgsqlDateTime? timestampDataType         { get; set; }
			[Column]                                   public DateTimeOffset? timestampTZDataType       { get; set; }
			[Column]                                   public NpgsqlDate?     dateDataType              { get; set; }
			[Column]                                   public TimeSpan?       timeDataType              { get; set; }
			[Column  (DbType = "time with time zone")] public DateTimeOffset? timeTZDataType            { get; set; }
			[Column]                                   public NpgsqlTimeSpan? intervalDataType          { get; set; }
			// text
			[Column]                                   public char?  charDataType                       { get; set; }
			[Column]                                   public string char20DataType                     { get; set; }
			[Column]                                   public string varcharDataType                    { get; set; }
			[Column]                                   public string textDataType                       { get; set; }
			// misc
			[Column]                                   public byte[]   binaryDataType                   { get; set; }
			[Column]                                   public Guid?    uuidDataType                     { get; set; }
			[Column]                                   public BitArray bitDataType                      { get; set; }
			[Column]                                   public bool?    booleanDataType                  { get; set; }
			[Column]                                   public string   colorDataType                    { get; set; }
			[Column]                                   public string   xmlDataType                      { get; set; }
			[Column]                                   public BitArray varBitDataType                   { get; set; }
			// geometry
			[Column]                                   public NpgsqlPoint?   pointDataType              { get; set; }
			[Column]                                   public NpgsqlLSeg?    lsegDataType               { get; set; }
			[Column]                                   public NpgsqlBox?     boxDataType                { get; set; }
			[Column]                                   public NpgsqlPath?    pathDataType               { get; set; }
			[Column]                                   public NpgsqlPolygon? polygonDataType            { get; set; }
			[Column]                                   public NpgsqlCircle?  circleDataType             { get; set; }
			[NotColumn(Configuration = ProviderName.PostgreSQL92)]
			[NotColumn(Configuration = ProviderName.PostgreSQL93)]
			[Column]                                   public NpgsqlLine?    lineDataType               { get; set; }
			// inet types
			[Column]                                   public IPAddress       inetDataType              { get; set; }
			[Column  (DbType = "cidr")]                public NpgsqlInet?     cidrDataType              { get; set; }
			[Column  (DbType = "macaddr")]             public PhysicalAddress macaddrDataType           { get; set; }
			// PGSQL10+
			[Column(DbType = "macaddr8", Configuration = TestProvName.PostgreSQL10)]
			[Column(DbType = "macaddr8", Configuration = TestProvName.PostgreSQL11)]
			[Column(DbType = "macaddr8", Configuration = TestProvName.PostgreSQLLatest)]
			                                           public PhysicalAddress macaddr8DataType          { get; set; }
			// json
			[Column]                                   public string jsonDataType                       { get; set; }
			[NotColumn(Configuration = ProviderName.PostgreSQL92)]
			[NotColumn(Configuration = ProviderName.PostgreSQL93)]
			[Column  (DataType = DataType.BinaryJson)] public string jsonbDataType                      { get; set; }

			public static IEqualityComparer<AllTypes> Comparer = Tools.ComparerBuilder<AllTypes>.GetEqualityComparer(true);
		}

		public enum BulkTestMode
		{
			WithoutTransaction,
			WithTransaction,
			WithRollback
		}

		// test that native bulk copy method inserts data properly
		[Test]
		public void BulkCopyTest([Values]BulkTestMode mode, [IncludeDataSources(false, ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)] string context)
		{
			var macaddr8Supported = context.Contains(TestProvName.PostgreSQL10) || context.Contains(TestProvName.PostgreSQL11) || context.Contains(TestProvName.PostgreSQLLatest);
			var lineSupported     = !context.Contains(ProviderName.PostgreSQL92) && !context.Contains(ProviderName.PostgreSQL93);
			var jsonbSupported    = !context.Contains(ProviderName.PostgreSQL92) && !context.Contains(ProviderName.PostgreSQL93);
			var testData = new[]
			{
				// test null values
				new AllTypes(),
				// test non-null values
				new AllTypes()
				{
					bigintDataType      = long.MaxValue,
					numericDataType     = 12345.6789M,
					smallintDataType    = short.MaxValue,
					intDataType         = int.MaxValue,
					moneyDataType       = 9876.54M,
					doubleDataType      = double.MaxValue,
					realDataType        = float.MaxValue,

					timestampDataType   = new NpgsqlDateTime(2010, 5, 30, 1, 2, 3, 4),
					timestampTZDataType = new DateTimeOffset(2011, 3, 22, 10, 11, 12, 13, TimeSpan.FromMinutes(30)),
					dateDataType        = new NpgsqlDate(2010, 5, 30),
					timeDataType        = new TimeSpan(0, 1, 2, 3, 4),
					timeTZDataType      = new DateTimeOffset(1, 1, 2, 10, 11, 12, 13, TimeSpan.FromMinutes(30)),
					intervalDataType    = TimeSpan.FromTicks(-123456780),

					charDataType        = 'ы',
					char20DataType      = "тест1",
					varcharDataType     = "тест2",
					textDataType        = "текст",

					binaryDataType      = new byte[] { 1, 2, 3 },
					uuidDataType        = Guid.NewGuid(),
					bitDataType         = new BitArray(new []{ true, false, true }),
					booleanDataType     = true,
					colorDataType       = "Green",
					xmlDataType         = "<test>data</test>",
					varBitDataType      = new BitArray(new []{ true, false, true, false, true }),

					pointDataType       = new NpgsqlPoint(1.4, 4.3),
					lsegDataType        = new NpgsqlLSeg(1.1, 2.2, 3.3, 4.4),
					boxDataType         = new NpgsqlBox(6.6, 5.5, 4.4, 3.3),
					pathDataType        = new NpgsqlPath(new NpgsqlPoint(1.4, 4.3), new NpgsqlPoint(2.4, 2.3), new NpgsqlPoint(3.4, 3.3)),
					polygonDataType     = new NpgsqlPolygon(new NpgsqlPoint(1.4, 4.3), new NpgsqlPoint(6.4, 2.3), new NpgsqlPoint(3.4, 7.3)),
					circleDataType      = new NpgsqlCircle(1.1, 2.2, 3.3),
					lineDataType        = new NpgsqlLine(3.3, 4.4, 5.5),

					inetDataType        = IPAddress.Parse("2001:0db8:0000:0042:0000:8a2e:0370:7334"),
					cidrDataType        = new NpgsqlInet("::ffff:1.2.3.0/120"),
					macaddrDataType     = PhysicalAddress.Parse("08-00-2B-01-02-03"),
					macaddr8DataType    = PhysicalAddress.Parse("08-00-2B-FF-FE-01-02-03"),

					jsonDataType        = "{\"test\": 1}",
					jsonbDataType       = "{\"test\": 2}"
				}
			};

			using (var db = new DataConnection(context, new MappingSchema(context)))
			{
				// color enum type will not work without this call if _create test was run in the same session
				// More details here: https://github.com/npgsql/npgsql/issues/1357
				// must be called before transaction opened due to: https://github.com/npgsql/npgsql/issues/2244
				((dynamic)db.Connection).ReloadTypes();

				DataConnectionTransaction ts = null;

				if (mode != BulkTestMode.WithoutTransaction)
					ts = db.BeginTransaction();

				int[] ids = null;
				try
				{
					var result = db.BulkCopy(new BulkCopyOptions() { BulkCopyType = BulkCopyType.ProviderSpecific }, testData);

					Assert.AreEqual(testData.Length, result.RowsCopied);

					var data = db.GetTable<AllTypes>().OrderByDescending(_ => _.ID).Take(2).AsEnumerable().Reverse().ToArray();

					ids = data.Select(_ => _.ID).ToArray();

					// comparer generator miss collections support
					if (testData.Length == data.Length)
						for (var i = 0; i < testData.Length; i++)
						{
							var expectedBinary = testData[i].binaryDataType;
							var actualBinary = data[i].binaryDataType;

							if (expectedBinary != null && actualBinary != null)
								Assert.True(expectedBinary.SequenceEqual(actualBinary));
							else if (expectedBinary != null || actualBinary != null)
								Assert.Fail();

							var expectedBit = testData[i].bitDataType;
							var actualBit = data[i].bitDataType;

							if (expectedBit != null && actualBit != null)
								Assert.True(expectedBit.Cast<bool>().SequenceEqual(actualBit.Cast<bool>()));
							else if (expectedBit != null || actualBit != null)
								Assert.Fail();

							expectedBit = testData[i].varBitDataType;
							actualBit = data[i].varBitDataType;

							if (expectedBit != null && actualBit != null)
								Assert.True(expectedBit.Cast<bool>().SequenceEqual(actualBit.Cast<bool>()));
							else if (expectedBit != null || actualBit != null)
								Assert.Fail();
						}

					AreEqual(
						r => {
							r.ID             = 0;
							r.binaryDataType = null;
							r.bitDataType    = null;
							r.varBitDataType = null;
							if (!lineSupported)     r.lineDataType     = null;
							if (!jsonbSupported)    r.jsonbDataType    = null;
							if (!macaddr8Supported) r.macaddr8DataType = null;
							// npgsql4 returns 2/1/1 instead of 1/1/1 as date part
							if (r.timeTZDataType != null && !context.Contains(TestProvName.PostgreSQLLatest))
								r.timeTZDataType = r.timeTZDataType.Value.AddDays(-1);
							return r;
						},
						testData,
						data,
						AllTypes.Comparer);

					if (mode != BulkTestMode.WithoutTransaction)
						ts.Rollback();
				}
				finally
				{
					if (mode == BulkTestMode.WithoutTransaction)
						db.GetTable<AllTypes>().Where(_ => ids.Contains(_.ID)).Delete();
				}

				if (mode == BulkTestMode.WithRollback)
					Assert.AreEqual(0, db.GetTable<AllTypes>().Where(_ => ids.Contains(_.ID)).Count());
			}
		}

		public static IEnumerable<ProcedureSchema> ProcedureTestCases
		{
			get
			{
				// test table function schema
				yield return new ProcedureSchema()
				{
					CatalogName = "SET_BY_TEST",
					SchemaName = "public",
					ProcedureName = "TestTableFunctionSchema",
					MemberName = "TestTableFunctionSchema",
					IsFunction = true,
					IsTableFunction = true,
					IsAggregateFunction = false,
					IsDefaultSchema = true,
					IsLoaded = true,
					Parameters = new List<ParameterSchema>(),
					ResultTable = new TableSchema()
					{
						IsProcedureResult = true,
						TypeName = "TestTableFunctionSchemaResult",
						Columns = new List<ColumnSchema>()
						{
							new ColumnSchema()
							{
								ColumnName = "ID",
								ColumnType = "int4",
								MemberName = "ID",
								MemberType = "int?",
								SystemType = typeof(int),
								IsNullable = true, // must be false, but we don't get this information from provider
								DataType   = DataType.Int32
							},
							new ColumnSchema()
							{
								ColumnName = "bigintDataType",
								ColumnType = "int8",
								MemberName = "bigintDataType",
								MemberType = "long?",
								SystemType = typeof(long),
								IsNullable = true,
								DataType   = DataType.Int64
							},
							new ColumnSchema()
							{
								ColumnName = "numericDataType",
								ColumnType = "numeric(0,0)",
								MemberName = "numericDataType",
								MemberType = "decimal?",
								SystemType = typeof(decimal),
								IsNullable = true,
								DataType   = DataType.Decimal
							},
							new ColumnSchema()
							{
								ColumnName = "smallintDataType",
								ColumnType = "int2",
								MemberName = "smallintDataType",
								MemberType = "short?",
								SystemType = typeof(short),
								IsNullable = true,
								DataType   = DataType.Int16
							},
							new ColumnSchema()
							{
								ColumnName = "intDataType",
								ColumnType = "int4",
								MemberName = "intDataType",
								MemberType = "int?",
								SystemType = typeof(int),
								IsNullable = true,
								DataType   = DataType.Int32
							},
							new ColumnSchema()
							{
								ColumnName = "moneyDataType",
								ColumnType = "money",
								MemberName = "moneyDataType",
								MemberType = "decimal?",
								SystemType = typeof(decimal),
								IsNullable = true,
								DataType   = DataType.Money
							},
							new ColumnSchema()
							{
								ColumnName = "doubleDataType",
								ColumnType = "float8",
								MemberName = "doubleDataType",
								MemberType = "double?",
								SystemType = typeof(double),
								IsNullable = true,
								DataType   = DataType.Double
							},
							new ColumnSchema()
							{
								ColumnName = "realDataType",
								ColumnType = "float4",
								MemberName = "realDataType",
								MemberType = "float?",
								SystemType = typeof(float),
								IsNullable = true,
								DataType   = DataType.Single
							},
							new ColumnSchema()
							{
								ColumnName           = "timestampDataType",
								ColumnType           = "timestamp (0) without time zone",
								MemberName           = "timestampDataType",
								MemberType           = "DateTime?",
								SystemType           = typeof(DateTime),
								ProviderSpecificType = "NpgsqlDate",
								IsNullable           = true,
								DataType             = DataType.DateTime2
							},
							new ColumnSchema()
							{
								ColumnName           = "timestampTZDataType",
								ColumnType           = "timestamp (0) with time zone",
								MemberName           = "timestampTZDataType",
								MemberType           = "DateTimeOffset?",
								SystemType           = typeof(DateTimeOffset),
								ProviderSpecificType = "NpgsqlDate",
								IsNullable           = true,
								DataType             = DataType.DateTimeOffset
							},
							new ColumnSchema()
							{
								ColumnName           = "dateDataType",
								ColumnType           = "date",
								MemberName           = "dateDataType",
								MemberType           = "NpgsqlDate?",
								SystemType           = typeof(NpgsqlDate),
								ProviderSpecificType = "NpgsqlDate",
								IsNullable           = true,
								DataType             = DataType.Date
							},
							new ColumnSchema()
							{
								ColumnName = "timeDataType",
								ColumnType = "time (0) without time zone",
								MemberName = "timeDataType",
								MemberType = "TimeSpan?",
								SystemType = typeof(TimeSpan),
								IsNullable = true,
								DataType   = DataType.Time
							},
							new ColumnSchema()
							{
								ColumnName           = "timeTZDataType",
								ColumnType           = "time (0) with time zone",
								MemberName           = "timeTZDataType",
								MemberType           = "DateTimeOffset?",
								SystemType           = typeof(DateTimeOffset),
								IsNullable           = true,
								DataType             = DataType.Time
							},
							new ColumnSchema()
							{
								ColumnName           = "intervalDataType",
								ColumnType           = "interval(0)",
								MemberName           = "intervalDataType",
								MemberType           = "NpgsqlTimeSpan?",
								SystemType           = typeof(NpgsqlTimeSpan),
								ProviderSpecificType = "NpgsqlTimeSpan",
								IsNullable           = true,
								DataType             = DataType.Time
							},
							new ColumnSchema()
							{
								ColumnName = "charDataType",
								ColumnType = "character(1)",
								MemberName = "charDataType",
								MemberType = "char?",
								SystemType = typeof(char),
								IsNullable = true,
								DataType   = DataType.NChar
							},
							new ColumnSchema()
							{
								ColumnName = "char20DataType",
								ColumnType = "character(20)",
								MemberName = "char20DataType",
								MemberType = "string",
								SystemType = typeof(string),
								IsNullable = true,
								DataType   = DataType.NChar
							},
							new ColumnSchema()
							{
								ColumnName = "varcharDataType",
								ColumnType = "character varying(20)",
								MemberName = "varcharDataType",
								MemberType = "string",
								SystemType = typeof(string),
								IsNullable = true,
								DataType   = DataType.NVarChar
							},
							new ColumnSchema()
							{
								ColumnName = "textDataType",
								ColumnType = "text",
								MemberName = "textDataType",
								MemberType = "string",
								SystemType = typeof(string),
								IsNullable = true,
								DataType   = DataType.Text
							},
							new ColumnSchema()
							{
								ColumnName = "binaryDataType",
								ColumnType = "bytea",
								MemberName = "binaryDataType",
								MemberType = "byte[]",
								SystemType = typeof(byte[]),
								IsNullable = true,
								DataType   = DataType.Binary
							},
							new ColumnSchema()
							{
								ColumnName = "uuidDataType",
								ColumnType = "uuid",
								MemberName = "uuidDataType",
								MemberType = "Guid?",
								SystemType = typeof(Guid),
								IsNullable = true,
								DataType   = DataType.Guid
							},
							new ColumnSchema()
							{
								ColumnName = "bitDataType",
								ColumnType = "bit(-1)", // TODO: must be 3, but npgsql3 doesn't return it (npgsql4 do)
								MemberName = "bitDataType",
								MemberType = "BitArray",
								SystemType = typeof(BitArray),
								IsNullable = true,
								DataType   = DataType.BitArray
							},
							new ColumnSchema()
							{
								ColumnName = "booleanDataType",
								ColumnType = "bool",
								MemberName = "booleanDataType",
								MemberType = "bool?",
								SystemType = typeof(bool),
								IsNullable = true,
								DataType   = DataType.Boolean
							},
							new ColumnSchema()
							{
								ColumnName = "colorDataType",
								ColumnType = "public.color",
								MemberName = "colorDataType",
								MemberType = "string",
								SystemType = typeof(string),
								IsNullable = true,
								DataType   = DataType.Undefined
							},
							new ColumnSchema()
							{
								ColumnName           = "pointDataType",
								ColumnType           = "point",
								MemberName           = "pointDataType",
								MemberType           = "NpgsqlPoint?",
								SystemType           = typeof(NpgsqlPoint),
								ProviderSpecificType = "NpgsqlPoint",
								IsNullable           = true,
								DataType             = DataType.Udt
							},
							new ColumnSchema()
							{
								ColumnName           = "lsegDataType",
								ColumnType           = "lseg",
								MemberName           = "lsegDataType",
								MemberType           = "NpgsqlLSeg?",
								SystemType           = typeof(NpgsqlLSeg),
								ProviderSpecificType = "NpgsqlLSeg",
								IsNullable           = true,
								DataType             = DataType.Udt
							},
							new ColumnSchema()
							{
								ColumnName           = "boxDataType",
								ColumnType           = "box",
								MemberName           = "boxDataType",
								MemberType           = "NpgsqlBox?",
								SystemType           = typeof(NpgsqlBox),
								ProviderSpecificType = "NpgsqlBox",
								IsNullable           = true,
								DataType             = DataType.Udt
							},
							new ColumnSchema()
							{
								ColumnName           = "pathDataType",
								ColumnType           = "path",
								MemberName           = "pathDataType",
								MemberType           = "NpgsqlPath?",
								SystemType           = typeof(NpgsqlPath),
								ProviderSpecificType = "NpgsqlPath",
								IsNullable           = true,
								DataType             = DataType.Udt
							},
							new ColumnSchema()
							{
								ColumnName           = "polygonDataType",
								ColumnType           = "polygon",
								MemberName           = "polygonDataType",
								MemberType           = "NpgsqlPolygon?",
								SystemType           = typeof(NpgsqlPolygon),
								ProviderSpecificType = "NpgsqlPolygon",
								IsNullable           = true,
								DataType             = DataType.Udt
							},
							new ColumnSchema()
							{
								ColumnName           = "circleDataType",
								ColumnType           = "circle",
								MemberName           = "circleDataType",
								MemberType           = "NpgsqlCircle?",
								SystemType           = typeof(NpgsqlCircle),
								ProviderSpecificType = "NpgsqlCircle",
								IsNullable           = true,
								DataType             = DataType.Udt
							},
							new ColumnSchema()
							{
								ColumnName           = "lineDataType",
								ColumnType           = "line",
								MemberName           = "lineDataType",
								MemberType           = "NpgsqlLine?",
								SystemType           = typeof(NpgsqlLine),
								ProviderSpecificType = "NpgsqlLine",
								IsNullable           = true,
								DataType             = DataType.Udt
							},
							new ColumnSchema()
							{
								ColumnName           = "inetDataType",
								ColumnType           = "inet",
								MemberName           = "inetDataType",
								MemberType           = "NpgsqlInet?",
								SystemType           = typeof(NpgsqlInet),
								ProviderSpecificType = "NpgsqlInet",
								IsNullable           = true,
								DataType             = DataType.Udt
							},
							new ColumnSchema()
							{
								ColumnName           = "cidrDataType",
								ColumnType           = "cidr",
								MemberName           = "cidrDataType",
								MemberType           = "NpgsqlInet?",
								SystemType           = typeof(NpgsqlInet),
								ProviderSpecificType = "NpgsqlInet",
								IsNullable           = true,
								DataType             = DataType.Udt
							},
							new ColumnSchema()
							{
								ColumnName = "macaddrDataType",
								ColumnType = "macaddr",
								MemberName = "macaddrDataType",
								MemberType = "PhysicalAddress",
								SystemType = typeof(PhysicalAddress),
								IsNullable = true,
								DataType   = DataType.Udt
							},
							new ColumnSchema()
							{
								ColumnName = "macaddr8DataType",
								ColumnType = "macaddr8",
								MemberName = "macaddr8DataType",
								MemberType = "PhysicalAddress",
								SystemType = typeof(PhysicalAddress),
								IsNullable = true,
								DataType   = DataType.Udt
							},
							new ColumnSchema()
							{
								ColumnName = "jsonDataType",
								ColumnType = "json",
								MemberName = "jsonDataType",
								MemberType = "string",
								SystemType = typeof(string),
								IsNullable = true,
								DataType   = DataType.Json
							},
							new ColumnSchema()
							{
								ColumnName = "jsonbDataType",
								ColumnType = "jsonb",
								MemberName = "jsonbDataType",
								MemberType = "string",
								SystemType = typeof(string),
								IsNullable = true,
								DataType   = DataType.BinaryJson
							},
							new ColumnSchema()
							{
								ColumnName = "xmlDataType",
								ColumnType = "xml",
								MemberName = "xmlDataType",
								MemberType = "string",
								SystemType = typeof(string),
								IsNullable = true,
								DataType   = DataType.Xml
							},
							new ColumnSchema()
							{
								ColumnName = "varBitDataType",
								ColumnType = "bit varying(-1)", // TODO: length missing from npgsql
								MemberName = "varBitDataType",
								MemberType = "BitArray",
								SystemType = typeof(BitArray),
								IsNullable = true,
								DataType   = DataType.BitArray
							},
						}
					},
					SimilarTables = new List<TableSchema>()
					{
						new TableSchema()
						{
							TableName = "AllTypes"
						}
					}
				};

				// test parameters directions
				yield return new ProcedureSchema()
				{
					CatalogName = "SET_BY_TEST",
					SchemaName = "public",
					ProcedureName = "TestFunctionParameters",
					MemberName = "TestFunctionParameters",
					IsFunction = true,
					IsTableFunction = false,
					IsAggregateFunction = false,
					IsDefaultSchema = true,
					IsLoaded = false,
					Parameters = new List<ParameterSchema>()
					{
						new ParameterSchema()
						{
							SchemaName    = "param1",
							SchemaType    = "integer",
							IsIn          = true,
							ParameterName = "param1",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						},
						new ParameterSchema()
						{
							SchemaName    = "param2",
							SchemaType    = "integer",
							IsIn          = true,
							IsOut         = true,
							ParameterName = "param2",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						},
						new ParameterSchema()
						{
							SchemaName    = "param3",
							SchemaType    = "integer",
							IsOut         = true,
							ParameterName = "param3",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						}
					}
				};

				// table function with single column result
				yield return new ProcedureSchema()
				{
					CatalogName = "SET_BY_TEST",
					SchemaName = "public",
					ProcedureName = "TestTableFunction",
					MemberName = "TestTableFunction",
					IsFunction = true,
					IsTableFunction = true,
					IsAggregateFunction = false,
					IsDefaultSchema = true,
					IsLoaded = true,
					Parameters = new List<ParameterSchema>()
					{
						new ParameterSchema()
						{
							SchemaName    = "param1",
							SchemaType    = "integer",
							IsIn          = true,
							ParameterName = "param1",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						},
						new ParameterSchema()
						{
							SchemaName    = "param2",
							SchemaType    = "integer",
							IsOut         = true,
							ParameterName = "param2",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						}
					},
					ResultTable = new TableSchema()
					{
						IsProcedureResult = true,
						TypeName = "TestTableFunctionResult",
						Columns = new List<ColumnSchema>()
						{
							new ColumnSchema()
							{
								ColumnName = "param2",
								ColumnType = "int4",
								MemberName = "param2",
								MemberType = "int?",
								SystemType = typeof(int),
								IsNullable = true,
								DataType   = DataType.Int32
							}
						}
					}
				};

				// table function with multiple columns result
				yield return new ProcedureSchema()
				{
					CatalogName = "SET_BY_TEST",
					SchemaName = "public",
					ProcedureName = "TestTableFunction1",
					MemberName = "TestTableFunction1",
					IsFunction = true,
					IsTableFunction = true,
					IsAggregateFunction = false,
					IsDefaultSchema = true,
					IsLoaded = true,
					Parameters = new List<ParameterSchema>()
					{
						new ParameterSchema()
						{
							SchemaName    = "param1",
							SchemaType    = "integer",
							IsIn          = true,
							ParameterName = "param1",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						},
						new ParameterSchema()
						{
							SchemaName    = "param2",
							SchemaType    = "integer",
							IsIn          = true,
							ParameterName = "param2",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						},
						new ParameterSchema()
						{
							SchemaName    = "param3",
							SchemaType    = "integer",
							IsOut         = true,
							ParameterName = "param3",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						},
						new ParameterSchema()
						{
							SchemaName    = "param4",
							SchemaType    = "integer",
							IsOut         = true,
							ParameterName = "param4",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						}
					},
					ResultTable = new TableSchema()
					{
						IsProcedureResult = true,
						TypeName = "TestTableFunction1Result",
						Columns = new List<ColumnSchema>()
						{
							new ColumnSchema()
							{
								ColumnName = "param3",
								ColumnType = "int4",
								MemberName = "param3",
								MemberType = "int?",
								SystemType = typeof(int),
								IsNullable = true,
								DataType   = DataType.Int32
							},
							new ColumnSchema()
							{
								ColumnName = "param4",
								ColumnType = "int4",
								MemberName = "param4",
								MemberType = "int?",
								SystemType = typeof(int),
								IsNullable = true,
								DataType   = DataType.Int32
							}
						}
					}
				};

				// scalar function
				yield return new ProcedureSchema()
				{
					CatalogName = "SET_BY_TEST",
					SchemaName = "public",
					ProcedureName = "TestScalarFunction",
					MemberName = "TestScalarFunction",
					IsFunction = true,
					IsTableFunction = false,
					IsAggregateFunction = false,
					IsDefaultSchema = true,
					IsLoaded = false,
					Parameters = new List<ParameterSchema>()
					{
						new ParameterSchema()
						{
							SchemaType    = "character varying",
							IsResult      = true,
							ParameterName = "__skip", // name is dynamic as it is generated by schema loader
							ParameterType = "string",
							SystemType    = typeof(string),
							DataType      = DataType.NVarChar
						},
						new ParameterSchema()
						{
							SchemaName    = "param",
							SchemaType    = "integer",
							IsIn          = true,
							ParameterName = "param",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						}
					}
				};

				// scalar function
				yield return new ProcedureSchema()
				{
					CatalogName         = "SET_BY_TEST",
					SchemaName          = "public",
					ProcedureName       = "TestSingleOutParameterFunction",
					MemberName          = "TestSingleOutParameterFunction",
					IsFunction          = true,
					IsTableFunction     = false,
					IsAggregateFunction = false,
					IsDefaultSchema     = true,
					IsLoaded            = false,
					Parameters          = new List<ParameterSchema>()
					{
						new ParameterSchema()
						{
							SchemaName    = "param1",
							SchemaType    = "integer",
							IsIn          = true,
							ParameterName = "param1",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						},
						new ParameterSchema()
						{
							SchemaName    = "param2",
							SchemaType    = "integer",
							IsOut         = true,
							ParameterName = "param2",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						}
					}
				};

				// custom aggregate
				yield return new ProcedureSchema()
				{
					CatalogName = "SET_BY_TEST",
					SchemaName = "public",
					ProcedureName = "test_avg",
					MemberName = "test_avg",
					IsFunction = true,
					IsTableFunction = false,
					IsAggregateFunction = true,
					IsDefaultSchema = true,
					IsLoaded = false,
					Parameters = new List<ParameterSchema>()
					{
						new ParameterSchema()
						{
							SchemaType    = "double precision",
							IsResult      = true,
							ParameterName = "__skip",
							ParameterType = "double?",
							SystemType    = typeof(double),
							DataType      = DataType.Double
						},
						new ParameterSchema()
						{
							SchemaType    = "double precision",
							IsIn          = true,
							ParameterName = "__skip",
							ParameterType = "double?",
							SystemType    = typeof(double),
							DataType      = DataType.Double
						}
					}
				};
			}
		}

#if !NETSTANDARD1_6
		public void SchemaProceduresLoadedTest(
			[IncludeDataSources(false, ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			{
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, TestUtils.GetDefaultSchemaOptions(context));

				foreach (var proc in schema.Procedures)
				{
					Assert.IsNull(proc.ResultException);
				}
			}
		}

		[Test]
		public void ProceduresSchemaProviderTest(
			[IncludeDataSources(false, ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)] string context,
			[ValueSource(nameof(ProcedureTestCases))] ProcedureSchema expectedProc)
		{
			var macaddr8Supported =  context.Contains(TestProvName.PostgreSQL10) || context.Contains(TestProvName.PostgreSQL11) || context.Contains(TestProvName.PostgreSQLLatest);
			var jsonbSupported    = !context.Contains(ProviderName.PostgreSQL92) && !context.Contains(ProviderName.PostgreSQL93);
			using (var db = (DataConnection)GetDataContext(context))
			{
				expectedProc.CatalogName = TestUtils.GetDatabaseName(db);

				// schema load takes too long if system schema included
				// added SchemaProceduresLoadedTest to test system schema
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(
					db,
					TestUtils.GetDefaultSchemaOptions(context, new GetSchemaOptions() { ExcludedSchemas = new[] { "pg_catalog" } }));

				var procedures = schema.Procedures.Where(_ => _.ProcedureName == expectedProc.ProcedureName).ToList();

				Assert.AreEqual(1, procedures.Count);

				var procedure = procedures[0];

				Assert.AreEqual(expectedProc.CatalogName, procedure.CatalogName);
				Assert.AreEqual(expectedProc.SchemaName, procedure.SchemaName);
				Assert.AreEqual(expectedProc.MemberName, procedure.MemberName);
				Assert.AreEqual(expectedProc.IsTableFunction, procedure.IsTableFunction);
				Assert.AreEqual(expectedProc.IsAggregateFunction, procedure.IsAggregateFunction);
				Assert.AreEqual(expectedProc.IsDefaultSchema, procedure.IsDefaultSchema);
				Assert.AreEqual(expectedProc.IsFunction, procedure.IsFunction);
				Assert.AreEqual(expectedProc.IsLoaded, procedure.IsLoaded);
				Assert.AreEqual(expectedProc.IsResultDynamic, procedure.IsResultDynamic);

				Assert.IsNull(procedure.ResultException);

				Assert.AreEqual(expectedProc.Parameters.Count, procedure.Parameters.Count);

				for (var i = 0; i < procedure.Parameters.Count; i++)
				{
					var actualParam = procedure.Parameters[i];
					var expectedParam = expectedProc.Parameters[i];

					Assert.IsNotNull(expectedParam);

					Assert.AreEqual(expectedParam.SchemaName, actualParam.SchemaName);
					if (expectedParam.ParameterName != "__skip")
						Assert.AreEqual(expectedParam.ParameterName, actualParam.ParameterName);
					Assert.AreEqual(expectedParam.SchemaType, actualParam.SchemaType);
					Assert.AreEqual(expectedParam.IsIn, actualParam.IsIn);
					Assert.AreEqual(expectedParam.IsOut, actualParam.IsOut);
					Assert.AreEqual(expectedParam.IsResult, actualParam.IsResult);
					Assert.AreEqual(expectedParam.Size, actualParam.Size);
					Assert.AreEqual(expectedParam.ParameterType, actualParam.ParameterType);
					Assert.AreEqual(expectedParam.SystemType, actualParam.SystemType);
					Assert.AreEqual(expectedParam.DataType, actualParam.DataType);
					Assert.AreEqual(expectedParam.ProviderSpecificType, actualParam.ProviderSpecificType);
				}

				if (expectedProc.ResultTable == null)
				{
					Assert.IsNull(procedure.ResultTable);

					// maybe it is worth changing
					Assert.IsNull(procedure.SimilarTables);
				}
				else
				{
					Assert.IsNotNull(procedure.ResultTable);

					var expectedTable = expectedProc.ResultTable;
					var actualTable = procedure.ResultTable;

					Assert.AreEqual(expectedTable.ID, actualTable.ID);
					Assert.AreEqual(expectedTable.CatalogName, actualTable.CatalogName);
					Assert.AreEqual(expectedTable.SchemaName, actualTable.SchemaName);
					Assert.AreEqual(expectedTable.TableName, actualTable.TableName);
					Assert.AreEqual(expectedTable.Description, actualTable.Description);
					Assert.AreEqual(expectedTable.IsDefaultSchema, actualTable.IsDefaultSchema);
					Assert.AreEqual(expectedTable.IsView, actualTable.IsView);
					Assert.AreEqual(expectedTable.IsProcedureResult, actualTable.IsProcedureResult);
					Assert.AreEqual(expectedTable.TypeName, actualTable.TypeName);
					Assert.AreEqual(expectedTable.IsProviderSpecific, actualTable.IsProviderSpecific);

					Assert.IsNotNull(actualTable.ForeignKeys);
					Assert.IsEmpty(actualTable.ForeignKeys);

					var expectedColumns = expectedTable.Columns;
					if (!jsonbSupported)
						expectedColumns = expectedColumns.Where(_ => _.ColumnType != "jsonb").ToList();
					if (!macaddr8Supported)
						expectedColumns = expectedColumns.Where(_ => _.ColumnType != "macaddr8").ToList();

					Assert.AreEqual(expectedColumns.Count, actualTable.Columns.Count);

					foreach (var actualColumn in actualTable.Columns)
					{
						var expectedColumn = expectedColumns
							.Where(_ => _.ColumnName == actualColumn.ColumnName)
							.SingleOrDefault();

						Assert.IsNotNull(expectedColumn);

						// npgsql4 uses more standard names like 'integer' instead of 'int4'
						// and we don't have proper type synonyms support/normalization in 2.x
						if (expectedColumn.ColumnType == "int4")
							Assert.Contains(actualColumn.ColumnType, new[] { "int4", "integer" });
						else if (expectedColumn.ColumnType == "int8")
							Assert.Contains(actualColumn.ColumnType, new[] { "int8", "bigint" });
						else if (expectedColumn.ColumnType == "int2")
							Assert.Contains(actualColumn.ColumnType, new[] { "int2", "smallint" });
						else if (expectedColumn.ColumnType == "float8")
							Assert.Contains(actualColumn.ColumnType, new[] { "float8", "double precision" });
						else if (expectedColumn.ColumnType == "float4")
							Assert.Contains(actualColumn.ColumnType, new[] { "float4", "real" });
						else if (expectedColumn.ColumnType == "character(1)")
							Assert.Contains(actualColumn.ColumnType, new[] { "character(1)", "character" });
						else if (expectedColumn.ColumnType == "bit(-1)")
							Assert.Contains(actualColumn.ColumnType, new[] { "bit(-1)", "bit(3)" });
						else if (expectedColumn.ColumnType == "bool")
							Assert.Contains(actualColumn.ColumnType, new[] { "bool", "boolean" });
						else
							Assert.AreEqual(expectedColumn.ColumnType, actualColumn.ColumnType);

						Assert.AreEqual(expectedColumn.IsNullable, actualColumn.IsNullable);
						Assert.AreEqual(expectedColumn.IsIdentity, actualColumn.IsIdentity);
						Assert.AreEqual(expectedColumn.IsPrimaryKey, actualColumn.IsPrimaryKey);
						Assert.AreEqual(expectedColumn.PrimaryKeyOrder, actualColumn.PrimaryKeyOrder);
						Assert.AreEqual(expectedColumn.Description, actualColumn.Description);
						Assert.AreEqual(expectedColumn.MemberName, actualColumn.MemberName);
						Assert.AreEqual(expectedColumn.MemberType, actualColumn.MemberType);
						Assert.AreEqual(expectedColumn.ProviderSpecificType, actualColumn.ProviderSpecificType);
						Assert.AreEqual(expectedColumn.SystemType, actualColumn.SystemType);
						Assert.AreEqual(expectedColumn.DataType, actualColumn.DataType);
						Assert.AreEqual(expectedColumn.SkipOnInsert, actualColumn.SkipOnInsert);
						Assert.AreEqual(expectedColumn.SkipOnUpdate, actualColumn.SkipOnUpdate);
						Assert.AreEqual(expectedColumn.Length, actualColumn.Length);
						Assert.AreEqual(expectedColumn.Precision, actualColumn.Precision);
						Assert.AreEqual(expectedColumn.Scale, actualColumn.Scale);
						Assert.AreEqual(actualTable, actualColumn.Table);
					}

					Assert.IsNotNull(procedure.SimilarTables);

					foreach (var table in procedure.SimilarTables)
					{
						var tbl = expectedProc.SimilarTables
							.Where(_ => _.TableName == table.TableName)
							.SingleOrDefault();

						Assert.IsNotNull(tbl);
					}
				}
			}
		}

		[Test]
		public void TestVoidFunction([IncludeDataSources(false, ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.Select(() => TestPgFunctions.AddIfNotExists("test"));

				// actually void function returns void, which is not null, but in C# void is not a 'real' type
				// https://stackoverflow.com/questions/11318973/void-in-c-sharp-generics
				Assert.AreEqual(DBNull.Value, result);
			}
		}

		[Test]
		public void TestCustomAggregate([IncludeDataSources(false, ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.GetTable<AllTypes>()
					.GroupBy(_ => _.bitDataType)
					.Select(g => new
					{
						avg       = g.Average(_ => _.doubleDataType),
						customAvg = g.CustomAvg(_ => _.doubleDataType)
					}).ToList();

				Assert.AreNotEqual(0, result.Count);

				foreach (var res in result)
				{
					Assert.AreEqual(res.avg, res.customAvg);
				}
			}
		}

		[Test]
		public void TestCustomAggregateNotNullable([IncludeDataSources(false, ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.GetTable<AllTypes>()
					.GroupBy(_ => _.bitDataType)
					.Select(g => new
					{
						avg       = g.Average(_ => _.doubleDataType ?? 0d),
						customAvg = g.CustomAvg(_ => _.doubleDataType ?? 0d)
					}).ToList();

				Assert.AreNotEqual(0, result.Count);

				foreach (var res in result)
				{
					Assert.AreEqual(res.avg, res.customAvg);
				}
			}
		}

		[Test]
		public void TestTableFunction([IncludeDataSources(false, ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)] string context)
		{
			using (var db = GetDataContext(context, new MappingSchema(context)))
			{
				var result = new TestPgFunctions(db).GetAllTypes().ToList();

				var res1 = db.GetTable<AllTypes>().OrderBy(_ => _.ID).ToArray()[1];
				var res2 = result                 .OrderBy(_ => _.ID).ToArray()[1];

				var c1 = res1.binaryDataType.GetHashCode() == res2.binaryDataType.GetHashCode();
				var c2 = res1.bitDataType   .GetHashCode() == res2.bitDataType   .GetHashCode();
				var c3 = res1.varBitDataType.GetHashCode() == res2.varBitDataType.GetHashCode();

				var e1 = res1.binaryDataType.Equals(res2.binaryDataType);
				var e2 = res1.bitDataType   .Equals(res2.bitDataType);
				var e3 = res1.varBitDataType.Equals(res2.varBitDataType);

				AreEqual(db.GetTable<AllTypes>().OrderBy(_ => _.ID), result.OrderBy(_ => _.ID), AllTypes.Comparer);
			}
		}

		[Test]
		public void TestParametersFunction([IncludeDataSources(false, ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.SetConvertExpression<object[], TestPgFunctions.TestParametersResult>(
					tuple => new TestPgFunctions.TestParametersResult() { param2 = (int?)tuple[0], param3 = (int?)tuple[1] });

				var result = db.Select(() => TestPgFunctions.TestParameters(1, 2));

				Assert.IsNotNull(result);
				Assert.AreEqual(1, result.param2);
				Assert.AreEqual(2, result.param3);
			}
		}

		[Test]
		public void TestScalarTableFunction([IncludeDataSources(false, ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = new TestPgFunctions(db).TestScalarTableFunction(4).ToList();

				Assert.IsNotNull(result);
				Assert.AreEqual(2, result.Count);
				Assert.AreEqual(4, result[0].param2);
				Assert.AreEqual(4, result[1].param2);

			}
		}

		[Test]
		public void TestRecordTableFunction([IncludeDataSources(false, ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = new TestPgFunctions(db).TestRecordTableFunction(1, 2).ToList();

				Assert.IsNotNull(result);
				Assert.AreEqual(2  , result.Count);
				Assert.AreEqual(1  , result[0].param3);
				Assert.AreEqual(23 , result[0].param4);
				Assert.AreEqual(333, result[1].param3);
				Assert.AreEqual(2  , result[1].param4);
			}
		}

		[Test]
		public void TestScalarFunction([IncludeDataSources(false, ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.Select(() => TestPgFunctions.TestScalarFunction(123));

				Assert.AreEqual("done", result);
			}
		}

		[Test]
		public void TestSingleOutParameterFunction([IncludeDataSources(false, ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.Select(() => TestPgFunctions.TestSingleOutParameterFunction(1));

				Assert.AreEqual(124, result);
			}
		}

		[ActiveIssue("Functionality not implemented yet")]
		[Test]
		public void TestDynamicRecordFunction([IncludeDataSources(false, ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result1 = db.Select(() => TestPgFunctions.DynamicRecordFunction<TestPgFunctions.TestRecordTableFunctionResult>("{param3:1, param4: 2}"));
				var result2 = db.Select(() => TestPgFunctions.DynamicRecordFunction<TestPgFunctions.TestRecordTableFunctionResult>("{param4:4}"));

				Assert.IsNotNull(result1);
				Assert.AreEqual(1, result1.param3);
				Assert.AreEqual(2, result1.param4);

				Assert.IsNotNull(result2);
				Assert.IsNull(result2.param3);
				Assert.AreEqual(4, result2.param4);
			}
		}

		[ActiveIssue("Functionality not implemented yet")]
		[Test]
		public void TestDynamicTableFunction([IncludeDataSources(false, ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93, ProviderName.PostgreSQL95, TestProvName.PostgreSQL10, TestProvName.PostgreSQL11, TestProvName.PostgreSQLLatest)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = new TestPgFunctions(db).DynamicTableFunction<TestPgFunctions.TestRecordTableFunctionResult>("[{param3:1, param4: 2},{param4: 3}]").ToList();

				Assert.IsNotNull(result);
				Assert.AreEqual(2, result.Count);
				Assert.AreEqual(1, result[0].param3);
				Assert.AreEqual(2, result[0].param4);
				Assert.IsNull(result[1].param3);
				Assert.AreEqual(4, result[1].param4);
			}
		}
	}

	public static class TestPgAggregates
	{
		[Sql.Function("test_avg", ServerSideOnly = true, IsAggregate = true, ArgIndices = new[] { 0 })]
		public static double CustomAvg<TSource>(this IEnumerable<TSource> src, Expression<Func<TSource, double>> value)
		{
			throw new InvalidOperationException();
		}

		[Sql.Function("test_avg", ServerSideOnly = true, IsAggregate = true, ArgIndices = new[] { 0 })]
		public static double? CustomAvg<TSource>(this IEnumerable<TSource> src, Expression<Func<TSource, double?>> value)
		{
			throw new InvalidOperationException();
		}
	}

	public class TestPgFunctions
	{
		private readonly IDataContext _ctx;

		public TestPgFunctions(IDataContext ctx)
		{
			_ctx = ctx;
		}

		[Sql.Function("add_if_not_exists", ServerSideOnly = true)]
		public static object AddIfNotExists(string value)
		{
			// this function has void return type in pg, but we use object in C# to make it usable
			throw new InvalidOperationException();
		}

		// TODO: function names should be escaped by linq2db, but it is not implemented yet
		[Sql.TableFunction("\"TestTableFunctionSchema\"")]
		public LinqToDB.ITable<PostgreSQLTests.AllTypes> GetAllTypes()
		{
			var methodInfo = typeof(TestPgFunctions).GetMethod("GetAllTypes", new Type[0]);

			return _ctx.GetTable<PostgreSQLTests.AllTypes>(this, methodInfo);
		}

		[Sql.Function("\"TestFunctionParameters\"", ServerSideOnly = true)]
		public static TestParametersResult TestParameters(int? param1, int? param2)
		{
			throw new InvalidOperationException();
		}

		[Sql.TableFunction("\"TestTableFunction\"")]
		public LinqToDB.ITable<TestScalarTableFunctionResult> TestScalarTableFunction(int? param1)
		{
			var methodInfo = typeof(TestPgFunctions).GetMethod("TestScalarTableFunction", new[] { typeof(int?) });

			return _ctx.GetTable<TestScalarTableFunctionResult>(this, methodInfo, param1);
		}

		[Sql.TableFunction("\"TestTableFunction1\"")]
		public LinqToDB.ITable<TestRecordTableFunctionResult> TestRecordTableFunction(int? param1, int? param2)
		{
			var methodInfo = typeof(TestPgFunctions).GetMethod("TestRecordTableFunction", new[] { typeof(int?), typeof(int?) });

			return _ctx.GetTable<TestRecordTableFunctionResult>(this, methodInfo, param1, param2);
		}

		[Sql.Function("\"TestScalarFunction\"", ServerSideOnly = true)]
		public static string TestScalarFunction(int? param)
		{
			throw new InvalidOperationException();
		}

		[Sql.Function("\"TestSingleOutParameterFunction\"", ServerSideOnly = true)]
		public static int? TestSingleOutParameterFunction(int? param)
		{
			throw new InvalidOperationException();
		}

		[Sql.Function("jsonb_to_record", ServerSideOnly = true)]
		public static TRecord DynamicRecordFunction<TRecord>(string json)
		{
			throw new InvalidOperationException();
		}

		[Sql.TableFunction("jsonb_to_recordset")]
		public LinqToDB.ITable<TRecord> DynamicTableFunction<TRecord>(string json)
			where TRecord : class
		{
			var methodInfo = typeof(TestPgFunctions).GetMethod("DynamicTableFunction", new [] { typeof(string) });

			return _ctx.GetTable<TRecord>(this, methodInfo, json);
		}

		public class TestScalarTableFunctionResult
		{
			public int? param2 { get; set; }
		}

		public class TestRecordTableFunctionResult
		{
			public int? param3 { get; set; }

			public int? param4 { get; set; }
		}

		public class TestParametersResult
		{
			public int? param2 { get; set; }

			public int? param3 { get; set; }
		}
#endif
	}
}
