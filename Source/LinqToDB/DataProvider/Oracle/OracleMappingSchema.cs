using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using Expressions;
	using Extensions;
	using Mapping;
	using SqlQuery;
	using System.Data.Linq;

	public class OracleMappingSchema : MappingSchema
	{
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

			SetValueToSqlConverter(typeof(Guid),           (sb,dt,v) => ConvertGuidToSql    (sb,     (Guid)    v));
			SetValueToSqlConverter(typeof(DateTime),       (sb,dt,v) => ConvertDateTimeToSql(sb, dt, (DateTime)v));
			SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt,v) => ConvertDateTimeToSql(sb, dt, ((DateTimeOffset)v).DateTime));

			SetValueToSqlConverter(typeof(String),   (sb,dt,v) => ConvertStringToSql  (sb, v.ToString()));
			SetValueToSqlConverter(typeof(Char),     (sb,dt,v) => ConvertCharToSql    (sb, (char)v));

			SetValueToSqlConverter(typeof(double), (sb, dt, v) => sb.Append(((double)v).ToString("G17", NumberFormatInfo.InvariantInfo)).Append("D"));

			SetValueToSqlConverter(typeof(byte[]), (sb, dt, v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary), (sb, dt, v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("HEXTORAW('");

			foreach (var b in value)
				stringBuilder.Append(b.ToString("X2"));

			stringBuilder.Append("')");
		}

		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder
				.Append("chr(")
				.Append(value)
				.Append(")")
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

		public override LambdaExpression TryGetConvertExpression(Type from, Type to)
		{
			if (to.IsEnumEx() && from == typeof(decimal))
			{
				var type = Converter.GetDefaultMappingFromEnumType(this, to);

				if (type != null)
				{
					var fromDecimalToType = GetConvertExpression(from, type, false);
					var fromTypeToEnum    = GetConvertExpression(type, to,   false);

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
			switch (dataType.DataType)
			{
				case DataType.Date:
					format = "TO_DATE('{0:yyyy-MM-dd}', 'YYYY-MM-DD')";
					break;
				case DataType.DateTime2:
					switch (dataType.Precision)
					{
						case 0: format = "TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')"              ; break;
						case 1: format = "TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.f}', 'YYYY-MM-DD HH24:MI:SS.FF1')"        ; break;
						case 2: format = "TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.ff}', 'YYYY-MM-DD HH24:MI:SS.FF2')"       ; break;
						case 3: format = "TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.fff}', 'YYYY-MM-DD HH24:MI:SS.FF3')"      ; break;
						case 4: format = "TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.ffff}', 'YYYY-MM-DD HH24:MI:SS.FF4')"     ; break;
						case 5: format = "TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.fffff}', 'YYYY-MM-DD HH24:MI:SS.FF5')"    ; break;
						default:
						case 6: format = "TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.ffffff}', 'YYYY-MM-DD HH24:MI:SS.FF6')"   ; break;
						case 7: // .net types doesn't support more than 7 digits, so it doesn't make sense to generate 8/9
						case 8:
						case 9: format = "TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.fffffff}', 'YYYY-MM-DD HH24:MI:SS.FF7')"  ; break;
					}
					break;
				case DataType.DateTimeOffset:
					// just use UTC literal
					value = value.ToUniversalTime();
					switch (dataType.Precision)
					{
						case 0: format = "TO_TIMESTAMP_TZ('{0:yyyy-MM-dd HH:mm:ss} 00:00', 'YYYY-MM-DD HH24:MI:SS TZH:TZM')"              ; break;
						case 1: format = "TO_TIMESTAMP_TZ('{0:yyyy-MM-dd HH:mm:ss.f} 00:00', 'YYYY-MM-DD HH24:MI:SS.FF1 TZH:TZM')"        ; break;
						case 2: format = "TO_TIMESTAMP_TZ('{0:yyyy-MM-dd HH:mm:ss.ff} 00:00', 'YYYY-MM-DD HH24:MI:SS.FF2 TZH:TZM')"       ; break;
						case 3: format = "TO_TIMESTAMP_TZ('{0:yyyy-MM-dd HH:mm:ss.fff} 00:00', 'YYYY-MM-DD HH24:MI:SS.FF3 TZH:TZM')"      ; break;
						case 4: format = "TO_TIMESTAMP_TZ('{0:yyyy-MM-dd HH:mm:ss.ffff} 00:00', 'YYYY-MM-DD HH24:MI:SS.FF4 TZH:TZM')"     ; break;
						case 5: format = "TO_TIMESTAMP_TZ('{0:yyyy-MM-dd HH:mm:ss.fffff} 00:00', 'YYYY-MM-DD HH24:MI:SS.FF5 TZH:TZM')"    ; break;
						default:
						case 6: format = "TO_TIMESTAMP_TZ('{0:yyyy-MM-dd HH:mm:ss.ffffff} 00:00', 'YYYY-MM-DD HH24:MI:SS.FF6 TZH:TZM')"   ; break;
						case 7:
						case 8:
						case 9: format = "TO_TIMESTAMP_TZ('{0:yyyy-MM-dd HH:mm:ss.fffffff} 00:00', 'YYYY-MM-DD HH24:MI:SS.FF7 TZH:TZM')"; break;
					}
					break;
				case DataType.DateTime:
				default:
					format = "TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')";
					break;
			}

			stringBuilder.AppendFormat(format, value);
		}

		internal static readonly OracleMappingSchema Instance = new OracleMappingSchema();

		public class NativeMappingSchema : MappingSchema
		{
			public NativeMappingSchema()
				: base(ProviderName.OracleNative, Instance)
			{
			}
		}

		public class ManagedMappingSchema : MappingSchema
		{
			public ManagedMappingSchema()
				: base(ProviderName.OracleManaged, Instance)
			{
			}
		}
	}
}
