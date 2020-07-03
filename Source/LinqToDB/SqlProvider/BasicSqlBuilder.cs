﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LinqToDB.SqlProvider
{
	using Common;
	using Mapping;
	using SqlQuery;

	public abstract partial class BasicSqlBuilder : ISqlBuilder
	{
		#region Init

		protected BasicSqlBuilder(MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
		{
			MappingSchema       = mappingSchema;
			SqlOptimizer        = sqlOptimizer;
			SqlProviderFlags    = sqlProviderFlags;
		}

		protected SqlStatement           Statement = null!;
		protected readonly MappingSchema MappingSchema;
		protected int                    Indent;
		protected Step                   BuildStep;
		protected ISqlOptimizer          SqlOptimizer;
		protected SqlProviderFlags       SqlProviderFlags;
		protected ValueToSqlConverter    ValueToSqlConverter => MappingSchema.ValueToSqlConverter;
		protected StringBuilder          StringBuilder = null!;
		protected bool                   SkipAlias;

		#endregion

		#region Parameters Support

		public    List<SqlParameter>     ActualParameters { get; } = new List<SqlParameter>();

		protected void AddParameters(List<SqlParameter> parameters)
		{
			foreach (var parameter in parameters)
			{
				if (!ActualParameters.Contains(parameter))
					ActualParameters.Add(parameter);
			}
		}

		protected void AddParameter(SqlParameter parameter)
		{
			if (!ActualParameters.Contains(parameter))
				ActualParameters.Add(parameter);
		}

		#endregion

		#region Support Flags

		public virtual bool IsNestedJoinSupported           => true;
		public virtual bool IsNestedJoinParenthesisRequired => false;

		/// <summary>
		/// True if it is needed to wrap join condition with ()
		/// </summary>
		/// <example>
		/// <code>
		/// INNER JOIN Table2 t2 ON (t1.Value = t2.Value)
		/// </code>
		/// </example>
		public virtual bool WrapJoinCondition => false;

		protected virtual bool CanSkipRootAliases(SqlStatement statement) => true;

		#endregion

		#region CommandCount

		public virtual int CommandCount(SqlStatement statement)
		{
			return 1;
		}

		#endregion

		#region BuildSql

		public void BuildSql(int commandNumber, SqlStatement statement, StringBuilder sb, int startIndent = 0)
		{
			BuildSql(commandNumber, statement, sb, startIndent, !Configuration.Sql.GenerateFinalAliases && CanSkipRootAliases(statement));
		}

		protected virtual void BuildSetOperation(SetOperation operation, StringBuilder sb)
		{
			switch (operation)
			{
				case SetOperation.Union:
					sb.Append("UNION");
					break;
				case SetOperation.UnionAll:
					sb.Append("UNION ALL");
					break;
				case SetOperation.Except:
					sb.Append("EXCEPT");
					break;
				case SetOperation.ExceptAll:
					sb.Append("EXCEPT ALL");
					break;
				case SetOperation.Intersect:
					sb.Append("INTERSECT");
					break;
				case SetOperation.IntersectAll:
					sb.Append("INTERSECT ALL");
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
			}
		}

		protected virtual void BuildSql(int commandNumber, SqlStatement statement, StringBuilder sb, int indent, bool skipAlias)
		{
			Statement     = statement;
			StringBuilder = sb;
			Indent        = indent;
			SkipAlias     = skipAlias;

			if (commandNumber == 0)
			{
				BuildSql();

				if (Statement.SelectQuery != null && Statement.SelectQuery.HasSetOperators)
				{
					foreach (var union in Statement.SelectQuery.SetOperators)
					{
						AppendIndent();
						BuildSetOperation(union.Operation, sb);
						sb.AppendLine();

						var sqlBuilder = ((BasicSqlBuilder)CreateSqlBuilder());
						sqlBuilder.BuildSql(commandNumber,
							new SqlSelectStatement(union.SelectQuery) { ParentStatement = statement }, sb, indent,
							skipAlias);
						AddParameters(sqlBuilder.ActualParameters);
					}
				}
			}
			else
			{
				BuildCommand(statement, commandNumber);
			}
		}

		protected virtual void BuildCommand(SqlStatement statement, int commandNumber)
		{
		}

		#endregion

		#region Overrides

		protected virtual void BuildSqlBuilder(SelectQuery selectQuery, int indent, bool skipAlias)
		{
			if (!SqlProviderFlags.GetIsSkipSupportedFlag(selectQuery)
				&& selectQuery.Select.SkipValue != null)
				throw new SqlException("Skip for subqueries is not supported by the '{0}' provider.", Name);

			if (!SqlProviderFlags.IsTakeSupported && selectQuery.Select.TakeValue != null)
				throw new SqlException("Take for subqueries is not supported by the '{0}' provider.", Name);

			var sqlBuilder = (BasicSqlBuilder)CreateSqlBuilder();
			sqlBuilder.BuildSql(0,
				new SqlSelectStatement(selectQuery) { ParentStatement = Statement }, StringBuilder, indent, skipAlias);
			AddParameters(sqlBuilder.ActualParameters);
		}

		protected abstract ISqlBuilder CreateSqlBuilder();

		protected T WithStringBuilder<T>(StringBuilder sb, Func<T> func)
		{
			var current = StringBuilder;

			StringBuilder = sb;

			var ret = func();

			StringBuilder = current;

			return ret;
		}

		void WithStringBuilder(StringBuilder sb, Action func)
		{
			var current = StringBuilder;

			StringBuilder = sb;

			func();

			StringBuilder = current;
		}

		protected virtual bool ParenthesizeJoin(List<SqlJoinedTable> joins)
		{
			return false;
		}

		protected virtual void BuildSql()
		{
			switch (Statement.QueryType)
			{
				case QueryType.Select        : BuildSelectQuery           ((SqlSelectStatement)Statement);                                            break;
				case QueryType.Delete        : BuildDeleteQuery           ((SqlDeleteStatement)Statement);                                            break;
				case QueryType.Update        : BuildUpdateQuery           (Statement, Statement.SelectQuery!, ((SqlUpdateStatement)Statement).Update); break;
				case QueryType.Insert        : BuildInsertQuery           (Statement, ((SqlInsertStatement)Statement).Insert, false);                 break;
				case QueryType.InsertOrUpdate: BuildInsertOrUpdateQuery   ((SqlInsertOrUpdateStatement)Statement);                                    break;
				case QueryType.CreateTable   : BuildCreateTableStatement  ((SqlCreateTableStatement)Statement);                                       break;
				case QueryType.DropTable     : BuildDropTableStatement    ((SqlDropTableStatement)Statement);                                         break;
				case QueryType.TruncateTable : BuildTruncateTableStatement((SqlTruncateTableStatement)Statement);                                     break;
				case QueryType.Merge         : BuildMergeStatement        ((SqlMergeStatement)Statement);                                             break;
				default                      : BuildUnknownQuery();                                                                                   break;
			}
		}

		protected virtual void BuildDeleteQuery(SqlDeleteStatement deleteStatement)
		{
			BuildStep = Step.WithClause;    BuildWithClause(deleteStatement.With);
			BuildStep = Step.DeleteClause;  BuildDeleteClause(deleteStatement);
			BuildStep = Step.FromClause;    BuildFromClause(Statement, deleteStatement.SelectQuery);
			BuildStep = Step.WhereClause;   BuildWhereClause(deleteStatement.SelectQuery);
			BuildStep = Step.GroupByClause; BuildGroupByClause(deleteStatement.SelectQuery);
			BuildStep = Step.HavingClause;  BuildHavingClause(deleteStatement.SelectQuery);
			BuildStep = Step.OrderByClause; BuildOrderByClause(deleteStatement.SelectQuery);
			BuildStep = Step.OffsetLimit;   BuildOffsetLimit(deleteStatement.SelectQuery);
		}

		protected void BuildDeleteQuery2(SqlDeleteStatement deleteStatement)
		{
			BuildStep = Step.DeleteClause; BuildDeleteClause(deleteStatement);

			while (StringBuilder[StringBuilder.Length - 1] == ' ')
				StringBuilder.Length--;

			StringBuilder.AppendLine();
			AppendIndent().AppendLine("(");

			++Indent;

			var selectStatement = new SqlSelectStatement(deleteStatement.SelectQuery)
				{ ParentStatement = deleteStatement, With = deleteStatement.GetWithClause() };

			var sqlBuilder = ((BasicSqlBuilder)CreateSqlBuilder());
			sqlBuilder.BuildSql(0, selectStatement, StringBuilder, Indent);
			AddParameters(sqlBuilder.ActualParameters);

			--Indent;

			AppendIndent().AppendLine(")");

		}

		protected virtual void BuildUpdateQuery(SqlStatement statement, SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			BuildStep = Step.WithClause;    BuildWithClause(statement.GetWithClause());
			BuildStep = Step.UpdateClause;  BuildUpdateClause(Statement, selectQuery, updateClause);

			if (SqlProviderFlags.IsUpdateFromSupported)
				{BuildStep = Step.FromClause;    BuildFromClause(Statement, selectQuery);}

			BuildStep = Step.WhereClause;   BuildWhereClause(selectQuery);
			BuildStep = Step.GroupByClause; BuildGroupByClause(selectQuery);
			BuildStep = Step.HavingClause;  BuildHavingClause(selectQuery);
			BuildStep = Step.OrderByClause; BuildOrderByClause(selectQuery);
			BuildStep = Step.OffsetLimit;   BuildOffsetLimit(selectQuery);
		}

		protected virtual void BuildSelectQuery(SqlSelectStatement selectStatement)
		{
			BuildStep = Step.WithClause;    BuildWithClause(selectStatement.With);
			BuildStep = Step.SelectClause;  BuildSelectClause(selectStatement.SelectQuery);
			BuildStep = Step.FromClause;    BuildFromClause(selectStatement, selectStatement.SelectQuery);
			BuildStep = Step.WhereClause;   BuildWhereClause(selectStatement.SelectQuery);
			BuildStep = Step.GroupByClause; BuildGroupByClause(selectStatement.SelectQuery);
			BuildStep = Step.HavingClause;  BuildHavingClause(selectStatement.SelectQuery);
			BuildStep = Step.OrderByClause; BuildOrderByClause(selectStatement.SelectQuery);
			BuildStep = Step.OffsetLimit;   BuildOffsetLimit(selectStatement.SelectQuery);
		}

		protected virtual void BuildCteBody(SelectQuery selectQuery)
		{
			var sqlBuilder = (BasicSqlBuilder)CreateSqlBuilder();
			sqlBuilder.BuildSql(0, new SqlSelectStatement(selectQuery), StringBuilder, Indent, SkipAlias);
			AddParameters(sqlBuilder.ActualParameters);
		}

		protected virtual void BuildInsertQuery(SqlStatement statement, SqlInsertClause insertClause, bool addAlias)
		{
			BuildStep = Step.WithClause;   BuildWithClause(statement.GetWithClause());
			BuildStep = Step.InsertClause; BuildInsertClause(statement, insertClause, addAlias);

			if (statement.QueryType == QueryType.Insert && statement.SelectQuery!.From.Tables.Count != 0)
			{
				BuildStep = Step.SelectClause;  BuildSelectClause(statement.SelectQuery);
				BuildStep = Step.FromClause;    BuildFromClause(statement, statement.SelectQuery);
				BuildStep = Step.WhereClause;   BuildWhereClause(statement.SelectQuery);
				BuildStep = Step.GroupByClause; BuildGroupByClause(statement.SelectQuery);
				BuildStep = Step.HavingClause;  BuildHavingClause(statement.SelectQuery);
				BuildStep = Step.OrderByClause; BuildOrderByClause(statement.SelectQuery);
				BuildStep = Step.OffsetLimit;   BuildOffsetLimit(statement.SelectQuery);
			}

			if (insertClause.WithIdentity)
				BuildGetIdentity(insertClause);
			else
			{
				BuildReturningSubclause(statement);
			}
		}

		protected void BuildInsertQuery2(SqlStatement statement, SqlInsertClause insertClause, bool addAlias)
		{
			BuildStep = Step.InsertClause;
			BuildInsertClause(statement, insertClause, addAlias);

			AppendIndent().AppendLine("SELECT * FROM");
			AppendIndent().AppendLine("(");

			++Indent;

			BuildStep = Step.WithClause;   BuildWithClause(statement.GetWithClause());

			if (statement.QueryType == QueryType.Insert && statement.SelectQuery!.From.Tables.Count != 0)
			{
				BuildStep = Step.SelectClause;  BuildSelectClause(statement.SelectQuery);
				BuildStep = Step.FromClause;    BuildFromClause(statement, statement.SelectQuery);
				BuildStep = Step.WhereClause;   BuildWhereClause(statement.SelectQuery);
				BuildStep = Step.GroupByClause; BuildGroupByClause(statement.SelectQuery);
				BuildStep = Step.HavingClause;  BuildHavingClause(statement.SelectQuery);
				BuildStep = Step.OrderByClause; BuildOrderByClause(statement.SelectQuery);
				BuildStep = Step.OffsetLimit;   BuildOffsetLimit(statement.SelectQuery);
			}

			if (insertClause.WithIdentity)
				BuildGetIdentity(insertClause);
			else
				BuildReturningSubclause(statement);

			--Indent;

			AppendIndent().AppendLine(")");
		}

		protected virtual void BuildUnknownQuery()
		{
			throw new SqlException("Unknown query type '{0}'.", Statement.QueryType);
		}

		public virtual StringBuilder ConvertTableName(StringBuilder sb, string? server, string? database, string? schema, string table)
		{
			if (server   != null) server   = ConvertInline(server,   ConvertType.NameToServer);
			if (database != null) database = ConvertInline(database, ConvertType.NameToDatabase);
			if (schema   != null) schema   = ConvertInline(schema,   ConvertType.NameToSchema);
								  table    = ConvertInline(table,    ConvertType.NameToQueryTable);

			return BuildTableName(sb, server, database, schema, table);
		}

		public virtual StringBuilder BuildTableName(StringBuilder sb,
			string? server,
			string? database,
			string? schema,
			string table)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			if (database != null && database.Length == 0) database = null;
			if (schema   != null && schema.  Length == 0) schema   = null;

			if (database != null)
			{
				if (schema == null) sb.Append(database).Append("..");
				else                sb.Append(database).Append(".").Append(schema).Append(".");
			}
			else if (schema != null) sb.Append(schema).Append(".");

			return sb.Append(table);
		}

		public string ConvertInline(string value, ConvertType convertType)
		{
			return Convert(new StringBuilder(), value, convertType).ToString();
		}

		public virtual StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			sb.Append(value);
			return sb;
		}

		#endregion

		#region Build CTE

		protected virtual bool IsRecursiveCteKeywordRequired => false;

		protected virtual void BuildWithClause(SqlWithClause? with)
		{
			if (with == null || with.Clauses.Count == 0)
				return;

			var first = true;

			foreach (var cte in with.Clauses)
			{
				if (first)
				{
					AppendIndent();
					StringBuilder.Append("WITH ");

					if (IsRecursiveCteKeywordRequired && with.Clauses.Any(c => c.IsRecursive))
						StringBuilder.Append("RECURSIVE ");

					first = false;
				}
				else
				{
					StringBuilder.Append(',').AppendLine();
					AppendIndent();
				}

				ConvertTableName(StringBuilder, null, null, null, cte.Name!);

				if (cte.Fields!.Length > 3)
				{
					StringBuilder.AppendLine();
					AppendIndent(); StringBuilder.AppendLine("(");
					++Indent;

					var firstField = true;
					foreach (var field in cte.Fields)
					{
						if (!firstField)
							StringBuilder.AppendLine(",");
						firstField = false;
						AppendIndent();
						Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
					}

					--Indent;
					StringBuilder.AppendLine();
					AppendIndent(); StringBuilder.AppendLine(")");
				}
				else if (cte.Fields.Length > 0)
				{
					StringBuilder.Append(" (");

					var firstField = true;
					foreach (var field in cte.Fields)
					{
						if (!firstField)
							StringBuilder.Append(", ");
						firstField = false;
						Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
					}
					StringBuilder.AppendLine(")");
				}
				else
				{
					StringBuilder.Append(' ');
				}

				AppendIndent();
				StringBuilder.AppendLine("AS");
				AppendIndent();
				StringBuilder.AppendLine("(");

				Indent++;

				BuildCteBody(cte.Body!);

				Indent--;

				AppendIndent();
				StringBuilder.Append(")");
			}

			StringBuilder.AppendLine();
		}

		#endregion

		#region Build Select

		protected virtual void BuildSelectClause(SelectQuery selectQuery)
		{
			AppendIndent();
			StringBuilder.Append("SELECT");

			if (selectQuery.Select.IsDistinct)
				StringBuilder.Append(" DISTINCT");

			BuildSkipFirst(selectQuery);

			StringBuilder.AppendLine();
			BuildColumns(selectQuery);
		}

		protected virtual IEnumerable<SqlColumn> GetSelectedColumns(SelectQuery selectQuery)
		{
			return selectQuery.Select.Columns;
		}

		protected virtual void BuildColumns(SelectQuery selectQuery)
		{
			Indent++;

			var first = true;

			foreach (var col in GetSelectedColumns(selectQuery))
			{
				if (!first)
					StringBuilder.Append(',').AppendLine();
				first = false;

				var addAlias = true;

				AppendIndent();
				BuildColumnExpression(selectQuery, col.Expression, col.Alias, ref addAlias);

				if (!SkipAlias && addAlias && !col.Alias.IsNullOrEmpty())
				{
					StringBuilder.Append(" as ");
					Convert(StringBuilder, col.Alias, ConvertType.NameToQueryFieldAlias);
				}
			}

			if (first)
				AppendIndent().Append("*");

			Indent--;

			StringBuilder.AppendLine();
		}

		protected virtual void BuildColumnExpression(SelectQuery? selectQuery, ISqlExpression expr, string? alias, ref bool addAlias)
		{
			BuildExpression(expr, true, true, alias, ref addAlias, true);
		}

		#endregion

		#region Build Delete

		protected virtual void BuildDeleteClause(SqlDeleteStatement deleteStatement)
		{
			AppendIndent();
			StringBuilder.Append("DELETE");
			BuildSkipFirst(deleteStatement.SelectQuery);
			StringBuilder.Append(" ");
		}

		#endregion

		#region Build Update

		protected virtual void BuildUpdateClause(SqlStatement statement, SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			BuildUpdateTable(selectQuery, updateClause);
			BuildUpdateSet  (selectQuery, updateClause);
		}

		protected virtual void BuildUpdateTable(SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			AppendIndent().Append("UPDATE");

			BuildSkipFirst(selectQuery);

			StringBuilder.AppendLine().Append('\t');
			BuildUpdateTableName(selectQuery, updateClause);
			StringBuilder.AppendLine();
		}

		protected virtual void BuildUpdateTableName(SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			if (updateClause.Table != null && (selectQuery.From.Tables.Count == 0 || updateClause.Table != selectQuery.From.Tables[0].Source))
			{
				BuildPhysicalTable(updateClause.Table, null);
			}
			else
			{
				if (selectQuery.From.Tables[0].Source is SelectQuery)
					StringBuilder.Length--;

				BuildTableName(selectQuery.From.Tables[0], true, true);
			}
		}

		protected virtual void BuildUpdateSet(SelectQuery? selectQuery, SqlUpdateClause updateClause)
		{
			AppendIndent()
				.AppendLine("SET");

			Indent++;

			var first = true;

			foreach (var expr in updateClause.Items)
			{
				if (!first)
					StringBuilder.Append(',').AppendLine();
				first = false;

				AppendIndent();

				BuildExpression(expr.Column, SqlProviderFlags.IsUpdateSetTableAliasSupported, true, false);

				if (expr.Expression != null)
				{
					StringBuilder.Append(" = ");

					var addAlias = false;

					BuildColumnExpression(selectQuery, expr.Expression, null, ref addAlias);
				}
			}

			Indent--;

			StringBuilder.AppendLine();
		}

		#endregion

		#region Build Insert

		protected void BuildInsertClause(SqlStatement statement, SqlInsertClause insertClause, bool addAlias)
		{
			BuildInsertClause(statement, insertClause, "INSERT INTO ", true, addAlias);
		}

		protected virtual void BuildEmptyInsert(SqlInsertClause insertClause)
		{
			StringBuilder.AppendLine("DEFAULT VALUES");
		}

		protected virtual void BuildOutputSubclause(SqlStatement statement, SqlInsertClause insertClause)
		{
		}

		protected virtual void BuildReturningSubclause(SqlStatement statement)
		{
		}

		protected virtual void BuildInsertClause(SqlStatement statement, SqlInsertClause insertClause, string? insertText, bool appendTableName, bool addAlias)
		{
			AppendIndent().Append(insertText);

			if (appendTableName)
			{
				BuildPhysicalTable(insertClause.Into!, null);

				if (addAlias)
				{
					var ts = Statement.SelectQuery!.GetTableSource(insertClause.Into!);
					var alias = GetTableAlias(ts!);
					if (alias != null)
					{
						StringBuilder
							.Append(" AS ");
						Convert(StringBuilder, alias, ConvertType.NameToQueryTableAlias);
					}
				}
			}

			if (insertClause.Items.Count == 0)
			{
				StringBuilder.Append(' ');

				BuildOutputSubclause(statement, insertClause);

				BuildEmptyInsert(insertClause);
			}
			else
			{
				StringBuilder.AppendLine();

				AppendIndent().AppendLine("(");

				Indent++;

				var first = true;

				foreach (var expr in insertClause.Items)
				{
					if (!first)
						StringBuilder.Append(',').AppendLine();
					first = false;

					AppendIndent();
					BuildExpression(expr.Column, false, true);
				}

				Indent--;

				StringBuilder.AppendLine();
				AppendIndent().AppendLine(")");

				BuildOutputSubclause(statement, insertClause);

				if (statement.QueryType == QueryType.InsertOrUpdate || statement.EnsureQuery().From.Tables.Count == 0)
				{
					AppendIndent().AppendLine("VALUES");
					AppendIndent().AppendLine("(");

					Indent++;

					first = true;

					foreach (var expr in insertClause.Items)
					{
						if (!first)
							StringBuilder.Append(',').AppendLine();
						first = false;

						AppendIndent();
						BuildExpression(expr.Expression!);
					}

					Indent--;

					StringBuilder.AppendLine();
					AppendIndent().AppendLine(")");
				}
			}
		}

		protected virtual void BuildGetIdentity(SqlInsertClause insertClause)
		{
			//throw new SqlException("Insert with identity is not supported by the '{0}' sql provider.", Name);
		}

		#endregion

		#region Build InsertOrUpdate

		protected virtual void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			throw new SqlException("InsertOrUpdate query type is not supported by {0} provider.", Name);
		}

		protected virtual void BuildInsertOrUpdateQueryAsMerge(SqlInsertOrUpdateStatement insertOrUpdate, string? fromDummyTable)
		{
			SkipAlias = false;

			var table       = insertOrUpdate.Insert.Into;
			var targetAlias = ConvertInline(insertOrUpdate.SelectQuery.From.Tables[0].Alias!, ConvertType.NameToQueryTableAlias);
			var sourceAlias = ConvertInline(GetTempAliases(1, "s")[0],        ConvertType.NameToQueryTableAlias);
			var keys        = insertOrUpdate.Update.Keys;

			AppendIndent().Append("MERGE INTO ");
			BuildPhysicalTable(table!, null);
			StringBuilder.Append(' ').AppendLine(targetAlias);

			AppendIndent().Append("USING (SELECT ");

			ExtractMergeParametersIfCannotCombine(insertOrUpdate, keys);

			for (var i = 0; i < keys.Count; i++)
			{
				BuildExpression(keys[i].Expression!, false, false);
				StringBuilder.Append(" AS ");
				BuildExpression(keys[i].Column, false, false);

				if (i + 1 < keys.Count)
					StringBuilder.Append(", ");
			}

			if (!string.IsNullOrEmpty(fromDummyTable))
				StringBuilder.Append(' ').Append(fromDummyTable);

			StringBuilder.Append(") ").Append(sourceAlias).AppendLine(" ON");

			AppendIndent().AppendLine("(");

			Indent++;

			for (var i = 0; i < keys.Count; i++)
			{
				var key = keys[i];

				AppendIndent();

				if (key.Column.CanBeNull)
				{
					StringBuilder.Append("(");

					StringBuilder.Append(targetAlias).Append('.');
					BuildExpression(key.Column, false, false);
					StringBuilder.Append(" IS NULL AND ");

					StringBuilder.Append(sourceAlias).Append('.');
					BuildExpression(key.Column, false, false);
					StringBuilder.Append(" IS NULL OR ");
				}

				StringBuilder.Append(targetAlias).Append('.');
				BuildExpression(key.Column, false, false);

				StringBuilder.Append(" = ").Append(sourceAlias).Append('.');
				BuildExpression(key.Column, false, false);

				if (key.Column.CanBeNull)
					StringBuilder.Append(")");

				if (i + 1 < keys.Count)
					StringBuilder.Append(" AND");

				StringBuilder.AppendLine();
			}

			Indent--;

			AppendIndent().AppendLine(")");

			if (insertOrUpdate.Update.Items.Count > 0)
			{
				AppendIndent().AppendLine("WHEN MATCHED THEN");

				Indent++;
				AppendIndent().AppendLine("UPDATE ");
				BuildUpdateSet(insertOrUpdate.SelectQuery, insertOrUpdate.Update);
				Indent--;
			}

			AppendIndent().AppendLine("WHEN NOT MATCHED THEN");

			Indent++;
			BuildInsertClause(insertOrUpdate, insertOrUpdate.Insert, "INSERT", false, false);
			Indent--;

			while (EndLine.Contains(StringBuilder[StringBuilder.Length - 1]))
				StringBuilder.Length--;
		}

		protected void ExtractMergeParametersIfCannotCombine(SqlInsertOrUpdateStatement insertOrUpdate, List<SqlSetExpression> keys)
		{
			if (!SqlProviderFlags.CanCombineParameters)
			{
				insertOrUpdate.Parameters.Clear();

				for (var i = 0; i < keys.Count; i++)
					ExtractParameters(insertOrUpdate, keys[i].Expression!);

				foreach (var expr in insertOrUpdate.Update.Items)
					ExtractParameters(insertOrUpdate, expr.Expression!);

				foreach (var expr in insertOrUpdate.Insert.Items)
					ExtractParameters(insertOrUpdate, expr.Expression!);

				if (insertOrUpdate.Parameters.Count > 0)
					insertOrUpdate.IsParameterDependent = true;
			}
		}

		private void ExtractParameters(SqlStatement statement, ISqlExpression expression)
		{
			new QueryVisitor().Visit(expression, e =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlParameter:
						{
							var p = (SqlParameter)e;

							if (p.IsQueryParameter)
								statement.Parameters.Add(p);
						}

						break;
				}
			});
		}

		protected static readonly char[] EndLine = { ' ', '\r', '\n' };

		protected void BuildInsertOrUpdateQueryAsUpdateInsert(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			AppendIndent().AppendLine("BEGIN TRAN").AppendLine();

			var buildUpdate = insertOrUpdate.Update.Items.Count > 0;
			if (buildUpdate)
			{
				BuildUpdateQuery(insertOrUpdate, insertOrUpdate.SelectQuery, insertOrUpdate.Update);
			}
			else
			{
				AppendIndent().AppendLine("IF NOT EXISTS(");
				Indent++;
				AppendIndent().AppendLine("SELECT 1 ");
				BuildFromClause(insertOrUpdate, insertOrUpdate.SelectQuery);
			}

			AppendIndent().AppendLine("WHERE");

			var alias = ConvertInline(insertOrUpdate.SelectQuery.From.Tables[0].Alias!, ConvertType.NameToQueryTableAlias);
			var exprs = insertOrUpdate.Update.Keys;

			Indent++;

			for (var i = 0; i < exprs.Count; i++)
			{
				var expr = exprs[i];

				AppendIndent();

				if (expr.Column.CanBeNull)
				{
					StringBuilder.Append("(");

					StringBuilder.Append(alias).Append('.');
					BuildExpression(expr.Column, false, false);
					StringBuilder.Append(" IS NULL OR ");
				}

				StringBuilder.Append(alias).Append('.');
				BuildExpression(expr.Column, false, false);

				StringBuilder.Append(" = ");
				BuildExpression(Precedence.Comparison, expr.Expression!);

				if (expr.Column.CanBeNull)
					StringBuilder.Append(")");

				if (i + 1 < exprs.Count)
					StringBuilder.Append(" AND");

				StringBuilder.AppendLine();
			}

			Indent--;

			if (buildUpdate)
			{
				StringBuilder.AppendLine();
				AppendIndent().AppendLine("IF @@ROWCOUNT = 0");
			}
			else
			{
				Indent--;
				AppendIndent().AppendLine(")");
			}

			AppendIndent().AppendLine("BEGIN");

			Indent++;

			BuildInsertQuery(insertOrUpdate, insertOrUpdate.Insert, false);

			Indent--;

			AppendIndent().AppendLine("END");

			StringBuilder.AppendLine();
			AppendIndent().AppendLine("COMMIT");
		}

		#endregion

		#region Build DDL

		protected virtual void BuildTruncateTableStatement(SqlTruncateTableStatement truncateTable)
		{
			var table = truncateTable.Table;

			AppendIndent();

			BuildTruncateTable(truncateTable);

			BuildPhysicalTable(table!, null);
			StringBuilder.AppendLine();
		}

		protected virtual void BuildTruncateTable(SqlTruncateTableStatement truncateTable)
		{
			//StringBuilder.Append("TRUNCATE TABLE ");
			StringBuilder.Append("DELETE FROM ");
		}

		protected virtual void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			AppendIndent().Append("DROP TABLE ");
			BuildPhysicalTable(dropTable.Table!, null);
			StringBuilder.AppendLine();
		}

		protected void BuildDropTableStatementIfExists(SqlDropTableStatement dropTable)
		{
			AppendIndent().Append("DROP TABLE ");

			if (dropTable.IfExists)
				StringBuilder.Append("IF EXISTS ");

			BuildPhysicalTable(dropTable.Table!, null);
			StringBuilder.AppendLine();
		}

		protected virtual void BuildStartCreateTableStatement(SqlCreateTableStatement createTable)
		{
			if (createTable.StatementHeader == null)
			{
				AppendIndent().Append("CREATE TABLE ");
				BuildPhysicalTable(createTable.Table!, null);
			}
			else
			{
				var name = WithStringBuilder(
					new StringBuilder(),
					() =>
					{
						BuildPhysicalTable(createTable.Table!, null);
						return StringBuilder.ToString();
					});

				AppendIndent().AppendFormat(createTable.StatementHeader, name);
			}
		}

		protected virtual void BuildEndCreateTableStatement(SqlCreateTableStatement createTable)
		{
			if (createTable.StatementFooter != null)
				AppendIndent().Append(createTable.StatementFooter);
		}

		class CreateFieldInfo
		{
			public SqlField      Field = null!;
			public StringBuilder StringBuilder = null!;
			public string        Name = null!;
			public string?       Type;
			public string        Identity = null!;
			public string?       Null;
		}

		protected virtual void BuildCreateTableStatement(SqlCreateTableStatement createTable)
		{
			var table = createTable.Table!;

			BuildStartCreateTableStatement(createTable);

			StringBuilder.AppendLine();
			AppendIndent().Append("(");
			Indent++;

			// Order columns by the Order field. Positive first then negative.
			var orderedFields = table.Fields.Values.OrderBy(_ => _.CreateOrder >= 0 ? 0 : (_.CreateOrder == null ? 1 : 2)).ThenBy(_ => _.CreateOrder);
			var fields = orderedFields.Select(f => new CreateFieldInfo { Field = f, StringBuilder = new StringBuilder() }).ToList();
			var maxlen = 0;

			void AppendToMax(bool addCreateFormat)
			{
				foreach (var field in fields)
					if (addCreateFormat || field.Field.CreateFormat == null)
						while (maxlen > field.StringBuilder.Length)
							field.StringBuilder.Append(' ');
			}

			var isAnyCreateFormat = false;

			// Build field name.
			//
			foreach (var field in fields)
			{
				Convert(field.StringBuilder, field.Field.PhysicalName, ConvertType.NameToQueryField);

				if (maxlen < field.StringBuilder.Length)
					maxlen = field.StringBuilder.Length;

				if (field.Field.CreateFormat != null)
					isAnyCreateFormat = true;
			}

			AppendToMax(true);

			if (isAnyCreateFormat)
				foreach (var field in fields)
					if (field.Field.CreateFormat != null)
						field.Name = field.StringBuilder.ToString() + ' ';

			// Build field type.
			//
			foreach (var field in fields)
			{
				field.StringBuilder.Append(' ');

				if (!string.IsNullOrEmpty(field.Field.Type!.Value.DbType))
					field.StringBuilder.Append(field.Field.Type!.Value.DbType);
				else
				{
					var sb = StringBuilder;
					StringBuilder = field.StringBuilder;

					BuildCreateTableFieldType(field.Field);

					StringBuilder = sb;
				}

				if (maxlen < field.StringBuilder.Length)
					maxlen = field.StringBuilder.Length;
			}

			AppendToMax(true);

			if (isAnyCreateFormat)
			{
				foreach (var field in fields)
				{
					if (field.Field.CreateFormat != null)
					{
						var sb = field.StringBuilder;

						field.Type = sb.ToString().Substring(field.Name.Length) + ' ';
						sb.Length = 0;
					}
				}
			}

			var hasIdentity = fields.Any(f => f.Field.IsIdentity);

			// Build identity attribute.
			//
			if (hasIdentity)
			{
				foreach (var field in fields)
				{
					if (field.Field.CreateFormat == null)
						field.StringBuilder.Append(' ');

					if (field.Field.IsIdentity)
						WithStringBuilder(field.StringBuilder, () => BuildCreateTableIdentityAttribute1(field.Field));

					if (field.Field.CreateFormat != null)
					{
						field.Identity = field.StringBuilder.ToString();

						if (field.Identity.Length != 0)
							field.Identity += ' ';

						field.StringBuilder.Length = 0;
					}
					else if (maxlen < field.StringBuilder.Length)
					{
						maxlen = field.StringBuilder.Length;
					}
				}

				AppendToMax(false);
			}

			// Build nullable attribute.
			//
			foreach (var field in fields)
			{
				if (field.Field.CreateFormat == null)
					field.StringBuilder.Append(' ');

				WithStringBuilder(
					field.StringBuilder,
					() => BuildCreateTableNullAttribute(field.Field, createTable.DefaultNullable));

				if (field.Field.CreateFormat != null)
				{
					field.Null = field.StringBuilder.ToString() + ' ';
					field.StringBuilder.Length = 0;
				}
				else if (maxlen < field.StringBuilder.Length)
				{
					maxlen = field.StringBuilder.Length;
				}
			}

			AppendToMax(false);

			// Build identity attribute.
			//
			if (hasIdentity)
			{
				foreach (var field in fields)
				{
					if (field.Field.CreateFormat == null)
						field.StringBuilder.Append(' ');

					if (field.Field.IsIdentity)
						WithStringBuilder(field.StringBuilder, () => BuildCreateTableIdentityAttribute2(field.Field));

					if (field.Field.CreateFormat != null)
					{
						if (field.Field.CreateFormat != null && field.Identity.Length == 0)
						{
							field.Identity = field.StringBuilder.ToString() + ' ';
							field.StringBuilder.Length = 0;
						}
					}
					else if (maxlen < field.StringBuilder.Length)
					{
						maxlen = field.StringBuilder.Length;
					}
				}

				AppendToMax(false);
			}

			// Build fields.
			//
			for (var i = 0; i < fields.Count; i++)
			{
				while (fields[i].StringBuilder.Length > 0 && fields[i].StringBuilder[fields[i].StringBuilder.Length - 1] == ' ')
					fields[i].StringBuilder.Length--;

				StringBuilder.AppendLine(i == 0 ? "" : ",");
				AppendIndent();

				var field = fields[i];

				if (field.Field.CreateFormat != null)
				{
					StringBuilder.AppendFormat(field.Field.CreateFormat, field.Name, field.Type, field.Null, field.Identity);

					while (StringBuilder.Length > 0 && StringBuilder[StringBuilder.Length - 1] == ' ')
						StringBuilder.Length--;
				}
				else
				{
					StringBuilder.Append(field.StringBuilder);
				}
			}

			var pk =
			(
				from f in fields
				where f.Field.IsPrimaryKey
				orderby f.Field.PrimaryKeyOrder
				select f
			).ToList();

			if (pk.Count > 0)
			{
				StringBuilder.AppendLine(",").AppendLine();

				BuildCreateTablePrimaryKey(createTable, ConvertInline("PK_" + createTable.Table!.PhysicalName, ConvertType.NameToQueryTable),
					pk.Select(f => ConvertInline(f.Field.PhysicalName, ConvertType.NameToQueryField)));
			}

			Indent--;
			StringBuilder.AppendLine();
			AppendIndent().AppendLine(")");

			BuildEndCreateTableStatement(createTable);
		}

		internal void BuildTypeName(StringBuilder sb, SqlDataType type)
		{
			StringBuilder = sb;
			BuildDataType(type, true);
		}

		protected virtual void BuildCreateTableFieldType(SqlField field)
		{
			BuildDataType(new SqlDataType(field), true);
		}

		protected virtual void BuildCreateTableNullAttribute(SqlField field, DefaultNullable defaultNullable)
		{
			if (defaultNullable == DefaultNullable.Null && field.CanBeNull)
				return;

			if (defaultNullable == DefaultNullable.NotNull && !field.CanBeNull)
				return;

			StringBuilder.Append(field.CanBeNull ? "    NULL" : "NOT NULL");
		}

		protected virtual void BuildCreateTableIdentityAttribute1(SqlField field)
		{
		}

		protected virtual void BuildCreateTableIdentityAttribute2(SqlField field)
		{
		}

		protected virtual void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY (");
			StringBuilder.Append(fieldNames.Aggregate((f1, f2) => f1 + ", " + f2));
			StringBuilder.Append(")");
		}

		#endregion

		#region Build From

		protected virtual void BuildFromClause(SqlStatement statement, SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count == 0 || selectQuery.From.Tables[0].Alias == "$F")
				return;

			AppendIndent();

			StringBuilder.Append("FROM").AppendLine();

			Indent++;
			AppendIndent();

			var first = true;

			foreach (var ts in selectQuery.From.Tables)
			{
				if (!first)
				{
					StringBuilder.AppendLine(",");
					AppendIndent();
				}

				first = false;

				var jn = ParenthesizeJoin(ts.Joins) ? ts.GetJoinNumber() : 0;

				if (jn > 0)
				{
					jn--;
					for (var i = 0; i < jn; i++)
						StringBuilder.Append("(");
				}

				BuildTableName(ts, true, true);

				foreach (var jt in ts.Joins)
					BuildJoinTable(selectQuery, jt, ref jn);
			}

			Indent--;

			StringBuilder.AppendLine();
		}

		private static readonly Regex _selectDetector = new Regex(@"^[\W\r\n]*select\W+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		protected bool? BuildPhysicalTable(ISqlTableSource table, string? alias)
		{
			bool? buildAlias = null;
			switch (table.ElementType)
			{
				case QueryElementType.SqlTable        :
				case QueryElementType.TableSource     :
					StringBuilder.Append(GetPhysicalTableName(table, alias));
					break;

				case QueryElementType.SqlQuery        :
					StringBuilder.Append("(").AppendLine();
					BuildSqlBuilder((SelectQuery)table, Indent + 1, false);
					AppendIndent().Append(")");
					break;

				case QueryElementType.SqlCteTable     :
				case QueryElementType.MergeSourceTable:
					StringBuilder.Append(GetPhysicalTableName(table, alias));
					break;

				case QueryElementType.SqlRawSqlTable  :

					var rawSqlTable = (SqlRawSqlTable)table;

					var appendParentheses = _selectDetector.IsMatch(rawSqlTable.SQL);
					var multiLine         = appendParentheses || rawSqlTable.SQL.Contains('\n');

					if (appendParentheses)
						StringBuilder.Append("(");
					if (multiLine)
						StringBuilder.AppendLine();

					var parameters = rawSqlTable.Parameters;
					if (rawSqlTable.Parameters.Any(e => e.ElementType == QueryElementType.SqlAliasPlaceholder))
					{
						buildAlias = false;
						var aliasExpr = new SqlExpression(ConvertInline(alias!, ConvertType.NameToQueryTableAlias), Precedence.Primary);
						parameters = rawSqlTable.Parameters.Select(e =>
								e.ElementType == QueryElementType.SqlAliasPlaceholder ? aliasExpr : e)
							.ToArray();
					}

					BuildFormatValues(IdentText(rawSqlTable.SQL, multiLine ? Indent + 1 : 0), parameters, () => Precedence.Primary);

					if (multiLine)
						StringBuilder.AppendLine();
					if (appendParentheses)
						AppendIndent().Append(")");

					break;

				default:
					throw new InvalidOperationException($"Unexpected table type {table.ElementType}");
			}

			return buildAlias;
		}

		protected void BuildTableName(SqlTableSource ts, bool buildName, bool buildAlias)
		{
			if (buildName)
			{
				var alias = GetTableAlias(ts);
				var isBuildAlias = BuildPhysicalTable(ts.Source, alias);
				if (isBuildAlias == false)
					buildAlias = false;
			}

			if (buildAlias)
			{
				if (ts.SqlTableType != SqlTableType.Expression)
				{
					var alias = GetTableAlias(ts);

					if (!string.IsNullOrEmpty(alias))
					{
						if (buildName)
							StringBuilder.Append(" ");
						Convert(StringBuilder, alias!, ConvertType.NameToQueryTableAlias);
					}
				}
			}
		}

		void BuildJoinTable(SelectQuery selectQuery, SqlJoinedTable join, ref int joinCounter)
		{
			StringBuilder.AppendLine();
			Indent++;
			AppendIndent();

			var buildOn = BuildJoinType(join);

			if (IsNestedJoinParenthesisRequired && join.Table.Joins.Count != 0)
				StringBuilder.Append('(');

			BuildTableName(join.Table, true, true);

			if (IsNestedJoinSupported && join.Table.Joins.Count != 0)
			{
				foreach (var jt in join.Table.Joins)
					BuildJoinTable(selectQuery, jt, ref joinCounter);

				if (IsNestedJoinParenthesisRequired && join.Table.Joins.Count != 0)
					StringBuilder.Append(')');

				if (buildOn)
				{
					StringBuilder.AppendLine();
					AppendIndent();
					StringBuilder.Append("ON ");
				}
			}
			else if (buildOn)
				StringBuilder.Append(" ON ");

			if (WrapJoinCondition && join.Condition.Conditions.Count > 0)
				StringBuilder.Append("(");

			if (buildOn)
			{
				if (join.Condition.Conditions.Count != 0)
					BuildSearchCondition(Precedence.Unknown, join.Condition);
				else
					StringBuilder.Append("1=1");
			}

			if (WrapJoinCondition && join.Condition.Conditions.Count > 0)
				StringBuilder.Append(")");

			if (joinCounter > 0)
			{
				joinCounter--;
				StringBuilder.Append(")");
			}

			if (!IsNestedJoinSupported)
				foreach (var jt in join.Table.Joins)
					BuildJoinTable(selectQuery, jt, ref joinCounter);

			Indent--;
		}

		protected virtual bool BuildJoinType(SqlJoinedTable join)
		{
			switch (join.JoinType)
			{
				case JoinType.Inner     :
					if (SqlProviderFlags.IsCrossJoinSupported && join.Condition.Conditions.IsNullOrEmpty())
					{
						StringBuilder.Append("CROSS JOIN ");
						return false;
					}
					else
					{
						StringBuilder.Append("INNER JOIN ");
						return true;
					}
				case JoinType.Left      : StringBuilder.Append("LEFT JOIN ");   return true;
				case JoinType.CrossApply: StringBuilder.Append("CROSS APPLY "); return false;
				case JoinType.OuterApply: StringBuilder.Append("OUTER APPLY "); return false;
				case JoinType.Right     : StringBuilder.Append("RIGHT JOIN ");  return true;
				case JoinType.Full      : StringBuilder.Append("FULL JOIN ");   return true;
				default: throw new InvalidOperationException();
			}
		}

		#endregion

		#region Where Clause

		protected virtual bool BuildWhere(SelectQuery selectQuery)
		{
			return selectQuery.Where.SearchCondition.Conditions.Count != 0;
		}

		protected virtual void BuildWhereClause(SelectQuery selectQuery)
		{
			if (!BuildWhere(selectQuery))
				return;

			AppendIndent();

			StringBuilder.Append("WHERE").AppendLine();

			Indent++;
			AppendIndent();
			BuildWhereSearchCondition(selectQuery, selectQuery.Where.SearchCondition);
			Indent--;

			StringBuilder.AppendLine();
		}

		#endregion

		#region GroupBy Clause

		protected virtual void BuildGroupByClause(SelectQuery selectQuery)
		{
			if (selectQuery.GroupBy.Items.Count == 0)
				return;

			var items = selectQuery.GroupBy.Items.Where(i => !(i is SqlValue || i is SqlParameter)).ToList();

			if (items.Count == 0)
				return;

			BuildGroupByBody(selectQuery.GroupBy.GroupingType, items);
		}

		protected virtual void BuildGroupByBody(GroupingType groupingType, List<ISqlExpression> items)
		{
			AppendIndent();

			StringBuilder.Append("GROUP BY");

			switch (groupingType)
			{
				case GroupingType.Default:
					break;
				case GroupingType.GroupBySets:
					StringBuilder.Append(" GROUPING SETS");
					break;
				case GroupingType.Rollup:
					StringBuilder.Append(" ROLLUP");
					break;
				case GroupingType.Cube:
					StringBuilder.Append(" CUBE");
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (groupingType != GroupingType.Default)
				StringBuilder.Append(" (");

			StringBuilder.AppendLine();

			Indent++;

			for (var i = 0; i < items.Count; i++)
			{
				AppendIndent();

				BuildExpression(items[i]);

				if (i + 1 < items.Count)
					StringBuilder.Append(',');

				StringBuilder.AppendLine();
			}

			Indent--;

			if (groupingType != GroupingType.Default)
			{
				AppendIndent();
				StringBuilder.Append(")").AppendLine();
			}
		}

		#endregion

		#region Having Clause

		protected virtual void BuildHavingClause(SelectQuery selectQuery)
		{
			if (selectQuery.Having.SearchCondition.Conditions.Count == 0)
				return;

			AppendIndent();

			StringBuilder.Append("HAVING").AppendLine();

			Indent++;
			AppendIndent();
			BuildWhereSearchCondition(selectQuery, selectQuery.Having.SearchCondition);
			Indent--;

			StringBuilder.AppendLine();
		}

		#endregion

		#region OrderBy Clause

		protected virtual void BuildOrderByClause(SelectQuery selectQuery)
		{
			if (selectQuery.OrderBy.Items.Count == 0)
				return;

			AppendIndent();

			StringBuilder.Append("ORDER BY").AppendLine();

			Indent++;

			for (var i = 0; i < selectQuery.OrderBy.Items.Count; i++)
			{
				AppendIndent();

				var item = selectQuery.OrderBy.Items[i];

				BuildExpression(item.Expression);

				if (item.IsDescending)
					StringBuilder.Append(" DESC");

				if (i + 1 < selectQuery.OrderBy.Items.Count)
					StringBuilder.Append(',');

				StringBuilder.AppendLine();
			}

			Indent--;
		}

		#endregion

		#region Skip/Take

		protected virtual bool   SkipFirst    => true;
		protected virtual string? SkipFormat   => null;
		protected virtual string? FirstFormat  (SelectQuery selectQuery) => null;
		protected virtual string? LimitFormat  (SelectQuery selectQuery) => null;
		protected virtual string? OffsetFormat (SelectQuery selectQuery) => null;
		protected virtual bool   OffsetFirst  => false;
		protected virtual string TakePercent  => "PERCENT";
		protected virtual string TakeTies     => "WITH TIES";

		protected bool NeedSkip(SelectQuery selectQuery)
			=> selectQuery.Select.SkipValue != null && SqlProviderFlags.GetIsSkipSupportedFlag(selectQuery);

		protected bool NeedTake(SelectQuery selectQuery)
			=> selectQuery.Select.TakeValue != null && SqlProviderFlags.IsTakeSupported;

		protected virtual void BuildSkipFirst(SelectQuery selectQuery)
		{
			if (SkipFirst && NeedSkip(selectQuery) && SkipFormat != null)
				StringBuilder.Append(' ').AppendFormat(
					SkipFormat, WithStringBuilder(new StringBuilder(), () => BuildExpression(selectQuery.Select.SkipValue!)));

			if (NeedTake(selectQuery) && FirstFormat(selectQuery) != null)
			{
				StringBuilder.Append(' ').AppendFormat(
					FirstFormat(selectQuery)!, WithStringBuilder(new StringBuilder(), () => BuildExpression(selectQuery.Select.TakeValue!)));

				BuildTakeHints(selectQuery);
			}

			if (!SkipFirst && NeedSkip(selectQuery) && SkipFormat != null)
				StringBuilder.Append(' ').AppendFormat(
					SkipFormat, WithStringBuilder(new StringBuilder(), () => BuildExpression(selectQuery.Select.SkipValue!)));
		}

		protected virtual void BuildTakeHints(SelectQuery selectQuery)
		{
			if (selectQuery.Select.TakeHints == null)
				return;

			if ((selectQuery.Select.TakeHints.Value & TakeHints.Percent) != 0)
				StringBuilder.Append(' ').Append(TakePercent);

			if ((selectQuery.Select.TakeHints.Value & TakeHints.WithTies) != 0)
				StringBuilder.Append(' ').Append(TakeTies);
		}

		protected virtual void BuildOffsetLimit(SelectQuery selectQuery)
		{
			var doSkip = NeedSkip(selectQuery) && OffsetFormat(selectQuery) != null;
			var doTake = NeedTake(selectQuery) && LimitFormat(selectQuery)  != null;

			if (doSkip || doTake)
			{
				AppendIndent();

				if (doSkip && OffsetFirst)
				{
					StringBuilder.AppendFormat(
						OffsetFormat(selectQuery)!, WithStringBuilder(new StringBuilder(), () => BuildExpression(selectQuery.Select.SkipValue!)));

					if (doTake)
						StringBuilder.Append(' ');
				}

				if (doTake)
				{
					StringBuilder.AppendFormat(
						LimitFormat(selectQuery)!, WithStringBuilder(new StringBuilder(), () => BuildExpression(selectQuery.Select.TakeValue!)));

					if (doSkip)
						StringBuilder.Append(' ');
				}

				if (doSkip && !OffsetFirst)
					StringBuilder.AppendFormat(
						OffsetFormat(selectQuery)!, WithStringBuilder(new StringBuilder(), () => BuildExpression(selectQuery.Select.SkipValue!)));

				StringBuilder.AppendLine();
			}
		}

		#endregion

		#region Builders

		#region BuildSearchCondition

		protected virtual void BuildWhereSearchCondition(SelectQuery selectQuery, SqlSearchCondition condition)
		{
			BuildSearchCondition(Precedence.Unknown, condition);
		}

		protected virtual void BuildSearchCondition(SqlSearchCondition condition)
		{
			var isOr = (bool?)null;
			var len = StringBuilder.Length;
			var parentPrecedence = condition.Precedence + 1;

			//TODO: Possible refactoring
			var whereSearchCondition = Statement.SelectQuery?.Where.SearchCondition;

			foreach (var cond in condition.Conditions)
			{
				if (isOr != null)
				{
					StringBuilder.Append(isOr.Value ? " OR" : " AND");

					if (condition.Conditions.Count < 4 && StringBuilder.Length - len < 50 || condition != whereSearchCondition)
					{
						StringBuilder.Append(' ');
					}
					else
					{
						StringBuilder.AppendLine();
						AppendIndent();
						len = StringBuilder.Length;
					}
				}

				if (cond.IsNot)
					StringBuilder.Append("NOT ");

				var precedence = GetPrecedence(cond.Predicate);

				BuildPredicate(cond.IsNot ? Precedence.LogicalNegation : parentPrecedence, precedence, cond.Predicate);

				isOr = cond.IsOr;
			}
		}

		protected virtual void BuildSearchCondition(int parentPrecedence, SqlSearchCondition condition)
		{
			var wrap = Wrap(GetPrecedence(condition as ISqlExpression), parentPrecedence);

			if (wrap) StringBuilder.Append('(');
			BuildSearchCondition(condition);
			if (wrap) StringBuilder.Append(')');
		}

		#endregion

		#region BuildPredicate

		protected virtual void BuildPredicate(ISqlPredicate predicate)
		{
			switch (predicate.ElementType)
			{
				case QueryElementType.ExprExprPredicate:
					BuildPredicateX((SqlPredicate.ExprExpr) predicate);
					break;

				case QueryElementType.LikePredicate:
					BuildLikePredicate((SqlPredicate.Like)predicate);
					break;

				case QueryElementType.BetweenPredicate:
					{
						BuildExpression(GetPrecedence((SqlPredicate.Between)predicate), ((SqlPredicate.Between)predicate).Expr1);
						if (((SqlPredicate.Between)predicate).IsNot) StringBuilder.Append(" NOT");
						StringBuilder.Append(" BETWEEN ");
						BuildExpression(GetPrecedence((SqlPredicate.Between)predicate), ((SqlPredicate.Between)predicate).Expr2);
						StringBuilder.Append(" AND ");
						BuildExpression(GetPrecedence((SqlPredicate.Between)predicate), ((SqlPredicate.Between)predicate).Expr3);
					}

					break;

				case QueryElementType.IsTruePredicate:
					{
						var reduced = ((SqlPredicate.IsTrue)predicate).Reduce();
						
						BuildPredicate(GetPrecedence(predicate), GetPrecedence(reduced), reduced);
					}

					break;

				case QueryElementType.IsNullPredicate:
					{
						BuildExpression(GetPrecedence((SqlPredicate.IsNull)predicate), ((SqlPredicate.IsNull)predicate).Expr1);
						StringBuilder.Append(((SqlPredicate.IsNull)predicate).IsNot ? " IS NOT NULL" : " IS NULL");
					}

					break;

				case QueryElementType.InSubQueryPredicate:
					{
						BuildExpression(GetPrecedence((SqlPredicate.InSubQuery)predicate), ((SqlPredicate.InSubQuery)predicate).Expr1);
						StringBuilder.Append(((SqlPredicate.InSubQuery)predicate).IsNot ? " NOT IN " : " IN ");
						BuildExpression(GetPrecedence((SqlPredicate.InSubQuery)predicate), ((SqlPredicate.InSubQuery)predicate).SubQuery);
					}

					break;

				case QueryElementType.InListPredicate:
					BuildInListPredicate(predicate);
					break;

				case QueryElementType.FuncLikePredicate:
					BuildExpression(((SqlPredicate.FuncLike)predicate).Function.Precedence, ((SqlPredicate.FuncLike)predicate).Function);
					break;

				case QueryElementType.SearchCondition:
					BuildSearchCondition(predicate.Precedence, (SqlSearchCondition)predicate);
					break;

				case QueryElementType.NotExprPredicate:
					{
						var p = (SqlPredicate.NotExpr)predicate;

						if (p.IsNot)
							StringBuilder.Append("NOT ");

						BuildExpression(
							((SqlPredicate.NotExpr)predicate).IsNot
								? Precedence.LogicalNegation
								: GetPrecedence((SqlPredicate.NotExpr)predicate),
							((SqlPredicate.NotExpr)predicate).Expr1);
					}

					break;

				case QueryElementType.ExprPredicate:
					{
						var p = (SqlPredicate.Expr)predicate;

						if (p.Expr1 is SqlValue sqlValue)
						{
							var value = sqlValue.Value;

							if (value is bool b)
							{
								StringBuilder.Append(b ? "1 = 1" : "1 = 0");
								return;
							}
						}

						BuildExpression(GetPrecedence(p), p.Expr1);
					}

					break;

				default:
					throw new InvalidOperationException($"Unexpected predicate type {predicate.ElementType}");
			}
		}

		void BuildPredicateX(SqlPredicate.ExprExpr expr)
		{
			switch (expr.Operator)
			{
				case SqlPredicate.Operator.Equal:
				case SqlPredicate.Operator.NotEqual:
				{
					ISqlExpression? e = null;

					if (expr.Expr1 is IValueContainer container && container.Value == null)
						e = expr.Expr2;
					else if (expr.Expr2 is IValueContainer c && c.Value == null)
						e = expr.Expr1;

					if (e != null)
					{
						BuildExpression(GetPrecedence(expr), e);
						StringBuilder.Append(expr.Operator == SqlPredicate.Operator.Equal ? " IS NULL" : " IS NOT NULL");
						return;
					}

					break;
				}
			}

			BuildExpression(GetPrecedence(expr), expr.Expr1);

			switch (expr.Operator)
			{
				case SqlPredicate.Operator.Equal          : StringBuilder.Append(" = ");  break;
				case SqlPredicate.Operator.NotEqual       : StringBuilder.Append(" <> "); break;
				case SqlPredicate.Operator.Greater        : StringBuilder.Append(" > ");  break;
				case SqlPredicate.Operator.GreaterOrEqual : StringBuilder.Append(" >= "); break;
				case SqlPredicate.Operator.NotGreater     : StringBuilder.Append(" !> "); break;
				case SqlPredicate.Operator.Less           : StringBuilder.Append(" < ");  break;
				case SqlPredicate.Operator.LessOrEqual    : StringBuilder.Append(" <= "); break;
				case SqlPredicate.Operator.NotLess        : StringBuilder.Append(" !< "); break;
			}

			BuildExpression(GetPrecedence(expr), expr.Expr2);
		}

		static SqlField GetUnderlayingField(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlField: return (SqlField)expr;
				case QueryElementType.Column  : return GetUnderlayingField(((SqlColumn)expr).Expression);
			}

			throw new InvalidOperationException();
		}

		void BuildInListPredicate(ISqlPredicate predicate)
		{
			var p = (SqlPredicate.InList)predicate;

			if (p.Values == null || p.Values.Count == 0)
			{
				BuildPredicate(new SqlPredicate.Expr(new SqlValue(false)));
			}
			else
			{
				ICollection values = p.Values;

				if (p.Values.Count == 1 && p.Values[0] is SqlParameter pr &&
					!(p.Expr1.SystemType == typeof(string) && pr.Value is string))
				{
					var prValue = pr.Value;

					if (prValue == null)
					{
						BuildPredicate(new SqlPredicate.Expr(new SqlValue(false)));
						return;
					}

					if (prValue is IEnumerable items)
					{
						if (p.Expr1 is ISqlTableSource table)
						{
							var firstValue = true;
							var keys       = table.GetKeys(true);

							if (keys == null || keys.Count == 0)
								throw new SqlException("Cannot create IN expression.");

							if (keys.Count == 1)
							{
								foreach (var item in items)
								{
									if (firstValue)
									{
										firstValue = false;
										BuildExpression(GetPrecedence(p), keys[0]);
										StringBuilder.Append(p.IsNot ? " NOT IN (" : " IN (");
									}

									var field = GetUnderlayingField(keys[0]);
									var value = field.ColumnDescriptor.MemberAccessor.GetValue(item!);

									if (value is ISqlExpression expression)
										BuildExpression(expression);
									else
										BuildValue(new SqlDataType(field), value);

									StringBuilder.Append(", ");
								}
							}
							else
							{
								var len = StringBuilder.Length;
								var rem = 1;

								foreach (var item in items)
								{
									if (firstValue)
									{
										firstValue = false;
										StringBuilder.Append('(');
									}

									foreach (var key in keys)
									{
										var field = GetUnderlayingField(key);
										var value = field.ColumnDescriptor.MemberAccessor.GetValue(item!);

										BuildExpression(GetPrecedence(p), key);

										if (value == null)
										{
											StringBuilder.Append(" IS NULL");
										}
										else
										{
											StringBuilder.Append(" = ");
											BuildValue(new SqlDataType(field), value);
										}

										StringBuilder.Append(" AND ");
									}

									StringBuilder.Remove(StringBuilder.Length - 4, 4).Append("OR ");

									if (StringBuilder.Length - len >= 50)
									{
										StringBuilder.AppendLine();
										AppendIndent();
										StringBuilder.Append(' ');
										len = StringBuilder.Length;
										rem = 5 + Indent;
									}
								}

								if (!firstValue)
									StringBuilder.Remove(StringBuilder.Length - rem, rem);
							}

							if (firstValue)
								BuildPredicate(new SqlPredicate.Expr(new SqlValue(p.IsNot)));
							else
								StringBuilder.Remove(StringBuilder.Length - 2, 2).Append(')');
						}
						else
						{
							BuildInListValues(p, items);
						}

						return;
					}
				}

				BuildInListValues(p, values);
			}
		}

		void BuildInListValues(SqlPredicate.InList predicate, IEnumerable values)
		{
			var firstValue = true;
			var len        = StringBuilder.Length;
			var hasNull    = false;
			var count      = 0;
			var longList   = false;

			SqlDataType? sqlDataType = null;

			foreach (object? value in values)
			{
				if (count++ >= SqlProviderFlags.MaxInListValuesCount)
				{
					count    = 1;
					longList = true;

					// start building next bucked
					firstValue = true;
					StringBuilder.Remove(StringBuilder.Length - 2, 2).Append(')');
					if (predicate.IsNot)
						StringBuilder.Append(" AND ");
					else
						StringBuilder.Append(" OR ");
				}

				object? val = value;

				if (val is IValueContainer valueContainer)
					val = valueContainer.Value;

				if (val == null)
				{
					hasNull = true;
					continue;
				}

				if (firstValue)
				{
					firstValue = false;
					BuildExpression(GetPrecedence(predicate), predicate.Expr1);
					StringBuilder.Append(predicate.IsNot ? " NOT IN (" : " IN (");

					switch (predicate.Expr1.ElementType)
					{
						case QueryElementType.SqlField:
							{
								var field = (SqlField)predicate.Expr1;

								sqlDataType = new SqlDataType(field);
							}
							break;

						case QueryElementType.SqlParameter:
							{
								var p = (SqlParameter)predicate.Expr1;
								sqlDataType = new SqlDataType(p.Type);
							}

							break;
					}
				}

				if (value is ISqlExpression expression)
					BuildExpression(expression);
				else
					BuildValue(sqlDataType, value);

				StringBuilder.Append(", ");
			}

			if (firstValue)
			{
				BuildPredicate(
					hasNull ?
					new SqlPredicate.IsNull(predicate.Expr1, predicate.IsNot) :
					new SqlPredicate.Expr(new SqlValue(predicate.IsNot)));
			}
			else
			{
				StringBuilder.Remove(StringBuilder.Length - 2, 2).Append(')');

				if (hasNull)
				{
					StringBuilder.Insert(len, "(");
					StringBuilder.Append(" OR ");
					BuildPredicate(new SqlPredicate.IsNull(predicate.Expr1, predicate.IsNot));
					StringBuilder.Append(")");
				}
			}

			if (longList && !hasNull)
			{
				StringBuilder.Insert(len, "(");
				StringBuilder.Append(")");
			}
		}

		protected void BuildPredicate(int parentPrecedence, int precedence, ISqlPredicate predicate)
		{
			var wrap = Wrap(precedence, parentPrecedence);

			if (wrap) StringBuilder.Append('(');
			BuildPredicate(predicate);
			if (wrap) StringBuilder.Append(')');
		}

		protected virtual void BuildLikePredicate(SqlPredicate.Like predicate)
		{
			var precedence = GetPrecedence(predicate);

			BuildExpression(precedence, predicate.Expr1);
			StringBuilder.Append(predicate.IsNot ? " NOT LIKE " : " LIKE ");
			BuildExpression(precedence, predicate.Expr2);

			if (predicate.Escape != null)
			{
				StringBuilder.Append(" ESCAPE ");
				BuildExpression(predicate.Escape);
			}
		}

		#endregion

		#region BuildExpression

		protected virtual StringBuilder BuildExpression(
			ISqlExpression expr,
			bool           buildTableName,
			bool           checkParentheses,
			string?        alias,
			ref bool       addAlias,
			bool           throwExceptionIfTableNotFound = true)
		{
			// TODO: check the necessity.
			//
			expr = SqlOptimizer.ConvertExpression(expr);

			switch (expr.ElementType)
			{
				case QueryElementType.SqlField:
					{
						var field = (SqlField)expr;

						if (buildTableName && field.Table != null)
						{
							var ts = field.Table.SqlTableType == SqlTableType.SystemTable
								? field.Table
								: Statement.SelectQuery?.GetTableSource(field.Table);

							if (ts == null)
							{
								SqlStatement? current = Statement;
								do
								{
									ts = current.GetTableSource(field.Table);
									if (ts != null)
										break;
									current = current.ParentStatement;
								} while (current != null);
							}

							if (ts == null)
							{
								if (field != field.Table.All)
								{
#if DEBUG
									//SqlQuery.GetTableSource(field.Table);
#endif

									if (throwExceptionIfTableNotFound)
										throw new SqlException("Table '{0}' not found.", field.Table);
								}
							}
							else
							{
								var table = GetTableAlias(ts);
								var len = StringBuilder.Length;

								if (table == null)
									StringBuilder.Append(GetPhysicalTableName(field.Table, null, true));
								else
									Convert(StringBuilder, table, ConvertType.NameToQueryTableAlias);

								if (len == StringBuilder.Length)
									throw new SqlException("Table {0} should have an alias.", field.Table);

								addAlias = alias != field.PhysicalName;

								StringBuilder
									.Append('.');
							}
						}

						if (field == field.Table?.All)
							StringBuilder.Append("*");
						else
							Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
					}

					break;

				case QueryElementType.Column:
					{
						var column = (SqlColumn)expr;

#if DEBUG
						var sql = Statement.SqlText;
#endif

						ISqlTableSource? table;
						SqlStatement?    currentStatement = Statement;
						do
						{
							table = currentStatement.GetTableSource(column.Parent!);
							if (table != null)
								break;
							currentStatement = currentStatement.ParentStatement;
						} while (currentStatement != null);

						if (table == null)
						{
#if DEBUG
							table = Statement.GetTableSource(column.Parent!);
#endif

							throw new SqlException("Table not found for '{0}'.", column);
						}

						var tableAlias = GetTableAlias(table) ?? GetPhysicalTableName(column.Parent!, null, true);

						if (string.IsNullOrEmpty(tableAlias))
							throw new SqlException("Table {0} should have an alias.", column.Parent);

						addAlias = alias != column.Alias;

						Convert(StringBuilder, tableAlias, ConvertType.NameToQueryTableAlias);
						StringBuilder.Append('.');
						Convert(StringBuilder, column.Alias!, ConvertType.NameToQueryField);
					}

					break;

				case QueryElementType.SqlQuery:
					{
						var hasParentheses = checkParentheses && StringBuilder[StringBuilder.Length - 1] == '(';

						if (!hasParentheses)
							StringBuilder.Append("(");
						StringBuilder.AppendLine();

						BuildSqlBuilder((SelectQuery)expr, Indent + 1, BuildStep != Step.FromClause);

						AppendIndent();

						if (!hasParentheses)
							StringBuilder.Append(")");
					}

					break;

				case QueryElementType.SqlValue:
					var sqlval = (SqlValue)expr;
					var dt     = new SqlDataType(sqlval.ValueType);

					BuildValue(dt, sqlval.Value);
					break;

				case QueryElementType.SqlExpression:
					{
						var e = (SqlExpression)expr;

						BuildFormatValues(e.Expr, e.Parameters, () => GetPrecedence(e));
					}

					break;

				case QueryElementType.SqlBinaryExpression:
					BuildBinaryExpression((SqlBinaryExpression)expr);
					break;

				case QueryElementType.SqlFunction:
					BuildFunction((SqlFunction)expr);
					break;

				case QueryElementType.SqlParameter:
					{
						var parm = (SqlParameter)expr;

						if (parm.IsQueryParameter)
						{
							Convert(StringBuilder, parm.Name!, ConvertType.NameToQueryParameter);
							AddParameter(parm);
						}
						else
						{
							BuildValue(new SqlDataType(parm.Type), parm.Value);
						}
					}

					break;

				case QueryElementType.SqlDataType:
					BuildDataType((SqlDataType)expr, false);
					break;

				case QueryElementType.SearchCondition:
					BuildSearchCondition(expr.Precedence, (SqlSearchCondition)expr);
					break;

				case QueryElementType.SqlTable:
				case QueryElementType.SqlRawSqlTable:
				case QueryElementType.TableSource:
					{
						var table = (ISqlTableSource) expr;
						var tableAlias = GetTableAlias(table) ?? GetPhysicalTableName(table, null, true);
						StringBuilder.Append(tableAlias);
					}

					break;

				case QueryElementType.GroupingSet:
					{
						var groupingSet = (SqlGroupingSet) expr;
						StringBuilder.Append("(");
						for (var index = 0; index < groupingSet.Items.Count; index++)
						{
							var setItem = groupingSet.Items[index];
							BuildExpression(setItem, buildTableName, checkParentheses, throwExceptionIfTableNotFound);
							if (index < groupingSet.Items.Count - 1)
								StringBuilder.Append(", ");
						}

						StringBuilder.Append(")");
					}

					break;

				default:
					throw new InvalidOperationException($"Unexpected expression type {expr.ElementType}");
			}

			return StringBuilder;
		}

		void BuildFormatValues(string format, IReadOnlyList<ISqlExpression>? parameters, Func<int> getPrecedence)
		{
			if (parameters == null || parameters.Count == 0)
				StringBuilder.Append(format);
			else
			{
				StringBuilder s = new StringBuilder();
				var values = new object[parameters.Count];

				for (var i = 0; i < values.Length; i++)
				{
					var value = parameters[i];

					s.Length = 0;
					WithStringBuilder(s, () => BuildExpression(getPrecedence(), value));
					values[i] = s.ToString();
				}

				StringBuilder.AppendFormat(format, values);
			}
		}

		string IdentText(string text, int ident)
		{
			if (text.IsNullOrEmpty())
				return text;

			text = text.Replace("\r", "");
			
			var strArray = text.Split('\n'); 
			var sb = new StringBuilder();
			for (var i = 0; i < strArray.Length; i++)
			{
				var s = strArray[i];
				sb.Append('\t', ident).Append(s);
				if (i < strArray.Length - 1)
					sb.AppendLine();
			}

			return sb.ToString();
		}

		void BuildExpression(int parentPrecedence, ISqlExpression expr, string? alias, ref bool addAlias)
		{
			var wrap = Wrap(GetPrecedence(expr), parentPrecedence);

			if (wrap) StringBuilder.Append('(');
			BuildExpression(expr, true, true, alias, ref addAlias);
			if (wrap) StringBuilder.Append(')');
		}

		protected StringBuilder BuildExpression(ISqlExpression expr)
		{
			var dummy = false;
			return BuildExpression(expr, true, true, null, ref dummy);
		}

		protected void BuildExpression(ISqlExpression expr, bool buildTableName, bool checkParentheses, bool throwExceptionIfTableNotFound = true)
		{
			var dummy = false;
			BuildExpression(expr, buildTableName, checkParentheses, null, ref dummy, throwExceptionIfTableNotFound);
		}

		protected void BuildExpression(int precedence, ISqlExpression expr)
		{
			var dummy = false;
			BuildExpression(precedence, expr, null, ref dummy);
		}

		protected virtual void BuildTypedExpression(SqlDataType dataType, ISqlExpression value)
		{
			StringBuilder.Append("CAST(");
			BuildExpression(value);
			StringBuilder.Append(" AS ");
			BuildDataType(dataType, false);
			StringBuilder.Append(")");
		}

		#endregion

		#region BuildValue

		protected void BuildValue(SqlDataType? dataType, object? value)
		{
			if (dataType != null)
				ValueToSqlConverter.Convert(StringBuilder, dataType, value);
			else
				ValueToSqlConverter.Convert(StringBuilder, value);
		}

		#endregion

		#region BuildBinaryExpression

		protected virtual void BuildBinaryExpression(SqlBinaryExpression expr)
		{
			BuildBinaryExpression(expr.Operation, expr);
		}

		void BuildBinaryExpression(string op, SqlBinaryExpression expr)
		{
			if (expr.Operation == "*" && expr.Expr1 is SqlValue value)
			{
				if (value.Value is int i && i == -1)
				{
					StringBuilder.Append('-');
					BuildExpression(GetPrecedence(expr), expr.Expr2);
					return;
				}
			}

			BuildExpression(GetPrecedence(expr), expr.Expr1);
			StringBuilder.Append(' ').Append(op).Append(' ');
			BuildExpression(GetPrecedence(expr), expr.Expr2);
		}

		#endregion

		#region BuildFunction

		protected virtual void BuildFunction(SqlFunction func)
		{
			if (func.Name == "CASE")
			{
				StringBuilder.Append(func.Name).AppendLine();

				Indent++;

				var i = 0;

				for (; i < func.Parameters.Length - 1; i += 2)
				{
					AppendIndent().Append("WHEN ");

					var len = StringBuilder.Length;

					BuildExpression(func.Parameters[i]);

					if (SqlExpression.NeedsEqual(func.Parameters[i]))
					{
						StringBuilder.Append(" = ");
						BuildValue(null, true);
					}

					if (StringBuilder.Length - len > 20)
					{
						StringBuilder.AppendLine();
						AppendIndent().Append("\tTHEN ");
					}
					else
						StringBuilder.Append(" THEN ");

					BuildExpression(func.Parameters[i + 1]);
					StringBuilder.AppendLine();
				}

				if (i < func.Parameters.Length)
				{
					AppendIndent().Append("ELSE ");
					BuildExpression(func.Parameters[i]);
					StringBuilder.AppendLine();
				}

				Indent--;

				AppendIndent().Append("END");
			}
			else
				BuildFunction(func.Name, func.Parameters);
		}

		void BuildFunction(string name, ISqlExpression[] exprs)
		{
			StringBuilder.Append(name).Append('(');

			var first = true;

			foreach (var parameter in exprs)
			{
				if (!first)
					StringBuilder.Append(", ");

				BuildExpression(parameter, true, !first || name == "EXISTS");

				first = false;
			}

			StringBuilder.Append(')');
		}

		#endregion

		#region BuildDataType
		protected void BuildDataType(SqlDataType type, bool forCreateTable)
		{
			if (!string.IsNullOrEmpty(type.Type.DbType))
				StringBuilder.Append(type.Type.DbType);
			else
			{
				if (type.Type.DataType == DataType.Undefined)
					type = MappingSchema.GetDataType(type.Type.SystemType);

				if (type.Type.DataType == DataType.Undefined)
					// give some hint to user that it is expected situation and he need to fix something on his side
					throw new LinqToDBException("Database type cannot be determined automatically and must be specified explicitly");

				BuildDataTypeFromDataType(type, forCreateTable);
			}
		}

		protected virtual void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.Double : StringBuilder.Append("Float");    return;
				case DataType.Single : StringBuilder.Append("Real");     return;
				case DataType.SByte  : StringBuilder.Append("TinyInt");  return;
				case DataType.UInt16 : StringBuilder.Append("Int");      return;
				case DataType.UInt32 : StringBuilder.Append("BigInt");   return;
				case DataType.UInt64 : StringBuilder.Append("Decimal");  return;
				case DataType.Byte   : StringBuilder.Append("TinyInt");  return;
				case DataType.Int16  : StringBuilder.Append("SmallInt"); return;
				case DataType.Int32  : StringBuilder.Append("Int");      return;
				case DataType.Int64  : StringBuilder.Append("BigInt");   return;
				case DataType.Boolean: StringBuilder.Append("Bit");      return;
			}

			StringBuilder.Append(type.Type.DataType);

			if (type.Type.Length > 0)
				StringBuilder.Append('(').Append(type.Type.Length).Append(')');

			if (type.Type.Precision > 0)
				StringBuilder.Append('(').Append(type.Type.Precision).Append(',').Append(type.Type.Scale).Append(')');
		}

		#endregion

		#region GetPrecedence

		static int GetPrecedence(ISqlExpression expr)
		{
			return expr.Precedence;
		}

		protected static int GetPrecedence(ISqlPredicate predicate)
		{
			return predicate.Precedence;
		}

		#endregion

		#endregion

		#region Internal Types

		protected enum Step
		{
			WithClause,
			SelectClause,
			DeleteClause,
			UpdateClause,
			InsertClause,
			FromClause,
			WhereClause,
			GroupByClause,
			HavingClause,
			OrderByClause,
			OffsetLimit
		}

		#endregion

		#region Alternative Builders

		void BuildAliases(string table, List<SqlColumn> columns, string? postfix)
		{
			Indent++;

			var first = true;

			foreach (var col in columns)
			{
				if (!first)
					StringBuilder.Append(',').AppendLine();
				first = false;

				AppendIndent()
					.Append(table)
					.Append('.');
				Convert(StringBuilder, col.Alias!, ConvertType.NameToQueryFieldAlias);

				if (postfix != null)
					StringBuilder.Append(postfix);
			}

			Indent--;

			StringBuilder.AppendLine();
		}

		protected void AlternativeBuildSql2(Action buildSql)
		{
			var selectQuery = Statement.SelectQuery;
			if (selectQuery == null)
				return;

			var aliases = GetTempAliases(3, "t");

			AppendIndent().Append("SELECT *").AppendLine();
			AppendIndent().Append("FROM").    AppendLine();
			AppendIndent().Append("(").       AppendLine();
			Indent++;

			AppendIndent().Append("SELECT TOP ");
			BuildExpression(selectQuery.Select.TakeValue!);
			StringBuilder.Append(" *").   AppendLine();
			AppendIndent().Append("FROM").AppendLine();
			AppendIndent().Append("(").   AppendLine();
			Indent++;

			if (selectQuery.OrderBy.IsEmpty)
			{
				AppendIndent().Append("SELECT TOP ");

				if (selectQuery.Select.SkipValue is SqlParameter p &&
					!p.IsQueryParameter &&
					selectQuery.Select.TakeValue is SqlValue v)
					BuildValue(null, (int)p.Value! + (int)v.Value!);
				else
					BuildExpression(Add<int>(selectQuery.Select.SkipValue!, selectQuery.Select.TakeValue!));

				StringBuilder.Append(" *").   AppendLine();
				AppendIndent().Append("FROM").AppendLine();
				AppendIndent().Append("(").   AppendLine();
				Indent++;
			}

			buildSql();

			if (selectQuery.OrderBy.IsEmpty)
			{
				Indent--;
				AppendIndent().AppendFormat(") {0}", aliases[2]).AppendLine();
				AppendIndent().Append("ORDER BY").               AppendLine();
				BuildAliases(aliases[2], selectQuery.Select.Columns, null);
			}

			Indent--;
			AppendIndent().AppendFormat(") {0}", aliases[1]).AppendLine();

			if (selectQuery.OrderBy.IsEmpty)
			{
				AppendIndent().Append("ORDER BY").AppendLine();
				BuildAliases(aliases[1], selectQuery.Select.Columns, " DESC");
			}
			else
			{
				BuildAlternativeOrderBy(false);
			}

			Indent--;
			AppendIndent().AppendFormat(") {0}", aliases[0]).AppendLine();

			if (selectQuery.OrderBy.IsEmpty)
			{
				AppendIndent().Append("ORDER BY").AppendLine();
				BuildAliases(aliases[0], selectQuery.Select.Columns, null);
			}
			else
			{
				BuildAlternativeOrderBy(true);
			}
		}

		void BuildAlternativeOrderBy(bool ascending)
		{
			var selectQuery = Statement.SelectQuery;
			if (selectQuery == null)
				return;

			SkipAlias = false;

			AppendIndent().Append("ORDER BY").AppendLine();

			var obys = GetTempAliases(selectQuery.OrderBy.Items.Count, "oby");

			Indent++;

			for (var i = 0; i < obys.Length; i++)
			{
				AppendIndent().Append(obys[i]);

				if ( ascending &&  selectQuery.OrderBy.Items[i].IsDescending ||
					!ascending && !selectQuery.OrderBy.Items[i].IsDescending)
					StringBuilder.Append(" DESC");

				if (i + 1 < obys.Length)
					StringBuilder.Append(',');

				StringBuilder.AppendLine();
			}

			Indent--;
		}

		protected delegate IEnumerable<SqlColumn> ColumnSelector();

		protected IEnumerable<SqlColumn> AlternativeGetSelectedColumns(SelectQuery selectQuery, ColumnSelector columnSelector)
		{
			foreach (var col in columnSelector())
				yield return col;

			SkipAlias = false;

			var obys = GetTempAliases(selectQuery.OrderBy.Items.Count, "oby");

			for (var i = 0; i < obys.Length; i++)
				yield return new SqlColumn(selectQuery, selectQuery.OrderBy.Items[i].Expression, obys[i]);
		}

		protected static bool IsDateDataType(ISqlExpression expr, string dateName)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlDataType  : return ((SqlDataType)expr).Type.DataType == DataType.Date;
				case QueryElementType.SqlExpression: return ((SqlExpression)expr).Expr     == dateName;
			}

			return false;
		}

		protected static bool IsTimeDataType(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlDataType  : return ((SqlDataType)expr).Type.DataType == DataType.Time;
				case QueryElementType.SqlExpression: return ((SqlExpression)expr).Expr     == "Time";
			}

			return false;
		}

		static bool IsBooleanParameter(ISqlExpression expr, int count, int i)
		{
			if ((i % 2 == 1 || i == count - 1) && expr.SystemType == typeof(bool) || expr.SystemType == typeof(bool?))
			{
				switch (expr.ElementType)
				{
					case QueryElementType.SearchCondition: return true;
				}
			}

			return false;
		}

		protected SqlFunction ConvertFunctionParameters(SqlFunction func)
		{
			if (func.Name == "CASE" &&
				func.Parameters.Select((p, i) => new { p, i }).Any(p => IsBooleanParameter(p.p, func.Parameters.Length, p.i)))
			{
				return new SqlFunction(
					func.SystemType,
					func.Name,
					false,
					func.Precedence,
					func.Parameters.Select((p, i) =>
						IsBooleanParameter(p, func.Parameters.Length, i) ?
							SqlOptimizer.ConvertExpression(new SqlFunction(typeof(bool), "CASE", p, new SqlValue(true), new SqlValue(false))) :
							p
					).ToArray());
			}

			return func;
		}

		#endregion

		#region Helpers

		protected SequenceNameAttribute? GetSequenceNameAttribute(SqlTable table, bool throwException)
		{
			var identityField = table.GetIdentityField();

			if (identityField == null)
				if (throwException)
					throw new SqlException("Identity field must be defined for '{0}'.", table.Name);
				else
					return null;

			if (table.ObjectType == null)
				if (throwException)
					throw new SqlException("Sequence name can not be retrieved for the '{0}' table.", table.Name);
				else
					return null;

			var attrs = table.SequenceAttributes;

			if (attrs.IsNullOrEmpty())
				if (throwException)
					throw new SqlException("Sequence name can not be retrieved for the '{0}' table.", table.Name);
				else
					return null;

			SequenceNameAttribute? defaultAttr = null;

			foreach (var attr in attrs)
			{
				if (attr.Configuration == Name)
					return attr;

				if (defaultAttr == null && attr.Configuration == null)
					defaultAttr = attr;
			}

			if (defaultAttr == null)
				if (throwException)
					throw new SqlException("Sequence name can not be retrieved for the '{0}' table.", table.Name);
				else
					return null;

			return defaultAttr;
		}

		static bool Wrap(int precedence, int parentPrecedence)
		{
			return
				precedence == 0 ||
				/* maybe it will be no harm to put "<=" here? */
				precedence < parentPrecedence ||
				(precedence == parentPrecedence &&
					(parentPrecedence == Precedence.Subtraction ||
					 parentPrecedence == Precedence.Multiplicative ||
					 parentPrecedence == Precedence.LogicalNegation));
		}

		protected string[] GetTempAliases(int n, string defaultAlias)
		{
			return Statement?.GetTempAliases(n, defaultAlias) ?? Array<string>.Empty;
		}

		protected string? GetTableAlias(ISqlTableSource table)
		{
			switch (table.ElementType)
			{
				case QueryElementType.TableSource:
					{
						var ts    = (SqlTableSource)table;
						var alias = string.IsNullOrEmpty(ts.Alias) ? GetTableAlias(ts.Source) : ts.Alias;
						return alias != "$" && alias != "$F" ? alias : null;
					}
				case QueryElementType.SqlTable        :
				case QueryElementType.SqlCteTable     :
					{
						var alias = ((SqlTable)table).Alias;
						return alias != "$" && alias != "$F" ? alias : null;
					}	
				case QueryElementType.SqlRawSqlTable  :
					{
						var ts = Statement.SelectQuery?.GetTableSource(table);
						if (ts == null)
							ts = Statement.GetTableSource(table);

						if (ts != null)
							return GetTableAlias(ts);
						else
						{
						var alias = ((SqlTable)table).Alias;
						return alias != "$" && alias != "$F" ? alias : null;
					}	
					}
				case QueryElementType.MergeSourceTable:
					return null;

				default:
					throw new InvalidOperationException($"Unexpected table type {table.ElementType}");
			}
		}

		protected virtual string? GetTableServerName(SqlTable table)
		{
			return table.Server == null ? null : ConvertInline(table.Server, ConvertType.NameToServer);
		}

		protected virtual string? GetTableDatabaseName(SqlTable table)
		{
			return table.Database == null ? null : ConvertInline(table.Database, ConvertType.NameToDatabase);
		}

		protected virtual string? GetTableSchemaName(SqlTable table)
		{
			return table.Schema == null ? null : ConvertInline(table.Schema, ConvertType.NameToSchema);
		}

		protected virtual string? GetTablePhysicalName(SqlTable table)
		{
			return table.PhysicalName == null ? null : ConvertInline(table.PhysicalName, ConvertType.NameToQueryTable);
		}

		string GetPhysicalTableName(ISqlTableSource table, string? alias, bool ignoreTableExpression = false)
		{
			switch (table.ElementType)
			{
				case QueryElementType.SqlTable:
					{
						var tbl = (SqlTable)table;

						var server       = GetTableServerName  (tbl);
						var database     = GetTableDatabaseName(tbl);
						var schema       = GetTableSchemaName  (tbl);
						var physicalName = GetTablePhysicalName(tbl)!;

						var sb = new StringBuilder();

						BuildTableName(sb, server, database, schema, physicalName);

						if (!ignoreTableExpression && tbl.SqlTableType == SqlTableType.Expression)
						{
							var values = new object[2 + (tbl.TableArguments?.Length ?? 0)];

							values[0] = sb.ToString();

							if (alias != null)
								values[1] = ConvertInline(alias, ConvertType.NameToQueryTableAlias);
							else
								values[1] = "";

							for (var i = 2; i < values.Length; i++)
							{
								var value = tbl.TableArguments![i - 2];

								sb.Length = 0;
								WithStringBuilder(sb, () => BuildExpression(Precedence.Primary, value));
								values[i] = sb.ToString();
							}

							sb.Length = 0;
							sb.AppendFormat(tbl.Name!, values);
						}

						if (tbl.SqlTableType == SqlTableType.Function)
						{
							sb.Append('(');

							if (tbl.TableArguments != null && tbl.TableArguments.Length > 0)
							{
								var first = true;

								foreach (var arg in tbl.TableArguments)
								{
									if (!first)
										sb.Append(", ");

									WithStringBuilder(sb, () => BuildExpression(arg, true, !first));

									first = false;
								}
							}

							sb.Append(')');
						}

						return sb.ToString();
					}

				case QueryElementType.TableSource:
					return GetPhysicalTableName(((SqlTableSource)table).Source, alias);

				case QueryElementType.SqlCteTable:
				case QueryElementType.SqlRawSqlTable:
					return GetTablePhysicalName((SqlTable)table)!;

				case QueryElementType.MergeSourceTable:
					return ConvertInline(((SqlMergeSourceTable)table).Name, ConvertType.NameToQueryTable);

				default:
					throw new InvalidOperationException($"Unexpected table type {table.ElementType}");
			}
		}

		protected StringBuilder AppendIndent()
		{
			if (Indent > 0)
				StringBuilder.Append('\t', Indent);

			return StringBuilder;
		}

		ISqlExpression Add(ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return SqlOptimizer.ConvertExpression(new SqlBinaryExpression(type, expr1, "+", expr2, Precedence.Additive));
		}

		protected ISqlExpression Add<T>(ISqlExpression expr1, ISqlExpression expr2)
		{
			return Add(expr1, expr2, typeof(T));
		}

		protected virtual bool IsReserved(string word)
		{
			return ReservedWords.IsReserved(word);
		}

		#endregion

		#region ISqlProvider Members

		public virtual ISqlExpression? GetIdentityExpression(SqlTable table)
		{
			return null;
		}

		protected virtual void PrintParameterName(StringBuilder sb, IDbDataParameter parameter)
		{
			if (!parameter.ParameterName.StartsWith("@"))
				sb.Append('@');
			sb.Append(parameter.ParameterName);
		}

		protected virtual string? GetTypeName(IDbDataParameter parameter)
		{
			return null;
		}

		protected virtual string? GetUdtTypeName(IDbDataParameter parameter)
		{
			return null;
		}

		protected virtual string? GetProviderTypeName(IDbDataParameter parameter)
		{
			switch (parameter.DbType)
			{
				case DbType.AnsiString           : return "VarChar";
				case DbType.AnsiStringFixedLength: return "Char";
				case DbType.String               : return "NVarChar";
				case DbType.StringFixedLength    : return "NChar";
				case DbType.Decimal              : return "Decimal";
				case DbType.Binary               : return "Binary";
			}

			return null;
		}

		protected virtual void PrintParameterType(StringBuilder sb, IDbDataParameter parameter)
		{
			var typeName = GetTypeName(parameter);
			if (!string.IsNullOrEmpty(typeName))
				sb.Append(typeName).Append(" -- ");

			var udtTypeName = GetUdtTypeName(parameter);
			if (!string.IsNullOrEmpty(udtTypeName))
				sb.Append(udtTypeName).Append(" -- ");

			var t1 = GetProviderTypeName(parameter);
			var t2 = parameter.DbType.ToString();

			sb.Append(t1);

			if (t1 != null)
			{
				if (parameter.Size > 0)
				{
					if (t1.IndexOf('(') < 0)
						sb.Append('(').Append(parameter.Size).Append(')');
				}
				else if (parameter.Precision > 0)
				{
					if (t1.IndexOf('(') < 0)
						sb.Append('(').Append(parameter.Precision).Append(',').Append(parameter.Scale).Append(')');
				}
				else
				{
					switch (parameter.DbType)
					{
						case DbType.AnsiString           :
						case DbType.AnsiStringFixedLength:
						case DbType.String               :
						case DbType.StringFixedLength    :
							{
								var value = parameter.Value as string;

								if (!string.IsNullOrEmpty(value))
									sb.Append('(').Append(value!.Length).Append(')');

								break;
							}
						case DbType.Decimal:
							{
								var value = parameter.Value;

								if (value is decimal dec)
								{
									var d = new SqlDecimal(dec);
									sb.Append('(').Append(d.Precision).Append(',').Append(d.Scale).Append(')');
								}

								break;
							}
						case DbType.Binary:
							{
								if (parameter.Value is byte[] value)
									sb.Append('(').Append(value.Length).Append(')');

								break;
							}
					}
				}
			}

			if (t1 != t2)
				sb.Append(" -- ").Append(t2);
		}

		public virtual StringBuilder PrintParameters(StringBuilder sb, IEnumerable<IDbDataParameter>? parameters)
		{
			if (parameters != null)
			{
				foreach (var p in parameters)
				{
					sb.Append("DECLARE ");
					PrintParameterName(sb, p);
					sb.Append(' ');
					PrintParameterType(sb, p);
					sb.AppendLine();

					sb.Append("SET     ");
					PrintParameterName(sb, p);
					sb.Append(" = ");
					if (!ValueToSqlConverter.TryConvert(sb, p.Value))
						sb.Append(p.Value);
					sb.AppendLine();
				}

				sb.AppendLine();
			}

			return sb;
		}

		public string ApplyQueryHints(string sql, List<string> queryHints)
		{
			var sb = new StringBuilder();

			foreach (var hint in queryHints)
				if (hint?.Length >= 2 && hint.StartsWith("**"))
					sb.AppendLine(hint.Substring(2));

			sb.Append(sql);

			foreach (var hint in queryHints)
				if (!(hint?.Length >= 2 && hint.StartsWith("**")))
					sb.AppendLine(hint);

			return sb.ToString();
		}

		public virtual string GetReserveSequenceValuesSql(int count, string sequenceName)
		{
			throw new NotImplementedException();
		}

		public virtual string GetMaxValueSql(EntityDescriptor entity, ColumnDescriptor column)
		{
			var server   = entity.ServerName;
			var database = entity.DatabaseName;
			var schema   = entity.SchemaName;
			var table    = entity.TableName;

			var columnName = ConvertInline(column.ColumnName, ConvertType.NameToQueryField);
			var tableName  = BuildTableName(
				new StringBuilder(),
				server   == null ? null : ConvertInline(server,   ConvertType.NameToServer),
				database == null ? null : ConvertInline(database, ConvertType.NameToDatabase),
				schema   == null ? null : ConvertInline(schema,   ConvertType.NameToSchema),
										  ConvertInline(table,    ConvertType.NameToQueryTable))
			.ToString();

			return $"SELECT Max({columnName}) FROM {tableName}";
		}

		private string? _name;

		public virtual string Name => _name ?? (_name = GetType().Name.Replace("SqlBuilder", ""));

		#endregion
	}
}
