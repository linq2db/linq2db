namespace LinqToDB.DataProvider.NitrosBase
{
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	class NitrosBaseSqlBuilder : BasicSqlBuilder
	{
		private readonly NitrosBaseDataProvider? _provider;

		public NitrosBaseSqlBuilder(
			NitrosBaseDataProvider? provider,
			MappingSchema           mappingSchema,
			ISqlOptimizer           sqlOptimizer,
			SqlProviderFlags        sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
			_provider = provider;
		}

		public NitrosBaseSqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder() => new NitrosBaseSqlBuilder(_provider, MappingSchema, SqlOptimizer, SqlProviderFlags);

		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			throw new LinqToDBException($"{Name} provider doesn't support SQL MERGE statement");
		}
	}
}
