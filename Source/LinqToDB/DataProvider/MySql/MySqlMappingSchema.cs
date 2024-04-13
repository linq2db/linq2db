using System;
using System.Collections;
using System.Data.Linq;
using System.Text;

namespace LinqToDB.DataProvider.MySql
{
	using Common;
	using Data;
	using Mapping;
	using SqlQuery;

	sealed class MySqlMappingSchema : LockedMappingSchema
	{
		MySqlMappingSchema() : base(ProviderName.MySql)
		{
			SetValueToSqlConverter(typeof(string), (sb,_,_,v) => ConvertStringToSql(sb, (string)v));
			SetValueToSqlConverter(typeof(char),   (sb,_,_,v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]), (sb,_,_,v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary), (sb,_,_,v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string)));

			// both providers doesn't support BitArray directly and map bit fields to ulong by default
			SetConvertExpression<BitArray?, DataParameter>(ba => new DataParameter(null, ba == null ? null : GetBits(ba), DataType.UInt64), false);
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

		internal static readonly MySqlMappingSchema Instance = new ();

		public sealed class MySqlOfficialMappingSchema : LockedMappingSchema
		{
			public MySqlOfficialMappingSchema(MappingSchema adapterSchema)
				: base(ProviderName.MySqlOfficial, adapterSchema, Instance)
			{
			}
		}

		public sealed class MySqlConnectorMappingSchema : LockedMappingSchema
		{
			public MySqlConnectorMappingSchema(MappingSchema adapterSchema)
				: base(ProviderName.MySqlConnector, adapterSchema, Instance)
			{
			}
		}
	}
}
