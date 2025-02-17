using System;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Common.Internal;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

#region ReSharper disable
// ReSharper disable SuggestUseVarKeywordEverywhere
// ReSharper disable SuggestUseVarKeywordEvident
#endregion

namespace LinqToDB.DataProvider.Firebird
{
	public partial class FirebirdSqlBuilder : BasicSqlBuilder<FirebirdOptions>
	{
		public override bool CteFirst => false;

		public FirebirdSqlBuilder(IDataProvider provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected FirebirdSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new FirebirdSqlBuilder(this);
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

		protected override void BuildSkipFirst(SelectQuery selectQuery)
		{
			if (Statement.QueryType is not QueryType.Update and not QueryType.Delete)
			{
				base.BuildSkipFirst(selectQuery);
			}
		}

		protected override void BuildOffsetLimit(SelectQuery selectQuery)
		{
			if (Statement.QueryType is QueryType.Update or QueryType.Delete)
			{
				SqlOptimizer.ConvertSkipTake(NullabilityContext, MappingSchema, DataOptions, selectQuery, OptimizationContext, out var takeExpr, out var skipExpr);

				if (takeExpr != null)
				{
					AppendIndent();

					if (skipExpr != null)
					{
						StringBuilder.AppendFormat(CultureInfo.InvariantCulture, "ROWS {0} + 1 TO {0} + {1}", WithStringBuilderBuildExpression(skipExpr), WithStringBuilderBuildExpression(takeExpr));
					}
					else
					{
						StringBuilder.AppendFormat(CultureInfo.InvariantCulture, "ROWS {0}", WithStringBuilderBuildExpression(takeExpr));
					}

					StringBuilder.AppendLine();
				}
			}
			else
			{
				base.BuildOffsetLimit(selectQuery);
			}
		}

		protected override bool   SkipFirst                     => false;
		protected override string SkipFormat                    => "SKIP {0}";
		protected override bool   IsRecursiveCteKeywordRequired => true;

		protected override string FirstFormat(SelectQuery selectQuery)
		{
			return "FIRST {0}";
		}

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			var identityField = insertClause.Into!.GetIdentityField();

			if (identityField == null)
				throw new LinqToDBException($"Identity field must be defined for '{insertClause.Into.NameForLogging}'.");

			AppendIndent().AppendLine("RETURNING");
			AppendIndent().Append('\t');
			BuildExpression(identityField, false, true);
		}

		public override ISqlExpression? GetIdentityExpression(SqlTable table)
		{
			if (!table.SequenceAttributes.IsNullOrEmpty())
				return new SqlExpression("GEN_ID(" + ConvertInline(table.SequenceAttributes[0].SequenceName, ConvertType.SequenceName) + ", 1)", Precedence.Primary);

			return base.GetIdentityExpression(table);
		}

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.DataType)
			{
				// FB4+ types:
				case DataType.Int128        : StringBuilder.Append("INT128");                             break;
				case DataType.TimeTZ        : StringBuilder.Append("TIME WITH TIME ZONE");                break;
				case DataType.DateTimeOffset: StringBuilder.Append("TIMESTAMP WITH TIME ZONE");           break;
				case DataType.DecFloat      :
					StringBuilder.Append("DECFLOAT");
					if (type.Precision != null && type.Precision <= 16)
						StringBuilder.Append("(16)");
					break;

				case DataType.Decimal       :
					base.BuildDataTypeFromDataType(type.Precision > 18 ? type.WithPrecision(10) : type, forCreateTable, canBeNull);
					break;
				case DataType.SByte         :
				case DataType.Byte          : StringBuilder.Append("SmallInt");                           break;
				case DataType.Money         : StringBuilder.AppendFormat(CultureInfo.InvariantCulture, "Decimal(18{0}4)", InlineComma); break;
				case DataType.SmallMoney    : StringBuilder.AppendFormat(CultureInfo.InvariantCulture, "Decimal(10{0}4)", InlineComma); break;
				case DataType.DateTime2     :
				case DataType.SmallDateTime :
				case DataType.DateTime      : StringBuilder.Append("TimeStamp");                          break;
				case DataType.VarChar       :
				case DataType.NVarChar      :
					StringBuilder.Append("VarChar");

					// 10921 is implementation limit for UNICODE_FSS encoding
					// use 255 as default length, because FB have 64k row-size limits
					// also it is not good to depend on implementation limits
					if (type.Length == null || type.Length < 1)
						StringBuilder.Append("(255)");
					else
						StringBuilder.Append(CultureInfo.InvariantCulture, $"({type.Length})");

					// type for UUID, e.g. see https://firebirdsql.org/refdocs/langrefupd25-intfunc-gen_uuid.html
					StringBuilder.Append(" CHARACTER SET UNICODE_FSS");
																										  break;

				case DataType.Guid          : StringBuilder.Append("CHAR(16) CHARACTER SET OCTETS");      break;
				case DataType.NChar         :
				case DataType.Char          :
					if (type.SystemType == typeof(Guid) || type.SystemType == typeof(Guid?))
						StringBuilder.Append("CHAR(38)");
					else
						base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
					break;

				case DataType.Binary when type.Length == null || type.Length < 1:
					StringBuilder.Append("CHAR CHARACTER SET OCTETS");
					break;

				case DataType.Binary:
					StringBuilder.Append(CultureInfo.InvariantCulture, $"CHAR({type.Length}) CHARACTER SET OCTETS");
					break;

				case DataType.VarBinary when type.Length == null || type.Length > 32_765:
					StringBuilder.Append("BLOB");
					break;

				case DataType.VarBinary:
					StringBuilder.Append(CultureInfo.InvariantCulture, $"VARCHAR({type.Length}) CHARACTER SET OCTETS");
					break;

				case DataType.Boolean       : StringBuilder.Append("BOOLEAN");                            break;
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

		protected override void BuildParameter(SqlParameter parameter)
		{
			if (parameter.NeedsCast && BuildStep != Step.TypedExpression)
			{
				var paramValue = parameter.GetParameterValue(OptimizationContext.EvaluationContext.ParameterValues);

				var dbDataType = paramValue.DbDataType;

				if (dbDataType.DataType == DataType.Undefined)
				{
					// TODO: We should avoid such tricks, proper TypeMapping required
					dbDataType = MappingSchema.GetDataType(dbDataType.SystemType).Type;
				}

				// Same code in DB2 provider
				if (paramValue.ProviderValue is byte[] bytes)
				{
					dbDataType = dbDataType.WithLength(bytes.Length);
				}
				else if (paramValue.ProviderValue is string str)
				{
					dbDataType = dbDataType.WithLength(str.Length);
				}
				else if (paramValue.ProviderValue is decimal d)
				{
					if (dbDataType.Precision == null)
						dbDataType = dbDataType.WithPrecision(DecimalHelper.GetPrecision(d));
					if (dbDataType.Scale == null)
						dbDataType = dbDataType.WithScale(DecimalHelper.GetScale(d));
				}

				// TODO: temporary guard against cast to unknown type (Variant)
				if (dbDataType.DataType == DataType.Undefined)
				{
					base.BuildParameter(parameter);
					return;
				}

				var saveStep = BuildStep;
				BuildStep = Step.TypedExpression;
				BuildTypedExpression(dbDataType, parameter);
				BuildStep = saveStep;

				return;
			}

			base.BuildParameter(parameter);
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
					AppendIndent().Append(CultureInfo.InvariantCulture, $"IF (EXISTS(SELECT 1 FROM {schemaTable} WHERE {nameColumn} = ");

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

		public override StringBuilder BuildObjectName(StringBuilder sb, SqlObjectName name, ConvertType objectType, bool escape, TableOptions tableOptions, bool withoutSuffix)
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

		protected override string GetPhysicalTableName(ISqlTableSource table, string? alias,
			bool ignoreTableExpression = false, string? defaultDatabaseName = null, bool withoutSuffix = false)
		{
			// for parameter-less table function skip argument list generation
			if (table is SqlTable tbl
				&& tbl.SqlTableType == SqlTableType.Function
				&& (tbl.TableArguments == null || tbl.TableArguments.Length == 0))
			{
				var tableName = tbl.TableName;
				if (tableName.Database == null && defaultDatabaseName != null)
					tableName = tableName with { Database = defaultDatabaseName };

				using var sb = Pools.StringBuilder.Allocate();

				BuildObjectName(sb.Value, tableName, ConvertType.NameToProcedure, true, tbl.TableOptions, withoutSuffix: withoutSuffix);

				return sb.Value.ToString();
			}

			return base.GetPhysicalTableName(table, alias, ignoreTableExpression: ignoreTableExpression, defaultDatabaseName: defaultDatabaseName, withoutSuffix: withoutSuffix);
		}

		// FB 2.5 need to use small values to avoid error due to bad row size calculation
		// resulting it being bigger than that limit (64Kb)
		// limit is the same for newer versions, but only FB 2.5 fails
		protected virtual int NullCharSize    => 1;
		protected virtual int UnknownCharSize => 8191;

		protected override void BuildTypedExpression(DbDataType dataType, ISqlExpression value)
		{
			if (dataType is { DbType: null, DataType: DataType.NVarChar or DataType.NChar })
			{
				object? providerValue = null;
				var     typeRequired  = false;

				var isClientValue = false;
				switch (value)
				{
					case SqlValue sqlValue:
						providerValue = sqlValue.Value;
						isClientValue = true;
						break;
					case SqlParameter param:
					{
						typeRequired = true;
						var paramValue = param.GetParameterValue(OptimizationContext.EvaluationContext.ParameterValues);
						providerValue = paramValue.ProviderValue;
						isClientValue = true;
						break;
					}
				}

				var length = providerValue switch
				{
					string strValue => Encoding.UTF8.GetByteCount(strValue),
					char charValue => Encoding.UTF8.GetByteCount(new[] { charValue }),
					null when isClientValue => NullCharSize,
					_ => -1
				};

				if (length == 0)
					length = 1;

				typeRequired = typeRequired || length > 0;

				if (typeRequired && length < 0)
				{
					length = UnknownCharSize;
				}

				if (typeRequired)
					StringBuilder.Append("CAST(");

				BuildExpression(value);

				if (typeRequired)
				{
					if (dataType.DataType  == DataType.NChar)
						StringBuilder.Append(CultureInfo.InvariantCulture, $" AS CHAR({length}))");
					else
						StringBuilder.Append(CultureInfo.InvariantCulture, $" AS VARCHAR({length}))");
				}
			}
			else
				base.BuildTypedExpression(dataType, value);
		}
	}
}
