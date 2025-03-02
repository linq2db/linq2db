using System;
using System.Collections;
using System.Data.Linq;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.MySql
{
	sealed class MySqlMappingSchema : LockedMappingSchema
	{
		MySqlMappingSchema() : base(ProviderName.MySql)
		{
			SetValueToSqlConverter(typeof(string), (StringBuilder sb, DbDataType _,  DataOptions _, object v) => ConvertStringToSql(sb, (string)v));
			SetValueToSqlConverter(typeof(char),   (StringBuilder sb, DbDataType _,  DataOptions _, object v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]), (StringBuilder sb, DbDataType _,  DataOptions _, object v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary), (StringBuilder sb, DbDataType _,  DataOptions _, object v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));

			SetDataType(typeof(string),  new DbDataType(typeof(string), DataType.NVarChar));
			SetDataType(typeof(decimal), new DbDataType(typeof(decimal), DataType.Decimal, null, null, 29, 10));

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

		internal static readonly MySqlMappingSchema     Instance          = new ();
		internal static readonly MySql57MappingSchema   MySql57Instance   = new ();
		internal static readonly MySql80MappingSchema   MySql80Instance   = new ();
		internal static readonly MariaDB10MappingSchema MariaDB10Instance = new ();

		public sealed class MySql57MappingSchema : LockedMappingSchema
		{
			public MySql57MappingSchema()
				: base(ProviderName.MySql57, Instance)
			{
			}
		}

		public sealed class MySql80MappingSchema : LockedMappingSchema
		{
			public MySql80MappingSchema()
				: base(ProviderName.MySql80, Instance)
			{
			}
		}

		public sealed class MariaDB10MappingSchema : LockedMappingSchema
		{
			public MariaDB10MappingSchema()
				: base(ProviderName.MariaDB10, Instance)
			{
			}
		}

		public sealed class MySqlData57MappingSchema : LockedMappingSchema
		{
			public MySqlData57MappingSchema()
				: base(ProviderName.MySql57MySqlData, MySqlProviderAdapter.GetInstance(MySqlProvider.MySqlData).MappingSchema, MySql57Instance)
			{
			}
		}

		public sealed class MySqlData80MappingSchema : LockedMappingSchema
		{
			public MySqlData80MappingSchema()
				: base(ProviderName.MySql80MySqlData, MySqlProviderAdapter.GetInstance(MySqlProvider.MySqlData).MappingSchema, MySql80Instance)
			{
			}
		}

		public sealed class MySqlDataMariaDB10MappingSchema : LockedMappingSchema
		{
			public MySqlDataMariaDB10MappingSchema()
				: base(ProviderName.MariaDB10MySqlData, MySqlProviderAdapter.GetInstance(MySqlProvider.MySqlData).MappingSchema, MariaDB10Instance)
			{
			}
		}

		public sealed class MySqlConnector57MappingSchema : LockedMappingSchema
		{
			public MySqlConnector57MappingSchema()
				: base(ProviderName.MySql57MySqlConnector, MySqlProviderAdapter.GetInstance(MySqlProvider.MySqlConnector).MappingSchema, MySql57Instance)
			{
			}
		}

		public sealed class MySqlConnector80MappingSchema : LockedMappingSchema
		{
			public MySqlConnector80MappingSchema()
				: base(ProviderName.MySql80MySqlConnector, MySqlProviderAdapter.GetInstance(MySqlProvider.MySqlConnector).MappingSchema, MySql80Instance)
			{
			}
		}

		public sealed class MySqlConnectorMariaDB10MappingSchema : LockedMappingSchema
		{
			public MySqlConnectorMariaDB10MappingSchema()
				: base(ProviderName.MariaDB10MySqlConnector, MySqlProviderAdapter.GetInstance(MySqlProvider.MySqlConnector).MappingSchema, MariaDB10Instance)
			{
			}
		}
	}
}
