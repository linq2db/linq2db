using System;
using System.Globalization;
using System.Text;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Mapping;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	/// <summary>
	/// Provides a read-only <see cref="MappingSchema"/> implementation for YDB (Yandex Database),
	/// used by the LINQ to DB framework for type mapping and SQL literal generation.
	/// 
	/// This schema:
	/// - Is immutable and optimized for thread-safe reuse.
	/// - Is derived from a legacy implementation, preserving all custom behaviors.
	/// - Has been refined with fixes and insights from newer versions but deliberately avoids
	///   <c>DataTools.ConvertStringToSql</c>, since escaping is handled internally.
	/// - Only supports primitive .NET types. Complex types (UDTs) are currently not supported.
	/// </summary>
	public sealed class YdbMappingSchema : LockedMappingSchema
	{
		// --------------------------------------------------------------------
		// Default settings for SQL Decimal(p, s) types in YDB
		// --------------------------------------------------------------------

		/// <summary>
		/// Default precision used for SQL <c>Decimal</c> values when not explicitly specified.
		/// </summary>
		internal const int DEFAULT_DECIMAL_PRECISION = 22;

		/// <summary>
		/// Default scale used for SQL <c>Decimal</c> values when not explicitly specified.
		/// </summary>
		internal const int DEFAULT_DECIMAL_SCALE     = 9;

		/// <summary>
		/// Initializes the mapping schema with .NET to YDB type mappings and
		/// custom converters for SQL literal formatting.
		/// </summary>
		private YdbMappingSchema() : base(ProviderName.Ydb)
		{
			// ----------------------------------------------------------------
			// .NET types mapped to YDB DataType enums
			// ----------------------------------------------------------------
			AddScalarType(typeof(string), DataType.VarChar);
			AddScalarType(typeof(bool), DataType.Boolean);
			AddScalarType(typeof(Guid), DataType.Guid);
			AddScalarType(typeof(byte[]), DataType.VarBinary);
			AddScalarType(typeof(TimeSpan), DataType.Interval);
#if NET6_0_OR_GREATER
			AddScalarType(typeof(DateOnly), DataType.Date);

			SetConverter<DateOnly, DateTime>(d =>
				new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Unspecified));

			SetConverter<DateOnly?, DateTime?>(d =>
				d.HasValue
					? new DateTime(d.Value.Year, d.Value.Month, d.Value.Day, 0, 0, 0, DateTimeKind.Unspecified)
					: null);

			SetValueToSqlConverter(typeof(DateOnly),
				(sb, _, _, v) => sb.AppendFormat(
					CultureInfo.InvariantCulture,
					"Date(\"{0:yyyy-MM-dd}\")", (DateOnly)v));
#endif

			// ----------------------------------------------------------------
			// SQL literal converters
			// ----------------------------------------------------------------
			SetValueToSqlConverter(typeof(string), (sb, dt, _, v) => BuildStringLiteral(sb, (string)v, dt.Type.DataType));
			SetValueToSqlConverter(typeof(char), (sb, _, _, v) => BuildCharLiteral(sb, (char)v));
			SetValueToSqlConverter(typeof(bool), (sb, dt, _, v) => BuildBoolLiteral(sb, (bool)v, dt.Type.DataType));
			SetValueToSqlConverter(typeof(Guid), (sb, _, _, v) => BuildUuidLiteral(sb, (Guid)v));
			SetValueToSqlConverter(typeof(byte[]), (sb, _, _, v) => BuildBinaryLiteral(sb, (byte[])v));
			SetValueToSqlConverter(typeof(DateTime), (sb, dt, _, v) => BuildDateTimeLiteral(sb, (DateTime)v, dt.Type.DataType));
			SetValueToSqlConverter(typeof(TimeSpan), (sb, _, _, v) => BuildIntervalLiteral(sb, (TimeSpan)v));

			// Integers
			SetValueToSqlConverter(typeof(byte), (sb, dt, _, v) => BuildUIntLiteral(sb, (byte)v, dt.Type.DataType, "Uint8"));
			SetValueToSqlConverter(typeof(sbyte), (sb, dt, _, v) => BuildIntLiteral(sb, (sbyte)v, dt.Type.DataType, "Int8"));
			SetValueToSqlConverter(typeof(short), (sb, dt, _, v) => BuildIntLiteral(sb, (short)v, dt.Type.DataType, "Int16"));
			SetValueToSqlConverter(typeof(ushort), (sb, dt, _, v) => BuildUIntLiteral(sb, (ushort)v, dt.Type.DataType, "Uint16"));
			SetValueToSqlConverter(typeof(int), (sb, dt, _, v) => BuildIntLiteral(sb, (int)v, dt.Type.DataType, "Int32"));
			SetValueToSqlConverter(typeof(uint), (sb, dt, _, v) => BuildUIntLiteral(sb, (uint)v, dt.Type.DataType, "Uint32"));
			SetValueToSqlConverter(typeof(long), (sb, dt, _, v) => BuildIntLiteral(sb, (long)v, dt.Type.DataType, "Int64"));
			SetValueToSqlConverter(typeof(ulong), (sb, dt, _, v) => BuildUIntLiteral(sb, (ulong)v, dt.Type.DataType, "Uint64"));

			// Floating point types
			SetValueToSqlConverter(typeof(float), (sb, _, _, v) => sb.AppendFormat(CultureInfo.InvariantCulture, "Float(\"{0:G9}\")", (float)v));
			SetValueToSqlConverter(typeof(double), (sb, _, _, v) => sb.AppendFormat(CultureInfo.InvariantCulture, "Double(\"{0:G17}\")", (double)v));

			// Decimal type with custom precision/scale
			SetValueToSqlConverter(typeof(decimal), (sb, dt, _, v) => BuildDecimalLiteral(sb, (decimal)v, dt));
		}

		// --------------------------------------------------------------------
		// Helper methods for building SQL literals in YDB syntax
		// --------------------------------------------------------------------

		/// <summary>
		/// Escapes quotes and backslashes in the input string and wraps it with the given quote character.
		/// </summary>
		private static void EscapeAndQuote(StringBuilder sb, string raw, char quote)
		{
			sb.Append(quote);
			foreach (var c in raw)
			{
				if (c == quote || c == '\\')
					sb.Append('\\');
				sb.Append(c);
			}

			sb.Append(quote);
		}

		private static void BuildStringLiteral(StringBuilder sb, string val, DataType dt)
		{
			EscapeAndQuote(sb, val, '"');
			if (dt == DataType.Json || dt == DataType.BinaryJson)
				sb.Insert(0, "Json(").Append(')');
		}

		private static void BuildCharLiteral(StringBuilder sb, char c) =>
			EscapeAndQuote(sb, c.ToString(), '"');

		private static void BuildBoolLiteral(StringBuilder sb, bool v, DataType _) =>
			sb.Append("Bool(\"").Append(v ? "true" : "false").Append("\")");

		private static void BuildUuidLiteral(StringBuilder sb, Guid g) =>
			sb.AppendFormat(CultureInfo.InvariantCulture, "Uuid(\"{0:D}\")", g);

		private static void BuildBinaryLiteral(StringBuilder sb, byte[] bytes)
		{
			sb.Append("FromBytes(\"");
			foreach (var b in bytes)
			{
				sb.Append("\\x");
				sb.AppendByteAsHexViaLookup32(b);
			}

			sb.Append("\")");
		}

		private static void BuildDateTimeLiteral(StringBuilder sb, DateTime dt, DataType target)
		{
			if (target == DataType.Date)
			{
				sb.AppendFormat(CultureInfo.InvariantCulture,
								"Date(\"{0:yyyy-MM-dd}\")", dt);
				return;
			}

			var isSecondPrecision =
				target == DataType.DateTime &&
				dt.Millisecond == 0 &&
				dt.Ticks % TimeSpan.TicksPerSecond == 0;

			if (isSecondPrecision)
			{
				sb.AppendFormat(
					CultureInfo.InvariantCulture,
					"Datetime(\"{0:yyyy-MM-ddTHH:mm:ssZ}\")",
					dt);
			}
			else
			{
				sb.AppendFormat(
					CultureInfo.InvariantCulture,
					"Timestamp(\"{0:yyyy-MM-ddTHH:mm:ss.ffffffZ}\")",
					dt);
			}
		}

		private static void BuildIntervalLiteral(StringBuilder sb, TimeSpan ts) =>
			sb.AppendFormat(CultureInfo.InvariantCulture, "Interval(\"{0}\")", System.Xml.XmlConvert.ToString(ts));

		private static void BuildIntLiteral(StringBuilder sb, long v, DataType dt, string ydbFn)
		{
			if (dt is DataType.Int32 or DataType.Int64 or DataType.Int16 or DataType.SByte or DataType.Undefined)
				sb.Append(v.ToString(CultureInfo.InvariantCulture));
			else
				sb.AppendFormat(CultureInfo.InvariantCulture, "{0}(\"{1}\")", ydbFn, v);
		}

		private static void BuildUIntLiteral(StringBuilder sb, ulong v, DataType dt, string ydbFn)
		{
			if (dt is DataType.UInt32 or DataType.UInt64 or DataType.UInt16 or DataType.Byte or DataType.Undefined)
				sb.Append(v.ToString(CultureInfo.InvariantCulture));
			else
				sb.AppendFormat(CultureInfo.InvariantCulture, "{0}(\"{1}\")", ydbFn, v);
		}

		private static void BuildDecimalLiteral(StringBuilder sb, decimal v, SqlDataType dt)
		{
			var p = dt.Type.Precision ?? DEFAULT_DECIMAL_PRECISION;
			var s = dt.Type.Scale     ?? DEFAULT_DECIMAL_SCALE;
			sb.AppendFormat(CultureInfo.InvariantCulture, "Decimal(\"{0}\", {1}, {2})", v, p, s);
		}

		// --------------------------------------------------------------------
		// Singleton instance
		// --------------------------------------------------------------------

		/// <summary>
		/// Globally shared instance of <see cref="YdbMappingSchema"/>.
		/// </summary>
		public static MappingSchema Instance { get; } = new YdbMappingSchema();

		/// <summary>
		/// An extended variant of the mapping schema that includes reflection-based type support
		/// from the native YDB client. Useful when using custom client types like YdbDate, YdbDateTime, etc.
		/// </summary>
		public sealed class YdbClientMappingSchema : LockedMappingSchema
		{
			/// <summary>
			/// Constructs the extended mapping schema using the provider adapter and base instance.
			/// </summary>
			public YdbClientMappingSchema()
				: base(ProviderName.Ydb, Instance)
			{
			}
		}
	}
}
