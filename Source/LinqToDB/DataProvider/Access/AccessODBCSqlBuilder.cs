using System;
using System.Data.Common;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.Access
{
	sealed class AccessODBCSqlBuilder : AccessSqlBuilderBase
	{
		public AccessODBCSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
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
			if (DataProvider is AccessDataProvider provider && provider.Provider == AccessProvider.ODBC)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
					return provider.Adapter.GetOdbcDbType!(param).ToString();
			}

			return base.GetProviderTypeName(dataContext, parameter);
		}

		protected override bool TryConvertParameterToSql(SqlParameterValue paramValue)
		{
			// see BuildValue notes
			if (paramValue.ProviderValue is Guid g)
			{
				return false;
			}
			
			return base.TryConvertParameterToSql(paramValue);
		}

		protected override void BuildValue(DbDataType? dataType, object? value)
		{
			// We force GUID literal to parameter as it's use of {} brackets conflicts with ODBC runtime
			// problem with this fix is that sometimes string-based syntax '{xxx}' works and sometimes - doesn't
			// and it isn't clear why
			// (see Typestests.Guid1/2 and DataTypesTests.TestGuid)
			// Native literal works always, but cannot be used with ODBC
			// Al "works everywhere" solution we decided to use parameter here
			if (value is Guid g)
			{
				BuildParameter(new SqlParameter(dataType ?? MappingSchema.GetDbDataType(typeof(Guid)), "value", value));
				return;
			}

			base.BuildValue(dataType, value);
		}
	}
}
