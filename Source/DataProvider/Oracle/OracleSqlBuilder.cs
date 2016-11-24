using System;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using SqlQuery;
	using SqlProvider;

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

		protected override void BuildDataType(SqlDataType type, bool createDbType = false)
		{
			switch (type.DataType)
			{
				case DataType.DateTime   : StringBuilder.Append("timestamp");    break;
				case DataType.DateTime2  : StringBuilder.Append("timestamp");    break;
				case DataType.UInt32     :
				case DataType.Int64      : StringBuilder.Append("Number(19)");   break;
				case DataType.SByte      :
				case DataType.Byte       : StringBuilder.Append("Number(3)");    break;
				case DataType.Money      : StringBuilder.Append("Number(19,4)"); break;
				case DataType.SmallMoney : StringBuilder.Append("Number(10,4)"); break;
				case DataType.NVarChar   :
					StringBuilder.Append("VarChar2");
					if (type.Length > 0)
						StringBuilder.Append('(').Append(type.Length).Append(')');
					break;
				case DataType.Boolean    : StringBuilder.Append("Char(1)");      break;
				case DataType.NText      : StringBuilder.Append("NClob");        break;
				case DataType.Text       : StringBuilder.Append("Clob");         break;
				default                  : base.BuildDataType(type);             break;
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
				StringBuilder
					.Append("DROP TRIGGER TIDENTITY_")
					.Append(SelectQuery.CreateTable.Table.PhysicalName)
					.AppendLine();
			}
		}

		protected override void BuildCommand(int commandNumber)
		{
			if (SelectQuery.CreateTable.IsDrop)
			{
				if (commandNumber == 1)
				{
					StringBuilder
						.Append("DROP SEQUENCE SIDENTITY_")
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
						.Append("CREATE SEQUENCE SIDENTITY_")
						.Append(SelectQuery.CreateTable.Table.PhysicalName)
						.AppendLine();
				}
				else
				{
					StringBuilder
						.AppendFormat("CREATE OR REPLACE TRIGGER  TIDENTITY_{0}", SelectQuery.CreateTable.Table.PhysicalName)
						.AppendLine  ()
						.AppendFormat("BEFORE INSERT ON {0} FOR EACH ROW", SelectQuery.CreateTable.Table.PhysicalName)
						.AppendLine  ()
						.AppendLine  ("BEGIN")
						.AppendFormat("\tSELECT SIDENTITY_{1}.NEXTVAL INTO :NEW.{0} FROM dual;", _identityField.PhysicalName, SelectQuery.CreateTable.Table.PhysicalName)
						.AppendLine  ()
						.AppendLine  ("END;");
				}
			}
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
