using System;
using System.Collections.Generic;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider.SQLite
{
	public class SQLiteSqlBuilder : BasicSqlBuilder
	{
		public SQLiteSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		SQLiteSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SQLiteSqlBuilder(this);
		}

		protected override bool SupportsColumnAliasesInSource => false;

		public override int CommandCount(SqlStatement statement)
		{
			if (statement is SqlTruncateTableStatement trun)
				return trun.ResetIdentity && trun.Table!.IdentityFields.Count > 0 ? 2 : 1;
			return statement.NeedsIdentity() ? 2 : 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			if (statement is SqlTruncateTableStatement trun)
			{
				StringBuilder.Append("UPDATE SQLITE_SEQUENCE SET SEQ=0 WHERE NAME=");
				MappingSchema.ConvertToSqlValue(StringBuilder, null, DataOptions, trun.Table!.TableName.Name);
			}
			else
			{
				StringBuilder.AppendLine("SELECT last_insert_rowid()");
			}
		}

		protected override string LimitFormat(SelectQuery selectQuery)
		{
			return "LIMIT {0}";
		}

		protected override string OffsetFormat(SelectQuery selectQuery)
		{
			return "OFFSET {0}";
		}

		public override bool IsNestedJoinParenthesisRequired => true;

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return sb.Append('@').Append(value);

				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
					if (value.Length > 0 && value[0] == '[')
						return sb.Append(value);

					return sb.Append('[').Append(value).Append(']');

				case ConvertType.NameToDatabase  :
				case ConvertType.NameToSchema    :
				case ConvertType.NameToQueryTable:
				case ConvertType.NameToProcedure :
					if (value.Length > 0 && value[0] == '[')
						return sb.Append(value);

					if (value.IndexOf('.') > 0)
						value = string.Join("].[", value.Split('.'));

					return sb.Append('[').Append(value).Append(']');

				case ConvertType.SprocParameterToName:
					return value.Length > 0 && value[0] == '@'
						? sb.Append(value.Substring(1))
						: sb.Append(value);
			}

			return sb.Append(value);
		}

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.DataType)
			{
				case DataType.Int32 : StringBuilder.Append("INTEGER");                                 break;
				default             : base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull); break;
			}
		}

		protected override void BuildCreateTableIdentityAttribute2(SqlField field)
		{
			StringBuilder.Append("PRIMARY KEY AUTOINCREMENT");
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			if (createTable.Table!.IdentityFields.Count > 0)
			{
				while (StringBuilder[StringBuilder.Length - 1] != ',')
					StringBuilder.Length--;
				StringBuilder.Length--;
			}
			else
			{
				AppendIndent()
					.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY (")
					.Append(string.Join(InlineComma, fieldNames))
					.Append(')');
			}
		}

		public override StringBuilder BuildObjectName(StringBuilder sb, SqlObjectName name, ConvertType objectType, bool escape, TableOptions tableOptions, bool withoutSuffix)
		{
			// either "temp", "main" or attached db name supported
			if (tableOptions.IsTemporaryOptionSet())
				sb.Append("temp.");
			else  if (name.Database != null)
			{
				(escape ? Convert(sb, name.Database, ConvertType.NameToDatabase) : sb.Append(name.Database))
					.Append('.');
			}

			return escape ? Convert(sb, name.Name, objectType) : sb.Append(name.Name);
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			BuildDropTableStatementIfExists(dropTable);
		}

		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			throw new LinqToDBException($"{Name} provider doesn't support SQL MERGE statement");
		}

		protected override void BuildCreateTableCommand(SqlTable table)
		{
			string command;

			if (table.TableOptions.IsTemporaryOptionSet())
			{
				switch (table.TableOptions & TableOptions.IsTemporaryOptionSet)
				{
					case TableOptions.IsTemporary                                                                              :
					case TableOptions.IsTemporary |                                          TableOptions.IsLocalTemporaryData :
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure                                     :
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData :
					case                                                                     TableOptions.IsLocalTemporaryData :
					case                            TableOptions.IsLocalTemporaryStructure                                     :
					case                            TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData :
						command = "CREATE TEMPORARY TABLE ";
						break;
					case var value :
						throw new InvalidOperationException($"Incompatible table options '{value}'");
				}
			}
			else
			{
				command = "CREATE TABLE ";
			}

			StringBuilder.Append(command);

			if (table.TableOptions.HasCreateIfNotExists())
				StringBuilder.Append("IF NOT EXISTS ");
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsOnConflictUpdateOrNothing(insertOrUpdate);
		}

		// 3.39.0 adds standard DISTINCT FROM, but let's keep older implementation for now
		protected override void BuildIsDistinctPredicate(SqlPredicate.IsDistinct expr)
		{
			BuildExpression(GetPrecedence(expr), expr.Expr1);
			StringBuilder.Append(expr.IsNot ? " IS " : " IS NOT ");
			BuildExpression(GetPrecedence(expr), expr.Expr2);
		}

		protected override void BuildSqlValuesTable(SqlValuesTable valuesTable, string alias, out bool aliasBuilt)
		{
			valuesTable = ConvertElement(valuesTable);
			var rows = valuesTable.BuildRows(OptimizationContext.EvaluationContext);

			if (rows.Count == 0)
			{
				StringBuilder.Append(OpenParens);
				BuildEmptyValues(valuesTable);
				StringBuilder.Append(')');
			}
			else
			{
				StringBuilder.Append(OpenParens);

				++Indent;

				StringBuilder.AppendLine();
				AppendIndent();
				BuildEmptyValues(valuesTable);
				StringBuilder.AppendLine();

				AppendIndent();

				if (rows.Count > 0)
				{
					StringBuilder.AppendLine("UNION ALL");
					AppendIndent();

					BuildValues(valuesTable, rows);
				}

				StringBuilder.Append(')');

				--Indent;
			}

			aliasBuilt = false;
		}

		protected override void BuildTableExtensions(SqlTable table, string alias)
		{
			if (table.SqlQueryExtensions is not null)
				BuildTableExtensions(StringBuilder, table, alias, " ", " ", null);
		}

		protected override void BuildUpdateTableName(SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			base.BuildUpdateTableName(selectQuery, updateClause);

			if (updateClause.Table != null)
				BuildTableExtensions(updateClause.Table, "");
		}

		protected override void BuildUpdateQuery(SqlStatement statement, SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			BuildStep = Step.Tag;             BuildTag(statement);
			BuildStep = Step.WithClause;      BuildWithClause(statement.GetWithClause());
			BuildStep = Step.UpdateClause;    BuildUpdateClause(Statement, selectQuery, updateClause);
			BuildStep = Step.FromClause;      BuildFromClause(Statement, selectQuery);
			BuildStep = Step.WhereClause;     BuildUpdateWhereClause(selectQuery);
			BuildStep = Step.GroupByClause;   BuildGroupByClause(selectQuery);
			BuildStep = Step.HavingClause;    BuildHavingClause(selectQuery);
			BuildStep = Step.Output;          BuildOutputSubclause(statement.GetOutputClause());
			BuildStep = Step.OrderByClause;   BuildOrderByClause(selectQuery);
			BuildStep = Step.OffsetLimit;     BuildOffsetLimit(selectQuery);
			BuildStep = Step.QueryExtensions; BuildSubQueryExtensions(statement);
		}

	}
}
