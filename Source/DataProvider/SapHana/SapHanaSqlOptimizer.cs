using System;

namespace LinqToDB.DataProvider.SapHana
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	class SapHanaSqlOptimizer : BasicSqlOptimizer
	{
		public SapHanaSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{

		}

		public override SqlQuery Finalize(SqlQuery sqlQuery)
		{
			var selectQuery = (SelectQuery)base.Finalize(sqlQuery);

			switch (selectQuery.QueryType)
			{
				case QueryType.Delete:
					selectQuery = GetAlternativeDelete(selectQuery);
					break;
				case QueryType.Update:
					selectQuery = GetAlternativeUpdate(selectQuery);
					break;
			}

			return selectQuery;
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlFunction)
			{
				var func = expr as SqlFunction;
				if (func.Name == "Convert")
				{
					var ftype = func.SystemType.ToUnderlying();

					if (ftype == typeof(bool))
					{
						var ex = AlternativeConvertToBoolean(func, 1);
						if (ex != null)
							return ex;
					}
					return new SqlExpression(func.SystemType, "Cast({0} as {1})", PrecedenceLevel.Primary, FloorBeforeConvert(func), func.Parameters[0]);
				}
			}
			else if (expr is SqlBinaryExpression)
			{
				var be = expr as SqlBinaryExpression;

				switch (be.Operation)
				{
					case "%":
						return new SqlFunction(be.SystemType, "MOD", be.Expr1, be.Expr2);
					case "&": 
						return new SqlFunction(be.SystemType, "BITAND", be.Expr1, be.Expr2);
					case "|":
						return Sub(
							Add(be.Expr1, be.Expr2, be.SystemType),
							new SqlFunction(be.SystemType, "BITAND", be.Expr1, be.Expr2),
							be.SystemType);
					case "^": // (a + b) - BITAND(a, b) * 2
						return Sub(
							Add(be.Expr1, be.Expr2, be.SystemType),
							Mul(new SqlFunction(be.SystemType, "BITAND", be.Expr1, be.Expr2), 2),
							be.SystemType);
					case "+": 
						return be.SystemType == typeof(string) ? 
							new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence) : 
							expr;
				}
			}

			return expr;
		}
	}
}
