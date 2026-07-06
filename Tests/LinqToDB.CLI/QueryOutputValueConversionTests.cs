using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Data.SqlTypes;

using LinqToDB.CommandLine;

using Microsoft.SqlServer.Types;

using NUnit.Framework;

using Oracle.ManagedDataAccess.Types;

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

			table.Columns.Add("StringValue", typeof(string));
			table.Columns.Add("BooleanValue", typeof(bool));
			table.Columns.Add("Int32Value", typeof(int));
			table.Columns.Add("Int64Value", typeof(long));
			table.Columns.Add("DecimalValue", typeof(decimal));
			table.Columns.Add("DoubleValue", typeof(double));
			table.Columns.Add("DateTimeValue", typeof(DateTime));
			table.Columns.Add("TimeSpanValue", typeof(TimeSpan));
			table.Columns.Add("GuidValue", typeof(Guid));
			table.Columns.Add("BytesValue", typeof(byte[]));
			table.Columns.Add("NullValue", typeof(string));
			table.Rows.Add("text", true, 42, 42000000000L, 123.45m, 1.25d, created, new TimeSpan(12, 34, 56), id, new byte[] { 1, 2, 3 }, DBNull.Value);

			using var reader = table.CreateDataReader();

			Assert.That(reader.Read(), Is.True);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(ReadFieldAsString(reader, "None", 0), Is.EqualTo("text"));
				Assert.That(ReadFieldAsString(reader, "Boolean", 1), Is.EqualTo("true"));
				Assert.That(ReadFieldAsString(reader, "None", 2), Is.EqualTo("42"));
				Assert.That(ReadFieldAsString(reader, "None", 3), Is.EqualTo("42000000000"));
				Assert.That(ReadFieldAsString(reader, "None", 4), Is.EqualTo("123.45"));
				Assert.That(ReadFieldAsString(reader, "Double", 5), Is.EqualTo("1.25"));
				Assert.That(ReadFieldAsString(reader, "DateTime", 6), Is.EqualTo("2026-07-05T12:34:56.0000000"));
				Assert.That(ReadFieldAsString(reader, "TimeSpan", 7), Is.EqualTo("12:34:56"));
				Assert.That(ReadFieldAsString(reader, "None", 8), Is.EqualTo("01234567-89ab-cdef-0123-456789abcdef"));
				Assert.That(ReadFieldAsString(reader, "Bytes", 9), Is.EqualTo("0x010203"));
				Assert.That(reader.IsDBNull(10), Is.True);
			}
		}

		[Test]
		public void ReadFieldAsStringConvertsProviderSpecificBinaryTypes()
		{
			var table = new DataTable();

			table.Columns.Add("SqlBinaryValue", typeof(SqlBinary));
			table.Columns.Add("SqlBytesValue",  typeof(SqlBytes));
			table.Rows.Add(new SqlBinary([1, 2, 3]), new SqlBytes([4, 5, 6]));

			using var reader = table.CreateDataReader();

			Assert.That(reader.Read(), Is.True);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(ReadFieldAsString(reader, "SqlBinary", 0), Is.EqualTo("0x010203"));
				Assert.That(ReadFieldAsString(reader, "SqlBytes",  1), Is.EqualTo("0x040506"));
			}
		}

		[Test]
		public void ReadFieldAsStringConvertsSqlServerUdtTypes()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(ReadSingleValue(SqlHierarchyId.Parse("/1/2/3/"), "SqlHierarchyId"), Is.EqualTo("/1/2/3/"));
				Assert.That(ReadSingleValue(SqlGeometry.STGeomFromText(new SqlChars("POINT (1 2)"), 0), "SqlGeometry"), Is.EqualTo("POINT (1 2)"));
				Assert.That(ReadSingleValue(SqlGeography.STGeomFromText(new SqlChars("POINT(-122.34900 47.65100)"), 4326), "SqlGeography"), Is.EqualTo("POINT (-122.349 47.651)"));
			}
		}

		[Test]
		public void ReadFieldAsStringConvertsOracleProviderSpecificTypes()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(ReadSingleValue(new OracleBinary([0x30, 0x39]), "OracleBinary"), Is.EqualTo("0x3039"));
				Assert.That(ReadSingleValue(new OracleDate(2024, 1, 2, 3, 4, 5), "OracleDate"), Is.EqualTo("2024-01-02T03:04:05"));
				Assert.That(ReadSingleValue(new OracleTimeStamp(2024, 1, 2, 3, 4, 5, 123456000), "OracleTimeStamp"), Is.EqualTo("2024-01-02T03:04:05.123456000"));
				Assert.That(ReadSingleValue(new OracleTimeStampTZ(2024, 1, 2, 3, 4, 5, 123456000, "-05:00"), "OracleTimeStampTZ"), Is.EqualTo("2024-01-02T03:04:05.123456000-05:00"));
				Assert.That(ReadSingleValue(new OracleTimeStampLTZ(2024, 1, 2, 3, 4, 5, 123456000), "OracleTimeStampLTZ"), Is.EqualTo("2024-01-02T03:04:05.123456000"));
			}
		}

		[Test]
		public void ReadFieldAsStringUsesProviderSpecificValueByDefault()
		{
			using var reader = new SingleValueDataReader(
				new ProviderSpecificStringValue("provider-specific"),
				typeof(ProviderSpecificStringValue),
				typeof(string),
				"clr-value");

			Assert.That(reader.Read(), Is.True);

			Assert.That(ReadFieldAsString(reader, "None", 0), Is.EqualTo("provider-specific"));
		}

		[Test]
		public void ReadFieldAsStringFallsBackToGetValueWhenProviderSpecificReadFails()
		{
			using var reader = new SingleValueDataReader(
				new ProviderSpecificStringValue("provider-specific"),
				typeof(ProviderSpecificStringValue),
				typeof(string),
				"clr-value",
				providerSpecificException: new InvalidOperationException());

			Assert.That(reader.Read(), Is.True);

			Assert.That(ReadFieldAsString(reader, "None", 0), Is.EqualTo("clr-value"));
		}

		static string? ReadFieldAsString(DbDataReader reader, string actualFieldTypeName, int ordinal)
		{
			var enumType = typeof(QueryCommandExecutor).GetNestedType("QueryActualFieldType", BindingFlags.NonPublic)
				?? throw new MissingMemberException(nameof(QueryCommandExecutor), "QueryActualFieldType");
			var actualFieldType = Enum.Parse(enumType, actualFieldTypeName);
			var method = typeof(QueryCommandExecutor).GetMethod("ReadFieldAsString", BindingFlags.NonPublic | BindingFlags.Static)
				?? throw new MissingMethodException(nameof(QueryCommandExecutor), "ReadFieldAsString");

			return (string?)method.Invoke(null, new[] { reader, actualFieldType, ordinal });
		}

		static string? ReadSingleValue(object value, string actualFieldTypeName)
		{
			using var reader = new SingleValueDataReader(value, value.GetType(), value.GetType());

			Assert.That(reader.Read(), Is.True);

			return ReadFieldAsString(reader, actualFieldTypeName, 0);
		}

		sealed class SingleValueDataReader : DbDataReader
		{
			readonly object _providerSpecificValue;
			readonly Type   _providerSpecificType;
			readonly Type   _fieldType;
			readonly object _value;
			readonly bool   _throwOnGetValue;
			readonly Exception? _providerSpecificException;
			bool _read;

			public SingleValueDataReader(object providerSpecificValue, Type providerSpecificType, Type fieldType, object? value = null, bool throwOnGetValue = false, Exception? providerSpecificException = null)
			{
				_providerSpecificValue     = providerSpecificValue;
				_providerSpecificType      = providerSpecificType;
				_fieldType                 = fieldType;
				_value                     = value ?? providerSpecificValue;
				_throwOnGetValue           = throwOnGetValue;
				_providerSpecificException = providerSpecificException;
			}

			public override int FieldCount => 1;

			public override bool HasRows => true;

			public override bool IsClosed => false;

			public override int RecordsAffected => 0;

			public override int Depth => 0;

			public override object this[int ordinal] => GetValue(ordinal);

			public override object this[string name] => GetValue(GetOrdinal(name));

			public override bool Read()
			{
				if (_read)
					return false;

				_read = true;
				return true;
			}

			public override bool NextResult()
			{
				return false;
			}

			public override string GetName(int ordinal)
			{
				return "Value";
			}

			public override int GetOrdinal(string name)
			{
				return string.Equals(name, "Value", StringComparison.Ordinal) ? 0 : -1;
			}

			public override string GetDataTypeName(int ordinal)
			{
				return "decimal";
			}

			public override Type GetFieldType(int ordinal)
			{
				return _fieldType;
			}

			public override Type GetProviderSpecificFieldType(int ordinal)
			{
				return _providerSpecificType;
			}

			public override object GetProviderSpecificValue(int ordinal)
			{
				if (_providerSpecificException is not null)
					throw _providerSpecificException;

				return _providerSpecificValue;
			}

			public override object GetValue(int ordinal)
			{
				if (_throwOnGetValue)
					throw new OverflowException("GetValue should not be used for provider-specific conversion.");

				return _value;
			}

			public override int GetValues(object[] values)
			{
				values[0] = _value;
				return 1;
			}

			public override bool IsDBNull(int ordinal)
			{
				return false;
			}

			public override bool GetBoolean(int ordinal) => throw new NotSupportedException();
			public override byte GetByte(int ordinal) => throw new NotSupportedException();
			public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
			public override char GetChar(int ordinal) => throw new NotSupportedException();
			public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
			public override Guid GetGuid(int ordinal) => throw new NotSupportedException();
			public override short GetInt16(int ordinal) => throw new NotSupportedException();
			public override int GetInt32(int ordinal) => throw new NotSupportedException();
			public override long GetInt64(int ordinal) => throw new NotSupportedException();
			public override float GetFloat(int ordinal) => throw new NotSupportedException();
			public override double GetDouble(int ordinal) => throw new NotSupportedException();
			public override string GetString(int ordinal) => throw new NotSupportedException();
			public override decimal GetDecimal(int ordinal) => throw new OverflowException("GetDecimal should not be used for SqlDecimal conversion.");
			public override DateTime GetDateTime(int ordinal) => throw new NotSupportedException();
			public override System.Collections.IEnumerator GetEnumerator() => throw new NotSupportedException();
		}

		sealed class ProviderSpecificStringValue(string value)
		{
			public override string ToString()
			{
				return value;
			}
		}
	}
}
