using System;
using System.Numerics;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.DataProvider
{
	// DuckDB type system: https://duckdb.org/docs/sql/data_types/overview
	[TestFixture]
	public sealed class DuckDBTypeTests : TypeTestsBase
	{
		sealed class DuckDBDataSourcesAttribute : IncludeDataSourcesAttribute
		{
			public DuckDBDataSourcesAttribute()
				: base(ProviderName.DuckDB)
			{
			}
		}

		// DuckDB Appender requires exact CLR type match for each column.
		// Cross-type tests (e.g. byte value → SMALLINT column) fail with Appender.
		static readonly Func<BulkCopyType, bool> SkipAppender = bt => bt != BulkCopyType.ProviderSpecific;

		#region Boolean

		[Test]
		public async ValueTask TestBoolean([DuckDBDataSources] string context)
		{
			await TestType<bool, bool?>(context, new(typeof(bool)), default, default);
			await TestType<bool, bool?>(context, new(typeof(bool)), true, false);
			await TestType<bool, bool?>(context, new(typeof(bool)), false, true);
		}

		#endregion

		#region Integer types

		ValueTask TestInteger<TType>(string context, DataType dataType, TType min, TType max, bool? testParameters = null)
			where TType : struct
		{
			return TestInteger(context, new DbDataType(typeof(TType), dataType), max, min, testParameters: testParameters);
		}

		async ValueTask TestInteger<TType>(string context, DbDataType dataType, TType min, TType max, bool? testParameters = null)
			where TType : struct
		{
			await TestType<TType, TType?>(context, dataType, default, default, testParameters: testParameters, testBulkCopyType: SkipAppender);
			await TestType<TType, TType?>(context, dataType, min, max, testParameters: testParameters, testBulkCopyType: SkipAppender);
			await TestType<TType, TType?>(context, dataType, max, min, testParameters: testParameters, testBulkCopyType: SkipAppender);
		}

		[Test]
		public async ValueTask TestTinyInt([DuckDBDataSources] string context)
		{
			// TINYINT (sbyte)
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte)), default, default);
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte)), sbyte.MinValue, sbyte.MaxValue);
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte)), sbyte.MaxValue, sbyte.MinValue);

			// cross-type: unsigned
			await TestInteger<byte>(context, DataType.SByte, 0, 127);
			await TestInteger<ushort>(context, DataType.SByte, 0, 127);
			await TestInteger<uint>(context, DataType.SByte, 0, 127);
			await TestInteger<ulong>(context, DataType.SByte, 0, 127);

			// cross-type: signed
			await TestInteger<short>(context, DataType.SByte, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<int>(context, DataType.SByte, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<long>(context, DataType.SByte, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<decimal>(context, DataType.SByte, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<float>(context, DataType.SByte, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<double>(context, DataType.SByte, sbyte.MinValue, sbyte.MaxValue);
		}

		[Test]
		public async ValueTask TestUTinyInt([DuckDBDataSources] string context)
		{
			// UTINYINT (byte)
			await TestType<byte, byte?>(context, new(typeof(byte)), default, default);
			await TestType<byte, byte?>(context, new(typeof(byte)), byte.MinValue, byte.MaxValue);
			await TestType<byte, byte?>(context, new(typeof(byte)), byte.MaxValue, byte.MinValue);

			// cross-type: unsigned
			await TestInteger<ushort>(context, DataType.Byte, byte.MinValue, byte.MaxValue);
			await TestInteger<uint>(context, DataType.Byte, byte.MinValue, byte.MaxValue);
			await TestInteger<ulong>(context, DataType.Byte, byte.MinValue, byte.MaxValue);

			// cross-type: signed
			await TestInteger<sbyte>(context, DataType.Byte, 0, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.Byte, byte.MinValue, byte.MaxValue);
			await TestInteger<int>(context, DataType.Byte, byte.MinValue, byte.MaxValue);
			await TestInteger<long>(context, DataType.Byte, byte.MinValue, byte.MaxValue);
			await TestInteger<decimal>(context, DataType.Byte, byte.MinValue, byte.MaxValue);
			await TestInteger<float>(context, DataType.Byte, byte.MinValue, byte.MaxValue);
			await TestInteger<double>(context, DataType.Byte, byte.MinValue, byte.MaxValue);
		}

		[Test]
		public async ValueTask TestSmallInt([DuckDBDataSources] string context)
		{
			// SMALLINT (short)
			await TestType<short, short?>(context, new(typeof(short)), default, default);
			await TestType<short, short?>(context, new(typeof(short)), short.MinValue, short.MaxValue);
			await TestType<short, short?>(context, new(typeof(short)), short.MaxValue, short.MinValue);

			// cross-type: unsigned
			await TestInteger<byte>(context, DataType.Int16, 0, byte.MaxValue);
			await TestInteger<ushort>(context, DataType.Int16, 0, (ushort)short.MaxValue);
			await TestInteger<uint>(context, DataType.Int16, 0, (uint)short.MaxValue);
			await TestInteger<ulong>(context, DataType.Int16, 0, (ulong)short.MaxValue);

			// cross-type: signed
			await TestInteger<sbyte>(context, DataType.Int16, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<int>(context, DataType.Int16, short.MinValue, short.MaxValue);
			await TestInteger<long>(context, DataType.Int16, short.MinValue, short.MaxValue);
			await TestInteger<decimal>(context, DataType.Int16, short.MinValue, short.MaxValue);
			await TestInteger<float>(context, DataType.Int16, short.MinValue, short.MaxValue);
			await TestInteger<double>(context, DataType.Int16, short.MinValue, short.MaxValue);
		}

		[Test]
		public async ValueTask TestUSmallInt([DuckDBDataSources] string context)
		{
			// USMALLINT (ushort)
			await TestType<ushort, ushort?>(context, new(typeof(ushort)), default, default);
			await TestType<ushort, ushort?>(context, new(typeof(ushort)), ushort.MinValue, ushort.MaxValue);
			await TestType<ushort, ushort?>(context, new(typeof(ushort)), ushort.MaxValue, ushort.MinValue);

			// cross-type: unsigned
			await TestInteger<byte>(context, DataType.UInt16, byte.MinValue, byte.MaxValue);
			await TestInteger<uint>(context, DataType.UInt16, ushort.MinValue, ushort.MaxValue);
			await TestInteger<ulong>(context, DataType.UInt16, ushort.MinValue, ushort.MaxValue);

			// cross-type: signed
			await TestInteger<sbyte>(context, DataType.UInt16, 0, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.UInt16, 0, short.MaxValue);
			await TestInteger<int>(context, DataType.UInt16, ushort.MinValue, ushort.MaxValue);
			await TestInteger<long>(context, DataType.UInt16, ushort.MinValue, ushort.MaxValue);
			await TestInteger<decimal>(context, DataType.UInt16, ushort.MinValue, ushort.MaxValue);
			await TestInteger<float>(context, DataType.UInt16, ushort.MinValue, ushort.MaxValue);
			await TestInteger<double>(context, DataType.UInt16, ushort.MinValue, ushort.MaxValue);
		}

		[Test]
		public async ValueTask TestInteger([DuckDBDataSources] string context)
		{
			// INTEGER (int)
			await TestType<int, int?>(context, new(typeof(int)), default, default);
			await TestType<int, int?>(context, new(typeof(int)), int.MinValue, int.MaxValue);
			await TestType<int, int?>(context, new(typeof(int)), int.MaxValue, int.MinValue);

			// cross-type: unsigned
			await TestInteger<byte>(context, DataType.Int32, 0, byte.MaxValue);
			await TestInteger<ushort>(context, DataType.Int32, 0, ushort.MaxValue);
			await TestInteger<uint>(context, DataType.Int32, 0, (uint)int.MaxValue);
			await TestInteger<ulong>(context, DataType.Int32, 0, (ulong)int.MaxValue);

			// cross-type: signed
			await TestInteger<sbyte>(context, DataType.Int32, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.Int32, short.MinValue, short.MaxValue);
			await TestInteger<long>(context, DataType.Int32, int.MinValue, int.MaxValue);
			await TestInteger<decimal>(context, DataType.Int32, int.MinValue, int.MaxValue);
			await TestInteger<float>(context, DataType.Int32, 16777216, 16777216);
			await TestInteger<double>(context, DataType.Int32, int.MinValue, int.MaxValue);
		}

		[Test]
		public async ValueTask TestUInteger([DuckDBDataSources] string context)
		{
			// UINTEGER (uint)
			await TestType<uint, uint?>(context, new(typeof(uint)), default, default);
			await TestType<uint, uint?>(context, new(typeof(uint)), uint.MinValue, uint.MaxValue);
			await TestType<uint, uint?>(context, new(typeof(uint)), uint.MaxValue, uint.MinValue);

			// cross-type: unsigned
			await TestInteger<byte>(context, DataType.UInt32, byte.MinValue, byte.MaxValue);
			await TestInteger<ushort>(context, DataType.UInt32, ushort.MinValue, ushort.MaxValue);
			await TestInteger<ulong>(context, DataType.UInt32, uint.MinValue, uint.MaxValue);

			// cross-type: signed
			await TestInteger<sbyte>(context, DataType.UInt32, 0, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.UInt32, 0, short.MaxValue);
			await TestInteger<int>(context, DataType.UInt32, 0, int.MaxValue);
			await TestInteger<long>(context, DataType.UInt32, uint.MinValue, uint.MaxValue);
			await TestInteger<decimal>(context, DataType.UInt32, uint.MinValue, uint.MaxValue);
			await TestInteger<float>(context, DataType.UInt32, uint.MinValue, 16777216u);
			await TestInteger<double>(context, DataType.UInt32, uint.MinValue, uint.MaxValue);
		}

		[Test]
		public async ValueTask TestBigInt([DuckDBDataSources] string context)
		{
			// BIGINT (long)
			await TestType<long, long?>(context, new(typeof(long)), default, default);
			await TestType<long, long?>(context, new(typeof(long)), long.MinValue, long.MaxValue);
			await TestType<long, long?>(context, new(typeof(long)), long.MaxValue, long.MinValue);

			// cross-type: unsigned
			await TestInteger<byte>(context, DataType.Int64, 0, byte.MaxValue);
			await TestInteger<ushort>(context, DataType.Int64, 0, ushort.MaxValue);
			await TestInteger<uint>(context, DataType.Int64, 0, uint.MaxValue);
			await TestInteger<ulong>(context, DataType.Int64, 0, (ulong)long.MaxValue);

			// cross-type: signed
			await TestInteger<sbyte>(context, DataType.Int64, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.Int64, short.MinValue, short.MaxValue);
			await TestInteger<int>(context, DataType.Int64, int.MinValue, int.MaxValue);
			await TestInteger<decimal>(context, DataType.Int64, long.MinValue, long.MaxValue);
			await TestInteger<float>(context, DataType.Int64, -16777216L, 16777216L);
			await TestInteger<double>(context, DataType.Int64, -9007199254740991L, 9007199254740991L);
		}

		[Test]
		public async ValueTask TestUBigInt([DuckDBDataSources] string context)
		{
			// UBIGINT (ulong)
			await TestType<ulong, ulong?>(context, new(typeof(ulong)), default, default);
			await TestType<ulong, ulong?>(context, new(typeof(ulong)), ulong.MinValue, ulong.MaxValue);
			await TestType<ulong, ulong?>(context, new(typeof(ulong)), ulong.MaxValue, ulong.MinValue);

			// cross-type: unsigned
			await TestInteger<byte>(context, DataType.UInt64, byte.MinValue, byte.MaxValue);
			await TestInteger<ushort>(context, DataType.UInt64, ushort.MinValue, ushort.MaxValue);
			await TestInteger<uint>(context, DataType.UInt64, uint.MinValue, uint.MaxValue);

			// cross-type: signed
			await TestInteger<sbyte>(context, DataType.UInt64, 0, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.UInt64, 0, short.MaxValue);
			await TestInteger<int>(context, DataType.UInt64, 0, int.MaxValue);
			await TestInteger<long>(context, DataType.UInt64, 0, long.MaxValue);
			await TestInteger<decimal>(context, DataType.UInt64, ulong.MinValue, ulong.MaxValue);
			await TestInteger<float>(context, DataType.UInt64, ulong.MinValue, 16777216UL);
			await TestInteger<double>(context, DataType.UInt64, ulong.MinValue, 9007199254740991UL);
		}

		[Test]
		public async ValueTask TestHugeInt([DuckDBDataSources] string context)
		{
			// HUGEINT (BigInteger via VarNumeric)
			// DuckDB HUGEINT range: -170141183460469231731687303715884105727 to 170141183460469231731687303715884105727
			var min = BigInteger.Parse("-170141183460469231731687303715884105727");
			var max = BigInteger.Parse("170141183460469231731687303715884105727");

			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.VarNumeric), default, default);
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.VarNumeric), min, max);
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.VarNumeric), max, min);
		}

		#endregion

		#region Floating point

		[Test]
		public async ValueTask TestFloat([DuckDBDataSources] string context)
		{
			// FLOAT (float)
			await TestType<float, float?>(context, new(typeof(float)), default, default);
			await TestType<float, float?>(context, new(typeof(float)), float.MinValue, float.MaxValue);
			await TestType<float, float?>(context, new(typeof(float)), float.MaxValue, float.MinValue);
			await TestType<float, float?>(context, new(typeof(float)), float.Epsilon, float.NaN, filterByNullableValue: false);
			await TestType<float, float?>(context, new(typeof(float)), float.NaN, float.Epsilon, filterByValue: false);
			await TestType<float, float?>(context, new(typeof(float)), float.PositiveInfinity, float.NegativeInfinity);
			await TestType<float, float?>(context, new(typeof(float)), float.NegativeInfinity, float.PositiveInfinity);
		}

		[Test]
		public async ValueTask TestDouble([DuckDBDataSources] string context)
		{
			// DOUBLE (double)
			await TestType<double, double?>(context, new(typeof(double)), default, default);
			await TestType<double, double?>(context, new(typeof(double)), double.MinValue, double.MaxValue);
			await TestType<double, double?>(context, new(typeof(double)), double.MaxValue, double.MinValue);
			await TestType<double, double?>(context, new(typeof(double)), double.Epsilon, double.NaN, filterByNullableValue: false);
			await TestType<double, double?>(context, new(typeof(double)), double.NaN, double.Epsilon, filterByValue: false);
			await TestType<double, double?>(context, new(typeof(double)), double.PositiveInfinity, double.NegativeInfinity);
			await TestType<double, double?>(context, new(typeof(double)), double.NegativeInfinity, double.PositiveInfinity);
		}

		#endregion

		#region Decimal

		[Test]
		public async ValueTask TestDecimal([DuckDBDataSources] string context)
		{
			// DECIMAL(p,s) — DuckDB supports up to DECIMAL(38,s)
			await TestType<decimal, decimal?>(context, new(typeof(decimal)), default, default);
			await TestType<decimal, decimal?>(context, new(typeof(decimal)), 123456.789m, -987654.321m);

			// with explicit precision/scale
			var type18_6 = new DbDataType(typeof(decimal)).WithPrecision(18).WithScale(6);
			await TestType<decimal, decimal?>(context, type18_6, default, default);
			await TestType<decimal, decimal?>(context, type18_6, 123456789012.123456m, -123456789012.123456m);

			// cross-type: unsigned
			await TestInteger<byte>(context, DataType.Decimal, 0, byte.MaxValue);
			await TestInteger<ushort>(context, DataType.Decimal, 0, ushort.MaxValue);
			await TestInteger<uint>(context, DataType.Decimal, 0, uint.MaxValue);
			await TestInteger<ulong>(context, new DbDataType(typeof(ulong), DataType.Decimal).WithPrecision(20).WithScale(0), 0, ulong.MaxValue);

			// cross-type: signed
			await TestInteger<sbyte>(context, DataType.Decimal, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.Decimal, short.MinValue, short.MaxValue);
			await TestInteger<int>(context, DataType.Decimal, int.MinValue, int.MaxValue);
			await TestInteger<long>(context, new DbDataType(typeof(long), DataType.Decimal).WithPrecision(19).WithScale(0), long.MinValue, long.MaxValue);
			await TestInteger<float>(context, new DbDataType(typeof(float), DataType.Decimal).WithPrecision(8).WithScale(0), -16777220L, 16777220L);
			await TestInteger<double>(context, new DbDataType(typeof(double), DataType.Decimal).WithPrecision(16).WithScale(0), -9007199254740990L, 9007199254740990L);
		}

		#endregion

		#region String and Binary

		[Test]
		public async ValueTask TestVarChar([DuckDBDataSources] string context)
		{
			// VARCHAR (string)
			await TestType<string, string?>(context, new(typeof(string)), "test string", default);
			await TestType<string, string?>(context, new(typeof(string)), "test string", "другая строка");
			await TestType<string, string?>(context, new(typeof(string)), "with 'quotes' and \"double\"", "special\tchars\n");

			// char → string (DuckDB has no char type)
			await TestType<char, char?>(context, new(typeof(char)), 'A', default);
			await TestType<char, char?>(context, new(typeof(char)), 'A', 'я');
		}

		[Test]
		public async ValueTask TestBlob([DuckDBDataSources] string context)
		{
			// BLOB (byte[]) — DuckDB doesn't support binary comparison via parameters
			await TestType<byte[], byte[]?>(context, new(typeof(byte[])), new byte[] { 0 }, default, filterByValue: false, filterByNullableValue: false);
			await TestType<byte[], byte[]?>(context, new(typeof(byte[])), new byte[] { 0, 1, 2, 3, 4, 0 }, new byte[] { 255, 254, 253 }, filterByValue: false, filterByNullableValue: false);
		}

		#endregion

		#region UUID

		[Test]
		public async ValueTask TestUUID([DuckDBDataSources] string context)
		{
			await TestType<Guid, Guid?>(context, new(typeof(Guid)), default, default);
			await TestType<Guid, Guid?>(context, new(typeof(Guid)), TestData.Guid1, TestData.Guid2);
		}

		#endregion

		#region Date and Time

		[Test]
		public async ValueTask TestDate([DuckDBDataSources] string context)
		{
			// DATE
#if SUPPORTS_DATEONLY
			await TestType<DateOnly, DateOnly?>(context, new(typeof(DateOnly)), DateOnly.FromDateTime(TestData.Date), default);
			await TestType<DateOnly, DateOnly?>(context, new(typeof(DateOnly)), new DateOnly(1970, 1, 1), new DateOnly(2100, 12, 31));
#endif

			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date), new DateTime(1970, 1, 1), new DateTime(2100, 12, 31));
		}

		[Test]
		public async ValueTask TestTime([DuckDBDataSources] string context)
		{
			// TIME — microsecond precision, 00:00:00 to 23:59:59.999999
			var max = new TimeSpan(0, 23, 59, 59, 999).Add(TimeSpan.FromTicks(9990)); // 23:59:59.999999

#if SUPPORTS_TIMEONLY
			await TestType<TimeOnly, TimeOnly?>(context, new(typeof(TimeOnly)), default, default);
			await TestType<TimeOnly, TimeOnly?>(context, new(typeof(TimeOnly)), TimeOnly.MinValue, TimeOnly.FromTimeSpan(max));
#endif

			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan), DataType.Time), new TimeSpan(1, 2, 3), default);
			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan), DataType.Time), new TimeSpan(1, 2, 3), max);
		}

		[Test]
		public async ValueTask TestTimestamp([DuckDBDataSources] string context)
		{
			// TIMESTAMP — microsecond precision
			var min = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var max = new DateTime(2100, 12, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(9999990); // microsecond precision

			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), min, max);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), max, min);
		}

		[Test]
		public async ValueTask TestTimestampTZ([DuckDBDataSources] string context)
		{
			// TIMESTAMPTZ — microsecond precision, always stored as UTC
			// DuckDB normalizes all values to UTC, original offset is lost
			var min = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
			var max = new DateTimeOffset(2100, 12, 31, 23, 59, 59, TimeSpan.Zero).AddTicks(9999990);

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset)), default, default);
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset)), min, max);
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset)), max, min);

			// values with non-zero offset are normalized to UTC
			var withOffset = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.FromHours(3));
			var expectedUtc = withOffset.ToOffset(TimeSpan.Zero);
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset)), withOffset, default, getExpectedValue: _ => expectedUtc);
		}

		[Test]
		public async ValueTask TestInterval([DuckDBDataSources] string context)
		{
			// INTERVAL — DuckDB Appender doesn't support TimeSpan for INTERVAL columns
			var small = new TimeSpan(1, 2, 3, 4);            // 1 day, 02:03:04 (>24h, renders as INTERVAL)
			var large = new TimeSpan(30, 12, 30, 45);         // 30 days, 12:30:45

			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan), DataType.Interval), small, default, testBulkCopyType: SkipAppender);
			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan), DataType.Interval), large, small, testBulkCopyType: SkipAppender);
		}

		#endregion

		#region JSON

		[Test]
		public async ValueTask TestJson([DuckDBDataSources] string context)
		{
			await TestType<string, string?>(context, new(typeof(string), DataType.Json), "{}", default, filterByValue: false, filterByNullableValue: false);
			await TestType<string, string?>(context, new(typeof(string), DataType.Json), /*lang=json,strict*/ "{\"key\": 123}", /*lang=json,strict*/ "{\"arr\": [1, 2]}", filterByValue: false, filterByNullableValue: false);
		}

		#endregion
	}
}
