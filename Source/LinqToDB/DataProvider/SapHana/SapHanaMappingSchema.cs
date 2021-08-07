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
		public SapHanaMappingSchema() : this(ProviderName.SapHana)
		{
		}

		protected SapHanaMappingSchema(string configuration) : base(configuration)
		{
			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));

			SetValueToSqlConverter(typeof(string), (sb, dt, v) => ConvertStringToSql(sb, v.ToString()!));
			SetValueToSqlConverter(typeof(char)  , (sb, dt, v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]), (sb, dt, v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary), (sb, dt, v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));
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

		public class NativeMappingSchema : MappingSchema
		{
			public NativeMappingSchema()
				: base(ProviderName.SapHanaNative, SapHanaMappingSchema.Instance)
			{
			}

			public static NativeMappingSchema Instance { get; } = new();
		}

		public class OdbcMappingSchema : MappingSchema
		{
			public OdbcMappingSchema()
				: base(ProviderName.SapHanaOdbc, SapHanaMappingSchema.Instance)
			{
			}

			public static OdbcMappingSchema Instance { get; } = new();
		}

		public class Native2SPS04MappingSchema : MappingSchema
		{
			public Native2SPS04MappingSchema()
				: base(ProviderName.SapHana2SPS04Native, SapHanaMappingSchema.Instance)
			{
			}

			public static Native2SPS04MappingSchema Instance { get; } = new();
		}

		public class Odbc2SPS04MappingSchema : MappingSchema
		{
			public Odbc2SPS04MappingSchema()
				: base(ProviderName.SapHana2SPS04Odbc, SapHanaMappingSchema.Instance)
			{
			}

			public static Odbc2SPS04MappingSchema Instance { get; } = new();
		}
	}
}
