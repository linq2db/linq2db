using System.Data.Common;

using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.Access
{
	sealed class AccessOleDbSqlBuilder : AccessSqlBuilderBase
	{
		public AccessOleDbSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		AccessOleDbSqlBuilder(SqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override SqlBuilder CreateSqlBuilder()
		{
			return new AccessOleDbSqlBuilder(this);
		}

		protected override string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (DataProvider is AccessDataProvider provider && provider.Provider == AccessProvider.OleDb)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
					return provider.Adapter.GetOleDbDbType!(param).ToString();
			}

			return base.GetProviderTypeName(dataContext, parameter);
		}
	}
}
