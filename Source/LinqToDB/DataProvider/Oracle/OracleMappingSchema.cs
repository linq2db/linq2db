using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using Expressions;
	using Mapping;
	using SqlQuery;
	using System.Data.Linq;

	public class OracleMappingSchema : MappingSchema
	{
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

		public OracleMappingSchema() : this(ProviderName.Oracle)
		{
		}

		protected OracleMappingSchema(string configuration) : base(configuration)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

			SetDataType(typeof(Guid),   DataType.Guid);
			SetDataType(typeof(Guid?),  DataType.Guid);
			SetDataType(typeof(string), new SqlDataType(DataType.VarChar, typeof(string), 255));

			SetConvertExpression<decimal,TimeSpan>(v => new TimeSpan((long)v));

			SetValueToSqlConverter(typeof(Guid),     (sb,dt,v)         => ConvertGuidToSql    (sb,     (Guid)    v));
			SetValueToSqlConverter(typeof(DateTime), (sb,dt,v)         => ConvertDateTimeToSql(sb, dt, (DateTime)v));
			SetValueToSqlConverter(typeof(DateTimeOffset), (sb, dt, v) => ConvertDateTimeToSql(sb, dt, ((DateTimeOffset)v).UtcDateTime));
			SetValueToSqlConverter(typeof(string)        , (sb, dt, v) => ConvertStringToSql  (sb, v.ToString()!));
			SetValueToSqlConverter(typeof(char)          , (sb, dt, v) => ConvertCharToSql    (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]), (sb, dt, v)         => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary), (sb, dt, v)         => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));

			// adds floating point special values support
			SetValueToSqlConverter(typeof(float), (sb, dt, v) =>
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
			SetValueToSqlConverter(typeof(double), (sb, dt, v) =>
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
			stringBuilder.Append("HEXTORAW('");

			stringBuilder.AppendByteArrayAsHexViaLookup32(value);

			stringBuilder.Append("')");
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

		static void ConvertGuidToSql(StringBuilder stringBuilder, Guid value)
		{
			var s = value.ToString("N");

			stringBuilder
				.Append("Cast('")
				.Append(s.Substring( 6,  2))
				.Append(s.Substring( 4,  2))
				.Append(s.Substring( 2,  2))
				.Append(s.Substring( 0,  2))
				.Append(s.Substring(10,  2))
				.Append(s.Substring( 8,  2))
				.Append(s.Substring(14,  2))
				.Append(s.Substring(12,  2))
				.Append(s.Substring(16, 16))
				.Append("' as raw(16))")
				;
		}

		static void ConvertDateTimeToSql(StringBuilder stringBuilder, SqlDataType dataType, DateTime value)
		{
			string format;
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

		internal static readonly OracleMappingSchema Instance = new ();

		public class NativeMappingSchema : MappingSchema
		{
			public NativeMappingSchema()
				: base(ProviderName.OracleNative, Instance)
			{
			}

			public NativeMappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.OracleNative, Array<MappingSchema>.Append(schemas, Instance))
			{
			}
		}

		public class ManagedMappingSchema : MappingSchema
		{
			public ManagedMappingSchema()
				: base(ProviderName.OracleManaged, Instance)
			{
			}

			public ManagedMappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.OracleManaged, Array<MappingSchema>.Append(schemas, Instance))
			{
			}
		}
	}
}
