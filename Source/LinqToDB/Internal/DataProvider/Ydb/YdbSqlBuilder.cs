using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;

using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Ydb;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	public class YdbSqlBuilder : BasicSqlBuilder<YdbOptions>
	{
		readonly         YdbOptions                     _providerOptions;

		public YdbSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
			_providerOptions = dataOptions.FindOrDefault(YdbOptions.Default);
		}

		YdbSqlBuilder(BasicSqlBuilder parentBuilder, YdbOptions ydbOptions) : base(parentBuilder)
		{
			_providerOptions = ydbOptions;
		}

		protected override ISqlBuilder CreateSqlBuilder() => new YdbSqlBuilder(this, _providerOptions);

		// YDB can't infer a type for VALUES cells in two cases: a numeric literal in the first row
		// (it may pick a narrower type than the column) and a column whose cells are all untyped NULL
		// (the derived column becomes nullType and fails when projected). Emit CAST(value AS <type>)
		// for those, mirroring the PostgreSQL builder.
		protected override bool IsSqlValuesTableValueTypeRequired(SqlValuesTable source, IReadOnlyList<List<ISqlExpression>> rows, int row, int column)
		{
			if (row == 0 && rows[0][column] is SqlValue { Value: long or float or double or decimal })
				return true;

			return row < 0
				|| (row == 0 && rows.All(r => r[column] is SqlValue { Value: null }));
		}

		protected override ConcatBuildStyle ConcatStyle => ConcatBuildStyle.Pipes;

		// YQL allows a derived column list only on VALUES, not on a scalar/raw-SQL subquery source
		// (SupportsColumnAliasesInSource stays true so VALUES columns keep their names).
		protected override bool SupportsColumnAliasesInScalarSource => false;

		protected override string LimitFormat(SelectQuery selectQuery) => "LIMIT {0}";
		protected override string OffsetFormat(SelectQuery selectQuery) => "OFFSET {0} ";

		protected override void PrintParameterName(StringBuilder sb, DbParameter parameter)
		{
			sb.Append(parameter.ParameterName);
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent()
				.Append("PRIMARY KEY (")
				.AppendJoinStrings(InlineComma, fieldNames)
				.Append(')');
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
			=> BuildDropTableStatementIfExists(dropTable);

		protected override void BuildCreateTableNullAttribute(SqlField field, DefaultNullable defaultNullable)
		{
			// columns are nullable by default
			// primary key columns always non-nullable even with NULL specified
			if (!field.CanBeNull && !field.IsPrimaryKey)
				StringBuilder.Append("NOT NULL");
		}

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			var id = insertClause.Into?.GetIdentityField()
				?? throw new LinqToDBException($"Identity field must be defined for '{insertClause.Into?.NameForLogging}'.");

			AppendIndent().AppendLine("RETURNING");
			AppendIndent().Append('\t');
			BuildExpression(id, false, true);
			StringBuilder.AppendLine();
		}

		protected override void BuildCreateTableCommand(SqlTable table)
		{
			var command = table.TableOptions.TemporaryOptionValue switch
			{
				TableOptions.IsTemporary                                                                              or
				TableOptions.IsTemporary |                                          TableOptions.IsLocalTemporaryData or
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure                                     or
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData or
				                                                                    TableOptions.IsLocalTemporaryData or
				                           TableOptions.IsLocalTemporaryStructure                                     or
				                           TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData =>
					"CREATE TEMPORARY TABLE ",

				0 =>
					"CREATE TABLE ",

				var value =>
					throw new LinqToDBException($"Incompatible table options '{value}'"),
			};

			StringBuilder.Append(command);
		}

		// duplicate aliases in final select are not supported
		protected override bool CanSkipRootAliases(SqlStatement statement) => false;

		protected override void BuildCreateTableFieldType(SqlField field)
		{
			if (field.IsIdentity)
			{
				StringBuilder.Append(field.Type.DataType switch
				{
					DataType.Int16 => "SMALLSERIAL",
					DataType.Int32 => "SERIAL",
					DataType.Int64 => "BIGSERIAL",
					_ => throw new InvalidOperationException($"Unsupported identity field type {field.Type.DataType}"),
				});

				return;
			}

			base.BuildCreateTableFieldType(field);
		}

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			var isList = type.DataType.HasFlag(DataType.Array);

			if (isList)
			{
				type = type.WithDataType(type.DataType ^ DataType.Array);
				StringBuilder.Append("List<");
			}

			switch (type.DataType)
			{
				case DataType.Boolean    : StringBuilder.Append("Bool");         break;
				case DataType.SByte      : StringBuilder.Append("Int8");         break;
				case DataType.Byte       : StringBuilder.Append("Uint8");        break;
				case DataType.Int16      : StringBuilder.Append("Int16");        break;
				case DataType.UInt16     : StringBuilder.Append("Uint16");       break;
				case DataType.Int32      : StringBuilder.Append("Int32");        break;
				case DataType.UInt32     : StringBuilder.Append("Uint32");       break;
				case DataType.Int64      : StringBuilder.Append("Int64");        break;
				case DataType.UInt64     : StringBuilder.Append("Uint64");       break;
				case DataType.Single     : StringBuilder.Append("Float");        break;
				case DataType.Double     : StringBuilder.Append("Double");       break;
				case DataType.DecFloat   : StringBuilder.Append("DyNumber");     break;
				case DataType.Binary
					or DataType.Blob
					or DataType.VarBinary: StringBuilder.Append("Bytes");       break;
				case DataType.NChar
					or DataType.Char
					or DataType.NVarChar
					or DataType.VarChar
										 : StringBuilder.Append("Text");         break;
				case DataType.Json       : StringBuilder.Append("Json");         break;
				case DataType.BinaryJson : StringBuilder.Append("JsonDocument"); break;
				case DataType.Yson       : StringBuilder.Append("Yson");         break;
				case DataType.Guid       : StringBuilder.Append("Uuid");         break;
				case DataType.Date       : StringBuilder.Append("Date");         break;
				case DataType.Date32     : StringBuilder.Append("Date32");       break;
				case DataType.DateTime   : StringBuilder.Append("Datetime");     break;
				case DataType.DateTime64 : StringBuilder.Append("Datetime64");   break;
				case DataType.DateTime2
					or DataType.DateTimeOffset: StringBuilder.Append("Timestamp"); break;
				case DataType.Timestamp64: StringBuilder.Append("Timestamp64");  break;
				case DataType.Interval
					or DataType.Time     : StringBuilder.Append("Interval");     break;
				case DataType.Interval64 : StringBuilder.Append("Interval64");   break;

				//Tz types not supported as column types
				case DataType.DateTz     : StringBuilder.Append(forCreateTable ? "Date"      : "TzDate");      break;
				case DataType.DateTimeTz : StringBuilder.Append(forCreateTable ? "Datetime"  : "TzDatetime");  break;
				case DataType.DateTime2Tz: StringBuilder.Append(forCreateTable ? "Timestamp" : "TzTimestamp"); break;

				case DataType.Decimal
					or DataType.Money
					or DataType.SmallMoney:
				{
					if (_providerOptions.UseParametrizedDecimal)
					{
						StringBuilder.AppendFormat(
							CultureInfo.InvariantCulture,
							"Decimal({0},{1})",
							type.Precision ?? YdbMappingSchema.DEFAULT_DECIMAL_PRECISION,
							type.Scale ?? YdbMappingSchema.DEFAULT_DECIMAL_SCALE);
					}
					else
					{
						StringBuilder.Append("Decimal");
					}

					break;
				}

				default:
					base.BuildDataTypeFromDataType(type, false, false);

					break;
			}

			if (isList)
				StringBuilder.Append('>');
		}

		protected sealed override bool IsReserved(string word)
		{
			return ReservedWords.IsReserved(word, ProviderName.Ydb);
		}

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			// https://ydb.tech/docs/en/yql/reference/syntax/create_table/#object-naming-rules
			// https://ydb.tech/docs/en/yql/reference/syntax/lexer#keywords-and-ids
			// Documentation doesn't match to database behavior in some places:
			// - keywords could be used as identifiers
			// - . and - are not allowed
			// - reserved words work strange - in some places you can use them as-is, in some - need quotation
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				//{
				//	return sb.Append('@').Append(value);
				//}

				case ConvertType.NameToCteName:
				{
					var quote = (value.Length > 0 && char.IsDigit(value[0]))
					            || value.Any(c => !c.IsAsciiLetterOrDigit() && c is not '_');

					sb.Append('$');

					if (quote)
						return sb.Append('`').Append(value.Replace("`", "\\`", StringComparison.Ordinal)).Append('`');

					return sb.Append(value);
				}

				case ConvertType.NameToQueryField
					or ConvertType.NameToQueryFieldAlias
					or ConvertType.NameToQueryTable
					or ConvertType.NameToQueryTableAlias:
				{
					// don't check for __ydb_ prefix as it is not allowed even in quoted mode
					var quote = (value.Length > 0 && char.IsDigit(value[0]))
						|| value.Any(c => !c.IsAsciiLetterOrDigit() && c is not '_')
						|| IsReserved(value);

					if (quote)
						return sb.Append('`').Append(value.Replace("`", "\\`", StringComparison.Ordinal)).Append('`');

					return sb.Append(value);
				}

				default:
					return sb.Append(value);
			}
		}

		protected override void BuildOutputColumnExpressions(IReadOnlyList<ISqlExpression> expressions)
		{
			Indent++;

			var first = true;

			// RETURNING supports only column names without table reference
			// expressions also not supported, but it is user's fault
			var oldValue = _buildTableName;
			_buildTableName = false;

			foreach (var expr in expressions)
			{
				if (!first)
					StringBuilder.AppendLine(Comma);

				first = false;

				var addAlias  = true;

				AppendIndent();
				BuildColumnExpression(null, expr, null, ref addAlias);
			}

			_buildTableName = oldValue;

			Indent--;

			StringBuilder.AppendLine();
		}

		private bool _buildTableName= true;

		protected override void BuildColumnExpression(SelectQuery? selectQuery, ISqlExpression expr, string? alias, ref bool addAlias)
		{
			BuildExpression(expr, _buildTableName, true, alias, ref addAlias, true);
			addAlias = true;
		}

		protected override void BuildSelectClause(SelectQuery selectQuery)
		{
			// YQL rejects a WHERE without a FROM ("Filtering is not allowed without FROM"). For a
			// from-less constant query that still has a filter, supply a one-row dummy source (YQL has no
			// DUAL). Mirrors MySql57SqlBuilder, which emits "FROM DUAL" for the same reason.
			if (selectQuery.From.Tables.Count == 0 && !selectQuery.Where.IsEmpty)
			{
				AppendIndent().Append("SELECT").AppendLine();
				BuildColumns(selectQuery);
				AppendIndent().Append("FROM (SELECT 1) AS dual").AppendLine();
			}
			else
				base.BuildSelectClause(selectQuery);
		}

		// An empty-values source is "SELECT <typed nulls> WHERE 1 = 0"; YQL rejects a WHERE without a FROM
		// and has no DUAL, so project over a one-row dummy source (mirrors BuildSelectClause above).
		protected override void BuildEmptyValuesFrom() => StringBuilder.Append(" FROM (SELECT 1) AS dual");

		protected override bool IsCteColumnListSupported => false;

		protected override void BuildSql(
			SqlStatement        statement,
			StringBuilder       sb,
			OptimizationContext optimizationContext,
			int                 indent,
			ColumnAliasMode     aliasMode,
			NullabilityContext? nullabilityContext)
		{
			// YDB CTEs are named query variables ($name) that share the parameter-name bucket;
			// reserve all CTE names up front (by registering them with the parameter normalizer) so
			// parameter names generated during the build can't collide with a CTE variable name
			// (resolves the conflict noted in BasicSqlOptimizer.FinalizeCte).
			if (statement is SqlStatementWithQueryBase { With.Clauses.Count: > 0 } withQuery)
			{
				foreach (var cte in withQuery.With.Clauses)
					if (!string.IsNullOrEmpty(cte.Name))
						optimizationContext.NormalizeParameterName(cte.Name);
			}

			base.BuildSql(statement, sb, optimizationContext, indent, aliasMode, nullabilityContext);
		}

		protected override void BuildWithClause(SqlWithClause? with)
		{
			if (with == null || with.Clauses.Count == 0)
				return;

			foreach (var cte in with.Clauses)
			{
				BuildObjectName(StringBuilder, new(cte.Name!), ConvertType.NameToCteName, true, TableOptions.NotSet);
				StringBuilder.Append(" = ");

				Indent++;

				BuildCteBody(cte.Body!);
				StringBuilder.AppendLine(";");

				Indent--;
			}

			StringBuilder.AppendLine();
		}

		protected override void BuildFromClause(SqlStatement statement, SelectQuery selectQuery)
		{
			if (!statement.IsUpdate)
				base.BuildFromClause(statement, selectQuery);
		}

		protected override string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (DataProvider is YdbDataProvider provider)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
				{
					return provider.Adapter.GetDbType(param).ToString();
				}
			}

			return base.GetProviderTypeName(dataContext, parameter);
		}

		protected override void BuildSubQueryExtensions(SqlStatement statement)
		{
			var sqlExts = statement.SelectQuery?.SqlQueryExtensions;
			if (sqlExts is null || sqlExts.Count == 0)
				return;

			BuildQueryExtensions(
				StringBuilder,
				sqlExts,
				prefix: null,     // PragmaQueryHintBuilder will insert line breaks/PRAGMA
				delimiter: "\n",
				suffix: null,
				Sql.QueryExtensionScope.SubQueryHint);
		}

		protected override void BuildInListPredicate(SqlPredicate.InList predicate)
		{
			static List<object?>? TryMaterializeItems(
				OptimizationContext           opt,
				IReadOnlyList<ISqlExpression> values)
			{
				if (values is [SqlParameter pr])
				{
					var pv = pr.GetParameterValue(opt.EvaluationContext.ParameterValues).ProviderValue;
					switch (pv)
					{
						case string:
							return null;
						case IEnumerable en:
						{
							return en.Cast<object?>().ToList();
						}
					}
				}

				var tmp = new List<object?>(values.Count);
				foreach (var v in values)
				{
					switch (v)
					{
						case SqlValue sv:
							tmp.Add(sv.Value);
							break;
						case SqlParameter sp:
							tmp.Add(sp.GetParameterValue(opt.EvaluationContext.ParameterValues).ProviderValue);
							break;
						default:
							return null;
					}
				}

				return tmp;
			}

			var items = TryMaterializeItems(OptimizationContext, predicate.Values);
			if (items == null)
			{
				base.BuildInListPredicate(predicate);
				return;
			}

			var dbDataType = QueryHelper.GetDbDataType(predicate.Expr1, MappingSchema);

			// YQL forbids a NULL literal in an IN list, so strip the NULLs and recreate their null
			// semantics explicitly. Under CompareNulls.LikeClr a NULL in the list matches NULL rows
			// (rendered as IS NULL). Under LikeSql it follows SQL three-valued logic: a NULL never adds
			// a match to IN, and a NULL anywhere in a NOT IN makes the predicate UNKNOWN for every row
			// (empty result). (Under LikeClr the all-NULL case is normally already lowered to IS NULL
			// upstream; the IsNull branch below stays as a defensive fallback.)
			var likeClrNulls = DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr;

			var hasNull = false;
			for (var i = items.Count - 1; i >= 0; i--)
			{
				if (items[i] != null)
				{
					continue;
				}

				hasNull = true;
				items.RemoveAt(i);
			}

			if (hasNull && !likeClrNulls && predicate.IsNot)
			{
				// LikeSql: `x NOT IN (..., NULL)` is UNKNOWN for every row.
				BuildPredicate(SqlPredicate.MakeBool(false));
				return;
			}

			if (items.Count == 0)
			{
				// LikeClr: NULL matches NULL rows. LikeSql: `x IN (NULL, ...)` is never true.
				BuildPredicate(likeClrNulls ? new SqlPredicate.IsNull(predicate.Expr1, predicate.IsNot) : SqlPredicate.MakeBool(false));
				return;
			}

			var max         = SqlProviderFlags.MaxInListValuesCount;
			var startLen    = StringBuilder.Length;
			var bucketIndex = 0;
			for (var i = 0; i < items.Count; i += max, bucketIndex++)
			{
				if (i > 0)
					StringBuilder.Append(predicate.IsNot ? " AND " : " OR ");

				BuildExpression(GetPrecedence(predicate), predicate.Expr1);
				StringBuilder.Append(predicate.IsNot ? " NOT IN (" : " IN (");

				var within = 1;
				for (var j = i; j < Math.Min(i + max, items.Count); j++, within++)
				{
					var p = new SqlParameter(dbDataType, string.Create(CultureInfo.InvariantCulture, $"Ids{bucketIndex}_{within}"), items[j]);
					BuildParameter(p);
					StringBuilder.Append(InlineComma);
				}

				RemoveInlineComma().Append(')');
			}

			// 'x IN (...) OR x IS NULL'
			var addedNullCheck = false;
			if (hasNull && likeClrNulls)
			{
				// LikeClr: a NULL in the list matches NULL rows. (LikeSql drops it: `x IN (1, NULL)` ≡ `x IN (1)`.)
				StringBuilder.Append(predicate.IsNot ? " AND " : " OR ");
				BuildPredicate(new SqlPredicate.IsNull(predicate.Expr1, predicate.IsNot));
				addedNullCheck = true;
			}
			else if (!hasNull && predicate.WithNull == true && predicate.Expr1.ShouldCheckForNull(NullabilityContext))
			{
				// C# Contains semantics: when the tested expression itself is NULL it must still
				// match (NOT IN) / not match (IN), which SQL three-valued logic wouldn't do on its own.
				StringBuilder.Append(" OR ");
				BuildPredicate(new SqlPredicate.IsNull(predicate.Expr1, false));
				addedNullCheck = true;
			}

			if (bucketIndex > 1 || addedNullCheck)
			{
				StringBuilder.Insert(startLen, '(').Append(')');
			}
		}

		protected override void BuildMergeStatement(SqlMergeStatement merge) => throw new LinqToDBException($"{Name} provider doesn't support SQL MERGE statement");

		public override StringBuilder BuildObjectName(
			StringBuilder sb,
			SqlObjectName name,
			ConvertType objectType = ConvertType.NameToQueryTable,
			bool escape = true,
			TableOptions tableOptions = TableOptions.NotSet,
			bool withoutSuffix = false
		)
		{
			string fqn;

			if (name.Database == null && name.Schema == null)
			{
				fqn = name.Name;
			}
			else
			{
				// full name is escaped and whole and consists of:
				// [/databasename/][path][object_name]
				// path: (dir/)+
				using var fullName = Pools.StringBuilder.Allocate();

				if (name.Database != null)
				{
					fullName.Value
						.Append('/')
						.Append(name.Database)
						.Append('/');
				}

				if (name.Schema != null)
				{
					fullName.Value.Append(name.Schema).Append('/');
				}

				fullName.Value.Append(name.Name);

				fqn = fullName.Value.ToString()!;
			}

			return Convert(sb, fqn, objectType);
		}

		protected override void BuildSqlCastExpression(SqlCastExpression castExpression)
		{
			// YQL CAST yields an Optional<T>; Unwrap() coerces it to the non-optional T. Wrapping a
			// nullable cast throws "Failed to unwrap empty optional" at runtime when the value is NULL,
			// so only Unwrap when the cast cannot be null.
			if (castExpression.CanBeNullable(NullabilityContext))
			{
				base.BuildSqlCastExpression(castExpression);
			}
			else
			{
				StringBuilder.Append("Unwrap(");
				base.BuildSqlCastExpression(castExpression);
				StringBuilder.Append(')');
			}
		}

		protected override void BuildOrderByClause(SelectQuery selectQuery)
		{
			if (selectQuery.OrderBy.Items.Count == 0)
				return;

			var orderBy = ConvertElement(selectQuery.OrderBy);

			var nonConstant =
				orderBy.Items.TrueForAll(i => !QueryHelper.IsConstantFast(i.Expression))
				? orderBy.Items
				: orderBy.Items.Where(i => !QueryHelper.IsConstantFast(i.Expression))
					.ToList();

			if (nonConstant.Count == 0)
				return;

			AppendIndent();

			StringBuilder.Append("ORDER BY").AppendLine();

			Indent++;

			for (var i = 0; i < nonConstant.Count; i++)
			{
				AppendIndent();

				var item            = nonConstant[i];
				var orderExpression = item.Expression;

				// Once DISTINCT/GROUP BY reshapes the output, YQL can't reference a source column in
				// ORDER BY - sort by the select alias instead. For a plain query keep the qualified
				// expression: sorting by a bare alias there is ambiguous when a joined table shares the name.
				if (selectQuery.Select.IsDistinct || !selectQuery.GroupBy.IsEmpty)
				{
					var col      = selectQuery.Select.Columns.Find(c => c.Expression.Equals(item.Expression));
					var colAlias = col != null ? AliasesContext.GetColumnAlias(col) : null;
					if (colAlias != null)
						orderExpression = new SqlFragment(colAlias);
				}

				BuildExpressionForOrderBy(orderExpression);

				if (item.IsDescending)
					StringBuilder.Append(" DESC");

				// NULLS positioning is lowered to a CASE key in the AST for YDB (no native support), so any
				// position remaining here would only be present on a native provider.
				if (item.NullsPosition != Sql.NullsPosition.None)
				{
					StringBuilder.Append(" NULLS ");
					StringBuilder.Append(item.NullsPosition == Sql.NullsPosition.First ? "FIRST" : "LAST");
				}

				if (i + 1 < nonConstant.Count)
					StringBuilder.AppendLine(Comma);
				else
					StringBuilder.AppendLine();
			}

			Indent--;
		}
	}
}
