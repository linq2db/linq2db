using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LinqToDB;

using NUnit.Framework;

namespace Tests.DataProvider
{
	// https://ydb.tech/docs/en/yql/reference/types/primitive
	[TestFixture]
	public sealed class YdbTypeTests : TypeTestsBase
	{
		private static readonly string TestEscapingString = string.Join("", Enumerable.Range(0, 255).Select(i => (char)i));

		sealed class YdbDataSourcesAttribute : IncludeDataSourcesAttribute
		{
			public YdbDataSourcesAttribute()
				: base(ProviderName.Ydb)
			{
			}
		}

		[Sql.Expression("ListHas({1}, {0})", IsPredicate = true, ServerSideOnly = true)]
		static bool InList(object? field, object? list) => throw new InvalidOperationException();

		protected override IQueryable<TypeTable<TType, TNullableType>> MakeListFilter<TType, TNullableType>(TempTable<TypeTable<TType, TNullableType>> table, object? columnValue, object? nullableColumnValue)
		{
			if (columnValue != null && nullableColumnValue != null)
			{
				var list = new List<TType>() { (TType)columnValue };
				var nullableList = new List<TNullableType>() { (TNullableType)nullableColumnValue };
				return table.Where(r => InList(r.Column, list) && InList(r.ColumnNullable, nullableList));
			}

			if (columnValue != null)
			{
				var list = new List<TType>() { (TType)columnValue };
				return table.Where(r => InList(r.Column, list));
			}

			if (nullableColumnValue != null)
			{
				var nullableList = new List<TNullableType>() { (TNullableType)nullableColumnValue };
				return table.Where(r => InList(r.ColumnNullable, nullableList));
			}

			throw new InvalidOperationException("Unreachable");
		}

		[Test]
		public async ValueTask TestBool([YdbDataSources] string context)
		{
			await TestType<bool, bool?>(context, new(typeof(bool)), default, default, testListParameter: true);
			await TestType<bool, bool?>(context, new(typeof(bool)), true, false, testListParameter: true);
			await TestType<bool, bool?>(context, new(typeof(bool)), false, true, testListParameter: true);
		}

		ValueTask TestInteger<TType>(string context, DataType dataType, TType min, TType max, bool? testParameters = null)
			where TType : struct
		{
			return TestInteger(context, new DbDataType(typeof(TType), dataType), max, min, testParameters: testParameters);
		}

		async ValueTask TestInteger<TType>(string context, DbDataType dataType, TType min, TType max, bool? testParameters = null)
			where TType : struct
		{
			await TestType<TType, TType?>(context, dataType, default, default, testParameters: testParameters);
			await TestType<TType, TType?>(context, dataType, min, max, testParameters: testParameters);
			await TestType<TType, TType?>(context, dataType, max, min, testParameters: testParameters);
		}

		[Test]
		public async ValueTask TestInt8([YdbDataSources] string context)
		{
			// default
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte)), default, default);
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte)), sbyte.MinValue, sbyte.MaxValue);
			await TestType<sbyte, sbyte?>(context, new(typeof(sbyte)), sbyte.MaxValue, sbyte.MinValue);

			// other types: unsigned
			await TestInteger<byte>(context, DataType.SByte, 0, 127);
			await TestInteger<ushort>(context, DataType.SByte, 0, 127);
			await TestInteger<uint>(context, DataType.SByte, 0, 127);
			await TestInteger<ulong>(context, DataType.SByte, 0, 127);

			// other types: signed
			await TestInteger<short>(context, DataType.SByte, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<int>(context, DataType.SByte, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<long>(context, DataType.SByte, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<decimal>(context, DataType.SByte, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<float>(context, DataType.SByte, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<double>(context, DataType.SByte, sbyte.MinValue, sbyte.MaxValue);
		}

		[Test]
		public async ValueTask TestUInt8([YdbDataSources] string context)
		{
			// default
			await TestType<byte, sbyte?>(context, new(typeof(byte)), default, default);
			await TestType<byte, byte?>(context, new(typeof(byte)), byte.MinValue, byte.MaxValue);
			await TestType<byte, byte?>(context, new(typeof(byte)), byte.MaxValue, byte.MinValue);

			// bool
			await TestInteger<bool>(context, DataType.Byte, false, true);

			// other types: unsigned
			await TestInteger<ushort>(context, DataType.Byte, byte.MinValue, byte.MaxValue);
			await TestInteger<uint>(context, DataType.Byte, byte.MinValue, byte.MaxValue);
			await TestInteger<ulong>(context, DataType.Byte, byte.MinValue, byte.MaxValue);

			// other types: signed
			await TestInteger<sbyte>(context, DataType.Byte, 0, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.Byte, byte.MinValue, byte.MaxValue);
			await TestInteger<int>(context, DataType.Byte, byte.MinValue, byte.MaxValue);
			await TestInteger<long>(context, DataType.Byte, byte.MinValue, byte.MaxValue);
			await TestInteger<decimal>(context, DataType.Byte, byte.MinValue, byte.MaxValue);
			await TestInteger<float>(context, DataType.Byte, byte.MinValue, byte.MaxValue);
			await TestInteger<double>(context, DataType.Byte, byte.MinValue, byte.MaxValue);
		}

		[Test]
		public async ValueTask TestInt16([YdbDataSources] string context)
		{
			// default
			await TestType<short, short?>(context, new(typeof(short)), default, default);
			await TestType<short, short?>(context, new(typeof(short)), short.MinValue, short.MaxValue);
			await TestType<short, short?>(context, new(typeof(short)), short.MaxValue, short.MinValue);

			// other types: unsigned
			await TestInteger<byte>(context, DataType.Int16, 0, byte.MaxValue);
			await TestInteger<ushort>(context, DataType.Int16, 0, (ushort)short.MaxValue);
			await TestInteger<uint>(context, DataType.Int16, 0, (uint)short.MaxValue);
			await TestInteger<ulong>(context, DataType.Int16, 0, (ulong)short.MaxValue);

			// other types: signed
			await TestInteger<sbyte>(context, DataType.Int16, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<int>(context, DataType.Int16, short.MinValue, short.MaxValue);
			await TestInteger<long>(context, DataType.Int16, short.MinValue, short.MaxValue);
			await TestInteger<decimal>(context, DataType.Int16, short.MinValue, short.MaxValue);
			await TestInteger<float>(context, DataType.Int16, short.MinValue, short.MaxValue);
			await TestInteger<double>(context, DataType.Int16, short.MinValue, short.MaxValue);
		}

		[Test]
		public async ValueTask TestUInt16([YdbDataSources] string context)
		{
			// default
			await TestType<ushort, ushort?>(context, new(typeof(ushort)), default, default);
			await TestType<ushort, ushort?>(context, new(typeof(ushort)), ushort.MinValue, ushort.MaxValue);
			await TestType<ushort, ushort?>(context, new(typeof(ushort)), ushort.MaxValue, ushort.MinValue);

			// other types: unsigned
			await TestInteger<byte>(context, DataType.UInt16, byte.MinValue, byte.MaxValue);
			await TestInteger<uint>(context, DataType.UInt16, ushort.MinValue, ushort.MaxValue);
			await TestInteger<ulong>(context, DataType.UInt16, ushort.MinValue, ushort.MaxValue);

			// other types: signed
			await TestInteger<sbyte>(context, DataType.UInt16, 0, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.UInt16, 0, short.MaxValue);
			await TestInteger<int>(context, DataType.UInt16, ushort.MinValue, ushort.MaxValue);
			await TestInteger<long>(context, DataType.UInt16, ushort.MinValue, ushort.MaxValue);
			await TestInteger<decimal>(context, DataType.UInt16, ushort.MinValue, ushort.MaxValue);
			await TestInteger<float>(context, DataType.UInt16, ushort.MinValue, ushort.MaxValue);
			await TestInteger<double>(context, DataType.UInt16, ushort.MinValue, ushort.MaxValue);
		}

		[Test]
		public async ValueTask TestInt32([YdbDataSources] string context)
		{
			// default
			await TestType<int, int?>(context, new(typeof(int)), default, default);
			await TestType<int, int?>(context, new(typeof(int)), int.MinValue, int.MaxValue);
			await TestType<int, int?>(context, new(typeof(int)), int.MaxValue, int.MinValue);

			// other types: unsigned
			await TestInteger<byte>(context, DataType.Int32, 0, byte.MaxValue);
			await TestInteger<ushort>(context, DataType.Int32, 0, ushort.MaxValue);
			await TestInteger<uint>(context, DataType.Int32, 0, (uint)int.MaxValue);
			await TestInteger<ulong>(context, DataType.Int32, 0, (ulong)int.MaxValue);

			// other types: signed
			await TestInteger<sbyte>(context, DataType.Int32, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.Int32, short.MinValue, short.MaxValue);
			await TestInteger<long>(context, DataType.Int32, int.MinValue, int.MaxValue);
			await TestInteger<decimal>(context, DataType.Int32, int.MinValue, int.MaxValue);
			await TestInteger<float>(context, DataType.Int32, 16777216, 16777216);
			await TestInteger<double>(context, DataType.Int32, int.MinValue, int.MaxValue);
		}

		[Test]
		public async ValueTask TestUInt32([YdbDataSources] string context)
		{
			// default
			await TestType<uint, uint?>(context, new(typeof(uint)), default, default);
			await TestType<uint, uint?>(context, new(typeof(uint)), uint.MinValue, uint.MaxValue);
			await TestType<uint, uint?>(context, new(typeof(uint)), uint.MaxValue, uint.MinValue);

			// other types: unsigned
			await TestInteger<byte>(context, DataType.UInt32, byte.MinValue, byte.MaxValue);
			await TestInteger<ushort>(context, DataType.UInt32, ushort.MinValue, ushort.MaxValue);
			await TestInteger<ulong>(context, DataType.UInt32, uint.MinValue, uint.MaxValue);

			// other types: signed
			await TestInteger<sbyte>(context, DataType.UInt32, 0, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.UInt32, 0, short.MaxValue);
			await TestInteger<int>(context, DataType.UInt32, 0, int.MaxValue);
			await TestInteger<long>(context, DataType.UInt32, uint.MinValue, uint.MaxValue);
			await TestInteger<decimal>(context, DataType.UInt32, uint.MinValue, uint.MaxValue);
			await TestInteger<float>(context, DataType.UInt32, uint.MinValue, 16777216u);
			await TestInteger<double>(context, DataType.UInt32, uint.MinValue, uint.MaxValue);
		}

		[Test]
		public async ValueTask TestInt64([YdbDataSources] string context)
		{
			// default
			await TestType<long, long?>(context, new(typeof(long)), default, default);
			await TestType<long, long?>(context, new(typeof(long)), long.MinValue, long.MaxValue);
			await TestType<long, long?>(context, new(typeof(long)), long.MaxValue, long.MinValue);

			// other types: unsigned
			await TestInteger<byte>(context, DataType.Int64, 0, byte.MaxValue);
			await TestInteger<ushort>(context, DataType.Int64, 0, ushort.MaxValue);
			await TestInteger<uint>(context, DataType.Int64, 0, uint.MaxValue);
			await TestInteger<ulong>(context, DataType.Int64, 0, (ulong)long.MaxValue);

			// other types: signed
			await TestInteger<sbyte>(context, DataType.Int64, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.Int64, short.MinValue, short.MaxValue);
			await TestInteger<int>(context, DataType.Int64, int.MinValue, int.MaxValue);
			await TestInteger<decimal>(context, DataType.Int64, long.MinValue, long.MaxValue);
			await TestInteger<float>(context, DataType.Int64, -16777216L, 16777216L);
			await TestInteger<double>(context, DataType.Int64, -9007199254740991L, 9007199254740991L);
		}

		[Test]
		public async ValueTask TestUInt64([YdbDataSources] string context)
		{
			// default
			await TestType<ulong, ulong?>(context, new(typeof(ulong)), default, default);
			await TestType<ulong, ulong?>(context, new(typeof(ulong)), ulong.MinValue, ulong.MaxValue);
			await TestType<ulong, ulong?>(context, new(typeof(ulong)), ulong.MaxValue, ulong.MinValue);

			// other types: unsigned
			await TestInteger<byte>(context, DataType.UInt64, byte.MinValue, byte.MaxValue);
			await TestInteger<ushort>(context, DataType.UInt64, ushort.MinValue, ushort.MaxValue);
			await TestInteger<uint>(context, DataType.UInt64, uint.MinValue, uint.MaxValue);

			// other types: signed
			await TestInteger<sbyte>(context, DataType.UInt64, 0, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.UInt64, 0, short.MaxValue);
			await TestInteger<int>(context, DataType.UInt64, 0, int.MaxValue);
			await TestInteger<long>(context, DataType.UInt64, 0, long.MaxValue);
			await TestInteger<decimal>(context, DataType.UInt64, ulong.MinValue, ulong.MaxValue);
			await TestInteger<float>(context, DataType.UInt64, ulong.MinValue, 16777216UL);
			await TestInteger<double>(context, DataType.UInt64, ulong.MinValue, 9007199254740991UL);
		}

		[Test]
		public async ValueTask TestFloat([YdbDataSources] string context)
		{
			// default
			await TestType<float, float?>(context, new(typeof(float)), default, default);
			await TestType<float, float?>(context, new(typeof(float)), float.MinValue, float.MaxValue);
			await TestType<float, float?>(context, new(typeof(float)), float.MaxValue, float.MinValue);
			await TestType<float, float?>(context, new(typeof(float)), float.Epsilon, float.NaN, filterByNullableValue: false);
			await TestType<float, float?>(context, new(typeof(float)), float.NaN, float.Epsilon, filterByValue: false);
			await TestType<float, float?>(context, new(typeof(float)), float.PositiveInfinity, float.NegativeInfinity);
			await TestType<float, float?>(context, new(typeof(float)), float.NegativeInfinity, float.PositiveInfinity);

			// other types: unsigned
			await TestInteger<byte>(context, DataType.Single, 0, byte.MaxValue);
			await TestInteger<ushort>(context, DataType.Single, 0, ushort.MaxValue);
			await TestInteger<uint>(context, DataType.Single, 0, 16777216u);
			await TestInteger<ulong>(context, DataType.Single, 0, 16777216ul);

			// other types: signed
			await TestInteger<sbyte>(context, DataType.Single, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.Single, short.MinValue, short.MaxValue);
			await TestInteger<int>(context, DataType.Single, -16777216, 16777216);
			await TestInteger<long>(context, DataType.Single, -16777216L, 16777216L);
			await TestInteger<decimal>(context, DataType.Single, -16777220m, 16777220m);
			await TestInteger<double>(context, DataType.Single, float.MinValue, float.MaxValue);
		}

		[Test]
		public async ValueTask TestDouble([YdbDataSources] string context)
		{
			// default
			await TestType<double, double?>(context, new(typeof(double)), default, default);
			await TestType<double, double?>(context, new(typeof(double)), double.MinValue, double.MaxValue);
			await TestType<double, double?>(context, new(typeof(double)), double.MaxValue, double.MinValue);
			await TestType<double, double?>(context, new(typeof(double)), double.Epsilon, double.NaN, filterByNullableValue: false);
			await TestType<double, double?>(context, new(typeof(double)), double.NaN, double.Epsilon, filterByValue: false);
			await TestType<double, double?>(context, new(typeof(double)), double.PositiveInfinity, double.NegativeInfinity);
			await TestType<double, double?>(context, new(typeof(double)), double.NegativeInfinity, double.PositiveInfinity);

			// other types: unsigned
			await TestInteger<byte>(context, DataType.Double, 0, byte.MaxValue);
			await TestInteger<ushort>(context, DataType.Double, 0, ushort.MaxValue);
			await TestInteger<uint>(context, DataType.Double, 0, uint.MaxValue);
			await TestInteger<ulong>(context, DataType.Double, 0, 9007199254740991UL);

			// other types: signed
			await TestInteger<sbyte>(context, DataType.Double, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.Double, short.MinValue, short.MaxValue);
			await TestInteger<int>(context, DataType.Double, int.MinValue, int.MaxValue);
			await TestInteger<long>(context, DataType.Double, -9007199254740991L, 9007199254740991L);
			await TestInteger<decimal>(context, DataType.Double, -9007199254740990m, 9007199254740990m);
			await TestInteger<float>(context, DataType.Double, float.MinValue, float.MaxValue);
		}

		[ActiveIssue("https://github.com/ydb-platform/ydb-dotnet-sdk/issues/331")]
		[Test]
		public async ValueTask TestDyNumber([YdbDataSources] string context)
		{
			// no default mappings

			// unsigned
			await TestInteger<byte>(context, DataType.DecFloat, 0, byte.MaxValue);
			await TestInteger<ushort>(context, DataType.DecFloat, 0, ushort.MaxValue);
			await TestInteger<uint>(context, DataType.DecFloat, 0, uint.MaxValue);
			await TestInteger<ulong>(context, DataType.DecFloat, 0, ulong.MaxValue);

			// signed
			await TestInteger<sbyte>(context, DataType.DecFloat, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.DecFloat, short.MinValue, short.MaxValue);
			await TestInteger<int>(context, DataType.DecFloat, int.MinValue, int.MaxValue);
			await TestInteger<long>(context, DataType.DecFloat, long.MinValue, long.MaxValue);
			await TestInteger<decimal>(context, DataType.DecFloat, decimal.MinValue, decimal.MaxValue);
			await TestInteger<float>(context, DataType.DecFloat, float.MinValue, float.MaxValue);
			await TestInteger<double>(context, DataType.DecFloat, double.MinValue, double.MaxValue);

			// string
			var dyNumber1 = "1234567890123.4567890123456789012345678e113";
			var dyNumber2 = "-1234567890123.4567890123456789012345678e113";
			await TestType<string, string?>(context, new(typeof(string), DataType.DecFloat), "", default);
			await TestType<string, string?>(context, new(typeof(string), DataType.DecFloat), "", dyNumber2);
			await TestType<string, string?>(context, new(typeof(string), DataType.DecFloat), dyNumber2, dyNumber1);
		}

		[Test]
		public async ValueTask TestDecimal([YdbDataSources] string context)
		{
			// default mapping
			var defaultMax = 6251426433751.935439503M;
			var defaultMin = -6251426433752.935439503M;

			await TestType<decimal, decimal?>(context, new(typeof(decimal)), default, default);

			await TestType<decimal, decimal?>(context, new(typeof(decimal)), defaultMax, defaultMin);

			var precisions = new int[] { 1, 2, 21, 22, 23, 34, 35 };
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
						maxString = maxString.Substring(0, p - s) + '.' + maxString.Substring(p - s);
					if (maxString[0] == '.')
						maxString = $"0{maxString}";
					var minString = $"-{maxString}";

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

					// another provider bug - cannot read big decimals from reader even as strings - this data already lost due to conversion to decimal
					if (p < 34)
					{
						await TestType<decimal, decimal?>(context, decimalType, default, default);
						await TestType<decimal, decimal?>(context, decimalType, minDecimal, maxDecimal);

						// to use parameters for string mapping we need to implement custom parameter value factory from string
						// while possible - not gonna do it
						var zero = s == 0 ? "0" : $"0.{new string('0', s)}";
						await TestType<string, string?>(context, stringType, "0", default, getExpectedValue: _ => zero);
						await TestType<string, string?>(context, stringType, minString, maxString);

						if (s > 0 && p > s)
							await TestType<string, string?>(context, stringType, "1.2", "-2.1", getExpectedValue: v => v + new string('0', s - 1), getExpectedNullableValue: v => v + new string('0', s - 1));
					}
				}
			}

			// unsigned
			await TestInteger<byte>(context, DataType.Decimal, 0, byte.MaxValue);
			await TestInteger<ushort>(context, DataType.Decimal, 0, ushort.MaxValue);
			await TestInteger<uint>(context, DataType.Decimal, 0, uint.MaxValue);
			await TestInteger<ulong>(context, new DbDataType(typeof(ulong), DataType.Decimal).WithPrecision(20).WithScale(0), 0, ulong.MaxValue);

			// signed
			await TestInteger<sbyte>(context, DataType.Decimal, sbyte.MinValue, sbyte.MaxValue);
			await TestInteger<short>(context, DataType.Decimal, short.MinValue, short.MaxValue);
			await TestInteger<int>(context, DataType.Decimal, int.MinValue, int.MaxValue);
			await TestInteger<long>(context, new DbDataType(typeof(long), DataType.Decimal).WithPrecision(19).WithScale(0), long.MinValue, long.MaxValue);
			await TestInteger<float>(context, new DbDataType(typeof(float), DataType.Decimal).WithPrecision(8).WithScale(0), -16777220L, 16777220L);
			await TestInteger<double>(context, new DbDataType(typeof(double), DataType.Decimal).WithPrecision(16).WithScale(0), -9007199254740990L, 9007199254740990L);
		}

		[Test]
		public async ValueTask TestBytes([YdbDataSources] string context)
		{
			await TestType<byte[], byte[]?>(context, new(typeof(byte[])), Array.Empty<byte>(), default);
			await TestType<byte[], byte[]?>(context, new(typeof(byte[])), new byte[] { 0, 1, 2, 3, 4, 0 }, new byte[] { 1, 2, 3, 4, 0, 0 });

			await TestType<string, string?>(context, new(typeof(string), DataType.VarBinary), string.Empty, default);
			await TestType<string, string?>(context, new(typeof(string), DataType.VarBinary), TestEscapingString, TestEscapingString);

			await TestType<char, char?>(context, new(typeof(char), DataType.VarBinary), default, default);
			await TestType<char, char?>(context, new(typeof(char), DataType.VarBinary), '\0', '1');

			await TestType<string, string?>(context, new(typeof(string), DataType.Binary), string.Empty, default);
			await TestType<string, string?>(context, new(typeof(string), DataType.Binary), TestEscapingString, TestEscapingString);

			await TestType<char, char?>(context, new(typeof(char), DataType.Binary), default, default);
			await TestType<char, char?>(context, new(typeof(char), DataType.Binary), '\x01', '\x03');

			var streamEmpty = new MemoryStream(Array.Empty<byte>());
			var streamData1 = new MemoryStream(new byte[] { 0, 1, 2, 3, 4, 0 });
			var streamData2 = new MemoryStream(new byte[] { 1, 2, 3, 4, 0, 0 });
			await TestType<MemoryStream, MemoryStream?>(context, new(typeof(MemoryStream)), streamEmpty, default);
			await TestType<MemoryStream, MemoryStream?>(context, new(typeof(MemoryStream)), streamData1, streamData2);
		}

		[Test]
		public async ValueTask TestText([YdbDataSources] string context)
		{
			await TestType<string, string?>(context, new(typeof(string)), string.Empty, default);
			await TestType<string, string?>(context, new(typeof(string)), TestEscapingString, TestEscapingString);

			await TestType<char, char?>(context, new(typeof(char)), default, default);
			await TestType<char, char?>(context, new(typeof(char)), '\0', '1');
			await TestType<char, char?>(context, new(typeof(char)), 'ы', '\xFE');
			await TestType<char, char?>(context, new(typeof(char)), '\xFF', '\n');
		}

		[Test]
		public async ValueTask TestYson([YdbDataSources] string context)
		{
			await TestType<string, string?>(context, new(typeof(string), DataType.Yson), "{}", default, filterByValue: false, filterByNullableValue: false);
			await TestType<string, string?>(context, new(typeof(string), DataType.Yson), "null", "null", filterByValue: false, filterByNullableValue: false);
			await TestType<string, string?>(context, new(typeof(string), DataType.Yson), "false", "true", filterByValue: false, filterByNullableValue: false);
			await TestType<string, string?>(context, new(typeof(string), DataType.Yson), "-12.345", "123", filterByValue: false, filterByNullableValue: false);
			await TestType<string, string?>(context, new(typeof(string), DataType.Yson), "{\u0001\u0010тест=\u0001\u0014валью}", "{\u0001\bprop=\u0004}", filterByValue: false, filterByNullableValue: false);

			await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.Yson), Encoding.UTF8.GetBytes("{}"), default, filterByValue: false, filterByNullableValue: false);
			await TestType<byte[], byte[]?>(context, new(typeof(byte[]), DataType.Yson), Encoding.UTF8.GetBytes("{\u0001\bprop=\u0005}"), Encoding.UTF8.GetBytes("{\u0001\u0010тест=\u0001\u0014валью}"), filterByValue: false, filterByNullableValue: false);
		}

		[Test]
		public async ValueTask TestJson([YdbDataSources] string context)
		{
			await TestType<string, string?>(context, new(typeof(string), DataType.Json), "{}", default, filterByValue: false, filterByNullableValue: false);
			await TestType<string, string?>(context, new(typeof(string), DataType.Json), "null", "null", filterByValue: false, filterByNullableValue: false);
			await TestType<string, string?>(context, new(typeof(string), DataType.Json), "false", "true", filterByValue: false, filterByNullableValue: false);
			await TestType<string, string?>(context, new(typeof(string), DataType.Json), "\"test\"", "123", filterByValue: false, filterByNullableValue: false);
			await TestType<string, string?>(context, new(typeof(string), DataType.Json), /*lang=json,strict*/ "{\"ы\": 1.23}", /*lang=json,strict*/ "{\"prop\": false }", filterByValue: false, filterByNullableValue: false);
		}

		[Test]
		public async ValueTask TestJsonDocument([YdbDataSources] string context)
		{
			await TestType<string, string?>(context, new(typeof(string), DataType.BinaryJson), "{}", default, filterByValue: false, filterByNullableValue: false);
			await TestType<string, string?>(context, new(typeof(string), DataType.BinaryJson), "null", "null", filterByValue: false, filterByNullableValue: false);
			await TestType<string, string?>(context, new(typeof(string), DataType.BinaryJson), "false", "true", filterByValue: false, filterByNullableValue: false);
			await TestType<string, string?>(context, new(typeof(string), DataType.BinaryJson), "12.34", "123", filterByValue: false, filterByNullableValue: false);
			await TestType<string, string?>(context, new(typeof(string), DataType.BinaryJson), /*lang=json,strict*/ "{\"ы\":1.23}", /*lang=json,strict*/ "{\"prop\":false}", filterByValue: false, filterByNullableValue: false);
		}

		[Test]
		public async ValueTask TestUUID([YdbDataSources] string context)
		{
			// UUID
			await TestType<Guid, Guid?>(context, new(typeof(Guid)), default, default);
			await TestType<Guid, Guid?>(context, new(typeof(Guid)), TestData.Guid1, TestData.Guid2);
		}

		[Test]
		public async ValueTask TestDate([YdbDataSources] string context)
		{
			var min = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var max = new DateTime(2105, 12, 31, 0, 0, 0, DateTimeKind.Utc);

#if SUPPORTS_DATEONLY
			await TestType<DateOnly, DateOnly?>(context, new(typeof(DateOnly)), DateOnly.FromDateTime(TestData.Date), default);
			await TestType<DateOnly, DateOnly?>(context, new(typeof(DateOnly)), DateOnly.FromDateTime(min), DateOnly.FromDateTime(max));
#endif

			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date), min, max);

			var expectedDtoMin = new DateTimeOffset(min);
			var expectedDtoMax = new DateTimeOffset(max);

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.Date), new DateTimeOffset(max, default), default, getExpectedValue: _ => expectedDtoMax);
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.Date), new DateTimeOffset(min, default), new DateTimeOffset(max, default), getExpectedValue: _ => expectedDtoMin, getExpectedNullableValue: _ => expectedDtoMax);
		}

		[Test]
		public async ValueTask TestDateTime([YdbDataSources] string context)
		{
			// A moment in time in UTC, precision to the second
			// from 00:00 01.01.1970 to 00:00 01.01.2106

			var min = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var max = new DateTime(2105, 12, 31, 0, 0, 0, DateTimeKind.Utc);
			var minWithTime = min.AddSeconds(1);
			var maxWithTime = max.AddSeconds(-1);

			var minU = DateTime.SpecifyKind(min, DateTimeKind.Unspecified);
			var maxU = DateTime.SpecifyKind(max, DateTimeKind.Unspecified);
			var minL = min.ToLocalTime();
			var maxL = max.ToLocalTime();

			var minWithTimeU = DateTime.SpecifyKind(minWithTime, DateTimeKind.Unspecified);
			var maxWithTimeU = DateTime.SpecifyKind(maxWithTime, DateTimeKind.Unspecified);
			var minWithTimeL = minWithTime.ToLocalTime();
			var maxWithTimeL = maxWithTime.ToLocalTime();

			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime), min, max);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime), minU, maxU);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime), minL, maxL, getExpectedValue: _ => min, getExpectedNullableValue: _ => max);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime), minWithTime, maxWithTime);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime), minWithTimeU, maxWithTimeU);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime), minWithTimeL, maxWithTimeL, getExpectedValue: _ => minWithTime, getExpectedNullableValue: _ => maxWithTime);

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.DateTime), new DateTimeOffset(min, default), default);
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.DateTime), new DateTimeOffset(min, default), new DateTimeOffset(max, default));
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.DateTime), new DateTimeOffset(minWithTime, default), new DateTimeOffset(maxWithTime, default));
		}

		[Test]
		public async ValueTask TestTimestamp([YdbDataSources] string context)
		{
			// A moment in time in UTC, precision to the second
			// from 00:00 01.01.1970 to 00:00 01.01.2106

			var min = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var max = new DateTime(2105, 12, 31, 0, 0, 0, DateTimeKind.Utc);
			var minWithTime = min.AddTicks(10);
			var maxWithTime = max.AddTicks(-10);

			var minU = DateTime.SpecifyKind(min, DateTimeKind.Unspecified);
			var maxU = DateTime.SpecifyKind(max, DateTimeKind.Unspecified);
			var minL = min.ToLocalTime();
			var maxL = max.ToLocalTime();

			var minWithTimeU = DateTime.SpecifyKind(minWithTime, DateTimeKind.Unspecified);
			var maxWithTimeU = DateTime.SpecifyKind(maxWithTime, DateTimeKind.Unspecified);
			var minWithTimeL = minWithTime.ToLocalTime();
			var maxWithTimeL = maxWithTime.ToLocalTime();

			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), min, max);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), minU, maxU);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), minL, maxL, getExpectedValue: _ => min, getExpectedNullableValue: _ => max);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), minWithTime, maxWithTime);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), minWithTimeU, maxWithTimeU);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), minWithTimeL, maxWithTimeL, getExpectedValue: _ => minWithTime, getExpectedNullableValue: _ => maxWithTime);

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset)), new DateTimeOffset(min, default), default);
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset)), new DateTimeOffset(min, default), new DateTimeOffset(max, default));
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset)), new DateTimeOffset(minWithTime, default), new DateTimeOffset(maxWithTime, default));

			//var min = new DateTime(1970, 1, 1);
			//var max = new DateTime(2105, 12, 31);

			//var minWithTime = min.AddTicks(10);
			//var maxWithtime = max.AddTicks(-10);

			//await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), TestData.Date, default);
			//await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), min, max);
			//await TestType<DateTime, DateTime?>(context, new(typeof(DateTime)), minWithTime, maxWithtime);

			//var minDto = new DateTimeOffset(min, default);
			//var maxDto = new DateTimeOffset(max, default);

			//await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset)), new DateTimeOffset(TestData.Date, default), default);
			//await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset)), minDto, maxDto);
			//await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset)), maxDto, minDto);
		}

		[Test]
		public async ValueTask TestInterval([YdbDataSources] string context)
		{
			var min = TimeSpan.FromDays(-49673) + TimeSpan.FromTicks(1);
			var max = TimeSpan.FromDays(49673) - TimeSpan.FromTicks(1);

			var minExpected = TimeSpan.FromDays(-49673) + TimeSpan.FromTicks(10);
			var maxExpected = TimeSpan.FromDays(49673) - TimeSpan.FromTicks(10);

			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan)), max, default, getExpectedValue: _ => maxExpected);
			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan)), min, max, getExpectedValue: _ => minExpected, getExpectedNullableValue: _ => maxExpected);
		}

		[Test]
		public async ValueTask TestDate32([YdbDataSources] string context)
		{
			// actual min/max are:
			// from 00:00 01.01.144169 BC to 00:00 01.01.148107 AD
			// but require provider type to support such range
			var min = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc).Date;
			var max = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc).Date;

#if SUPPORTS_DATEONLY
			await TestType<DateOnly, DateOnly?>(context, new(typeof(DateOnly), DataType.Date32), DateOnly.FromDateTime(TestData.Date), default);
			await TestType<DateOnly, DateOnly?>(context, new(typeof(DateOnly), DataType.Date32), DateOnly.FromDateTime(min), DateOnly.FromDateTime(max));
#endif

			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date32), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Date32), min, max);

			// tz adjustments
			min = min.AddDays(1);
			max = max.AddDays(-1);
			var expectedDtoMin = new DateTimeOffset(min);
			var expectedDtoMax = new DateTimeOffset(max);

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.Date32), new DateTimeOffset(max, default), default, getExpectedValue: _ => expectedDtoMax);
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.Date32), new DateTimeOffset(min, default), new DateTimeOffset(max, default), getExpectedValue: _ => expectedDtoMin, getExpectedNullableValue: _ => expectedDtoMax);

			// TODO: we can write values to database, but cannot read it
			// raw
			//var intMin = int.MinValue;
			//var intMax = int.MaxValue;
			//await TestType<int, int?>(context, new(typeof(int), DataType.Date32), default, default, expectedParamCount: 0);
			//await TestType<int, int?>(context, new(typeof(int), DataType.Date32), intMin, intMax, expectedParamCount: 0);
		}

		[Test]
		public async ValueTask TestDatetime64([YdbDataSources] string context)
		{
			// A moment in time in UTC, precision to the second
			// from 00:00 01.01.144169 BC to 00:00 01.01.148107 AD

			var min = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
			var max = DateTime.SpecifyKind(DateTime.MaxValue.AddSeconds(-1).AddTicks(1), DateTimeKind.Utc);

			var minU = DateTime.SpecifyKind(min, DateTimeKind.Unspecified);
			var maxU = DateTime.SpecifyKind(max, DateTimeKind.Unspecified);
			// for local time use 1 day adjustment to avoid timezone offset overflow
			var minL = min.AddDays(1).ToLocalTime();
			var maxL = max.AddDays(-1).ToLocalTime();

			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime64), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime64), min, max);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime64), minU, maxU);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.DateTime64), minL, maxL, getExpectedValue: _ => min.AddDays(1), getExpectedNullableValue: _ => max.AddDays(-1));

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.DateTime64), new DateTimeOffset(min, default), default);
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.DateTime64), new DateTimeOffset(min, default), new DateTimeOffset(max, default));

			// TODO: we can write values to database, but cannot read it
			//// raw
			//var intMin = long.MinValue;
			//var intMax = long.MaxValue;
			//await TestType<long, long?>(context, new(typeof(long), DataType.DateTime64), default, default, expectedParamCount: 0);
			//await TestType<long, long?>(context, new(typeof(long), DataType.DateTime64), intMin, intMax, expectedParamCount: 0);
		}

		[Test]
		public async ValueTask TestTimestamp64([YdbDataSources] string context)
		{
			// A moment in time in UTC, precision to the microsecond
			// from 00:00 01.01.144169 BC to 00:00 01.01.148107 AD

			var min = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
			var max = DateTime.SpecifyKind(DateTime.MaxValue.AddTicks(-9), DateTimeKind.Utc);

			var minU = DateTime.SpecifyKind(min, DateTimeKind.Unspecified);
			var maxU = DateTime.SpecifyKind(max, DateTimeKind.Unspecified);
			// for local time use 1 day adjustment to avoid timezone offset overflow
			var minL = min.AddDays(1).ToLocalTime();
			var maxL = max.AddDays(-1).ToLocalTime();

			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Timestamp64), TestData.Date, default);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Timestamp64), min, max);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Timestamp64), minU, maxU);
			await TestType<DateTime, DateTime?>(context, new(typeof(DateTime), DataType.Timestamp64), minL, maxL, getExpectedValue: _ => min.AddDays(1), getExpectedNullableValue: _ => max.AddDays(-1));

			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.Timestamp64), new DateTimeOffset(min, default), default);
			await TestType<DateTimeOffset, DateTimeOffset?>(context, new(typeof(DateTimeOffset), DataType.Timestamp64), new DateTimeOffset(min, default), new DateTimeOffset(max, default));

			// TODO: we can write values to database, but cannot read it
			//// raw
			//var intMin = long.MinValue;
			//var intMax = long.MaxValue;
			//await TestType<long, long?>(context, new(typeof(long), DataType.Timestamp64), default, default, expectedParamCount: 0);
			//await TestType<long, long?>(context, new(typeof(long), DataType.Timestamp64), intMin, intMax, expectedParamCount: 0);
		}

		[ActiveIssue("https://github.com/ydb-platform/ydb-dotnet-sdk/issues/561")]
		[Test]
		public async ValueTask TestInterval64([YdbDataSources] string context)
		{
			// from -292277 years to +292277 years
			// In YDB Interval in microseconds: round to 10 ticks
			var min = TimeSpan.FromTicks(TimeSpan.MinValue.Ticks + 8);
			var max = TimeSpan.FromTicks(TimeSpan.MaxValue.Ticks - 7);

			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan), DataType.Interval64), max, default);
			await TestType<TimeSpan, TimeSpan?>(context, new(typeof(TimeSpan), DataType.Interval64), min, max);

			// TODO: we can write values to database, but cannot read it
			//// raw. no idea why such min/max used
			var intMin = -9223339708799999999;
			var intMax = 9223339708799999999;
			await TestType<long, long?>(context, new(typeof(long), DataType.Interval64), default, default, expectedParamCount: 0);
			await TestType<long, long?>(context, new(typeof(long), DataType.Interval64), intMin, intMax, expectedParamCount: 0);
		}
	}
}
