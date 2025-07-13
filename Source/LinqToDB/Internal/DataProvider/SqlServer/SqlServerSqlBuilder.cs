using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;

using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
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

		protected override void BuildOutputSubclause(SqlStatement statement, SqlInsertClause insertClause)
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
				BuildOutputSubclause(statement.GetOutputClause());
			}
		}

		protected override string OutputKeyword       => "OUTPUT";
		protected override string DeletedOutputTable  => "DELETED";
		protected override string InsertedOutputTable => "INSERTED";

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
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

		protected override void BuildDeleteClause(SqlDeleteStatement deleteStatement)
		{
			ISqlTableSource? table = null;

			if (deleteStatement.Table != null)
			{
				table = deleteStatement.SelectQuery.From.FindTableSource(deleteStatement.Table);
			}

			table ??= deleteStatement.SelectQuery.From.Tables[0];

			AppendIndent()
				.Append("DELETE");

			BuildSkipFirst(deleteStatement.SelectQuery);

			StringBuilder.Append(' ');

			var alias = GetTableAlias(table);
			if (alias == null)
			{
				throw new InvalidOperationException();
			}

			Convert(StringBuilder, alias, ConvertType.NameToQueryTableAlias);
			StringBuilder.AppendLine();
			BuildOutputSubclause(deleteStatement.GetOutputClause());
		}

		protected override void BuildOutputSubclause(SqlOutputClause? output)
		{
			if (BuildStep == Step.Output)
			{
				return;
			}

			base.BuildOutputSubclause(output);
		}

		protected override void BuildUpdateClause(SqlStatement statement, SelectQuery selectQuery,
			SqlUpdateClause                                    updateClause)
		{
			base.BuildUpdateClause(statement, selectQuery, updateClause);
			BuildOutputSubclause(statement.GetOutputClause());
		}

		protected override void BuildUpdateTableName(SelectQuery selectQuery,
			SqlUpdateClause                                      updateClause)
		{
			if (updateClause.TableSource != null)
				Convert(StringBuilder, GetTableAlias(updateClause.TableSource)!, ConvertType.NameToQueryTableAlias);
			else if (updateClause.Table != null)
				BuildPhysicalTable(updateClause.Table, null);
			else
				throw new InvalidOperationException();
		}

		private static string GetTablePhysicalName(string tableName, TableOptions tableOptions)
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

		public override StringBuilder BuildObjectName(StringBuilder sb, SqlObjectName name, ConvertType objectType, bool escape, TableOptions tableOptions, bool withoutSuffix)
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
					if (value == PseudoFunctions.MERGE_ACTION)
						return sb.Append("$action");
					goto case ConvertType.NameToQueryFieldAlias;
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
					if (value.Length > 0 && value[0] == '[')
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
				BuildPhysicalTable(table, alias : null, defaultDatabaseName : defaultDatabaseName);
				StringBuilder.AppendLine("', N'U') IS NOT NULL)");
				Indent++;
			}

			AppendIndent().Append("DROP TABLE ");
			BuildPhysicalTable(table, alias : null);

			if (dropTable.Table.TableOptions.HasDropIfExists())
				Indent--;
		}

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.DataType)
			{
				case DataType.Json      : StringBuilder.Append("JSON");             return;
				case DataType.Guid      : StringBuilder.Append("UniqueIdentifier"); return;
				case DataType.Variant   : StringBuilder.Append("Sql_Variant");      return;
				case DataType.Money     : StringBuilder.Append("MONEY");            return;
				case DataType.SmallMoney: StringBuilder.Append("SMALLMONEY");       return;
				case DataType.NVarChar  :
					if (type.Length is null or > 4000 or < 1)
					{
						StringBuilder.Append(CultureInfo.InvariantCulture, $"{type.DataType}(Max)");
						return;
					}

					break;
				case DataType.Array | DataType.Single:
					StringBuilder
						.Append("VECTOR(")
						// length is required and in 1-1998 range
						// we use default 0 to produce error when user didn't specify length
						.Append(CultureInfo.InvariantCulture, $"{type.Length ?? 0}")
						.Append(')');
					return;

				case DataType.VarChar   :
				case DataType.VarBinary :
					if (type.Length is null or > 8000 or < 1)
					{
						StringBuilder.Append(CultureInfo.InvariantCulture, $"{type.DataType}(Max)");
						return;
					}

					break;

				case DataType.DateTime2:
				case DataType.DateTimeOffset:
				case DataType.Time:
					StringBuilder.Append(CultureInfo.InvariantCulture, $"{type.DataType}");
					// Default precision for all three types is 7.
					// For all other non-null values (including 0) precision must be specified.
					if (type.Precision != null && type.Precision != 7)
					{
						StringBuilder.Append(CultureInfo.InvariantCulture, $"({type.Precision})");
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
			BuildTableName(table, true, false);
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

		protected override void BuildTableExtensions(SqlTable table, string alias)
		{
			if (table.SqlQueryExtensions is not null)
				BuildTableExtensions(StringBuilder, table, alias, " WITH (", ", ", ")");
		}

		protected override void BuildTableNameExtensions(SqlTable table)
		{
			var ext = table.SqlQueryExtensions?.LastOrDefault(e => e.Scope == Sql.QueryExtensionScope.TableNameHint);

			if (ext is { BuilderType: not null })
			{
				var extensionBuilder = GetExtensionBuilder(ext.BuilderType);

				switch (extensionBuilder)
				{
					case ISqlQueryExtensionBuilder queryExtensionBuilder:
						queryExtensionBuilder.Build(NullabilityContext, this, StringBuilder, ext);
						break;
					default:
						throw new LinqToDBException($"Type '{ext.BuilderType.FullName}' must implement the '{typeof(ISqlQueryExtensionBuilder).FullName}' interface.");
				}
			}
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
						case JoinType.Inner when SqlProviderFlags.IsCrossJoinSupported && condition.Predicates.IsNullOrEmpty() :
						                           StringBuilder.Append(CultureInfo.InvariantCulture, $"CROSS {h} JOIN "); return false;
						case JoinType.Inner      : StringBuilder.Append(CultureInfo.InvariantCulture, $"INNER {h} JOIN "); return true;
						case JoinType.Left       : StringBuilder.Append(CultureInfo.InvariantCulture, $"LEFT {h} JOIN ");  return true;
						case JoinType.Right      : StringBuilder.Append(CultureInfo.InvariantCulture, $"RIGHT {h} JOIN "); return true;
						case JoinType.Full       : StringBuilder.Append(CultureInfo.InvariantCulture, $"FULL {h} JOIN ");  return true;
						default                  : throw new InvalidOperationException();
					}
				}
			}

			return base.BuildJoinType(join, condition);
		}

		protected override void BuildQueryExtensions(SqlStatement statement)
		{
			if (statement.SqlQueryExtensions is not null)
				BuildQueryExtensions(StringBuilder, statement.SqlQueryExtensions, "OPTION (", ", ", ")", Sql.QueryExtensionScope.QueryHint);
		}
	}
}
