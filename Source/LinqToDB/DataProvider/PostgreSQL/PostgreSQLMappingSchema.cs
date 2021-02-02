using System;
using System.Net.NetworkInformation;
using System.Text;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using LinqToDB.Common;
	using LinqToDB.Data;
	using LinqToDB.SqlQuery;
	using Mapping;
	using System.Data.Linq;

	public class PostgreSQLMappingSchema : MappingSchema
	{
		public PostgreSQLMappingSchema() : this(ProviderName.PostgreSQL)
		{
		}

		public PostgreSQLMappingSchema(params MappingSchema[] schemas) : this(ProviderName.PostgreSQL, schemas)
		{
		}

		protected PostgreSQLMappingSchema(string configuration, params MappingSchema[] schemas)
			: base(configuration, schemas)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

			AddScalarType(typeof(PhysicalAddress), DataType.Udt);

			SetValueToSqlConverter(typeof(bool),     (sb,dt,v) => sb.Append(v));
			SetValueToSqlConverter(typeof(string),   (sb,dt,v) => ConvertStringToSql(sb, v.ToString()!));
			SetValueToSqlConverter(typeof(char),     (sb,dt,v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]),   (sb,dt,v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),   (sb,dt,v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));
			SetValueToSqlConverter(typeof(DateTime), (sb,dt,v) => BuildDateTime(sb, dt, (DateTime)v));

			AddScalarType(typeof(string),          DataType.Text);
			AddScalarType(typeof(TimeSpan),        DataType.Interval);
			AddScalarType(typeof(TimeSpan?),       DataType.Interval);

			// npgsql doesn't support unsigned types except byte (and sbyte)
			SetConvertExpression<ushort?, DataParameter>(value => new DataParameter(null, value == null ? (int?)null     : (int)value    , DataType.Int32)  , false);
			SetConvertExpression<uint?  , DataParameter>(value => new DataParameter(null, value == null ? (long?)null    : (long)value   , DataType.Int64)  , false);
			SetConvertExpression<ulong? , DataParameter>(value => new DataParameter(null, value == null ? (decimal?)null : (decimal)value, DataType.Decimal), false);
		}

		static void BuildDateTime(StringBuilder stringBuilder, SqlDataType dt, DateTime value)
		{
			string dbType;
			string format;

			if (value.Millisecond == 0)
			{
				if (value.Hour == 0 && value.Minute == 0 && value.Second == 0)
				{
					format = "'{0:yyyy-MM-dd}'::{1}";
					dbType = dt.Type.DbType ?? "date";
				}
				else
				{
					format = "'{0:yyyy-MM-dd HH:mm:ss}'::{1}";
					dbType = dt.Type.DbType ?? "timestamp";
				}
			}
			else
			{
				format = "'{0:yyyy-MM-dd HH:mm:ss.fff}'::{1}";
				dbType = dt.Type.DbType ?? "timestamp";
			}

			stringBuilder.AppendFormat(format, value, dbType);
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("E'\\\\x");

			stringBuilder.AppendByteArrayAsHexViaLookup32(value);

			stringBuilder.Append('\'');
		}

		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder
				.Append("chr(")
				.Append(value)
				.Append(')')
				;
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			DataTools.ConvertStringToSql(stringBuilder, "||", null, AppendConversion, value, null);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversion, value);
		}

		internal static MappingSchema Instance { get; } = new PostgreSQLMappingSchema();
	}

	public class PostgreSQL92MappingSchema : MappingSchema
	{
		public PostgreSQL92MappingSchema()
			: base(ProviderName.PostgreSQL92, PostgreSQLMappingSchema.Instance)
		{
		}

		public PostgreSQL92MappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.PostgreSQL92, Array<MappingSchema>.Append(schemas, PostgreSQLMappingSchema.Instance))
		{
		}
	}

	public class PostgreSQL93MappingSchema : MappingSchema
	{
		public PostgreSQL93MappingSchema()
			: base(ProviderName.PostgreSQL93, PostgreSQLMappingSchema.Instance)
		{
		}

		public PostgreSQL93MappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.PostgreSQL93, Array<MappingSchema>.Append(schemas, PostgreSQLMappingSchema.Instance))
		{
		}
	}

	public class PostgreSQL95MappingSchema : MappingSchema
	{
		public PostgreSQL95MappingSchema()
			: base(ProviderName.PostgreSQL95, PostgreSQLMappingSchema.Instance)
		{
		}

		public PostgreSQL95MappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.PostgreSQL95, Array<MappingSchema>.Append(schemas, PostgreSQLMappingSchema.Instance))
		{
		}
	}
}
