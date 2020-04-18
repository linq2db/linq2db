namespace LinqToDB.ServiceModel
{
	using LinqToDB.Common;
	using Mapping;
	using System;
	using System.Data.Linq;
	using System.Globalization;

	/// <summary>
	/// Mapping schema, that defines remote context values de-/serialization converters.
	/// Contains mappings between basic data types and <see cref="string"/>.
	/// </summary>
	internal class SerializationMappingSchema : MappingSchema
	{
		public SerializationMappingSchema()
			: this(null)
		{
		}

		public SerializationMappingSchema(MappingSchema? mappingSchema)
			: base("RemoteContextSerialization", mappingSchema == null ? Array<MappingSchema>.Empty : new[] { mappingSchema })
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
			SetConvertExpression<Guid          , string>(value => value.ToString("N"));
			SetConvertExpression<TimeSpan      , string>(value => value.Ticks.ToString(CultureInfo.InvariantCulture));
			SetConvertExpression<Binary        , string>(value => Convert.ToBase64String(value.ToArray()));
			SetConvertExpression<byte[]        , string>(value => Convert.ToBase64String(value));

			SetConvertExpression<string, bool          >(value => value == "1");
			SetConvertExpression<string, int           >(value => int     .Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, byte          >(value => byte    .Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, sbyte         >(value => sbyte   .Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, long          >(value => long    .Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, short         >(value => short   .Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, ushort        >(value => ushort  .Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, uint          >(value => uint    .Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, ulong         >(value => ulong   .Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, decimal       >(value => decimal .Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, double        >(value => double  .Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, float         >(value => float   .Parse(value, CultureInfo.InvariantCulture));
			SetConvertExpression<string, char          >(value => value[0]);
			SetConvertExpression<string, DateTime      >(value => DateTime.FromBinary(long.Parse(value, CultureInfo.InvariantCulture)));
			SetConvertExpression<string, DateTimeOffset>(value => StringToDateTimeOffset(value));
			SetConvertExpression<string, Guid          >(value => Guid    .Parse(value));
			SetConvertExpression<string, TimeSpan      >(value => TimeSpan.FromTicks(long.Parse(value, CultureInfo.InvariantCulture)));
			SetConvertExpression<string, Binary        >(value => new Binary(Convert.FromBase64String(value)));
			SetConvertExpression<string, byte[]        >(value => Convert.FromBase64String(value));
		}

		// DTO serialized as two fields to preserve offset information
		private static DateTimeOffset StringToDateTimeOffset(string data)
		{
			var parts = data.Split(':');
			return new DateTimeOffset(long.Parse(parts[0], CultureInfo.InvariantCulture), TimeSpan.FromTicks(long.Parse(parts[1], CultureInfo.InvariantCulture)));
		}
	}
}
