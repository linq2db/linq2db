using System;
using System.Data.Common;

namespace LinqToDB.DataProvider.Access
{
	using Mapping;
	using SqlProvider;

	sealed class AccessOleDbSqlBuilder : AccessSqlBuilderBase
	{
		public AccessOleDbSqlBuilder(AccessDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
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
