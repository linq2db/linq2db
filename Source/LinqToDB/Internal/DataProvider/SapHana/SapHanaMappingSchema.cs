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
#if SUPPORTS_COMPOSITE_FORMAT
		private static readonly CompositeFormat TIMESTAMP_FORMAT = CompositeFormat.Parse("TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffffff}'");
#else
		private const string TIMESTAMP_FORMAT  = "TIMESTAMP '{0:yyyy-MM-dd HH:mm:ss.fffffff}'";
#endif

		SapHanaMappingSchema() : base(ProviderName.SapHana)
		{
			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));
			SetDataType(typeof(float[]), new SqlDataType(new DbDataType(typeof(float[]), DataType.Vector32, "REAL_VECTOR")));

			SetValueToSqlConverter(typeof(string),         (sb,_,_,v) => ConvertStringToSql(sb, (string)v));
			SetValueToSqlConverter(typeof(char)  ,         (sb,_,_,v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]),         (sb,_,_,v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),         (sb,_,_,v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));
			SetValueToSqlConverter(typeof(DateTime),       (sb,_,_,v) => BuildTimeStamp(sb, (DateTime)v));
			SetValueToSqlConverter(typeof(DateTimeOffset), (sb,_,_,v) => BuildTimeStamp(sb, ((DateTimeOffset)v).DateTime));
			SetDataType(typeof(decimal), new SqlDataType(DataType.Decimal, typeof(decimal), 38, 10));
		}

		static void BuildTimeStamp(StringBuilder stringBuilder, DateTime value)
		{
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, TIMESTAMP_FORMAT, value);
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

		public sealed class NativeMappingSchema() : LockedMappingSchema(ProviderName.SapHanaNative, new MappingSchema?[] { SapHanaProviderAdapter.GetInstance(SapHanaProvider.Unmanaged).MappingSchema, Instance }.Where(_ => _ != null).ToArray()!);

		public sealed class OdbcMappingSchema() : LockedMappingSchema(ProviderName.SapHanaOdbc, Instance);
	}
}
