using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.MySql
{
	using SqlQuery;
	using SqlProvider;

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
				case DataType.Int16         :
				case DataType.Int32         :
				case DataType.Int64         :
					if (createDbType) goto default;
					StringBuilder.Append("Signed");
					break;
				case DataType.SByte         :
				case DataType.Byte          :
				case DataType.UInt16        :
				case DataType.UInt32        :
				case DataType.UInt64        :
					if (createDbType) goto default;
					StringBuilder.Append("Unsigned");
					break;
				case DataType.Money         : StringBuilder.Append("Decimal(19,4)");                 break;
				case DataType.SmallMoney    : StringBuilder.Append("Decimal(10,4)");                 break;
				case DataType.DateTime2     :
				case DataType.SmallDateTime : StringBuilder.Append("DateTime");                      break;
				case DataType.Boolean       : StringBuilder.Append("Boolean");                       break;
				case DataType.Double        :
				case DataType.Single        : base.BuildDataType(SqlDataType.Decimal, createDbType); break;
				case DataType.VarChar       :
				case DataType.NVarChar      :
					// yep, char(0) is allowed
					if (type.Length == null || type.Length > 255 || type.Length < 0)
						StringBuilder.Append("Char(255)");
					else
						StringBuilder.Append($"Char({type.Length})");
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

//		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
//		{
//			var table = dropTable.Table;
//
//			AppendIndent().Append("DROP TABLE ");
//			BuildPhysicalTable(table, null);
//			StringBuilder.AppendLine(" IF EXISTS");
//
//			base.BuildDropTableStatement(dropTable);
//		}
	}
}
