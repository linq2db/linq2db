using System;
using System.Data.Linq;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using MySql.Data.Types;

namespace Tests.DataProvider
{
	using Model;

	[TestFixture]
	public class MySqlTest : DataProviderTestBase
	{
		[AttributeUsage(AttributeTargets.Method)]
		class MySqlDataContextAttribute : IncludeDataContextSourceAttribute
		{
			public MySqlDataContextAttribute()
				: base(ProviderName.MySql, TestProvName.MariaDB)
			{
			}
		}

		[Test, MySqlDataContextAttribute]
		public void TestParameters(string context)
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

		[Test, MySqlDataContextAttribute]
		public void TestDataTypes(string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(TestType<long?>         (conn, "bigintDataType",    DataType.Int64),               Is.EqualTo(1000000));
				Assert.That(TestType<short?>        (conn, "smallintDataType",  DataType.Int16),               Is.EqualTo(25555));
				Assert.That(TestType<sbyte?>        (conn, "tinyintDataType",   DataType.SByte),               Is.EqualTo(111));
				Assert.That(TestType<int?>          (conn, "mediumintDataType", DataType.Int32),               Is.EqualTo(5555));
				Assert.That(TestType<int?>          (conn, "intDataType",       DataType.Int32),               Is.EqualTo(7777777));
				Assert.That(TestType<decimal?>      (conn, "numericDataType",   DataType.Decimal),             Is.EqualTo(9999999m));
				Assert.That(TestType<decimal?>      (conn, "decimalDataType",   DataType.Decimal),             Is.EqualTo(8888888m));
				            TestType<MySqlDecimal?> (conn, "decimalDataType",   DataType.Decimal);
				Assert.That(TestType<double?>       (conn, "doubleDataType",    DataType.Double),              Is.EqualTo(20.31d));
				Assert.That(TestType<float?>        (conn, "floatDataType",     DataType.Single),              Is.EqualTo(16.0f));

				Assert.That(TestType<DateTime?>     (conn, "dateDataType",      DataType.Date),                Is.EqualTo(new DateTime(2012, 12, 12)));
				Assert.That(TestType<DateTime?>     (conn, "datetimeDataType",  DataType.DateTime),            Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(TestType<DateTime?>     (conn, "datetimeDataType",  DataType.DateTime2),           Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(TestType<MySqlDateTime?>(conn, "datetimeDataType",  DataType.DateTime),            Is.EqualTo(new MySqlDateTime(2012, 12, 12, 12, 12, 12, 0)));
				Assert.That(TestType<DateTime?>     (conn, "timestampDataType", DataType.Timestamp),           Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(TestType<TimeSpan?>     (conn, "timeDataType",      DataType.Time),                Is.EqualTo(new TimeSpan(12, 12, 12)));
				Assert.That(TestType<int?>          (conn, "yearDataType",      DataType.Int32),               Is.EqualTo(1998));
				Assert.That(TestType<int?>          (conn, "year2DataType",     DataType.Int32),               Is.EqualTo(97));
				Assert.That(TestType<int?>          (conn, "year4DataType",     DataType.Int32),               Is.EqualTo(2012));

				Assert.That(TestType<char?>         (conn, "charDataType",      DataType.Char),                Is.EqualTo('1'));
				Assert.That(TestType<string>        (conn, "charDataType",      DataType.Char),                Is.EqualTo("1"));
				Assert.That(TestType<string>        (conn, "charDataType",      DataType.NChar),               Is.EqualTo("1"));
				Assert.That(TestType<string>        (conn, "varcharDataType",   DataType.VarChar),             Is.EqualTo("234"));
				Assert.That(TestType<string>        (conn, "varcharDataType",   DataType.NVarChar),            Is.EqualTo("234"));
				Assert.That(TestType<string>        (conn, "textDataType",      DataType.Text),                Is.EqualTo("567"));

				Assert.That(TestType<byte[]>        (conn, "binaryDataType",    DataType.Binary),              Is.EqualTo(new byte[] {  97,  98,  99 }));
				Assert.That(TestType<byte[]>        (conn, "binaryDataType",    DataType.VarBinary),           Is.EqualTo(new byte[] {  97,  98,  99 }));
				Assert.That(TestType<byte[]>        (conn, "varbinaryDataType", DataType.Binary),              Is.EqualTo(new byte[] {  99, 100, 101 }));
				Assert.That(TestType<byte[]>        (conn, "varbinaryDataType", DataType.VarBinary),           Is.EqualTo(new byte[] {  99, 100, 101 }));
				Assert.That(TestType<Binary>        (conn, "varbinaryDataType", DataType.VarBinary).ToArray(), Is.EqualTo(new byte[] {  99, 100, 101 }));
				Assert.That(TestType<byte[]>        (conn, "blobDataType",      DataType.Binary),              Is.EqualTo(new byte[] { 100, 101, 102 }));
				Assert.That(TestType<byte[]>        (conn, "blobDataType",      DataType.VarBinary),           Is.EqualTo(new byte[] { 100, 101, 102 }));
				Assert.That(TestType<byte[]>        (conn, "blobDataType",      DataType.Blob),                Is.EqualTo(new byte[] { 100, 101, 102 }));

				Assert.That(TestType<ulong?>        (conn, "bitDataType"),                                     Is.EqualTo(5));
				Assert.That(TestType<string>        (conn, "enumDataType"),                                    Is.EqualTo("Green"));
				Assert.That(TestType<string>        (conn, "setDataType"),                                     Is.EqualTo("one"));
			}
		}

		[Test, MySqlDataContextAttribute]
		public void TestDate(string context)
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

		[Test, MySqlDataContextAttribute]
		public void TestDateTime(string context)
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

		[Test, MySqlDataContextAttribute]
		public void TestChar(string context)
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

		[Test, MySqlDataContextAttribute]
		public void TestString(string context)
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

		[Test, MySqlDataContextAttribute]
		public void TestBinary(string context)
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

		[Test, MySqlDataContextAttribute]
		public void TestXml(string context)
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

		[Test, MySqlDataContextAttribute]
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

		[Test, MySqlDataContextAttribute]
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

		[Table("alltypes")]
		public partial class AllType
		{
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
			[Column,     Nullable] public int?      yearDataType        { get; set; } // year(4)
			[Column,     Nullable] public int?      year2DataType       { get; set; } // year(2)
			[Column,     Nullable] public int?      year4DataType       { get; set; } // year(4)
			[Column,     Nullable] public char?     charDataType        { get; set; } // char(1)
			[Column,     Nullable] public string    varcharDataType     { get; set; } // varchar(20)
			[Column,     Nullable] public string    textDataType        { get; set; } // text
			[Column,     Nullable] public byte[]    binaryDataType      { get; set; } // binary(3)
			[Column,     Nullable] public byte[]    varbinaryDataType   { get; set; } // varbinary(5)
			[Column,     Nullable] public byte[]    blobDataType        { get; set; } // blob
			[Column,     Nullable] public UInt64?   bitDataType         { get; set; } // bit(3)
			[Column,     Nullable] public string    enumDataType        { get; set; } // enum('Green','Red','Blue')
			[Column,     Nullable] public string    setDataType         { get; set; } // set('one','two')
			[Column,     Nullable] public uint?     intUnsignedDataType { get; set; } // int(10) unsigned
		}

		void BulkCopyTest(string context, BulkCopyType bulkCopyType)
		{
			using (var conn = new DataConnection(context))
			{
				conn.BeginTransaction();

				conn.BulkCopy(new BulkCopyOptions { MaxBatchSize = 50000, BulkCopyType = bulkCopyType },
					Enumerable.Range(0, 100000).Select(n =>
						new AllType
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
							dateDataType        = DateTime.Now,
							datetimeDataType    = DateTime.Now,
							timestampDataType   = null,
							timeDataType        = null,
							yearDataType        = (1000 + n) % 100,
							year2DataType       = (1000 + n) % 100,
							year4DataType       = null,
							charDataType        = 'A',
							varcharDataType     = "",
							textDataType        = "",
							binaryDataType      = null,
							varbinaryDataType   = null,
							blobDataType        = new byte[] { 1, 2, 3 },
							bitDataType         = null,
							enumDataType        = "Green",
							setDataType         = "one",
							intUnsignedDataType = (uint)(5000 + n),
						}));

				//var list = conn.GetTable<ALLTYPE>().ToList();

				conn.GetTable<DB2Test.ALLTYPE>().Delete(p => p.SMALLINTDATATYPE >= 5000);
			}
		}

		[Test, MySqlDataContextAttribute, Ignore("It works too long.")]
		public void BulkCopyMultipleRows(string context)
		{
			BulkCopyTest(context, BulkCopyType.MultipleRows);
		}

		[Test, MySqlDataContextAttribute, Ignore("It works too long.")]
		public void BulkCopyProviderSpecific(string context)
		{
			BulkCopyTest(context, BulkCopyType.ProviderSpecific);
		}

		[Test, MySqlDataContextAttribute]
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

		[Test, MySqlDataContextAttribute]
		public void TestTransaction1(string context)
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

		[Test, MySqlDataContextAttribute]
		public void TestTransaction2(string context)
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
	}
}
