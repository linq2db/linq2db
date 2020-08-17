﻿using System;

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

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			switch (statement.QueryType)
			{
				case QueryType.Delete :
					statement = GetAlternativeDelete((SqlDeleteStatement)statement);
					statement.SelectQuery!.From.Tables[0].Alias = "$";
					break;

				case QueryType.Update :
					statement = GetAlternativeUpdate((SqlUpdateStatement)statement);
					break;
			}

			return statement;
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr, bool withParameters)
		{
			expr = base.ConvertExpression(expr, withParameters);

			if (expr is SqlBinaryExpression be)
			{
				switch (be.Operation)
				{
					case "+": return be.SystemType == typeof(string)? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence) : expr;
					case "^": // (a + b) - (a & b) * 2
						return Sub(
							Add(be.Expr1, be.Expr2, be.SystemType),
							Mul(new SqlBinaryExpression(be.SystemType, be.Expr1, "&", be.Expr2), 2), be.SystemType);
				}
			}
			else if (expr is SqlFunction func)
			{
				switch (func.Name)
				{
					case "Space"   : return new SqlFunction(func.SystemType, "PadR", new SqlValue(" "), func.Parameters[0]);
					case "Convert" :
					{
						var ftype = func.SystemType.ToUnderlying();

						if (ftype == typeof(bool))
						{
							var ex = AlternativeConvertToBoolean(func, 1, withParameters);
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

			return expr;
		}
	}
}
