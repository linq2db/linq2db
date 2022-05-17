﻿using System;
﻿
namespace LinqToDB.DataProvider.Access
{
	using Infrastructure;
	using System.Data.Common;
	using Mapping;
	using SqlProvider;

	class AccessOleDbSqlBuilder : AccessSqlBuilderBase
	{
		public AccessOleDbSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, LinqOptionSet linqOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, linqOptions, sqlOptimizer, sqlProviderFlags)
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
			if (DataProvider is AccessOleDbDataProvider provider)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
					return provider.Adapter.GetDbType(param).ToString();
			}

			return base.GetProviderTypeName(dataContext, parameter);
		}
	}
}
