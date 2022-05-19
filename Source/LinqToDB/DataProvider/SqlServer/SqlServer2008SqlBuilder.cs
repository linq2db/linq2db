using System;

namespace LinqToDB.DataProvider.SqlServer;

using SqlQuery;
using SqlProvider;
using Mapping;

partial class SqlServer2008SqlBuilder : SqlServerSqlBuilder
{
	public SqlServer2008SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
		: base(provider, mappingSchema, sqlOptimizer, sqlProviderFlags)
	{
	}

	SqlServer2008SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
	{
	}

	protected override ISqlBuilder CreateSqlBuilder()
	{
		return new SqlServer2008SqlBuilder(this);
	}

	protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
	{
		BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, null);
		StringBuilder.AppendLine(";");
	}

	public override string  Name => ProviderName.SqlServer2008;
}
