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
		{ }

		protected OracleSqlBuilderBase(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{ }

		protected override void BuildSelectClause(SelectQuery selectQuery)
		{
			// Oracle doesn't support SELECT without FROM,
			// must select from dummy table DUAL instead.
			if (selectQuery.From.Tables.Count == 0)
			{
				Write($"{Tab}SELECT");
				StartStatementQueryExtensions(selectQuery);
				WriteLine();
				BuildColumns(selectQuery);
				WriteLine($"{Tab}FROM SYS.DUAL");
				return;
			}
			
			base.BuildSelectClause(selectQuery);
		}

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			var identityField = insertClause.Into!.GetIdentityField();

			if (identityField == null)
				throw new LinqToDBException($"Identity field must be defined for '{insertClause.Into.NameForLogging}'.");

			Write($"{Tab}RETURNING \n{Tab}\t");
			BuildExpression(identityField, false, true);
			WriteLine(" INTO :IDENTITY_PARAMETER");
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
			SqlOptimizer.ConvertSkipTake(NullabilityContext, MappingSchema, DataOptions, selectQuery, OptimizationContext, out var takeExpr, out var skipExpr);

			return
				base.ShouldBuildWhere(selectQuery, out condition) ||
				!NeedSkip(takeExpr, skipExpr) &&
				 NeedTake(takeExpr) &&
				selectQuery.OrderBy.IsEmpty && 
				selectQuery.Having.IsEmpty;
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
			var fixedLength = type.DataType switch
			{
				DataType.Date or DataType.DateTime => "date",
				DataType.DateTime2 when type.Precision is null or 6 => "timestamp",
				DataType.DateTimeOffset when type.Precision is null or 6 => "timestamp with time zone",
				DataType.UInt32 or DataType.Int64 => "Number(19)",
				DataType.SByte or DataType.Byte => "Number(3)",
				DataType.Money => "Number(19, 4)",
				DataType.SmallMoney => "Number(10, 4)",
				DataType.VarChar when type.Length is null or > 4000 or < 1 => "VarChar(4000)",
				DataType.NVarChar when type.Length is null or > 4000 or < 1 => "VarChar2(4000)",
				DataType.Boolean => "Char(1)",
				DataType.NText => "NClob",
				DataType.Text => "Clob",
				DataType.Guid => "Raw(16)",
				DataType.Binary or DataType.VarBinary when type.Length is null or 0 => "BLOB",
				_ => null,
			};
			if (fixedLength is not null)
			{
				Write(fixedLength);
				return;
			}

			switch (type.DataType)
			{
				case DataType.DateTime2:
					Write($"timestamp({type.Precision})");
					return;
				case DataType.DateTimeOffset:
					Write($"timestamp({type.Precision}) with time zone");
					return;
				case DataType.VarChar:
					Write($"VarChar({type.Length})");
					return;
				case DataType.NVarChar:
					Write($"VarChar2({type.Length})");
					return;
				case DataType.Binary or DataType.VarBinary:
					Write($"Raw({type.Length})");
					return;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
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
			return convertType switch
			{
				ConvertType.NameToQueryParameter   => sb.Append(':').Append(value),
				
				// needs proper list of reserved words and name validation
				// something like we did for Firebird
				// right now reserved words list contains garbage
				ConvertType.NameToQueryFieldAlias  or 
				ConvertType.NameToQueryField       or
				ConvertType.NameToQueryTable       or
				ConvertType.NameToProcedure        or
				ConvertType.NameToPackage          or
				ConvertType.NameToServer           or
				ConvertType.SequenceName           or
				ConvertType.NameToSchema           or
				ConvertType.TriggerName 
					when !IsValidIdentifier(value) => sb.Append('"').Append(value).Append('"'),

				_                                  => sb.Append(value),
			};
		}

		protected override StringBuilder BuildExpression(
			ISqlExpression expr,
			bool           buildTableName,
			bool           checkParentheses,
			string?        alias,
			ref bool       addAlias,
			bool           throwExceptionIfTableNotFound = true)
		{
			return base.BuildExpression(
				expr,
				buildTableName && Statement.QueryType != QueryType.MultiInsert,
				checkParentheses,
				alias,
				ref addAlias,
				throwExceptionIfTableNotFound);
		}

		protected override void BuildExprExprPredicate(SqlPredicate.ExprExpr expr)
		{
			// Oracle needs brackets around right-hand side row literal to disambiguate syntax, e.g.:
			// (1, 2) = ( (3, 4) )
			if (QueryHelper.UnwrapNullablity(expr.Expr2).ElementType == QueryElementType.SqlRow && 
			    expr.Operator != SqlPredicate.Operator.Overlaps)
			{
				var precedence = GetPrecedence(expr);
				Write($"{ At(precedence, expr.Expr1) }{ GetOperator(expr) }({ expr.Expr2 })");
				return;
			}
			
			base.BuildExprExprPredicate(expr);
		}

		protected override void BuildIsDistinctPredicate(SqlPredicate.IsDistinct expr)
		{
			var result = expr.IsNot ? '0' : '1';
			Write($"DECODE({ expr.Expr1 }, { expr.Expr2 }, 0, 1) = { result }");
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
			=> BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, "FROM SYS.DUAL");

		public override string GetReserveSequenceValuesSql(int count, string sequenceName) => FormattableString.Invariant(
			$"SELECT {ConvertInline(sequenceName, ConvertType.SequenceName)}.nextval ID from DUAL connect by level <= {count}");

		protected override void BuildEmptyInsert(SqlInsertClause insertClause)
		{
			Write("VALUES ");
			foreach (var _ in insertClause.Into!.Fields) Write("(DEFAULT)");
			WriteLine();
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
			var table = dropTable.Table;
			var hasIdentity = table.IdentityFields is [not null, ..];
			var hasDropIfExistsOrTemporary =
				(table.TableOptions & (TableOptions.DropIfExists | TableOptions.IsTemporary)) != 0; 
			
			if (!hasIdentity && !hasDropIfExistsOrTemporary)
			{
				base.BuildDropTableStatement(dropTable);
				return;
			}

			var tableName = table.TableName;
			
			if (!hasIdentity)
			{
				WriteLine($"""
					{ dropTable.Tag }BEGIN
						EXECUTE IMMEDIATE 'DROP TABLE { table }';
					EXCEPTION
						WHEN OTHERS THEN
							IF SQLCODE != -942 THEN
								RAISE;
							END IF;
					END;
					""");
			}
			else if (!hasDropIfExistsOrTemporary)
			{
				var sqName = MakeIdentitySequenceName(tableName.Name);
				var trName = MakeIdentityTriggerName(tableName.Name);
				
				WriteLine($"""
					{ dropTable.Tag }BEGIN
						EXECUTE IMMEDIATE 'DROP TRIGGER { Ident(trName, ConvertType.TriggerName, tableName.Schema) }';
						EXECUTE IMMEDIATE 'DROP SEQUENCE { Ident(sqName, ConvertType.TriggerName, tableName.Schema) }';
						EXECUTE IMMEDIATE 'DROP TABLE { table }';
					END;
					""");
			}
			else
			{
				var sqName = MakeIdentitySequenceName(tableName.Name);
				var trName = MakeIdentityTriggerName(tableName.Name);

				WriteLine($"""
					{ dropTable.Tag }BEGIN
						BEGIN
							EXECUTE IMMEDIATE 'DROP TRIGGER { Ident(trName, ConvertType.TriggerName, tableName.Schema) }';
						EXCEPTION
							WHEN OTHERS THEN
								IF SQLCODE != -4080 THEN
									RAISE;
								END IF;
						END;
						BEGIN
							EXECUTE IMMEDIATE 'DROP SEQUENCE { Ident(sqName, ConvertType.SequenceName, tableName.Schema) }'; 
						EXCEPTION
							WHEN OTHERS THEN
								IF SQLCODE != -2289 THEN
									RAISE;
								END IF;
						END;
						BEGIN
							EXECUTE IMMEDIATE 'DROP TABLE { table }';
						EXCEPTION
							WHEN OTHERS THEN
								IF SQLCODE != -942 THEN
									RAISE;
								END IF;
						END;
					END;
					""");
			}
		}

		protected static string MakeIdentityTriggerName(string tableName) => "TIDENTITY_" + tableName;

		protected static string MakeIdentitySequenceName(string tableName) => "SIDENTITY_" + tableName;

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			switch (Statement)
			{
				case SqlTruncateTableStatement truncate:
					// FIXME: Oracle 12.1+ has `ALTER SEQUENCE <sequence_name> RESTART START WITH 1` 
					var sequenceName = ConvertInline(MakeIdentitySequenceName(truncate.Table!.TableName.Name), ConvertType.SequenceName);
					WriteLine($"""
						DECLARE
							l_value number;
						BEGIN
							-- Select the next value of the sequence
							EXECUTE IMMEDIATE 'SELECT {sequenceName}.NEXTVAL FROM dual' INTO l_value;

							-- Set a negative increment for the sequence, with value = the current value of the sequence
							EXECUTE IMMEDIATE 'ALTER SEQUENCE {sequenceName} INCREMENT BY -' || l_value || ' MINVALUE 0';

							-- Select once from the sequence, to take its current value back to 0
							EXECUTE IMMEDIATE 'select {sequenceName}.NEXTVAL FROM dual' INTO l_value;

							-- Set the increment back to 1
							EXECUTE IMMEDIATE 'ALTER SEQUENCE {sequenceName} INCREMENT BY 1 MINVALUE 0';
						END;
						""");
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
			=> Write("TRUNCATE TABLE ");

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

		private void AppendSchemaPrefix(StringBuilder sb, string? schema)
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

			Write(command);
		}

		protected override void BuildStartCreateTableStatement(SqlCreateTableStatement createTable)
		{
			var options = createTable.Table.TableOptions;
			if (createTable.StatementHeader == null && (options.HasCreateIfNotExists() || options.HasIsTemporary()))
			{
				WriteLine($"""
					{Tab}BEGIN
					{Tab}	EXECUTE IMMEDIATE '
					""");
				Indent += 2;
			}

			base.BuildStartCreateTableStatement(createTable);
		}

		protected override void BuildEndCreateTableStatement(SqlCreateTableStatement createTable)
		{
			base.BuildEndCreateTableStatement(createTable);

			if (createTable.StatementHeader == null)
			{
				var options = createTable.Table.TableOptions;

				if (options.IsTemporaryOptionSet())
				{
					var onCommit = options.HasIsTransactionTemporaryData() ? "DELETE" : "PRESERVE";
					Write($"{Tab}ON COMMIT {onCommit} ROWS");
				}

				if (options.HasCreateIfNotExists() || options.HasIsTemporary())
				{
					Indent--;
					WriteLine($"""
						{Tab}';
						EXCEPTION
							WHEN OTHERS THEN
								IF SQLCODE != -955 THEN
									RAISE;
								END IF;
						END;
						""");
					Indent--;
				}
			}
		}

		#region Build MultiInsert

		protected override void BuildMultiInsertQuery(SqlMultiInsertStatement statement)
		{
			BuildMultiInsertClause(statement);
			BuildSqlBuilder((SelectQuery)statement.Source.Source, Indent, skipAlias : false);
		}

		protected void BuildMultiInsertClause(SqlMultiInsertStatement statement)
		{
			WriteLine(statement.InsertType == MultiInsertType.First ? "INSERT FIRST" : "INSERT ALL");

			Indent++;
			
			if (statement.InsertType == MultiInsertType.Unconditional)
			{
				foreach (var insert in statement.Inserts)
					BuildInsertClause(statement, insert.Insert, "INTO ", appendTableName: true, addAlias: false);
			}
			else
			{
				foreach (var insert in statement.Inserts)
				{
					if (insert.When != null)
					{
						Write("WHEN ");
						int length = StringBuilder.Length;
						BuildSearchCondition(insert.When, wrapCondition : true);
						// If `when` condition is optimized to always `true`,
						// then BuildSearchCondition doesn't write anything.
						if (StringBuilder.Length == length) Write("1 = 1");
						WriteLine(" THEN");
					}
					else
					{
						WriteLine("ELSE");
					}

					BuildInsertClause(statement, insert.Insert, "INTO ", appendTableName: true, addAlias: false);
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
						.Append(')');
			}
			else if (selectQuery?.QueryName is {} queryName)
			{
				Write($" /*+ QB_NAME({queryName}) */");
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
