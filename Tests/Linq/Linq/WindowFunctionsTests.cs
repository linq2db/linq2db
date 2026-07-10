using System;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

#pragma warning disable NUnit1028

namespace Tests.Linq
{
	[TestFixture]
	public partial class WindowFunctionsTests : TestBase
	{
		public class WindowFunctionTestEntity
		{
			[PrimaryKey]
			public int       Id                    { get; set; }
			public string?   Name                  { get; set; }
			public int       CategoryId            { get; set; }
			public double?   Value                 { get; set; }
			public DateTime? Timestamp             { get; set; }
			public int       IntValue              { get; set; }
			public int?      NullableIntValue      { get; set; }
			[Column(Configuration = ProviderName.Access, DataType = DataType.Int32)]
			public long      LongValue             { get; set; }
			[Column(Configuration = ProviderName.Access, DataType = DataType.Int32)]
			public long?     NullableLongValue     { get; set; }
			public double    DoubleValue           { get; set; }
			public double?   NullableDoubleValue   { get; set; }
			[Column(Configuration = ProviderName.Access, DataType = DataType.Double)]
			public decimal   DecimalValue          { get; set; }
			[Column(Configuration = ProviderName.Access, DataType = DataType.Double)]
			public decimal?  NullableDecimalValue  { get; set; }
			public float     FloatValue            { get; set; }
			public float?    NullableFloatValue    { get; set; }
			[Column(Configuration = ProviderName.Access, DataType = DataType.Int32)]
			public short     ShortValue            { get; set; }
			[Column(Configuration = ProviderName.Access, DataType = DataType.Int32)]
			public short?    NullableShortValue    { get; set; }
			[Column(Configuration = ProviderName.Access, DataType = DataType.Int32)]
			public byte      ByteValue             { get; set; }
			[Column(Configuration = ProviderName.Access, DataType = DataType.Int32)]
			public byte?     NullableByteValue     { get; set; }

			public override string ToString()
			{
				return $"Id: {Id}, Name: {Name}, CategoryId: {CategoryId}, Value: {Value}, Timestamp: {Timestamp}";
			}

			public static WindowFunctionTestEntity[] Seed()
			{
				return
				[
					new WindowFunctionTestEntity
					{
						Id                   = 1,
						Name                 = "Alice",
						CategoryId           = 1,
						Value                = 10.5,
						Timestamp            = new DateTime(2024, 1, 1, 9, 0, 0),
						IntValue             = 10,
						NullableIntValue     = 10,
						LongValue            = 100L,
						NullableLongValue    = 100L,
						DoubleValue          = 10.5,
						NullableDoubleValue  = 10.5,
						DecimalValue         = 10.5M,
						NullableDecimalValue = 10.5M,
						FloatValue           = 10.5f,
						NullableFloatValue   = 10.5f,
						ShortValue           = 10,
						NullableShortValue   = 10,
						ByteValue            = 10,
						NullableByteValue    = 10
					},
					new WindowFunctionTestEntity
					{
						Id                   = 2,
						Name                 = "Bob",
						CategoryId           = 1,
						Value                = 15.0,
						Timestamp            = new DateTime(2024, 1, 1, 10, 0, 0),
						IntValue             = 20,
						NullableIntValue     = 20,
						LongValue            = 200L,
						NullableLongValue    = 200L,
						DoubleValue          = 20.5,
						NullableDoubleValue  = 20.5,
						DecimalValue         = 20.5M,
						NullableDecimalValue = 20.5M,
						FloatValue           = 20.5f,
						NullableFloatValue   = 20.5f,
						ShortValue           = 20,
						NullableShortValue   = 20,
						ByteValue            = 20,
						NullableByteValue    = 20
					},
					new WindowFunctionTestEntity
					{
						Id                   = 3,
						Name                 = "Charlie",
						CategoryId           = 2,
						Value                = 8.0,
						Timestamp            = new DateTime(2024, 1, 2, 11, 0, 0),
						IntValue             = 30,
						NullableIntValue     = 30,
						LongValue            = 300L,
						NullableLongValue    = 300L,
						DoubleValue          = 30.5,
						NullableDoubleValue  = 30.5,
						DecimalValue         = 30.5M,
						NullableDecimalValue = 30.5M,
						FloatValue           = 30.5f,
						NullableFloatValue   = 30.5f,
						ShortValue           = 30,
						NullableShortValue   = 30,
						ByteValue            = 30,
						NullableByteValue    = 30
					},
					new WindowFunctionTestEntity
					{
						Id                   = 4,
						Name                 = "Diana",
						CategoryId           = 2,
						Value                = 12.5,
						Timestamp            = new DateTime(2024, 1, 2, 12, 0, 0),
						IntValue             = 40,
						NullableIntValue     = 40,
						LongValue            = 400L,
						NullableLongValue    = 400L,
						DoubleValue          = 40.5,
						NullableDoubleValue  = 40.5,
						DecimalValue         = 40.5M,
						NullableDecimalValue = 40.5M,
						FloatValue           = 40.5f,
						NullableFloatValue   = 40.5f,
						ShortValue           = 40,
						NullableShortValue   = 40,
						ByteValue            = 40,
						NullableByteValue    = 40
					},
					new WindowFunctionTestEntity
					{
						Id                   = 5,
						Name                 = "Eve",
						CategoryId           = 1,
						Value                = 18.5,
						Timestamp            = new DateTime(2024, 1, 3, 13, 0, 0),
						IntValue             = 50,
						NullableIntValue     = 50,
						LongValue            = 500L,
						NullableLongValue    = 500L,
						DoubleValue          = 50.5,
						NullableDoubleValue  = 50.5,
						DecimalValue         = 50.5M,
						NullableDecimalValue = 50.5M,
						FloatValue           = 50.5f,
						NullableFloatValue   = 50.5f,
						ShortValue           = 50,
						NullableShortValue   = 50,
						ByteValue            = 50,
						NullableByteValue    = 50
					},
					new WindowFunctionTestEntity
					{
						Id                   = 6,
						Name                 = "Frank",
						CategoryId           = 3,
						Value                = 20.0,
						Timestamp            = new DateTime(2024, 1, 3, 14, 0, 0),
						IntValue             = 60,
						NullableIntValue     = 60,
						LongValue            = 600L,
						NullableLongValue    = 600L,
						DoubleValue          = 60.5,
						NullableDoubleValue  = 60.5,
						DecimalValue         = 60.5M,
						NullableDecimalValue = 60.5M,
						FloatValue           = 60.5f,
						NullableFloatValue   = 60.5f,
						ShortValue           = 60,
						NullableShortValue   = 60,
						ByteValue            = 60,
						NullableByteValue    = 60
					},
					new WindowFunctionTestEntity
					{
						Id                   = 7,
						Name                 = "Grace",
						CategoryId           = 3,
						Value                = 25.0,
						Timestamp            = new DateTime(2024, 1, 4, 15, 0, 0),
						IntValue             = 70,
						NullableIntValue     = 70,
						LongValue            = 700L,
						NullableLongValue    = 700L,
						DoubleValue          = 70.5,
						NullableDoubleValue  = 70.5,
						DecimalValue         = 70.5M,
						NullableDecimalValue = 70.5M,
						FloatValue           = 70.5f,
						NullableFloatValue   = 70.5f,
						ShortValue           = 70,
						NullableShortValue   = 70,
						ByteValue            = 70,
						NullableByteValue    = 70
					},
					new WindowFunctionTestEntity
					{
						Id                   = 8,
						Name                 = "Hank",
						CategoryId           = 1,
						Value                = 30.0,
						Timestamp            = new DateTime(2024, 1, 4, 16, 0, 0),
						IntValue             = 80,
						NullableIntValue     = 80,
						LongValue            = 800L,
						NullableLongValue    = 800L,
						DoubleValue          = 80.5,
						NullableDoubleValue  = 80.5,
						DecimalValue         = 80.5M,
						NullableDecimalValue = 80.5M,
						FloatValue           = 80.5f,
						NullableFloatValue   = 80.5f,
						ShortValue           = 80,
						NullableShortValue   = 80,
						ByteValue            = 80,
						NullableByteValue    = 80
					},
					new WindowFunctionTestEntity
					{
						Id                   = 9,
						Name                 = null,
						CategoryId           = 1,
						Value                = null,
						Timestamp            = null,
						IntValue             = 90,
						NullableIntValue     = null,
						LongValue            = 900L,
						NullableLongValue    = null,
						DoubleValue          = 90.5,
						NullableDoubleValue  = null,
						DecimalValue         = 90.5M,
						NullableDecimalValue = null,
						FloatValue           = 90.5f,
						NullableFloatValue   = null,
						ShortValue           = 90,
						NullableShortValue   = null,
						ByteValue            = 90,
						NullableByteValue    = null
					}
				];
			}
		}

		// Expected running (cumulative) population/sample variance over a window PARTITION BY CategoryId
		// ORDER BY Id (default RANGE UNBOUNDED PRECEDING .. CURRENT ROW): the set is every row in the same
		// category with Id <= the current row's Id. Sample variance is undefined (NULL) for a single row.
		internal static double? ExpectedRunningVariance(WindowFunctionTestEntity[] data, int id, bool population)
		{
			var current = System.Array.Find(data, d => d.Id == id)!;
			var values  = new System.Collections.Generic.List<double>();

			foreach (var d in data)
				if (d.CategoryId == current.CategoryId && d.Id <= id)
					values.Add(d.IntValue);

			var n = values.Count;

			if (!population && n < 2)
				return null;

			var mean = 0d;
			foreach (var v in values)
				mean += v;
			mean /= n;

			var sumSq = 0d;
			foreach (var v in values)
				sumSq += (v - mean) * (v - mean);

			return sumSq / (population ? n : n - 1);
		}

		// Asserts a windowed stddev/variance result against the expected running value. The single-row window case
		// (sample variance undefined) is intentionally relaxed: engines disagree on the result — NULL (PostgreSQL,
		// DuckDB), 0 (Oracle, MySQL, SAP HANA, Informix), or NaN (ClickHouse) — and it is not a sample-vs-population
		// discriminator. For windows of 2+ rows the assertion is strict, so a provider that computes a population
		// statistic where the API promises a sample one (e.g. bare STDDEV = STDDEV_POP on MySQL/DB2) fails here.
		internal static void AssertRunningStat(double? actual, double? expectedVariance, bool stdDev)
		{
			if (expectedVariance is null)
			{
				Assert.That(actual is null || double.IsNaN(actual.Value) || System.Math.Abs(actual.Value) < 0.1, Is.True);
				return;
			}

			var expected = stdDev ? System.Math.Sqrt(expectedVariance.Value) : expectedVariance.Value;

			Assert.That(actual, Is.Not.Null);
			Assert.That(actual!.Value, Is.EqualTo(expected).Within(0.1));
		}

		// Expected running FILTERed aggregate over a window PARTITION BY CategoryId ORDER BY Id (default
		// RANGE UNBOUNDED PRECEDING .. CURRENT ROW): the set is every row in the same category with Id <= the
		// current row's Id that also satisfies the FILTER predicate (CategoryId == 1). Because the partition key
		// IS the filter column, the filter is all-true inside the CategoryId==1 partition and all-false elsewhere,
		// so categories 2/3 aggregate over an empty set and the window function returns NULL — a dropped/mangled
		// FILTER (or its CASE-WHEN emulation) would instead return the unfiltered running value here.
		internal static int? ExpectedRunningFilteredSum(WindowFunctionTestEntity[] data, int id)
		{
			var current = System.Array.Find(data, d => d.Id == id)!;
			var sum     = 0;
			var any     = false;

			foreach (var d in data)
				if (d.CategoryId == current.CategoryId && d.Id <= id && d.CategoryId == 1)
				{
					sum += d.IntValue;
					any  = true;
				}

			return any ? sum : (int?)null;
		}

		internal static double? ExpectedRunningFilteredAvg(WindowFunctionTestEntity[] data, int id)
		{
			var current = System.Array.Find(data, d => d.Id == id)!;
			var sum     = 0d;
			var n       = 0;

			foreach (var d in data)
				if (d.CategoryId == current.CategoryId && d.Id <= id && d.CategoryId == 1)
				{
					sum += d.DoubleValue;
					n++;
				}

			return n == 0 ? (double?)null : sum / n;
		}

		// Expected windowed SUM(IntValue) over an explicit frame within a partition (PARTITION BY CategoryId
		// ORDER BY Id). The seed uses unique Id values, so every ORDER BY peer group is a single row: ROWS and
		// GROUPS frames are therefore equivalent, and RANGE frames select by Id value. An empty frame produces
		// SQL NULL, which linq2db materializes into the non-nullable int projection as 0 (verified on the FILTER
		// tests), so the empty case returns 0 here. Boundary kinds: "UP" unbounded preceding, "UF" unbounded
		// following, "CR" current row, "P" <offset> preceding, "F" <offset> following. exclude: "none" | "current"
		// | "group" | "ties" (with unique keys, "group" == "current" and "ties" removes nothing).
		internal static int ExpectedFrameSum(
			WindowFunctionTestEntity[] data, int id, bool range,
			string startKind, int startOffset, string endKind, int endOffset,
			string exclude = "none")
		{
			var current   = System.Array.Find(data, d => d.Id == id)!;
			var partition = new System.Collections.Generic.List<WindowFunctionTestEntity>();

			foreach (var d in data)
				if (d.CategoryId == current.CategoryId)
					partition.Add(d);

			partition.Sort((a, b) => a.Id.CompareTo(b.Id));

			var included = new System.Collections.Generic.List<WindowFunctionTestEntity>();

			if (!range)
			{
				var i  = partition.FindIndex(d => d.Id == id);
				var lo = startKind switch { "UP" => 0, "CR" => i, "P" => i - startOffset, "F" => i + startOffset, _ => 0 };
				var hi = endKind   switch { "UF" => partition.Count - 1, "CR" => i, "P" => i - endOffset, "F" => i + endOffset, _ => partition.Count - 1 };

				if (lo < 0)
					lo = 0;
				if (hi > partition.Count - 1)
					hi = partition.Count - 1;

				for (var k = lo; k <= hi; k++)
					included.Add(partition[k]);
			}
			else
			{
				var loVal = startKind switch { "UP" => int.MinValue, "CR" => id, "P" => id - startOffset, "F" => id + startOffset, _ => int.MinValue };
				var hiVal = endKind   switch { "UF" => int.MaxValue, "CR" => id, "P" => id - endOffset, "F" => id + endOffset, _ => int.MaxValue };

				foreach (var d in partition)
					if (d.Id >= loVal && d.Id <= hiVal)
						included.Add(d);
			}

			if (exclude is "current" or "group")
				included.RemoveAll(d => d.Id == id);

			var sum = 0;
			foreach (var d in included)
				sum += d.IntValue;

			return sum;
		}
	}
}

