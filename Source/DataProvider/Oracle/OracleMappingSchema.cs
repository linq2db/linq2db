using System;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using Expressions;
	using Mapping;

	public class OracleMappingSchema : MappingSchema
	{
		public OracleMappingSchema() : this(ProviderName.Oracle)
		{
		}

		protected OracleMappingSchema(string configuration) : base(configuration)
		{
			ColumnComparisonOption = StringComparison.OrdinalIgnoreCase;

			SetDataType(typeof(Guid), DataType.Guid);

			SetConvertExpression<decimal,TimeSpan>(v => new TimeSpan((long)v));

			SetValueToSqlConverter(typeof(Guid),     (sb,v) => ConvertGuidToSql    (sb, (Guid)    v));
			SetValueToSqlConverter(typeof(DateTime), (sb,v) => ConvertDateTimeToSql(sb, (DateTime)v));
		}

		public override LambdaExpression TryGetConvertExpression(Type from, Type to)
		{
			if (to.IsEnum && from == typeof(decimal))
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

		static void ConvertDateTimeToSql(StringBuilder stringBuilder, DateTime value)
		{
			var format = "TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.fffffff}', 'YYYY-MM-DD HH24:MI:SS.FF7')";

			if (value.Millisecond == 0)
			{
				format = value.Hour == 0 && value.Minute == 0 && value.Second == 0 ?
					"TO_DATE('{0:yyyy-MM-dd}', 'YYYY-MM-DD')" :
					"TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')";
			}

			stringBuilder.AppendFormat(format, value);
		}
	}
}
