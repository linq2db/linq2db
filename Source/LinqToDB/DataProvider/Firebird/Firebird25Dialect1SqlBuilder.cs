namespace LinqToDB.DataProvider.Firebird
{
	using Mapping;
	using SqlProvider;

	// this class is needed for LinqService to work
	// dialect-specific code should be added to base builder using Dialect1 flag
	public partial class Firebird25Dialect1SqlBuilder : Firebird25SqlBuilder
	{
		public Firebird25Dialect1SqlBuilder(
			FirebirdDataProvider? provider,
			MappingSchema         mappingSchema,
			ISqlOptimizer         sqlOptimizer,
			SqlProviderFlags      sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags, true)
		{
		}

		// remote context
		public Firebird25Dialect1SqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(null, mappingSchema, sqlOptimizer, sqlProviderFlags, true)
		{
		}
	}
}
