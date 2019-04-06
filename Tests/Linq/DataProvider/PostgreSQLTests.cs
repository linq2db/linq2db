﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq.Expressions;
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
using LinqToDB.Tools.Comparers;

using NpgsqlTypes;

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tests.DataProvider
{
	using Model;

	[TestFixture]
	public class PostgreSQLTests : DataProviderTestBase
	{
		private static readonly string _nextValSearchPattern = "nextval";

		public PostgreSQLTests()
		{
			PassNullSql  = "SELECT \"ID\" FROM \"AllTypes\" WHERE :p IS NULL AND \"{0}\" IS NULL OR :p IS NOT NULL AND \"{0}\" = :p";
			PassValueSql = "SELECT \"ID\" FROM \"AllTypes\" WHERE \"{0}\" = :p";
			GetNullSql   = "SELECT \"{0}\" FROM \"{1}\" WHERE \"ID\" = 1";
			GetValueSql  = "SELECT \"{0}\" FROM \"{1}\" WHERE \"ID\" = 2";
		}

		[Test]
		public void TestParameters([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		//[Test]
		public void TestNumerics([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void TestDate([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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
		[Test]
		public void TestJson([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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
		[Test]
		public void TestJsonb([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
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

		[Test]
		public void TestDateTime([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void TestChar([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void TestString([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void TestBinary([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void TestGuid([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void TestXml([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void TestEnum1([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<TestEnum>("SELECT 'A'"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'A'"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum>("SELECT 'B'"), Is.EqualTo(TestEnum.BB));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'B'"), Is.EqualTo(TestEnum.BB));
			}
		}

		[Test]
		public void TestEnum2([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void SequenceInsert1([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void SequenceInsert2([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void SequenceInsert3([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void SequenceInsertWithIdentity_CustomNaming([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.GetTable<PostgreSQLSpecific.SequenceCustomNamingTest>().Where(_ => _.Value == "SeqValue").Delete();

				var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.SequenceCustomNamingTest { Value = "SeqValue" }));
				var id2 = db.GetTable<PostgreSQLSpecific.SequenceCustomNamingTest>().Single(_ => _.Value == "SeqValue").ID;

				Assert.AreEqual(id1, id2);

				db.GetTable<PostgreSQLSpecific.SequenceCustomNamingTest>().Where(_ => _.ID == id1).Delete();

				Assert.AreEqual(0, db.GetTable<PostgreSQLSpecific.SequenceCustomNamingTest>().Count(_ => _.Value == "SeqValue"));
			}
		}

		[Test]
		public void SequenceInsertWithUserDefinedSequenceNameAttribute([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var table = new LinqToDB.SqlQuery.SqlTable(db.MappingSchema, typeof(PostgreSQLSpecific.SequenceTest1));
				Assert.That(table.SequenceAttributes, Is.Not.Null);
				Assert.That(table.SequenceAttributes.Length, Is.EqualTo(1));

				db.Insert(new PostgreSQLSpecific.SequenceTest1 { Value = "SeqValue" });

				Assert.That(db.LastQuery, Does.Contain(_nextValSearchPattern));
			}
		}
		[Test]
		public void SequenceInsertWithoutSequenceNameAttribute([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var table = new LinqToDB.SqlQuery.SqlTable(db.MappingSchema, typeof(PostgreSQLSpecific.SequenceTest2));
				Assert.That(table.SequenceAttributes.IsNullOrEmpty());

				db.Insert(new PostgreSQLSpecific.SequenceTest2 { Value = "SeqValue" });

				Assert.That(db.LastQuery, Does.Not.Contains(_nextValSearchPattern));
			}
		}

		[Test]
		public void SequenceInsertWithIdentity1([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void SequenceInsertWithIdentity2([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void SequenceInsertWithIdentity3([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void SequenceInsertWithIdentity4([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void SequenceInsertWithIdentity5([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void BulkCopyLinqTypes([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void Issue140([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void CreateTableTest([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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
		[Test]
		public void NpgsqlDateTimeTest([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

			public static IEqualityComparer<AllTypes> Comparer = ComparerBuilder.GetEqualityComparer<AllTypes>();
		}

		public enum BulkTestMode
		{
			WithoutTransaction,
			WithTransaction,
			WithRollback
		}

		// test that native bulk copy method inserts data properly
		[Test]
		public void BulkCopyTest([Values]BulkTestMode mode, [IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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
					// npgsql4 uses 2/1/1 instead of 1/1/1 as date part
					timeTZDataType      = new DateTimeOffset(1, 1, !context.Contains(TestProvName.PostgreSQLLatest) ? 1 : 2, 10, 11, 12, 13, TimeSpan.FromMinutes(30)),
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
						r =>
						{
							r.ID = 0;
							r.binaryDataType = null;
							r.bitDataType = null;
							r.varBitDataType = null;
							if (!lineSupported)     r.lineDataType     = null;
							if (!jsonbSupported)    r.jsonbDataType    = null;
							if (!macaddr8Supported) r.macaddr8DataType = null;
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

#if !NETSTANDARD1_6
		[Test]
		public void TestVoidFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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
		public void TestCustomAggregate([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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
		public void TestCustomAggregateNotNullable([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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
		public void TestTableFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataContext(context, new MappingSchema(context)))
			{
				var result = new TestPgFunctions(db).GetAllTypes().ToList();

				var res1 = db.GetTable<AllTypes>().OrderBy(_ => _.ID).ToArray()[1];
				var res2 = result.OrderBy(_ => _.ID).ToArray()[1];

				var c1 = res1.binaryDataType.GetHashCode() == res2.binaryDataType.GetHashCode();
				var c2 = res1.bitDataType.GetHashCode() == res2.bitDataType.GetHashCode();
				var c3 = res1.varBitDataType.GetHashCode() == res2.varBitDataType.GetHashCode();

				var e1 = res1.binaryDataType.Equals(res2.binaryDataType);
				var e2 = res1.bitDataType.Equals(res2.bitDataType);
				var e3 = res1.varBitDataType.Equals(res2.varBitDataType);

				AreEqual(db.GetTable<AllTypes>().OrderBy(_ => _.ID), result.OrderBy(_ => _.ID), AllTypes.Comparer);
			}
		}

		[Test]
		public void TestParametersFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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
		public void TestScalarTableFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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
		public void TestRecordTableFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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
		public void TestScalarFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.Select(() => TestPgFunctions.TestScalarFunction(123));

				Assert.AreEqual("done", result);
			}
		}

		[Test]
		public void TestSingleOutParameterFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.Select(() => TestPgFunctions.TestSingleOutParameterFunction(1));

				Assert.AreEqual(124, result);
			}
		}

		[ActiveIssue("Functionality not implemented yet")]
		[Test]
		public void TestDynamicRecordFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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
		public void TestDynamicTableFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		public class SomeRange<T> where T: struct
		{
			public SomeRange(T? start, T? end)
			{
				Start = start;
				End = end;
			}

			public T? Start { get; set; }
			public T? End { get; set; }
		}

		public class TableWithDateRanges
		{
			[Column(DbType = "tsrange")]
			public SomeRange<DateTime> SimpleRange { get; set; }

			[Column(DbType = "tstzrange")]
			public SomeRange<DateTime> RangeWithTimeZone { get; set; }
		}

		private static MappingSchema CreateRangesMapping()
		{
			NpgsqlRange<DateTime> ConvertToNpgSqlRange(SomeRange<DateTime> r)
			{
				var range = NpgsqlRange<DateTime>.Empty;
					range = new NpgsqlRange<DateTime>(
						r.Start ?? default, true,  r.Start == null,
						r.End   ?? default, false, r.End == null);

				return range;
			}

			var mapping = new MappingSchema();
			mapping.SetConverter<NpgsqlRange<DateTime>, SomeRange<DateTime>>(r =>
				{
					if (r.IsEmpty)
						return default;

					return new SomeRange<DateTime>(
						r.LowerBoundInfinite
							? (DateTime?) null
							: r.LowerBound,
						r.UpperBoundInfinite
							? (DateTime?) null
							: r.UpperBound
					);
				}
			);

			mapping.SetConverter<SomeRange<DateTime>, DataParameter>(r =>
			{
				var range = ConvertToNpgSqlRange(r);
				return new DataParameter("", range, "tstzrange");
			}, new DbDataType(typeof(SomeRange<DateTime>)), new DbDataType(typeof(DataParameter), "tstzrange"));

			mapping.SetConverter<SomeRange<DateTime>, DataParameter>(r =>
			{
				var range = ConvertToNpgSqlRange(r);
				return new DataParameter("", range, "tsrange");
			}, new DbDataType(typeof(SomeRange<DateTime>)), new DbDataType(typeof(DataParameter), "tsrange"));

			return mapping;
		}

		[Test]
		public void TestCustomType([IncludeDataSources(TestProvName.AllPostgreSQLv3)] string context)
		{
			using (var db = GetDataContext(context, CreateRangesMapping()))
			using (var table = db.CreateLocalTable<TableWithDateRanges>())
			{
				var date = DateTime.UtcNow;
				var range1 = new SomeRange<DateTime>(date, null);
				var range2 = new SomeRange<DateTime>(date.AddDays(1), null);
				db.Insert(new TableWithDateRanges
				{
					SimpleRange       = range1,
					RangeWithTimeZone = range2,
				});
			}
		}

		[Test]
		public void TestCustomTypeBulkCopy([IncludeDataSources(TestProvName.AllPostgreSQLv3)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context, CreateRangesMapping()))
			using (var table = db.CreateLocalTable<TableWithDateRanges>())
			{
				var date = DateTime.UtcNow;

				var items = Enumerable.Range(1, 100).Select(i =>
				{
					var range1 = new SomeRange<DateTime>(date.AddDays(i), date.AddDays(i + 1));
					var range2 = new SomeRange<DateTime>(date.AddDays(i), date.AddDays(i + 1));
					return new TableWithDateRanges
					{
						SimpleRange = range1,
						RangeWithTimeZone = range2,
					};
				});

				db.BulkCopy(items);

				var loadedItems = table.ToArray();
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
