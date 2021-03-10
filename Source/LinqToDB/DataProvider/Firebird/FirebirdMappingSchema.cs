using System;
using System.Reflection;
using System.Text;


namespace LinqToDB.DataProvider.Firebird
{
	using Common;
	using Mapping;
	using SqlQuery;
	using System.Data.Linq;

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
		}

		internal static FirebirdMappingSchema Instance = new FirebirdMappingSchema();

		internal static MappingSchema GetMappingSchema(bool useLegacyGuidEncoding)
		{
			return useLegacyGuidEncoding ? new LegacyGuidEncodingMappingSchema() : new DefaultGuidEncodingMappingSchema();
		}

		public class DefaultGuidEncodingMappingSchema: MappingSchema
		{
			public DefaultGuidEncodingMappingSchema(): base(ProviderName.Firebird, Instance)
			{
				SetValueToSqlConverter(typeof(Guid), (sb, dt, v) => ConvertGuidToSql(false, sb, dt, (Guid)v));
			}			
		}
		public class LegacyGuidEncodingMappingSchema : MappingSchema
		{
			public LegacyGuidEncodingMappingSchema() : base(ProviderName.Firebird, Instance)
			{
				SetValueToSqlConverter(typeof(Guid), (sb, dt, v) => ConvertGuidToSql(true, sb, dt, (Guid)v));
			}
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

		static void ConvertGuidToSql(bool useLegacyGuidEncoding, StringBuilder sb, SqlDataType dataType, Guid value)
		{
			if (dataType.Type.DataType == DataType.Guid)
			{
				var bytes = value.ToByteArray();
				// workaround for http://tracker.firebirdsql.org/browse/DNET-509
				if (BitConverter.IsLittleEndian && !useLegacyGuidEncoding)
				{
					Array.Reverse(bytes, 0, 4);
					Array.Reverse(bytes, 4, 2);
					Array.Reverse(bytes, 6, 2);
				}
				sb.Append("X'");
				sb.AppendByteArrayAsHexViaLookup32(bytes);
				sb.Append('\'');
			} 
			else
			{
				sb.Append('\'').Append(value.ToString()).Append('\'');
			}
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
	}
}
