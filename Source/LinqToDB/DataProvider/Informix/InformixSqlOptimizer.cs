using System;

namespace LinqToDB.DataProvider.Informix
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	class InformixSqlOptimizer : BasicSqlOptimizer
	{
		public InformixSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		static void SetQueryParameter(IQueryElement element)
		{
			if (element.ElementType == QueryElementType.SqlParameter)
			{
				var p = (SqlParameter)element;
				if (p.SystemType == null || p.SystemType.IsScalar(false))
					p.IsQueryParameter = false;
			}
		}

		public override SqlStatement Finalize(SqlStatement statement)
		{
			CheckAliases(statement, int.MaxValue);

			var query = statement as SelectQuery;
			if (query != null)
			{
				new QueryVisitor().Visit(query.Select, SetQueryParameter);

				statement = base.Finalize(statement);

				switch (statement.QueryType)
				{
					case QueryType.Delete:
						statement = GetAlternativeDelete((SelectQuery)statement);
						query.From.Tables[0].Alias = "$";
						break;

					case QueryType.Update:
						statement = GetAlternativeUpdate((SelectQuery)statement);
						break;
				}
			}

			return statement;
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

	}
}
