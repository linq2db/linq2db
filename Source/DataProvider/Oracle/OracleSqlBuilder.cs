﻿using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using SqlQuery;
	using SqlProvider;
	using System.Text;

	class OracleSqlBuilder : BasicSqlBuilder
	{
		public OracleSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		protected override void BuildSelectClause()
		{
			if (SelectQuery.From.Tables.Count == 0)
			{
				AppendIndent().Append("SELECT").AppendLine();
				BuildColumns();
				AppendIndent().Append("FROM SYS.DUAL").AppendLine();
			}
			else
				base.BuildSelectClause();
		}

		protected override void BuildGetIdentity()
		{
			var identityField = SelectQuery.Insert.Into.GetIdentityField();

			if (identityField == null)
				throw new SqlException("Identity field must be defined for '{0}'.", SelectQuery.Insert.Into.Name);

			AppendIndent().AppendLine("RETURNING ");
			AppendIndent().Append("\t");
			BuildExpression(identityField, false, true);
			StringBuilder.AppendLine(" INTO :IDENTITY_PARAMETER");
		}

		public override ISqlExpression GetIdentityExpression(SqlTable table)
		{
			if (!table.SequenceAttributes.IsNullOrEmpty())
			{
				var attr = GetSequenceNameAttribute(table, false);

				if (attr != null)
					return new SqlExpression(attr.SequenceName + ".nextval", Precedence.Primary);
			}

			return base.GetIdentityExpression(table);
		}

		private static void ConvertEmptyStringToNullIfNeeded(ISqlExpression expr)
		{
			var sqlParameter = expr as SqlParameter;
			var sqlValue     = expr as SqlValue;

			if (sqlParameter != null && sqlParameter.Value is string && sqlParameter.Value.ToString() == "")
				sqlParameter.Value = null;

			if (sqlValue != null && sqlValue.Value is string && sqlValue.Value.ToString() == "")
				sqlValue.Value = null;
		}

		protected override void BuildPredicate(ISqlPredicate predicate)
		{
			if (predicate.ElementType == QueryElementType.ExprExprPredicate)
			{
				var expr = (SelectQuery.Predicate.ExprExpr)predicate;
				if (expr.Operator == SelectQuery.Predicate.Operator.Equal ||
					expr.Operator == SelectQuery.Predicate.Operator.NotEqual)
				{
					ConvertEmptyStringToNullIfNeeded(expr.Expr1);
					ConvertEmptyStringToNullIfNeeded(expr.Expr2);
				}
			}
			base.BuildPredicate(predicate);
		}

		protected override bool BuildWhere()
		{
			return base.BuildWhere() || !NeedSkip && NeedTake && SelectQuery.OrderBy.IsEmpty && SelectQuery.Having.IsEmpty;
		}

		string _rowNumberAlias;

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new OracleSqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override void BuildSql()
		{
			if (NeedSkip)
			{
				var aliases = GetTempAliases(2, "t");

				if (_rowNumberAlias == null)
					_rowNumberAlias = GetTempAliases(1, "rn")[0];

				AppendIndent().AppendFormat("SELECT {0}.*", aliases[1]).AppendLine();
				AppendIndent().Append("FROM").    AppendLine();
				AppendIndent().Append("(").       AppendLine();
				Indent++;

				AppendIndent().AppendFormat("SELECT {0}.*, ROWNUM as {1}", aliases[0], _rowNumberAlias).AppendLine();
				AppendIndent().Append("FROM").    AppendLine();
				AppendIndent().Append("(").       AppendLine();
				Indent++;

				base.BuildSql();

				Indent--;
				AppendIndent().Append(") ").Append(aliases[0]).AppendLine();

				if (NeedTake)
				{
					AppendIndent().AppendLine("WHERE");
					AppendIndent().Append("\tROWNUM <= ");
					BuildExpression(Add<int>(SelectQuery.Select.SkipValue, SelectQuery.Select.TakeValue));
					StringBuilder.AppendLine();
				}

				Indent--;
				AppendIndent().Append(") ").Append(aliases[1]).AppendLine();
				AppendIndent().Append("WHERE").AppendLine();

				Indent++;

				AppendIndent().AppendFormat("{0}.{1} > ", aliases[1], _rowNumberAlias);
				BuildExpression(SelectQuery.Select.SkipValue);

				StringBuilder.AppendLine();
				Indent--;
			}
			else if (NeedTake && (!SelectQuery.OrderBy.IsEmpty || !SelectQuery.Having.IsEmpty))
			{
				var aliases = GetTempAliases(1, "t");

				AppendIndent().AppendFormat("SELECT {0}.*", aliases[0]).AppendLine();
				AppendIndent().Append("FROM").    AppendLine();
				AppendIndent().Append("(").       AppendLine();
				Indent++;

				base.BuildSql();

				Indent--;
				AppendIndent().Append(") ").Append(aliases[0]).AppendLine();
				AppendIndent().Append("WHERE").AppendLine();

				Indent++;

				AppendIndent().Append("ROWNUM <= ");
				BuildExpression(SelectQuery.Select.TakeValue);

				StringBuilder.AppendLine();
				Indent--;
			}
			else
			{
				base.BuildSql();
			}
		}

		protected override void BuildWhereSearchCondition(SelectQuery.SearchCondition condition)
		{
			if (NeedTake && !NeedSkip && SelectQuery.OrderBy.IsEmpty && SelectQuery.Having.IsEmpty)
			{
				BuildPredicate(
					Precedence.LogicalConjunction,
					new SelectQuery.Predicate.ExprExpr(
						new SqlExpression(null, "ROWNUM", Precedence.Primary),
						SelectQuery.Predicate.Operator.LessOrEqual,
						SelectQuery.Select.TakeValue));

				if (base.BuildWhere())
				{
					StringBuilder.Append(" AND ");
					BuildSearchCondition(Precedence.LogicalConjunction, condition);
				}
			}
			else
				BuildSearchCondition(Precedence.Unknown, condition);
		}

		protected override void BuildFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(func);
		}

		protected override void BuildDataType(SqlDataType type, bool createDbType)
		{
			switch (type.DataType)
			{
				case DataType.DateTime       : StringBuilder.Append("timestamp");                 break;
				case DataType.DateTime2      : StringBuilder.Append("timestamp");                 break;
				case DataType.DateTimeOffset : StringBuilder.Append("timestamp with time zone");  break;
				case DataType.UInt32         :
				case DataType.Int64          : StringBuilder.Append("Number(19)");                break;
				case DataType.SByte          :
				case DataType.Byte           : StringBuilder.Append("Number(3)");                 break;
				case DataType.Money          : StringBuilder.Append("Number(19,4)");              break;
				case DataType.SmallMoney     : StringBuilder.Append("Number(10,4)");              break;
				case DataType.NVarChar       :
					StringBuilder.Append("VarChar2");
					if (type.Length > 0)
						StringBuilder.Append('(').Append(type.Length).Append(')');
					break;
				case DataType.Boolean        : StringBuilder.Append("Char(1)");                   break;
				case DataType.NText          : StringBuilder.Append("NClob");                     break;
				case DataType.Text           : StringBuilder.Append("Clob");                      break;
				case DataType.Guid           : StringBuilder.Append("Raw(16)");                   break;
				case DataType.Binary         :
				case DataType.VarBinary      :
					if (type.Length == null || type.Length == 0)
						StringBuilder.Append("BLOB");
					else 
						StringBuilder.Append("Raw(").Append(type.Length).Append(")");
					break;
				default: base.BuildDataType(type, createDbType);                                  break;
			}
		}

		protected override void BuildFromClause()
		{
			if (!SelectQuery.IsUpdate)
				base.BuildFromClause();
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

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
					return ":" + value;
			}

			return value;
		}

		protected override void BuildInsertOrUpdateQuery()
		{
			BuildInsertOrUpdateQueryAsMerge("FROM SYS.DUAL");
		}

		public string BuildReserveSequenceValuesSql(int count, string sequenceName)
		{
			return "SELECT " + sequenceName + ".nextval Id from DUAL connect by level <= " + count;
		}

		protected override void BuildEmptyInsert()
		{
			StringBuilder.Append("VALUES ");

			foreach (var col in SelectQuery.Insert.Into.Fields)
				StringBuilder.Append("(DEFAULT)");

			StringBuilder.AppendLine();
		}

		SqlField _identityField;

		public override int CommandCount(SelectQuery selectQuery)
		{
			if (selectQuery.IsCreateTable)
			{
				_identityField = selectQuery.CreateTable.Table.Fields.Values.FirstOrDefault(f => f.IsIdentity);

				if (_identityField != null)
					return 3;
			}

			return base.CommandCount(selectQuery);
		}

		protected override void BuildDropTableStatement()
		{
			if (_identityField == null)
			{
				base.BuildDropTableStatement();
			}
			else
			{
			var schemaPrefix = string.IsNullOrWhiteSpace(SelectQuery.CreateTable.Table.Owner)
				? string.Empty
				: SelectQuery.CreateTable.Table.Owner + ".";

				StringBuilder
					.Append("DROP TRIGGER ")
					.Append(schemaPrefix)
					.Append("TIDENTITY_")
					.Append(SelectQuery.CreateTable.Table.PhysicalName)
					.AppendLine();
			}
		}

		protected override void BuildCommand(int commandNumber)
		{
			var schemaPrefix = string.IsNullOrWhiteSpace(SelectQuery.CreateTable.Table.Owner)
				? string.Empty
				: SelectQuery.CreateTable.Table.Owner + ".";

			if (SelectQuery.CreateTable.IsDrop)
			{
				if (commandNumber == 1)
				{
					StringBuilder
						.Append("DROP SEQUENCE ")
						.Append(schemaPrefix)
						.Append("SIDENTITY_")
						.Append(SelectQuery.CreateTable.Table.PhysicalName)
						.AppendLine();
				}
				else
					base.BuildDropTableStatement();
			}
			else
			{
				if (commandNumber == 1)
				{
					StringBuilder
						.Append("CREATE SEQUENCE ")
						.Append(schemaPrefix)
						.Append("SIDENTITY_")
						.Append(SelectQuery.CreateTable.Table.PhysicalName)
						.AppendLine();
				}
				else
				{
					StringBuilder
						.AppendFormat("CREATE OR REPLACE TRIGGER {0}TIDENTITY_{1}", schemaPrefix, SelectQuery.CreateTable.Table.PhysicalName)
						.AppendLine()
						.AppendFormat("BEFORE INSERT ON ");

					BuildPhysicalTable(SelectQuery.CreateTable.Table, null);

					StringBuilder
						.AppendLine(" FOR EACH ROW")
						.AppendLine  ()
						.AppendLine  ("BEGIN")
						.AppendFormat("\tSELECT {2}SIDENTITY_{1}.NEXTVAL INTO :NEW.{0} FROM dual;", _identityField.PhysicalName, SelectQuery.CreateTable.Table.PhysicalName, schemaPrefix)
						.AppendLine  ()
						.AppendLine  ("END;");
				}
			}
		}

		public override StringBuilder BuildTableName(StringBuilder sb, string database, string owner, string table)
		{
			if (owner != null)
				sb.Append(owner).Append(".");

			return sb.Append(table);
		}

#if !SILVERLIGHT

		protected override string GetProviderTypeName(IDbDataParameter parameter)
		{
			dynamic p = parameter;
			return p.OracleDbType.ToString();
		}

#endif
	}
}
