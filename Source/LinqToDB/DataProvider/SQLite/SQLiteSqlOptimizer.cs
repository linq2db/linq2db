using System;

namespace LinqToDB.DataProvider.SQLite
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	class SQLiteSqlOptimizer : BasicSqlOptimizer
	{
		public SQLiteSqlOptimizer(SqlProviderFlags sqlProviderFlags)
			: base(sqlProviderFlags)
		{
		}

		public override SqlStatement Finalize(SqlStatement statement)
		{
			statement = base.Finalize(statement);

			switch (statement.QueryType)
			{
				case QueryType.Delete :
					statement = GetAlternativeDelete((SelectQuery)statement);
					((SelectQuery)statement).From.Tables[0].Alias = "$";
					break;

				case QueryType.Update :
					statement = GetAlternativeUpdate((SelectQuery)statement);
					break;
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
					case "+": return be.SystemType == typeof(string)? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence): expr;
					case "^": // (a + b) - (a & b) * 2
						return Sub(
							Add(be.Expr1, be.Expr2, be.SystemType),
							Mul(new SqlBinaryExpression(be.SystemType, be.Expr1, "&", be.Expr2), 2), be.SystemType);
				}
			}
			else if (expr is SqlFunction)
			{
				var func = (SqlFunction) expr;

				switch (func.Name)
				{
					case "Space"   : return new SqlFunction(func.SystemType, "PadR", new SqlValue(" "), func.Parameters[0]);
					case "Convert" :
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
								if (IsDateDataType(func.Parameters[0], "Date"))
									return new SqlFunction(func.SystemType, "Date", func.Parameters[1]);
								return new SqlFunction(func.SystemType, "DateTime", func.Parameters[1]);
							}

							return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, func.Parameters[1], func.Parameters[0]);
						}
				}
			}
			else if (expr is SqlExpression)
			{
				var e = (SqlExpression)expr;

				if (e.Expr.StartsWith("Cast(StrFTime(Quarter"))
					return Inc(Div(Dec(new SqlExpression(e.SystemType, e.Expr.Replace("Cast(StrFTime(Quarter", "Cast(StrFTime('%m'"), e.Parameters)), 3));

				if (e.Expr.StartsWith("Cast(StrFTime('%w'"))
					return Inc(new SqlExpression(e.SystemType, e.Expr.Replace("Cast(StrFTime('%w'", "Cast(strFTime('%w'"), e.Parameters));

				if (e.Expr.StartsWith("Cast(StrFTime('%f'"))
					return new SqlExpression(e.SystemType, "Cast(strFTime('%f', {0}) * 1000 as int) % 1000", Precedence.Multiplicative, e.Parameters);

				if (e.Expr.StartsWith("DateTime"))
				{
					if (e.Expr.EndsWith("Quarter')"))
						return new SqlExpression(e.SystemType, "DateTime({1}, '{0} Month')", Precedence.Primary, Mul(e.Parameters[0], 3), e.Parameters[1]);

					if (e.Expr.EndsWith("Week')"))
						return new SqlExpression(e.SystemType, "DateTime({1}, '{0} Day')",   Precedence.Primary, Mul(e.Parameters[0], 7), e.Parameters[1]);
				}
			}

			return expr;
		}
	}
}
