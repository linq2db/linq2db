using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.MySql
{
	using SqlQuery;
	using SqlProvider;
	using LinqToDB.Mapping;

	class MySqlSqlBuilder : BasicSqlBuilder
	{
		private readonly MySqlDataProvider? _provider;

		public MySqlSqlBuilder(MySqlDataProvider? provider, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
			_provider = provider;
		}

		public MySqlSqlBuilder(MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
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
						"LIMIT {0},{1}",
						WithStringBuilder(new StringBuilder(), () => BuildExpression(selectQuery.Select.SkipValue)),
						selectQuery.Select.TakeValue == null ?
							long.MaxValue.ToString() :
							WithStringBuilder(new StringBuilder(), () => BuildExpression(selectQuery.Select.TakeValue).ToString()))
					.AppendLine();
			}
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.DataType)
			{
				case DataType.Int16         :
				case DataType.Int32         :
				case DataType.Int64         :
					if (forCreateTable) goto default;
					StringBuilder.Append("Signed");
					break;
				case DataType.SByte         :
				case DataType.Byte          :
				case DataType.UInt16        :
				case DataType.UInt32        :
				case DataType.UInt64        :
					if (forCreateTable) goto default;
					StringBuilder.Append("Unsigned");
					break;
				case DataType.Money         : StringBuilder.Append("Decimal(19,4)");                               break;
				case DataType.SmallMoney    : StringBuilder.Append("Decimal(10,4)");                               break;
				case DataType.DateTime2     :
				case DataType.SmallDateTime : StringBuilder.Append("DateTime");                                    break;
				case DataType.Boolean       : StringBuilder.Append("Boolean");                                     break;
				case DataType.Double        :
				case DataType.Single        : base.BuildDataTypeFromDataType(SqlDataType.Decimal, forCreateTable); break;
				case DataType.VarChar       :
				case DataType.NVarChar      :
					// yep, char(0) is allowed
					if (type.Length == null || type.Length > 255 || type.Length < 0)
						StringBuilder.Append("Char(255)");
					else
						StringBuilder.Append($"Char({type.Length})");
					break;
				default: base.BuildDataTypeFromDataType(type, forCreateTable);                                     break;
			}
		}

		protected override void BuildDeleteClause(SqlDeleteStatement deleteStatement)
		{
			var table = deleteStatement.Table != null ?
				(deleteStatement.SelectQuery.From.FindTableSource(deleteStatement.Table) ?? deleteStatement.Table) :
				deleteStatement.SelectQuery.From.Tables[0];

			AppendIndent()
				.Append("DELETE ")
				.Append(Convert(GetTableAlias(table)!, ConvertType.NameToQueryTableAlias))
				.AppendLine();
		}

		protected override void BuildUpdateClause(SqlStatement statement, SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			base.BuildFromClause(statement, selectQuery);
			StringBuilder.Remove(0, 4).Insert(0, "UPDATE");
			base.BuildUpdateSet(selectQuery, updateClause);
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
			get => _convertParameterSymbols == null ? (_convertParameterSymbols = new List<char>()) : _convertParameterSymbols;
			set => _convertParameterSymbols = value ?? new List<char>();
		}

		public override string Convert(string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
					return ParameterSymbol + value;

				case ConvertType.NameToCommandParameter:
					return ParameterSymbol + CommandParameterPrefix + value;

				case ConvertType.NameToSprocParameter:
					if(string.IsNullOrEmpty(value))
						throw new ArgumentException("Argument 'value' must represent parameter name.");

					if (value[0] == ParameterSymbol)
						value = value.Substring(1);

					if (value.StartsWith(SprocParameterPrefix, StringComparison.Ordinal))
						value = value.Substring(SprocParameterPrefix.Length);

					return ParameterSymbol + SprocParameterPrefix + value;

				case ConvertType.SprocParameterToName:
					value = (value.Length > 0 && (value[0] == ParameterSymbol || (TryConvertParameterSymbol && ConvertParameterSymbols.Contains(value[0])))) ? value.Substring(1) : value;

					if (!string.IsNullOrEmpty(SprocParameterPrefix) && value.StartsWith(SprocParameterPrefix))
						value = value.Substring(SprocParameterPrefix.Length);

					return value;

				case ConvertType.NameToQueryField     :
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
					if (value.Length > 0 && value[0] == '`')
						return value;
					return "`" + value + "`";

				case ConvertType.NameToDatabase   :
				case ConvertType.NameToSchema     :
				case ConvertType.NameToQueryTable :
					if (value.Length > 0 && value[0] == '`')
						return value;

					if (value.IndexOf('.') > 0)
						value = string.Join("`.`", value.Split('.'));

					return "`" + value + "`";
			}

			return value;
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

		public override StringBuilder BuildTableName(StringBuilder sb, string? server, string? database, string? schema, string table)
		{
			if (database != null && database.Length == 0) database = null;

			if (database != null)
				sb.Append(database).Append(".");

			return sb.Append(table);
		}

		protected override string? GetProviderTypeName(IDbDataParameter parameter)
		{
			if (_provider != null)
			{
				var wrapper = MySqlWrappers.Initialize(_provider);

				var param = _provider.TryConvertParameter(wrapper.ParameterType, parameter, MappingSchema);
				if (param != null)
					return wrapper.GetParameterType(param).ToString();
			}

			return base.GetProviderTypeName(parameter);
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

		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			throw new LinqToDBException($"{Name} provider doesn't support SQL MERGE statement");
		}
	}
}
