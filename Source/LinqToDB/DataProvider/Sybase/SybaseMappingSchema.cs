using System;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.Sybase
{
	using Mapping;
	using SqlQuery;

	public class SybaseMappingSchema : MappingSchema
	{
		public SybaseMappingSchema() : this(ProviderName.Sybase)
		{
		}

		protected SybaseMappingSchema(string configuration) : base(configuration)
		{
			SetValueToSqlConverter(typeof(String)  , (sb, dt, v) => ConvertStringToSql(sb, v.ToString()));
			SetValueToSqlConverter(typeof(Char)    , (sb, dt, v) => ConvertCharToSql  (sb, (char)v));
			SetValueToSqlConverter(typeof(TimeSpan), (sb, dt, v) => ConvertTimeSpanToSql(sb, dt, (TimeSpan)v));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));
		}

		static void ConvertTimeSpanToSql(StringBuilder stringBuilder, SqlDataType sqlDataType, TimeSpan value)
		{
			if (sqlDataType.DataType == DataType.Int64)
			{
				stringBuilder.Append(value.Ticks);
			}
			else
			{
				var format = "hh\\:mm\\:ss\\.fff";

				stringBuilder
					.Append('\'')
					.Append(value.ToString(format))
					.Append('\'')
					;
			}
		}

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
			string startPrefix = null;

			if (value.Any(ch => ch > 127))
				startPrefix = "N";

			DataTools.ConvertStringToSql(stringBuilder, "+", startPrefix, AppendConversion, value, null);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, char value)
		{
			var start = value > 127 ? "N'" : "'";
			DataTools.ConvertCharToSql(stringBuilder, start, AppendConversion, value);
		}

		internal static readonly SybaseMappingSchema Instance = new SybaseMappingSchema();

		public class NativeMappingSchema : MappingSchema
		{
			public NativeMappingSchema()
				: base(ProviderName.Sybase, Instance)
			{
			}
		}

		public class ManagedMappingSchema : MappingSchema
		{
			public ManagedMappingSchema()
				: base(ProviderName.SybaseManaged, Instance)
			{
			}
		}
	}
}
