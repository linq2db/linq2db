using System;
using System.Data.Linq;
using System.Globalization;
using System.Text;

namespace LinqToDB.DataProvider.Access
{
	using Common;
	using Mapping;
	using SqlQuery;

	sealed class AccessMappingSchema : LockedMappingSchema
	{
#if SUPPORTS_COMPOSITE_FORMAT
		private static readonly CompositeFormat DATE_FORMAT     = CompositeFormat.Parse("#{0:yyyy-MM-dd}#");
		private static readonly CompositeFormat DATETIME_FORMAT = CompositeFormat.Parse("#{0:yyyy-MM-dd HH:mm:ss}#");
#else
		private const string DATE_FORMAT     = "#{0:yyyy-MM-dd}#";
		private const string DATETIME_FORMAT = "#{0:yyyy-MM-dd HH:mm:ss}#";
#endif

		AccessMappingSchema() : base(ProviderName.Access)
		{
			SetDataType(typeof(DateTime),  DataType.DateTime);

			SetValueToSqlConverter(typeof(bool),     (sb,_,_,v) => sb.Append((bool)v));
			SetValueToSqlConverter(typeof(Guid),     (sb,_,_,v) => sb.Append(CultureInfo.InvariantCulture, $"'{(Guid)v:B}'"));
			SetValueToSqlConverter(typeof(DateTime), (sb,_,_,v) => ConvertDateTimeToSql(sb, (DateTime)v));
#if NET6_0_OR_GREATER
			SetValueToSqlConverter(typeof(DateOnly), (sb,_,_,v) => ConvertDateOnlyToSql(sb, (DateOnly)v));
#endif

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));

			SetValueToSqlConverter(typeof(string),   (sb,_,_,v) => ConvertStringToSql  (sb, (string)v));
			SetValueToSqlConverter(typeof(char),     (sb,_,_,v) => ConvertCharToSql    (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]),   (sb,_,_,v) => ConvertBinaryToSql  (sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),   (sb,_,_,v) => ConvertBinaryToSql  (sb, ((Binary)v).ToArray()));

			// Why:
			// 1. Access use culture-specific string format for decimals
			// 2. we need to use string type for decimal parameters
			// This leads to issues with database data parsing as ConvertBuilder will generate parse with InvariantCulture
			// We need to specify culture-specific converter explicitly
			SetConvertExpression((string v) => decimal.Parse(v, NumberFormatInfo.InvariantInfo));
			SetConvertExpression((string v) => float.Parse(v, NumberFormatInfo.InvariantInfo));
			SetConvertExpression((string v) => double.Parse(v, NumberFormatInfo.InvariantInfo));
			SetConvertExpression((string v) => v == "-1");
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder
				.Append("0x")
				.AppendByteArrayAsHexViaLookup32(value);
		}

		static readonly Action<StringBuilder, int> _appendConversionAction = AppendConversion;

		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder.Append(CultureInfo.InvariantCulture, $"chr({value})");
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			DataTools.ConvertStringToSql(stringBuilder, "+", null, _appendConversionAction, value, null);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", _appendConversionAction, value);
		}

		static void ConvertDateTimeToSql(StringBuilder stringBuilder, DateTime value)
		{
			var format = value.Hour == 0 && value.Minute == 0 && value.Second == 0 ? DATE_FORMAT : DATETIME_FORMAT;

			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, value);
		}

#if NET6_0_OR_GREATER
		static void ConvertDateOnlyToSql(StringBuilder stringBuilder, DateOnly value)
		{
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
		}
#endif

		internal static readonly AccessMappingSchema Instance = new ();

		public sealed class OleDbMappingSchema : LockedMappingSchema
		{
			public OleDbMappingSchema() : base(ProviderName.Access, Instance)
			{
			}
		}

		public sealed class OdbcMappingSchema : LockedMappingSchema
		{
			public OdbcMappingSchema() : base(ProviderName.AccessOdbc, Instance)
			{
			}
		}
	}
}
