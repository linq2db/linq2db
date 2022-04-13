using System;
using System.Data.Linq;
using System.Text;

namespace LinqToDB.DataProvider.SapHana
{
	using Common;
	using Mapping;
	using SqlQuery;

	public class SapHanaMappingSchema : MappingSchema
	{
		SapHanaMappingSchema() : base(ProviderName.SapHana)
		{
			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));

			SetValueToSqlConverter(typeof(string), (sb, _, v) => ConvertStringToSql(sb, v.ToString()!));
			SetValueToSqlConverter(typeof(char)  , (sb, _, v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]), (sb, _, v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary), (sb, _, v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));

			CreateID();
		}

		static readonly Action<StringBuilder, int> AppendConversionAction = AppendConversion;
		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			// char works with values in 0..255 range
			// this is fine as long as we use it only for \0 character
			stringBuilder
				.Append("char(")
				.Append(value)
				.Append(')')
				;
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

		public override bool IsFluentMappingSupported => false;

		public sealed class NativeMappingSchema : MappingSchema
		{
			public NativeMappingSchema()
				: base(ProviderName.SapHanaNative, Instance)
			{
				CreateID(ref _id);
			}

			static int? _id;

			public override bool IsFluentMappingSupported => false;
		}

		public sealed class OdbcMappingSchema : MappingSchema
		{
			public OdbcMappingSchema()
				: base(ProviderName.SapHanaOdbc, Instance)
			{
				CreateID(ref _id);
			}

			static int? _id;

			public override bool IsFluentMappingSupported => false;
		}
	}
}
