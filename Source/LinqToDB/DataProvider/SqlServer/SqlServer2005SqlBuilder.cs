﻿namespace LinqToDB.DataProvider.SqlServer
{
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	sealed class SqlServer2005SqlBuilder : SqlServerSqlBuilder
	{
		public SqlServer2005SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		SqlServer2005SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2005SqlBuilder(this);
		}

		protected override bool IsValuesSyntaxSupported => false;

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.Type.DataType)
			{
				case DataType.DateTimeOffset :
				case DataType.DateTime2      :
				case DataType.Time           :
				case DataType.Date           : StringBuilder.Append("DateTime");                                break;
				default                      : base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull); break;
			}
		}

		public override string  Name => ProviderName.SqlServer2005;
	}
}
