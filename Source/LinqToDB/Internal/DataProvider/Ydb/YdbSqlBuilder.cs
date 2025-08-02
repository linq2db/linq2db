// -----------------------------------------------------------------------------
//  LinqToDB provider : YDB  ✧  SQL‑builder
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Ydb;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	/// <summary>
	///  SQL code builder (YQL) for the YDB provider.
	///  Adapts LINQ to DB expressions to YQL syntax,
	///  supports UPSERT, special types, temporary tables, hints, etc.
	/// </summary>
	public class YdbSqlBuilder : BasicSqlBuilder<YdbOptions>
	{
		//--------------------------------------------------------------------- ctor
		public YdbSqlBuilder(
			IDataProvider? provider,
			MappingSchema mappingSchema,
			DataOptions dataOptions,
			ISqlOptimizer sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		// internal copy-constructor
		private YdbSqlBuilder(YdbSqlBuilder parent) : base(parent) { }

		protected override ISqlBuilder CreateSqlBuilder() => new YdbSqlBuilder(this);

		//--------------------------------------------------------------------- basic syntax

		protected override string LimitFormat(SelectQuery selectQuery) => "LIMIT {0}";
		protected override string OffsetFormat(SelectQuery selectQuery) => "OFFSET {0} ";
		protected override bool IsRecursiveCteKeywordRequired => false;

		//--------------------------------------------------------------------- PRIMARY KEY :: CREATE TABLE

		protected override void BuildCreateTablePrimaryKey(
			SqlCreateTableStatement createTable,
			string pkName,
			IEnumerable<string> fieldNames)
		{
			AppendIndent()
				.Append("PRIMARY KEY (")
				.Append(string.Join(InlineComma, fieldNames))
				.Append(')');
		}

		//--------------------------------------------------------------------- DROP TABLE / TRUNCATE

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
			=> BuildDropTableStatementIfExists(dropTable);

		protected override void BuildTruncateTableStatement(SqlTruncateTableStatement truncateTable)
		{
			BuildTag(truncateTable);
			AppendIndent().Append("TRUNCATE TABLE ");
			BuildPhysicalTable(truncateTable.Table!, null);
			StringBuilder.AppendLine();
		}

		//--------------------------------------------------------------------- NULL / IDENTITY / RETURNING

		protected override void BuildCreateTableNullAttribute(SqlField field, DefaultNullable defaultNullable)
		{
			if (!field.CanBeNull)
				StringBuilder.Append("NOT NULL");
		}

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			var id = insertClause.Into?.GetIdentityField()
					 ?? throw new LinqToDBException(
						 $"Identity field must be defined for '{insertClause.Into?.NameForLogging}'.");

			AppendIndent().AppendLine("RETURNING");
			AppendIndent().Append('\t');
			BuildExpression(id, false, true);
			StringBuilder.AppendLine();
		}

		//--------------------------------------------------------------------- CREATE TABLE helpers

		protected override void BuildCreateTableFieldType(SqlField field)
		{
			if (field.IsIdentity)
			{
				StringBuilder.Append(field.Type.DataType switch
				{
					DataType.Int64 => "BigSerial",
					DataType.Int16 => "SmallSerial",
					_ => "Serial"
				});
				return;
			}

			base.BuildCreateTableFieldType(field);
		}

		protected override void BuildCreateTableCommand(SqlTable table)
		{
			var cmd = table.TableOptions.IsTemporaryOptionSet()
				? "CREATE TEMPORARY TABLE "
				: "CREATE TABLE ";

			StringBuilder.Append(cmd);

			if (table.TableOptions.HasCreateIfNotExists())
				StringBuilder.Append("IF NOT EXISTS ");
		}

		//--------------------------------------------------------------------- data types + quoting

		protected override void BuildDataTypeFromDataType(
			DbDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.DataType)
			{
				case DataType.Boolean: StringBuilder.Append("Bool"); break;
				case DataType.SByte: StringBuilder.Append("Int8"); break;
				case DataType.Byte: StringBuilder.Append("Uint8"); break;
				case DataType.Int16: StringBuilder.Append("Int16"); break;
				case DataType.UInt16: StringBuilder.Append("Uint16"); break;
				case DataType.Int32: StringBuilder.Append("Int32"); break;
				case DataType.UInt32: StringBuilder.Append("Uint32"); break;
				case DataType.Int64: StringBuilder.Append("Int64"); break;
				case DataType.UInt64: StringBuilder.Append("Uint64"); break;
				case DataType.Single: StringBuilder.Append("Float"); break;
				case DataType.Double: StringBuilder.Append("Double"); break;
				case DataType.Decimal:
					StringBuilder.AppendFormat(
						CultureInfo.InvariantCulture,
						"Decimal({0},{1})",
						YdbMappingSchema.DEFAULT_DECIMAL_PRECISION,
						YdbMappingSchema.DEFAULT_DECIMAL_SCALE);
					break;
				case DataType.Date: StringBuilder.Append("Date"); break;
				case DataType.DateTime: StringBuilder.Append("Datetime"); break;
				case DataType.DateTime2: StringBuilder.Append("Timestamp"); break;
				case DataType.DateTimeOffset: StringBuilder.Append("TzTimestamp"); break;
				case DataType.Interval: StringBuilder.Append("Interval"); break;

				case DataType.Char or DataType.NChar or
					 DataType.VarChar or DataType.NVarChar or
					 DataType.Text or DataType.NText:
					StringBuilder.Append("Utf8"); break;

				case DataType.Binary or DataType.VarBinary or DataType.Blob:
					StringBuilder.Append("Bytes"); break;

				case DataType.Guid: StringBuilder.Append("Uuid"); break;
				case DataType.Json: StringBuilder.Append("JsonDocument"); break;

				default:
					base.BuildDataTypeFromDataType(type, false, false);
					break;
			}
		}

		protected override bool IsReserved(string word)
			=> ReservedWords.IsReserved(word, ProviderName.Ydb);

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter
				   or ConvertType.NameToCommandParameter
				   or ConvertType.NameToSprocParameter:
					return sb.Append('@').Append(value);

				case ConvertType.SprocParameterToName:
					return sb.Append(value.TrimStart('@', '$', ':'));

				case ConvertType.NameToQueryField
				   or ConvertType.NameToQueryFieldAlias
				   or ConvertType.NameToQueryTable
				   or ConvertType.NameToQueryTableAlias
				   or ConvertType.NameToDatabase
				   or ConvertType.NameToSchema
				   or ConvertType.NameToProcedure:
				{
					var quote = value.Any(char.IsUpper)
							|| IsReserved(value)
							|| value.Length == 0 || value[0] is not '_' and not (>= 'a' and <= 'z')
							|| value.Skip(1).Any(c => !(char.IsLetterOrDigit(c) || c is '_' or '$'));

					if (quote)
						return sb.Append('`').Append(value.Replace("`", "``")).Append('`');

					return sb.Append(value);
				}

				default: return sb.Append(value);
			}
		}

		//--------------------------------------------------------------------- MERGE → UPSERT

		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			var ins = merge.Operations
			   .FirstOrDefault(op => op.OperationType == MergeOperationType.Insert);
			if (ins == null || ins.Items.Count == 0)
				return;

			AppendIndent().Append("UPSERT INTO ");
			BuildPhysicalTable(merge.Target.Source, merge.Target.Alias);

			StringBuilder.Append(" (");
			for (int i = 0; i < ins.Items.Count; i++)
			{
				if (i > 0) StringBuilder.Append(", ");
				var addAlias = false;
				BuildExpression(ins.Items[i].Column, false, true, null, ref addAlias);
			}

			StringBuilder.Append(')');

			StringBuilder.AppendLine();
			AppendIndent().Append("SELECT ");
			for (int i = 0; i < ins.Items.Count; i++)
			{
				if (i > 0) StringBuilder.Append(", ");
				var addAlias = false;
				BuildExpression(ins.Items[i].Expression!, false, true, null, ref addAlias);
			}

			StringBuilder.AppendLine();
			AppendIndent().Append("FROM ");
			BuildMergeSourceQuery(NullabilityContext, merge.Source);

			BuildMergeTerminator(NullabilityContext, merge);
		}

		//---------------------------------------------------------------------
		//  UPDATE without alias
		//---------------------------------------------------------------------
		/// <summary>
		/// Builds the <c>UPDATE … SET …</c> section without using an alias
		/// (a requirement of YDB/YQL syntax).
		/// </summary>
		protected override void BuildUpdateClause(
			SqlStatement statement,
			SelectQuery selectQuery,
			SqlUpdateClause updateClause)
		{
			// -----------------------------------------------------------------
			// Clear auto-generated alias on the target table
			// -----------------------------------------------------------------
			if (selectQuery.From.Tables.Count == 1)
			{
				var ts = selectQuery.From.Tables[0];
				ts.Alias = null;
				if (ts.Source is SqlTable tbl)
					tbl.Alias = null;
			}

			// -----------------------------------------------------------------
			//  UPDATE <Table>
			// -----------------------------------------------------------------
			AppendIndent();
			StringBuilder.Append("UPDATE ");
			BuildPhysicalTable(selectQuery.From.Tables[0].Source, null);
			StringBuilder.AppendLine();

			// -----------------------------------------------------------------
			//  SET  col = expr [, …]
			// -----------------------------------------------------------------
			AppendIndent();
			StringBuilder.Append("SET ");

			for (int i = 0; i < updateClause.Items.Count; i++)
			{
				if (i > 0)
				{
					StringBuilder.Append(',');
					StringBuilder.AppendLine();
					AppendIndent();
					StringBuilder.Append("    ");
				}

				var item = updateClause.Items[i];

				// col =
				BuildExpression(item.Column, false, true);
				StringBuilder.Append(" = ");

				// expr
				var addAlias = false;
				BuildExpression(item.Expression!, false, true, null, ref addAlias);
			}

			StringBuilder.AppendLine();
		}

		//---------------------------------------------------------------------
		// DELETE without alias
		//---------------------------------------------------------------------
		protected override void BuildDeleteQuery(SqlDeleteStatement deleteStatement)
		{
			if (deleteStatement.SelectQuery.From.Tables.Count == 1)
			{
				var ts = deleteStatement.SelectQuery.From.Tables[0];
				ts.Alias = null;
				if (ts.Source is SqlTable tbl)
					tbl.Alias = null;

				// -----------------------------------------------------------------
				// WITH / TAG Clauses
				// -----------------------------------------------------------------
				BuildStep = Step.Tag;
				BuildTag(deleteStatement);

				BuildStep = Step.WithClause;
				BuildWithClause(deleteStatement.With);

				// -----------------------------------------------------------------
				// DELETE FROM <Table>        — without alias
				// -----------------------------------------------------------------
				BuildStep = Step.DeleteClause;
				AppendIndent();
				StringBuilder.Append("DELETE");
				StartStatementQueryExtensions(deleteStatement.SelectQuery);
				BuildSkipFirst(deleteStatement.SelectQuery);
				StringBuilder.AppendLine();

				AppendIndent().Append("FROM ");
				BuildPhysicalTable(deleteStatement.SelectQuery.From.Tables[0].Source, null);
				StringBuilder.AppendLine();

				// -----------------------------------------------------------------
				// WHERE / GROUP BY / HAVING / ORDER BY / LIMIT
				// (all rendered **without** aliases)
				// -----------------------------------------------------------------
				var savedSkipAlias = SkipAlias;
				SkipAlias = true;

				BuildStep = Step.WhereClause;
				BuildWhereClause(deleteStatement.SelectQuery);

				BuildStep = Step.GroupByClause;
				BuildGroupByClause(deleteStatement.SelectQuery);

				BuildStep = Step.HavingClause;
				BuildHavingClause(deleteStatement.SelectQuery);

				BuildStep = Step.OrderByClause;
				BuildOrderByClause(deleteStatement.SelectQuery);

				BuildStep = Step.OffsetLimit;
				BuildOffsetLimit(deleteStatement.SelectQuery);

				SkipAlias = savedSkipAlias; // restore original alias state

				// -----------------------------------------------------------------
				// Query-level hints, if specified
				// -----------------------------------------------------------------
				BuildStep = Step.QueryExtensions;
				BuildSubQueryExtensions(deleteStatement);
			}
			else
			{
				base.BuildDeleteQuery(deleteStatement);
			}
		}

		//---------------------------------------------------------------------
		// Sequence Mock Implementation
		//---------------------------------------------------------------------
		public override string GetReserveSequenceValuesSql(int count, string sequenceName)
		{
			return FormattableString.Invariant(
				$"SELECT * FROM GENERATE_SERIES(1,{count}) AS id; -- YDB doesn’t support sequences"
			);
		}

		//--------------------------------------------------------------------- ✧ QUERY HINTS ✧
		//
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
				Sql.QueryExtensionScope.QueryHint);
		}
	}
}
