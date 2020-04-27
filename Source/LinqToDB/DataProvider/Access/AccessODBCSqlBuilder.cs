namespace LinqToDB.DataProvider.Access
{
	using System.Data;
	using LinqToDB.Mapping;
	using LinqToDB.SqlProvider;

	class AccessODBCSqlBuilder : AccessSqlBuilderBase
	{
		private readonly AccessODBCDataProvider? _provider;

		public AccessODBCSqlBuilder(
			AccessODBCDataProvider? provider,
			MappingSchema           mappingSchema,
			ISqlOptimizer           sqlOptimizer,
			SqlProviderFlags        sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
			_provider = provider;
		}

			// remote context
		public AccessODBCSqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		public override string Convert(string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return "?";
			}

			return base.Convert(value, convertType);
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new AccessODBCSqlBuilder(_provider, MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override string? GetProviderTypeName(IDbDataParameter parameter)
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
