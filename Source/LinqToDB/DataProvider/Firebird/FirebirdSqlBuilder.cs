using System;
using System.Data;
using System.Linq;

#region ReSharper disable
// ReSharper disable SuggestUseVarKeywordEverywhere
// ReSharper disable SuggestUseVarKeywordEvident
#endregion

namespace LinqToDB.DataProvider.Firebird
{
	using Common;
	using Mapping;
	using SqlQuery;
	using SqlProvider;
	using System.Text;

	public partial class FirebirdSqlBuilder : BasicSqlBuilder
	{
		private readonly FirebirdDataProvider? _provider;

		public FirebirdSqlBuilder(
			FirebirdDataProvider? provider,
			MappingSchema         mappingSchema,
			ISqlOptimizer         sqlOptimizer,
			SqlProviderFlags      sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
			_provider = provider;
		}

		// remote context
		public FirebirdSqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new FirebirdSqlBuilder(_provider, MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override void BuildSelectClause(SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count == 0)
			{
				AppendIndent();
				StringBuilder.Append("SELECT").AppendLine();
				BuildColumns(selectQuery);
				AppendIndent();
				StringBuilder.Append("FROM rdb$database").AppendLine();
			}
			else if (selectQuery.Select.IsDistinct)
			{
				AppendIndent();
				StringBuilder.Append("SELECT");
				BuildSkipFirst(selectQuery);
				StringBuilder.Append(" DISTINCT");
				StringBuilder.AppendLine();
				BuildColumns(selectQuery);
			}
			else
				base.BuildSelectClause(selectQuery);
		}

		protected override bool   SkipFirst  => false;
		protected override string SkipFormat => "SKIP {0}";
		protected override bool   IsRecursiveCteKeywordRequired => true;

		protected override string FirstFormat(SelectQuery selectQuery)
		{
			return "FIRST {0}";
		}

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			var identityField = insertClause.Into!.GetIdentityField();

			if (identityField == null)
				throw new SqlException("Identity field must be defined for '{0}'.", insertClause.Into.Name);

			AppendIndent().AppendLine("RETURNING");
			AppendIndent().Append("\t");
			BuildExpression(identityField, false, true);
		}

		public override ISqlExpression? GetIdentityExpression(SqlTable table)
		{
			if (!table.SequenceAttributes.IsNullOrEmpty())
				return new SqlExpression("GEN_ID(" + ConvertInline(table.SequenceAttributes[0].SequenceName, ConvertType.SequenceName) + ", 1)", Precedence.Primary);

			return base.GetIdentityExpression(table);
		}

		protected override void BuildFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(func);
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.Decimal       :
					base.BuildDataTypeFromDataType(type.Type.Precision > 18 ? new SqlDataType(type.Type.DataType, type.Type.SystemType, null, 18, type.Type.Scale, type.Type.DbType) : type, forCreateTable);
					                                                                                      break;
				case DataType.SByte         :
				case DataType.Byte          : StringBuilder.Append("SmallInt");                           break;
				case DataType.Money         : StringBuilder.AppendFormat("Decimal(18{0}4)", InlineComma); break;
				case DataType.SmallMoney    : StringBuilder.AppendFormat("Decimal(10{0}4)", InlineComma); break;
				case DataType.DateTime2     :
				case DataType.SmallDateTime :
				case DataType.DateTime      : StringBuilder.Append("TimeStamp");                          break;
				case DataType.NVarChar      :
					StringBuilder.Append("VarChar");

					// 10921 is implementation limit for UNICODE_FSS encoding
					// use 255 as default length, because FB have 64k row-size limits
					// also it is not good to depend on implementation limits
					if (type.Type.Length == null || type.Type.Length < 1)
						StringBuilder.Append("(255)");
					else
						StringBuilder.Append($"({type.Type.Length})");

					StringBuilder.Append(" CHARACTER SET UNICODE_FSS");
					                                                                                      break;
				case DataType.VarBinary     : StringBuilder.Append("BLOB");                               break;
				// BOOLEAN type available since FB 3.0, but FirebirdDataProvider.SetParameter converts boolean to '1'/'0'
				// so for now we will use type, compatible with SetParameter by default
				case DataType.Boolean       : StringBuilder.Append("CHAR");                               break;
				default: base.BuildDataTypeFromDataType(type, forCreateTable);                            break;
			}
		}

		protected sealed override bool IsReserved(string word)
		{
			return ReservedWords.IsReserved(word, ProviderName.Firebird);
		}

		protected override void BuildColumnExpression(SelectQuery? selectQuery, ISqlExpression expr, string? alias, ref bool addAlias)
		{
			var wrap = false;

			if (expr.SystemType == typeof(bool))
			{
				if (expr is SqlSearchCondition)
					wrap = true;
				else
					wrap =
						expr is SqlExpression ex      &&
						ex.Expr              == "{0}" &&
						ex.Parameters.Length == 1     &&
						ex.Parameters[0] is SqlSearchCondition;
			}

			if (wrap) StringBuilder.Append("CASE WHEN ");
			base.BuildColumnExpression(selectQuery, expr, alias, ref addAlias);
			if (wrap) StringBuilder.Append(" THEN 1 ELSE 0 END");
		}

		/// <summary>
		/// Check if identifier is valid without quotation. Expects non-zero length string as input.
		/// </summary>
		private bool IsValidIdentifier(string name)
		{
			// https://firebirdsql.org/file/documentation/reference_manuals/fblangref25-en/html/fblangref25-structure-identifiers.html
			return !IsReserved(name) &&
				name[0] >= 'A' && name[0] <= 'Z' &&
				name.All(c =>
					(c >= 'A' && c <= 'Z') ||
					(c >= '0' && c <= '9') ||
					c == '$' ||
					c == '_');
		}

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryFieldAlias :
				case ConvertType.NameToQueryTableAlias :
				case ConvertType.NameToQueryField      :
				case ConvertType.NameToQueryTable      :
				case ConvertType.SequenceName          :
					if (FirebirdConfiguration.IdentifierQuoteMode == FirebirdIdentifierQuoteMode.Quote ||
					   (FirebirdConfiguration.IdentifierQuoteMode == FirebirdIdentifierQuoteMode.Auto && !IsValidIdentifier(value)))
					{
						// I wonder what to do if identifier has " in name?
						return sb.Append('"').Append(value).Append('"');
					}

					break;

				case ConvertType.NameToQueryParameter  :
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter  :
					return sb.Append('@').Append(value);

				case ConvertType.SprocParameterToName  :
					return value.Length > 0 && value[0] == '@'
						? sb.Append(value.Substring(1))
						: sb.Append(value);
			}

			return sb.Append(value);
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, "FROM rdb$database");
		}

		protected override void BuildCreateTableNullAttribute(SqlField field, DefaultNullable defaultNullable)
		{
			if (!field.CanBeNull)
				StringBuilder.Append("NOT NULL");
		}

		SqlField? _identityField;

		public override int CommandCount(SqlStatement statement)
		{
			switch (statement)
			{
				case SqlTruncateTableStatement truncate:
					return truncate.ResetIdentity && truncate.Table!.IdentityFields.Count > 0 ? 2 : 1;
			}

			return base.CommandCount(statement);
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			var identityField = dropTable.Table!.IdentityFields.FirstOrDefault();

			if (identityField == null && dropTable.Table.TableOptions.HasDropIfExists() == false && dropTable.Table.TableOptions.HasIsTemporary() == false)
			{
				base.BuildDropTableStatement(dropTable);
				return;
			}

			// implementation use following approach: http://www.firebirdfaq.org/faq69/
			StringBuilder
				.AppendLine("EXECUTE BLOCK AS BEGIN");

			Indent++;

			if (identityField != null)
			{
				BuildDropWithSchemaCheck("TRIGGER"  , "rdb$triggers"  , "rdb$trigger_name"  , "TIDENTITY_" + dropTable.Table.PhysicalName);
				BuildDropWithSchemaCheck("GENERATOR", "rdb$generators", "rdb$generator_name", "GIDENTITY_" + dropTable.Table.PhysicalName);
			}

			BuildDropWithSchemaCheck("TABLE", "rdb$relations", "rdb$relation_name", dropTable.Table.PhysicalName!);

			Indent--;

			StringBuilder
				.AppendLine("END");

			void BuildDropWithSchemaCheck(string objectName, string schemaTable, string nameColumn, string identifier)
			{
				if (dropTable.Table.TableOptions.HasDropIfExists() || dropTable.Table.TableOptions.HasIsTemporary())
				{
					AppendIndent().AppendFormat("IF (EXISTS(SELECT 1 FROM {0} WHERE {1} = ", schemaTable, nameColumn);

					var identifierValue = identifier;

					// if identifier is not quoted, it must be converted to upper case to match record in rdb$relation_name
					if (FirebirdConfiguration.IdentifierQuoteMode == FirebirdIdentifierQuoteMode.None ||
						FirebirdConfiguration.IdentifierQuoteMode == FirebirdIdentifierQuoteMode.Auto && IsValidIdentifier(identifierValue))
						identifierValue = identifierValue.ToUpper();

					BuildValue(null, identifierValue);

					StringBuilder
						.AppendLine(")) THEN");

					Indent++;
				}

				AppendIndent().Append("EXECUTE STATEMENT ");

				var dropCommand = new StringBuilder();

				dropCommand
					.Append("DROP ")
					.Append(objectName)
					.Append(" ");

				Convert(dropCommand, identifier, ConvertType.NameToQueryTable);

				BuildValue(null, dropCommand.ToString());

				StringBuilder.AppendLine(";");

				if (dropTable.Table.TableOptions.HasDropIfExists() || dropTable.Table.TableOptions.HasIsTemporary())
					Indent--;
			}
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			// should we introduce new convertion types like NameToGeneratorName/NameToTriggerName?
			switch (Statement)
			{
				case SqlTruncateTableStatement truncate:
					StringBuilder.Append("SET GENERATOR ");
					Convert(StringBuilder, "GIDENTITY_" + truncate.Table!.PhysicalName, ConvertType.NameToQueryTable);
					StringBuilder.AppendLine(" TO 0");
					break;
			}
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string? server, string? database, string? schema, string table, TableOptions tableOptions)
		{
			return sb.Append(table);
		}

		protected override string? GetProviderTypeName(IDbDataParameter parameter)
		{
			if (_provider != null)
			{
				var param = _provider.TryGetProviderParameter(parameter, MappingSchema);
				if (param != null)
					return _provider.Adapter.GetDbType(param).ToString();
			}

			return base.GetProviderTypeName(parameter);
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
			if (createTable.StatementHeader == null)
			{
				_identityField = createTable.Table!.IdentityFields.FirstOrDefault();

				var checkExistence = createTable.Table.TableOptions.HasCreateIfNotExists() || createTable.Table.TableOptions.HasIsTemporary();

				if (_identityField != null || checkExistence)
				{
					StringBuilder
						.AppendLine("EXECUTE BLOCK AS BEGIN");

					Indent++;

					if (checkExistence)
					{
						AppendIndent().Append("IF (NOT EXISTS(SELECT 1 FROM rdb$relations WHERE rdb$relation_name = ");

						var identifierValue = createTable.Table.PhysicalName!;

						// if identifier is not quoted, it must be converted to upper case to match record in rdb$relation_name
						if (FirebirdConfiguration.IdentifierQuoteMode == FirebirdIdentifierQuoteMode.None ||
							FirebirdConfiguration.IdentifierQuoteMode == FirebirdIdentifierQuoteMode.Auto && IsValidIdentifier(identifierValue))
							identifierValue = identifierValue.ToUpper();

						BuildValue(null, identifierValue);

						StringBuilder
							.AppendLine(")) THEN");

						Indent++;
					}

					AppendIndent().AppendLine("EXECUTE STATEMENT '");

					Indent++;
				}
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

				var checkExistence = table.TableOptions.HasCreateIfNotExists() || table.TableOptions.HasIsTemporary();

				if (_identityField != null || checkExistence)
				{
					var identifierValue = createTable.Table.PhysicalName!;

					// if identifier is not quoted, it must be converted to upper case to match record in rdb$relation_name
					if (FirebirdConfiguration.IdentifierQuoteMode == FirebirdIdentifierQuoteMode.None ||
						FirebirdConfiguration.IdentifierQuoteMode == FirebirdIdentifierQuoteMode.Auto && IsValidIdentifier(identifierValue))
						identifierValue = identifierValue.ToUpper();

					Indent--;

					AppendIndent()
						.AppendLine("';");

					if (checkExistence)
					{
						Indent--;

						AppendIndent()
							.Append("IF (NOT EXISTS(SELECT 1 FROM rdb$generators WHERE rdb$generator_name = '")
							.Append("GIDENTITY_")
							.Append(identifierValue)
							.AppendLine("')) THEN")
							;

						Indent++;

						AddGenerator();

						Indent--;

						AppendIndent()
							.Append("IF (NOT EXISTS(SELECT 1 FROM rdb$triggers WHERE rdb$trigger_name = '")
							.Append("TIDENTITY_")
							.Append(identifierValue)
							.AppendLine("')) THEN")
							;

						Indent++;

						AddTrigger();

						Indent--;
					}
					else
					{
						AddGenerator();
						AddTrigger();
					}

					void AddGenerator()
					{
						AppendIndent()
							.AppendLine("EXECUTE STATEMENT '");

						Indent++;

						AppendIndent().Append("CREATE GENERATOR ");
						Convert(StringBuilder, "GIDENTITY_" + createTable.Table!.PhysicalName, ConvertType.NameToQueryTable);
						StringBuilder.AppendLine();

						Indent--;

						AppendIndent()
							.AppendLine("';");
					}

					void AddTrigger()
					{
						AppendIndent().AppendLine("EXECUTE STATEMENT '");

						Indent++;

						AppendIndent().Append("CREATE TRIGGER ");
						Convert(StringBuilder, "TIDENTITY_" + createTable.Table!.PhysicalName, ConvertType.NameToQueryTable);
						StringBuilder .Append(" FOR ");
						Convert(StringBuilder, createTable.Table.PhysicalName!, ConvertType.NameToQueryTable);
						StringBuilder .AppendLine();
						AppendIndent().AppendLine("BEFORE INSERT POSITION 0");
						AppendIndent().AppendLine("AS BEGIN");
						AppendIndent().Append("\tNEW.");
						Convert(StringBuilder, _identityField!.PhysicalName, ConvertType.NameToQueryField);
						StringBuilder. Append(" = GEN_ID(");
						Convert(StringBuilder, "GIDENTITY_" + createTable.Table.PhysicalName, ConvertType.NameToQueryTable);
						StringBuilder. AppendLine(", 1);");
						AppendIndent().AppendLine("END");

						Indent--;

						AppendIndent()
							.AppendLine("';");
					}

					Indent--;

					StringBuilder
						.AppendLine("END");
				}
			}
		}
	}
}
