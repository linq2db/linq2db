using System.Data.Common;

using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Access;
using LinqToDB.Internals.SqlProvider;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.Internals.DataProviders.Access
{
	sealed class AccessOleDbSqlBuilder : AccessSqlBuilderBase
	{
		public AccessOleDbSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		AccessOleDbSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new AccessOleDbSqlBuilder(this);
		}

		protected override string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (DataProvider is AccessDataProvider { Provider: AccessProvider.OleDb } provider)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
					return provider.Adapter.GetOleDbDbType!(param).ToString();
			}

			return base.GetProviderTypeName(dataContext, parameter);
		}
	}
}
