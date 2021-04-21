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
		}

		static void BuildDateTime(StringBuilder stringBuilder, SqlDataType dt, DateTime value)
		{
			var dbType = dt.Type.DbType ?? "timestamp";
			var format = "CAST('{0:yyyy-MM-dd HH:mm:ss.fff}' AS {1})";

			if (value.Millisecond == 0)
				format = value.Hour == 0 && value.Minute == 0 && value.Second == 0
					? "CAST('{0:yyyy-MM-dd}' AS {1})"
					: "CAST('{0:yyyy-MM-dd HH:mm:ss}' AS {1})";

			stringBuilder.AppendFormat(format, value, dbType);
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
			{
				stringBuilder.AppendFormat("{0:X2}", bt);
			}

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
