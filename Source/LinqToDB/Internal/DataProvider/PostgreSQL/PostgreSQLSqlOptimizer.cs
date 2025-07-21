using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.PostgreSQL
{
	sealed class PostgreSQLSqlOptimizer : BasicSqlOptimizer
	{
		public PostgreSQLSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new PostgreSQLSqlExpressionConvertVisitor(allowModify);
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);

			switch (statement.QueryType)
			{
				case QueryType.Delete:
				{
					statement = CorrectPostgreSqlDelete((SqlDeleteStatement)statement);
					break;
				}
				case QueryType.Update:
				{
					statement = GetAlternativeUpdatePostgreSqlite((SqlUpdateStatement)statement, dataOptions, mappingSchema);
					break;
				}
			}

			statement = CorrectPostgreSqlOutput(statement);

			return statement;
		}

		SqlStatement CorrectPostgreSqlDelete(SqlDeleteStatement statement)
		{
			statement = GetAlternativeDelete(statement);

			return statement;
		}

		SqlStatement CorrectPostgreSqlOutput(SqlStatement statement)
		{
			if (statement.QueryType is QueryType.Update or QueryType.Merge)
			{
				statement.VisitAll(static qe =>
				{
					if (qe is not SqlAnchor { AnchorKind: SqlAnchor.AnchorKindEnum.Inserted or SqlAnchor.AnchorKindEnum.Deleted } anchor)
						return;

					var field = QueryHelper.ExtractField(anchor.SqlExpression);
					if (field is null)
						throw new LinqToDBException($"PostgreSQL does not support output columns which are not field.");

					anchor.Modify(field);
				});
			}

			return statement;
		}

	}
}
