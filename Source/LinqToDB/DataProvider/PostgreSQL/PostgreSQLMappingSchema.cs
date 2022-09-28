﻿using System;
using System.Data.Linq;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Text;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Common;
	using Data;
	using Mapping;
	using SqlQuery;

	sealed class PostgreSQLMappingSchema : LockedMappingSchema
	{
		private const string DATE_FORMAT       = "'{0:yyyy-MM-dd}'::{1}";
		private const string TIMESTAMP0_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss}'::{1}";
		private const string TIMESTAMP3_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.fff}'::{1}";

		PostgreSQLMappingSchema() : base(ProviderName.PostgreSQL)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

			AddScalarType(typeof(PhysicalAddress), DataType.Udt);

			SetValueToSqlConverter(typeof(bool),     (sb,dt,v) => sb.Append(v));
			SetValueToSqlConverter(typeof(string),   (sb,dt,v) => ConvertStringToSql(sb, v.ToString()!));
			SetValueToSqlConverter(typeof(char),     (sb,dt,v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]),   (sb,dt,v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),   (sb,dt,v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));
			SetValueToSqlConverter(typeof(Guid),     (sb,dt,v) => sb.AppendFormat("'{0:D}'::uuid", (Guid)v));
			SetValueToSqlConverter(typeof(DateTime), (sb,dt,v) => BuildDateTime(sb, dt, (DateTime)v));

			// adds floating point special values support
			SetValueToSqlConverter(typeof(float) , (sb, dt, v) =>
			{
				var f = (float)v;
				var quote = float.IsNaN(f) || float.IsInfinity(f);
				if (quote) sb.Append('\'');
				sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G9}", f);
				if (quote) sb.Append("'::float4");
			});
			SetValueToSqlConverter(typeof(double), (sb, dt, v) =>
			{
				var d = (double)v;
				var quote = double.IsNaN(d) || double.IsInfinity(d);
				if (quote) sb.Append('\'');
				sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G17}", d);
				if (quote) sb.Append("'::float8");
			});

			AddScalarType(typeof(string),    DataType.Text);
			AddScalarType(typeof(TimeSpan),  DataType.Interval);

#if NET6_0_OR_GREATER
			SetValueToSqlConverter(typeof(DateOnly), (sb, dt, v) => BuildDate(sb, dt, (DateOnly)v));
#endif

			// npgsql doesn't support unsigned types except byte (and sbyte)
			SetConvertExpression<ushort , DataParameter>(value => new DataParameter(null, (int  )value, DataType.Int32));
			SetConvertExpression<ushort?, DataParameter>(value => new DataParameter(null, (int? )value, DataType.Int32), addNullCheck: false);
			SetConvertExpression<uint   , DataParameter>(value => new DataParameter(null, (long )value, DataType.Int64));
			SetConvertExpression<uint?  , DataParameter>(value => new DataParameter(null, (long?)value, DataType.Int64), addNullCheck: false);

			var ulongType = new SqlDataType(DataType.Decimal, typeof(ulong), 20, 0);
			// set type for proper SQL type generation
			AddScalarType(typeof(ulong ), ulongType);

			SetConvertExpression<ulong , DataParameter>(value => new DataParameter(null, (decimal)value , DataType.Decimal) /*{ Precision = 20, Scale = 0 }*/);
			SetConvertExpression<ulong?, DataParameter>(value => new DataParameter(null, (decimal?)value, DataType.Decimal) /*{ Precision = 20, Scale = 0 }*/, addNullCheck: false);
		}

		static void BuildDateTime(StringBuilder stringBuilder, SqlDataType dt, DateTime value)
		{
			string dbType;
			string format;

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

#if NET6_0_OR_GREATER
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
			stringBuilder
				.Append("chr(")
				.Append(value)
				.Append(')')
				;
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

		public sealed class PostgreSQL15MappingSchema : LockedMappingSchema
		{
			public PostgreSQL15MappingSchema() : base(ProviderName.PostgreSQL15, NpgsqlProviderAdapter.GetInstance().MappingSchema, Instance)
			{
			}
		}
	}
}
