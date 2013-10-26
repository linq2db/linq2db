using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	class SqlServerSqlOptimizer : BasicSqlOptimizer
	{
		public SqlServerSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			switch (expr.ElementType)
			{
				case QueryElementType.SqlBinaryExpression:
					{
						var be = (SqlBinaryExpression)expr;

						switch (be.Operation)
						{
							case "%":
								{
									var type1 = be.Expr1.SystemType.ToUnderlying();

									if (type1 == typeof(double) || type1 == typeof(float))
									{
										return new SqlBinaryExpression(
											be.Expr2.SystemType,
											new SqlFunction(typeof(int), "Convert", SqlDataType.Int32, be.Expr1),
											be.Operation,
											be.Expr2);
									}

									break;
								}
						}

						break;
					}

				case QueryElementType.SqlFunction:
					{
						var func = (SqlFunction)expr;

						switch (func.Name)
						{
							case "Convert" :
								{
									if (func.SystemType.ToUnderlying() == typeof(ulong) &&
										func.Parameters[1].SystemType.IsFloatType())
										return new SqlFunction(
											func.SystemType,
											func.Name,
											func.Precedence,
											func.Parameters[0],
											new SqlFunction(func.SystemType, "Floor", func.Parameters[1]));

									break;
								}
						}

						break;
					}
			}

			return expr;
		}

		public ISqlExpression ConvertConvertFunction(SqlFunction func)
		{
			switch (Type.GetTypeCode(func.SystemType.ToUnderlying()))
			{
				case TypeCode.DateTime :

					if (func.Name == "Convert")
					{
						var type1 = func.Parameters[1].SystemType.ToUnderlying();

						if (IsTimeDataType(func.Parameters[0]))
						{
							if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
								return new SqlExpression(
									func.SystemType, "Cast(Convert(Char, {0}, 114) as DateTime)", Precedence.Primary, func.Parameters[1]);

							if (func.Parameters[1].SystemType == typeof(string))
								return func.Parameters[1];

							return new SqlExpression(
								func.SystemType, "Convert(Char, {0}, 114)", Precedence.Primary, func.Parameters[1]);
						}

						if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
						{
							if (IsDateDataType(func.Parameters[0], "Datetime"))
								return new SqlExpression(
									func.SystemType, "Cast(Floor(Cast({0} as Float)) as DateTime)", Precedence.Primary, func.Parameters[1]);
						}

						if (func.Parameters.Length == 2 && func.Parameters[0] is SqlDataType && func.Parameters[0] == SqlDataType.DateTime)
							return new SqlFunction(func.SystemType, func.Name, func.Precedence, func.Parameters[0], func.Parameters[1], new SqlValue(120));
					}

					break;
			}

			return func;
		}
	}
}
