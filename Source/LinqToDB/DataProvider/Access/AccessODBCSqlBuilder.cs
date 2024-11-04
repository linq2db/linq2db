using System;
using System.Data.Common;
using System.Text;

namespace LinqToDB.DataProvider.Access
{
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	sealed class AccessODBCSqlBuilder : AccessSqlBuilderBase
	{
		public AccessODBCSqlBuilder(AccessDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
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
			if (DataProvider is AccessDataProvider provider && provider.Provider == AccessProvider.OleDb)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
					return provider.Adapter.GetOdbcDbType!(param).ToString();
			}

			return base.GetProviderTypeName(dataContext, parameter);
		}
	}
}
