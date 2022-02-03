using System;
using System.Data.Common;
using System.Text;

namespace LinqToDB.DataProvider.Access
{
	using Mapping;
	using SqlProvider;

	class AccessODBCSqlBuilder : AccessSqlBuilderBase
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

		protected override string? GetProviderTypeName(DbParameter parameter)
		{
			if (DataProvider is AccessODBCDataProvider provider)
			{
				var param = provider.TryGetProviderParameter(parameter, MappingSchema);
				if (param != null)
					return provider.Adapter.GetDbType(param).ToString();
			}

			return base.GetProviderTypeName(parameter);
		}
	}
}
