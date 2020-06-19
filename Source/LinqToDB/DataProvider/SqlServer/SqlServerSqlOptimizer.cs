using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	abstract class SqlServerSqlOptimizer : BasicSqlOptimizer
	{
		private readonly SqlServerVersion _sqlVersion;

		protected SqlServerSqlOptimizer(SqlProviderFlags sqlProviderFlags, SqlServerVersion sqlVersion) : base(sqlProviderFlags)
		{
			_sqlVersion = sqlVersion;
		}

		protected SqlStatement ReplaceSkipWithRowNumber(SqlStatement statement)
			=> ReplaceTakeSkipWithRowNumber(statement, query => query.Select.SkipValue != null);

		protected SqlStatement WrapRootTakeSkipOrderBy(SqlStatement statement)
		{
			return QueryHelper.WrapQuery(
				statement,
				query => query.ParentSelect == null && (query.Select.SkipValue != null || query.Select.TakeValue != null || query.Select.TakeHints != null || !query.OrderBy.IsEmpty),
				(query, wrappedQuery) => { }
				);
		}

		protected SqlStatement CorrectEmptyRoot(SqlStatement statement)
		{
			var selectQuery = statement.SelectQuery!;

			if (selectQuery.Select.Columns.Count == 0)
			{
				var source = selectQuery.Select.From.Tables[0].Source;
				var keys = source.GetKeys(true);

				foreach (var key in keys)
				{
					selectQuery.Select.AddNew(key);
				}
			}

			return statement;
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
									var type1 = be.Expr1.SystemType!.ToUnderlying();

									if (type1 == typeof(double) || type1 == typeof(float))
									{
										return new SqlBinaryExpression(
											be.Expr2.SystemType!,
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
										func.Parameters[1].SystemType!.IsFloatType())
										return new SqlFunction(
											func.SystemType,
											func.Name,
											false,
											func.Precedence,
											func.Parameters[0],
											new SqlFunction(func.SystemType, "Floor", func.Parameters[1]));

									if (Type.GetTypeCode(func.SystemType.ToUnderlying()) == TypeCode.DateTime)
									{
										var type1 = func.Parameters[1].SystemType!.ToUnderlying();

										if (IsTimeDataType(func.Parameters[0]))
										{
											if (type1 == typeof(DateTimeOffset) || type1 == typeof(DateTime))
												if (_sqlVersion >= SqlServerVersion.v2008)
													return new SqlExpression(
														func.SystemType, "CAST({0} AS TIME)", Precedence.Primary, func.Parameters[1]);
												else
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
											return new SqlFunction(func.SystemType, func.Name, func.IsAggregate, func.Precedence, func.Parameters[0], func.Parameters[1], new SqlValue(120));
									}


									break;
								}
						}

						break;
					}
			}

			return expr;
		}

	}
}
