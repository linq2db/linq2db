using System;

using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SqlServer
{
	class SqlServer2016SqlBuilder : SqlServer2014SqlBuilder
	{
		public SqlServer2016SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected SqlServer2016SqlBuilder(SqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override SqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2016SqlBuilder(this);
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			BuildDropTableStatementIfExists(dropTable);
		}

		public override string Name => ProviderName.SqlServer2016;

		internal bool ConvertDateTimeAsLiteral;

		protected override bool TryConvertParameterToSql(SqlParameterValue paramValue)
		{
			// SQL Server FOR SYSTEM_TIME clause does not support expressions. Parameters or literals only.
			//
			if (ConvertDateTimeAsLiteral && paramValue.ProviderValue is DateTime)
				return base.TryConvertParameterToSql(new (paramValue.ProviderValue, paramValue.ClientValue, new (typeof(DateTime), DataType.Char)));

			return base.TryConvertParameterToSql(paramValue);
		}
	}
}
