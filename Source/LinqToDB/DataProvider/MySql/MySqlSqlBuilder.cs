using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.MySql
{
	using SqlQuery;
	using SqlProvider;
	using LinqToDB.Extensions;

	class MySqlSqlBuilder : BasicSqlBuilder
	{
		public MySqlSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
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
			return new MySqlSqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
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
						"LIMIT {0},{1}",
						WithStringBuilder(new StringBuilder(), () => BuildExpression(selectQuery.Select.SkipValue)),
						selectQuery.Select.TakeValue == null ?
							long.MaxValue.ToString() :
							WithStringBuilder(new StringBuilder(), () => BuildExpression(selectQuery.Select.TakeValue).ToString()))
					.AppendLine();
			}
		}

		protected override void BuildDataType(SqlDataType type, bool createDbType)
		{
			switch (type.DataType)
			{
				case DataType.SByte         : StringBuilder.Append("TINYINT");                       break;
				case DataType.Int16         : StringBuilder.Append("SMALLINT");                      break;
				case DataType.Int32         : StringBuilder.Append("INT");                           break;
				case DataType.Int64         : StringBuilder.Append("BIGINT");                        break;
				case DataType.Byte          : StringBuilder.Append("TINYINT UNSIGNED");              break;
				case DataType.UInt16        : StringBuilder.Append("SMALLINT UNSIGNED");             break;
				case DataType.UInt32        : StringBuilder.Append("INT UNSIGNED");                  break;
				case DataType.UInt64        : StringBuilder.Append("BIGINT UNSIGNED");               break;
				case DataType.Money         : StringBuilder.Append("DECIMAL(19,4)");                 break;
				case DataType.SmallMoney    : StringBuilder.Append("DECIMAL(10,4)");                 break;
				case DataType.Decimal       :
					if (type.Scale != null && type.Scale != 0)
						StringBuilder.Append($"DECIMAL({type.Precision ?? 10},{type.Scale})");
					else if (type.Precision != null && type.Precision != 10)
						StringBuilder.Append($"DECIMAL({type.Precision})");
					else
						StringBuilder.Append("DECIMAL"); break;
				case DataType.DateTime      :
				case DataType.DateTime2     :
				case DataType.SmallDateTime :
					if (type.Precision > 0 && type.Precision <= 6)
						StringBuilder.Append($"DATETIME({type.Precision})");
					else
						StringBuilder.Append("DATETIME");
					break;
				case DataType.DateTimeOffset:
					if (type.Precision > 0 && type.Precision <= 6)
						StringBuilder.Append($"TIMESTAMP({type.Precision})");
					else
						StringBuilder.Append("TIMESTAMP");
					break;
				case DataType.Time:
					if (type.Precision > 0 && type.Precision <= 6)
						StringBuilder.Append($"TIME({type.Precision})");
					else
						StringBuilder.Append("TIME");
					break;
				case DataType.Boolean       : StringBuilder.Append("BOOLEAN");                       break;
				case DataType.Double        :
					if (type.Precision >= 0 && type.Precision <= 53)
						StringBuilder.Append($"FLOAT({type.Precision})"); // this is correct, FLOAT(p)
					else
						StringBuilder.Append("DOUBLE");
					break;
				case DataType.Single        :
					if (type.Precision >= 0 && type.Precision <= 53)
						StringBuilder.Append($"FLOAT({type.Precision})");
					else
						StringBuilder.Append("FLOAT");
					break;
				case DataType.BitArray:
					{
						var length = type.Length;
						if (length == null)
						{
							var columnType = type.Type?.ToNullableUnderlying();
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
					if (type.Length == null || type.Length > 255 || type.Length < 0)
						StringBuilder.Append("CHAR(255)");
					else if (type.Length == 1)
						StringBuilder.Append("CHAR");
					else
						StringBuilder.Append($"CHAR({type.Length})");
					break;
				case DataType.VarChar       :
				case DataType.NVarChar      :
					if (type.Length == null || type.Length > 255 || type.Length < 0)
						StringBuilder.Append("VARCHAR(255)");
					else
						StringBuilder.Append($"VARCHAR({type.Length})");
					break;
				case DataType.Binary:
					if (type.Length == null || type.Length < 0)
						StringBuilder.Append("BINARY(255)");
					else if (type.Length == 1)
						StringBuilder.Append("BINARY");
					else
						StringBuilder.Append($"BINARY({type.Length})");
					break;
				case DataType.VarBinary:
					if (type.Length == null || type.Length < 0)
						StringBuilder.Append("VARBINARY(255)");
					else
						StringBuilder.Append($"VARBINARY({type.Length})");
					break;
				case DataType.Blob:
					if (type.Length == null || type.Length < 0)
						StringBuilder.Append("BLOB");
					else if (type.Length <= 255)
						StringBuilder.Append("TINYBLOB");
					else if (type.Length <= 65535)
						StringBuilder.Append("BLOB");
					else if (type.Length <= 16777215)
						StringBuilder.Append("MEDIUMBLOB");
					else
						StringBuilder.Append("LONGBLOB");
					break;
				case DataType.NText:
				case DataType.Text:
					if (type.Length == null || type.Length < 0)
						StringBuilder.Append("TEXT");
					else if (type.Length <= 255)
						StringBuilder.Append("TINYTEXT");
					else if (type.Length <= 65535)
						StringBuilder.Append("TEXT");
					else if (type.Length <= 16777215)
						StringBuilder.Append("MEDIUMTEXT");
					else
						StringBuilder.Append("LONGTEXT");
					break;
				default: base.BuildDataType(type, createDbType);                                     break;
			}
		}

		protected override void BuildDeleteClause(SqlDeleteStatement deleteStatement)
		{
			var table = deleteStatement.Table != null ?
				(deleteStatement.SelectQuery.From.FindTableSource(deleteStatement.Table) ?? deleteStatement.Table) :
				deleteStatement.SelectQuery.From.Tables[0];

			AppendIndent()
				.Append("DELETE ")
				.Append(Convert(GetTableAlias(table), ConvertType.NameToQueryTableAlias))
				.AppendLine();
		}

		protected override void BuildUpdateClause(SqlStatement statement, SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			base.BuildFromClause(statement, selectQuery);
			StringBuilder.Remove(0, 4).Insert(0, "UPDATE");
			base.BuildUpdateSet(selectQuery, updateClause);
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

		private static List<char> _convertParameterSymbols;
		public  static List<char>  ConvertParameterSymbols
		{
			get => _convertParameterSymbols;
			set => _convertParameterSymbols = value ?? new List<char>();
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
					return ParameterSymbol + value.ToString();

				case ConvertType.NameToCommandParameter:
					return ParameterSymbol + CommandParameterPrefix + value;

				case ConvertType.NameToSprocParameter:
					{
						var valueStr = value.ToString();

						if(string.IsNullOrEmpty(valueStr))
							throw new ArgumentException("Argument 'value' must represent parameter name.");

						if (valueStr[0] == ParameterSymbol)
							valueStr = valueStr.Substring(1);

						if (valueStr.StartsWith(SprocParameterPrefix, StringComparison.Ordinal))
							valueStr = valueStr.Substring(SprocParameterPrefix.Length);

						return ParameterSymbol + SprocParameterPrefix + valueStr;
					}

				case ConvertType.SprocParameterToName:
					{
						var str = value.ToString();
						str = (str.Length > 0 && (str[0] == ParameterSymbol || (TryConvertParameterSymbol && ConvertParameterSymbols.Contains(str[0])))) ? str.Substring(1) : str;

						if (!string.IsNullOrEmpty(SprocParameterPrefix) && str.StartsWith(SprocParameterPrefix))
							str = str.Substring(SprocParameterPrefix.Length);

						return str;
					}

				case ConvertType.NameToQueryField     :
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
					{
						var name = value.ToString();
						if (name.Length > 0 && name[0] == '`')
							return value;
						return "`" + value + "`";
					}

				case ConvertType.NameToDatabase   :
				case ConvertType.NameToSchema     :
				case ConvertType.NameToQueryTable :
					if (value != null)
					{
						var name = value.ToString();
						if (name.Length > 0 && name[0] == '`')
							return value;

						if (name.IndexOf('.') > 0)
							value = string.Join("`.`", name.Split('.'));

						return "`" + value + "`";
					}

					break;
			}

			return value;
		}

		protected override StringBuilder BuildExpression(
			ISqlExpression expr,
			bool           buildTableName,
			bool           checkParentheses,
			string         alias,
			ref bool       addAlias,
			bool           throwExceptionIfTableNotFound = true)
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
						StringBuilder.Append(',').AppendLine();
					first = false;

					AppendIndent();
					BuildExpression(expr.Column, false, true);
					StringBuilder.Append(" = ");
					BuildExpression(expr.Expression, false, true);
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
			StringBuilder.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
			StringBuilder.Append(")");
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string database, string schema, string table)
		{
			if (database != null && database.Length == 0) database = null;

			if (database != null)
				sb.Append(database).Append(".");

			return sb.Append(table);
		}

		protected override string GetProviderTypeName(IDbDataParameter parameter)
		{
			dynamic p = parameter;
			return p.MySqlDbType.ToString();
		}

		protected override void BuildTruncateTable(SqlTruncateTableStatement truncateTable)
		{
			if (truncateTable.ResetIdentity || truncateTable.Table.Fields.Values.All(f => !f.IsIdentity))
				StringBuilder.Append("TRUNCATE TABLE ");
			else
				StringBuilder.Append("DELETE FROM ");
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			BuildDropTableStatementIfExists(dropTable);
		}
	}
}
