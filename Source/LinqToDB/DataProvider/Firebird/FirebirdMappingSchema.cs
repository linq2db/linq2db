using System;
using System.Text;

namespace LinqToDB.DataProvider.Firebird
{
	using Common;
	using Mapping;
	using SqlQuery;
	using System.Data.Linq;
	using System.Globalization;
	using System.Numerics;

	public class FirebirdMappingSchema : MappingSchema
	{
		private const string DATE_FORMAT      = "CAST('{0:yyyy-MM-dd}' AS {1})";
		private const string DATETIME_FORMAT  = "CAST('{0:yyyy-MM-dd HH:mm:ss}' AS {1})";
		private const string TIMESTAMP_FORMAT = "CAST('{0:yyyy-MM-dd HH:mm:ss.fff}' AS {1})";

		public FirebirdMappingSchema() : this(ProviderName.Firebird)
		{
		}

		protected FirebirdMappingSchema(string configuration) : base(configuration)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));

			// firebird string literals can contain only limited set of characters, so we should encode them
			SetValueToSqlConverter(typeof(string)  , (sb, dt, v) => ConvertStringToSql(sb, (string)v));
			SetValueToSqlConverter(typeof(char)    , (sb, dt, v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[])  , (sb, dt, v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary)  , (sb, dt, v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));
			SetValueToSqlConverter(typeof(DateTime), (sb, dt, v) => BuildDateTime(sb, dt, (DateTime)v));

			SetDataType(typeof(BigInteger), new SqlDataType(DataType.Int128, typeof(BigInteger), "INT128"));
			SetValueToSqlConverter(typeof(BigInteger), (sb, dt, v) => sb.Append(((BigInteger)v).ToString(CultureInfo.InvariantCulture)));

			// adds floating point special values support
			// Firebird support special values but lacks literals support, so we use LOG function instead of literal
			// https://firebirdsql.org/refdocs/langrefupd25-intfunc-log.html
			SetValueToSqlConverter(typeof(float), (sb, dt, v) =>
			{
				// infinity cast could fail due to bug (fix not yet released when this code added):
				// https://github.com/FirebirdSQL/firebird/issues/6750
				var f = (float)v;
				if (float.IsNaN(f))
					sb.Append("CAST(LOG(1, 1) AS FLOAT)");
				else if (float.IsNegativeInfinity(f))
					sb.Append("CAST(LOG(1, 0.5) AS FLOAT)");
				else if (float.IsPositiveInfinity(f))
					sb.Append("CAST(LOG(1, 2) AS FLOAT)");
				else
					sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G9}", f);
			});
			SetValueToSqlConverter(typeof(double), (sb, dt, v) =>
			{
				var d = (double)v;
				if (double.IsNaN(d))
					sb.Append("LOG(1, 1)");
				else if (double.IsNegativeInfinity(d))
					sb.Append("LOG(1, 0.5)");
				else if (double.IsPositiveInfinity(d))
					sb.Append("LOG(1, 2)");
				else
					sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G17}", d);
			});
		}

		static void BuildDateTime(StringBuilder stringBuilder, SqlDataType dt, DateTime value)
		{
			var dbType = dt.Type.DbType ?? "timestamp";
			var format = TIMESTAMP_FORMAT;

			if (value.Millisecond == 0)
				format = value.Hour == 0 && value.Minute == 0 && value.Second == 0
					? DATE_FORMAT
					: DATETIME_FORMAT;

			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, value, dbType);
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("X'");

			stringBuilder.AppendByteArrayAsHexViaLookup32(value);

			stringBuilder.Append('\'');
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			if (value == string.Empty)
				stringBuilder.Append("''");
			else
				if (FirebirdConfiguration.IsLiteralEncodingSupported && NeedsEncoding(value))
					MakeUtf8Literal(stringBuilder, Encoding.UTF8.GetBytes(value));
				else
				{
					stringBuilder
						.Append('\'')
						.Append(value.Replace("'", "''"))
						.Append('\'');
				}
		}

		static bool NeedsEncoding(string str)
		{
			foreach (char t in str)
				if (NeedsEncoding(t))
					return true;

			return false;
		}

		static bool NeedsEncoding(char c)
		{
			return c == '\x00' || c >= '\x80';
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			if (FirebirdConfiguration.IsLiteralEncodingSupported && NeedsEncoding(value))
				MakeUtf8Literal(stringBuilder, Encoding.UTF8.GetBytes(new[] {value}));
			else
			{
				stringBuilder
					.Append('\'')
					.Append(value == '\'' ? '\'' : value)
					.Append('\'');
			}
		}

		private static void MakeUtf8Literal(StringBuilder stringBuilder, byte[] bytes)
		{
			stringBuilder.Append("_utf8 x'");

			foreach (var bt in bytes)
				stringBuilder.AppendFormat("{0:X2}", bt);

			stringBuilder.Append('\'');
		}

		internal static MappingSchema Instance { get; } = new FirebirdMappingSchema();
	}

	// internal as it will be replaced with versioned schemas in v4
	internal class FirebirdProviderMappingSchema : MappingSchema
	{
		public FirebirdProviderMappingSchema()
			: base(ProviderName.Firebird, FirebirdMappingSchema.Instance)
		{
		}

		public FirebirdProviderMappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.Firebird, Array<MappingSchema>.Append(schemas, FirebirdMappingSchema.Instance))
		{
		}
	}
}
