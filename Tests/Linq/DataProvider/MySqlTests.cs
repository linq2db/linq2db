using System;
using System.Data.Linq;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.Tools;

using NUnit.Framework;

using MySql.Data.Types;

namespace Tests.DataProvider
{
	using LinqToDB.SchemaProvider;
	using Model;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;

	[TestFixture]
	public class MySqlTests : DataProviderTestBase
	{
		[AttributeUsage(AttributeTargets.Method)]
		class MySqlDataContextAttribute : IncludeDataContextSourceAttribute
		{
			public MySqlDataContextAttribute()
				: this(false)
			{
			}
			public MySqlDataContextAttribute(bool includeLinqService)
				: base(includeLinqService, ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57)
			{
			}
		}

		[Test, MySqlDataContext]
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

		[Test, MySqlDataContext]
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
				Assert.That(TestType<int?>          (conn, "year2DataType",     DataType.Int32),               Is.EqualTo(context == TestProvName.MySql57 ? 1997 : 97));
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

		[Test, MySqlDataContext]
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

		[Test, MySqlDataContext]
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

		[Test, MySqlDataContext]
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

				conn.GetTable<ALLTYPE>().Delete(p => p.SMALLINTDATATYPE >= 5000);
			}
		}

		[Test, MySqlDataContext, Ignore("It works too long.")]
		public void BulkCopyMultipleRows(string context)
		{
			BulkCopyTest(context, BulkCopyType.MultipleRows);
		}

		[Test, MySqlDataContext, Explicit("It works too long.")]
		public void BulkCopyRetrieveSequencesMultipleRows(string context)
		{
			BulkCopyRetrieveSequence(context, BulkCopyType.MultipleRows);
		}

		[Test, MySqlDataContext, Ignore("It works too long.")]
		public void BulkCopyProviderSpecific(string context)
		{
			BulkCopyTest(context, BulkCopyType.ProviderSpecific);
		}

		[Test, MySqlDataContext, Explicit("It works too long.")]
		public void BulkCopyRetrieveSequencesProviderSpecific(string context)
		{
			BulkCopyRetrieveSequence(context, BulkCopyType.ProviderSpecific);
		}

		[Test, MySqlDataContext]
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
						}));
					db.GetTable<LinqDataTypes>().Delete(p => p.ID >= 4000);
				}
			}
		}

		static void BulkCopyRetrieveSequence(string context, BulkCopyType bulkCopyType)
		{
			var data = new[]
			{
				new Doctor { Taxonomy = "Neurologist"},
				new Doctor { Taxonomy = "Sports Medicine"},
				new Doctor { Taxonomy = "Optometrist"},
				new Doctor { Taxonomy = "Pediatrics" },
				new Doctor { Taxonomy = "Psychiatry" }
			};

			using (var db = new TestDataConnection(context))
			{
				var options = new BulkCopyOptions
				{
					MaxBatchSize = 5,
					//RetrieveSequence = true,
					KeepIdentity = true,
					BulkCopyType = bulkCopyType,
					NotifyAfter  = 3,
					RowsCopiedCallback = copied => Debug.WriteLine(copied.RowsCopied)
				};
				db.BulkCopy(options, data.RetrieveIdentity(db));

				foreach (var d in data)
					Assert.That(d.PersonID, Is.GreaterThan(0));
			}
		}

		[Test, MySqlDataContext]
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

		[Test, MySqlDataContext]
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

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
		[Test, MySqlDataContext(false)]
		public void SchemaProviderTest(string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			{
				var sp = db.DataProvider.GetSchemaProvider();
				var schema = sp.GetSchema(db);

				var systemTables = schema.Tables.Where(_ => _.CatalogName.Equals("sys", StringComparison.OrdinalIgnoreCase)).ToList();

				Assert.That(systemTables.All(_ => _.IsProviderSpecific));

				var views = schema.Tables.Where(_ => _.IsView).ToList();
				Assert.AreEqual(1, views.Count);
			}
		}

		public static IEnumerable<ProcedureSchema> ProcedureTestCases
		{
			get
			{
				// create procedure
				yield return new ProcedureSchema()
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
				};

				// create function
				yield return new ProcedureSchema()
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
							SchemaType    = "VARCHAR",
							IsResult      = true,
							ParameterName = "par1",
							ParameterType = "string",
							SystemType    = typeof(string),
							DataType      = DataType.VarChar
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
				};

				// create function
				yield return new ProcedureSchema()
				{
					CatalogName     = "SET_BY_TEST",
					ProcedureName   = "test_proc",
					MemberName      = "test_proc",
					IsDefaultSchema = true,
					Parameters      = new List<ParameterSchema>()
					{
						new ParameterSchema()
						{
							SchemaName    = "aInParam",
							SchemaType    = "VARCHAR",
							IsIn          = true,
							ParameterName = "aInParam",
							ParameterType = "string",
							SystemType    = typeof(string),
							DataType      = DataType.VarChar
						},
						new ParameterSchema()
						{
							SchemaName    = "aOutParam",
							SchemaType    = "TINYINT",
							IsOut         = true,
							ParameterName = "aOutParam",
							ParameterType = "sbyte",
							SystemType    = typeof(sbyte),
							DataType      = DataType.SByte
						}
					}
				};
			}
		}

		[Test, Combinatorial]
		public void ProceduresSchemaProviderTest(
			[IncludeDataSources(false, ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57)] string context,
			[ValueSource(nameof(ProcedureTestCases))] ProcedureSchema expectedProc)
		{
			// TODO: add aggregate/udf functions test cases
			using (var db = (DataConnection)GetDataContext(context))
			{
				expectedProc.CatalogName = TestUtils.GetDatabaseName(db);

				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);

				var procedures = schema.Procedures.Where(_ => _.ProcedureName == expectedProc.ProcedureName).ToList();

				Assert.AreEqual(1, procedures.Count);

				var procedure = procedures[0];

				Assert.AreEqual(expectedProc.CatalogName,         procedure.CatalogName);
				Assert.AreEqual(expectedProc.SchemaName,          procedure.SchemaName);
				Assert.AreEqual(expectedProc.MemberName,          procedure.MemberName);
				Assert.AreEqual(expectedProc.IsTableFunction,     procedure.IsTableFunction);
				Assert.AreEqual(expectedProc.IsAggregateFunction, procedure.IsAggregateFunction);
				Assert.AreEqual(expectedProc.IsDefaultSchema,     procedure.IsDefaultSchema);
				Assert.AreEqual(expectedProc.IsLoaded,            procedure.IsLoaded);

				Assert.IsNull(procedure.ResultException);

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
					var actualTable = procedure.ResultTable;

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
							.SingleOrDefault();

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
#endif

		[Sql.Expression("@n:=@n+1", ServerSideOnly = true)]
		static int IncrementIndex()
		{
			throw new NotImplementedException();
		}

		[Description("https://stackoverflow.com/questions/50858172/linq2db-mysql-set-row-index/50958483")]
		[Test, MySqlDataContext]
		public void RowIndexTest(string context)
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

		[Test, MySqlDataContext(false)]
		public void TestTestProcedure(string context)
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

		//[Test, MySqlDataContext]
		//public void TestSingleOutParameterFunction(string context)
		//{
		//	using (var db = GetDataContext(context))
		//	{
		//		db.TestProc("test", out var value);

		//		Assert.AreEqual(1, value);
		//	}
		//}

		//public static void TestProc(this DataConnection dataConnection, string aInParam, out sbyte? aOutParam)
		//{
		//	var ret = dataConnection.QueryProc("`test_proc`",
		//		new DataParameter("aInParam", aInParam, DataType.VarChar));

		//	aOutParam = Converter.ChangeTypeTo<sbyte?>(((IDbDataParameter)dataConnection.Command.Parameters["aOutParam"]).Value);

		//	return ret;
		//}
	}

	internal static class MySqlTestFunctions
	{
		public static IEnumerable<Person> TestProcedure(this DataConnection dataConnection, int? param3, ref int? param2, out int? param1)
		{
			var ret = dataConnection.QueryProc<Person>("`TestProcedure`",
				new DataParameter("param3", param3, DataType.Int32),
				new DataParameter("param2", param2, DataType.Int32) { Direction = ParameterDirection.InputOutput },
				new DataParameter("param1", null, DataType.Int32) { Direction = ParameterDirection.Output }).ToList();

			param2 = Converter.ChangeTypeTo<int?>(((IDbDataParameter)dataConnection.Command.Parameters["param2"]).Value);
			param1 = Converter.ChangeTypeTo<int?>(((IDbDataParameter)dataConnection.Command.Parameters["param1"]).Value);

			return ret;
		}
	}
}
