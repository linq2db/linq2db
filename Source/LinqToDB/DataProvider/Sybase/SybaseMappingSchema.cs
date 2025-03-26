using System;
using System.Data.Linq;
using System.Globalization;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Sybase
{
	sealed class SybaseMappingSchema : LockedMappingSchema
	{
#if SUPPORTS_COMPOSITE_FORMAT
		private static readonly CompositeFormat TIME3_FORMAT = CompositeFormat.Parse("'{0:hh\\:mm\\:ss\\.fff}'");
#else
		private const string TIME3_FORMAT= "'{0:hh\\:mm\\:ss\\.fff}'";
#endif

		SybaseMappingSchema() : base(ProviderName.Sybase)
		{
			SetValueToSqlConverter(typeof(string)  , (StringBuilder sb, DbDataType _, DataOptions _, object v) => ConvertStringToSql  (sb, (string)v));
			SetValueToSqlConverter(typeof(char)    , (StringBuilder sb, DbDataType _, DataOptions _, object v) => ConvertCharToSql    (sb, (char)v));
			SetValueToSqlConverter(typeof(TimeSpan), (sb, dt, _, v) => ConvertTimeSpanToSql(sb, dt, (TimeSpan)v));
			SetValueToSqlConverter(typeof(byte[])  , (StringBuilder sb, DbDataType _, DataOptions _, object v) => ConvertBinaryToSql  (sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary)  , (StringBuilder sb, DbDataType _, DataOptions _, object v) => ConvertBinaryToSql  (sb, ((Binary)v).ToArray()));

			SetDataType(typeof(string), new DbDataType(typeof(string), DataType.NVarChar, null, 255));
			// in ASE DECIMAL=DECIMAL(18,0)
			SetDataType(typeof(decimal), new DbDataType(typeof(decimal), DataType.Decimal, null, null, 18, 10));

			SetDefaultValue(typeof(DateTime), new DateTime(1753, 1, 1));
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder
				.Append("0x")
				.AppendByteArrayAsHexViaLookup32(value);
		}

		static void ConvertTimeSpanToSql(StringBuilder stringBuilder, DbDataType sqlDataType, TimeSpan value)
		{
			if (sqlDataType.DataType == DataType.Int64)
			{
				stringBuilder.Append(value.Ticks.ToString(NumberFormatInfo.InvariantInfo));
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
			stringBuilder.Append(CultureInfo.InvariantCulture, $"char({value})");
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
