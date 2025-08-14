﻿using System;

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
				case QueryType.Insert:
					statement = CorrectUpdateStatement((SqlInsertStatement)statement);
					break;
			}

			return statement;
		}

		private SqlStatement CorrectUpdateStatement(SqlInsertStatement statement)
		{
			if (statement.SelectQuery != null
				&& statement.SelectQuery.Select.Columns.Count == statement.Insert.Items.Count)
			{
				for (var i = 0; i < statement.Insert.Items.Count; i++)
				{
					statement.SelectQuery.Select.Columns[i].Alias = ((SqlField)statement.Insert.Items[i].Column).Name;
				}

				statement.SelectQuery.DoNotSetAliases = true;
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
			if (statement is not SqlStatementWithQueryBase withStatement)
				return false;

			var cteCount = withStatement.With?.Clauses.Count ?? 0;

			if (statement.SelectQuery != null && statement.QueryType != QueryType.Merge)
				statement.SelectQuery = ConvertToCte(statement.SelectQuery, withStatement);

			if (statement is SqlInsertStatement insert)
				insert.Insert = ConvertToCte(insert.Insert, withStatement);

			return withStatement.With?.Clauses.Count > cteCount;

			static T ConvertToCte<T>(T statement, SqlStatementWithQueryBase withStatement)
				where T: class, IQueryElement
			{
				return statement.Convert(withStatement, static (visitor, elem) =>
				{
					if (elem is SelectQuery { Select.Columns: [var column] } subQuery
						&& !QueryHelper.IsDependsOnOuterSources(subQuery))
					{
						if (column.SystemType == null)
							throw new InvalidOperationException();

						if (visitor.Stack?.Count > 1
							// in column or predicate
							&& visitor.Stack[^2] is SqlSelectClause
								or ISqlPredicate
								or SqlExpressionBase
								or SqlSetExpression)
						{
							var cte = new CteClause(subQuery, column.SystemType, false, null);
							(visitor.Context.With ??= new()).Clauses.Add(cte);
							return new SqlCteTable(cte, column.SystemType);
						}
					}

					return elem;
				}, true);
			}
		}
	}
}
