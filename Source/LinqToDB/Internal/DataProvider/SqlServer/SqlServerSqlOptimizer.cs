using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	public class SqlServerSqlOptimizer : BasicSqlOptimizer
	{
		protected readonly SqlServerVersion SQLVersion;

		protected SqlServerSqlOptimizer(SqlProviderFlags sqlProviderFlags, SqlServerVersion sqlVersion) : base(sqlProviderFlags)
		{
			SQLVersion = sqlVersion;
		}

		public override SqlExpressionConvertVisitor CreateConvertVisitor(bool allowModify)
		{
			return new SqlServerSqlExpressionConvertVisitor(allowModify, SQLVersion);
		}

		protected SqlStatement ReplaceSkipWithRowNumber(SqlStatement statement, MappingSchema mappingSchema)
			=> ReplaceTakeSkipWithRowNumber((object?)null, statement, mappingSchema, static (_, query) => query.Select.SkipValue != null, false);

		protected SqlStatement WrapRootTakeSkipOrderBy(SqlStatement statement)
		{
			var query = statement.SelectQuery;
			if (query == null)
				return statement;

			if (query.Select.SkipValue != null ||
				!query.Select.OrderBy.IsEmpty)
			{
				statement = QueryHelper.WrapQuery(statement, query, true);
			}

			return statement;
		}

		protected override SqlStatement FinalizeUpdate(SqlStatement statement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			var newStatement = base.FinalizeUpdate(statement, dataOptions, mappingSchema);

			if (newStatement is SqlUpdateStatement updateStatement)
			{
				updateStatement = CorrectSqlServerUpdate(updateStatement, dataOptions, mappingSchema);
				newStatement    = updateStatement;
			}

			return newStatement;
		}

		static bool IsUpdateUsingSingeTable(SqlUpdateStatement updateStatement)
		{
			return QueryHelper.IsSingleTableInQuery(updateStatement.SelectQuery, updateStatement.Update.Table!);
		}

		SqlUpdateStatement CorrectSqlServerUpdate(SqlUpdateStatement updateStatement, DataOptions dataOptions, MappingSchema mappingSchema)
		{
			if (updateStatement.Update.Table == null)
				throw new InvalidOperationException();

			var correctionFinished = false;

			SqlTableSource? removedTableSource = null;

			var hasUpdateTableInQuery = QueryHelper.HasTableInQuery(updateStatement.SelectQuery, updateStatement.Update.Table);

			if (hasUpdateTableInQuery)
			{
				// do not remove if there is other tables
				if (QueryHelper.EnumerateAccessibleTables(updateStatement.SelectQuery).Take(2).Count() == 1)
				{
					if (RemoveUpdateTableIfPossible(updateStatement.SelectQuery, updateStatement.Update.Table, out removedTableSource))
					{
						hasUpdateTableInQuery = false;
					}
				}
			}

			if (hasUpdateTableInQuery)
			{
				// handle simple UPDATE TOP n case
				if (updateStatement.SelectQuery.Select.SkipValue == null && updateStatement.SelectQuery.Select.TakeValue != null)
				{
					if (IsUpdateUsingSingeTable(updateStatement))
					{
						updateStatement.SelectQuery.From.Tables.Clear();
						updateStatement.Update.TableSource = null;
						correctionFinished = true;
					}
				}

				if (!correctionFinished)
				{
					var isCompatibleForUpdate = IsCompatibleForUpdate(updateStatement.SelectQuery, updateStatement.Update.Table);
					if (isCompatibleForUpdate)
					{
						// for OUTPUT we have to use datached variant
						if (!IsUpdateUsingSingeTable(updateStatement) && updateStatement.Output != null)
						{
							// check that UpdateTable is visible for SET and OUTPUT
							if (QueryHelper.EnumerateLevelSources(updateStatement.SelectQuery).All(e => e.Source != updateStatement.Update.Table))
							{
								isCompatibleForUpdate = false;
							}
						}
					}

					if (isCompatibleForUpdate)
					{
						var needsWrapping = updateStatement.SelectQuery.Select.SkipValue != null;
						if (needsWrapping)
						{
							updateStatement = QueryHelper.WrapQuery(updateStatement, updateStatement.SelectQuery, true);
						}

						var (ts, path) = FindTableSource(new Stack<IQueryElement>(), updateStatement.SelectQuery,
							updateStatement.Update.Table!);

						updateStatement.Update.TableSource = ts;
					}
					else
					{
						updateStatement = DetachUpdateTableFromUpdateQuery(updateStatement, dataOptions, moveToJoin: false, addNewSource: true, out var sqlTableSource);
						updateStatement.Update.TableSource = sqlTableSource;

						var optimizationContext = this.CreateOptimizationContext(mappingSchema, dataOptions);
						OptimizeQueries(updateStatement, updateStatement, optimizationContext);
					}
				}
			}
			else
			{
				if (updateStatement.Update.TableSource == null)
				{
					var tableName      = updateStatement.Update.Table.TableName;
					var hasComplexName = !string.IsNullOrEmpty(tableName.Server) || !string.IsNullOrEmpty(tableName.Schema) || !string.IsNullOrEmpty(tableName.Database);

					if (updateStatement.SelectQuery.From.Tables.Count > 0 || hasComplexName)
					{
						var suggestedSource = new SqlTableSource(updateStatement.Update.Table!,
							QueryHelper.SuggestTableSourceAlias(updateStatement.SelectQuery, "u"));

						updateStatement.SelectQuery.From.Tables.Insert(0, suggestedSource);

						updateStatement.Update.TableSource = suggestedSource;
					}
				}
			}

			CorrectUpdateSetters(updateStatement);

			if (updateStatement.Update.TableSource != null)
			{
				updateStatement.Update.Table = null;
			}

			return updateStatement;
		}
	}
}
