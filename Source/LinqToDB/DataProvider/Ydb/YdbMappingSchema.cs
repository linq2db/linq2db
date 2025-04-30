using System;
using System.Globalization;
using System.Numerics;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Ydb
{
	/// <summary>
	/// Locked (read-only) <see cref="MappingSchema"/> for YDB.
	/// 
	/// * Based on the previously used implementation (all custom builders preserved).
	/// * Enhanced with minor fixes and comments from the new schema, but without
	///   calls to <c>DataTools.ConvertStringToSql</c> (they're not needed – custom escaping logic is used here).
	/// * Only primitive types are supported; complex/UDT types are not required yet.
	/// </summary>
	sealed class YdbMappingSchema : LockedMappingSchema
	{
		// --------------------------------------------------------------------
		// Defaults for parametric Decimal(<precision>, <scale>)
		// --------------------------------------------------------------------
		internal const int DEFAULT_DECIMAL_PRECISION = 22;
		internal const int DEFAULT_DECIMAL_SCALE     = 9;

		private YdbMappingSchema() : base(ProviderName.Ydb)
		{
			//----------------------------------------------------------------
			// .NET-type  ⇢  YDB DataType
			//----------------------------------------------------------------
			AddScalarType(typeof(string), DataType.VarChar);
			AddScalarType(typeof(bool), DataType.Boolean);
			AddScalarType(typeof(Guid), DataType.Guid);
			AddScalarType(typeof(byte[]), DataType.VarBinary);
			AddScalarType(typeof(TimeSpan), DataType.Interval);
#if NET6_0_OR_GREATER
			AddScalarType(typeof(DateOnly), DataType.Date);
#endif

			//----------------------------------------------------------------
			// Value ⇒ SQL-literal builders
			//----------------------------------------------------------------
			SetValueToSqlConverter(typeof(string), (sb, dt, _, v) => BuildStringLiteral(sb, (string)v, dt.Type.DataType));
			SetValueToSqlConverter(typeof(char), (sb, _, _, v) => BuildCharLiteral(sb, (char)v));
			SetValueToSqlConverter(typeof(bool), (sb, dt, _, v) => BuildBoolLiteral(sb, (bool)v, dt.Type.DataType));
			SetValueToSqlConverter(typeof(Guid), (sb, _, _, v) => BuildUuidLiteral(sb, (Guid)v));
			SetValueToSqlConverter(typeof(byte[]), (sb, _, _, v) => BuildBinaryLiteral(sb, (byte[])v));
			SetValueToSqlConverter(typeof(DateTime), (sb, dt, _, v) => BuildDateTimeLiteral(sb, (DateTime)v, dt.Type.DataType));
			SetValueToSqlConverter(typeof(TimeSpan), (sb, _, _, v) => BuildIntervalLiteral(sb, (TimeSpan)v));

			// Integer types
			SetValueToSqlConverter(typeof(byte), (sb, dt, _, v) => BuildUIntLiteral(sb, (byte)v, dt.Type.DataType, "Uint8"));
			SetValueToSqlConverter(typeof(sbyte), (sb, dt, _, v) => BuildIntLiteral(sb, (sbyte)v, dt.Type.DataType, "Int8"));
			SetValueToSqlConverter(typeof(short), (sb, dt, _, v) => BuildIntLiteral(sb, (short)v, dt.Type.DataType, "Int16"));
			SetValueToSqlConverter(typeof(ushort), (sb, dt, _, v) => BuildUIntLiteral(sb, (ushort)v, dt.Type.DataType, "Uint16"));
			SetValueToSqlConverter(typeof(int), (sb, dt, _, v) => BuildIntLiteral(sb, (int)v, dt.Type.DataType, "Int32"));
			SetValueToSqlConverter(typeof(uint), (sb, dt, _, v) => BuildUIntLiteral(sb, (uint)v, dt.Type.DataType, "Uint32"));
			SetValueToSqlConverter(typeof(long), (sb, dt, _, v) => BuildIntLiteral(sb, (long)v, dt.Type.DataType, "Int64"));
			SetValueToSqlConverter(typeof(ulong), (sb, dt, _, v) => BuildUIntLiteral(sb, (ulong)v, dt.Type.DataType, "Uint64"));

			// Floating point
			SetValueToSqlConverter(typeof(float), (sb, _, _, v) => sb.AppendFormat(CultureInfo.InvariantCulture, "Float(\"{0:G9}\")", (float)v));
			SetValueToSqlConverter(typeof(double), (sb, _, _, v) => sb.AppendFormat(CultureInfo.InvariantCulture, "Double(\"{0:G17}\")", (double)v));

			// Decimal(<p>,<s>)
			SetValueToSqlConverter(typeof(decimal), (sb, dt, _, v) => BuildDecimalLiteral(sb, (decimal)v, dt));
		}

		// --------------------------------------------------------------------
		// Helper builders
		// --------------------------------------------------------------------
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
			EscapeAndQuote(sb, val, '"'); // "abc"

			// Json / Yson / JsonDocument can be wrapped in Json("...") if needed
			if (dt == DataType.Json || dt == DataType.BinaryJson)
				sb.Insert(0, "Json(").Append(')');
		}

		private static void BuildCharLiteral(StringBuilder sb, char c) => EscapeAndQuote(sb, c.ToString(), '"');

		private static void BuildBoolLiteral(StringBuilder sb, bool v, DataType _) => sb.Append("Bool(\"").Append(v ? "true" : "false").Append("\")");

		private static void BuildUuidLiteral(StringBuilder sb, Guid g) => sb.AppendFormat(CultureInfo.InvariantCulture, "Uuid(\"{0:D}\")", g);

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
			// YDB expects UTC strings with Z
			if (dt.Kind != DateTimeKind.Utc)
				dt = dt.ToUniversalTime();

			if (dt.Millisecond == 0 && dt.Ticks % TimeSpan.TicksPerSecond == 0 && target == DataType.DateTime)
				sb.AppendFormat(CultureInfo.InvariantCulture, "Datetime(\"{0:yyyy-MM-ddTHH:mm:ssZ}\")", dt);
			else
				sb.AppendFormat(CultureInfo.InvariantCulture, "Timestamp(\"{0:yyyy-MM-ddTHH:mm:ss.ffffffZ}\")", dt);
		}

		private static void BuildIntervalLiteral(StringBuilder sb, TimeSpan ts) => sb.AppendFormat(CultureInfo.InvariantCulture, "Interval(\"{0}\")", System.Xml.XmlConvert.ToString(ts));

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
		internal static MappingSchema Instance { get; } = new YdbMappingSchema();

		/// <summary>
		/// Variant that includes the built-in (reflection-based) schema from the YDB client -
		/// allows using types like <c>YdbDate</c>, <c>YdbDateTime</c>, etc.,
		/// if they are connected (see <see cref="YdbProviderAdapter"/>).
		/// </summary>
		public sealed class YdbClientMappingSchema : LockedMappingSchema
		{
			public YdbClientMappingSchema() : base(ProviderName.Ydb, YdbProviderAdapter.GetInstance().MappingSchema, Instance) { }
		}
	}
}
