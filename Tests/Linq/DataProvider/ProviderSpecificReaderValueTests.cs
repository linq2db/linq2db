#if !NETFRAMEWORK

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Runtime.CompilerServices;

using ClickHouse.Driver.Numerics;
using LinqToDB;
using LinqToDB.Data;

using Microsoft.Data.SqlTypes;
using Microsoft.SqlServer.Types;

using NUnit.Framework;
using Oracle.ManagedDataAccess.Types;

namespace Tests.DataProvider
{
	[TestFixture]
	public class ProviderSpecificReaderValueTests : DataProviderTestBase
	{
		[Test]
		public void SqlServerProviderSpecificReadMatrix([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "Cast(1 as bit)"                                      , typeof(SqlBoolean), typeof(bool)    , "true" );
				AssertReadMatrix(conn, "Cast(255 as tinyint)"                                , typeof(SqlByte)   , typeof(byte)    , "255"  );
				AssertReadMatrix(conn, "Cast(32767 as smallint)"                             , typeof(SqlInt16)  , typeof(short)   , "32767");
				AssertReadMatrix(conn, "Cast(2147483647 as int)"                             , typeof(SqlInt32)  , typeof(int)     , "2147483647");
				AssertReadMatrix(conn, "Cast(9223372036854775807 as bigint)"                 , typeof(SqlInt64)  , typeof(long)    , "9223372036854775807");
				AssertReadMatrix(conn, "Cast(1.25 as real)"                                  , typeof(SqlSingle) , typeof(float)   , "1.25" );
				AssertReadMatrix(conn, "Cast(1.25 as float)"                                 , typeof(SqlDouble) , typeof(double)  , "1.25" );
				AssertReadMatrix(conn, "Cast(12.34 as money)"                                , typeof(SqlMoney)  , typeof(decimal) , "12.34", "12.3400");
				AssertReadMatrix(conn, "Cast('6F9619FF-8B86-D011-B42D-00C04FC964FF' as uniqueidentifier)", typeof(SqlGuid), typeof(Guid), "6f9619ff-8b86-d011-b42d-00c04fc964ff");
				AssertReadMatrix(conn, "Cast(N'text' as nvarchar(10))"                       , typeof(SqlString) , typeof(string)  , "text" );
				AssertReadMatrix(conn, "Cast(0x010203 as varbinary(3))"                      , typeof(SqlBinary) , typeof(byte[])  , "0x010203" );
				AssertReadMatrix(conn, "Cast('<root />' as xml)"                             , typeof(SqlXml)    , typeof(string)  , "<root />");
				AssertReadMatrix(conn, "Cast('2026-07-05T12:34:56' as datetime)"             , typeof(SqlDateTime), typeof(DateTime), "2026-07-05T12:34:56.0000000");
				AssertProviderSpecificRequired(conn, "Cast(1.222222222222222222222222222222 as decimal(31,30))", typeof(SqlDecimal), "1.222222222222222222222222222222");
			}
		}

		[Test]
		public void SqlServerProviderSpecificReadMatrix2008Plus([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "Cast('2026-07-05' as date)"                         , typeof(DateTime), typeof(DateTime), "2026-07-05");
				AssertReadMatrix(conn, "Cast('2026-07-05T12:34:56.1234567' as datetime2(7))", typeof(DateTime), typeof(DateTime), "2026-07-05T12:34:56.1234567");
				AssertReadMatrix(conn, "Cast('12:34:56.1234567' as time(7))"                , typeof(TimeSpan), typeof(TimeSpan), "12:34:56.1234567");
				AssertReadMatrix(conn, "Cast('2026-07-05T12:34:56.1234567+03:00' as datetimeoffset(7))", typeof(DateTimeOffset), typeof(DateTimeOffset), "2026-07-05T12:34:56.1234567+03:00");

				if (context.IsAnyOf(TestProvName.AllSqlServerMS))
				{
					AssertBothReadsFail(conn, "Cast('/1/3/' as hierarchyid)", "Microsoft.SqlServer.Server.InvalidUdtException");
				}
				else
				{
					AssertReadMatrix(conn, "Cast('/1/3/' as hierarchyid)", typeof(SqlHierarchyId), typeof(SqlHierarchyId), "/1/3/");
				}
			}
		}

		[Test]
		public void SqlServerVectorProviderSpecificReadMatrix([IncludeDataSources(TestProvName.AllSqlServer2025PlusMS)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "Cast('[1.0,2.0,3.0]' as vector(3))", typeof(SqlVector<float>), typeof(SqlVector<float>), "[1,2,3]");
			}
		}

		[Test]
		public void SqlServerSpatialProviderSpecificReadMatrix([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			if (context.IsAnyOf(TestProvName.AllSqlServerMS))
				Assert.Inconclusive("Spatial types test disabled for Microsoft.Data.SqlClient");

			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "geometry::STGeomFromText('LINESTRING (100 100, 20 180, 180 180)', 0)",            typeof(SqlGeometry),  typeof(SqlGeometry) , "LINESTRING (100 100, 20 180, 180 180)");
				AssertReadMatrix(conn, "geography::STGeomFromText('LINESTRING(-122.360 47.656, -122.343 47.656)', 4326)", typeof(SqlGeography), typeof(SqlGeography), "LINESTRING (-122.36 47.656, -122.343 47.656)");
			}
		}

		[Test]
		public void OracleManagedProviderSpecificReadMatrix([IncludeDataSources(TestProvName.AllOracleManaged)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "hextoraw('3039')"                                      , typeof(OracleBinary)     , typeof(byte[])        , "0x3039");
				AssertReadMatrix(conn, "to_blob('3039')"                                      , typeof(OracleBlob)       , typeof(byte[])        , "0x3039");
				AssertReadMatrix(conn, "to_clob('hello, csv')"                                , typeof(OracleClob)       , typeof(string)        , "hello, csv");
				AssertReadMatrix(conn, "to_nclob(N'привет')"                                  , typeof(OracleClob)       , typeof(string)        , "привет");
				AssertReadMatrix(conn, "xmltype('<root><v>1</v></root>')"                     , typeof(OracleXmlType)    , typeof(string)        , "<root><v>1</v></root>");
				AssertReadMatrix(conn, "date '2024-01-02'"                                   , typeof(OracleDate)       , typeof(DateTime)      , "2024-01-02");
				AssertReadMatrix(conn, "timestamp '2024-01-02 03:04:05.123456'"               , typeof(OracleTimeStamp)  , typeof(DateTime)      , "2024-01-02T03:04:05.123456000");
				AssertReadMatrix(conn, "timestamp '2024-01-02 03:04:05.123456 -05:00'"        , typeof(OracleTimeStampTZ), typeof(DateTimeOffset), "2024-01-02T03:04:05.123456000-05:00", "2024-01-02T03:04:05.1234560-05:00");
				AssertReadMatrix(conn, "interval '1-2' year to month"                         , typeof(OracleIntervalYM) , typeof(long)          , "+01-02");
				AssertReadMatrix(conn, "interval '3 04:05:06.789' day to second"              , typeof(OracleIntervalDS) , typeof(TimeSpan)      , "+03 04:05:06.789000", "3.04:05:06.7890000");
				AssertReadMatrix(conn, "bfilename('DATA_PUMP_DIR', 'missing.bin')"            , typeof(OracleBFile)      , typeof(byte[])        , "<BFILE>", providerSpecificOnly: true);
			}
		}

		[Test]
		public void ClickHouseOctonicaProviderSpecificReadMatrix([IncludeDataSources(ProviderName.ClickHouseOctonica)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "toDate('2024-01-02')"                          , typeof(DateOnly)                 , typeof(DateOnly)                 , "2024-01-02");
				AssertReadMatrix(conn, "toDateTime('2024-01-02 00:00:00')"             , typeof(DateTimeOffset)           , typeof(DateTimeOffset)           , "2024-01-02T00:00:00.0000000+00:00");
				AssertReadMatrix(conn, "[1, 2, 3]"                                     , typeof(byte[])                   , typeof(byte[])                   , "[1,2,3]");
				AssertReadMatrix(conn, "tuple(1, 'a')"                                 , typeof(Tuple<byte, string>)      , typeof(Tuple<byte, string>)      , "(1,a)");
				AssertReadMatrix(conn, "map('a', 1, 'b', 2)"                           , typeof(KeyValuePair<string, byte>[]), typeof(KeyValuePair<string, byte>[]), "{a:1,b:2}");
				AssertReadMatrix(conn, "toUUID('01234567-89ab-cdef-0123-456789abcdef')", typeof(Guid)                     , typeof(Guid)                     , "01234567-89ab-cdef-0123-456789abcdef");
			}
		}

		[Test]
		public void ClickHouseDriverProviderSpecificReadMatrix([IncludeDataSources(ProviderName.ClickHouseDriver)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "toDate('2024-01-02')"                          , typeof(DateTime)                , typeof(DateTime)                , "2024-01-02");
				AssertReadMatrix(conn, "toDateTime('2024-01-02 00:00:00')"             , typeof(DateTime)                , typeof(DateTime)                , "2024-01-02T00:00:00.0000000");
				AssertReadMatrix(conn, "[1, 2, 3]"                                     , typeof(byte[])                  , typeof(byte[])                  , "[1,2,3]");
				AssertReadMatrix(conn, "tuple(1, 'a')"                                 , typeof(Tuple<byte, byte[]>)     , typeof(Tuple<byte, byte[]>)     , "(1,0x61)");
				AssertReadMatrix(conn, "map('a', 1, 'b', 2)"                           , typeof(Dictionary<byte[], byte>), typeof(Dictionary<byte[], byte>), "{0x61:1,0x62:2}");
				AssertReadMatrix(conn, "toDecimal256('1234567890123456789012345678901234567890.12', 2)", typeof(ClickHouseDecimal), typeof(ClickHouseDecimal), "1234567890123456789012345678901234567890.12");
			}
		}

		[Test]
		public void ClickHouseMySqlProviderSpecificReadMatrix([IncludeDataSources(ProviderName.ClickHouseMySql)] string context)
		{
			using var conn = GetDataConnection(context);

			using (Assert.EnterMultipleScope())
			{
				AssertReadMatrix(conn, "toDate('2024-01-02')"                          , typeof(DateTime), typeof(DateTime), "2024-01-02");
				AssertReadMatrix(conn, "toDateTime('2024-01-02 00:00:00')"             , typeof(DateTime), typeof(DateTime), "2024-01-02T00:00:00.0000000");
				AssertReadMatrix(conn, "[1, 2, 3]"                                     , typeof(string)  , typeof(string)  , "[1,2,3]");
				AssertReadMatrix(conn, "tuple(1, 'a')"                                 , typeof(string)  , typeof(string)  , "(1,'a')");
				AssertReadMatrix(conn, "map('a', 1, 'b', 2)"                           , typeof(string)  , typeof(string)  , "{'a':1,'b':2}");
				AssertReadMatrix(conn, "toUUID('01234567-89ab-cdef-0123-456789abcdef')", typeof(string)  , typeof(string)  , "01234567-89ab-cdef-0123-456789abcdef");
			}
		}

		void AssertReadMatrix(DataConnection conn, string sqlExpression, Type expectedProviderSpecificType, Type expectedGetValueType, string expectedString, bool providerSpecificOnly = false)
		{
			AssertReadMatrix(conn, sqlExpression, expectedProviderSpecificType, expectedGetValueType, expectedString, expectedString, providerSpecificOnly);
		}

		void AssertReadMatrix(DataConnection conn, string sqlExpression, Type expectedProviderSpecificType, Type expectedGetValueType, string expectedProviderSpecificString, string expectedGetValueString, bool providerSpecificOnly = false)
		{
			var providerSpecific = ReadValue(conn, sqlExpression, providerSpecific: true);
			var getValue         = ReadValue(conn, sqlExpression, providerSpecific: false);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(providerSpecific.ExceptionTypeName, Is.Null, sqlExpression);
				Assert.That(providerSpecific.Type, Is.EqualTo(expectedProviderSpecificType), sqlExpression);
				Assert.That(providerSpecific.StringValue, Is.EqualTo(expectedProviderSpecificString), sqlExpression);

				if (!providerSpecificOnly)
				{
					Assert.That(getValue.ExceptionTypeName, Is.Null, sqlExpression);
					Assert.That(getValue.Type, Is.EqualTo(expectedGetValueType), sqlExpression);
					Assert.That(getValue.StringValue, Is.EqualTo(expectedGetValueString), sqlExpression);
				}
			}
		}

		void AssertProviderSpecificRequired(DataConnection conn, string sqlExpression, Type expectedProviderSpecificType, string expectedString)
		{
			var providerSpecific = ReadValue(conn, sqlExpression, providerSpecific: true);
			var getValue         = ReadValue(conn, sqlExpression, providerSpecific: false);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(providerSpecific.ExceptionTypeName, Is.Null, sqlExpression);
				Assert.That(providerSpecific.Type, Is.EqualTo(expectedProviderSpecificType), sqlExpression);
				Assert.That(providerSpecific.StringValue, Is.EqualTo(expectedString), sqlExpression);
				Assert.That(getValue.ExceptionTypeName, Is.Not.Null, sqlExpression);
			}
		}

		void AssertBothReadsFail(DataConnection conn, string sqlExpression, string expectedExceptionTypeName)
		{
			var providerSpecific = ReadValue(conn, sqlExpression, providerSpecific: true);
			var getValue         = ReadValue(conn, sqlExpression, providerSpecific: false);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(providerSpecific.ExceptionTypeName, Is.EqualTo(expectedExceptionTypeName), sqlExpression);
				Assert.That(getValue.ExceptionTypeName, Is.EqualTo(expectedExceptionTypeName), sqlExpression);
			}
		}

		ReadResult ReadValue(DataConnection conn, string sqlExpression, bool providerSpecific)
		{
			using var result = conn.ExecuteReader("SELECT " + sqlExpression);
			var reader       = result.Reader ?? throw new InvalidOperationException("Reader is not available.");

			Assert.That(reader.Read(), Is.True);

			try
			{
				var dataTypeName = reader.GetDataTypeName(0);
				var value        = providerSpecific ? reader.GetProviderSpecificValue(0) : reader.GetValue(0);

				return new ReadResult(value.GetType(), value.GetType().FullName, ConvertValueToString(value, dataTypeName), null);
			}
			catch (Exception exception)
			{
				return new ReadResult(null, null, null, exception.GetType().FullName);
			}
		}

		static string? ConvertValueToString(object value, string dataTypeName)
		{
			return value switch
			{
				SqlBoolean sqlBoolean         => sqlBoolean.Value ? "true" : "false",
				bool boolValue                => boolValue ? "true" : "false",
				SqlSingle sqlSingle           => sqlSingle.Value.ToString("R", CultureInfo.InvariantCulture),
				float singleValue             => singleValue.ToString("R", CultureInfo.InvariantCulture),
				SqlDouble sqlDouble           => sqlDouble.Value.ToString("R", CultureInfo.InvariantCulture),
				double doubleValue            => doubleValue.ToString("R", CultureInfo.InvariantCulture),
				string stringValue            => stringValue,
				OracleDate oracleDate         => FormatDate(oracleDate.Value),
				OracleTimeStamp timestamp     => FormatOracleTimeStamp(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, timestamp.Second, timestamp.Nanosecond),
				OracleTimeStampTZ timestamp   => FormatOracleTimeStamp(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, timestamp.Second, timestamp.Nanosecond) + timestamp.TimeZone,
				OracleTimeStampLTZ timestamp  => FormatOracleTimeStamp(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, timestamp.Second, timestamp.Nanosecond),
				SqlDateTime sqlDateTime       => sqlDateTime.Value.ToString("O", CultureInfo.InvariantCulture),
				DateOnly dateOnly             => dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				DateTime dateTime             => IsDateDataType(dataTypeName) ? FormatDate(dateTime) : dateTime.ToString("O", CultureInfo.InvariantCulture),
				DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
				TimeSpan timeSpan             => timeSpan.ToString("c", CultureInfo.InvariantCulture),
				SqlGuid sqlGuid               => sqlGuid.Value.ToString("D"),
				Guid guid                     => guid.ToString("D"),
				SqlBinary sqlBinary           => ConvertBytesToString(sqlBinary.Value),
				OracleBinary oracleBinary     => ConvertBytesToString(oracleBinary.Value),
				OracleBlob oracleBlob         => ConvertBytesToString(oracleBlob.Value),
				OracleClob oracleClob         => oracleClob.Value,
				OracleXmlType oracleXmlType   => oracleXmlType.Value,
				OracleBFile                   => "<BFILE>",
				byte[] bytes                  => dataTypeName.StartsWith("Array(", StringComparison.OrdinalIgnoreCase) ? ConvertByteArrayToString(bytes) : ConvertBytesToString(bytes),
				SqlXml sqlXml                 => sqlXml.Value,
				SqlVector<float> vector       => ConvertVectorToString(vector.Memory.ToArray()),
				SqlVector<Half> vector        => ConvertVectorToString(vector.Memory.ToArray()),
				ITuple tuple                  => ConvertTupleToString(tuple),
				IEnumerable sequence          => ConvertSequenceToString(sequence),
				_                             => Convert.ToString(value, CultureInfo.InvariantCulture),
			};
		}

		static bool IsDateDataType(string dataTypeName)
		{
			return string.Equals(dataTypeName, "Date", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(dataTypeName, "Date32", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(dataTypeName, "Nullable(Date)", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(dataTypeName, "Nullable(Date32)", StringComparison.OrdinalIgnoreCase);
		}

		static string FormatDate(DateTime value)
		{
			return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		static string FormatOracleTimeStamp(int year, int month, int day, int hour, int minute, int second, int nanosecond)
		{
			return string.Create(CultureInfo.InvariantCulture, $"{year:D4}-{month:D2}-{day:D2}T{hour:D2}:{minute:D2}:{second:D2}.{nanosecond:D9}");
		}

		static string ConvertBytesToString(byte[] bytes)
		{
			return "0x" + Convert.ToHexString(bytes);
		}

		static string ConvertByteArrayToString(byte[] bytes)
		{
			var builder = new System.Text.StringBuilder();

			builder.Append('[');
			for (var i = 0; i < bytes.Length; i++)
			{
				if (i > 0)
					builder.Append(',');

				builder.Append(bytes[i].ToString(CultureInfo.InvariantCulture));
			}

			builder.Append(']');
			return builder.ToString();
		}

		static string ConvertTupleToString(ITuple tuple)
		{
			var builder = new System.Text.StringBuilder();

			builder.Append('(');
			for (var i = 0; i < tuple.Length; i++)
			{
				if (i > 0)
					builder.Append(',');

				builder.Append(ConvertNestedValueToString(tuple[i]));
			}

			builder.Append(')');
			return builder.ToString();
		}

		static string ConvertSequenceToString(IEnumerable sequence)
		{
			var builder      = new System.Text.StringBuilder();
			var first        = true;
			var map          = false;
			var openBracket  = '[';
			var closeBracket = ']';

			foreach (var item in sequence)
			{
				if (first)
				{
					map = IsKeyValuePair(item);

					if (map)
					{
						openBracket  = '{';
						closeBracket = '}';
					}

					builder.Append(openBracket);
					first = false;
				}

				if (builder.Length > 1)
					builder.Append(',');

				if (map)
					AppendKeyValuePair(builder, item);
				else
					builder.Append(ConvertNestedValueToString(item));
			}

			if (first)
				builder.Append(openBracket);

			builder.Append(closeBracket);
			return builder.ToString();
		}

		static string? ConvertNestedValueToString(object? value)
		{
			return value switch
			{
				null               => null,
				string stringValue => stringValue,
				byte[] bytes       => ConvertBytesToString(bytes),
				ITuple tuple       => ConvertTupleToString(tuple),
				IEnumerable items  => ConvertSequenceToString(items),
				_                  => Convert.ToString(value, CultureInfo.InvariantCulture),
			};
		}

		static bool IsKeyValuePair(object? value)
		{
			return value != null
				&& value.GetType().IsGenericType
				&& value.GetType().GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
		}

		static void AppendKeyValuePair(System.Text.StringBuilder builder, object? value)
		{
			if (value == null)
			{
				builder.Append(':');
				return;
			}

			var type = value.GetType();
			var key  = type.GetProperty("Key")!.GetValue(value);
			var item = type.GetProperty("Value")!.GetValue(value);

			builder.Append(ConvertNestedValueToString(key));
			builder.Append(':');
			builder.Append(ConvertNestedValueToString(item));
		}

		static string ConvertVectorToString<T>(T[] vector)
		{
			var builder = new System.Text.StringBuilder();

			builder.Append('[');
			for (var i = 0; i < vector.Length; i++)
			{
				if (i > 0)
					builder.Append(',');

				builder.Append(Convert.ToString(vector[i], CultureInfo.InvariantCulture));
			}

			builder.Append(']');
			return builder.ToString();
		}

		sealed record ReadResult(Type? Type, string? TypeName, string? StringValue, string? ExceptionTypeName);
	}
}

#endif
