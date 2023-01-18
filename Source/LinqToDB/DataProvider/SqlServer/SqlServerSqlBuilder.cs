using System;
using System.Data.Common;
using System.Text;

namespace LinqToDB.DataProvider.SqlServer
{
	using Common;
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	abstract class SqlServerSqlBuilder : BasicSqlBuilder<SqlServerOptions>
	{
		protected SqlServerSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected SqlServerSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override string? FirstFormat(SelectQuery selectQuery)
		{
			return selectQuery.Select.SkipValue == null ? "TOP ({0})" : null;
		}

		StringBuilder AppendOutputTableVariable(SqlTable table)
		{
			return Convert(StringBuilder, table.TableName.Name + "Output", ConvertType.NameToQueryParameter);
		}

		protected override void BuildInsertQuery(SqlStatement statement, SqlInsertClause insertClause, bool addAlias)
		{
			if (insertClause.WithIdentity)
			{
				var identityField = insertClause.Into!.GetIdentityField();

				if (identityField != null && (identityField.Type.DataType == DataType.Guid || ProviderOptions.GenerateScopeIdentity == false))
				{
					AppendIndent()
						.Append("DECLARE ");
					AppendOutputTableVariable(insertClause.Into)
						.Append(" TABLE (");
					Convert(StringBuilder, identityField.PhysicalName, ConvertType.NameToQueryField);
					StringBuilder
						.Append(' ');
					BuildCreateTableFieldType(identityField);
					StringBuilder
						.AppendLine(")")
						.AppendLine();
				}
			}

			base.BuildInsertQuery(statement, insertClause, addAlias);
		}

		protected override void BuildOutputSubclause(NullabilityContext nullability, SqlStatement statement, SqlInsertClause insertClause)
		{
			if (insertClause.WithIdentity)
			{
				var identityField = insertClause.Into!.GetIdentityField();

				if (identityField != null && (identityField.Type.DataType == DataType.Guid || ProviderOptions.GenerateScopeIdentity == false))
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
				BuildOutputSubclause(nullability, statement.GetOutputClause());
			}
		}

		protected override string OutputKeyword       => "OUTPUT";
		protected override string DeletedOutputTable  => "DELETED";
		protected override string InsertedOutputTable => "INSERTED";

		protected override void BuildGetIdentity(NullabilityContext nullability, SqlInsertClause insertClause)
		{
			var identityField = insertClause.Into!.GetIdentityField();

			if (identityField != null && (identityField.Type.DataType == DataType.Guid || ProviderOptions.GenerateScopeIdentity == false))
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

		protected override void BuildDeleteClause(NullabilityContext nullability, SqlDeleteStatement deleteStatement)
		{
			ISqlTableSource? table = null;

			if (deleteStatement.Table != null)
			{
				table = deleteStatement.SelectQuery.From.FindTableSource(deleteStatement.Table);
			}

			if (table == null)
				table = deleteStatement.SelectQuery.From.Tables[0];

			AppendIndent()
				.Append("DELETE");

			BuildSkipFirst(nullability, deleteStatement.SelectQuery);

			StringBuilder.Append(' ');

			var alias = GetTableAlias(table);
			if (alias == null)
			{
				throw new InvalidOperationException();
			}

			Convert(StringBuilder, alias, ConvertType.NameToQueryTableAlias);
			StringBuilder.AppendLine();
			BuildOutputSubclause(nullability, deleteStatement.GetOutputClause());
		}

		protected override void BuildOutputSubclause(NullabilityContext nullability, SqlOutputClause? output)
		{
			if (BuildStep == Step.Output)
			{
				return;
			}

			base.BuildOutputSubclause(nullability, output);
		}

		protected override void BuildUpdateClause(NullabilityContext nullability, SqlStatement statement, SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			base.BuildUpdateClause(nullability, statement, selectQuery, updateClause);
			BuildOutputSubclause(nullability, statement.GetOutputClause());
		}

		protected override void BuildUpdateTableName(NullabilityContext nullability, SelectQuery selectQuery,
			SqlUpdateClause                                             updateClause)
		{
			if (updateClause.TableSource != null)
				Convert(StringBuilder, GetTableAlias(updateClause.TableSource)!, ConvertType.NameToQueryTableAlias);
			else if (updateClause.Table != null)
				BuildPhysicalTable(nullability, updateClause.Table, null);
			else
				throw new InvalidOperationException();
		}

		private string GetTablePhysicalName(string tableName, TableOptions tableOptions)
		{
			if (tableName.StartsWith("#") || !tableOptions.IsTemporaryOptionSet())
				return tableName;

			switch (tableOptions & TableOptions.IsTemporaryOptionSet)
			{
				case TableOptions.IsTemporary                                                                              :
				case TableOptions.IsTemporary |                                          TableOptions.IsLocalTemporaryData :
				case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure                                     :
				case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData :
				case                                                                     TableOptions.IsLocalTemporaryData :
				case                            TableOptions.IsLocalTemporaryStructure                                     :
				case                            TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData :
					return $"#{tableName}";
				case TableOptions.IsGlobalTemporaryStructure                                                               :
				case TableOptions.IsGlobalTemporaryStructure | TableOptions.IsGlobalTemporaryData                          :
					return $"##{tableName}";
				case var value :
					throw new InvalidOperationException($"Incompatible table options '{value}'");
			}
		}

		public override StringBuilder BuildObjectName(StringBuilder sb, SqlObjectName name, ConvertType objectType, bool escape, TableOptions tableOptions)
		{
			var databaseName = name.Database;

			// remove database name, which could be inherited from non-temporary table mapping
			// except explicit use of tempdb, needed in some cases at least for sql server 2014
			if ((name.Name.StartsWith("#") || tableOptions.IsTemporaryOptionSet()) && databaseName != "tempdb")
				databaseName = "tempdb";

			if (name.Server != null && (databaseName == null || name.Schema == null))
				// all components required for linked-server syntax by SQL server
				throw new LinqToDBException("You must specify both schema and database names explicitly for linked server query");

			if (name.Server != null)
			{
				(escape ? Convert(sb, name.Server, ConvertType.NameToServer) : sb.Append(name.Server))
					.Append('.');
			}

			if (databaseName != null)
			{
				(escape ? Convert(sb, databaseName, ConvertType.NameToDatabase) : sb.Append(databaseName))
					.Append('.');
			}

			if (name.Schema != null)
				(escape ? Convert(sb, name.Schema, ConvertType.NameToSchema) : sb.Append(name.Schema)).Append('.');
			else if (databaseName != null)
				sb.Append('.');

			var tableName = GetTablePhysicalName(name.Name, tableOptions);
			return escape ? Convert(sb, tableName, objectType) : sb.Append(tableName);
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

					if (value == "$action")
						return sb.Append(value);

					return SqlServerTools.QuoteIdentifier(sb, value);

				case ConvertType.NameToServer    :
				case ConvertType.NameToDatabase  :
				case ConvertType.NameToSchema    :
				case ConvertType.NameToQueryTable:
				case ConvertType.NameToProcedure :
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

			if (!pkName.StartsWith("[PK_#") && !createTable.Table.TableOptions.IsTemporaryOptionSet())
			{
				StringBuilder.Append("CONSTRAINT ");
				Convert(StringBuilder, pkName, ConvertType.NameToQueryTable).Append(' ');
			}

			StringBuilder.Append("PRIMARY KEY CLUSTERED (");
			StringBuilder.Append(string.Join(InlineComma, fieldNames));
			StringBuilder.Append(')');
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			var nullability = NullabilityContext.NonQuery;

			var table = dropTable.Table!;

			BuildTag(dropTable);

			if (dropTable.Table.TableOptions.HasDropIfExists())
			{
				var defaultDatabaseName =
					table.TableName.Name.StartsWith("#") || table.TableOptions.IsTemporaryOptionSet() ?
						"tempdb" : null;

				StringBuilder.Append("IF (OBJECT_ID(N'");
				BuildPhysicalTable(nullability, table, alias: null, defaultDatabaseName: defaultDatabaseName);
				StringBuilder.AppendLine("', N'U') IS NOT NULL)");
				Indent++;
			}

			AppendIndent().Append("DROP TABLE ");
			BuildPhysicalTable(nullability, table, alias: null);

			if (dropTable.Table.TableOptions.HasDropIfExists())
				Indent--;
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.Type.DataType)
			{
				case DataType.Guid      : StringBuilder.Append("UniqueIdentifier"); return;
				case DataType.Variant   : StringBuilder.Append("Sql_Variant");      return;
				case DataType.NVarChar  :
					if (type.Type.Length is null or > 4000 or < 1)
					{
						StringBuilder
							.Append(type.Type.DataType)
							.Append("(Max)");
						return;
					}

					break;

				case DataType.VarChar   :
				case DataType.VarBinary :
					if (type.Type.Length is null or > 8000 or < 1)
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

			base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
		}

		protected override string? GetTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (DataProvider is SqlServerDataProvider provider)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
					return provider.Adapter.GetTypeName(param);
			}

			return base.GetTypeName(dataContext, parameter);
		}

		protected override string? GetUdtTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (DataProvider is SqlServerDataProvider provider)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
					return provider.Adapter.GetUdtTypeName(param);
			}

			return base.GetUdtTypeName(dataContext, parameter);
		}

		protected override string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (DataProvider is SqlServerDataProvider provider)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
					return provider.Adapter.GetDbType(param).ToString();
			}

			return base.GetProviderTypeName(dataContext, parameter);
		}

		protected override void BuildTruncateTable(SqlTruncateTableStatement truncateTable)
		{
			if (truncateTable.ResetIdentity || truncateTable.Table!.IdentityFields.Count == 0)
				StringBuilder.Append("TRUNCATE TABLE ");
			else
				StringBuilder.Append("DELETE FROM ");
		}

		protected void BuildIdentityInsert(NullabilityContext nullability, SqlTableSource table, bool enable)
		{
			StringBuilder.Append("SET IDENTITY_INSERT ");
			BuildTableName(nullability, table, true, false);
			StringBuilder.AppendLine(enable ? " ON" : " OFF");
		}

		protected override void BuildStartCreateTableStatement(SqlCreateTableStatement createTable)
		{
			var nullability = NullabilityContext.NonQuery;

			if (createTable.StatementHeader == null && createTable.Table!.TableOptions.HasCreateIfNotExists())
			{
				var table = createTable.Table;

				var defaultDatabaseName =
					table.TableName.Name.StartsWith("#") || table.TableOptions.IsTemporaryOptionSet() ?
						"tempdb" : null;

				StringBuilder.Append("IF (OBJECT_ID(N'");
				BuildPhysicalTable(nullability, table, null, defaultDatabaseName: defaultDatabaseName);
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

		protected override void BuildIsDistinctPredicate(NullabilityContext nullability, SqlPredicate.IsDistinct expr) => BuildIsDistinctPredicateFallback(nullability, expr);

		protected override void BuildTableExtensions(SqlTable table, string alias)
		{
			if (table.SqlQueryExtensions is not null)
				BuildTableExtensions(StringBuilder, table, alias, " WITH (", ", ", ")");
		}

		protected override bool BuildJoinType(SqlJoinedTable join, SqlSearchCondition condition)
		{
			if (join.SqlQueryExtensions != null)
			{
				var ext = join.SqlQueryExtensions.LastOrDefault(e => e.Scope is Sql.QueryExtensionScope.JoinHint);

				if (ext?.Arguments["hint"] is SqlValue v)
				{
					var h = (string)v.Value!;

					switch (join.JoinType)
					{
						case JoinType.Inner when SqlProviderFlags.IsCrossJoinSupported && condition.Conditions.IsNullOrEmpty() :
							                       StringBuilder.Append($"CROSS {h} JOIN "); return false;
						case JoinType.Inner      : StringBuilder.Append($"INNER {h} JOIN "); return true;
						case JoinType.Left       : StringBuilder.Append($"LEFT {h} JOIN ");  return true;
						case JoinType.Right      : StringBuilder.Append($"RIGHT {h} JOIN "); return true;
						case JoinType.Full       : StringBuilder.Append($"FULL {h} JOIN ");  return true;
						default                  : throw new InvalidOperationException();
					}
				}
			}

			return base.BuildJoinType(join, condition);
		}

		protected override void BuildQueryExtensions(SqlStatement statement)
		{
			if (statement.SqlQueryExtensions is not null)
				BuildQueryExtensions(StringBuilder, statement.SqlQueryExtensions, "OPTION (", ", ", ")");
		}
	}
}
