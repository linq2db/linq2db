extern alias MySqlData;
extern alias MySqlConnector;

using System;
using System.Data.Linq;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Reflection;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using LinqToDB.Tools;
using LinqToDB.DataProvider.MySql;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;
using MySqlDataDateTime = MySqlData::MySql.Data.Types.MySqlDateTime;
using MySqlDataDecimal = MySqlData::MySql.Data.Types.MySqlDecimal;
using MySqlConnectorDateTime = MySqlConnector::MySqlConnector.MySqlDateTime;
using MySqlConnectorDecimal = MySqlConnector::MySqlConnector.MySqlDecimal;
using MySqlConnectorGuidFormat = MySqlConnector::MySqlConnector.MySqlGuidFormat;


namespace Tests.DataProvider
{
	using Model;

	[TestFixture]
	public class MySqlTests : DataProviderTestBase
	{
		[Test]
		public void TestParameters([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT @p", new { p = 1 }), Is.EqualTo("1"));
					Assert.That(conn.Execute<string>("SELECT @p", new { p = "1" }), Is.EqualTo("1"));
					Assert.That(conn.Execute<int>("SELECT @p", new { p = new DataParameter { Value = 1 } }), Is.EqualTo(1));
					Assert.That(conn.Execute<string>("SELECT @p1", new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
					Assert.That(conn.Execute<int>("SELECT @p1 + ?p2", new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
					Assert.That(conn.Execute<int>("SELECT @p2 + ?p1", new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
				});
			}
		}

		[Test]
		public void TestDataTypes([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(TestType<long?>(conn, "bigintDataType", DataType.Int64), Is.EqualTo(1000000));
					Assert.That(TestType<short?>(conn, "smallintDataType", DataType.Int16), Is.EqualTo(25555));
					Assert.That(TestType<sbyte?>(conn, "tinyintDataType", DataType.SByte), Is.EqualTo(111));
					Assert.That(TestType<int?>(conn, "mediumintDataType", DataType.Int32), Is.EqualTo(5555));
					Assert.That(TestType<int?>(conn, "intDataType", DataType.Int32), Is.EqualTo(7777777));
					Assert.That(TestType<decimal?>(conn, "numericDataType", DataType.Decimal), Is.EqualTo(9999999m));
					Assert.That(TestType<decimal?>(conn, "decimalDataType", DataType.Decimal), Is.EqualTo(8888888m));
					Assert.That(TestType<double?>(conn, "doubleDataType", DataType.Double), Is.EqualTo(20.31d));
					Assert.That(TestType<float?>(conn, "floatDataType", DataType.Single), Is.EqualTo(16.0f));
					Assert.That(TestType<DateTime?>(conn, "dateDataType", DataType.Date), Is.EqualTo(new DateTime(2012, 12, 12)));
					Assert.That(TestType<DateTime?>(conn, "datetimeDataType", DataType.DateTime), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
					Assert.That(TestType<DateTime?>(conn, "datetimeDataType", DataType.DateTime2), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
					Assert.That(TestType<DateTime?>(conn, "timestampDataType", DataType.Timestamp), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
					Assert.That(TestType<TimeSpan?>(conn, "timeDataType", DataType.Time), Is.EqualTo(new TimeSpan(12, 12, 12)));
					Assert.That(TestType<int?>(conn, "yearDataType", DataType.Int32), Is.EqualTo(1998));

					Assert.That(TestType<char?>(conn, "charDataType", DataType.Char), Is.EqualTo('1'));
					Assert.That(TestType<string>(conn, "charDataType", DataType.Char), Is.EqualTo("1"));
					Assert.That(TestType<string>(conn, "charDataType", DataType.NChar), Is.EqualTo("1"));
					Assert.That(TestType<string>(conn, "varcharDataType", DataType.VarChar), Is.EqualTo("234"));
					Assert.That(TestType<string>(conn, "varcharDataType", DataType.NVarChar), Is.EqualTo("234"));
					Assert.That(TestType<string>(conn, "textDataType", DataType.Text), Is.EqualTo("567"));

					Assert.That(TestType<byte[]>(conn, "binaryDataType", DataType.Binary), Is.EqualTo(new byte[] { 97, 98, 99 }));
					Assert.That(TestType<byte[]>(conn, "binaryDataType", DataType.VarBinary), Is.EqualTo(new byte[] { 97, 98, 99 }));
					Assert.That(TestType<byte[]>(conn, "varbinaryDataType", DataType.Binary), Is.EqualTo(new byte[] { 99, 100, 101 }));
					Assert.That(TestType<byte[]>(conn, "varbinaryDataType", DataType.VarBinary), Is.EqualTo(new byte[] { 99, 100, 101 }));
					Assert.That(TestType<Binary>(conn, "varbinaryDataType", DataType.VarBinary).ToArray(), Is.EqualTo(new byte[] { 99, 100, 101 }));
					Assert.That(TestType<byte[]>(conn, "blobDataType", DataType.Binary), Is.EqualTo(new byte[] { 100, 101, 102 }));
					Assert.That(TestType<byte[]>(conn, "blobDataType", DataType.VarBinary), Is.EqualTo(new byte[] { 100, 101, 102 }));
					Assert.That(TestType<byte[]>(conn, "blobDataType", DataType.Blob), Is.EqualTo(new byte[] { 100, 101, 102 }));

					Assert.That(TestType<ulong?>(conn, "bitDataType"), Is.EqualTo(5));
					Assert.That(TestType<string>(conn, "enumDataType"), Is.EqualTo("Green"));
					Assert.That(TestType<string>(conn, "setDataType"), Is.EqualTo("one"));
				});

				using (new DisableBaseline("Platform-specific baselines"))
				{
					if (context.IsAnyOf(TestProvName.AllMySqlData))
					{
						TestType<MySqlDataDecimal?>(conn, "decimalDataType", DataType.Decimal);
						var dt1 = TestType<MySqlDataDateTime?>(conn, "datetimeDataType", DataType.DateTime);
						var dt2 = new MySqlDataDateTime(2012, 12, 12, 12, 12, 12, 0)
						{
							TimezoneOffset = dt1!.Value.TimezoneOffset
						};

						Assert.That(dt1, Is.EqualTo(dt2));
					}
					else
					{
						TestType<MySqlConnectorDecimal?>(conn, "decimalDataType", DataType.Decimal);
						using (new DisableBaseline("Output (datetime format) is culture-/system-dependent"))
							Assert.That(TestType<MySqlConnectorDateTime?>(conn, "datetimeDataType", DataType.DateTime), Is.EqualTo(new MySqlConnectorDateTime(2012, 12, 12, 12, 12, 12, 0)));
					}
				}
			}
		}

		[Table]
		public class BigDecimalMySqlDataTable
		{
			[Column                            ] public int               Id       { get; set; }
			[Column(DbType = "decimal(65, 30)")] public MySqlDataDecimal  Decimal  { get; set; }
			[Column(DbType = "decimal(65, 30)")] public MySqlDataDecimal? DecimalN { get; set; }
		}

		[Test]
		public void TestMySqlDataBigDecimal([IncludeDataSources(TestProvName.AllMySqlData)] string context, [Values] BulkCopyType bulkCopyType, [Values] bool inline)
		{
			using (var db = new DataConnection(context))
			using (var tb = db.CreateLocalTable<BigDecimalMySqlDataTable>())
			{
				db.InlineParameters = inline;

				// due to internal constructor we should ask provider to create parameter value for us
				var value1 = db.Execute<MySqlDataDecimal>("SELECT 12345678901234567890123456789012345.123456789012345678901234567891");
				var value2 = db.Execute<MySqlDataDecimal>("SELECT -12345678901234567890123456789012345.123456789012345678901234567891");

				var testRecord1 = new BigDecimalMySqlDataTable()
				{
					Id       = 1,
					Decimal  = value1,
					DecimalN = value2,
				};
				var testRecord2 = new BigDecimalMySqlDataTable()
				{
					Id       = 2,
					Decimal  = value2,
					DecimalN = null,
				};

				// test insert
				db.Insert(testRecord1);
				db.Insert(testRecord2);

				// test select (not really as it is broken badly in provider)
				// IsDBNull fails with exception, so we cannot fix it easily
				Assert.Throws<OverflowException>(() => tb.Single());
				//var records = tb.OrderBy(_ => _.Id).ToArray();
				//Assert.AreEqual(1, records[0].Id);
				//Assert.AreEqual(value1, records[0].Decimal);
				//Assert.AreEqual(value2, records[0].DecimalN);
				//Assert.AreEqual(2, records[1].Id);
				//Assert.AreEqual(value2, records[1].Decimal);
				//Assert.IsNull(records[1].DecimalN);

				// test insert linq (to force parameters)
				tb.Delete();
				tb.Insert(() => new BigDecimalMySqlDataTable()
				{
					Id       = 1,
					Decimal  = value1,
					DecimalN = value2,
				});
				tb.Insert(() => new BigDecimalMySqlDataTable()
				{
					Id       = 2,
					Decimal  = value2,
					DecimalN = null,
				});

				// test select (not really as it is broken badly in provider)
				// IsDBNull fails with exception, so we cannot fix it easily
				Assert.Throws<OverflowException>(() => tb.Single());
				//var records = tb.OrderBy(_ => _.Id).ToArray();
				//Assert.AreEqual(1, records[0].Id);
				//Assert.AreEqual(value1, records[0].Decimal);
				//Assert.AreEqual(value2, records[0].DecimalN);
				//Assert.AreEqual(2, records[1].Id);
				//Assert.AreEqual(value2, records[1].Decimal);
				//Assert.IsNull(records[1].DecimalN);

				// test bulk copy
				tb.Delete();
				db.BulkCopy(new BulkCopyOptions() { BulkCopyType = bulkCopyType }, new[] { testRecord1, testRecord2 });

				Assert.Throws<OverflowException>(() => tb.Single());
				//records = tb.OrderBy(_ => _.Id).ToArray();
				//Assert.AreEqual(1, records[0].Id);
				//Assert.AreEqual(value1, records[0].Decimal);
				//Assert.AreEqual(value2, records[0].DecimalN);
				//Assert.AreEqual(2, records[1].Id);
				//Assert.AreEqual(value2, records[1].Decimal);
				//Assert.IsNull(records[1].DecimalN);
			}
		}

		[Table]
		public class BigDecimalMySqlConnectorTable
		{
			[Column] public int Id { get; set; }
			[Column(DbType = "decimal(65, 30)")] public MySqlConnectorDecimal  Decimal  { get; set; }
			[Column(DbType = "decimal(65, 30)")] public MySqlConnectorDecimal? DecimalN { get; set; }
		}

		[Test]
		public void TestMySqlConnectorBigDecimal([IncludeDataSources(TestProvName.AllMySqlConnector)] string context, [Values] BulkCopyType bulkCopyType, [Values] bool inline)
		{
			using (var db = new DataConnection(context))
			using (var tb = db.CreateLocalTable<BigDecimalMySqlConnectorTable>())
			{
				db.InlineParameters = inline;

				// due to internal constructor we should ask provider to create parameter value for us
				// https://github.com/mysql-net/MySqlConnector/issues/1142
				var value1 = db.Execute<MySqlConnectorDecimal>("SELECT 12345678901234567890123456789012345.123456789012345678901234567891");
				var value2 = db.Execute<MySqlConnectorDecimal>("SELECT -12345678901234567890123456789012345.123456789012345678901234567891");

				var testRecord1 = new BigDecimalMySqlConnectorTable()
				{
					Id       = 1,
					Decimal  = value1,
					DecimalN = value2,
				};
				var testRecord2 = new BigDecimalMySqlConnectorTable()
				{
					Id       = 2,
					Decimal  = value2,
					DecimalN = null,
				};

				// test insert
				db.Insert(testRecord1);
				db.Insert(testRecord2);

				// test select
				var records = tb.OrderBy(_ => _.Id).ToArray();
				Assert.Multiple(() =>
				{
					Assert.That(records[0].Id, Is.EqualTo(1));
					Assert.That(records[0].Decimal, Is.EqualTo(value1));
					Assert.That(records[0].DecimalN, Is.EqualTo(value2));
					Assert.That(records[1].Id, Is.EqualTo(2));
					Assert.That(records[1].Decimal, Is.EqualTo(value2));
					Assert.That(records[1].DecimalN, Is.Null);
				});

				// test insert linq (to force parameters)
				tb.Delete();
				tb.Insert(() => new BigDecimalMySqlConnectorTable()
				{
					Id       = 1,
					Decimal  = value1,
					DecimalN = value2,
				});
				tb.Insert(() => new BigDecimalMySqlConnectorTable()
				{
					Id       = 2,
					Decimal  = value2,
					DecimalN = null,
				});

				// test select
				records = tb.OrderBy(_ => _.Id).ToArray();
				Assert.Multiple(() =>
				{
					Assert.That(records[0].Id, Is.EqualTo(1));
					Assert.That(records[0].Decimal, Is.EqualTo(value1));
					Assert.That(records[0].DecimalN, Is.EqualTo(value2));
					Assert.That(records[1].Id, Is.EqualTo(2));
					Assert.That(records[1].Decimal, Is.EqualTo(value2));
					Assert.That(records[1].DecimalN, Is.Null);
				});

				// cannot test filtering as there is no equality/comparison defined on .net type

				// test bulk copy
				tb.Delete();
				db.BulkCopy(new BulkCopyOptions() { BulkCopyType = bulkCopyType }, new[] { testRecord1, testRecord2 });

				records = tb.OrderBy(_ => _.Id).ToArray();
				Assert.Multiple(() =>
				{
					Assert.That(records[0].Id, Is.EqualTo(1));
					Assert.That(records[0].Decimal, Is.EqualTo(value1));
					Assert.That(records[0].DecimalN, Is.EqualTo(value2));
					Assert.That(records[1].Id, Is.EqualTo(2));
					Assert.That(records[1].Decimal, Is.EqualTo(value2));
					Assert.That(records[1].DecimalN, Is.Null);
				});
			}
		}

		[Test]
		public void TestDate([IncludeDataSources(TestProvName.AllMySql)] string context)
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
		public void TestDateTime([IncludeDataSources(TestProvName.AllMySql)] string context)
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
		public void TestChar([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<char>("SELECT Cast('1' as char)"), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast('1' as char)"), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast('1' as char(1))"), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast('1' as char(1))"), Is.EqualTo('1'));

					Assert.That(conn.Execute<char>("SELECT @p", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT @p", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast(@p as char)", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as char)", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>("SELECT Cast(@p as char(1))", DataParameter.Char("@p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1))", DataParameter.Char("@p", '1')), Is.EqualTo('1'));

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
		public void TestString([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20))"), Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT Cast(NULL    as char(20))"), Is.Null);

					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Char("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.VarChar("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Text("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NChar("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NText("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Create("p", "123")), Is.EqualTo("123"));

					Assert.That(conn.Execute<string>("SELECT @p", new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
				});
			}
		}

		[Test]
		public void TestBinary([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			var arr1 = new byte[] { 48, 57 };

			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Binary("p", arr1)), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", arr1)), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create("p", arr1)), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", null)), Is.EqualTo(null));
					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.VarBinary("p", Array.Empty<byte>())), Is.EqualTo(Array.Empty<byte>()));
					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Image("p", Array.Empty<byte>())), Is.EqualTo(Array.Empty<byte>()));
					Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>("SELECT @p", DataParameter.Create("p", new Binary(arr1))), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>("SELECT @p", new DataParameter("p", new Binary(arr1))), Is.EqualTo(arr1));
				});
			}
		}

		[Test]
		public void TestXml([IncludeDataSources(TestProvName.AllMySql)] string context)
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
		public void TestEnum1([IncludeDataSources(TestProvName.AllMySql)] string context)
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
		public void TestEnum2([IncludeDataSources(TestProvName.AllMySql)] string context)
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

		[Table("AllTypes")]
		public partial class AllType : AllTypeBaseProviderSpecific
		{
			[IgnoreComparison]
			[Column,     Nullable] public int?      yearDataType        { get; set; } // year(4)
		}

		// excludes year columns, as they doesn't supported by native bulk copy
		[Table("AllTypesNoYear")]
		public partial class AllTypeBaseProviderSpecific
		{
			[IgnoreComparison]
			[PrimaryKey, Identity] public int       ID                  { get; set; } // int(11)
			[Column,     Nullable] public long?     bigintDataType      { get; set; } // bigint(20)
			[Column,     Nullable] public short?    smallintDataType    { get; set; } // smallint(6)
			[Column,     Nullable] public sbyte?    tinyintDataType     { get; set; } // tinyint(4)
			[Column,     Nullable] public int?      mediumintDataType   { get; set; } // mediumint(9)
			[Column,     Nullable] public int?      intDataType         { get; set; } // int(11)
			[Column,     Nullable] public decimal?  numericDataType     { get; set; } // decimal(10,0)
			[Column,     Nullable] public decimal?  decimalDataType     { get; set; } // decimal(10,0)
			[Column,     Nullable] public double?   doubleDataType      { get; set; } // double
			[Column,     Nullable] public float?    floatDataType       { get; set; } // float
			[Column,     Nullable] public DateTime? dateDataType        { get; set; } // date
			[Column,     Nullable] public DateTime? datetimeDataType    { get; set; } // datetime
			[Column,     Nullable] public DateTime? timestampDataType   { get; set; } // timestamp
			[Column,     Nullable] public TimeSpan? timeDataType        { get; set; } // time
			[Column,     Nullable] public char?     charDataType        { get; set; } // char(1)
			[Column,     Nullable] public string?   varcharDataType     { get; set; } // varchar(20)
			[Column,     Nullable] public string?   textDataType        { get; set; } // text
			[Column,     Nullable] public byte[]?   binaryDataType      { get; set; } // binary(3)
			[Column,     Nullable] public byte[]?   varbinaryDataType   { get; set; } // varbinary(5)
			[Column,     Nullable] public byte[]?   blobDataType        { get; set; } // blob
			[Column,     Nullable] public ulong?    bitDataType         { get; set; } // bit(3)
			[Column,     Nullable] public string?   enumDataType        { get; set; } // enum('Green','Red','Blue')
			[Column,     Nullable] public string?   setDataType         { get; set; } // set('one','two')
			[Column,     Nullable] public uint?     intUnsignedDataType { get; set; } // int(10) unsigned
		}

		void BulkCopyTest(string context, BulkCopyType bulkCopyType, bool withTransaction)
		{
			const int records   = 1000;
			const int batchSize = 500;

			using (var conn = GetDataConnection(context))
			{
				MySqlTestUtils.EnableNativeBulk(conn, context);
				DataConnectionTransaction? transaction = null;
				if (withTransaction)
					transaction = conn.BeginTransaction();

				try
				{
					var source = Enumerable.Range(0, records).Select(n =>
							new AllType()
							{
								ID                  = 2000 + n,
								bigintDataType      = 3000 + n,
								smallintDataType    = (short)(4000 + n),
								tinyintDataType     = (sbyte)(5000 + n),
								mediumintDataType   = 6000 + n,
								intDataType         = 7000 + n,
								numericDataType     = 8000 + n,
								decimalDataType     = 9000 + n,
								doubleDataType      = 8800 + n,
								floatDataType       = 7700 + n,
								dateDataType        = TestData.Date,
								datetimeDataType    = TestUtils.StripMilliseconds(TestData.DateTime, true),
								timestampDataType   = TestUtils.StripMilliseconds(TestData.DateTime, true),
								timeDataType        = TestUtils.StripMilliseconds(TestData.DateTime, true).TimeOfDay,
								yearDataType        = (1000 + n) % 100,
								charDataType        = 'A',
								varcharDataType     = "_btest",
								textDataType        = "test",
								binaryDataType      = new byte[] { 6, 15, 4 },
								varbinaryDataType   = new byte[] { 123, 22 },
								blobDataType        = new byte[] { 1, 2, 3 },
								bitDataType         = 7,
								enumDataType        = "Green",
								setDataType         = "one",
								intUnsignedDataType = (uint)(5000 + n),
							}).ToList();

					var isNativeCopy = bulkCopyType == BulkCopyType.ProviderSpecific && ((MySqlProviderAdapter)((MySqlDataProvider)conn.DataProvider).Adapter).BulkCopy != null;

					if (isNativeCopy)
					{
						conn.BulkCopy<AllTypeBaseProviderSpecific>(
							new BulkCopyOptions { MaxBatchSize = batchSize, BulkCopyType = bulkCopyType },
							source);
						var result = conn.GetTable<AllTypeBaseProviderSpecific>().OrderBy(_ => _.ID).Where(_ => _.varcharDataType == "_btest");

						// compare only 10 records
						// as we don't compare all, we must ensure we inserted all records
						Assert.That(result.Count(), Is.EqualTo(source.Count));
						AreEqual(source.Take(10), result.Take(10), ComparerBuilder.GetEqualityComparer<AllTypeBaseProviderSpecific>());
					}
					else
					{
						conn.BulkCopy(
							new BulkCopyOptions { MaxBatchSize = batchSize, BulkCopyType = bulkCopyType },
							source);
						var result = conn.GetTable<AllType>().OrderBy(_ => _.ID).Where(_ => _.varcharDataType == "_btest");

						// compare only 10 records
						// as we don't compare all, we must ensure we inserted all records
						Assert.That(result.Count(), Is.EqualTo(source.Count));
						AreEqual(source.Take(10), result.Take(10), ComparerBuilder.GetEqualityComparer<AllType>());
					}
				}
				finally
				{
					if (transaction != null)
						transaction.Rollback();
					else
					{
						conn.GetTable<AllType>().Delete(_ => _.varcharDataType == "_btest");
						conn.GetTable<AllTypeBaseProviderSpecific>().Delete(_ => _.varcharDataType == "_btest");
					}
				}
			}
		}

		async Task BulkCopyTestAsync(string context, BulkCopyType bulkCopyType, bool withTransaction)
		{
			const int records   = 1000;
			const int batchSize = 500;

			using (var conn = GetDataConnection(context))
			{
				MySqlTestUtils.EnableNativeBulk(conn, context);
				DataConnectionTransaction? transaction = null;
				if (withTransaction)
					transaction = conn.BeginTransaction();

				try
				{
					var source = Enumerable.Range(0, records).Select(n =>
							new AllType()
							{
								ID                  = 2000 + n,
								bigintDataType      = 3000 + n,
								smallintDataType    = (short)(4000 + n),
								tinyintDataType     = (sbyte)(5000 + n),
								mediumintDataType   = 6000 + n,
								intDataType         = 7000 + n,
								numericDataType     = 8000 + n,
								decimalDataType     = 9000 + n,
								doubleDataType      = 8800 + n,
								floatDataType       = 7700 + n,
								dateDataType        = TestData.Date,
								datetimeDataType    = TestUtils.StripMilliseconds(TestData.DateTime, true),
								timestampDataType   = TestUtils.StripMilliseconds(TestData.DateTime, true),
								timeDataType        = TestUtils.StripMilliseconds(TestData.DateTime, true).TimeOfDay,
								yearDataType        = (1000 + n) % 100,
								charDataType        = 'A',
								varcharDataType     = "_btest",
								textDataType        = "test",
								binaryDataType      = new byte[] { 6, 15, 4 },
								varbinaryDataType   = new byte[] { 123, 22 },
								blobDataType        = new byte[] { 1, 2, 3 },
								bitDataType         = 7,
								enumDataType        = "Green",
								setDataType         = "one",
								intUnsignedDataType = (uint)(5000 + n),
							}).ToList();

					var isNativeCopy = bulkCopyType == BulkCopyType.ProviderSpecific && ((MySqlProviderAdapter)((MySqlDataProvider)conn.DataProvider).Adapter).BulkCopy != null;

					if (isNativeCopy)
					{
						await conn.BulkCopyAsync<AllTypeBaseProviderSpecific>(
							new BulkCopyOptions { MaxBatchSize = batchSize, BulkCopyType = bulkCopyType },
							source);
						var result = conn.GetTable<AllTypeBaseProviderSpecific>().OrderBy(_ => _.ID).Where(_ => _.varcharDataType == "_btest");

						// compare only 10 records
						// as we don't compare all, we must ensure we inserted all records
						Assert.That(result.Count(), Is.EqualTo(source.Count));
						AreEqual(source.Take(10), result.Take(10), ComparerBuilder.GetEqualityComparer<AllTypeBaseProviderSpecific>());
					}
					else
					{
						await conn.BulkCopyAsync(
							new BulkCopyOptions { MaxBatchSize = batchSize, BulkCopyType = bulkCopyType },
							source);
						var result = conn.GetTable<AllType>().OrderBy(_ => _.ID).Where(_ => _.varcharDataType == "_btest");

						// compare only 10 records
						// as we don't compare all, we must ensure we inserted all records
						Assert.That(result.Count(), Is.EqualTo(source.Count));
						AreEqual(source.Take(10), result.Take(10), ComparerBuilder.GetEqualityComparer<AllType>());
					}
				}
				finally
				{
					if (transaction != null)
						transaction.Rollback();
					else
					{
						conn.GetTable<AllType>().Delete(_ => _.varcharDataType == "_btest");
						conn.GetTable<AllTypeBaseProviderSpecific>().Delete(_ => _.varcharDataType == "_btest");
					}
				}
			}
		}

		[Test]
		public void BulkCopyMultipleRows([IncludeDataSources(TestProvName.AllMySql)] string context, [Values] bool withTransaction)
		{
			BulkCopyTest(context, BulkCopyType.MultipleRows, withTransaction);
		}

		[Test]
		public void BulkCopyRetrieveSequencesMultipleRows([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			BulkCopyRetrieveSequence(context, BulkCopyType.MultipleRows);
		}

		[Test]
		public void BulkCopyProviderSpecific([IncludeDataSources(TestProvName.AllMySql)] string context, [Values] bool withTransaction)
		{
			BulkCopyTest(context, BulkCopyType.ProviderSpecific, withTransaction);
		}

		[Test]
		public void BulkCopyRetrieveSequencesProviderSpecific([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			BulkCopyRetrieveSequence(context, BulkCopyType.ProviderSpecific);
		}

		[Test]
		public async Task BulkCopyMultipleRowsAsync([IncludeDataSources(TestProvName.AllMySql)] string context, [Values] bool withTransaction)
		{
			await BulkCopyTestAsync(context, BulkCopyType.MultipleRows, withTransaction);
		}

		[Test]
		public async Task BulkCopyRetrieveSequencesMultipleRowsAsync([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			await BulkCopyRetrieveSequenceAsync(context, BulkCopyType.MultipleRows);
		}

		[Test]
		public async Task BulkCopyProviderSpecificAsync([IncludeDataSources(TestProvName.AllMySql)] string context, [Values] bool withTransaction)
		{
			await BulkCopyTestAsync(context, BulkCopyType.ProviderSpecific, withTransaction);
		}

		[Test]
		public async Task BulkCopyRetrieveSequencesProviderSpecificAsync([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			await BulkCopyRetrieveSequenceAsync(context, BulkCopyType.ProviderSpecific);
		}

		[Table("NeedS.esca Pin`g")]
		sealed class BinaryTypes
		{
			[Column("ne.eds `escaPing", IsPrimaryKey = true)] public int Id { get; set; }

			[Column(DbType = "bit(64)", DataType = DataType.BitArray)] public ulong?    Bit_1 { get; set; }
			[Column(DbType = "bit(63)", DataType = DataType.BitArray)] public long?     Bit_2 { get; set; }
			[Column(DbType = "bit(64)")                              ] public BitArray? Bit_3 { get; set; }
			[Column(DbType = "bit(30)", DataType = DataType.BitArray)] public uint?     Bit_4 { get; set; }
			[Column(DbType = "bit(1)" , DataType = DataType.BitArray)] public bool?     Bit_5 { get; set; }
			[Column("needs escaping2", DbType = "bit(1)")            ] public BitArray? Bit_6 { get; set; }

			[Column(DbType = "binary(3)")                                  ] public byte[]? Binary_1     { get; set; }
			[Column(DbType = "binary(3)")                                  ] public Binary? Binary_2     { get; set; }
			[Column(DbType = "binary(3)"   , DataType = DataType.Binary)   ] public byte[]? Binary_3     { get; set; }
			[Column(DbType = "binary(3)"   , DataType = DataType.Binary)   ] public Binary? Binary_4     { get; set; }
			[Column(DbType = "varbinary(3)", DataType = DataType.VarBinary)] public byte[]? VarBinary_1  { get; set; }
			[Column(DbType = "varbinary(3)", DataType = DataType.VarBinary)] public Binary? VarBinary_2  { get; set; }
			[Column(DbType = "blob"        , DataType = DataType.Blob)     ] public byte[]? Blob_1       { get; set; }
			[Column(DbType = "blob"        , DataType = DataType.Blob)     ] public Binary? Blob_2       { get; set; }
			[Column(DbType = "tinyblob"    , DataType = DataType.Blob)     ] public byte[]? TinyBlob_1   { get; set; }
			[Column(DbType = "tinyblob"    , DataType = DataType.Blob)     ] public Binary? TinyBlob_2   { get; set; }
			[Column(DbType = "mediumblob"  , DataType = DataType.Blob)     ] public byte[]? MediumBlob_1 { get; set; }
			[Column(DbType = "mediumblob"  , DataType = DataType.Blob)     ] public Binary? MediumBlob_2 { get; set; }
			[Column(DbType = "longblob"    , DataType = DataType.Blob)     ] public byte[]? LongBlob_1   { get; set; }
			[Column(DbType = "longblob"    , DataType = DataType.Blob)     ] public Binary? LongBlob_2   { get; set; }
		}

		// this test tests binary and bit types, that require special handling by MySqlConnector bulk copy
		[Test]
		public void BulkCopyBinaryAndBitTypes([IncludeDataSources(TestProvName.AllMySql)] string context, [Values] BulkCopyType bulkCopyType)
		{
			using (var db    = GetDataConnection(context))
			using (var table = db.CreateLocalTable<BinaryTypes>())
			{
				MySqlTestUtils.EnableNativeBulk(db, context);

				// just to make assert work, as we receive 64 bits from server in ulong value
				var bit1 = new BitArray(64);
				bit1.Set(0, true);

				var data = new BinaryTypes[]
				{
					new BinaryTypes() { Id = 1 },
					new BinaryTypes()
					{
						Id = 2,

						Bit_1 = 0xFFFFFFFFFFFFFFFF,
						Bit_2 = 0x7FFFFFFFFFFFFFFF,
						Bit_3 = new BitArray(BitConverter.GetBytes(0xFFFFFFFFFFFFFFFF)),
						Bit_4 = 0x3FFFFFFF,
						Bit_5 = true,
						Bit_6 = bit1,

						Binary_1     = new byte[] { 1, 2, 3},
						Binary_2     = new Binary(new byte[] { 4, 5, 6 }),
						Binary_3     = new byte[] { 7, 8, 9 },
						Binary_4     = new Binary(new byte[] { 10, 11, 12 }),
						VarBinary_1  = new byte[] { 13, 14, 15 },
						VarBinary_2  = new Binary(new byte[] { 16, 17, 18 }),
						Blob_1       = new byte[] { 19, 20, 21 },
						Blob_2       = new Binary(new byte[] { 22, 23, 24 }),
						TinyBlob_1   = new byte[] { 25, 26, 27 },
						TinyBlob_2   = new Binary(new byte[] { 28, 29, 30 }),
						MediumBlob_1 = new byte[] { 31, 32, 33 },
						MediumBlob_2 = new Binary(new byte[] { 34, 35, 36 }),
						LongBlob_1   = new byte[] { 37, 38, 39 },
						LongBlob_2   = new Binary(new byte[] { 40, 41, 42 }),
					},
				};

				db.BulkCopy(new BulkCopyOptions { BulkCopyType = bulkCopyType }, data);

				var res = table.OrderBy(_ => _.Id).ToArray();
				Assert.That(res, Has.Length.EqualTo(data.Length));

				AreEqual(data, res, ComparerBuilder.GetEqualityComparer<BinaryTypes>());
			}
		}

		[Test]
		public async Task BulkCopyBinaryAndBitTypesAsync([IncludeDataSources(TestProvName.AllMySql)] string context, [Values] BulkCopyType bulkCopyType)
		{
			using (var db    = GetDataConnection(context))
			using (var table = db.CreateLocalTable<BinaryTypes>())
			{
				MySqlTestUtils.EnableNativeBulk(db, context);

				// just to make assert work, as we receive 64 bits from server in ulong value
				var bit1 = new BitArray(64);
				bit1.Set(0, true);

				var data = new BinaryTypes[]
				{
					new BinaryTypes() { Id = 1 },
					new BinaryTypes()
					{
						Id = 2,

						Bit_1 = 0xFFFFFFFFFFFFFFFF,
						Bit_2 = 0x7FFFFFFFFFFFFFFF,
						Bit_3 = new BitArray(BitConverter.GetBytes(0xFFFFFFFFFFFFFFFF)),
						Bit_4 = 0x3FFFFFFF,
						Bit_5 = true,
						Bit_6 = bit1,

						Binary_1     = new byte[] { 1, 2, 3},
						Binary_2     = new Binary(new byte[] { 4, 5, 6 }),
						Binary_3     = new byte[] { 7, 8, 9 },
						Binary_4     = new Binary(new byte[] { 10, 11, 12 }),
						VarBinary_1  = new byte[] { 13, 14, 15 },
						VarBinary_2  = new Binary(new byte[] { 16, 17, 18 }),
						Blob_1       = new byte[] { 19, 20, 21 },
						Blob_2       = new Binary(new byte[] { 22, 23, 24 }),
						TinyBlob_1   = new byte[] { 25, 26, 27 },
						TinyBlob_2   = new Binary(new byte[] { 28, 29, 30 }),
						MediumBlob_1 = new byte[] { 31, 32, 33 },
						MediumBlob_2 = new Binary(new byte[] { 34, 35, 36 }),
						LongBlob_1   = new byte[] { 37, 38, 39 },
						LongBlob_2   = new Binary(new byte[] { 40, 41, 42 }),
					},
				};

				await db.BulkCopyAsync(new BulkCopyOptions { BulkCopyType = bulkCopyType }, data);

				var res = await table.OrderBy(_ => _.Id).ToArrayAsync();
				Assert.That(res, Has.Length.EqualTo(data.Length));

				AreEqual(data, res, ComparerBuilder.GetEqualityComparer<BinaryTypes>());
			}
		}

		[Test]
		public void BulkCopyLinqTypes([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using (var db = GetDataConnection(context))
				{
					MySqlTestUtils.EnableNativeBulk(db, context);

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
							}));
					}
					finally
					{
						db.GetTable<LinqDataTypes>().Delete(p => p.ID >= 4000);
					}
				}
			}
		}

		[Test]
		public async Task BulkCopyLinqTypesAsync([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using (var db = GetDataConnection(context))
				{
					MySqlTestUtils.EnableNativeBulk(db, context);
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
							}));
					}
					finally
					{
						await db.GetTable<LinqDataTypes>().DeleteAsync(p => p.ID >= 4000);
					}
				}
			}
		}

		void BulkCopyRetrieveSequence(string context, BulkCopyType bulkCopyType)
		{
			var data = new[]
			{
				new Person { FirstName = "Neurologist"    , LastName = "test" },
				new Person { FirstName = "Sports Medicine", LastName = "test" },
				new Person { FirstName = "Optometrist"    , LastName = "test" },
				new Person { FirstName = "Pediatrics"     , LastName = "test"  },
				new Person { FirstName = "Psychiatry"     , LastName = "test"  }
			};

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				MySqlTestUtils.EnableNativeBulk(db, context);
				var options = new BulkCopyOptions
				{
					MaxBatchSize = 5,
					KeepIdentity = true,
					BulkCopyType = bulkCopyType,
					NotifyAfter = 3,
					RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied)
				};

				db.BulkCopy(options, data.RetrieveIdentity(db));

				foreach (var d in data)
					Assert.That(d.ID, Is.GreaterThan(0));
			}
		}

		async Task BulkCopyRetrieveSequenceAsync(string context, BulkCopyType bulkCopyType)
		{
			var data = new[]
			{
				new Person { FirstName = "Neurologist"    , LastName = "test" },
				new Person { FirstName = "Sports Medicine", LastName = "test" },
				new Person { FirstName = "Optometrist"    , LastName = "test" },
				new Person { FirstName = "Pediatrics"     , LastName = "test"  },
				new Person { FirstName = "Psychiatry"     , LastName = "test"  }
			};

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				MySqlTestUtils.EnableNativeBulk(db, context);
				var options = new BulkCopyOptions
				{
					MaxBatchSize = 5,
					KeepIdentity = true,
					BulkCopyType = bulkCopyType,
					NotifyAfter = 3,
					RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied)
				};

				await db.BulkCopyAsync(options, data.RetrieveIdentity(db));

				foreach (var d in data)
					Assert.That(d.ID, Is.GreaterThan(0));
			}
		}

		[Test]
		public void TestTransaction1([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.GetTable<Parent>().Update(p => p.ParentID == 1, p => new Parent { Value1 = 1 });

				db.BeginTransaction();

				db.GetTable<Parent>().Update(p => p.ParentID == 1, p => new Parent { Value1 = null });

				Assert.That(db.GetTable<Parent>().First(p => p.ParentID == 1).Value1, Is.Null);

				db.RollbackTransaction();

				Assert.That(db.GetTable<Parent>().First(p => p.ParentID == 1).Value1, Is.EqualTo(1));
			}
		}

		[Test]
		public void TestTransaction2([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.GetTable<Parent>().Update(p => p.ParentID == 1, p => new Parent { Value1 = 1 });

				using (var tran = db.BeginTransaction())
				{
					db.GetTable<Parent>().Update(p => p.ParentID == 1, p => new Parent { Value1 = null });

					Assert.That(db.GetTable<Parent>().First(p => p.ParentID == 1).Value1, Is.Null);

					tran.Rollback();

					Assert.That(db.GetTable<Parent>().First(p => p.ParentID == 1).Value1, Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void TestBeginTransactionWithIsolationLevel([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.GetTable<Parent>().Update(p => p.ParentID == 1, p => new Parent { Value1 = 1 });

				using (var tran = db.BeginTransaction(IsolationLevel.Unspecified))
				{
					db.GetTable<Parent>().Update(p => p.ParentID == 1, p => new Parent { Value1 = null });

					Assert.That(db.GetTable<Parent>().First(p => p.ParentID == 1).Value1, Is.Null);

					tran.Rollback();

					Assert.That(db.GetTable<Parent>().First(p => p.ParentID == 1).Value1, Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void SchemaProviderTest([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			{
				var sp = db.DataProvider.GetSchemaProvider();
				var schema = sp.GetSchema(db);

				var systemTables = schema.Tables.Where(_ => _.CatalogName!.Equals("sys", StringComparison.OrdinalIgnoreCase)).ToList();

				Assert.That(systemTables.All(_ => _.IsProviderSpecific));

				var views = schema.Tables.Where(_ => _.IsView).ToList();
				Assert.That(views, Has.Count.EqualTo(1));
			}
		}

		public class ProcedureTestCase
		{
			public ProcedureTestCase(ProcedureSchema schema)
			{
				Schema = schema;
			}

			public ProcedureSchema Schema { get; }

			public override string ToString() => Schema.ProcedureName;
		}

		public static IEnumerable<ProcedureTestCase> ProcedureTestCases
		{
			get
			{
				// create procedure
				yield return new ProcedureTestCase(new ProcedureSchema()
				{
					CatalogName     = "SET_BY_TEST",
					ProcedureName   = "TestProcedure",
					MemberName      = "TestProcedure",
					IsDefaultSchema = true,
					IsLoaded        = true,
					Parameters      = new List<ParameterSchema>()
					{
						new ParameterSchema()
						{
							SchemaName    = "param3",
							SchemaType    = "INT",
							IsIn          = true,
							ParameterName = "param3",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						},
						new ParameterSchema()
						{
							SchemaName    = "param2",
							SchemaType    = "INT",
							IsIn          = true,
							IsOut         = true,
							ParameterName = "param2",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						},
						new ParameterSchema()
						{
							SchemaName    = "param1",
							SchemaType    = "INT",
							IsOut         = true,
							ParameterName = "param1",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						}
					},
					ResultTable = new TableSchema()
					{
						IsProcedureResult = true,
						TypeName          = "TestProcedureResult",
						Columns           = new List<ColumnSchema>()
						{
							new ColumnSchema()
							{
								ColumnName = "PersonID",
								ColumnType = "INT",
								MemberName = "PersonID",
								MemberType = "int",
								SystemType = typeof(int),
								DataType   = DataType.Int32
							},
							new ColumnSchema()
							{
								ColumnName = "FirstName",
								ColumnType = "VARCHAR(50)",
								MemberName = "FirstName",
								MemberType = "string",
								SystemType = typeof(string),
								DataType   = DataType.VarChar
							},
							new ColumnSchema()
							{
								ColumnName = "LastName",
								ColumnType = "VARCHAR(50)",
								MemberName = "LastName",
								MemberType = "string",
								SystemType = typeof(string),
								DataType   = DataType.VarChar
							},
							new ColumnSchema()
							{
								ColumnName = "MiddleName",
								ColumnType = "VARCHAR(50)",
								IsNullable = true,
								MemberName = "MiddleName",
								MemberType = "string",
								SystemType = typeof(string),
								DataType   = DataType.VarChar
							},
							new ColumnSchema()
							{
								ColumnName = "Gender",
								ColumnType = "CHAR(1)",
								MemberName = "Gender",
								MemberType = "char",
								SystemType = typeof(char),
								DataType   = DataType.Char
							}
						}
					},
					SimilarTables = new List<TableSchema>()
					{
						new TableSchema()
						{
							TableName = "person"
						}
					}
				});

				// create function
				yield return new ProcedureTestCase(new ProcedureSchema()
				{
					CatalogName     = "SET_BY_TEST",
					ProcedureName   = "TestFunction",
					MemberName      = "TestFunction",
					IsFunction      = true,
					IsDefaultSchema = true,
					Parameters      = new List<ParameterSchema>()
					{
						new ParameterSchema()
						{
							SchemaType    = "VARCHAR(10)",
							IsResult      = true,
							ParameterName = "par1",
							ParameterType = "string",
							SystemType    = typeof(string),
							DataType      = DataType.VarChar,
							Size          = 10
						},
						new ParameterSchema()
						{
							SchemaName    = "param",
							SchemaType    = "INT",
							IsIn          = true,
							ParameterName = "param",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						}
					}
				});

				// create function
				yield return new ProcedureTestCase(new ProcedureSchema()
				{
					CatalogName     = "SET_BY_TEST",
					ProcedureName   = "TestOutputParametersWithoutTableProcedure",
					MemberName      = "TestOutputParametersWithoutTableProcedure",
					IsDefaultSchema = true,
					IsLoaded        = true,
					Parameters      = new List<ParameterSchema>()
					{
						new ParameterSchema()
						{
							SchemaName    = "aInParam",
							SchemaType    = "VARCHAR(256)",
							IsIn          = true,
							ParameterName = "aInParam",
							ParameterType = "string",
							SystemType    = typeof(string),
							DataType      = DataType.VarChar,
							Size          = 256
						},
						new ParameterSchema()
						{
							SchemaName    = "aOutParam",
							SchemaType    = "TINYINT",
							IsOut         = true,
							ParameterName = "aOutParam",
							ParameterType = "bool?",
							SystemType    = typeof(bool),
							DataType      = DataType.SByte
						}
					}
				});
			}
		}

		[Test]
		public void ProceduresSchemaProviderTest(
			[IncludeDataSources(TestProvName.AllMySql)] string context,
			[ValueSource(nameof(ProcedureTestCases))] ProcedureTestCase testCase)
		{
			// TODO: add aggregate/udf functions test cases
			using (var db = (DataConnection)GetDataContext(context))
			{
				var expectedProc = testCase.Schema;

				expectedProc.CatalogName = TestUtils.GetDatabaseName(db, context);

				var schema     = db.DataProvider.GetSchemaProvider().GetSchema(db);
				var procedures = schema.Procedures.Where(_ => _.ProcedureName == expectedProc.ProcedureName).ToList();

				Assert.That(procedures, Has.Count.EqualTo(1));

				var procedure = procedures[0];

				Assert.Multiple(() =>
				{
					Assert.That(procedure.CatalogName!.ToLowerInvariant(), Is.EqualTo(expectedProc.CatalogName.ToLowerInvariant()));
					Assert.That(procedure.SchemaName, Is.EqualTo(expectedProc.SchemaName));
					Assert.That(procedure.MemberName, Is.EqualTo(expectedProc.MemberName));
					Assert.That(procedure.IsTableFunction, Is.EqualTo(expectedProc.IsTableFunction));
					Assert.That(procedure.IsAggregateFunction, Is.EqualTo(expectedProc.IsAggregateFunction));
					Assert.That(procedure.IsDefaultSchema, Is.EqualTo(expectedProc.IsDefaultSchema));
				});

				if (context.IsAnyOf(TestProvName.AllMySqlConnector) && procedure.ResultException != null)
				{
					Assert.Multiple(() =>
					{
						Assert.That(procedure.IsLoaded, Is.False);
						Assert.That(procedure.ResultException, Is.InstanceOf<InvalidOperationException>());
					});
					Assert.That(procedure.ResultException.Message, Is.EqualTo("There is no current result set."));
				}
				else
				{
					Assert.Multiple(() =>
					{
						Assert.That(procedure.IsLoaded, Is.EqualTo(expectedProc.IsLoaded));
						Assert.That(procedure.ResultException, Is.Null);
					});
				}

				Assert.That(procedure.Parameters, Has.Count.EqualTo(expectedProc.Parameters.Count));

				for (var i = 0; i < procedure.Parameters.Count; i++)
				{
					var actualParam = procedure.Parameters[i];
					var expectedParam = expectedProc.Parameters[i];

					Assert.Multiple(() =>
					{
						Assert.That(expectedParam, Is.Not.Null);

						Assert.That(actualParam.SchemaName, Is.EqualTo(expectedParam.SchemaName));
						Assert.That(actualParam.ParameterName, Is.EqualTo(expectedParam.ParameterName));
						Assert.That(actualParam.SchemaType, Is.EqualTo(expectedParam.SchemaType));
						Assert.That(actualParam.IsIn, Is.EqualTo(expectedParam.IsIn));
						Assert.That(actualParam.IsOut, Is.EqualTo(expectedParam.IsOut));
						Assert.That(actualParam.IsResult, Is.EqualTo(expectedParam.IsResult));
						Assert.That(actualParam.Size, Is.EqualTo(expectedParam.Size));
						Assert.That(actualParam.ParameterType, Is.EqualTo(expectedParam.ParameterType));
						Assert.That(actualParam.SystemType, Is.EqualTo(expectedParam.SystemType));
						Assert.That(actualParam.DataType, Is.EqualTo(expectedParam.DataType));
						Assert.That(actualParam.ProviderSpecificType, Is.EqualTo(expectedParam.ProviderSpecificType));
					});
				}

				if (expectedProc.ResultTable == null)
				{
					Assert.Multiple(() =>
					{
						Assert.That(procedure.ResultTable, Is.Null);

						// maybe it is worth changing
						Assert.That(procedure.SimilarTables, Is.Null);
					});
				}
				else
				{
					Assert.That(procedure.ResultTable, Is.Not.Null);

					var expectedTable = expectedProc.ResultTable;
					var actualTable = procedure.ResultTable!;

					Assert.Multiple(() =>
					{
						Assert.That(actualTable.ID, Is.EqualTo(expectedTable.ID));
						Assert.That(actualTable.CatalogName, Is.EqualTo(expectedTable.CatalogName));
						Assert.That(actualTable.SchemaName, Is.EqualTo(expectedTable.SchemaName));
						Assert.That(actualTable.TableName, Is.EqualTo(expectedTable.TableName));
						Assert.That(actualTable.Description, Is.EqualTo(expectedTable.Description));
						Assert.That(actualTable.IsDefaultSchema, Is.EqualTo(expectedTable.IsDefaultSchema));
						Assert.That(actualTable.IsView, Is.EqualTo(expectedTable.IsView));
						Assert.That(actualTable.IsProcedureResult, Is.EqualTo(expectedTable.IsProcedureResult));
						Assert.That(actualTable.TypeName, Is.EqualTo(expectedTable.TypeName));
						Assert.That(actualTable.IsProviderSpecific, Is.EqualTo(expectedTable.IsProviderSpecific));

						Assert.That(actualTable.ForeignKeys, Is.Not.Null);
					});
					Assert.Multiple(() =>
					{
						Assert.That(actualTable.ForeignKeys, Is.Empty);

						Assert.That(actualTable.Columns, Has.Count.EqualTo(expectedTable.Columns.Count));
					});

					foreach (var actualColumn in actualTable.Columns)
					{
						var expectedColumn = expectedTable.Columns.SingleOrDefault(_ => _.ColumnName == actualColumn.ColumnName)!;

						Assert.Multiple(() =>
						{
							Assert.That(expectedColumn, Is.Not.Null);

							Assert.That(actualColumn.ColumnType, Is.EqualTo(expectedColumn.ColumnType));
							Assert.That(actualColumn.IsNullable, Is.EqualTo(expectedColumn.IsNullable));
							Assert.That(actualColumn.IsIdentity, Is.EqualTo(expectedColumn.IsIdentity));
							Assert.That(actualColumn.IsPrimaryKey, Is.EqualTo(expectedColumn.IsPrimaryKey));
							Assert.That(actualColumn.PrimaryKeyOrder, Is.EqualTo(expectedColumn.PrimaryKeyOrder));
							Assert.That(actualColumn.Description, Is.EqualTo(expectedColumn.Description));
							Assert.That(actualColumn.MemberName, Is.EqualTo(expectedColumn.MemberName));
							Assert.That(actualColumn.MemberType, Is.EqualTo(expectedColumn.MemberType));
							Assert.That(actualColumn.ProviderSpecificType, Is.EqualTo(expectedColumn.ProviderSpecificType));
							Assert.That(actualColumn.SystemType, Is.EqualTo(expectedColumn.SystemType));
							Assert.That(actualColumn.DataType, Is.EqualTo(expectedColumn.DataType));
							Assert.That(actualColumn.SkipOnInsert, Is.EqualTo(expectedColumn.SkipOnInsert));
							Assert.That(actualColumn.SkipOnUpdate, Is.EqualTo(expectedColumn.SkipOnUpdate));
							Assert.That(actualColumn.Length, Is.EqualTo(expectedColumn.Length));
						});
						Assert.Multiple(() =>
						{
							Assert.That(actualColumn.Precision, Is.EqualTo(expectedColumn.Precision));
							Assert.That(actualColumn.Scale, Is.EqualTo(expectedColumn.Scale));
							Assert.That(actualColumn.Table, Is.EqualTo(actualTable));
						});
					}

					Assert.That(procedure.SimilarTables, Is.Not.Null);

					foreach (var table in procedure.SimilarTables!)
					{
						var tbl = expectedProc.SimilarTables!
							.SingleOrDefault(_ => _.TableName!.ToLowerInvariant() == table.TableName!.ToLowerInvariant());

						Assert.That(tbl, Is.Not.Null);
					}
				}
			}
		}

		[Test]
		public void FullTextIndexTest([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			{
				DatabaseSchema schema = db.DataProvider.GetSchemaProvider().GetSchema(db);
				var res = schema.Tables.FirstOrDefault(c => c.ID!.ToLowerInvariant().Contains("fulltextindex"));
				Assert.That(res, Is.Not.EqualTo(null));
			}
		}

		[Test(Description = "TODO: Issue not reproduced")]
		public void Issue1993([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			{
				DatabaseSchema schema = db.DataProvider.GetSchemaProvider().GetSchema(db);
				var table = schema.Tables.FirstOrDefault(t => t.ID!.ToLowerInvariant().Contains("issue1993"))!;
				Assert.That(table, Is.Not.Null);
				Assert.That(table.Columns, Has.Count.EqualTo(2));
				Assert.Multiple(() =>
				{
					Assert.That(table.Columns[0].ColumnName, Is.EqualTo("id"));
					Assert.That(table.Columns[1].ColumnName, Is.EqualTo("description"));
				});
			}
		}

		[Sql.Expression("@n:=@n+1", ServerSideOnly = true)]
		static int IncrementIndex()
		{
			throw new NotImplementedException();
		}

		[Description("https://stackoverflow.com/questions/50858172/linq2db-mysql-set-row-index/50958483")]
		[Test]
		public void RowIndexTest([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.NextQueryHints.Add("**/*(SELECT @n := 0) `rowcounter`*/");
				db.NextQueryHints.Add(", (SELECT @n := 0) `rowcounter`");

				var q =
					from p in db.Person
					select new
					{
						rank = IncrementIndex(),
						id   = p.ID
					};

				var list = q.ToList();
			}
		}

		[Test]
		public void TestTestProcedure([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			{
				int? param2 = 5;
				int? param1 = 11;

				var res = db.TestProcedure(123, ref param2, out param1);

				Assert.Multiple(() =>
				{
					Assert.That(param2, Is.EqualTo(10));
					Assert.That(param1, Is.EqualTo(133));
				});
				AreEqual(db.GetTable<Person>(), res);
			}
		}

		[Test]
		public void TestTestOutputParametersWithoutTableProcedure([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			{
				var res = db.TestOutputParametersWithoutTableProcedure("test", out var outParam);

				Assert.Multiple(() =>
				{
					Assert.That(outParam, Is.EqualTo(123));
					Assert.That(res, Is.EqualTo(1));
				});
			}
		}

		[Table]
		public class CreateTable
		{
			[Column                                                              ] public string? VarCharDefault;
			[Column(Length = 1)                                                  ] public string? VarChar1;
			[Column(Length = 112)                                                ] public string? VarChar112;
			[Column                                                              ] public char    Char;
			[Column(DataType = DataType.Char)                                    ] public string? Char255;
			[Column(DataType = DataType.Char, Length = 1)                        ] public string? Char1;
			[Column(DataType = DataType.Char, Length = 112)                      ] public string? Char112;
			[Column(Length = 1)                                                  ] public byte[]? VarBinary1;
			[Column                                                              ] public byte[]? VarBinary255;
			[Column(Length = 3)                                                  ] public byte[]? VarBinary3;
			[Column(DataType = DataType.Binary, Length = 1)                      ] public byte[]? Binary1;
			[Column(DataType = DataType.Binary)                                  ] public byte[]? Binary255;
			[Column(DataType = DataType.Binary, Length = 3)                      ] public byte[]? Binary3;
			[Column(DataType = DataType.Blob, Length = 200)                      ] public byte[]? TinyBlob;
			[Column(DataType = DataType.Blob, Length = 2000)                     ] public byte[]? Blob;
			[Column(DataType = DataType.Blob, Length = 200000)                   ] public byte[]? MediumBlob;
			[Column(DataType = DataType.Blob)                                    ] public byte[]? BlobDefault;
			[Column(DataType = DataType.Blob, Length = int.MaxValue)             ] public byte[]? LongBlob;
			[Column(DataType = DataType.Text, Length = 200)                      ] public string? TinyText;
			[Column(DataType = DataType.Text, Length = 2000)                     ] public string? Text;
			[Column(DataType = DataType.Text, Length = 200000)                   ] public string? MediumText;
			[Column(DataType = DataType.Text, Length = int.MaxValue)             ] public string? LongText;
			[Column(DataType = DataType.Text)                                    ] public string? TextDefault;
			[Column(DataType = DataType.Date)                                    ] public DateTime Date;
			[Column                                                              ] public DateTime DateTime;
			[Column(Precision = 3)                                               ] public DateTime DateTime3;
			// MySQL.Data provider has issues with timestamps
			// TODO: look into it later
			[Column(Configuration = "MySqlConnector.5.7")                        ]
			[Column(Configuration = "MySqlConnector.8.0")                        ]
			[Column(Configuration = "MariaDB.11")                                ]
			                                                                       public DateTimeOffset TimeStamp;
			[Column(Precision = 5, Configuration = "MySqlConnector.5.7", CreateFormat = "{0}{1}{2}{3} DEFAULT '1970-01-01 00:00:01'")]
			[Column(Precision = 5, Configuration = "MySqlConnector.8.0")         ]
			[Column(Precision = 5, Configuration = "MariaDB.11")                 ]
			                                                                       public DateTimeOffset TimeStamp5;
			[Column                                                              ] public TimeSpan Time;
			[Column(Precision = 2)                                               ] public TimeSpan Time2;
			[Column                                                              ] public sbyte TinyInt;
			[Column                                                              ] public byte UnsignedTinyInt;
			[Column                                                              ] public short SmallInt;
			[Column                                                              ] public ushort UnsignedSmallInt;
			[Column                                                              ] public int Int;
			[Column                                                              ] public uint UnsignedInt;
			[Column                                                              ] public long BigInt;
			[Column                                                              ] public ulong UnsignedBigInt;
			[Column                                                              ] public decimal Decimal;
			[Column(Precision = 15)                                              ] public decimal Decimal15_0;
			[Column(Scale = 5)                                                   ] public decimal Decimal10_5;
			[Column(Precision = 20, Scale = 2)                                   ] public decimal Decimal20_2;
			[Column                                                              ] public float Float;
			[Column(Precision = 10)                                              ] public float Float10;
			[Column                                                              ] public double Double;
			[Column(Precision = 30)                                              ] public double Float30;
			[Column                                                              ] public bool Bool;
			[Column(DataType = DataType.BitArray)                                ] public bool Bit1;
			[Column(DataType = DataType.BitArray)                                ] public byte Bit8;
			[Column(DataType = DataType.BitArray)                                ] public short Bit16;
			[Column(DataType = DataType.BitArray)                                ] public int Bit32;
			[Column(DataType = DataType.BitArray, Length = 10)                   ] public int Bit10;
			[Column(DataType = DataType.BitArray)                                ] public long Bit64;
			[Column(DataType = DataType.Json)                                    ] public string? Json;
			// not mysql type, just mapping testing
			[Column                                                              ] public Guid Guid;
		}

		[Test]
		public void TestCreateTable([IncludeDataSources(false, TestProvName.AllMySql)] string context)
		{
			var isMySqlConnector = context.IsAnyOf(TestProvName.AllMySqlConnector);

			// TODO: Following types not mapped to DataType enum now and should be defined explicitly using DbType:
			// - ENUM      : https://dev.mysql.com/doc/refman/8.0/en/enum.html
			// - SET       : https://dev.mysql.com/doc/refman/8.0/en/set.html
			// - YEAR      : https://dev.mysql.com/doc/refman/8.0/en/year.html
			// - MEDIUMINT : https://dev.mysql.com/doc/refman/8.0/en/integer-types.html
			// - SERIAL    : https://dev.mysql.com/doc/refman/8.0/en/numeric-type-syntax.html
			// - spatial types : https://dev.mysql.com/doc/refman/8.0/en/spatial-type-overview.html
			// - any additional attributes for column create clause
			//
			// Also we deliberatly don't support various deprecated modifiers:
			// - display width
			// - unsigned for non-integer types
			// - floating point (M,D) specifiers
			// - synonyms (except BOOLEAN)
			// etc
			using (var db = GetDataConnection(context))
			{
				// enable configuration use in mapping attributes
				db.AddMappingSchema(new MappingSchema(context));
				using (var table = db.CreateLocalTable<CreateTable>())
				{
					var sql = db.LastQuery!;

					Assert.That(sql, Does.Contain("\t`VarCharDefault`   VARCHAR(4000)         NULL"));
					Assert.That(sql, Does.Contain("\t`VarChar1`         VARCHAR(1)            NULL"));
					Assert.That(sql, Does.Contain("\t`VarChar112`       VARCHAR(112)          NULL"));
					Assert.That(sql, Does.Contain("\t`Char`             CHAR              NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Char1`            CHAR                  NULL"));
					Assert.That(sql, Does.Contain("\t`Char255`          CHAR(255)             NULL"));
					Assert.That(sql, Does.Contain("\t`Char112`          CHAR(112)             NULL"));
					Assert.That(sql, Does.Contain("\t`VarBinary1`       VARBINARY(1)          NULL"));
					Assert.That(sql, Does.Contain("\t`VarBinary255`     VARBINARY(255)        NULL"));
					Assert.That(sql, Does.Contain("\t`VarBinary3`       VARBINARY(3)          NULL"));
					Assert.That(sql, Does.Contain("\t`Binary1`          BINARY                NULL"));
					Assert.That(sql, Does.Contain("\t`Binary255`        BINARY(255)           NULL"));
					Assert.That(sql, Does.Contain("\t`Binary3`          BINARY(3)             NULL"));
					Assert.That(sql, Does.Contain("\t`TinyBlob`         TINYBLOB              NULL"));
					Assert.That(sql, Does.Contain("\t`Blob`             BLOB                  NULL"));
					Assert.That(sql, Does.Contain("\t`MediumBlob`       MEDIUMBLOB            NULL"));
					Assert.That(sql, Does.Contain("\t`LongBlob`         LONGBLOB              NULL"));
					Assert.That(sql, Does.Contain("\t`BlobDefault`      BLOB                  NULL"));
					Assert.That(sql, Does.Contain("\t`TinyText`         TINYTEXT              NULL"));
					Assert.That(sql, Does.Contain("\t`Text`             TEXT                  NULL"));
					Assert.That(sql, Does.Contain("\t`MediumText`       MEDIUMTEXT            NULL"));
					Assert.That(sql, Does.Contain("\t`LongText`         LONGTEXT              NULL"));
					Assert.That(sql, Does.Contain("\t`TextDefault`      TEXT                  NULL"));
					Assert.That(sql, Does.Contain("\t`Date`             DATE              NOT NULL"));
					Assert.That(sql, Does.Contain("\t`DateTime`         DATETIME          NOT NULL"));
					Assert.That(sql, Does.Contain("\t`DateTime3`        DATETIME(3)       NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Time2`            TIME(2)           NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Json`             JSON                  NULL"));
					if (isMySqlConnector)
					{
						Assert.That(sql, Does.Contain("\t`TimeStamp`        TIMESTAMP         NOT NULL"));
						Assert.That(sql, Does.Contain("\t`TimeStamp5`       TIMESTAMP(5)      NOT NULL"));
					}
					Assert.That(sql, Does.Contain("\t`Time`             TIME              NOT NULL"));
					Assert.That(sql, Does.Contain("\t`TinyInt`          TINYINT           NOT NULL"));
					Assert.That(sql, Does.Contain("\t`UnsignedTinyInt`  TINYINT UNSIGNED  NOT NULL"));
					Assert.That(sql, Does.Contain("\t`SmallInt`         SMALLINT          NOT NULL"));
					Assert.That(sql, Does.Contain("\t`UnsignedSmallInt` SMALLINT UNSIGNED NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Int`              INT               NOT NULL"));
					Assert.That(sql, Does.Contain("\t`UnsignedInt`      INT UNSIGNED      NOT NULL"));
					Assert.That(sql, Does.Contain("\t`BigInt`           BIGINT            NOT NULL"));
					Assert.That(sql, Does.Contain("\t`UnsignedBigInt`   BIGINT UNSIGNED   NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Decimal`          DECIMAL           NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Decimal15_0`      DECIMAL(15)       NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Decimal10_5`      DECIMAL(10, 5)    NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Decimal20_2`      DECIMAL(20, 2)    NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Float`            FLOAT             NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Float10`          FLOAT             NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Double`           DOUBLE            NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Float30`          DOUBLE            NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Bool`             BOOLEAN           NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Bit1`             BIT               NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Bit8`             BIT(8)            NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Bit16`            BIT(16)           NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Bit32`            BIT(32)           NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Bit10`            BIT(10)           NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Bit64`            BIT(64)           NOT NULL"));
					Assert.That(sql, Does.Contain("\t`Guid`             CHAR(36)          NOT NULL"));

					var testRecord = new CreateTable()
					{
						VarChar1         = "ы",
						VarCharDefault   = "ыsdf",
						VarChar112       = "ы123",
						Char             = 'я',
						Char1            = "!",
						Char255          = "!sdg3@",
						Char112          = "123 fd",
						VarBinary1       = new byte[] { 1 },
						VarBinary255     = new byte[] { 1, 4, 22 },
						VarBinary3       = new byte[] { 1, 2, 4 },
						Binary1          = new byte[] { 22 },
						Binary255        = new byte[] { 22, 44, 21 },
						Binary3          = new byte[] { 1, 33 },
						TinyBlob         = new byte[] { 3, 2, 1 },
						Blob             = new byte[] { 13, 2, 1 },
						MediumBlob       = new byte[] { 23, 2, 1 },
						BlobDefault      = new byte[] { 33, 2, 1 },
						LongBlob         = new byte[] { 133, 2, 1 },
						TinyText         = "12я3",
						Text             = "1232354",
						MediumText       = "1df3",
						LongText         = "1v23",
						TextDefault      = "12 #3",
						Date             = new DateTime(2123, 2, 3),
						DateTime         = new DateTime(2123, 2, 3, 11, 22, 33),
						DateTime3        = new DateTime(2123, 2, 3, 11, 22, 33, 123),
						TimeStamp        = new DateTimeOffset(2023, 2, 3, 11, 22, 33, TimeSpan.FromMinutes(60)),
						TimeStamp5       = new DateTimeOffset(2013, 2, 3, 11, 22, 33, 123, TimeSpan.FromMinutes(-60)).AddTicks(45000),
						Time             = new TimeSpan(-5, 56, 7),
						Time2            = new TimeSpan(5, 56, 7, 12),
						TinyInt          = -123,
						UnsignedTinyInt  = 223,
						SmallInt         = short.MinValue,
						UnsignedSmallInt = ushort.MaxValue,
						Int              = int.MinValue,
						UnsignedInt      = uint.MaxValue,
						BigInt           = long.MinValue,
						UnsignedBigInt   = ulong.MaxValue,
						Decimal          = 1234m,
						Decimal15_0      = 123456789012345m,
						Decimal10_5      = -12345.2345m,
						Decimal20_2      = -3412345.23m,
						Float            = 3244.23999f,
						Float10          = 124.354f,
						Double           = 452.23523d,
						Float30          = 332.235d,
						Bool             = true,
						Bit1             = true,
						Bit8             = 0x07,
						Bit16            = 0xFE,
						Bit32            = 0xADFE,
						Bit10            = 0x003F,
						Bit64            = 0xDEADBEAF,
						Json             = /*lang=json,strict*/ "{\"x\": 10}",
						Guid             = TestData.Guid1
					};

					db.Insert(testRecord);
					var readRecord = table.Single();

					Assert.Multiple(() =>
					{
						Assert.That(readRecord.VarChar1, Is.EqualTo(testRecord.VarChar1));
						Assert.That(readRecord.VarCharDefault, Is.EqualTo(testRecord.VarCharDefault));
						Assert.That(readRecord.VarChar112, Is.EqualTo(testRecord.VarChar112));
						Assert.That(readRecord.Char, Is.EqualTo(testRecord.Char));
						Assert.That(readRecord.Char1, Is.EqualTo(testRecord.Char1));
						Assert.That(readRecord.Char255, Is.EqualTo(testRecord.Char255));
						Assert.That(readRecord.Char112, Is.EqualTo(testRecord.Char112));
						Assert.That(readRecord.VarBinary1, Is.EqualTo(testRecord.VarBinary1));
						Assert.That(readRecord.VarBinary255, Is.EqualTo(testRecord.VarBinary255));
						Assert.That(readRecord.VarBinary3, Is.EqualTo(testRecord.VarBinary3));
						Assert.That(readRecord.Binary1, Is.EqualTo(testRecord.Binary1));
						// we trim padding only from char fields
						Assert.That(readRecord.Binary255, Is.EqualTo(testRecord.Binary255.Concat(new byte[252])));
						Assert.That(readRecord.Binary3, Is.EqualTo(testRecord.Binary3.Concat(new byte[1])));
						Assert.That(readRecord.TinyBlob, Is.EqualTo(testRecord.TinyBlob));
						Assert.That(readRecord.Blob, Is.EqualTo(testRecord.Blob));
						Assert.That(readRecord.MediumBlob, Is.EqualTo(testRecord.MediumBlob));
						Assert.That(readRecord.BlobDefault, Is.EqualTo(testRecord.BlobDefault));
						Assert.That(readRecord.LongBlob, Is.EqualTo(testRecord.LongBlob));
						Assert.That(readRecord.TinyText, Is.EqualTo(testRecord.TinyText));
						Assert.That(readRecord.Text, Is.EqualTo(testRecord.Text));
						Assert.That(readRecord.MediumText, Is.EqualTo(testRecord.MediumText));
						Assert.That(readRecord.LongText, Is.EqualTo(testRecord.LongText));
						Assert.That(readRecord.TextDefault, Is.EqualTo(testRecord.TextDefault));
						Assert.That(readRecord.Date, Is.EqualTo(testRecord.Date));
						Assert.That(readRecord.DateTime, Is.EqualTo(testRecord.DateTime));
						Assert.That(readRecord.DateTime3, Is.EqualTo(testRecord.DateTime3));
						Assert.That(readRecord.Time2, Is.EqualTo(testRecord.Time2));
						Assert.That(readRecord.Json, Is.EqualTo(testRecord.Json));
					});
					if (isMySqlConnector)
					{
						Assert.Multiple(() =>
						{
							Assert.That(readRecord.TimeStamp, Is.EqualTo(testRecord.TimeStamp));
							Assert.That(readRecord.TimeStamp5, Is.EqualTo(testRecord.TimeStamp5));
						});
					}

					Assert.Multiple(() =>
					{
						Assert.That(readRecord.Time, Is.EqualTo(testRecord.Time));
						Assert.That(readRecord.TinyInt, Is.EqualTo(testRecord.TinyInt));
						Assert.That(readRecord.UnsignedTinyInt, Is.EqualTo(testRecord.UnsignedTinyInt));
						Assert.That(readRecord.SmallInt, Is.EqualTo(testRecord.SmallInt));
						Assert.That(readRecord.UnsignedSmallInt, Is.EqualTo(testRecord.UnsignedSmallInt));
						Assert.That(readRecord.Int, Is.EqualTo(testRecord.Int));
						Assert.That(readRecord.UnsignedInt, Is.EqualTo(testRecord.UnsignedInt));
						Assert.That(readRecord.BigInt, Is.EqualTo(testRecord.BigInt));
						Assert.That(readRecord.UnsignedBigInt, Is.EqualTo(testRecord.UnsignedBigInt));
						Assert.That(readRecord.Decimal, Is.EqualTo(testRecord.Decimal));
						Assert.That(readRecord.Decimal15_0, Is.EqualTo(testRecord.Decimal15_0));
						Assert.That(readRecord.Decimal10_5, Is.EqualTo(testRecord.Decimal10_5));
						Assert.That(readRecord.Decimal20_2, Is.EqualTo(testRecord.Decimal20_2));
						Assert.That(readRecord.Float, Is.EqualTo(testRecord.Float));
						Assert.That(readRecord.Float10, Is.EqualTo(testRecord.Float10));
						Assert.That(readRecord.Double, Is.EqualTo(testRecord.Double));
						Assert.That(readRecord.Float30, Is.EqualTo(testRecord.Float30));
						Assert.That(readRecord.Bool, Is.EqualTo(testRecord.Bool));
						Assert.That(readRecord.Bit1, Is.EqualTo(testRecord.Bit1));
						Assert.That(readRecord.Bit8, Is.EqualTo(testRecord.Bit8));
						Assert.That(readRecord.Bit16, Is.EqualTo(testRecord.Bit16));
						Assert.That(readRecord.Bit32, Is.EqualTo(testRecord.Bit32));
						Assert.That(readRecord.Bit10, Is.EqualTo(testRecord.Bit10));
						Assert.That(readRecord.Bit64, Is.EqualTo(testRecord.Bit64));
						Assert.That(readRecord.Guid, Is.EqualTo(testRecord.Guid));
					});
				}
			}
		}

		[Table]
		public class TestSchemaTypesTable
		{
			[Column                                                  ] public string? VarCharDefault;
			[Column(Length = 1)                                      ] public string? VarChar1;
			[Column(Length = 112)                                    ] public string? VarChar112;
			[Column                                                  ] public char    Char;
			[Column(DataType = DataType.Char)                        ] public string? Char255;
			[Column(DataType = DataType.Char, Length = 1)            ] public string? Char1;
			[Column(DataType = DataType.Char, Length = 112)          ] public string? Char112;
			[Column(Length = 1)                                      ] public byte[]? VarBinary1;
			[Column                                                  ] public byte[]? VarBinary255;
			[Column(Length = 3)                                      ] public byte[]? VarBinary3;
			[Column(DataType = DataType.Binary, Length = 1)          ] public byte[]? Binary1;
			[Column(DataType = DataType.Binary)                      ] public byte[]? Binary255;
			[Column(DataType = DataType.Binary, Length = 3)          ] public byte[]? Binary3;
			[Column(DataType = DataType.Blob, Length = 200)          ] public byte[]? TinyBlob;
			[Column(DataType = DataType.Blob, Length = 2000)         ] public byte[]? Blob;
			[Column(DataType = DataType.Blob, Length = 200000)       ] public byte[]? MediumBlob;
			[Column(DataType = DataType.Blob)                        ] public byte[]? BlobDefault;
			[Column(DataType = DataType.Blob, Length = int.MaxValue) ] public byte[]? LongBlob;
			[Column(DataType = DataType.Text, Length = 200)          ] public string? TinyText;
			[Column(DataType = DataType.Text, Length = 2000)         ] public string? Text;
			[Column(DataType = DataType.Text, Length = 200000)       ] public string? MediumText;
			[Column(DataType = DataType.Text, Length = int.MaxValue) ] public string? LongText;
			[Column(DataType = DataType.Text)                        ] public string? TextDefault;
			[Column(DataType = DataType.Date)                        ] public DateTime Date;
			[Column                                                  ] public DateTime DateTime;
			[Column(Precision = 3)                                   ] public DateTime DateTime3;
			[Column                                                  ] public DateTimeOffset TimeStamp;
			[Column(Precision = 5, Configuration = ProviderName.MySql57, CreateFormat = "{0}{1}{2}{3} DEFAULT '1970-01-01 00:00:01'")]
			[Column(Precision = 5)                                   ] public DateTimeOffset TimeStamp5;
			[Column                                                  ] public TimeSpan Time;
			[Column(Precision = 2)                                   ] public TimeSpan Time2;
			[Column                                                  ] public sbyte TinyInt;
			[Column                                                  ] public byte UnsignedTinyInt;
			[Column                                                  ] public short SmallInt;
			[Column                                                  ] public ushort UnsignedSmallInt;
			[Column                                                  ] public int Int;
			[Column                                                  ] public uint UnsignedInt;
			[Column                                                  ] public long BigInt;
			[Column                                                  ] public ulong UnsignedBigInt;
			[Column                                                  ] public decimal Decimal;
			[Column(Precision = 15)                                  ] public decimal Decimal15_0;
			[Column(Scale = 5)                                       ] public decimal Decimal10_5;
			[Column(Precision = 20, Scale = 2)                       ] public decimal Decimal20_2;
			[Column                                                  ] public float Float;
			[Column(Precision = 10)                                  ] public float Float10;
			[Column                                                  ] public double Double;
			[Column(Precision = 30)                                  ] public double Float30;
			[Column                                                  ] public bool Bool;
			[Column(DataType = DataType.BitArray)                    ] public bool Bit1;
			[Column(DataType = DataType.BitArray)                    ] public byte Bit8;
			[Column(DataType = DataType.BitArray)                    ] public short Bit16;
			[Column(DataType = DataType.BitArray)                    ] public int Bit32;
			[Column(DataType = DataType.BitArray, Length = 10)       ] public int Bit10;
			[Column(DataType = DataType.BitArray)                    ] public long Bit64;
			[Column(DataType = DataType.Json)                        ] public string? Json;
			// not mysql type, just mapping testing
			[Column                                                  ] public Guid Guid;
			[Column(DbType = "ENUM('one', 'two')")                   ] public string? Enum;
			[Column(DbType = "SET('one', 'two')")                    ] public string? Set;
			[Column(DbType = "YEAR")                                 ] public int Year;
			[Column(DbType = "MEDIUMINT")                            ] public int MediumInt;
			[Column(DbType = "MEDIUMINT UNSIGNED")                   ] public uint UnsignedMediumInt;
			[Column(DbType = "GEOMETRY")                             ] public object? Geometry;
			[Column(DbType = "POINT")                                ] public object? Point;
			[Column(DbType = "LINESTRING")                           ] public object? LineString;
			[Column(DbType = "POLYGON")                              ] public object? Polygon;
			[Column(DbType = "MULTIPOINT")                           ] public object? MultiPoint;
			[Column(DbType = "MULTILINESTRING")                      ] public object? MultiLineString;
			[Column(DbType = "MULTIPOLYGON")                         ] public object? MultiPolygon;
			[Column(DbType = "GEOMETRYCOLLECTION")                   ] public object? GeometryCollection;
		}

		[Test]
		public void TestTypesSchema([IncludeDataSources(false, TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				// enable configuration use in mapping attributes
				db.AddMappingSchema(new MappingSchema(context));
				using (var table = db.CreateLocalTable<TestSchemaTypesTable>())
				{
					var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions() { GetProcedures = false });

					var tableSchema = schema.Tables.Where(t => t.TableName!.ToLowerInvariant() == "testschematypestable").SingleOrDefault()!;
					Assert.That(tableSchema, Is.Not.Null);

					assertColumn("VarCharDefault"    , "string"  , DataType.VarChar);
					assertColumn("VarChar1"          , "char?"   , DataType.VarChar);
					assertColumn("VarChar112"        , "string"  , DataType.VarChar);
					assertColumn("Char"              , "char"    , DataType.Char);
					assertColumn("Char1"             , "char?"   , DataType.Char);
					assertColumn("Char255"           , "string"  , DataType.Char);
					assertColumn("Char112"           , "string"  , DataType.Char);
					assertColumn("VarBinary1"        , "byte[]"  , DataType.VarBinary);
					assertColumn("VarBinary255"      , "byte[]"  , DataType.VarBinary);
					assertColumn("VarBinary3"        , "byte[]"  , DataType.VarBinary);
					assertColumn("Binary1"           , "byte[]"  , DataType.Binary);
					assertColumn("Binary255"         , "byte[]"  , DataType.Binary);
					assertColumn("Binary3"           , "byte[]"  , DataType.Binary);
					assertColumn("TinyBlob"          , "byte[]"  , DataType.Blob);
					assertColumn("Blob"              , "byte[]"  , DataType.Blob);
					assertColumn("MediumBlob"        , "byte[]"  , DataType.Blob);
					assertColumn("LongBlob"          , "byte[]"  , DataType.Blob);
					assertColumn("BlobDefault"       , "byte[]"  , DataType.Blob);
					assertColumn("TinyText"          , "string"  , DataType.Text);
					assertColumn("Text"              , "string"  , DataType.Text);
					assertColumn("MediumText"        , "string"  , DataType.Text);
					assertColumn("LongText"          , "string"  , DataType.Text);
					assertColumn("TextDefault"       , "string"  , DataType.Text);
					assertColumn("Date"              , "DateTime", DataType.Date);
					assertColumn("DateTime"          , "DateTime", DataType.DateTime);
					assertColumn("TimeStamp"         , "DateTime", DataType.DateTime);
					assertColumn("Time"              , "TimeSpan", DataType.Time);
					assertColumn("TinyInt"           , "sbyte"   , DataType.SByte);
					assertColumn("UnsignedTinyInt"   , "byte"    , DataType.Byte);
					assertColumn("SmallInt"          , "short"   , DataType.Int16);
					assertColumn("UnsignedSmallInt"  , "ushort"  , DataType.UInt16);
					assertColumn("Int"               , "int"     , DataType.Int32);
					assertColumn("UnsignedInt"       , "uint"    , DataType.UInt32);
					assertColumn("BigInt"            , "long"    , DataType.Int64);
					assertColumn("UnsignedBigInt"    , "ulong"   , DataType.UInt64);
					assertColumn("Decimal"           , "decimal" , DataType.Decimal);
					assertColumn("Decimal15_0"       , "decimal" , DataType.Decimal);
					assertColumn("Decimal10_5"       , "decimal" , DataType.Decimal);
					assertColumn("Decimal20_2"       , "decimal" , DataType.Decimal);
					assertColumn("Float"             , "float"   , DataType.Single);
					assertColumn("Float10"           , "float"   , DataType.Single);
					assertColumn("Double"            , "double"  , DataType.Double);
					assertColumn("Float30"           , "double"  , DataType.Double);
					assertColumn("Bool"              , "bool"    , DataType.SByte);
					assertColumn("Bit1"              , "bool"    , DataType.BitArray);
					assertColumn("Bit8"              , "byte"    , DataType.BitArray);
					assertColumn("Bit16"             , "ushort"  , DataType.BitArray);
					assertColumn("Bit32"             , "uint"    , DataType.BitArray);
					assertColumn("Bit10"             , "ushort"  , DataType.BitArray);
					assertColumn("Bit64"             , "ulong"   , DataType.BitArray);
					assertColumn("Guid"              , "string"  , DataType.Char);
					assertColumn("Enum"              , "string"  , DataType.VarChar);
					assertColumn("Set"               , "string"  , DataType.VarChar);
					assertColumn("Year"              , "int"     , DataType.Int32);
					assertColumn("MediumInt"         , "int"     , DataType.Int32);
					assertColumn("UnsignedMediumInt" , "uint"    , DataType.UInt32);
					assertColumn("Geometry"          , "byte[]"  , DataType.Undefined);
					assertColumn("Point"             , "byte[]"  , DataType.Undefined);
					assertColumn("LineString"        , "byte[]"  , DataType.Undefined);
					assertColumn("Polygon"           , "byte[]"  , DataType.Undefined);
					assertColumn("MultiPoint"        , "byte[]"  , DataType.Undefined);
					assertColumn("MultiLineString"   , "byte[]"  , DataType.Undefined);
					assertColumn("MultiPolygon"      , "byte[]"  , DataType.Undefined);
					assertColumn("GeometryCollection", "byte[]"  , DataType.Undefined);

					assertColumn("DateTime3" , "DateTime", DataType.DateTime);
					assertColumn("Time2"     , "TimeSpan", DataType.Time);
					assertColumn("TimeStamp5", "DateTime", DataType.DateTime);

					if (context.IsAnyOf(TestProvName.AllMySqlServer))
						assertColumn("Json", "string", DataType.Json);
					else
						assertColumn("Json", "string", DataType.Text);

					void assertColumn(string name, string type, DataType dataType)
					{
						var column = tableSchema.Columns.Where(c => c.ColumnName == name).SingleOrDefault()!;
						Assert.That(column, Is.Not.Null);
						Assert.Multiple(() =>
						{
							Assert.That(column.MemberType, Is.EqualTo(type));
							Assert.That(column.DataType, Is.EqualTo(dataType));
						});
					}
				}
			}
		}

		[Test]
		public void TestProcedureTypesParameters([IncludeDataSources(false, TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions() { GetTables = false });

				var proc = schema.Procedures.Where(t => t.ProcedureName == "Issue2313Parameters").SingleOrDefault()!;

				Assert.That(proc, Is.Not.Null);

				assertParameter("VarCharDefault"    , "string"   , DataType.VarChar);
				assertParameter("VarChar1"          , "char?"    , DataType.VarChar);
				assertParameter("Char255"           , "string"   , DataType.Char);
				assertParameter("Char1"             , "char?"    , DataType.Char);
				assertParameter("VarBinary255"      , "byte[]"   , DataType.VarBinary);
				assertParameter("Binary255"         , "byte[]"   , DataType.Binary);
				assertParameter("TinyBlob"          , "byte[]"   , DataType.Blob);
				assertParameter("Blob"              , "byte[]"   , DataType.Blob);
				assertParameter("MediumBlob"        , "byte[]"   , DataType.Blob);
				assertParameter("LongBlob"          , "byte[]"   , DataType.Blob);
				assertParameter("TinyText"          , "string"   , DataType.Text);
				assertParameter("Text"              , "string"   , DataType.Text);
				assertParameter("MediumText"        , "string"   , DataType.Text);
				assertParameter("LongText"          , "string"   , DataType.Text);
				assertParameter("Date"              , "DateTime?", DataType.Date);
				assertParameter("DateTime"          , "DateTime?", DataType.DateTime);
				assertParameter("TimeStamp"         , "DateTime?", DataType.DateTime);
				assertParameter("Time"              , "TimeSpan?", DataType.Time);
				assertParameter("TinyInt"           , "sbyte?"   , DataType.SByte);
				assertParameter("TinyIntUnsigned"   , "byte?"    , DataType.Byte);
				assertParameter("SmallInt"          , "short?"   , DataType.Int16);
				assertParameter("SmallIntUnsigned"  , "ushort?"  , DataType.UInt16);
				assertParameter("MediumInt"         , "int?"     , DataType.Int32);
				assertParameter("MediumIntUnsigned" , "uint?"    , DataType.UInt32);
				assertParameter("Int"               , "int?"     , DataType.Int32);
				assertParameter("IntUnsigned"       , "uint?"    , DataType.UInt32);
				assertParameter("BigInt"            , "long?"    , DataType.Int64);
				assertParameter("BigIntUnsigned"    , "ulong?"   , DataType.UInt64);
				assertParameter("Decimal"           , "decimal?" , DataType.Decimal);
				assertParameter("Float"             , "float?"   , DataType.Single);
				assertParameter("Double"            , "double?"  , DataType.Double);
				assertParameter("Boolean"           , "bool?"    , DataType.SByte);
				assertParameter("Bit1"              , "bool?"    , DataType.BitArray);
				assertParameter("Bit8"              , "byte?"    , DataType.BitArray);
				assertParameter("Bit10"             , "ushort?"  , DataType.BitArray);
				assertParameter("Bit16"             , "ushort?"  , DataType.BitArray);
				assertParameter("Bit32"             , "uint?"    , DataType.BitArray);
				assertParameter("Bit64"             , "ulong?"   , DataType.BitArray);
				assertParameter("Enum"              , "string"   , DataType.VarChar);
				assertParameter("Set"               , "string"   , DataType.VarChar);
				assertParameter("Year"              , "int?"     , DataType.Int32);
				assertParameter("Geometry"          , "byte[]"   , DataType.Undefined);
				assertParameter("Point"             , "byte[]"   , DataType.Undefined);
				assertParameter("LineString"        , "byte[]"   , DataType.Undefined);
				assertParameter("Polygon"           , "byte[]"   , DataType.Undefined);
				assertParameter("MultiPoint"        , "byte[]"   , DataType.Undefined);
				assertParameter("MultiLineString"   , "byte[]"   , DataType.Undefined);
				assertParameter("MultiPolygon"      , "byte[]"   , DataType.Undefined);
				assertParameter("GeometryCollection", "byte[]"   , DataType.Undefined);

				if (context.IsAnyOf(TestProvName.AllMySqlServer))
					assertParameter("Json", "string", DataType.Json);
				else
					assertParameter("Json", "string", DataType.Text);

				void assertParameter(string name, string type, DataType dataType)
				{
					var parameter = proc.Parameters.Where(c => c.ParameterName == name).SingleOrDefault()!;

					Assert.That(parameter, Is.Not.Null);

					Assert.Multiple(() =>
					{
						Assert.That(parameter.ParameterType, Is.EqualTo(type));
						Assert.That(parameter.DataType, Is.EqualTo(dataType));
					});
				}
			}
		}

		[Test]
		public void TestProcedureTypesResults([IncludeDataSources(false, TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions() { GetTables = false });

				var proc = schema.Procedures.SingleOrDefault(t => t.ProcedureName == "Issue2313Results")!;

				Assert.That(proc, Is.Not.Null);
				Assert.That(proc.ResultTable, Is.Not.Null);

				assertColumn("VarCharDefault"    , "string"   , DataType.VarChar);
				assertColumn("VarChar1"          , "char?"    , DataType.VarChar);
				assertColumn("Char255"           , "string"   , DataType.Char);
				assertColumn("Char1"             , "char?"    , DataType.Char);
				assertColumn("VarBinary255"      , "byte[]"   , DataType.VarBinary);
				assertColumn("Binary255"         , "byte[]"   , DataType.Binary);
				assertColumn("TinyBlob"          , "byte[]"   , DataType.Blob);
				assertColumn("Blob"              , "byte[]"   , DataType.Blob);
				assertColumn("MediumBlob"        , "byte[]"   , DataType.Blob);
				assertColumn("LongBlob"          , "byte[]"   , DataType.Blob);
				assertColumn("TinyText"          , "string"   , DataType.Text);
				assertColumn("Text"              , "string"   , DataType.Text);
				assertColumn("MediumText"        , "string"   , DataType.Text);
				assertColumn("LongText"          , "string"   , DataType.Text);
				assertColumn("Date"              , "DateTime?", DataType.Date);
				assertColumn("DateTime"          , "DateTime?", DataType.DateTime);
				assertColumn("TimeStamp"         , "DateTime?", DataType.DateTime);
				assertColumn("Time"              , "TimeSpan?", DataType.Time);
				assertColumn("TinyInt"           , "sbyte?"   , DataType.SByte);
				assertColumn("TinyIntUnsigned"   , "byte?"    , DataType.Byte);
				assertColumn("SmallInt"          , "short?"   , DataType.Int16);
				assertColumn("SmallIntUnsigned"  , "ushort?"  , DataType.UInt16);
				assertColumn("MediumInt"         , "int?"     , DataType.Int32);
				assertColumn("MediumIntUnsigned" , "uint?"    , DataType.UInt32);
				assertColumn("Int"               , "int?"     , DataType.Int32);
				assertColumn("IntUnsigned"       , "uint?"    , DataType.UInt32);
				assertColumn("BigInt"            , "long?"    , DataType.Int64);
				assertColumn("BigIntUnsigned"    , "ulong?"   , DataType.UInt64);
				assertColumn("Decimal"           , "decimal?" , DataType.Decimal);
				assertColumn("Float"             , "float?"   , DataType.Single);
				assertColumn("Double"            , "double?"  , DataType.Double);
				assertColumn("Boolean"           , "bool?"    , DataType.SByte);
				assertColumn("Bit1"              , "bool?"    , DataType.BitArray);
				assertColumn("Bit8"              , "byte?"    , DataType.BitArray);
				assertColumn("Bit10"             , "ushort?"  , DataType.BitArray);
				assertColumn("Bit16"             , "ushort?"  , DataType.BitArray);
				assertColumn("Bit32"             , "uint?"    , DataType.BitArray);
				assertColumn("Bit64"             , "ulong?"   , DataType.BitArray);
				assertColumn("Year"              , "int?"     , DataType.Int32);

				// mysql.data cannot handle json procedure parameter
				if (context.IsAnyOf(TestProvName.AllMySqlConnector))
				{
					assertColumn("Point"               , "byte[]", DataType.Undefined);
					assertColumn("LineString"          , "byte[]", DataType.Undefined);
					assertColumn("Polygon"             , "byte[]", DataType.Undefined);
					assertColumn("MultiPoint"          , "byte[]", DataType.Undefined);
					assertColumn("MultiLineString"     , "byte[]", DataType.Undefined);
					assertColumn("MultiPolygon"        , "byte[]", DataType.Undefined);
					assertColumn("Geometry"            , "byte[]", DataType.Undefined);
					assertColumn("GeometryCollection"  , "byte[]", DataType.Undefined);

					assertColumn("Json"    , "string", !context.IsAnyOf(TestProvName.AllMySqlServer) ? DataType.Text : DataType.Json);
					assertColumn("Enum"    , "string", DataType.VarChar);
					assertColumn("Set"     , "string", DataType.VarChar);
				}
				else
				{
					assertColumn("Enum", "string", DataType.Char);
					assertColumn("Set" , "string", DataType.Char);
				}

				void assertColumn(string name, string type, DataType dataType)
				{
					// m'kaaaay...
					name       = "`" + name + "`";
					var column = proc.ResultTable!.Columns.SingleOrDefault(c => c.ColumnName == name)!;

					Assert.That(column, Is.Not.Null);

					Assert.Multiple(() =>
					{
						Assert.That(column.MemberType, Is.EqualTo(type));
						Assert.That(column.DataType, Is.EqualTo(dataType));
					});
				}
			}
		}

		[Test]
		public void TestModule([IncludeDataSources(false, TestProvName.AllMariaDB)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.Execute("SET SQL_MODE='ORACLE'");

				try
				{
					if (context.IsAnyOf(TestProvName.AllMySqlConnector))
					{
						Assert.Multiple(() =>
						{
							Assert.That(db.QueryProc<int>("TEST_PROCEDURE", new { i = 1 }).First(), Is.EqualTo(4));
							Assert.That(db.QueryProc<int>("TEST_PACKAGE1.TEST_PROCEDURE", new { i = 1 }).First(), Is.EqualTo(2));
							Assert.That(db.QueryProc<int>("TEST_PACKAGE2.TEST_PROCEDURE", new { i = 1 }).First(), Is.EqualTo(3));
						});
					}
					else
					{
						Assert.Multiple(() =>
						{
							// MySql.Data cannot call package proedures using CommandType.StoredProcedure
							// and we cannot generate "CALL procedure" statement for it as it will break
							// SchemaOnly procedure calls with output parameters
							Assert.That(db.Query<int>("CALL TEST_PROCEDURE(@i)", new { i = 1 }).First(), Is.EqualTo(4));
							Assert.That(db.Query<int>("CALL TEST_PACKAGE1.TEST_PROCEDURE(@i)", new { i = 1 }).First(), Is.EqualTo(2));
							Assert.That(db.Query<int>("CALL TEST_PACKAGE2.TEST_PROCEDURE(@i)", new { i = 1 }).First(), Is.EqualTo(3));
						});
					}

					Assert.Multiple(() =>
					{
						Assert.That(db.Person.Select(p => MariaDBModuleFunctions.TestFunction(1)).First(), Is.EqualTo(4));
						Assert.That(db.Person.Select(p => MariaDBModuleFunctions.TestFunctionP1(1)).First(), Is.EqualTo(2));
						Assert.That(db.Person.Select(p => MariaDBModuleFunctions.TestFunctionP2(1)).First(), Is.EqualTo(3));
					});
				}
				finally
				{
					db.Execute("set session sql_mode=default");
				}
			}
		}

		sealed class Issue3611Table
		{
			[Column(Length = 2000, DataType = DataType.VarChar)]   public string? VarChar   { get; set; }
			[Column(Length = 2000, DataType = DataType.VarBinary)] public byte[]? VarBinary { get; set; }
		}

		[Test]
		public void Issue3611([IncludeDataSources(false, TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable<Issue3611Table>())
			{
				// VARCHAR/VARBINARY max length depends on many factors:
				// 1. MySQL version: 255 prior to 5.0.3 and 65535 for 5.0.3+
				// 2. column encoding: for utf8 it will be 21844
				// 3. other columns. total row size is limited to 64K
				Assert.That(db.LastQuery!, Does.Contain("VARCHAR(2000)"));
				Assert.That(db.LastQuery!, Does.Contain("VARBINARY(2000)"));
			}
		}

		[Table]
		sealed class TinyIntTestTable
		{
			[Column(DbType = "tinyint(1)")]          public byte  Byte  { get; set; }
			[Column(DbType = "tinyint(1) unsigned")] public sbyte SByte { get; set; }
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/86")]
		public void TinyInt1IsByte([IncludeDataSources(false, TestProvName.AllMySql)] string context)
		{
			// TODO: when option implemented, update test with/without option set
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable<TinyIntTestTable>();

			var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions()
			{
				LoadTable = t => t.Name == nameof(TinyIntTestTable)
			});

			var table = schema.Tables.FirstOrDefault(t => t.TableName == nameof(TinyIntTestTable));

			Assert.That(table, Is.Not.Null);

			var byteColumn  = table!.Columns.FirstOrDefault(c => c.ColumnName == nameof(TinyIntTestTable.Byte));
			var sbyteColumn = table.Columns.FirstOrDefault(c => c.ColumnName == nameof(TinyIntTestTable.SByte));

			Assert.Multiple(() =>
			{
				Assert.That(byteColumn, Is.Not.Null);
				Assert.That(sbyteColumn, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(byteColumn!.SystemType, Is.EqualTo(typeof(byte)));
				Assert.That(sbyteColumn!.SystemType, Is.EqualTo(typeof(sbyte)));
			});
		}


		#region Issue 4439
		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4439")]
		public void Issue4439Test([IncludeDataSources(false, TestProvName.AllMySql)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable<TEST_TB_AA>();

			db.BeginTransaction();
			try
			{
				db.Insert(new TEST_TB_AA() { CODE_AA = "AA" });
				using (var tempTB = db.CreateTempTable<TMP_MIN_TEMPORARY>(tableOptions: TableOptions.IsTemporary | TableOptions.CheckExistence))
				{
					Assert.That(db.LastQuery, Does.Contain(" TEMPORARY "));
				}

				Assert.That(db.LastQuery, Does.Contain(" TEMPORARY "));

				db.Insert(new TEST_TB_AA() { CODE_AA = "AA" });
#pragma warning disable CA2201 // Do not raise reserved exception types
				throw new InvalidOperationException();
#pragma warning restore CA2201 // Do not raise reserved exception types
			}
			catch (InvalidOperationException)
			{
				db.RollbackTransaction();
			}

			Assert.That(tb.Count(), Is.EqualTo(0));
		}

		[Table]
		sealed class TMP_MIN_TEMPORARY
		{
			[Column(IsPrimaryKey = true, IsIdentity = true)]
			public int IDX { get; set; }

			[Column(CanBeNull = false, Length = 80)]
			public string KEY_A { get; set; } = null!;
		}

		[Table]
		sealed class TEST_TB_AA
		{
			[Column(IsPrimaryKey = true, IsIdentity = true)]
			public int AID { get; set; }

			[Column(CanBeNull = false, Length = 80)]
			public string CODE_AA { get; set; } = null!;
		}
		#endregion

		#region issue 4354
		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4354")]
		public void Issue4354Test(
			// MySql.Data has enum, but it is not configurable
			[IncludeDataSources(false, TestProvName.AllMySqlConnector)] string context,
			[Values] MySqlConnectorGuidFormat format,
			[Values] BulkCopyType copyType,
			[Values] bool inline)
		{
			var connectionString = DataConnection.GetConnectionString(context);
			var dataProvider     = DataConnection.GetDataProvider(context);

			if (format == MySqlConnectorGuidFormat.None || format == MySqlConnectorGuidFormat.Default)
			{
				Assert.Inconclusive($"{format} not tested by this test");
			}

			int? length = format switch
			{
				MySqlConnectorGuidFormat.Char32 => 32,
				MySqlConnectorGuidFormat.Char36 => 36,
				MySqlConnectorGuidFormat.TimeSwapBinary16 => 16,
				MySqlConnectorGuidFormat.Binary16 => 16,
				MySqlConnectorGuidFormat.LittleEndianBinary16 => 16,
				_ => null
			};

			var type = format switch
			{
				MySqlConnectorGuidFormat.Char32 => DataType.Char,
				MySqlConnectorGuidFormat.Char36 => DataType.Char,
				MySqlConnectorGuidFormat.TimeSwapBinary16 => DataType.Binary,
				MySqlConnectorGuidFormat.Binary16 => DataType.Binary,
				MySqlConnectorGuidFormat.LittleEndianBinary16 => DataType.Binary,
				_ => DataType.Guid
			};

			var fb = new FluentMappingBuilder();

			var prop = fb.Entity<Issue4354Table>()
				.Property(x => x.Value)
					.HasDataType(type);

			if (length != null)
				prop.HasLength(length.Value);

			fb.Build();

			using var db = GetDataConnection(
				context,
				o => o.UseMappingSchema(fb.MappingSchema).UseConnectionString(dataProvider, connectionString + $";GuidFormat={format}"));

			db.InlineParameters = inline;

			using var tb = db.CreateLocalTable<Issue4354Table>();

			var items = new Issue4354Table[] { new Issue4354Table() { Value = TestData.Guid1 } };

			db.BulkCopy(new BulkCopyOptions() { BulkCopyType = copyType }, items);

			var record = tb.Single();

			Assert.That(record.Value, Is.EqualTo(TestData.Guid1));
		}

		sealed class Issue4354Table
		{
			public Guid Value { get; set; }
		}
		#endregion

		#region Issue 3726
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3726")]
		public void Issue3726Test([IncludeDataSources(TestProvName.AllMySql)] string context, [Values] bool inline)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable<Issue3726Table>();


			db.Insert(new Issue3726Table() { Id = 1, Value = 123 });

			db.InlineParameters = inline;

			var bar = 123;

			tb.Where(f => f.Value == bar)
				.Set(f => f.Value2, "Baz")
				.Update();

			Assert.That(db.LastQuery!.ToLowerInvariant(), Does.Contain("cast"));
		}

		sealed class Issue3726Table
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public uint Value { get; set; }
			[Column] public string? Value2 { get; set; }
		}
		#endregion
	}

	#region Extensions
	static class MariaDBModuleFunctions
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

	internal static class MySqlTestFunctions
	{
		public static int TestOutputParametersWithoutTableProcedure(this DataConnection dataConnection, string? aInParam, out sbyte? aOutParam)
		{
			var parameters = new []
			{
				new DataParameter("aInParam",  aInParam,  DataType.VarChar)
				{
					Size = 256
				},
				new DataParameter("aOutParam", null, DataType.SByte)
				{
					Direction = ParameterDirection.Output
				}
			};

			var ret = dataConnection.ExecuteProc("`TestOutputParametersWithoutTableProcedure`", parameters);

			aOutParam = Converter.ChangeTypeTo<sbyte?>(parameters[1].Value);

			return ret;
		}

		public static IEnumerable<Person> TestProcedure(this DataConnection dataConnection, int? param3, ref int? param2, out int? param1)
		{
			var parameters = new []
			{
				new DataParameter("param3", param3, DataType.Int32),
				new DataParameter("param2", param2, DataType.Int32)
				{
					Direction = ParameterDirection.InputOutput
				},
				new DataParameter("param1", null, DataType.Int32)
				{
					Direction = ParameterDirection.Output
				}
			};

			var ret = dataConnection.QueryProc<Person>("`TestProcedure`", parameters).ToList();

			param2 = Converter.ChangeTypeTo<int?>(parameters[1].Value);
			param1 = Converter.ChangeTypeTo<int?>(parameters[2].Value);

			return ret;
		}
	}
	#endregion
}
