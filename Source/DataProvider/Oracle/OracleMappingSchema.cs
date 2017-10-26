using System;
using System.Linq.Expressions;
using System.Text;
using LinqToDB.Extensions;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using Expressions;
	using Mapping;
	using SqlQuery;
	using System.Globalization;

	public class OracleMappingSchema : MappingSchema
	{
		public OracleMappingSchema() : this(ProviderName.Oracle)
		{
		}

		protected OracleMappingSchema(string configuration) : base(configuration)
		{
			ColumnComparisonOption = StringComparison.OrdinalIgnoreCase;

			SetDataType(typeof(Guid),   DataType.Guid);
			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));

			SetConvertExpression<decimal,TimeSpan>(v => new TimeSpan((long)v));

			SetValueToSqlConverter(typeof(Guid),     (sb,dt,v) => ConvertGuidToSql    (sb,     (Guid)    v));
			SetValueToSqlConverter(typeof(DateTime), (sb,dt,v) => ConvertDateTimeToSql(sb, dt, (DateTime)v));

			SetValueToSqlConverter(typeof(String),   (sb,dt,v) => ConvertStringToSql  (sb, v.ToString()));
			SetValueToSqlConverter(typeof(Char),     (sb,dt,v) => ConvertCharToSql    (sb, (char)v));

			SetValueToSqlConverter(typeof(double), (sb, dt, v) => sb.Append(((double)v).ToString("G17", NumberFormatInfo.InvariantInfo)).Append("D"));
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
			if (value.Millisecond != 0 && (dataType.DataType == DataType.DateTime2 || dataType.DataType == DataType.Undefined))
				format = "TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.fffffff}', 'YYYY-MM-DD HH24:MI:SS.FF7')";
			else
				format = value.Hour == 0 && value.Minute == 0 && value.Second == 0 ?
					"TO_DATE('{0:yyyy-MM-dd}', 'YYYY-MM-DD')" :
					"TO_DATE('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')";

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
