﻿using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	class SqlServer2016SqlBuilder : SqlServer2014SqlBuilder
	{
		public SqlServer2016SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected SqlServer2016SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
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
				return base.TryConvertParameterToSql(new (paramValue.ProviderValue, new (typeof(DateTime), DataType.Char)));

			return base.TryConvertParameterToSql(paramValue);
		}
	}
}
