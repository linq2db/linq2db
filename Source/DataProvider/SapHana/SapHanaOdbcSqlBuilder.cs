using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.SapHana
{

	using SqlQuery;
	using SqlProvider;

	class SapHanaOdbcSqlBuilder : BasicSqlBuilder
	{
		public SapHanaOdbcSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}
		
		public override int CommandCount(SelectQuery selectQuery)
		{
			return selectQuery.IsInsert && selectQuery.Insert.WithIdentity ? 2 : 1;
		}

		protected override void BuildCommand(int commandNumber)
		{
			var identityField = SelectQuery.Insert.Into.GetIdentityField();
			var table         = SelectQuery.Insert.Into;

			if (identityField == null || table == null)
				throw new SqlException("Identity field must be defined for '{0}'.", SelectQuery.Insert.Into.Name);

			StringBuilder.Append("SELECT MAX(");
			BuildExpression(identityField, false, true);
			StringBuilder.Append(") FROM ");
			BuildPhysicalTable(table, null);
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SapHanaOdbcSqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override string LimitFormat { get { return "LIMIT {0}"; } }
		protected override string OffsetFormat { get { return SelectQuery.Select.TakeValue == null ? "LIMIT 4200000000 OFFSET {0}" : "OFFSET {0}"; } }
		
		public override bool IsNestedJoinParenthesisRequired { get { return true; } }

		protected override void BuildStartCreateTableStatement(SelectQuery.CreateTableStatement createTable)
		{
			if (createTable.StatementHeader == null)
			{
				AppendIndent().Append("CREATE COLUMN TABLE ");
				BuildPhysicalTable(createTable.Table, null);
			}
			else
			{
				var name = WithStringBuilder(
					new StringBuilder(),
					() =>
					{
						BuildPhysicalTable(createTable.Table, null);
						return StringBuilder.ToString();
					});

				AppendIndent().AppendFormat(createTable.StatementHeader, name);
			}
		}

		protected override void BuildInsertOrUpdateQuery()
		{
			BuildInsertOrUpdateQueryAsUpdateInsert();
		}

		protected override void BuildDataType(SqlDataType type, bool createDbType = false)
		{
			switch (type.DataType)
			{
				case DataType.Int32         :
				case DataType.UInt16        :
					StringBuilder.Append("Integer");
					break;
				case DataType.Double:
					StringBuilder.Append("Double");
					break;
				case DataType.Money         :
					StringBuilder.Append("Decimal(19,4)");
					break;
				case DataType.SmallMoney    : 
					StringBuilder.Append("Decimal(10,4)");
					break;
				case DataType.DateTime2     :
				case DataType.DateTime      :
				case DataType.Time:
					StringBuilder.Append("Timestamp");
					break;                
				case DataType.SmallDateTime : 
					StringBuilder.Append("SecondDate");
					break;
				case DataType.Boolean       : 
					StringBuilder.Append("TinyInt");
					break;
				case DataType.Image:
					StringBuilder.Append("Blob");
					break;
				case DataType.Xml:
					StringBuilder.Append("Clob");
					break;
				case DataType.Guid:
					StringBuilder.Append("Char (36)");
					break;
				default:
					base.BuildDataType(type, createDbType); 
					break;
			}
		}

		protected override void BuildFromClause()
		{
			if (!SelectQuery.IsUpdate)
				base.BuildFromClause();
			if (SelectQuery.From.Tables.Count == 0)
				StringBuilder.Append("FROM DUMMY");
		}

		public static bool TryConvertParameterSymbol { get; set; }

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
					return "?";

				case ConvertType.NameToCommandParameter:
					return value;

				case ConvertType.NameToSprocParameter:
					{
						var valueStr = value.ToString();

						if(string.IsNullOrEmpty(valueStr))
							throw new ArgumentException("Argument 'value' must represent parameter name.");

						return valueStr;
					}

				case ConvertType.SprocParameterToName:
					{
						return value.ToString();
					}

				case ConvertType.NameToQueryField     :
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
					{
						var name = value.ToString();
						if (name.Length > 0 && name[0] == '"')
							return value;
						return "\"" + value + "\"";
					}

				case ConvertType.NameToDatabase   :
				case ConvertType.NameToOwner      :
				case ConvertType.NameToQueryTable :
					if (value != null)
					{
						var name = value.ToString();
						if (name.Length > 0 && name[0] == '\"')
							return value;

						if (name.IndexOf('.') > 0)
							value = string.Join("\".\"", name.Split('.'));

						return "\"" + value + "\"";
					}

					break;
			}

			return value;
		}

		protected override void BuildCreateTableIdentityAttribute1(SqlField field)
		{
			StringBuilder.Append("GENERATED BY DEFAULT AS IDENTITY");
		}

		protected override void BuildCreateTablePrimaryKey(string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("PRIMARY KEY (");
			StringBuilder.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
			StringBuilder.Append(")");
		}

		protected override void BuildColumnExpression(ISqlExpression expr, string alias, ref bool addAlias)
		{
			var wrap = false;

			if (expr.SystemType == typeof(bool))
			{
				if (expr is SelectQuery.SearchCondition)
					wrap = true;
				else
				{
					var ex = expr as SqlExpression;
					wrap = ex != null && ex.Expr == "{0}" && ex.Parameters.Length == 1 && ex.Parameters[0] is SelectQuery.SearchCondition;
				}
			}

			if (wrap) StringBuilder.Append("CASE WHEN ");
			base.BuildColumnExpression(expr, alias, ref addAlias);
			if (wrap) StringBuilder.Append(" THEN 1 ELSE 0 END");
		}

		//this is for Tests.Linq.Common.CoalesceLike test
		protected override void BuildFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			switch (func.Name)
			{
				case "CASE": func = ConvertCase(func.SystemType, func.Parameters, 0); 
					break;
			}
			base.BuildFunction(func);
		}

		//this is for Tests.Linq.Common.CoalesceLike test
		static SqlFunction ConvertCase(Type systemType, ISqlExpression[] parameters, int start)
		{
			var len = parameters.Length - start;
			const string name = "CASE";
			var cond = parameters[start];

			if (start == 0 && SqlExpression.NeedsEqual(cond))
			{
				cond = new SelectQuery.SearchCondition(
					new SelectQuery.Condition(
						false,
						new SelectQuery.Predicate.ExprExpr(cond, SelectQuery.Predicate.Operator.Equal, new SqlValue(1))));
			}

			if (len == 3)
				return new SqlFunction(systemType, name, cond, parameters[start + 1], parameters[start + 2]);

			return new SqlFunction(systemType, name,
				cond,
				parameters[start + 1],
				ConvertCase(systemType, parameters, start + 2));
		}
	}
}
