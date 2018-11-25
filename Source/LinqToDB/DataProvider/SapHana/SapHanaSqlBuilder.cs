using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.SapHana
{
	using SqlQuery;
	using SqlProvider;

	class SapHanaSqlBuilder : BasicSqlBuilder
	{
		public SapHanaSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		public override int CommandCount(SqlStatement statement)
		{
			return statement.NeedsIdentity() ? 2 : 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			var insertClause = Statement.GetInsertClause();
			if (insertClause != null)
			{
				var identityField = insertClause.Into.GetIdentityField();
				var table = insertClause.Into;

				if (identityField == null || table == null)
					throw new SqlException("Identity field must be defined for '{0}'.", insertClause.Into.Name);

				StringBuilder.Append("SELECT MAX(");
				BuildExpression(identityField, false, true);
				StringBuilder.Append(") FROM ");
				BuildPhysicalTable(table, null);
			}
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SapHanaSqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override string LimitFormat(SelectQuery selectQuery)
		{
			return "LIMIT {0}";
		}

		protected override string OffsetFormat(SelectQuery selectQuery)
		{
			return selectQuery.Select.TakeValue == null ? "LIMIT 4200000000 OFFSET {0}" : "OFFSET {0}";
		}

		public override bool IsNestedJoinParenthesisRequired { get { return true; } }

		protected override void BuildStartCreateTableStatement(SqlCreateTableStatement createTable)
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

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsUpdateInsert(insertOrUpdate);
		}

		protected override void BuildDataType(SqlDataType type, bool createDbType)
		{
			switch (type.DataType)
			{
				case DataType.Int32         :
				case DataType.UInt16        :
					StringBuilder.Append("Integer");
					return;
				case DataType.Double:
					StringBuilder.Append("Double");
					return;
				case DataType.DateTime2     :
				case DataType.DateTime      :
				case DataType.Time:
					StringBuilder.Append("Timestamp");
					return;
				case DataType.SmallDateTime :
					StringBuilder.Append("SecondDate");
					return;
				case DataType.Boolean       :
					StringBuilder.Append("TinyInt");
					return;
				case DataType.Image:
					StringBuilder.Append("Blob");
					return;
				case DataType.Xml:
					StringBuilder.Append("Clob");
					return;
				case DataType.Guid:
					StringBuilder.Append("Char (36)");
					return;
				case DataType.NVarChar:
				case DataType.VarChar:
				case DataType.VarBinary:
					if (type.Length == null || type.Length > 5000 || type.Length < 1)
					{
						StringBuilder
							.Append(type.DataType)
							.Append("(Max)");
						return;
					}
					break;
			}
			base.BuildDataType(type, createDbType);
		}

		protected override void BuildFromClause(SqlStatement statement, SelectQuery selectQuery)
		{
			if (!statement.IsUpdate())
				base.BuildFromClause(statement, selectQuery);
			if (selectQuery.From.Tables.Count == 0)
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
					return ":" + value;

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
				case ConvertType.NameToSchema     :
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

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("PRIMARY KEY (");
			StringBuilder.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
			StringBuilder.Append(")");
		}

		protected override void BuildColumnExpression(SelectQuery selectQuery, ISqlExpression expr, string alias, ref bool addAlias)
		{
			var wrap = false;

			if (expr.SystemType == typeof(bool))
			{
				if (expr is SqlSearchCondition)
					wrap = true;
				else
				{
					var ex = expr as SqlExpression;
					wrap = ex != null && ex.Expr == "{0}" && ex.Parameters.Length == 1 && ex.Parameters[0] is SqlSearchCondition;
				}
			}

			if (wrap) StringBuilder.Append("CASE WHEN ");
			base.BuildColumnExpression(selectQuery, expr, alias, ref addAlias);
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
			var len  = parameters.Length - start;
			var cond = parameters[start];

			if (start == 0 && SqlExpression.NeedsEqual(cond))
			{
				cond = new SqlSearchCondition(
					new SqlCondition(
						false,
						new SqlPredicate.ExprExpr(cond, SqlPredicate.Operator.Equal, new SqlValue(1))));
			}

			const string name = "CASE";

			if (len == 3)
				return new SqlFunction(systemType, name, cond, parameters[start + 1], parameters[start + 2]);

			return new SqlFunction(systemType, name,
				cond,
				parameters[start + 1],
				ConvertCase(systemType, parameters, start + 2));
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string database, string schema, string table)
		{
			if (database != null && database.Length == 0) database = null;
			if (schema    != null && schema.   Length == 0) schema    = null;

			// "db..table" syntax not supported:
			// <table_name> ::= [[<database_name>.]<schema.name>.]<identifier>
			if (database != null && schema == null)
				throw new LinqToDBException("SAP HANA requires schema name if database name provided.");

			return base.BuildTableName(sb, database, schema, table);
		}
	}
}
