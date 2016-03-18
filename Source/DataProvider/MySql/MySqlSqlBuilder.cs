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

		public override int CommandCount(SelectQuery selectQuery)
		{
			return selectQuery.IsInsert && selectQuery.Insert.WithIdentity ? 2 : 1;
		}

		protected override void BuildCommand(int commandNumber)
		{
			StringBuilder.AppendLine("SELECT LAST_INSERT_ID()");
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new MySqlSqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override string LimitFormat { get { return "LIMIT {0}"; } }

		public override bool IsNestedJoinParenthesisRequired { get { return true; } }

		protected override void BuildOffsetLimit()
		{
			if (SelectQuery.Select.SkipValue == null)
				base.BuildOffsetLimit();
			else
			{
				AppendIndent()
					.AppendFormat(
						"LIMIT {0},{1}",
						WithStringBuilder(new StringBuilder(), () => BuildExpression(SelectQuery.Select.SkipValue)),
						SelectQuery.Select.TakeValue == null ?
							long.MaxValue.ToString() :
							WithStringBuilder(new StringBuilder(), () => BuildExpression(SelectQuery.Select.TakeValue).ToString()))
					.AppendLine();
			}
		}

        protected override void BuildDataType(SqlDataType type, bool createDbType = false)
        {
            switch (type.DataType)
            {
                case DataType.Int16:
                case DataType.Int32:
                case DataType.Int64:
                    if (createDbType) goto default;
                    StringBuilder.Append("Signed");
                    break;
                case DataType.SByte:
                case DataType.Byte:
                case DataType.UInt16:
                case DataType.UInt32:
                case DataType.UInt64:
                    if (createDbType) goto default;
                    StringBuilder.Append("Unsigned");
                    break;
                case DataType.Money: StringBuilder.Append("Decimal(19,4)"); break;
                case DataType.SmallMoney: StringBuilder.Append("Decimal(10,4)"); break;
                case DataType.DateTime2:
                case DataType.SmallDateTime: StringBuilder.Append("DateTime"); break;
                case DataType.Boolean: StringBuilder.Append("Boolean"); break;
                case DataType.Double:
                case DataType.Single: base.BuildDataType(SqlDataType.Decimal); break;
                case DataType.VarChar:
                case DataType.NVarChar:
                    StringBuilder.Append("Char");
                    if (type.Length > 0)
                        StringBuilder.Append('(').Append(type.Length).Append(')');
                    break;
                default: base.BuildDataType(type); break;
            }
        }

        protected override void BuildDeleteClause()
		{
			var table = SelectQuery.Delete.Table != null ?
				(SelectQuery.From.FindTableSource(SelectQuery.Delete.Table) ?? SelectQuery.Delete.Table) :
				SelectQuery.From.Tables[0];

			AppendIndent()
				.Append("DELETE ")
				.Append(Convert(GetTableAlias(table), ConvertType.NameToQueryTableAlias))
				.AppendLine();
		}

		protected override void BuildUpdateClause()
		{
			base.BuildFromClause();
			StringBuilder.Remove(0, 4).Insert(0, "UPDATE");
			base.BuildUpdateSet();
		}

		protected override void BuildFromClause()
		{
			if (!SelectQuery.IsUpdate)
				base.BuildFromClause();
		}

		public static char ParameterSymbol           { get; set; }
		public static bool TryConvertParameterSymbol { get; set; }

		private static string _commandParameterPrefix = string.Empty;
		public  static string  CommandParameterPrefix
		{
			get { return _commandParameterPrefix; }
			set { _commandParameterPrefix = value ?? string.Empty; }
		}

		private static string _sprocParameterPrefix = string.Empty;
		public  static string  SprocParameterPrefix
		{
			get { return _sprocParameterPrefix; }
			set { _sprocParameterPrefix = value ?? string.Empty; }
		}

		private static List<char> _convertParameterSymbols;
		public  static List<char>  ConvertParameterSymbols
		{
			get { return _convertParameterSymbols; }
			set { _convertParameterSymbols = value ?? new List<char>(); }
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
				case ConvertType.NameToOwner      :
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
				buildTableName && SelectQuery.QueryType != QueryType.InsertOrUpdate,
				checkParentheses,
				alias,
				ref addAlias,
				throwExceptionIfTableNotFound);
		}

		protected override void BuildInsertOrUpdateQuery()
		{
			BuildInsertQuery();
			AppendIndent().AppendLine("ON DUPLICATE KEY UPDATE");

			Indent++;

			var first = true;

			foreach (var expr in SelectQuery.Update.Items)
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

		protected override void BuildEmptyInsert()
		{
			StringBuilder.AppendLine("() VALUES ()");
		}

		protected override void BuildCreateTableIdentityAttribute1(SqlField field)
		{
			StringBuilder.Append("AUTO_INCREMENT");
		}

		protected override void BuildCreateTablePrimaryKey(string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY CLUSTERED (");
			StringBuilder.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
			StringBuilder.Append(")");
		}

#if !SILVERLIGHT

		protected override string GetProviderTypeName(IDbDataParameter parameter)
		{
			dynamic p = parameter;
			return p.MySqlDbType.ToString();
		}

#endif
	}
}
