using System;

namespace LinqToDB.DataProvider.Oracle
{
	using SqlProvider;
	using SqlQuery;

	public class OracleSqlOptimizerV12 : OracleSqlOptimizer
	{
		public OracleSqlOptimizerV12(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			switch (statement.QueryType)
			{
				case QueryType.Delete : statement = GetAlternativeDelete((SqlDeleteStatement) statement); break;
				case QueryType.Update : statement = GetAlternativeUpdate((SqlUpdateStatement) statement); break;
			}

			statement = QueryHelper.OptimizeSubqueries(statement);

			return statement;
		}
	}
}
