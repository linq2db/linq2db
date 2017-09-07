using System;
using System.Collections.Generic;
using System.Xml.Schema;

namespace LinqToDB.DataProvider.MySql
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	class MySqlSqlOptimizer : BasicSqlOptimizer
	{
		public MySqlSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlBinaryExpression)
			{
				var be = (SqlBinaryExpression)expr;

				switch (be.Operation)
				{
					case "+":
						if (be.SystemType == typeof(string))
						{
							if (be.Expr1 is SqlFunction)
							{
								var func = (SqlFunction)be.Expr1;

								if (func.Name == "Concat")
								{
									var list = new List<ISqlExpression>(func.Parameters) { be.Expr2 };
									return new SqlFunction(be.SystemType, "Concat", list.ToArray());
								}
							}
							else if (be.Expr1 is SqlBinaryExpression && be.Expr1.SystemType == typeof(string) && ((SqlBinaryExpression)be.Expr1).Operation == "+")
							{
								var list = new List<ISqlExpression> { be.Expr2 };
								var ex   = be.Expr1;

								while (ex is SqlBinaryExpression && ex.SystemType == typeof(string) && ((SqlBinaryExpression)be.Expr1).Operation == "+")
								{
									var bex = (SqlBinaryExpression)ex;

									list.Insert(0, bex.Expr2);
									ex = bex.Expr1;
								}

								list.Insert(0, ex);

								return new SqlFunction(be.SystemType, "Concat", list.ToArray());
							}

							return new SqlFunction(be.SystemType, "Concat", be.Expr1, be.Expr2);
						}

						break;
				}
			}
			else if (expr is SqlFunction)
			{
				var func = (SqlFunction) expr;

				switch (func.Name)
				{
					case "Convert" :
						var ftype = func.SystemType.ToUnderlying();

						if (ftype == typeof(bool))
						{
							var ex = AlternativeConvertToBoolean(func, 1);
							if (ex != null)
								return ex;
						}

						if ((ftype == typeof(double) || ftype == typeof(float)) && func.Parameters[1].SystemType.ToUnderlying() == typeof(decimal))
							return func.Parameters[1];

						return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);
				}
			}

			return expr;
		}
	}
}
