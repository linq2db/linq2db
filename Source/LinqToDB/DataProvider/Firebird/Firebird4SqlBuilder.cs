namespace LinqToDB.DataProvider.Firebird
{
	using LinqToDB.SqlQuery;
	using Mapping;
	using SqlProvider;

	public partial class Firebird4SqlBuilder : Firebird3SqlBuilder
	{
		public Firebird4SqlBuilder(
			FirebirdDataProvider? provider,
			MappingSchema         mappingSchema,
			ISqlOptimizer         sqlOptimizer,
			SqlProviderFlags      sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags, false)
		{
		}

		// remote context
		public Firebird4SqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(null, mappingSchema, sqlOptimizer, sqlProviderFlags, false)
		{
		}

		// for derived builders
		protected Firebird4SqlBuilder(
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
				case DataType.Decimal:
					base.BuildDataTypeFromDataType(type.Type.Precision > 34 ? new SqlDataType(type.Type.DataType, type.Type.SystemType, null, 34, type.Type.Scale, type.Type.DbType) : type, forCreateTable);
					return;
					// TODO: currently FB 7.10.1 provider cannot work with this type (also restore type in db script)
				//case DataType.Int64:
				//	// FB4 allows INT128 use for storage in Dialect1, so we can use NUMERIC(19)
				//	// to fit all possible int64 values
				//	if (Dialect1)
				//	{
				//		StringBuilder.Append("NUMERIC(19)");
				//		return;
				//	}

				//	break;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable);
		}
	}
}
