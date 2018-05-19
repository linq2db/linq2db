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
	using System.Globalization;

	using Model;

	[TestFixture]
	public class FirebirdTests : DataProviderTestBase
	{
		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
		public void TestParameters(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast(@p as int) FROM Dual",      new { p =  1  }), Is.EqualTo("1"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as char(1)) FROM Dual",  new { p = "1" }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT Cast(@p as int) FROM Dual",      new { p =  new DataParameter { Value = 1   } }), Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT Cast(@p1 as char(1)) FROM Dual", new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT Cast(@p1 as int) + Cast(@p2 as int) FROM Dual", new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
				Assert.That(conn.Execute<int>   ("SELECT Cast(@p2 as int) + Cast(@p1 as int) FROM Dual", new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
		public void TestDataTypes(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(TestType<long?>    (conn, "bigintDataType",    DataType.Int64),    Is.EqualTo(1000000L));
				Assert.That(TestType<short?>   (conn, "smallintDataType",  DataType.Int16),    Is.EqualTo(25555));
				Assert.That(TestType<decimal?> (conn, "decimalDataType",   DataType.Decimal),  Is.EqualTo(2222222));
				Assert.That(TestType<int?>     (conn, "intDataType",       DataType.Int32),    Is.EqualTo(7777777));
				Assert.That(TestType<float?>   (conn, "floatDataType",     DataType.Single),   Is.EqualTo(20.31f));
				Assert.That(TestType<double?>  (conn, "realDataType",      DataType.Double),   Is.EqualTo(16d));

				Assert.That(TestType<DateTime?>(conn, "timestampDataType", DataType.DateTime), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));

				Assert.That(TestType<string>   (conn, "charDataType",      DataType.Char),     Is.EqualTo("1"));
				Assert.That(TestType<string>   (conn, "varcharDataType",   DataType.VarChar),  Is.EqualTo("234"));
				Assert.That(TestType<string>   (conn, "textDataType",      DataType.Text),     Is.EqualTo("567"));
				Assert.That(TestType<string>   (conn, "ncharDataType",     DataType.NChar),    Is.EqualTo("23233"));
				Assert.That(TestType<string>   (conn, "nvarcharDataType",  DataType.NVarChar), Is.EqualTo("3323"));
				Assert.That(TestType<string>   (conn, "textDataType",      DataType.NText),    Is.EqualTo("567"));

				Assert.That(TestType<byte[]>   (conn, "blobDataType",      DataType.Binary),   Is.EqualTo(new byte[] { 49, 50, 51, 52, 53 }));
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
				var sqlValue = expectedValue is bool ? (bool)(object)expectedValue? 1 : 0 : (object)expectedValue;

				var sql = sqlValue == null ?
					"SELECT NULL FROM Dual" :
					string.Format(CultureInfo.InvariantCulture, "SELECT Cast({0} as {1}) FROM Dual", sqlValue, sqlType);

				Debug.WriteLine(sql + " -> " + typeof(T));

				Assert.That(conn.Execute<T>(sql), Is.EqualTo(expectedValue));
			}

			{
				var sql =
					dataType == DataType.SByte      ? "SELECT Cast(@p as smallint)    FROM Dual" :
					dataType == DataType.Int16      ? "SELECT Cast(@p as smallint)    FROM Dual" :
					dataType == DataType.Int32      ? "SELECT Cast(@p as int)         FROM Dual" :
					dataType == DataType.Int64      ? "SELECT Cast(@p as bigint)      FROM Dual" :
					dataType == DataType.Byte       ? "SELECT Cast(@p as smallint)    FROM Dual" :
					dataType == DataType.UInt16     ? "SELECT Cast(@p as int)         FROM Dual" :
					dataType == DataType.UInt32     ? "SELECT Cast(@p as bigint)      FROM Dual" :
					dataType == DataType.UInt64     ? "SELECT Cast(@p as decimal(18)) FROM Dual" :
					dataType == DataType.Single     ? "SELECT Cast(@p as float)       FROM Dual" :
					dataType == DataType.Double     ? "SELECT Cast(@p as real)        FROM Dual" :
					dataType == DataType.Decimal    ? "SELECT Cast(@p as decimal(18)) FROM Dual" :
					dataType == DataType.VarNumeric ? "SELECT Cast(@p as decimal(18)) FROM Dual" :
					dataType == DataType.Money      ? "SELECT Cast(@p as decimal(18)) FROM Dual" :
					dataType == DataType.SmallMoney ? "SELECT Cast(@p as decimal(18)) FROM Dual" :
													  "SELECT @p                      FROM Dual";

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

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
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

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
		public void TestDateTime(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12 12:12:12' as timestamp) FROM Dual"), Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12' as timestamp) FROM Dual"), Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime> ("SELECT Cast(@p as timestamp) FROM Dual", DataParameter.DateTime("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast(@p as timestamp) FROM Dual", new DataParameter("p", dateTime)),                    Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast(@p as timestamp) FROM Dual", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
		public void TestChar(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char) FROM Dual"),        Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char) FROM Dual"),        Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char(1)) FROM Dual"),     Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char(1)) FROM Dual"),     Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char(2)) FROM Dual"),     Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast('1' as varchar(1)) FROM Dual"),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar(1)) FROM Dual"),  Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as varchar(20)) FROM Dual"), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar(20)) FROM Dual"), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char) FROM Dual", DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char) FROM Dual", DataParameter.Char("p",  '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char(1)) FROM Dual", DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM Dual", DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char(1)) FROM Dual", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM Dual", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char(1)) FROM Dual", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM Dual", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char(1)) FROM Dual", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM Dual", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char(1)) FROM Dual", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM Dual", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
		public void TestString(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(5)) FROM Dual"),     Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20)) FROM Dual"),    Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345 ' as char(20)) FROM Dual"),   Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as char(20)) FROM Dual"),    Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(5)) FROM Dual"),  Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(20)) FROM Dual"), Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as varchar(20)) FROM Dual"), Is.Null);

				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM Dual", DataParameter.Char    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM Dual", DataParameter.VarChar ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM Dual", DataParameter.Text    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM Dual", DataParameter.NChar   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM Dual", DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM Dual", DataParameter.NText   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM Dual", DataParameter.Create  ("p", "123")), Is.EqualTo("123"));

				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM Dual", DataParameter.Create("p", (string)null)), Is.EqualTo(null));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM Dual", new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
		public void TestBinary(string context)
		{
			var arr1 = new byte[] { 50, 51         };
			var arr2 = new byte[] { 49, 50, 51, 52 };

			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<byte[]>("SELECT Cast('23' as blob) FROM Dual"),   Is.EqualTo(           arr1));
				Assert.That(conn.Execute<Binary>("SELECT Cast('1234' as blob) FROM Dual"), Is.EqualTo(new Binary(arr2)));

				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM Dual", DataParameter.Binary   ("p", arr1)),             Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM Dual", DataParameter.Blob     ("p", arr1)),             Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM Dual", DataParameter.VarBinary("p", arr1)),             Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM Dual", DataParameter.Create   ("p", arr1)),             Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM Dual", DataParameter.Blob     ("p", null)),             Is.EqualTo(null));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM Dual", DataParameter.VarBinary("p", null)),             Is.EqualTo(null));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM Dual", DataParameter.Binary   ("p", new byte[0])),      Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM Dual", DataParameter.Blob     ("p", new byte[0])),      Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM Dual", DataParameter.VarBinary("p", new byte[0])),      Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM Dual", DataParameter.Image    ("p", new byte[0])),      Is.EqualTo(new byte[0]));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM Dual", DataParameter.Image    ("p", arr2)),             Is.EqualTo(arr2));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM Dual", new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM Dual", DataParameter.Create   ("p", new Binary(arr1))), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM Dual", new DataParameter("p", new Binary(arr1))),       Is.EqualTo(arr1));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
		public void TestGuid(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(
					conn.Execute<Guid>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as char(38)) FROM Dual"),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				Assert.That(
					conn.Execute<Guid?>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as char(38)) FROM Dual"),
					Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

				var guid = Guid.NewGuid();

				Assert.That(conn.Execute<Guid>("SELECT Cast(@p as char(38)) FROM Dual", DataParameter.Create("p", guid)),                Is.EqualTo(guid));
				Assert.That(conn.Execute<Guid>("SELECT Cast(@p as char(38)) FROM Dual", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
		public void TestGuid2(string context)
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

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
		public void TestXml(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>     ("SELECT Cast('<xml/>' as varchar(100)) FROM Dual"),            Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast('<xml/>' as varchar(100)) FROM Dual").ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT Cast('<xml/>' as varchar(100)) FROM Dual").InnerXml,   Is.EqualTo("<xml />"));

				var xdoc = XDocument.Parse("<xml/>");
				var xml  = Convert<string,XmlDocument>.Lambda("<xml/>");

				Assert.That(conn.Execute<string>     ("SELECT Cast(@p as varchar(100)) FROM Dual", DataParameter.Xml("p", "<xml/>")),        Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast(@p as varchar(100)) FROM Dual", DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT Cast(@p as varchar(100)) FROM Dual", DataParameter.Xml("p", xml)). InnerXml,   Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast(@p as varchar(100)) FROM Dual", new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XDocument>  ("SELECT Cast(@p as varchar(100)) FROM Dual", new DataParameter("p", xml)). ToString(), Is.EqualTo("<xml />"));
			}
		}

		enum TestEnum
		{
			[MapValue("A")] AA,
			[MapValue("B")] BB,
		}

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
		public void TestEnum1(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<TestEnum> ("SELECT Cast('A' as char) FROM Dual"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum?>("SELECT Cast('A' as char) FROM Dual"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum> ("SELECT Cast('B' as char) FROM Dual"), Is.EqualTo(TestEnum.BB));
				Assert.That(conn.Execute<TestEnum?>("SELECT Cast('B' as char) FROM Dual"), Is.EqualTo(TestEnum.BB));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
		public void TestEnum2(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM Dual", new { p = TestEnum.AA }),            Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM Dual", new { p = (TestEnum?)TestEnum.BB }), Is.EqualTo("B"));

				Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM Dual", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM Dual", new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM Dual", new { p = conn.MappingSchema.GetConverter<TestEnum?,string>()(TestEnum.AA) }), Is.EqualTo("A"));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
		public void SequenceInsert(string context)
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

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
		public void SequenceInsertWithIdentity(string context)
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

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
		public void DataProviderTest(string context)
		{
			using (var con = new FbConnection(DataConnection.GetConnectionString(context)))
			using (var dbm = new DataConnection(new FirebirdDataProvider(), con))
			{
				dbm.GetTable<AllTypes>().Where(t => t.timestampDataType == DateTime.Now).ToList();
			}
		}

		[Table("LINQDATATYPES")]
		class MyLinqDataType
		{
			[Column]
			public byte[] BinaryValue { get; set; }
		}

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
		public void ForcedInlineParametersInSelectClauseTest(string context)
		{
			using (var db = GetDataContext(context))
			{
				Assert.AreEqual(10, db.Select(() => Sql.AsSql(10))); // if 10 is not inlined, when FB raise "unknown data type error"

				var blob = new byte[] {1, 2, 3};
				db.GetTable<MyLinqDataType>().Any(x => x.BinaryValue == blob); // if blob is inlined - FB raise error(blob can not be sql literal)
			}
		}

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
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

		[Table]
		public class Issue76Entity
		{
			[Column] public long   Id        { get; set; } 
			[Column] public string Caption   { get; set; } 
			[Column] public long?  ParentId  { get; set; } 

					 public bool HasChildren { get; set; }
		}

		[Test, IncludeDataContextSource(ProviderName.Firebird, TestProvName.Firebird3)]
		public void Issue76(string context)
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
		[Combinatorial]
		public void DropTableTest(
			[IncludeDataSources(ProviderName.Firebird, TestProvName.Firebird3)] string context,
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
				{
					// first drop deletes table if it remains from previous test run
					// second drop deletes non-existing table
					db.DropTable<TTable>(throwExceptionIfNotExists: throwIfNotExists);
					db.DropTable<TTable>(throwExceptionIfNotExists: throwIfNotExists);
					db.CreateTable<TTable>();
					db.DropTable<TTable>(throwExceptionIfNotExists: throwIfNotExists);
				}
			}
		}
	}
}
