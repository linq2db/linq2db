using LinqToDB.Common;
using LinqToDB.DataProvider;
using LinqToDB.Mapping;

namespace LinqToDB.SqlProvider
{
	public abstract class SqlBuilder<T> : SqlBuilder
	where T : DataProviderOptions<T>, IOptionSet, new()
	{
		protected SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected SqlBuilder(SqlBuilder parentBuilder)
			: base(parentBuilder)
		{
		}

		private T? _providerOptions;
		public  T   ProviderOptions => _providerOptions ??= DataOptions.FindOrDefault(DataProviderOptions<T>.Default);
	}
}
