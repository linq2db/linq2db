using System;
using System.Collections.Generic;
using System.Data;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands.QueryExecution;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class QueryOutputValueConversionTests
	{
		[Test]
		public void ReadFieldAsStringConvertsKnownTypes()
		{
			var created = new DateTime(2026, 07, 05, 12, 34, 56, DateTimeKind.Unspecified);
			var id      = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
			var table   = new DataTable();

			table.Columns.Add("StringValue",   typeof(string));
			table.Columns.Add("BooleanValue",  typeof(bool));
			table.Columns.Add("Int32Value",    typeof(int));
			table.Columns.Add("Int64Value",    typeof(long));
			table.Columns.Add("DecimalValue",  typeof(decimal));
			table.Columns.Add("DoubleValue",   typeof(double));
			table.Columns.Add("DateTimeValue", typeof(DateTime));
			table.Columns.Add("TimeSpanValue", typeof(TimeSpan));
			table.Columns.Add("GuidValue",     typeof(Guid));
			table.Columns.Add("BytesValue",    typeof(byte[]));
			table.Columns.Add("TimeOnlyValue", typeof(TimeOnly));
			table.Columns.Add("NullValue",     typeof(string));

			table.Rows.Add("text", true, 42, 42000000000L, 123.45m, 1.25d, created, new TimeSpan(12, 34, 56), id, new byte[] { 1, 2, 3 }, new TimeOnly(3, 4, 5, 123), DBNull.Value);

			using var reader = table.CreateDataReader();

			(reader.Read()).ShouldBe(true);

			using (Assert.EnterMultipleScope())
			{
				ReadFieldAsString(reader, "None",      0). ShouldBe("text");
				ReadFieldAsString(reader, "Boolean",   1). ShouldBe("true");
				ReadFieldAsString(reader, "None",      2). ShouldBe("42");
				ReadFieldAsString(reader, "None",      3). ShouldBe("42000000000");
				ReadFieldAsString(reader, "None",      4). ShouldBe("123.45");
				ReadFieldAsString(reader, "Double",    5). ShouldBe("1.25");
				ReadFieldAsString(reader, "Date",      6). ShouldBe("2026-07-05");
				ReadFieldAsString(reader, "DateTime",  6). ShouldBe("2026-07-05T12:34:56.0000000");
				ReadFieldAsString(reader, "TimeSpan",  7). ShouldBe("12:34:56");
				ReadFieldAsString(reader, "None",      8). ShouldBe("01234567-89ab-cdef-0123-456789abcdef");
				ReadFieldAsString(reader, "Bytes",     9). ShouldBe("0x010203");
				ReadFieldAsString(reader, "ByteArray", 9). ShouldBe("[1,2,3]");
				ReadFieldAsString(reader, "None",      10).ShouldBe("03:04:05.1230000");

				reader.IsDBNull(11).ShouldBe(true);
			}
		}

		[Test]
		public void BoundedFormatterStopsSequenceEnumerationAtUtf8Limit()
		{
			var enumerated = 0;

			var formatted = QueryValueFormatter.TryFormat(
				GetValues(),
				"Array(String)",
				QueryValueFormatter.QueryActualFieldType.None,
				10,
				out var value);

			using (Assert.EnterMultipleScope())
			{
				formatted. ShouldBe(false);
				value.     ShouldBeNull();
				enumerated.ShouldBeLessThan(5);
			}

			IEnumerable<object> GetValues()
			{
				for (var i = 0; i < 1000; i++)
				{
					enumerated++;
					yield return "abcd";
				}
			}
		}

		[TestCase(new object?[] { null, 5    }, "[,5]")]
		[TestCase(new object?[] { "",   "a" }, "[,a]")]
		public void SequenceKeepsSeparatorForElementsFormattingToNothing(object?[] sequence, string expected)
		{
			var table = new DataTable();

			table.Columns.Add("SequenceValue", typeof(object));

			var row = table.NewRow();
			row[0] = sequence;
			table.Rows.Add(row);

			using var reader = table.CreateDataReader();

			(reader.Read()).ShouldBe(true);

			var unbounded = ReadFieldAsString(reader, "None", 0);
			var bounded   = QueryValueFormatter.TryFormat(sequence, "Array", QueryValueFormatter.QueryActualFieldType.None, 64, out var boundedValue);

			using (Assert.EnterMultipleScope())
			{
				unbounded.   ShouldBe(expected);
				bounded.     ShouldBe(true);
				boundedValue.ShouldBe(expected);
			}
		}

		[Test]
		public void NestedScalarsUseTheSameFormatAsTopLevelValues()
		{
			var sequence = new object?[]
			{
				true,
				new DateOnly(2024, 1, 2),
				new TimeOnly(3, 4, 5, 123),
				new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Unspecified),
			};

			const string expected = "[true,2024-01-02,03:04:05.1230000,2024-01-02T03:04:05.0000000]";

			var table = new DataTable();

			table.Columns.Add("SequenceValue", typeof(object));

			var row = table.NewRow();
			row[0] = sequence;
			table.Rows.Add(row);

			using var reader = table.CreateDataReader();

			(reader.Read()).ShouldBe(true);

			var unbounded = ReadFieldAsString(reader, "None", 0);
			var bounded   = QueryValueFormatter.TryFormat(sequence, "Array", QueryValueFormatter.QueryActualFieldType.None, 128, out var boundedValue);

			using (Assert.EnterMultipleScope())
			{
				unbounded.   ShouldBe(expected);
				bounded.     ShouldBe(true);
				boundedValue.ShouldBe(expected);
			}
		}

		[Test]
		public void BoundedFormatterFormatsNestedValuesWithinUtf8Limit()
		{
			var formatted = QueryValueFormatter.TryFormat(
				new object[] { new[] { 1, 2 }, (3, 4), "é" },
				"Array",
				QueryValueFormatter.QueryActualFieldType.None,
				64,
				out var value);

			using (Assert.EnterMultipleScope())
			{
				formatted.ShouldBe(true);
				value.    ShouldBe("[[1,2],(3,4),é]");
			}
		}

		static string? ReadFieldAsString(DataTableReader reader, string actualFieldTypeName, int ordinal)
		{
			var actualFieldType = Enum.Parse<QueryValueFormatter.QueryActualFieldType>(actualFieldTypeName);

			return QueryExecutionExecutor.ReadFieldAsString(reader, actualFieldType, ordinal);
		}
	}
}
