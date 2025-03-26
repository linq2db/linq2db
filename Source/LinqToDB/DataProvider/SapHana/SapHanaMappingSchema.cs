using System;
using System.Data.Linq;
using System.Globalization;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SapHana
{
	public class SapHanaMappingSchema : LockedMappingSchema
	{
		SapHanaMappingSchema() : base(ProviderName.SapHana)
		{
			SetDataType(typeof(string), new DbDataType(typeof(string), DataType.NVarChar, null, 255));

			SetValueToSqlConverter(typeof(string), (StringBuilder sb, DbDataType _, DataOptions _, object v) => ConvertStringToSql(sb, (string)v));
			SetValueToSqlConverter(typeof(char)  , (StringBuilder sb, DbDataType _, DataOptions _, object v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]), (StringBuilder sb, DbDataType _, DataOptions _, object v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary), (StringBuilder sb, DbDataType _, DataOptions _, object v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));
		}

		static readonly Action<StringBuilder, int> AppendConversionAction = AppendConversion;

		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			// char works with values in 0..255 range
			// this is fine as long as we use it only for \0 character
			stringBuilder.Append(CultureInfo.InvariantCulture, $"char({value})");
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("x'");

			stringBuilder.AppendByteArrayAsHexViaLookup32(value);

			stringBuilder.Append('\'');
		}

		internal static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			DataTools.ConvertStringToSql(stringBuilder, "||", null, AppendConversionAction, value, null);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversionAction, value);
		}

		internal static readonly SapHanaMappingSchema Instance = new ();

		public sealed class NativeMappingSchema : LockedMappingSchema
		{
			public NativeMappingSchema() : base(ProviderName.SapHanaNative, Instance)
			{
			}
		}

		public sealed class OdbcMappingSchema : LockedMappingSchema
		{
			public OdbcMappingSchema() : base(ProviderName.SapHanaOdbc, Instance)
			{
			}
		}
	}
}
