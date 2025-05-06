using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Common.Internal;
using LinqToDB.Data;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Ydb
{
	/// <summary>
	///     SQL builder implementation for the YDB (Yandex Database) data provider.
	///     This builder translates LINQ to DB expressions into YQL-compliant SQL statements.
	///     It respects YDB syntax conventions, limitations, and extended features such as UPSERT,
	///     custom data types (e.g., Uuid, JsonDocument), and temporary table semantics.
	/// </summary>
	public sealed class YdbSqlBuilder : BasicSqlBuilder<YdbOptions>
	{
		/// <summary>
		///     Standard constructor for the YDB SQL builder.
		/// </summary>
		public YdbSqlBuilder(IDataProvider? provider,
							 MappingSchema mappingSchema,
							 DataOptions dataOptions,
							 ISqlOptimizer sqlOptimizer,
							 SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		/// <summary>
		///     Copy constructor used internally for cloning.
		/// </summary>
		private YdbSqlBuilder(YdbSqlBuilder parent) : base(parent) { }

		/// <inheritdoc />
		protected override ISqlBuilder CreateSqlBuilder() => new YdbSqlBuilder(this);

		#region General SQL Syntax

		/// <inheritdoc />
		protected override string LimitFormat(SelectQuery selectQuery) => "LIMIT {0}";

		/// <inheritdoc />
		protected override string OffsetFormat(SelectQuery selectQuery) => "OFFSET {0} ";

		/// <summary>
		///     Indicates whether the CTE keyword RECURSIVE is required.
		///     For YQL, it is not needed.
		/// </summary>
		protected override bool IsRecursiveCteKeywordRequired => false;

		#endregion

		#region CREATE/DROP Table Adjustments

		/// <summary>
		///     Builds PRIMARY KEY clause for CREATE TABLE without constraint name,
		///     as YQL doesn't support named constraints.
		/// </summary>
		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder
				.Append("PRIMARY KEY (")
				.Append(string.Join(InlineComma, fieldNames))
				.Append(')');
		}

		/// <summary>
		///     Uses <c>IF EXISTS</c> for DROP TABLE by delegating to the base implementation.
		/// </summary>
		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			BuildDropTableStatementIfExists(dropTable);
		}

		/// <summary>
		///     Outputs <c>NOT NULL</c> only when necessary, as YDB does not accept the <c>NULL</c> keyword explicitly.
		/// </summary>
		protected override void BuildCreateTableNullAttribute(SqlField field, DefaultNullable defaultNullable)
		{
			if (!field.CanBeNull)
				StringBuilder.Append("NOT NULL");
		}

		#endregion

		#region Identity / Sequences

		/// <summary>
		///     Appends <c>RETURNING</c> clause for identity column retrieval after insert.
		///     Assumes serial columns are used.
		/// </summary>
		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			var identityField = insertClause.Into?.GetIdentityField();
			if (identityField == null)
				throw new LinqToDBException($"Identity field must be defined for '{insertClause.Into?.NameForLogging}'.");

			AppendIndent().AppendLine("RETURNING");
			AppendIndent().Append('\t');
			BuildExpression(identityField, false, true);
			StringBuilder.AppendLine();
		}

		#endregion

		#region TRUNCATE / CREATE TABLE

		/// <summary>
		///     Generates a TRUNCATE TABLE statement compatible with YQL (without identity resets).
		/// </summary>
		protected override void BuildTruncateTableStatement(SqlTruncateTableStatement truncateTable)
		{
			BuildTag(truncateTable);
			AppendIndent();
			StringBuilder.Append("TRUNCATE TABLE ");
			BuildPhysicalTable(truncateTable.Table!, null);
			StringBuilder.AppendLine();
		}

		/// <summary>
		///     Emits type names for identity (serial) columns.
		///     Falls back to base logic for other fields.
		/// </summary>
		protected override void BuildCreateTableFieldType(SqlField field)
		{
			if (field.IsIdentity)
			{
				switch (field.Type.DataType)
				{
					case DataType.Int64: StringBuilder.Append("BigSerial"); break;
					case DataType.Int16: StringBuilder.Append("SmallSerial"); break;
					default: StringBuilder.Append("Serial"); break;
				}

				return;
			}

			base.BuildCreateTableFieldType(field);
		}

		/// <summary>
		///     Constructs CREATE TABLE command with support for temporary tables.
		/// </summary>
		protected override void BuildCreateTableCommand(SqlTable table)
		{
			string command;

			if (table.TableOptions.IsTemporaryOptionSet())
			{
				switch (table.TableOptions & TableOptions.IsTemporaryOptionSet)
				{
					case TableOptions.IsTemporary:
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryData:
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure:
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData:
					case TableOptions.IsLocalTemporaryData:
					case TableOptions.IsLocalTemporaryStructure:
						command = "CREATE TEMPORARY TABLE ";
						break;
					default:
						throw new InvalidOperationException($"Incompatible table options '{table.TableOptions}'");
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

		/// <summary>
		///     Finalizes CREATE TABLE statement. No ON COMMIT support in YQL.
		/// </summary>
		protected override void BuildEndCreateTableStatement(SqlCreateTableStatement createTable)
		{
			base.BuildEndCreateTableStatement(createTable);
		}

		#endregion

		#region Type Mapping

		/// <summary>
		///     Maps .NET <see cref="DataType"/> values to YDB native SQL types.
		///     Ensures compatibility with YQL type system.
		/// </summary>
		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
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
				case DataType.Char:
				case DataType.NChar:
				case DataType.VarChar:
				case DataType.NVarChar:
				case DataType.Text:
				case DataType.NText: StringBuilder.Append("Utf8"); break;
				case DataType.Binary:
				case DataType.VarBinary:
				case DataType.Blob: StringBuilder.Append("Bytes"); break;
				case DataType.Guid: StringBuilder.Append("Uuid"); break;
				case DataType.Json: StringBuilder.Append("JsonDocument"); break;
				default: base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull); break;
			}
		}

		#endregion

		#region Quoting and Reserved Words

		/// <summary>
		///     Determines whether the specified word is reserved in YQL.
		/// </summary>
		protected override bool IsReserved(string word) =>
			ReservedWords.IsReserved(word, ProviderName.Ydb);

		/// <inheritdoc />
		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return sb.Append('@').Append(value);

				case ConvertType.SprocParameterToName:
					return sb.Append(value.TrimStart('@', '$', ':'));

				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTable:
				case ConvertType.NameToQueryTableAlias:
				case ConvertType.NameToDatabase:
				case ConvertType.NameToSchema:
				case ConvertType.NameToProcedure:
					if (ProviderOptions.IdentifierQuoteMode != YdbIdentifierQuoteMode.None)
					{
						var shouldQuote =
							   ProviderOptions.IdentifierQuoteMode == YdbIdentifierQuoteMode.Quote
							|| ProviderOptions.IdentifierQuoteMode == YdbIdentifierQuoteMode.Auto   && value.Any(char.IsUpper)
							|| IsReserved(value)
							|| value.Length == 0 || value[0] is not '_' and not (>= 'a' and <= 'z')
							|| value.Skip(1).Any(c => !(char.IsLetterOrDigit(c) || c is '_' or '$'));

						if (shouldQuote)
							return sb.Append('`').Append(value.Replace("`", "``")).Append('`');
					}

					return sb.Append(value);

				default:
					return sb.Append(value);
			}
		}

		#endregion

		#region MERGE / UPSERT Support

		/// <summary>
		///     Implements YDB-style UPSERT logic.
		///     Only handles simple INSERT-style operations from MERGE.
		/// </summary>
		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			var insertOp = merge.Operations
				.FirstOrDefault(op => op.OperationType == MergeOperationType.Insert);
			if (insertOp == null || insertOp.Items.Count == 0)
				return;

			AppendIndent().Append("UPSERT INTO ");
			BuildPhysicalTable(merge.Target.Source, merge.Target.Alias);

			StringBuilder.Append(" (");
			for (int i = 0; i < insertOp.Items.Count; i++)
			{
				if (i > 0) StringBuilder.Append(", ");
				bool addAlias = false;
				BuildExpression(insertOp.Items[i].Column, false, true, null, ref addAlias);
			}

			StringBuilder.Append(')');

			StringBuilder.AppendLine();
			AppendIndent().Append("SELECT ");
			for (int i = 0; i < insertOp.Items.Count; i++)
			{
				if (i > 0) StringBuilder.Append(", ");
				bool addAlias = false;
				BuildExpression(insertOp.Items[i].Expression!, false, true, null, ref addAlias);
			}

			StringBuilder.AppendLine();
			AppendIndent().Append("FROM ");
			BuildMergeSourceQuery(NullabilityContext, merge.Source);

			BuildMergeTerminator(NullabilityContext, merge);
		}

		#endregion

		#region Sequences

		/// <summary>
		///     YDB does not support sequences. Returns a mock SELECT using <c>GENERATE_SERIES</c>.
		/// </summary>
		public override string GetReserveSequenceValuesSql(int count, string sequenceName) =>
			FormattableString.Invariant(
				$"SELECT * FROM GENERATE_SERIES(1,{count}) AS id; -- YDB doesn’t support sequences");

		#endregion
	}
}
