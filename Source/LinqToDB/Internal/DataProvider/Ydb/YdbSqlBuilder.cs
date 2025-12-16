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
				case DataType.DateTime2  : StringBuilder.Append("Timestamp");    break;
				case DataType.Timestamp64: StringBuilder.Append("Timestamp64");  break;
				case DataType.Interval   : StringBuilder.Append("Interval");     break;
				case DataType.Interval64 : StringBuilder.Append("Interval64");   break;

				//Tz types not supported as column types
				case DataType.DateTz     : StringBuilder.Append(forCreateTable ? "Date"      : "TzDate");      break;
				case DataType.DateTimeTz : StringBuilder.Append(forCreateTable ? "Datetime"  : "TzDatetime");  break;
				case DataType.DateTime2Tz: StringBuilder.Append(forCreateTable ? "Timestamp" : "TzTimestamp"); break;

				case DataType.Decimal:
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
						return sb.Append('`').Append(value.Replace("`", "\\`")).Append('`');

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
						return sb.Append('`').Append(value.Replace("`", "\\`")).Append('`');

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

		protected override bool IsCteColumnListSupported => false;

		protected override void BuildWithClause(SqlWithClause? with)
		{
			if (with == null || with.Clauses.Count == 0)
				return;

			foreach (var cte in with.Clauses)
			{
				// TODO: we should ensure that cte name doesn't conflict with parameter name
				// see BasicSqlOptimizer.FinalizeCte
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
			if (!statement.IsUpdate())
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

			if (items.Count == 0)
			{
				BuildPredicate(new SqlPredicate.IsNull(predicate.Expr1, predicate.IsNot));
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
			if (hasNull)
			{
				StringBuilder.Append(predicate.IsNot ? " AND " : " OR ");
				BuildPredicate(new SqlPredicate.IsNull(predicate.Expr1, predicate.IsNot));
			}

			if (bucketIndex > 1 || hasNull)
			{
				StringBuilder.Insert(startLen, "(").Append(')');
			}
		}

		protected override void BuildMergeStatement(SqlMergeStatement merge) => throw new LinqToDBException($"{Name} provider doesn't support SQL MERGE statement");

		public override StringBuilder BuildObjectName(StringBuilder sb, SqlObjectName name, ConvertType objectType, bool escape, TableOptions tableOptions, bool withoutSuffix = false)
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
			StringBuilder.Append("Unwrap(");
			base.BuildSqlCastExpression(castExpression);
			StringBuilder.Append(')');
		}
	}
}
