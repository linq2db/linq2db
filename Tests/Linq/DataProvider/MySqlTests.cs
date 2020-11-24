using System;
using System.Data.Linq;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using LinqToDB.Tools;

using NUnit.Framework;

using MySqlDataDateTime = MySql.Data.Types.MySqlDateTime;
using MySqlDataDecimal  = MySql.Data.Types.MySqlDecimal;

using MySqlConnectorDateTime = MySqlConnector.MySqlDateTime;

namespace Tests.DataProvider
{
	using LinqToDB.DataProvider.MySql;
	using LinqToDB.SqlProvider;
	using LinqToDB.Tools.Comparers;
	using Model;
	using System.Collections;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.Threading.Tasks;

	[TestFixture]
	public class MySqlTests : DataProviderTestBase
	{
		[Test]
		public void TestParameters([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT @p",        new { p =  1  }), Is.EqualTo("1"));
				Assert.That(conn.Execute<string>("SELECT @p",        new { p = "1" }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT @p",        new { p =  new DataParameter { Value = 1   } }), Is.EqualTo(1));
				Assert.That(conn.Execute<string>("SELECT @p1",       new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
				Assert.That(conn.Execute<int>   ("SELECT @p1 + ?p2", new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
				Assert.That(conn.Execute<int>   ("SELECT @p2 + ?p1", new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
			}
		}

		[Test]
		public void TestDataTypes([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(TestType<long?>						(conn, "bigintDataType",    DataType.Int64),               Is.EqualTo(1000000));
				Assert.That(TestType<short?>					(conn, "smallintDataType",  DataType.Int16),               Is.EqualTo(25555));
				Assert.That(TestType<sbyte?>					(conn, "tinyintDataType",   DataType.SByte),               Is.EqualTo(111));
				Assert.That(TestType<int?>						(conn, "mediumintDataType", DataType.Int32),               Is.EqualTo(5555));
				Assert.That(TestType<int?>						(conn, "intDataType",       DataType.Int32),               Is.EqualTo(7777777));
				Assert.That(TestType<decimal?>					(conn, "numericDataType",   DataType.Decimal),             Is.EqualTo(9999999m));
				Assert.That(TestType<decimal?>					(conn, "decimalDataType",   DataType.Decimal),             Is.EqualTo(8888888m));
				Assert.That(TestType<double?>					(conn, "doubleDataType",    DataType.Double),              Is.EqualTo(20.31d));
				Assert.That(TestType<float?>					(conn, "floatDataType",     DataType.Single),              Is.EqualTo(16.0f));
				Assert.That(TestType<DateTime?>					(conn, "dateDataType",      DataType.Date),                Is.EqualTo(new DateTime(2012, 12, 12)));
				Assert.That(TestType<DateTime?>					(conn, "datetimeDataType",  DataType.DateTime),            Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(TestType<DateTime?>					(conn, "datetimeDataType",  DataType.DateTime2),           Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(TestType<DateTime?>					(conn, "timestampDataType", DataType.Timestamp),           Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(TestType<TimeSpan?>					(conn, "timeDataType",      DataType.Time),                Is.EqualTo(new TimeSpan(12, 12, 12)));
				Assert.That(TestType<int?>						(conn, "yearDataType",      DataType.Int32),               Is.EqualTo(1998));
				Assert.That(TestType<int?>						(conn, "year2DataType",     DataType.Int32),               Is.EqualTo(context != TestProvName.MySql55 ? 1997 : 97));
				Assert.That(TestType<int?>						(conn, "year4DataType",     DataType.Int32),               Is.EqualTo(2012));

				Assert.That(TestType<char?>						(conn, "charDataType",      DataType.Char),                Is.EqualTo('1'));
				Assert.That(TestType<string>					(conn, "charDataType",      DataType.Char),                Is.EqualTo("1"));
				Assert.That(TestType<string>					(conn, "charDataType",      DataType.NChar),               Is.EqualTo("1"));
				Assert.That(TestType<string>					(conn, "varcharDataType",   DataType.VarChar),             Is.EqualTo("234"));
				Assert.That(TestType<string>					(conn, "varcharDataType",   DataType.NVarChar),            Is.EqualTo("234"));
				Assert.That(TestType<string>					(conn, "textDataType",      DataType.Text),                Is.EqualTo("567"));

				Assert.That(TestType<byte[]>					(conn, "binaryDataType",    DataType.Binary),              Is.EqualTo(new byte[] {  97,  98,  99 }));
				Assert.That(TestType<byte[]>					(conn, "binaryDataType",    DataType.VarBinary),           Is.EqualTo(new byte[] {  97,  98,  99 }));
				Assert.That(TestType<byte[]>					(conn, "varbinaryDataType", DataType.Binary),              Is.EqualTo(new byte[] {  99, 100, 101 }));
				Assert.That(TestType<byte[]>					(conn, "varbinaryDataType", DataType.VarBinary),           Is.EqualTo(new byte[] {  99, 100, 101 }));
				Assert.That(TestType<Binary>					(conn, "varbinaryDataType", DataType.VarBinary).ToArray(), Is.EqualTo(new byte[] {  99, 100, 101 }));
				Assert.That(TestType<byte[]>					(conn, "blobDataType",      DataType.Binary),              Is.EqualTo(new byte[] { 100, 101, 102 }));
				Assert.That(TestType<byte[]>					(conn, "blobDataType",      DataType.VarBinary),           Is.EqualTo(new byte[] { 100, 101, 102 }));
				Assert.That(TestType<byte[]>					(conn, "blobDataType",      DataType.Blob),                Is.EqualTo(new byte[] { 100, 101, 102 }));

				Assert.That(TestType<ulong?>					(conn, "bitDataType"),                                     Is.EqualTo(5));
				Assert.That(TestType<string>					(conn, "enumDataType"),                                    Is.EqualTo("Green"));
				Assert.That(TestType<string>					(conn, "setDataType"),                                     Is.EqualTo("one"));

				if (context != ProviderName.MySqlConnector)
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
					using (new DisableBaseline("Output (datetime format) is culture-/system-dependent"))
						Assert.That(TestType<MySqlConnectorDateTime?>(conn, "datetimeDataType", DataType.DateTime), Is.EqualTo(new MySqlConnectorDateTime(2012, 12, 12, 12, 12, 12, 0)));
				}
			}
		}

		[Test]
		public void TestDate([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12' as date)"),                          Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12' as date)"),                          Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime> ("SELECT @p", DataParameter.Date("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime, DataType.Date)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestDateTime([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.That(conn.Execute<DateTime> ("SELECT Cast('2012-12-12 12:12:12' as datetime)"),                 Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT Cast('2012-12-12 12:12:12' as datetime)"),                 Is.EqualTo(dateTime));

				Assert.That(conn.Execute<DateTime> ("SELECT @p", DataParameter.DateTime("p", dateTime)),               Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime)),                    Is.EqualTo(dateTime));
				Assert.That(conn.Execute<DateTime?>("SELECT @p", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
			}
		}

		[Test]
		public void TestChar([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char)"),         Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char)"),         Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast('1' as char(1))"),      Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast('1' as char(1))"),      Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT @p",                  DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p",                  DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char)",    DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char)",    DataParameter.Char("p",  '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT Cast(@p as char(1))", DataParameter.Char("@p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT Cast(@p as char(1))", DataParameter.Char("@p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT @p", DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p", DataParameter.VarChar ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT @p", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p", DataParameter.NChar   ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT @p", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char> ("SELECT @p", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p", DataParameter.Create  ("p", '1')), Is.EqualTo('1'));

				Assert.That(conn.Execute<char> ("SELECT @p", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				Assert.That(conn.Execute<char?>("SELECT @p", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
			}
		}

		[Test]
		public void TestString([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT Cast('12345' as char(20))"),      Is.EqualTo("12345"));
				Assert.That(conn.Execute<string>("SELECT Cast(NULL    as char(20))"),      Is.Null);

				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Char    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.VarChar ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Text    ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NChar   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.NText   ("p", "123")), Is.EqualTo("123"));
				Assert.That(conn.Execute<string>("SELECT @p", DataParameter.Create  ("p", "123")), Is.EqualTo("123"));

				Assert.That(conn.Execute<string>("SELECT @p", new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestBinary([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			var arr1 = new byte[] { 48, 57 };

			using (var conn = new DataConnection(context))
			{
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

		[Test]
		public void TestXml([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>     ("SELECT '<xml/>'"),            Is.EqualTo("<xml/>"));
				Assert.That(conn.Execute<XDocument>  ("SELECT '<xml/>'").ToString(), Is.EqualTo("<xml />"));
				Assert.That(conn.Execute<XmlDocument>("SELECT '<xml/>'").InnerXml,   Is.EqualTo("<xml />"));

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

		[Test]
		public void TestEnum1([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<TestEnum> ("SELECT 'A'"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'A'"), Is.EqualTo(TestEnum.AA));
				Assert.That(conn.Execute<TestEnum> ("SELECT 'B'"), Is.EqualTo(TestEnum.BB));
				Assert.That(conn.Execute<TestEnum?>("SELECT 'B'"), Is.EqualTo(TestEnum.BB));
			}
		}

		[Test]
		public void TestEnum2([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Execute<string>("SELECT @p", new { p = TestEnum.AA }),            Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = (TestEnum?)TestEnum.BB }), Is.EqualTo("B"));

				Assert.That(conn.Execute<string>("SELECT @p", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = ConvertTo<string>.From(TestEnum.AA) }), Is.EqualTo("A"));
				Assert.That(conn.Execute<string>("SELECT @p", new { p = conn.MappingSchema.GetConverter<TestEnum?,string>()!(TestEnum.AA) }), Is.EqualTo("A"));
			}
		}

		[Table("AllTypes")]
		public partial class AllType : AllTypeBaseProviderSpecific
		{
			[IgnoreComparison]
			[Column,     Nullable] public int?      yearDataType        { get; set; } // year(4)
			[IgnoreComparison]
			[Column,     Nullable] public int?      year2DataType       { get; set; } // year(2)
			[IgnoreComparison]
			[Column,     Nullable] public int?      year4DataType       { get; set; } // year(4)
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

			using (var conn = new DataConnection(context))
			{
				EnableNativeBulk(conn, context);
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
								year2DataType       = (1000 + n) % 100,
								year4DataType       = (1000 + n) % 100,
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

					var isNativeCopy = bulkCopyType == BulkCopyType.ProviderSpecific && ((MySqlDataProvider)conn.DataProvider).Adapter.BulkCopy != null;

					if (isNativeCopy)
					{
						conn.BulkCopy<AllTypeBaseProviderSpecific>(
							new BulkCopyOptions { MaxBatchSize = batchSize, BulkCopyType = bulkCopyType },
							source);
						var result = conn.GetTable<AllTypeBaseProviderSpecific>().OrderBy(_ => _.ID).Where(_ => _.varcharDataType == "_btest");

						// compare only 10 records
						// as we don't compare all, we must ensure we inserted all records
						Assert.AreEqual(source.Count(), result.Count());
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
						Assert.AreEqual(source.Count(), result.Count());
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

			using (var conn = new DataConnection(context))
			{
				EnableNativeBulk(conn, context);
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
								year2DataType       = (1000 + n) % 100,
								year4DataType       = (1000 + n) % 100,
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

					var isNativeCopy = bulkCopyType == BulkCopyType.ProviderSpecific && ((MySqlDataProvider)conn.DataProvider).Adapter.BulkCopy != null;

					if (isNativeCopy)
					{
						await conn.BulkCopyAsync<AllTypeBaseProviderSpecific>(
							new BulkCopyOptions { MaxBatchSize = batchSize, BulkCopyType = bulkCopyType },
							source);
						var result = conn.GetTable<AllTypeBaseProviderSpecific>().OrderBy(_ => _.ID).Where(_ => _.varcharDataType == "_btest");

						// compare only 10 records
						// as we don't compare all, we must ensure we inserted all records
						Assert.AreEqual(source.Count(), result.Count());
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
						Assert.AreEqual(source.Count(), result.Count());
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

		public static void EnableNativeBulk(DataConnection db, string context)
		{
			if (context == ProviderName.MySqlConnector)
				db.Execute("SET GLOBAL local_infile=ON");
		}

		[Table("NeedS.esca Pin`g")]
		class BinaryTypes
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
			using (var db    = new DataConnection(context))
			using (var table = db.CreateLocalTable<BinaryTypes>())
			{
				EnableNativeBulk(db, context);


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
				Assert.AreEqual(data.Length, res.Length);

				AreEqual(data, res, ComparerBuilder.GetEqualityComparer<BinaryTypes>());
			}
		}

		[Test]
		public async Task BulkCopyBinaryAndBitTypesAsync([IncludeDataSources(TestProvName.AllMySql)] string context, [Values] BulkCopyType bulkCopyType)
		{
			using (var db    = new DataConnection(context))
			using (var table = db.CreateLocalTable<BinaryTypes>())
			{
				EnableNativeBulk(db, context);


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
				Assert.AreEqual(data.Length, res.Length);

				AreEqual(data, res, ComparerBuilder.GetEqualityComparer<BinaryTypes>());
			}
		}

		[Test]
		public void BulkCopyLinqTypes([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using (var db = new DataConnection(context))
				{
					EnableNativeBulk(db, context);

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
				using (var db = new DataConnection(context))
				{
					EnableNativeBulk(db, context);
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

		static void BulkCopyRetrieveSequence(string context, BulkCopyType bulkCopyType)
		{
			var data = new[]
			{
				new Person { FirstName = "Neurologist"    , LastName = "test" },
				new Person { FirstName = "Sports Medicine", LastName = "test" },
				new Person { FirstName = "Optometrist"    , LastName = "test" },
				new Person { FirstName = "Pediatrics"     , LastName = "test"  },
				new Person { FirstName = "Psychiatry"     , LastName = "test"  }
			};

			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				EnableNativeBulk(db, context);
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

		static async Task BulkCopyRetrieveSequenceAsync(string context, BulkCopyType bulkCopyType)
		{
			var data = new[]
			{
				new Person { FirstName = "Neurologist"    , LastName = "test" },
				new Person { FirstName = "Sports Medicine", LastName = "test" },
				new Person { FirstName = "Optometrist"    , LastName = "test" },
				new Person { FirstName = "Pediatrics"     , LastName = "test"  },
				new Person { FirstName = "Psychiatry"     , LastName = "test"  }
			};

			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				EnableNativeBulk(db, context);
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
			using (var db = new DataConnection(context))
			{
				db.GetTable<Parent>().Update(p => p.ParentID == 1, p => new Parent { Value1 = 1 });

				db.BeginTransaction();

				db.GetTable<Parent>().Update(p => p.ParentID == 1, p => new Parent { Value1 = null });

				Assert.IsNull(db.GetTable<Parent>().First(p => p.ParentID == 1).Value1);

				db.RollbackTransaction();

				Assert.That(1, Is.EqualTo(db.GetTable<Parent>().First(p => p.ParentID == 1).Value1));
			}
		}

		[Test]
		public void TestTransaction2([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var db = new DataConnection(context))
			{
				db.GetTable<Parent>().Update(p => p.ParentID == 1, p => new Parent { Value1 = 1 });

				using (var tran = db.BeginTransaction())
				{
					db.GetTable<Parent>().Update(p => p.ParentID == 1, p => new Parent { Value1 = null });

					Assert.IsNull(db.GetTable<Parent>().First(p => p.ParentID == 1).Value1);

					tran.Rollback();

					Assert.That(1, Is.EqualTo(db.GetTable<Parent>().First(p => p.ParentID == 1).Value1));
				}
			}
		}

		[Test]
		public void TestBeginTransactionWithIsolationLevel([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var db = new DataConnection(context))
			{
				db.GetTable<Parent>().Update(p => p.ParentID == 1, p => new Parent { Value1 = 1 });

				using (var tran = db.BeginTransaction(IsolationLevel.Unspecified))
				{
					db.GetTable<Parent>().Update(p => p.ParentID == 1, p => new Parent { Value1 = null });

					Assert.IsNull(db.GetTable<Parent>().First(p => p.ParentID == 1).Value1);

					tran.Rollback();

					Assert.That(1, Is.EqualTo(db.GetTable<Parent>().First(p => p.ParentID == 1).Value1));
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
				Assert.AreEqual(1, views.Count);
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
				expectedProc.CatalogName = TestUtils.GetDatabaseName(db);

				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);

				var procedures = schema.Procedures.Where(_ => _.ProcedureName == expectedProc.ProcedureName).ToList();

				Assert.AreEqual(1, procedures.Count);

				var procedure = procedures[0];

				Assert.AreEqual(expectedProc.CatalogName.ToLower(), procedure.CatalogName!.ToLower());
				Assert.AreEqual(expectedProc.SchemaName,            procedure.SchemaName);
				Assert.AreEqual(expectedProc.MemberName,            procedure.MemberName);
				Assert.AreEqual(expectedProc.IsTableFunction,       procedure.IsTableFunction);
				Assert.AreEqual(expectedProc.IsAggregateFunction,   procedure.IsAggregateFunction);
				Assert.AreEqual(expectedProc.IsDefaultSchema,       procedure.IsDefaultSchema);

				if (GetProviderName(context, out var _) == ProviderName.MySqlConnector
					&& procedure.ResultException != null)
				{
					Assert.False       (procedure.IsLoaded);
					Assert.IsInstanceOf(typeof(InvalidOperationException), procedure.ResultException);
					Assert.AreEqual    ("There is no current result set.", procedure.ResultException.Message);
				}
				else
				{
					Assert.AreEqual(expectedProc.IsLoaded, procedure.IsLoaded);
					Assert.IsNull(procedure.ResultException);
				}

				Assert.AreEqual(expectedProc.Parameters.Count, procedure.Parameters.Count);

				for (var i = 0; i < procedure.Parameters.Count; i++)
				{
					var actualParam = procedure.Parameters[i];
					var expectedParam = expectedProc.Parameters[i];

					Assert.IsNotNull(expectedParam);

					Assert.AreEqual(expectedParam.SchemaName,           actualParam.SchemaName);
					Assert.AreEqual(expectedParam.ParameterName,        actualParam.ParameterName);
					Assert.AreEqual(expectedParam.SchemaType,           actualParam.SchemaType);
					Assert.AreEqual(expectedParam.IsIn,                 actualParam.IsIn);
					Assert.AreEqual(expectedParam.IsOut,                actualParam.IsOut);
					Assert.AreEqual(expectedParam.IsResult,             actualParam.IsResult);
					Assert.AreEqual(expectedParam.Size,                 actualParam.Size);
					Assert.AreEqual(expectedParam.ParameterType,        actualParam.ParameterType);
					Assert.AreEqual(expectedParam.SystemType,           actualParam.SystemType);
					Assert.AreEqual(expectedParam.DataType,             actualParam.DataType);
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
					var actualTable = procedure.ResultTable!;

					Assert.AreEqual(expectedTable.ID,                 actualTable.ID);
					Assert.AreEqual(expectedTable.CatalogName,        actualTable.CatalogName);
					Assert.AreEqual(expectedTable.SchemaName,         actualTable.SchemaName);
					Assert.AreEqual(expectedTable.TableName,          actualTable.TableName);
					Assert.AreEqual(expectedTable.Description,        actualTable.Description);
					Assert.AreEqual(expectedTable.IsDefaultSchema,    actualTable.IsDefaultSchema);
					Assert.AreEqual(expectedTable.IsView,             actualTable.IsView);
					Assert.AreEqual(expectedTable.IsProcedureResult,  actualTable.IsProcedureResult);
					Assert.AreEqual(expectedTable.TypeName,           actualTable.TypeName);
					Assert.AreEqual(expectedTable.IsProviderSpecific, actualTable.IsProviderSpecific);

					Assert.IsNotNull(actualTable.ForeignKeys);
					Assert.IsEmpty(actualTable.ForeignKeys);

					Assert.AreEqual(expectedTable.Columns.Count, actualTable.Columns.Count);

					foreach (var actualColumn in actualTable.Columns)
					{
						var expectedColumn = expectedTable.Columns
							.Where(_ => _.ColumnName == actualColumn.ColumnName)
							.SingleOrDefault()!;

						Assert.IsNotNull(expectedColumn);

						Assert.AreEqual(expectedColumn.ColumnType,           actualColumn.ColumnType);
						Assert.AreEqual(expectedColumn.IsNullable,           actualColumn.IsNullable);
						Assert.AreEqual(expectedColumn.IsIdentity,           actualColumn.IsIdentity);
						Assert.AreEqual(expectedColumn.IsPrimaryKey,         actualColumn.IsPrimaryKey);
						Assert.AreEqual(expectedColumn.PrimaryKeyOrder,      actualColumn.PrimaryKeyOrder);
						Assert.AreEqual(expectedColumn.Description,          actualColumn.Description);
						Assert.AreEqual(expectedColumn.MemberName,           actualColumn.MemberName);
						Assert.AreEqual(expectedColumn.MemberType,           actualColumn.MemberType);
						Assert.AreEqual(expectedColumn.ProviderSpecificType, actualColumn.ProviderSpecificType);
						Assert.AreEqual(expectedColumn.SystemType,           actualColumn.SystemType);
						Assert.AreEqual(expectedColumn.DataType,             actualColumn.DataType);
						Assert.AreEqual(expectedColumn.SkipOnInsert,         actualColumn.SkipOnInsert);
						Assert.AreEqual(expectedColumn.SkipOnUpdate,         actualColumn.SkipOnUpdate);
						Assert.AreEqual(expectedColumn.Length,               actualColumn.Length);
						Assert.AreEqual(expectedColumn.Precision,            actualColumn.Precision);
						Assert.AreEqual(expectedColumn.Scale,                actualColumn.Scale);
						Assert.AreEqual(actualTable,                         actualColumn.Table);
					}

					Assert.IsNotNull(procedure.SimilarTables);

					foreach (var table in procedure.SimilarTables!)
					{
						var tbl = expectedProc.SimilarTables!
							.SingleOrDefault(_ => _.TableName!.ToLower() == table.TableName!.ToLower());

						Assert.IsNotNull(tbl);
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
				var res = schema.Tables.FirstOrDefault(c => c.ID!.ToLower().Contains("fulltextindex"));
				Assert.AreNotEqual(null, res);
			}
		}

		[Test(Description = "TODO: Issue not reproduced")]
		public void Issue1993([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			{
				DatabaseSchema schema = db.DataProvider.GetSchemaProvider().GetSchema(db);
				var table = schema.Tables.FirstOrDefault(t => t.ID!.ToLower().Contains("issue1993"))!;
				Assert.IsNotNull(table);
				Assert.AreEqual(2, table.Columns.Count);
				Assert.AreEqual("id",          table.Columns[0].ColumnName);
				Assert.AreEqual("description", table.Columns[1].ColumnName);
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

				Assert.AreEqual(10, param2);
				Assert.AreEqual(133, param1);
				AreEqual(db.GetTable<Person>(), res);
			}
		}

		[Test]
		public void TestTestOutputParametersWithoutTableProcedure([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			{
				var res = db.TestOutputParametersWithoutTableProcedure("test", out var outParam);

				Assert.AreEqual(123, outParam);
				Assert.AreEqual(1, res);
			}
		}

		[Table]
		public class CreateTable
		{
			[Column                                                            ] public string? VarChar255;
			[Column(Length = 1)                                                ] public string? VarChar1;
			[Column(Length = 112)                                              ] public string? VarChar112;
			[Column                                                            ] public char    Char;
			[Column(DataType = DataType.Char)                                  ] public string? Char255;
			[Column(DataType = DataType.Char, Length = 1)                      ] public string? Char1;
			[Column(DataType = DataType.Char, Length = 112)                    ] public string? Char112;
			[Column(Length = 1)                                                ] public byte[]? VarBinary1;
			[Column                                                            ] public byte[]? VarBinary255;
			[Column(Length = 3)                                                ] public byte[]? VarBinary3;
			[Column(DataType = DataType.Binary, Length = 1)                    ] public byte[]? Binary1;
			[Column(DataType = DataType.Binary)                                ] public byte[]? Binary255;
			[Column(DataType = DataType.Binary, Length = 3)                    ] public byte[]? Binary3;
			[Column(DataType = DataType.Blob, Length = 200)                    ] public byte[]? TinyBlob;
			[Column(DataType = DataType.Blob, Length = 2000)                   ] public byte[]? Blob;
			[Column(DataType = DataType.Blob, Length = 200000)                 ] public byte[]? MediumBlob;
			[Column(DataType = DataType.Blob)                                  ] public byte[]? BlobDefault;
			[Column(DataType = DataType.Blob, Length = int.MaxValue)           ] public byte[]? LongBlob;
			[Column(DataType = DataType.Text, Length = 200)                    ] public string? TinyText;
			[Column(DataType = DataType.Text, Length = 2000)                   ] public string? Text;
			[Column(DataType = DataType.Text, Length = 200000)                 ] public string? MediumText;
			[Column(DataType = DataType.Text, Length = int.MaxValue)           ] public string? LongText;
			[Column(DataType = DataType.Text)                                  ] public string? TextDefault;
			[Column(DataType = DataType.Date)                                  ] public DateTime Date;
			[Column                                                            ] public DateTime DateTime;
			[NotColumn(Configuration = TestProvName.MySql55)                   ]
			[Column(Precision = 3)                                             ] public DateTime DateTime3;
			// MySQL.Data provider has issues with timestamps
			// TODO: look into it later
			[Column(Configuration = ProviderName.MySqlConnector)               ] public DateTimeOffset TimeStamp;
			[Column(Precision = 5, Configuration = ProviderName.MySqlConnector)] public DateTimeOffset TimeStamp5;
			[Column                                                            ] public TimeSpan Time;
			[NotColumn(Configuration = TestProvName.MySql55)                   ]
			[Column(Precision = 2)                                             ] public TimeSpan Time2;
			[Column                                                            ] public sbyte TinyInt;
			[Column                                                            ] public byte UnsignedTinyInt;
			[Column                                                            ] public short SmallInt;
			[Column                                                            ] public ushort UnsignedSmallInt;
			[Column                                                            ] public int Int;
			[Column                                                            ] public uint UnsignedInt;
			[Column                                                            ] public long BigInt;
			[Column                                                            ] public ulong UnsignedBigInt;
			[Column                                                            ] public decimal Decimal;
			[Column(Precision = 15)                                            ] public decimal Decimal15_0;
			[Column(Scale = 5)                                                 ] public decimal Decimal10_5;
			[Column(Precision = 20, Scale = 2)                                 ] public decimal Decimal20_2;
			[Column                                                            ] public float Float;
			[Column(Precision = 10)                                            ] public float Float10;
			[Column                                                            ] public double Double;
			[Column(Precision = 30)                                            ] public double Float30;
			[Column                                                            ] public bool Bool;
			[Column(DataType = DataType.BitArray)                              ] public bool Bit1;
			[Column(DataType = DataType.BitArray)                              ] public byte Bit8;
			[Column(DataType = DataType.BitArray)                              ] public short Bit16;
			[Column(DataType = DataType.BitArray)                              ] public int Bit32;
			[Column(DataType = DataType.BitArray, Length = 10)                 ] public int Bit10;
			[Column(DataType = DataType.BitArray)                              ] public long Bit64;
			[NotColumn(Configuration = TestProvName.MySql55)                   ]
			[Column(DataType = DataType.Json)                                  ] public string? Json;
			// not mysql type, just mapping testing
			[Column                                                            ] public Guid Guid;
		}

		[Test]
		public void TestCreateTable([IncludeDataSources(false, TestProvName.AllMySql)] string context)
		{
			var isMySqlConnector = context == ProviderName.MySqlConnector;

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
			using (var db = new TestDataConnection(context))
			{
				// enable configuration use in mapping attributes
				if (context == TestProvName.MySql55)
					db.AddMappingSchema(new MappingSchema(context));
				using (var table = db.CreateLocalTable<CreateTable>())
				{
					var sql = db.LastQuery!;

					Assert.True(sql.Contains("\t`VarChar255`       VARCHAR(255)          NULL"));
					Assert.True(sql.Contains("\t`VarChar1`         VARCHAR(1)            NULL"));
					Assert.True(sql.Contains("\t`VarChar112`       VARCHAR(112)          NULL"));
					Assert.True(sql.Contains("\t`Char`             CHAR              NOT NULL"));
					Assert.True(sql.Contains("\t`Char1`            CHAR                  NULL"));
					Assert.True(sql.Contains("\t`Char255`          CHAR(255)             NULL"));
					Assert.True(sql.Contains("\t`Char112`          CHAR(112)             NULL"));
					Assert.True(sql.Contains("\t`VarBinary1`       VARBINARY(1)          NULL"));
					Assert.True(sql.Contains("\t`VarBinary255`     VARBINARY(255)        NULL"));
					Assert.True(sql.Contains("\t`VarBinary3`       VARBINARY(3)          NULL"));
					Assert.True(sql.Contains("\t`Binary1`          BINARY                NULL"));
					Assert.True(sql.Contains("\t`Binary255`        BINARY(255)           NULL"));
					Assert.True(sql.Contains("\t`Binary3`          BINARY(3)             NULL"));
					Assert.True(sql.Contains("\t`TinyBlob`         TINYBLOB              NULL"));
					Assert.True(sql.Contains("\t`Blob`             BLOB                  NULL"));
					Assert.True(sql.Contains("\t`MediumBlob`       MEDIUMBLOB            NULL"));
					Assert.True(sql.Contains("\t`LongBlob`         LONGBLOB              NULL"));
					Assert.True(sql.Contains("\t`BlobDefault`      BLOB                  NULL"));
					Assert.True(sql.Contains("\t`TinyText`         TINYTEXT              NULL"));
					Assert.True(sql.Contains("\t`Text`             TEXT                  NULL"));
					Assert.True(sql.Contains("\t`MediumText`       MEDIUMTEXT            NULL"));
					Assert.True(sql.Contains("\t`LongText`         LONGTEXT              NULL"));
					Assert.True(sql.Contains("\t`TextDefault`      TEXT                  NULL"));
					Assert.True(sql.Contains("\t`Date`             DATE              NOT NULL"));
					Assert.True(sql.Contains("\t`DateTime`         DATETIME          NOT NULL"));
					if (context != TestProvName.MySql55)
					{
						Assert.True(sql.Contains("\t`DateTime3`        DATETIME(3)       NOT NULL"));
						Assert.True(sql.Contains("\t`Time2`            TIME(2)           NOT NULL"));
						Assert.True(sql.Contains("\t`Json`             JSON                  NULL"));
					}
					if (isMySqlConnector)
					{
						Assert.True(sql.Contains("\t`TimeStamp`        TIMESTAMP         NOT NULL"));
						Assert.True(sql.Contains("\t`TimeStamp5`       TIMESTAMP(5)      NOT NULL"));
					}
					Assert.True(sql.Contains("\t`Time`             TIME              NOT NULL"));
					Assert.True(sql.Contains("\t`TinyInt`          TINYINT           NOT NULL"));
					Assert.True(sql.Contains("\t`UnsignedTinyInt`  TINYINT UNSIGNED  NOT NULL"));
					Assert.True(sql.Contains("\t`SmallInt`         SMALLINT          NOT NULL"));
					Assert.True(sql.Contains("\t`UnsignedSmallInt` SMALLINT UNSIGNED NOT NULL"));
					Assert.True(sql.Contains("\t`Int`              INT               NOT NULL"));
					Assert.True(sql.Contains("\t`UnsignedInt`      INT UNSIGNED      NOT NULL"));
					Assert.True(sql.Contains("\t`BigInt`           BIGINT            NOT NULL"));
					Assert.True(sql.Contains("\t`UnsignedBigInt`   BIGINT UNSIGNED   NOT NULL"));
					Assert.True(sql.Contains("\t`Decimal`          DECIMAL           NOT NULL"));
					Assert.True(sql.Contains("\t`Decimal15_0`      DECIMAL(15)       NOT NULL"));
					Assert.True(sql.Contains("\t`Decimal10_5`      DECIMAL(10, 5)    NOT NULL"));
					Assert.True(sql.Contains("\t`Decimal20_2`      DECIMAL(20, 2)    NOT NULL"));
					Assert.True(sql.Contains("\t`Float`            FLOAT             NOT NULL"));
					Assert.True(sql.Contains("\t`Float10`          FLOAT(10)         NOT NULL"));
					Assert.True(sql.Contains("\t`Double`           DOUBLE            NOT NULL"));
					Assert.True(sql.Contains("\t`Float30`          FLOAT(30)         NOT NULL"));
					Assert.True(sql.Contains("\t`Bool`             BOOLEAN           NOT NULL"));
					Assert.True(sql.Contains("\t`Bit1`             BIT               NOT NULL"));
					Assert.True(sql.Contains("\t`Bit8`             BIT(8)            NOT NULL"));
					Assert.True(sql.Contains("\t`Bit16`            BIT(16)           NOT NULL"));
					Assert.True(sql.Contains("\t`Bit32`            BIT(32)           NOT NULL"));
					Assert.True(sql.Contains("\t`Bit10`            BIT(10)           NOT NULL"));
					Assert.True(sql.Contains("\t`Bit64`            BIT(64)           NOT NULL"));
					Assert.True(sql.Contains("\t`Guid`             CHAR(36)          NOT NULL"));

					var testRecord = new CreateTable()
					{
						VarChar1         = "ы",
						VarChar255       = "ыsdf",
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
						Json             = "{\"x\": 10}",
						Guid             = TestData.Guid1
					};

					db.Insert(testRecord);
					var readRecord = table.Single();

					Assert.AreEqual(testRecord.VarChar1        , readRecord.VarChar1);
					Assert.AreEqual(testRecord.VarChar255      , readRecord.VarChar255);
					Assert.AreEqual(testRecord.VarChar112      , readRecord.VarChar112);
					Assert.AreEqual(testRecord.Char            , readRecord.Char);
					Assert.AreEqual(testRecord.Char1           , readRecord.Char1);
					Assert.AreEqual(testRecord.Char255         , readRecord.Char255);
					Assert.AreEqual(testRecord.Char112         , readRecord.Char112);
					Assert.AreEqual(testRecord.VarBinary1      , readRecord.VarBinary1);
					Assert.AreEqual(testRecord.VarBinary255    , readRecord.VarBinary255);
					Assert.AreEqual(testRecord.VarBinary3      , readRecord.VarBinary3);
					Assert.AreEqual(testRecord.Binary1         , readRecord.Binary1);
					// we trim padding only from char fields
					Assert.AreEqual(testRecord.Binary255.Concat(new byte[252]), readRecord.Binary255);
					Assert.AreEqual(testRecord.Binary3.Concat(new byte[1]), readRecord.Binary3);
					Assert.AreEqual(testRecord.TinyBlob        , readRecord.TinyBlob);
					Assert.AreEqual(testRecord.Blob            , readRecord.Blob);
					Assert.AreEqual(testRecord.MediumBlob      , readRecord.MediumBlob);
					Assert.AreEqual(testRecord.BlobDefault     , readRecord.BlobDefault);
					Assert.AreEqual(testRecord.LongBlob        , readRecord.LongBlob);
					Assert.AreEqual(testRecord.TinyText        , readRecord.TinyText);
					Assert.AreEqual(testRecord.Text            , readRecord.Text);
					Assert.AreEqual(testRecord.MediumText      , readRecord.MediumText);
					Assert.AreEqual(testRecord.LongText        , readRecord.LongText);
					Assert.AreEqual(testRecord.TextDefault     , readRecord.TextDefault);
					Assert.AreEqual(testRecord.Date            , readRecord.Date);
					Assert.AreEqual(testRecord.DateTime        , readRecord.DateTime);
					if (context != TestProvName.MySql55)
					{
						Assert.AreEqual(testRecord.DateTime3   , readRecord.DateTime3);
						Assert.AreEqual(testRecord.Time2       , readRecord.Time2);
						Assert.AreEqual(testRecord.Json        , readRecord.Json);
					}
					if (isMySqlConnector)
					{
						Assert.AreEqual(testRecord.TimeStamp,  readRecord.TimeStamp);
						Assert.AreEqual(testRecord.TimeStamp5, readRecord.TimeStamp5);
					}
					Assert.AreEqual(testRecord.Time            , readRecord.Time);
					Assert.AreEqual(testRecord.TinyInt         , readRecord.TinyInt);
					Assert.AreEqual(testRecord.UnsignedTinyInt , readRecord.UnsignedTinyInt);
					Assert.AreEqual(testRecord.SmallInt        , readRecord.SmallInt);
					Assert.AreEqual(testRecord.UnsignedSmallInt, readRecord.UnsignedSmallInt);
					Assert.AreEqual(testRecord.Int             , readRecord.Int);
					Assert.AreEqual(testRecord.UnsignedInt     , readRecord.UnsignedInt);
					Assert.AreEqual(testRecord.BigInt          , readRecord.BigInt);
					Assert.AreEqual(testRecord.UnsignedBigInt  , readRecord.UnsignedBigInt);
					Assert.AreEqual(testRecord.Decimal         , readRecord.Decimal);
					Assert.AreEqual(testRecord.Decimal15_0     , readRecord.Decimal15_0);
					Assert.AreEqual(testRecord.Decimal10_5     , readRecord.Decimal10_5);
					Assert.AreEqual(testRecord.Decimal20_2     , readRecord.Decimal20_2);
					Assert.AreEqual(testRecord.Float           , readRecord.Float);
					Assert.AreEqual(testRecord.Float10         , readRecord.Float10);
					Assert.AreEqual(testRecord.Double          , readRecord.Double);
					Assert.AreEqual(testRecord.Float30         , readRecord.Float30);
					Assert.AreEqual(testRecord.Bool            , readRecord.Bool);
					Assert.AreEqual(testRecord.Bit1            , readRecord.Bit1);
					Assert.AreEqual(testRecord.Bit8            , readRecord.Bit8);
					Assert.AreEqual(testRecord.Bit16           , readRecord.Bit16);
					Assert.AreEqual(testRecord.Bit32           , readRecord.Bit32);
					Assert.AreEqual(testRecord.Bit10           , readRecord.Bit10);
					Assert.AreEqual(testRecord.Bit64           , readRecord.Bit64);
					Assert.AreEqual(testRecord.Guid            , readRecord.Guid);
				}
			}
		}

		[Table]
		public class TestSchemaTypesTable
		{
			[Column                                                 ] public string? VarChar255;
			[Column(Length = 1)                                     ] public string? VarChar1;
			[Column(Length = 112)                                   ] public string? VarChar112;
			[Column                                                 ] public char    Char;
			[Column(DataType = DataType.Char)                       ] public string? Char255;
			[Column(DataType = DataType.Char, Length = 1)           ] public string? Char1;
			[Column(DataType = DataType.Char, Length = 112)         ] public string? Char112;
			[Column(Length = 1)                                     ] public byte[]? VarBinary1;
			[Column                                                 ] public byte[]? VarBinary255;
			[Column(Length = 3)                                     ] public byte[]? VarBinary3;
			[Column(DataType = DataType.Binary, Length = 1)         ] public byte[]? Binary1;
			[Column(DataType = DataType.Binary)                     ] public byte[]? Binary255;
			[Column(DataType = DataType.Binary, Length = 3)         ] public byte[]? Binary3;
			[Column(DataType = DataType.Blob, Length = 200)         ] public byte[]? TinyBlob;
			[Column(DataType = DataType.Blob, Length = 2000)        ] public byte[]? Blob;
			[Column(DataType = DataType.Blob, Length = 200000)      ] public byte[]? MediumBlob;
			[Column(DataType = DataType.Blob)                       ] public byte[]? BlobDefault;
			[Column(DataType = DataType.Blob, Length = int.MaxValue)] public byte[]? LongBlob;
			[Column(DataType = DataType.Text, Length = 200)         ] public string? TinyText;
			[Column(DataType = DataType.Text, Length = 2000)        ] public string? Text;
			[Column(DataType = DataType.Text, Length = 200000)      ] public string? MediumText;
			[Column(DataType = DataType.Text, Length = int.MaxValue)] public string? LongText;
			[Column(DataType = DataType.Text)                       ] public string? TextDefault;
			[Column(DataType = DataType.Date)                       ] public DateTime Date;
			[Column                                                 ] public DateTime DateTime;
			[NotColumn(Configuration = TestProvName.MySql55)        ]
			[Column(Precision = 3)                                  ] public DateTime DateTime3;
			[Column                                                 ] public DateTimeOffset TimeStamp;
			[NotColumn(Configuration = TestProvName.MySql55)]
			[Column(Precision = 5)                                  ] public DateTimeOffset TimeStamp5;
			[Column                                                 ] public TimeSpan Time;
			[NotColumn(Configuration = TestProvName.MySql55)        ]
			[Column(Precision = 2)                                  ] public TimeSpan Time2;
			[Column                                                 ] public sbyte TinyInt;
			[Column                                                 ] public byte UnsignedTinyInt;
			[Column                                                 ] public short SmallInt;
			[Column                                                 ] public ushort UnsignedSmallInt;
			[Column                                                 ] public int Int;
			[Column                                                 ] public uint UnsignedInt;
			[Column                                                 ] public long BigInt;
			[Column                                                 ] public ulong UnsignedBigInt;
			[Column                                                 ] public decimal Decimal;
			[Column(Precision = 15)                                 ] public decimal Decimal15_0;
			[Column(Scale = 5)                                      ] public decimal Decimal10_5;
			[Column(Precision = 20, Scale = 2)                      ] public decimal Decimal20_2;
			[Column                                                 ] public float Float;
			[Column(Precision = 10)                                 ] public float Float10;
			[Column                                                 ] public double Double;
			[Column(Precision = 30)                                 ] public double Float30;
			[Column                                                 ] public bool Bool;
			[Column(DataType = DataType.BitArray)                   ] public bool Bit1;
			[Column(DataType = DataType.BitArray)                   ] public byte Bit8;
			[Column(DataType = DataType.BitArray)                   ] public short Bit16;
			[Column(DataType = DataType.BitArray)                   ] public int Bit32;
			[Column(DataType = DataType.BitArray, Length = 10)      ] public int Bit10;
			[Column(DataType = DataType.BitArray)                   ] public long Bit64;
			[NotColumn(Configuration = TestProvName.MySql55)        ]
			[Column(DataType = DataType.Json)                       ] public string? Json;
			// not mysql type, just mapping testing
			[Column                                                 ] public Guid Guid;

			[Column(DbType = "ENUM('one', 'two')")                  ] public string? Enum;
			[Column(DbType = "SET('one', 'two')")                   ] public string? Set;
			[Column(DbType = "YEAR")                                ] public int Year;
			[Column(DbType = "MEDIUMINT")                           ] public int MediumInt;
			[Column(DbType = "MEDIUMINT UNSIGNED")                  ] public uint UnsignedMediumInt;
			[Column(DbType = "GEOMETRY")                            ] public object? Geometry;
			[Column(DbType = "POINT")                               ] public object? Point;
			[Column(DbType = "LINESTRING")                          ] public object? LineString;
			[Column(DbType = "POLYGON")                             ] public object? Polygon;
			[Column(DbType = "MULTIPOINT")                          ] public object? MultiPoint;
			[Column(DbType = "MULTILINESTRING")                     ] public object? MultiLineString;
			[Column(DbType = "MULTIPOLYGON")                        ] public object? MultiPolygon;
			[Column(DbType = "GEOMETRYCOLLECTION")                  ] public object? GeometryCollection;
		}

		[Test]
		public void TestTypesSchema([IncludeDataSources(false, TestProvName.AllMySql)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				// enable configuration use in mapping attributes
				if (context == TestProvName.MySql55)
					db.AddMappingSchema(new MappingSchema(context));
				using (var table = db.CreateLocalTable<TestSchemaTypesTable>())
				{
					var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions() { GetProcedures = false });

					var tableSchema = schema.Tables.Where(t => t.TableName!.ToLower() == "testschematypestable").SingleOrDefault()!;
					Assert.IsNotNull(tableSchema);

					assertColumn("VarChar255"        , "string"  , DataType.VarChar);
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

					if (context != TestProvName.MySql55)
					{
						assertColumn("DateTime3" , "DateTime", DataType.DateTime);
						assertColumn("Time2"     , "TimeSpan", DataType.Time);
						assertColumn("TimeStamp5", "DateTime", DataType.DateTime);

						if (context != TestProvName.MariaDB)
							assertColumn("Json", "string", DataType.Json);
						else
							assertColumn("Json", "string", DataType.Text);
					}

					void assertColumn(string name, string type, DataType dataType)
					{
						var column = tableSchema.Columns.Where(c => c.ColumnName == name).SingleOrDefault()!;
						Assert.IsNotNull(column);
						Assert.AreEqual(type, column.MemberType);
						Assert.AreEqual(dataType, column.DataType);
					}
				}
			}
		}

		[Test]
		public void TestProcedureTypesParameters([IncludeDataSources(false, TestProvName.AllMySql)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions() { GetTables = false });

				var proc = schema.Procedures.Where(t => t.ProcedureName == "Issue2313Parameters").SingleOrDefault()!;

				Assert.IsNotNull(proc);

				assertParameter("VarChar255"        , "string"   , DataType.VarChar);
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

				if (context != TestProvName.MySql55)
				{
					if (context != TestProvName.MariaDB)
						assertParameter("Json", "string", DataType.Json);
					else
						assertParameter("Json", "string", DataType.Text);
				}

				void assertParameter(string name, string type, DataType dataType)
				{
					var parameter = proc.Parameters.Where(c => c.ParameterName == name).SingleOrDefault()!;

					Assert.IsNotNull(parameter);

					Assert.AreEqual(type, parameter.ParameterType);
					Assert.AreEqual(dataType, parameter.DataType);
				}
			}
		}

		[Test]
		public void TestProcedureTypesResults([IncludeDataSources(false, TestProvName.AllMySql)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions() { GetTables = false });

				var proc = schema.Procedures.Where(t => t.ProcedureName == "Issue2313Results").SingleOrDefault()!;

				Assert.IsNotNull(proc);
				Assert.IsNotNull(proc.ResultTable);

				assertColumn("VarChar255"        , "string"   , DataType.VarChar);
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
				if (context == ProviderName.MySqlConnector)
				{
					assertColumn("Point"               , "byte[]", DataType.Undefined);
					assertColumn("LineString"          , "byte[]", DataType.Undefined);
					assertColumn("Polygon"             , "byte[]", DataType.Undefined);
					assertColumn("MultiPoint"          , "byte[]", DataType.Undefined);
					assertColumn("MultiLineString"     , "byte[]", DataType.Undefined);
					assertColumn("MultiPolygon"        , "byte[]", DataType.Undefined);
					assertColumn("Geometry"            , "byte[]", DataType.Undefined);
					assertColumn("GeometryCollection", "byte[]", DataType.Undefined);

					assertColumn("Json"    , "string", DataType.Json);
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
					var column = proc.ResultTable!.Columns.Where(c => c.ColumnName == name).SingleOrDefault()!;

					Assert.IsNotNull(column);

					Assert.AreEqual(type, column.MemberType);
					Assert.AreEqual(dataType, column.DataType);
				}
			}
		}
	}

	internal static class MySqlTestFunctions
	{
		public static int TestOutputParametersWithoutTableProcedure(this DataConnection dataConnection, string aInParam, out sbyte? aOutParam)
		{
			// WORKAROUND: db name needed for MySql.Data 8.0.21, as they managed to break already escaped procedure name handling
			var dbName = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema).ConvertInline(TestUtils.GetDatabaseName(dataConnection), ConvertType.NameToDatabase);
			var ret = dataConnection.ExecuteProc($"{dbName}.`TestOutputParametersWithoutTableProcedure`",
				new DataParameter("aInParam", aInParam, DataType.VarChar),
				new DataParameter("aOutParam", null, DataType.SByte) { Direction = ParameterDirection.Output });

			aOutParam = Converter.ChangeTypeTo<sbyte?>(((IDbDataParameter)dataConnection.Command.Parameters["aOutParam"]).Value);

			return ret;
		}

		public static IEnumerable<Person> TestProcedure(this DataConnection dataConnection, int? param3, ref int? param2, out int? param1)
		{
			// WORKAROUND: db name needed for MySql.Data 8.0.21, as they managed to break already escaped procedure name handling
			var dbName = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema).ConvertInline(TestUtils.GetDatabaseName(dataConnection), ConvertType.NameToDatabase);
			var ret = dataConnection.QueryProc<Person>($"{dbName}.`TestProcedure`",
				new DataParameter("param3", param3, DataType.Int32),
				new DataParameter("param2", param2, DataType.Int32) { Direction = ParameterDirection.InputOutput },
				new DataParameter("param1", null, DataType.Int32) { Direction = ParameterDirection.Output }).ToList();

			param2 = Converter.ChangeTypeTo<int?>(((IDbDataParameter)dataConnection.Command.Parameters["param2"]).Value);
			param1 = Converter.ChangeTypeTo<int?>(((IDbDataParameter)dataConnection.Command.Parameters["param1"]).Value);

			return ret;
		}
	}
}
