using System;
using System.Data;
using System.Reflection;

using LinqToDB.CommandLine;

using NUnit.Framework;

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

			Assert.That(reader.Read(), Is.True);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(ReadFieldAsString(reader, "None",      0), Is.EqualTo("text"));
				Assert.That(ReadFieldAsString(reader, "Boolean",   1), Is.EqualTo("true"));
				Assert.That(ReadFieldAsString(reader, "None",      2), Is.EqualTo("42"));
				Assert.That(ReadFieldAsString(reader, "None",      3), Is.EqualTo("42000000000"));
				Assert.That(ReadFieldAsString(reader, "None",      4), Is.EqualTo("123.45"));
				Assert.That(ReadFieldAsString(reader, "Double",    5), Is.EqualTo("1.25"));
				Assert.That(ReadFieldAsString(reader, "Date",      6), Is.EqualTo("2026-07-05"));
				Assert.That(ReadFieldAsString(reader, "DateTime",  6), Is.EqualTo("2026-07-05T12:34:56.0000000"));
				Assert.That(ReadFieldAsString(reader, "TimeSpan",  7), Is.EqualTo("12:34:56"));
				Assert.That(ReadFieldAsString(reader, "None",      8), Is.EqualTo("01234567-89ab-cdef-0123-456789abcdef"));
				Assert.That(ReadFieldAsString(reader, "Bytes",     9), Is.EqualTo("0x010203"));
				Assert.That(ReadFieldAsString(reader, "ByteArray", 9), Is.EqualTo("[1,2,3]"));
				Assert.That(ReadFieldAsString(reader, "None",      10), Is.EqualTo("03:04:05.1230000"));

				Assert.That(reader.IsDBNull(11), Is.True);
			}
		}

		static string? ReadFieldAsString(DataTableReader reader, string actualFieldTypeName, int ordinal)
		{
			var enumType = typeof(QueryCommandExecutor).GetNestedType("QueryActualFieldType", BindingFlags.NonPublic)
				?? throw new MissingMemberException(nameof(QueryCommandExecutor), "QueryActualFieldType");
			var actualFieldType = Enum.Parse(enumType, actualFieldTypeName);
			var method = typeof(QueryCommandExecutor).GetMethod("ReadFieldAsString", BindingFlags.NonPublic | BindingFlags.Static)
				?? throw new MissingMethodException(nameof(QueryCommandExecutor), "ReadFieldAsString");

			return (string?)method.Invoke(null, new[] { reader, actualFieldType, ordinal });
		}
	}
}
