using System;
using System.Collections;
using System.Data.Linq;
using System.Text;

using LinqToDB.Data;
using LinqToDB.DataProvider.MySql;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Mapping;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.MySql
{
	public sealed class MySqlMappingSchema : LockedMappingSchema
	{
		MySqlMappingSchema() : base(ProviderName.MySql)
		{
			SetValueToSqlConverter(typeof(string), (sb,_,_,v) => ConvertStringToSql(sb, (string)v));
			SetValueToSqlConverter(typeof(char),   (sb,_,_,v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]), (sb,_,_,v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary), (sb,_,_,v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));
			SetValueToSqlConverter(typeof(float[]),(sb,_,_,v) => ConvertVectorToSql(sb, (float[])v));

			SetDataType(typeof(string),  new SqlDataType(DataType.NVarChar, typeof(string)));
			SetDataType(typeof(decimal), new SqlDataType(DataType.Decimal, typeof(decimal), 29, 10));
			SetDataType(typeof(float[]), new SqlDataType(new DbDataType(typeof(float[]), DataType.Vector32)));

			// both providers doesn't support BitArray directly and map bit fields to ulong by default
			SetConvertExpression<BitArray?, DataParameter>(ba => new DataParameter(null, ba == null ? null : GetBits(ba), DataType.UInt64), false);

			// vector provider-to-client translations
			SetConvertExpression<byte[], float[]>(bytes => ConvertToVector(bytes), conversionType: ConversionType.FromDatabase);
#if NET8_0_OR_GREATER
			SetConvertExpression<ReadOnlyMemory<float>, float[]>(v => v.ToArray(), conversionType: ConversionType.FromDatabase);
#endif
		}

		private float[] ConvertToVector(byte[] bytes)
		{
			if (bytes.Length % 4 != 0)
				throw new InvalidOperationException($"Server send vector data of invalid size: {bytes.Length}");

			var result = new float[bytes.Length / 4];

			for (var i = 0; i < bytes.Length; i += 4)
				result[i / 4] = BitConverter.ToSingle(bytes, i);

			return result;
		}

		static ulong GetBits(BitArray ba)
		{
			// mysql supports bit(64) max, so we use 8 bytes
			var data = new byte[8];
			ba.CopyTo(data, 0);
			return BitConverter.ToUInt64(data, 0);
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			stringBuilder
				.Append('\'')
				.Append(value.Replace("\\", "\\\\").Replace("'",  "''"))
				.Append('\'');
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			if (value == '\\')
			{
				stringBuilder.Append("\\\\");
			}
			else
			{
				stringBuilder.Append('\'');

				if (value == '\'') stringBuilder.Append("''");
				else               stringBuilder.Append(value);

				stringBuilder.Append('\'');
			}
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("0x");

			stringBuilder.AppendByteArrayAsHexViaLookup32(value);
		}

		static void ConvertVectorToSql(StringBuilder stringBuilder, float[] value)
		{
			stringBuilder.Append("0x");

			foreach (var val in value)
				stringBuilder.AppendByteArrayAsHexViaLookup32(BitConverter.GetBytes(val));
		}

		internal static readonly MySqlMappingSchema     Instance          = new ();
		internal static readonly MySql57MappingSchema   MySql57Instance   = new ();
		internal static readonly MySql80MappingSchema   MySql80Instance   = new ();
		internal static readonly MariaDB10MappingSchema MariaDB10Instance = new ();

		public sealed class MySql57MappingSchema() : LockedMappingSchema(ProviderName.MySql57, Instance);

		public sealed class MySql80MappingSchema() : LockedMappingSchema(ProviderName.MySql80, Instance);

		public sealed class MariaDB10MappingSchema() : LockedMappingSchema(ProviderName.MariaDB10, Instance);

		public sealed class MySqlData57MappingSchema() : LockedMappingSchema(ProviderName.MySql57MySqlData, MySqlProviderAdapter.GetInstance(MySqlProvider.MySqlData).MappingSchema, MySql57Instance);

		public sealed class MySqlData80MappingSchema() : LockedMappingSchema(ProviderName.MySql80MySqlData, MySqlProviderAdapter.GetInstance(MySqlProvider.MySqlData).MappingSchema, MySql80Instance);

		public sealed class MySqlDataMariaDB10MappingSchema() : LockedMappingSchema(ProviderName.MariaDB10MySqlData, MySqlProviderAdapter.GetInstance(MySqlProvider.MySqlData).MappingSchema, MariaDB10Instance);

		public sealed class MySqlConnector57MappingSchema() : LockedMappingSchema(ProviderName.MySql57MySqlConnector, MySqlProviderAdapter.GetInstance(MySqlProvider.MySqlConnector).MappingSchema, MySql57Instance);

		public sealed class MySqlConnector80MappingSchema() : LockedMappingSchema(ProviderName.MySql80MySqlConnector, MySqlProviderAdapter.GetInstance(MySqlProvider.MySqlConnector).MappingSchema, MySql80Instance);

		public sealed class MySqlConnectorMariaDB10MappingSchema() : LockedMappingSchema(ProviderName.MariaDB10MySqlConnector, MySqlProviderAdapter.GetInstance(MySqlProvider.MySqlConnector).MappingSchema, MariaDB10Instance);
	}
}
