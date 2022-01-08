﻿using System;
﻿using System.Data;

namespace LinqToDB.DataProvider.Access
{
	using Mapping;
	using SqlProvider;

	class AccessOleDbSqlBuilder : AccessSqlBuilderBase
	{
		public AccessOleDbSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		AccessOleDbSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new AccessOleDbSqlBuilder(this);
		}

		protected override string? GetProviderTypeName(IDbDataParameter parameter)
		{
			if (DataProvider is AccessOleDbDataProvider provider)
			{
				var param = provider.TryGetProviderParameter(parameter, MappingSchema);
				if (param != null)
					return provider.Adapter.GetDbType(param).ToString();
			}

			return base.GetProviderTypeName(parameter);
		}
	}
}
