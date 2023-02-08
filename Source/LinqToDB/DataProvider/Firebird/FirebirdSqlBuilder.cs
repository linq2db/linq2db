using System;
using System.Data.Common;
using System.Text;

#region ReSharper disable
// ReSharper disable SuggestUseVarKeywordEverywhere
// ReSharper disable SuggestUseVarKeywordEvident
#endregion

namespace LinqToDB.DataProvider.Firebird
{
	using Common;
	using Common.Internal;
	using Mapping;
	using SqlQuery;
	using SqlProvider;

	public partial class FirebirdSqlBuilder : BasicSqlBuilder<FirebirdOptions>
	{
		public override bool CteFirst => false;

		public FirebirdSqlBuilder(IDataProvider provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		FirebirdSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new FirebirdSqlBuilder(this);
		}

		protected override void BuildSelectClause(NullabilityContext nullability, SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count == 0)
			{
				AppendIndent();
				StringBuilder.Append("SELECT").AppendLine();
				BuildColumns(nullability, selectQuery);
				AppendIndent();
				StringBuilder.Append("FROM rdb$database").AppendLine();
			}
			else if (selectQuery.Select.IsDistinct)
			{
				AppendIndent();
				StringBuilder.Append("SELECT");
				BuildSkipFirst(nullability, selectQuery);
				StringBuilder.Append(" DISTINCT");
				StringBuilder.AppendLine();
				BuildColumns(nullability, selectQuery);
			}
			else
				base.BuildSelectClause(nullability, selectQuery);
		}

		protected override bool   SkipFirst                     => false;
		protected override string SkipFormat                    => "SKIP {0}";
		protected override bool   IsRecursiveCteKeywordRequired => true;

		protected override string FirstFormat(SelectQuery selectQuery)
		{
			return "FIRST {0}";
		}

		protected override void BuildGetIdentity(NullabilityContext nullability, SqlInsertClause insertClause)
		{
			var identityField = insertClause.Into!.GetIdentityField();

			if (identityField == null)
				throw new SqlException("Identity field must be defined for '{0}'.", insertClause.Into.NameForLogging);

			AppendIndent().AppendLine("RETURNING");
			AppendIndent().Append('\t');
			BuildExpression(nullability, identityField, false, true);
		}

		public override ISqlExpression? GetIdentityExpression(SqlTable table)
		{
			if (!table.SequenceAttributes.IsNullOrEmpty())
				return new SqlExpression("GEN_ID(" + ConvertInline(table.SequenceAttributes[0].SequenceName, ConvertType.SequenceName) + ", 1)", Precedence.Primary);

			return base.GetIdentityExpression(table);
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.Type.DataType)
			{
				// FB4+ types:
				case DataType.Int128        : StringBuilder.Append("INT128");                             break;
				case DataType.TimeTZ        : StringBuilder.Append("TIME WITH TIME ZONE");                break;
				case DataType.DateTimeOffset: StringBuilder.Append("TIMESTAMP WITH TIME ZONE");           break;
				case DataType.DecFloat      :
					StringBuilder.Append("DECFLOAT");
					if (type.Type.Precision != null && type.Type.Precision <= 16)
						StringBuilder.Append("(16)");
					break;

				case DataType.Decimal       :
					base.BuildDataTypeFromDataType(type.Type.Precision > 18 ? new SqlDataType(type.Type.DataType, type.Type.SystemType, null, 18, type.Type.Scale, type.Type.DbType) : type, forCreateTable, canBeNull);
					break;
				case DataType.SByte         :
				case DataType.Byte          : StringBuilder.Append("SmallInt");                           break;
				case DataType.Money         : StringBuilder.AppendFormat("Decimal(18{0}4)", InlineComma); break;
				case DataType.SmallMoney    : StringBuilder.AppendFormat("Decimal(10{0}4)", InlineComma); break;
				case DataType.DateTime2     :
				case DataType.SmallDateTime :
				case DataType.DateTime      : StringBuilder.Append("TimeStamp");                          break;
				case DataType.VarChar       :
				case DataType.NVarChar      :
					StringBuilder.Append("VarChar");

					// 10921 is implementation limit for UNICODE_FSS encoding
					// use 255 as default length, because FB have 64k row-size limits
					// also it is not good to depend on implementation limits
					if (type.Type.Length == null || type.Type.Length < 1)
						StringBuilder.Append("(255)");
					else
						StringBuilder.Append($"({type.Type.Length})");

					// type for UUID, e.g. see https://firebirdsql.org/refdocs/langrefupd25-intfunc-gen_uuid.html
					StringBuilder.Append(" CHARACTER SET UNICODE_FSS");
					                                                                                      break;

				case DataType.Guid          : StringBuilder.Append("CHAR(16) CHARACTER SET OCTETS");      break;
				case DataType.NChar         :
				case DataType.Char          :
					if (type.Type.SystemType == typeof(Guid) || type.Type.SystemType == typeof(Guid?))
						StringBuilder.Append("CHAR(38)");
					else
						base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
					break;

				case DataType.VarBinary     : StringBuilder.Append("BLOB");                               break;
				// BOOLEAN type available since FB 3.0, but FirebirdDataProvider.SetParameter converts boolean to '1'/'0'
				// so for now we will use type, compatible with SetParameter by default
				case DataType.Boolean       : StringBuilder.Append("CHAR");                               break;
				default: base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);                 break;
			}
		}

		protected sealed override bool IsReserved(string word)
		{
			return ReservedWords.IsReserved(word, ProviderName.Firebird);
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
				case ConvertType.NameToProcedure       :
				case ConvertType.NameToPackage         :
				case ConvertType.SequenceName          :
					if (ProviderOptions.IdentifierQuoteMode == FirebirdIdentifierQuoteMode.Quote ||
					   (ProviderOptions.IdentifierQuoteMode == FirebirdIdentifierQuoteMode.Auto && !IsValidIdentifier(value)))
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
			var identityField = dropTable.Table.IdentityFields.Count > 0 ? dropTable.Table.IdentityFields[0] : null;

			if (identityField == null && dropTable.Table.TableOptions.HasDropIfExists() == false && dropTable.Table.TableOptions.HasIsTemporary() == false)
			{
				base.BuildDropTableStatement(dropTable);
				return;
			}

			BuildTag(dropTable);

			// implementation use following approach: http://www.firebirdfaq.org/faq69/
			StringBuilder
				.AppendLine("EXECUTE BLOCK AS BEGIN");

			Indent++;

			if (identityField != null)
			{
				BuildDropWithSchemaCheck("TRIGGER"  , "rdb$triggers"  , "rdb$trigger_name"  , "TIDENTITY_" + dropTable.Table.TableName.Name);
				BuildDropWithSchemaCheck("GENERATOR", "rdb$generators", "rdb$generator_name", "GIDENTITY_" + dropTable.Table.TableName.Name);
			}

			BuildDropWithSchemaCheck("TABLE", "rdb$relations", "rdb$relation_name", dropTable.Table.TableName.Name);

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
					if (ProviderOptions.IdentifierQuoteMode == FirebirdIdentifierQuoteMode.None ||
						ProviderOptions.IdentifierQuoteMode == FirebirdIdentifierQuoteMode.Auto && IsValidIdentifier(identifierValue))
						identifierValue = identifierValue.ToUpperInvariant();

					BuildValue(null, identifierValue);

					StringBuilder
						.AppendLine(")) THEN");

					Indent++;
				}

				AppendIndent().Append("EXECUTE STATEMENT ");

				using var dropCommand = Pools.StringBuilder.Allocate();

				dropCommand.Value
					.Append("DROP ")
					.Append(objectName)
					.Append(' ');

				Convert(dropCommand.Value, identifier, ConvertType.NameToQueryTable);

				BuildValue(null, dropCommand.Value.ToString());

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
					Convert(StringBuilder, "GIDENTITY_" + truncate.Table!.TableName.Name, ConvertType.NameToQueryTable);
					StringBuilder.AppendLine(" TO 0");
					break;
			}
		}

		protected override ISqlExpression WrapColumnExpression(ISqlExpression expr)
		{
			expr = base.WrapColumnExpression(expr);
			if (expr is SqlParameter param)
			{
				return new SqlFunction(param.Type.SystemType, "Convert", false, new SqlDataType(param.Type), param);
			}

			return expr;
		}

		public override StringBuilder BuildObjectName(StringBuilder sb, SqlObjectName name, ConvertType objectType, bool escape, TableOptions tableOptions)
		{
			if (name.Package != null)
			{
				(escape ? Convert(sb, name.Package, ConvertType.NameToPackage) : sb.Append(name.Package))
					.Append('.');
			}

			return escape ? Convert(sb, name.Name, objectType) : sb.Append(name.Name);
		}

		protected override string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (DataProvider is FirebirdDataProvider provider)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
					return provider.Adapter.GetDbType(param).ToString();
			}

			return base.GetProviderTypeName(dataContext, parameter);
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
				_identityField = createTable.Table.IdentityFields.Count > 0 ? createTable.Table.IdentityFields[0] : null;

				var checkExistence = createTable.Table.TableOptions.HasCreateIfNotExists() || createTable.Table.TableOptions.HasIsTemporary();

				if (_identityField != null || checkExistence)
				{
					StringBuilder
						.AppendLine("EXECUTE BLOCK AS BEGIN");

					Indent++;

					if (checkExistence)
					{
						AppendIndent().Append("IF (NOT EXISTS(SELECT 1 FROM rdb$relations WHERE rdb$relation_name = ");

						var identifierValue = createTable.Table.TableName.Name;

						// if identifier is not quoted, it must be converted to upper case to match record in rdb$relation_name
						if (ProviderOptions.IdentifierQuoteMode == FirebirdIdentifierQuoteMode.None ||
							ProviderOptions.IdentifierQuoteMode == FirebirdIdentifierQuoteMode.Auto && IsValidIdentifier(identifierValue))
							identifierValue = identifierValue.ToUpperInvariant();

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
					var identifierValue = createTable.Table.TableName.Name;

					// if identifier is not quoted, it must be converted to upper case to match record in rdb$relation_name
					if (ProviderOptions.IdentifierQuoteMode == FirebirdIdentifierQuoteMode.None ||
						ProviderOptions.IdentifierQuoteMode == FirebirdIdentifierQuoteMode.Auto && IsValidIdentifier(identifierValue))
						identifierValue = identifierValue.ToUpperInvariant();

					Indent--;

					AppendIndent()
						.AppendLine("';");

					if (_identityField != null)
					{
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
							Convert(StringBuilder, "GIDENTITY_" + createTable.Table.TableName.Name, ConvertType.NameToQueryTable);
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
							Convert(StringBuilder, "TIDENTITY_" + createTable.Table.TableName.Name, ConvertType.NameToQueryTable);
							StringBuilder .Append(" FOR ");
							Convert(StringBuilder, createTable.Table.TableName.Name, ConvertType.NameToQueryTable);
							StringBuilder .AppendLine();
							AppendIndent().AppendLine("BEFORE INSERT POSITION 0");
							AppendIndent().AppendLine("AS BEGIN");
							AppendIndent().Append("\tNEW.");
							Convert(StringBuilder, _identityField!.PhysicalName, ConvertType.NameToQueryField);
							StringBuilder. Append(" = GEN_ID(");
							Convert(StringBuilder, "GIDENTITY_" + createTable.Table.TableName.Name, ConvertType.NameToQueryTable);
							StringBuilder. AppendLine(", 1);");
							AppendIndent().AppendLine("END");

							Indent--;

							AppendIndent()
								.AppendLine("';");
						}
					}

					Indent--;

					StringBuilder
						.AppendLine("END");
				}
			}
		}
	}
}
