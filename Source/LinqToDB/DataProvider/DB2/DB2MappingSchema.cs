using System;
using System.Text;

namespace LinqToDB.DataProvider.DB2
{
	using LinqToDB.Common;
	using Mapping;
	using SqlQuery;
	using System.Data.Linq;

	public class DB2MappingSchema : MappingSchema
	{
		public DB2MappingSchema() : this(ProviderName.DB2)
		{
		}

		protected DB2MappingSchema(string configuration) : base(configuration)
		{
			SetValueToSqlConverter(typeof(Guid), (sb,dt,v) => ConvertGuidToSql(sb, (Guid)v));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));

			SetValueToSqlConverter(typeof(string),   (sb,dt,v) => ConvertStringToSql  (sb, v.ToString()));
			SetValueToSqlConverter(typeof(char),     (sb,dt,v) => ConvertCharToSql    (sb, (char)v));
			SetValueToSqlConverter(typeof(byte[]),   (sb,dt,v) => ConvertBinaryToSql  (sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),   (sb,dt,v) => ConvertBinaryToSql  (sb, ((Binary)v).ToArray()));
			SetValueToSqlConverter(typeof(TimeSpan), (sb,dt,v) => ConvertTimeToSql    (sb, (TimeSpan)v));
			SetValueToSqlConverter(typeof(DateTime), (sb,dt,v) => ConvertDateTimeToSql(sb, dt, (DateTime)v));
		}

		static void ConvertTimeToSql(StringBuilder stringBuilder, TimeSpan time)
		{
			stringBuilder.Append($"'{time:hh\\:mm\\:ss}'");
		}

		static string GetTimestampFormat(SqlDataType type)
		{
			var precision = type.Type.Precision;

			if (precision == null && type.Type.DbType != null)
			{
				var dbtype = type.Type.DbType.ToLowerInvariant();
				if (dbtype.StartsWith("timestamp("))
				{
					int fromDbType;
					if (int.TryParse(dbtype.Substring(10, dbtype.Length - 11), out fromDbType))
						precision = fromDbType;
				}
			}

			precision = precision == null || precision < 0 ? 6 : (precision > 7 ? 7 : precision);
			switch (precision)
			{
				case 0: return "yyyy-MM-dd-HH.mm.ss";
				case 1: return "yyyy-MM-dd-HH.mm.ss.f";
				case 2: return "yyyy-MM-dd-HH.mm.ss.ff";
				case 3: return "yyyy-MM-dd-HH.mm.ss.fff";
				case 4: return "yyyy-MM-dd-HH.mm.ss.ffff";
				case 5: return "yyyy-MM-dd-HH.mm.ss.fffff";
				case 6: return "yyyy-MM-dd-HH.mm.ss.ffffff";
				case 7: return "yyyy-MM-dd-HH.mm.ss.fffffff";
			}

			throw new InvalidOperationException();
		}

		static void ConvertDateTimeToSql(StringBuilder stringBuilder, SqlDataType type, DateTime value)
		{
			stringBuilder.Append("'");
			if (type.Type.DataType == DataType.Date || "date".Equals(type.Type.DbType, StringComparison.OrdinalIgnoreCase))
				stringBuilder.Append(value.ToString("yyyy-MM-dd-HH.mm.ss"));
			else
				stringBuilder.Append(value.ToString(GetTimestampFormat(type)));
			stringBuilder.Append("'");
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("BX'");

			foreach (var b in value)
				stringBuilder.Append(b.ToString("X2"));

			stringBuilder.Append("'");
		}

		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder
				.Append("chr(")
				.Append(value)
				.Append(")")
				;
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, string value)
		{
			DataTools.ConvertStringToSql(stringBuilder, "||", null, AppendConversion, value, null);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			DataTools.ConvertCharToSql(stringBuilder, "'", AppendConversion, value);
		}

		internal static readonly DB2MappingSchema Instance = new DB2MappingSchema();

		static void ConvertGuidToSql(StringBuilder stringBuilder, Guid value)
		{
			var s = value.ToString("N");

			stringBuilder
				.Append("Cast(x'")
				.Append(s.Substring( 6,  2))
				.Append(s.Substring( 4,  2))
				.Append(s.Substring( 2,  2))
				.Append(s.Substring( 0,  2))
				.Append(s.Substring(10,  2))
				.Append(s.Substring( 8,  2))
				.Append(s.Substring(14,  2))
				.Append(s.Substring(12,  2))
				.Append(s.Substring(16, 16))
				.Append("' as char(16) for bit data)")
				;
		}
	}

	public class DB2zOSMappingSchema : MappingSchema
	{
		public DB2zOSMappingSchema()
			: base(ProviderName.DB2zOS, DB2MappingSchema.Instance)
		{
		}

		public DB2zOSMappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.DB2zOS, Array<MappingSchema>.Append(schemas, DB2MappingSchema.Instance))
		{
		}
	}

	public class DB2LUWMappingSchema : MappingSchema
	{
		public DB2LUWMappingSchema()
			: base(ProviderName.DB2LUW, DB2MappingSchema.Instance)
		{
		}

		public DB2LUWMappingSchema(params MappingSchema[] schemas)
				: base(ProviderName.DB2LUW, Array<MappingSchema>.Append(schemas, DB2MappingSchema.Instance))
		{
		}
	}
}
