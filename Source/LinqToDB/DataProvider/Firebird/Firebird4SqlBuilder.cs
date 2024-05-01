using System.Text;

namespace LinqToDB.DataProvider.Firebird
{
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	public class Firebird4SqlBuilder : Firebird3SqlBuilder
	{
		public Firebird4SqlBuilder(IDataProvider provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		Firebird4SqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new Firebird4SqlBuilder(this);
		}

		protected override bool BuildJoinType(SqlJoinedTable join, SqlSearchCondition condition)
		{
			switch (join.JoinType)
			{
				case JoinType.CrossApply: StringBuilder.Append("CROSS JOIN LATERAL "); return false;
				case JoinType.OuterApply: StringBuilder.Append("LEFT JOIN LATERAL " ); return true;
			}

			return base.BuildJoinType(join, condition);
		}
	}
}
