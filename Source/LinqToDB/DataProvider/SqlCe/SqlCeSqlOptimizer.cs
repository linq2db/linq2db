using System;

namespace LinqToDB.DataProvider.SqlCe
{
	using Extensions;
	using SqlQuery;
	using SqlProvider;
	using Mapping;

	class SqlCeSqlOptimizer : BasicSqlOptimizer
	{
		public SqlCeSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			// This function mutates statement which is allowed only in this place
			CorrectSkipAndColumns(statement);

			// This function mutates statement which is allowed only in this place
			CorrectInsertParameters(statement);

			CorrectFunctionParameters(statement);

			switch (statement.QueryType)
			{
				case QueryType.Delete :
					statement = GetAlternativeDelete((SqlDeleteStatement) statement);
					statement.SelectQuery!.From.Tables[0].Alias = "$";
					break;

				case QueryType.Update :
					statement = GetAlternativeUpdate((SqlUpdateStatement) statement);
					break;
			}


			// call fixer after CorrectSkipAndColumns for remaining cases
			base.FixEmptySelect(statement);

			return statement;
		}

		protected static string[] LikeSqlCeCharactersToEscape = { "_", "%" };

		public override string[] LikeCharactersToEscape => LikeSqlCeCharactersToEscape;

		public override ISqlPredicate ConvertSearchStringPredicate<TContext>(MappingSchema mappingSchema, SqlPredicate.SearchString predicate, ConvertVisitor<RunOptimizationContext<TContext>> visitor,
			OptimizationContext optimizationContext)
		{
			var like = ConvertSearchStringPredicateViaLike(mappingSchema, predicate, visitor,
				optimizationContext);

			if (predicate.CaseSensitive.EvaluateBoolExpression(optimizationContext.Context))
			{
				SqlPredicate.ExprExpr? subStrPredicate = null;

				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.StartsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary, 
									new SqlFunction(typeof(string), "SUBSTRING", 
										predicate.Expr1,
										new SqlValue(1),
									new SqlFunction(typeof(int), "Length", predicate.Expr2))),
								SqlPredicate.Operator.Equal,
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary, predicate.Expr2),
								null
							);
						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						var indexExpression = new SqlBinaryExpression(typeof(int),
							new SqlBinaryExpression(typeof(int),
								new SqlFunction(typeof(int), "Length", predicate.Expr1),
								"-",
								new SqlFunction(typeof(int), "Length", predicate.Expr2)),
							"+",
							new SqlValue(1));

						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(byte[]), "Convert", SqlDataType.DbVarBinary, 
									new SqlFunction(typeof(string), "SUBSTRING", 
										predicate.Expr1,
										indexExpression,
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
					var result = new SqlSearchCondition(
						new SqlCondition(false, like, predicate.IsNot),
						new SqlCondition(predicate.IsNot, subStrPredicate));

					return result;
				}
			}

			return like;
		}

		void CorrectInsertParameters(SqlStatement statement)
		{
			//SlqCe do not support parameters in columns for insert
			//
			if (statement.IsInsert())
			{
				var query = statement.SelectQuery;
				if (query != null)
				{
					foreach (var column in query.Select.Columns)
					{
						if (column.Expression is SqlParameter parameter)
						{
							parameter.IsQueryParameter = false;
						}
					}
				}
			}
		}

		void CorrectSkipAndColumns(SqlStatement statement)
		{
			statement.Visit(static e =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlQuery:
						{
							var q = (SelectQuery)e;

							if (q.Select.SkipValue != null && q.OrderBy.IsEmpty)
							{
								if (q.Select.Columns.Count == 0)
								{
									var source = q.Select.From.Tables[0].Source;
									var keys   = source.GetKeys(true);

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

							// looks like SqlCE do not allow '*' for grouped records
							if (!q.GroupBy.IsEmpty && q.Select.Columns.Count == 0)
							{
								q.Select.Add(new SqlValue(1));
							}

							break;
						}
				}
			});
		}

		void CorrectFunctionParameters(SqlStatement statement)
		{
			if (!SqlCeConfiguration.InlineFunctionParameters)
				return;

			statement.Visit(static e =>
			{
				if (e.ElementType == QueryElementType.SqlFunction)
				{
					var sqlFunction = (SqlFunction)e;
					foreach (var parameter in sqlFunction.Parameters)
					{
						if (parameter.ElementType == QueryElementType.SqlParameter &&
						    parameter is SqlParameter sqlParameter)
						{
							sqlParameter.IsQueryParameter = false;
						}
					}
				}
			});
		}

		protected override void FixEmptySelect(SqlStatement statement)
		{
			// already fixed by CorrectSkipAndColumns
		}

		public override ISqlExpression ConvertExpressionImpl<TContext>(ISqlExpression expression, ConvertVisitor<TContext> visitor,
			EvaluationContext context)
		{
			expression = base.ConvertExpressionImpl(expression, visitor, context);

			switch (expression)
			{
				case SqlBinaryExpression be:
					switch (be.Operation)
					{
						case "%":
							return be.Expr1.SystemType!.IsIntegerType()?
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
						case "Length":
						{
							return new SqlFunction(func.SystemType, "LEN", func.IsAggregate, func.IsPure,
								func.Precedence, func.Parameters);
						}
						case "Convert" :
							switch (Type.GetTypeCode(func.SystemType.ToUnderlying()))
							{
								case TypeCode.UInt64 :
									if (func.Parameters[1].SystemType!.IsFloatType())
										return new SqlFunction(
											func.SystemType,
											func.Name,
											false,
											func.Precedence,
											func.Parameters[0],
											new SqlFunction(func.SystemType, "Floor", func.Parameters[1]));

									break;

								case TypeCode.DateTime :
									var type1 = func.Parameters[1].SystemType!.ToUnderlying();

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

			return expression;
		}

		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);
			return base.ConvertFunction(func);
		}

	}
}
