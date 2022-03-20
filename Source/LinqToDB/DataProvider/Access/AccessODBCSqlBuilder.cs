namespace LinqToDB.DataProvider.Access
{
	using System.Data;
	using System.Text;
	using LinqToDB.SqlQuery;
	using Mapping;
	using SqlProvider;

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

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return sb.Append('?');
			}

			return base.Convert(sb, value, convertType);
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

		protected override void BuildColumnExpression(SelectQuery? selectQuery, ISqlExpression expr, string? alias, ref bool addAlias)
		{
			// ODBC provider doesn't support NULL parameter as top-level select column value
			if (expr is SqlParameter p && p.IsQueryParameter && p.Value == null)
				expr = new SqlValue(p.Type, null);

			base.BuildColumnExpression(selectQuery, expr, alias, ref addAlias);
		}
	}
}
