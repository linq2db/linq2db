using System;

namespace LinqToDB.DataProvider.SqlCe
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	class SqlCeSqlOptimizer : BasicSqlOptimizer
	{
		public SqlCeSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlQuery Finalize(SqlQuery sqlQuery)
		{
			var sql = (SelectQuery)base.Finalize(sqlQuery);

			new QueryVisitor().Visit(sql.Select, element =>
			{
				if (element.ElementType == QueryElementType.SqlParameter)
				{
					((SqlParameter)element).IsQueryParameter = false;
					sql.IsParameterDependent = true;
				}
			});

			switch (sql.QueryType)
			{
				case QueryType.Delete :
					sql = GetAlternativeDelete(sql);
					sql.From.Tables[0].Alias = "$";
					break;

				case QueryType.Update :
					sql = GetAlternativeUpdate(sql);
					break;
			}

			return sql;
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
						return be.Expr1.SystemType.IsIntegerType()?
							be :
							new SqlBinaryExpression(
								typeof(int),
								new SqlFunction(typeof(int), "Convert", SqlDataType.Int32, be.Expr1),
								be.Operation,
								be.Expr2,
								be.Precedence);
				}
			}
			else if (expr is SqlFunction)
			{
				var func = (SqlFunction)expr;

				switch (func.Name)
				{
					case "Convert" :
						switch (Type.GetTypeCode(func.SystemType.ToUnderlying()))
						{
							case TypeCode.UInt64 :
								if (func.Parameters[1].SystemType.IsFloatType())
									return new SqlFunction(
										func.SystemType,
										func.Name,
										func.Precedence,
										func.Parameters[0],
										new SqlFunction(func.SystemType, "Floor", func.Parameters[1]));

								break;

							case TypeCode.DateTime :
								var type1 = func.Parameters[1].SystemType.ToUnderlying();

								if (IsTimeDataType(func.Parameters[0]))
								{
									if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
										return new SqlExpression(
											func.SystemType, "Cast(Convert(NChar, {0}, 114) as DateTime)", PrecedenceLevel.Primary, func.Parameters[1]);

									if (func.Parameters[1].SystemType == typeof(string))
										return func.Parameters[1];

									return new SqlExpression(
										func.SystemType, "Convert(NChar, {0}, 114)", PrecedenceLevel.Primary, func.Parameters[1]);
								}

								if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
								{
									if (IsDateDataType(func.Parameters[0], "Datetime"))
										return new SqlExpression(
											func.SystemType, "Cast(Floor(Cast({0} as Float)) as DateTime)", PrecedenceLevel.Primary, func.Parameters[1]);
								}

								break;
						}

						break;
				}
			}

			return expr;
		}

	}
}
