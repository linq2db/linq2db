using System;

namespace LinqToDB.DataProvider.SQLite
{
	using SqlProvider;
	using SqlQuery;

	sealed class SQLiteSqlOptimizer : BasicSqlOptimizer
	{
		public SQLiteSqlOptimizer(SqlProviderFlags sqlProviderFlags)
			: base(sqlProviderFlags)
		{

		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new SQLiteSqlExpressionConvertVisitor(allowModify);
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions)
		{
			switch (statement.QueryType)
			{
				case QueryType.Delete :
				{
					statement = GetAlternativeDelete((SqlDeleteStatement)statement, dataOptions);
					statement.SelectQuery!.From.Tables[0].Alias = "$";
					break;
				}

				case QueryType.Update :
				{
					if (SqlProviderFlags.IsUpdateFromSupported)
					{
						statement = GetAlternativeUpdatePostgreSqlite((SqlUpdateStatement)statement, dataOptions);
					}
					else
					{
						statement = GetAlternativeUpdate((SqlUpdateStatement)statement, dataOptions);
					}

					break;
				}
			}

			return statement;
		}

	}
}
