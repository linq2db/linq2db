using System;

namespace LinqToDB.DataProvider.Oracle
{
	using SqlProvider;
	using SqlQuery;

	public class Oracle12SqlOptimizer : Oracle11SqlOptimizer
	{
		public Oracle12SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement, LinqOptions linqOptions)
		{
			if (statement.IsUpdate() || statement.IsInsert() || statement.IsDelete())
				statement = ReplaceTakeSkipWithRowNum(statement, false);

			switch (statement.QueryType)
			{
				case QueryType.Delete : statement = GetAlternativeDelete((SqlDeleteStatement) statement, linqOptions); break;
				case QueryType.Update : statement = GetAlternativeUpdate((SqlUpdateStatement) statement, linqOptions); break;
			}

			return statement;
		}
	}
}
