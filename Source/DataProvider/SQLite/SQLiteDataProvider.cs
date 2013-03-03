using System;
using System.Data;

namespace LinqToDB.DataProvider.SQLite
{
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class SQLiteDataProvider : DynamicDataProviderBase
	{
		public SQLiteDataProvider()
			: this(ProviderName.SQLite, new SQLiteMappingSchema())
		{
		}

		protected SQLiteDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.IsSkipSupported       = false;
			SqlProviderFlags.IsSkipSupportedIfTake = true;

			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd());
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd());

			SetTypes("System.Data.SQLite", "SQLiteConnection", "SQLiteDataReader", "SQLiteParameter");
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new SQLiteSqlProvider(SqlProviderFlags);
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return new SQLiteSchemaProvider();
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			base.SetParameter(parameter, "@" + name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.UInt32    : dataType = DataType.Int64;    break;
				case DataType.UInt64    : dataType = DataType.Decimal;  break;
				case DataType.DateTime2 : dataType = DataType.DateTime; break;
			}

			base.SetParameterType(parameter, dataType);
		}
	}
}
