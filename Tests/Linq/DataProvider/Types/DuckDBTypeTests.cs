#if SUPPORTS_DATEONLY
using System;
using System.Collections;
using System.Data.Linq;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;

using DuckDB.NET.Native;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.DataProvider
{
	/*
	 * https://duckdb.org/docs/sql/data_types/overview
	 * 
	 * Uncovered:
	 * - enums https://duckdb.org/docs/current/sql/data_types/enum
	 * - geometry https://duckdb.org/docs/current/sql/data_types/geometry
	 * - list https://duckdb.org/docs/current/sql/data_types/list
	 * - array https://duckdb.org/docs/current/sql/data_types/array
	 * - map https://duckdb.org/docs/current/sql/data_types/map
	 * - struct https://duckdb.org/docs/current/sql/data_types/struct
	 * - union https://duckdb.org/docs/current/sql/data_types/union
	 * - Stream
	 * 
	 * For write-supported types check
	 * https://github.com/Giorgi/DuckDB.NET/blob/main/DuckDB.NET.Data/PreparedStatement/ClrToDuckDBConverter.cs
	 * 
	 * All commented test-cases are provider bugs or missing functionality in provider
	 * - disabled parameters: lack of type support by provider parameters, workarounded using literals
	 * - disabled bulk copy: lack of type support by provider-specific copy without workaround possible
	 */
	[TestFixture]
	public sealed class DuckDBTypeTests : TypeTestsBase
	{
		sealed class DuckDBDataSourcesAttribute : IncludeDataSourcesAttribute
		{
			public DuckDBDataSourcesAttribute()
				: base(TestProvName.AllDuckDB)
			{
			}
		}

		static bool TestBulkCopyType(BulkCopyType bc) => bc != BulkCopyType.ProviderSpecific;

		#region Boolean

		[Test]
		public async ValueTask TestBoolean([DuckDBDataSources] string context)
		{
			// https://duckdb.org/docs/current/sql/data_types/boolean
			await TestType<bool, bool?>(context, new(typeof(bool)), default, default);
			await TestType<bool, bool?>(context, new(typeof(bool)), true, false);
			await TestType<bool, bool?>(context, new(typeof(bool)), false, true);
		}

		#endregion

		// Numeric types
		// https://duckdb.org/docs/current/sql/data_types/numeric

		#region Integer types

		// https://duckdb.org/docs/current/sql/data_types/numeric#fixed-width-integer-types
		async ValueTask TestInteger<TType>(string context, DbDataType dataType, TType min, TType max)
			where TType : struct
		{
			await TestType<TType, TType?>(context, dataType, default, default);
			await TestType<TType, TType?>(context, dataType, min, max);
			await TestType<TType, TType?>(context, dataType, max, min);
		}

		[Test]
		public async ValueTask TestTinyInt([DuckDBDataSources] string context)
		{
			// TINYINT (sbyte)
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte)), default, default);
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte)), sbyte.MinValue, sbyte.MaxValue);
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte)), sbyte.MaxValue, sbyte.MinValue);
		}

		[Test]
		public async ValueTask TestUTinyInt([DuckDBDataSources] string context)
		{
			// UTINYINT (byte)
			await TestType<byte, byte?>(context, new(typeof(byte)), default, default);
			await TestType<byte, byte?>(context, new(typeof(byte)), byte.MinValue, byte.MaxValue);
			await TestType<byte, byte?>(context, new(typeof(byte)), byte.MaxValue, byte.MinValue);
		}

		[Test]
		public async ValueTask TestSmallInt([DuckDBDataSources] string context)
		{
			// SMALLINT (short)
			await TestType<short, short?>(context, new(typeof(short)), default, default);
			await TestType<short, short?>(context, new(typeof(short)), short.MinValue, short.MaxValue);
			await TestType<short, short?>(context, new(typeof(short)), short.MaxValue, short.MinValue);
		}

		[Test]
		public async ValueTask TestUSmallInt([DuckDBDataSources] string context)
		{
			// USMALLINT (ushort)
			await TestType<ushort, ushort?>(context, new(typeof(ushort)), default, default);
			await TestType<ushort, ushort?>(context, new(typeof(ushort)), ushort.MinValue, ushort.MaxValue);
			await TestType<ushort, ushort?>(context, new(typeof(ushort)), ushort.MaxValue, ushort.MinValue);
		}

		[Test]
		public async ValueTask TestInteger([DuckDBDataSources] string context)
		{
			// INTEGER (int)
			await TestType<int, int?>(context, new(typeof(int)), default, default);
			await TestType<int, int?>(context, new(typeof(int)), int.MinValue, int.MaxValue);
			await TestType<int, int?>(context, new(typeof(int)), int.MaxValue, int.MinValue);
		}

		[Test]
		public async ValueTask TestUInteger([DuckDBDataSources] string context)
		{
			// UINTEGER (uint)
			await TestType<uint, uint?>(context, new(typeof(uint)), default, default);
			await TestType<uint, uint?>(context, new(typeof(uint)), uint.MinValue, uint.MaxValue);
			await TestType<uint, uint?>(context, new(typeof(uint)), uint.MaxValue, uint.MinValue);
		}

		[Test]
		public async ValueTask TestBigInt([DuckDBDataSources] string context)
		{
			// BIGINT (long)
			await TestType<long, long?>(context, new(typeof(long)), default, default);
			await TestType<long, long?>(context, new(typeof(long)), long.MinValue, long.MaxValue);
			await TestType<long, long?>(context, new(typeof(long)), long.MaxValue, long.MinValue);

			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan), DataType.Int64), default, default);
			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan), DataType.Int64), TimeSpan.MinValue, TimeSpan.MaxValue);
		}

		[Test]
		public async ValueTask TestUBigInt([DuckDBDataSources] string context)
		{
			// UBIGINT (ulong)
			await TestType<ulong, ulong?>(context, new(typeof(ulong)), default, default);
			await TestType<ulong, ulong?>(context, new(typeof(ulong)), ulong.MinValue, ulong.MaxValue);
			await TestType<ulong, ulong?>(context, new(typeof(ulong)), ulong.MaxValue, ulong.MinValue);
		}

		[Test]
		public async ValueTask TestHugeInt([DuckDBDataSources] string context)
		{
			var min = BigInteger.Parse("-170141183460469231731687303715884105727");
			var max = BigInteger.Parse("170141183460469231731687303715884105727");

			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.Int128), default, default);
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.Int128), min, max);
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.Int128), max, min);
		}

		[Test]
		public async ValueTask TestUHugeInt([DuckDBDataSources] string context)
		{
			var min = BigInteger.Zero;
			var max = BigInteger.Parse("340282366920938463463374607431768211455");

			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.UInt128), default, default);
			// provider bug with Int128 ranges applied to UInt128 type
			//await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.UInt128), min, max);
			//await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.UInt128), max, min);
		}

		[Test]
		public async ValueTask TestBigNum([DuckDBDataSources] string context)
		{
			// https://duckdb.org/docs/current/sql/data_types/numeric#variable-length-integers
			// we will not test real min/max as they will take 8 MB single number
			// MIN/MAX: ±4.27e20201778
			var min = BigInteger.Parse("-340282366920938463463374607431768211455340282366920938463463374607431768211455");
			var max = BigInteger.Parse("340282366920938463463374607431768211455340282366920938463463374607431768211455");

			// provider bug : reads garbage, fails to read or crash
			//await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger)), default, default, expectedParamCount: 0);
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger)), min, max, expectedParamCount: 0, testBulkCopyType: TestBulkCopyType);
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger)), max, min, expectedParamCount: 0, testBulkCopyType: TestBulkCopyType);
		}

		#endregion

		#region Floating point

		// https://duckdb.org/docs/current/sql/data_types/numeric#floating-point-types

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
			// https://duckdb.org/docs/current/sql/data_types/numeric#fixed-point-decimals
			// default mapping: DECIMAL(18, 3)
			var defaultMax = 999999999999999.999M;
			var defaultMin = -999999999999999.999M;

			await TestType<decimal, decimal?>(context, new(typeof(decimal)), default, default);

			await TestType<decimal, decimal?>(context, new(typeof(decimal)), defaultMax, defaultMin);

			// max precision: 38
			var precisions = new int[] { 1, 2, 21, 22, 23, 37, 38 };
			foreach (var p in precisions)
			{
				for (var s = 0; s <= p; s++)
				{
					// test only s=0,1,..,p-1, p and 9
					if (s > 1 && s < p - 1 && s != 9)
						continue;

					var decimalType = new DbDataType(typeof(decimal)).WithPrecision(p).WithScale(s);
					var stringType  = new DbDataType(typeof(string), DataType.Decimal, null, null, p, s);

					var maxString = new string('9', p);
					if (s > 0)
						maxString = $"{maxString.Substring(0, p - s)}.{maxString.Substring(p - s)}";
					if (maxString[0] == '.')
						maxString = $"0{maxString}";

					decimal minDecimal;
					decimal maxDecimal;
					if (p >= 29)
					{
						maxDecimal = decimal.MaxValue;
						minDecimal = decimal.MinValue;

						for (var i = 0; i < s; i++)
						{
							maxDecimal /= 10;
							minDecimal /= 10;
						}
					}
					else
					{
						maxDecimal = decimal.Parse(maxString, CultureInfo.InvariantCulture);
						minDecimal = -maxDecimal;
					}

					await TestType<decimal, decimal?>(context, decimalType, default, default);
					await TestType<decimal, decimal?>(context, decimalType, minDecimal, maxDecimal);
				}
			}
		}

		#endregion

		#region String and Binary

		[Test]
		public async ValueTask TestVarChar([DuckDBDataSources] string context)
		{
			// https://duckdb.org/docs/current/sql/data_types/text
			// VARCHAR (string)
			await TestType<string, string?>(context, new(typeof(string)), string.Empty, default);
			await TestType<string, string?>(context, new(typeof(string)), "test string  ", "другая строка  ");
			await TestType<string, string?>(context, new(typeof(string)), "with 'quotes' and \"double\"", "special\tc\b\f\rhars\n");
			// provider has issues with at least \0, \1 chars in parameters. literals work
			await TestType<string, string?>(context, new(typeof(string)), "1\02", "3\x00014", testParameters: false, testBulkCopyType: TestBulkCopyType);

			// with length contraint on column
			await TestType<string, string?>(context, new DbDataType(typeof(string)).WithLength(10), "1234567890x", "0123456789q");

			// char → string (DuckDB has no char type)
			await TestType<char, char?>(context, new(typeof(char)), 'A', default);
			await TestType<char, char?>(context, new(typeof(char)), 'A', 'я');
			// provider has issues with at least \0, \1 chars. literals work
			await TestType<char, char?>(context, new(typeof(char)), '\0', '\x1', testParameters: false, testBulkCopyType: TestBulkCopyType);

			// DuckDBString not tested for now
		}

		[Test]
		public async ValueTask TestBlob([DuckDBDataSources] string context)
		{
			// https://duckdb.org/docs/current/sql/data_types/blob
			// BLOB (byte[]) — DuckDB doesn't support binary comparison via parameters
			await TestType<byte[], byte[]?>(context, new(typeof(byte[])), new byte[] { 0 }, default/*, filterByValue: false, filterByNullableValue: false*/);
			await TestType<byte[], byte[]?>(context, new(typeof(byte[])), new byte[] { 0, 1, 2, 3, 4, 0 }, new byte[] { 255, 254, 253 }/*, filterByValue: false, filterByNullableValue: false*/);

			await TestType<Binary, Binary?>(context, new(typeof(Binary)), new Binary([]), default);
			await TestType<Binary, Binary?>(context, new(typeof(Binary)), new Binary([1, 2, 255]), new Binary([123, 2, 253, 1]));
		}

		[Test]
		public async ValueTask TestBitString([DuckDBDataSources] string context)
		{
			// https://duckdb.org/docs/current/sql/data_types/bitstring

			// bulk copy support not implemented yet by provider

			await TestType<BitArray, BitArray?>(context, new(typeof(BitArray)), new BitArray(1, true), default, expectedParamCount: 0, testBulkCopyType: TestBulkCopyType);
			await TestType<BitArray, BitArray?>(context, new(typeof(BitArray)), new BitArray(new byte[] { 0, 1, 2, 3, 4, 0 }), new BitArray(new byte[] { 255, 254, 253 }), expectedParamCount: 0, testBulkCopyType: TestBulkCopyType);

			await TestType<string, string?>(context, new(typeof(string), DataType.BitArray), "0", default, testBulkCopyType: TestBulkCopyType, expectedParamCount: 0);
			await TestType<string, string?>(context, new(typeof(string), DataType.BitArray), "00101010101010101", "100101001010110101010101", testBulkCopyType: TestBulkCopyType, expectedParamCount: 0);

			await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.BitArray), new byte[] { 0 }, default, testBulkCopyType: TestBulkCopyType, expectedParamCount: 0);
			await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.BitArray), new byte[] { 0, 1, 2, 3, 4, 0 }, new byte[] { 255, 254, 253 }, testBulkCopyType: TestBulkCopyType, expectedParamCount: 0);
		}

		#endregion

		#region UUID

		[Test]
		public async ValueTask TestUUID([DuckDBDataSources] string context)
		{
			// https://duckdb.org/docs/current/sql/data_types/numeric#universally-unique-identifiers-uuids
			await TestType<Guid, Guid?>(context, new(typeof(Guid)), default, default);
			await TestType<Guid, Guid?>(context, new(typeof(Guid)), TestData.Guid1, TestData.Guid2);
		}

		#endregion

		#region Date and Time

		[Test]
		public async ValueTask TestDate([DuckDBDataSources] string context)
		{
			// https://duckdb.org/docs/current/sql/data_types/date
			// DATE
			// min: 0001-01-01
			// max: 5881580-07-10

			await TestType<DateOnly, DateOnly?>(context, new(typeof(DateOnly)), default, default);
			await TestType<DateOnly, DateOnly?>(context, new(typeof(DateOnly)), DateOnly.MinValue, DateOnly.MaxValue);

			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date), DateTime.MinValue.Date, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date), DateTime.MinValue.Date, DateTime.MaxValue.Date);

			await TestType<DuckDBDateOnly, DuckDBDateOnly?>(context, new(typeof(DuckDBDateOnly)), new(1, 2, 3), default);
			await TestType<DuckDBDateOnly, DuckDBDateOnly?>(context, new(typeof(DuckDBDateOnly)), DuckDBDateOnly.NegativeInfinity, DuckDBDateOnly.PositiveInfinity);
			await TestType<DuckDBDateOnly, DuckDBDateOnly?>(context, new(typeof(DuckDBDateOnly)), new(-5877641, 6, 25), new(5881580, 7, 10));
		}

		[Test]
		public async ValueTask TestTime([DuckDBDataSources] string context)
		{
			// https://duckdb.org/docs/current/sql/data_types/time
			// TIME — microsecond precision, 00:00:00 to 23:59:59.999999. Precision: 0-6
			// TIME_NS — nanosecond precision, 00:00:00 to 23:59:59.999999999. Precision: 7+
			var max = new TimeSpan(0, 23, 59, 59, 999).Add(TimeSpan.FromTicks(9990)); // 23:59:59.999999

			await TestType<TimeOnly, TimeOnly?>(context, new(typeof(TimeOnly)), default, default);
			//await TestType<TimeOnly, TimeOnly?>(context, new(typeof(TimeOnly)), TimeOnly.MinValue, TimeOnly.FromTimeSpan(max));

			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan), DataType.Time), default, default);
			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan), DataType.Time), new TimeSpan(1, 2, 3), max);

			var precision = 0;
			await TestType<TimeOnly, TimeOnly?>(context, new DbDataType(typeof(TimeOnly)).WithPrecision(precision), default, default);
			// provider incorrectly works with TimeOnly fractional second
			//await TestType<TimeOnly, TimeOnly?>(context, new DbDataType(typeof(TimeOnly)).WithPrecision(precision), TimeOnly.MinValue, TimeOnly.FromTimeSpan(max));

			await TestType<TimeSpan, TimeSpan?>(context, new DbDataType(typeof(TimeSpan), DataType.Time).WithPrecision(precision), default, default);
			await TestType<TimeSpan, TimeSpan?>(context, new DbDataType(typeof(TimeSpan), DataType.Time).WithPrecision(precision), new TimeSpan(1, 2, 3), max);

			precision = 5;
			await TestType<TimeOnly, TimeOnly?>(context, new DbDataType(typeof(TimeOnly)).WithPrecision(precision), default, default);
			//await TestType<TimeOnly, TimeOnly?>(context, new DbDataType(typeof(TimeOnly)).WithPrecision(precision), TimeOnly.MinValue, TimeOnly.FromTimeSpan(max));

			await TestType<TimeSpan, TimeSpan?>(context, new DbDataType(typeof(TimeSpan), DataType.Time).WithPrecision(precision), default, default);
			await TestType<TimeSpan, TimeSpan?>(context, new DbDataType(typeof(TimeSpan), DataType.Time).WithPrecision(precision), new TimeSpan(1, 2, 3), max);

			precision = 6;
			await TestType<TimeOnly, TimeOnly?>(context, new DbDataType(typeof(TimeOnly)).WithPrecision(precision), default, default);
			//await TestType<TimeOnly, TimeOnly?>(context, new DbDataType(typeof(TimeOnly)).WithPrecision(precision), TimeOnly.MinValue, TimeOnly.FromTimeSpan(max));

			await TestType<TimeSpan, TimeSpan?>(context, new DbDataType(typeof(TimeSpan), DataType.Time).WithPrecision(precision), default, default);
			await TestType<TimeSpan, TimeSpan?>(context, new DbDataType(typeof(TimeSpan), DataType.Time).WithPrecision(precision), new TimeSpan(1, 2, 3), max);

			// TIME_NS
			max = new TimeSpan(0, 23, 59, 59, 999).Add(TimeSpan.FromTicks(9999)); // 23:59:59.999999

			// TIME_NS not supported by provider:
			// ArgumentException: 'Unrecognised type 39 (39) for column Column'
			precision = 7;
			//await TestType<TimeOnly, TimeOnly?>(context, new DbDataType(typeof(TimeOnly)).WithPrecision(precision), default, default, expectedParamCount: 0);
			//await TestType<TimeOnly, TimeOnly?>(context, new DbDataType(typeof(TimeOnly)).WithPrecision(precision), TimeOnly.MinValue, TimeOnly.FromTimeSpan(max), expectedParamCount: 0);

			//await TestType<TimeSpan, TimeSpan?>(context, new DbDataType(typeof(TimeSpan), DataType.Time).WithPrecision(precision), default, default, expectedParamCount: 0);
			//await TestType<TimeSpan, TimeSpan?>(context, new DbDataType(typeof(TimeSpan), DataType.Time).WithPrecision(precision), new TimeSpan(1, 2, 3), max, expectedParamCount: 0);

			precision = 9;
			//await TestType<TimeOnly, TimeOnly?>(context, new DbDataType(typeof(TimeOnly)).WithPrecision(precision), default, default, expectedParamCount: 0);
			//await TestType<TimeOnly, TimeOnly?>(context, new DbDataType(typeof(TimeOnly)).WithPrecision(precision), TimeOnly.MinValue, TimeOnly.FromTimeSpan(max), expectedParamCount: 0);

			//await TestType<TimeSpan, TimeSpan?>(context, new DbDataType(typeof(TimeSpan), DataType.Time).WithPrecision(precision), default, default, expectedParamCount: 0);
			//await TestType<TimeSpan, TimeSpan?>(context, new DbDataType(typeof(TimeSpan), DataType.Time).WithPrecision(precision), new TimeSpan(1, 2, 3), max, expectedParamCount: 0);

			// native types
			await TestType<DuckDBTimeOnly, DuckDBTimeOnly?>(context, new DbDataType(typeof(DuckDBTimeOnly)), new DuckDBTimeOnly(1, 2, 3, 123456), new DuckDBTimeOnly(23, 59, 59, 999999));
		}

		[Test]
		public async ValueTask TestTimeTZ([DuckDBDataSources] string context)
		{
			// https://duckdb.org/docs/current/sql/data_types/time
			// TIME + TZ
			var max = new TimeSpan(0, 23, 59, 59, 999).Add(TimeSpan.FromTicks(9990)); // 23:59:59.999999

			// TimeOnly handled incorrectly by provider
			//await TestType<TimeOnly, TimeOnly?>(context, new(typeof(TimeOnly), DataType.TimeTZ), default, default);
			//await TestType<TimeOnly, TimeOnly?>(context, new(typeof(TimeOnly), DataType.TimeTZ), TimeOnly.MinValue, TimeOnly.FromTimeSpan(max));

			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan), DataType.TimeTZ), default, default);
			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan), DataType.TimeTZ), new TimeSpan(1, 2, 3), max);

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.TimeTZ), default, default);
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.TimeTZ), new DateTimeOffset(1, 1, 1, 1, 2, 3, 456, 789, TimeSpan.FromMinutes(45)), new DateTimeOffset(1, 1, 1, 5, 2, 3, 456, 789, TimeSpan.FromMinutes(-45)));
		}

		[Test]
		public async ValueTask TestInterval([DuckDBDataSources] string context)
		{
			// https://duckdb.org/docs/current/sql/data_types/interval
			var small = new TimeSpan(2, 3, 4) + TimeSpan.FromTicks(1234560); // no days
			var large = new TimeSpan(30, 12, 30, 45) + TimeSpan.FromTicks(1234560); // with days

			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan)), small, default);
			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan)), large, small);
			// provider bug: negative interval support missing
			//await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan)), -large, -small);
			//await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan)), TimeSpan.MinValue, TimeSpan.MaxValue);

			// native types
			// provider bug: negative interval support missing
			//await TestType<DuckDBInterval, DuckDBInterval?>(context, new DbDataType(typeof(DuckDBInterval)), new DuckDBInterval(123, 2, 123456789012), new DuckDBInterval(-2, 30, 212345678901), expectedParamCount: 0);
			await TestType<DuckDBInterval, DuckDBInterval?>(context, new DbDataType(typeof(DuckDBInterval)), new DuckDBInterval(0, 2, 23456789012), new DuckDBInterval(0, 30, 12345678901), expectedParamCount: 0);
			await TestType<DuckDBInterval, DuckDBInterval?>(context, new DbDataType(typeof(DuckDBInterval)), new DuckDBInterval(0, 0, 56789012), new DuckDBInterval(0, 0, 56789012), expectedParamCount: 0);
		}

		[Test]
		public async ValueTask TestTimestamp([DuckDBDataSources] string context)
		{
			// for timestamp types we use precision to select type
			// TIMESTAMP — microsecond precision, P = 4-6 or default
			// TIMESTAMP_S — microsecond precision, P = 0-2
			// TIMESTAMP_MS — microsecond precision, P = 3-5
			// TIMESTAMP_NS — microsecond precision, P = 7+
			var min = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
			var max = DateTime.SpecifyKind(DateTime.MaxValue.AddTicks(-9), DateTimeKind.Utc);
			var minU = DateTime.SpecifyKind(min, DateTimeKind.Unspecified);
			var maxU = DateTime.SpecifyKind(max, DateTimeKind.Unspecified);
			var minL = min.AddDays(1).ToLocalTime();
			var maxL = max.AddDays(-1).ToLocalTime();

			// TIMESTAMP
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), min, max);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), minU, maxU);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), minL, maxL);

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.DateTime), TestData.DateTimeOffset6, default, getExpectedValue: v => new DateTimeOffset(v.DateTime, default));

			var precision = 6;
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), min, max);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), minU, maxU);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), minL, maxL);

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new DbDataType(typeof(DateTimeOffset), DataType.DateTime).WithPrecision(precision), TestData.DateTimeOffset6, default, getExpectedValue: v => new DateTimeOffset(v.DateTime, default));

			precision = 4;
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), min, max);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), minU, maxU);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), minL, maxL);

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new DbDataType(typeof(DateTimeOffset), DataType.DateTime).WithPrecision(precision), TestData.DateTimeOffset6, default, getExpectedValue: v => new DateTimeOffset(v.DateTime, default));

			// TIMESTAMP_S
			min = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
			max = DateTime.SpecifyKind(DateTime.MaxValue.AddTicks(-9999999), DateTimeKind.Utc);
			minU = DateTime.SpecifyKind(min, DateTimeKind.Unspecified);
			maxU = DateTime.SpecifyKind(max, DateTimeKind.Unspecified);
			minL = min.AddDays(1).ToLocalTime();
			maxL = max.AddDays(-1).ToLocalTime();

			precision = 0;
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), min, max);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), minU, maxU);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), minL, maxL);

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new DbDataType(typeof(DateTimeOffset), DataType.DateTime).WithPrecision(precision), TestData.DateTimeOffset0, default, getExpectedValue: v => new DateTimeOffset(v.DateTime, default));

			// TIMESTAMP_MS
			min = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
			max = DateTime.SpecifyKind(DateTime.MaxValue.AddTicks(-9999), DateTimeKind.Utc);
			minU = DateTime.SpecifyKind(min, DateTimeKind.Unspecified);
			maxU = DateTime.SpecifyKind(max, DateTimeKind.Unspecified);
			minL = min.AddDays(1).ToLocalTime();
			maxL = max.AddDays(-1).ToLocalTime();
			precision = 3;
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), min, max);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), minU, maxU);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), minL, maxL);

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new DbDataType(typeof(DateTimeOffset), DataType.DateTime).WithPrecision(precision), TestData.DateTimeOffset3, default, getExpectedValue: v => new DateTimeOffset(v.DateTime, default));

			precision = 2;
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), min, max);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), minU, maxU);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), minL, maxL);

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new DbDataType(typeof(DateTimeOffset), DataType.DateTime).WithPrecision(precision), TestData.DateTimeOffset3, default, getExpectedValue: v => new DateTimeOffset(v.DateTime, default));

			// TIMESTAMP_NS: test only 7 precision digits: DateTime[Offset] limitation
			// this type has smaller range:
			// 1677-09-21 00:12:43.145224192
			// 2262-04-11 23:47:16.854775807
			min = new DateTime(1677, 9, 21, 0, 12, 43, 145, 225, DateTimeKind.Utc).AddTicks(1);
			max = new DateTime(2262, 4, 11, 23, 47, 16, 854, 775, DateTimeKind.Utc).AddTicks(8);
			minU = DateTime.SpecifyKind(min, DateTimeKind.Unspecified);
			maxU = DateTime.SpecifyKind(max, DateTimeKind.Unspecified);
			minL = min.AddDays(1).ToLocalTime();
			maxL = max.AddDays(-1).ToLocalTime();
			precision = 9;
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), min, max);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), minU, maxU);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), minL, maxL);

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new DbDataType(typeof(DateTimeOffset), DataType.DateTime).WithPrecision(precision), TestData.DateTimeOffset, default, getExpectedValue: v => new DateTimeOffset(v.DateTime, default));

			precision = 7;
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), min, max);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), minU, maxU);
			await TestType<DateTime, DateTime?>(context, new DbDataType(typeof(DateTime)).WithPrecision(precision), minL, maxL);

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new DbDataType(typeof(DateTimeOffset), DataType.DateTime).WithPrecision(precision), TestData.DateTimeOffset3, default, getExpectedValue: v => new DateTimeOffset(v.DateTime, default));

			// native

			// TestBulkCopyType: for values outsize of DateTime range

			await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)), DuckDBTimestamp.NegativeInfinity, DuckDBTimestamp.PositiveInfinity, testBulkCopyType: TestBulkCopyType);
			await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)), new DuckDBTimestamp(new DuckDBDateOnly(1677, 9, 21), new DuckDBTimeOnly(0,12,43,145224)), new DuckDBTimestamp(new DuckDBDateOnly(2262, 4, 11), new DuckDBTimeOnly(23, 47, 16, 854775)));

			precision = 0;
			await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), DuckDBTimestamp.NegativeInfinity, DuckDBTimestamp.PositiveInfinity, testBulkCopyType: TestBulkCopyType);
			await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), new DuckDBTimestamp(new DuckDBDateOnly(1677, 9, 21), new DuckDBTimeOnly(0, 12, 43, 0)), new DuckDBTimestamp(new DuckDBDateOnly(2262, 4, 11), new DuckDBTimeOnly(23, 47, 16, 0)));

			precision = 1;
			await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), DuckDBTimestamp.NegativeInfinity, DuckDBTimestamp.PositiveInfinity, testBulkCopyType: TestBulkCopyType);
			await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), new DuckDBTimestamp(new DuckDBDateOnly(1677, 9, 21), new DuckDBTimeOnly(0, 12, 43, 100000)), new DuckDBTimestamp(new DuckDBDateOnly(2262, 4, 11), new DuckDBTimeOnly(23, 47, 16, 800000)));

			precision = 2;
			await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), DuckDBTimestamp.NegativeInfinity, DuckDBTimestamp.PositiveInfinity, testBulkCopyType: TestBulkCopyType);
			await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), new DuckDBTimestamp(new DuckDBDateOnly(1677, 9, 21), new DuckDBTimeOnly(0, 12, 43, 140000)), new DuckDBTimestamp(new DuckDBDateOnly(2262, 4, 11), new DuckDBTimeOnly(23, 47, 16, 850000)));

			precision = 3;
			await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), DuckDBTimestamp.NegativeInfinity, DuckDBTimestamp.PositiveInfinity, testBulkCopyType: TestBulkCopyType);
			await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), new DuckDBTimestamp(new DuckDBDateOnly(1677, 9, 21), new DuckDBTimeOnly(0, 12, 43, 145000)), new DuckDBTimestamp(new DuckDBDateOnly(2262, 4, 11), new DuckDBTimeOnly(23, 47, 16, 854000)));

			precision = 4;
			await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), DuckDBTimestamp.NegativeInfinity, DuckDBTimestamp.PositiveInfinity, testBulkCopyType: TestBulkCopyType);
			await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), new DuckDBTimestamp(new DuckDBDateOnly(1677, 9, 21), new DuckDBTimeOnly(0, 12, 43, 145200)), new DuckDBTimestamp(new DuckDBDateOnly(2262, 4, 11), new DuckDBTimeOnly(23, 47, 16, 854700)));

			precision = 5;
			await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), DuckDBTimestamp.NegativeInfinity, DuckDBTimestamp.PositiveInfinity, testBulkCopyType: TestBulkCopyType);
			await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), new DuckDBTimestamp(new DuckDBDateOnly(1677, 9, 21), new DuckDBTimeOnly(0, 12, 43, 145220)), new DuckDBTimestamp(new DuckDBDateOnly(2262, 4, 11), new DuckDBTimeOnly(23, 47, 16, 854770)));

			precision = 6;
			await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), DuckDBTimestamp.NegativeInfinity, DuckDBTimestamp.PositiveInfinity, testBulkCopyType: TestBulkCopyType);
			await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), new DuckDBTimestamp(new DuckDBDateOnly(1677, 9, 21), new DuckDBTimeOnly(0, 12, 43, 145224)), new DuckDBTimestamp(new DuckDBDateOnly(2262, 4, 11), new DuckDBTimeOnly(23, 47, 16, 854775)));

			// DuckDBTimestamp cannot be used with TIMESTAMP_NS - handles it like TIMESTAMP leading to bad data
			//precision = 7;
			//await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), DuckDBTimestamp.NegativeInfinity, DuckDBTimestamp.PositiveInfinity, testBulkCopyType: TestBulkCopyType, expectedParamCount: 0);
			//await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), new DuckDBTimestamp(new DuckDBDateOnly(1677, 9, 21), new DuckDBTimeOnly(0, 12, 43, 145224)), new DuckDBTimestamp(new DuckDBDateOnly(2262, 4, 11), new DuckDBTimeOnly(23, 47, 16, 854775)), expectedParamCount: 0);

			//precision = 8;
			//await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), DuckDBTimestamp.NegativeInfinity, DuckDBTimestamp.PositiveInfinity, testBulkCopyType: TestBulkCopyType, expectedParamCount: 0);
			//await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), new DuckDBTimestamp(new DuckDBDateOnly(1677, 9, 21), new DuckDBTimeOnly(0, 12, 43, 145224)), new DuckDBTimestamp(new DuckDBDateOnly(2262, 4, 11), new DuckDBTimeOnly(23, 47, 16, 854775)), expectedParamCount: 0);

			//precision = 9;
			//await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), DuckDBTimestamp.NegativeInfinity, DuckDBTimestamp.PositiveInfinity, testBulkCopyType: TestBulkCopyType, expectedParamCount: 0);
			//await TestType<DuckDBTimestamp, DuckDBTimestamp?>(context, new DbDataType(typeof(DuckDBTimestamp)).WithPrecision(precision), new DuckDBTimestamp(new DuckDBDateOnly(1677, 9, 21), new DuckDBTimeOnly(0, 12, 43, 145224)), new DuckDBTimestamp(new DuckDBDateOnly(2262, 4, 11), new DuckDBTimeOnly(23, 47, 16, 854775)), expectedParamCount: 0);
		}

		[Test]
		public async ValueTask TestTimestampTZ([DuckDBDataSources] string context)
		{
			// TIMESTAMP — microsecond precision
			var min = DateTimeOffset.MinValue.AddTicks(1234560);
			var max = DateTimeOffset.MaxValue.AddTicks(-9);
			var maxL = max.AddDays(-1).ToLocalTime();

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset)), default, default);
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset)), min, max);
		}

		#endregion

		#region JSON

		[Test]
		public async ValueTask TestJson([DuckDBDataSources] string context)
		{
			await TestType<string, string?>(context, new(typeof(string), DataType.Json), "{}", default);
			await TestType<string, string?>(context, new(typeof(string), DataType.Json), "null", "null");
			await TestType<string, string?>(context, new(typeof(string), DataType.Json), "false", "true");
			await TestType<string, string?>(context, new(typeof(string), DataType.Json), "\"test\"", "123");
			await TestType<string, string?>(context, new(typeof(string), DataType.Json), /*lang=json,strict*/ "{\"ы\": 1.23}", /*lang=json,strict*/ "{\"prop\": false }");
		}

		#endregion
	}
}
#endif
