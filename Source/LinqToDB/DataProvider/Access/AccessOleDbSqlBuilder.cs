﻿namespace LinqToDB.DataProvider.Access
{
	using System.Data.Common;
	using Mapping;
	using SqlProvider;

	class AccessOleDbSqlBuilder : AccessSqlBuilderBase<AccessOleDbDataProvider>
	{
		public AccessOleDbSqlBuilder(AccessOleDbDataProvider provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{ }

		AccessOleDbSqlBuilder(AccessOleDbSqlBuilder parentBuilder) : base(parentBuilder)
		{ }

		protected override BasicSqlBuilder<AccessOleDbDataProvider> CreateSqlBuilder()
			=>  new AccessOleDbSqlBuilder(this);

		protected override string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			var param = DataProvider.TryGetProviderParameter(dataContext, parameter);
			return param != null
				? DataProvider.Adapter.GetDbType(param).ToString()
				: base.GetProviderTypeName(dataContext, parameter);
		}
	}
}
