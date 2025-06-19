using System;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

#if !NETFRAMEWORK
using ClickHouse.Client.Numerics;
#endif

namespace Tests.DataProvider
{
	public sealed  class ClickHouseTypeTests : TypeTestsBase
	{
		sealed class ClickHouseDataSourcesAttribute : IncludeDataSourcesAttribute
		{
			public ClickHouseDataSourcesAttribute()
				: this(true)
			{
			}

			public ClickHouseDataSourcesAttribute(bool includeLinqService)
				: base(includeLinqService, TestProvName.AllClickHouse)
			{
			}
		}

		// parameters support not implemented currently
		protected override bool TestParameters => false;

		/*
		 * Currently missing types:
		 *
		 * 1. LowCardinality (https://clickhouse.com/docs/en/sql-reference/data-types/lowcardinality/)
		 *      type modifier, needed only for table creation API
		 *      Workaround: specify DbType explicitly
		 *
		 * 2. AggregateFunction (https://clickhouse.com/docs/en/sql-reference/data-types/aggregatefunction/)
		 *    SimpleAggregateFunction (https://clickhouse.com/docs/en/sql-reference/data-types/simpleaggregatefunction/)
		 *      types usage is not clear with linq2db
		 *
		 * 3. Nested (https://clickhouse.com/docs/en/sql-reference/data-types/nested-data-structures/nested/)
		 *      implementation requires a lot of work, we could look at it at request
		 *
		 * 4. Tuple (https://clickhouse.com/docs/en/sql-reference/data-types/tuple/)
		 *      SqlRow could be used for construction, full support requires more work
		 *
		 * 5. Map (https://clickhouse.com/docs/en/sql-reference/data-types/map/)
		 *      same as nested/tuple - requires extra work to support
		 *
		 * 6. Array (https://clickhouse.com/docs/en/sql-reference/data-types/array/)
		 *      implement on request
		 *
		 * 7. Interval (https://clickhouse.com/docs/en/sql-reference/data-types/special-data-types/interval/)
		 *      as type cannot be used for columns, we don't test it here (some query support implemented)
		 *
		 * 8. Geo types (https://clickhouse.com/docs/en/sql-reference/data-types/geo/)
		 *      not suppored by providers and still experimental
		 *      implementation requires providers support and array and tuple types support first
		 *
		 * 9.  Decimals with precision > 29 lack support from Octonica provider
		 *
		 * 10. DateTime/DateTime64 with precision > 8 lack of suitable type to store such precision
		 *
		 * 11. Timezone qualifier for datetime types not covered
		 *
		 * 12. Enums lack type generation from mappings
		 *
		 * 13. JSON type doesn't look usable outside of DB
		 *
		 */

		[Test]
		public async ValueTask TestIntegerTypes([ClickHouseDataSources(false)] string context)
		{
			// https://clickhouse.com/docs/en/sql-reference/data-types/int-uint/

			// Int8
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte)), default, default);
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte)), sbyte.MinValue, sbyte.MaxValue);
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte)), sbyte.MaxValue, sbyte.MinValue);

			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte), DataType.SByte), default, default);
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte), DataType.SByte), sbyte.MinValue, sbyte.MaxValue);
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte), DataType.SByte), sbyte.MaxValue, sbyte.MinValue);

			// UInt8
			await TestType<byte, byte?>(context, new(typeof(byte)), default, default);
			await TestType<byte, byte?>(context, new(typeof(byte)), byte.MinValue, byte.MaxValue);
			await TestType<byte, byte?>(context, new(typeof(byte)), byte.MaxValue, byte.MinValue);

			await TestType<byte, byte?>(context, new(typeof(byte), DataType.Byte), default, default);
			await TestType<byte, byte?>(context, new(typeof(byte), DataType.Byte), byte.MinValue, byte.MaxValue);
			await TestType<byte, byte?>(context, new(typeof(byte), DataType.Byte), byte.MaxValue, byte.MinValue);

			// Int16
			await TestType<short, short?>(context, new(typeof(short)), default, default);
			await TestType<short, short?>(context, new(typeof(short)), short.MinValue, short.MaxValue);
			await TestType<short, short?>(context, new(typeof(short)), short.MaxValue, short.MinValue);

			await TestType<short, short?>(context, new(typeof(short), DataType.Int16), default, default);
			await TestType<short, short?>(context, new(typeof(short), DataType.Int16), short.MinValue, short.MaxValue);
			await TestType<short, short?>(context, new(typeof(short), DataType.Int16), short.MaxValue, short.MinValue);

			// UInt16
			await TestType<ushort, ushort?>(context, new(typeof(ushort)), default, default);
			await TestType<ushort, ushort?>(context, new(typeof(ushort)), ushort.MinValue, ushort.MaxValue);
			await TestType<ushort, ushort?>(context, new(typeof(ushort)), ushort.MaxValue, ushort.MinValue);

			await TestType<ushort, ushort?>(context, new(typeof(ushort), DataType.UInt16), default, default);
			await TestType<ushort, ushort?>(context, new(typeof(ushort), DataType.UInt16), ushort.MinValue, ushort.MaxValue);
			await TestType<ushort, ushort?>(context, new(typeof(ushort), DataType.UInt16), ushort.MaxValue, ushort.MinValue);

			// Int32
			await TestType<int, int?>(context, new(typeof(int)), default, default);
			await TestType<int, int?>(context, new(typeof(int)), int.MinValue, int.MaxValue);
			await TestType<int, int?>(context, new(typeof(int)), int.MaxValue, int.MinValue);

			await TestType<int, int?>(context, new(typeof(int), DataType.Int32), default, default);
			await TestType<int, int?>(context, new(typeof(int), DataType.Int32), int.MinValue, int.MaxValue);
			await TestType<int, int?>(context, new(typeof(int), DataType.Int32), int.MaxValue, int.MinValue);

			// UInt32
			await TestType<uint, uint?>(context, new(typeof(uint)), default, default);
			await TestType<uint, uint?>(context, new(typeof(uint)), uint.MinValue, uint.MaxValue);
			await TestType<uint, uint?>(context, new(typeof(uint)), uint.MaxValue, uint.MinValue);

			await TestType<uint, uint?>(context, new(typeof(uint), DataType.UInt32), default, default);
			await TestType<uint, uint?>(context, new(typeof(uint), DataType.UInt32), uint.MinValue, uint.MaxValue);
			await TestType<uint, uint?>(context, new(typeof(uint), DataType.UInt32), uint.MaxValue, uint.MinValue);

			// Int64
			await TestType<long, long?>(context, new(typeof(long)), default, default);
			await TestType<long, long?>(context, new(typeof(long)), long.MinValue, long.MaxValue);
			await TestType<long, long?>(context, new(typeof(long)), long.MaxValue, long.MinValue);

			await TestType<long, long?>(context, new(typeof(long), DataType.Int64), default, default);
			await TestType<long, long?>(context, new(typeof(long), DataType.Int64), long.MinValue, long.MaxValue);
			await TestType<long, long?>(context, new(typeof(long), DataType.Int64), long.MaxValue, long.MinValue);

			// UInt64
			await TestType<ulong, ulong?>(context, new(typeof(ulong)), default, default);
			await TestType<ulong, ulong?>(context, new(typeof(ulong)), ulong.MinValue, ulong.MaxValue);
			await TestType<ulong, ulong?>(context, new(typeof(ulong)), ulong.MaxValue, ulong.MinValue);

			await TestType<ulong, ulong?>(context, new(typeof(ulong), DataType.UInt64), default, default);
			await TestType<ulong, ulong?>(context, new(typeof(ulong), DataType.UInt64), ulong.MinValue, ulong.MaxValue);
			await TestType<ulong, ulong?>(context, new(typeof(ulong), DataType.UInt64), ulong.MaxValue, ulong.MinValue);

			// Int128 : no default type
			var int128Min = BigInteger.Parse("-170141183460469231731687303715884105728");
			var int128Max = BigInteger.Parse("170141183460469231731687303715884105727");
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.Int128), default, default);
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.Int128), int128Min, int128Max);
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.Int128), int128Max, int128Min);

			// UInt128 : no default type
			var uint128Min = BigInteger.Zero;
			var uint128Max = BigInteger.Parse("340282366920938463463374607431768211455");
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.UInt128), default, default);
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.UInt128), uint128Min, uint128Max);
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.UInt128), uint128Max, uint128Min);

			// Int256
			var int256Min = BigInteger.Parse("-57896044618658097711785492504343953926634992332820282019728792003956564819968");
			var int256Max = BigInteger.Parse("57896044618658097711785492504343953926634992332820282019728792003956564819967");

			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger)), default, default);
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger)), int256Min, int256Max);
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger)), int256Max, int256Min);

			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.Int256), default, default);
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.Int256), int256Min, int256Max);
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.Int256), int256Max, int256Min);

			// UInt256 : no default type
			var uint256Min = BigInteger.Zero;
			var uint256Max = BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639935");
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.UInt256), default, default);
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.UInt256), uint256Min, uint256Max);
			await TestType<BigInteger, BigInteger?>(context, new(typeof(BigInteger), DataType.UInt256), uint256Max, uint256Min);
		}

		[Test]
		public async ValueTask TestFloatingPointTypes([ClickHouseDataSources(false)] string context)
		{
			// https://clickhouse.com/docs/en/sql-reference/data-types/float/

			// NaN comparison not tested as ClickHouse doesn't implement proper comparison for it
			// see https://github.com/ClickHouse/ClickHouse/issues/38506
			// instead of strict support or not support it just works randomly
			// We don't plan to support this comparison on our side currently (also needed for Firebird)
			// as it is requires code, similar to NULL comparison translation code, and there is no
			// request for support of it from users
			const bool testNaNComparison = false;
			var infinitySupported        = true;
			var nonNullableNaNSupported  = true;

			// Float32
			await TestType<float, float?>(context, new(typeof(float)), default, default);
			await TestType<float, float?>(context, new(typeof(float)), float.MinValue, float.MaxValue);
			await TestType<float, float?>(context, new(typeof(float)), float.MaxValue, float.MinValue);
			await TestType<float, float?>(context, new(typeof(float)), float.Epsilon, float.NaN, filterByNullableValue: testNaNComparison);
			if (nonNullableNaNSupported)
				await TestType<float, float?>(context, new(typeof(float)), float.NaN, float.Epsilon, filterByValue: testNaNComparison);
			if (infinitySupported)
			{
				await TestType<float, float?>(context, new(typeof(float)), float.PositiveInfinity, float.NegativeInfinity);
				await TestType<float, float?>(context, new(typeof(float)), float.NegativeInfinity, float.PositiveInfinity);
			}

			await TestType<float, float?>(context, new(typeof(float), DataType.Single), default, default);
			await TestType<float, float?>(context, new(typeof(float), DataType.Single), float.MinValue, float.MaxValue);
			await TestType<float, float?>(context, new(typeof(float), DataType.Single), float.MaxValue, float.MinValue);
			await TestType<float, float?>(context, new(typeof(float), DataType.Single), float.Epsilon, float.NaN, filterByNullableValue: testNaNComparison);
			if (nonNullableNaNSupported)
				await TestType<float, float?>(context, new(typeof(float), DataType.Single), float.NaN, float.Epsilon, filterByValue: testNaNComparison);
			if (infinitySupported)
			{
				await TestType<float, float?>(context, new(typeof(float), DataType.Single), float.PositiveInfinity, float.NegativeInfinity);
				await TestType<float, float?>(context, new(typeof(float), DataType.Single), float.NegativeInfinity, float.PositiveInfinity);
			}

			// Float64
			await TestType<double, double?>(context, new(typeof(double)), default, default);
			await TestType<double, double?>(context, new(typeof(double)), double.MinValue, double.MaxValue);
			await TestType<double, double?>(context, new(typeof(double)), double.MaxValue, double.MinValue);
			// https://github.com/ClickHouse/ClickHouse/issues/38455
			await TestType<double, double?>(context, new(typeof(double)), /*double.Epsilon*/1.23d, double.NaN, filterByNullableValue: testNaNComparison);
			// https://github.com/ClickHouse/ClickHouse/issues/38455
			if (nonNullableNaNSupported)
				await TestType<double, double?>(context, new(typeof(double)), double.NaN, /*double.Epsilon*/-1.23d, filterByValue: testNaNComparison);
			if (infinitySupported)
			{
				await TestType<double, double?>(context, new(typeof(double)), double.PositiveInfinity, double.NegativeInfinity);
				await TestType<double, double?>(context, new(typeof(double)), double.NegativeInfinity, double.PositiveInfinity);
			}

			await TestType<double, double?>(context, new(typeof(double), DataType.Double), default, default);
			await TestType<double, double?>(context, new(typeof(double), DataType.Double), double.MinValue, double.MaxValue);
			await TestType<double, double?>(context, new(typeof(double), DataType.Double), double.MaxValue, double.MinValue);
			// https://github.com/ClickHouse/ClickHouse/issues/38455
			await TestType<double, double?>(context, new(typeof(double), DataType.Double), /*double.Epsilon*/1.23d, double.NaN, filterByNullableValue: testNaNComparison);
			// https://github.com/ClickHouse/ClickHouse/issues/38455
			if (nonNullableNaNSupported)
				await TestType<double, double?>(context, new(typeof(double), DataType.Double), double.NaN, /*double.Epsilon*/-1.23d, filterByValue: testNaNComparison);
			if (infinitySupported)
			{
				await TestType<double, double?>(context, new(typeof(double), DataType.Double), double.PositiveInfinity, double.NegativeInfinity);
				await TestType<double, double?>(context, new(typeof(double), DataType.Double), double.NegativeInfinity, double.PositiveInfinity);
			}
		}

		[Test]
		public async ValueTask TestBoolType([ClickHouseDataSources(false)] string context)
		{
			// https://clickhouse.com/docs/en/sql-reference/data-types/boolean/

			await TestType<bool, bool?>(context, new(typeof(bool)), default, default);
			await TestType<bool, bool?>(context, new(typeof(bool)), true, false);
			await TestType<bool, bool?>(context, new(typeof(bool)), false, true);

			await TestType<bool, bool?>(context, new(typeof(bool), DataType.Boolean), default, default);
			await TestType<bool, bool?>(context, new(typeof(bool), DataType.Boolean), true, false);
			await TestType<bool, bool?>(context, new(typeof(bool), DataType.Boolean), false, true);

			// as underlying type
			await TestType<bool, bool?>(context, new(typeof(bool), DataType.Byte), default, default);
			await TestType<bool, bool?>(context, new(typeof(bool), DataType.Byte), true, false);
			await TestType<bool, bool?>(context, new(typeof(bool), DataType.Byte), false, true);
		}

		[Test]
		public async ValueTask TestUUIDType([ClickHouseDataSources(false)] string context)
		{
			// https://clickhouse.com/docs/en/sql-reference/data-types/uuid/

			// UUID
			await TestType<Guid, Guid?>(context, new(typeof(Guid)), default, default);
			await TestType<Guid, Guid?>(context, new(typeof(Guid)), TestData.Guid1, TestData.Guid2);

			await TestType<Guid, Guid?>(context, new(typeof(Guid), DataType.Guid), default, default);
			await TestType<Guid, Guid?>(context, new(typeof(Guid), DataType.Guid), TestData.Guid1, TestData.Guid2);

			// TODO: disabled for now, as currently mapper expression doesn't know about DataType to generate correct mapping
			// workaround possible but it will be better to add DataType/DbDataType support to reader expressions
			//await TestType<Guid, Guid?>(context, new(typeof(Guid), DataType.Binary, null, length: 16), default, default);
			//await TestType<Guid, Guid?>(context, new(typeof(Guid), DataType.Binary, null, length: 16), TestData.Guid1, TestData.Guid2);

			// as text
			await TestType<Guid, Guid?>(context, new(typeof(Guid), DataType.VarChar), default, default);
			await TestType<Guid, Guid?>(context, new(typeof(Guid), DataType.VarChar), TestData.Guid1, TestData.Guid2);
			await TestType<Guid, Guid?>(context, new(typeof(Guid), DataType.NVarChar), default, default);
			await TestType<Guid, Guid?>(context, new(typeof(Guid), DataType.NVarChar), TestData.Guid1, TestData.Guid2);

			// as text
			await TestType<Guid, Guid?>(context, new(typeof(Guid), DataType.Char, null, length: 36), default, default);
			await TestType<Guid, Guid?>(context, new(typeof(Guid), DataType.Char, null, length: 36), TestData.Guid1, TestData.Guid2);

			await TestType<Guid, Guid?>(context, new(typeof(Guid), DataType.NChar, null, length: 36), default, default);
			await TestType<Guid, Guid?>(context, new(typeof(Guid), DataType.NChar, null, length: 36), TestData.Guid1, TestData.Guid2);
		}

		[Test]
		public async ValueTask TestDateType([ClickHouseDataSources(false)] string context)
		{
			// https://clickhouse.com/docs/en/sql-reference/data-types/date/

			var min = new DateTime(1970, 1, 1);
			var max = new DateTime(2149, 6, 6);

			// https://github.com/Octonica/ClickHouseClient/issues/60
			if (context.IsAnyOf(ProviderName.ClickHouseOctonica))
				min = new DateTime(1970, 1, 2);

			// Date : no default type
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date), min, max);

#if NET8_0_OR_GREATER
			await TestType<DateOnly, DateOnly?>(context, new(typeof(DateOnly), DataType.Date), DateOnly.FromDateTime(TestData.Date), default);
			await TestType<DateOnly, DateOnly?>(context, new(typeof(DateOnly), DataType.Date), DateOnly.FromDateTime(min), DateOnly.FromDateTime(max));
#endif

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.Date), new DateTimeOffset(TestData.Date, default), default);
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.Date), new DateTimeOffset(min, default), new DateTimeOffset(max, default));

			min = new DateTime(min.Ticks, DateTimeKind.Utc);
			max = new DateTime(max.Ticks, DateTimeKind.Utc);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date), min, max);

			min = new DateTime(min.Ticks, DateTimeKind.Local);
			max = new DateTime(max.Ticks, DateTimeKind.Local);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date), min, max);
		}

		[Test]
		public async ValueTask TestDate32Type([ClickHouseDataSources(false)] string context)
		{
			// https://clickhouse.com/docs/en/sql-reference/data-types/date32/

			var min = new DateTime(1900, 1, 1);
			var max = new DateTime(2299, 12, 31);

			// Date32
#if NET8_0_OR_GREATER
			await TestType<DateOnly, DateOnly?>(context, new(typeof(DateOnly)), DateOnly.FromDateTime(TestData.Date), default);
			await TestType<DateOnly, DateOnly?>(context, new(typeof(DateOnly)), DateOnly.FromDateTime(min), DateOnly.FromDateTime(max));

			await TestType<DateOnly, DateOnly?>(context, new(typeof(DateOnly), DataType.Date32), DateOnly.FromDateTime(TestData.Date), default);
			await TestType<DateOnly, DateOnly?>(context, new(typeof(DateOnly), DataType.Date32), DateOnly.FromDateTime(min), DateOnly.FromDateTime(max));
#endif

			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date32), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date32), min, max);

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.Date32), new DateTimeOffset(TestData.Date, default), default);
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.Date32), new DateTimeOffset(min, default), new DateTimeOffset(max, default));

			min = new DateTime(min.Ticks, DateTimeKind.Utc);
			max = new DateTime(max.Ticks, DateTimeKind.Utc);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date32), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date32), min, max);

			min = new DateTime(min.Ticks, DateTimeKind.Local);
			max = new DateTime(max.Ticks, DateTimeKind.Local);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date32), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date32), min, max);
		}

		[Test]
		public async ValueTask TestDateTimeType([ClickHouseDataSources(false)] string context)
		{
			// https://clickhouse.com/docs/en/sql-reference/data-types/datetime/

			var min = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
			var max = new DateTime(2106, 2, 7, 6, 28, 15, DateTimeKind.Unspecified);
			var val = TestData.DateTime.TrimPrecision(0);

			// https://github.com/Octonica/ClickHouseClient/issues/60
			if (context.IsAnyOf(ProviderName.ClickHouseOctonica))
				min = min.AddSeconds(1);

			// DateTime
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime), val, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime), min, max);

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.DateTime), TestData.DateTimeOffsetUtc.TrimPrecision(0), default);
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.DateTime), new DateTimeOffset(min, default), new DateTimeOffset(max, default));

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.DateTime), TestData.DateTimeOffset.TrimPrecision(0), default);
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.DateTime), new DateTimeOffset(min, TimeSpan.FromMinutes(-45)), new DateTimeOffset(max, TimeSpan.FromMinutes(45)));

			min = new DateTime(min.Ticks, DateTimeKind.Utc);
			max = new DateTime(max.Ticks, DateTimeKind.Utc);
			val = new DateTime(val.Ticks, DateTimeKind.Utc);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime), val, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime), min, max);

			min = new DateTime(min.Ticks, DateTimeKind.Local);
			max = new DateTime(max.Ticks, DateTimeKind.Local);
			val = new DateTime(val.Ticks, DateTimeKind.Local);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime), val, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime), min, max);
		}

		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/55310", Configuration = ProviderName.ClickHouseMySql)]
		[Test]
		public async ValueTask TestDateTime64Type([ClickHouseDataSources(false)] string context)
		{
			// https://clickhouse.com/docs/en/sql-reference/data-types/datetime64/

			var min  = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
			// DateTime/DateTimeOffset cannot store precision 8/9 without precision loss
			var max  = new DateTime(2299, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified).AddTicks(9999);
			// max value for DateTime64(9)
			var max9 = new DateTime(2262, 4, 11, 23, 47, 16, 854, DateTimeKind.Unspecified).AddTicks(7758);
			var val  = TestData.DateTime;

			// DateTime64

			// default mappings: DateTime64(7)
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), val, default, getExpectedValue: v => v.TrimPrecision(7), getExpectedNullableValue: v => v?.TrimPrecision(7));
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), min, max, getExpectedValue: v => v.TrimPrecision(7), getExpectedNullableValue: v => v?.TrimPrecision(7));

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset)), TestData.DateTimeOffsetUtc, default, getExpectedValue: v => v.TrimPrecision(7), getExpectedNullableValue: v => v?.TrimPrecision(7));
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset)), new DateTimeOffset(min, default), new DateTimeOffset(max, default), getExpectedValue: v => v.TrimPrecision(7), getExpectedNullableValue: v => v?.TrimPrecision(7));

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset)), TestData.DateTimeOffset, default, getExpectedValue: v => v.TrimPrecision(7), getExpectedNullableValue: v => v?.TrimPrecision(7));
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset)), new DateTimeOffset(min, TimeSpan.FromMinutes(-45)), new DateTimeOffset(max, TimeSpan.FromMinutes(45)), getExpectedValue: v => v.TrimPrecision(7), getExpectedNullableValue: v => v?.TrimPrecision(7));

			for (var p = 0; p < 10; p++)
			{
				min = new DateTime(min.Ticks, DateTimeKind.Unspecified);
				max = p == 9 ? max9 : new DateTime(max.Ticks, DateTimeKind.Unspecified);
				val = new DateTime(val.Ticks, DateTimeKind.Unspecified);

				var actualPrecision = p > 7 ? 7 : p;
				var dtType =  new DbDataType(typeof(DateTime), DataType.DateTime64, null, null, p, null);
				var dtoType =  new DbDataType(typeof(DateTime), DataType.DateTime64, null, null, p, null);

				await TestType<DateTime, DateTime?>(context, dtType, val, default, getExpectedValue: v => v.TrimPrecision(actualPrecision), getExpectedNullableValue: v => v?.TrimPrecision(actualPrecision));
				await TestType<DateTime, DateTime?>(context, dtType, min, max, getExpectedValue: v => v.TrimPrecision(actualPrecision), getExpectedNullableValue: v => v?.TrimPrecision(actualPrecision));

				await TestType<DateTimeOffset, DateTimeOffset?>(context, dtoType, TestData.DateTimeOffsetUtc.TrimPrecision(0), default, getExpectedValue: v => v.TrimPrecision(actualPrecision), getExpectedNullableValue: v => v?.TrimPrecision(actualPrecision));
				await TestType<DateTimeOffset, DateTimeOffset?>(context, dtoType, new DateTimeOffset(min, default), new DateTimeOffset(max, default), getExpectedValue: v => v.TrimPrecision(actualPrecision), getExpectedNullableValue: v => v?.TrimPrecision(actualPrecision));

				await TestType<DateTimeOffset, DateTimeOffset?>(context, dtoType, TestData.DateTimeOffset.TrimPrecision(0), default, getExpectedValue: v => v.TrimPrecision(actualPrecision), getExpectedNullableValue: v => v?.TrimPrecision(actualPrecision));
				await TestType<DateTimeOffset, DateTimeOffset?>(context, dtoType, new DateTimeOffset(min, TimeSpan.FromMinutes(-45)), new DateTimeOffset(max, TimeSpan.FromMinutes(45)), getExpectedValue: v => v.TrimPrecision(actualPrecision), getExpectedNullableValue: v => v?.TrimPrecision(actualPrecision));

				min = new DateTime(min.Ticks, DateTimeKind.Utc);
				max = new DateTime(max.Ticks, DateTimeKind.Utc);
				val = new DateTime(val.Ticks, DateTimeKind.Utc);
				await TestType<DateTime, DateTime?>(context, dtType, val, default, getExpectedValue: v => v.TrimPrecision(actualPrecision), getExpectedNullableValue: v => v?.TrimPrecision(actualPrecision));
				await TestType<DateTime, DateTime?>(context, dtType, min, max, getExpectedValue: v => v.TrimPrecision(actualPrecision), getExpectedNullableValue: v => v?.TrimPrecision(actualPrecision));

				min = new DateTime(min.Ticks, DateTimeKind.Local);
				max = new DateTime(max.Ticks, DateTimeKind.Local);
				val = new DateTime(val.Ticks, DateTimeKind.Local);
				await TestType<DateTime, DateTime?>(context, dtType, val, default, getExpectedValue: v => v.TrimPrecision(actualPrecision), getExpectedNullableValue: v => v?.TrimPrecision(actualPrecision));
				await TestType<DateTime, DateTime?>(context, dtType, min, max, getExpectedValue: v => v.TrimPrecision(actualPrecision), getExpectedNullableValue: v => v?.TrimPrecision(actualPrecision));
			}
		}

		[Test]
		public async ValueTask TestDecimal([ClickHouseDataSources(false)] string context)
		{
			// https://clickhouse.com/docs/en/sql-reference/data-types/decimal/

			// default mapping
			var defaultMax = 7922816251426433759.3543950335M;
			var defaultMin = -7922816251426433759.3543950335M;

			await TestType<decimal, decimal?>(context, new(typeof(decimal)), default, default);
			await TestType<decimal, decimal?>(context, new(typeof(decimal)), defaultMax, defaultMin);

#if !NETFRAMEWORK
			if (context.IsAnyOf(ProviderName.ClickHouseClient))
			{
				var customMin = new ClickHouseDecimal(BigInteger.Parse("-" + new string('9', 76)), 10);
				var customMax = new ClickHouseDecimal(BigInteger.Parse(new string('9', 76)), 10);
				await TestType<ClickHouseDecimal, ClickHouseDecimal?>(context, new(typeof(ClickHouseDecimal)), customMax, customMin);
			}
#endif

			// testing of all combinations is not feasible
			// we will test only several precisions and first/last two scales for each tested precision
			var precisions = new int[] { 1, 2, 8, 9, 10, 11, 17, 18, 19, 20, 28, 29, 30, 37, 38, 39, 40, 75, 76 };
			foreach (var p in precisions)
			{
				// https://github.com/Octonica/ClickHouseClient/issues/28
				if (p >= 29 && context.IsAnyOf(ProviderName.ClickHouseOctonica))
					continue;

				var skipBasicTypes = p >= 29 && context.IsAnyOf(ProviderName.ClickHouseClient);

				for (var s = 0; s <= p; s++)
				{
					if (s > 1 && s < p - 1)
						continue;

					var dataType = p switch
					{
						< 10 => DataType.Decimal32,
						< 19 => DataType.Decimal64,
						< 38 => DataType.Decimal128,
						_    => DataType.Decimal256
					};

					var decimalType = new DbDataType(typeof(decimal), dataType, null, null, p, s);
					var stringType  = new DbDataType(typeof(string), dataType, null, null, p, s);

					var maxString = new string('9', p);
					if (s > 0)
						maxString = maxString.Substring(0, p - s) + '.' + maxString.Substring(p - s);
					if (maxString[0] == '.')
						maxString = $"0{maxString}";
					var minString = $"-{maxString}";

					// not really issue
					// only ClickHouseClient fails because other providers parse values differently
					// and we test only 0 value, which works for them
					var skipOutOfRange = p >= 29 && context.IsAnyOf(ProviderName.ClickHouseClient);
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

					if (!skipOutOfRange)
						await TestType<decimal, decimal?>(context, decimalType, default, default);
					if (!skipBasicTypes)
						await TestType<decimal, decimal?>(context, decimalType, minDecimal, maxDecimal);

					var zero = "0";
					if (context.IsAnyOf(ProviderName.ClickHouseOctonica, ProviderName.ClickHouseClient) && s > 0)
						zero = $"{zero}.{new string('0', s)}";
					await TestType<string, string?>(context, stringType, "0", default, getExpectedValue: v => zero);
					if (!skipBasicTypes)
						await TestType<string, string?>(context, stringType, minString, maxString);

#if !NETFRAMEWORK
					if (context.IsAnyOf(ProviderName.ClickHouseClient))
					{
						var customDecimalType = new DbDataType(typeof(ClickHouseDecimal), dataType, null, null, p, s);
						var customMin         = new ClickHouseDecimal(BigInteger.Parse("-" + new string('9', p)), s);
						var customMax         = new ClickHouseDecimal(BigInteger.Parse(new string('9', p)), s);

						await TestType<ClickHouseDecimal, ClickHouseDecimal?>(context, customDecimalType, customMin, customMax);
					}
#endif
				}
			}
		}

		[Test]
		public async ValueTask TestString([ClickHouseDataSources(false)] string context)
		{
			// https://clickhouse.com/docs/en/sql-reference/data-types/string/
			// String

			// default
			await TestType<string, string?>(context, new(typeof(string)), string.Empty, default);
			await TestType<string, string?>(context, new(typeof(string)), "test ", "test\0");
			await TestType<byte[], byte[]?>(context, new(typeof(byte[])), Array.Empty<byte>(), default);
			await TestType<byte[], byte[]?>(context, new(typeof(byte[])), new byte[] { 1, 2, 3, 4 }, new byte[] { 1, 2, 3, 4, 0, 0 });

			await TestType<string, string?>(context, new(typeof(string), DataType.NVarChar), string.Empty, default);
			await TestType<string, string?>(context, new(typeof(string), DataType.NVarChar), "test ", "test\0");
			await TestType<string, string?>(context, new(typeof(string), DataType.VarChar), string.Empty, default);
			await TestType<string, string?>(context, new(typeof(string), DataType.VarChar), "test ", "test\0");
			await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.VarBinary), Array.Empty<byte>(), default);
			await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.VarBinary), new byte[] { 1, 2, 0, 3, 4 }, new byte[] { 1, 2, 3, 0, 4, 0, 0 });

			// various characters handing
			await TestType<string, string?>(context, new(typeof(string), DataType.NVarChar), "\x00", "\x01");
			await TestType<string, string?>(context, new(typeof(string), DataType.NVarChar), "\x02", "\x03");
			await TestType<string, string?>(context, new(typeof(string), DataType.NVarChar), "\xFF", "\xFE");
			await TestType<string, string?>(context, new(typeof(string), DataType.NVarChar), "\r", "\n");
			await TestType<string, string?>(context, new(typeof(string), DataType.NVarChar), "'", "\\");

			await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.VarBinary), new byte[] { 0 }, new byte[] { 1 });
			await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.VarBinary), new byte[] { 2 }, new byte[] { 3 });
			// https://github.com/DarkWanderer/ClickHouse.Client/issues/138
			// https://github.com/ClickHouse/ClickHouse/issues/38790
			if (!context.IsAnyOf(ProviderName.ClickHouseClient, ProviderName.ClickHouseMySql))
			{
				await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.VarBinary), new byte[] { 255 }, new byte[] { 254 });
			}

			// https://clickhouse.com/docs/en/sql-reference/data-types/fixedstring/
			// FixedString

			// https://github.com/ClickHouse/ClickHouse/issues/38059
			// we are not trimming trailing \0 for ClickHouse
			var padValues = true;

			await TestType<string, string?>(context, new(typeof(string), DataType.NChar,  null, length: 7), string.Empty, default, getExpectedValue: v => padValues ? "\0\0\0\0\0\0\0" : v);
			await TestType<string, string?>(context, new(typeof(string), DataType.NChar,  null, length: 7), "test ", "test\0", getExpectedValue: v => padValues ? "test \0\0" : v, getExpectedNullableValue: v => padValues ? "test\0\0\0" : "test");
			await TestType<string, string?>(context, new(typeof(string), DataType.Char,   null, length: 7), string.Empty, default, getExpectedValue: v => padValues ? "\0\0\0\0\0\0\0" : v);
			await TestType<string, string?>(context, new(typeof(string), DataType.Char,   null, length: 7), "test ", "test\0", getExpectedValue: v => padValues ? "test \0\0" : v, getExpectedNullableValue: v => padValues ? "test\0\0\0" : "test");
			await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.Binary, null, length: 7), Array.Empty<byte>(), default, getExpectedValue: v => new byte[7]);
			await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.Binary, null, length: 7), new byte[] { 1, 2, 0, 3, 4 }, new byte[] { 1, 2, 3, 0, 4, 0, 0 }, getExpectedValue: v => { Array.Resize(ref v, 7);return v; }, getExpectedNullableValue: v => { Array.Resize(ref v, 7); return v; });

			// default length (ClickHouseMappingSchema.DEFAULT_FIXED_STRING_LENGTH=100)
			var defaultBinary = new byte[100];
			var defaultString = new string('\0', 100);
			await TestType<string, string?>(context, new(typeof(string), DataType.NChar), string.Empty, default, getExpectedValue: _ => defaultString);
			await TestType<string, string?>(context, new(typeof(string), DataType.Char), string.Empty, default, getExpectedValue: _ => defaultString);
			await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.Binary), Array.Empty<byte>(), default, getExpectedValue: _ => defaultBinary);

			// various characters handing
			await TestType<string, string?>(context, new(typeof(string), DataType.NChar, null, length: 7), "\x00", "\x01", getExpectedValue: v => padValues ? "\0\0\0\0\0\0\0" : string.Empty, getExpectedNullableValue: v => padValues ? "\x1\0\0\0\0\0\0" : v);
			await TestType<string, string?>(context, new(typeof(string), DataType.NChar, null, length: 7), "\x02", "\x03", getExpectedValue: v => padValues ? "\x2\0\0\0\0\0\0" : v, getExpectedNullableValue: v => padValues ? "\x3\0\0\0\0\0\0" : v);
			await TestType<string, string?>(context, new(typeof(string), DataType.NChar, null, length: 7), "\xFF", "\xFE", getExpectedValue: v => padValues ? "\xff\0\0\0\0\0" : v, getExpectedNullableValue: v => padValues ? "\xfe\0\0\0\0\0" : v);
			await TestType<string, string?>(context, new(typeof(string), DataType.NChar, null, length: 7), "\r", "\n", getExpectedValue: v => padValues ? "\r\0\0\0\0\0\0" : v, getExpectedNullableValue: v => padValues ? "\n\0\0\0\0\0\0" : v);
			await TestType<string, string?>(context, new(typeof(string), DataType.NChar, null, length: 7), "'", "\\", getExpectedValue: v => padValues ? "'\0\0\0\0\0\0" : v, getExpectedNullableValue: v => padValues ? "\\\0\0\0\0\0\0" : v);

			await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.Binary, null, length: 7), new byte[] { 0 }, new byte[] { 1 }, getExpectedValue: v => { Array.Resize(ref v, 7); return v; }, getExpectedNullableValue: v => { Array.Resize(ref v, 7); return v; });
			await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.Binary, null, length: 7), new byte[] { 2 }, new byte[] { 3 }, getExpectedValue: v => { Array.Resize(ref v, 7); return v; }, getExpectedNullableValue: v => { Array.Resize(ref v, 7); return v; });
			// https://github.com/DarkWanderer/ClickHouse.Client/issues/138
			// https://github.com/ClickHouse/ClickHouse/issues/38790
			if (!context.IsAnyOf(ProviderName.ClickHouseClient, ProviderName.ClickHouseMySql))
			{
				await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.Binary, null, length: 7), new byte[] { 255 }, new byte[] { 254 }, getExpectedValue: v => { Array.Resize(ref v, 7); return v; }, getExpectedNullableValue: v => { Array.Resize(ref v, 7); return v; });
			}
		}

		[ActiveIssue(Configurations = [ProviderName.ClickHouseClient, ProviderName.ClickHouseOctonica])]
		[Test]
		public async ValueTask TestJSONType([ClickHouseDataSources(false)] string context)
		{
			// https://clickhouse.com/docs/en/sql-reference/data-types/json/
			// currently JSON type looks completely unusable

			// JSON

			// cannot even insert (with nonsense message):
			// Subcolumn '' already exists
			//await TestType<string, string?>(context, new(typeof(string), DataType.Json), "true", "false", filterByValue: false, filterByNullableValue: false);
			//await TestType<string, string?>(context, new(typeof(string), DataType.Json), "12", "-34", filterByValue: false, filterByNullableValue: false);

			// provider errors not reported as JSON type is not yet unusable - nothing to fix on client side
			// Client: ArgumentException: 'Unknown type: JSON'
			// Octonica: ClickHouseException : The type "JSON" is not supported
			//if (!context.IsAnyOf(ProviderName.ClickHouseClient, ProviderName.ClickHouseOctonica))
			{
				// WTF is (0)
				await TestType<string, string?>(context, new(typeof(string), DataType.Json), "null", "null",
					filterByValue: false, filterByNullableValue: false,
					getExpectedValue: _ => "{}", getExpectedNullableValue: _ => "(0)");

				//await TestType<string, string?>(
				//	context, new(typeof(string), DataType.Json), string.Empty, default,
				//	filterByValue: false, filterByNullableValue: false,
				//	getExpectedValue: _ => "(0)", getExpectedNullableValue: _ => "(0)");

				// Why number became string...
				await TestType<string, string?>(context, new(typeof(string), DataType.Json),
					/*lang=json,strict*/ "{ \"prop\": 333 }", /*lang=json,strict*/ "{ \"prop\": 123 }",
					filterByValue: false, filterByNullableValue: false,
					getExpectedValue: _ => /*lang=json,strict*/ "{\"prop\":\"333\"}", getExpectedNullableValue: _ => "(123)");
			}
		}

		[Test]
		public async ValueTask TestIPTypes([ClickHouseDataSources(false)] string context)
		{
			// https://clickhouse.com/docs/en/sql-reference/data-types/domains/ipv4/
			// https://clickhouse.com/docs/en/sql-reference/data-types/domains/ipv6/

			// IPv4

			// https://github.com/ClickHouse/ClickHouse/issues/39056
			if (!context.IsAnyOf(ProviderName.ClickHouseMySql))
			{
				await TestType<uint, uint?>(context, new(typeof(uint), DataType.IPv4), default, default);
				await TestType<uint, uint?>(context, new(typeof(uint), DataType.IPv4), 0x12345678, 0x87654321);

				await TestType<string, string?>(context, new(typeof(string), DataType.IPv4), "0.0.0.0", default);
				await TestType<string, string?>(context, new(typeof(string), DataType.IPv4), "127.0.0.2", "172.1.1.1");

				await TestType<IPAddress, IPAddress?>(context, new(typeof(IPAddress), DataType.IPv4), IPAddress.Parse("0.0.0.0"), default);
				await TestType<IPAddress, IPAddress?>(context, new(typeof(IPAddress), DataType.IPv4), IPAddress.Parse("127.0.0.2"), IPAddress.Parse("172.1.1.1"));
			}

			// IPv6
			await TestType<IPAddress, IPAddress?>(context, new(typeof(IPAddress)), IPAddress.Parse("0.0.0.0"), default, getExpectedValue: _ => IPAddress.Parse("::ffff:0:0"));
			await TestType<IPAddress, IPAddress?>(context, new(typeof(IPAddress)), IPAddress.Parse("127.0.0.2"), IPAddress.Parse("2001:44c8:129:2632:33:0:252:2"), getExpectedValue: _ => IPAddress.Parse("::ffff:7f00:2"));

			await TestType<string, string?>(context, new(typeof(string), DataType.IPv6), "0000:0000:0000:0000:0000:ffff:7f00:0002", default, getExpectedValue: _ => "::ffff:127.0.0.2");
			await TestType<string, string?>(context, new(typeof(string), DataType.IPv6), "127.0.0.2", "2001:44c8:129:2632:33:0:252:2", getExpectedValue: _ => "::ffff:127.0.0.2");
			await TestType<string, string?>(context, new(typeof(string), DataType.IPv6), "0:0:0:0:0:ffff:7f00:0002", "::ffff:7f00:2", getExpectedValue: _ => "::ffff:127.0.0.2", getExpectedNullableValue: _ => "::ffff:127.0.0.2");

			await TestType<IPAddress, IPAddress?>(context, new(typeof(IPAddress), DataType.IPv6), IPAddress.Parse("0.0.0.0"), default, getExpectedValue: _ => IPAddress.Parse("::ffff:0:0"));
			await TestType<IPAddress, IPAddress?>(context, new(typeof(IPAddress), DataType.IPv6), IPAddress.Parse("127.0.0.2"), IPAddress.Parse("2001:44c8:129:2632:33:0:252:2"), getExpectedValue: _ => IPAddress.Parse("::ffff:7f00:2"));

			// TODO: MySQL: we need support for DbDataType support in mapper to read value properly
			if (!context.IsAnyOf(ProviderName.ClickHouseMySql))
			{
				await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.IPv6), new byte[16], default);
				await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.IPv6), new byte[4] { 1, 2, 3, 4 }, new byte[16] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }, getExpectedValue: _ => new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xFF, 0xFF, 1, 2, 3, 4 });
			}
		}

		public enum Enum
		{
			One = 1234,
			Two = -2234
		}

		// use variable-length String type
		public enum EnumMappedVar
		{
			[MapValue("value1")]
			One = -1234,
			[MapValue("value 2")]
			Two = 4567
		}

		// use fixed-length String type
		public enum EnumMappedFixed
		{
			[MapValue("value1")]
			One = -1234,
			[MapValue("value2")]
			Two = 4567
		}

		public enum Enum8Mapped : sbyte
		{
			[MapValue("value1")]
			One = -111,
			[MapValue("value2")]
			Two = 123
		}

		public enum Enum16Mapped : short
		{
			[MapValue("value1")]
			One = -1111,
			[MapValue("value2")]
			Two = 2212
		}

		[Test]
		public async ValueTask TestEnumTypes([ClickHouseDataSources(false)] string context)
		{
			// https://clickhouse.com/docs/en/sql-reference/data-types/enum/

			// Unmapped enums cannot be used as server returns string values, which cannot be converted to enum
			// because we don't have mapping information

			// Enum8

			await TestType<Enum8Mapped, Enum8Mapped?>(context, new(typeof(Enum8Mapped), DataType.Enum8, "Enum8('value1' = -111, 'value2' = 123)"), Enum8Mapped.Two, default);
			await TestType<Enum8Mapped, Enum8Mapped?>(context, new(typeof(Enum8Mapped), DataType.Enum8, "Enum8('value1' = -111, 'value2' = 123)"), Enum8Mapped.One, Enum8Mapped.Two);

			// Enum16

			await TestType<Enum16Mapped, Enum16Mapped?>(context, new(typeof(Enum16Mapped), DataType.Enum16, "Enum16('value1' = -1111, 'value2' = 2212)"), Enum16Mapped.Two, default);
			await TestType<Enum16Mapped, Enum16Mapped?>(context, new(typeof(Enum16Mapped), DataType.Enum16, "Enum16('value1' = -1111, 'value2' = 2212)"), Enum16Mapped.One, Enum16Mapped.Two);

			await TestType<EnumMappedVar, EnumMappedVar?>(context, new(typeof(EnumMappedVar), DataType.Enum16, "Enum16('value1' = -1234, 'value 2' = 4567)"), EnumMappedVar.Two, default);
			await TestType<EnumMappedVar, EnumMappedVar?>(context, new(typeof(EnumMappedVar), DataType.Enum16, "Enum16('value1' = -1234, 'value 2' = 4567)"), EnumMappedVar.One, EnumMappedVar.Two);

			await TestType<EnumMappedFixed, EnumMappedFixed?>(context, new(typeof(EnumMappedFixed), DataType.Enum16, "Enum16('value1' = -1234, 'value2' = 4567)"), EnumMappedFixed.Two, default);
			await TestType<EnumMappedFixed, EnumMappedFixed?>(context, new(typeof(EnumMappedFixed), DataType.Enum16, "Enum16('value1' = -1234, 'value2' = 4567)"), EnumMappedFixed.One, EnumMappedFixed.Two);

			// check that default mappings still work
			await TestType<Enum, Enum?>(context, new(typeof(Enum)), Enum.Two, default);
			await TestType<Enum, Enum?>(context, new(typeof(Enum)), Enum.One, Enum.Two);

			await TestType<EnumMappedVar, EnumMappedVar?>(context, new(typeof(EnumMappedVar)), EnumMappedVar.Two, default);
			await TestType<EnumMappedVar, EnumMappedVar?>(context, new(typeof(EnumMappedVar)), EnumMappedVar.One, EnumMappedVar.Two);

			await TestType<EnumMappedFixed, EnumMappedFixed?>(context, new(typeof(EnumMappedFixed)), EnumMappedFixed.Two, default);
			await TestType<EnumMappedFixed, EnumMappedFixed?>(context, new(typeof(EnumMappedFixed)), EnumMappedFixed.One, EnumMappedFixed.Two);
		}

		[Test]
		public async ValueTask TestTimeSpan([ClickHouseDataSources(false)] string context)
		{
			// Interval* type is not supported in tables
			// Here we test mapping to Int64 ticks (no default mapping)

			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan), DataType.Int64), default, default);
			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan), DataType.Int64), TimeSpan.MaxValue, TimeSpan.MinValue);
		}

		// TODO: types currently not supported, see notes below
		//[Test]
		//public async ValueTask TestGeoTypes([ClickHouseDataSources(false)] string context)
		//{
		//	// https://clickhouse.com/docs/en/sql-reference/data-types/geo/

		//	// Geo types cannot be nullable
		//	// Client/Octonica providers currently throw "unsupported type" exceptions
		//	// MySQL provider implementation could be added by require types parser from string

		//	// Point

		//	await TestType<Tuple<double, double>, Tuple<double, double>?>(context, new(typeof(Tuple<double, double>), DataType.Point), Tuple.Create<double, double>(0, 0), null, skipNullable: true);
		//	await TestType<Tuple<double, double>, Tuple<double, double>?>(context, new(typeof(Tuple<double, double>), DataType.Point), Tuple.Create<double, double>(-12.34, 45.664), null, skipNullable: true);

		//	// Ring

		//	await TestType<Tuple<double, double>[], Tuple<double, double>[]?>(context, new(typeof(Tuple<double, double>[]), DataType.Ring), Array<Tuple<double, double>>.Empty, null, skipNullable: true);
		//	await TestType<Tuple<double, double>[], Tuple<double, double>[]?>(context, new(typeof(Tuple<double, double>[]), DataType.Ring), new[] { Tuple.Create<double, double>(-12.34, 45.664) }, null, skipNullable: true);

		//	// Polygon

		//	await TestType<Tuple<double, double>[][], Tuple<double, double>[][]?>(context, new(typeof(Tuple<double, double>[][]), DataType.Polygon), Array<Tuple<double, double>[]>.Empty, null, skipNullable: true);
		//	await TestType<Tuple<double, double>[][], Tuple<double, double>[][]?>(context, new(typeof(Tuple<double, double>[][]), DataType.Polygon),
		//		new Tuple<double, double>[][]
		//		{
		//			new[]
		//			{
		//				Tuple.Create<double, double>(-12.34, 45.664)
		//			},
		//			new[]
		//			{
		//				Tuple.Create<double, double>(-252.2, 445.6264),
		//				Tuple.Create<double, double>(-53, 145.6634)
		//			},
		//			Array<Tuple<double, double>>.Empty
		//		},
		//		null, skipNullable: true);

		//	// MultiPolygon

		//	await TestType<Tuple<double, double>[][][], Tuple<double, double>[][][]?>(context, new(typeof(Tuple<double, double>[][][]), DataType.MultiPolygon), Array<Tuple<double, double>[][]>.Empty, null, skipNullable: true);
		//	await TestType<Tuple<double, double>[][][], Tuple<double, double>[][][]?>(context, new(typeof(Tuple<double, double>[][][]), DataType.MultiPolygon),
		//		new Tuple<double, double>[][][]
		//		{
		//			new Tuple<double, double>[][]
		//			{
		//				new[]
		//				{
		//					Tuple.Create<double, double>(-12.34, 45.664)
		//				},
		//				new[]
		//				{
		//					Tuple.Create<double, double>(-252.2, 445.6264),
		//					Tuple.Create<double, double>(-53, 145.6634)
		//				},
		//				Array<Tuple<double, double>>.Empty
		//			}
		//		},
		//		null, skipNullable: true);
		//}
	}
}
