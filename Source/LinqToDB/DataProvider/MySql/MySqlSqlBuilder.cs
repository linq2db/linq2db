using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.MySql
{
	using SqlQuery;
	using SqlProvider;
	using Mapping;
	using Extensions;
	using Tools;

	class MySqlSqlBuilder : BasicSqlBuilder
	{
		private readonly MySqlDataProvider? _provider;

		public MySqlSqlBuilder(
			MySqlDataProvider? provider,
			MappingSchema      mappingSchema,
			ISqlOptimizer      sqlOptimizer,
			SqlProviderFlags   sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
			_provider = provider;
		}

		// remote context
		public MySqlSqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		static MySqlSqlBuilder()
		{
			ParameterSymbol = '@';
		}

		protected override bool IsRecursiveCteKeywordRequired   => true;
		public    override bool IsNestedJoinParenthesisRequired => true;

		protected override bool CanSkipRootAliases(SqlStatement statement)
		{
			if (statement.SelectQuery != null)
			{
				return statement.SelectQuery.From.Tables.Count > 0;
			}

			return true;
		}

		public override int CommandCount(SqlStatement statement)
		{
			return statement.NeedsIdentity() ? 2 : 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			StringBuilder.AppendLine("SELECT LAST_INSERT_ID()");
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new MySqlSqlBuilder(MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override string LimitFormat(SelectQuery selectQuery)
		{
			return "LIMIT {0}";
		}

		protected override void BuildOffsetLimit(SelectQuery selectQuery)
		{
			if (selectQuery.Select.SkipValue == null)
				base.BuildOffsetLimit(selectQuery);
			else
			{
				AppendIndent()
					.AppendFormat(
						"LIMIT {0}, {1}",
						WithStringBuilder(new StringBuilder(), () => BuildExpression(selectQuery.Select.SkipValue)),
						selectQuery.Select.TakeValue == null ?
							long.MaxValue.ToString() :
							WithStringBuilder(new StringBuilder(), () => BuildExpression(selectQuery.Select.TakeValue).ToString()))
					.AppendLine();
			}
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			// mysql has limited support for types in type-CAST expressions
			if (!forCreateTable)
			{
				switch (type.Type.DataType)
				{
					case DataType.Boolean       :
					case DataType.SByte         :
					case DataType.Int16         :
					case DataType.Int32         :
					case DataType.Int64         : StringBuilder.Append("SIGNED");         break;
					case DataType.BitArray      : // wild guess
					case DataType.Byte          :
					case DataType.UInt16        :
					case DataType.UInt32        :
					case DataType.UInt64        : StringBuilder.Append("UNSIGNED");       break;
					case DataType.Money         : StringBuilder.Append("DECIMAL(19, 4)"); break;
					case DataType.SmallMoney    : StringBuilder.Append("DECIMAL(10, 4)"); break;
					case DataType.DateTime      :
					case DataType.DateTime2     :
					case DataType.SmallDateTime :
					case DataType.DateTimeOffset: StringBuilder.Append("DATETIME");       break;
					case DataType.Time          : StringBuilder.Append("TIME");           break;
					case DataType.Date          : StringBuilder.Append("DATE");           break;
					case DataType.Json          : StringBuilder.Append("JSON");           break;
					case DataType.Guid          : StringBuilder.Append("CHAR(36)");       break;
					// TODO: FLOAT/DOUBLE support in CAST added just recently (v8.0.17)
					// and needs version sniffing
					case DataType.Double        :
					case DataType.Single        : base.BuildDataTypeFromDataType(SqlDataType.Decimal, forCreateTable); break;
					case DataType.Decimal       :
						if (type.Type.Scale != null && type.Type.Scale != 0)
							StringBuilder.Append($"DECIMAL({type.Type.Precision ?? 10}, {type.Type.Scale})");
						else if (type.Type.Precision != null && type.Type.Precision != 10)
							StringBuilder.Append($"DECIMAL({type.Type.Precision})");
						else
							StringBuilder.Append("DECIMAL"); break;
					case DataType.Char          :
					case DataType.NChar         :
					case DataType.VarChar       :
					case DataType.NVarChar      :
					case DataType.NText         :
					case DataType.Text          :
						if (type.Type.Length == null || type.Type.Length > 255 || type.Type.Length < 0)
							StringBuilder.Append("CHAR(255)");
						else if (type.Type.Length == 1)
							StringBuilder.Append("CHAR");
						else
							StringBuilder.Append($"CHAR({type.Type.Length})");
						break;
					case DataType.VarBinary     :
					case DataType.Binary        :
					case DataType.Blob          :
						if (type.Type.Length == null || type.Type.Length < 0)
							StringBuilder.Append("BINARY(255)");
						else if (type.Type.Length == 1)
							StringBuilder.Append("BINARY");
						else
							StringBuilder.Append($"BINARY({type.Type.Length})");
					break;
					default                     : base.BuildDataTypeFromDataType(type, forCreateTable); break;
				}

				return;
			}

			// types for CREATE TABLE statement
			switch (type.Type.DataType)
			{
				case DataType.SByte         : StringBuilder.Append("TINYINT");                       break;
				case DataType.Int16         : StringBuilder.Append("SMALLINT");                      break;
				case DataType.Int32         : StringBuilder.Append("INT");                           break;
				case DataType.Int64         : StringBuilder.Append("BIGINT");                        break;
				case DataType.Byte          : StringBuilder.Append("TINYINT UNSIGNED");              break;
				case DataType.UInt16        : StringBuilder.Append("SMALLINT UNSIGNED");             break;
				case DataType.UInt32        : StringBuilder.Append("INT UNSIGNED");                  break;
				case DataType.UInt64        : StringBuilder.Append("BIGINT UNSIGNED");               break;
				case DataType.Money         : StringBuilder.Append("DECIMAL(19, 4)");                break;
				case DataType.SmallMoney    : StringBuilder.Append("DECIMAL(10, 4)");                break;
				case DataType.Decimal       :
					if (type.Type.Scale != null && type.Type.Scale != 0)
						StringBuilder.Append($"DECIMAL({type.Type.Precision ?? 10}, {type.Type.Scale})");
					else if (type.Type.Precision != null && type.Type.Precision != 10)
						StringBuilder.Append($"DECIMAL({type.Type.Precision})");
					else
						StringBuilder.Append("DECIMAL"); break;
				case DataType.DateTime      :
				case DataType.DateTime2     :
				case DataType.SmallDateTime :
					if (type.Type.Precision > 0 && type.Type.Precision <= 6)
						StringBuilder.Append($"DATETIME({type.Type.Precision})");
					else
						StringBuilder.Append("DATETIME");
					break;
				case DataType.DateTimeOffset:
					if (type.Type.Precision > 0 && type.Type.Precision <= 6)
						StringBuilder.Append($"TIMESTAMP({type.Type.Precision})");
					else
						StringBuilder.Append("TIMESTAMP");
					break;
				case DataType.Time:
					if (type.Type.Precision > 0 && type.Type.Precision <= 6)
						StringBuilder.Append($"TIME({type.Type.Precision})");
					else
						StringBuilder.Append("TIME");
					break;
				case DataType.Boolean       : StringBuilder.Append("BOOLEAN");                       break;
				case DataType.Double        :
					if (type.Type.Precision >= 0 && type.Type.Precision <= 53)
						StringBuilder.Append($"FLOAT({type.Type.Precision})"); // this is correct, FLOAT(p)
					else
						StringBuilder.Append("DOUBLE");
					break;
				case DataType.Single        :
					if (type.Type.Precision >= 0 && type.Type.Precision <= 53)
						StringBuilder.Append($"FLOAT({type.Type.Precision})");
					else
						StringBuilder.Append("FLOAT");
					break;
				case DataType.BitArray:
					{
						var length = type.Type.Length;
						if (length == null)
						{
							var columnType = type.Type.SystemType.ToNullableUnderlying();
							if (columnType == typeof(byte) || columnType == typeof(sbyte))
								length = 8;
							else if (columnType == typeof(short) || columnType == typeof(ushort))
								length = 16;
							else if (columnType == typeof(int) || columnType == typeof(uint))
								length = 32;
							else if (columnType == typeof(long) || columnType == typeof(ulong))
								length = 64;
						}

						if (length != null && length != 1 && length >= 0)
							StringBuilder.Append($"BIT({length})");
						else
							StringBuilder.Append("BIT");
					}
					break;
				case DataType.Date          : StringBuilder.Append("DATE");                          break;
				case DataType.Json          : StringBuilder.Append("JSON");                          break;
				case DataType.Guid          : StringBuilder.Append("CHAR(36)");                      break;
				case DataType.Char          :
				case DataType.NChar         :
					if (type.Type.Length == null || type.Type.Length > 255 || type.Type.Length < 0)
						StringBuilder.Append("CHAR(255)");
					else if (type.Type.Length == 1)
						StringBuilder.Append("CHAR");
					else
						StringBuilder.Append($"CHAR({type.Type.Length})");
					break;
				case DataType.VarChar       :
				case DataType.NVarChar      :
					if (type.Type.Length == null || type.Type.Length > 255 || type.Type.Length < 0)
						StringBuilder.Append("VARCHAR(255)");
					else
						StringBuilder.Append($"VARCHAR({type.Type.Length})");
					break;
				case DataType.Binary:
					if (type.Type.Length == null || type.Type.Length < 0)
						StringBuilder.Append("BINARY(255)");
					else if (type.Type.Length == 1)
						StringBuilder.Append("BINARY");
					else
						StringBuilder.Append($"BINARY({type.Type.Length})");
					break;
				case DataType.VarBinary:
					if (type.Type.Length == null || type.Type.Length < 0)
						StringBuilder.Append("VARBINARY(255)");
					else
						StringBuilder.Append($"VARBINARY({type.Type.Length})");
					break;
				case DataType.Blob:
					if (type.Type.Length == null || type.Type.Length < 0)
						StringBuilder.Append("BLOB");
					else if (type.Type.Length <= 255)
						StringBuilder.Append("TINYBLOB");
					else if (type.Type.Length <= 65535)
						StringBuilder.Append("BLOB");
					else if (type.Type.Length <= 16777215)
						StringBuilder.Append("MEDIUMBLOB");
					else
						StringBuilder.Append("LONGBLOB");
					break;
				case DataType.NText:
				case DataType.Text:
					if (type.Type.Length == null || type.Type.Length < 0)
						StringBuilder.Append("TEXT");
					else if (type.Type.Length <= 255)
						StringBuilder.Append("TINYTEXT");
					else if (type.Type.Length <= 65535)
						StringBuilder.Append("TEXT");
					else if (type.Type.Length <= 16777215)
						StringBuilder.Append("MEDIUMTEXT");
					else
						StringBuilder.Append("LONGTEXT");
					break;
				default: base.BuildDataTypeFromDataType(type, forCreateTable);                       break;
			}
		}

		protected override void BuildDeleteClause(SqlDeleteStatement deleteStatement)
		{
			var table = deleteStatement.Table != null ?
				(deleteStatement.SelectQuery.From.FindTableSource(deleteStatement.Table) ?? deleteStatement.Table) :
				deleteStatement.SelectQuery.From.Tables[0];

			AppendIndent().Append("DELETE ");
			Convert(StringBuilder, GetTableAlias(table)!, ConvertType.NameToQueryTableAlias);
			StringBuilder.AppendLine();
		}

		protected override void BuildUpdateClause(SqlStatement statement, SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			var pos = StringBuilder.Length;

			base.BuildFromClause(statement, selectQuery);

			StringBuilder.Remove(pos, 4).Insert(pos, "UPDATE");

			base.BuildUpdateSet(selectQuery, updateClause);
		}

		protected override void BuildInsertQuery(SqlStatement statement, SqlInsertClause insertClause, bool addAlias)
		{
			BuildStep = Step.InsertClause; BuildInsertClause(statement, insertClause, addAlias);

			if (statement.QueryType == QueryType.Insert && statement.SelectQuery!.From.Tables.Count != 0)
			{
				BuildStep = Step.WithClause;    BuildWithClause(statement.GetWithClause());
				BuildStep = Step.SelectClause;  BuildSelectClause(statement.SelectQuery);
				BuildStep = Step.FromClause;    BuildFromClause(statement, statement.SelectQuery);
				BuildStep = Step.WhereClause;   BuildWhereClause(statement.SelectQuery);
				BuildStep = Step.GroupByClause; BuildGroupByClause(statement.SelectQuery);
				BuildStep = Step.HavingClause;  BuildHavingClause(statement.SelectQuery);
				BuildStep = Step.OrderByClause; BuildOrderByClause(statement.SelectQuery);
				BuildStep = Step.OffsetLimit;   BuildOffsetLimit(statement.SelectQuery);
			}

			if (insertClause.WithIdentity)
				BuildGetIdentity(insertClause);
			else
			{
				BuildReturningSubclause(statement);
			}
		}

		protected override void BuildFromClause(SqlStatement statement, SelectQuery selectQuery)
		{
			if (!statement.IsUpdate())
				base.BuildFromClause(statement, selectQuery);
		}

		public static char ParameterSymbol           { get; set; }
		public static bool TryConvertParameterSymbol { get; set; }

		private static string _commandParameterPrefix = string.Empty;
		public  static string  CommandParameterPrefix
		{
			get => _commandParameterPrefix;
			set => _commandParameterPrefix = value ?? string.Empty;
		}

		private static string _sprocParameterPrefix = string.Empty;
		public  static string  SprocParameterPrefix
		{
			get => _sprocParameterPrefix;
			set => _sprocParameterPrefix = value ?? string.Empty;
		}

		private static List<char>? _convertParameterSymbols;
		public  static List<char>  ConvertParameterSymbols
		{
			get => _convertParameterSymbols ??= new List<char>();
			set => _convertParameterSymbols = value ?? new List<char>();
		}

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
					return sb.Append(ParameterSymbol).Append(value);

				case ConvertType.NameToCommandParameter:
					return sb.Append(ParameterSymbol).Append(CommandParameterPrefix).Append(value);

				case ConvertType.NameToSprocParameter:
					if(string.IsNullOrEmpty(value))
							throw new ArgumentException("Argument 'value' must represent parameter name.");

					if (value[0] == ParameterSymbol)
						value = value.Substring(1);

					if (value.StartsWith(SprocParameterPrefix, StringComparison.Ordinal))
						value = value.Substring(SprocParameterPrefix.Length);

					return sb.Append(ParameterSymbol).Append(SprocParameterPrefix).Append(value);

				case ConvertType.SprocParameterToName:
					value = (value.Length > 0 && (value[0] == ParameterSymbol || (TryConvertParameterSymbol && ConvertParameterSymbols.Contains(value[0])))) ? value.Substring(1) : value;

					if (!string.IsNullOrEmpty(SprocParameterPrefix) && value.StartsWith(SprocParameterPrefix))
						value = value.Substring(SprocParameterPrefix.Length);

					return sb.Append(value);

				case ConvertType.NameToQueryField     :
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
				case ConvertType.NameToDatabase       :
				case ConvertType.NameToSchema         :
				case ConvertType.NameToQueryTable     :
					// https://dev.mysql.com/doc/refman/8.0/en/identifiers.html
					if (value.Contains('`'))
						value = value.Replace("`", "``");

					return sb.Append('`').Append(value).Append('`');
			}

			return sb.Append(value);
		}

		protected override StringBuilder BuildExpression(ISqlExpression expr,
			bool buildTableName,
			bool checkParentheses,
			string? alias,
			ref bool addAlias,
			bool throwExceptionIfTableNotFound = true)
		{
			return base.BuildExpression(
				expr,
				buildTableName && Statement.QueryType != QueryType.InsertOrUpdate,
				checkParentheses,
				alias,
				ref addAlias,
				throwExceptionIfTableNotFound);
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			var position = StringBuilder.Length;

			BuildInsertQuery(insertOrUpdate, insertOrUpdate.Insert, false);

			if (insertOrUpdate.Update.Items.Count > 0)
			{
				AppendIndent().AppendLine("ON DUPLICATE KEY UPDATE");

				Indent++;

				var first = true;

				foreach (var expr in insertOrUpdate.Update.Items)
				{
					if (!first)
						StringBuilder.AppendLine(Comma);
					first = false;

					AppendIndent();
					BuildExpression(expr.Column, false, true);
					StringBuilder.Append(" = ");
					BuildExpression(expr.Expression!, false, true);
				}

				Indent--;

				StringBuilder.AppendLine();
			}
			else
			{
				var sql = StringBuilder.ToString();
				var insertIndex = sql.IndexOf("INSERT", position);

				StringBuilder.Clear()
					.Append(sql.Substring(0, insertIndex))
					.Append("INSERT IGNORE")
					.Append(sql.Substring(insertIndex + "INSERT".Length));
			}
		}

		protected override void BuildEmptyInsert(SqlInsertClause insertClause)
		{
			StringBuilder.AppendLine("() VALUES ()");
		}

		protected override void BuildCreateTableIdentityAttribute1(SqlField field)
		{
			StringBuilder.Append("AUTO_INCREMENT");
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY CLUSTERED (");
			StringBuilder.Append(string.Join(InlineComma, fieldNames));
			StringBuilder.Append(')');
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string? server, string? database, string? schema, string table, TableOptions tableOptions)
		{
			if (database != null && database.Length == 0) database = null;

			if (database != null)
				sb.Append(database).Append('.');

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

		protected override void BuildTruncateTable(SqlTruncateTableStatement truncateTable)
		{
			if (truncateTable.ResetIdentity || truncateTable.Table!.IdentityFields.Count == 0)
				StringBuilder.Append("TRUNCATE TABLE ");
			else
				StringBuilder.Append("DELETE FROM ");
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			BuildDropTableStatementIfExists(dropTable);
		}

		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			throw new LinqToDBException($"{Name} provider doesn't support SQL MERGE statement");
		}

		protected override void BuildGroupByBody(GroupingType groupingType, List<ISqlExpression> items)
		{
			if (groupingType.In(GroupingType.GroupBySets, GroupingType.Default))
			{
				base.BuildGroupByBody(groupingType, items);
				return;
			}

			AppendIndent();

			StringBuilder.Append("GROUP BY");

			StringBuilder.AppendLine();

			Indent++;

			for (var i = 0; i < items.Count; i++)
			{
				AppendIndent();

				BuildExpression(items[i]);

				if (i + 1 < items.Count)
					StringBuilder.AppendLine(Comma);
				else
					StringBuilder.AppendLine();
			}

			Indent--;

			switch (groupingType)
			{
				case GroupingType.Rollup:
					StringBuilder.Append("WITH ROLLUP");
					break;
				case GroupingType.Cube:
					StringBuilder.Append("WITH CUBE");
					break;
				default:
					throw new InvalidOperationException($"Unexpected grouping type: {groupingType}");
			}
		}

		protected override void BuildCreateTableCommand(SqlTable table)
		{
			string command;

			if (table.TableOptions.IsTemporaryOptionSet())
			{
				switch (table.TableOptions & TableOptions.IsTemporaryOptionSet)
				{
					case TableOptions.IsTemporary                                                                              :
					case TableOptions.IsTemporary |                                          TableOptions.IsLocalTemporaryData :
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure                                     :
					case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData :
					case                                                                     TableOptions.IsLocalTemporaryData :
					case                            TableOptions.IsLocalTemporaryStructure                                     :
					case                            TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData :
						command = "CREATE TEMPORARY TABLE ";
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

			if (table.TableOptions.HasCreateIfNotExists())
				StringBuilder.Append("IF NOT EXISTS ");
		}
	}
}
