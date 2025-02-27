using System;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Oracle
{
	abstract partial class OracleSqlBuilderBase : BasicSqlBuilder<OracleOptions>
	{
		public override bool CteFirst => false;

		protected OracleSqlBuilderBase(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected OracleSqlBuilderBase(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override void BuildSelectClause(SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count == 0)
			{
				AppendIndent().Append("SELECT");
				StartStatementQueryExtensions(selectQuery);
				StringBuilder.AppendLine();
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
				throw new LinqToDBException($"Identity field must be defined for '{insertClause.Into.NameForLogging}'.");

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

		protected override bool ShouldBuildWhere(SelectQuery selectQuery, out SqlSearchCondition condition)
		{
			SqlOptimizer.ConvertSkipTake(NullabilityContext, MappingSchema, DataOptions, selectQuery, OptimizationContext, out var takeExpr, out var skipEpr);

			return
				base.ShouldBuildWhere(selectQuery, out condition) ||
				!NeedSkip(takeExpr, skipEpr) &&
				 NeedTake(takeExpr) &&
				selectQuery.OrderBy.IsEmpty && selectQuery.Having.IsEmpty;
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

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.DataType)
			{
				case DataType.Date           :
				case DataType.DateTime       : StringBuilder.Append("date");                      break;
				case DataType.DateTime2      :
					if (type.Precision == 6 || type.Precision == null)
						StringBuilder.Append("timestamp");
					else
						StringBuilder.Append(CultureInfo.InvariantCulture, $"timestamp({type.Precision})");
					break;
				case DataType.DateTimeOffset :
					if (type.Precision == 6 || type.Precision == null)
						StringBuilder.Append("timestamp with time zone");
					else
						StringBuilder.Append(CultureInfo.InvariantCulture, $"timestamp({type.Precision}) with time zone");
					break;
				case DataType.UInt32         :
				case DataType.Int64          : StringBuilder.Append("Number(19)");                break;
				case DataType.SByte          :
				case DataType.Byte           : StringBuilder.Append("Number(3)");                 break;
				case DataType.Money          : StringBuilder.Append("Number(19, 4)");             break;
				case DataType.SmallMoney     : StringBuilder.Append("Number(10, 4)");             break;
				case DataType.VarChar        :
					if (type.Length == null || type.Length > 4000 || type.Length < 1)
						StringBuilder.Append("VarChar(4000)");
					else
						StringBuilder.Append(CultureInfo.InvariantCulture, $"VarChar({type.Length})");
					break;
				case DataType.NVarChar       :
					if (type.Length == null || type.Length > 4000 || type.Length < 1)
						StringBuilder.Append("VarChar2(4000)");
					else
						StringBuilder.Append(CultureInfo.InvariantCulture, $"VarChar2({type.Length})");
					break;
				case DataType.Boolean        : StringBuilder.Append("Char(1)");                   break;
				case DataType.NText          : StringBuilder.Append("NClob");                     break;
				case DataType.Text           : StringBuilder.Append("Clob");                      break;
				case DataType.Guid           : StringBuilder.Append("Raw(16)");                   break;
				case DataType.Binary         :
				case DataType.VarBinary      :
					if (type.Length == null || type.Length == 0)
						StringBuilder.Append("BLOB");
					else
						StringBuilder.Append(CultureInfo.InvariantCulture, $"Raw({type.Length})");
					break;
				case DataType.Interval       : StringBuilder.Append("interval day (9) to second (9)");
					break;
				default: base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);         break;
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
			return !IsReserved(name)                                                                                               &&
				((ProviderOptions.DontEscapeLowercaseIdentifiers && name[0] is >= 'a' and <= 'z') || name[0] is >= 'A' and <= 'Z') &&
				name.All(c =>
					(ProviderOptions.DontEscapeLowercaseIdentifiers && c is >= 'a' and <= 'z') || c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '$' or '#' or '_');
		}

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter :
					return sb.Append(':').Append(value);
				// needs proper list of reserved words and name validation
				// something like we did for Firebird
				// right now reserved words list contains garbage
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryField     :
				case ConvertType.NameToQueryTable     :
				case ConvertType.NameToProcedure      :
				case ConvertType.NameToPackage        :
				case ConvertType.NameToServer         :
				case ConvertType.SequenceName         :
				case ConvertType.NameToSchema         :
				case ConvertType.TriggerName          :
					if (!IsValidIdentifier(value))
						return sb.Append('"').Append(value).Append('"');

					return sb.Append(value);
			}

			return sb.Append(value);
		}

		protected override StringBuilder BuildExpression(ISqlExpression expr,
			bool                                                        buildTableName,
			bool                                                        checkParentheses,
			string?                                                     alias,
			ref bool                                                    addAlias,
			bool                                                        throwExceptionIfTableNotFound = true)
		{
			return base.BuildExpression(expr,
				buildTableName && Statement.QueryType != QueryType.MultiInsert,
				checkParentheses,
				alias,
				ref addAlias,
				throwExceptionIfTableNotFound);
		}

		protected override void BuildExprExprPredicate(SqlPredicate.ExprExpr expr)
		{
			BuildExpression(GetPrecedence(expr), expr.Expr1);

			BuildExprExprPredicateOperator(expr);

			var exprPrecedence = GetPrecedence(expr);

			if (QueryHelper.UnwrapNullablity(expr.Expr2).ElementType == QueryElementType.SqlRow && expr.Operator != SqlPredicate.Operator.Overlaps)
			{
				// Oracle needs brackets around the right-hand side to disambiguate the syntax, e.g.:
				// (1, 2) = ( (3, 4) )

				exprPrecedence = int.MaxValue;
			}

			BuildExpression(exprPrecedence, expr.Expr2);
		}

		protected override void BuildIsDistinctPredicate(SqlPredicate.IsDistinct expr)
		{
			StringBuilder.Append("DECODE(");
			BuildExpression(Precedence.Unknown, expr.Expr1);
			StringBuilder.Append(", ");
			BuildExpression(Precedence.Unknown, expr.Expr2);
			StringBuilder
				.Append(", 0, 1) = ")
				.Append(expr.IsNot ? '0' : '1');
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, "FROM SYS.DUAL");
		}

		public override string GetReserveSequenceValuesSql(int count, string sequenceName)
		{
			return FormattableString.Invariant($"SELECT {ConvertInline(sequenceName, ConvertType.SequenceName)}.nextval ID from DUAL connect by level <= {count}");
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
			var nullability = NullabilityContext.NonQuery;

			var identityField = dropTable.Table!.IdentityFields.Count > 0 ? dropTable.Table!.IdentityFields[0] : null;

			if (identityField == null && dropTable.Table.TableOptions.HasDropIfExists() == false && dropTable.Table.TableOptions.HasIsTemporary() == false)
			{
				base.BuildDropTableStatement(dropTable);
			}
			else
			{
				BuildTag(dropTable);

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

					AppendSchemaPrefix(StringBuilder, dropTable.Table!.TableName.Schema);
					Convert(StringBuilder, MakeIdentityTriggerName(dropTable.Table.TableName.Name), ConvertType.TriggerName);

					StringBuilder
						.AppendLine("';")
						.Append("\tEXECUTE IMMEDIATE 'DROP SEQUENCE ");

					AppendSchemaPrefix(StringBuilder, dropTable.Table!.TableName.Schema);
					Convert(StringBuilder, MakeIdentitySequenceName(dropTable.Table.TableName.Name), ConvertType.SequenceName);

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

					AppendSchemaPrefix(StringBuilder, dropTable.Table!.TableName.Schema);
					Convert(StringBuilder, MakeIdentityTriggerName(dropTable.Table.TableName.Name), ConvertType.TriggerName);

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

					AppendSchemaPrefix(StringBuilder, dropTable.Table!.TableName.Schema);
					Convert(StringBuilder, MakeIdentitySequenceName(dropTable.Table.TableName.Name), ConvertType.SequenceName);

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
					var sequenceName = ConvertInline(MakeIdentitySequenceName(truncate.Table!.TableName.Name), ConvertType.SequenceName);
					StringBuilder
						.AppendFormat(
						CultureInfo.InvariantCulture,
						@"DECLARE
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
						AppendSchemaPrefix(StringBuilder, createTable.Table!.TableName.Schema);
						Convert(StringBuilder, MakeIdentitySequenceName(createTable.Table!.TableName.Name), ConvertType.SequenceName);
						StringBuilder
							.AppendLine();
					}
					else
					{
						StringBuilder
							.Append("CREATE OR REPLACE TRIGGER ");
						AppendSchemaPrefix(StringBuilder, createTable.Table!.TableName.Schema);
						Convert(StringBuilder, MakeIdentityTriggerName(createTable.Table!.TableName.Name), ConvertType.TriggerName);
						StringBuilder
							.AppendLine()
							.Append("BEFORE INSERT ON ");

						BuildPhysicalTable(createTable.Table, null);

						StringBuilder
							.AppendLine(" FOR EACH ROW")
							.AppendLine("BEGIN")
							.Append("\tSELECT ");
						AppendSchemaPrefix(StringBuilder, createTable.Table!.TableName.Schema);
						Convert(StringBuilder, MakeIdentitySequenceName(createTable.Table!.TableName.Name), ConvertType.SequenceName);
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

		public override StringBuilder BuildObjectName(StringBuilder sb, SqlObjectName name, ConvertType objectType, bool escape, TableOptions tableOptions, bool withoutSuffix)
		{
			if (name.Schema != null)
			{
				(escape ? Convert(sb, name.Schema, ConvertType.NameToSchema) : sb.Append(name.Schema))
					.Append('.');
			}

			if (name.Package != null)
			{
				(escape ? Convert(sb, name.Package, ConvertType.NameToPackage) : sb.Append(name.Package))
					.Append('.');
			}

			if (escape)
				Convert(sb, name.Name, objectType);
			else
				sb.Append(name.Name);

			if (name.Server != null && !withoutSuffix)
				BuildObjectNameSuffix(sb, name, escape);

			return sb;
		}

		protected override StringBuilder BuildObjectNameSuffix(StringBuilder sb, SqlObjectName name, bool escape)
		{
			if (name.Server != null)
			{
				sb.Append('@');
				if (escape)
					Convert(sb, name.Server, ConvertType.NameToServer);
				else
					sb.Append(name.Server);
			}

			return sb;
		}

		void AppendSchemaPrefix(StringBuilder sb, string? schema)
		{
			if (schema != null)
			{
				Convert(sb, schema, ConvertType.NameToSchema);
				sb.Append('.');
			}
		}

		protected override string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (DataProvider is OracleDataProvider provider)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
					return provider.Adapter.GetDbType(param).ToString();
			}

			return base.GetProviderTypeName(dataContext, parameter);
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

		#region Build MultiInsert

		protected override void BuildMultiInsertQuery(SqlMultiInsertStatement statement)
		{
			var nullability = NullabilityContext.NonQuery;
			BuildMultiInsertClause(statement);
			BuildSqlBuilder((SelectQuery)statement.Source.Source, Indent, skipAlias : false);
		}

		protected void BuildMultiInsertClause(SqlMultiInsertStatement statement)
		{
			StringBuilder.AppendLine(statement.InsertType == MultiInsertType.First ? "INSERT FIRST" : "INSERT ALL");

			Indent++;

			var nullability = NullabilityContext.NonQuery;

			if (statement.InsertType == MultiInsertType.Unconditional)
			{
				foreach (var insert in statement.Inserts)
					BuildInsertClause(statement, insert.Insert, "INTO ", appendTableName : true, addAlias : false);
			}
			else
			{
				foreach (var insert in statement.Inserts)
				{
					if (insert.When != null)
					{
						int length = StringBuilder.Append("WHEN ").Length;
						BuildSearchCondition(insert.When, wrapCondition : true);
						// If `when` condition is optimized to always `true`,
						// then BuildSearchCondition doesn't write anything.
						if (StringBuilder.Length == length)
							StringBuilder.Append("1 = 1");
						StringBuilder.AppendLine(" THEN");
					}
					else
					{
						StringBuilder.AppendLine("ELSE");
					}

					BuildInsertClause(statement, insert.Insert, "INTO ", appendTableName : true, addAlias : false);
				}
			}

			Indent--;
		}

		#endregion

		protected StringBuilder? HintBuilder;

		int  _hintPosition;
		bool _isTopLevelBuilder;

		protected override void StartStatementQueryExtensions(SelectQuery? selectQuery)
		{
			if (HintBuilder == null)
			{
				HintBuilder        = new();
				_isTopLevelBuilder = true;
				_hintPosition      = StringBuilder.Length;

				if (Statement is SqlInsertStatement)
					_hintPosition -= " INTO ".Length;

				if (selectQuery?.QueryName is {} queryName)
					HintBuilder
						.Append("QB_NAME(")
						.Append(queryName)
						.Append(')')
						;
			}
			else if (selectQuery?.QueryName is {} queryName)
			{
				StringBuilder
					.Append(" /*+ QB_NAME(")
					.Append(queryName)
					.Append(") */")
					;
			}
		}

		protected override void FinalizeBuildQuery(SqlStatement statement)
		{
			base.FinalizeBuildQuery(statement);

			if (statement.SqlQueryExtensions is not null && HintBuilder is not null)
			{
				if (HintBuilder.Length > 0 && HintBuilder[^1] != ' ')
					HintBuilder.Append(' ');
				BuildQueryExtensions(HintBuilder, statement.SqlQueryExtensions, null, " ", null, Sql.QueryExtensionScope.QueryHint);
			}

			if (_isTopLevelBuilder && HintBuilder!.Length > 0)
			{
				HintBuilder.Insert(0, " /*+ ");
				HintBuilder.Append(" */");

				StringBuilder.Insert(_hintPosition, HintBuilder.ToString());
			}
		}

		protected override void BuildTableExtensions(SqlTable table, string alias)
		{
			if (HintBuilder is not null && table.SqlQueryExtensions is not null)
				BuildTableExtensions(HintBuilder, table, alias, null, " ", null);
		}
	}
}
