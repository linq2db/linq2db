using System;
using System.Data.Common;
using System.Text;

namespace LinqToDB.DataProvider.Access
{
	using SqlQuery;
	using Mapping;
	using SqlProvider;

	sealed class AccessODBCSqlBuilder : AccessSqlBuilderBase
	{
		public AccessODBCSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		AccessODBCSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new AccessODBCSqlBuilder(this);
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

		protected override string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (DataProvider is AccessODBCDataProvider provider)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
					return provider.Adapter.GetDbType(param).ToString();
			}

			return base.GetProviderTypeName(dataContext, parameter);
		}

		protected override void BuildColumnExpression(NullabilityContext nullability, SelectQuery? selectQuery, ISqlExpression expr, string? alias, ref bool addAlias)
		{
			// ODBC provider doesn't support NULL parameter as top-level select column value
			if (expr is SqlParameter p
				&& p.IsQueryParameter
				&& selectQuery != null
				&& Statement.QueryType == QueryType.Select
				&& Statement.SelectQuery == selectQuery
				&& p.GetParameterValue(OptimizationContext.Context.ParameterValues).ProviderValue == null)
			{
				expr = new SqlValue(p.Type, null);
			}

			base.BuildColumnExpression(nullability, selectQuery, expr, alias, ref addAlias);
		}
	}
}
