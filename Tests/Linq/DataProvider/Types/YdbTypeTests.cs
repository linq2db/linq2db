using System;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Mapping;

using NUnit.Framework;
using System.Globalization;

namespace Tests.DataProvider
{
	[TestFixture]
	public sealed class YdbTypeTests : TypeTestsBase
	{

		#region Infrastructure

		sealed class YdbDataSourcesAttribute : IncludeDataSourcesAttribute
		{
			public YdbDataSourcesAttribute()
				: this(includeLinqService: true)
			{
			}

			public YdbDataSourcesAttribute(bool includeLinqService)
				: base(includeLinqService, ProviderName.Ydb)
			{
			}
		}

		protected override bool TestParameters => true;

		#endregion

		#region Integer

		[Test]
		public async ValueTask TestIntegerTypes([YdbDataSources(false)] string context)
		{
			// Int8 / UInt8 ----------------------------------------------------
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte)), default, default);
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte)), sbyte.MinValue, sbyte.MaxValue);
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte)), sbyte.MaxValue, sbyte.MinValue);

			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte), DataType.SByte), default, default);
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte), DataType.SByte), sbyte.MinValue, sbyte.MaxValue);
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte), DataType.SByte), sbyte.MaxValue, sbyte.MinValue);

			await TestType<byte, byte?>(context, new(typeof(byte)), default, default);
			await TestType<byte, byte?>(context, new(typeof(byte)), byte.MinValue, byte.MaxValue);
			await TestType<byte, byte?>(context, new(typeof(byte)), byte.MaxValue, byte.MinValue);

			await TestType<byte, byte?>(context, new(typeof(byte), DataType.Byte), default, default);
			await TestType<byte, byte?>(context, new(typeof(byte), DataType.Byte), byte.MinValue, byte.MaxValue);
			await TestType<byte, byte?>(context, new(typeof(byte), DataType.Byte), byte.MaxValue, byte.MinValue);

			// Int16 / UInt16 ---------------------------------------------------
			await TestType<short, short?>(context, new(typeof(short)), default, default);
			await TestType<short, short?>(context, new(typeof(short)), short.MinValue, short.MaxValue);
			await TestType<short, short?>(context, new(typeof(short)), short.MaxValue, short.MinValue);

			await TestType<short, short?>(context, new(typeof(short), DataType.Int16), default, default);
			await TestType<short, short?>(context, new(typeof(short), DataType.Int16), short.MinValue, short.MaxValue);
			await TestType<short, short?>(context, new(typeof(short), DataType.Int16), short.MaxValue, short.MinValue);

			await TestType<ushort, ushort?>(context, new(typeof(ushort)), default, default);
			await TestType<ushort, ushort?>(context, new(typeof(ushort)), ushort.MinValue, ushort.MaxValue);
			await TestType<ushort, ushort?>(context, new(typeof(ushort)), ushort.MaxValue, ushort.MinValue);

			await TestType<ushort, ushort?>(context, new(typeof(ushort), DataType.UInt16), default, default);
			await TestType<ushort, ushort?>(context, new(typeof(ushort), DataType.UInt16), ushort.MinValue, ushort.MaxValue);
			await TestType<ushort, ushort?>(context, new(typeof(ushort), DataType.UInt16), ushort.MaxValue, ushort.MinValue);

			// Int32 / UInt32 ---------------------------------------------------
			await TestType<int, int?>(context, new(typeof(int)), default, default);
			await TestType<int, int?>(context, new(typeof(int)), int.MinValue, int.MaxValue);
			await TestType<int, int?>(context, new(typeof(int)), int.MaxValue, int.MinValue);

			await TestType<int, int?>(context, new(typeof(int), DataType.Int32), default, default);
			await TestType<int, int?>(context, new(typeof(int), DataType.Int32), int.MinValue, int.MaxValue);
			await TestType<int, int?>(context, new(typeof(int), DataType.Int32), int.MaxValue, int.MinValue);

			await TestType<uint, uint?>(context, new(typeof(uint)), default, default);
			await TestType<uint, uint?>(context, new(typeof(uint)), uint.MinValue, uint.MaxValue);
			await TestType<uint, uint?>(context, new(typeof(uint)), uint.MaxValue, uint.MinValue);

			await TestType<uint, uint?>(context, new(typeof(uint), DataType.UInt32), default, default);
			await TestType<uint, uint?>(context, new(typeof(uint), DataType.UInt32), uint.MinValue, uint.MaxValue);
			await TestType<uint, uint?>(context, new(typeof(uint), DataType.UInt32), uint.MaxValue, uint.MinValue);

			// Int64 / UInt64 ---------------------------------------------------
			await TestType<long, long?>(context, new(typeof(long)), default, default);
			await TestType<long, long?>(context, new(typeof(long)), long.MinValue, long.MaxValue);
			await TestType<long, long?>(context, new(typeof(long)), long.MaxValue, long.MinValue);

			await TestType<long, long?>(context, new(typeof(long), DataType.Int64), default, default);
			await TestType<long, long?>(context, new(typeof(long), DataType.Int64), long.MinValue, long.MaxValue);
			await TestType<long, long?>(context, new(typeof(long), DataType.Int64), long.MaxValue, long.MinValue);

			await TestType<ulong, ulong?>(context, new(typeof(ulong)), default, default);
			await TestType<ulong, ulong?>(context, new(typeof(ulong)), ulong.MinValue, ulong.MaxValue);
			await TestType<ulong, ulong?>(context, new(typeof(ulong)), ulong.MaxValue, ulong.MinValue);

			await TestType<ulong, ulong?>(context, new(typeof(ulong), DataType.UInt64), default, default);
			await TestType<ulong, ulong?>(context, new(typeof(ulong), DataType.UInt64), ulong.MinValue, ulong.MaxValue);
			await TestType<ulong, ulong?>(context, new(typeof(ulong), DataType.UInt64), ulong.MaxValue, ulong.MinValue);
		}

		#endregion

		#region Decimal
		[Test]
		public async ValueTask TestDecimalTypes([YdbDataSources(false)] string context)
		{
		    bool isYdb = context.StartsWith("YDB", StringComparison.OrdinalIgnoreCase);

		    const decimal ydbMax = 999_999_999_999.999_999_999M; // 12 integer digits, 9 fractional digits
		    const decimal ydbMin = -ydbMax;

		    // ───────────────────────────────────
		    // 1. Default Mapping (Decimal(22,9))
		    // ───────────────────────────────────
		    // Tests the default mapping for the decimal type.
		    await TestType<decimal, decimal?>(context,
		       new(typeof(decimal)), default, default);

		    // For YDB, also test with its specific maximum and minimum values.
		    if (isYdb)
		       await TestType<decimal, decimal?>(context,
		          new(typeof(decimal)), ydbMax, ydbMin);

		    // ───────────────────────────────────
		    // 2. Precision and Scale Iteration (p,s)
		    // ───────────────────────────────────
		    // Iterates through various precision (p) and scale (s) combinations
		    // to ensure correct handling of decimal types with different defined sizes.
		    int[] precisions = { 1, 9, 18, 22, 28, 29, 38 };

		    foreach (var p in precisions)
		    {
		       int[] scales = p > 1 ? new[] { 0, 1, p - 1 } : new[] { 0 };

		       foreach (var s in scales)
		       {
		          if (s > p) continue;           // Invalid SQL combination

		          // YDB only supports Decimal(22,9); skip others for YDB.
		          if (isYdb && (p != 22 || s != 9))
		             continue;

		          var decType = new DbDataType(typeof(decimal), DataType.Decimal, null, null, p, s);
		          var strType = new DbDataType(typeof(string),  DataType.Decimal, null, null, p, s);

		          // Construct string representations of ±maximum values.
		          string digits = new string('9', p);
		          string maxStr = s == 0
		       ? digits
		       : $"{digits[..(p - s)]}.{digits[(p - s)..]}".TrimStart('.');
		          string minStr = '-' + maxStr;

		          // Test with "0" and null values (always valid).
		          await TestType<string, string?>(context, strType, "0", default);

		          // Test with string representations of ±maximum values.
		          await TestType<string, string?>(context, strType, minStr, maxStr);

		          // Decimal round-trip test:
		          //   • Not performed if p >= 29 (does not fit into .NET decimal).
		          //   • For YDB (22,9), use the "safe" maximum/minimum values.
		          if (p < 29)
		          {
		             decimal maxDec, minDec;

		             if (isYdb && p == 22 && s == 9)
		             {
		                maxDec = ydbMax;
		                minDec = ydbMin;
		             }
		             else
		             {
		                maxDec = decimal.Parse(maxStr, CultureInfo.InvariantCulture);
		                minDec = -maxDec;
		             }

		             await TestType<decimal, decimal?>(context, decType, minDec, maxDec);
		          }
		       }
		    }
		}
		#endregion

		#region Boolean
		[Test]
		public async ValueTask TestBoolType([YdbDataSources(false)] string context)
		{
		    //------------------------------------------------------
		    // 1. Schema: bool <-> byte (1/0)
		    //------------------------------------------------------
		    // Defines a custom mapping schema where boolean values are converted
		    // to and from byte values (1 for true, 0 for false).
		    var boolByte = new MappingSchema();
		    boolByte.SetConverter<bool, byte>(b => b ? (byte)1 : (byte)0);
		    boolByte.SetConverter<bool?, byte?>(b => b.HasValue ? (byte?)(b.Value ? (byte)1 : (byte)0) : null);
		    boolByte.SetConverter<byte, bool>(b => b != 0);
		    boolByte.SetConverter<byte?, bool?>(b => b.HasValue ? b.Value != 0 : (bool?)null);

		    // Helper function to apply the custom mapping schema.
		    DataOptions useBoolByte(DataOptions opts) => opts.UseMappingSchema(boolByte);

		    //------------------------------------------------------
		    // 2. Standard Mapping: Bool <-> YDB.Bool
		    //------------------------------------------------------
		    // Tests the default mapping of .NET bool to YDB's native Boolean type.
		    await TestType<bool, bool?>(context, new(typeof(bool)), default, default);
		    await TestType<bool, bool?>(context, new(typeof(bool)), true, false);
		    await TestType<bool, bool?>(context, new(typeof(bool)), false, true);

		    await TestType<bool, bool?>(context, new(typeof(bool), DataType.Boolean), default, default);
		    await TestType<bool, bool?>(context, new(typeof(bool), DataType.Boolean), true, false);
		    await TestType<bool, bool?>(context, new(typeof(bool), DataType.Boolean), false, true);

		    //------------------------------------------------------
		    // 3. Mapping: Bool <-> YDB.Uint8 (Requires Converter)
		    //------------------------------------------------------
		    // Tests mapping of .NET bool to YDB's Uint8 type, using the custom
		    // converter defined in section 1.
		    await TestType<bool, bool?>(context, new(typeof(bool), DataType.Byte), default, default,
		                         optionsBuilder: useBoolByte);
		    await TestType<bool, bool?>(context, new(typeof(bool), DataType.Byte), true, false,
		                         optionsBuilder: useBoolByte);
		    await TestType<bool, bool?>(context, new(typeof(bool), DataType.Byte), false, true,
		                         optionsBuilder: useBoolByte);
		}
		#endregion

		#region Date / Datetime / Timestamp
		/// <summary>
       /// Verifies YDB calendar types:
       ///   • Date       → DataType.Date        (days)
       ///   • Datetime   → DataType.DateTime    (seconds)
       ///   • Timestamp  → DataType.DateTime2   (microseconds)
       /// <br/><br/>
       /// According to the <a href="https://ydb.tech/docs/en/yql/reference/types/primitive#datetime">official documentation</a>,
       /// the range of values for all time types except for `Interval` is from `1970-01-01 00:00:00` to `2106-01-01 00:00:00`.
       /// The internal representation for `Date` is an unsigned 16-bit integer.
       /// </summary>
       [Test]
       public async ValueTask TestDateTypes([YdbDataSources(false)] string context)
       {
          //----------------------------------------------------------------
          // 1. Date (available only as DateOnly or a "YYYY-MM-DD" string)
          //----------------------------------------------------------------
#if NET6_0_OR_GREATER
    {
       var dMin = new DateOnly(1970, 1, 1);
       var dMax = new DateOnly(2105, 12, 31);

       await TestType<DateOnly, DateOnly?>(
          context,
          new(typeof(DateOnly), DataType.Date),
          DateOnly.FromDateTime(TestData.Date), default);

       await TestType<DateOnly, DateOnly?>(
          context,
          new(typeof(DateOnly), DataType.Date),
          dMin, dMax);
    }
#endif
          //----------------------------------------------------------------
          // 2. Datetime (seconds from the Unix epoch, up to 2106-02-07 06:28:15)
          //----------------------------------------------------------------
          var dtMin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
          var dtMax = new DateTime(2105, 12, 31, 23, 59, 59, DateTimeKind.Unspecified);

          await TestType<DateTime, DateTime?>(
             context,
             new(typeof(DateTime), DataType.DateTime),
             TestData.DateTime.TrimPrecision(0), default);

          await TestType<DateTime, DateTime?>(
             context,
             new(typeof(DateTime), DataType.DateTime),
             dtMin, dtMax);

          //----------------------------------------------------------------
          // 3. Timestamp (64-bit microseconds, up to 2262-04-11 23:47:16.8547758)
          //----------------------------------------------------------------
          var tsMin = TestData.DateTime.TrimPrecision(6);
          var tsMax = new DateTime(2105, 12, 31, 23, 59, 59, 999, DateTimeKind.Unspecified)
             .AddTicks(7758)
             .TrimPrecision(6);

          await TestType<DateTime, DateTime?>(
             context,
             new(typeof(DateTime), DataType.DateTime2),
             tsMin, default);

          await TestType<DateTime, DateTime?>(
             context,
             new(typeof(DateTime), DataType.DateTime2),
             tsMin, tsMax);
       }
		#endregion

	}
}
