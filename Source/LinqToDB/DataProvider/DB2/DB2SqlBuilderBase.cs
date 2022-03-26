using System;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.DB2
{
	using Mapping;
	using SqlQuery;
	using SqlProvider;
	using System.Collections.Generic;

	abstract partial class DB2SqlBuilderBase : BasicSqlBuilder
	{
		protected DB2DataProvider? Provider { get; }

		protected DB2SqlBuilderBase(
			DB2DataProvider? provider,
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
			Provider = provider;
		}

		SqlField? _identityField;

		protected abstract DB2Version Version { get; }

		protected override bool SupportsNullInColumn => false;

		public override int CommandCount(SqlStatement statement)
		{
			if (statement is SqlTruncateTableStatement trun)
				return trun.ResetIdentity ? 1 + trun.Table!.IdentityFields.Count : 1;

			if (Version == DB2Version.LUW && statement is SqlInsertStatement insertStatement && insertStatement.Insert.WithIdentity)
			{
				_identityField = insertStatement.Insert.Into!.GetIdentityField();

				if (_identityField == null)
					return 2;
			}

			return 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			if (statement is SqlTruncateTableStatement trun)
			{
				var field = trun.Table!.IdentityFields[commandNumber - 1];

				StringBuilder.Append("ALTER TABLE ");
				ConvertTableName(StringBuilder, trun.Table.Server, trun.Table.Database, trun.Table.Schema, trun.Table.PhysicalName!, trun.Table.TableOptions);
				StringBuilder.Append(" ALTER ");
				Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
				StringBuilder.AppendLine(" RESTART WITH 1");
			}
			else
			{
				StringBuilder.AppendLine("SELECT identity_val_local() FROM SYSIBM.SYSDUMMY1");
			}
		}

		protected override void BuildTruncateTableStatement(SqlTruncateTableStatement truncateTable)
		{
			var table = truncateTable.Table!;

			BuildTag(truncateTable);
			AppendIndent();
			StringBuilder.Append("TRUNCATE TABLE ");
			BuildPhysicalTable(table, null);
			StringBuilder.Append(" IMMEDIATE");
			StringBuilder.AppendLine();
		}

		protected override void BuildSql(int commandNumber, SqlStatement statement, StringBuilder sb, OptimizationContext optimizationContext, int indent, bool skipAlias)
		{
			Statement           = statement;
			StringBuilder       = sb;
			OptimizationContext = optimizationContext;
			Indent              = indent;
			SkipAlias           = skipAlias;

			if (_identityField != null)
			{
				indent += 2;

				AppendIndent().AppendLine("SELECT");
				AppendIndent().Append('\t');
				BuildExpression(_identityField, false, true);
				sb.AppendLine();
				AppendIndent().AppendLine("FROM");
				AppendIndent().AppendLine("\tNEW TABLE");
				AppendIndent().Append('\t').AppendLine(OpenParens);
			}

			base.BuildSql(commandNumber, statement, sb, optimizationContext, indent, skipAlias);

			if (_identityField != null)
				sb.AppendLine("\t)");
		}

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			if (Version == DB2Version.zOS)
			{
				StringBuilder
					.AppendLine(";")
					.AppendLine("SELECT IDENTITY_VAL_LOCAL() FROM SYSIBM.SYSDUMMY1");
			}
		}

		protected override void BuildSelectClause(SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count == 0)
			{
				AppendIndent().AppendLine("SELECT");
				BuildColumns(selectQuery);
				AppendIndent().AppendLine("FROM SYSIBM.SYSDUMMY1");
			}
			else
				base.BuildSelectClause(selectQuery);
		}

		protected override string? LimitFormat(SelectQuery selectQuery)
		{
			return selectQuery.Select.SkipValue == null ? "FETCH FIRST {0} ROWS ONLY" : null;
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.DateTime  :
				case DataType.DateTime2 :
					StringBuilder.Append("timestamp");
					if (type.Type.Precision != null && type.Type.Precision != 6)
						StringBuilder.Append($"({type.Type.Precision})");
					return;
				case DataType.Boolean   : StringBuilder.Append("smallint");              return;
				case DataType.Guid      : StringBuilder.Append("char(16) for bit data"); return;
				case DataType.VarChar   :
					if (type.Type.Length == null || type.Type.Length > 32672 || type.Type.Length < 1)
					{
						StringBuilder
							.Append(type.Type.DataType)
							.Append("(32672)");
						return;
					}

					break;
				case DataType.NVarChar:
					if (type.Type.Length == null || type.Type.Length > 8168 || type.Type.Length < 1)
					{
						StringBuilder
							.Append(type.Type.DataType)
							.Append("(8168)");
						return;
					}

					break;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable);
		}

		protected override void BuildCreateTableNullAttribute(SqlField field, DefaultNullable defaultNullable)
		{
			if (field.CanBeNull && field.Type.DataType == DataType.Guid)
				return;

			base.BuildCreateTableNullAttribute(field, defaultNullable);
		}

		public static DB2IdentifierQuoteMode IdentifierQuoteMode = DB2IdentifierQuoteMode.Auto;

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
					return sb.Append('@').Append(value);

				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return sb.Append(':').Append(value);

				case ConvertType.SprocParameterToName:
					return value.Length > 0 && value[0] == ':'
						? sb.Append(value.Substring(1))
						: sb.Append(value);

				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTable:
				case ConvertType.NameToQueryTableAlias:
					if (IdentifierQuoteMode != DB2IdentifierQuoteMode.None)
					{
						if (value.Length > 0 && value[0] == '"')
							return sb.Append(value);

						if (IdentifierQuoteMode == DB2IdentifierQuoteMode.Quote ||
							value.StartsWith("_") ||
							value.Any(c => char.IsLower(c) || char.IsWhiteSpace(c)))
							return sb.Append('"').Append(value).Append('"');
					}

					break;
			}

			return sb.Append(value);
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, "FROM SYSIBM.SYSDUMMY1 FETCH FIRST 1 ROW ONLY");
		}

		protected override void BuildEmptyInsert(SqlInsertClause insertClause)
		{
			StringBuilder.Append("VALUES ");

			foreach (var _ in insertClause.Into!.Fields)
				StringBuilder.Append("(DEFAULT)");

			StringBuilder.AppendLine();
		}

		protected override void BuildCreateTableIdentityAttribute1(SqlField field)
		{
			StringBuilder.Append("GENERATED ALWAYS AS IDENTITY");
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string? server, string? database, string? schema, string table, TableOptions tableOptions)
		{
			if (database != null && database.Length == 0) database = null;
			if (schema   != null && schema.  Length == 0) schema   = null;

			// "db..table" syntax not supported
			if (database != null && schema == null)
				throw new LinqToDBException("DB2 requires schema name if database name provided.");

			return base.BuildTableName(sb, null, database, schema, table, tableOptions);
		}

		protected override string? GetProviderTypeName(IDbDataParameter parameter)
		{
			if (parameter.DbType == DbType.Decimal && parameter.Value is decimal decValue)
			{
				var d = new SqlDecimal(decValue);
				return "(" + d.Precision + InlineComma + d.Scale + ")";
			}

			if (Provider != null)
			{
				var param = Provider.TryGetProviderParameter(parameter, MappingSchema);
				if (param != null)
					return Provider.Adapter.GetDbType(param).ToString();
			}

			return base.GetProviderTypeName(parameter);
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			var table = dropTable.Table!;

			BuildTag(dropTable);
			if (dropTable.Table.TableOptions.HasDropIfExists())
			{
				AppendIndent().Append(@"BEGIN
	DECLARE CONTINUE HANDLER FOR SQLSTATE '42704' BEGIN END;
	EXECUTE IMMEDIATE 'DROP TABLE ");
				BuildPhysicalTable(table, null);
				StringBuilder.AppendLine(
				@"';
END");
			}
			else
			{
				AppendIndent().Append("DROP TABLE ");
				BuildPhysicalTable(table, null);
				StringBuilder.AppendLine();
			}
		}

		protected override void BuildCreateTableCommand(SqlTable table)
		{
			string command;

			if (table.TableOptions.IsTemporaryOptionSet())
			{
				switch (table.TableOptions & TableOptions.IsTemporaryOptionSet)
				{
					case TableOptions.IsTemporary                                                                               :
					case TableOptions.IsTemporary |                                           TableOptions.IsLocalTemporaryData :
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure                                      :
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure  | TableOptions.IsLocalTemporaryData :
					case                                                                      TableOptions.IsLocalTemporaryData :
					case                            TableOptions.IsLocalTemporaryStructure                                      :
					case                            TableOptions.IsLocalTemporaryStructure  | TableOptions.IsLocalTemporaryData :
						command = "DECLARE GLOBAL TEMPORARY TABLE ";
						break;
					case                            TableOptions.IsGlobalTemporaryStructure                                     :
					case                            TableOptions.IsGlobalTemporaryStructure | TableOptions.IsLocalTemporaryData :
						command = "CREATE GLOBAL TEMPORARY TABLE ";
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
		}

		protected override void BuildStartCreateTableStatement(SqlCreateTableStatement createTable)
		{
			if (createTable.StatementHeader == null && createTable.Table!.TableOptions.HasCreateIfNotExists())
			{
				AppendIndent().AppendLine(@"BEGIN");

				Indent++;

				AppendIndent().AppendLine(@"DECLARE CONTINUE HANDLER FOR SQLSTATE '42710' BEGIN END;");
				AppendIndent().AppendLine(@"EXECUTE IMMEDIATE '");

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

				AppendIndent()
					.AppendLine("';");

				Indent--;

				StringBuilder
					.AppendLine("END");
			}
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			// DB2 doesn't support constraints on temp tables
			if (createTable.Table.TableOptions.IsTemporaryOptionSet())
			{
				var idx = StringBuilder.Length - 1;
				while (idx >= 0 && StringBuilder[idx] != ',')
					idx--;
				StringBuilder.Length = idx == -1 ? 0 : idx;
				return;
			}

			base.BuildCreateTablePrimaryKey(createTable, pkName, fieldNames);
		}

		public override string? GetTableSchemaName(SqlTable table)
		{
			return table.Schema == null && table.TableOptions.IsTemporaryOptionSet() ? "SESSION" : base.GetTableSchemaName(table);
		}
	}
}
