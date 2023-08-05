namespace LinqToDB.DataProvider.DB2
{
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	sealed class DB2zOSSqlBuilder : DB2SqlBuilderBase
	{
		public DB2zOSSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		DB2zOSSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new DB2zOSSqlBuilder(this);
		}

		protected override DB2Version Version => DB2Version.zOS;

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.Type.DataType)
			{
				case DataType.VarBinary:
					// https://www.ibm.com/docs/en/db2-for-zos/12?topic=strings-varying-length-binary
					StringBuilder
						.Append("VARBINARY(")
						.Append(type.Type.Length == null || type.Type.Length > 32704 || type.Type.Length < 1 ? 32704 : type.Type.Length)
						.Append(')');
					return;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
		}
	}
}
