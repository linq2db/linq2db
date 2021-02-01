namespace LinqToDB.DataProvider.Firebird
{
	using LinqToDB.SqlQuery;
	using Mapping;
	using SqlProvider;

	public partial class Firebird3SqlBuilder : Firebird25SqlBuilder
	{
		public Firebird3SqlBuilder(
			FirebirdDataProvider? provider,
			MappingSchema         mappingSchema,
			ISqlOptimizer         sqlOptimizer,
			SqlProviderFlags      sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags, false)
		{
		}

		// remote context
		public Firebird3SqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(null, mappingSchema, sqlOptimizer, sqlProviderFlags, false)
		{
		}

		// for derived builders
		protected Firebird3SqlBuilder(
			FirebirdDataProvider? provider,
			MappingSchema         mappingSchema,
			ISqlOptimizer         sqlOptimizer,
			SqlProviderFlags      sqlProviderFlags,
			bool                  dialect1)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags, dialect1)
		{
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.Boolean: StringBuilder.Append("BOOLEAN"); return;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable);
		}
	}
}
