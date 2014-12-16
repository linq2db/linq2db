using System;

namespace LinqToDB.DataProvider.Informix
{
	using Mapping;

	public class InformixMappingSchema : MappingSchema
	{
		public InformixMappingSchema() : this(ProviderName.Informix)
		{
		}

		protected InformixMappingSchema(string configuration) : base(configuration)
		{
			ColumnComparisonOption = StringComparison.OrdinalIgnoreCase;

			SetValueToSqlConverter(typeof(bool), (sb,v) => sb.Append("'").Append((bool)v ? 't' : 'f').Append("'"));
		}
	}
}
