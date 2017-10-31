using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using Model;

	public partial class MergeTests : TestBase
	{
		[Table("unspecified")]
		class MergeTypes
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
			public string FieldString;

			[Column(IsColumn = false, Configuration = ProviderName.Informix)]
			[Column("FieldNString")]
			public string FieldNString;

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
			[Column("FieldDateTime")]
			public DateTime? FieldDateTime;

			[Column(IsColumn = false, Configuration = ProviderName.Sybase)]
			[Column(IsColumn = false, Configuration = ProviderName.DB2)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlServer2000)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlServer2005)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlCe)]
			[Column(IsColumn = false, Configuration = ProviderName.Informix)]
			[Column(IsColumn = false, Configuration = ProviderName.Firebird)]
			[Column(IsColumn = false, Configuration = ProviderName.Access)]
			[Column(IsColumn = false, Configuration = ProviderName.MySql)]
			[Column(IsColumn = false, Configuration = ProviderName.SQLite)]
			[Column(IsColumn = false, Configuration = ProviderName.SapHana)]
			[Column("FieldDateTime2")]
			public DateTimeOffset? FieldDateTime2;

			[Column(IsColumn = false, Configuration = ProviderName.Firebird)]
			[Column(IsColumn = false, Configuration = ProviderName.Oracle)]
			[Column(IsColumn = false, Configuration = ProviderName.OracleManaged)]
			[Column(IsColumn = false, Configuration = ProviderName.OracleNative)]
			[Column(IsColumn = false, Configuration = ProviderName.Informix)] // for some reason it breaks merge
			[Column("FieldBinary")]
			public byte[] FieldBinary;

			[Column(IsColumn = false, Configuration = ProviderName.Informix)]
			[Column("FieldGuid")]
			public Guid? FieldGuid;

			[Column(IsColumn = false, Configuration = ProviderName.SQLite)]
			[Column("FieldDecimal")]
			public decimal? FieldDecimal;

			[Column(IsColumn = false, Configuration = ProviderName.SqlServer2000)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlServer2005)]
			[Column(IsColumn = false, Configuration = ProviderName.Oracle)]
			[Column(IsColumn = false, Configuration = ProviderName.OracleManaged)]
			[Column(IsColumn = false, Configuration = ProviderName.OracleNative)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlCe)]
			[Column("FieldDate"     , Configuration = ProviderName.Informix, DataType = DataType.Date)]
			[Column("FieldDate"     , Configuration = ProviderName.Sybase  , DataType = DataType.Date)]
			[Column("FieldDate")]
			public DateTime? FieldDate;

			[Column(IsColumn = false, Configuration = ProviderName.Firebird)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlServer2000)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlServer2005)]
			[Column(IsColumn = false, Configuration = ProviderName.MySql)]
			[Column(IsColumn = false, Configuration = ProviderName.Oracle)]
			[Column(IsColumn = false, Configuration = ProviderName.OracleManaged)]
			[Column(IsColumn = false, Configuration = ProviderName.OracleNative)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlCe)]
			[Column(IsColumn = false, Configuration = ProviderName.SQLite)]
			[Column("FieldTime"     , Configuration = ProviderName.Sybase, DataType = DataType.Time)]
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
			[MapValue("\0")]
			Value2,
			[MapValue("_", Configuration = ProviderName.Oracle)]
			[MapValue("_", Configuration = ProviderName.OracleManaged)]
			[MapValue("_", Configuration = ProviderName.OracleNative)]
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
				FieldBinary     = new byte[0],
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
				FieldDate       = new DateTime(3210, 11, 23),
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
				FieldDate       = new DateTime(3210, 11, 23),
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
				FieldDate       = new DateTime(3210, 11, 23),
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

		// TODO: check how it is possible as we don't even save 4 to this column
		//  Failed : Tests.Merge.MergeTests.TestMergeTypes("SQLiteMs")
		// Expected: '*'
		// But was:  '4'
		// at Tests.Merge.MergeTests.AssertChar
		[Test, DataContextSource(false, ProviderName.SQLiteMS)]
		public void TestMergeTypes(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareTypesData(db);

				var result1 = GetTypes1(db).OrderBy(_ => _.Id).ToList();
				var result2 = GetTypes2(db).OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(InitialTypes1Data.Length, result1.Count);
				Assert.AreEqual(InitialTypes2Data.Length, result2.Count);

				for (var i = 0; i < InitialTypes1Data.Length; i++)
				{
					AssertTypesRow(InitialTypes1Data[i], result1[i], context);
				}

				for (var i = 0; i < InitialTypes2Data.Length; i++)
				{
					AssertTypesRow(InitialTypes2Data[i], result2[i], context);
				}
			}
		}

		private void AssertTypesRow(MergeTypes expected, MergeTypes actual, string context)
		{
			Assert.AreEqual(expected.Id, actual.Id);
			Assert.AreEqual(expected.FieldInt32, actual.FieldInt32);

			if (context != ProviderName.Access)
				Assert.AreEqual(expected.FieldInt64, actual.FieldInt64);

			if (context != ProviderName.Sybase)
				if (context != ProviderName.Access)
					Assert.AreEqual(expected.FieldBoolean, actual.FieldBoolean);
				else
					Assert.AreEqual(expected.FieldBoolean ?? false, actual.FieldBoolean);

			AssertString(expected.FieldString, actual.FieldString, context);
			AssertNString(expected.FieldNString, actual.FieldNString, context);

			AssertChar(expected.FieldChar, actual.FieldChar, context);

			AssertNChar(expected.FieldChar, actual.FieldChar, context);

			Assert.AreEqual(expected.FieldFloat, actual.FieldFloat);

			if (context != ProviderName.Firebird
				&& context != TestProvName.Firebird3)
				Assert.AreEqual(expected.FieldDouble, actual.FieldDouble);

			AssertDateTime(expected.FieldDateTime, actual.FieldDateTime, context);

			AssertDateTimeOffset(expected.FieldDateTime2, actual.FieldDateTime2, context);

			AssertBinary(expected.FieldBinary, actual.FieldBinary, context);

			if (context != ProviderName.Informix)
				Assert.AreEqual(expected.FieldGuid, actual.FieldGuid);

			if (context != ProviderName.SQLiteClassic && context != ProviderName.SQLiteMS)
				Assert.AreEqual(expected.FieldDecimal, actual.FieldDecimal);

			if (context != ProviderName.SqlServer2000
				&& context != ProviderName.SqlServer2005
				&& context != ProviderName.SqlCe
				&& context != ProviderName.Oracle
				&& context != ProviderName.OracleManaged
				&& context != ProviderName.OracleNative)
				Assert.AreEqual(expected.FieldDate, actual.FieldDate);

			AssertTime(expected.FieldTime, actual.FieldTime, context);

			if (expected.FieldEnumString == StringEnum.Value4)
				Assert.IsNull(actual.FieldEnumString);
			else
				Assert.AreEqual(expected.FieldEnumString, actual.FieldEnumString);

			if (expected.FieldEnumNumber == NumberEnum.Value4)
				Assert.IsNull(actual.FieldEnumNumber);
			else
				Assert.AreEqual(expected.FieldEnumNumber, actual.FieldEnumNumber);
		}

		private static void AssertNString(string expected, string actual, string context)
		{
			if (expected != null)
			{
				if (context == ProviderName.Sybase)
					expected = expected.TrimEnd(' ');
			}

			if (context != ProviderName.Informix)
				Assert.AreEqual(expected, actual);
		}

		private static void AssertBinary(byte[] expected, byte[] actual, string context)
		{
			if (context == ProviderName.Informix
				|| context == ProviderName.Oracle
				|| context == ProviderName.OracleManaged
				|| context == ProviderName.OracleNative
				|| context == ProviderName.Firebird
				|| context == TestProvName.Firebird3)
				return;

			if (expected != null)
			{
				if (context == ProviderName.Sybase)
				{
					while (expected.Length > 1 && expected[expected.Length - 1] == 0)
						expected = expected.Take(expected.Length - 1).ToArray();

					 if (expected.Length == 0)
						expected = new byte[] { 0 };
				}
			}

			Assert.AreEqual(expected, actual);
		}

		private static void AssertDateTimeOffset(DateTimeOffset? expected, DateTimeOffset? actual, string context)
		{
			if (expected != null)
			{
				if (context == ProviderName.Oracle
					|| context == ProviderName.OracleManaged
					|| context == ProviderName.OracleNative)
				{
					var trimmable = expected.Value.Ticks % 10;
					if (trimmable >= 5)
						trimmable -= 10;

					expected = expected.Value.AddTicks(-trimmable);
				}

				if (context == ProviderName.PostgreSQL)
					expected = expected.Value.AddTicks(-expected.Value.Ticks % 10);
			}

			if (   context != ProviderName.SqlServer2000
				&& context != ProviderName.SqlServer2005
				&& context != ProviderName.SqlCe
				&& context != ProviderName.Informix
				&& context != ProviderName.Firebird
				&& context != TestProvName.Firebird3
				&& context != ProviderName.MySql
				&& context != TestProvName.MySql57
				&& context != TestProvName.MariaDB
				&& context != ProviderName.Access
				&& context != ProviderName.SQLiteClassic
				&& context != ProviderName.SQLiteMS
				&& context != ProviderName.Sybase
				&& context != ProviderName.DB2
				&& context != ProviderName.SapHana)
				Assert.AreEqual(expected, actual);
		}

		private static void AssertChar(char? expected, char? actual, string context)
		{
			if (expected != null)
			{
				if (expected == ' '
					&& (   context == ProviderName.MySql
						|| context == TestProvName.MariaDB
						|| context == TestProvName.MySql57))
					expected = '\0';
			}

			Assert.AreEqual(expected, actual);
		}

		private static void AssertNChar(char? expected, char? actual, string context)
		{
			if (expected != null)
			{
				if (expected == ' '
					&& (context == ProviderName.MySql
						|| context == TestProvName.MariaDB
						|| context == TestProvName.MySql57))
					expected = '\0';
			}

			Assert.AreEqual(expected, actual);
		}

		private static void AssertDateTime(DateTime? expected, DateTime? actual, string context)
		{
			if (expected != null)
			{
				if (context == TestProvName.MySql57 && expected.Value.Millisecond > 500)
					expected = expected.Value.AddSeconds(1);

				if (context == ProviderName.Sybase)
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

				if (   context == ProviderName.MySql
					|| context == TestProvName.MariaDB
					|| context == TestProvName.MySql57
					|| context == ProviderName.Oracle
					|| context == ProviderName.OracleManaged
					|| context == ProviderName.OracleNative)
					expected = expected.Value.AddMilliseconds(-expected.Value.Millisecond);
			}

			Assert.AreEqual(expected, actual);
		}

		private static void AssertString(string expected, string actual, string context)
		{
			if (expected != null)
			{
				switch (context)
				{
					case ProviderName.Sybase:
						expected = expected.TrimEnd(' ');
						break;
					case ProviderName.Informix:
						expected = expected.TrimEnd('\t', ' ');
						break;
				}
			}

			Assert.AreEqual(expected, actual);
		}

		private static void AssertTime(TimeSpan? expected, TimeSpan? actual, string context)
		{
			if (   context == ProviderName.SqlServer2000
				|| context == ProviderName.SqlServer2005
				|| context == ProviderName.Oracle
				|| context == ProviderName.OracleManaged
				|| context == ProviderName.OracleNative
				|| context == ProviderName.SqlCe
				|| context == ProviderName.SQLiteClassic
				|| context == ProviderName.SQLiteMS
				|| context == ProviderName.MySql
				// MySql57 and MariaDB work, but column is disabled...
				|| context == TestProvName.MySql57
				|| context == TestProvName.MariaDB
				|| context == ProviderName.Firebird
				|| context == TestProvName.Firebird3)
				return;

			if (expected != null)
			{
				switch (context)
				{
					case ProviderName.Sybase:
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
					case ProviderName.Firebird:
					case TestProvName.Firebird3:
						expected = TimeSpan.FromTicks((expected.Value.Ticks / 1000) * 1000);
						break;
					case ProviderName.Informix:
						expected = TimeSpan.FromTicks((expected.Value.Ticks / 100) * 100);
						break;
					case ProviderName.PostgreSQL:
						expected = TimeSpan.FromTicks((expected.Value.Ticks / 10) * 10);
						break;
					case ProviderName.DB2:
					case ProviderName.Access:
					case ProviderName.SapHana:
						expected = TimeSpan.FromTicks((expected.Value.Ticks / 10000000) * 10000000);
						break;
				}
			}

			Assert.AreEqual(expected, actual);
		}

		[Test, MergeDataContextSource(ProviderName.Informix, ProviderName.Sybase)]
		public void TestTypesInsertByMerge(string context)
		{
			using (var db = new TestDataConnection(context))
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

				Assert.AreEqual(InitialTypes1Data.Length, result1.Count);
				Assert.AreEqual(InitialTypes2Data.Length, result2.Count);

				for (var i = 0; i < InitialTypes1Data.Length; i++)
				{
					AssertTypesRow(InitialTypes1Data[i], result1[i], context);
				}

				for (var i = 0; i < InitialTypes2Data.Length; i++)
				{
					AssertTypesRow(InitialTypes2Data[i], result2[i], context);
				}
			}
		}
	}
}
