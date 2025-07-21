using System;
using System.Data.Linq;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;

using LinqToDB.Common;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Oracle
{
	sealed class OracleMappingSchema : LockedMappingSchema
	{
#if SUPPORTS_COMPOSITE_FORMAT
		private static readonly CompositeFormat DATE_FORMAT     = CompositeFormat.Parse("DATE '{0:yyyy-MM-dd}'");
		private static readonly CompositeFormat DATETIME_FORMAT = CompositeFormat.Parse("TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')");

		private static readonly CompositeFormat TIMESTAMP0_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss}'");
		private static readonly CompositeFormat TIMESTAMP1_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.f}'");
		private static readonly CompositeFormat TIMESTAMP2_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ff}'");
		private static readonly CompositeFormat TIMESTAMP3_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fff}'");
		private static readonly CompositeFormat TIMESTAMP4_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffff}'");
		private static readonly CompositeFormat TIMESTAMP5_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffff}'");
		private static readonly CompositeFormat TIMESTAMP6_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffffff}'");
		private static readonly CompositeFormat TIMESTAMP7_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffffff}'");

		private static readonly CompositeFormat TIMESTAMPTZ0_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss} +00:00'");
		private static readonly CompositeFormat TIMESTAMPTZ1_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.f} +00:00'");
		private static readonly CompositeFormat TIMESTAMPTZ2_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ff} +00:00'");
		private static readonly CompositeFormat TIMESTAMPTZ3_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fff} +00:00'");
		private static readonly CompositeFormat TIMESTAMPTZ4_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffff} +00:00'");
		private static readonly CompositeFormat TIMESTAMPTZ5_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffff} +00:00'");
		private static readonly CompositeFormat TIMESTAMPTZ6_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffffff} +00:00'");
		private static readonly CompositeFormat TIMESTAMPTZ7_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffffff} +00:00'");
#else
		private const string DATE_FORMAT = "DATE '{0:yyyy-MM-dd}'";

		private const string DATETIME_FORMAT = "TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')";

		private const string TIMESTAMP0_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss}'";
		private const string TIMESTAMP1_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.f}'";
		private const string TIMESTAMP2_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ff}'";
		private const string TIMESTAMP3_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fff}'";
		private const string TIMESTAMP4_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffff}'";
		private const string TIMESTAMP5_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffff}'";
		private const string TIMESTAMP6_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffffff}'";
		private const string TIMESTAMP7_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffffff}'";

		private const string TIMESTAMPTZ0_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss} +00:00'";
		private const string TIMESTAMPTZ1_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.f} +00:00'";
		private const string TIMESTAMPTZ2_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ff} +00:00'";
		private const string TIMESTAMPTZ3_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fff} +00:00'";
		private const string TIMESTAMPTZ4_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffff} +00:00'";
		private const string TIMESTAMPTZ5_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffff} +00:00'";
		private const string TIMESTAMPTZ6_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffffff} +00:00'";
		private const string TIMESTAMPTZ7_FORMAT = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffffff} +00:00'";
#endif

		OracleMappingSchema() : base(ProviderName.Oracle)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

			SetDataType(typeof(Guid),   DataType.Guid);
			SetDataType(typeof(string),  new SqlDataType(DataType.VarChar, typeof(string), 255));
			SetDataType(typeof(decimal), new SqlDataType(DataType.Decimal, typeof(decimal), 28, 10));

			SetConvertExpression<decimal,TimeSpan>(v => new TimeSpan((long)v));

			SetValueToSqlConverter(typeof(Guid),           (sb, _,_,v) => ConvertBinaryToSql  (sb,     ((Guid)   v).ToByteArray()));
			SetValueToSqlConverter(typeof(DateTime),       (sb,dt,_,v) => ConvertDateTimeToSql(sb, dt, (DateTime)v));
			SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt,_,v) => ConvertDateTimeToSql(sb, dt, ((DateTimeOffset)v).UtcDateTime));
			SetValueToSqlConverter(typeof(string)        , (sb, _,_,v) => ConvertStringToSql  (sb,     (string)v));
			SetValueToSqlConverter(typeof(char)          , (sb, _,_,v) => ConvertCharToSql    (sb,     (char)v));
			SetValueToSqlConverter(typeof(byte[]),         (sb, _,_,v) => ConvertBinaryToSql  (sb,     (byte[])v));
			SetValueToSqlConverter(typeof(Binary),         (sb, _,_,v) => ConvertBinaryToSql  (sb,     ((Binary)v).ToArray()));

#if SUPPORTS_DATEONLY
			SetValueToSqlConverter(typeof(DateOnly),       (sb,dt,_,v) => ConvertDateOnlyToSql(sb, (DateOnly)v));
#endif

			// adds floating point special values support
			SetValueToSqlConverter(typeof(float), (sb,_,_,v) =>
			{
				var f = (float)v;
				if (float.IsNaN(f))
					sb.Append("BINARY_FLOAT_NAN");
				else if (float.IsNegativeInfinity(f))
					sb.Append("-BINARY_FLOAT_INFINITY");
				else if (float.IsPositiveInfinity(f))
					sb.Append("BINARY_FLOAT_INFINITY");
				else
					sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G9}", f);
			});
			SetValueToSqlConverter(typeof(double), (sb,_,_,v) =>
			{
				var d = (double)v;
				if (double.IsNaN(d))
					sb.Append("BINARY_DOUBLE_NAN");
				else if (double.IsNegativeInfinity(d))
					sb.Append("-BINARY_DOUBLE_INFINITY");
				else if (double.IsPositiveInfinity(d))
					sb.Append("BINARY_DOUBLE_INFINITY");
				else
					sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G17}D", d);
			});
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder
				.Append("HEXTORAW('")
				.AppendByteArrayAsHexViaLookup32(value);

			stringBuilder.Append("')");
		}

		static readonly Action<StringBuilder, int> AppendConversionAction = AppendConversion;

		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder.Append(CultureInfo.InvariantCulture, $"chr({value})");
		}

		internal static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			DataTools.ConvertStringToSql(stringBuilder, "||", null, AppendConversionAction, value, null);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversionAction, value);
		}

		public override LambdaExpression? TryGetConvertExpression(Type from, Type to)
		{
			if (to.IsEnum && from == typeof(decimal))
			{
				var type = Converter.GetDefaultMappingFromEnumType(this, to);

				if (type != null)
				{
					var fromDecimalToType = GetConvertExpression(from, type, false)!;
					var fromTypeToEnum    = GetConvertExpression(type, to,   false)!;

					return Expression.Lambda(
						fromTypeToEnum.GetBody(fromDecimalToType.Body),
						fromDecimalToType.Parameters);
				}
			}

			return base.TryGetConvertExpression(from, to);
		}

		static void ConvertDateTimeToSql(StringBuilder stringBuilder, SqlDataType dataType, DateTime value)
		{
#if SUPPORTS_COMPOSITE_FORMAT
			CompositeFormat format;
#else
			string format;
#endif
			switch (dataType.Type.DataType)
			{
				case DataType.Date:
					format = DATE_FORMAT;
					break;
				case DataType.DateTime2:
					switch (dataType.Type.Precision)
					{
						case 0   : format = TIMESTAMP0_FORMAT; break;
						case 1   : format = TIMESTAMP1_FORMAT; break;
						case 2   : format = TIMESTAMP2_FORMAT; break;
						case 3   : format = TIMESTAMP3_FORMAT; break;
						case 4   : format = TIMESTAMP4_FORMAT; break;
						case 5   : format = TIMESTAMP5_FORMAT; break;
						// .net types doesn't support more than 7 digits, so it doesn't make sense to generate 8/9
						case >= 7: format = TIMESTAMP7_FORMAT; break;
						default  : format = TIMESTAMP6_FORMAT; break;
					}

					break;
				case DataType.DateTimeOffset:
					// just use UTC literal
					value = value.ToUniversalTime();
					switch (dataType.Type.Precision)
					{
						case 0   : format = TIMESTAMPTZ0_FORMAT; break;
						case 1   : format = TIMESTAMPTZ1_FORMAT; break;
						case 2   : format = TIMESTAMPTZ2_FORMAT; break;
						case 3   : format = TIMESTAMPTZ3_FORMAT; break;
						case 4   : format = TIMESTAMPTZ4_FORMAT; break;
						case 5   : format = TIMESTAMPTZ5_FORMAT; break;
						// .net types doesn't support more than 7 digits, so it doesn't make sense to generate 8/9
						case >= 7: format = TIMESTAMPTZ7_FORMAT; break;
						default  : format = TIMESTAMPTZ6_FORMAT; break;
					}

					break;
				case DataType.DateTime:
				default:
					format = DATETIME_FORMAT;
					break;
			}

			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, value);
		}

#if SUPPORTS_DATEONLY
		static void ConvertDateOnlyToSql(StringBuilder stringBuilder, DateOnly value)
		{
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
		}
#endif

		internal static readonly OracleMappingSchema Instance = new ();

		public sealed class NativeMappingSchema : LockedMappingSchema
		{
			public NativeMappingSchema() : base(ProviderName.OracleNative, OracleProviderAdapter.GetInstance(OracleProvider.Native).MappingSchema, Instance)
			{
			}
		}

		public sealed class ManagedMappingSchema : LockedMappingSchema
		{
			public ManagedMappingSchema() : base(ProviderName.OracleManaged, OracleProviderAdapter.GetInstance(OracleProvider.Managed).MappingSchema, Instance)
			{
			}
		}

		public sealed class DevartMappingSchema : LockedMappingSchema
		{
			public DevartMappingSchema() : base(ProviderName.OracleDevart, OracleProviderAdapter.GetInstance(OracleProvider.Devart).MappingSchema, Instance)
			{
			}
		}

		public sealed class Native11MappingSchema : LockedMappingSchema
		{
			public Native11MappingSchema() : base(ProviderName.Oracle11Native, OracleProviderAdapter.GetInstance(OracleProvider.Native).MappingSchema, Instance)
			{
			}
		}

		public sealed class Managed11MappingSchema : LockedMappingSchema
		{
			public Managed11MappingSchema() : base(ProviderName.Oracle11Managed, OracleProviderAdapter.GetInstance(OracleProvider.Managed).MappingSchema, Instance)
			{
			}
		}

		public sealed class Devart11MappingSchema : LockedMappingSchema
		{
			public Devart11MappingSchema() : base(ProviderName.Oracle11Devart, OracleProviderAdapter.GetInstance(OracleProvider.Devart).MappingSchema, Instance)
			{
			}
		}
	}
}
