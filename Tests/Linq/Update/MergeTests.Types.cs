using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Tests.Model;

namespace Tests.Merge
{
	public partial class MergeTests
	{
		[Table("unspecified")]
		class MergeTypes
		{
			[Column("Id")]
			[PrimaryKey]
			public int Id;

			[Column("Field1")]
			public int? FieldInt32;

			[Column("FieldInt64")]
			public long? FieldInt64;

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

			[Column("FieldDouble")]
			public double? FieldDouble;

			[Column("FieldDateTime")]
			public DateTime? FieldDateTime;

			[Column(IsColumn = false, Configuration = ProviderName.SqlServer2000)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlServer2005)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlCe)]
			[Column(IsColumn = false, Configuration = ProviderName.Informix)]
			[Column(IsColumn = false, Configuration = ProviderName.Firebird)]
			[Column(IsColumn = false, Configuration = TestProvName.Firebird3)]
			[Column("FieldDateTime2")]
			public DateTimeOffset? FieldDateTime2;

			[Column("FieldBinary")]
			public byte[] FieldBinary;

			[Column(IsColumn = false, Configuration = ProviderName.Informix)]
			[Column("FieldGuid")]
			public Guid? FieldGuid;

			[Column("FieldDecimal")]
			public decimal? FieldDecimal;

			[Column(IsColumn = false, Configuration = ProviderName.SqlServer2000)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlServer2005)]
			[Column("FieldDate")]
			public DateTime? FieldDate;

			[Column(IsColumn = false, Configuration = ProviderName.SqlServer2000)]
			[Column(IsColumn = false, Configuration = ProviderName.SqlServer2005)]
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
			[MapValue("\0")]
			Value2,
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
				FieldFloat      = float.MinValue,
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
				FieldString     = "test\r\n\v\b\t\0",
				FieldNString    = "ЙЦУКЩывапр\0м\r\nq",
				FieldChar       = '\0',
				FieldNChar      = '\0',
				FieldFloat      = float.MaxValue,
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
				FieldFloat      = float.Epsilon,
				FieldDouble     = -double.Epsilon,
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
				FieldFloat      = -float.Epsilon,
				FieldDouble     = double.Epsilon,
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
				FieldString     = "test\r\n\v\b\t\0",
				FieldNString    = "ЙЦУКЩывапр\0м\r\nq",
				FieldChar       = '\0',
				FieldNChar      = '\0',
				FieldFloat      = float.MaxValue,
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
				FieldFloat      = -float.Epsilon,
				FieldDouble     = double.Epsilon,
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
				FieldString     = "test\r\n\v\b\t\0 \r ",
				FieldNString    = "ЙЦУКЩывапр\0м\r\nq \r ",
				FieldChar       = '\0',
				FieldNChar      = '\0',
				FieldFloat      = float.MaxValue,
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

		[DataContextSource(false)]
		public void TestMergeTypes(string context)
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
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
			Assert.AreEqual(expected.FieldInt64, actual.FieldInt64);
			Assert.AreEqual(expected.FieldBoolean, actual.FieldBoolean);

			AssertString(expected.FieldString, actual.FieldString, context);

			if (context != ProviderName.Informix)
				Assert.AreEqual(expected.FieldNString, actual.FieldNString);

			//Assert.AreEqual(expected.FieldChar, actual.FieldChar);
			//Assert.AreEqual(expected.FieldNChar, actual.FieldNChar);
			Assert.AreEqual(expected.FieldFloat, actual.FieldFloat);
			Assert.AreEqual(expected.FieldDouble, actual.FieldDouble);
			Assert.AreEqual(expected.FieldDateTime, actual.FieldDateTime);

			if (context != ProviderName.SqlServer2000
				&& context != ProviderName.SqlServer2005
				&& context != ProviderName.SqlCe
				&& context != ProviderName.Informix
				&& context != ProviderName.Firebird
				&& context != TestProvName.Firebird3)
				Assert.AreEqual(expected.FieldDateTime2, actual.FieldDateTime2);

			Assert.AreEqual(expected.FieldBinary, actual.FieldBinary);

			if (context != ProviderName.Informix)
				Assert.AreEqual(expected.FieldGuid, actual.FieldGuid);

			Assert.AreEqual(expected.FieldDecimal, actual.FieldDecimal);

			if (context != ProviderName.SqlServer2000
				&& context != ProviderName.SqlServer2005)
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

		private static void AssertString(string expected, string actual, string context)
		{
			if (expected != null)
			{
				switch (context)
				{
					case ProviderName.Informix:
						expected = expected.Replace("\t", string.Empty).Replace("\0", string.Empty);
						break;
				}
			}

			Assert.AreEqual(expected, actual);
		}

		private static void AssertTime(TimeSpan? expected, TimeSpan? actual, string context)
		{
			if (   context == ProviderName.SqlServer2000
				|| context == ProviderName.SqlServer2005)
				return;

			if (expected != null)
			{
				switch (context)
				{
					case ProviderName.Firebird:
					case TestProvName.Firebird3:
						expected = TimeSpan.FromTicks((expected.Value.Ticks / 1000) * 1000);
						break;
					case ProviderName.Informix:
						expected = TimeSpan.FromTicks((expected.Value.Ticks / 100) * 100);
						break;
				}
			}

			Assert.AreEqual(expected, actual);
		}
	}
}
