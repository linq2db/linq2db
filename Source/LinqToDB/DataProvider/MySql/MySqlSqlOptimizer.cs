namespace LinqToDB.DataProvider.MySql
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	using SqlBinary = SqlQuery.SqlBinaryExpression;

	class MySqlSqlOptimizer : BasicSqlOptimizer
	{
		public MySqlSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override bool CanCompareSearchConditions => true;

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			return statement.QueryType switch
			{
				QueryType.Update => CorrectMySqlUpdate((SqlUpdateStatement)statement),
				QueryType.Delete => PrepareDelete((SqlDeleteStatement)statement),
				_                => statement,
			};
		}

		SqlStatement PrepareDelete(SqlDeleteStatement statement)
		{
			var tables = statement.SelectQuery.From.Tables;

			if (statement.Output != null && tables.Count == 1 && tables[0].Joins.Count == 0)
				tables[0].Alias = "$";

			return statement;
		}

		private SqlUpdateStatement CorrectMySqlUpdate(SqlUpdateStatement statement)
		{
			if (statement.SelectQuery.Select.SkipValue != null)
				ThrowHelper.ThrowLinqToDBException("MySql does not support Skip in update query");

			statement = CorrectUpdateTable(statement);

			if (!statement.SelectQuery.OrderBy.IsEmpty)
				statement.SelectQuery.OrderBy.Items.Clear();

			return statement;
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expression, ConvertVisitor<RunOptimizationContext> visitor)
		{
			expression = base.ConvertExpressionImpl(expression, visitor);

			return Convert(expression);

			ISqlExpression Convert(ISqlExpression expr)
			{
				switch (expr)
				{
					case SqlBinary(var type, var ex1, "+", var ex2) when type == typeof(string) :
					{
						return ConvertFunc(new (type, "Concat", ex1, ex2));

						static SqlFunction ConvertFunc(SqlFunction func)
						{
							for (var i = 0; i < func.Parameters.Length; i++)
							{
								switch (func.Parameters[i])
								{
									case SqlBinary(var t, var e1, "+", var e2) when t == typeof(string) :
									{
										var ps = new List<ISqlExpression>(func.Parameters);

										ps.RemoveAt(i);
										ps.Insert(i,     e1);
										ps.Insert(i + 1, e2);

										return ConvertFunc(new (t, func.Name, ps.ToArray()));
									}

									case SqlFunction(var t, "Concat") f when t == typeof(string) :
									{
										var ps = new List<ISqlExpression>(func.Parameters);

										ps.RemoveAt(i);
										ps.InsertRange(i, f.Parameters);

										return ConvertFunc(new (t, func.Name, ps.ToArray()));
									}
								}
							}

							return func;
						}
					}

					case SqlFunction(var type, "Convert") func:
					{
						var ftype = type.ToUnderlying();

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

					default : return expr;
				}
			}
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate, ConvertVisitor<RunOptimizationContext> visitor)
		{
			var caseSensitive = predicate.CaseSensitive.EvaluateBoolExpression(visitor.Context.OptimizationContext.Context);

			if (caseSensitive == null || caseSensitive == false)
			{
				var searchExpr = predicate.Expr2;
				var dataExpr = predicate.Expr1;

				if (caseSensitive == false)
				{
					searchExpr = PseudoFunctions.MakeToLower(searchExpr);
					dataExpr   = PseudoFunctions.MakeToLower(dataExpr);
				}

				ISqlPredicate? newPredicate = null;
				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						newPredicate = new SqlPredicate.ExprExpr(
							new SqlFunction(typeof(int), "LOCATE", searchExpr, dataExpr), SqlPredicate.Operator.Greater,
							new SqlValue(0), null);
						break;
					}
				}

				if (newPredicate != null)
				{
					if (predicate.IsNot)
					{
						newPredicate = new SqlSearchCondition(new SqlCondition(true, newPredicate));
					}

					return newPredicate;
				}

				if (caseSensitive == false)
				{
					predicate = new SqlPredicate.SearchString(
						dataExpr,
						predicate.IsNot,
						searchExpr,
						predicate.Kind,
						new SqlValue(false));
				}
			}

			if (caseSensitive == true)
			{
				predicate = new SqlPredicate.SearchString(
					new SqlExpression(typeof(string), $"{{0}} COLLATE utf8_bin", Precedence.Primary, predicate.Expr1),
					predicate.IsNot,
					predicate.Expr2,
					predicate.Kind,
					new SqlValue(false));
			}

			return ConvertSearchStringPredicateViaLike(predicate, visitor);
		}
	}
}
