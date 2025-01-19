using System;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public partial class WindowFunctionsTests : TestBase
	{
		public class WindowFunctionTestEntity
		{
			public int Id { get; set; }
			public string? Name { get; set; }
			public int CategoryId { get; set; }
			public double? Value { get; set; }
			public DateTime? Timestamp { get; set; }
			public int IntValue { get; set; }
			public int? NullableIntValue { get; set; }
			public long LongValue { get; set; }
			public long? NullableLongValue { get; set; }
			public double DoubleValue { get; set; }
			public double? NullableDoubleValue { get; set; }
			public decimal DecimalValue { get; set; }
			public decimal? NullableDecimalValue { get; set; }
			public float FloatValue { get; set; }
			public float? NullableFloatValue { get; set; }
			public short ShortValue { get; set; }
			public short? NullableShortValue { get; set; }
			public byte ByteValue { get; set; }
			public byte? NullableByteValue { get; set; }

			public override string ToString()
			{
				return $"Id: {Id}, Name: {Name}, CategoryId: {CategoryId}, Value: {Value}, Timestamp: {Timestamp}";
			}

			public static WindowFunctionTestEntity[] Seed()
			{
				return
				[
					new WindowFunctionTestEntity { Id = 1, Name = "Alice", CategoryId = 1, Value = 10.5, Timestamp = new DateTime(2024, 1, 1, 9, 0, 0), IntValue = 10, NullableIntValue = 10, LongValue = 100L, NullableLongValue = 100L, DoubleValue = 10.5, NullableDoubleValue = 10.5, DecimalValue = 10.5M, NullableDecimalValue = 10.5M, FloatValue = 10.5f, NullableFloatValue = 10.5f, ShortValue = 10, NullableShortValue = 10, ByteValue = 10, NullableByteValue = 10 },
					new WindowFunctionTestEntity { Id = 2, Name = "Bob", CategoryId = 1, Value = 15.0, Timestamp = new DateTime(2024, 1, 1, 10, 0, 0), IntValue = 20, NullableIntValue = 20, LongValue = 200L, NullableLongValue = 200L, DoubleValue = 20.5, NullableDoubleValue = 20.5, DecimalValue = 20.5M, NullableDecimalValue = 20.5M, FloatValue = 20.5f, NullableFloatValue = 20.5f, ShortValue = 20, NullableShortValue = 20, ByteValue = 20, NullableByteValue = 20 },
					new WindowFunctionTestEntity { Id = 3, Name = "Charlie", CategoryId = 2, Value = 8.0, Timestamp = new DateTime(2024, 1, 2, 11, 0, 0), IntValue = 30, NullableIntValue = 30, LongValue = 300L, NullableLongValue = 300L, DoubleValue = 30.5, NullableDoubleValue = 30.5, DecimalValue = 30.5M, NullableDecimalValue = 30.5M, FloatValue = 30.5f, NullableFloatValue = 30.5f, ShortValue = 30, NullableShortValue = 30, ByteValue = 30, NullableByteValue = 30 },
					new WindowFunctionTestEntity { Id = 4, Name = "Diana", CategoryId = 2, Value = 12.5, Timestamp = new DateTime(2024, 1, 2, 12, 0, 0), IntValue = 40, NullableIntValue = 40, LongValue = 400L, NullableLongValue = 400L, DoubleValue = 40.5, NullableDoubleValue = 40.5, DecimalValue = 40.5M, NullableDecimalValue = 40.5M, FloatValue = 40.5f, NullableFloatValue = 40.5f, ShortValue = 40, NullableShortValue = 40, ByteValue = 40, NullableByteValue = 40 },
					new WindowFunctionTestEntity { Id = 5, Name = "Eve", CategoryId = 1, Value = 18.5, Timestamp = new DateTime(2024, 1, 3, 13, 0, 0), IntValue = 50, NullableIntValue = 50, LongValue = 500L, NullableLongValue = 500L, DoubleValue = 50.5, NullableDoubleValue = 50.5, DecimalValue = 50.5M, NullableDecimalValue = 50.5M, FloatValue = 50.5f, NullableFloatValue = 50.5f, ShortValue = 50, NullableShortValue = 50, ByteValue = 50, NullableByteValue = 50 },
					new WindowFunctionTestEntity { Id = 6, Name = "Frank", CategoryId = 3, Value = 20.0, Timestamp = new DateTime(2024, 1, 3, 14, 0, 0), IntValue = 60, NullableIntValue = 60, LongValue = 600L, NullableLongValue = 600L, DoubleValue = 60.5, NullableDoubleValue = 60.5, DecimalValue = 60.5M, NullableDecimalValue = 60.5M, FloatValue = 60.5f, NullableFloatValue = 60.5f, ShortValue = 60, NullableShortValue = 60, ByteValue = 60, NullableByteValue = 60 },
					new WindowFunctionTestEntity { Id = 7, Name = "Grace", CategoryId = 3, Value = 25.0, Timestamp = new DateTime(2024, 1, 4, 15, 0, 0), IntValue = 70, NullableIntValue = 70, LongValue = 700L, NullableLongValue = 700L, DoubleValue = 70.5, NullableDoubleValue = 70.5, DecimalValue = 70.5M, NullableDecimalValue = 70.5M, FloatValue = 70.5f, NullableFloatValue = 70.5f, ShortValue = 70, NullableShortValue = 70, ByteValue = 70, NullableByteValue = 70 },
					new WindowFunctionTestEntity { Id = 8, Name = "Hank", CategoryId = 1, Value = 30.0, Timestamp = new DateTime(2024, 1, 4, 16, 0, 0), IntValue = 80, NullableIntValue = 80, LongValue = 800L, NullableLongValue = 800L, DoubleValue = 80.5, NullableDoubleValue = 80.5, DecimalValue = 80.5M, NullableDecimalValue = 80.5M, FloatValue = 80.5f, NullableFloatValue = 80.5f, ShortValue = 80, NullableShortValue = 80, ByteValue = 80, NullableByteValue = 80 },
					new WindowFunctionTestEntity { Id = 9, Name = null, CategoryId = 1, Value = null, Timestamp = null, IntValue = 90, NullableIntValue = null, LongValue = 900L, NullableLongValue = null, DoubleValue = 90.5, NullableDoubleValue = null, DecimalValue = 90.5M, NullableDecimalValue = null, FloatValue = 90.5f, NullableFloatValue = null, ShortValue = 90, NullableShortValue = null, ByteValue = 90, NullableByteValue = null }
				];
			}
		}
	}
}

