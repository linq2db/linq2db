using System;
using System.Data;
using System.Text;

namespace LinqToDB.DataProvider
{
	using Extensions;
	using SqlBuilder;
	using SqlProvider;

	public class InformixSqlProvider : BasicSqlProvider
	{
		public InformixSqlProvider(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override int CommandCount(SqlQuery sqlQuery)
		{
			return sqlQuery.IsInsert && sqlQuery.Insert.WithIdentity ? 2 : 1;
		}

		protected override void BuildCommand(int commandNumber, StringBuilder sb)
		{
			sb.AppendLine("SELECT DBINFO('sqlca.sqlerrd1') FROM systables where tabid = 1");
		}

		protected override ISqlProvider CreateSqlProvider()
		{
			return new InformixSqlProvider(SqlProviderFlags);
		}

		public override int BuildSql(int commandNumber, SqlQuery sqlQuery, StringBuilder sb, int indent, int nesting, bool skipAlias)
		{
			var n = base.BuildSql(commandNumber, sqlQuery, sb, indent, nesting, skipAlias);

			sb
				.Replace("NULL IS NOT NULL", "1=0")
				.Replace("NULL IS NULL",     "1=1");

			return n;
		}

		protected override void BuildSelectClause(StringBuilder sb)
		{
			if (SqlQuery.From.Tables.Count == 0)
			{
				AppendIndent(sb).Append("SELECT FIRST 1").AppendLine();
				BuildColumns(sb);
				AppendIndent(sb).Append("FROM SYSTABLES").AppendLine();
			}
			else
				base.BuildSelectClause(sb);
		}

		public override bool IsSubQueryTakeSupported      { get { return false; } }
		public override bool IsInsertOrUpdateSupported    { get { return false; } }
		public override bool IsGroupByExpressionSupported { get { return false; } }

		protected override string FirstFormat { get { return "FIRST {0}"; } }
		protected override string SkipFormat  { get { return "SKIP {0}";  } }

		protected override void BuildLikePredicate(StringBuilder sb, SqlQuery.Predicate.Like predicate)
		{
			if (predicate.IsNot)
				sb.Append("NOT ");

			var precedence = GetPrecedence(predicate);

			BuildExpression(sb, precedence, predicate.Expr1);
			sb.Append(" LIKE ");
			BuildExpression(sb, precedence, predicate.Expr2);

			if (predicate.Escape != null)
			{
				sb.Append(" ESCAPE ");
				BuildExpression(sb, precedence, predicate.Escape);
			}
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlBinaryExpression)
			{
				var be = (SqlBinaryExpression)expr;

				switch (be.Operation)
				{
					case "%": return new SqlFunction(be.SystemType, "Mod",    be.Expr1, be.Expr2);
					case "&": return new SqlFunction(be.SystemType, "BitAnd", be.Expr1, be.Expr2);
					case "|": return new SqlFunction(be.SystemType, "BitOr",  be.Expr1, be.Expr2);
					case "^": return new SqlFunction(be.SystemType, "BitXor", be.Expr1, be.Expr2);
					case "+": return be.SystemType == typeof(string)? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence): expr;
				}
			}
			else if (expr is SqlFunction)
			{
				var func = (SqlFunction)expr;

				switch (func.Name)
				{
					case "Coalesce" : return new SqlFunction(func.SystemType, "Nvl", func.Parameters);
					case "Convert"  :
						{
							var par0 = func.Parameters[0];
							var par1 = func.Parameters[1];

							switch (Type.GetTypeCode(func.SystemType.ToUnderlying()))
							{
								case TypeCode.String   : return new SqlFunction(func.SystemType, "To_Char", func.Parameters[1]);
								case TypeCode.Boolean  :
									{
										var ex = AlternativeConvertToBoolean(func, 1);
										if (ex != null)
											return ex;
										break;
									}

								case TypeCode.UInt64:
									if (func.Parameters[1].SystemType.IsFloatType())
										par1 = new SqlFunction(func.SystemType, "Floor", func.Parameters[1]);
									break;

								case TypeCode.DateTime :
									if (IsDateDataType(func.Parameters[0], "Date"))
									{
										if (func.Parameters[1].SystemType == typeof(string))
										{
											return new SqlFunction(
												func.SystemType,
												"Date",
												new SqlFunction(func.SystemType, "To_Date", func.Parameters[1], new SqlValue("%Y-%m-%d")));
										}

										return new SqlFunction(func.SystemType, "Date", func.Parameters[1]);
									}

									if (IsTimeDataType(func.Parameters[0]))
										return new SqlExpression(func.SystemType, "Cast(Extend({0}, hour to second) as Char(8))", Precedence.Primary, func.Parameters[1]);

									return new SqlFunction(func.SystemType, "To_Date", func.Parameters[1]);

								default:
									if (func.SystemType.ToUnderlying() == typeof(DateTimeOffset))
										goto case TypeCode.DateTime;
									break;
							}

							return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, par1, par0);
						}

					case "Quarter"  : return Inc(Div(Dec(new SqlFunction(func.SystemType, "Month", func.Parameters)), 3));
					case "WeekDay"  : return Inc(new SqlFunction(func.SystemType, "weekDay", func.Parameters));
					case "DayOfYear":
						return
							Inc(Sub<int>(
								new SqlFunction(null, "Mdy",
									new SqlFunction(null, "Month", func.Parameters),
									new SqlFunction(null, "Day",   func.Parameters),
									new SqlFunction(null, "Year",  func.Parameters)),
								new SqlFunction(null, "Mdy",
									new SqlValue(1),
									new SqlValue(1),
									new SqlFunction(null, "Year", func.Parameters))));
					case "Week"     :
						return
							new SqlExpression(
								func.SystemType,
								"((Extend({0}, year to day) - (Mdy(12, 31 - WeekDay(Mdy(1, 1, year({0}))), Year({0}) - 1) + Interval(1) day to day)) / 7 + Interval(1) day to day)::char(10)::int",
								func.Parameters);
					case "Hour"     :
					case "Minute"   :
					case "Second"   : return new SqlExpression(func.SystemType, string.Format("({{0}}::datetime {0} to {0})::char(3)::int", func.Name), func.Parameters);
				}
			}

			return expr;
		}

		public virtual object ConvertBooleanValue(bool value)
		{
			return value ? 't' : 'f';
		}

		public override void BuildValue(StringBuilder sb, object value)
		{
			if (value is bool || value is bool?)
				sb.Append("'").Append(ConvertBooleanValue((bool)value)).Append("'");
			else
				base.BuildValue(sb, value);
		}

		protected override void BuildDataType(StringBuilder sb, SqlDataType type)
		{
			switch (type.SqlDbType)
			{
				case SqlDbType.TinyInt    : sb.Append("SmallInt");        break;
				case SqlDbType.SmallMoney : sb.Append("Decimal(10,4)");   break;
				default                   : base.BuildDataType(sb, type); break;
			}
		}

		static void SetQueryParameter(IQueryElement element)
		{
			if (element.ElementType == QueryElementType.SqlParameter)
				((SqlParameter)element).IsQueryParameter = false;
		}

		public override SqlQuery Finalize(SqlQuery sqlQuery)
		{
			CheckAliases(sqlQuery, int.MaxValue);

			new QueryVisitor().Visit(sqlQuery.Select, SetQueryParameter);

			//if (sqlQuery.QueryType == QueryType.InsertOrUpdate)
			//{
			//	foreach (var key in sqlQuery.Insert.Items)
			//		new QueryVisitor().Visit(key.Expression, SetQueryParameter);
			//
			//	foreach (var key in sqlQuery.Update.Items)
			//		new QueryVisitor().Visit(key.Expression, SetQueryParameter);
			//}

			sqlQuery = base.Finalize(sqlQuery);

			switch (sqlQuery.QueryType)
			{
				case QueryType.Delete :
					sqlQuery = GetAlternativeDelete(sqlQuery);
					sqlQuery.From.Tables[0].Alias = "$";
					break;

				case QueryType.Update :
					sqlQuery = GetAlternativeUpdate(sqlQuery);
					break;
			}

			return sqlQuery;
		}

		protected override void BuildFromClause(StringBuilder sb)
		{
			if (!SqlQuery.IsUpdate)
				base.BuildFromClause(sb);
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter   : return "?";
				case ConvertType.NameToCommandParameter :
				case ConvertType.NameToSprocParameter   : return ":" + value;
				case ConvertType.SprocParameterToName   :
					if (value != null)
					{
						var str = value.ToString();
						return (str.Length > 0 && str[0] == ':')? str.Substring(1): str;
					}

					break;
			}

			return value;
		}

		//protected override void BuildInsertOrUpdateQuery(StringBuilder sb)
		//{
		//	BuildInsertOrUpdateQueryAsMerge(sb, "FROM SYSTABLES");
		//}
	}
}
