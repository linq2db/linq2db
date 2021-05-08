﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;
	using SqlProvider;
	using Mapping;

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

		protected override string? FirstFormat(SelectQuery selectQuery)
		{
			return selectQuery.Select.SkipValue == null ? "TOP ({0})" : null;
		}

		StringBuilder AppendOutputTableVariable(SqlTable table)
		{
			StringBuilder.Append('@').Append(table.PhysicalName).Append("Output");
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
					StringBuilder.Append(' ');
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
						StringBuilder.AppendLine(Comma);
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
						.AppendLine(OpenParens);

					++Indent;

					var firstColumn = true;
					foreach (var oi in output.OutputItems)
					{
						if (!firstColumn)
							StringBuilder.AppendLine(Comma);
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

		protected override void BuildDeleteClause(SqlDeleteStatement deleteStatement)
		{
			var table = deleteStatement.Table != null ?
				(deleteStatement.SelectQuery.From.FindTableSource(deleteStatement.Table) ?? deleteStatement.Table) :
				deleteStatement.SelectQuery.From.Tables[0];

			AppendIndent()
				.Append("DELETE");

			BuildSkipFirst(deleteStatement.SelectQuery);

			StringBuilder.Append(' ');
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

		public override string? GetTableDatabaseName(SqlTable table)
		{
			if (table.PhysicalName!.StartsWith("#") || table.TableOptions.IsTemporaryOptionSet())
				return null;

			return base.GetTableDatabaseName(table);
		}

		public override string? GetTablePhysicalName(SqlTable table)
		{
			if (table.PhysicalName == null)
				return null;

			var physicalName = table.PhysicalName.StartsWith("#") ? table.PhysicalName : GetName();

			string GetName()
			{
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
							return $"#{table.PhysicalName}";
						case TableOptions.IsGlobalTemporaryStructure                                                               :
						case TableOptions.IsGlobalTemporaryStructure | TableOptions.IsGlobalTemporaryData                          :
							return $"##{table.PhysicalName}";
						case var value :
							throw new InvalidOperationException($"Incompatible table options '{value}'");
					}
				}
				else
				{
					return table.PhysicalName;
				}
			}

			return Convert(new StringBuilder(), physicalName, ConvertType.NameToQueryTable).ToString();
		}

		public override StringBuilder BuildTableName(StringBuilder sb,
			string?      server,
			string?      database,
			string?      schema,
			string       table,
			TableOptions tableOptions)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			if (server   != null && server  .Length == 0) server   = null;
			if (database != null && database.Length == 0) database = null;
			if (schema   != null && schema.  Length == 0) schema   = null;

			if (server != null)
			{
				// all components required for linked-server syntax by SQL server
				if (database == null || schema == null)
					throw new LinqToDBException("You must specify both schema and database names explicitly for linked server query");

				sb.Append(server).Append('.').Append(database).Append('.').Append(schema).Append('.');
			}
			else if (database != null)
			{
				if (schema == null) sb.Append(database).Append("..");
				else sb.Append(database).Append('.').Append(schema).Append('.');
			}
			else if (schema != null) sb.Append(schema).Append('.');

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
			StringBuilder.Append(string.Join(InlineComma, fieldNames));
			StringBuilder.Append(')');
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			var table = dropTable.Table!;

			BuildTag(dropTable);

			if (dropTable.Table.TableOptions.HasDropIfExists())
			{
				var defaultDatabaseName =
					table.PhysicalName!.StartsWith("#") || table.TableOptions.IsTemporaryOptionSet() ?
						"[tempdb]" : null;

				StringBuilder.Append("IF (OBJECT_ID(N'");
				BuildPhysicalTable(table, alias: null, defaultDatabaseName: defaultDatabaseName);
				StringBuilder.AppendLine("', N'U') IS NOT NULL)");
				Indent++;
			}

			AppendIndent().Append("DROP TABLE ");
			BuildPhysicalTable(table, alias: null);

			if (dropTable.Table.TableOptions.HasDropIfExists())
				Indent--;
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
			if (truncateTable.ResetIdentity || truncateTable.Table!.IdentityFields.Count == 0)
				StringBuilder.Append("TRUNCATE TABLE ");
			else
				StringBuilder.Append("DELETE FROM ");
		}

		protected void BuildIdentityInsert(SqlTableSource table, bool enable)
		{
			StringBuilder.Append("SET IDENTITY_INSERT ");
			BuildTableName(table, true, false);
			StringBuilder.AppendLine(enable ? " ON" : " OFF");
		}

		protected override void BuildStartCreateTableStatement(SqlCreateTableStatement createTable)
		{
			if (createTable.StatementHeader == null && createTable.Table!.TableOptions.HasCreateIfNotExists())
			{
				var table = createTable.Table;

				var defaultDatabaseName =
					table.PhysicalName!.StartsWith("#") || table.TableOptions.IsTemporaryOptionSet() ?
						"[tempdb]" : null;

				StringBuilder.Append("IF (OBJECT_ID(N'");
				BuildPhysicalTable(table, null, defaultDatabaseName : defaultDatabaseName);
				StringBuilder.AppendLine("', N'U') IS NULL)");
				Indent++;
			}

			base.BuildStartCreateTableStatement(createTable);
		}

		protected override void BuildEndCreateTableStatement(SqlCreateTableStatement createTable)
		{
			base.BuildEndCreateTableStatement(createTable);

			if (createTable.StatementHeader == null && createTable.Table!.TableOptions.HasCreateIfNotExists())
			{
				Indent--;
			}
		}

		protected override void BuildIsDistinctPredicate(SqlPredicate.IsDistinct expr) => BuildIsDistinctPredicateFallback(expr);
	}
}
