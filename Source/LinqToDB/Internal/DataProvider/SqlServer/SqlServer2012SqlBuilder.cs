using LinqToDB.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	public partial class SqlServer2012SqlBuilder : SqlServerSqlBuilder
	{
		public SqlServer2012SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected SqlServer2012SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2012SqlBuilder(this);
		}

		protected override string? LimitFormat(SelectQuery selectQuery)
		{
			return selectQuery.Select.SkipValue != null ? "FETCH NEXT {0} ROWS ONLY" : null;
		}

		protected override string OffsetFormat(SelectQuery selectQuery)
		{
			return "OFFSET {0} ROWS";
		}

		protected override bool OffsetFirst => true;

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, null);
			StringBuilder.AppendLine(";");
		}

		protected override void BuildSqlConditionExpression(SqlConditionExpression conditionExpression)
		{
			BuildSqlConditionExpressionAsFunction("IIF", conditionExpression);
		}

		public override string  Name => ProviderName.SqlServer2012;

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
