using System;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Text;

using LinqToDB.DataProvider.SapHana;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Mapping;
using LinqToDB.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	public sealed class SapHanaMappingSchema : LockedMappingSchema
	{
		SapHanaMappingSchema() : base(ProviderName.SapHana)
		{
			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));
			SetDataType(typeof(float[]), new SqlDataType(new DbDataType(typeof(float[]), DataType.Vector32, "REAL_VECTOR")));

			SetValueToSqlConverter(typeof(string), (sb,_,_,v) => ConvertStringToSql(sb, (string)v));
			SetValueToSqlConverter(typeof(char)  , (sb,_,_,v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]), (sb,_,_,v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary), (sb,_,_,v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));
			SetDataType(typeof(decimal), new SqlDataType(DataType.Decimal, typeof(decimal), 38, 10));
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
			public NativeMappingSchema() : base(ProviderName.SapHanaNative, new MappingSchema?[] { SapHanaProviderAdapter.GetInstance(SapHanaProvider.Unmanaged).MappingSchema, Instance }.Where(_ => _ != null).ToArray()!)
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
