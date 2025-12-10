using LinqToDB.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	public partial class SqlServer2008SqlBuilder : SqlServerSqlBuilder
	{
		public SqlServer2008SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
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

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			if (type.DataType == DataType.Json)
			{
				StringBuilder.Append("NVARCHAR(MAX)");
				return;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
		}
	}
}
