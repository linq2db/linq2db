using System;
using System.Linq;
using System.Text;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	public partial class MergeTests : TestBase
	{
		[Table("unspecified")]
		sealed class MergeTypes
		{
			[Column("Id")]
			[PrimaryKey]
			public int Id;

			[Column("Field1")]
			public int? FieldInt32;

			[Column(IsColumn = false, Configuration = ProviderName.Access)]
			[Column("FieldInt64")]
			public long? FieldInt64;

			[Column(IsColumn = false, Configuration = ProviderName.Sybase)]
			[Column("FieldBoolean")]
			public bool? FieldBoolean;

			[Column("FieldString")]
			public string? FieldString;

			[Column(IsColumn = false, Configuration = ProviderName.Informix)]
			[Column("FieldNString")]
			public string? FieldNString;

			[Column("FieldChar")]
			public char? FieldChar;

			[Column(IsColumn = false, Configuration = ProviderName.Informix)]
			[Column("FieldNChar")]
			public char? FieldNChar;

			[Column("FieldFloat")]
			public float? FieldFloat;

			[Column(IsColumn = false, Configuration = ProviderName.Firebird)] // disabled due to test data
			[Column("FieldDouble")]
			public double? FieldDouble;

			[Column("FieldDateTime", Configuration = ProviderName.Sybase, DataType = DataType.DateTime)]
			[Column("FieldDateTime", Configuration = ProviderName.ClickHouse, DataType = DataType.DateTime64, Precision = 3)]
			[Column("FieldDateTime")]
			[Column(Configuration = ProviderName.ClickHouse, DataType = DataType.DateTime2, Precision = 3)]
			public DateTime? FieldDateTime;

			[Column(IsColumn = false, Configuration = ProviderName.Sybase)]
			[Column(IsColumn = false, Configuration = ProviderName.DB2)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlServer2005)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlCe)]
			[Column(IsColumn = false, Configuration = ProviderName.Informix)]
			[Column(IsColumn = false, Configuration = ProviderName.Firebird)]
			[Column(IsColumn = false, Configuration = ProviderName.Access)]
			[Column(IsColumn = false, Configuration = ProviderName.MySql)]
			[Column(IsColumn = false, Configuration = ProviderName.SQLite)]
			[Column(IsColumn = false, Configuration = ProviderName.SapHana)]
			[Column(Configuration = ProviderName.Oracle, Precision = 7)]
			[Column("FieldDateTime2")]
			public DateTimeOffset? FieldDateTime2;

			[Column(IsColumn = false, Configuration = ProviderName.Firebird)]
			[Column(IsColumn = false, Configuration = ProviderName.Oracle)]
			[Column(IsColumn = false, Configuration = ProviderName.Informix)] // for some reason it breaks merge
			[Column("FieldBinary")]
			public byte[]? FieldBinary;

			[Column(IsColumn = false, Configuration = ProviderName.Informix)]
			[Column("FieldGuid")]
			public Guid? FieldGuid;

			[Column(IsColumn = false, Configuration = ProviderName.SQLite)]
			[Column("FieldDecimal")]
			public decimal? FieldDecimal;

			[Column(IsColumn = false, Configuration = ProviderName.SqlServer2005)]
			[Column(IsColumn = false, Configuration = ProviderName.Oracle)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlCe)]
			[Column(Configuration = ProviderName.Informix  , DataType = DataType.Date)]
			[Column(Configuration = ProviderName.Sybase    , DataType = DataType.Date)]
			[Column(Configuration = ProviderName.ClickHouse, DataType = DataType.Date)]
			[Column("FieldDate")]
			public DateTime? FieldDate;

			[Column(IsColumn = false, Configuration = ProviderName.Firebird)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlServer2005)]
			[Column(IsColumn = false, Configuration = ProviderName.Oracle)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlCe)]
			[Column(IsColumn = false, Configuration = ProviderName.SQLite)]
			[Column(Configuration = ProviderName.Sybase    , DataType = DataType.Time)]
			[Column(Configuration = ProviderName.ClickHouse, DataType = DataType.Int64)]
			[Column("FieldTime")]
			public TimeSpan? FieldTime;

			[Column("FieldEnumString")]
			public StringEnum? FieldEnumString;

			[Column("FieldEnumNumber")]
			public NumberEnum? FieldEnumNumber;
		}

		public enum StringEnum
		{
			[MapValue("FIRST")]
			Value1,
			[MapValue("\b", Configuration = ProviderName.Informix)]
			[MapValue("\b", Configuration = ProviderName.PostgreSQL)]
			[MapValue("\b", Configuration = ProviderName.SqlCe)]
			[MapValue("\b", Configuration = ProviderName.Sybase)]
			[MapValue("\b", Configuration = ProviderName.SapHana)]
			[MapValue("\b", Configuration = ProviderName.DB2)]
			[MapValue("\b", Configuration = ProviderName.OracleDevart)]
			[MapValue("\b", Configuration = ProviderName.Oracle11Devart)]
			[MapValue("\0")]
			Value2,
			[MapValue("_", Configuration = ProviderName.Oracle)]
			[MapValue("_", Configuration = ProviderName.Sybase)]
			[MapValue("")]
			Value3,
			[MapValue(null)]
			Value4
		}

		public enum NumberEnum
		{
			[MapValue(int.MinValue + 1)]
			Value1,
			[MapValue(int.MaxValue)]
			Value2,
			[MapValue(0)]
			Value3,
			[MapValue(null)]
			Value4
		}

		private static ITable<MergeTypes> GetTypes1(IDataContext db)
		{
			return db.GetTable<MergeTypes>().TableName("TestMerge1");
		}

		private static ITable<MergeTypes> GetTypes2(IDataContext db)
		{
			return db.GetTable<MergeTypes>().TableName("TestMerge2");
		}

		private void PrepareTypesData(IDataContext db)
		{
			//using (new DisableLogging())
			{
				GetTypes1(db).Delete();
				GetTypes2(db).Delete();

				foreach (var record in InitialTypes1Data)
				{
					db.Insert(record, "TestMerge1");
				}

				foreach (var record in InitialTypes2Data)
				{
					db.Insert(record, "TestMerge2");
				}
			}
		}

		private static readonly MergeTypes[] InitialTypes1Data = new[]
		{
			new MergeTypes()
			{
				Id              = 1,
			},
			new MergeTypes()
			{
				Id              = 2,
				FieldInt32      = int.MinValue + 1,
				FieldInt64      = long.MinValue + 1,
				FieldBoolean    = true,
				FieldString     = "normal strinG",
				FieldNString    = "всЁ нормально",
				FieldChar       = '*',
				FieldNChar      = 'ё',
				FieldFloat      = -3.40282002E+38f, //float.MinValue,
				FieldDouble     = double.MinValue,
				FieldDateTime   = new DateTime(2000, 11, 12, 21, 14, 15, 167),
				FieldDateTime2  = new DateTimeOffset(2000, 11, 22, 13, 14, 15, 1, TimeSpan.FromMinutes(15)).AddTicks(1234567),
				FieldBinary     = Array.Empty<byte>(),
				FieldGuid       = Guid.Empty,
				FieldDecimal    = 12345678.9012345678M,
				FieldDate       = new DateTime(2000, 11, 23),
				FieldTime       = new TimeSpan(0, 9, 44, 33, 888).Add(TimeSpan.FromTicks(7654321)),
				FieldEnumString = StringEnum.Value1,
				FieldEnumNumber = NumberEnum.Value4
			},
			new MergeTypes()
			{
				Id              = 3,
				FieldInt32      = int.MaxValue,
				FieldInt64      = long.MaxValue,
				FieldBoolean    = false,
				FieldString     = "test\r\n\v\b\t\f",
				FieldNString    = "ЙЦУКЩывапрм\r\nq",
				FieldChar       = '&',
				FieldNChar      = '>',
				FieldFloat      = 3.40282002E+38f, //float.MaxValue,
				FieldDouble     = double.MaxValue,
				FieldDateTime   = new DateTime(2001, 10, 12, 21, 14, 15, 167),
				FieldDateTime2  = new DateTimeOffset(2001, 11, 22, 13, 14, 15, 0, TimeSpan.FromMinutes(-15)).AddTicks(1234567),
				FieldBinary     = new byte[] { 0, 1, 2, 3, 0, 4 },
				FieldGuid       = new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"),
				FieldDecimal    = -99999999.9999999999M,
				FieldDate       = new DateTime(2123, 11, 23),
				FieldTime       = new TimeSpan(0, 0, 44, 33, 876).Add(TimeSpan.FromTicks(7654321)),
				FieldEnumString = StringEnum.Value2,
				FieldEnumNumber = NumberEnum.Value3
			},
			new MergeTypes()
			{
				Id              = 4,
				FieldInt32      = -123,
				FieldInt64      = 987,
				FieldBoolean    = null,
				FieldString     = "`~!@#$%^&*()_+{}|[]\\",
				FieldNString    = "<>?/.,;'щЩ\":",
				FieldChar       = '\r',
				FieldNChar      = '\n',
				FieldFloat      = 1.1755e-38f, //float.Epsilon,
				FieldDouble     = -2.2250738585072014e-308d, //-double.Epsilon,
				FieldDateTime   = new DateTime(2098, 10, 12, 21, 14, 15, 997),
				FieldDateTime2  = new DateTimeOffset(2001, 11, 22, 13, 14, 15, 999, TimeSpan.FromMinutes(99)).AddTicks(1234567),
				FieldBinary     = new byte[] { 255, 200, 100, 50, 20, 0 },
				FieldGuid       = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
				FieldDecimal    = 99999999.9999999999M,
				FieldDate       = new DateTime(2110, 11, 23),
				FieldTime       = TimeSpan.Zero,
				FieldEnumString = StringEnum.Value3,
				FieldEnumNumber = NumberEnum.Value2
			}
		};

		private static readonly MergeTypes[] InitialTypes2Data = new[]
		{
			new MergeTypes()
			{
				Id              = 3,
				FieldInt32      = -123,
				FieldInt64      = 987,
				FieldBoolean    = null,
				FieldString     = "<>?/.,;'zZ\":",
				FieldNString    = "`~!@#$%^&*()_+{}|[]\\",
				FieldChar       = '\f',
				FieldNChar      = '\v',
				FieldFloat      = -1.1755e-38f, //-float.Epsilon,
				FieldDouble     = 2.2250738585072014e-308d, //double.Epsilon,
				FieldDateTime   = new DateTime(2098, 10, 12, 21, 14, 15, 907),
				FieldDateTime2  = new DateTimeOffset(2001, 11, 22, 13, 14, 15, 111, TimeSpan.FromMinutes(-99)).AddTicks(-9876543),
				FieldBinary     = new byte[] { 255, 200, 100, 50, 20, 0 },
				FieldGuid       = new Guid("ffffffff-ffff-ffff-FFFF-ffffffffffff"),
				FieldDecimal    = -0.123M,
				FieldDate       = new DateTime(2111, 11, 23),
				FieldTime       = TimeSpan.FromHours(24).Add(TimeSpan.FromTicks(-1)),
				FieldEnumString = StringEnum.Value4,
				FieldEnumNumber = NumberEnum.Value1
			},
			new MergeTypes()
			{
				Id              = 4,
				FieldInt32      = int.MaxValue,
				FieldInt64      = long.MaxValue,
				FieldBoolean    = false,
				FieldString     = "test\r\n\v\b\t",
				FieldNString    = "ЙЦУКЩывапрм\r\nq",
				FieldChar       = '1',
				FieldNChar      = ' ',
				FieldFloat      = 3.40282002E+38f, //float.MaxValue,
				FieldDouble     = double.MaxValue,
				FieldDateTime   = new DateTime(2001, 10, 12, 21, 14, 15, 167),
				FieldDateTime2  = new DateTimeOffset(2001, 11, 22, 13, 14, 15, 321, TimeSpan.FromMinutes(-15)),
				FieldBinary     = new byte[] { 0, 1, 2, 3, 0, 4 },
				FieldGuid       = new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"),
				FieldDecimal    = -99999999.9999999999M,
				FieldDate       = new DateTime(2123, 11, 23),
				FieldTime       = new TimeSpan(0, 14, 44, 33, 234),
				FieldEnumString = StringEnum.Value2,
				FieldEnumNumber = NumberEnum.Value3
			},
			new MergeTypes()
			{
				Id              = 5,
				FieldInt32      = -123,
				FieldInt64      = 987,
				FieldBoolean    = null,
				FieldString     = "<>?/.,;'zZ\":",
				FieldNString    = "`~!@#$%^&*()_+{}|[]\\",
				FieldChar       = ' ',
				FieldNChar      = ' ',
				FieldFloat      = -1.1755e-38f, //-float.Epsilon,
				FieldDouble     = 2.2250738585072014e-308d, //double.Epsilon,
				FieldDateTime   = new DateTime(2098, 10, 12, 21, 14, 15, 913),
				FieldDateTime2  = new DateTimeOffset(2001, 11, 22, 13, 14, 15, 0, TimeSpan.FromMinutes(-99)),
				FieldBinary     = new byte[] { 255, 200, 100, 50, 20, 0 },
				FieldGuid       = new Guid("ffffffff-ffff-ffff-FFFF-ffffffffffff"),
				FieldDecimal    = -0.123M,
				FieldDate       = new DateTime(2010, 11, 23),
				FieldTime       = TimeSpan.FromHours(24).Add(TimeSpan.FromTicks(-1)),
				FieldEnumString = StringEnum.Value4,
				FieldEnumNumber = NumberEnum.Value1
			},
			new MergeTypes()
			{
				Id              = 6,
				FieldInt32      = int.MaxValue,
				FieldInt64      = long.MaxValue,
				FieldBoolean    = false,
				FieldString     = "test\r\n\v\b\t \r ",
				FieldNString    = "ЙЦУКЩывапрм\r\nq \r ",
				FieldChar       = '-',
				FieldNChar      = '~',
				FieldFloat      = 3.40282002E+38f, //float.MaxValue,
				FieldDouble     = double.MaxValue,
				FieldDateTime   = new DateTime(2001, 10, 12, 21, 14, 15, 167),
				FieldDateTime2  = new DateTimeOffset(2001, 11, 22, 13, 14, 15, 999, TimeSpan.FromMinutes(-15)),
				FieldBinary     = new byte[] { 0, 1, 2, 3, 0, 4 },
				FieldGuid       = new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"),
				FieldDecimal    = -99999999.9999999999M,
				FieldDate       = new DateTime(2123, 11, 23),
				FieldTime       = new TimeSpan(0, 22, 44, 33, 0),
				FieldEnumString = StringEnum.Value2,
				FieldEnumNumber = NumberEnum.Value3
			}
		};

		[ActiveIssue(Configurations = new[] { TestProvName.Oracle21DevartDirect })]
		[Test]
		public void TestMergeTypes([DataSources(true)] string context)
		{
			var isIDS = IsIDSProvider(context);

			using (var db = GetDataContext(context))
			{
				PrepareTypesData(db);

				var result1 = GetTypes1(db).OrderBy(_ => _.Id).ToList();
				var result2 = GetTypes2(db).OrderBy(_ => _.Id).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result1, Has.Count.EqualTo(InitialTypes1Data.Length));
					Assert.That(result2, Has.Count.EqualTo(InitialTypes2Data.Length));
				}

				var provider = GetProviderName(context, out var _);
				for (var i = 0; i < InitialTypes1Data.Length; i++)
				{
					AssertTypesRow(InitialTypes1Data[i], result1[i], provider, isIDS);
				}

				for (var i = 0; i < InitialTypes2Data.Length; i++)
				{
					AssertTypesRow(InitialTypes2Data[i], result2[i], provider, isIDS);
				}
			}
		}

		private void AssertTypesRow(MergeTypes expected, MergeTypes actual, string provider, bool isIDS)
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(actual.Id, Is.EqualTo(expected.Id));
				Assert.That(actual.FieldInt32, Is.EqualTo(expected.FieldInt32));
			}

			if (!provider.IsAnyOf(TestProvName.AllAccess))
				Assert.That(actual.FieldInt64, Is.EqualTo(expected.FieldInt64));

			if (!provider.IsAnyOf(TestProvName.AllSybase))
				if (!provider.IsAnyOf(TestProvName.AllAccess))
					Assert.That(actual.FieldBoolean, Is.EqualTo(expected.FieldBoolean));
				else
					Assert.That(actual.FieldBoolean, Is.EqualTo(expected.FieldBoolean ?? false));

			AssertString(expected.FieldString, actual.FieldString, provider, isIDS);
			AssertNString(expected.FieldNString, actual.FieldNString, provider);

			AssertChar(expected.FieldChar, actual.FieldChar, provider);

			AssertNChar(expected.FieldChar, actual.FieldChar, provider);

			Assert.That(actual.FieldFloat, Is.EqualTo(expected.FieldFloat));

			if (!provider.IsAnyOf(TestProvName.AllFirebird))
				Assert.That(actual.FieldDouble, Is.EqualTo(expected.FieldDouble));

			AssertDateTime(expected.FieldDateTime, actual.FieldDateTime, provider);

			AssertDateTimeOffset(expected.FieldDateTime2, actual.FieldDateTime2, provider);

			AssertBinary(expected.FieldBinary, actual.FieldBinary, provider);

			if (!provider.IsAnyOf(TestProvName.AllInformix))
				Assert.That(actual.FieldGuid, Is.EqualTo(expected.FieldGuid));

			if (!provider.IsAnyOf(TestProvName.AllSQLite))
				Assert.That(actual.FieldDecimal, Is.EqualTo(expected.FieldDecimal));

			if (   !provider.IsAnyOf(TestProvName.AllSqlServer2005)
				&& provider != ProviderName.SqlCe
				&& !provider.IsAnyOf(TestProvName.AllOracle))
				Assert.That(actual.FieldDate, Is.EqualTo(expected.FieldDate));

			AssertTime(expected.FieldTime, actual.FieldTime, provider);

			if (expected.FieldEnumString == StringEnum.Value4)
				Assert.That(actual.FieldEnumString, Is.Null);
			else
				Assert.That(actual.FieldEnumString, Is.EqualTo(expected.FieldEnumString));

			if (expected.FieldEnumNumber == NumberEnum.Value4)
				Assert.That(actual.FieldEnumNumber, Is.Null);
			else
				Assert.That(actual.FieldEnumNumber, Is.EqualTo(expected.FieldEnumNumber));
		}

		private static void AssertNString(string? expected, string? actual, string provider)
		{
			if (expected != null)
			{
				if (provider.IsAnyOf(TestProvName.AllSybase))
					expected = expected.TrimEnd(' ');
			}

			if (!provider.IsAnyOf(TestProvName.AllInformix))
				Assert.That(actual, Is.EqualTo(expected));
		}

		private static void AssertBinary(byte[]? expected, byte[]? actual, string provider)
		{
			if (provider.IsAnyOf(TestProvName.AllInformix)
				|| provider.IsAnyOf(TestProvName.AllOracle)
				|| provider.IsAnyOf(TestProvName.AllFirebird))
				return;

			if (expected != null)
			{
				if (provider.IsAnyOf(TestProvName.AllSybase))
				{
					while (expected.Length > 1 && expected[expected.Length - 1] == 0)
						expected = expected.Take(expected.Length - 1).ToArray();

					 if (expected.Length == 0)
						expected = new byte[] { 0 };
				}

				if (provider.IsAnyOf(ProviderName.ClickHouseMySql, ProviderName.ClickHouseDriver))
				{
					// https://github.com/DarkWanderer/ClickHouse.Driver/issues/138
					// https://github.com/ClickHouse/ClickHouse/issues/38790
					expected = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(expected));
				}
			}

			Assert.That(actual, Is.EqualTo(expected));
		}

		private static void AssertDateTimeOffset(DateTimeOffset? expected, DateTimeOffset? actual, string provider)
		{
			if (expected != null)
			{
				if (provider.IsAnyOf(TestProvName.AllPostgreSQL, ProviderName.ClickHouseMySql))
					expected = expected.Value.AddTicks(-expected.Value.Ticks % 10);
			}

			if (   !provider.IsAnyOf(TestProvName.AllSqlServer2005)
				&& !provider.IsAnyOf(ProviderName.SqlCe)
				&& !provider.IsAnyOf(TestProvName.AllInformix)
				&& !provider.IsAnyOf(TestProvName.AllFirebird)
				&& !provider.IsAnyOf(TestProvName.AllMySql)
				&& !provider.IsAnyOf(TestProvName.AllAccess)
				&& !provider.IsAnyOf(TestProvName.AllSQLite)
				&& !provider.IsAnyOf(TestProvName.AllSybase)
				&& !provider.IsAnyOf(TestProvName.AllSapHana)
				&& !provider.IsAnyOf(ProviderName.DB2))
				Assert.That(actual, Is.EqualTo(expected));
		}

		private static void AssertChar(char? expected, char? actual, string provider)
		{
			if (expected != null)
			{
				if (expected == ' '
					&& (   provider.IsAnyOf(TestProvName.AllMySql)
						// after migration to 2.4.126 provider + SPS4, hana or provider started to trim spaces on insert for some reason
						|| provider.IsAnyOf(TestProvName.AllSapHana)))
					expected = '\0';
			}

			Assert.That(actual, Is.EqualTo(expected));
		}

		private static void AssertNChar(char? expected, char? actual, string provider)
		{
			if (expected != null)
			{
				if (expected == ' '
					&& (provider.IsAnyOf(TestProvName.AllMySql)
						// after migration to 2.4.126 provider + SPS4, hana or provider started to trim spaces on insert for some reason
						|| provider.IsAnyOf(TestProvName.AllSapHana)))
					expected = '\0';
			}

			Assert.That(actual, Is.EqualTo(expected));
		}

		private static void AssertDateTime(DateTime? expected, DateTime? actual, string provider)
		{
			if (expected != null)
			{
				if (provider.IsAnyOf(TestProvName.AllMySql))
				{
					if (expected.Value.Ticks % 10 >= 5)
						expected = expected.Value.AddTicks(10);
					expected = expected.Value.AddTicks(-expected.Value.Ticks % 10);
				}
				else if (provider.IsAnyOf(TestProvName.AllSybase))
				{
					switch (expected.Value.Millisecond % 10)
					{
						case 1:
						case 4:
						case 7:
							expected = expected.Value.AddMilliseconds(-1);
							break;
						case 2:
						case 5:
						case 9:
							expected = expected.Value.AddMilliseconds(1);
							break;
						case 8:
							expected = expected.Value.AddMilliseconds(-2);
							break;
					}
				}

				if (   provider.IsAnyOf(TestProvName.AllOracle)
					|| provider.IsAnyOf(TestProvName.AllAccessOdbc))
					expected = expected.Value.AddMilliseconds(-expected.Value.Millisecond);
			}

			Assert.That(actual, Is.EqualTo(expected));
		}

		private static void AssertString(string? expected, string? actual, string provider, bool isIDS)
		{
			if (expected != null)
			{
				switch (provider)
				{
					case string when provider.IsAnyOf(TestProvName.AllSybase):
						expected = expected.TrimEnd(' ');
						break;
					case ProviderName.Informix:
						expected = isIDS ? expected : expected.TrimEnd('\t', ' ');
						break;
				}
			}
			
			Assert.That(actual, Is.EqualTo(expected));
		}

		private static void AssertTime(TimeSpan? expected, TimeSpan? actual, string provider)
		{
			if (   provider.IsAnyOf(TestProvName.AllSqlServer2005)
				|| provider.IsAnyOf(TestProvName.AllOracle)
				|| provider.IsAnyOf(TestProvName.AllSQLite)
				|| provider.IsAnyOf(TestProvName.AllFirebird)
				|| provider == ProviderName.SqlCe)
				return;

			if (expected != null)
			{
				switch (provider)
				{
					case string when provider.IsAnyOf(TestProvName.AllSybase):
					{
						expected = TimeSpan.FromTicks((expected.Value.Ticks / 10000) * 10000);
						switch (expected.Value.Milliseconds % 10)
						{
							case 1:
							case 4:
							case 7:
								expected = expected.Value.Add(TimeSpan.FromMilliseconds(-1));
								break;
							case 2:
							case 5:
							case 9:
								expected = expected.Value.Add(TimeSpan.FromMilliseconds(1));
								break;
							case 8:
								expected = expected.Value.Add(TimeSpan.FromMilliseconds(2));
								break;
						}

						if (expected == TimeSpan.FromDays(1))
							expected = expected.Value.Add(TimeSpan.FromMilliseconds(-4));
						break;
					};
					case string when provider.IsAnyOf(TestProvName.AllFirebird):
						expected = TimeSpan.FromTicks((expected.Value.Ticks / 1000) * 1000);
						break;
					case string when provider.IsAnyOf(TestProvName.AllInformix):
						expected = TimeSpan.FromTicks((expected.Value.Ticks / 100) * 100);
						break;
					case string when provider.IsAnyOf(TestProvName.AllPostgreSQL, TestProvName.AllMariaDB):
						expected = TimeSpan.FromTicks((expected.Value.Ticks / 10) * 10);
						break;
					case string when provider.IsAnyOf(ProviderName.DB2, TestProvName.AllAccess, TestProvName.AllSapHana):
						expected = TimeSpan.FromTicks((expected.Value.Ticks / 10000000) * 10000000);
						break;
					case string when provider.IsAnyOf(TestProvName.AllMySqlServer):
						// TIME doesn't support fractional seconds and value is rounded
						//
						// round
						if ((expected.Value.Ticks / 1_000_000) % 10 >= 5)
							expected = expected.Value.Add(TimeSpan.FromSeconds(1));
						// trim fraction
						expected = TimeSpan.FromTicks((expected.Value.Ticks / 10_000_000) * 10_000_000);
						break;
				}
			}

			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test]
		public void TestTypesInsertByMerge([MergeDataContextSource(
			TestProvName.AllInformix, TestProvName.AllSybase)]
			string context)
		{
			var isIDS = IsIDSProvider(context);

			using (var db = GetDataContext(context))
			{
				using (new DisableLogging())
				{
					GetTypes1(db).Delete();
					GetTypes2(db).Delete();
				}

				GetTypes1(db).Merge().Using(InitialTypes1Data).OnTargetKey().InsertWhenNotMatched().Merge();
				GetTypes2(db).Merge().Using(InitialTypes2Data).OnTargetKey().InsertWhenNotMatched().Merge();

				var result1 = GetTypes1(db).OrderBy(_ => _.Id).ToList();
				var result2 = GetTypes2(db).OrderBy(_ => _.Id).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result1, Has.Count.EqualTo(InitialTypes1Data.Length));
					Assert.That(result2, Has.Count.EqualTo(InitialTypes2Data.Length));
				}

				var provider = GetProviderName(context, out var _);
				for (var i = 0; i < InitialTypes1Data.Length; i++)
				{
					AssertTypesRow(InitialTypes1Data[i], result1[i], provider, isIDS);
				}

				for (var i = 0; i < InitialTypes2Data.Length; i++)
				{
					AssertTypesRow(InitialTypes2Data[i], result2[i], provider, isIDS);
				}
			}
		}
	}
}
