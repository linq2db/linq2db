﻿using System.Data;
using System.Linq;

#region ReSharper disable
// ReSharper disable SuggestUseVarKeywordEverywhere
// ReSharper disable SuggestUseVarKeywordEvident
#endregion

namespace LinqToDB.DataProvider.Firebird
{
	using Common;
	using SqlQuery;
	using SqlProvider;
	using System.Text;
	using LinqToDB.Mapping;

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
				return new SqlExpression("GEN_ID(" + table.SequenceAttributes![0].SequenceName + ", 1)", Precedence.Primary);

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
				case DataType.Byte          : StringBuilder.Append("SmallInt");        break;
				case DataType.Money         : StringBuilder.Append("Decimal(18,4)");   break;
				case DataType.SmallMoney    : StringBuilder.Append("Decimal(10,4)");   break;
				case DataType.DateTime2     :
				case DataType.SmallDateTime :
				case DataType.DateTime      : StringBuilder.Append("TimeStamp");       break;
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
				case DataType.VarBinary     : StringBuilder.Append("BLOB");            break;
				// BOOLEAN type available since FB 3.0, but FirebirdDataProvider.SetParameter converts boolean to '1'/'0'
				// so for now we will use type, compatible with SetParameter by default
				case DataType.Boolean       : StringBuilder.Append("CHAR");            break;
				default: base.BuildDataTypeFromDataType(type, forCreateTable);         break;
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
		/// Specifies how identifiers like table and field names should be quoted.
		/// </summary>
		/// <remarks>
		/// Default value: <see cref="FirebirdIdentifierQuoteMode.Auto"/>.
		/// </remarks>
		public static FirebirdIdentifierQuoteMode IdentifierQuoteMode = FirebirdIdentifierQuoteMode.Auto;

		/// <summary>
		/// Check if identifier is valid without quotation. Expects non-zero length string as input.
		/// </summary>
		private bool IsValidIdentifier(string name)
		{
			// https://firebirdsql.org/file/documentation/reference_manuals/fblangref25-en/html/fblangref25-structure-identifiers.html
			return !IsReserved(name) &&
				((name[0] >= 'a' && name[0] <= 'z') || (name[0] >= 'A' && name[0] <= 'Z')) &&
				name.All(c =>
					(c >= 'a' && c <= 'z') ||
					(c >= 'A' && c <= 'Z') ||
					(c >= '0' && c <= '9') ||
					c == '$' ||
					c == '_');
		}

		public override string Convert(string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryFieldAlias :
				case ConvertType.NameToQueryField      :
				case ConvertType.NameToQueryTable      :
					if (IdentifierQuoteMode == FirebirdIdentifierQuoteMode.Quote ||
					   (IdentifierQuoteMode == FirebirdIdentifierQuoteMode.Auto && !IsValidIdentifier(value)))
					{
						// I wonder what to do if identifier has " in name?
						return '"' + value + '"';
					}

					break;

				case ConvertType.NameToQueryParameter  :
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter  :
					return "@" + value;

				case ConvertType.SprocParameterToName  :
					return value.Length > 0 && value[0] == '@' ? value.Substring(1) : value;
			}

			return value;
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
					return truncate.ResetIdentity && truncate.Table!.Fields.Values.Any(f => f.IsIdentity) ? 2 : 1;

				case SqlCreateTableStatement createTable:
					_identityField = createTable.Table!.Fields.Values.FirstOrDefault(f => f.IsIdentity);
					if (_identityField != null)
						return 3;
					break;
			}

			return base.CommandCount(statement);
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			var identityField = dropTable.Table!.Fields.Values.FirstOrDefault(f => f.IsIdentity);

			if (identityField == null && dropTable.IfExists == false)
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
				if (dropTable.IfExists)
				{
					AppendIndent().AppendFormat("IF (EXISTS(SELECT 1 FROM {0} WHERE {1} = ", schemaTable, nameColumn);

					var identifierValue = identifier;

					// if identifier is not quoted, it must be converted to upper case to match record in rdb$relation_name
					if (IdentifierQuoteMode == FirebirdIdentifierQuoteMode.None ||
					    IdentifierQuoteMode == FirebirdIdentifierQuoteMode.Auto && IsValidIdentifier(identifierValue))
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
					.Append(" ")
					.Append(Convert(identifier, ConvertType.NameToQueryTable));

				BuildValue(null, dropCommand.ToString());

				StringBuilder.AppendLine(";");

				if (dropTable.IfExists)
					Indent--;
			}
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			// should we introduce new convertion types like NameToGeneratorName/NameToTriggerName?
			switch (Statement)
			{
				case SqlTruncateTableStatement truncate:
					StringBuilder
						.Append("SET GENERATOR ")
						.Append(Convert("GIDENTITY_" + truncate.Table!.PhysicalName, ConvertType.NameToQueryTable))
						.AppendLine(" TO 0")
						;
					break;

				case SqlCreateTableStatement createTable:
					{
						if (commandNumber == 1)
						{
							StringBuilder
								.Append("CREATE GENERATOR ")
								.Append(Convert("GIDENTITY_" + createTable.Table!.PhysicalName, ConvertType.NameToQueryTable))
								.AppendLine();
						}
						else
						{
							StringBuilder
								.AppendFormat(
									"CREATE TRIGGER {0} FOR {1}",
									Convert("TIDENTITY_" + createTable.Table!.PhysicalName, ConvertType.NameToQueryTable),
									Convert(createTable.Table.PhysicalName!, ConvertType.NameToQueryTable))
								.AppendLine  ()
								.AppendLine  ("BEFORE INSERT POSITION 0")
								.AppendLine  ("AS BEGIN")
								.AppendFormat(
									"\tNEW.{0} = GEN_ID({1}, 1);",
									Convert(_identityField!.PhysicalName, ConvertType.NameToQueryField),
									Convert("GIDENTITY_" + createTable.Table.PhysicalName, ConvertType.NameToQueryTable))
								.AppendLine  ()
								.AppendLine  ("END");
						}

						break;
					}
			}
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string? server, string? database, string? schema, string table)
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
	}
}
