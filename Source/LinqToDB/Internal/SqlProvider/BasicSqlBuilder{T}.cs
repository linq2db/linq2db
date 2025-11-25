using LinqToDB.DataProvider;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Options;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.SqlProvider
{
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

		public T ProviderOptions => field ??= DataOptions.FindOrDefault(DataProviderOptions<T>.Default);
	}
}
