using System;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using Extensions;
	using SqlProvider;
	using SqlQuery;

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
					var (a, op, b, withNull) = (SqlPredicate.ExprExpr)element;

					// See ConvertPredicateImpl, we transform empty strings "" into null-handling expressions
					if (withNull != null ||
						(Configuration.Linq.CompareNulls != CompareNulls.LikeSql &&
							op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual))					
					{
						if (a.SystemType == typeof(string) && a.CanBeEvaluated(true))
							return true;
						if (b.SystemType == typeof(string) && b.CanBeEvaluated(true))
							return true;
					}
					break;
				}
			}

			return false;
		}

		public override ISqlPredicate ConvertPredicateImpl(ISqlPredicate predicate, ConvertVisitor<RunOptimizationContext> visitor)
		{
			switch (predicate.ElementType)
			{
				case QueryElementType.ExprExprPredicate:
				{
					var (a, op, b, withNull) = (SqlPredicate.ExprExpr)predicate;

					// We want to modify comparisons involving "" as Oracle treats "" as null

					// Comparisons to constant "" are always converted to IS [NOT] NULL (as == null or == default)
					if (op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual)
					{
						if (a is SqlValue { Value: string { Length: 0 } })
							return new SqlPredicate.IsNull(b, isNot: op == SqlPredicate.Operator.NotEqual);
						if (b is SqlValue { Value: string { Length: 0 } })
							return new SqlPredicate.IsNull(a, isNot: op == SqlPredicate.Operator.NotEqual);
					}

					// CompareNulls.LikeSql compiles as-is, no change
					// CompareNulls.LikeSqlExceptParameters sniffs parameters to == and != and replaces by IS [NOT] NULL
					// CompareNulls.LikeCSharp (withNull) always handles nulls.
					// Note: LikeCSharp sometimes generates `withNull: null` expressions, in which case it works the
					//       same way as LikeSqlExceptParameters (for backward compatibility).
					if (withNull != null || 
						(Configuration.Linq.CompareNulls != CompareNulls.LikeSql && 
							op is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual))
					{
						if (b.SystemType == typeof(string) &&
							b.TryEvaluateExpression(visitor.Context.OptimizationContext.Context, out var bStr) && 
							bStr is string { Length: 0 })
							return CompareToEmptyString(a, op);

						if (a.SystemType == typeof(string) &&
							a.TryEvaluateExpression(visitor.Context.OptimizationContext.Context, out var aStr) && 
							aStr is string { Length: 0 })
							return CompareToEmptyString(b, InvertDirection(op));

						static ISqlPredicate CompareToEmptyString(ISqlExpression x, SqlPredicate.Operator op)
						{
							return op switch
							{
								SqlPredicate.Operator.NotGreater     or
								SqlPredicate.Operator.LessOrEqual    or
								SqlPredicate.Operator.Equal          => new SqlPredicate.IsNull(x, isNot: false),
								SqlPredicate.Operator.NotLess        or
								SqlPredicate.Operator.Greater        or
								SqlPredicate.Operator.NotEqual       => new SqlPredicate.IsNull(x, isNot: true),
								SqlPredicate.Operator.GreaterOrEqual => new SqlPredicate.ExprExpr(
									// Always true
									new SqlValue(1), SqlPredicate.Operator.Equal, new SqlValue(1), withNull: null),
								SqlPredicate.Operator.Less           => new SqlPredicate.ExprExpr(
									// Always false
									new SqlValue(1), SqlPredicate.Operator.Equal, new SqlValue(0), withNull: null),
								// Overlaps doesn't operate on strings
								_ => throw new InvalidOperationException(),
							};
						}

						static SqlPredicate.Operator InvertDirection(SqlPredicate.Operator op)
						{
							return op switch 
							{
								SqlPredicate.Operator.NotEqual       or 
								SqlPredicate.Operator.Equal          => op,
								SqlPredicate.Operator.Greater        => SqlPredicate.Operator.Less,
								SqlPredicate.Operator.GreaterOrEqual => SqlPredicate.Operator.LessOrEqual,
								SqlPredicate.Operator.Less           => SqlPredicate.Operator.Greater,
								SqlPredicate.Operator.LessOrEqual    => SqlPredicate.Operator.GreaterOrEqual,
								SqlPredicate.Operator.NotGreater     => SqlPredicate.Operator.NotLess,
								SqlPredicate.Operator.NotLess        => SqlPredicate.Operator.NotGreater,
								// Overlaps doesn't operate on strings
								_ => throw new InvalidOperationException(),
							};
						}
					}
					break;
				}
			}

			predicate = base.ConvertPredicateImpl(predicate, visitor);

			return predicate;
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expression, ConvertVisitor<RunOptimizationContext> visitor)
		{
			expression = base.ConvertExpressionImpl(expression, visitor);

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

						if (ftype == typeof(DateTime) || ftype == typeof(DateTimeOffset)
#if NET6_0_OR_GREATER
							|| ftype == typeof(DateOnly)
#endif
							)
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
						else if (ftype == typeof(string))
						{
							var stype = func.Parameters[1].SystemType!.ToUnderlying();

							if (stype == typeof(DateTimeOffset))
							{
								return new SqlFunction(func.SystemType, "To_Char", func.Parameters[1], new SqlValue("YYYY-MM-DD HH24:MI:SS TZH:TZM"));
							}
							else if (stype == typeof(DateTime))
							{
								return new SqlFunction(func.SystemType, "To_Char", func.Parameters[1], new SqlValue("YYYY-MM-DD HH24:MI:SS"));
							}
#if NET6_0_OR_GREATER
							else if (stype == typeof(DateOnly))
							{
								return new SqlFunction(func.SystemType, "To_Char", func.Parameters[1], new SqlValue("YYYY-MM-DD"));
							}
#endif
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
			return QueryHelper.WrapQuery(
				(object?)null,
				statement,
				static (_, query, _) =>
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
				},
				static (_, queries) =>
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
				allowMutation: true,
				withStack: false);
		}

		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);
			return base.ConvertFunction(func);
		}
	}
}
