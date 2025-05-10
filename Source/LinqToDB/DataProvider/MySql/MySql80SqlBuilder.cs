using System.Text;

using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.MySql
{
	sealed class MySql80SqlBuilder : MySqlSqlBuilder
	{
		public MySql80SqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		MySql80SqlBuilder(SqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override SqlBuilder CreateSqlBuilder()
		{
			return new MySql80SqlBuilder(this) { HintBuilder = HintBuilder };
		}

		protected override bool BuildJoinType(SqlJoinedTable join, SqlSearchCondition condition)
		{
			switch (join.JoinType)
			{
				case JoinType.CrossApply:
					// join with function implies lateral keyword
					if (join.Table.SqlTableType == SqlTableType.Function)
						StringBuilder.Append("INNER JOIN ");
					else
						StringBuilder.Append("INNER JOIN LATERAL ");
					return true;
				case JoinType.OuterApply:
					// join with function implies lateral keyword
					if (join.Table.SqlTableType == SqlTableType.Function)
						StringBuilder.Append("LEFT JOIN ");
					else
						StringBuilder.Append("LEFT JOIN LATERAL ");
					return true;
			}

			return base.BuildJoinType(join, condition);
		}
	}
}
