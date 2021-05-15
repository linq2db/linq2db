using System;

namespace LinqToDB.DataProvider.Oracle
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;
	using Mapping;

	public class Oracle11SqlOptimizer : BasicSqlOptimizer
	{
		public Oracle11SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement Finalize(SqlStatement statement)
		{
			CheckAliases(statement, 30);

			return base.Finalize(statement);
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			statement = ReplaceTakeSkipWithRowNum(statement, false);

			switch (statement.QueryType)
			{
				case QueryType.Delete : statement = GetAlternativeDelete((SqlDeleteStatement) statement); break;
				case QueryType.Update : statement = GetAlternativeUpdate((SqlUpdateStatement) statement); break;
			}

			return statement;
		}

		protected static string[] OracleLikeCharactersToEscape = {"%", "_"};

		public override string[] LikeCharactersToEscape => OracleLikeCharactersToEscape;

		public override bool IsParameterDependedElement(IQueryElement element)
		{
			if (base.IsParameterDependedElement(element))
				return true;

			switch (element.ElementType)
			{
				case QueryElementType.ExprExprPredicate:
				{
					var expr = (SqlPredicate.ExprExpr)element;

					// Oracle saves empty string as null to database, so we need predicate modification before sending query
					//
					if ((expr.Operator == SqlPredicate.Operator.Equal          ||
						 expr.Operator == SqlPredicate.Operator.NotEqual       ||
						 expr.Operator == SqlPredicate.Operator.GreaterOrEqual ||
						 expr.Operator == SqlPredicate.Operator.LessOrEqual) && expr.WithNull == true)
					{
						if (expr.Expr1.SystemType == typeof(string) && expr.Expr1.CanBeEvaluated(true))
							return true;
						if (expr.Expr2.SystemType == typeof(string) && expr.Expr2.CanBeEvaluated(true))
							return true;
					}
					break;
				}
			}

			return false;
		}

		public override ISqlPredicate ConvertPredicateImpl<TContext>(MappingSchema mappingSchema, ISqlPredicate predicate, ConvertVisitor<RunOptimizationContext<TContext>> visitor, OptimizationContext optimizationContext)
		{
			switch (predicate.ElementType)
			{
				case QueryElementType.ExprExprPredicate:
				{
					var expr = (SqlPredicate.ExprExpr)predicate;

					// Oracle saves empty string as null to database, so we need predicate modification before sending query
					//
					if (expr.WithNull == true &&
						(expr.Operator == SqlPredicate.Operator.Equal          ||
						 expr.Operator == SqlPredicate.Operator.NotEqual       ||
						 expr.Operator == SqlPredicate.Operator.GreaterOrEqual ||
						 expr.Operator == SqlPredicate.Operator.LessOrEqual))
					{
						if (expr.Expr1.SystemType == typeof(string) &&
						    expr.Expr1.TryEvaluateExpression(optimizationContext.Context, out var value1) && value1 is string string1)
						{
							if (string1 == "")
							{
								var sc = new SqlSearchCondition();
								sc.Conditions.Add(new SqlCondition(false, new SqlPredicate.ExprExpr(expr.Expr1, expr.Operator, expr.Expr2, null), true));
								sc.Conditions.Add(new SqlCondition(false, new SqlPredicate.IsNull(expr.Expr2, false), true));
								return sc;
							}
						}

						if (expr.Expr2.SystemType == typeof(string) &&
						    expr.Expr2.TryEvaluateExpression(optimizationContext.Context, out var value2) && value2 is string string2)
						{
							if (string2 == "")
							{
								var sc = new SqlSearchCondition();
								sc.Conditions.Add(new SqlCondition(false, new SqlPredicate.ExprExpr(expr.Expr1, expr.Operator, expr.Expr2, null), true));
								sc.Conditions.Add(new SqlCondition(false, new SqlPredicate.IsNull(expr.Expr1, false), true));
								return sc;
							}
						}
					}
					break;
				}
			}

			predicate = base.ConvertPredicateImpl(mappingSchema, predicate, visitor, optimizationContext);

			return predicate;
		}

		public override ISqlExpression ConvertExpressionImpl<TContext>(ISqlExpression expression, ConvertVisitor<TContext> visitor,
			EvaluationContext context)
		{
			expression = base.ConvertExpressionImpl(expression, visitor, context);

			if (expression is SqlBinaryExpression be)
			{
				switch (be.Operation)
				{
					case "%": return new SqlFunction(be.SystemType, "MOD", be.Expr1, be.Expr2);
					case "&": return new SqlFunction(be.SystemType, "BITAND", be.Expr1, be.Expr2);
					case "|": // (a + b) - BITAND(a, b)
						return Sub(
							Add(be.Expr1, be.Expr2, be.SystemType),
							new SqlFunction(be.SystemType, "BITAND", be.Expr1, be.Expr2),
							be.SystemType);

					case "^": // (a + b) - BITAND(a, b) * 2
						return Sub(
							Add(be.Expr1, be.Expr2, be.SystemType),
							Mul(new SqlFunction(be.SystemType, "BITAND", be.Expr1, be.Expr2), 2),
							be.SystemType);
					case "+": return be.SystemType == typeof(string) ? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence) : expression;
				}
			}
			else if (expression is SqlFunction func)
			{
				switch (func.Name)
				{
					case "Coalesce":
					{
						return ConvertCoalesceToBinaryFunc(func, "Nvl");
					}
					case "Convert"        :
					{
						var ftype = func.SystemType.ToUnderlying();

						if (ftype == typeof(bool))
						{
							var ex = AlternativeConvertToBoolean(func, 1);
							if (ex != null)
								return ex;
						}

						if (ftype == typeof(DateTime) || ftype == typeof(DateTimeOffset))
						{
							if (IsTimeDataType(func.Parameters[0]))
							{
								if (func.Parameters[1].SystemType == typeof(string))
									return func.Parameters[1];

								return new SqlFunction(func.SystemType, "To_Char", func.Parameters[1], new SqlValue("HH24:MI:SS"));
							}

							if (IsDateDataType(func.Parameters[0], "Date"))
							{
								if (func.Parameters[1].SystemType!.ToUnderlying() == typeof(DateTime)
									|| func.Parameters[1].SystemType!.ToUnderlying() == typeof(DateTimeOffset))
								{
									return new SqlFunction(func.SystemType, "Trunc", func.Parameters[1], new SqlValue("DD"));
								}

								return new SqlFunction(func.SystemType, "TO_DATE", func.Parameters[1], new SqlValue("YYYY-MM-DD"));
							}
							else if (IsDateDataOffsetType(func.Parameters[0]))
							{
								if (ftype == typeof(DateTimeOffset))
									return func.Parameters[1];

								return new SqlFunction(func.SystemType, "TO_TIMESTAMP_TZ", func.Parameters[1], new SqlValue("YYYY-MM-DD HH24:MI:SS"));
							}

							return new SqlFunction(func.SystemType, "TO_TIMESTAMP", func.Parameters[1], new SqlValue("YYYY-MM-DD HH24:MI:SS"));
						}

						return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);
					}

					case "CharIndex"      :
						return func.Parameters.Length == 2?
							new SqlFunction(func.SystemType, "InStr", func.Parameters[1], func.Parameters[0]):
							new SqlFunction(func.SystemType, "InStr", func.Parameters[1], func.Parameters[0], func.Parameters[2]);
					case "Avg"            : 
						return new SqlFunction(
							func.SystemType,
							"Round",
							new SqlFunction(func.SystemType, "AVG", func.Parameters[0]),
							new SqlValue(27));
				}
			}
			else if (expression is SqlExpression e)
			{
				if (e.Expr.StartsWith("To_Number(To_Char(") && e.Expr.EndsWith(", 'FF'))"))
					return Div(new SqlExpression(e.SystemType, e.Expr.Replace("To_Number(To_Char(", "to_Number(To_Char("), e.Parameters), 1000);
			}

			return expression;
		}

		static readonly ISqlExpression RowNumExpr = new SqlExpression(typeof(long), "ROWNUM", Precedence.Primary, SqlFlags.IsAggregate | SqlFlags.IsWindowFunction);

		/// <summary>
		/// Replaces Take/Skip by ROWNUM usage.
		/// See <a href="https://blogs.oracle.com/oraclemagazine/on-rownum-and-limiting-results">'Pagination with ROWNUM'</a> for more information.
		/// </summary>
		/// <param name="statement">Statement which may contain take/skip modifiers.</param>
		/// <param name="onlySubqueries">Indicates when transformation needed only for subqueries.</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when optimization has been performed.</returns>
		protected SqlStatement ReplaceTakeSkipWithRowNum(SqlStatement statement, bool onlySubqueries)
		{
			return QueryHelper.WrapQuery(statement,
				(query, _) =>
				{
					if (query.Select.TakeValue == null && query.Select.SkipValue == null)
						return 0;
					if (query.Select.SkipValue != null)
						return 2;

					if (query.Select.TakeValue != null && query.Select.OrderBy.IsEmpty && query.GroupBy.IsEmpty && !query.Select.IsDistinct)
					{
						query.Select.Where.EnsureConjunction().Expr(RowNumExpr)
							.LessOrEqual.Expr(query.Select.TakeValue);

						query.Select.Take(null, null);
						return 0;
					}
						
					return 1;
				}
				, queries =>
				{
					var query = queries[queries.Count - 1];
					var processingQuery = queries[queries.Count - 2];

					if (query.Select.SkipValue != null)
					{
						var rnColumn = processingQuery.Select.AddNewColumn(RowNumExpr);
						rnColumn.Alias = "RN";

						if (query.Select.TakeValue != null)
						{
							processingQuery.Where.EnsureConjunction().Expr(RowNumExpr)
								.LessOrEqual.Expr(new SqlBinaryExpression(query.Select.SkipValue.SystemType!,
									query.Select.SkipValue, "+", query.Select.TakeValue));
						}

						queries[queries.Count - 3].Where.Expr(rnColumn).Greater.Expr(query.Select.SkipValue);
					}
					else
					{
						processingQuery.Where.EnsureConjunction().Expr(RowNumExpr)
							.LessOrEqual.Expr(query.Select.TakeValue!);
					}

					query.Select.SkipValue = null;
					query.Select.Take(null, null);

				},
				allowMutation: true
				);
		}

		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);
			return base.ConvertFunction(func);
		}
	}
}
