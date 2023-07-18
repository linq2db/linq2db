using System;

namespace LinqToDB.DataProvider.DB2
{
	using SqlProvider;
	using SqlQuery;

	sealed class DB2SqlOptimizer : BasicSqlOptimizer
	{
		public DB2SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions)
		{
			// DB2 LUW 9/10 supports only FETCH, v11 adds OFFSET, but for that we need to introduce versions into DB2 provider first
			statement = SeparateDistinctFromPagination(statement, q => q.Select.SkipValue != null);
			statement = ReplaceDistinctOrderByWithRowNumber(statement, q => q.Select.SkipValue != null);
			statement = ReplaceTakeSkipWithRowNumber(SqlProviderFlags, statement, static (SqlProviderFlags, query) => query.Select.SkipValue != null && SqlProviderFlags.GetIsSkipSupportedFlag(query.Select.TakeValue, query.Select.SkipValue), true);

			// This is mutable part
			return statement.QueryType switch
			{
				QueryType.Delete => GetAlternativeDelete((SqlDeleteStatement)statement, dataOptions),
				QueryType.Update => GetAlternativeUpdate((SqlUpdateStatement)statement, dataOptions),
				_                => statement,
			};
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new DB2SqlExpressionConvertVisitor(allowModify);
		}
	}
}
