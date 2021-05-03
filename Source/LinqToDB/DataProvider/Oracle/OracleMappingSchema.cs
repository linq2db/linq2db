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

			SetValueToSqlConverter(typeof(Guid),     (sb,dt,v) => ConvertGuidToSql    (sb,     (Guid)    v));
			SetValueToSqlConverter(typeof(DateTime), (sb,dt,v) => ConvertDateTimeToSql(sb, dt, (DateTime)v));
			SetValueToSqlConverter(typeof(DateTimeOffset), (sb, dt, v) => ConvertDateTimeToSql(sb, dt, ((DateTimeOffset)v).UtcDateTime));
			SetValueToSqlConverter(typeof(string)        , (sb, dt, v) => ConvertStringToSql  (sb, v.ToString()!));
			SetValueToSqlConverter(typeof(char)          , (sb, dt, v) => ConvertCharToSql    (sb, (char)v));
			SetValueToSqlConverter(typeof(double), (sb, dt, v) => sb.Append(((double)v).ToString("G17", NumberFormatInfo.InvariantInfo)).Append('D'));
			SetValueToSqlConverter(typeof(byte[]), (sb, dt, v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary), (sb, dt, v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("HEXTORAW('");

			stringBuilder.AppendByteArrayAsHexViaLookup32(value);

			stringBuilder.Append("')");
		}

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
			DataTools.ConvertStringToSql(stringBuilder, "||", null, AppendConversion, value, null);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversion, value);
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
					format = "DATE '{0:yyyy-MM-dd}'";
					break;
				case DataType.DateTime2:
					switch (dataType.Type.Precision)
					{
						case 0: format = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss}'"          ; break;
						case 1: format = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.f}'"        ; break;
						case 2: format = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ff}'"       ; break;
						case 3: format = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fff}'"      ; break;
						case 4: format = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffff}'"     ; break;
						case 5: format = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffff}'"    ; break;
						default:
						case 6: format = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffffff}'"   ; break;
						case 7: // .net types doesn't support more than 7 digits, so it doesn't make sense to generate 8/9
						case 8:
						case 9: format = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffffff}'"  ; break;
					}
					break;
				case DataType.DateTimeOffset:
					// just use UTC literal
					value = value.ToUniversalTime();
					switch (dataType.Type.Precision)
					{
						case 0: format = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss} +00:00'"        ; break;
						case 1: format = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.f} +00:00'"      ; break;
						case 2: format = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ff} +00:00'"     ; break;
						case 3: format = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fff} +00:00'"    ; break;
						case 4: format = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffff} +00:00'"   ; break;
						case 5: format = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffff} +00:00'"  ; break;
						default:
						case 6: format = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.ffffff} +00:00'" ; break;
						case 7:
						case 8:
						case 9: format = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffffff} +00:00'"; break;
					}
					break;
				case DataType.DateTime:
				default:
					format = "TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')";
					break;
			}

			stringBuilder.AppendFormat(format, value);
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
