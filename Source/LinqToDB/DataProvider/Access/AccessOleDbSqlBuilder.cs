﻿using System.Data;

namespace LinqToDB.DataProvider.Access
{
	using System.Data.Common;
	using Mapping;
	using SqlProvider;

	class AccessOleDbSqlBuilder : AccessSqlBuilderBase
	{
		private readonly AccessOleDbDataProvider? _provider;

		public AccessOleDbSqlBuilder(
			AccessOleDbDataProvider? provider,
			MappingSchema            mappingSchema,
			ISqlOptimizer            sqlOptimizer,
			SqlProviderFlags         sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
			_provider = provider;
		}

		// remote context
		public AccessOleDbSqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new AccessOleDbSqlBuilder(_provider, MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override string? GetProviderTypeName(DbParameter parameter)
		{
			if (_provider != null)
			{
				var param = _provider.TryGetProviderParameter(parameter, MappingSchema);
				if (param != null)
					return _provider.Adapter.GetDbType(param).ToString();
			}

			return base.GetProviderTypeName(parameter);
		}
	}
}
