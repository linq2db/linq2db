namespace LinqToDB.DataProvider.Oracle
{
	using SqlQuery;
	using SqlProvider;
	using Mapping;

	partial class Oracle11SqlBuilder : OracleSqlBuilderBase
	{
		public Oracle11SqlBuilder(OracleDataProvider provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{ }

		protected Oracle11SqlBuilder(Oracle11SqlBuilder parentBuilder) : base(parentBuilder)
		{ }

		protected override BasicSqlBuilder<OracleDataProvider> CreateSqlBuilder()
			=> new Oracle11SqlBuilder(this) { HintBuilder = HintBuilder };

		protected override string GetPhysicalTableName(ISqlTableSource table, string? alias, bool ignoreTableExpression = false, string? defaultDatabaseName = null)
		{
			var name = base.GetPhysicalTableName(table, alias, ignoreTableExpression, defaultDatabaseName);

			if (table.SqlTableType == SqlTableType.Function)
				return $"TABLE({name})";

			return name;
		}
	}
}
