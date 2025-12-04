using System;
using System.Data.Linq;
using System.Globalization;

using LinqToDB.Internal.Mapping;

namespace LinqToDB.Internal.Remote
{
	/// <summary>
	/// Mapping schema, that defines remote context values de-/serialization converters.
	/// Contains mappings between basic data types and <see cref="string"/>.
	/// </summary>
	sealed class SerializationMappingSchema : LockedMappingSchema
	{
		public static readonly SerializationMappingSchema Instance = new();

		private SerializationMappingSchema()
			: base("RemoteContextSerialization")
		{
			SetConvertExpression<bool          , string>(value => value ? "1" : "0");
			SetConvertExpression<int           , string>(value => value.ToString(CultureInfo.InvariantCulture));
			SetConvertExpression<byte          , string>(value => value.ToString(CultureInfo.InvariantCulture));
			SetConvertExpression<sbyte         , string>(value => value.ToString(CultureInfo.InvariantCulture));
			SetConvertExpression<long          , string>(value => value.ToString(CultureInfo.InvariantCulture));
			SetConvertExpression<short         , string>(value => value.ToString(CultureInfo.InvariantCulture));
			SetConvertExpression<ushort        , string>(value => value.ToString(CultureInfo.InvariantCulture));
			SetConvertExpression<uint          , string>(value => value.ToString(CultureInfo.InvariantCulture));
			SetConvertExpression<ulong         , string>(value => value.ToString(CultureInfo.InvariantCulture));
			SetConvertExpression<decimal       , string>(value => value.ToString(CultureInfo.InvariantCulture));
			SetConvertExpression<double        , string>(value => value.ToString("G17", CultureInfo.InvariantCulture));
			SetConvertExpression<float         , string>(value => value.ToString("G9", CultureInfo.InvariantCulture));
			SetConvertExpression<char          , string>(value => value.ToString(CultureInfo.InvariantCulture));
			SetConvertExpression<DateTime      , string>(value => value.ToBinary().ToString(CultureInfo.InvariantCulture));
			SetConvertExpression<DateTimeOffset, string>(value => $"{value.Ticks.ToString(CultureInfo.InvariantCulture)}:{value.Offset.Ticks.ToString(CultureInfo.InvariantCulture)}");
#if SUPPORTS_DATEONLY
			SetConvertExpression<DateOnly      , string>(value => value.DayNumber.ToString(CultureInfo.InvariantCulture));
#endif
			SetConvertExpression<Guid          , string>(value => value.ToString("N"));
			SetConvertExpression<TimeSpan      , string>(value => value.Ticks.ToString(CultureInfo.InvariantCulture));
			SetConvertExpression<Binary        , string>(value => Convert.ToBase64String(value.ToArray()));
			SetConvertExpression<byte[]        , string>(value => Convert.ToBase64String(value));
			SetConvertExpression<float[]       , string>(value => ConvertToString(value));
#if NET8_0_OR_GREATER
			SetConvertExpression<Half[]        , string>(value => ConvertToString(value));
#endif

			SetConvertExpression<string, bool          >(value => value == "1");
			SetConvertExpression<string, int           >(value => int.Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, byte          >(value => byte.Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, sbyte         >(value => sbyte.Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, long          >(value => long.Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, short         >(value => short.Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, ushort        >(value => ushort.Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, uint          >(value => uint.Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, ulong         >(value => ulong.Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, decimal       >(value => decimal.Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, double        >(value => double.Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, float         >(value => float.Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, char          >(value => value[0]);
			SetConvertExpression<string, DateTime      >(value => DateTime.FromBinary(long.Parse(value, CultureInfo.InvariantCulture)));
			SetConvertExpression<string, DateTimeOffset>(value => StringToDateTimeOffset(value));
#if SUPPORTS_DATEONLY
			SetConvertExpression<string, DateOnly      >(value => DateOnly.FromDayNumber(int.Parse(value, CultureInfo.InvariantCulture)));
#endif
			SetConvertExpression<string, Guid          >(value => Guid.Parse(value));
			SetConvertExpression<string, TimeSpan      >(value => TimeSpan.FromTicks(long.Parse(value, CultureInfo.InvariantCulture)));
			SetConvertExpression<string, Binary        >(value => new Binary(Convert.FromBase64String(value)));
			SetConvertExpression<string, byte[]        >(value => Convert.FromBase64String(value));
			SetConvertExpression<string, float[]       >(value => ReadSingleVector(value));
#if NET8_0_OR_GREATER
			SetConvertExpression<string, Half[]        >(value => ReadHalfVector(value));
#endif
		}

		// DTO serialized as two fields to preserve offset information
		private static DateTimeOffset StringToDateTimeOffset(string data)
		{
			var parts = data.Split(':');
			return new DateTimeOffset(long.Parse(parts[0], CultureInfo.InvariantCulture), TimeSpan.FromTicks(long.Parse(parts[1], CultureInfo.InvariantCulture)));
		}

		// TODO: in future implement generic collection converters
		private static string ConvertToString(float[] vector)
		{
			var arr = new byte[vector.Length * 4];
			Buffer.BlockCopy(vector, 0, arr, 0, arr.Length);
			return Convert.ToBase64String(arr);
		}

		private static float[] ReadSingleVector(string value)
		{
			var arr = Convert.FromBase64String(value);

			var result = new float[arr.Length / 4];
			Buffer.BlockCopy(arr, 0, result, 0, arr.Length);
			return result;
		}

#if NET8_0_OR_GREATER
		private static string ConvertToString(Half[] vector)
		{
			var arr = new byte[vector.Length * 2];
			Buffer.BlockCopy(vector, 0, arr, 0, arr.Length);
			return Convert.ToBase64String(arr);
		}

		private static Half[] ReadHalfVector(string value)
		{
			var arr = Convert.FromBase64String(value);

			var result = new Half[arr.Length / 2];
			Buffer.BlockCopy(arr, 0, result, 0, arr.Length);
			return result;
		}
#endif
	}
}
