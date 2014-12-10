using System;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using NpgsqlTypes;
using Tests.Model;

namespace Tests.DataProvider
{
	[TestFixture]
	public class PostgreSQLTest : DataProviderTestBase
	{
		public PostgreSQLTest()
		{
			PassNullSql  = "SELECT ID FROM AllTypes WHERE :p IS NULL AND {0} IS NULL OR :p IS NOT NULL AND {0} = :p";
			PassValueSql = "SELECT ID FROM AllTypes WHERE {0} = :p";
		}

		const string CurrentProvider = ProviderName.PostgreSQL;

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestParameters(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT :p",        new { p =  1  }), Is.EqualTo("1"));
				Assert.That(conn.Execute<string>("SELECT :p",        new { p = "1" }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT :p",        new { p =  new DataParameter { Value = 1   } }), Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT :p1",       new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT :p1 + :p2", new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
				Assert.That(conn.Execute<int>   ("SELECT :p2 + :p1", new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestDataTypes(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(TestType<long?>             (conn, "bigintDataType",      DataType.Int64),                   Is.EqualTo(1000000));
				Assert.That(TestType<decimal?>          (conn, "numericDataType",     DataType.Decimal),                 Is.EqualTo(9999999m));
				Assert.That(TestType<short?>            (conn, "smallintDataType",    DataType.Int16),                   Is.EqualTo(25555));
				Assert.That(TestType<int?>              (conn, "intDataType",         DataType.Int32),                   Is.EqualTo(7777777));
//				Assert.That(TestType<decimal?>          (conn, "moneyDataType",       DataType.Money),                   Is.EqualTo(100000m));
				Assert.That(TestType<double?>           (conn, "doubleDataType",      DataType.Double),                  Is.EqualTo(20.31d));
				Assert.That(TestType<float?>            (conn, "realDataType",        DataType.Single),                  Is.EqualTo(16.2f));

				Assert.That(TestType<NpgsqlTimeStamp?>  (conn, "timestampDataType"),                                     Is.EqualTo(new NpgsqlTimeStamp(2012, 12, 12, 12, 12, 12)));
				Assert.That(TestType<DateTime?>         (conn, "timestampDataType",   DataType.DateTime2),               Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(TestType<NpgsqlTimeStampTZ?>(conn, "timestampTZDataType"),                                   Is.EqualTo(new NpgsqlTimeStampTZ(2012, 12, 12, 11, 12, 12, new NpgsqlTimeZone(-5, 0))));
				Assert.That(TestType<DateTimeOffset?>   (conn, "timestampTZDataType", DataType.DateTimeOffset),          Is.EqualTo(new DateTimeOffset(2012, 12, 12, 11, 12, 12, new TimeSpan(-5, 0, 0))));
				Assert.That(TestType<NpgsqlDate?>       (conn, "dateDataType"),                                          Is.EqualTo(new NpgsqlDate(2012, 12, 12)));
				Assert.That(TestType<DateTime?>         (conn, "dateDataType",        DataType.Date),                    Is.EqualTo(new DateTime(2012, 12, 12)));
				Assert.That(TestType<NpgsqlTime?>       (conn, "timeDataType"),                                          Is.EqualTo(new NpgsqlTime(12, 12, 12)));
				Assert.That(TestType<NpgsqlTimeTZ?>     (conn, "timeTZDataType"),                                        Is.EqualTo(new NpgsqlTimeTZ(12, 12, 12)));
				Assert.That(TestType<NpgsqlInterval?>   (conn, "intervalDataType"),                                      Is.EqualTo(new NpgsqlInterval(1, 3, 5, 20)));

				Assert.That(TestType<char?>             (conn, "charDataType",        DataType.Char),                    Is.EqualTo('1'));
				Assert.That(TestType<string>            (conn, "charDataType",        DataType.Char),                    Is.EqualTo("1"));
				Assert.That(TestType<string>            (conn, "charDataType",        DataType.NChar),                   Is.EqualTo("1"));
				Assert.That(TestType<string>            (conn, "varcharDataType",     DataType.VarChar),                 Is.EqualTo("234"));
				Assert.That(TestType<string>            (conn, "varcharDataType",     DataType.NVarChar),                Is.EqualTo("234"));
				Assert.That(TestType<string>            (conn, "textDataType",        DataType.Text),                    Is.EqualTo("567"));

				Assert.That(TestType<byte[]>            (conn, "binaryDataType",      DataType.Binary),                  Is.EqualTo(new byte[] { 42 }));
				Assert.That(TestType<byte[]>            (conn, "binaryDataType",      DataType.VarBinary),               Is.EqualTo(new byte[] { 42 }));
				Assert.That(TestType<Binary>            (conn, "binaryDataType",      DataType.VarBinary).ToArray(),     Is.EqualTo(new byte[] { 42 }));

				Assert.That(TestType<Guid?>             (conn, "uuidDataType",        DataType.Guid),                    Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));
				Assert.That(TestType<BitString?>        (conn, "bitDataType"),                                           Is.EqualTo(new BitString(new[] { true, false, true })));
				Assert.That(TestType<bool?>             (conn, "booleanDataType",     DataType.Boolean),                 Is.EqualTo(true));
				Assert.That(TestType<string>            (conn, "colorDataType"),                                         Is.EqualTo("Green"));

				Assert.That(TestType<NpgsqlPoint?>      (conn, "pointDataType", skipNull:true, skipNotNull:true),        Is.EqualTo(new NpgsqlPoint(1, 2)));
				Assert.That(TestType<NpgsqlLSeg?>       (conn, "lsegDataType"),                                          Is.EqualTo(new NpgsqlLSeg(new NpgsqlPoint(1, 2), new NpgsqlPoint(3, 4))));
				Assert.That(TestType<NpgsqlBox?>        (conn, "boxDataType").ToString(),                                Is.EqualTo(new NpgsqlBox(new NpgsqlPoint(1, 2), new NpgsqlPoint(3, 4)).ToString()));
				Assert.That(TestType<NpgsqlPath?>       (conn, "pathDataType"),                                          Is.EqualTo(new NpgsqlPath(new[] { new NpgsqlPoint(1, 2), new NpgsqlPoint(3, 4) })));
				Assert.That(TestType<NpgsqlPolygon?>    (conn, "polygonDataType", skipNull:true, skipNotNull:true),      Is.EqualTo(new NpgsqlPolygon(new[] { new NpgsqlPoint(1, 2), new NpgsqlPoint(3, 4) })));
				Assert.That(TestType<NpgsqlCircle?>     (conn, "circleDataType"),                                        Is.EqualTo(new NpgsqlCircle(new NpgsqlPoint(1, 2), 3)));

				Assert.That(TestType<NpgsqlInet?>       (conn, "inetDataType"),                                          Is.EqualTo(new NpgsqlInet(new IPAddress(new byte[] { 192, 168, 1, 1 }))));
				Assert.That(TestType<NpgsqlMacAddress?> (conn, "macaddrDataType"),                                       Is.EqualTo(new NpgsqlMacAddress("01:02:03:04:05:06")));

				Assert.That(TestType<string>            (conn, "xmlDataType",         DataType.Xml, skipNull:true, skipNotNull:true),
					Is.EqualTo("<root><element strattr=\"strvalue\" intattr=\"12345\"/></root>"));
				Assert.That(TestType<XDocument>         (conn, "xmlDataType",         DataType.Xml, skipNull:true, skipNotNull:true).ToString(),
					Is.EqualTo(XDocument.Parse("<root><element strattr=\"strvalue\" intattr=\"12345\"/></root>").ToString()));
				Assert.That(TestType<XmlDocument>       (conn, "xmlDataType",         DataType.Xml, skipNull:true, skipNotNull:true).InnerXml,
					Is.EqualTo(ConvertTo<XmlDocument>.From("<root><element strattr=\"strvalue\" intattr=\"12345\"/></root>").InnerXml));
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

			Debug.WriteLine("{0} -> DataType.{1}",  typeof(T), dataType);
			Assert.That(conn.Execute<T>("SELECT :p", new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> auto", typeof(T));
			Assert.That(conn.Execute<T>("SELECT :p", new DataParameter { Name = "p", Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> new",  typeof(T));
			Assert.That(conn.Execute<T>("SELECT :p", new { p = expectedValue }), Is.EqualTo(expectedValue));
		}

		static void TestSimple<T>(DataConnection conn, T expectedValue, DataType dataType)
			where T : struct
		{
			TestNumeric<T> (conn, expectedValue, dataType);
			TestNumeric<T?>(conn, expectedValue, dataType);
			TestNumeric<T?>(conn, (T?)null,      dataType);
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestNumerics(string context)
		{
			using (var conn = new DataConnection(context))
			{
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

				TestNumeric(conn, sbyte.MinValue,    DataType.SByte,      "money");
				TestNumeric(conn, sbyte.MaxValue,    DataType.SByte);
				TestNumeric(conn, short.MinValue,    DataType.Int16,      "money");
				TestNumeric(conn, short.MaxValue,    DataType.Int16);
				TestNumeric(conn, int.MinValue,      DataType.Int32,      "money smallint");
				TestNumeric(conn, int.MaxValue,      DataType.Int32,      "smallint real");
				TestNumeric(conn, long.MinValue,     DataType.Int64,      "int money smallint");
				TestNumeric(conn, long.MaxValue,     DataType.Int64,      "int money smallint float real");

				TestNumeric(conn, byte.MaxValue,     DataType.Byte);
				TestNumeric(conn, ushort.MaxValue,   DataType.UInt16,     "int smallint");
				TestNumeric(conn, uint.MaxValue,     DataType.UInt32,     "int smallint real");
				TestNumeric(conn, ulong.MaxValue,    DataType.UInt64,     "bigint int money smallint float real");

				TestNumeric(conn, -3.40282306E+38f,  DataType.Single,     "bigint int money smallint numeric numeric(38)");
				TestNumeric(conn, 3.40282306E+38f,   DataType.Single,     "bigint int money numeric numeric(38) smallint");
				TestNumeric(conn, -1.79E+308d,       DataType.Double,     "bigint int money numeric numeric(38) smallint real");
				TestNumeric(conn,  1.79E+308d,       DataType.Double,     "bigint int money numeric numeric(38) smallint real");
				TestNumeric(conn, decimal.MinValue,  DataType.Decimal,    "bigint int money numeric numeric(38) smallint float real");
				TestNumeric(conn, decimal.MaxValue,  DataType.Decimal,    "bigint int money numeric numeric(38) smallint float real");
				TestNumeric(conn, decimal.MinValue,  DataType.VarNumeric, "bigint int money numeric numeric(38) smallint float real");
				TestNumeric(conn, decimal.MaxValue,  DataType.VarNumeric, "bigint int money numeric numeric(38) smallint float real");
				TestNumeric(conn, -922337203685477m, DataType.Money,      "int money smallint real");
				TestNumeric(conn, +922337203685477m, DataType.Money,      "int smallint real");
				TestNumeric(conn, -214748m,          DataType.SmallMoney, "money smallint smallint");
				TestNumeric(conn, +214748m,          DataType.SmallMoney, "smallint");
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestDate(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12' as date)"),                          Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12' as date)"),                          Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime> ("SELECT :p", DataParameter.Date("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT :p", new DataParameter("p", dateTime, DataType.Date)), Is.EqualTo(dateTime));
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestDateTime(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12 12:12:12' as timestamp)"),                Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12' as timestamp)"),                Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime> ("SELECT :p", DataParameter.DateTime("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT :p", new DataParameter("p", dateTime)),                    Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT :p", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestChar(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char)"),         Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char)"),         Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char(1))"),      Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char(1))"),      Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast('1' as varchar)"),      Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar)"),      Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as varchar(20))"),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar(20))"),  Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT :p",                  DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p",                  DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(:p as char)",    DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(:p as char)",    DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(:p as char(1))", DataParameter.Char("@p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(:p as char(1))", DataParameter.Char("@p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT :p", DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p", DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT :p", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT :p", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT :p", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT :p", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT :p", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestString(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20))"),      Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as char(20))"),      Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(20))"),   Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as varchar(20))"),   Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as text)"),          Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as text)"),          Is.Null);

				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.Char    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.VarChar ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.Text    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.NChar   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.NText   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.Create  ("p", "123")), Is.EqualTo("123"));

				Assert.That(conn.Execute<string>("SELECT :p", DataParameter.Create("p", (string)null)), Is.EqualTo(null));
				Assert.That(conn.Execute<string>("SELECT :p", new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestBinary(string context)
		{
			var arr1 = new byte[] { 48, 57 };

			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<byte[]>("SELECT E'\\060\\071'::bytea"), Is.EqualTo(arr1));

				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Binary   ("p", arr1)),             Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", arr1)),             Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create   ("p", arr1)),             Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", null)),             Is.EqualTo(null));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", new byte[0])),      Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Image    ("p", new byte[0])),      Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create   ("p", new Binary(arr1))), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter("p", new Binary(arr1))),       Is.EqualTo(arr1));
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
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

				Assert.That(conn.Execute<Guid>("SELECT :p", DataParameter.Create("p", guid)),                Is.EqualTo(guid));
				Assert.That(conn.Execute<Guid>("SELECT :p", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestXml(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>     ("SELECT XMLPARSE (DOCUMENT'<xml/>')"),            Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>  ("SELECT XMLPARSE (DOCUMENT'<xml/>')").ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT XMLPARSE (DOCUMENT'<xml/>')").InnerXml,   Is.EqualTo("<xml />"));

				var xdoc = XDocument.Parse("<xml/>");
				var xml  = Convert<string,XmlDocument>.Lambda("<xml/>");

				Assert.That(conn.Execute<string>     ("SELECT @p", DataParameter.Xml("p", "<xml/>")),        Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>  ("SELECT @p", DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT @p", DataParameter.Xml("p", xml)). InnerXml,   Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  ("SELECT @p", new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  ("SELECT @p", new DataParameter("p", xml)). ToString(), Is.EqualTo("<xml />"));
			}
		}

		enum TestEnum
		{
			[MapValue("A")] AA,
			[MapValue("B")] BB,
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestEnum1(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<TestEnum> ("SELECT 'A'"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'A'"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum> ("SELECT 'B'"), Is.EqualTo(TestEnum.BB));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'B'"), Is.EqualTo(TestEnum.BB));
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void TestEnum2(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT @p", new { p = TestEnum.AA }),            Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = (TestEnum?)TestEnum.BB }), Is.EqualTo("B"));

				Assert.That(conn.Execute<string>("SELECT @p", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = conn.MappingSchema.GetConverter<TestEnum?,string>()(TestEnum.AA) }), Is.EqualTo("A"));
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
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

		[Test, IncludeDataContextSource(CurrentProvider)]
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

		[Test, IncludeDataContextSource(CurrentProvider)]
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

		[Test, IncludeDataContextSource(CurrentProvider)]
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

		[Test, IncludeDataContextSource(CurrentProvider)]
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

		[Test, IncludeDataContextSource(CurrentProvider)]
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

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void SequenceInsertWithIdentity4(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<PostgreSQLSpecific.TestSchemaIdentity>().Delete();

				var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.TestSchemaIdentity { }));
				var id2 = db.GetTable<PostgreSQLSpecific.TestSchemaIdentity>().Single().ID;

				Assert.AreEqual(id1, id2);

				db.GetTable<PostgreSQLSpecific.TestSchemaIdentity>().Delete();
			}
		}

		[Test, IncludeDataContextSource(CurrentProvider)]
		public void SequenceInsertWithIdentity5(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<PostgreSQLSpecific.TestSerialIdentity>().Delete();

				var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.TestSerialIdentity { }));
				var id2 = db.GetTable<PostgreSQLSpecific.TestSerialIdentity>().Single().ID;

				Assert.AreEqual(id1, id2);

				db.GetTable<PostgreSQLSpecific.TestSerialIdentity>().Delete();
			}
		}
	}
}
