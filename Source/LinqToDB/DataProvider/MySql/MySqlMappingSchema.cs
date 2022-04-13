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

	public class MySqlMappingSchema : MappingSchema
	{
		MySqlMappingSchema() : base(ProviderName.MySql)
		{
			SetValueToSqlConverter(typeof(string), (sb,dt,v) => ConvertStringToSql(sb, v.ToString()!));
			SetValueToSqlConverter(typeof(char),   (sb,dt,v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]), (sb,dt,v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary), (sb,dt,v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string)));

			// both providers doesn't support BitArray directly and map bit fields to ulong by default
			SetConvertExpression<BitArray?, DataParameter>(ba => new DataParameter(null, ba == null ? (ulong?)null :GetBits(ba), DataType.UInt64), false);

			CreateID();
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

		internal static readonly MappingSchema Instance = new MySqlMappingSchema();

		public override bool IsFluentMappingSupported => false;

		public sealed class MySqlOfficialMappingSchema : MappingSchema
		{
			public MySqlOfficialMappingSchema()
				: base(ProviderName.MySqlOfficial, Instance)
			{
				CreateID(ref _id);
			}

			static int? _id;

			public MySqlOfficialMappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.MySqlOfficial, Array<MappingSchema>.Append(schemas, Instance))
			{
				CreateID();
			}

			public override bool IsFluentMappingSupported => false;
		}

		public sealed class MySqlConnectorMappingSchema : MappingSchema
		{
			public MySqlConnectorMappingSchema()
				: base(ProviderName.MySqlConnector, Instance)
			{
				CreateID(ref _id);
			}

			static int? _id;

			public MySqlConnectorMappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.MySqlConnector, Array<MappingSchema>.Append(schemas, Instance))
			{
				CreateID();
			}

			public override bool IsFluentMappingSupported => false;
		}
	}
}
