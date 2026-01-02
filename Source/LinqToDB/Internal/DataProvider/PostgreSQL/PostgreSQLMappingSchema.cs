using System;
using System.Data.Linq;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Text;

using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Mapping;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.PostgreSQL
{
	public sealed class PostgreSQLMappingSchema : LockedMappingSchema
	{
#if SUPPORTS_COMPOSITE_FORMAT
		private static readonly CompositeFormat DATE_FORMAT       = CompositeFormat.Parse("'{0:yyyy-MM-dd}'::{1}");
		private static readonly CompositeFormat TIMESTAMP0_FORMAT = CompositeFormat.Parse("'{0:yyyy-MM-dd HH:mm:ss}'::{1}");
		private static readonly CompositeFormat TIMESTAMP3_FORMAT = CompositeFormat.Parse("'{0:yyyy-MM-dd HH:mm:ss.fff}'::{1}");
#else
		private const string DATE_FORMAT       = "'{0:yyyy-MM-dd}'::{1}";
		private const string TIMESTAMP0_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss}'::{1}";
		private const string TIMESTAMP3_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.fff}'::{1}";
#endif

		PostgreSQLMappingSchema() : base(ProviderName.PostgreSQL)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

			AddScalarType(typeof(PhysicalAddress), DataType.Udt);

			SetValueToSqlConverter(typeof(bool),       (sb, _,_,v) => sb.Append((bool)v));
			SetValueToSqlConverter(typeof(string),     (sb, _,_,v) => ConvertStringToSql(sb, (string)v));
			SetValueToSqlConverter(typeof(char),       (sb, _,_,v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]),     (sb, _,_,v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),     (sb, _,_,v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));
			SetValueToSqlConverter(typeof(Guid),       (sb, _,_,v) => sb.AppendFormat(CultureInfo.InvariantCulture, "'{0:D}'::uuid", (Guid)v));
			SetValueToSqlConverter(typeof(DateTime),   (sb,dt,_,v) => BuildDateTime(sb, dt, (DateTime)v));
			SetValueToSqlConverter(typeof(BigInteger), (sb, _,_,v) => sb.Append(((BigInteger)v).ToString(CultureInfo.InvariantCulture)));

			// adds floating point special values support
			SetValueToSqlConverter(typeof(float) , (sb,_,_,v) =>
			{
				var f = (float)v;
				var quote = float.IsNaN(f) || float.IsInfinity(f);
				if (quote) sb.Append('\'');
				sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G9}", f);
				if (quote) sb.Append("'::float4");
			});
			SetValueToSqlConverter(typeof(double), (sb,_,_,v) =>
			{
				var d = (double)v;
				var quote = double.IsNaN(d) || double.IsInfinity(d);
				if (quote) sb.Append('\'');
				sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G17}", d);
				if (quote) sb.Append("'::float8");
			});

			AddScalarType(typeof(string),    DataType.Text);
			AddScalarType(typeof(TimeSpan),  DataType.Interval);

#if SUPPORTS_DATEONLY
			SetValueToSqlConverter(typeof(DateOnly), (sb,dt,_,v) => BuildDate(sb, dt, (DateOnly)v));

			// backward compat:
			// npgsql 10 returns TimeOnly instead of DateTime as before
			SetConvertExpression<TimeOnly, DateTime>(value => new DateTime(default, value), conversionType: ConversionType.FromDatabase);

			AddScalarType(typeof(IPNetwork), new SqlDataType(new DbDataType(typeof(IPNetwork), DataType.Undefined, "cidr")));
#endif

			// npgsql doesn't support unsigned types except byte (and sbyte)
			SetConvertExpression<ushort , DataParameter>(value => new DataParameter(null, (int  )value, DataType.Int32));
			SetConvertExpression<uint   , DataParameter>(value => new DataParameter(null, (long )value, DataType.Int64));

			var ulongType = new SqlDataType(DataType.Decimal, typeof(ulong), 20, 0);
			// set type for proper SQL type generation
			AddScalarType(typeof(ulong ), ulongType);

			SetConvertExpression<ulong , DataParameter>(value => new DataParameter(null, (decimal)value , DataType.Decimal) /*{ Precision = 20, Scale = 0 }*/);
		}

		static void BuildDateTime(StringBuilder stringBuilder, SqlDataType dt, DateTime value)
		{
			string dbType;
#if SUPPORTS_COMPOSITE_FORMAT
			CompositeFormat format;
#else
			string format;
#endif

			if (value.Millisecond == 0)
			{
				if (value.Hour == 0 && value.Minute == 0 && value.Second == 0)
				{
					format = DATE_FORMAT;
					dbType = dt.Type.DbType ?? "date";
				}
				else
				{
					format = TIMESTAMP0_FORMAT;
					dbType = dt.Type.DbType ?? "timestamp";
				}
			}
			else
			{
				format = TIMESTAMP3_FORMAT;
				dbType = dt.Type.DbType ?? "timestamp";
			}

			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, value, dbType);
		}

#if SUPPORTS_DATEONLY
		static void BuildDate(StringBuilder stringBuilder, SqlDataType dt, DateOnly value)
		{
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value, dt.Type.DbType ?? "date");
		}
#endif

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("E'\\\\x");

			stringBuilder.AppendByteArrayAsHexViaLookup32(value);

			stringBuilder.Append("'::bytea");
		}

		static readonly Action<StringBuilder, int> AppendConversionAction = AppendConversion;
		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder.Append(CultureInfo.InvariantCulture, $"chr({value})");
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			DataTools.ConvertStringToSql(stringBuilder, "||", null, AppendConversionAction, value, null);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversionAction, value);
		}

		internal static MappingSchema Instance { get; } = new PostgreSQLMappingSchema();

		public sealed class PostgreSQL92MappingSchema : LockedMappingSchema
		{
			public PostgreSQL92MappingSchema() : base(ProviderName.PostgreSQL92, NpgsqlProviderAdapter.GetInstance().MappingSchema, Instance)
			{
			}
		}

		public sealed class PostgreSQL93MappingSchema : LockedMappingSchema
		{
			public PostgreSQL93MappingSchema() : base(ProviderName.PostgreSQL93, NpgsqlProviderAdapter.GetInstance().MappingSchema, Instance)
			{
			}
		}

		public sealed class PostgreSQL95MappingSchema : LockedMappingSchema
		{
			public PostgreSQL95MappingSchema() : base(ProviderName.PostgreSQL95, NpgsqlProviderAdapter.GetInstance().MappingSchema, Instance)
			{
			}
		}

		public sealed class PostgreSQL13MappingSchema() : LockedMappingSchema(
			ProviderName.PostgreSQL13,
			NpgsqlProviderAdapter.GetInstance().MappingSchema,
			Instance
		);

		public sealed class PostgreSQL15MappingSchema : LockedMappingSchema
		{
			public PostgreSQL15MappingSchema() : base(ProviderName.PostgreSQL15, NpgsqlProviderAdapter.GetInstance().MappingSchema, Instance)
			{
			}
		}

		public sealed class PostgreSQL18MappingSchema : LockedMappingSchema
		{
			public PostgreSQL18MappingSchema() : base(ProviderName.PostgreSQL18, NpgsqlProviderAdapter.GetInstance().MappingSchema, Instance)
			{
			}
		}
	}
}
