using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using DuckDB.NET.Native;

using FirebirdSql.Data.Types;

using LinqToDB.Internal.Common;

using Microsoft.Data.SqlTypes;
using Microsoft.SqlServer.Types;

using Oracle.ManagedDataAccess.Types;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// Formats provider-specific values for query output.
	/// </summary>
	public static class QueryValueFormatter
	{
		/// <summary>
		/// Provider-specific field value conversion mode.
		/// </summary>
		public enum QueryActualFieldType
		{
			None = 0,
			Boolean,
			Double,
			Single,
			Date,
			DateTime,
			DateTimeOffset,
			TimeSpan,
			Guid,
			Bytes,
			ByteArray,
			SqlBinary,
			SqlBytes,
			SqlChars,
			SqlString,
			SqlXml,
			SqlVectorFloat,
			SqlVectorHalf,
			SqlHierarchyId,
			SqlGeometry,
			SqlGeography,
			OracleBinary,
			OracleBlob,
			OracleBFile,
			OracleClob,
			OracleXmlType,
			OracleDate,
			OracleTimeStamp,
			OracleTimeStampTZ,
			OracleTimeStampLTZ,
			DB2Binary,
			DB2Blob,
			DB2Clob,
			DB2Date,
			DB2Time,
			DB2TimeStamp,
			DB2Xml,
			MySqlDecimal,
			FirebirdDecFloat,
			FirebirdZonedDateTime,
			FirebirdZonedTime,
			NpgsqlRange,
		}

		internal const string OracleBFilePlaceholder = "<BFILE>";

		internal static string? Format(object? value, string dataTypeName, QueryActualFieldType actualFieldType = QueryActualFieldType.None)
		{
			if (actualFieldType == QueryActualFieldType.Date)
				return FormatDate(value);

			if (value is byte[] bytes)
			{
				return actualFieldType == QueryActualFieldType.ByteArray
					|| actualFieldType == QueryActualFieldType.None && dataTypeName.StartsWith("Array(", StringComparison.OrdinalIgnoreCase)
					? ConvertByteArrayToString(bytes)
					: ConvertBytesToString(bytes);
			}

			return value switch
			{
				null                          => null,
				DBNull                        => null,
				SqlBoolean sqlBoolean         => sqlBoolean.Value ? "true" : "false",
				bool boolValue                => boolValue ? "true" : "false",
				SqlSingle sqlSingle           => sqlSingle.Value.ToString("R", CultureInfo.InvariantCulture),
				float singleValue             => singleValue.ToString("R", CultureInfo.InvariantCulture),
				SqlDouble sqlDouble           => sqlDouble.Value.ToString("R", CultureInfo.InvariantCulture),
				double doubleValue            => doubleValue.ToString("R", CultureInfo.InvariantCulture),
				string stringValue            => stringValue,
				SqlDateTime sqlDateTime       => sqlDateTime.Value.ToString("O", CultureInfo.InvariantCulture),
				DateOnly dateOnly             => dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				TimeOnly timeOnly             => timeOnly.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
				DateTime dateTime             => IsDateDataType(dataTypeName) ? FormatDate(dateTime) : dateTime.ToString("O", CultureInfo.InvariantCulture),
				DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
				TimeSpan timeSpan             => timeSpan.ToString("c", CultureInfo.InvariantCulture),
				SqlGuid sqlGuid               => sqlGuid.Value.ToString("D"),
				Guid guid                     => guid.ToString("D"),
				SqlBinary sqlBinary           => ConvertBytesToString(sqlBinary.Value),
				SqlBytes sqlBytes             => ConvertBytesToString(sqlBytes.Value),
				SqlChars sqlChars             => new string(sqlChars.Value),
				SqlString sqlString           => sqlString.Value,
				SqlXml sqlXml                 => sqlXml.Value,
				SqlVector<float> vector       => ConvertVectorToString(vector.Memory.ToArray()),
				SqlVector<Half> vector        => ConvertVectorToString(vector.Memory.ToArray()),
				SqlHierarchyId hierarchyId    => hierarchyId.ToString(),
				SqlGeometry geometry          => geometry.ToString(),
				SqlGeography geography        => geography.ToString(),
				OracleBinary oracleBinary     => ConvertBytesToString(oracleBinary.Value),
				OracleBlob oracleBlob         => ConvertBytesToString(oracleBlob.Value),
				OracleBFile                   => OracleBFilePlaceholder,
				OracleClob oracleClob         => oracleClob.Value,
				OracleXmlType oracleXmlType   => oracleXmlType.Value,
				OracleDate oracleDate         => FormatDate(oracleDate.Value),
				OracleTimeStamp timestamp     => FormatOracleTimeStamp(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, timestamp.Second, timestamp.Nanosecond),
				OracleTimeStampTZ timestamp   => FormatOracleTimeStamp(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, timestamp.Second, timestamp.Nanosecond) + timestamp.TimeZone,
				OracleTimeStampLTZ timestamp  => FormatOracleTimeStamp(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, timestamp.Second, timestamp.Nanosecond),
				FbDecFloat decFloat           => FormatFirebirdDecFloat(decFloat),
				FbZonedDateTime zonedDateTime => FormatFirebirdZonedDateTime(zonedDateTime),
				FbZonedTime zonedTime         => FormatFirebirdZonedTime(zonedTime),
				DuckDBDateOnly date           => FormatDuckDBDateOnly(date),
				DuckDBTimeOnly time           => FormatDuckDBTimeOnly(time),
				DuckDBTimestamp timestamp     => FormatDuckDBTimestamp(timestamp),
				_ when IsNpgsqlRange(value)   => FormatNpgsqlRange(value),
				_ when IsDB2Value(value)      => FormatDB2Value(value),
				Stream stream                 => ConvertStreamToString(stream),
				ITuple tuple                  => ConvertTupleToString(tuple),
				IEnumerable sequence          => ConvertSequenceToString(sequence),
				_                             => Convert.ToString(value, CultureInfo.InvariantCulture),
			};

			static bool IsDateDataType(string dataTypeName)
			{
				return string.Equals(dataTypeName, "Date",             StringComparison.OrdinalIgnoreCase)
					|| string.Equals(dataTypeName, "Date32",           StringComparison.OrdinalIgnoreCase)
					|| string.Equals(dataTypeName, "Nullable(Date)",   StringComparison.OrdinalIgnoreCase)
					|| string.Equals(dataTypeName, "Nullable(Date32)", StringComparison.OrdinalIgnoreCase);
			}

			static string? FormatDate(object? value)
			{
				return value switch
				{
					null              => null,
					DateOnly date     => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
					DateTime dateTime => dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
					_                 => Convert.ToString(value, CultureInfo.InvariantCulture),
				};
			}

			static string FormatOracleTimeStamp(int year, int month, int day, int hour, int minute, int second, int nanosecond)
			{
				return string.Create(CultureInfo.InvariantCulture, $"{year:D4}-{month:D2}-{day:D2}T{hour:D2}:{minute:D2}:{second:D2}.{nanosecond:D9}");
			}

			static string FormatFirebirdDecFloat(FbDecFloat value)
			{
				return string.Create(CultureInfo.InvariantCulture, $"{value.Coefficient}E{value.Exponent}");
			}

			static string FormatFirebirdZonedDateTime(FbZonedDateTime value)
			{
				return value.DateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture) + " " + FormatFirebirdTimeZone(value.TimeZone, value.Offset);
			}

			static string FormatFirebirdZonedTime(FbZonedTime value)
			{
				return value.Time.ToString("c", CultureInfo.InvariantCulture) + " " + FormatFirebirdTimeZone(value.TimeZone, value.Offset);
			}

			static string FormatFirebirdTimeZone(string timeZone, TimeSpan? offset)
			{
				return offset?.ToString("c", CultureInfo.InvariantCulture) ?? timeZone;
			}

			static string FormatDuckDBDateOnly(DuckDBDateOnly value)
			{
				if (value.IsPositiveInfinity)
					return "infinity";
				if (value.IsNegativeInfinity)
					return "-infinity";

				return string.Create(CultureInfo.InvariantCulture, $"{value.Year:D4}-{value.Month:D2}-{value.Day:D2}");
			}

			static string FormatDuckDBTimeOnly(DuckDBTimeOnly value)
			{
				return string.Create(CultureInfo.InvariantCulture, $"{value.Hour:D2}:{value.Min:D2}:{value.Sec:D2}.{value.Microsecond:D6}0");
			}

			static string FormatDuckDBTimestamp(DuckDBTimestamp value)
			{
				if (value.IsPositiveInfinity)
					return "infinity";
				if (value.IsNegativeInfinity)
					return "-infinity";

				return FormatDuckDBDateOnly(value.Date) + "T" + FormatDuckDBTimeOnly(value.Time);
			}

			static bool IsNpgsqlRange(object value)
			{
				var type = value.GetType();

				return type.IsGenericType
					&& string.Equals(type.GetGenericTypeDefinition().FullName, "NpgsqlTypes.NpgsqlRange`1", StringComparison.Ordinal);
			}

			static string FormatNpgsqlRange(object value)
			{
				if ((bool)GetPropertyValue(value, "IsEmpty"))
					return "empty";

				var output = new StringBuilder();

				var lowerBoundInfinite    = (bool)GetPropertyValue(value, "LowerBoundInfinite");
				var upperBoundInfinite    = (bool)GetPropertyValue(value, "UpperBoundInfinite");
				var lowerBoundIsInclusive = (bool)GetPropertyValue(value, "LowerBoundIsInclusive");
				var upperBoundIsInclusive = (bool)GetPropertyValue(value, "UpperBoundIsInclusive");

				output.Append(lowerBoundIsInclusive ? '[' : '(');

				if (!lowerBoundInfinite)
					output.Append(ConvertRangeBoundToString(GetPropertyValue(value, "LowerBound")));

				output.Append(',');

				if (!upperBoundInfinite)
					output.Append(ConvertRangeBoundToString(GetPropertyValue(value, "UpperBound")));

				output.Append(upperBoundIsInclusive ? ']' : ')');
				return output.ToString();
			}

			static string? ConvertRangeBoundToString(object? value)
			{
				return value switch
				{
					null                    => null,
					DateOnly date           => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
					DateTime dateTime       => dateTime.ToString("O", CultureInfo.InvariantCulture),
					DateTimeOffset dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
					TimeOnly time           => time.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
					TimeSpan time           => time.ToString("c", CultureInfo.InvariantCulture),
					_                       => Convert.ToString(value, CultureInfo.InvariantCulture),
				};
			}

			static bool IsDB2Value(object value)
			{
				return value.GetType().FullName?.StartsWith("IBM.Data.DB2Types.DB2", StringComparison.Ordinal) == true;
			}

			static string? FormatDB2Value(object value)
			{
				return value.GetType().Name switch
				{
					"DB2Binary"    => ConvertBytesToString((byte[])GetPropertyValue(value, "Value")),
					"DB2Blob"      => ConvertBytesToString((byte[])GetPropertyValue(value, "Value")),
					"DB2Clob"      => (string)GetPropertyValue(value, "Value"),
					"DB2Date"      => FormatDate((DateTime)GetPropertyValue(value, "Value")),
					"DB2Time"      => ((TimeSpan)GetPropertyValue(value, "Value")).ToString("c", CultureInfo.InvariantCulture),
					"DB2TimeStamp" => ((DateTime)GetPropertyValue(value, "Value")).ToString("O", CultureInfo.InvariantCulture),
					"DB2Xml"       => (string)GetMethodValue(value, "GetString"),
					_              => Convert.ToString(value, CultureInfo.InvariantCulture),
				};
			}

			static object GetPropertyValue(object value, string propertyName)
			{
				var property = value.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
					?? throw new InvalidOperationException($"Provider-specific type '{value.GetType().FullName}' doesn't contain '{propertyName}' property.");

				return property.GetValue(value)
					?? throw new InvalidOperationException($"Provider-specific type '{value.GetType().FullName}' property '{propertyName}' returned null.");
			}

			static object GetMethodValue(object value, string methodName)
			{
				var method = value.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, binder: null, types: Type.EmptyTypes, modifiers: null)
					?? throw new InvalidOperationException($"Provider-specific type '{value.GetType().FullName}' doesn't contain '{methodName}' method.");

				return method.InvokeExt(value, null)
					?? throw new InvalidOperationException($"Provider-specific type '{value.GetType().FullName}' method '{methodName}' returned null.");
			}

			static string ConvertBytesToString(byte[] bytes)
			{
				return "0x" + Convert.ToHexString(bytes);
			}

			static string ConvertStreamToString(Stream stream)
			{
				if (stream.CanSeek)
					stream.Position = 0;

				using var memory = new MemoryStream();

				stream.CopyTo(memory);
				return ConvertBytesToString(memory.ToArray());
			}

			static string ConvertByteArrayToString(byte[] bytes)
			{
				var output = new StringBuilder();

				output.Append('[');
				for (var i = 0; i < bytes.Length; i++)
				{
					if (i > 0)
						output.Append(',');

					output.Append(bytes[i].ToString(CultureInfo.InvariantCulture));
				}

				output.Append(']');
				return output.ToString();
			}

			static string ConvertTupleToString(ITuple tuple)
			{
				var output = new StringBuilder();

				output.Append('(');
				for (var i = 0; i < tuple.Length; i++)
				{
					if (i > 0)
						output.Append(',');

					output.Append(ConvertNestedValueToString(tuple[i]));
				}

				output.Append(')');
				return output.ToString();
			}

			static string ConvertSequenceToString(IEnumerable sequence)
			{
				var output       = new StringBuilder();
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

						output.Append(openBracket);
						first = false;
					}

					if (output.Length > 1)
						output.Append(',');

					if (map)
						AppendKeyValuePair(output, item);
					else
						output.Append(ConvertNestedValueToString(item));
				}

				if (first)
					output.Append(openBracket);

				output.Append(closeBracket);
				return output.ToString();
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

			static void AppendKeyValuePair(StringBuilder output, object? value)
			{
				if (value == null)
				{
					output.Append(':');
					return;
				}

				var type = value.GetType();
				var key  = type.GetProperty("Key")!.GetValue(value);
				var item = type.GetProperty("Value")!.GetValue(value);

				output.Append(ConvertNestedValueToString(key));
				output.Append(':');
				output.Append(ConvertNestedValueToString(item));
			}

			static string ConvertVectorToString<T>(T[] vector)
			{
				var output = new StringBuilder();

				output.Append('[');
				for (var i = 0; i < vector.Length; i++)
				{
					if (i > 0)
						output.Append(',');

					output.Append(Convert.ToString(vector[i], CultureInfo.InvariantCulture));
				}

				output.Append(']');
				return output.ToString();
			}
		}
	}
}
