﻿using System.Collections.Generic;

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

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			return statement.QueryType switch
			{
				QueryType.Update => CorrectMySqlUpdate((SqlUpdateStatement)statement),
				_                => statement,
			};
		}

		private SqlUpdateStatement CorrectMySqlUpdate(SqlUpdateStatement statement)
		{
			if (statement.SelectQuery.Select.SkipValue != null)
				throw new LinqToDBException("MySql does not support Skip in update query");

			statement = CorrectUpdateTable(statement);

			if (!statement.SelectQuery.OrderBy.IsEmpty)
				statement.SelectQuery.OrderBy.Items.Clear();

			return statement;
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlBinaryExpression be)
			{
				switch (be.Operation)
				{
					case "+":
						if (be.SystemType == typeof(string))
						{
							if (be.Expr1 is SqlFunction func)
							{
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
			else if (expr is SqlFunction func)
			{
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

						if ((ftype == typeof(double) || ftype == typeof(float)) && func.Parameters[1].SystemType!.ToUnderlying() == typeof(decimal))
							return func.Parameters[1];

						return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);
				}
			}

			return expr;
		}
	}
}
