using System;
using System.Data.Common;
using System.Text;

using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Access;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.Access
{
	public class AccessODBCSqlBuilder : AccessSqlBuilderBase
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
			return convertType switch
			{
				ConvertType.NameToQueryParameter or ConvertType.NameToCommandParameter or ConvertType.NameToSprocParameter =>
					sb.Append('?'),

				_ => base.Convert(sb, value, convertType),
			};
		}

		/// <summary>
		/// The ODBC driver rewrites a double quote inside a bracketed name before parsing it
		/// (<c>[Say "Hi"]</c> reaches the engine as <c>[Say [Hi]]</c>), so such a name is refused here.
		/// </summary>
		protected override StringBuilder DelimitIdentifier(StringBuilder sb, string value)
		{
			if (value.Contains('"', StringComparison.Ordinal))
				throw new LinqToDBException($"Access cannot represent the identifier '{value}' over ODBC: the driver cannot read a double quote inside a bracketed identifier.");

			return base.DelimitIdentifier(sb, value);
		}

		protected override string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (DataProvider is AccessDataProvider { Provider: AccessProvider.ODBC } provider)
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
			return paramValue.ProviderValue switch
			{
				Guid g => false,
				_ => base.TryConvertParameterToSql(paramValue),
			};
		}

		protected override void BuildValue(DbDataType? dataType, object? value)
		{
			// We force GUID literal to parameter as it's use of {} brackets conflicts with ODBC runtime
			// problem with this fix is that sometimes string-based syntax '{xxx}' works and sometimes - doesn't
			// and it isn't clear why
			// (see Typestests.Guid1/2 and DataTypesTests.TestGuid)
			// Native literal works always, but cannot be used with ODBC
			// As "works everywhere" solution we decided to use parameter here
			if (value is Guid g)
			{
				BuildParameter(new SqlParameter(dataType ?? MappingSchema.GetDbDataType(typeof(Guid)), "value", value));
				return;
			}

			base.BuildValue(dataType, value);
		}
	}
}
