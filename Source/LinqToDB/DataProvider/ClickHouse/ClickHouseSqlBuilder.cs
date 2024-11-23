using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.ClickHouse
{
	using Common;
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	sealed class ClickHouseSqlBuilder : BasicSqlBuilder
	{
		public ClickHouseSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		ClickHouseSqlBuilder(ClickHouseSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder() => new ClickHouseSqlBuilder(this);

		protected override void BuildMergeStatement(SqlMergeStatement merge)     => throw new LinqToDBException($"{Name} provider doesn't support SQL MERGE statement");
		protected override void BuildParameter(SqlParameter parameter) => throw new LinqToDBException($"Parameters not supported for {Name} provider");

		#region Identifiers

		public override StringBuilder BuildObjectName(StringBuilder sb, SqlObjectName name, ConvertType objectType, bool escape, TableOptions tableOptions, bool withoutSuffix)
		{
			// FQN: [db].name (actually FQN schema is more complex, but we don't support such scenarios)
			if (name.Database != null && !tableOptions.IsTemporaryOptionSet())
			{
				(escape ? Convert(sb, name.Database, ConvertType.NameToDatabase) : sb.Append(name.Database))
					.Append('.');
			}

			return escape ? Convert(sb, name.Name, objectType) : sb.Append(name.Name);
		}

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryTable     :
				case ConvertType.NameToQueryField     :
				case ConvertType.NameToQueryParameter :
				case ConvertType.NameToQueryTableAlias:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToDatabase       :
				case ConvertType.NameToSchema         :
				case ConvertType.NameToProcedure      :
				default                               :
					if (IsValidIdentifier(value))
						sb.Append(value);
					else
						EscapeIdentifier(sb, value);
					break;
			}

			return sb;
		}

		private static bool IsValidIdentifier(string identifier)
		{
			if (identifier.Length == 0)
				throw new LinqToDBException("Empty identifier");

			// https://clickhouse.com/docs/en/sql-reference/syntax/#identifiers
			if (identifier[0] is not '_' and not (>= 'a' and <= 'z') and not (>= 'A' and <= 'Z'))
				return false;

			for (var i = 1; i < identifier.Length; i++)
			{
				if (identifier[i] is not '_' and not (>= 'a' and <= 'z') and not (>= 'A' and <= 'Z') and not (>= '0' and <= '9'))
					return false;
			}

			return true;
		}

		// both ` or " supported, we choose `
		private const char IDENTIFIER_QUOTE = '`';

		private static void EscapeIdentifier(StringBuilder sb, string value)
		{
			sb.Append(IDENTIFIER_QUOTE);

			foreach (var chr in value)
			{
				sb.Append(chr);

				// duplicate quote character
				if (chr == IDENTIFIER_QUOTE)
					sb.Append(IDENTIFIER_QUOTE);
			}

			sb.Append(IDENTIFIER_QUOTE);
		}

		#endregion

		#region Types

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			BuildTypeName(StringBuilder, type, canBeNull);
		}

		protected override void BuildCreateTableNullAttribute(SqlField field, DefaultNullable defaultNullable)
		{
			// we use Nullable(T) syntax for create table to:
			// - be consistent with type generation is queries
			// - be consistent with nullable JSON column type
		}

		private static void BuildTypeName(StringBuilder sb, DbDataType type, bool nullable)
		{
			// nullable JSON type has "special" syntax
			if (nullable && type.DataType != DataType.Json)
				sb.Append("Nullable(");

			switch (type.DataType)
			{
				case DataType.Char or DataType.NChar or DataType.Binary         : sb.AppendFormat(CultureInfo.InvariantCulture, "FixedString({0})", type.Length ?? ClickHouseMappingSchema.DEFAULT_FIXED_STRING_LENGTH);    break;
				case DataType.VarChar or DataType.NVarChar or DataType.VarBinary: sb.Append("String");                                                                                                                      break;
				case DataType.Boolean                                           : sb.Append("Bool");                                                                                                                        break;
				case DataType.Guid                                              : sb.Append("UUID");                                                                                                                        break;
				case DataType.SByte                                             : sb.Append("Int8");                                                                                                                        break;
				case DataType.Byte                                              : sb.Append("UInt8");                                                                                                                       break;
				case DataType.Int16                                             : sb.Append("Int16");                                                                                                                       break;
				case DataType.UInt16                                            : sb.Append("UInt16");                                                                                                                      break;
				case DataType.Int32                                             : sb.Append("Int32");                                                                                                                       break;
				case DataType.UInt32                                            : sb.Append("UInt32");                                                                                                                      break;
				case DataType.Int64                                             : sb.Append("Int64");                                                                                                                       break;
				case DataType.UInt64                                            : sb.Append("UInt64");                                                                                                                      break;
				case DataType.Int128                                            : sb.Append("Int128");                                                                                                                      break;
				case DataType.UInt128                                           : sb.Append("UInt128");                                                                                                                     break;
				case DataType.Int256                                            : sb.Append("Int256");                                                                                                                      break;
				case DataType.UInt256                                           : sb.Append("UInt256");                                                                                                                     break;
				case DataType.Single                                            : sb.Append("Float32");                                                                                                                     break;
				case DataType.Double                                            : sb.Append("Float64");                                                                                                                     break;
				case DataType.Decimal32                                         : sb.AppendFormat(CultureInfo.InvariantCulture, "Decimal32({0})", type.Scale ?? ClickHouseMappingSchema.DEFAULT_DECIMAL_SCALE);             break;
				case DataType.Decimal64                                         : sb.AppendFormat(CultureInfo.InvariantCulture, "Decimal64({0})", type.Scale ?? ClickHouseMappingSchema.DEFAULT_DECIMAL_SCALE);             break;
				case DataType.Decimal128                                        : sb.AppendFormat(CultureInfo.InvariantCulture, "Decimal128({0})", type.Scale ?? ClickHouseMappingSchema.DEFAULT_DECIMAL_SCALE);            break;
				case DataType.Decimal256                                        : sb.AppendFormat(CultureInfo.InvariantCulture, "Decimal256({0})", type.Scale ?? ClickHouseMappingSchema.DEFAULT_DECIMAL_SCALE);            break;
				case DataType.Date                                              : sb.Append("Date");                                                                                                                        break;
				case DataType.Date32                                            : sb.Append("Date32");                                                                                                                      break;
				case DataType.DateTime                                          : sb.Append("DateTime");                                                                                                                    break;
				case DataType.DateTime64                                        : sb.AppendFormat(CultureInfo.InvariantCulture, "DateTime64({0})", type.Precision ?? ClickHouseMappingSchema.DEFAULT_DATETIME64_PRECISION); break;
				case DataType.IPv4                                              : sb.Append("IPv4");                                                                                                                        break;
				case DataType.IPv6                                              : sb.Append("IPv6");                                                                                                                        break;
				case DataType.IntervalSecond                                    : sb.Append("IntervalSecond");                                                                                                              break;
				case DataType.IntervalMinute                                    : sb.Append("IntervalMinute");                                                                                                              break;
				case DataType.IntervalHour                                      : sb.Append("IntervalHour");                                                                                                                break;
				case DataType.IntervalDay                                       : sb.Append("IntervalDay");                                                                                                                 break;
				case DataType.IntervalWeek                                      : sb.Append("IntervalWeek");                                                                                                                break;
				case DataType.IntervalMonth                                     : sb.Append("IntervalMonth");                                                                                                               break;
				case DataType.IntervalQuarter                                   : sb.Append("IntervalQuarter");                                                                                                             break;
				case DataType.IntervalYear                                      : sb.Append("IntervalYear");                                                                                                                break;
				// that's kinda sad
				case DataType.Json                                              : sb.Append(nullable ? "Object(Nullable('json'))" : "JSON");                                                                                break;
				// TODO                                                         : implement type generation at some point
				case DataType.Enum8                                             :
				case DataType.Enum16                                            : throw new LinqToDBException($"Enum type name generation in not supported yet. Use {nameof(ColumnAttribute.DbType)} property to specify enum type explicitly");
				default                                                         : throw new LinqToDBException($"Cannot infer type name from {type}. Specify DataType or DbType explicitly");
			}

			if (nullable && type.DataType != DataType.Json)
				sb.Append(')');
		}

		#endregion

		#region Tables DDL

		protected override void BuildEndCreateTableStatement(SqlCreateTableStatement createTable)
		{
			base.BuildEndCreateTableStatement(createTable);

			// TODO: add support for engine configuration API
			// For now we use fixed engines and it is recommended to use RAW SQL for table creation
			// append MergeTree engine for mappings with primary key
			// and Memory engine for others
			var primaryKey = createTable.Table.Fields
				.Where(_ => _.IsPrimaryKey)
				.OrderBy(_ => _.PrimaryKeyOrder)
				.ToArray();

			if (primaryKey.Length > 0)
			{
				StringBuilder
					.AppendLine("ENGINE = MergeTree()")
					.Append("ORDER BY ");

				if (primaryKey.Length > 1)
					StringBuilder.Append('(');

				for (var i = 0; i < primaryKey.Length; i++)
				{
					if (i > 0)
						StringBuilder.Append(", ");
					Convert(StringBuilder, primaryKey[i].PhysicalName, ConvertType.NameToQueryField);
				}

				if (primaryKey.Length > 1)
					StringBuilder.Append(')');

				StringBuilder.AppendLine();
			}
			else
				StringBuilder
					.AppendLine("ENGINE = Memory()");
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();

			StringBuilder.Append("PRIMARY KEY (");

			var first = true;
			foreach (var fieldName in fieldNames)
			{
				if (!first)
					StringBuilder.Append(InlineComma);
				else
					first = false;

				StringBuilder.Append(fieldName);
			}

			StringBuilder.Append(')');
		}

		protected override void BuildCreateTableCommand(SqlTable table)
		{
			string command;

			if (table.TableOptions.IsTemporaryOptionSet())
			{
				switch (table.TableOptions & TableOptions.IsTemporaryOptionSet)
				{
					case TableOptions.IsTemporary                                                                             :
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryData                                         :
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure                                    :
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData:
					case TableOptions.IsLocalTemporaryData                                                                    :
					case TableOptions.IsLocalTemporaryStructure                                                               :
					case TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData                           :
						command = "CREATE TEMPORARY TABLE ";
						break;
					case var value                                                                                            :
						throw new LinqToDBException($"Incompatible table options '{value}'");
				}
			}
			else
			{
				command = "CREATE TABLE ";
			}

			StringBuilder.Append(command);

			if (table.TableOptions.HasCreateIfNotExists())
				StringBuilder.Append("IF NOT EXISTS ");
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			BuildDropTableStatementIfExists(dropTable);
		}

		#endregion

		#region TRUNCATE/DELETE/UPDATE

		protected override void BuildTruncateTable(SqlTruncateTableStatement truncateTable)
		{
			StringBuilder.Append("TRUNCATE TABLE ");
		}

		// ALTER DELETE/UPDATE doesn't support table aliases
		private bool _disableTableAliases;

		protected override void BuildDeleteQuery(SqlDeleteStatement deleteStatement)
		{
			var old = _disableTableAliases;

			_disableTableAliases = true;

			base.BuildDeleteQuery(deleteStatement);

			_disableTableAliases = old;
		}

		protected override void BuildDeleteFromClause(SqlDeleteStatement deleteStatement)
		{
			// explicit guard to avoid situations when query produce valid SQL after aliases stripped
			if (deleteStatement.SelectQuery.From.Tables.Count != 1
				|| deleteStatement.SelectQuery.From.Tables[0].Joins.Count != 0)
				throw new LinqToDBException(ErrorHelper.ClickHouse.Error_CorrelatedDelete);

			AppendIndent();

			StringBuilder.Append("ALTER TABLE").AppendLine();

			Indent++;
			AppendIndent();

			var ts = deleteStatement.SelectQuery.From.Tables[0];
			BuildTableName(ts, true, false);

			Indent--;

			StringBuilder.AppendLine();
		}

		protected override void BuildDeleteClause(SqlDeleteStatement deleteStatement)
		{
			AppendIndent();
		}

		protected override void BuildAlterDeleteClause(SqlDeleteStatement deleteStatement)
		{
			StringBuilder.Append("DELETE ");

			// WHERE clause is required for DELETE queries
			if (deleteStatement.SelectQuery.Where.IsEmpty)
				StringBuilder.Append("WHERE 1");
		}

		protected override string UpdateKeyword    => "ALTER TABLE";
		protected override string UpdateSetKeyword => "UPDATE";

		protected override void BuildUpdateQuery(SqlStatement statement, SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			// explicit guard to avoid situations when query produce valid SQL after aliases stripped
			if (selectQuery.From.Tables.Count != 1
				|| selectQuery.From.Tables[0].Joins.Count != 0)
				throw new LinqToDBException(ErrorHelper.ClickHouse.Error_CorrelatedUpdate);

			var old = _disableTableAliases;
			_disableTableAliases = true;

			base.BuildUpdateQuery(statement, selectQuery, updateClause);

			_disableTableAliases = old;
		}

		protected override void BuildUpdateWhereClause(SelectQuery selectQuery)
		{
			// WHERE clause required for UPDATE query
			if (selectQuery.Where.IsEmpty)
				StringBuilder.Append("WHERE 1");
			else
				BuildWhereClause(selectQuery);
		}

		protected override void BuildUpdateTableName(SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			if (updateClause.Table != null && (selectQuery.From.Tables.Count == 0 || updateClause.Table != selectQuery.From.Tables[0].Source))
			{
				BuildPhysicalTable(updateClause.Table, null);
			}
			else
			{
				if (selectQuery.From.Tables[0].Source is SelectQuery)
					StringBuilder.Length--;

				BuildTableName(selectQuery.From.Tables[0], true, false);
			}
		}

		protected override bool BuildFieldTableAlias(SqlField field) => !_disableTableAliases;

		#endregion

		#region TAKE/SKIP

		protected override void BuildOffsetLimit(SelectQuery selectQuery)
		{
			SqlOptimizer.ConvertSkipTake(NullabilityContext, MappingSchema, DataOptions, selectQuery, OptimizationContext, out var takeExpr, out var skipExpr);

			if (takeExpr != null || skipExpr != null)
			{
				AppendIndent()
					.Append("LIMIT ");

				if (skipExpr != null)
				{
					BuildExpression(skipExpr);
					StringBuilder.Append(", ");
				}

				if (takeExpr != null)
					BuildExpression(takeExpr);
				else
					// ulong max
					StringBuilder.Append("18446744073709551615");

				BuildTakeHints(selectQuery);

				StringBuilder.AppendLine();
			}
		}

		#endregion

		#region CTE

		protected override bool IsCteColumnListSupported => false;
		protected override bool IsRecursiveCteKeywordRequired => true;

		protected override void BuildCteBody(SelectQuery selectQuery)
		{
			var sqlBuilder = (ClickHouseSqlBuilder)CreateSqlBuilder();
			sqlBuilder.BuildSql(0, new SqlSelectStatement(selectQuery), StringBuilder, OptimizationContext, Indent, false);
		}

		protected override void BuildInsertQuery(SqlStatement statement, SqlInsertClause insertClause, bool addAlias)
		{
			// move CTE to SELECT clause for INSERT FROM SELECT queries

			var nullability = NullabilityContext.GetContext(statement.SelectQuery);

			BuildStep = Step.Tag; BuildTag(statement);
			BuildStep = Step.InsertClause; BuildInsertClause(statement, insertClause, addAlias);

			if (statement.QueryType == QueryType.Insert && statement.SelectQuery!.From.Tables.Count != 0)
			{
				BuildStep = Step.WithClause     ; BuildWithClause     (statement.GetWithClause());
				BuildStep = Step.SelectClause   ; BuildSelectClause   (statement.SelectQuery);
				BuildStep = Step.FromClause     ; BuildFromClause     (statement, statement.SelectQuery);
				BuildStep = Step.WhereClause    ; BuildWhereClause    (statement.SelectQuery);
				BuildStep = Step.GroupByClause  ; BuildGroupByClause  (statement.SelectQuery);
				BuildStep = Step.HavingClause   ; BuildHavingClause   (statement.SelectQuery);
				BuildStep = Step.OrderByClause  ; BuildOrderByClause  (statement.SelectQuery);
				BuildStep = Step.OffsetLimit    ; BuildOffsetLimit    (statement.SelectQuery);
				BuildStep = Step.QueryExtensions; BuildSubQueryExtensions(statement);
			}

			if (insertClause.WithIdentity)
				BuildGetIdentity(insertClause);
			else
			{
				if (nullability == null)
					throw new InvalidOperationException();

				BuildStep = Step.Output;
				BuildOutputSubclause(statement.GetOutputClause());
			}
		}
		#endregion

		protected override bool CanSkipRootAliases(SqlStatement statement)
		{
			if (statement.SelectQuery != null)
			{
				if (statement.SelectQuery.HasSetOperators)
					return false;
			}

			return base.CanSkipRootAliases(statement);
		}

		protected override void BuildIsDistinctPredicate(SqlPredicate.IsDistinct expr) => BuildIsDistinctPredicateFallback(expr);

		protected override bool IsValuesSyntaxSupported => false;

		protected override void BuildSetOperation(SetOperation operation, StringBuilder sb)
		{
			switch (operation)
			{
				case SetOperation.Union       : sb.Append("UNION DISTINCT");     break;
				case SetOperation.UnionAll    : sb.Append("UNION ALL");          break;
				case SetOperation.Except      : sb.Append("EXCEPT DISTINCT");    break;
				case SetOperation.Intersect   : sb.Append("INTERSECT DISTINCT"); break;
				case SetOperation.IntersectAll: sb.Append("INTERSECT ALL");      break;
				case SetOperation.ExceptAll   : sb.Append("EXCEPT ALL");         break;
				default                       : throw new LinqToDBException($"SET operation {nameof(operation)} is not supported by ClickHouse");
			}
		}

		protected override void BuildColumnExpression(SelectQuery? selectQuery, ISqlExpression expr, string? alias,
			ref bool                                               addAlias)
		{
			base.BuildColumnExpression(selectQuery, expr, alias, ref addAlias);

			// force alias generation on nested queries otherwise column in parent query will have composite name subqueryAlias.columnName
			// (could have many nesting levels) which we don't support and have no plans to support
			addAlias = addAlias || Statement.ParentStatement != null;
		}

		protected override void BuildTableExtensions(SqlTable table, string alias)
		{
			if (table.SqlQueryExtensions is not null)
			{
				BuildTableExtensions(StringBuilder, table, alias, null, ", ", null,
					ext =>
						ext.Scope is Sql.QueryExtensionScope.TableHint or Sql.QueryExtensionScope.TablesInScopeHint &&
						!(ext.Arguments.TryGetValue("hint", out var hint) && hint is SqlValue(ClickHouseHints.Table.Final)));
			}
		}

		protected override bool BuildJoinType(SqlJoinedTable join, SqlSearchCondition condition)
		{
			var ext = join.SqlQueryExtensions?.LastOrDefault(e => e.Scope is Sql.QueryExtensionScope.JoinHint);

			if (ext?.Arguments["hint"] is SqlValue v)
			{
				var h = (string)v.Value!;

				if (h.StartsWith(ClickHouseHints.Join.Global))
				{
					StringBuilder
						.Append(ClickHouseHints.Join.Global)
						.Append(' ');

					if (h ==  ClickHouseHints.Join.Global)
						return base.BuildJoinType(join, condition);

					h = h[(ClickHouseHints.Join.Global.Length + 1)..];
				}
				else if (h.StartsWith(ClickHouseHints.Join.All))
				{
					StringBuilder
						.Append(ClickHouseHints.Join.All)
						.Append(' ');

					if (h ==  ClickHouseHints.Join.All)
						return base.BuildJoinType(join, condition);

					h = h[(ClickHouseHints.Join.All.Length + 1)..];
				}

				switch (join.JoinType)
				{
					case JoinType.Inner when SqlProviderFlags.IsCrossJoinSupported && condition.Predicates.IsNullOrEmpty() :
					                      StringBuilder.Append(CultureInfo.InvariantCulture, $"CROSS {h} JOIN "); return false;
					case JoinType.Inner : StringBuilder.Append(CultureInfo.InvariantCulture, $"INNER {h} JOIN "); return true;
					case JoinType.Left  : StringBuilder.Append(CultureInfo.InvariantCulture, $"LEFT {h} JOIN ");  return true;
					case JoinType.Right : StringBuilder.Append(CultureInfo.InvariantCulture, $"RIGHT {h} JOIN "); return true;
					case JoinType.Full  : StringBuilder.Append(CultureInfo.InvariantCulture, $"FULL {h} JOIN ");  return true;
					default             : throw new InvalidOperationException();
				}
			}

			return base.BuildJoinType(join, condition);
		}

		protected override void BuildQueryExtensions(SqlStatement statement)
		{
			if (statement.SqlQueryExtensions is not null)
				BuildQueryExtensions(StringBuilder, statement.SqlQueryExtensions, null, Environment.NewLine, null, Sql.QueryExtensionScope.QueryHint);
		}

		HashSet<SqlQueryExtension>? _finalHints;

		protected override void BuildFromExtensions(SelectQuery selectQuery)
		{
			var hasFinal =
				selectQuery.SqlQueryExtensions?.Any(HasFinal) == true ||
				selectQuery.From.Tables.Any(t => t.Source switch
				{
					SqlTable                   s  => s.SqlQueryExtensions?.Any(HasFinal) == true,
					SelectQuery                s  => s.SqlQueryExtensions?.Any(HasFinal) == true,
					SqlTableSource(SelectQuery s) => s.SqlQueryExtensions?.Any(HasFinal) == true,
					_ => false
				});

			bool HasFinal(SqlQueryExtension ext)
			{
				var has =
					ext.Scope is Sql.QueryExtensionScope.TableHint or Sql.QueryExtensionScope.TablesInScopeHint or Sql.QueryExtensionScope.SubQueryHint &&
					ext.Arguments.TryGetValue("hint", out var hint) && hint is SqlValue(ClickHouseHints.Table.Final);

				if (!has)
					return false;

				if (_finalHints == null)
					_finalHints = new();
				else if (_finalHints.Contains(ext))
					return false;

				_finalHints.Add(ext);

				return true;
			}

			if (hasFinal)
			{
				StringBuilder
					.Append(' ')
					.Append(ClickHouseHints.Table.Final)
					;
			}
		}

		protected override void MergeSqlBuilderData(BasicSqlBuilder sqlBuilder)
		{
			if (sqlBuilder is ClickHouseSqlBuilder { _finalHints: {} fh } )
				(_finalHints ??= new()).AddRange(fh);
		}
	}
}
