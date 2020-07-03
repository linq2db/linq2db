using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;
	using SqlProvider;
	using LinqToDB.Mapping;

	abstract class SqlServerSqlBuilder : BasicSqlBuilder
	{
		protected readonly SqlServerDataProvider? Provider;

		protected SqlServerSqlBuilder(
			SqlServerDataProvider? provider,
			MappingSchema          mappingSchema,
			ISqlOptimizer          sqlOptimizer,
			SqlProviderFlags       sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
			Provider = provider;
		}

		protected virtual  bool BuildAlternativeSql => false;

		protected override string? FirstFormat(SelectQuery selectQuery)
		{
			return selectQuery.Select.SkipValue == null ? "TOP ({0})" : null;
		}

		protected override void BuildSql()
		{
			if (BuildAlternativeSql)
				AlternativeBuildSql(true, base.BuildSql, "\t(SELECT NULL)");
			else
				base.BuildSql();
		}

		StringBuilder AppendOutputTableVariable(SqlTable table)
		{
			StringBuilder.Append("@").Append(table.PhysicalName).Append("Output");
			return StringBuilder;
		}

		protected override void BuildInsertQuery(SqlStatement statement, SqlInsertClause insertClause, bool addAlias)
		{
			if (insertClause.WithIdentity)
			{
				var identityField = insertClause.Into!.GetIdentityField();

				if (identityField != null && (identityField.Type!.Value.DataType == DataType.Guid || SqlServerConfiguration.GenerateScopeIdentity == false))
				{
					AppendIndent()
						.Append("DECLARE ");
					AppendOutputTableVariable(insertClause.Into)
						.Append(" TABLE (");
					Convert(StringBuilder, identityField.PhysicalName, ConvertType.NameToQueryField);
					StringBuilder.Append(" ");
					BuildCreateTableFieldType(identityField);
					StringBuilder
							.AppendLine(")")
							.AppendLine();
				}
			}

			base.BuildInsertQuery(statement, insertClause, addAlias);
		}

		protected override void BuildOutputSubclause(SqlStatement statement, SqlInsertClause insertClause)
		{
			if (insertClause.WithIdentity)
			{
				var identityField = insertClause.Into!.GetIdentityField();

				if (identityField != null && (identityField.Type!.Value.DataType == DataType.Guid || SqlServerConfiguration.GenerateScopeIdentity == false))
				{
					StringBuilder
						.Append("OUTPUT [INSERTED].");
					Convert(StringBuilder, identityField.PhysicalName, ConvertType.NameToQueryField);
					StringBuilder.AppendLine();
					AppendIndent()
						.Append("INTO ");
					AppendOutputTableVariable(insertClause.Into)
						.AppendLine();
				}
			}
			else
			{
				var output = statement.GetOutputClause();
				BuildOutputSubclause(output);
			}
		}

		private void BuildOutputSubclause(SqlOutputClause? output)
		{
			if (output != null && output.HasOutputItems)
			{
				AppendIndent()
					.AppendLine("OUTPUT");

				if (output.InsertedTable != null)
					output.InsertedTable.PhysicalName = "INSERTED";

				if (output.DeletedTable != null)
					output.DeletedTable.PhysicalName = "DELETED";

				++Indent;

				bool first = true;
				foreach (var oi in output.OutputItems)
				{
					if (!first)
						StringBuilder.Append(',').AppendLine();
					first = false;

					AppendIndent();

					BuildExpression(oi.Expression!);
				}

				if (output.OutputItems.Count > 0)
				{
					StringBuilder
						.AppendLine();
				}

				--Indent;

				if (output.OutputQuery != null)
				{
					BuildColumns(output.OutputQuery);
				}

				if (output.OutputTable != null)
				{
					AppendIndent()
						.Append("INTO ")
						.Append(GetTablePhysicalName(output.OutputTable))
						.AppendLine();

					AppendIndent()
						.AppendLine("(");

					++Indent;

					var firstColumn = true;
					foreach (var oi in output.OutputItems)
					{
						if (!firstColumn)
							StringBuilder.Append(',').AppendLine();
						firstColumn = false;

						AppendIndent();

						BuildExpression(oi.Column, false, true);
					}

					StringBuilder
						.AppendLine();

					--Indent;

					AppendIndent()
						.AppendLine(")");
				}
			}
		}

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			var identityField = insertClause.Into!.GetIdentityField();

			if (identityField != null && (identityField.Type!.Value.DataType == DataType.Guid || SqlServerConfiguration.GenerateScopeIdentity == false))
			{
				StringBuilder
					.AppendLine();
				AppendIndent()
					.Append("SELECT ");
				Convert(StringBuilder, identityField.PhysicalName, ConvertType.NameToQueryField);
				StringBuilder.Append(" FROM ");
				AppendOutputTableVariable(insertClause.Into)
					.AppendLine();
			}
			else
			{
				StringBuilder
					.AppendLine()
					.AppendLine("SELECT SCOPE_IDENTITY()");
			}
		}

		protected override void BuildOrderByClause(SelectQuery selectQuery)
		{
			if (!BuildAlternativeSql || !NeedSkip(selectQuery))
				base.BuildOrderByClause(selectQuery);
		}

		protected override IEnumerable<SqlColumn> GetSelectedColumns(SelectQuery selectQuery)
		{
			if (BuildAlternativeSql && NeedSkip(selectQuery) && !selectQuery.OrderBy.IsEmpty)
				return AlternativeGetSelectedColumns(selectQuery, () => base.GetSelectedColumns(selectQuery));
			return base.GetSelectedColumns(selectQuery);
		}

		protected override void BuildDeleteClause(SqlDeleteStatement deleteStatement)
		{
			var table = deleteStatement.Table != null ?
				(deleteStatement.SelectQuery.From.FindTableSource(deleteStatement.Table) ?? deleteStatement.Table) :
				deleteStatement.SelectQuery.From.Tables[0];

			AppendIndent()
				.Append("DELETE");

			BuildSkipFirst(deleteStatement.SelectQuery);

			StringBuilder.Append(" ");
			Convert(StringBuilder, GetTableAlias(table)!, ConvertType.NameToQueryTableAlias);
			StringBuilder.AppendLine();

			BuildOutputSubclause(deleteStatement);
		}

		protected virtual void BuildOutputSubclause(SqlDeleteStatement deleteStatement)
		{
			var output = deleteStatement.GetOutputClause();
			BuildOutputSubclause(output);
		}

		protected override void BuildUpdateTableName(SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			var table = updateClause.Table != null ?
				(selectQuery.From.FindTableSource(updateClause.Table) ?? updateClause.Table) :
				selectQuery.From.Tables[0];

			if (table is SqlTable)
				BuildPhysicalTable(table, null);
			else
				Convert(StringBuilder, GetTableAlias(table)!, ConvertType.NameToQueryTableAlias);
		}

		protected override void BuildColumnExpression(SelectQuery? selectQuery, ISqlExpression expr, string? alias, ref bool addAlias)
		{
			var wrap = false;

			if (expr.SystemType == typeof(bool))
			{
				if (expr is SqlSearchCondition)
					wrap = true;
				else
				{
					var ex = expr as SqlExpression;
					wrap = ex != null && ex.Expr == "{0}" && ex.Parameters.Length == 1 && ex.Parameters[0] is SqlSearchCondition;
				}
			}

			if (wrap) StringBuilder.Append("CASE WHEN ");
			base.BuildColumnExpression(selectQuery, expr, alias, ref addAlias);
			if (wrap) StringBuilder.Append(" THEN 1 ELSE 0 END");
		}

		protected override void BuildLikePredicate(SqlPredicate.Like predicate)
		{
			if (predicate.Expr2 is SqlValue sqlValue)
			{
				var value = sqlValue.Value;

				if (value != null)
				{
					var text  = value.ToString()!;
					var ntext = predicate.IsSqlLike ? text :  DataTools.EscapeUnterminatedBracket(text);

					if (text != ntext)
						predicate = new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, new SqlValue(ntext), predicate.Escape, predicate.IsSqlLike);
				}
			}
			else if (predicate.Expr2 is SqlParameter p)
				p.ReplaceLike = predicate.IsSqlLike != true;

			base.BuildLikePredicate(predicate);
		}

		public override StringBuilder BuildTableName(StringBuilder sb,
			string? server,
			string? database,
			string? schema,
			string table)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			if (server   != null && server  .Length == 0) server   = null;
			if (database != null && database.Length == 0) database = null;
			if (schema   != null && schema.  Length == 0) schema   = null;

			if(server != null)
			{
				// all components required for linked-server syntax by SQL server
				if (database == null || schema == null)
					throw new LinqToDBException("You must specify both schema and database names explicitly for linked server query");

				sb.Append(server).Append(".").Append(database).Append(".").Append(schema).Append(".");
			}
			else if(database != null)
			{
				if (schema == null) sb.Append(database).Append("..");
				else sb.Append(database).Append(".").Append(schema).Append(".");
			}
			else if (schema != null) sb.Append(schema).Append(".");

			return sb.Append(table);
		}

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

					return SqlServerTools.QuoteIdentifier(sb, value);

				case ConvertType.NameToServer:
				case ConvertType.NameToDatabase:
				case ConvertType.NameToSchema:
				case ConvertType.NameToQueryTable:
					if (value.Length > 0 && value[0] == '[')
						return sb.Append(value);

					return SqlServerTools.QuoteIdentifier(sb, value);

				case ConvertType.SprocParameterToName:
					return value.Length > 0 && value[0] == '@'
						? sb.Append(value.Substring(1))
						: sb.Append(value);
			}

			return sb.Append(value);
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsUpdateInsert(insertOrUpdate);
		}

		protected override void BuildCreateTableIdentityAttribute2(SqlField field)
		{
			StringBuilder.Append("IDENTITY");
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();

			if (!pkName.StartsWith("[PK_#"))
				StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(' ');

			StringBuilder.Append("PRIMARY KEY CLUSTERED (");
			StringBuilder.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
			StringBuilder.Append(")");
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			var table = dropTable.Table!;

			if (table.PhysicalName!.StartsWith("#"))
			{
				AppendIndent().Append("DROP TABLE ");
				BuildPhysicalTable(table, null);
				StringBuilder.AppendLine();
			}
			else
			{
				if (dropTable.IfExists)
				{
					StringBuilder.Append("IF (OBJECT_ID(N'");
					BuildPhysicalTable(table, null);
					StringBuilder.AppendLine("', N'U') IS NOT NULL)");
					Indent++;
				}

				AppendIndent().Append("DROP TABLE ");
				BuildPhysicalTable(table, null);

				if (dropTable.IfExists)
					Indent--;
			}
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.Guid      : StringBuilder.Append("UniqueIdentifier"); return;
				case DataType.Variant   : StringBuilder.Append("Sql_Variant");      return;
				case DataType.NVarChar  :
					if (type.Type.Length == null || type.Type.Length > 4000 || type.Type.Length < 1)
					{
						StringBuilder
							.Append(type.Type.DataType)
							.Append("(Max)");
						return;
					}

					break;

				case DataType.VarChar   :
				case DataType.VarBinary :
					if (type.Type.Length == null || type.Type.Length > 8000 || type.Type.Length < 1)
					{
						StringBuilder
							.Append(type.Type.DataType)
							.Append("(Max)");
						return;
					}

					break;

				case DataType.DateTime2:
				case DataType.DateTimeOffset:
				case DataType.Time:
					StringBuilder.Append(type.Type.DataType);
					// Default precision for all three types is 7.
					// For all other non-null values (including 0) precision must be specified.
					if (type.Type.Precision != null && type.Type.Precision != 7)
					{
						StringBuilder.Append('(').Append(type.Type.Precision).Append(')');
					}
					return;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable);
		}

		protected override string? GetTypeName(IDbDataParameter parameter)
		{
			if (Provider != null)
			{
				var param = Provider.TryGetProviderParameter(parameter, MappingSchema);
				if (param != null)
					return Provider.Adapter.GetTypeName(param);
			}

			return base.GetTypeName(parameter);
		}

		protected override string? GetUdtTypeName(IDbDataParameter parameter)
		{
			if (Provider != null)
			{
				var param = Provider.TryGetProviderParameter(parameter, MappingSchema);
				if (param != null)
					return Provider.Adapter.GetUdtTypeName(param);
			}

			return base.GetUdtTypeName(parameter);
		}

		protected override string? GetProviderTypeName(IDbDataParameter parameter)
		{
			if (Provider != null)
			{
				var param = Provider.TryGetProviderParameter(parameter, MappingSchema);
				if (param != null)
					return Provider.Adapter.GetDbType(param).ToString();
			}

			return base.GetProviderTypeName(parameter);
		}

		protected override void BuildTruncateTable(SqlTruncateTableStatement truncateTable)
		{
			if (truncateTable.ResetIdentity || truncateTable.Table!.Fields.Values.All(f => !f.IsIdentity))
				StringBuilder.Append("TRUNCATE TABLE ");
			else
				StringBuilder.Append("DELETE FROM ");
		}

		protected void BuildIdentityInsert(SqlTableSource table, bool enable)
		{
			StringBuilder.Append($"SET IDENTITY_INSERT ");
			BuildTableName(table, true, false);
			StringBuilder.AppendLine(enable ? " ON" : " OFF");
		}
	}
}
