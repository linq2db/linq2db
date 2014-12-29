using System;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Mapping;

	public class PostgreSQLMappingSchema : MappingSchema
	{
		public PostgreSQLMappingSchema() : this(ProviderName.PostgreSQL)
		{
		}

		protected PostgreSQLMappingSchema(string configuration) : base(configuration)
		{
			ColumnComparisonOption = StringComparison.OrdinalIgnoreCase;

			SetDataType(typeof(string), DataType.Undefined);

			SetValueToSqlConverter(typeof(bool), (sb,dt,v) => sb.Append(v));
		}
	}
}
