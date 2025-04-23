using System;
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
	///     SQL builder for YDB (Yandex DataBase) provider (YQL dialect).
	///     The implementation follows the same pattern used by other providers
	///     shipped with LINQ to DB and is focused on features currently
	///     supported by the official <c>ydb-dotnet-sdk</c>.
	/// </summary>
	public sealed class YdbSqlBuilder : BasicSqlBuilder<YdbOptions>
	{
		public YdbSqlBuilder(IDataProvider? provider,
							 MappingSchema mappingSchema,
							 DataOptions dataOptions,
							 ISqlOptimizer sqlOptimizer,
							 SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		/// <summary>
		///     Private copy‑constructor used by <see cref="CreateSqlBuilder"/>.
		/// </summary>
		private YdbSqlBuilder(YdbSqlBuilder parent) : base(parent) { }

		/// <inheritdoc />
		protected override ISqlBuilder CreateSqlBuilder() => new YdbSqlBuilder(this);

		#region General SQL syntax overrides

		protected override string LimitFormat(SelectQuery selectQuery) => "LIMIT {0}";
		protected override string OffsetFormat(SelectQuery selectQuery) => "OFFSET {0} ";

		/// <summary>
		///     YQL supports CTE but doesn’t require the <c>RECURSIVE</c> keyword,
		///     so we keep the default value (<c>false</c>).
		/// </summary>
		protected override bool IsRecursiveCteKeywordRequired => false;

		#endregion

		#region Identity / sequences

		/// <summary>
		///     YDB doesn’t expose the concept of “identity”/“auto‑increment”
		///     that can be fetched back in the same statement,
		///     therefore we refuse to generate a <c>RETURNING</c> clause.
		/// </summary>
		protected override void BuildGetIdentity(SqlInsertClause insertClause)
			=> throw new LinqToDBException(
				"YDB provider cannot generate SQL to fetch the last auto‑generated identity value. " +
				"Use PRIMARY KEY values supplied by the application instead.");

		#endregion

		#region Tables DDL

		protected override void BuildTruncateTableStatement(SqlTruncateTableStatement truncateTable)
		{
			// YDB: обычный TRUNCATE, без сброса идентичности (их просто нет)
			BuildTag(truncateTable);
			AppendIndent();
			StringBuilder.Append("TRUNCATE TABLE ");
			BuildPhysicalTable(truncateTable.Table!, null);
			StringBuilder.AppendLine();
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			// IF EXISTS формируется базовым методом
			BuildDropTableStatementIfExists(dropTable);
		}

		protected override void BuildCreateTableCommand(SqlTable table)
		{
			string command;

			if (table.TableOptions.IsTemporaryOptionSet())
			{
				// TEMPORARY таблицы поддерживаются, других вариантов «локальности» у YDB нет
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
					case var value:
						throw new InvalidOperationException($"Incompatible table options '{value}'");
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

		protected override void BuildEndCreateTableStatement(SqlCreateTableStatement createTable)
		{
			// У YDB нет ON COMMIT‑опций — просто закрываем скобку и вызываем базовую реализацию
			base.BuildEndCreateTableStatement(createTable);
		}

		#endregion

		#region Type mapping

		/// <inheritdoc />
		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			// nullable columns use Optional<T> wrapper in YQL,
			// but SQL builder must output plain type names.
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
					// YDB implements high‑precision decimal with fixed (38,9) scale.
					StringBuilder.Append("Decimal(38,9)");
					break;

				case DataType.Date: StringBuilder.Append("Date"); break;
				case DataType.DateTime: StringBuilder.Append("Datetime"); break;
				case DataType.DateTime2: StringBuilder.Append("Timestamp"); break;
				case DataType.DateTimeOffset:
					// Time‑zone aware timestamp.
					StringBuilder.Append("TzTimestamp");
					break;

				case DataType.Interval: StringBuilder.Append("Interval"); break;

				case DataType.Char:
				case DataType.NChar:
				case DataType.VarChar:
				case DataType.NVarChar:
				case DataType.Text:
				case DataType.NText:
					StringBuilder.Append("Utf8");
					break;

				case DataType.Binary:
				case DataType.VarBinary:
				case DataType.Blob:
					StringBuilder.Append("Bytes");
					break;

				case DataType.Guid: StringBuilder.Append("Uuid"); break;

				case DataType.Json: StringBuilder.Append("JsonDocument"); break;

				default:
					base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
					break;
			}
		}

		#endregion

		#region Identifier quoting / reserved words

		protected override bool IsReserved(string word)
			=> ReservedWords.IsReserved(word, ProviderName.Ydb);

		/// <inheritdoc />
		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					// The YDB ADO.NET provider happily accepts @param and rewrites it to $param.
					return sb.Append('@').Append(value);

				case ConvertType.SprocParameterToName:
					return sb.Append(value.TrimStart('@', '$', ':'));

				// Identifier quoting ------------------------------------------------------------
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

		#region MERGE / UPSERT

		/// <summary>
		///     YQL has its own <c>UPSERT</c> statement, however LINQ to DB’s
		///     higher‑level Merge‑API generates complex MERGE commands which are
		///     not yet implemented for YDB.  Until the advanced MERGE support
		///     is added, we throw a clear, descriptive exception.
		/// </summary>
		protected override void BuildMergeStatement(SqlMergeStatement merge)
			=> throw new LinqToDBException(
				$"{Name} provider doesn’t support LINQ to DB SQL MERGE yet. " +
				"Use BulkCopy/Upsert methods or plain SQL instead.");

		#endregion

		#region Utility

		public override string GetReserveSequenceValuesSql(int count, string sequenceName)
			=> FormattableString.Invariant(
				$"SELECT * FROM GENERATE_SERIES(1,{count}) AS id; -- YDB doesn’t support sequences");

		#endregion
	}
}
