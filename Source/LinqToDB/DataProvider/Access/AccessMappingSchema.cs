using System;
using System.Data.Linq;
using System.Globalization;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Access
{
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

			SetValueToSqlConverter(typeof(bool),     (StringBuilder sb, DbDataType _, DataOptions _, object v) => sb.Append((bool)v));
			SetValueToSqlConverter(typeof(Guid),     (StringBuilder sb, DbDataType _, DataOptions _, object v) => sb.Append(CultureInfo.InvariantCulture, $"'{(Guid)v:B}'"));
			SetValueToSqlConverter(typeof(DateTime), (StringBuilder sb, DbDataType _, DataOptions _, object v) => ConvertDateTimeToSql(sb, (DateTime)v));
#if NET6_0_OR_GREATER
			SetValueToSqlConverter(typeof(DateOnly), (StringBuilder sb, DbDataType _, DataOptions _, object v) => ConvertDateOnlyToSql(sb, (DateOnly)v));
#endif

			SetDataType(typeof(string), new DbDataType(typeof(string), DataType.NVarChar, null, 255));

			SetValueToSqlConverter(typeof(string),   (StringBuilder sb, DbDataType _, DataOptions _, object v) => ConvertStringToSql  (sb, (string)v));
			SetValueToSqlConverter(typeof(char),     (StringBuilder sb, DbDataType _, DataOptions _, object v) => ConvertCharToSql    (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]),   (StringBuilder sb, DbDataType _, DataOptions _, object v) => ConvertBinaryToSql  (sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),   (StringBuilder sb, DbDataType _, DataOptions _, object v) => ConvertBinaryToSql  (sb, ((Binary)v).ToArray()));

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

		sealed class AccessOleDbMappingSchema : LockedMappingSchema
		{
			public AccessOleDbMappingSchema() : base("Access.OleDb")
			{
				// ODBC provider cannot handle this literal as:
				// https://ftp.zx.net.nz/pub/archive/ftp.microsoft.com/MISC/KB/en-us/170/117.HTM
				// Because ODBC defines the curly brace as an escape code for vendor specific escape clauses, you must turn off escape clause scanning when you use literal GUIDs in SQL statements with the Microsoft Access ODBC driver. Note that this functionality is not supported in the Microsoft Access ODBC driver that ships with MDAC 2.1 or later.
#if NETFRAMEWORK
				// NETFX format parser fails to digest format string (even v4.8)
				SetValueToSqlConverter(typeof(Guid), (StringBuilder sb, DbDataType _, DataOptions _, object v) => sb.Append('{').Append(CultureInfo.InvariantCulture, $"guid {(Guid)v:B}").Append('}'));
#else
				SetValueToSqlConverter(typeof(Guid), (StringBuilder sb, DbDataType _, DataOptions _, object v) => sb.Append(CultureInfo.InvariantCulture, $"{{guid {(Guid)v:B}}}"));
#endif
			}
		}

		private static readonly AccessMappingSchema      Instance      = new ();
		private static readonly AccessOleDbMappingSchema OleDbInstance = new ();

		public sealed class JetOleDbMappingSchema () : LockedMappingSchema(ProviderName.AccessJetOleDb, OleDbInstance, Instance);
		public sealed class JetOdbcDbMappingSchema() : LockedMappingSchema(ProviderName.AccessJetOdbc , Instance);
		public sealed class AceOleDbMappingSchema () : LockedMappingSchema(ProviderName.AccessAceOleDb, OleDbInstance, Instance);
		public sealed class AceOdbcDbMappingSchema() : LockedMappingSchema(ProviderName.AccessAceOdbc , Instance);
	}
}
