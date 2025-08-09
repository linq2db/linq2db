using System;

using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	public class YdbSqlOptimizer : BasicSqlOptimizer
	{
		public YdbSqlOptimizer(SqlProviderFlags sqlProviderFlags)
			: base(sqlProviderFlags) { }

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
			=> new YdbSqlExpressionConvertVisitor(allowModify);

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			statement = base.TransformStatement(statement, dataOptions, mappingSchema);

			switch (statement.QueryType)
			{
				case QueryType.Delete:
					// disable table alias
					statement = GetAlternativeDelete((SqlDeleteStatement)statement);
					statement.SelectQuery!.From.Tables[0].Alias = "$";
					break;
				case QueryType.Update:
					// disable table alias
					statement.SelectQuery!.From.Tables[0].Alias = "$";
					break;
			}

			return statement;
		}

		public override SqlStatement Finalize(MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions)
		{
			statement = base.Finalize(mappingSchema, statement, dataOptions);

			if (MoveScalarSubQueriesToCte(statement))
				FinalizeCte(statement);

			return statement;
		}

		private bool MoveScalarSubQueriesToCte(SqlStatement statement)
		{
			if (statement is not SqlStatementWithQueryBase withStatement
				|| statement.SelectQuery == null
				|| statement.QueryType == QueryType.Merge)
				return false;

			var cteCount = withStatement.With?.Clauses.Count ?? 0;
			statement.SelectQuery = statement.SelectQuery.Convert(withStatement, static (visitor, elem) =>
			{
				if (elem != visitor.Context.SelectQuery
					&& elem is SelectQuery { IsParameterDependent: false, Select.Columns: [var column] } subQuery)
				{
					if (column.SystemType == null)
					{
						throw new NotImplementedException("TODO");
					}

					var cte = new CteClause(subQuery, column.SystemType, false, null);
					(visitor.Context.With ??= new()).Clauses.Add(cte);
					return new SqlCteTable(cte, column.SystemType);
				}

				return elem;
			});

			return withStatement.With?.Clauses.Count > cteCount;
		}
	}
}
