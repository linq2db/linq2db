using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using LinqToDB.SqlQuery;
using LinqToDB.Tools;
using LinqToDB.Tools.Comparers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Npgsql;

using NpgsqlTypes;

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

using Shouldly;

using Tests.Model;

using FormattableStringFactory = System.Runtime.CompilerServices.FormattableStringFactory;
using JsonSerializer = System.Text.Json.JsonSerializer;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace Tests.DataProvider
{
	[TestFixture]
	public class PostgreSQLTests : DataProviderTestBase
	{
		private const string _nextValSearchPattern = "nextval";

		protected override string? PassNullSql(DataConnection dc, out int paramCount)
		{
			paramCount = 1;
			return "SELECT \"ID\" FROM \"AllTypes\" WHERE :p IS NULL AND \"{0}\" IS NULL OR :p IS NOT NULL AND \"{0}\" = :p";
		}
		protected override string  GetNullSql  (DataConnection dc) => "SELECT \"{0}\" FROM {1} WHERE \"ID\" = 1";
		protected override string  PassValueSql(DataConnection dc) => "SELECT \"ID\" FROM \"AllTypes\" WHERE \"{0}\" = :p";
		protected override string  GetValueSql (DataConnection dc) => "SELECT \"{0}\" FROM {1} WHERE \"ID\" = 2";

		[Test]
		public void TestParameters([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var conn = GetDataContext(context, suppressSequentialAccess: true);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<string>("SELECT :p", new { p = "1" }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>("SELECT :p", new { p = new DataParameter { Value = 1 } }), Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT :p1", new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>("SELECT :p1 + :p2", new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
				Assert.That(conn.Execute<int>("SELECT :p2 + :p1", new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
				Assert.That(conn.Execute<string>("SELECT :p", new { p = 1 }), Is.EqualTo("1"));
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
			public TypeTestData(string name, Func<string, PostgreSQLTests, DataConnection, object?> func, object result)
			{
				Name   = name;
				Func   = func;
				Result = result;
			}

			public TypeTestData(string name, int id, Func<string, PostgreSQLTests, DataConnection, object?> func, object result)
			{
				Name   = name;
				ID     = id;
				Func   = func;
				Result = result;
			}

			public string Name                                                 { get; set; }
			public int    ID                                                   { get; set; }
			public Func<string, PostgreSQLTests, DataConnection, object?> Func { get; set; }
			public object Result                                               { get; set; }
		}

		[AttributeUsage(AttributeTargets.Method)]
		sealed class TestDataTypeAttribute : NUnitAttribute, ITestBuilder, IImplyFixture
		{
			public TestDataTypeAttribute(string providerName)
			{
				_providerName = providerName;
			}

			readonly string _providerName;

			public IEnumerable<TestMethod> BuildFrom(IMethodInfo method, Test? suite)
			{
				var tests = TestConfiguration.UserProviders.Contains(_providerName) ?
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

						new TypeTestData("timeDataType",        (n,t,c) => t.TestTypeEx<TimeSpan?>         (c, n),                       new TimeSpan(12, 12, 12)),
//						new TypeTestData("intervalDataType",    (n,t,c) => t.TestTypeEx<TimeSpan?>         (c, n),                       new TimeSpan(1, 3, 5, 20)),
						new TypeTestData("bitDataType",         (n,t,c) => t.TestTypeEx<BitArray>          (c, n),                       new BitArray(new[] { true, false, true })),
						new TypeTestData("varBitDataType",      (n,t,c) => t.TestTypeEx<BitArray>          (c, n),                       new BitArray(new[] { true, false, true, true })),
						new TypeTestData("macaddrDataType",     (n,t,c) => t.TestTypeEx<PhysicalAddress>   (c, n, skipDefaultNull:true), new PhysicalAddress(new byte[] { 1, 2, 3, 4, 5, 6 })),

						new TypeTestData("timestampDataType",   (n,t,c) => t.TestTypeEx<DateTime?>         (c, n, DataType.DateTime2),      new DateTime(2012, 12, 12, 12, 12, 12)),
						new TypeTestData("timestampTZDataType", (n,t,c) => t.TestTypeEx<DateTimeOffset?>   (c, n, DataType.DateTimeOffset), new DateTimeOffset(2012, 12, 12, 11, 12, 12, new TimeSpan(-5, 0, 0))),
						new TypeTestData("dateDataType",        (n,t,c) => t.TestTypeEx<DateTime?>         (c, n, DataType.Date),           new DateTime(2012, 12, 12)),

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
						new TypeTestData("pathDataType",        (n,t,c) => t.TestTypeEx<NpgsqlPath?>       (c, n, skipDefaultNull:true),            new NpgsqlPath   (new NpgsqlPoint(1, 2), new NpgsqlPoint(3, 4))),
						new TypeTestData("polygonDataType",     (n,t,c) => t.TestTypeEx<NpgsqlPolygon?>    (c, n, skipNull:true, skipNotNull:true), new NpgsqlPolygon(new NpgsqlPoint(1, 2), new NpgsqlPoint(3, 4))),
						new TypeTestData("circleDataType",      (n,t,c) => t.TestTypeEx<NpgsqlCircle?>     (c, n, skipDefaultNull:true),            new NpgsqlCircle (new NpgsqlPoint(1, 2), 3)),

#pragma warning disable CS0618 // NpgsqlInet obsolete
						new TypeTestData("inetDataType",        (n,t,c) => t.TestTypeEx<NpgsqlInet?>       (c, n, skipDefaultNull:true),            new NpgsqlInet(new IPAddress(new byte[] { 192, 168, 1, 1 }))),
#pragma warning restore CS0618
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

					if (!TestConfiguration.UserProviders.Contains(_providerName))
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
			using var conn = GetDataConnection(context);
			var value = data.Func(typeName, this, conn);
			if (data.Result is NpgsqlPoint)
			{
				Assert.That(value, Is.EqualTo(data.Result));
			}
			else
			{
				Assert.That(data.Result, Is.EqualTo(value));
			}
		}

		static void TestNumeric<T>(IDataContext conn, T expectedValue, DataType dataType, string skip = "")
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
				var sqlValue = (object?)expectedValue;

				var sql = string.Format("SELECT Cast({0} as {1})", sqlValue ?? "NULL", sqlType);

				Assert.That(conn.Execute<T>(sql), Is.EqualTo(expectedValue));
			}

			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<T>("SELECT :p", new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
				Assert.That(conn.Execute<T>("SELECT :p", new DataParameter { Name = "p", Value = expectedValue }), Is.EqualTo(expectedValue));
				Assert.That(conn.Execute<T>("SELECT :p", new { p = expectedValue }), Is.EqualTo(expectedValue));
			}
		}

		static void TestSimple<T>(DataConnection conn, T expectedValue, DataType dataType)
			where T : struct
		{
			TestNumeric(conn, expectedValue, dataType);
			TestNumeric<T?>(conn, expectedValue, dataType);
			TestNumeric(conn, (T?)null, dataType);
		}

		//[Test]
		//public void TestNumerics([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		//{
		//	using (var conn = GetDataContext(context))
		//	{
		//		TestSimple<short> (conn, 1,   DataType.Int16);
		//		TestSimple        (conn, 1,   DataType.Int32);
		//		TestSimple        (conn, 1L,  DataType.Int64);
		//		TestSimple<byte>  (conn, 1,   DataType.Byte);
		//		TestSimple<ushort>(conn, 1,   DataType.UInt16);
		//		TestSimple        (conn, 1u,  DataType.UInt32);
		//		TestSimple        (conn, 1ul, DataType.UInt64);
		//		TestSimple<float> (conn, 1,   DataType.Single);
		//		TestSimple        (conn, 1d,  DataType.Double);
		//		TestSimple        (conn, 1m,  DataType.Decimal);
		//		TestSimple        (conn, 1m,  DataType.VarNumeric);
		//		TestSimple        (conn, 1m,  DataType.Money);
		//		TestSimple        (conn, 1m,  DataType.SmallMoney);
		//		TestSimple<sbyte> (conn, 1,   DataType.SByte);

		//		TestNumeric(conn, sbyte.MinValue, DataType.SByte, "money");
		//		TestNumeric(conn, sbyte.MaxValue, DataType.SByte);
		//		TestNumeric(conn, short.MinValue, DataType.Int16, "money");
		//		TestNumeric(conn, short.MaxValue, DataType.Int16);
		//		TestNumeric(conn, int.MinValue,   DataType.Int32, "money smallint");
		//		TestNumeric(conn, int.MaxValue,   DataType.Int32, "smallint real");
		//		TestNumeric(conn, long.MinValue,  DataType.Int64, "int money smallint");
		//		TestNumeric(conn, long.MaxValue,  DataType.Int64, "int money smallint float real");

		//		TestNumeric(conn, byte.MaxValue,   DataType.Byte);
		//		TestNumeric(conn, ushort.MaxValue, DataType.UInt16, "int smallint");
		//		TestNumeric(conn, uint.MaxValue,   DataType.UInt32, "int smallint real");
		//		TestNumeric(conn, ulong.MaxValue,  DataType.UInt64, "bigint int money smallint float real");

		//		TestNumeric(conn, -3.40282306E+38f,  DataType.Single, "bigint int money smallint numeric numeric(38)");
		//		TestNumeric(conn, 3.40282306E+38f,   DataType.Single, "bigint int money numeric numeric(38) smallint");
		//		TestNumeric(conn, -1.79E+308d,       DataType.Double, "bigint int money numeric numeric(38) smallint real");
		//		TestNumeric(conn, 1.79E+308d,        DataType.Double, "bigint int money numeric numeric(38) smallint real");
		//		TestNumeric(conn, decimal.MinValue,  DataType.Decimal, "bigint int money numeric numeric(38) smallint float real");
		//		TestNumeric(conn, decimal.MaxValue,  DataType.Decimal, "bigint int money numeric numeric(38) smallint float real");
		//		TestNumeric(conn, decimal.MinValue,  DataType.VarNumeric, "bigint int money numeric numeric(38) smallint float real");
		//		TestNumeric(conn, decimal.MaxValue,  DataType.VarNumeric, "bigint int money numeric numeric(38) smallint float real");
		//		TestNumeric(conn, -922337203685477m, DataType.Money, "int money smallint real");
		//		TestNumeric(conn, +922337203685477m, DataType.Money, "int smallint real");
		//		TestNumeric(conn, -214748m,          DataType.SmallMoney, "money smallint smallint");
		//		TestNumeric(conn, +214748m,          DataType.SmallMoney, "smallint");
		//	}
		//}

		[Test]
		public void TestDate([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var conn = GetDataContext(context);
			var dateTime = new DateTime(2012, 12, 12);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<DateTime>("SELECT Cast('2012-12-12' as date)"), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12' as date)"), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime>("SELECT :p", DataParameter.Date("p", dateTime)), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT :p", new DataParameter("p", dateTime, DataType.Date)), Is.EqualTo(dateTime));
			}
		}

		[Table]
		sealed class TimeColumnTable
		{
			[PrimaryKey]                 public int      Id   { get; set; }
			[Column(DbType = "time")]    public TimeSpan Time { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6077")]
		public void ReadTimeAsTimeSpanScalar([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			db
				.Execute<TimeSpan>("SELECT Cast('12:12:12' as time)")
				.ShouldBe(new TimeSpan(12, 12, 12));

			db
				.Execute<TimeSpan?>("SELECT Cast('12:12:12' as time)")
				.ShouldBe(new TimeSpan(12, 12, 12));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6077")]
		public void ReadTimeAsTimeSpanEntity([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable([new TimeColumnTable { Id = 1, Time = new TimeSpan(12, 12, 12) }]);

			table
				.Single()
				.Time
				.ShouldBe(new TimeSpan(12, 12, 12));
		}

#if NET8_0_OR_GREATER
		// net462 tests run Npgsql 8, which returns DateTime for date, so the string carries a time part there.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6077")]
		public void ReadDateAsString([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			db
				.Execute<string>("SELECT Cast('2012-12-12' as date)")
				.ShouldBe("2012-12-12");
		}
#endif

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6077")]
		public void ReadTimeAsDateTime([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			db
				.Execute<DateTime>("SELECT Cast('12:12:12' as time)")
				.ShouldBe(new DateTime(1, 1, 1, 12, 12, 12));

			db
				.Execute<DateTime?>("SELECT Cast('12:12:12' as time)")
				.ShouldBe(new DateTime(1, 1, 1, 12, 12, 12));
		}

		[Table]
		sealed class SkipLockedTable
		{
			[Column] public int Value { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6137")]
		public void ForUpdateSkipLockedHintByVersion([Values] PostgreSQLVersion version)
		{
			if (version == PostgreSQLVersion.AutoDetect)
				return;

			using var db = new DataConnection(new DataOptions()
				.UseConnectionString(PostgreSQLTools.GetDataProvider(version), "Server=localhost"));

			var sql = db.GetTable<SkipLockedTable>()
				.AsPostgreSQL()
				.ForUpdateSkipLockedHint()
				.ToSqlQuery()
				.Sql;

			if (version is PostgreSQLVersion.v92 or PostgreSQLVersion.v93)
			{
				sql.ShouldContain(PostgreSQLHints.ForUpdate);
				sql.ShouldNotContain(PostgreSQLHints.SkipLocked);
			}
			else
			{
				sql.ShouldContain($"{PostgreSQLHints.ForUpdate} {PostgreSQLHints.SkipLocked}");
			}
		}

		/// <summary>
		/// Ensure we can pass data as Json parameter type and get
		/// same value back out equivalent in value
		/// </summary>
		[Test]
		public void TestJson([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var conn = GetDataContext(context);
			var testJson = /*lang=json,strict*/ "{\"name\":\"bob\", \"age\":10}";

			Assert.That(conn.Execute<string>("SELECT :p", new DataParameter("p", testJson, DataType.Json)), Is.EqualTo(testJson));
		}

		/// <summary>
		/// Ensure we can pass data as binary json and have things handled
		/// with values coming back as being equivalent in value
		/// </summary>
		[Test]
		public void TestJsonb([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			var json = new { name = "bob", age = 10 };
			using var conn = GetDataContext(context);
			//properties come back out in potentially diff order as its being
			//converted between a binary json format and the string representation
			var raw = conn.Execute<string>("SELECT :p", new DataParameter("p", JsonConvert.SerializeObject(json), DataType.BinaryJson));
			var obj = JObject.Parse(raw);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(obj.Value<string>("name"), Is.EqualTo(json.name));
				Assert.That(obj.Value<int>("age"), Is.EqualTo(json.age));
			}
		}

		[Test]
		public void TestDateTime([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var conn = GetDataContext(context);
			var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);
			using (Assert.EnterMultipleScope())
			{
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
			using var conn = GetDataContext(context);
			using (Assert.EnterMultipleScope())
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

				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.Create("p", (string?)null)), Is.Null);
				Assert.That(conn.Execute<string>("SELECT :p", new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestBinary([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var arr1 = new byte[] { 48, 57 };

			using var conn = GetDataContext(context);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<byte[]>("SELECT E'\\060\\071'::bytea"), Is.EqualTo(arr1));

				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Binary("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create("p", arr1)), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", null)), Is.Null);
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", Array.Empty<byte>())), Is.EqualTo(Array.Empty<byte>()));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Image("p", Array.Empty<byte>())), Is.EqualTo(Array.Empty<byte>()));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create("p", new Binary(arr1))), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter("p", new Binary(arr1))), Is.EqualTo(arr1));
			}
		}

		[Test]
		public void TestGuid([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var conn = GetDataContext(context);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(
								conn.Execute<Guid>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uuid)"),
								Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				Assert.That(
					conn.Execute<Guid?>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uuid)"),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));
			}

			var guid = TestData.Guid1;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<Guid>("SELECT :p", DataParameter.Create("p", guid)), Is.EqualTo(guid));
				Assert.That(conn.Execute<Guid>("SELECT :p", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test]
		public void TestXml([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var conn = GetDataContext(context);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(conn.Execute<string>("SELECT XMLPARSE (DOCUMENT'<xml/>')"), Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>("SELECT XMLPARSE (DOCUMENT'<xml/>')").ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT XMLPARSE (DOCUMENT'<xml/>')").InnerXml, Is.EqualTo("<xml />"));
			}

			var xdoc = XDocument.Parse("<xml/>");
			var xml = Convert<string, XmlDocument>.Lambda("<xml/>");
			using (Assert.EnterMultipleScope())
			{
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

		// works with v9 too, but requires npgsql < 6
		[Test]
		public void TestEnum1([IncludeDataSources(TestProvName.AllPostgreSQL10Plus)] string context)
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
		public void TestEnum2([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		[Test]
		public void SequenceInsert1([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			ResetTestSequence(context);
			db.GetTable<PostgreSQLSpecific.SequenceTest1>().Where(_ => _.Value == "SeqValue").Delete();
			db.Insert(new PostgreSQLSpecific.SequenceTest1 { Value = "SeqValue" });

			var id = db.GetTable<PostgreSQLSpecific.SequenceTest1>().Single(_ => _.Value == "SeqValue").ID;

			db.GetTable<PostgreSQLSpecific.SequenceTest1>().Where(_ => _.ID == id).Delete();

			Assert.That(db.GetTable<PostgreSQLSpecific.SequenceTest1>().Count(_ => _.Value == "SeqValue"), Is.Zero);
		}

		[Test]
		public void SequenceInsert2([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<PostgreSQLSpecific.SequenceTest2>().Where(_ => _.Value == "SeqValue").Delete();
			db.Insert(new PostgreSQLSpecific.SequenceTest2 { Value = "SeqValue" });

			var id = db.GetTable<PostgreSQLSpecific.SequenceTest2>().Single(_ => _.Value == "SeqValue").ID;

			db.GetTable<PostgreSQLSpecific.SequenceTest2>().Where(_ => _.ID == id).Delete();

			Assert.That(db.GetTable<PostgreSQLSpecific.SequenceTest2>().Count(_ => _.Value == "SeqValue"), Is.Zero);
		}

		[Test]
		public void SequenceInsert3([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			ResetTestSequence(context);
			db.GetTable<PostgreSQLSpecific.SequenceTest3>().Where(_ => _.Value == "SeqValue").Delete();
			db.Insert(new PostgreSQLSpecific.SequenceTest3 { Value = "SeqValue" });

			var id = db.GetTable<PostgreSQLSpecific.SequenceTest3>().Single(_ => _.Value == "SeqValue").ID;

			db.GetTable<PostgreSQLSpecific.SequenceTest3>().Where(_ => _.ID == id).Delete();

			Assert.That(db.GetTable<PostgreSQLSpecific.SequenceTest3>().Count(_ => _.Value == "SeqValue"), Is.Zero);
		}

		[Test]
		public void SequenceInsertWithIdentity_CustomNaming([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<PostgreSQLSpecific.SequenceCustomNamingTest>().Where(_ => _.Value == "SeqValue").Delete();

			var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.SequenceCustomNamingTest { Value = "SeqValue" }));
			var id2 = db.GetTable<PostgreSQLSpecific.SequenceCustomNamingTest>().Single(_ => _.Value == "SeqValue").ID;

			Assert.That(id2, Is.EqualTo(id1));

			db.GetTable<PostgreSQLSpecific.SequenceCustomNamingTest>().Where(_ => _.ID == id1).Delete();

			Assert.That(db.GetTable<PostgreSQLSpecific.SequenceCustomNamingTest>().Count(_ => _.Value == "SeqValue"), Is.Zero);
		}

		private static SqlTable CreateSqlTable<T>(IDataContext dataContext)
		{
			return new SqlTable(dataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataContext.Options.ConnectionOptions.OnEntityDescriptorCreated));
		}

		[Test]
		public void SequenceInsertWithUserDefinedSequenceNameAttribute([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataConnection(context);
			var table = CreateSqlTable<PostgreSQLSpecific.SequenceTest1>(db);
			Assert.That(table.SequenceAttributes, Is.Not.Null);
			Assert.That(table.SequenceAttributes!, Has.Length.EqualTo(1));

			db.Insert(new PostgreSQLSpecific.SequenceTest1 { Value = "SeqValue" });

			Assert.That(db.LastQuery, Does.Contain(_nextValSearchPattern));
		}
		[Test]
		public void SequenceInsertWithoutSequenceNameAttribute([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataConnection(context);
			var table = CreateSqlTable<PostgreSQLSpecific.SequenceTest2>(db);
			Assert.That(table.SequenceAttributes.IsNullOrEmpty());

			db.Insert(new PostgreSQLSpecific.SequenceTest2 { Value = "SeqValue" });

			Assert.That(db.LastQuery, Does.Not.Contains(_nextValSearchPattern));
		}

		[Test]
		public void SequenceInsertWithIdentity1([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			ResetTestSequence(context);
			db.GetTable<PostgreSQLSpecific.SequenceTest1>().Where(_ => _.Value == "SeqValue").Delete();

			var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.SequenceTest1 { Value = "SeqValue" }));
			var id2 = db.GetTable<PostgreSQLSpecific.SequenceTest1>().Single(_ => _.Value == "SeqValue").ID;

			Assert.That(id2, Is.EqualTo(id1));

			db.GetTable<PostgreSQLSpecific.SequenceTest1>().Where(_ => _.ID == id1).Delete();

			Assert.That(db.GetTable<PostgreSQLSpecific.SequenceTest1>().Count(_ => _.Value == "SeqValue"), Is.Zero);
		}

		[Test]
		public void SequenceInsertWithIdentity2([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<PostgreSQLSpecific.SequenceTest2>().Where(_ => _.Value == "SeqValue").Delete();

			var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.SequenceTest2 { Value = "SeqValue" }));
			var id2 = db.GetTable<PostgreSQLSpecific.SequenceTest2>().Single(_ => _.Value == "SeqValue").ID;

			Assert.That(id2, Is.EqualTo(id1));

			db.GetTable<PostgreSQLSpecific.SequenceTest2>().Where(_ => _.ID == id1).Delete();

			Assert.That(db.GetTable<PostgreSQLSpecific.SequenceTest2>().Count(_ => _.Value == "SeqValue"), Is.Zero);
		}

		[Test]
		public void SequenceInsertWithIdentity3([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			ResetTestSequence(context);
			db.GetTable<PostgreSQLSpecific.SequenceTest3>().Where(_ => _.Value == "SeqValue").Delete();

			var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.SequenceTest3 { Value = "SeqValue" }));
			var id2 = db.GetTable<PostgreSQLSpecific.SequenceTest3>().Single(_ => _.Value == "SeqValue").ID;

			Assert.That(id2, Is.EqualTo(id1));

			db.GetTable<PostgreSQLSpecific.SequenceTest3>().Where(_ => _.ID == id1).Delete();

			Assert.That(db.GetTable<PostgreSQLSpecific.SequenceTest3>().Count(_ => _.Value == "SeqValue"), Is.Zero);
		}

		[Test]
		public void SequenceInsertWithIdentity4([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<PostgreSQLSpecific.TestSchemaIdentity>().Delete();

			var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.TestSchemaIdentity()));
			var id2 = db.GetTable<PostgreSQLSpecific.TestSchemaIdentity>().Single().ID;

			Assert.That(id2, Is.EqualTo(id1));

			db.GetTable<PostgreSQLSpecific.TestSchemaIdentity>().Delete();
		}

		[Test]
		public void SequenceInsertWithIdentity5([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<PostgreSQLSpecific.TestSerialIdentity>().Delete();

			var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.TestSerialIdentity()));
			var id2 = db.GetTable<PostgreSQLSpecific.TestSerialIdentity>().Single().ID;

			Assert.That(id2, Is.EqualTo(id1));

			db.GetTable<PostgreSQLSpecific.TestSerialIdentity>().Delete();
		}

		[Test]
		public void BulkCopyLinqTypes([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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
		public async Task BulkCopyLinqTypesAsync([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
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

		public class TestTeamplate
		{
			public string? cdni_cd_cod_numero_item1;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/140")]
		public void QueryIntColumnIntoStringField([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			var list = db.Query<TestTeamplate>("select 1 as cdni_cd_cod_numero_item1").ToList();

			Assert.That(list, Has.Count.EqualTo(1));
			Assert.That(list[0].cdni_cd_cod_numero_item1, Is.EqualTo("1"));
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
					Guid       = TestData.Guid1,
					TimeOffset = new DateTimeOffset(2017, 06, 17, 16, 40, 33, 0, TimeSpan.FromHours(-3))
				};
				db.Insert(e);

				var e2 = db.GetTable<CreateTableTestClass>()
					.FirstOrDefault(_ => _.Guid == e.Guid)!;

				Assert.That(e2, Is.Not.Null);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(e2.Guid, Is.EqualTo(e.Guid));
					Assert.That(e2.TimeOffset, Is.EqualTo(e.TimeOffset));
				}
			}
		}

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
			[Column]                                   public DateTime?       timestampDataType         { get; set; }
			[Column]                                   public DateTimeOffset? timestampTZDataType       { get; set; }
#if SUPPORTS_DATEONLY
			[Column]                                   public DateOnly?       dateDataType              { get; set; }
#else
			[Column(DataType = DataType.Date)]         public DateTime?       dateDataType              { get; set; }
#endif
			[Column(DbType = "time")]                  public TimeSpan?       timeDataType              { get; set; }
			[Column  (DbType = "time with time zone")] public DateTimeOffset? timeTZDataType            { get; set; }
			[Column]                                   public NpgsqlInterval? intervalDataType          { get; set; }
			[Column(DataType = DataType.Interval)]     public TimeSpan?       intervalDataType2         { get; set; }
			// text
			[Column]                                   public char?   charDataType                      { get; set; }
			[Column]                                   public string? char20DataType                    { get; set; }
			[Column]                                   public string? varcharDataType                   { get; set; }
			[Column]                                   public string? textDataType                      { get; set; }
			// misc
			[Column]                                   public byte[]?   binaryDataType                  { get; set; }
			[Column]                                   public Guid?     uuidDataType                    { get; set; }
			[Column]                                   public BitArray? bitDataType                     { get; set; }
			[Column]                                   public bool?     booleanDataType                 { get; set; }
			[Column]                                   public string?   colorDataType                   { get; set; }
			[Column]                                   public string?   xmlDataType                     { get; set; }
			[Column]                                   public BitArray? varBitDataType                  { get; set; }
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
			[Column]                                   public IPAddress?       inetDataType             { get; set; }
#if NET8_0_OR_GREATER
			[Column]                                   public IPNetwork?       cidrDataType             { get; set; }
#else
			[Column]                                   public NpgsqlCidr?      cidrDataType             { get; set; }
#endif
			[Column  (DbType = "macaddr")]             public PhysicalAddress? macaddrDataType          { get; set; }
			// PGSQL10+
			// also supported by ProviderName.PostgreSQL, but it is hard to setup...
			[NotColumn]
			[Column(DbType = "macaddr8", Configuration = TestProvName.PostgreSQL10)]
			[Column(DbType = "macaddr8", Configuration = TestProvName.PostgreSQL11)]
			[Column(DbType = "macaddr8", Configuration = TestProvName.PostgreSQL12)]
			[Column(DbType = "macaddr8", Configuration = ProviderName.PostgreSQL13)]
			[Column(DbType = "macaddr8", Configuration = TestProvName.PostgreSQL14)]
			[Column(DbType = "macaddr8", Configuration = ProviderName.PostgreSQL15)]
			[Column(DbType = "macaddr8", Configuration = TestProvName.PostgreSQL16)]
			[Column(DbType = "macaddr8", Configuration = TestProvName.PostgreSQL17)]
			[Column(DbType = "macaddr8", Configuration = ProviderName.PostgreSQL18)]
			[Column(DbType = "macaddr8", Configuration = ProviderName.PostgreSQL19)]
			                                           public PhysicalAddress? macaddr8DataType         { get; set; }
			// json
			[Column]                                   public string? jsonDataType                      { get; set; }
			[NotColumn(Configuration = ProviderName.PostgreSQL92)]
			[NotColumn(Configuration = ProviderName.PostgreSQL93)]
			[Column  (DataType = DataType.BinaryJson)] public string? jsonbDataType                     { get; set; }

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
		public void BulkCopyTest([Values] BulkTestMode mode, [IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var macaddr8Supported = context.IsAnyOf(TestProvName.AllPostgreSQL10Plus);
			var lineSupported     = context.IsAnyOf(TestProvName.AllPostgreSQL95Plus);
			var jsonbSupported    = context.IsAnyOf(TestProvName.AllPostgreSQL95Plus);
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

					timestampDataType   = new DateTime(2010, 5, 30, 1, 2, 3, 4),
					timestampTZDataType = new DateTimeOffset(2011, 3, 22, 10, 11, 12, 13, TimeSpan.FromMinutes(30)),
					dateDataType        = new (2010, 5, 30),
					timeDataType        = new TimeSpan(0, 1, 2, 3, 4),
					// npgsql4 uses 2/1/1 instead of 1/1/1 as date part in npgsql3
					timeTZDataType      = new DateTimeOffset(1, 1, 2, 10, 11, 12, 13, TimeSpan.FromMinutes(30)),
					intervalDataType    = new NpgsqlInterval(1, 2, 3),
					intervalDataType2   = TimeSpan.FromTicks(-123456780),

					charDataType        = 'ы',
					char20DataType      = "тест1",
					varcharDataType     = "тест2",
					textDataType        = "текст",

					binaryDataType      = new byte[] { 1, 2, 3 },
					uuidDataType        = TestData.Guid1,
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
#if NET8_0_OR_GREATER
					cidrDataType        = IPNetwork.Parse("::ffff:1.2.3.0/120"),
#else
					cidrDataType        = new NpgsqlCidr("::ffff:1.2.3.0/120"),
#endif
					macaddrDataType     = PhysicalAddress.Parse("08-00-2B-01-02-03"),
					macaddr8DataType    = PhysicalAddress.Parse("08-00-2B-FF-FE-01-02-03"),

					jsonDataType        = /*lang=json,strict*/ "{\"test\": 1}",
					jsonbDataType       = /*lang=json,strict*/ "{\"test\": 2}"
				}
			};

			using var db = GetDataConnection(context);
			db.AddMappingSchema(new MappingSchema(context));
			// color enum type will not work without this call if _create test was run in the same session
			// More details here: https://github.com/npgsql/npgsql/issues/1357
			// must be called before transaction opened due to: https://github.com/npgsql/npgsql/issues/2244
			((dynamic)db.OpenDbConnection()).ReloadTypes();

			DataConnectionTransaction? ts = null;

			if (mode != BulkTestMode.WithoutTransaction)
				ts = db.BeginTransaction();

			int[]? ids = null;
			try
			{
				var result = db.BulkCopy(new BulkCopyOptions() { BulkCopyType = BulkCopyType.ProviderSpecific }, testData);

				Assert.That(result.RowsCopied, Is.EqualTo(testData.Length));

				var data = db.GetTable<AllTypes>().OrderByDescending(_ => _.ID).Take(2).AsEnumerable().Reverse().ToArray();

				ids = data.Select(_ => _.ID).ToArray();

				// comparer generator miss collections support
				if (testData.Length == data.Length)
					for (var i = 0; i < testData.Length; i++)
					{
						var expectedBinary = testData[i].binaryDataType;
						var actualBinary = data[i].binaryDataType;

						if (expectedBinary != null && actualBinary != null)
							Assert.That(expectedBinary.SequenceEqual(actualBinary), Is.True);
						else if (expectedBinary != null || actualBinary != null)
							Assert.Fail();

						var expectedBit = testData[i].bitDataType;
						var actualBit = data[i].bitDataType;

						if (expectedBit != null && actualBit != null)
							Assert.That(expectedBit.Cast<bool>().SequenceEqual(actualBit.Cast<bool>()), Is.True);
						else if (expectedBit != null || actualBit != null)
							Assert.Fail();

						expectedBit = testData[i].varBitDataType;
						actualBit = data[i].varBitDataType;

						if (expectedBit != null && actualBit != null)
							Assert.That(expectedBit.Cast<bool>().SequenceEqual(actualBit.Cast<bool>()), Is.True);
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
						if (!lineSupported) r.lineDataType = null;
						if (!jsonbSupported) r.jsonbDataType = null;
						if (!macaddr8Supported) r.macaddr8DataType = null;
						return r;
					},
					testData,
					data,
					AllTypes.Comparer);

				if (mode != BulkTestMode.WithoutTransaction)
					ts!.Rollback();
			}
			finally
			{
				if (mode == BulkTestMode.WithoutTransaction)
					db.GetTable<AllTypes>().Where(_ => ids!.Contains(_.ID)).Delete();
			}

			if (mode == BulkTestMode.WithRollback)
				Assert.That(db.GetTable<AllTypes>().Where(_ => ids.Contains(_.ID)).Count(), Is.Zero);
		}

		[Test]
		public async Task BulkCopyTestAsync([Values]BulkTestMode mode, [IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var macaddr8Supported = context.IsAnyOf(TestProvName.AllPostgreSQL10Plus);
			var lineSupported     = context.IsAnyOf(TestProvName.AllPostgreSQL95Plus);
			var jsonbSupported    = context.IsAnyOf(TestProvName.AllPostgreSQL95Plus);
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

					timestampDataType   = new DateTime(2010, 5, 30, 1, 2, 3, 4),
					timestampTZDataType = new DateTimeOffset(2011, 3, 22, 10, 11, 12, 13, TimeSpan.FromMinutes(30)),
					dateDataType        = new (2010, 5, 30),
					timeDataType        = new TimeSpan(0, 1, 2, 3, 4),
					// npgsql4 uses 2/1/1 instead of 1/1/1 as date part in npgsql3
					timeTZDataType      = new DateTimeOffset(1, 1, 2, 10, 11, 12, 13, TimeSpan.FromMinutes(30)),
					intervalDataType    = new NpgsqlInterval(-1, 2, 3),
					intervalDataType2   = TimeSpan.FromTicks(-123456780),

					charDataType        = 'ы',
					char20DataType      = "тест1",
					varcharDataType     = "тест2",
					textDataType        = "текст",

					binaryDataType      = new byte[] { 1, 2, 3 },
					uuidDataType        = TestData.Guid2,
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
#if NET8_0_OR_GREATER
					cidrDataType        = IPNetwork.Parse("::ffff:1.2.3.0/120"),
#else
					cidrDataType        = new NpgsqlCidr("::ffff:1.2.3.0/120"),
#endif
					macaddrDataType     = PhysicalAddress.Parse("08-00-2B-01-02-03"),
					macaddr8DataType    = PhysicalAddress.Parse("08-00-2B-FF-FE-01-02-03"),

					jsonDataType        = /*lang=json,strict*/ "{\"test\": 1}",
					jsonbDataType       = /*lang=json,strict*/ "{\"test\": 2}"
				}
			};

			using var db = GetDataConnection(context);
			db.AddMappingSchema(new MappingSchema(context));
			// color enum type will not work without this call if _create test was run in the same session
			// More details here: https://github.com/npgsql/npgsql/issues/1357
			// must be called before transaction opened due to: https://github.com/npgsql/npgsql/issues/2244
			((dynamic)db.OpenDbConnection()).ReloadTypes();

			DataConnectionTransaction? ts = null;

			if (mode != BulkTestMode.WithoutTransaction)
				ts = db.BeginTransaction();

			int[]? ids = null;
			try
			{
				var result = await db.BulkCopyAsync(new BulkCopyOptions() { BulkCopyType = BulkCopyType.ProviderSpecific }, testData);

				Assert.That(result.RowsCopied, Is.EqualTo(testData.Length));

				var data = db.GetTable<AllTypes>().OrderByDescending(_ => _.ID).Take(2).AsEnumerable().Reverse().ToArray();

				ids = data.Select(_ => _.ID).ToArray();

				// comparer generator miss collections support
				if (testData.Length == data.Length)
					for (var i = 0; i < testData.Length; i++)
					{
						var expectedBinary = testData[i].binaryDataType;
						var actualBinary = data[i].binaryDataType;

						if (expectedBinary != null && actualBinary != null)
							Assert.That(expectedBinary.SequenceEqual(actualBinary), Is.True);
						else if (expectedBinary != null || actualBinary != null)
							Assert.Fail();

						var expectedBit = testData[i].bitDataType;
						var actualBit = data[i].bitDataType;

						if (expectedBit != null && actualBit != null)
							Assert.That(expectedBit.Cast<bool>().SequenceEqual(actualBit.Cast<bool>()), Is.True);
						else if (expectedBit != null || actualBit != null)
							Assert.Fail();

						expectedBit = testData[i].varBitDataType;
						actualBit = data[i].varBitDataType;

						if (expectedBit != null && actualBit != null)
							Assert.That(expectedBit.Cast<bool>().SequenceEqual(actualBit.Cast<bool>()), Is.True);
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
						if (!lineSupported) r.lineDataType = null;
						if (!jsonbSupported) r.jsonbDataType = null;
						if (!macaddr8Supported) r.macaddr8DataType = null;
						return r;
					},
					testData,
					data,
					AllTypes.Comparer);

				if (mode != BulkTestMode.WithoutTransaction)
					ts!.Rollback();
			}
			finally
			{
				if (mode == BulkTestMode.WithoutTransaction)
					db.GetTable<AllTypes>().Where(_ => ids!.Contains(_.ID)).Delete();
			}

			if (mode == BulkTestMode.WithRollback)
				Assert.That(db.GetTable<AllTypes>().Where(_ => ids!.Contains(_.ID)).Count(), Is.Zero);
		}

		[Table("SequenceTest1")]
		public class SequenceTest
		{
			[Column, SequenceName("sequencetestseq")]
			public int    ID;
			[Column]
			public string Value = null!;
		}

		// Does committed (non-transactional) writes to SequenceTest1 and the sequence sequencetestseq, and
		// asserts exact sequence values 1..40 - so nothing else may touch them while it runs. Both are
		// scoped to one database, and every test against a database runs on that database's lane, so no
		// concurrent test can reach them: this needs no NonParallelizable. Different PostgreSQL versions are
		// different databases.
		[Test]
		public void BulkCopyRetrieveSequences(
			[IncludeDataSources(TestProvName.AllPostgreSQL)] string context,
			[Values] BulkCopyType bulkCopyType,
			[Values] bool useSequence)
		{
				var data = Enumerable.Range(1, 40).Select(i => new SequenceTest { Value = $"SeqValue{i}" }).ToArray();

			using var db = GetDataContext(context);
			try
			{
				db.GetTable<SequenceTest>().Where(_ => _.Value.StartsWith("SeqValue")).Delete();

				if (useSequence)
					ResetTestSequence(context);

				var options = new BulkCopyOptions()
				{
					KeepIdentity = bulkCopyType == BulkCopyType.RowByRow ? false : true,
					MaxBatchSize = 10,
					BulkCopyType = bulkCopyType
				};

				db.BulkCopy(options, data.RetrieveIdentity(db, useSequence));

				var cnt = 1;
				foreach (var d in data)
				{
					Assert.That(d.ID, Is.EqualTo(cnt));
					cnt++;
				}
			}
			finally
			{
				db.GetTable<SequenceTest>().Where(_ => _.Value.StartsWith("SeqValue")).Delete();
			}
		}

		[Test]
		public void TestVoidFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => TestPgFunctions.AddIfNotExists("test"));

			// actually void function returns void, which is not null, but in C# void is not a 'real' type
			// https://stackoverflow.com/questions/11318973/void-in-c-sharp-generics
			Assert.That(result, Is.Null);
		}

		[Test]
		public void TestCustomAggregate([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.GetTable<AllTypes>()
					.GroupBy(_ => _.bitDataType)
					.Select(g => new
					{
						avg       = g.Average(_ => _.doubleDataType),
						customAvg = g.CustomAvg(_ => _.doubleDataType)
					}).ToList();

			Assert.That(result, Is.Not.Empty);

			foreach (var res in result)
			{
				Assert.That(res.customAvg, Is.EqualTo(res.avg));
			}
		}

		[Test]
		public void TestCustomAggregateNotNullable([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.GetTable<AllTypes>()
					.GroupBy(_ => _.bitDataType)
					.Select(g => new
					{
						avg       = g.Average(_ => _.doubleDataType ?? 0d),
						customAvg = g.CustomAvg(_ => _.doubleDataType ?? 0d)
					}).ToList();

			Assert.That(result, Is.Not.Empty);

			foreach (var res in result)
			{
				Assert.That(res.customAvg, Is.EqualTo(res.avg));
			}
		}

		[Test]
		public void TestTableFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			// needed for proper AllTypes columns mapping
			db.AddMappingSchema(new MappingSchema(context));

			var result = new TestPgFunctions(db).GetAllTypes().ToList();

			var res1 = db.GetTable<AllTypes>().OrderBy(_ => _.ID).ToArray()[1];
			var res2 = result.OrderBy(_ => _.ID).ToArray()[1];

			var c1 = res1.binaryDataType!.GetHashCode() == res2.binaryDataType!.GetHashCode();
			var c2 = res1.bitDataType!.GetHashCode()    == res2.bitDataType!.GetHashCode();
			var c3 = res1.varBitDataType!.GetHashCode() == res2.varBitDataType!.GetHashCode();

			var e1 = res1.binaryDataType.Equals(res2.binaryDataType);
			var e2 = res1.bitDataType   .Equals(res2.bitDataType);
			var e3 = res1.varBitDataType.Equals(res2.varBitDataType);

			AreEqual(db.GetTable<AllTypes>().OrderBy(_ => _.ID), result.OrderBy(_ => _.ID), AllTypes.Comparer);
		}

		[Test]
		public void TestParametersFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var ms = new MappingSchema();
			ms.SetConvertExpression<object[], TestPgFunctions.TestParametersResult>(
				tuple => new TestPgFunctions.TestParametersResult() { param2 = (int?)tuple[0], param3 = (int?)tuple[1] });

			using var db = GetDataContext(context, ms);
			var result = db.Select(() => TestPgFunctions.TestParameters(1, 2));

			Assert.That(result, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.param2, Is.EqualTo(1));
				Assert.That(result.param3, Is.EqualTo(2));
			}
		}

		[Test]
		public void TestScalarTableFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			var result = new TestPgFunctions(db).TestScalarTableFunction(4).ToList();

			Assert.That(result, Is.Not.Null);
			Assert.That(result, Has.Count.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].param2, Is.EqualTo(4));
				Assert.That(result[1].param2, Is.EqualTo(4));
			}
		}

		[Test]
		public void TestRecordTableFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			var result = new TestPgFunctions(db).TestRecordTableFunction(1, 2).ToList();

			Assert.That(result, Is.Not.Null);
			Assert.That(result, Has.Count.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].param3, Is.EqualTo(1));
				Assert.That(result[0].param4, Is.EqualTo(23));
				Assert.That(result[1].param3, Is.EqualTo(333));
				Assert.That(result[1].param4, Is.EqualTo(2));
			}
		}

		[Test]
		public void TestScalarFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => TestPgFunctions.TestScalarFunction(123));

			Assert.That(result, Is.EqualTo("done"));
		}

		[Test]
		public void TestSingleOutParameterFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => TestPgFunctions.TestSingleOutParameterFunction(1));

			Assert.That(result, Is.EqualTo(124));
		}

		// Fragment stops before the system type: that renders as an assembly-qualified name carrying the assembly
		// version, which would stop matching at the next version bump.
		[ActiveIssue(ErrorTypeName = "LinqToDB.LinqToDBException", ErrorMessage = "Database column type cannot be determined automatically and must be specified explicitly for system type",
			Details = "no-issue: functionality not implemented yet - a dynamic record function's column types cannot be inferred.")]
		[Test]
		public void TestDynamicRecordFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			var result1 = db.Select(() => TestPgFunctions.DynamicRecordFunction<TestPgFunctions.TestRecordTableFunctionResult>(/*lang=json*/ "{param3:1, param4: 2}"));
			var result2 = db.Select(() => TestPgFunctions.DynamicRecordFunction<TestPgFunctions.TestRecordTableFunctionResult>(/*lang=json*/ "{param4:4}"));

			Assert.That(result1, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result1.param3, Is.EqualTo(1));
				Assert.That(result1.param4, Is.EqualTo(2));

				Assert.That(result2, Is.Not.Null);
				Assert.That(result2.param3, Is.Null);
				Assert.That(result2.param4, Is.EqualTo(4));
			}
		}

		[ActiveIssue(ErrorTypeName = "System.ArgumentException", ErrorMessage = "is a generic method definition",
			Details = "no-issue: Functionality not implemented yet - the builder is handed the open generic method definition and never closes it. Nothing on the tracker covers it.")]
		[Test]
		public void TestDynamicTableFunction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			var result = new TestPgFunctions(db).DynamicTableFunction<TestPgFunctions.TestRecordTableFunctionResult>(/*lang=json*/ "[{param3:1, param4: 2},{param4: 3}]").ToList();

			Assert.That(result, Is.Not.Null);
			Assert.That(result, Has.Count.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].param3, Is.EqualTo(1));
				Assert.That(result[0].param4, Is.EqualTo(2));
				Assert.That(result[1].param3, Is.Null);
				Assert.That(result[1].param4, Is.EqualTo(4));
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
			public SomeRange<DateTime>? SimpleRange { get; set; }

			[Column(DbType = "tstzrange")]
			public SomeRange<DateTime>? RangeWithTimeZone { get; set; }
		}

		private static MappingSchema CreateRangesMapping()
		{
			NpgsqlRange<DateTime> ConvertToNpgSqlRange(SomeRange<DateTime> r, bool withTimeZone)
			{
				// specify proper kind for npgsql 6
				var range = NpgsqlRange<DateTime>.Empty;
					range = new NpgsqlRange<DateTime>(
						DateTime.SpecifyKind(r.Start ?? default, withTimeZone ? DateTimeKind.Utc : DateTimeKind.Unspecified), true,  r.Start == null,
						DateTime.SpecifyKind(r.End   ?? default, withTimeZone ? DateTimeKind.Utc : DateTimeKind.Unspecified), false, r.End == null);

				return range;
			}

			var mapping = new MappingSchema();
			mapping.SetConverter<NpgsqlRange<DateTime>, SomeRange<DateTime>?>(r =>
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
				var range = ConvertToNpgSqlRange(r, true);
				return new DataParameter("", range, "tstzrange");
			}, new DbDataType(typeof(SomeRange<DateTime>)), new DbDataType(typeof(DataParameter), "tstzrange"));

			mapping.SetConverter<SomeRange<DateTime>, DataParameter>(r =>
			{
				var range = ConvertToNpgSqlRange(r, false);
				return new DataParameter("", range, "tsrange");
			}, new DbDataType(typeof(SomeRange<DateTime>)), new DbDataType(typeof(DataParameter), "tsrange"));

			return mapping;
		}

		[Test]
		[NoBaselines("Output (datetime format) is culture-/system-dependent")]
		public void TestCustomType([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context, CreateRangesMapping());
			using var table = db.CreateLocalTable<TableWithDateRanges>();
			var date = TestData.DateTimeUtc;
			var range1 = new SomeRange<DateTime>(date, null);
			var range2 = new SomeRange<DateTime>(date.AddDays(1), null);
			db.Insert(new TableWithDateRanges
			{
				SimpleRange       = range1,
				RangeWithTimeZone = range2,
			});
		}

		[Test]
		[NoBaselines("Output (datetime format) is culture-/system-dependent")]
		public void TestCustomTypeBulkCopy([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = (DataConnection)GetDataContext(context, CreateRangesMapping());
			using var table = db.CreateLocalTable<TableWithDateRanges>();
			var date = TestData.DateTimeUtc;

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

		[Test]
		[NoBaselines("Output (datetime format) is culture-/system-dependent")]
		public async Task TestCustomTypeBulkCopyAsync([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = (DataConnection)GetDataContext(context, CreateRangesMapping());
			using var table = db.CreateLocalTable<TableWithDateRanges>();
			var date = TestData.DateTimeUtc;

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

			await db.BulkCopyAsync(items);

			var loadedItems = table.ToArray();
		}

		[Table]
		public class UIntTable
		{
			[Column] public ushort  Field16  { get; set; }
			[Column] public uint    Field32  { get; set; }
			[Column] public ulong   Field64  { get; set; }
			[Column] public ushort? Field16N { get; set; }
			[Column] public uint?   Field32N { get; set; }
			[Column] public ulong?  Field64N { get; set; }
		}

		[Test]
		public void UIntXXMappingTest([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = (DataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable<UIntTable>();
			// test create table
			Assert.That(db.LastQuery!, Does.Contain("\"Field16\"  Int"));
			Assert.That(db.LastQuery!, Does.Contain("\"Field32\"  BigInt"));
			Assert.That(db.LastQuery!, Does.Contain("\"Field64\"  decimal(20)"));
			Assert.That(db.LastQuery!, Does.Contain("\"Field16N\" Int"));
			Assert.That(db.LastQuery!, Does.Contain("\"Field32N\" BigInt"));
			Assert.That(db.LastQuery!, Does.Contain("\"Field64N\" decimal(20)"));

			var value16      = ushort.MaxValue;
			var value32      = uint.MaxValue;
			var value64      = ulong.MaxValue;
			ushort? value16N = ushort.MaxValue;
			uint? value32N   = uint.MaxValue;
			ulong? value64N  = ulong.MaxValue;

			// test literal (+materialization)
			db.InlineParameters = true;
			table.Insert(() => new UIntTable() { Field16 = value16, Field32 = value32, Field64 = value64, Field16N = value16N, Field32N = value32N, Field64N = value64N });
			Assert.That(db.LastQuery!, Does.Contain("\t65535,"));
			Assert.That(db.LastQuery!, Does.Contain("\t4294967295,"));
			Assert.That(db.LastQuery!, Does.Contain("18446744073709551615"));
			var res = table.ToArray();
			Assert.That(res, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Field16, Is.EqualTo(ushort.MaxValue));
				Assert.That(res[0].Field32, Is.EqualTo(uint.MaxValue));
				Assert.That(res[0].Field64, Is.EqualTo(ulong.MaxValue));
				Assert.That(res[0].Field16N, Is.EqualTo(ushort.MaxValue));
				Assert.That(res[0].Field32N, Is.EqualTo(uint.MaxValue));
				Assert.That(res[0].Field64N, Is.EqualTo(ulong.MaxValue));
			}

			table.Delete();

			// test parameter (+materialization)
			db.InlineParameters = false;
			table.Insert(() => new UIntTable() { Field16 = value16, Field32 = value32, Field64 = value64, Field16N = value16N, Field32N = value32N, Field64N = value64N });
			Assert.That(db.LastQuery!, Does.Not.Contain("65535"));
			Assert.That(db.LastQuery!, Does.Not.Contain("4294967295"));
			Assert.That(db.LastQuery!, Does.Not.Contain("18446744073709551615"));
			res = table.ToArray();
			Assert.That(res, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Field16, Is.EqualTo(ushort.MaxValue));
				Assert.That(res[0].Field32, Is.EqualTo(uint.MaxValue));
				Assert.That(res[0].Field64, Is.EqualTo(ulong.MaxValue));
				Assert.That(res[0].Field16N, Is.EqualTo(ushort.MaxValue));
				Assert.That(res[0].Field32N, Is.EqualTo(uint.MaxValue));
				Assert.That(res[0].Field64N, Is.EqualTo(ulong.MaxValue));
			}

			// test schema
			var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new LinqToDB.SchemaProvider.GetSchemaOptions()
			{
				GetProcedures = false,
				GetTables     = true,
				LoadTable     = t => t.Name == nameof(UIntTable)
			});

			Assert.That(schema.Tables, Has.Count.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(schema.Tables[0].TableName, Is.EqualTo(nameof(UIntTable)));
				Assert.That(schema.Tables[0].Columns, Has.Count.EqualTo(6));
			}

			var column = schema.Tables[0].Columns.Single(c => c.ColumnName == nameof(UIntTable.Field16));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(column.ColumnType, Is.EqualTo("integer"));
				Assert.That(column.DataType, Is.EqualTo(DataType.Int32));
				Assert.That(column.MemberType, Is.EqualTo("int"));
				Assert.That(column.SystemType, Is.EqualTo(typeof(int)));
			}

			column = schema.Tables[0].Columns.Single(c => c.ColumnName == nameof(UIntTable.Field32));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(column.ColumnType, Is.EqualTo("bigint"));
				Assert.That(column.DataType, Is.EqualTo(DataType.Int64));
				Assert.That(column.MemberType, Is.EqualTo("long"));
				Assert.That(column.SystemType, Is.EqualTo(typeof(long)));
			}

			column = schema.Tables[0].Columns.Single(c => c.ColumnName == nameof(UIntTable.Field64));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(column.ColumnType, Is.EqualTo("numeric(20,0)"));
				Assert.That(column.DataType, Is.EqualTo(DataType.Decimal));
				Assert.That(column.MemberType, Is.EqualTo("decimal"));
				Assert.That(column.Precision, Is.EqualTo(20));
				Assert.That(column.Scale, Is.Zero);
				Assert.That(column.SystemType, Is.EqualTo(typeof(decimal)));
			}

			column = schema.Tables[0].Columns.Single(c => c.ColumnName == nameof(UIntTable.Field16N));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(column.ColumnType, Is.EqualTo("integer"));
				Assert.That(column.DataType, Is.EqualTo(DataType.Int32));
				Assert.That(column.MemberType, Is.EqualTo("int?"));
				Assert.That(column.SystemType, Is.EqualTo(typeof(int)));
			}

			column = schema.Tables[0].Columns.Single(c => c.ColumnName == nameof(UIntTable.Field32N));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(column.ColumnType, Is.EqualTo("bigint"));
				Assert.That(column.DataType, Is.EqualTo(DataType.Int64));
				Assert.That(column.MemberType, Is.EqualTo("long?"));
				Assert.That(column.SystemType, Is.EqualTo(typeof(long)));
			}

			column = schema.Tables[0].Columns.Single(c => c.ColumnName == nameof(UIntTable.Field64N));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(column.ColumnType, Is.EqualTo("numeric(20,0)"));
				Assert.That(column.DataType, Is.EqualTo(DataType.Decimal));
				Assert.That(column.MemberType, Is.EqualTo("decimal?"));
				Assert.That(column.Precision, Is.EqualTo(20));
				Assert.That(column.Scale, Is.Zero);
				Assert.That(column.SystemType, Is.EqualTo(typeof(decimal)));
			}
		}

		sealed class ExtraBulkCopyTypesTable
		{
			[Column                            ] public int     Id      { get; set; }
			[Column                            ] public byte?   Byte    { get; set; }
			[Column                            ] public sbyte?  SByte   { get; set; }
			[Column                            ] public short?  Int16   { get; set; }
			[Column                            ] public ushort? UInt16  { get; set; }
			[Column                            ] public int?    Int32   { get; set; }
			[Column                            ] public uint?   UInt32  { get; set; }
			[Column                            ] public long?   Int64   { get; set; }
			[Column                            ] public ulong?  UInt64  { get; set; }
			[Column(DataType = DataType.Byte)  ] public byte?   ByteT   { get; set; }
			[Column(DataType = DataType.SByte) ] public sbyte?  SByteT  { get; set; }
			[Column(DataType = DataType.Int16) ] public short?  Int16T  { get; set; }
			[Column(DataType = DataType.UInt16)] public ushort? UInt16T { get; set; }
			[Column(DataType = DataType.Int32) ] public int?    Int32T  { get; set; }
			[Column(DataType = DataType.UInt32)] public uint?   UInt32T { get; set; }
			[Column(DataType = DataType.Int64) ] public long?   Int64T  { get; set; }
			[Column(DataType = DataType.UInt64)] public ulong?  UInt64T { get; set; }
		}

		[Test]
		public void TestExtraTypesBulkCopy([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values] BulkCopyType type)
		{
			using var db = (DataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable<ExtraBulkCopyTypesTable>();
			var items = new []
				{
					new ExtraBulkCopyTypesTable() { Id = 1 },
					new ExtraBulkCopyTypesTable()
					{
						Id          = 2,

						Byte        = byte.MaxValue,
						SByte       = sbyte.MaxValue,
						Int16       = short.MaxValue,
						UInt16      = ushort.MaxValue,
						Int32       = int.MaxValue,
						UInt32      = uint.MaxValue,
						Int64       = long.MaxValue,
						UInt64      = ulong.MaxValue,
						ByteT       = byte.MaxValue,
						SByteT      = sbyte.MaxValue,
						Int16T      = short.MaxValue,
						UInt16T     = ushort.MaxValue,
						Int32T      = int.MaxValue,
						UInt32T     = uint.MaxValue,
						Int64T      = long.MaxValue,
						UInt64T     = ulong.MaxValue,
					}
				};

			db.BulkCopy(new BulkCopyOptions() { BulkCopyType = type }, items);

			var result = table.OrderBy(_ => _.Id).ToArray();

			Assert.That(result, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[0].Byte, Is.Null);
				Assert.That(result[0].SByte, Is.Null);
				Assert.That(result[0].Int16, Is.Null);
				Assert.That(result[0].UInt16, Is.Null);
				Assert.That(result[0].Int32, Is.Null);
				Assert.That(result[0].UInt32, Is.Null);
				Assert.That(result[0].Int64, Is.Null);
				Assert.That(result[0].UInt64, Is.Null);
				Assert.That(result[0].ByteT, Is.Null);
				Assert.That(result[0].SByteT, Is.Null);
				Assert.That(result[0].Int16T, Is.Null);
				Assert.That(result[0].UInt16T, Is.Null);
				Assert.That(result[0].Int32T, Is.Null);
				Assert.That(result[0].UInt32T, Is.Null);
				Assert.That(result[0].Int64T, Is.Null);
				Assert.That(result[0].UInt64T, Is.Null);

				Assert.That(result[1].Id, Is.EqualTo(2));
				Assert.That(result[1].Byte, Is.EqualTo(byte.MaxValue));
				Assert.That(result[1].SByte, Is.EqualTo(sbyte.MaxValue));
				Assert.That(result[1].Int16, Is.EqualTo(short.MaxValue));
				Assert.That(result[1].UInt16, Is.EqualTo(ushort.MaxValue));
				Assert.That(result[1].Int32, Is.EqualTo(int.MaxValue));
				Assert.That(result[1].UInt32, Is.EqualTo(uint.MaxValue));
				Assert.That(result[1].Int64, Is.EqualTo(long.MaxValue));
				Assert.That(result[1].UInt64, Is.EqualTo(ulong.MaxValue));
				Assert.That(result[1].ByteT, Is.EqualTo(byte.MaxValue));
				Assert.That(result[1].SByteT, Is.EqualTo(sbyte.MaxValue));
				Assert.That(result[1].Int16T, Is.EqualTo(short.MaxValue));
				Assert.That(result[1].UInt16T, Is.EqualTo(ushort.MaxValue));
				Assert.That(result[1].Int32T, Is.EqualTo(int.MaxValue));
				Assert.That(result[1].UInt32T, Is.EqualTo(uint.MaxValue));
				Assert.That(result[1].Int64T, Is.EqualTo(long.MaxValue));
				Assert.That(result[1].UInt64T, Is.EqualTo(ulong.MaxValue));
			}
		}

		[Test]
		public async Task TestExtraTypesBulkCopyAsync([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values] BulkCopyType type)
		{
			using var db = (DataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable<ExtraBulkCopyTypesTable>();
			var items = new []
				{
					new ExtraBulkCopyTypesTable() { Id = 1 },
					new ExtraBulkCopyTypesTable()
					{
						Id      = 2,
						Byte    = byte.MaxValue,
						SByte   = sbyte.MaxValue,
						Int16   = short.MaxValue,
						UInt16  = ushort.MaxValue,
						Int32   = int.MaxValue,
						UInt32  = uint.MaxValue,
						Int64   = long.MaxValue,
						UInt64  = ulong.MaxValue,
						ByteT   = byte.MaxValue,
						SByteT  = sbyte.MaxValue,
						Int16T  = short.MaxValue,
						UInt16T = ushort.MaxValue,
						Int32T  = int.MaxValue,
						UInt32T = uint.MaxValue,
						Int64T  = long.MaxValue,
						UInt64T = ulong.MaxValue,
					}
				};

			await db.BulkCopyAsync(new BulkCopyOptions() { BulkCopyType = type }, items);

			var result = await table.OrderBy(_ => _.Id).ToArrayAsync();

			Assert.That(result, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[0].Byte, Is.Null);
				Assert.That(result[0].SByte, Is.Null);
				Assert.That(result[0].Int16, Is.Null);
				Assert.That(result[0].UInt16, Is.Null);
				Assert.That(result[0].Int32, Is.Null);
				Assert.That(result[0].UInt32, Is.Null);
				Assert.That(result[0].Int64, Is.Null);
				Assert.That(result[0].UInt64, Is.Null);
				Assert.That(result[0].ByteT, Is.Null);
				Assert.That(result[0].SByteT, Is.Null);
				Assert.That(result[0].Int16T, Is.Null);
				Assert.That(result[0].UInt16T, Is.Null);
				Assert.That(result[0].Int32T, Is.Null);
				Assert.That(result[0].UInt32T, Is.Null);
				Assert.That(result[0].Int64T, Is.Null);
				Assert.That(result[0].UInt64T, Is.Null);

				Assert.That(result[1].Id, Is.EqualTo(2));
				Assert.That(result[1].Byte, Is.EqualTo(byte.MaxValue));
				Assert.That(result[1].SByte, Is.EqualTo(sbyte.MaxValue));
				Assert.That(result[1].Int16, Is.EqualTo(short.MaxValue));
				Assert.That(result[1].UInt16, Is.EqualTo(ushort.MaxValue));
				Assert.That(result[1].Int32, Is.EqualTo(int.MaxValue));
				Assert.That(result[1].UInt32, Is.EqualTo(uint.MaxValue));
				Assert.That(result[1].Int64, Is.EqualTo(long.MaxValue));
				Assert.That(result[1].UInt64, Is.EqualTo(ulong.MaxValue));
				Assert.That(result[1].ByteT, Is.EqualTo(byte.MaxValue));
				Assert.That(result[1].SByteT, Is.EqualTo(sbyte.MaxValue));
				Assert.That(result[1].Int16T, Is.EqualTo(short.MaxValue));
				Assert.That(result[1].UInt16T, Is.EqualTo(ushort.MaxValue));
				Assert.That(result[1].Int32T, Is.EqualTo(int.MaxValue));
				Assert.That(result[1].UInt32T, Is.EqualTo(uint.MaxValue));
				Assert.That(result[1].Int64T, Is.EqualTo(long.MaxValue));
				Assert.That(result[1].UInt64T, Is.EqualTo(ulong.MaxValue));
			}
		}

		sealed class BulkCopyTimeoutTable
		{
			[Column] public int     Id    { get; set; }
			[Column] public string? Value { get; set; }
		}

		public enum BulkCopyTimeoutMode
		{
			Sync,
			Async,
			AsyncEnumerable,
		}

		[Test]
		public async Task BulkCopyTimeoutAppliesToAllBatches(
			[IncludeDataSources(TestProvName.AllPostgreSQL)] string context,
			[Values]                                         BulkCopyTimeoutMode mode,
			[Values(5, 15)]                                  int rows)
		{
			var connectionString = new NpgsqlConnectionStringBuilder(GetConnectionString(context)) { CommandTimeout = 1 }.ConnectionString;

			using var db    = (DataConnection)GetDataContext(context, o => o.UseConnectionString(GetDataProvider(context), connectionString));
			using var table = db.CreateLocalTable<BulkCopyTimeoutTable>(tableOptions: TableOptions.IsTemporary);

			// each COPY statement outlives the 1s connection command timeout
			db.Execute("CREATE FUNCTION pg_temp.bulkcopy_timeout_slow() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN PERFORM pg_sleep(1.5); RETURN NULL; END $$");
			db.Execute($"CREATE TRIGGER bulkcopy_timeout_slow_trg AFTER INSERT ON \"{nameof(BulkCopyTimeoutTable)}\" FOR EACH STATEMENT EXECUTE PROCEDURE pg_temp.bulkcopy_timeout_slow()");

			var options = new BulkCopyOptions { BulkCopyType = BulkCopyType.ProviderSpecific, BulkCopyTimeout = 0, MaxBatchSize = 10 };
			var items   = Enumerable.Range(1, rows).Select(i => new BulkCopyTimeoutTable { Id = i, Value = "x" }).ToList();

			switch (mode)
			{
				case BulkCopyTimeoutMode.Sync           : table.BulkCopy(options, items);                                 break;
				case BulkCopyTimeoutMode.Async          : await table.BulkCopyAsync(options, items);                      break;
				case BulkCopyTimeoutMode.AsyncEnumerable: await table.BulkCopyAsync(options, AsAsyncEnumerable(items));   break;
			}

			table.Count().ShouldBe(rows);

			static async IAsyncEnumerable<BulkCopyTimeoutTable> AsAsyncEnumerable(IEnumerable<BulkCopyTimeoutTable> source)
			{
				foreach (var item in source)
				{
					await Task.Yield();
					yield return item;
				}
			}
		}

		public class NpgsqlTableWithDateRanges
		{
			[Column]
			public int Id { get; set; }

			[Column(DbType = "daterange")]
			public NpgsqlRange<DateTime> DateRangeInclusive { get; set; }

			[Column(DbType = "daterange")]
			public NpgsqlRange<DateTime> DateRangeExclusive { get; set; }

			[Column(DbType = "tsrange")]
			public NpgsqlRange<DateTime> TSRange   { get; set; }

			// for unknown reason, npgsql enforce DateTime for tstzrange type
			[Column(DbType = "tstzrange")]
			public NpgsqlRange<DateTime> TSTZRange { get; set; }
		}

		[Test]
		[NoBaselines("Output (datetime format) is culture-/system-dependent")]
		public void TestRange([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<NpgsqlTableWithDateRanges>();
			var range1 = new NpgsqlRange<DateTime>(new DateTime(2000, 2, 3), true, new DateTime(2000, 3, 3), true);
			var range2 = new NpgsqlRange<DateTime>(new DateTime(2000, 2, 3), false, new DateTime(2000, 3, 3), false);
			var range3 = new NpgsqlRange<DateTime>(new DateTime(2000, 2, 3, 4, 5, 6), new DateTime(2000, 4, 3, 4, 5, 6));
			var range4 = new NpgsqlRange<DateTime>(new DateTime(2000, 4, 3, 4, 5, 6, DateTimeKind.Utc), new DateTime(2000, 5, 3, 4, 5, 6, DateTimeKind.Utc));
			db.Insert(new NpgsqlTableWithDateRanges
			{
				DateRangeInclusive = range1,
				DateRangeExclusive = range2,
				TSRange            = range3,
				TSTZRange          = range4,
			});

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.DateRangeInclusive, Is.EqualTo(new NpgsqlRange<DateTime>(new DateTime(2000, 2, 3), true, new DateTime(2000, 3, 4), false)));
				Assert.That(record.DateRangeExclusive, Is.EqualTo(new NpgsqlRange<DateTime>(new DateTime(2000, 2, 4), true, new DateTime(2000, 3, 3), false)));
				Assert.That(record.TSRange, Is.EqualTo(range3));
				Assert.That(record.TSTZRange, Is.EqualTo(range4));
			}
		}

		[Test]
		[NoBaselines("Output (datetime format) is culture-/system-dependent")]
		public void TestRangeBulkCopy([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db    = (DataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable<NpgsqlTableWithDateRanges>();
			var items = Enumerable.Range(1, 100)
				.Select(i =>
				{
					return new NpgsqlTableWithDateRanges
					{
						Id                 = i,
						DateRangeInclusive = new NpgsqlRange<DateTime>(new DateTime(2000, 2, 3), true, new DateTime(2000, 3, 3), true),
						DateRangeExclusive = new NpgsqlRange<DateTime>(new DateTime(2000, 2, 3), false, new DateTime(2000, 3, 3), false),
						TSRange            = new NpgsqlRange<DateTime>(new DateTime(2000 + i, 2, 3, 4, 5, 6), true, new DateTime(2000 + i, 4, 3, 4, 5, 6), true),
						TSTZRange          = new NpgsqlRange<DateTime>(new DateTime(2000 + i, 4, 3, 4, 5, 6, DateTimeKind.Utc), true, new DateTime(2000 + i, 5, 3, 4, 5, 6, DateTimeKind.Utc), true),
					};
				})
				.ToArray();

			db.BulkCopy(items);

			var records = table.OrderBy(_ => _.Id).ToArray();

			Assert.That(records, Has.Length.EqualTo(100));

			AreEqual(
				items.Select(t => new
				{
					t.Id,
					// date range read back as lower-inclusive, upper-exclusive and
					// NpgsqlRange Equals implementation cannot compare equal ranges if they use different flags or bounds types
					// so we need to convert original values
					// https://github.com/npgsql/npgsql/blob/dev/src/Npgsql/NpgsqlTypes/NpgsqlRange.cs#L304
					DateRangeInclusive = new NpgsqlRange<DateTime>(t.DateRangeInclusive.LowerBound, true, t.DateRangeInclusive.UpperBound.AddDays(1), false),
					DateRangeExclusive = new NpgsqlRange<DateTime>(t.DateRangeExclusive.LowerBound.AddDays(1), true, t.DateRangeExclusive.UpperBound, false),
					t.TSRange,
					t.TSTZRange
				}),
				records.Select(t => new { t.Id, t.DateRangeInclusive, t.DateRangeExclusive, t.TSRange, t.TSTZRange }));
		}

		[Test]
		[NoBaselines("Output (datetime format) is culture-/system-dependent")]
		public async Task TestRangeBulkCopyAsync([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = (DataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable<NpgsqlTableWithDateRanges>();
			var items = Enumerable.Range(1, 100)
				.Select(i =>
				{
					return new NpgsqlTableWithDateRanges
					{
						Id                 = i,
						DateRangeInclusive = new NpgsqlRange<DateTime>(new DateTime(2000, 2, 3), true, new DateTime(2000, 3, 3), true),
						DateRangeExclusive = new NpgsqlRange<DateTime>(new DateTime(2000, 2, 3), false, new DateTime(2000, 3, 3), false),
						TSRange            = new NpgsqlRange<DateTime>(new DateTime(2000 + i, 2, 3, 4, 5, 6), true, new DateTime(2000 + i, 4, 3, 4, 5, 6), true),
						TSTZRange          = new NpgsqlRange<DateTime>(new DateTime(2000 + i, 4, 3, 4, 5, 6, DateTimeKind.Utc), true, new DateTime(2000 + i, 5, 3, 4, 5, 6, DateTimeKind.Utc), true),
					};
				})
				.ToArray();

			await db.BulkCopyAsync(items);

			var records = await table.OrderBy(_ => _.Id).ToArrayAsync();

			Assert.That(records, Has.Length.EqualTo(100));

			AreEqual(
				items.Select(t => new
				{
					t.Id,
					// date range read back as lower-inclusive, upper-exclusive and
					// NpgsqlRange Equals implementation cannot compare equal ranges if they use different flags or bounds types
					// so we need to convert original values
					// https://github.com/npgsql/npgsql/blob/dev/src/Npgsql/NpgsqlTypes/NpgsqlRange.cs#L304
					DateRangeInclusive = new NpgsqlRange<DateTime>(t.DateRangeInclusive.LowerBound, true, t.DateRangeInclusive.UpperBound.AddDays(1), false),
					DateRangeExclusive = new NpgsqlRange<DateTime>(t.DateRangeExclusive.LowerBound.AddDays(1), true, t.DateRangeExclusive.UpperBound, false),
					t.TSRange,
					t.TSTZRange
				}),
				records.Select(t => new { t.Id, t.DateRangeInclusive, t.DateRangeExclusive, t.TSRange, t.TSTZRange }));
		}

		sealed class ScalarResult<T>
		{
			public T Value = default!;
		}

		public class DateTimeKindTestCase
		{
			public DateTimeKindTestCase(DateTime dateTime)
			{
				DateTime = dateTime;
			}

			public DateTime DateTime { get; }

			public override string ToString() => $"{DateTime}+{DateTime.Kind}";
		}

		public static IEnumerable<DateTimeKindTestCase> DateTimeKinds
		{
			get
			{
				yield return new DateTimeKindTestCase(new DateTime(2000, 2, 3, 4, 5, 6, 7, DateTimeKind.Local));
				yield return new DateTimeKindTestCase(new DateTime(2000, 2, 3, 4, 5, 6, 7, DateTimeKind.Utc));
				yield return new DateTimeKindTestCase(new DateTime(2000, 2, 3, 4, 5, 6, 7, DateTimeKind.Unspecified));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1742")]
		public void DateTimeKindParameterToTimestampFunction(
			[IncludeDataSources(TestProvName.AllPostgreSQL)] string context,
			[ValueSource(nameof(DateTimeKinds))] DateTimeKindTestCase value,
			[Values(DataType.Undefined, DataType.Date)] DataType dataType,
			[Values(null, "timestamp")] string? dbType)
		{
			using var db = GetDataContext(context);
			var result = db.FromSql<ScalarResult<int>>($"SELECT issue_1742_ts({new DataParameter { Name = "p1", Value = value.DateTime, DataType = dataType, DbType = dbType }}) as \"Value\"").Single();

			Assert.That(result.Value, Is.EqualTo(44));
		}

		public static IEnumerable<DataTypeTestCase> TSTZTypes
		{
			get
			{
				yield return new DataTypeTestCase(DataType.DateTimeOffset, null);
				yield return new DataTypeTestCase(DataType.Undefined, "timestamptz");
				yield return new DataTypeTestCase(DataType.DateTimeOffset, "timestamptz");
			}
		}

		public class DataTypeTestCase
		{
			public DataTypeTestCase(DataType dataType, string? dbType)
			{
				DataType = dataType;
				DbType   = dbType;
			}

			public DataType DataType { get; }
			public string?  DbType   { get; }

			public override string ToString() => $"{DataType}, {DbType}";
		}

		public static IEnumerable<DataTypeTestCase> DateTypes
		{
			get
			{
				yield return new DataTypeTestCase(DataType.Date, null);
				// right now we don't infer DataType from dbtype
				//yield return (DataType.Undefined, "date");
				yield return new DataTypeTestCase(DataType.Date, "date");
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1742")]
		public void DateTimeKindParameterToDateFunction(
			[IncludeDataSources(TestProvName.AllPostgreSQL)] string context,
			[ValueSource(nameof(DateTimeKinds))] DateTimeKindTestCase value,
			[ValueSource(nameof(DateTypes))] DataTypeTestCase type)
		{
			using var db = GetDataContext(context);
			var uspecified = new DateTime(TestData.DateTime.Ticks, DateTimeKind.Unspecified);

			var result = db.FromSql<ScalarResult<int>>($"SELECT issue_1742_date({new DataParameter { Name = "p1", Value = value.DateTime, DataType = type.DataType, DbType = type.DbType }}) as \"Value\"").Single();

			Assert.That(result.Value, Is.EqualTo(42));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1742")]
		public void DateTimeKindParameterToTimestampTzFunction(
			[IncludeDataSources(TestProvName.AllPostgreSQL)] string context,
			[ValueSource(nameof(DateTimeKinds))] DateTimeKindTestCase value,
			[ValueSource(nameof(TSTZTypes))] DataTypeTestCase type)
		{
			using var db = GetDataContext(context);
			var uspecified = new DateTime(TestData.DateTime.Ticks, DateTimeKind.Unspecified);

			var result = db.FromSql<ScalarResult<int>>($"SELECT issue_1742_tstz({new DataParameter { Name = "p1", Value = value.DateTime, DataType = type.DataType, DbType = type.DbType }}) as \"Value\"").Single();

			Assert.That(result.Value, Is.EqualTo(43));
		}

		[Table("AllTypes")]
		public class IntervalAllTypes
		{
			[Column, PrimaryKey, Identity]         public int             ID                { get; set; }
			[Column]                               public TimeSpan?       timeDataType      { get; set; }
			[Column]                               public NpgsqlInterval? intervalDataType  { get; set; }
			[Column(DataType = DataType.Interval)] public TimeSpan?       intervalDataType2 { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1429")]
		public void InsertIntervalColumnsFromTimeSpanAndNpgsqlInterval([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			var maxId = db.GetTable<IntervalAllTypes>().Select(_ => _.ID).Max();
			try
			{
				db.GetTable<IntervalAllTypes>().Insert(() => new IntervalAllTypes()
				{
					timeDataType = TimeSpan.FromMinutes(1),
					intervalDataType = new NpgsqlInterval(1, 2, 3),
					intervalDataType2 = TimeSpan.FromMinutes(1),
				});

				db.GetTable<IntervalAllTypes>().Insert(() => new IntervalAllTypes()
				{
					intervalDataType = new NpgsqlInterval(5, 6, 7),
					intervalDataType2 = TimeSpan.FromDays(3),
				});

				db.GetTable<IntervalAllTypes>().Insert(() => new IntervalAllTypes()
					{
						timeDataType = TimeSpan.FromDays(3)
					});
			}
			finally
			{
				db.GetTable<IntervalAllTypes>().Delete(_ => _.ID > maxId);
			}
		}

		sealed class TableWithArray
		{
			[Column(DbType = "text[]")]
			public string[] StringArray { get; set; } = null!;
		}

		[Test]
		public void UnnestTest([IncludeDataSources(TestProvName.AllPostgreSQL)]
			string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TableWithArray>();

			var query = from t in db.GetTable<TableWithArray>()
				select new
				{
					StringValue = TestPgFunctions.Unnest(t.StringArray)
				};

			query.ToArray();
		}

		[Test]
		public void UnnestTest2([IncludeDataSources(TestProvName.AllPostgreSQL)]
			string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TableWithArray>();

			var query = from t in db.GetTable<TableWithArray>()
				select new
				{
					StringValue = TestPgFunctions.Unnest(t.StringArray)
				};

			query.ToArray();
		}

		public class DataTypeBinaryMapping
		{
			public byte[] Binary { get; set; } = null!;
		}

		// see https://github.com/linq2db/linq2db/issues/3130
		[Test]
		public void DataTypeBinaryMappingTest([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<DataTypeBinaryMapping>()
					.Property(p => p.Binary).HasDataType(DataType.Binary).IsNullable(false)
				.Build();

			using (var db = (DataConnection)GetDataContext(context, ms))
			using (db.CreateLocalTable<DataTypeBinaryMapping>())
			{
				var data = new byte[] { 1, 2, 3 };

				db.BulkCopy(
					new BulkCopyOptions() { BulkCopyType = BulkCopyType.ProviderSpecific },
					new[] { new DataTypeBinaryMapping() { Binary = data } });

				var res = db.GetTable<DataTypeBinaryMapping>().Select(_ => _.Binary).Single();

				Assert.That(data.SequenceEqual(res), Is.True);
			}
		}

		[ActiveIssue(3352, ErrorTypeName = "Npgsql.PostgresException", ErrorMessage = "42883: function test_parameter_typing",
			Details = "the overload cannot be resolved because the parameters are not typed - #3352's subject. The fragment stops before the argument list, which the server spells out in full.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3352")]
		public void FunctionParameterTyping([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var ms = new MappingSchema();

			using var db = new TestDataConnection(context);

			db.Execute(@"
CREATE OR REPLACE        FUNCTION test_parameter_typing(psmallint smallint, pint integer, pbigint bigint, pdecimal decimal, pfloat real, pdouble double precision)
 RETURNS smallint
 LANGUAGE sql
AS $function$
   SELECT psmallint;
$function$
;");

			short?   int16 = 1;
			int?     int32 = 2;
			long?    int64 = 3;
			decimal? dec   = 4;
			float?   fl    = 5;
			double?  dbl   = 6;

			db.Select(() => test_parameter_typing(int16, int32, int64, dec, fl, dbl));

			int16 = null;
			int32 = null;
			int64 = null;
			dec   = null;
			fl    = null;
			dbl   = null;

			db.Select(() => test_parameter_typing(int16, int32, int64, dec, fl, dbl));

			db.Select(() => test_parameter_typing(1, 2, 3, 4, 5, 6));

			db.Select(() => test_parameter_typing(null, null, null, null, null, null));
		}

		[Sql.Function(ServerSideOnly = true)]
		static short? test_parameter_typing(short? input1, int? input2, long? input3, decimal? input4, float? input5, double? input6)
		{
			throw new ServerSideOnlyException(nameof(test_parameter_typing));
		}

		[Table]
		class BigIntegerTable
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(DataType = DataType.Decimal, Precision = 78, Scale = 0)]
			public BigInteger  Value1 { get; set; }

			[Column(DataType = DataType.Decimal, Precision = 78, Scale = 0)]
			public BigInteger? Value2 { get; set; }
		}

		[Test]
		public void TestBigInteger([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context, [Values] bool inline)
		{
			// test direct/remote
			using var db = GetDataContext(context);

			// test parameter/literal
			db.InlineParameters = inline;

			using var table = db.CreateLocalTable<BigIntegerTable>();

			var value1 = BigInteger.Parse("-12345678901234567890123456789012345678901234567890");
			var value2 = BigInteger.Parse("-22345678901234567890123456789012345678901234567890");

			// test write
			db.Insert(new BigIntegerTable() { Id = 1, Value1 = value1, Value2 = value2 });

			// test bulk copy
			if (db is DataConnection dc)
			{
				using var _ = new DisableBaseline("Makes baselines differ for remote context");
				dc.BulkCopy(
					new BulkCopyOptions() { BulkCopyType = BulkCopyType.ProviderSpecific },
					new[] { new BigIntegerTable() { Id = 2, Value1 = value2, Value2 = value1 } });
			}

			// test read
			var data = table.OrderBy(r => r.Id).ToArray();

			if (db is DataConnection)
			{
				Assert.That(data, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Value1, Is.EqualTo(value1));
					Assert.That(data[0].Value2, Is.EqualTo(value2));
					Assert.That(data[1].Value1, Is.EqualTo(value2));
					Assert.That(data[1].Value2, Is.EqualTo(value1));
				}
			}
			else
			{
				Assert.That(data, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Value1, Is.EqualTo(value1));
					Assert.That(data[0].Value2, Is.EqualTo(value2));
				}
			}
		}

		public enum PersonCategory
		{
			Friends   = 1,
			Relatives = 2
		}

		[Test]
		public void ObjectParamTest1([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var ms = new MappingSchema();

			ms.SetConverter<object?,DataParameter>(o => new(null, o is PersonCategory pc ? (int)pc : o, DataType.Undefined));

			using var db = GetDataContext(context, o => o.UseMappingSchema(ms));

			object categoryParam = PersonCategory.Friends;

			_ = db.GetTable<Person>()
				.Select(p => new { p.Name, Category = (PersonCategory)p.ID })
				.Where(p => p.Category.Equals(categoryParam))
				.Count();
		}

		[Test]
		public void ObjectParamTest2([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var ms = new MappingSchema();

			ms.SetConverter<object?,object?>(o => o is PersonCategory pc ? (int)pc : o);

			using var db = GetDataContext(context, o => o.UseMappingSchema(ms));

			object categoryParam = PersonCategory.Friends;

			_ = db.GetTable<Person>()
				.Select(p => new { p.Name, Category = (PersonCategory)p.ID })
				.Where(p => p.Category.Equals(categoryParam))
				.Count();
		}

		[Test]
		public void ObjectParamTest3([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var ms = new MappingSchema();

			ms.SetConverter<object?,object?>(o => o is Enum e ? Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType())) : o);

			using var db = GetDataContext(context, o => o.UseMappingSchema(ms));

			object categoryParam = PersonCategory.Friends;

			_ = db.GetTable<Person>()
				.Select(p => new { p.Name, Category = (PersonCategory)p.ID })
				.Where(p => p.Category.Equals(categoryParam))
				.Count();
		}

		[Test]
		public void ObjectParamTest4([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			object categoryParam = PersonCategory.Friends;

			_ = db.GetTable<Person>()
				.Select(p => new { p.Name, Category = (PersonCategory)p.ID })
				.Where(p => p.Category.Equals(categoryParam))
				.InlineParameters()
				.Count();
		}

		[Test]
		public void ObjectParamTest5([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var ms = new MappingSchema();

			ms.SetConverter<object?,object?>(o => o is Enum e ? ms.EnumToValue(e) : o);

			using var db = GetDataContext(context, o => o.UseMappingSchema(ms));

			object categoryParam = PersonCategory.Friends;

			_ = db.GetTable<Person>()
				.Select(p => new { p.Name, Category = (PersonCategory)p.ID })
				.Where(p => p.Category.Equals(categoryParam))
				.Count();
		}

		[Test]
		public void ObjectParamTest6([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var ms = new MappingSchema();

			ms.SetConverter<object?,DataParameter>(o => new(null, o is Enum e ? ms.EnumToValue(e) : o, DataType.Undefined));

			using var db = GetDataContext(context, o => o.UseMappingSchema(ms));

			object categoryParam = PersonCategory.Friends;

			_ = db.GetTable<Person>()
				.Select(p => new { p.Name, Category = (PersonCategory)p.ID })
				.Where(p => p.Category.Equals(categoryParam))
				.Count();
		}

		[Table("AllTypes")]
		public class TimestampKindAllTypes
		{
			[Column, PrimaryKey, Identity                  ] public int       ID                  { get; set; }
			[Column                                        ] public DateTime? timestampDataType   { get; set; }
			[Column(DbType = "timestamp with time zone")   ] public DateTime? timestampTZDataType { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3895")]
		public void TimestampKindParameterReusedAcrossColumns([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context, [Values] DateTimeKind kind)
		{
			using var db = GetDataContext(context);

			var dt = new DateTime(TestData.DateTime.Ticks, kind);

			_ = db.GetTable<TimestampKindAllTypes>()
				// also tests that same value used as two parameters with different kinds/db types
				.Where(e => e.timestampDataType == dt && e.timestampTZDataType == dt)
				.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3895")]
		public void BulkCopyTimestampKinds(
			[IncludeDataSources(TestProvName.AllPostgreSQL)] string context,
			[Values] DateTimeKind kind,
			[Values] BulkCopyType bulkCopyType)
		{
			var tableName = "BulkCopyTsKinds";

			using var db  = GetDataContext(context);
			using var t   = db.CreateLocalTable<TimestampKindAllTypes>(tableName: tableName);
			var dt        = new DateTime(TestData.DateTime.Ticks, kind);

			var options   = new BulkCopyOptions()
			{
				TableName = tableName,
				BulkCopyType = bulkCopyType
			};

			db.BulkCopy(options, new[] { new TimestampKindAllTypes() { timestampDataType = dt, timestampTZDataType = dt } });
		}

		// https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-SYNTAX-IDENTIFIERS
		// SQL identifiers and key words must begin with
		// - a letter
		// - an underscore (_).
		// Subsequent characters in an identifier or key word can be
		// - letters
		// - underscores
		// - digits (0-9)
		// - dollar signs ($).
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4285")]
		public void TestIdentifierHasNoEscaping(
			[IncludeDataSources(TestProvName.AllPostgreSQL)] string context,
			[Values("test", "тест", "_test", "x_", "x1", "x$")] string tableName)
		{
			using var db = GetDataConnection(context);
			using var t  = db.CreateLocalTable<Person>(tableName: tableName);

			t.ToList();

			Assert.That(db.LastQuery, Does.Not.Contain($"\"{tableName}\""));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4285")]
		public void TestIdentifierHasEscaping(
			[IncludeDataSources(TestProvName.AllPostgreSQL)] string context,
			[Values("Test", "Тест", "1test", "$test", "te-st", "te\"st")] string tableName)
		{
			using var db = GetDataConnection(context);
			using var t  = db.CreateLocalTable<Person>(tableName: tableName);

			t.ToList();

			Assert.That(db.LastQuery, Does.Contain($"\"{tableName.Replace("\"", "\"\"")}\""));
		}

		[Test]
		public void PartitionedTables([IncludeDataSources(TestProvName.AllPostgreSQL10Plus)] string context)
		{
			using var db     = GetDataConnection(context);

			var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions());

			var tables = schema.Tables
				.Where(t => t.TableName != null && t.TableName.StartsWith("multitenant_table"))
				.ToArray();

			tables.Length.ShouldBe(1);

			var multiTenantTable = tables[0];

			multiTenantTable!.Columns.Count.ShouldBe(5);

			var tenantidColumn = multiTenantTable.Columns.Find(c => c.ColumnName == "tenantid");
			tenantidColumn.ShouldNotBeNull();

			tenantidColumn!.IsPrimaryKey.ShouldBe(!context.IsAnyOf(TestProvName.PostgreSQL10));

			var idColumn = multiTenantTable.Columns.Find(c => c.ColumnName == "id");
			idColumn.ShouldNotBeNull();

			idColumn!.IsPrimaryKey.ShouldBe(!context.IsAnyOf(TestProvName.PostgreSQL10));
		}

		// TODO: enable remote context (requires dictionary serialization support)
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4556")]
		public void MergeEmptyAndNonEmptyJsonSourceByDataType([IncludeDataSources(TestProvName.AllPostgreSQL15Plus)] string context)
		{
			var builder = new NpgsqlDataSourceBuilder(GetConnectionString(context));
			builder.EnableDynamicJson();
			var dataSource = builder.Build();

			DataOptions OptionsBuilder(DataOptions o) => o.UseConnectionFactory(GetDataProvider(context), _ => dataSource.CreateConnection());

			using var db = GetDataContext(context, OptionsBuilder);
			using var tb  = db.CreateLocalTable<JsonByDataTypeTable>();

			// test empty set typing
			tb.Merge()
				.Using(Array.Empty<JsonByDataTypeTable>())
				.OnTargetKey()
				.InsertWhenNotMatched()
				.Merge();

			// test non-empty set typing with more than 1 row
			tb.Merge()
				.Using(JsonByDataTypeTable.TestData)
				.OnTargetKey()
				.InsertWhenNotMatched()
				.Merge();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4556")]
		public void MergeEmptyAndNonEmptyJsonSourceByDbType([IncludeDataSources(TestProvName.AllPostgreSQL15Plus)] string context)
		{
			var builder = new NpgsqlDataSourceBuilder(GetConnectionString(context));
			builder.EnableDynamicJson();
			var dataSource = builder.Build();

			DataOptions OptionsBuilder(DataOptions o) => o.UseConnectionFactory(GetDataProvider(context), _ => dataSource.CreateConnection());

			using var db = GetDataContext(context, OptionsBuilder);
			using var tb  = db.CreateLocalTable<JsonByDbTypeTable>();

			// test empty set typing
			tb.Merge()
				.Using(Array.Empty<JsonByDbTypeTable>())
				.OnTargetKey()
				.InsertWhenNotMatched()
				.Merge();

			// test non-empty set typing with more than 1 row
			tb.Merge()
				.Using(JsonByDbTypeTable.TestData)
				.OnTargetKey()
				.InsertWhenNotMatched()
				.Merge();
		}

		sealed class JsonByDataTypeTable
		{
			[PrimaryKey, Identity] public int Id { get; set; }

			[Column(DataType = DataType.Json, Name = "Payload_json")]
			public string? PayloadJson { get; set; }

			[Column(DataType = DataType.BinaryJson, Name = "Payload_jsonb")]
			public string? PayloadJsonB { get; set; }

			[Column(DataType = DataType.Json, Name = "Headers_json")]
			public Dictionary<string, string>? HeadersJson { get; set; }

			[Column(DataType = DataType.BinaryJson, Name = "Headers_jsonb")]
			public Dictionary<string, string>? HeadersJsonB { get; set; }

			public static JsonByDataTypeTable[] TestData =
			[
				new JsonByDataTypeTable()
				{
					PayloadJson = "true",
					PayloadJsonB = "123",
					HeadersJson = new Dictionary<string, string>()
					{
						{ "key1", "value1" }
					},
					HeadersJsonB = new Dictionary<string, string>()
					{
						{ "key2", "value3" }
					}
				},
				new JsonByDataTypeTable()
				{
					PayloadJson = "\"some string\"",
					PayloadJsonB = "-124",
					HeadersJson = new Dictionary<string, string>()
					{
						{ "sd", "sdfgsd" }
					},
					HeadersJsonB = new Dictionary<string, string>()
					{
						{ "g4", "sdg" }
					}
				}
			];
		}

		sealed class JsonByDbTypeTable
		{
			[PrimaryKey, Identity] public int Id { get; set; }

			[Column(DbType = "json", Name = "Payload_json")]
			public string? PayloadJson { get; set; }

			[Column(DbType = "jsonb", Name = "Payload_jsonb")]
			public string? PayloadJsonB { get; set; }

			[Column(DbType = "json", Name = "Headers_json")]
			public Dictionary<string, string>? HeadersJson { get; set; }

			[Column(DbType = "jsonb", Name = "Headers_jsonb")]
			public Dictionary<string, string>? HeadersJsonB { get; set; }

			public static JsonByDbTypeTable[] TestData =
			[
				new JsonByDbTypeTable()
				{
					PayloadJson = "true",
					PayloadJsonB = "123",
					HeadersJson = new Dictionary<string, string>()
					{
						{ "key1", "value1" }
					},
					HeadersJsonB = new Dictionary<string, string>()
					{
						{ "key2", "value3" }
					}
				},
				new JsonByDbTypeTable()
				{
					PayloadJson = "\"some string\"",
					PayloadJsonB = "-124",
					HeadersJson = new Dictionary<string, string>()
					{
						{ "sd", "sdfgsd" }
					},
					HeadersJsonB = new Dictionary<string, string>()
					{
						{ "g4", "sdg" }
					}
				}
			];
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4487")]
		public void InsertAndReadPgEnumAndEnumArray([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			IDataProvider dataProvider;
			string?       connectionString;
			using (var db = GetDataConnection(context))
			{
				dataProvider = db.DataProvider;
				connectionString = db.ConnectionString;
			}

			using var db1 = GetDataContext(context);
			try
			{
				db1.Execute(@"DROP TYPE IF EXISTS ""item_type_enum"";");
				db1.Execute(@"CREATE TYPE ""item_type_enum"" AS ENUM (
  'type1',
  'type2',
  'type3'
)");
				using var _ = db1.CreateLocalTable<PgEnumColumnsTable>();

				var builder = new NpgsqlDataSourceBuilder(connectionString);
				builder.MapEnum<PgItemEnum>("item_type_enum");
				var dataSource = builder.Build();

				using var db = GetDataContext(context, o => o.UseConnectionFactory(dataProvider, _ => dataSource.CreateConnection()));

				db.GetTable<PgEnumColumnsTable>()
					.Value(x => x.Id, 1)
					.Value(x => x.Value, PgItemEnum.Type1)
					.Insert();

				db.Execute("insert into \"PgEnumColumnsTable\"(\"Id\", \"Values\") values (2, '{type3,type2}')");

				db.GetTable<PgEnumColumnsTable>()
					.Value(x => x.Id, 3)
					.Value(x => x.Values, [PgItemEnum.Type1, PgItemEnum.Type2])
					.Insert();

				var result = db.GetTable<PgEnumColumnsTable>().OrderBy(r => r.Id).ToArray();

				Assert.That(result, Has.Length.EqualTo(3));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result[0].Id, Is.EqualTo(1));
					Assert.That(result[0].Value, Is.EqualTo(PgItemEnum.Type1));
					Assert.That(result[0].Values, Is.Null);

					Assert.That(result[1].Id, Is.EqualTo(2));
					Assert.That(result[1].Value, Is.Null);
					Assert.That(result[1].Values, Is.EqualTo(new PgItemEnum[] { PgItemEnum.Type3, PgItemEnum.Type2 }));

					Assert.That(result[2].Id, Is.EqualTo(3));
					Assert.That(result[2].Value, Is.Null);
					Assert.That(result[2].Values, Is.EqualTo(new PgItemEnum[] { PgItemEnum.Type1, PgItemEnum.Type2 }));
				}
			}
			finally
			{
				db1.Execute(@"DROP TYPE IF EXISTS ""item_type_enum"";");
			}
		}

		[Table]
		sealed class PgEnumColumnsTable
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DbType = "item_type_enum", DataType = DataType.Enum)] public PgItemEnum? Value { get; set; }
			[Column(DbType = "item_type_enum[]", DataType = DataType.Enum)] public PgItemEnum[]? Values { get; set; }
		}

		[PgName("item_type_enum")]
		enum PgItemEnum
		{
			[PgName("type1")]
			[MapValue("type1")]
			Type1,
			[PgName("type2")]
			[MapValue("type2")]
			Type2,
			[PgName("type3")]
			[MapValue("type3")]
			Type3,
		}

		[Sql.Expression("{point1} <-> {point2}", ServerSideOnly = true)]
		static double Distance([ExprParameter] NpgsqlPoint? point1, [ExprParameter] NpgsqlPoint? point2) => throw new ServerSideOnlyException(nameof(Distance));

		[ActiveIssue(4250, ErrorTypeName = "System.FormatException", ErrorMessage = "The input string 'point1' was not in a correct format.",
			Details = "a named parameter inside Sql.Expression is read as a format placeholder - #4250's subject.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4250")]
		public void NamedParameterInSqlExpression([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			db.GetTable<AllTypes>().Select(r => Distance(r.pointDataType, r.pointDataType)).ToArray();
		}

		[Sql.Expression("{0} @> '[{1}]'", ServerSideOnly = true, IsPredicate = true, InlineParameters = true)]
		static bool JsonContains(string? json, int value) => throw new ServerSideOnlyException(nameof(JsonContains));

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4348")]
		public void JsonbContainsWithNullCheck([IncludeDataSources(true, TestProvName.AllPostgreSQL95Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<JsonbContainsTable>();

			var storeId = 1;
			tb
				.Where(i => i.Value == null || JsonContains(i.Value, storeId))
				.Select(i => i.Id)
				.FirstOrDefault();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4348")]
		public void JsonbContainsWithConstantConditionAndNullCheck([IncludeDataSources(true, TestProvName.AllPostgreSQL95Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<JsonbContainsTable>();

			var storeId = 1;
			tb
				.Where(i => storeId == 0 || i.Value == null || JsonContains(i.Value, storeId))
				.Select(i => i.Id)
				.FirstOrDefault();
		}

		[Table]
		sealed class JsonbContainsTable
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(DbType = "jsonb")] public string? Value { get; set; }
		}

		// for tests to work, we need pg-specific metadata provider, which provides enum mappings:
		// - from Pg* attributes
		// - schema tables
		// - using npgsql-specific naming conventions
		[ActiveIssue("https://github.com/npgsql/npgsql/issues/4780", ErrorTypeName = "Npgsql.PostgresException", ErrorMessage = "42704: type \"enum\" does not exist",
			Details = "the enum's PostgreSQL type name is not known to us, so the column is rendered against a type literally called \"enum\". Needs the pg-specific metadata provider described above. The 4780 in the Description is npgsql's issue, not linq2db's - linq2db 4780 is an unrelated merged PR.")]
		[Test(Description = "https://github.com/npgsql/npgsql/issues/4780")]
		public void EnumVariableComparison([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values] bool inline)
		{
			IDataProvider dataProvider;
			string?       connectionString;
			using (var db = GetDataConnection(context))
			{
				dataProvider = db.DataProvider;
				connectionString = db.ConnectionString;
			}

			using var db1 = GetDataContext(context);
			try
			{
				db1.Execute(@"create type bar_enum as enum ('item_one', 'item_two');");
				using var _ = db1.CreateLocalTable<BarEnumTable>();

				var builder = new NpgsqlDataSourceBuilder(connectionString);
				builder.MapEnum<BarItemEnum>("item_type_enum");
				var dataSource = builder.Build();

				using var db = GetDataContext(context, o => o.UseConnectionFactory(dataProvider, _ => dataSource.CreateConnection()));

				db.Insert(new BarEnumTable() { Bar = BarItemEnum.ItemOne });
				db.Insert(new BarEnumTable() { Bar = BarItemEnum.ItemTwo });

				var variable = BarItemEnum.ItemOne;

				db.InlineParameters = inline;
				var record = db.GetTable<BarEnumTable>().Where(f => f.Bar == variable).Single();

				Assert.That(record.Bar, Is.EqualTo(variable));
			}
			finally
			{
				db1.Execute(@"DROP TYPE IF EXISTS bar_enum;");
			}
		}

		[ActiveIssue("https://github.com/npgsql/npgsql/issues/4780", ErrorTypeName = "Npgsql.PostgresException", ErrorMessage = "42704: type \"enum\" does not exist",
			Details = "as EnumVariableComparison - the enum's PostgreSQL type name is not known to us, so the column is rendered against a type literally called \"enum\".")]
		[Test(Description = "https://github.com/npgsql/npgsql/issues/4780")]
		public void EnumArrayContainsComparison([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values] bool inline)
		{
			IDataProvider dataProvider;
			string?       connectionString;
			using (var db = GetDataConnection(context))
			{
				dataProvider = db.DataProvider;
				connectionString = db.ConnectionString;
			}

			using var db1 = GetDataContext(context);
			try
			{
				db1.Execute(@"create type bar_enum as enum ('item_one', 'item_two');");
				using var _ = db1.CreateLocalTable<BarEnumTable>();

				var builder = new NpgsqlDataSourceBuilder(connectionString);
				builder.MapEnum<BarItemEnum>("item_type_enum");
				var dataSource = builder.Build();

				using var db = GetDataContext(context, o => o.UseConnectionFactory(dataProvider, _ => dataSource.CreateConnection()));

				db.Insert(new BarEnumTable() { Bar = BarItemEnum.ItemOne });
				db.Insert(new BarEnumTable() { Bar = BarItemEnum.ItemTwo });

				var items = new[]
					{
						BarItemEnum.ItemOne,
						BarItemEnum.ItemTwo
					};

				db.InlineParameters = inline;
				var record = db.GetTable<BarEnumTable>().Where(f => items.Contains(f.Bar)).ToArray();

				Assert.That(record, Has.Length.EqualTo(2));
			}
			finally
			{
				db1.Execute(@"DROP TYPE IF EXISTS bar_enum;");
			}
		}

		enum BarItemEnum
		{
			ItemOne,
			ItemTwo
		}

		[Table]
		sealed class BarEnumTable
		{
			[Column(IsIdentity = true)] public int Id { get; set; }
			[Column(CanBeNull = true, DataType = DataType.Enum)] public BarItemEnum Bar { get; set; }
		}

		[ActiveIssue(2796, SkipForNonLinqService = true, ErrorTypeName = "System.ArgumentException", ErrorMessage = "Cannot write DateTime with",
			Details = "the tstzrange's DateTime kind does not survive the remote transport - #2796's mapping gap, remote only.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2796")]
		public void InsertTstzRangeAsDateTime([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TstzRangeTable>();

			var record = new TstzRangeTable()
			{
				RangeMappedAsDateTime = new NpgsqlRange<DateTime>(TestData.DateTime6Utc, TestData.DateTime6Utc.AddDays(1))
			};

			db.Insert(record);

			var res = tb.Single();

			Assert.That(res.RangeMappedAsDateTime, Is.EqualTo(record.RangeMappedAsDateTime));
		}

		[ActiveIssue(2796, ErrorTypeName = "LinqToDB.Common.LinqToDBConvertException", ErrorMessage = "Cannot convert value{0}NpgsqlTypes.NpgsqlRange",
			Details = "Issue number taken from the test's own Description, which the bare attribute did not carry. One attribute covers both transports: the remote message carries the inner type name, which is what Matches looks for there.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2796")]
		public void InsertTstzRangeAsDateTimeOffset([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TstzRangeTable>();

			var record = new TstzRangeTable()
			{
				RangeMappedAsDateTimeOffset = new NpgsqlRange<DateTimeOffset>(TestData.DateTimeOffset6Utc, TestData.DateTimeOffset6Utc.AddDays(1))
			};

			db.Insert(record);

			var res = tb.Single();

			Assert.That(res.RangeMappedAsDateTimeOffset, Is.EqualTo(record.RangeMappedAsDateTimeOffset));
		}

		[Table("test")]
		public class TstzRangeTable
		{

			[Column(DbType = "tstzrange")]
			public NpgsqlRange<DateTimeOffset>? RangeMappedAsDateTimeOffset { get; set; }

			[Column(DbType = "tstzrange")]
			public NpgsqlRange<DateTime>? RangeMappedAsDateTime { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3352")]
		public void SchemaLoadsOverloadedFunctions([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			// ensure we load schema correctly and issue is in scaffold code
			using var db = GetDataConnection(context);

			var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions() { GetProcedures = true });

			var functions = schema.Procedures.Where(p => p.ProcedureName == "overloads").ToArray();

			Assert.That(functions, Has.Length.EqualTo(3));

			var overload1 = functions.Where(f => f.Parameters.Count == 2).Single();
			var overload2 = functions.Where(f => f.Parameters.Any(p => p.ParameterName == "input2" && p.SystemType == typeof(short))).Single();
			var overload3 = functions.Where(f => f.Parameters.Count == 3 && !f.Parameters.Any(p => p.ParameterName == "input2" && p.SystemType == typeof(short))).Single();
		}

		[Table]
		sealed class PeriodIntervalTable
		{
			[Column(IsIdentity = true)] public int Id { get; set; }
			[Column(DbType = "interval")] public NodaTime.Period? Interval { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4672")]
		public void BulkCopyNodaTimePeriodInterval([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values] bool inline, [Values] BulkCopyType copyType)
		{
			IDataProvider dataProvider;
			string?       connectionString;
			using (var db2 = GetDataConnection(context))
			{
				dataProvider = db2.DataProvider;
				connectionString = db2.ConnectionString;
			}

			var builder = new NpgsqlDataSourceBuilder(connectionString);
			builder.UseNodaTime();
			var dataSource = builder.Build();

			using var db = GetDataContext(context, o => o.UseConnectionFactory(dataProvider, _ => dataSource.CreateConnection()));
			using var tb = db.CreateLocalTable<PeriodIntervalTable>();

			db.InlineParameters = inline;

			// https://github.com/npgsql/npgsql/issues/5867
			var item = new PeriodIntervalTable() { Id = 1, Interval = NodaTime.Period.FromTicks(TestData.Interval.Ticks).Normalize() };
			db.BulkCopy(new BulkCopyOptions() { BulkCopyType = copyType }, [item]);

			var record = tb.Single();

			// Period equality is not correct
			Assert.That(record.Interval!.Ticks, Is.EqualTo(item.Interval.Ticks));
		}

		[Table]
		sealed class InstantColumnsTable
		{
			// NodaTime.Instant has no built-in linq2db DataType, so the column DDL type must be
			// given explicitly (the Npgsql NodaTime plugin maps Instant <-> timestamptz). The CLR
			// type itself stays unmapped, which is the point of the parameter-translation test.
			[Column(IsIdentity = true)               ] public int              Id        { get; set; }
			[Column(DbType = "timestamptz"), Nullable] public NodaTime.Instant? ClosedAt  { get; set; }
			[Column(DbType = "timestamptz")          ] public NodaTime.Instant  CreatedAt { get; set; }
		}

		// Solution 2 helper: translate a custom coalesce into a real SqlCoalesceExpression via an extension
		// builder, so the compared Instant? parameter can borrow a column descriptor — unlike the opaque
		// [Sql.Expression] template (InstantCoalesceRaw), which exposes none.
		sealed class InstantCoalesceBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				builder.ResultExpression = new LinqToDB.Internal.SqlQuery.SqlCoalesceExpression(
					builder.GetExpression(0)!, builder.GetExpression(1)!);
			}
		}

		[Sql.Extension("", BuilderType = typeof(InstantCoalesceBuilder), ServerSideOnly = true)]
		static NodaTime.Instant? InstantCoalesceBuilt(NodaTime.Instant? obj1, NodaTime.Instant obj2) => throw new ServerSideOnlyException(nameof(InstantCoalesceBuilt));

		[Sql.Expression("coalesce({0},{1})", ServerSideOnly = true)]
		static NodaTime.Instant? InstantCoalesceRaw(NodaTime.Instant? obj1, NodaTime.Instant obj2) => throw new ServerSideOnlyException(nameof(InstantCoalesceRaw));

		// Npgsql NodaTime plugin handles Instant at the ADO layer; linq2db has no built-in DataType for it.
		(NpgsqlDataSource DataSource, IDataProvider Provider) CreateNodaTimeDataSource(string context)
		{
			IDataProvider dataProvider;
			string?       connectionString;
			using (var db2 = GetDataConnection(context))
			{
				dataProvider     = db2.DataProvider;
				connectionString = db2.ConnectionString;
			}

			var builder = new NpgsqlDataSourceBuilder(connectionString);
			builder.UseNodaTime();
			return (builder.Build(), dataProvider);
		}

		static NodaTime.Instant PopulateInstantRows(IDataContext db)
		{
			// Fixed instant (not SystemClock) so the captured baseline SQL is deterministic; the test
			// only relies on relative arithmetic (now - 7d / 1h / 2h) and row counts.
			var now = NodaTime.Instant.FromUtc(2020, 1, 1, 0, 0, 0);
			db.Insert(new InstantColumnsTable { ClosedAt = null,                               CreatedAt = now });
			db.Insert(new InstantColumnsTable { ClosedAt = now - NodaTime.Duration.FromHours(1), CreatedAt = now - NodaTime.Duration.FromHours(2) });
			return now;
		}

		// Solution 1: built-in COALESCE via the ?? operator (produces a SqlCoalesceExpression).
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5549")]
		public void InstantCoalesceBuiltInOperator([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var (dataSource, dataProvider) = CreateNodaTimeDataSource(context);

			using var db = GetDataContext(context, o => o.UseConnectionFactory(dataProvider, _ => dataSource.CreateConnection()));
			using var tb = db.CreateLocalTable<InstantColumnsTable>();

			var               now      = PopulateInstantRows(db);
			NodaTime.Instant? fromDate = now - NodaTime.Duration.FromDays(7);

			var results = tb.Where(e => (e.ClosedAt ?? e.CreatedAt) >= fromDate).ToList();

			Assert.That(results, Has.Count.EqualTo(2));
		}

		// Solution 2: custom helper translated to a COALESCE node via an extension builder.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5549")]
		public void InstantCoalesceExtensionBuilder([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var (dataSource, dataProvider) = CreateNodaTimeDataSource(context);

			using var db = GetDataContext(context, o => o.UseConnectionFactory(dataProvider, _ => dataSource.CreateConnection()));
			using var tb = db.CreateLocalTable<InstantColumnsTable>();

			var               now      = PopulateInstantRows(db);
			NodaTime.Instant? fromDate = now - NodaTime.Duration.FromDays(7);

			var results = tb.Where(e => InstantCoalesceBuilt(e.ClosedAt, e.CreatedAt) >= fromDate).ToList();

			Assert.That(results, Has.Count.EqualTo(2));
		}

		// Solution 3: register the unmapped type so the opaque [Sql.Expression] template works too
		// (SetScalarType; equivalent to the reporter's SetDataType workaround).
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5549")]
		public void InstantCoalesceScalarType([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var (dataSource, dataProvider) = CreateNodaTimeDataSource(context);

			var ms = new MappingSchema();
			ms.SetScalarType(typeof(NodaTime.Instant));

			using var db = GetDataContext(context, o => o.UseConnectionFactory(dataProvider, _ => dataSource.CreateConnection()).UseMappingSchema(ms));
			using var tb = db.CreateLocalTable<InstantColumnsTable>();

			var               now      = PopulateInstantRows(db);
			NodaTime.Instant? fromDate = now - NodaTime.Duration.FromDays(7);

			var results = tb.Where(e => InstantCoalesceRaw(e.ClosedAt, e.CreatedAt) >= fromDate).ToList();

			Assert.That(results, Has.Count.EqualTo(2));
		}

		sealed class JsonComparisonTable1
		{
			[Column                                ] public string? Text  { get; set; }
			[Column(DataType = DataType.Json)      ] public string? Json  { get; set; }
			[Column(DataType = DataType.BinaryJson)] public string? Jsonb { get; set; }
		}

		sealed class JsonComparisonTable2
		{
			[Column                  ] public string? Text { get; set; }
			[Column(DbType = "json") ] public string? Json { get; set; }
			[Column(DbType = "json", Configuration = ProviderName.PostgreSQL92)]
			[Column(DbType = "json", Configuration = ProviderName.PostgreSQL93)]
			[Column(DbType = "jsonb")] public string? Jsonb { get; set; }
		}

		[Test]
		public void JsonComparison_ByDataType([IncludeDataSources(true, TestProvName.AllPostgreSQL95Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable([new JsonComparisonTable1()
			{
				Text  = /*lang=json,strict*/ "{ \"field\": 123}",
				Json  = /*lang=json,strict*/ "{  \"field\": 123}",
				Jsonb = /*lang=json,strict*/ "{   \"field\": 123}",
			}]);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Count(r => r.Text == r.Json), Is.EqualTo(1));
				Assert.That(tb.Count(r => r.Text == r.Jsonb), Is.EqualTo(1));
				Assert.That(tb.Count(r => r.Json == r.Json), Is.EqualTo(1));
				Assert.That(tb.Count(r => r.Json == r.Jsonb), Is.EqualTo(1));
				Assert.That(tb.Count(r => r.Jsonb == r.Jsonb), Is.EqualTo(1));

				// reverse
				Assert.That(tb.Count(r => r.Json == r.Text), Is.EqualTo(1));
				Assert.That(tb.Count(r => r.Jsonb == r.Text), Is.EqualTo(1));
				Assert.That(tb.Count(r => r.Jsonb == r.Json), Is.EqualTo(1));
			}
		}

		[Test]
		public void JsonComparison_ByDbType([IncludeDataSources(true, TestProvName.AllPostgreSQL95Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable([new JsonComparisonTable2()
			{
				Text  = /*lang=json,strict*/ "{ \"field\": 123}",
				Json  = /*lang=json,strict*/ "{  \"field\": 123}",
				Jsonb = /*lang=json,strict*/ "{   \"field\": 123}",
			}]);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(tb.Count(r => r.Text == r.Json), Is.EqualTo(1));
				Assert.That(tb.Count(r => r.Text == r.Jsonb), Is.EqualTo(1));
				Assert.That(tb.Count(r => r.Json == r.Json), Is.EqualTo(1));
				Assert.That(tb.Count(r => r.Json == r.Jsonb), Is.EqualTo(1));
				Assert.That(tb.Count(r => r.Jsonb == r.Jsonb), Is.EqualTo(1));

				// reverse
				Assert.That(tb.Count(r => r.Json == r.Text), Is.EqualTo(1));
				Assert.That(tb.Count(r => r.Jsonb == r.Text), Is.EqualTo(1));
				Assert.That(tb.Count(r => r.Jsonb == r.Json), Is.EqualTo(1));
			}
		}

		sealed class UtcNowComparisonTable
		{
			[PrimaryKey]
			public int Id { get; set; }

			public DateTime        Timestamp    { get; set; }
			public DateTimeOffset  TimestampTZ  { get; set; }
			public DateTime?       TimestampN   { get; set; }
			public DateTimeOffset? TimestampTZN { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5325")]
		public void UtcNowComparisonUsesTimezoneFunction([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable<UtcNowComparisonTable>();

			_ = tb.Where(r => r.Timestamp == DateTime.UtcNow).ToList();
			Assert.That(db.LastQuery, Does.Contain("timezone('UTC', now())"));

			_ = tb.Where(r => r.TimestampN == DateTime.UtcNow).ToList();
			Assert.That(db.LastQuery, Does.Contain("timezone('UTC', now())"));

			_ = tb.Where(r => r.TimestampTZ == DateTime.UtcNow).ToList();
			Assert.That(db.LastQuery, Does.Contain("timezone('UTC', now())"));

			_ = tb.Where(r => r.TimestampTZN == DateTime.UtcNow).ToList();
			Assert.That(db.LastQuery, Does.Contain("timezone('UTC', now())"));

			_ = tb.Where(r => r.Timestamp == DateTimeOffset.UtcNow).ToList();
			Assert.That(db.LastQuery, Does.Contain("now()"));

			_ = tb.Where(r => r.TimestampN == DateTimeOffset.UtcNow).ToList();
			Assert.That(db.LastQuery, Does.Contain("now()"));

			_ = tb.Where(r => r.TimestampTZ == DateTimeOffset.UtcNow).ToList();
			Assert.That(db.LastQuery, Does.Contain("now()"));

			_ = tb.Where(r => r.TimestampTZN == DateTimeOffset.UtcNow).ToList();
			Assert.That(db.LastQuery, Does.Contain("now()"));
		}

		[Table("pgtimestamptest")]
		public class PgTimestampBulkCopyRow
		{
			[Column("id")] public int Id { get; set; }
			[Column("updatedon")] public DateTime UpdatedOn { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3342")]
		public void BulkCopyProviderSpecificDateTimeIntoTimestampTz([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var ms = new MappingSchema();
			ms.AddScalarType(typeof(DateTimeOffset), DataType.DateTimeOffset);

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<PgTimestampBulkCopyRow>();
			var dc = ((DataConnection)db);
			var sql = "ALTER TABLE pgtimestamptest ALTER COLUMN updatedon TYPE timestamptz;";
			dc.Execute(sql);

			var obj = new PgTimestampBulkCopyRow() { Id = 0, UpdatedOn = DateTime.UtcNow };

			var ret =
					dc.BulkCopy(new BulkCopyOptions
					{
						CheckConstraints = false,
						BulkCopyType = BulkCopyType.ProviderSpecific,
						MaxBatchSize = 5000,
						UseInternalTransaction = false,
						NotifyAfter = 2000,
						BulkCopyTimeout = 0,
						TableName = "pgtimestamptest",
					}, new [] {obj });
		}

		[Table(Schema = "public", Name = "schedule")]
		public class EnumSchedule
		{
			[Column("id"), PrimaryKey, Identity] public int Id { get; set; } // integer
			[Column("unit", DataType = DataType.Enum)] public ScheduleTimeUnit Unit { get; set; } // USER-DEFINED
			[Column("unit_nullable", DataType = DataType.Enum)] public ScheduleTimeUnit? UnitNullable { get; set; } // USER-DEFINED
			[Column("amount")] public int Amount { get; set; } // integer
		}

		[Table(Schema = "public", Name = "schedule")]
		public class EnumScheduleWithDbType
		{
			[Column("id"), PrimaryKey, Identity] public int Id { get; set; } // integer
			[Column("unit", DataType = DataType.Enum, DbType = "time_unit")] public ScheduleTimeUnit Unit { get; set; } // USER-DEFINED
			[Column("unit_nullable", DataType = DataType.Enum, DbType = "time_unit")] public ScheduleTimeUnit? UnitNullable { get; set; } // USER-DEFINED
			[Column("amount")] public int Amount { get; set; } // integer
		}

		public enum ScheduleTimeUnit
		{
			[MapValue("hour")]
			Hour,
			[MapValue("day")]
			Day,
		}

		private DataOptions SetupScheduleEnumOptions(string context, bool withTable)
		{
			var mappingSchema = new MappingSchema();

			const string initScript = @"
DROP TABLE IF EXISTS schedule;
DROP TYPE IF EXISTS time_unit;
CREATE TYPE time_unit AS ENUM ('hour', 'day');";

			const string createTableScript = @"
CREATE TABLE IF NOT EXISTS schedule
(
  id SERIAL CONSTRAINT schedule_pk PRIMARY KEY,
  unit                 time_unit   NOT NULL,
  unit_nullable        time_unit       NULL,
  amount               int         NOT NULL
);
INSERT INTO schedule(unit, unit_nullable,amount) VALUES ('day','day',1),('day','day',2),('day','day',3);";

			// executing separately, we have to reload just created types
			IDataProvider dataProvider;
			string?       connectionString;
			using (var db = GetDataConnection(context))
			{
				db.Execute(initScript);
				if (withTable)
					db.Execute(createTableScript);

				dataProvider     = db.DataProvider;
				connectionString = db.ConnectionString;
			}

			var builder = new NpgsqlDataSourceBuilder(connectionString);
			builder.MapEnum<ScheduleTimeUnit>("time_unit");
			var dataSource = builder.Build();

			return new DataOptions()
				.UseConnectionFactory(dataProvider, _ => dataSource.CreateConnection())
				.UseMappingSchema(mappingSchema);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3453")]
		public void NativeEnumMapping([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			var       unit         = ScheduleTimeUnit.Day;
			ScheduleTimeUnit? unitNullable = ScheduleTimeUnit.Day;
			ScheduleTimeUnit? unitNull     = null;

			using var db = GetDataConnection(SetupScheduleEnumOptions(context, true));
			db.Insert(new EnumSchedule { Unit = ScheduleTimeUnit.Hour, Amount = 1 });

			db.GetTable<EnumSchedule>().ToList().Count().ShouldBe(4);

			db.GetTable<EnumSchedule>().Where(x => x.Unit == unit).ToList().Count().ShouldBe(3);
			db.GetTable<EnumSchedule>().Where(x => x.UnitNullable == unit).ToList().Count().ShouldBe(3);
			db.GetTable<EnumSchedule>().Where(x => x.UnitNullable == unitNullable).ToList().Count().ShouldBe(3);
			db.GetTable<EnumSchedule>().Where(x => x.UnitNullable == ScheduleTimeUnit.Day).ToList().Count().ShouldBe(3);

			db.GetTable<EnumSchedule>().Where(x => x.UnitNullable == unitNull).ToList().Count().ShouldBe(1);

			var alItems = db.GetTable<EnumSchedule>().ToArray();

			alItems[0].Unit.ShouldBe(ScheduleTimeUnit.Day);
			alItems[1].Unit.ShouldBe(ScheduleTimeUnit.Day);
			alItems[2].Unit.ShouldBe(ScheduleTimeUnit.Day);
			alItems[3].Unit.ShouldBe(ScheduleTimeUnit.Hour);

			alItems[0].UnitNullable.ShouldBe(ScheduleTimeUnit.Day);
			alItems[1].UnitNullable.ShouldBe(ScheduleTimeUnit.Day);
			alItems[2].UnitNullable.ShouldBe(ScheduleTimeUnit.Day);
			alItems[3].UnitNullable.ShouldBeNull();

		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3453")]
		public void NativeEnumBulkCopyWithDbType([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context, [Values] BulkCopyType bcType)
		{
			var data = new[]
			{
				new EnumScheduleWithDbType { Unit = ScheduleTimeUnit.Day, UnitNullable  = ScheduleTimeUnit.Day, Amount = 1 },
				new EnumScheduleWithDbType { Unit = ScheduleTimeUnit.Day, UnitNullable  = ScheduleTimeUnit.Day, Amount = 2 },
				new EnumScheduleWithDbType { Unit = ScheduleTimeUnit.Day, UnitNullable  = ScheduleTimeUnit.Day, Amount = 3 },
				new EnumScheduleWithDbType { Unit = ScheduleTimeUnit.Hour, UnitNullable = null, Amount         = 1 },
			};

			var       unit         = ScheduleTimeUnit.Day;
			ScheduleTimeUnit? unitNullable = ScheduleTimeUnit.Day;
			ScheduleTimeUnit? unitNull     = null;

			using (var db = GetDataConnection(SetupScheduleEnumOptions(context, false)))
			using (db.CreateLocalTable<EnumScheduleWithDbType>())
			{
				db.BulkCopy(new BulkCopyOptions() { BulkCopyType = bcType }, data);

				db.GetTable<EnumSchedule>().ToList().Count().ShouldBe(4);

				db.GetTable<EnumSchedule>().Where(x => x.Unit         == unit).ToList().Count().ShouldBe(3);
				db.GetTable<EnumSchedule>().Where(x => x.UnitNullable == unit).ToList().Count().ShouldBe(3);
				db.GetTable<EnumSchedule>().Where(x => x.UnitNullable == unitNullable).ToList().Count().ShouldBe(3);
				db.GetTable<EnumSchedule>().Where(x => x.UnitNullable == ScheduleTimeUnit.Day).ToList().Count().ShouldBe(3);

				db.GetTable<EnumSchedule>().Where(x => x.UnitNullable == unitNull).ToList().Count().ShouldBe(1);

				var alItems = db.GetTable<EnumSchedule>().ToArray();

				alItems[0].Unit.ShouldBe(ScheduleTimeUnit.Day);
				alItems[1].Unit.ShouldBe(ScheduleTimeUnit.Day);
				alItems[2].Unit.ShouldBe(ScheduleTimeUnit.Day);
				alItems[3].Unit.ShouldBe(ScheduleTimeUnit.Hour);

				alItems[0].UnitNullable.ShouldBe(ScheduleTimeUnit.Day);
				alItems[1].UnitNullable.ShouldBe(ScheduleTimeUnit.Day);
				alItems[2].UnitNullable.ShouldBe(ScheduleTimeUnit.Day);
				alItems[3].UnitNullable.ShouldBeNull();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3453")]
		public async Task NativeEnumBulkCopyWithDbTypeAsync([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context, [Values] BulkCopyType bcType)
		{
			var data = new[]
			{
				new EnumScheduleWithDbType { Unit = ScheduleTimeUnit.Day, UnitNullable  = ScheduleTimeUnit.Day, Amount = 1 },
				new EnumScheduleWithDbType { Unit = ScheduleTimeUnit.Day, UnitNullable  = ScheduleTimeUnit.Day, Amount = 2 },
				new EnumScheduleWithDbType { Unit = ScheduleTimeUnit.Day, UnitNullable  = ScheduleTimeUnit.Day, Amount = 3 },
				new EnumScheduleWithDbType { Unit = ScheduleTimeUnit.Hour, UnitNullable = null, Amount         = 1 },
			};

			var       unit         = ScheduleTimeUnit.Day;
			ScheduleTimeUnit? unitNullable = ScheduleTimeUnit.Day;
			ScheduleTimeUnit? unitNull     = null;

			using (var db = GetDataConnection(SetupScheduleEnumOptions(context, false)))
			using (db.CreateLocalTable<EnumScheduleWithDbType>())
			{
				await db.BulkCopyAsync(new BulkCopyOptions() { BulkCopyType = bcType }, data);

				db.GetTable<EnumSchedule>().ToList().Count().ShouldBe(4);

				db.GetTable<EnumSchedule>().Where(x => x.Unit         == unit).ToList().Count().ShouldBe(3);
				db.GetTable<EnumSchedule>().Where(x => x.UnitNullable == unit).ToList().Count().ShouldBe(3);
				db.GetTable<EnumSchedule>().Where(x => x.UnitNullable == unitNullable).ToList().Count().ShouldBe(3);
				db.GetTable<EnumSchedule>().Where(x => x.UnitNullable == ScheduleTimeUnit.Day).ToList().Count().ShouldBe(3);

				db.GetTable<EnumSchedule>().Where(x => x.UnitNullable == unitNull).ToList().Count().ShouldBe(1);

				var alItems = db.GetTable<EnumSchedule>().ToArray();

				alItems[0].Unit.ShouldBe(ScheduleTimeUnit.Day);
				alItems[1].Unit.ShouldBe(ScheduleTimeUnit.Day);
				alItems[2].Unit.ShouldBe(ScheduleTimeUnit.Day);
				alItems[3].Unit.ShouldBe(ScheduleTimeUnit.Hour);

				alItems[0].UnitNullable.ShouldBe(ScheduleTimeUnit.Day);
				alItems[1].UnitNullable.ShouldBe(ScheduleTimeUnit.Day);
				alItems[2].UnitNullable.ShouldBe(ScheduleTimeUnit.Day);
				alItems[3].UnitNullable.ShouldBeNull();
			}
		}

		[Table]
		public partial class EnumTypeUser
		{
			[PrimaryKey                                                 ] public int          Id             { get; set; }
			[Column(DataType = DataType.Enum, DbType = "user_type_enum")] public UserTypeEnum Type           { get; set; }
			[Column                                                     ] public int?         OrganizationId { get; set; }

			[ExpressionMethod(nameof(InYourOrganizationSelector))]
			public bool InYourOrganization(EnumTypeUser callingUser) => throw new ServerSideOnlyException(nameof(InYourOrganization));

			static Expression<Func<EnumTypeUser, EnumTypeUser, bool>> InYourOrganizationSelector()
			{
				return (user, callingUser) => (user.Type == UserTypeEnum.Organization && user.Id == callingUser.Id)
						|| (user.Type == UserTypeEnum.Organization && callingUser.Type == UserTypeEnum.OrganizationUser && user.Id == callingUser.OrganizationId)
						|| (user.Type == UserTypeEnum.OrganizationUser && callingUser.Type == UserTypeEnum.Organization && user.OrganizationId == callingUser.Id)
						|| (user.Type == UserTypeEnum.OrganizationUser && callingUser.Type == UserTypeEnum.OrganizationUser && user.OrganizationId == callingUser.OrganizationId);
			}

			public static readonly EnumTypeUser[] Data = new[]
			{
				new EnumTypeUser() { Id = 1, Type = UserTypeEnum.Organization },
				new EnumTypeUser() { Id = 2, Type = UserTypeEnum.Organization },
				new EnumTypeUser() { Id = 3, Type = UserTypeEnum.OrganizationUser, OrganizationId = 1 },
				new EnumTypeUser() { Id = 4, Type = UserTypeEnum.OrganizationUser, OrganizationId = 2 },
			};
		}

		[PgName("user_type_enum")]
		public enum UserTypeEnum
		{
			[PgName("org")]
			[MapValue("org")]
			Organization = 1,

			[PgName("org_user")]
			[MapValue("org_user")]
			OrganizationUser = 2,
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3548")]
		public void EnumComparisonInExpressionMethodFilter([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			IDataProvider dataProvider;
			string?       connectionString;

			using (var db = GetDataConnection(context))
			{
				db.Execute("DROP TABLE IF EXISTS \"EnumTypeUser\";DROP TYPE IF EXISTS user_type_enum;CREATE TYPE user_type_enum AS ENUM('org', 'org_user');");
				dataProvider     = db.DataProvider;
				connectionString = db.ConnectionString;
			}

			var builder = new NpgsqlDataSourceBuilder(connectionString);
			builder.MapEnum<UserTypeEnum>();
			var dataSource = builder.Build();

			var options = new DataOptions()
				.UseConnectionFactory(dataProvider, _ => dataSource.CreateConnection());

			try
			{
				using var db = GetDataConnection(options);
				using var _  = db.CreateLocalTable(EnumTypeUser.Data);

				((NpgsqlConnection)db.OpenDbConnection()).ReloadTypes();

				var user  = new EnumTypeUser() { Id = 1, Type = UserTypeEnum.Organization };
				var users = db.GetTable<EnumTypeUser>().Where(x => x.InYourOrganization(user)).OrderBy(x => x.Id).Select(x => x.Id.ToString()).ToList();

				Assert.That(users, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(users[0], Is.EqualTo("1"));
					Assert.That(users[1], Is.EqualTo("3"));
				}

				user  = new EnumTypeUser() { Id = 4, Type = UserTypeEnum.OrganizationUser, OrganizationId = 2 };
				users = db.GetTable<EnumTypeUser>().Where(x => x.InYourOrganization(user)).OrderBy(x => x.Id).Select(x => x.Id.ToString()).ToList();

				Assert.That(users, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(users[0], Is.EqualTo("2"));
					Assert.That(users[1], Is.EqualTo("4"));
				}
			}
			finally
			{
				using var db = GetDataContext(context);
				try
				{
					db.Execute("DROP TYPE IF EXISTS user_type_enum;");
				}
				catch
				{
				}
			}
		}

		// Repro of issue #5125 (https://github.com/linq2db/linq2db/issues/5125).
		// The original repro (non-nullable string + [AllowNull] + [ExpressionMethod] using
		// `string.IsNullOrEmpty` + interpolated string, projected via `new T { ... }` after
		// OrderBy on a calculated expression) emitted an invalid SQL where Length(...) inside
		// the subquery referenced the outer alias `x_1` instead of `x`. That was fixed by #5126.
		//
		// The v6.2.0+ follow-up regression reported in the same issue is reproduced here: when
		// the calling code installs an IExpressionPreprocessor that wraps OrderBy lambdas in
		// Sql.Expr<T>($"{0} NULLS FIRST"), the optimizer extracts the wrapped expression as a
		// subquery column but keeps the NULLS FIRST directive inside the column expression,
		// producing PostgreSQL like:
		//     CASE WHEN ... THEN ... ELSE ... END NULLS FIRST as c1
		// which fails with `42601: syntax error at or near "NULLS"`. The NULLS FIRST directive
		// must stay in the outer ORDER BY clause, not inside the subquery column.
		[Table("InterpolatedOrderEntity", IsColumnAttributeRequired = false)]
		public class InterpolatedOrderEntity
		{
			public int     Id     { get; set; }
			public string? StrVal { get; set; }
			public int     IntVal { get; set; }

			[AllowNull]
			[ExpressionMethod(nameof(Expr1Impl))]
			public string Expr1 { get; set; } = null!;

			private static Expression<Func<InterpolatedOrderEntity, IDataContext, string>> Expr1Impl() =>
				(e, ctx) => !string.IsNullOrEmpty(e.StrVal) ? e.StrVal! : e.IntVal.ToString();

			[AllowNull]
			[ExpressionMethod(nameof(Expr2Impl))]
			public string Expr2 { get; set; } = null!;

			private static Expression<Func<InterpolatedOrderEntity, IDataContext, string>> Expr2Impl() =>
				(e, ctx) => $"{(!string.IsNullOrEmpty(e.StrVal) ? e.StrVal : e.IntVal.ToString())}";
		}

		sealed class OrderByNullsFirstVisitor : ExpressionVisitor
		{
			static readonly MethodInfo SqlExprMethod =
				typeof(Sql).GetMethods(BindingFlags.Public | BindingFlags.Static)
					.First(m => m.Name == nameof(Sql.Expr) && m.IsGenericMethodDefinition
						&& m.GetParameters().Length == 1
						&& m.GetParameters()[0].ParameterType == typeof(FormattableString));

			static readonly MethodInfo CreateFormattable =
				typeof(FormattableStringFactory).GetMethod(
					nameof(FormattableStringFactory.Create),
					new[] { typeof(string), typeof(object[]) })!;

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				if (node.Method.DeclaringType == typeof(Queryable)
					&& node.Method.Name == nameof(Queryable.OrderBy))
				{
					var lambda = (LambdaExpression)((UnaryExpression)node.Arguments[1]).Operand;
					var body   = lambda.Body;

					var sqlExprCall = Expression.Call(
						SqlExprMethod.MakeGenericMethod(body.Type),
						Expression.Call(
							CreateFormattable,
							Expression.Constant("{0} NULLS FIRST"),
							Expression.NewArrayInit(typeof(object), Expression.Convert(body, typeof(object)))
						)
					);

					var newLambda = Expression.Lambda(sqlExprCall, lambda.Parameters);
					return Expression.Call(node.Method, Visit(node.Arguments[0]), newLambda);
				}

				return base.VisitMethodCall(node);
			}
		}

		sealed class InterpolatedDataConnection : DataConnection, IExpressionPreprocessor
		{
			public InterpolatedDataConnection(string configurationString) : base(configurationString)
			{
			}

			public Expression ProcessExpression(Expression expression)
				=> new OrderByNullsFirstVisitor().Visit(expression);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5125")]
		public void OrderByPreprocessorNullsFirstOnCalculatedColumn([IncludeDataSources(false, TestProvName.AllPostgreSQL)] string context)
		{
			var data = new[]
			{
				new InterpolatedOrderEntity { Id = 1, StrVal = null,     IntVal = 11 },
				new InterpolatedOrderEntity { Id = 2, StrVal = "",       IntVal = 12 },
				new InterpolatedOrderEntity { Id = 3, StrVal = "Value3", IntVal = 13 },
			};

			using var db = new InterpolatedDataConnection(context);
			using var tb = db.CreateLocalTable(data);

			var query = tb
				.OrderBy(x => x.Expr1)
				.Select(x => new InterpolatedOrderEntity
				{
					Id    = x.Id,
					Expr1 = x.Expr1,
					Expr2 = x.Expr2
				});

			var list = query.ToList();

			list.Count.ShouldBe(3);
		}

		public sealed record JsonbUpdateData
		{
			public string? SomeKey { get; init; }
			public bool?   Updated { get; init; }
		}

		sealed class JsonbUpdateDataConverterAttribute : ValueConverterAttribute
		{
			public JsonbUpdateDataConverterAttribute()
			{
				ValueConverter = new ValueConverter<JsonbUpdateData?, string?>(
					v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					s => s == null ? null : JsonSerializer.Deserialize<JsonbUpdateData>(s, (JsonSerializerOptions?)null),
					handlesNulls: true);
			}
		}

		[Table]
		sealed class JsonbConvertedTable
		{
			[PrimaryKey]                                                                  public int       Id   { get; set; }
			[Column(DataType = DataType.BinaryJson, CanBeNull = true), JsonbUpdateDataConverter] public JsonbUpdateData? Data { get; set; }
		}

		[Sql.Expression("jsonb_set({0}, ARRAY[{1}], {2}::jsonb)", ServerSideOnly = true, InlineParameters = true)]
		static T? JsonbSet<T>(T? jsonb, string key, string value)
		{
			throw new ServerSideOnlyException(nameof(JsonbSet));
		}

		/// <summary>
		/// Regression: UPDATE with a <c>ServerSideOnly</c> SQL function targeting a column
		/// that has a value converter fails translation because the builder wraps the
		/// function result with the converter's ToProvider expression.
		/// <see href="https://github.com/linq2db/linq2db/issues/5505"/>
		/// </summary>
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5505")]
		public void UpdateWithServerSideOnlyAndValueConverter([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<JsonbConvertedTable>();

			db.Insert(new JsonbConvertedTable { Id = 1, Data = new JsonbUpdateData { SomeKey = "abc" } });

			var affected = table
				.Where(x => x.Id == 1)
				.Set(x => x.Data, x => JsonbSet(x.Data, "Updated", "true"))
				.Update();

			affected.ShouldBe(1);

			var result = table.First(x => x.Id == 1);
			result.Data.ShouldNotBeNull();
			result.Data.SomeKey.ShouldBe("abc");
			result.Data.Updated.ShouldBe(true);
		}

		public class PgIdentityClient
		{
			public int Id { get; set; }

			public bool Has { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1438")]
		public void InsertWithInt32IdentityOnPostgreSql95ExplicitNpgsqlConnection([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values] bool avoidProviderSpecificApi)
		{
			var provider = PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v95);
			var cs       = DataConnection.GetConnectionString(context);

			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<PgIdentityClient>()
					.HasTableName("PgIdentityClient")
					.Property(x => x.Id)
						.IsPrimaryKey()
						.IsIdentity()
				.Build();

			using var cn = new NpgsqlConnection(cs);
			using var db = new DataConnection(new DataOptions().UseConnection(provider, cn));
			db.AddMappingSchema(ms);
			using var tbl = db.CreateLocalTable<PgIdentityClient>();
			var id = db.InsertWithInt32Identity(new PgIdentityClient()
			{
				Has = true
			});

			var record = tbl.Where(_ => _.Id == id).Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.Id, Is.EqualTo(id));
				Assert.That(record.Has, Is.True);
			}
		}
	}

	#region Extensions
	public static class TestPgAggregates
	{
		[Sql.Function("test_avg", ServerSideOnly = true, IsAggregate = true, ArgIndices = new[] { 1 })]
		public static double CustomAvg<TSource>(this IEnumerable<TSource> src, Expression<Func<TSource, double>> value)
		{
			throw new ServerSideOnlyException(nameof(CustomAvg));
		}

		[Sql.Function("test_avg", ServerSideOnly = true, IsAggregate = true, ArgIndices = new[] { 1 })]
		public static double? CustomAvg<TSource>(this IEnumerable<TSource> src, Expression<Func<TSource, double?>> value)
		{
			throw new ServerSideOnlyException(nameof(CustomAvg));
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
			throw new ServerSideOnlyException(nameof(AddIfNotExists));
		}

		[Sql.TableFunction("TestTableFunctionSchema")]
		public LinqToDB.ITable<PostgreSQLTests.AllTypes> GetAllTypes()
		{
			var methodInfo = typeof(TestPgFunctions).GetMethod("GetAllTypes", [])!;

			return _ctx.GetTable<PostgreSQLTests.AllTypes>(this, methodInfo);
		}

		// TODO: function names should be escaped by linq2db, but it is not implemented yet
		[Sql.Function("\"TestFunctionParameters\"", ServerSideOnly = true)]
		public static TestParametersResult TestParameters(int? param1, int? param2)
		{
			throw new ServerSideOnlyException(nameof(TestParameters));
		}

		[Sql.TableFunction("TestTableFunction")]
		public LinqToDB.ITable<TestScalarTableFunctionResult> TestScalarTableFunction(int? param1)
		{
			var methodInfo = typeof(TestPgFunctions).GetMethod("TestScalarTableFunction", new[] { typeof(int?) })!;

			return _ctx.GetTable<TestScalarTableFunctionResult>(this, methodInfo, param1);
		}

		[Sql.TableFunction("TestTableFunction1")]
		public LinqToDB.ITable<TestRecordTableFunctionResult> TestRecordTableFunction(int? param1, int? param2)
		{
			var methodInfo = typeof(TestPgFunctions).GetMethod("TestRecordTableFunction", new[] { typeof(int?), typeof(int?) })!;

			return _ctx.GetTable<TestRecordTableFunctionResult>(this, methodInfo, param1, param2);
		}

		[Sql.Function("\"TestScalarFunction\"", ServerSideOnly = true)]
		public static string TestScalarFunction(int? param)
		{
			throw new ServerSideOnlyException(nameof(TestScalarFunction));
		}

		[Sql.Function("\"TestSingleOutParameterFunction\"", ServerSideOnly = true)]
		public static int? TestSingleOutParameterFunction(int? param)
		{
			throw new ServerSideOnlyException(nameof(TestSingleOutParameterFunction));
		}

		[Sql.Function("jsonb_to_record", ServerSideOnly = true)]
		public static TRecord DynamicRecordFunction<TRecord>(string json)
		{
			throw new ServerSideOnlyException(nameof(DynamicRecordFunction));
		}

		[Sql.TableFunction("jsonb_to_recordset")]
		public LinqToDB.ITable<TRecord> DynamicTableFunction<TRecord>(string json)
			where TRecord : class
		{
			var methodInfo = typeof(TestPgFunctions).GetMethod("DynamicTableFunction", new [] { typeof(string) })!;

			return _ctx.GetTable<TRecord>(this, methodInfo, json);
		}

		[Sql.Function("unnest", 0, IsAggregate = true, ServerSideOnly = true, Precedence = Precedence.Primary)]
		public static T Unnest<T>(T[] array)
		{
			throw new ServerSideOnlyException(nameof(Unnest));
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
	}
	#endregion
}
