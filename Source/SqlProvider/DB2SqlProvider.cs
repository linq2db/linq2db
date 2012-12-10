using System;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using Extensions;
	using SqlBuilder;

	public class DB2SqlProvider : BasicSqlProvider
	{
		public override bool TakeAcceptsParameter { get { return SqlQuery.Select.SkipValue != null; } }

		SqlField _identityField;

		public override int CommandCount(SqlQuery sqlQuery)
		{
			if (sqlQuery.IsInsert && sqlQuery.Insert.WithIdentity)
			{
				_identityField = sqlQuery.Insert.Into.GetIdentityField();

				if (_identityField == null)
					return 2;
			}

			return 1;
		}

		public override int BuildSql(int commandNumber, SqlQuery sqlQuery, StringBuilder sb, int indent, int nesting, bool skipAlias)
		{
			if (_identityField != null)
			{
				indent += 2;

				AppendIndent(sb).AppendLine("SELECT");
				AppendIndent(sb).Append("\t");
				BuildExpression(sb, _identityField, false, true);
				sb.AppendLine();
				AppendIndent(sb).AppendLine("FROM");
				AppendIndent(sb).AppendLine("\tNEW TABLE");
				AppendIndent(sb).AppendLine("\t(");
			}

			var ret = base.BuildSql(commandNumber, sqlQuery, sb, indent, nesting, skipAlias);

			if (_identityField != null)
				sb.AppendLine("\t)");

			return ret;
		}

		protected override void BuildCommand(int commandNumber, StringBuilder sb)
		{
			sb.AppendLine("SELECT identity_val_local() FROM SYSIBM.SYSDUMMY1");
		}

		protected override ISqlProvider CreateSqlProvider()
		{
			return new DB2SqlProvider();
		}

		protected override void BuildSql(StringBuilder sb)
		{
			AlternativeBuildSql(sb, false, base.BuildSql);
		}

		protected override void BuildSelectClause(StringBuilder sb)
		{
			if (SqlQuery.From.Tables.Count == 0)
			{
				AppendIndent(sb).AppendLine("SELECT");
				BuildColumns(sb);
				AppendIndent(sb).AppendLine("FROM SYSIBM.SYSDUMMY1 FETCH FIRST 1 ROW ONLY");
			}
			else
				base.BuildSelectClause(sb);
		}

		protected override string LimitFormat
		{
			get { return SqlQuery.Select.SkipValue == null ? "FETCH FIRST {0} ROWS ONLY" : null; }
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlBinaryExpression)
			{
				var be = (SqlBinaryExpression)expr;

				switch (be.Operation)
				{
					case "%":
						{
							var expr1 = !be.Expr1.SystemType.IsIntegerType() ? new SqlFunction(typeof(int), "Int", be.Expr1) : be.Expr1;
							return new SqlFunction(be.SystemType, "Mod", expr1, be.Expr2);
						}
					case "&": return new SqlFunction(be.SystemType, "BitAnd", be.Expr1, be.Expr2);
					case "|": return new SqlFunction(be.SystemType, "BitOr",  be.Expr1, be.Expr2);
					case "^": return new SqlFunction(be.SystemType, "BitXor", be.Expr1, be.Expr2);
					case "+": return be.SystemType == typeof(string)? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence): expr;
				}
			}
			else if (expr is SqlFunction)
			{
				var func = (SqlFunction) expr;

				switch (func.Name)
				{
					case "Convert"    :
						if (func.SystemType.ToUnderlying() == typeof(bool))
						{
							var ex = AlternativeConvertToBoolean(func, 1);
							if (ex != null)
								return ex;
						}

						if (func.Parameters[0] is SqlDataType)
						{
							var type = (SqlDataType)func.Parameters[0];

							if (type.Type == typeof(string) && func.Parameters[1].SystemType != typeof(string))
								return new SqlFunction(func.SystemType, "RTrim", new SqlFunction(typeof(string), "Char", func.Parameters[1]));

							if (type.Length > 0)
								return new SqlFunction(func.SystemType, type.SqlDbType.ToString(), func.Parameters[1], new SqlValue(type.Length));

							if (type.Precision > 0)
								return new SqlFunction(func.SystemType, type.SqlDbType.ToString(), func.Parameters[1], new SqlValue(type.Precision), new SqlValue(type.Scale));

							return new SqlFunction(func.SystemType, type.SqlDbType.ToString(), func.Parameters[1]);
						}

						if (func.Parameters[0] is SqlFunction)
						{
							var f = (SqlFunction)func.Parameters[0];

							return
								f.Name == "Char" ?
									new SqlFunction(func.SystemType, f.Name, func.Parameters[1]) :
								f.Parameters.Length == 1 ?
									new SqlFunction(func.SystemType, f.Name, func.Parameters[1], f.Parameters[0]) :
									new SqlFunction(func.SystemType, f.Name, func.Parameters[1], f.Parameters[0], f.Parameters[1]);
						}

						{
							var e = (SqlExpression)func.Parameters[0];
							return new SqlFunction(func.SystemType, e.Expr, func.Parameters[1]);
						}

					case "Millisecond"   : return Div(new SqlFunction(func.SystemType, "Microsecond", func.Parameters), 1000);
					case "SmallDateTime" :
					case "DateTime"      :
					case "DateTime2"     : return new SqlFunction(func.SystemType, "TimeStamp", func.Parameters);
					case "TinyInt"       : return new SqlFunction(func.SystemType, "SmallInt",  func.Parameters);
					case "Money"         : return new SqlFunction(func.SystemType, "Decimal",   func.Parameters[0], new SqlValue(19), new SqlValue(4));
					case "SmallMoney"    : return new SqlFunction(func.SystemType, "Decimal",   func.Parameters[0], new SqlValue(10), new SqlValue(4));
					case "VarChar"       :
						if (func.Parameters[0].SystemType.ToUnderlying() == typeof(decimal))
							return new SqlFunction(func.SystemType, "Char", func.Parameters[0]);
						break;
					case "NChar"         :
					case "NVarChar"      : return new SqlFunction(func.SystemType, "Char",      func.Parameters);
					case "DateDiff"      :
						{
							switch ((Sql.DateParts)((SqlValue)func.Parameters[0]).Value)
							{
								case Sql.DateParts.Day         : return new SqlExpression(typeof(int), "((Days({0}) - Days({1})) * 86400 + (MIDNIGHT_SECONDS({0}) - MIDNIGHT_SECONDS({1}))) / 86400",                                               Precedence.Multiplicative, func.Parameters[2], func.Parameters[1]);
								case Sql.DateParts.Hour        : return new SqlExpression(typeof(int), "((Days({0}) - Days({1})) * 86400 + (MIDNIGHT_SECONDS({0}) - MIDNIGHT_SECONDS({1}))) / 3600",                                                Precedence.Multiplicative, func.Parameters[2], func.Parameters[1]);
								case Sql.DateParts.Minute      : return new SqlExpression(typeof(int), "((Days({0}) - Days({1})) * 86400 + (MIDNIGHT_SECONDS({0}) - MIDNIGHT_SECONDS({1}))) / 60",                                                  Precedence.Multiplicative, func.Parameters[2], func.Parameters[1]);
								case Sql.DateParts.Second      : return new SqlExpression(typeof(int), "(Days({0}) - Days({1})) * 86400 + (MIDNIGHT_SECONDS({0}) - MIDNIGHT_SECONDS({1}))",                                                         Precedence.Additive,       func.Parameters[2], func.Parameters[1]);
								case Sql.DateParts.Millisecond : return new SqlExpression(typeof(int), "((Days({0}) - Days({1})) * 86400 + (MIDNIGHT_SECONDS({0}) - MIDNIGHT_SECONDS({1}))) * 1000 + (MICROSECOND({0}) - MICROSECOND({1})) / 1000", Precedence.Additive,       func.Parameters[2], func.Parameters[1]);
							}
						}

						break;
				}
			}

			return expr;
		}

		static void SetQueryParameter(IQueryElement element)
		{
			if (element.ElementType == QueryElementType.SqlParameter)
				((SqlParameter)element).IsQueryParameter = false;
		}

		public override SqlQuery Finalize(SqlQuery sqlQuery)
		{
			new QueryVisitor().Visit(sqlQuery.Select, SetQueryParameter);

			//if (sqlQuery.QueryType == QueryType.InsertOrUpdate)
			//	foreach (var key in sqlQuery.Insert.Items)
			//		if (((SqlField)key.Column).IsPrimaryKey)
			//			new QueryVisitor().Visit(key.Expression, SetQueryParameter);

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
					.Append("Cast(x'")
					.Append(s.Substring( 6,  2))
					.Append(s.Substring( 4,  2))
					.Append(s.Substring( 2,  2))
					.Append(s.Substring( 0,  2))
					.Append(s.Substring(10,  2))
					.Append(s.Substring( 8,  2))
					.Append(s.Substring(14,  2))
					.Append(s.Substring(12,  2))
					.Append(s.Substring(16, 16))
					.Append("' as char(16) for bit data)");
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

		public static bool QuoteIdentifiers = true;

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
					return "@" + value;

				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return ":" + value;

				case ConvertType.SprocParameterToName:
					if (value != null)
					{
						var str = value.ToString();
						return str.Length > 0 && str[0] == ':'? str.Substring(1): str;
					}

					break;

				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTable:
				case ConvertType.NameToQueryTableAlias:
					if (QuoteIdentifiers)
					{
						var name = value.ToString();

						if (name.Length > 0 && name[0] == '"')
							return value;

						return '"' + name + '"';
					}

					break;
			}

			return value;
		}

		protected override void BuildInsertOrUpdateQuery(StringBuilder sb)
		{
			BuildInsertOrUpdateQueryAsMerge(sb, "FROM SYSIBM.SYSDUMMY1 FETCH FIRST 1 ROW ONLY");
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
