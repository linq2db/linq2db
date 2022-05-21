namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;
	using SqlProvider;
	using Mapping;

	class SqlServer2005SqlBuilder : SqlServerSqlBuilder
	{
		public SqlServer2005SqlBuilder(SqlServerDataProvider provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{ }

		SqlServer2005SqlBuilder(SqlServer2005SqlBuilder parentBuilder) : base(parentBuilder)
		{ }

		protected override BasicSqlBuilder<SqlServerDataProvider> CreateSqlBuilder()
			=> new SqlServer2005SqlBuilder(this);

		protected override bool IsValuesSyntaxSupported => false;

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.DateTimeOffset :
				case DataType.DateTime2      :
				case DataType.Time           :
				case DataType.Date           : StringBuilder.Append("DateTime");                     break;
				default                      : base.BuildDataTypeFromDataType(type, forCreateTable); break;
			}
		}

		public override string  Name => ProviderName.SqlServer2005;
	}
}
