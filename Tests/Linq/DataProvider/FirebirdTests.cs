using System;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using FirebirdSql.Data.FirebirdClient;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.DataProvider
{
	using System.Collections.Generic;
	using System.Data;
	using System.Globalization;
	using System.Linq.Expressions;
	using System.Numerics;
	using System.Threading.Tasks;
	using FirebirdSql.Data.Types;
	using LinqToDB.Linq;
	using LinqToDB.SchemaProvider;
	using Model;

	[TestFixture]
	public class FirebirdTests : DataProviderTestBase
	{
		[Test]
		public void TestParameters([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast(@p as int) FROM \"Dual\"",      new { p =  1  }), Is.EqualTo("1"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as char(1)) FROM \"Dual\"",  new { p = "1" }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT Cast(@p as int) FROM \"Dual\"",      new { p =  new DataParameter { Value = 1   } }), Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT Cast(@p1 as char(1)) FROM \"Dual\"", new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT Cast(@p1 as int) + Cast(@p2 as int) FROM \"Dual\"", new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
				Assert.That(conn.Execute<int>   ("SELECT Cast(@p2 as int) + Cast(@p1 as int) FROM \"Dual\"", new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
			}
		}

		[Test]
		public void TestDataTypes([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(TestType<long?>    (conn, "\"bigintDataType\"",    DataType.Int64),    Is.EqualTo(1000000L));
				Assert.That(TestType<short?>   (conn, "\"smallintDataType\"",  DataType.Int16),    Is.EqualTo(25555));
				Assert.That(TestType<decimal?> (conn, "\"decimalDataType\"",   DataType.Decimal),  Is.EqualTo(2222222));
				Assert.That(TestType<int?>     (conn, "\"intDataType\"",       DataType.Int32),    Is.EqualTo(7777777));
				Assert.That(TestType<float?>   (conn, "\"floatDataType\"",     DataType.Single),   Is.EqualTo(20.31f));
				Assert.That(TestType<double?>  (conn, "\"realDataType\"",      DataType.Double),   Is.EqualTo(16d));

				Assert.That(TestType<DateTime?>(conn, "\"timestampDataType\"", DataType.DateTime), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));

				Assert.That(TestType<string>   (conn, "\"charDataType\"",      DataType.Char),     Is.EqualTo("1"));
				Assert.That(TestType<string>   (conn, "\"varcharDataType\"",   DataType.VarChar),  Is.EqualTo("234"));
				Assert.That(TestType<string>   (conn, "\"textDataType\"",      DataType.Text),     Is.EqualTo("567"));
				Assert.That(TestType<string>   (conn, "\"ncharDataType\"",     DataType.NChar),    Is.EqualTo("23233"));
				Assert.That(TestType<string>   (conn, "\"nvarcharDataType\"",  DataType.NVarChar), Is.EqualTo("3323"));
				Assert.That(TestType<string>   (conn, "\"textDataType\"",      DataType.NText),    Is.EqualTo("567"));

				Assert.That(TestType<byte[]>   (conn, "\"blobDataType\"",      DataType.Binary),   Is.EqualTo(new byte[] { 49, 50, 51, 52, 53 }));

				if (context == TestProvName.Firebird4)
				{
					TestType<FbDecFloat?     >(conn, "\"decfloat16DataType\"" , DataType.DecFloat);
					TestType<FbDecFloat?     >(conn, "\"decfloat34DataType\"" , DataType.DecFloat);
					TestType<BigInteger?     >(conn, "\"int128DataType\""     , DataType.Int128);
					TestType<FbZonedDateTime?>(conn, "\"timestampTZDataType\"", DataType.DateTimeOffset);
					TestType<FbZonedTime?    >(conn, "\"timeTZDataType\""     , DataType.TimeTZ);
				}
			}
		}

		static void TestNumeric<T>(DataConnection conn, T expectedValue, DataType dataType, string skip = "")
		{
			var skipTypes = skip.Split(' ');

			foreach (var sqlType in new[]
				{
					"bigint",
					"smallint",
					"int",
					"decimal(18)",
					"float",
					"real"
				}.Except(skipTypes))
			{
				var sqlValue = expectedValue is bool ? (bool)(object)expectedValue? 1 : 0 : (object?)expectedValue;

				var sql = sqlValue == null ?
					"SELECT NULL FROM \"Dual\"" :
					string.Format(CultureInfo.InvariantCulture, "SELECT Cast({0} as {1}) FROM \"Dual\"", sqlValue, sqlType);

				Debug.WriteLine(sql + " -> " + typeof(T));

				Assert.That(conn.Execute<T>(sql), Is.EqualTo(expectedValue));
			}

			{
				var sql =
					dataType == DataType.SByte      ? "SELECT Cast(@p as smallint)    FROM \"Dual\"" :
					dataType == DataType.Int16      ? "SELECT Cast(@p as smallint)    FROM \"Dual\"" :
					dataType == DataType.Int32      ? "SELECT Cast(@p as int)         FROM \"Dual\"" :
					dataType == DataType.Int64      ? "SELECT Cast(@p as bigint)      FROM \"Dual\"" :
					dataType == DataType.Byte       ? "SELECT Cast(@p as smallint)    FROM \"Dual\"" :
					dataType == DataType.UInt16     ? "SELECT Cast(@p as int)         FROM \"Dual\"" :
					dataType == DataType.UInt32     ? "SELECT Cast(@p as bigint)      FROM \"Dual\"" :
					dataType == DataType.UInt64     ? "SELECT Cast(@p as decimal(18)) FROM \"Dual\"" :
					dataType == DataType.Single     ? "SELECT Cast(@p as float)       FROM \"Dual\"" :
					dataType == DataType.Double     ? "SELECT Cast(@p as real)        FROM \"Dual\"" :
					dataType == DataType.Decimal    ? "SELECT Cast(@p as decimal(18)) FROM \"Dual\"" :
					dataType == DataType.VarNumeric ? "SELECT Cast(@p as decimal(18)) FROM \"Dual\"" :
					dataType == DataType.Money      ? "SELECT Cast(@p as decimal(18)) FROM \"Dual\"" :
					dataType == DataType.SmallMoney ? "SELECT Cast(@p as decimal(18)) FROM \"Dual\"" :
													  "SELECT @p                      FROM \"Dual\"";

				Debug.WriteLine("{0} -> DataType.{1}",  typeof(T), dataType);
				var value = conn.Execute<T>(sql, new DataParameter { Name = "p", DataType = dataType, Value = expectedValue });
				if (!(value is double))
					Assert.That(value, Is.EqualTo(expectedValue));

				Debug.WriteLine("{0} -> auto", typeof(T));
				value = conn.Execute<T>(sql, new DataParameter { Name = "p", Value = expectedValue });
				if (!(value is double))
					Assert.That(value, Is.EqualTo(expectedValue));

				Debug.WriteLine("{0} -> new",  typeof(T));
				value = conn.Execute<T>(sql, new { p = expectedValue });
				if (!(value is double))
					Assert.That(value, Is.EqualTo(expectedValue));
			}
		}

		static void TestSimple<T>(DataConnection conn, T expectedValue, DataType dataType)
			where T : struct
		{
			TestNumeric<T> (conn, expectedValue, dataType);
			TestNumeric<T?>(conn, expectedValue, dataType);
			TestNumeric<T?>(conn, (T?)null,      dataType);
		}

		[Test]
		public void TestNumerics([IncludeDataSources(TestProvName.AllFirebird)] string context)
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

				TestNumeric(conn, sbyte.MinValue,   DataType.SByte);
				TestNumeric(conn, sbyte.MaxValue,   DataType.SByte);
				TestNumeric(conn, short.MinValue,   DataType.Int16);
				TestNumeric(conn, short.MaxValue,   DataType.Int16);
				TestNumeric(conn, int.MinValue,     DataType.Int32,      "smallint");
				TestNumeric(conn, int.MaxValue,     DataType.Int32,      "smallint float real");
				TestNumeric(conn, long.MinValue,    DataType.Int64,      "smallint int");
				TestNumeric(conn, long.MaxValue,    DataType.Int64,      "smallint int float real");

				TestNumeric(conn, byte.MaxValue,    DataType.Byte);
				TestNumeric(conn, ushort.MaxValue,  DataType.UInt16,     "smallint");
				TestNumeric(conn, uint.MaxValue,    DataType.UInt32,     "smallint int float real");

				TestNumeric(conn, -3.40282306E+38f, DataType.Single,     "bigint smallint int decimal(18)");
				TestNumeric(conn,  3.40282306E+38f, DataType.Single,     "bigint smallint int decimal(18)");
				TestNumeric(conn, -3.40282306E+38d, DataType.Double,     "bigint smallint int decimal(18) float real");
				TestNumeric(conn,  3.40282306E+38d, DataType.Double,     "bigint smallint int decimal(18) float real");

				const decimal decmax = 7922816251426433m;

				TestNumeric(conn, -decmax,          DataType.Decimal,    "bigint smallint int float real");
				TestNumeric(conn, +decmax,          DataType.Decimal,    "bigint smallint int float real");
				TestNumeric(conn, -decmax,          DataType.VarNumeric, "bigint smallint int float real");
				TestNumeric(conn, +decmax,          DataType.VarNumeric, "bigint smallint int float real");
				TestNumeric(conn, -9223372036854m,  DataType.Money,      "smallint int float real");
				TestNumeric(conn, +9223372036854m,  DataType.Money,      "smallint int float real");
				TestNumeric(conn, -214748m,         DataType.SmallMoney, "smallint");
				TestNumeric(conn, +214748m,         DataType.SmallMoney, "smallint");
			}
		}

		[Test]
		public void TestDateTime([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12 12:12:12' as timestamp) FROM \"Dual\""), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12' as timestamp) FROM \"Dual\""), Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime> ("SELECT Cast(@p as timestamp) FROM \"Dual\"", DataParameter.DateTime("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast(@p as timestamp) FROM \"Dual\"", new DataParameter("p", dateTime)),                    Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast(@p as timestamp) FROM \"Dual\"", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestChar([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char) FROM \"Dual\""),        Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char) FROM \"Dual\""),        Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char(1)) FROM \"Dual\""),     Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char(1)) FROM \"Dual\""),     Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char(2)) FROM \"Dual\""),     Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast('1' as varchar(1)) FROM \"Dual\""),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar(1)) FROM \"Dual\""),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as varchar(20)) FROM \"Dual\""), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar(20)) FROM \"Dual\""), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char) FROM \"Dual\"", DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char) FROM \"Dual\"", DataParameter.Char("p",  '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char(1)) FROM \"Dual\"", DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM \"Dual\"", DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char(1)) FROM \"Dual\"", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM \"Dual\"", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char(1)) FROM \"Dual\"", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM \"Dual\"", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char(1)) FROM \"Dual\"", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM \"Dual\"", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char(1)) FROM \"Dual\"", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM \"Dual\"", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
			}
		}

		[Test]
		public void TestString([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(5)) FROM \"Dual\""),     Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20)) FROM \"Dual\""),    Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345 ' as char(20)) FROM \"Dual\""),   Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as char(20)) FROM \"Dual\""),    Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(5)) FROM \"Dual\""),  Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(20)) FROM \"Dual\""), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as varchar(20)) FROM \"Dual\""), Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", DataParameter.Char    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", DataParameter.VarChar ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", DataParameter.Text    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", DataParameter.NChar   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", DataParameter.NText   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", DataParameter.Create  ("p", "123")), Is.EqualTo("123"));

				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", DataParameter.Create("p", (string?)null)), Is.EqualTo(null));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestBinary([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			var arr1 = new byte[] { 50, 51         };
			var arr2 = new byte[] { 49, 50, 51, 52 };

			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<byte[]>("SELECT Cast('23' as blob) FROM \"Dual\""),   Is.EqualTo(           arr1));
				Assert.That(conn.Execute<Binary>("SELECT Cast('1234' as blob) FROM \"Dual\""), Is.EqualTo(new Binary(arr2)));

				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Binary   ("p", arr1)),              Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Blob     ("p", arr1)),              Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.VarBinary("p", arr1)),              Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Create   ("p", arr1)),              Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Blob     ("p", null)),              Is.EqualTo(null));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.VarBinary("p", null)),              Is.EqualTo(null));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Binary   ("p", Array<byte>.Empty)), Is.EqualTo(Array<byte>.Empty));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Blob     ("p", Array<byte>.Empty)), Is.EqualTo(Array<byte>.Empty));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.VarBinary("p", Array<byte>.Empty)), Is.EqualTo(Array<byte>.Empty));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Image    ("p", Array<byte>.Empty)), Is.EqualTo(Array<byte>.Empty));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Image    ("p", arr2)),              Is.EqualTo(arr2));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", new DataParameter { Name = "p", Value = arr1 }),  Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Create   ("p", new Binary(arr1))),  Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", new DataParameter("p", new Binary(arr1))),        Is.EqualTo(arr1));
			}
		}

		[Test]
		public void TestGuid([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(
					conn.Execute<Guid>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as char(38)) FROM \"Dual\""),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				Assert.That(
					conn.Execute<Guid?>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as char(38)) FROM \"Dual\""),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				var guid = TestData.Guid1;

				Assert.That(conn.Execute<Guid>("SELECT Cast(@p as char(38)) FROM \"Dual\"", DataParameter.Create("p", guid)),                Is.EqualTo(guid));
				Assert.That(conn.Execute<Guid>("SELECT Cast(@p as char(38)) FROM \"Dual\"", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test]
		public void TestGuid2([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var conn = GetDataContext(context))
			{
				AreEqual(
					from t in      Types2 select t.GuidValue,
					from t in conn.Types2 select t.GuidValue);

				var dt = (from t in conn.Types2 select t).First();

				conn.Update(dt);
				conn.Types2.Update(
					t => t.ID == dt.ID,
					t => new LinqDataTypes2 { GuidValue = dt.GuidValue });
			}
		}

		[Test]
		public void TestXml([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>     ("SELECT Cast('<xml/>' as varchar(100)) FROM \"Dual\""),            Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast('<xml/>' as varchar(100)) FROM \"Dual\"").ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT Cast('<xml/>' as varchar(100)) FROM \"Dual\"").InnerXml,   Is.EqualTo("<xml />"));

				var xdoc = XDocument.Parse("<xml/>");
				var xml  = Convert<string,XmlDocument>.Lambda("<xml/>");

				Assert.That(conn.Execute<string>     ("SELECT Cast(@p as varchar(100)) FROM \"Dual\"", DataParameter.Xml("p", "<xml/>")),        Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast(@p as varchar(100)) FROM \"Dual\"", DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT Cast(@p as varchar(100)) FROM \"Dual\"", DataParameter.Xml("p", xml)). InnerXml,   Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast(@p as varchar(100)) FROM \"Dual\"", new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast(@p as varchar(100)) FROM \"Dual\"", new DataParameter("p", xml)). ToString(), Is.EqualTo("<xml />"));
			}
		}

		enum TestEnum
		{
			[MapValue("A")] AA,
			[MapValue("B")] BB,
		}

		[Test]
		public void TestEnum1([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<TestEnum> ("SELECT Cast('A' as char) FROM \"Dual\""), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum?>("SELECT Cast('A' as char) FROM \"Dual\""), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum> ("SELECT Cast('B' as char) FROM \"Dual\""), Is.EqualTo(TestEnum.BB));
				Assert.That(conn.Execute<TestEnum?>("SELECT Cast('B' as char) FROM \"Dual\""), Is.EqualTo(TestEnum.BB));
			}
		}

		[Test]
		public void TestEnum2([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM \"Dual\"", new { p = TestEnum.AA }),            Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM \"Dual\"", new { p = (TestEnum?)TestEnum.BB }), Is.EqualTo("B"));

				Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM \"Dual\"", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM \"Dual\"", new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM \"Dual\"", new { p = conn.MappingSchema.GetConverter<TestEnum?,string>()!(TestEnum.AA) }), Is.EqualTo("A"));
			}
		}

		[Test]
		public void SequenceInsert([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<FirebirdSpecific.SequenceTest>().Where(_ => _.Value == "SeqValue").Delete();
				db.Insert(new FirebirdSpecific.SequenceTest { Value = "SeqValue" });

				var id = db.GetTable<FirebirdSpecific.SequenceTest>().Single(_ => _.Value == "SeqValue").ID;

				db.GetTable<FirebirdSpecific.SequenceTest>().Where(_ => _.ID == id).Delete();

				Assert.AreEqual(0, db.GetTable<FirebirdSpecific.SequenceTest>().Count(_ => _.Value == "SeqValue"));
			}
		}

		[Test]
		public void SequenceInsertWithIdentity([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<FirebirdSpecific.SequenceTest>().Where(_ => _.Value == "SeqValue").Delete();

				var id1 = Convert.ToInt32(db.InsertWithIdentity(new FirebirdSpecific.SequenceTest { Value = "SeqValue" }));
				var id2 = db.GetTable<FirebirdSpecific.SequenceTest>().Single(_ => _.Value == "SeqValue").ID;

				Assert.AreEqual(id1, id2);

				db.GetTable<FirebirdSpecific.SequenceTest>().Where(_ => _.ID == id1).Delete();

				Assert.AreEqual(0, db.GetTable<FirebirdSpecific.SequenceTest>().Count(_ => _.Value == "SeqValue"));
			}
		}

		public class AllTypes
		{
			[PrimaryKey] public int      ID                { get; set; } // INTEGER
			[Column]     public DateTime timestampDataType { get; set; } // TIMESTAMP
		}

		[Test]
		public void DataProviderTest([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var con = new FbConnection(DataConnection.GetConnectionString(context)))
			using (var dbm = new DataConnection(new FirebirdDataProvider(), con))
			{
				dbm.GetTable<AllTypes>().Where(t => t.timestampDataType == TestData.DateTime).ToList();
			}
		}

		[Table("LinqDataTypes")]
		class MyLinqDataType
		{
			[Column]
			public byte[]? BinaryValue { get; set; }
		}

		[Test]
		public void ForcedInlineParametersInSelectClauseTest([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var db = GetDataContext(context))
			{
				Assert.AreEqual(10, db.Select(() => Sql.AsSql(10))); // if 10 is not inlined, when FB raise "unknown data type error"

				var blob = new byte[] {1, 2, 3};
				db.GetTable<MyLinqDataType>().Any(x => x.BinaryValue == blob); // if blob is inlined - FB raise error(blob can not be sql literal)
			}
		}

		[Test]
		public void BulkCopyLinqTypes([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using (var db = new DataConnection(context))
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
									DateTimeValue = new DateTime(2001, 1, 11, 1, 11, 21, 100),
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
		public async Task BulkCopyLinqTypesAsync([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using (var db = new DataConnection(context))
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
									DateTimeValue = new DateTime(2001, 1, 11, 1, 11, 21, 100),
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
		public class Issue76Entity
		{
			[Column] public long    Id          { get; set; }
			[Column] public string? Caption     { get; set; }
			[Column] public long?   ParentId    { get; set; }

			         public bool    HasChildren { get; set; }
		}

		[Test]
		public void Issue76([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (new FirebirdQuoteMode(FirebirdIdentifierQuoteMode.Quote))
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Issue76Entity>())
			{
				var folders = db.GetTable<Issue76Entity>().Select(f => new Issue76Entity()
				{
					Id          = f.Id,
					Caption     = f.Caption,
					HasChildren = db.GetTable<Issue76Entity>().Any(f2 => f2.ParentId == f.Id)
				});

				folders =
					from folder in folders
					join folder2 in db.GetTable<Issue76Entity>() on folder.ParentId equals folder2.Id
					where folder2.Caption == "dewde"
					select folder;


				Assert.DoesNotThrow(() => folders.ToList());
			}
		}

		[Table]
		class TestDropTable
		{
			[Column]
			public int Field;
		}

		[Table]
		class TestIdentityDropTable
		{
			[Column, Identity]
			public int Field;
		}

		[Test]
		public void DropTableTest(
			[IncludeDataSources(true, TestProvName.AllFirebird)] string context,
			[Values] FirebirdIdentifierQuoteMode quoteMode,
			[Values] bool withIdentity,
			[Values] bool throwIfNotExists)
		{
			using (new FirebirdQuoteMode(quoteMode))
			using (var db = GetDataContext(context))
			{
				if (withIdentity)
					test<TestIdentityDropTable>();
				else
					test<TestDropTable>();

				void test<TTable>()
					where TTable : notnull
				{
					// first drop deletes table if it remains from previous test run
					// second drop deletes non-existing table
					db.DropTable<TTable>(throwExceptionIfNotExists: false);

					try
					{
						db.DropTable<TTable>(throwExceptionIfNotExists: throwIfNotExists);
					}
					catch when(throwIfNotExists)
					{
					}


					db.CreateTable<TTable>();
					db.DropTable<TTable>(throwExceptionIfNotExists: throwIfNotExists);
				}
			}
		}

		[Table("CamelCaseName")]
		public partial class CamelCaseName
		{
			[Column("Id"), PrimaryKey] public int     Id    { get; set; }
			[Column                  ] public string? NAME1 { get; set; }
			[Column("Name2")         ] public string? Name2 { get; set; }
			[Column                  ] public string? NAME3 { get; set; }
			[Column("_NAME4")        ] public string? NAME4 { get; set; }
			[Column("NAME 5")        ] public string? NAME5 { get; set; }
		}

		[Test]
		public void TestNamesEscaping(
			[IncludeDataSources(TestProvName.AllFirebird)] string context,
			[Values(FirebirdIdentifierQuoteMode.Auto, FirebirdIdentifierQuoteMode.Quote)] FirebirdIdentifierQuoteMode quoteMode)
		{
			// TODO: quote mode is another candidate for query caching key
			Query.ClearCaches();
			using (new FirebirdQuoteMode(quoteMode))
			using (var db = GetDataContext(context))
			{
				try
				{
					db.GetTable<CamelCaseName>()
						.Insert(() => new CamelCaseName()
						{
							Id = 1,
							NAME1 = "name1",
							Name2 = "name2",
							NAME3 = "name3",
							NAME4 = "name4",
							NAME5 = "name5",
						});

					var result = db.GetTable<CamelCaseName>().ToList();
					Assert.AreEqual(1, result.Count);
					Assert.AreEqual(1, result[0].Id);
					Assert.AreEqual("name1", result[0].NAME1);
					Assert.AreEqual("name2", result[0].Name2);
					Assert.AreEqual("name3", result[0].NAME3);
					Assert.AreEqual("name4", result[0].NAME4);
					Assert.AreEqual("name5", result[0].NAME5);
				}
				finally
				{
					db.GetTable<CamelCaseName>().Delete();
					Query.ClearCaches();
				}
			}
		}

		[Test]
		public void TestProcedureNonLatinParameters1([IncludeDataSources(false, TestProvName.AllFirebird)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				int? id = null;
				try
				{
					var id1 = db.PersonInsert("Имя", "Фамилия", "Отчество", 'M', out id).ToList();
					Assert.AreEqual(1, id1.Count);
					Assert.IsNotNull(id1[0].PersonID);

					// TODO: see TestProcedureNonLatinParameters2
					// output parameter value is not set
					id = id1[0].PersonID;

					var record = db.Person.Single(p => p.ID == id);

					Assert.AreEqual("Имя", record.FirstName);
					Assert.AreEqual("Фамилия", record.LastName);
					Assert.AreEqual("Отчество", record.MiddleName);
					Assert.AreEqual(Gender.Male, record.Gender);
					Assert.IsNotNull(id);
					Assert.AreEqual(id, id1[0].PersonID);
				}
				finally
				{
					if (id != null)
						db.Person.Delete(p => p.ID == id);
				}
			}
		}

		[ActiveIssue(Details = "Output parameter not set")]
		[Test]
		public void TestProcedureNonLatinParameters2([IncludeDataSources(false, TestProvName.AllFirebird)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				int? id = null;
				try
				{
					var id1 = db.PersonInsert("Имя", "Фамилия", "Отчество", 'M', out id).ToList();
					Assert.AreEqual(1, id1.Count);
					Assert.IsNotNull(id1[0].PersonID);

					var record = db.Person.Single(p => p.ID == id);

					Assert.AreEqual("Имя", record.FirstName);
					Assert.AreEqual("Фамилия", record.LastName);
					Assert.AreEqual("Отчество", record.MiddleName);
					Assert.AreEqual(Gender.Male, record.Gender);
					Assert.IsNotNull(id);
					Assert.AreEqual(id, id1[0].PersonID);
				}
				finally
				{
					if (id != null)
						db.Person.Delete(p => p.ID == id);
				}
			}
		}

		#region issue 2445
		[Table]
		public class Card
		{
			[Column] public int     Id       { get; set; }
			[Column] public string? CardName { get; set; }
			[Column] public int     OwnerId  { get; set; }

			[Association(QueryExpressionMethod = nameof(Card_Owner), CanBeNull = true)]
			public Client Owner { get; set; } = null!;

			public static Expression<Func<Card, IDataContext, IQueryable<Client>>> Card_Owner()
			{
				return (c, db) => db.GetTable<Client>().Where(cl => cl.Id == c.OwnerId);
			}
		}

		[Table]
		public class Client
		{
			[Column] public int     Id   { get; set; }
			[Column] public string? Name { get; set; }

			[ExpressionMethod(nameof(Client_CountOfTCards), IsColumn = true)]
			public int CountOfTCards { get; set; }

			public static Expression<Func<Client, IDataContext, int>> Client_CountOfTCards()
			{
				return (cl, db) => db.GetTable<Card>().Where(t => t.OwnerId == cl.Id).Count();
			}
		}

		[Test]
		public void Issue2445Test(
			[IncludeDataSources(false, TestProvName.AllFirebird)] string context,
			[Values] FirebirdIdentifierQuoteMode quoteMode)
		{
			Query.ClearCaches();
			using (new FirebirdQuoteMode(quoteMode))
			using (var db      = new TestDataConnection(context))
			using (var cards   = db.CreateLocalTable<Card>())
			using (var clients = db.CreateLocalTable<Client>())
			{
				cards.LoadWith(x => x.Owner).ToList();

				var sql = db.LastQuery!;

				if (quoteMode == FirebirdIdentifierQuoteMode.None)
				{
					Assert.True(sql.Contains(") a_Owner ON")); // subquery alias
					Assert.True(sql.Contains("Client cl")); // table alias
					Assert.True(sql.Contains(") as CountOfTCards")); // column alias
				}
				else
				{
					Assert.True(sql.Contains(") \"a_Owner\" ON")); // subquery alias
					Assert.True(sql.Contains("\"Client\" \"cl\"")); // table alias
					Assert.True(sql.Contains(") as \"CountOfTCards\"")); // column alias
				}
			}
			Query.ClearCaches();
		}
		#endregion

		[Table]
		public partial class TestFbTypesTable
		{
			[Column("Id"), PrimaryKey] public int              Id         { get; set; }
			[Column(Precision = 16)  ] public FbDecFloat?      DecFloat16 { get; set; }
			[Column(Precision = 30)  ] public FbDecFloat?      DecFloat30 { get; set; }
			[Column(Precision = 34)  ] public FbDecFloat?      DecFloat34 { get; set; }
			[Column                  ] public FbDecFloat?      DecFloat   { get; set; }
			[Column                  ] public FbZonedDateTime? DateTimeTZ { get; set; }
			[Column                  ] public FbZonedTime?     TimeTZ     { get; set; }
			[Column                  ] public BigInteger?      Int128     { get; set; }

			public static TestFbTypesTable[] Data = new []
			{
				new TestFbTypesTable()
				{
					Id         = 1,
					DecFloat16 = new FbDecFloat(1234567890123456, 5),
					DecFloat30 = new FbDecFloat(BigInteger.Parse("1234567890123456789012345678901234"), 15),
					DecFloat34 = new FbDecFloat(BigInteger.Parse("1234567890123456789012345678901235"), 15),
					DecFloat   = new FbDecFloat(BigInteger.Parse("1234567890123456789012345678901236"), 15),
					DateTimeTZ = new FbZonedDateTime(TestData.DateTimeUtc, "UTC"),
					TimeTZ     = new FbZonedTime(TestData.TimeOfDay, "UTC"),
					Int128     = BigInteger.Parse("-170141183460469231731687303715884105728"),
				}
			};
		}

		[Test]
		public void TestFb4TypesProcedureSchema([IncludeDataSources(false, TestProvName.Firebird4)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (var t = db.CreateLocalTable(TestFbTypesTable.Data))
			{
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions()
				{
					GetTables     = false,
					GetProcedures = true,
					LoadProcedure = t => t.ProcedureName == "TEST_V4_TYPES"
				});

				var proc = schema.Procedures.Where(t => t.ProcedureName == "TEST_V4_TYPES").SingleOrDefault()!;

				Assert.IsNotNull(proc);
				Assert.AreEqual(5, proc.Parameters.Count);
				Assert.IsNotNull(proc.ResultTable);
				Assert.AreEqual(5, proc.ResultTable!.Columns.Count);

				AssertParameter("DECFLOAT16" , "decfloat"                , DataType.DecFloat      , typeof(FbDecFloat)     , 16  , "FbDecFloat"     );
				AssertParameter("DECFLOAT34" , "decfloat"                , DataType.DecFloat      , typeof(FbDecFloat)     , 34  , "FbDecFloat"     );
				AssertParameter("TSTZ"       , "timestamp with time zone", DataType.DateTimeOffset, typeof(FbZonedDateTime), null, "FbZonedDateTime");
				AssertParameter("TTZ"        , "time with time zone"     , DataType.TimeTZ        , typeof(FbZonedTime)    , null, "FbZonedTime"    );
				AssertParameter("INT_128"    , "int128"                  , DataType.Int128        , typeof(BigInteger)     , null, null             );

				AssertColumn("COL_DECFLOAT16" , "decfloat"                , DataType.DecFloat      , typeof(FbDecFloat)     , 16  , "FbDecFloat"     );
				AssertColumn("COL_DECFLOAT34" , "decfloat"                , DataType.DecFloat      , typeof(FbDecFloat)     , null, "FbDecFloat"     );
				AssertColumn("COL_TSTZ"       , "timestamp with time zone", DataType.DateTimeOffset, typeof(FbZonedDateTime), null, "FbZonedDateTime");
				AssertColumn("COL_TTZ"        , "time with time zone"     , DataType.TimeTZ        , typeof(FbZonedTime)    , null, "FbZonedTime"    );
				AssertColumn("COL_INT_128"    , "int128"                  , DataType.Int128        , typeof(BigInteger)     , null, null             );

				void AssertColumn(string name, string dbType, DataType dataType, Type type, int? precision, string? providerSpecificType)
				{
					var column = proc.ResultTable!.Columns.Where(c => c.ColumnName == name).SingleOrDefault()!;
					Assert.IsNotNull(column);
					Assert.AreEqual(dbType              , column.ColumnType);
					Assert.AreEqual(dataType            , column.DataType);
					Assert.AreEqual(providerSpecificType, column.ProviderSpecificType);
					Assert.AreEqual(type                , column.SystemType);
					Assert.AreEqual(type == typeof(object) ? "object" : type.Name + "?", column.MemberType);
					Assert.AreEqual(precision           , column.Precision);
				}

				void AssertParameter(string name, string dbType, DataType dataType, Type type, int? precision, string? providerSpecificType)
				{
					var parameter = proc!.Parameters.Where(c => c.ParameterName == name).SingleOrDefault()!;
					Assert.IsNotNull(parameter);
					Assert.AreEqual(dbType              , parameter.SchemaType);
					Assert.AreEqual(dataType            , parameter.DataType);
					Assert.AreEqual(providerSpecificType, parameter.ProviderSpecificType);
					Assert.AreEqual(type                , parameter.SystemType);
					Assert.AreEqual(type == typeof(object) ? "object" : type.Name + "?", parameter.ParameterType);
				}
			}
		}

		[Test]
		public void TestFb4TypesCreateTable([IncludeDataSources(false, TestProvName.Firebird4)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (var t = db.CreateTempTable<TestFbTypesTable>())
			{
				var sql = db.LastQuery!;

				// create table
				Assert.True(sql.Contains("\"Id\"         Int                      NOT NULL,"));
				Assert.True(sql.Contains("\"DecFloat16\" DECFLOAT(16),"));
				Assert.True(sql.Contains("\"DecFloat30\" DECFLOAT,"));
				Assert.True(sql.Contains("\"DecFloat34\" DECFLOAT,"));
				Assert.True(sql.Contains("\"DecFloat\"   DECFLOAT,"));
				Assert.True(sql.Contains("\"DateTimeTZ\" TIMESTAMP WITH TIME ZONE,"));
				Assert.True(sql.Contains("\"TimeTZ\"     TIME WITH TIME ZONE,"));
				Assert.True(sql.Contains("\"Int128\"     INT128,"));
			}
		}

		[Test]
		public void TestFb4TypesParametersAndLiterals(
			[IncludeDataSources(false, TestProvName.Firebird4)] string context,
			[Values] bool inline)
		{
			using (var db = new TestDataConnection(context))
			using (var t = db.CreateLocalTable(TestFbTypesTable.Data))
			{
				db.InlineParameters = inline;

				var sql = db.LastQuery!;

				Assert.AreEqual(1, t.Where(_ => _.DecFloat16 == TestFbTypesTable.Data[0].DecFloat16).Count());
				Assert.AreEqual(true, db.LastQuery!.Contains("@"));
				Assert.AreEqual(1, t.Where(_ => _.DecFloat30 == TestFbTypesTable.Data[0].DecFloat30).Count());
				Assert.AreEqual(true, db.LastQuery!.Contains("@"));
				Assert.AreEqual(1, t.Where(_ => _.DecFloat34 == TestFbTypesTable.Data[0].DecFloat34).Count());
				Assert.AreEqual(true, db.LastQuery!.Contains("@"));
				Assert.AreEqual(1, t.Where(_ => _.DecFloat == TestFbTypesTable.Data[0].DecFloat).Count());
				Assert.AreEqual(true, db.LastQuery!.Contains("@"));
				Assert.AreEqual(1, t.Where(_ => _.DateTimeTZ == TestFbTypesTable.Data[0].DateTimeTZ).Count());
				Assert.AreEqual(true, db.LastQuery!.Contains("@"));
				Assert.AreEqual(1, t.Where(_ => _.TimeTZ == TestFbTypesTable.Data[0].TimeTZ).Count());
				Assert.AreEqual(true, db.LastQuery!.Contains("@"));
				Assert.AreEqual(1, t.Where(_ => _.Int128 == TestFbTypesTable.Data[0].Int128).Count());
				Assert.AreEqual(!inline, db.LastQuery!.Contains("@"));
			}
		}

		public class FB4LiteralTestCase
		{
			private readonly string _caseName;

			public FB4LiteralTestCase(BigInteger int127, string literal)
			{
				Int128  = int127;
				Literal = literal;

				_caseName = $"INT128: {literal}";
			}

			public BigInteger?      Int128     { get; }
			public string           Literal    { get; }

			public override string ToString() => _caseName;
		}

		// we don't generate literals for Fb* types due:
		// - DECFLOAT: lack of support for special values (INF, (s)NaN)
		// - time-zoned types: could require datetime/time conversion to timezone which is:
		//   - extra work
		//   - could fail if timezone not known to runtime
		public static readonly IEnumerable<FB4LiteralTestCase> FB4LiteralTestCases
			= new []
			{
				// INT128
				new FB4LiteralTestCase(BigInteger.Parse("-170141183460469231731687303715884105728"), "= -170141183460469231731687303715884105728"),
				new FB4LiteralTestCase(BigInteger.Parse("170141183460469231731687303715884105727"), "= 170141183460469231731687303715884105727"),
				new FB4LiteralTestCase(BigInteger.Parse("0"), "= 0"),
				new FB4LiteralTestCase(BigInteger.Parse("1"), "= 1"),
				new FB4LiteralTestCase(BigInteger.Parse("-1"), "= -1"),
			};

		[Test]
		public void TestFb4TypesLiterals(
			[IncludeDataSources(false, TestProvName.Firebird4)] string context,
			[ValueSource(nameof(FB4LiteralTestCases))] FB4LiteralTestCase testCase)
		{
			using (var db = new TestDataConnection(context))
			using (var t = db.CreateLocalTable<TestFbTypesTable>())
			{
				t.Insert(() => new TestFbTypesTable()
				{
					Id         = 1,
					Int128     = testCase.Int128,
				});

				db.InlineParameters = true;

				Assert.AreEqual(1, t.Where(_ => _.Int128 == testCase.Int128).Count());

				Assert.True(db.LastQuery!.Contains(testCase.Literal));
			}
		}

		[Test]
		public void TestFb4TypesTableSchema([IncludeDataSources(false, TestProvName.Firebird4)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (var t = db.CreateLocalTable(TestFbTypesTable.Data))
			{
				// schema
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions()
				{
					GetTables     = true,
					GetProcedures = false,
					LoadTable     = t => t.Name == nameof(TestFbTypesTable)
				});

				var table = schema.Tables.Where(t => t.TableName == nameof(TestFbTypesTable)).SingleOrDefault()!;

				Assert.IsNotNull(table);
				Assert.AreEqual(8, table.Columns.Count);

				AssertColumn(nameof(TestFbTypesTable.DecFloat16), "decfloat"                , DataType.DecFloat      , typeof(FbDecFloat)     , 16  , "FbDecFloat"     );
				AssertColumn(nameof(TestFbTypesTable.DecFloat30), "decfloat"                , DataType.DecFloat      , typeof(FbDecFloat)     , 34  , "FbDecFloat"     );
				AssertColumn(nameof(TestFbTypesTable.DecFloat34), "decfloat"                , DataType.DecFloat      , typeof(FbDecFloat)     , 34  , "FbDecFloat"     );
				AssertColumn(nameof(TestFbTypesTable.DecFloat)  , "decfloat"                , DataType.DecFloat      , typeof(FbDecFloat)     , 34  , "FbDecFloat"     );
				AssertColumn(nameof(TestFbTypesTable.DateTimeTZ), "timestamp with time zone", DataType.DateTimeOffset, typeof(FbZonedDateTime), null, "FbZonedDateTime");
				AssertColumn(nameof(TestFbTypesTable.TimeTZ)    , "time with time zone"     , DataType.TimeTZ        , typeof(FbZonedTime)    , null, "FbZonedTime"    );
				AssertColumn(nameof(TestFbTypesTable.Int128)    , "int128"                  , DataType.Int128        , typeof(BigInteger)     , null, null             );

				void AssertColumn(string name, string dbType, DataType dataType, Type type, int? precision, string? providerSpecificType)
				{
					var column = table.Columns.Where(c => c.ColumnName == name).SingleOrDefault()!;

					Assert.IsNotNull(column);
					Assert.AreEqual(dbType              , column.ColumnType);
					Assert.AreEqual(dataType            , column.DataType);
					Assert.AreEqual(providerSpecificType, column.ProviderSpecificType);
					Assert.AreEqual(type                , column.SystemType);
					Assert.AreEqual(type == typeof(object) ? "object" : type.Name + "?"     , column.MemberType);
					Assert.AreEqual(precision           , column.Precision);
				}
			}
		}

		[Test]
		public void TestFb4TypesProcedure([IncludeDataSources(false, TestProvName.Firebird4)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var tstz       = new FbZonedDateTime(TestData.DateTime4Utc, "UTC");
				var ttz        = new FbZonedTime(TestData.TimeOfDay4, "UTC");
				var decfloat16 = new FbDecFloat(1234567890123456, 5);
				var decfloat34 = new FbDecFloat(BigInteger.Parse("1234567890123456789012345678901235"), 15);
				var int128     = BigInteger.Parse("-170141183460469231731687303715884105728");

				var res = db.TestV4Types(tstz, ttz, decfloat16, decfloat34, int128).ToList();

				Assert.AreEqual(1         , res.Count);

				Assert.AreEqual(tstz      , res[0].COL_TSTZ);
				Assert.AreEqual(ttz       , res[0].COL_TTZ);
				Assert.AreEqual(decfloat16, res[0].COL_DECFLOAT16);
				Assert.AreEqual(decfloat34, res[0].COL_DECFLOAT34);
				Assert.AreEqual(int128    , res[0].COL_INT_128);
			}
		}
	}

	static class FirebirdProcedures
	{
		public partial class PersonInsertResult
		{
			public int? PersonID { get; set; }
		}

		public static IEnumerable<PersonInsertResult> PersonInsert(this DataConnection dataConnection, string? FIRSTNAME, string? LASTNAME, string? MIDDLENAME, char? GENDER, out int? PERSONID)
		{
			var ret = dataConnection.QueryProc<PersonInsertResult>("\"Person_Insert\"",
				new DataParameter("FIRSTNAME" , FIRSTNAME , DataType.NVarChar),
				new DataParameter("LASTNAME"  , LASTNAME  , DataType.NVarChar),
				new DataParameter("MIDDLENAME", MIDDLENAME, DataType.NVarChar),
				new DataParameter("GENDER"    , GENDER    , DataType.NChar),
				new DataParameter("PERSONID"  , null      , DataType.Int32) { Direction = ParameterDirection.Output, Size = 4 }).ToList();

			PERSONID = Converter.ChangeTypeTo<int?>(((IDbDataParameter)dataConnection.Command.Parameters["PERSONID"]).Value);

			return ret;
		}

		public static IEnumerable<TestV4TYPESResult> TestV4Types(this DataConnection dataConnection, FbZonedDateTime? TSTZ, FbZonedTime? TTZ, FbDecFloat? DECFLOAT16, FbDecFloat? DECFLOAT34, BigInteger? INT_128)
		{
			return dataConnection.QueryProc<TestV4TYPESResult>("TEST_V4_TYPES",
				new DataParameter("TSTZ",       TSTZ,       DataType.DateTimeOffset),
				new DataParameter("TTZ",        TTZ,        DataType.TimeTZ),
				new DataParameter("DECFLOAT16", DECFLOAT16, DataType.DecFloat),
				new DataParameter("DECFLOAT34", DECFLOAT34, DataType.DecFloat),
				new DataParameter("INT_128",    INT_128,    DataType.Int128));
		}

		public partial class TestV4TYPESResult
		{
			public FbZonedDateTime? COL_TSTZ       { get; set; }
			public FbZonedTime?     COL_TTZ        { get; set; }
			public FbDecFloat?      COL_DECFLOAT16 { get; set; }
			public FbDecFloat?      COL_DECFLOAT34 { get; set; }
			public BigInteger?      COL_INT_128    { get; set; }
		}
	}
}
