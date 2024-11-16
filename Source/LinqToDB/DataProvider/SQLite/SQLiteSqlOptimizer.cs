namespace LinqToDB.DataProvider.SQLite
{
	using Mapping;
	using SqlProvider;
	using SqlQuery;

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

					if (statement is SqlUpdateStatement { Output.HasOutput: true } updateStatement)
					{
						updateStatement.Output = updateStatement.Output.Convert(1, (_, e) =>
						{
							if (e is SqlAnchor { AnchorKind: SqlAnchor.AnchorKindEnum.Inserted } anchor)
							{
								return anchor.SqlExpression;
							}

							return e;
						});
					}

					break;
				}
			}

			return statement;
		}

	}
}
