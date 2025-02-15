using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SQLite
{
	sealed class SQLiteSqlOptimizer : BasicSqlOptimizer
	{
		public SQLiteSqlOptimizer(SqlProviderFlags sqlProviderFlags)
			: base(sqlProviderFlags)
		{

		}

		public override bool RequiresCastingParametersForSetOperations => false;

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new SQLiteSqlExpressionConvertVisitor(allowModify);
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);

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
						statement = GetAlternativeUpdatePostgreSqlite((SqlUpdateStatement)statement, dataOptions, mappingSchema);
					}
					else
					{
						statement = GetAlternativeUpdate((SqlUpdateStatement)statement, dataOptions, mappingSchema);
					}

					break;
				}
			}

			return statement;
		}

	}
}
