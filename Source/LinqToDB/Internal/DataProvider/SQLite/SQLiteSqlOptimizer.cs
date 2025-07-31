﻿using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	public class SQLiteSqlOptimizer : BasicSqlOptimizer
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
					statement = GetAlternativeDelete((SqlDeleteStatement)statement);
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
