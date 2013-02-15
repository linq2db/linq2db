using System;
using System.Data;
using System.Text;

namespace LinqToDB.DataProvider
{
	using Extensions;
	using SqlBuilder;
	using SqlProvider;

	public class OracleSqlProvider : BasicSqlProvider
	{
		public OracleSqlProvider(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
			SqlProviderFlags.IsCountSubQuerySupported    = false;
			SqlProviderFlags.IsIdentityParameterRequired = true;
		}

		protected override void BuildSelectClause(StringBuilder sb)
		{
			if (SqlQuery.From.Tables.Count == 0)
			{
				AppendIndent(sb).Append("SELECT").AppendLine();
				BuildColumns(sb);
				AppendIndent(sb).Append("FROM SYS.DUAL").AppendLine();
			}
			else
				base.BuildSelectClause(sb);
		}

		protected override void BuildGetIdentity(StringBuilder sb)
		{
			var identityField = SqlQuery.Insert.Into.GetIdentityField();

			if (identityField == null)
				throw new SqlException("Identity field must be defined for '{0}'.", SqlQuery.Insert.Into.Name);

			AppendIndent(sb).AppendLine("RETURNING");
			AppendIndent(sb).Append("\t");
			BuildExpression(sb, identityField, false, true);
			sb.AppendLine(" INTO :IDENTITY_PARAMETER");
		}

		public override ISqlExpression GetIdentityExpression(SqlTable table, SqlField identityField, bool forReturning)
		{
			if (table.SequenceAttributes != null)
			{
				var attr = GetSequenceNameAttribute(table, false);

				if (attr != null)
					return new SqlExpression(attr.SequenceName + ".nextval", Precedence.Primary);
			}

			return base.GetIdentityExpression(table, identityField, forReturning);
		}

		protected override bool BuildWhere()
		{
			return base.BuildWhere() || !NeedSkip && NeedTake && SqlQuery.OrderBy.IsEmpty && SqlQuery.Having.IsEmpty;
		}

		string _rowNumberAlias;

		protected override ISqlProvider CreateSqlProvider()
		{
			return new OracleSqlProvider(SqlProviderFlags);
		}

		protected override void BuildSql(StringBuilder sb)
		{
			var buildRowNum = NeedSkip || NeedTake && (!SqlQuery.OrderBy.IsEmpty || !SqlQuery.Having.IsEmpty);
			var aliases     = null as string[];

			if (buildRowNum)
			{
				aliases = GetTempAliases(2, "t");

				if (_rowNumberAlias == null)
					_rowNumberAlias = GetTempAliases(1, "rn")[0];

				AppendIndent(sb).AppendFormat("SELECT {0}.*", aliases[1]).AppendLine();
				AppendIndent(sb).Append("FROM").    AppendLine();
				AppendIndent(sb).Append("(").       AppendLine();
				Indent++;

				AppendIndent(sb).AppendFormat("SELECT {0}.*, ROWNUM as {1}", aliases[0], _rowNumberAlias).AppendLine();
				AppendIndent(sb).Append("FROM").    AppendLine();
				AppendIndent(sb).Append("(").       AppendLine();
				Indent++;
			}

			base.BuildSql(sb);

			if (buildRowNum)
			{
				Indent--;
				AppendIndent(sb).Append(") ").Append(aliases[0]).AppendLine();

				if (NeedTake && NeedSkip)
				{
					AppendIndent(sb).AppendLine("WHERE");
					AppendIndent(sb).Append("\tROWNUM <= ");
					BuildExpression(sb, Add<int>(SqlQuery.Select.SkipValue, SqlQuery.Select.TakeValue));
					sb.AppendLine();
				}

				Indent--;
				AppendIndent(sb).Append(") ").Append(aliases[1]).AppendLine();
				AppendIndent(sb).Append("WHERE").AppendLine();

				Indent++;

				if (NeedTake && NeedSkip)
				{
					AppendIndent(sb).AppendFormat("{0}.{1} > ", aliases[1], _rowNumberAlias);
					BuildExpression(sb, SqlQuery.Select.SkipValue);
				}
				else if (NeedTake)
				{
					AppendIndent(sb).AppendFormat("{0}.{1} <= ", aliases[1], _rowNumberAlias);
					BuildExpression(sb, Precedence.Comparison, SqlQuery.Select.TakeValue);
				}
				else
				{
					AppendIndent(sb).AppendFormat("{0}.{1} > ", aliases[1], _rowNumberAlias);
					BuildExpression(sb, Precedence.Comparison, SqlQuery.Select.SkipValue);
				}

				sb.AppendLine();
				Indent--;
			}
		}

		protected override void BuildWhereSearchCondition(StringBuilder sb, SqlQuery.SearchCondition condition)
		{
			if (NeedTake && !NeedSkip && SqlQuery.OrderBy.IsEmpty && SqlQuery.Having.IsEmpty)
			{
				BuildPredicate(
					sb,
					Precedence.LogicalConjunction,
					new SqlQuery.Predicate.ExprExpr(
						new SqlExpression(null, "ROWNUM", Precedence.Primary),
						SqlQuery.Predicate.Operator.LessOrEqual,
						SqlQuery.Select.TakeValue));

				if (base.BuildWhere())
				{
					sb.Append(" AND ");
					BuildSearchCondition(sb, Precedence.LogicalConjunction, condition);
				}
			}
			else
				BuildSearchCondition(sb, Precedence.Unknown, condition);
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlBinaryExpression)
			{
				var be = (SqlBinaryExpression)expr;

				switch (be.Operation)
				{
					case "%": return new SqlFunction(be.SystemType, "MOD",    be.Expr1, be.Expr2);
					case "&": return new SqlFunction(be.SystemType, "BITAND", be.Expr1, be.Expr2);
					case "|": // (a + b) - BITAND(a, b)
						return Sub(
							Add(be.Expr1, be.Expr2, be.SystemType),
							new SqlFunction(be.SystemType, "BITAND", be.Expr1, be.Expr2),
							be.SystemType);

					case "^": // (a + b) - BITAND(a, b) * 2
						return Sub(
							Add(be.Expr1, be.Expr2, be.SystemType),
							Mul(new SqlFunction(be.SystemType, "BITAND", be.Expr1, be.Expr2), 2),
							be.SystemType);
					case "+": return be.SystemType == typeof(string)? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence): expr;
				}
			}
			else if (expr is SqlFunction)
			{
				var func = (SqlFunction) expr;

				switch (func.Name)
				{
					case "Coalesce"       : return new SqlFunction(func.SystemType, "Nvl", func.Parameters);
					case "Convert"        :
						{
							var ftype = func.SystemType.ToUnderlying();

							if (ftype == typeof(bool))
							{
								var ex = AlternativeConvertToBoolean(func, 1);
								if (ex != null)
									return ex;
							}

							if (ftype == typeof(DateTime) || ftype == typeof(DateTimeOffset))
							{
								if (IsTimeDataType(func.Parameters[0]))
								{
									if (func.Parameters[1].SystemType == typeof(string))
										return func.Parameters[1];

									return new SqlFunction(func.SystemType, "To_Char", func.Parameters[1], new SqlValue("HH24:MI:SS"));
								}

								if (func.Parameters[1].SystemType.ToUnderlying() == typeof(DateTime) &&
									IsDateDataType(func.Parameters[0], "Date"))
								{
									return new SqlFunction(func.SystemType, "Trunc", func.Parameters[1], new SqlValue("DD"));
								}

								return new SqlFunction(func.SystemType, "To_Timestamp", func.Parameters[1], new SqlValue("YYYY-MM-DD HH24:MI:SS"));
							}

							return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);
						}

					case "CharIndex"      :
						return func.Parameters.Length == 2?
							new SqlFunction(func.SystemType, "InStr", func.Parameters[1], func.Parameters[0]):
							new SqlFunction(func.SystemType, "InStr", func.Parameters[1], func.Parameters[0], func.Parameters[2]);
					case "AddYear"        : return new SqlFunction(func.SystemType, "Add_Months", func.Parameters[0], Mul(func.Parameters[1], 12));
					case "AddQuarter"     : return new SqlFunction(func.SystemType, "Add_Months", func.Parameters[0], Mul(func.Parameters[1],  3));
					case "AddMonth"       : return new SqlFunction(func.SystemType, "Add_Months", func.Parameters[0],     func.Parameters[1]);
					case "AddDayOfYear"   :
					case "AddWeekDay"     :
					case "AddDay"         : return Add<DateTime>(func.Parameters[0],     func.Parameters[1]);
					case "AddWeek"        : return Add<DateTime>(func.Parameters[0], Mul(func.Parameters[1], 7));
					case "AddHour"        : return Add<DateTime>(func.Parameters[0], Div(func.Parameters[1],                  24));
					case "AddMinute"      : return Add<DateTime>(func.Parameters[0], Div(func.Parameters[1],             60 * 24));
					case "AddSecond"      : return Add<DateTime>(func.Parameters[0], Div(func.Parameters[1],        60 * 60 * 24));
					case "AddMillisecond" : return Add<DateTime>(func.Parameters[0], Div(func.Parameters[1], 1000 * 60 * 60 * 24));
					case "Avg"            : 
						return new SqlFunction(
							func.SystemType,
							"Round",
							new SqlFunction(func.SystemType, "AVG", func.Parameters[0]),
							new SqlValue(27));
				}
			}
			else if (expr is SqlExpression)
			{
				var e = (SqlExpression)expr;

				if (e.Expr.StartsWith("To_Number(To_Char(") && e.Expr.EndsWith(", 'FF'))"))
					return Div(new SqlExpression(e.SystemType, e.Expr.Replace("To_Number(To_Char(", "to_Number(To_Char("), e.Parameters), 1000);
			}

			return expr;
		}

		protected override void BuildDataType(StringBuilder sb, SqlDataType type)
		{
			switch (type.SqlDbType)
			{
				case SqlDbType.BigInt     : sb.Append("Number(19)");      break;
				case SqlDbType.TinyInt    : sb.Append("Number(3)");       break;
				case SqlDbType.Money      : sb.Append("Number(19,4)");    break;
				case SqlDbType.SmallMoney : sb.Append("Number(10,4)");    break;
				case SqlDbType.NVarChar   :
					sb.Append("VarChar2");
					if (type.Length > 0)
						sb.Append('(').Append(type.Length).Append(')');
					break;
				default                   : base.BuildDataType(sb, type); break;
			}
		}

		public override SqlQuery Finalize(SqlQuery sqlQuery)
		{
			CheckAliases(sqlQuery, 30);

			new QueryVisitor().Visit(sqlQuery.Select, element =>
			{
				if (element.ElementType == QueryElementType.SqlParameter)
					((SqlParameter)element).IsQueryParameter = false;
			});

			sqlQuery = base.Finalize(sqlQuery);

			switch (sqlQuery.QueryType)
			{
				case QueryType.Delete : return GetAlternativeDelete(sqlQuery);
				case QueryType.Update : return GetAlternativeUpdate(sqlQuery);
				default               : return sqlQuery;
			}
		}

		protected override void BuildFromClause(StringBuilder sb)
		{
			if (!SqlQuery.IsUpdate)
				base.BuildFromClause(sb);
		}

		public override void BuildValue(StringBuilder sb, object value)
		{
			if (value is Guid)
			{
				var s = ((Guid)value).ToString("N");

				sb
					.Append("Cast('")
					.Append(s.Substring( 6,  2))
					.Append(s.Substring( 4,  2))
					.Append(s.Substring( 2,  2))
					.Append(s.Substring( 0,  2))
					.Append(s.Substring(10,  2))
					.Append(s.Substring( 8,  2))
					.Append(s.Substring(14,  2))
					.Append(s.Substring(12,  2))
					.Append(s.Substring(16, 16))
					.Append("' as raw(16))");
			}
			else if (value is DateTime)
			{
				sb.AppendFormat("TO_TIMESTAMP('{0:yyyy-MM-dd HH:mm:ss.fffffff}', 'YYYY-MM-DD HH24:MI:SS.FF7')", value);
			}
			else
				base.BuildValue(sb, value);
		}

		protected override void BuildColumn(StringBuilder sb, SqlQuery.Column col, ref bool addAlias)
		{
			var wrap = false;

			if (col.SystemType == typeof(bool))
			{
				if (col.Expression is SqlQuery.SearchCondition)
					wrap = true;
				else
				{
					var ex = col.Expression as SqlExpression;
					wrap = ex != null && ex.Expr == "{0}" && ex.Parameters.Length == 1 && ex.Parameters[0] is SqlQuery.SearchCondition;
				}
			}

			if (wrap) sb.Append("CASE WHEN ");
			base.BuildColumn(sb, col, ref addAlias);
			if (wrap) sb.Append(" THEN 1 ELSE 0 END");
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

		protected override void BuildInsertOrUpdateQuery(StringBuilder sb)
		{
			BuildInsertOrUpdateQueryAsMerge(sb, "FROM SYS.DUAL");
		}

		protected override void BuildEmptyInsert(StringBuilder sb)
		{
			sb.Append("VALUES ");

			foreach (var col in SqlQuery.Insert.Into.Fields)
				sb.Append("(DEFAULT)");

			sb.AppendLine();
		}
	}
}
