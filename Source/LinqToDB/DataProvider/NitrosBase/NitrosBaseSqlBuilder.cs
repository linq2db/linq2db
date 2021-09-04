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

		// this constructor is required for remote context and should always present
		public NitrosBaseSqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		// TODO: add/override base SQL generation implementation if needed

		// this method required for nested sql builder creation
		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new NitrosBaseSqlBuilder(_provider, MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			// default stub for MERGE sql statement generation
			// if database supports MERGE, this method should be removed
			// and merge-related SQL generation logic put to NitrosBaseSqlBuilder.Merge.cs file as partial class
			throw new LinqToDBException($"{Name} provider doesn't support SQL MERGE statement");
		}
	}
}
