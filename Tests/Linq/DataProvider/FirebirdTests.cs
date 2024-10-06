using System;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;

using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Types;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.Mapping;
using LinqToDB.Linq;
using LinqToDB.SchemaProvider;

using NUnit.Framework;

namespace Tests.DataProvider
{
	using LinqToDB.Expressions;
	using Model;

	[TestFixture]
	public class FirebirdTests : DataProviderTestBase
	{
		[Test]
		public void TestParameters([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT Cast(@p as int) FROM \"Dual\"", new { p = 1 }), Is.EqualTo("1"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as char(1)) FROM \"Dual\"", new { p = "1" }), Is.EqualTo("1"));
					Assert.That(conn.Execute<int>("SELECT Cast(@p as int) FROM \"Dual\"", new { p = new DataParameter { Value = 1 } }), Is.EqualTo(1));
					Assert.That(conn.Execute<string>("SELECT Cast(@p1 as char(1)) FROM \"Dual\"", new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
					Assert.That(conn.Execute<int>("SELECT Cast(@p1 as int) + Cast(@p2 as int) FROM \"Dual\"", new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
					Assert.That(conn.Execute<int>("SELECT Cast(@p2 as int) + Cast(@p1 as int) FROM \"Dual\"", new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
				});
			}
		}

		[Test]
		public void TestDataTypes([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(TestType<long?>(conn, "\"bigintDataType\"", DataType.Int64), Is.EqualTo(1000000L));
					Assert.That(TestType<short?>(conn, "\"smallintDataType\"", DataType.Int16), Is.EqualTo(25555));
					Assert.That(TestType<decimal?>(conn, "\"decimalDataType\"", DataType.Decimal), Is.EqualTo(2222222));
					Assert.That(TestType<int?>(conn, "\"intDataType\"", DataType.Int32), Is.EqualTo(7777777));
					Assert.That(TestType<float?>(conn, "\"floatDataType\"", DataType.Single), Is.EqualTo(20.31f));
					Assert.That(TestType<double?>(conn, "\"realDataType\"", DataType.Double), Is.EqualTo(16d));

					Assert.That(TestType<DateTime?>(conn, "\"timestampDataType\"", DataType.DateTime), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));

					Assert.That(TestType<string>(conn, "\"charDataType\"", DataType.Char), Is.EqualTo("1"));
					Assert.That(TestType<string>(conn, "\"varcharDataType\"", DataType.VarChar), Is.EqualTo("234"));
					Assert.That(TestType<string>(conn, "\"textDataType\"", DataType.Text), Is.EqualTo("567"));
					Assert.That(TestType<string>(conn, "\"ncharDataType\"", DataType.NChar), Is.EqualTo("23233"));
					Assert.That(TestType<string>(conn, "\"nvarcharDataType\"", DataType.NVarChar), Is.EqualTo("3323"));
					Assert.That(TestType<string>(conn, "\"textDataType\"", DataType.NText), Is.EqualTo("567"));

					Assert.That(TestType<byte[]>(conn, "\"blobDataType\"", DataType.Binary), Is.EqualTo(new byte[] { 49, 50, 51, 52, 53 }));
				});

				if (context.IsAnyOf(TestProvName.AllFirebird4Plus))
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
			using (var conn = GetDataConnection(context))
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
			using (var conn = GetDataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<DateTime>("SELECT Cast('2012-12-12 12:12:12' as timestamp) FROM \"Dual\""), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12' as timestamp) FROM \"Dual\""), Is.EqualTo(dateTime));

					Assert.That(conn.Execute<DateTime>("SELECT Cast(@p as timestamp) FROM \"Dual\"", DataParameter.DateTime("p", dateTime)), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>("SELECT Cast(@p as timestamp) FROM \"Dual\"", new DataParameter("p", dateTime)), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>("SELECT Cast(@p as timestamp) FROM \"Dual\"", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
				});
			}
		}

		[Test]
		public void TestChar([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<char>("SELECT Cast('1' as char) FROM \"Dual\""), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast('1' as char) FROM \"Dual\""), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast('1' as char(1)) FROM \"Dual\""), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast('1' as char(1)) FROM \"Dual\""), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast('1' as char(2)) FROM \"Dual\""), Is.EqualTo('1'));

					Assert.That(conn.Execute<char>("SELECT Cast('1' as varchar(1)) FROM \"Dual\""), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar(1)) FROM \"Dual\""), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast('1' as varchar(20)) FROM \"Dual\""), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast('1' as varchar(20)) FROM \"Dual\""), Is.EqualTo('1'));

					Assert.That(conn.Execute<char>("SELECT Cast(@p as char) FROM \"Dual\"", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as char) FROM \"Dual\"", DataParameter.Char("p", '1')), Is.EqualTo('1'));

					Assert.That(conn.Execute<char>("SELECT Cast(@p as char(1)) FROM \"Dual\"", DataParameter.VarChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM \"Dual\"", DataParameter.VarChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast(@p as char(1)) FROM \"Dual\"", DataParameter.NChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM \"Dual\"", DataParameter.NChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast(@p as char(1)) FROM \"Dual\"", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM \"Dual\"", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast(@p as char(1)) FROM \"Dual\"", DataParameter.Create("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM \"Dual\"", DataParameter.Create("p", '1')), Is.EqualTo('1'));

					Assert.That(conn.Execute<char>("SELECT Cast(@p as char(1)) FROM \"Dual\"", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1)) FROM \"Dual\"", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				});
			}
		}

		[Test]
		public void TestString([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(5)) FROM \"Dual\""), Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20)) FROM \"Dual\""), Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast('12345 ' as char(20)) FROM \"Dual\""), Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast(NULL    as char(20)) FROM \"Dual\""), Is.Null);

					Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(5)) FROM \"Dual\""), Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast('12345' as varchar(20)) FROM \"Dual\""), Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast(NULL    as varchar(20)) FROM \"Dual\""), Is.Null);

					Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", DataParameter.Char("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", DataParameter.VarChar("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", DataParameter.Text("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", DataParameter.NChar("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", DataParameter.NText("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", DataParameter.Create("p", "123")), Is.EqualTo("123"));

					Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", DataParameter.Create("p", (string?)null)), Is.EqualTo(null));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(3)) FROM \"Dual\"", new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
				});
			}
		}

		[Test]
		public void TestBinary([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			var arr1 = new byte[] { 50, 51         };
			var arr2 = new byte[] { 49, 50, 51, 52 };

			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<byte[]>("SELECT Cast('23' as blob) FROM \"Dual\""), Is.EqualTo(arr1));
					Assert.That(conn.Execute<Binary>("SELECT Cast('1234' as blob) FROM \"Dual\""), Is.EqualTo(new Binary(arr2)));

					Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Binary("p", arr1)), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Blob("p", arr1)), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.VarBinary("p", arr1)), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Create("p", arr1)), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Blob("p", null)), Is.EqualTo(null));
					Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.VarBinary("p", null)), Is.EqualTo(null));
					Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Binary("p", Array.Empty<byte>())), Is.EqualTo(Array.Empty<byte>()));
					Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Blob("p", Array.Empty<byte>())), Is.EqualTo(Array.Empty<byte>()));
					Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.VarBinary("p", Array.Empty<byte>())), Is.EqualTo(Array.Empty<byte>()));
					Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Image("p", Array.Empty<byte>())), Is.EqualTo(Array.Empty<byte>()));
					Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Image("p", arr2)), Is.EqualTo(arr2));
					Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", DataParameter.Create("p", new Binary(arr1))), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>("SELECT Cast(@p as blob) FROM \"Dual\"", new DataParameter("p", new Binary(arr1))), Is.EqualTo(arr1));
				});
			}
		}

		[Test]
		public void TestGuid([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(
									conn.Execute<Guid>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as char(38)) FROM \"Dual\""),
									Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));

					Assert.That(
						conn.Execute<Guid?>("SELECT Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as char(38)) FROM \"Dual\""),
						Is.EqualTo(new Guid("6F9619FF-8B86-D011-B42D-00C04FC964FF")));
				});

				var guid = TestData.Guid1;

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<Guid>("SELECT Cast(@p as char(38)) FROM \"Dual\"", DataParameter.Create("p", guid)), Is.EqualTo(guid));
					Assert.That(conn.Execute<Guid>("SELECT Cast(@p as char(38)) FROM \"Dual\"", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
				});
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
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT Cast('<xml/>' as varchar(100)) FROM \"Dual\""), Is.EqualTo("<xml/>"));
					Assert.That(conn.Execute<XDocument>("SELECT Cast('<xml/>' as varchar(100)) FROM \"Dual\"").ToString(), Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XmlDocument>("SELECT Cast('<xml/>' as varchar(100)) FROM \"Dual\"").InnerXml, Is.EqualTo("<xml />"));
				});

				var xdoc = XDocument.Parse("<xml/>");
				var xml  = Convert<string,XmlDocument>.Lambda("<xml/>");

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT Cast(@p as varchar(100)) FROM \"Dual\"", DataParameter.Xml("p", "<xml/>")), Is.EqualTo("<xml/>"));
					Assert.That(conn.Execute<XDocument>("SELECT Cast(@p as varchar(100)) FROM \"Dual\"", DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XmlDocument>("SELECT Cast(@p as varchar(100)) FROM \"Dual\"", DataParameter.Xml("p", xml)).InnerXml, Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XDocument>("SELECT Cast(@p as varchar(100)) FROM \"Dual\"", new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XDocument>("SELECT Cast(@p as varchar(100)) FROM \"Dual\"", new DataParameter("p", xml)).ToString(), Is.EqualTo("<xml />"));
				});
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
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<TestEnum>("SELECT Cast('A' as char) FROM \"Dual\""), Is.EqualTo(TestEnum.AA));
					Assert.That(conn.Execute<TestEnum?>("SELECT Cast('A' as char) FROM \"Dual\""), Is.EqualTo(TestEnum.AA));
					Assert.That(conn.Execute<TestEnum>("SELECT Cast('B' as char) FROM \"Dual\""), Is.EqualTo(TestEnum.BB));
					Assert.That(conn.Execute<TestEnum?>("SELECT Cast('B' as char) FROM \"Dual\""), Is.EqualTo(TestEnum.BB));
				});
			}
		}

		[Test]
		public void TestEnum2([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM \"Dual\"", new { p = TestEnum.AA }), Is.EqualTo("A"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM \"Dual\"", new { p = (TestEnum?)TestEnum.BB }), Is.EqualTo("B"));

					Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM \"Dual\"", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM \"Dual\"", new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
					Assert.That(conn.Execute<string>("SELECT Cast(@p as char) FROM \"Dual\"", new { p = conn.MappingSchema.GetConverter<TestEnum?, string>()!(TestEnum.AA) }), Is.EqualTo("A"));
				});
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

				Assert.That(db.GetTable<FirebirdSpecific.SequenceTest>().Count(_ => _.Value == "SeqValue"), Is.EqualTo(0));
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

				Assert.That(id2, Is.EqualTo(id1));

				db.GetTable<FirebirdSpecific.SequenceTest>().Where(_ => _.ID == id1).Delete();

				Assert.That(db.GetTable<FirebirdSpecific.SequenceTest>().Count(_ => _.Value == "SeqValue"), Is.EqualTo(0));
			}
		}

		[Table("LinqDataTypes")]
		sealed class MyLinqDataType
		{
			[Column]
			public byte[]? BinaryValue { get; set; }
		}

		[Test]
		public void ForcedInlineParametersInSelectClauseTest([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var db = GetDataContext(context))
			{
				Assert.That(db.Select(() => Sql.AsSql(10)), Is.EqualTo(10)); // if 10 is not inlined, when FB raise "unknown data type error"

				var blob = new byte[] {1, 2, 3};
				db.GetTable<MyLinqDataType>().Any(x => x.BinaryValue == blob); // if blob is inlined - FB raise error(blob can not be sql literal)
			}
		}

		[Test]
		public void BulkCopyLinqTypes([IncludeDataSources(TestProvName.AllFirebird)] string context)
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
					ParentId    = f.ParentId,
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
		sealed class TestDropTable
		{
			[Column]
			public int Field;
		}

		[Table]
		sealed class TestIdentityDropTable
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
					Assert.That(result, Has.Count.EqualTo(1));
					Assert.Multiple(() =>
					{
						Assert.That(result[0].Id, Is.EqualTo(1));
						Assert.That(result[0].NAME1, Is.EqualTo("name1"));
						Assert.That(result[0].Name2, Is.EqualTo("name2"));
						Assert.That(result[0].NAME3, Is.EqualTo("name3"));
						Assert.That(result[0].NAME4, Is.EqualTo("name4"));
						Assert.That(result[0].NAME5, Is.EqualTo("name5"));
					});
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
			using (var db = GetDataConnection(context))
			{
				int? id = null;
				try
				{
					var id1 = db.PersonInsert("Имя", "Фамилия", "Отчество", 'M', out id).ToList();
					Assert.That(id1, Has.Count.EqualTo(1));
					Assert.That(id1[0].PERSONID, Is.Not.Null);

					// TODO: see TestProcedureNonLatinParameters2
					// output parameter value is not set
					id = id1[0].PERSONID;

					var record = db.Person.Single(p => p.ID == id);

					Assert.Multiple(() =>
					{
						Assert.That(record.FirstName, Is.EqualTo("Имя"));
						Assert.That(record.LastName, Is.EqualTo("Фамилия"));
						Assert.That(record.MiddleName, Is.EqualTo("Отчество"));
						Assert.That(record.Gender, Is.EqualTo(Gender.Male));
						Assert.That(id, Is.Not.Null);
						Assert.That(id1[0].PERSONID, Is.EqualTo(id));
					});
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
			using (var db = GetDataConnection(context))
			{
				int? id = null;
				try
				{
					var id1 = db.PersonInsert("Имя", "Фамилия", "Отчество", 'M', out id).ToList();
					Assert.That(id1, Has.Count.EqualTo(1));
					Assert.That(id1[0].PERSONID, Is.Not.Null);

					var record = db.Person.Single(p => p.ID == id);

					Assert.Multiple(() =>
					{
						Assert.That(record.FirstName, Is.EqualTo("Имя"));
						Assert.That(record.LastName, Is.EqualTo("Фамилия"));
						Assert.That(record.MiddleName, Is.EqualTo("Отчество"));
						Assert.That(record.Gender, Is.EqualTo(Gender.Male));
						Assert.That(id, Is.Not.Null);
						Assert.That(id1[0].PERSONID, Is.EqualTo(id));
					});
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
			using (var db      = GetDataConnection(context))
			using (var cards   = db.CreateLocalTable<Card>())
			using (var clients = db.CreateLocalTable<Client>())
			{
				cards.LoadWith(x => x.Owner).ToList();

				var sql = db.LastQuery!;

				if (quoteMode == FirebirdIdentifierQuoteMode.None)
				{
					sql.Should().Contain("Client a_Owner");
				}
				else
				{
					sql.Should().Contain("\"Client\" \"a_Owner\"");
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
		public void TestFb4TypesProcedureSchema([IncludeDataSources(false, TestProvName.AllFirebird4Plus)] string context)
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

				Assert.That(proc, Is.Not.Null);
				Assert.Multiple(() =>
				{
					Assert.That(proc.Parameters, Has.Count.EqualTo(5));
					Assert.That(proc.ResultTable, Is.Not.Null);
				});
				Assert.That(proc.ResultTable!.Columns, Has.Count.EqualTo(5));

				AssertParameter("DECFLOAT16" , "DECFLOAT"                , DataType.DecFloat      , typeof(FbDecFloat)     , 16  , "FbDecFloat"     );
				AssertParameter("DECFLOAT34" , "DECFLOAT"                , DataType.DecFloat      , typeof(FbDecFloat)     , 34  , "FbDecFloat"     );
				AssertParameter("TSTZ"       , "TIMESTAMP WITH TIME ZONE", DataType.DateTimeOffset, typeof(FbZonedDateTime), null, "FbZonedDateTime");
				AssertParameter("TTZ"        , "TIME WITH TIME ZONE"     , DataType.TimeTZ        , typeof(FbZonedTime)    , null, "FbZonedTime"    );
				AssertParameter("INT_128"    , "INT128"                  , DataType.Int128        , typeof(BigInteger)     , null, null             );

				AssertColumn("COL_DECFLOAT16" , "decfloat"                , DataType.DecFloat      , typeof(FbDecFloat)     , 16  , "FbDecFloat"     );
				AssertColumn("COL_DECFLOAT34" , "decfloat"                , DataType.DecFloat      , typeof(FbDecFloat)     , null, "FbDecFloat"     );
				AssertColumn("COL_TSTZ"       , "timestamp with time zone", DataType.DateTimeOffset, typeof(FbZonedDateTime), null, "FbZonedDateTime");
				AssertColumn("COL_TTZ"        , "time with time zone"     , DataType.TimeTZ        , typeof(FbZonedTime)    , null, "FbZonedTime"    );
				AssertColumn("COL_INT_128"    , "int128"                  , DataType.Int128        , typeof(BigInteger)     , null, null             );

				void AssertColumn(string name, string dbType, DataType dataType, Type type, int? precision, string? providerSpecificType)
				{
					var column = proc.ResultTable!.Columns.Where(c => c.ColumnName == name).SingleOrDefault()!;
					Assert.That(column, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(column.ColumnType, Is.EqualTo(dbType));
						Assert.That(column.DataType, Is.EqualTo(dataType));
						Assert.That(column.ProviderSpecificType, Is.EqualTo(providerSpecificType));
						Assert.That(column.SystemType, Is.EqualTo(type));
						Assert.That(column.MemberType, Is.EqualTo(type == typeof(object) ? "object" : type.Name + "?"));
						Assert.That(column.Precision, Is.EqualTo(precision));
					});
				}

				void AssertParameter(string name, string dbType, DataType dataType, Type type, int? precision, string? providerSpecificType)
				{
					var parameter = proc!.Parameters.Where(c => c.ParameterName == name).SingleOrDefault()!;
					Assert.That(parameter, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(parameter.SchemaType, Is.EqualTo(dbType));
						Assert.That(parameter.DataType, Is.EqualTo(dataType));
						Assert.That(parameter.ProviderSpecificType, Is.EqualTo(providerSpecificType));
						Assert.That(parameter.SystemType, Is.EqualTo(type));
						Assert.That(parameter.ParameterType, Is.EqualTo(type == typeof(object) ? "object" : type.Name + "?"));
					});
				}
			}
		}

		[Test]
		public void TestFb4TypesCreateTable([IncludeDataSources(false, TestProvName.AllFirebird4Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (var t = db.CreateTempTable<TestFbTypesTable>())
			{
				var sql = db.LastQuery!;

				// create table
				Assert.That(sql, Does.Contain("\"Id\"         Int                      NOT NULL,"));
				Assert.That(sql, Does.Contain("\"DecFloat16\" DECFLOAT(16),"));
				Assert.That(sql, Does.Contain("\"DecFloat30\" DECFLOAT,"));
				Assert.That(sql, Does.Contain("\"DecFloat34\" DECFLOAT,"));
				Assert.That(sql, Does.Contain("\"DecFloat\"   DECFLOAT,"));
				Assert.That(sql, Does.Contain("\"DateTimeTZ\" TIMESTAMP WITH TIME ZONE,"));
				Assert.That(sql, Does.Contain("\"TimeTZ\"     TIME WITH TIME ZONE,"));
				Assert.That(sql, Does.Contain("\"Int128\"     INT128,"));
			}
		}

		[Test]
		public void TestFb4TypesParametersAndLiterals(
			[IncludeDataSources(false, TestProvName.AllFirebird4Plus)] string context,
			[Values] bool inline)
		{
			using (var db = new TestDataConnection(context))
			using (var t = db.CreateLocalTable(TestFbTypesTable.Data))
			{
				db.InlineParameters = inline;

				var sql = db.LastQuery!;

				Assert.Multiple(() =>
				{
					Assert.That(t.Where(_ => _.DecFloat16 == TestFbTypesTable.Data[0].DecFloat16).Count(), Is.EqualTo(1));
					Assert.That(db.LastQuery!.Contains("@"), Is.EqualTo(true));
					Assert.That(t.Where(_ => _.DecFloat30 == TestFbTypesTable.Data[0].DecFloat30).Count(), Is.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(db.LastQuery!.Contains("@"), Is.EqualTo(true));
					Assert.That(t.Where(_ => _.DecFloat34 == TestFbTypesTable.Data[0].DecFloat34).Count(), Is.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(db.LastQuery!.Contains("@"), Is.EqualTo(true));
					Assert.That(t.Where(_ => _.DecFloat == TestFbTypesTable.Data[0].DecFloat).Count(), Is.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(db.LastQuery!.Contains("@"), Is.EqualTo(true));
					Assert.That(t.Where(_ => _.DateTimeTZ == TestFbTypesTable.Data[0].DateTimeTZ).Count(), Is.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(db.LastQuery!.Contains("@"), Is.EqualTo(true));
					Assert.That(t.Where(_ => _.TimeTZ == TestFbTypesTable.Data[0].TimeTZ).Count(), Is.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(db.LastQuery!.Contains("@"), Is.EqualTo(true));
					Assert.That(t.Where(_ => _.Int128 == TestFbTypesTable.Data[0].Int128).Count(), Is.EqualTo(1));
				});
				Assert.That(db.LastQuery!.Contains("@"), Is.EqualTo(!inline));
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
			[IncludeDataSources(false, TestProvName.AllFirebird4Plus)] string context,
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

				Assert.Multiple(() =>
				{
					Assert.That(t.Where(_ => _.Int128 == testCase.Int128).Count(), Is.EqualTo(1));

					Assert.That(db.LastQuery!, Does.Contain(testCase.Literal));
				});
			}
		}

		[Test]
		public void TestFb4TypesTableSchema([IncludeDataSources(false, TestProvName.AllFirebird4Plus)] string context)
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

				Assert.That(table, Is.Not.Null);
				Assert.That(table.Columns, Has.Count.EqualTo(8));

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

					Assert.That(column, Is.Not.Null);
					Assert.Multiple(() =>
					{
						Assert.That(column.ColumnType, Is.EqualTo(dbType));
						Assert.That(column.DataType, Is.EqualTo(dataType));
						Assert.That(column.ProviderSpecificType, Is.EqualTo(providerSpecificType));
						Assert.That(column.SystemType, Is.EqualTo(type));
						Assert.That(column.MemberType, Is.EqualTo(type == typeof(object) ? "object" : type.Name + "?"));
						Assert.That(column.Precision, Is.EqualTo(precision));
					});
				}
			}
		}

		[Test]
		public void TestFb4TypesProcedure([IncludeDataSources(false, TestProvName.AllFirebird4Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var tstz       = new FbZonedDateTime(TestData.DateTime4Utc, "UTC");
				var ttz        = new FbZonedTime(TestData.TimeOfDay4, "UTC");
				var decfloat16 = new FbDecFloat(1234567890123456, 5);
				var decfloat34 = new FbDecFloat(BigInteger.Parse("1234567890123456789012345678901235"), 15);
				var int128     = BigInteger.Parse("-170141183460469231731687303715884105728");

				var res = db.TestV4Types(tstz, ttz, decfloat16, decfloat34, int128).ToList();

				Assert.That(res, Has.Count.EqualTo(1));

				Assert.Multiple(() =>
				{
					Assert.That(res[0].COL_TSTZ, Is.EqualTo(tstz));
					Assert.That(res[0].COL_TTZ, Is.EqualTo(ttz));
					Assert.That(res[0].COL_DECFLOAT16, Is.EqualTo(decfloat16));
					Assert.That(res[0].COL_DECFLOAT34, Is.EqualTo(decfloat34));
					Assert.That(res[0].COL_INT_128, Is.EqualTo(int128));
				});
			}
		}

		[Test]
		public void TestModule([IncludeDataSources(false, TestProvName.AllFirebird3Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var parameters = new []
				{
					new DataParameter("I", 1, DataType.Int32),
					new DataParameter("O", null, DataType.Int32)
					{
						Direction = ParameterDirection.Output
					}
				};

				db.ExecuteProc("TEST_PROCEDURE", parameters);
				Assert.That(parameters[1].Value, Is.EqualTo(4));
				db.ExecuteProc("TEST_PACKAGE1.TEST_PROCEDURE", parameters);
				Assert.That(parameters[1].Value, Is.EqualTo(2));
				db.ExecuteProc("TEST_PACKAGE2.TEST_PROCEDURE", parameters);
				Assert.Multiple(() =>
				{
					Assert.That(parameters[1].Value, Is.EqualTo(3));

					Assert.That(db.Person.Select(p => FirebirdModuleFunctions.TestFunction(1)).First(), Is.EqualTo(4));
					Assert.That(db.Person.Select(p => FirebirdModuleFunctions.TestFunctionP1(1)).First(), Is.EqualTo(2));
					Assert.That(db.Person.Select(p => FirebirdModuleFunctions.TestFunctionP2(1)).First(), Is.EqualTo(3));

					Assert.That(FirebirdModuleFunctions.TestTableFunction(db, 1).Select(r => r.O).First(), Is.EqualTo(4));
					Assert.That(FirebirdModuleFunctions.TestTableFunctionP1(db, 1).Select(r => r.O).First(), Is.EqualTo(2));
					Assert.That(FirebirdModuleFunctions.TestTableFunctionP2(db, 1).Select(r => r.O).First(), Is.EqualTo(3));
				});
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4065")]
		public void TestParameterlessProcedure([IncludeDataSources(false, TestProvName.AllFirebird)] string context)
		{
			using var db = GetDataConnection(context);

			// call as proc
			db.ExecuteProc("\"Person_SelectAll\"");
			db.QueryProc<PersonSelectAllResult>("\"Person_SelectAll\"").ToList();

			// call as table function
			PersonSelectAll(db).ToList();
		}

		[Sql.TableFunction("Person_SelectAll", ArgIndices = new int[0])]
		private static IQueryable<PersonSelectAllResult> PersonSelectAll(IDataContext ctx)
		{
			return ctx.GetTable<PersonSelectAllResult>(null, (MethodInfo)MethodBase.GetCurrentMethod()!, ctx);
		}

		public partial class PersonSelectAllResult
		{
			[Column("PERSONID"  , DataType = DataType.Int32   , DbType = "integer")    ] public int?    Personid   { get; set; }
			[Column("FIRSTNAME" , DataType = DataType.NVarChar, DbType = "varchar(50)")] public string? Firstname  { get; set; }
			[Column("LASTNAME"  , DataType = DataType.NVarChar, DbType = "varchar(50)")] public string? Lastname   { get; set; }
			[Column("MIDDLENAME", DataType = DataType.NVarChar, DbType = "varchar(50)")] public string? Middlename { get; set; }
			[Column("GENDER"    , DataType = DataType.NChar   , DbType = "char(1)")    ] public string? Gender     { get; set; }
		}

		[Table]
		sealed class BinaryMappingTable
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DbType = "CHAR(6) CHARACTER SET OCTETS")] public string? String { get; set; }
			[Column(DbType = "CHAR(6) CHARACTER SET OCTETS")] public char[] AsCharArray { get; set; } = null!;
			[Column(DbType = "CHAR(6) CHARACTER SET OCTETS")] public byte[]? AsBinary { get; set; }

			[NotColumn]
			public string? StringAccessor
			{
				get => String == null ? null : string.Join(":", String.Select(c => ((ushort)c).ToString("X2")));
				set => String = value == null ? null : string.Join("", value.Split(':').Select(c => (char)ushort.Parse(c, NumberStyles.HexNumber)));
			}

			[NotColumn]
			public string? BinaryAccessor
			{
				get => AsBinary == null ? null : string.Join(":", AsBinary.Select(b => b.ToString("X2")));
				set => AsBinary = value == null ? null : value.Split(':').Select(c => byte.Parse(c, NumberStyles.HexNumber)).ToArray();
			}

			[NotColumn]
			public string CharArrayAccessor
			{
				get => string.Join(":", AsCharArray.Select(c => ((ushort)c).ToString("X2")));
				set => AsCharArray = value.Split(':').Select(c => (char)ushort.Parse(c, NumberStyles.HexNumber)).ToArray();
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/755")]
		public void TestBinaryMapping_Binary([IncludeDataSources(false, TestProvName.AllFirebird)] string context)
		{
			using var db = GetDataConnection(context);
			using var t  = db.CreateLocalTable<BinaryMappingTable>();

			var mac1 = "00:FF:01:02:03:04";
			var mac2 = "DE:AD:00:BE:EF:11";

			db.Insert(new BinaryMappingTable()
			{
				Id                = 1,
				BinaryAccessor    = mac1,
			});

			var record = t.Single();
			Assert.That(record.BinaryAccessor, Is.EqualTo(mac1));

			db.Update(new BinaryMappingTable()
			{
				Id                = 1,
				BinaryAccessor    = mac2,
			});

			record = t.Single();
			Assert.That(record.BinaryAccessor, Is.EqualTo(mac2));
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/755")]
		public void TestBinaryMapping_String([IncludeDataSources(false, TestProvName.AllFirebird)] string context)
		{
			using var db = GetDataConnection(context);
			using var t  = db.CreateLocalTable<BinaryMappingTable>();

			var mac1 = "00:FF:01:02:03:04";
			var mac2 = "DE:AD:00:BE:EF:11";

			db.Insert(new BinaryMappingTable()
			{
				Id                = 1,
				StringAccessor    = mac1,
			});

			var record = t.Single();
			Assert.That(record.StringAccessor, Is.EqualTo(mac1));

			db.Update(new BinaryMappingTable()
			{
				Id                = 1,
				StringAccessor    = mac2,
			});

			record = t.Single();
			Assert.That(record.StringAccessor, Is.EqualTo(mac2));
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/755")]
		public void TestBinaryMapping_Char([IncludeDataSources(false, TestProvName.AllFirebird)] string context)
		{
			using var db = GetDataConnection(context);
			using var t  = db.CreateLocalTable<BinaryMappingTable>();

			var mac1 = "00:FF:01:02:03:04";
			var mac2 = "DE:AD:00:BE:EF:11";

			db.Insert(new BinaryMappingTable()
			{
				Id = 1,
				CharArrayAccessor = mac1,
			});

			var record = t.Single();
			Assert.That(record.CharArrayAccessor, Is.EqualTo(mac1));

			db.Update(new BinaryMappingTable()
			{
				Id = 1,
				CharArrayAccessor = mac2,
			});

			record = t.Single();
			Assert.That(record.CharArrayAccessor, Is.EqualTo(mac2));
		}
	}

	#region Extensions

	static class FirebirdModuleFunctions
	{
		[Sql.Function("TEST_FUNCTION", ServerSideOnly = true)]
		public static int TestFunction(int param)
		{
			throw new InvalidOperationException("Scalar function cannot be called outside of query");
		}

		[Sql.Function("TEST_PACKAGE1.TEST_FUNCTION", ServerSideOnly = true)]
		public static int TestFunctionP1(int param)
		{
			throw new InvalidOperationException("Scalar function cannot be called outside of query");
		}

		[Sql.Function("TEST_PACKAGE2.TEST_FUNCTION", ServerSideOnly = true)]
		public static int TestFunctionP2(int param)
		{
			throw new InvalidOperationException("Scalar function cannot be called outside of query");
		}

		[Sql.TableFunction("TEST_TABLE_FUNCTION", argIndices: new[] { 1 })]
		public static LinqToDB.ITable<Record> TestTableFunction(IDataContext db, int param1)
		{
			return db.GetTable<Record>(null, (MethodInfo)MethodBase.GetCurrentMethod()!, db, param1);
		}

		[Sql.TableFunction("TEST_TABLE_FUNCTION", argIndices: new[] { 1 }, Package = "TEST_PACKAGE1")]
		public static LinqToDB.ITable<Record> TestTableFunctionP1(IDataContext db, int param1)
		{
			return db.GetTable<Record>(null, (MethodInfo)MethodBase.GetCurrentMethod()!, db, param1);
		}

		[Sql.TableFunction("TEST_TABLE_FUNCTION", argIndices: new[] { 1 }, Package = "TEST_PACKAGE2")]
		public static LinqToDB.ITable<Record> TestTableFunctionP2(IDataContext db, int param1)
		{
			return db.GetTable<Record>(null, (MethodInfo)MethodBase.GetCurrentMethod()!, db, param1);
		}

		public sealed class Record
		{
			public int O { get; set; }
		}
	}

	static class FirebirdProcedures
	{
		public sealed class PersonInsertResult
		{
			public int? PERSONID { get; set; }
		}

		public static IEnumerable<PersonInsertResult> PersonInsert(this DataConnection dataConnection, string? FIRSTNAME, string? LASTNAME, string? MIDDLENAME, char? GENDER, out int? PERSONID)
		{
			var parameters = new []
			{
				new DataParameter("FIRSTNAME", FIRSTNAME, DataType.NVarChar)
				{
					Size = 50
				},
				new DataParameter("LASTNAME", LASTNAME, DataType.NVarChar)
				{
					Size = 50
				},
				new DataParameter("MIDDLENAME", MIDDLENAME, DataType.NVarChar)
				{
					Size = 50
				},
				new DataParameter("GENDER",   GENDER, DataType.NChar)
				{
					Size = 1
				},
				new DataParameter("PERSONID", null, DataType.Int32)
				{
					Direction = ParameterDirection.Output,
					Size      = 4
				}
			};

			var ret = dataConnection.QueryProc<PersonInsertResult>("\"Person_Insert\"", parameters).ToList();

			PERSONID = Converter.ChangeTypeTo<int?>(parameters[4].Value);

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

		public sealed class TestV4TYPESResult
		{
			public FbZonedDateTime? COL_TSTZ       { get; set; }
			public FbZonedTime?     COL_TTZ        { get; set; }
			public FbDecFloat?      COL_DECFLOAT16 { get; set; }
			public FbDecFloat?      COL_DECFLOAT34 { get; set; }
			public BigInteger?      COL_INT_128    { get; set; }
		}
	}

	#endregion
}
