using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;
	using Mapping;

	abstract class SqlServerSqlOptimizer : BasicSqlOptimizer
	{
		private readonly SqlServerVersion _sqlVersion;

		protected SqlServerSqlOptimizer(SqlProviderFlags sqlProviderFlags, SqlServerVersion sqlVersion) : base(sqlProviderFlags)
		{
			_sqlVersion = sqlVersion;
		}

		protected SqlStatement ReplaceSkipWithRowNumber(SqlStatement statement)
			=> ReplaceTakeSkipWithRowNumber(statement, query => query.Select.SkipValue != null, false);

		protected SqlStatement WrapRootTakeSkipOrderBy(SqlStatement statement)
		{
			return QueryHelper.WrapQuery(
				statement,
				(query, _) => query.ParentSelect == null && (query.Select.SkipValue != null ||
				                                        query.Select.TakeValue != null ||
				                                        query.Select.TakeHints != null || !query.OrderBy.IsEmpty),
				(query, wrappedQuery) => { },
				allowMutation: true
			);
		}


		public override ISqlPredicate ConvertSearchStringPredicate<TContext>(MappingSchema mappingSchema, SqlPredicate.SearchString predicate, ConvertVisitor<RunOptimizationContext<TContext>> visitor,
			OptimizationContext optimizationContext)
		{
			var like = ConvertSearchStringPredicateViaLike(mappingSchema, predicate, visitor,
				optimizationContext);

			if (!predicate.IgnoreCase)
			{
				SqlPredicate.ExprExpr? subStrPredicate = null;

				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.StartsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary, new SqlFunction(
									typeof(string), "LEFT", predicate.Expr1,
									new SqlFunction(typeof(int), "Length", predicate.Expr2))),
								SqlPredicate.Operator.Equal,
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary, predicate.Expr2),
								null
							);

						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary, new SqlFunction(
									typeof(string), "RIGHT", predicate.Expr1,
									new SqlFunction(typeof(int), "Length", predicate.Expr2))),
								SqlPredicate.Operator.Equal,
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary, predicate.Expr2),
								null
							);

						break;
					}
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(int), "CHARINDEX",
									new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary,
										predicate.Expr2),
									new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary,
										predicate.Expr1)),
								SqlPredicate.Operator.Greater,
								new SqlValue(0), null);

						break;
					}

				}

				if (subStrPredicate != null)
				{
					if (predicate.IsNot && like is IInvertibleElement invertible && invertible.CanInvert())
					{
						like = (ISqlPredicate)invertible.Invert();
					}

					var result = new SqlSearchCondition(
						new SqlCondition(predicate.IsNot, like, predicate.IsNot),
						new SqlCondition(predicate.IsNot, subStrPredicate));

					return result;
				}
			}

			return like;
		}

		public override ISqlExpression ConvertExpressionImpl<TContext>(ISqlExpression expression, ConvertVisitor<TContext> visitor,
			EvaluationContext context)
		{
			expression = base.ConvertExpressionImpl(expression, visitor, context);

			switch (expression.ElementType)
			{
				case QueryElementType.SqlBinaryExpression:
					{
						var be = (SqlBinaryExpression)expression;

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
						var func = (SqlFunction)expression;

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

			return expression;
		}

	}
}
