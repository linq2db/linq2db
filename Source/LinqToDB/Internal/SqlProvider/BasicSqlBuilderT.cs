namespace LinqToDB.Internal.SqlProvider
{
	using LinqToDB.Common;
	using LinqToDB.DataProvider;
	using LinqToDB.Mapping;

	public abstract class BasicSqlBuilder<T> : BasicSqlBuilder
		where T : DataProviderOptions<T>, IOptionSet, new()
	{
		protected BasicSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected BasicSqlBuilder(BasicSqlBuilder parentBuilder)
			: base(parentBuilder)
		{
		}

		private T? _providerOptions;
		public  T   ProviderOptions => _providerOptions ??= DataOptions.FindOrDefault(DataProviderOptions<T>.Default);
	}
}
