namespace LinqToDB.DataProvider.Oracle
{
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	sealed class Oracle11SqlBuilder : OracleSqlBuilderBase
	{
		public Oracle11SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		private Oracle11SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new Oracle11SqlBuilder(this) { HintBuilder = HintBuilder };
		}

		protected override string GetPhysicalTableName(NullabilityContext nullability, ISqlTableSource table, string? alias, bool ignoreTableExpression = false, string? defaultDatabaseName = null)
		{
			var name = base.GetPhysicalTableName(nullability, table, alias, ignoreTableExpression, defaultDatabaseName);

			if (table.SqlTableType == SqlTableType.Function)
				return $"TABLE({name})";

			return name;
		}
	}
}
