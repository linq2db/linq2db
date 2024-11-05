using System;
using System.Data.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.Access;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.DataProvider
{
	using System.Runtime.Serialization;

	using Model;

	[TestFixture]
	public class AccessTests : DataProviderTestBase
	{
		[Test]
		public void TestParameters([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			var param  = context.Contains("Odbc") ? "?" : "@p";
			var param1 = context.Contains("Odbc") ? "?" : "@p1";
			var param2 = context.Contains("Odbc") ? "?" : "@p2";

			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>($"SELECT {param}", new { p = 1 }), Is.EqualTo("1"));
					Assert.That(conn.Execute<string>($"SELECT {param}", new { p = "1" }), Is.EqualTo("1"));
					Assert.That(conn.Execute<int>($"SELECT {param}", new { p = new DataParameter { Value = 1 } }), Is.EqualTo(1));
					Assert.That(conn.Execute<string>($"SELECT {param1}", new { p1 = new DataParameter { Value = "1" } }), Is.EqualTo("1"));
					// doesn't really test ODBC parameters
					Assert.That(conn.Execute<int>($"SELECT {param1} + {param2}", new { p1 = 2, p2 = 3 }), Is.EqualTo(5));
					Assert.That(conn.Execute<int>($"SELECT {param2} + {param1}", new { p2 = 2, p1 = 3 }), Is.EqualTo(5));
				});
			}
		}

		protected override string? PassNullSql(DataConnection dc, out int paramCount)
		{
			paramCount = dc.DataProvider.Name.IsAnyOf(TestProvName.AllAccessOdbc) ? 3 : 1;
			return dc.DataProvider.Name.IsAnyOf(TestProvName.AllAccessOdbc)
				? "SELECT ID FROM {1} WHERE ? IS NULL AND {0} IS NULL OR ? IS NOT NULL AND {0} = ?"
				: base.PassNullSql(dc, out paramCount);
		}
		protected override string PassValueSql(DataConnection dc) =>
			dc.DataProvider.Name.IsAnyOf(TestProvName.AllAccessOdbc)
				? "SELECT ID FROM {1} WHERE {0} = ?"
				: base.PassValueSql(dc);

		[Test]
		public void TestDataTypes([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var isODBC = conn.DataProvider.Name.IsAnyOf(TestProvName.AllAccessOdbc);
				Assert.Multiple(() =>
				{
					Assert.That(TestType<bool>(conn, "bitDataType", DataType.Boolean, skipDefaultNull: isODBC), Is.EqualTo(true));
					Assert.That(TestType<short?>(conn, "smallintDataType", DataType.Int16, skipDefaultNull: isODBC), Is.EqualTo(25555));
					Assert.That(TestType<decimal?>(conn, "decimalDataType", DataType.Decimal, skipDefaultNull: isODBC), Is.EqualTo(2222222m));
					Assert.That(TestType<int?>(conn, "intDataType", DataType.Int32, skipDefaultNull: isODBC), Is.EqualTo(7777777));
					Assert.That(TestType<sbyte?>(conn, "tinyintDataType", DataType.SByte, skipDefaultNull: isODBC), Is.EqualTo(100));
					Assert.That(TestType<decimal?>(conn, "moneyDataType", DataType.Money, skipDefaultNull: isODBC), Is.EqualTo(100000m));
					Assert.That(TestType<double?>(conn, "floatDataType", DataType.Double, skipDefaultNull: isODBC), Is.EqualTo(20.31d));
					Assert.That(TestType<float?>(conn, "realDataType", DataType.Single, skipDefaultNull: isODBC), Is.EqualTo(16.2f));

					Assert.That(TestType<DateTime?>(conn, "datetimeDataType", DataType.DateTime, skipDefaultNull: isODBC), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));

					Assert.That(TestType<char?>(conn, "charDataType", DataType.Char, skipDefaultNull: isODBC), Is.EqualTo('1'));
					Assert.That(TestType<string>(conn, "varcharDataType", DataType.VarChar, skipDefaultNull: isODBC), Is.EqualTo("234"));
					Assert.That(TestType<string>(conn, "textDataType", DataType.Text, skipDefaultNull: isODBC), Is.EqualTo("567"));
					Assert.That(TestType<string>(conn, "ncharDataType", DataType.NChar, skipDefaultNull: isODBC), Is.EqualTo("23233"));
					Assert.That(TestType<string>(conn, "nvarcharDataType", DataType.NVarChar, skipDefaultNull: isODBC), Is.EqualTo("3323"));
					Assert.That(TestType<string>(conn, "ntextDataType", DataType.NText, skipDefaultNull: isODBC), Is.EqualTo("111"));

					Assert.That(TestType<byte[]>(conn, "binaryDataType", DataType.Binary, skipDefaultNull: isODBC), Is.EqualTo(new byte[] { 1, 2, 3, 4, 0, 0, 0, 0, 0, 0 }));
					Assert.That(TestType<byte[]>(conn, "varbinaryDataType", DataType.VarBinary, skipDefaultNull: isODBC), Is.EqualTo(new byte[] { 1, 2, 3, 5 }));
					Assert.That(TestType<byte[]>(conn, "imageDataType", DataType.Image, skipDefaultNull: isODBC), Is.EqualTo(new byte[] { 3, 4, 5, 6 }));
					Assert.That(TestType<byte[]>(conn, "oleobjectDataType", DataType.Variant, skipDefined: true, skipDefaultNull: isODBC), Is.EqualTo(new byte[] { 5, 6, 7, 8 }));

					Assert.That(TestType<Guid?>(conn, "uniqueidentifierDataType", DataType.Guid, skipDefaultNull: isODBC), Is.EqualTo(new Guid("{6F9619FF-8B86-D011-B42D-00C04FC964FF}")));
				});
			}
		}

		static void TestNumeric<T>(DataConnection conn, T expectedValue, DataType dataType, string skip = "cbool", bool isODBCNull = false)
		{
			var param = conn.DataProvider.Name.Contains("Odbc") ? "?" : "@p";

			var skipTypes = skip.Split(' ');

			if (expectedValue != null)
				foreach (var sqlType in new[]
					{
						"cbool",
						"cbyte",
						"clng",
						"cint",
						"ccur",
						"cdbl",
						"csng"
					}.Except(skipTypes))
				{
					var sqlValue = expectedValue is bool ? (bool)(object)expectedValue ? 1 : 0 : (object)expectedValue;

					var sql = string.Format(CultureInfo.InvariantCulture, "SELECT {0}({1})", sqlType, sqlValue);

					Debug.WriteLine(sql + " -> " + typeof(T));

					Assert.That(conn.Execute<T>(sql), Is.EqualTo(expectedValue));
				}

			var querySql = isODBCNull ? $"SELECT CVar({param})" : $"SELECT {param}";

			Debug.WriteLine("{0} -> DataType.{1}", typeof(T), dataType);
			Assert.That(conn.Execute<T>(querySql, new DataParameter { Name = "p", DataType = dataType, Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> auto", typeof(T));
			Assert.That(conn.Execute<T>(querySql, new DataParameter { Name = "p", Value = expectedValue }), Is.EqualTo(expectedValue));
			Debug.WriteLine("{0} -> new", typeof(T));
			Assert.That(conn.Execute<T>(querySql, new { p = expectedValue }), Is.EqualTo(expectedValue));
		}

		static void TestSimple<T>(DataConnection conn, T expectedValue, DataType dataType, bool isODBCNull)
			where T : struct
		{
			TestNumeric<T>(conn, expectedValue, dataType);
			TestNumeric<T?>(conn, expectedValue, dataType);

			TestNumeric<T?>(conn, (T?)null, dataType, isODBCNull: isODBCNull);
		}

		[Test]
		public void TestNumerics([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				// ODBC driver doesn't support null parameter in select
				var isODBC = conn.DataProvider.Name.IsAnyOf(TestProvName.AllAccessOdbc);

				TestSimple<bool   >(conn, true, DataType.Boolean   , isODBC);
				TestSimple<sbyte  >(conn, 1   , DataType.SByte     , isODBC);
				TestSimple<short  >(conn, 1   , DataType.Int16     , isODBC);
				TestSimple<int    >(conn, 1   , DataType.Int32     , isODBC);
				TestSimple<long   >(conn, 1L  , DataType.Int64     , isODBC);
				TestSimple<byte   >(conn, 1   , DataType.Byte      , isODBC);
				TestSimple<ushort >(conn, 1   , DataType.UInt16    , isODBC);
				TestSimple<uint   >(conn, 1u  , DataType.UInt32    , isODBC);
				TestSimple<ulong  >(conn, 1ul , DataType.UInt64    , isODBC);
				TestSimple<float  >(conn, 1   , DataType.Single    , isODBC);
				TestSimple<double >(conn, 1d  , DataType.Double    , isODBC);
				TestSimple<decimal>(conn, 1m  , DataType.Decimal   , isODBC);
				TestSimple<decimal>(conn, 1m  , DataType.VarNumeric, isODBC);
				TestSimple<decimal>(conn, 1m  , DataType.Money     , isODBC);
				TestSimple<decimal>(conn, 1m  , DataType.SmallMoney, isODBC);

				TestNumeric(conn, sbyte.MinValue, DataType.SByte, "cbool cbyte"          );
				TestNumeric(conn, sbyte.MaxValue, DataType.SByte                         );
				TestNumeric(conn, short.MinValue, DataType.Int16, "cbool cbyte"          );
				TestNumeric(conn, short.MaxValue, DataType.Int16, "cbool cbyte"          );
				TestNumeric(conn, int.MinValue  , DataType.Int32, "cbool cbyte cint"     );
				TestNumeric(conn, int.MaxValue  , DataType.Int32, "cbool cbyte cint csng");

				if (!isODBC)
				{
					// TODO: it is not clear if ODBC driver doesn't support 64-numbers at all or we just need
					// ACE 16 database
					TestNumeric(conn, long.MinValue , DataType.Int64 , "cbool cbyte cint clng ccur"          );
					TestNumeric(conn, long.MaxValue , DataType.Int64 , "cbool cbyte cint clng ccur cdbl csng");
					TestNumeric(conn, ulong.MaxValue, DataType.UInt64, "cbool cbyte cint clng csng ccur cdbl");
				}

				TestNumeric(conn, (long)int.MinValue  , DataType.Int64     , "cbool cbyte cint clng ccur"          );
				TestNumeric(conn, (long)int.MaxValue  , DataType.Int64     , "cbool cbyte cint clng ccur cdbl csng");
				TestNumeric(conn, (ulong)uint.MaxValue, DataType.UInt64    , "cbool cbyte cint clng csng ccur cdbl");

				TestNumeric(conn, byte.MaxValue       , DataType.Byte                                              );
				TestNumeric(conn, ushort.MaxValue     , DataType.UInt16    , "cbool cbyte cint"                    );
				TestNumeric(conn, uint.MaxValue       , DataType.UInt32    , "cbool cbyte cint clng csng"          );

				TestNumeric(conn, -3.40282306E+38f    , DataType.Single    , "cbool cbyte clng cint ccur"          );
				TestNumeric(conn, 3.40282306E+38f     , DataType.Single    , "cbool cbyte clng cint ccur"          );
				TestNumeric(conn, -1.79E+308d         , DataType.Double    , "cbool cbyte clng cint ccur csng"     );
				TestNumeric(conn, 1.79E+308d          , DataType.Double    , "cbool cbyte clng cint ccur csng"     );
				TestNumeric(conn, decimal.MinValue    , DataType.Decimal   , "cbool cbyte clng cint ccur cdbl csng");
				TestNumeric(conn, decimal.MaxValue    , DataType.Decimal   , "cbool cbyte clng cint ccur cdbl csng");
				TestNumeric(conn, 1.123456789m        , DataType.Decimal   , "cbool cbyte clng cint ccur cdbl csng");
				TestNumeric(conn, -1.123456789m       , DataType.Decimal   , "cbool cbyte clng cint ccur cdbl csng");
				TestNumeric(conn, -922337203685477m   , DataType.Money     , "cbool cbyte clng cint csng"          );
				TestNumeric(conn, +922337203685477m   , DataType.Money     , "cbool cbyte clng cint csng"          );
				TestNumeric(conn, -214748m            , DataType.SmallMoney, "cbool cbyte cint"                    );
				TestNumeric(conn, +214748m            , DataType.SmallMoney, "cbool cbyte cint"                    );
			}
		}

		[Test]
		public void TestDateTime([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			var param = context.Contains("Odbc") ? "?" : "@p";

			using (var conn = GetDataConnection(context))
			{
				var dateTime = new DateTime(2012, 12, 12, 12, 12, 12);

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<DateTime>("SELECT cdate('2012-12-12 12:12:12')"), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>("SELECT CDate('2012-12-12 12:12:12')"), Is.EqualTo(dateTime));

					Assert.That(conn.Execute<DateTime>($"SELECT {param}", DataParameter.DateTime("p", dateTime)), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>($"SELECT {param}", new DataParameter("p", dateTime)), Is.EqualTo(dateTime));
					Assert.That(conn.Execute<DateTime?>($"SELECT {param}", new DataParameter("p", dateTime, DataType.DateTime)), Is.EqualTo(dateTime));
				});
			}
		}

		[Test]
		public void TestChar([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			var param = context.Contains("Odbc") ? "?" : "@p";

			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<char>("SELECT CStr('1')"), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>("SELECT CStr('1')"), Is.EqualTo('1'));

					Assert.That(conn.Execute<char>($"SELECT {param}", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>($"SELECT {param}", DataParameter.Char("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>($"SELECT CStr({param})", DataParameter.Char("p", '1')), Is.EqualTo('1'));

					Assert.That(conn.Execute<char>($"SELECT {param}", DataParameter.VarChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>($"SELECT {param}", DataParameter.VarChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>($"SELECT {param}", DataParameter.NChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>($"SELECT {param}", DataParameter.NChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>($"SELECT {param}", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>($"SELECT {param}", DataParameter.NVarChar("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char>($"SELECT {param}", DataParameter.Create("p", '1')), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>($"SELECT {param}", DataParameter.Create("p", '1')), Is.EqualTo('1'));

					Assert.That(conn.Execute<char>($"SELECT {param}", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
					Assert.That(conn.Execute<char?>($"SELECT {param}", new DataParameter { Name = "p", Value = '1' }), Is.EqualTo('1'));
				});
			}
		}

		[Test]
		public void TestString([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			// ODBC driver doesn't support null parameter in select
			var isODBC = context.Contains("Odbc");
			var param = isODBC ? "?" : "@p";

			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT CStr('12345')"), Is.EqualTo("12345"));
					Assert.That(conn.Execute<string>("SELECT NULL"), Is.Null);

					Assert.That(conn.Execute<string>($"SELECT {param} & 1", DataParameter.Char("p", "123")), Is.EqualTo("1231"));
					Assert.That(conn.Execute<string>($"SELECT {param}", DataParameter.VarChar("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>($"SELECT {param}", DataParameter.Text("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>($"SELECT {param}", DataParameter.NChar("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>($"SELECT {param}", DataParameter.NVarChar("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>($"SELECT {param}", DataParameter.NText("p", "123")), Is.EqualTo("123"));
					Assert.That(conn.Execute<string>($"SELECT {param}", DataParameter.Create("p", "123")), Is.EqualTo("123"));
				});

				if (isODBC) // ODBC provider doesn't return type for NULL value
					Assert.That(conn.Execute<string>($"SELECT CVar({param})", DataParameter.Create("p", (string?)null)), Is.EqualTo(null));
				else
					Assert.That(conn.Execute<string>($"SELECT {param}", DataParameter.Create("p", (string?)null)), Is.EqualTo(null));

				Assert.That(conn.Execute<string>($"SELECT {param}", new DataParameter { Name = "p", Value = "1" }), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestBinary([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			var isODBC = context.Contains("Odbc");
			var param = isODBC ? "?" : "@p";

			var arr1 = new byte[] { 48, 57 };
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<byte[]>($"SELECT {param}", DataParameter.Binary("p", arr1)), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>($"SELECT {param}", DataParameter.VarBinary("p", arr1)), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>($"SELECT {param}", DataParameter.Create("p", arr1)), Is.EqualTo(arr1));
				});

				if (isODBC) // ODBC provider doesn't return type for NULL value
					Assert.That(conn.Execute<byte[]>($"SELECT CVar({param})", DataParameter.VarBinary("p", null)), Is.EqualTo(null));
				else
					Assert.That(conn.Execute<byte[]>($"SELECT {param}", DataParameter.VarBinary("p", null)), Is.EqualTo(null));

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<byte[]>($"SELECT {param}", DataParameter.VarBinary("p", Array.Empty<byte>())), Is.EqualTo(Array.Empty<byte>()));
					Assert.That(conn.Execute<byte[]>($"SELECT {param}", DataParameter.Image("p", Array.Empty<byte>())), Is.EqualTo(Array.Empty<byte>()));
					Assert.That(conn.Execute<byte[]>($"SELECT {param}", new DataParameter { Name = "p", Value = arr1 }), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>($"SELECT {param}", DataParameter.Create("p", new Binary(arr1))), Is.EqualTo(arr1));
					Assert.That(conn.Execute<byte[]>($"SELECT {param}", new DataParameter("p", new Binary(arr1))), Is.EqualTo(arr1));
				});
			}
		}

		[Test]
		public void TestGuid([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			var param = context.Contains("Odbc") ? "?" : "@p";

			using (var conn = GetDataConnection(context))
			{
				var guid = TestData.Guid1;

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<Guid>($"SELECT {param}", DataParameter.Create("p", guid)), Is.EqualTo(guid));
					Assert.That(conn.Execute<Guid>($"SELECT {param}", new DataParameter { Name = "p", Value = guid }), Is.EqualTo(guid));
				});
			}
		}

		[Test]
		public void TestSqlVariant([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			var isODBC = context.Contains("Odbc");
			var param = isODBC ? "?" : "@p";

			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<object>("SELECT CVar(1)"), Is.EqualTo("1"));
					Assert.That(conn.Execute<int>("SELECT CVar(1)"), Is.EqualTo(1));
					Assert.That(conn.Execute<int?>("SELECT CVar(1)"), Is.EqualTo(1));
					Assert.That(conn.Execute<string>("SELECT CVar(1)"), Is.EqualTo("1"));
				});

				// ODBC doesn't have variant type and maps it to Binary
				if (!isODBC)
					Assert.That(conn.Execute<string>($"SELECT {param}", DataParameter.Variant("p", 1)), Is.EqualTo("1"));
			}
		}

		[Test]
		public void TestXml([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			var param = context.Contains("Odbc") ? "?" : "@p";

			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>("SELECT '<xml/>'"), Is.EqualTo("<xml/>"));
					Assert.That(conn.Execute<XDocument>("SELECT '<xml/>'").ToString(), Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XmlDocument>("SELECT '<xml/>'").InnerXml, Is.EqualTo("<xml />"));
				});

				var xdoc = XDocument.Parse("<xml/>");
				var xml = Convert<string, XmlDocument>.Lambda("<xml/>");

				Assert.Multiple(() =>
				{
					Assert.That(conn.Execute<string>($"SELECT {param}", DataParameter.Xml("p", "<xml/>")), Is.EqualTo("<xml/>"));
					Assert.That(conn.Execute<XDocument>($"SELECT {param}", DataParameter.Xml("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XmlDocument>($"SELECT {param}", DataParameter.Xml("p", xml)).InnerXml, Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XDocument>($"SELECT {param}", new DataParameter("p", xdoc)).ToString(), Is.EqualTo("<xml />"));
					Assert.That(conn.Execute<XDocument>($"SELECT {param}", new DataParameter("p", xml)).ToString(), Is.EqualTo("<xml />"));
				});
			}
		}

		enum TestEnum
		{
			[MapValue("A")] AA,
			[MapValue("B")] BB,
		}

		[Test]
		public void TestEnum1([IncludeDataSources(TestProvName.AllAccess)] string context)
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
		public void TestEnum2([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			var param = context.Contains("Odbc") ? "?" : "@p";

			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Query<string>($"SELECT {param}", new { p = TestEnum.AA }).First(), Is.EqualTo("A"));
					Assert.That(conn.Query<string>($"SELECT {param}", new { p = (TestEnum?)TestEnum.BB }).First(), Is.EqualTo("B"));
					Assert.That(conn.Query<string>($"SELECT {param}", new { p = ConvertTo<string>.From((TestEnum?)TestEnum.AA) }).First(), Is.EqualTo("A"));
					Assert.That(conn.Query<string>($"SELECT {param}", new { p = ConvertTo<string>.From(TestEnum.AA) }).First(), Is.EqualTo("A"));
					Assert.That(conn.Query<string>($"SELECT {param}", new { p = conn.MappingSchema.GetConverter<TestEnum?, string>()!(TestEnum.AA) }).First(), Is.EqualTo("A"));
				});
			}
		}

		[Table(Name = "CreateTableTest", Schema = "IgnoreSchema", Database = "TestDatabase")]
		public class CreateTableTest
		{
			[PrimaryKey, Identity]
			public int Id;
		}

		[Test]
		public void CreateDatabase([IncludeDataSources(TestProvName.AllAccessOleDb)] string context)
		{
			var cs = DataConnection.GetConnectionString(context);
			AccessVersion version;
			string providerName;

			string expectedName;
			string? expectedExtension = null;
			if (cs.Contains("Microsoft.Jet.OLEDB.4.0"))
			{
				version = AccessVersion.Jet;
				providerName = "Microsoft.Jet.OLEDB.4.0";
				expectedName = "TestDatabase.mdb";
			}
			else
			{
				version = AccessVersion.Ace;
				providerName = "Microsoft.ACE.OLEDB.12.0";
				expectedName = "TestDatabase.accdb";
				expectedExtension = ".accdb";
			}

			AccessTools.CreateDatabase("TestDatabase", deleteIfExists: true, version: version);
			Assert.That(File.Exists(expectedName), Is.True);

			var connectionString = $"Provider={providerName};Data Source={expectedName};Locale Identifier=1033;Persist Security Info=True";
			using (var db = new DataConnection(AccessTools.GetDataProvider(version, AccessProvider.AutoDetect, connectionString), connectionString))
			{
				db.CreateTable<SqlCeTests.CreateTableTest>();
				db.DropTable<SqlCeTests.CreateTableTest>();
			}

			AccessTools.DropDatabase("TestDatabase", expectedExtension);
			Assert.That(File.Exists(expectedName), Is.False);
		}

		[Test]
		public void BulkCopyLinqTypes([IncludeDataSources(TestProvName.AllAccess)] string context)
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
		public async Task BulkCopyLinqTypesAsync([IncludeDataSources(TestProvName.AllAccess)] string context)
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
						db.GetTable<LinqDataTypes>().Delete(p => p.ID >= 4000);
					}
				}
			}
		}

		[Test]
		[Explicit("Long running test. Run explicitly.")]
		//		[Timeout(60000)]
		public void DataConnectionTest([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			var cs = DataConnection.GetConnectionString(context);

			for (var i = 0; i < 1000; i++)
			{
				using (var db = AccessTools.CreateDataConnection(cs))
				{
					var list = db.GetTable<Person>().Where(p => p.ID > 0).ToList();
				}
			}
		}

		public class DateTable
		{
			[Column] public int      ID   { get; set; }
			[Column] public DateTime Date { get; set; }
		}

		[Test]
		public void TestZeroDate([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db    = GetDataConnection(context))
			using (var table = db.CreateLocalTable<DateTable>())
			{
				table.Insert(() => new DateTable() { ID = 1, Date = new DateTime(1899, 12, 29)});
				table.Insert(() => new DateTable() { ID = 2, Date = new DateTime(1899, 12, 30)});
				table.Insert(() => new DateTable() { ID = 3, Date = new DateTime(1899, 12, 31) });
				table.Insert(() => new DateTable() { ID = 4, Date = new DateTime(1900, 1, 1) });

				var res = table.OrderBy(_ => _.ID).ToArray();

				Assert.That(res, Has.Length.EqualTo(4));
				Assert.Multiple(() =>
				{
					Assert.That(res[0].ID, Is.EqualTo(1));
					Assert.That(res[0].Date, Is.EqualTo(new DateTime(1899, 12, 29)));
					Assert.That(res[1].ID, Is.EqualTo(2));
					Assert.That(res[1].Date, Is.EqualTo(new DateTime(1899, 12, 30)));
					Assert.That(res[2].ID, Is.EqualTo(3));
					Assert.That(res[2].Date, Is.EqualTo(new DateTime(1899, 12, 31)));
					Assert.That(res[3].ID, Is.EqualTo(4));
					Assert.That(res[3].Date, Is.EqualTo(new DateTime(1900, 1, 1)));
				});
			}
		}

		[Test]
		public void TestParametersWrapping(
			[IncludeDataSources(TestProvName.AllAccessOdbc)] string context,
			[Values] bool hasDistinct,
			[Values] bool hasFrom,
			[Values] bool hasValue,
			[Values] bool isParameter)
		{
			var needsWrap = hasDistinct && hasFrom
				// should be parameter or NULL literal
				&& (isParameter || !hasValue);

			using var db = GetDataConnection(context);

			int? value = hasValue ? 5 : null;

			if (hasFrom)
			{
				var query = isParameter
					? db.Person.Select(r => new { Value = value })
					: hasValue
						? db.Person.Select(r => new { Value = (int?)5 })
						: db.Person.Select(r => new { Value = (int?)null });

				if (hasDistinct)
					query = query.Distinct();

				query.ToList();

				if (isParameter)
					Assert.That(db.LastQuery, needsWrap ? Does.Contain("CVar") : Does.Not.Contain("CVar"));
				else
					Assert.That(db.LastQuery, needsWrap ? Does.Contain("IIF(False") : Does.Not.Contain("IIF(False"));
			}
			else
			{
				// all those should pass as we don't have FROM clause
				if (!hasDistinct)
				{
					db.Select(() => new { Value = value });

					Assert.That(db.LastQuery, Does.Not.Contain("CVar"));
				}
				else if (isParameter)
				{
					Assert.DoesNotThrow(() => db.ExecuteReader("SELECT DISTINCT ?", new DataParameter() { Value = value }));
					Assert.DoesNotThrow(() => db.ExecuteReader("SELECT DISTINCT CVar(?)", new DataParameter() { Value = value }));
				}
				else if (hasValue)
				{
					Assert.DoesNotThrow(() => db.ExecuteReader("SELECT DISTINCT 3"));
					Assert.DoesNotThrow(() => db.ExecuteReader("SELECT DISTINCT CVar(3)"));

				}
				else
				{
					Assert.DoesNotThrow(() => db.ExecuteReader("SELECT DISTINCT NULL"));
					Assert.DoesNotThrow(() => db.ExecuteReader("SELECT DISTINCT CVar(NULL)"));
				}
			}
		}

		#region Issue 1906
		public class CtqResultModel
		{
			[Column, PrimaryKey, Identity]
			public int ResultId { get; set; }

			[Column, NotNull]
			public int DefinitionId { get; set; }

			[Association(ThisKey = nameof(DefinitionId), OtherKey = nameof(CtqDefinitionModel.DefinitionId), CanBeNull = false)]
			public CtqDefinitionModel Definition { get; set; } = null!;
		}

		public class CtqDefinitionModel
		{
			[Column, PrimaryKey, Identity]
			public int DefinitionId { get; set; }

			[Column, NotNull]
			public int SetId { get; set; }

			[Association(ThisKey = nameof(SetId), OtherKey = nameof(CtqSetModel.SetId), CanBeNull = true)]
			public CtqSetModel? Set { get; set; }
		}

		public class CtqSetModel
		{
			[Column, PrimaryKey, Identity]
			public int SetId { get; set; }

			[Column, NotNull]
			public int SectorId { get; set; }

			[Association(ThisKey = nameof(SectorId), OtherKey = nameof(FtqSectorModel.Id), CanBeNull = false)]
			public FtqSectorModel Sector { get; set; } = null!;
		}

		public class FtqSectorModel
		{
			[Column, PrimaryKey, Identity]
			public int Id { get; set; }
		}

		[Test]
		public void Issue1906Test([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable<CtqResultModel>())
			using (db.CreateLocalTable<CtqDefinitionModel>())
			using (db.CreateLocalTable<CtqSetModel>())
			using (db.CreateLocalTable<FtqSectorModel>())
			{
				db.GetTable<CtqResultModel>()
					.LoadWith(f => f.Definition.Set!.Sector)
					.ToList();
			}
		}
		#endregion

		#region Issue 3893
		// use characters from https://learn.microsoft.com/en-us/office/troubleshoot/access/error-using-special-characters
		private static readonly string[] _identifiers =
		[
			" leading_space",
			"char `",
			"char !",
			"char .",
			"char ]",
			"char [",
			"char \r",
			"char \t",
			"char \b",
			"char \n",
			"char >",
			"char <",
			"char *",
			"char :",
			"char ^",
			"char +",
			"char \\",
			"char /",
			"char =",
			"char &",
			"char '",
			"char \"",
			"char @",
			"char #",
			"char %",
			"char $",
			"char ;",
			"char ?",
			"char {",
			"char }",
			"char -",
			"char ~",
			"char |",
		];

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3893")]
		public void Issue3893Test([IncludeDataSources(TestProvName.AllAccess)] string context, [ValueSource(nameof(_identifiers))] string columName)
		{
			var builder = new FluentMappingBuilder(new MappingSchema())
				.Entity<Issue3893Table>()
					.Property(x => x.Id)
					.HasColumnName(columName)
				.Build();

			using var db = GetDataConnection(context, builder.MappingSchema);

			using var tb = db.CreateLocalTable<Issue3893Table>();

			var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);
			var table = schema.Tables.Single(t => t.TableName == nameof(Issue3893Table));
			var column = table.Columns.Single();

			Assert.That(column.ColumnName, Is.EqualTo(columName));
		}

		[Table]
		sealed class Issue3893Table
		{
			public int Id { get; set; }
		}
		#endregion
	}
}
