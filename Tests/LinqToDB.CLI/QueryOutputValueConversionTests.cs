using System;
using System.Data;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands.QueryExecution;

using NUnit.Framework;

using Shouldly;

#nullable enable annotations
#nullable disable warnings

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

			{
				(ReadFieldAsString(reader, "None",      0)).ShouldBe("text");
				(ReadFieldAsString(reader, "Boolean",   1)).ShouldBe("true");
				(ReadFieldAsString(reader, "None",      2)).ShouldBe("42");
				(ReadFieldAsString(reader, "None",      3)).ShouldBe("42000000000");
				(ReadFieldAsString(reader, "None",      4)).ShouldBe("123.45");
				(ReadFieldAsString(reader, "Double",    5)).ShouldBe("1.25");
				(ReadFieldAsString(reader, "Date",      6)).ShouldBe("2026-07-05");
				(ReadFieldAsString(reader, "DateTime",  6)).ShouldBe("2026-07-05T12:34:56.0000000");
				(ReadFieldAsString(reader, "TimeSpan",  7)).ShouldBe("12:34:56");
				(ReadFieldAsString(reader, "None",      8)).ShouldBe("01234567-89ab-cdef-0123-456789abcdef");
				(ReadFieldAsString(reader, "Bytes",     9)).ShouldBe("0x010203");
				(ReadFieldAsString(reader, "ByteArray", 9)).ShouldBe("[1,2,3]");
				(ReadFieldAsString(reader, "None",      10)).ShouldBe("03:04:05.1230000");

				(reader.IsDBNull(11)).ShouldBe(true);
			}
		}

		static string? ReadFieldAsString(DataTableReader reader, string actualFieldTypeName, int ordinal)
		{
			var actualFieldType = Enum.Parse<QueryExecutionExecutor.QueryActualFieldType>(actualFieldTypeName);

			return QueryExecutionExecutor.ReadFieldAsString(reader, actualFieldType, ordinal);
		}
	}
}
