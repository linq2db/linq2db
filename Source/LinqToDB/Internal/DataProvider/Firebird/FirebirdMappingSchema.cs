using System;
using System.Data.Linq;
using System.Globalization;
using System.Numerics;
using System.Text;

using LinqToDB.Data;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Mapping;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Firebird
{
	sealed class FirebirdMappingSchema : LockedMappingSchema
	{
#if SUPPORTS_COMPOSITE_FORMAT
		private static readonly CompositeFormat DATE_FORMAT      = CompositeFormat.Parse("CAST('{0:yyyy-MM-dd}' AS {1})");
		private static readonly CompositeFormat DATETIME_FORMAT  = CompositeFormat.Parse("CAST('{0:yyyy-MM-dd HH:mm:ss}' AS {1})");
		private static readonly CompositeFormat TIMESTAMP_FORMAT = CompositeFormat.Parse("CAST('{0:yyyy-MM-dd HH:mm:ss.fff}' AS {1})");
#else
		private const string DATE_FORMAT      = "CAST('{0:yyyy-MM-dd}' AS {1})";
		private const string DATETIME_FORMAT  = "CAST('{0:yyyy-MM-dd HH:mm:ss}' AS {1})";
		private const string TIMESTAMP_FORMAT = "CAST('{0:yyyy-MM-dd HH:mm:ss.fff}' AS {1})";
#endif

		FirebirdMappingSchema() : base(ProviderName.Firebird)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

			SetDataType(typeof(string),  new SqlDataType(DataType.NVarChar, typeof(string), 255));
			SetDataType(typeof(decimal), new SqlDataType(DataType.Decimal, typeof(decimal), 18, 10));
			SetDataType(typeof(ulong), new SqlDataType(DataType.Decimal, typeof(ulong), precision: 20, scale: 0));

			// firebird string literals can contain only limited set of characters, so we should encode them
			SetValueToSqlConverter(typeof(string)  , (sb, _,o,v) => ConvertStringToSql (sb, o, (string)v));
			SetValueToSqlConverter(typeof(char)    , (sb, _,o,v) => ConvertCharToSql   (sb, o, (char)v));
			SetValueToSqlConverter(typeof(byte[])  , (sb, _,_,v) => ConvertBinaryToSql (sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary)  , (sb, _,_,v) => ConvertBinaryToSql (sb, ((Binary)v).ToArray()));
			SetValueToSqlConverter(typeof(DateTime), (sb,dt,_,v) => BuildDateTime      (sb, dt, (DateTime)v));
			SetValueToSqlConverter(typeof(Guid)    , (sb,dt,_,v) => ConvertGuidToSql   (sb, dt, (Guid)v));
#if SUPPORTS_DATEONLY
			SetValueToSqlConverter(typeof(DateOnly), (sb,dt,_,v) => BuildDateOnly(sb, dt, (DateOnly)v));
#endif

			SetDataType(typeof(bool), new SqlDataType(DataType.Boolean, typeof(bool), "BOOLEAN"));
			SetValueToSqlConverter(typeof(bool), (sb, dt, _, v) => ConvertBooleanToSql(sb, dt, (bool)v));

			SetDataType(typeof(BigInteger), new SqlDataType(DataType.Int128, typeof(BigInteger), "INT128"));
			SetValueToSqlConverter(typeof(BigInteger), (sb,_,_,v) => sb.Append(((BigInteger)v).ToString(CultureInfo.InvariantCulture)));

			// adds floating point special values support
			// Firebird support special values but lacks literals support, so we use LOG function instead of literal
			// https://firebirdsql.org/refdocs/langrefupd25-intfunc-log.html
			SetValueToSqlConverter(typeof(float), (sb,_,_,v) =>
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

			SetValueToSqlConverter(typeof(double), (sb,_,_,v) =>
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

			SetConvertExpression((Guid g) => ReadGuidAsBinary(g), conversionType: ConversionType.FromDatabase);
		}

		static void BuildDateTime(StringBuilder stringBuilder, SqlDataType dt, DateTime value)
		{
			var dbType = dt.Type.DbType ?? (dt.Type.DataType == DataType.Date ? "date" : "timestamp");
			var format = TIMESTAMP_FORMAT;

			if (value.Millisecond == 0)
				format = value.Hour == 0 && value.Minute == 0 && value.Second == 0
					? DATE_FORMAT
					: DATETIME_FORMAT;

			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, value, dbType);
		}

#if SUPPORTS_DATEONLY
		static void BuildDateOnly(StringBuilder stringBuilder, SqlDataType dt, DateOnly value)
		{
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value, dt.Type.DbType ?? "date");
		}
#endif

		static byte[] ReadGuidAsBinary(Guid guid)
		{
			var bytes = guid.ToByteArray();

			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes, 0, 4);
				Array.Reverse(bytes, 4, 2);
				Array.Reverse(bytes, 6, 2);
			}

			return bytes;
		}

		static void ConvertGuidToSql(StringBuilder sb, SqlDataType dataType, Guid value)
		{
			if (dataType.Type.DataType is DataType.Char or DataType.NChar or DataType.VarChar or DataType.NVarChar)
			{
				sb
					.Append('\'')
					.Append(value.ToString())
					.Append('\'');
			}
			else
			{
				var bytes = value.ToByteArray();

				if (BitConverter.IsLittleEndian)
				{
					Array.Reverse(bytes, 0, 4);
					Array.Reverse(bytes, 4, 2);
					Array.Reverse(bytes, 6, 2);
				}

				sb
					.Append("X'")
					.AppendByteArrayAsHexViaLookup32(bytes)
					.Append('\'');
			}
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder
				.Append("X'")
				.AppendByteArrayAsHexViaLookup32(value)
				.Append('\'');
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, DataOptions options, string value)
		{
			if (value.Length == 0)
				stringBuilder.Append("''");
			else
			{
				var fbo = options.FindOrDefault(FirebirdOptions.Default);

				if (fbo.IsLiteralEncodingSupported && NeedsEncoding(value))
					MakeUtf8Literal(stringBuilder, Encoding.UTF8.GetBytes(value));
				else
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

		static void ConvertCharToSql(StringBuilder stringBuilder, DataOptions options, char value)
		{
			var fbo = options.FindOrDefault(FirebirdOptions.Default);

			if (fbo.IsLiteralEncodingSupported && NeedsEncoding(value))
				MakeUtf8Literal(stringBuilder, Encoding.UTF8.GetBytes(new[] {value}));
			else
				stringBuilder
					.Append('\'')
					.Append(value == '\'' ? '\'' : value)
					.Append('\'');
		}

		static void MakeUtf8Literal(StringBuilder stringBuilder, byte[] bytes)
		{
			stringBuilder.Append("_utf8 x'");

			foreach (var bt in bytes)
				stringBuilder.Append(CultureInfo.InvariantCulture, $"{bt:X2}");

			stringBuilder.Append('\'');
		}

		static void ConvertBooleanToSql(StringBuilder sb, SqlDataType dataType, bool value)
		{
			if (dataType.Type.DataType is DataType.Char)
			{
				sb
					.Append('\'')
					.Append(value ? '1' : '0')
					.Append('\'');
			}
			else
			{
				sb.Append(value ? "TRUE" : "FALSE");
			}
		}

		internal static MappingSchema Instance { get; } = new FirebirdMappingSchema();

		public sealed class Firebird25MappingSchema : LockedMappingSchema
		{
			public Firebird25MappingSchema()
				: base(ProviderName.Firebird25, FirebirdProviderAdapter.Instance.MappingSchema, Instance)
			{
				// setup bool to "1"/"0" conversions
				var booleanType = new SqlDataType(DataType.Char, typeof(bool), length: 1, null, null, dbType: "CHAR(1)");
				SetDataType(typeof(bool), booleanType);
				// TODO: we should add support for single converter to parameter for structs
				SetConvertExpression<bool , DataParameter>(value => new DataParameter(null, value ? '1' : '0', booleanType.Type));
				SetConvertExpression<bool?, DataParameter>(value => new DataParameter(null, value == null ? null : value.Value ? '1' : '0', booleanType.Type.WithSystemType(typeof(bool?))), addNullCheck: false);
				SetValueToSqlConverter(typeof(bool), (sb, dt, _, v) => ConvertBooleanToSql(sb, dt, (bool)v));
			}

			static void ConvertBooleanToSql(StringBuilder sb, SqlDataType dataType, bool value)
			{
				sb
					.Append('\'')
					.Append(value ? '1' : '0')
					.Append('\'');
			}
		}

		public sealed class Firebird3MappingSchema() : LockedMappingSchema(ProviderName.Firebird3, FirebirdProviderAdapter.Instance.MappingSchema, Instance)
		{
		}

		public sealed class Firebird4MappingSchema() : LockedMappingSchema(ProviderName.Firebird4, FirebirdProviderAdapter.Instance.MappingSchema, Instance)
		{
		}

		public sealed class Firebird5MappingSchema() : LockedMappingSchema(ProviderName.Firebird5, FirebirdProviderAdapter.Instance.MappingSchema, Instance)
		{
		}
	}
}
