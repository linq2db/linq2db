using System;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using SqlQuery;
	using SqlProvider;
	using System.Text;
	using Mapping;

	partial class Oracle11SqlBuilder : BasicSqlBuilder
	{
		protected OracleDataProvider? Provider { get; }

		public Oracle11SqlBuilder(
			OracleDataProvider? provider,
			MappingSchema       mappingSchema,
			ISqlOptimizer       sqlOptimizer,
			SqlProviderFlags    sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
			Provider = provider;
		}

		// remote context
		public Oracle11SqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override void BuildSelectClause(SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count == 0)
			{
				AppendIndent().Append("SELECT").AppendLine();
				BuildColumns(selectQuery);
				AppendIndent().Append("FROM SYS.DUAL").AppendLine();
			}
			else
				base.BuildSelectClause(selectQuery);
		}

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			var identityField = insertClause.Into!.GetIdentityField();

			if (identityField == null)
				throw new SqlException("Identity field must be defined for '{0}'.", insertClause.Into.Name);

			AppendIndent().AppendLine("RETURNING ");
			AppendIndent().Append('\t');
			BuildExpression(identityField, false, true);
			StringBuilder.AppendLine(" INTO :IDENTITY_PARAMETER");
		}

		public override ISqlExpression? GetIdentityExpression(SqlTable table)
		{
			if (!table.SequenceAttributes.IsNullOrEmpty())
			{
				var attr = GetSequenceNameAttribute(table, false);

				if (attr != null)
					return new SqlExpression(
							(attr.Schema != null ? ConvertInline(attr.Schema, ConvertType.NameToSchema) + "." : null) +
							ConvertInline(attr.SequenceName, ConvertType.SequenceName) +
							".nextval",
						Precedence.Primary);
			}

			return base.GetIdentityExpression(table);
		}

		protected override bool BuildWhere(SelectQuery selectQuery)
		{
			SqlOptimizer.ConvertSkipTake(MappingSchema, selectQuery, OptimizationContext, out var takeExpr, out var skipEpr);

			return
				base.BuildWhere(selectQuery) ||
				!NeedSkip(takeExpr, skipEpr) &&
				 NeedTake(takeExpr) &&
				selectQuery.OrderBy.IsEmpty && selectQuery.Having.IsEmpty;
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new Oracle11SqlBuilder(Provider, MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override void BuildSetOperation(SetOperation operation, StringBuilder sb)
		{
			switch (operation)
			{
				case SetOperation.Except    : sb.Append("MINUS");     return;
				case SetOperation.ExceptAll : sb.Append("MINUS ALL"); return;
			}
			base.BuildSetOperation(operation, sb);
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.Date           :
				case DataType.DateTime       : StringBuilder.Append("date");                      break;
				case DataType.DateTime2      :
					if (type.Type.Precision == 6 || type.Type.Precision == null)
						StringBuilder.Append("timestamp");
					else
						StringBuilder.Append($"timestamp({type.Type.Precision})");
					break;
				case DataType.DateTimeOffset :
					if (type.Type.Precision == 6 || type.Type.Precision == null)
						StringBuilder.Append("timestamp with time zone");
					else
						StringBuilder.Append($"timestamp({type.Type.Precision}) with time zone");
					break;
				case DataType.UInt32         :
				case DataType.Int64          : StringBuilder.Append("Number(19)");                break;
				case DataType.SByte          :
				case DataType.Byte           : StringBuilder.Append("Number(3)");                 break;
				case DataType.Money          : StringBuilder.Append("Number(19, 4)");             break;
				case DataType.SmallMoney     : StringBuilder.Append("Number(10, 4)");             break;
				case DataType.VarChar        :
					if (type.Type.Length == null || type.Type.Length > 4000 || type.Type.Length < 1)
						StringBuilder.Append("VarChar(4000)");
					else
						StringBuilder.Append($"VarChar({type.Type.Length})");
					break;
				case DataType.NVarChar       :
					if (type.Type.Length == null || type.Type.Length > 4000 || type.Type.Length < 1)
						StringBuilder.Append("VarChar2(4000)");
					else
						StringBuilder.Append($"VarChar2({type.Type.Length})");
					break;
				case DataType.Boolean        : StringBuilder.Append("Char(1)");                   break;
				case DataType.NText          : StringBuilder.Append("NClob");                     break;
				case DataType.Text           : StringBuilder.Append("Clob");                      break;
				case DataType.Guid           : StringBuilder.Append("Raw(16)");                   break;
				case DataType.Binary         :
				case DataType.VarBinary      :
					if (type.Type.Length == null || type.Type.Length == 0)
						StringBuilder.Append("BLOB");
					else
						StringBuilder.Append("Raw(").Append(type.Type.Length).Append(')');
					break;
				default: base.BuildDataTypeFromDataType(type, forCreateTable);                    break;
			}
		}

		protected override void BuildDeleteQuery(SqlDeleteStatement deleteStatement)
		{
			if (deleteStatement.With?.Clauses.Count > 0)
			{
				BuildDeleteQuery2(deleteStatement);
			}
			else
			{
				base.BuildDeleteQuery(deleteStatement);
			}
		}

		protected override void BuildInsertQuery(SqlStatement statement, SqlInsertClause insertClause, bool addAlias)
		{
			if (statement is SqlStatementWithQueryBase withQuery && withQuery.With?.Clauses.Count > 0)
			{
				BuildInsertQuery2(statement, insertClause, addAlias);
			}
			else
			{
				base.BuildInsertQuery(statement, insertClause, addAlias);
			}
		}

		protected sealed override bool IsReserved(string word)
		{
			// TODO: now we use static 11g list
			// proper solution will be use version-based list or load it from V$RESERVED_WORDS (needs research)
			// right now list is a merge of two lists:
			// SQL reserved words: https://docs.oracle.com/database/121/SQLRF/ap_keywd001.htm
			// PL/SQL reserved words: https://docs.oracle.com/cd/B28359_01/appdev.111/b28370/reservewords.htm
			// keywords are not included as they are keywords :)
			//
			// V$RESERVED_WORDS: https://docs.oracle.com/cd/B28359_01/server.111/b28320/dynviews_2126.htm
			return ReservedWords.IsReserved(word, ProviderName.Oracle);
		}

		/// <summary>
		/// Check if identifier is valid without quotation. Expects non-zero length string as input.
		/// </summary>
		private bool IsValidIdentifier(string name)
		{
			// https://docs.oracle.com/cd/B28359_01/server.111/b28286/sql_elements008.htm#SQLRF00223
			// TODO: "Nonquoted identifiers can contain only alphanumeric characters from your database character set"
			// now we check only for latin letters
			// Also we should allow only uppercase letters:
			// "Nonquoted identifiers are not case sensitive. Oracle interprets them as uppercase"
			return !IsReserved(name) &&
				((OracleTools.DontEscapeLowercaseIdentifiers && name[0] >= 'a' && name[0] <= 'z') || (name[0] >= 'A' && name[0] <= 'Z')) &&
				name.All(c =>
					(OracleTools.DontEscapeLowercaseIdentifiers && c >= 'a' && c <= 'z') ||
					(c >= 'A' && c <= 'Z') ||
					(c >= '0' && c <= '9') ||
					c == '$' ||
					c == '#' ||
					c == '_');
		}

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
					return sb.Append(':').Append(value);
				// needs proper list of reserved words and name validation
				// something like we did for Firebird
				// right now reserved words list contains garbage
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryTable:
				case ConvertType.NameToServer:
				case ConvertType.SequenceName:
				case ConvertType.NameToSchema:
				case ConvertType.TriggerName:
					if (!IsValidIdentifier(value))
						return sb.Append('"').Append(value).Append('"');

					return sb.Append(value);
			}

			return sb.Append(value);
		}

		protected override StringBuilder BuildExpression(
			ISqlExpression expr,
			bool buildTableName,
			bool checkParentheses,
			string? alias,
			ref bool addAlias,
			bool throwExceptionIfTableNotFound = true)
		{
			return base.BuildExpression(
				expr,
				buildTableName && Statement.QueryType != QueryType.MultiInsert,
				checkParentheses,
				alias,
				ref addAlias,
				throwExceptionIfTableNotFound);
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, "FROM SYS.DUAL");
		}

		public override string GetReserveSequenceValuesSql(int count, string sequenceName)
		{
			return $"SELECT {ConvertInline(sequenceName, ConvertType.SequenceName)}.nextval ID from DUAL connect by level <= {count}";
		}

		protected override void BuildEmptyInsert(SqlInsertClause insertClause)
		{
			StringBuilder.Append("VALUES ");

			foreach (var _ in insertClause.Into!.Fields)
				StringBuilder.Append("(DEFAULT)");

			StringBuilder.AppendLine();
		}

		SqlField? _identityField;

		public override int CommandCount(SqlStatement statement)
		{
			switch (statement)
			{
				case SqlTruncateTableStatement truncateTable:
					return truncateTable.ResetIdentity && truncateTable.Table!.IdentityFields.Count > 0 ? 2 : 1;

				case SqlCreateTableStatement createTable:
					_identityField = createTable.Table!.IdentityFields.Count > 0 ? createTable.Table!.IdentityFields[0] : null;
					if (_identityField != null)
						return 3;
					break;
			}

			return base.CommandCount(statement);
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			var identityField = dropTable.Table!.IdentityFields.Count > 0 ? dropTable.Table!.IdentityFields[0] : null;

			if (identityField == null && dropTable.Table.TableOptions.HasDropIfExists() == false && dropTable.Table.TableOptions.HasIsTemporary() == false)
			{
				base.BuildDropTableStatement(dropTable);
			}
			else
			{
				StringBuilder
					.AppendLine(@"BEGIN");

				if (identityField == null)
				{
					StringBuilder
						.Append("\tEXECUTE IMMEDIATE 'DROP TABLE ");
					BuildPhysicalTable(dropTable.Table, null);
					StringBuilder
						.AppendLine("';")
						;

					if (dropTable.Table.TableOptions.HasDropIfExists() || dropTable.Table.TableOptions.HasIsTemporary())
					{
						StringBuilder
							.AppendLine("EXCEPTION")
							.AppendLine("\tWHEN OTHERS THEN")
							.AppendLine("\t\tIF SQLCODE != -942 THEN")
							.AppendLine("\t\t\tRAISE;")
							.AppendLine("\t\tEND IF;")
							;
					}
				}
				else if (!dropTable.Table.TableOptions.HasDropIfExists() && !dropTable.Table.TableOptions.HasIsTemporary())
				{
					StringBuilder
						.Append("\tEXECUTE IMMEDIATE 'DROP TRIGGER ");

					AppendSchemaPrefix(StringBuilder, dropTable.Table!.Schema);
					Convert(StringBuilder, MakeIdentityTriggerName(dropTable.Table.PhysicalName!), ConvertType.TriggerName);

					StringBuilder
						.AppendLine("';")
						.Append("\tEXECUTE IMMEDIATE 'DROP SEQUENCE ");

					AppendSchemaPrefix(StringBuilder, dropTable.Table!.Schema);
					Convert(StringBuilder, MakeIdentitySequenceName(dropTable.Table.PhysicalName!), ConvertType.SequenceName);

					StringBuilder
						.AppendLine("';")
						.Append("\tEXECUTE IMMEDIATE 'DROP TABLE ");
					BuildPhysicalTable(dropTable.Table, null);
					StringBuilder
						.AppendLine("';")
						;
				}
				else
				{
					StringBuilder
						.AppendLine("\tBEGIN")
						.Append("\t\tEXECUTE IMMEDIATE 'DROP TRIGGER ");

					AppendSchemaPrefix(StringBuilder, dropTable.Table!.Schema);
					Convert(StringBuilder, MakeIdentityTriggerName(dropTable.Table.PhysicalName!), ConvertType.TriggerName);

					StringBuilder
						.AppendLine("';")
						.AppendLine("\tEXCEPTION")
						.AppendLine("\t\tWHEN OTHERS THEN")
						.AppendLine("\t\t\tIF SQLCODE != -4080 THEN")
						.AppendLine("\t\t\t\tRAISE;")
						.AppendLine("\t\t\tEND IF;")
						.AppendLine("\tEND;")

						.AppendLine("\tBEGIN")
						.Append("\t\tEXECUTE IMMEDIATE 'DROP SEQUENCE ");
					
					AppendSchemaPrefix(StringBuilder, dropTable.Table!.Schema);
					Convert(StringBuilder, MakeIdentitySequenceName(dropTable.Table.PhysicalName!), ConvertType.SequenceName);

					StringBuilder
						.AppendLine("';")
						.AppendLine("\tEXCEPTION")
						.AppendLine("\t\tWHEN OTHERS THEN")
						.AppendLine("\t\t\tIF SQLCODE != -2289 THEN")
						.AppendLine("\t\t\t\tRAISE;")
						.AppendLine("\t\t\tEND IF;")
						.AppendLine("\tEND;")

						.AppendLine("\tBEGIN")
						.Append("\t\tEXECUTE IMMEDIATE 'DROP TABLE ");
					BuildPhysicalTable(dropTable.Table, null);
					StringBuilder
						.AppendLine("';")
						.AppendLine("\tEXCEPTION")
						.AppendLine("\t\tWHEN OTHERS THEN")
						.AppendLine("\t\t\tIF SQLCODE != -942 THEN")
						.AppendLine("\t\t\t\tRAISE;")
						.AppendLine("\t\t\tEND IF;")
						.AppendLine("\tEND;")
						;
				}

				StringBuilder
					.AppendLine("END;")
					;
			}
		}

		protected static string MakeIdentityTriggerName(string tableName)
		{
			return "TIDENTITY_" + tableName;
		}

		protected static string MakeIdentitySequenceName(string tableName)
		{
			return "SIDENTITY_" + tableName;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			switch (Statement)
			{
				case SqlTruncateTableStatement truncate:
					var sequenceName = ConvertInline(MakeIdentitySequenceName(truncate.Table!.PhysicalName!), ConvertType.SequenceName);
					StringBuilder
						.AppendFormat(@"DECLARE
	l_value number;
BEGIN
	-- Select the next value of the sequence
	EXECUTE IMMEDIATE 'SELECT {0}.NEXTVAL FROM dual' INTO l_value;

	-- Set a negative increment for the sequence, with value = the current value of the sequence
	EXECUTE IMMEDIATE 'ALTER SEQUENCE {0} INCREMENT BY -' || l_value || ' MINVALUE 0';

	-- Select once from the sequence, to take its current value back to 0
	EXECUTE IMMEDIATE 'select {0}.NEXTVAL FROM dual' INTO l_value;

	-- Set the increment back to 1
	EXECUTE IMMEDIATE 'ALTER SEQUENCE {0} INCREMENT BY 1 MINVALUE 0';
END;",
							sequenceName)
						.AppendLine()
						;

					break;
				case SqlCreateTableStatement createTable:
				{
					if (commandNumber == 1)
					{
						StringBuilder
							.Append("CREATE SEQUENCE ");
						AppendSchemaPrefix(StringBuilder, createTable.Table!.Schema);
						Convert(StringBuilder, MakeIdentitySequenceName(createTable.Table!.PhysicalName!), ConvertType.SequenceName);
						StringBuilder
							.AppendLine();
					}
					else
					{
						StringBuilder
							.Append("CREATE OR REPLACE TRIGGER ");
						AppendSchemaPrefix(StringBuilder, createTable.Table!.Schema);
						Convert(StringBuilder, MakeIdentityTriggerName(createTable.Table!.PhysicalName!), ConvertType.TriggerName);
						StringBuilder
							.AppendLine()
							.AppendFormat("BEFORE INSERT ON ");

						BuildPhysicalTable(createTable.Table, null);

						StringBuilder
							.AppendLine(" FOR EACH ROW")
							.AppendLine("BEGIN")
							.Append("\tSELECT ");
						AppendSchemaPrefix(StringBuilder, createTable.Table!.Schema);
						Convert(StringBuilder, MakeIdentitySequenceName(createTable.Table!.PhysicalName!), ConvertType.SequenceName);
						StringBuilder
							.Append(".NEXTVAL INTO :NEW.");
						Convert(StringBuilder, _identityField!.PhysicalName, ConvertType.NameToQueryField);
						StringBuilder
							.Append(" FROM dual;")
							.AppendLine  ()
							.AppendLine  ("END;");
					}

					break;
				}
			}
		}

		protected override void BuildTruncateTable(SqlTruncateTableStatement truncateTable)
		{
			StringBuilder.Append("TRUNCATE TABLE ");
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string? server, string? database, string? schema, string table, TableOptions tableOptions)
		{
			if (server != null && server.Length == 0) server = null;
			if (schema != null && schema.Length == 0) schema = null;

			if (schema != null)
				sb.Append(schema).Append('.');

			sb.Append(table);

			if (server != null)
				sb.Append('@').Append(server);

			return sb;
		}

		private void AppendSchemaPrefix(StringBuilder sb, string? schema)
		{
			if (schema != null)
			{
				Convert(sb, schema, ConvertType.NameToSchema);
				sb.Append('.');
			}
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

		protected override void BuildCreateTableCommand(SqlTable table)
		{
			string command;

			if (table.TableOptions.IsTemporaryOptionSet())
			{
				switch (table.TableOptions & TableOptions.IsTemporaryOptionSet)
				{
					case TableOptions.IsTemporary                                                                                     :
					case TableOptions.IsTemporary |                                           TableOptions.IsLocalTemporaryData       :
					case TableOptions.IsTemporary | TableOptions.IsGlobalTemporaryStructure                                           :
					case TableOptions.IsTemporary | TableOptions.IsGlobalTemporaryStructure | TableOptions.IsLocalTemporaryData       :
					case                                                                      TableOptions.IsLocalTemporaryData       :
					case                                                                      TableOptions.IsTransactionTemporaryData :
					case                            TableOptions.IsGlobalTemporaryStructure                                           :
					case                            TableOptions.IsGlobalTemporaryStructure | TableOptions.IsLocalTemporaryData       :
					case                            TableOptions.IsGlobalTemporaryStructure | TableOptions.IsTransactionTemporaryData :
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
			if (createTable.StatementHeader == null && (createTable.Table.TableOptions.HasCreateIfNotExists() || createTable.Table.TableOptions.HasIsTemporary()))
			{
				AppendIndent().AppendLine(@"BEGIN");

				Indent++;

				AppendIndent().AppendLine(@"EXECUTE IMMEDIATE '");

				Indent++;
			}

			base.BuildStartCreateTableStatement(createTable);
		}

		protected override void BuildEndCreateTableStatement(SqlCreateTableStatement createTable)
		{
			base.BuildEndCreateTableStatement(createTable);

			if (createTable.StatementHeader == null)
			{
				var table = createTable.Table;

				if (table.TableOptions.IsTemporaryOptionSet())
				{
					AppendIndent().AppendLine(table.TableOptions.HasIsTransactionTemporaryData()
						? "ON COMMIT DELETE ROWS"
						: "ON COMMIT PRESERVE ROWS");
				}

				if (table.TableOptions.HasCreateIfNotExists() || table.TableOptions.HasIsTemporary())
				{
					Indent--;

					AppendIndent()
						.AppendLine("';");

					Indent--;

					StringBuilder
						.AppendLine("EXCEPTION")
						.AppendLine("\tWHEN OTHERS THEN")
						.AppendLine("\t\tIF SQLCODE != -955 THEN")
						.AppendLine("\t\t\tRAISE;")
						.AppendLine("\t\tEND IF;")
						.AppendLine("END;")
						;
				}
			}
		}
	}
}
