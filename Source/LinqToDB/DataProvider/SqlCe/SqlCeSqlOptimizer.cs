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

		public override SqlStatement Finalize(SqlStatement statement)
		{
			statement = base.Finalize(statement);

			var selectQuery = statement.SelectQuery;
			if (selectQuery != null)
				new QueryVisitor().Visit(selectQuery.Select, element =>
				{
					if (element.ElementType == QueryElementType.SqlParameter)
					{
						var p = (SqlParameter)element;
						if (p.SystemType == null || p.SystemType.IsScalar(false))
						{
							p.IsQueryParameter = false;

							selectQuery.IsParameterDependent = true;
						}
					}
				});

			switch (statement.QueryType)
			{
				case QueryType.Delete :
					statement = GetAlternativeDelete((SqlDeleteStatement) statement);
					statement.SelectQuery.From.Tables[0].Alias = "$";
					break;

				case QueryType.Update :
					statement = GetAlternativeUpdate((SqlUpdateStatement) statement);
					break;
			}

			if (selectQuery != null)
				CorrectSkip(selectQuery);

			return statement;
		}

		private void CorrectSkip(SelectQuery selectQuery)
		{
			((ISqlExpressionWalkable)selectQuery).Walk(false, e =>
			{
				if (e is SelectQuery q && q.Select.SkipValue != null && q.OrderBy.IsEmpty)
				{
					if (q.Select.Columns.Count == 0)
					{
						var source = q.Select.From.Tables[0].Source;
						var keys = source.GetKeys(true);

						foreach (var key in keys)
						{
							q.Select.AddNew(key);
						}
					}

					for (var i = 0; i < q.Select.Columns.Count; i++)
						q.OrderBy.ExprAsc(q.Select.Columns[i].Expression);

					if (q.OrderBy.IsEmpty)
					{
						throw new LinqToDBException("Order by required for Skip operation.");
					}
				}
				return e;
			}
			);
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			switch (expr)
			{
				case SqlBinaryExpression be:
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

					break;

				case SqlFunction func:
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
											false,
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
												func.SystemType, "Cast(Convert(NChar, {0}, 114) as DateTime)", Precedence.Primary, func.Parameters[1]);

										if (func.Parameters[1].SystemType == typeof(string))
											return func.Parameters[1];

										return new SqlExpression(
											func.SystemType, "Convert(NChar, {0}, 114)", Precedence.Primary, func.Parameters[1]);
									}

									if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
									{
										if (IsDateDataType(func.Parameters[0], "Datetime"))
											return new SqlExpression(
												func.SystemType, "Cast(Floor(Cast({0} as Float)) as DateTime)", Precedence.Primary, func.Parameters[1]);
									}

									break;
							}

							break;
					}

					break;
			}

			return expr;
		}
	}
}
