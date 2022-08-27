﻿using System.Data.Linq;
using System.Globalization;
using System.Text;

namespace LinqToDB.DataProvider.Sybase
{
	using Common;
	using Mapping;
	using SqlQuery;

	sealed class SybaseMappingSchema : LockedMappingSchema
	{
		private const string TIME3_FORMAT= "'{0:hh\\:mm\\:ss\\.fff}'";

		SybaseMappingSchema() : base(ProviderName.Sybase)
		{
			SetValueToSqlConverter(typeof(string)  , (sb, dt, v) => ConvertStringToSql(sb, v.ToString()!));
			SetValueToSqlConverter(typeof(char)    , (sb, dt, v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(TimeSpan), (sb, dt, v) => ConvertTimeSpanToSql(sb, dt, (TimeSpan)v));
			SetValueToSqlConverter(typeof(byte[])  , (sb, dt, v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary)  , (sb, dt, v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder
				.Append("0x")
				.AppendByteArrayAsHexViaLookup32(value);
		}

		static void ConvertTimeSpanToSql(StringBuilder stringBuilder, SqlDataType sqlDataType, TimeSpan value)
		{
			if (sqlDataType.Type.DataType == DataType.Int64)
			{
				stringBuilder.Append(value.Ticks);
			}
			else
			{
				// to match logic for values as parameters
				if (value < TimeSpan.Zero)
					value = TimeSpan.FromDays(1 - value.Days) + value;

				stringBuilder.AppendFormat(CultureInfo.InvariantCulture, TIME3_FORMAT, value);
			}
		}

		static readonly Action<StringBuilder, int> AppendConversionAction = AppendConversion;

		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder
				.Append("char(")
				.Append(value)
				.Append(')')
				;
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			DataTools.ConvertStringToSql(stringBuilder, "+", null, AppendConversionAction, value, null);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversionAction, value);
		}

		internal static readonly SybaseMappingSchema Instance = new ();

		public sealed class NativeMappingSchema : LockedMappingSchema
		{
			public NativeMappingSchema() : base(ProviderName.Sybase, Instance)
			{
			}
		}

		public sealed class ManagedMappingSchema : LockedMappingSchema
		{
			public ManagedMappingSchema() : base(ProviderName.SybaseManaged, Instance)
			{
			}
		}
	}
}
