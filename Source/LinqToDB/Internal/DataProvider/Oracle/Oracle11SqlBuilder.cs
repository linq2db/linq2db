using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.Internal.DataProvider.Oracle
{
	sealed class Oracle11SqlBuilder : OracleSqlBuilderBase
	{
		public Oracle11SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		private Oracle11SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new Oracle11SqlBuilder(this) { HintBuilder = HintBuilder };
		}

		protected override string GetPhysicalTableName(ISqlTableSource table, string? alias,
			bool ignoreTableExpression = false, string? defaultDatabaseName = null, bool withoutSuffix = false)
		{
			var name = base.GetPhysicalTableName(table, alias, ignoreTableExpression : ignoreTableExpression, defaultDatabaseName : defaultDatabaseName, withoutSuffix : withoutSuffix);

			if (table.SqlTableType == SqlTableType.Function)
				return $"TABLE({name})";

			return name;
		}
	}
}
