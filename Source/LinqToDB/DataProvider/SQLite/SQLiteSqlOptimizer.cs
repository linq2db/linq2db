namespace LinqToDB.DataProvider.SQLite
{
	using Common;
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	class SQLiteSqlOptimizer : BasicSqlOptimizer
	{
		public SQLiteSqlOptimizer(SqlProviderFlags sqlProviderFlags, AstFactory ast)
			: base(sqlProviderFlags, ast)
		{ }

		public override bool CanCompareSearchConditions => true;

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			switch (statement.QueryType)
			{
				case QueryType.Delete :
				{
					statement = GetAlternativeDelete((SqlDeleteStatement)statement);
					statement.SelectQuery!.From.Tables[0].Alias = "$";
					break;
				}

				case QueryType.Update :
				{
					if (SqlProviderFlags.IsUpdateFromSupported)
					{
						statement = GetAlternativeUpdatePostgreSqlite((SqlUpdateStatement)statement);
					}
					else
					{
						statement = GetAlternativeUpdate((SqlUpdateStatement)statement);
					}

					break;
				}
			}

			return statement;
		}

		public override ISqlPredicate ConvertSearchStringPredicate(
			SqlPredicate.SearchString              predicate,
			ConvertVisitor<RunOptimizationContext> visitor)
		{
			var like = ConvertSearchStringPredicateViaLike(predicate, visitor);

			if (predicate.CaseSensitive.EvaluateBoolExpression(visitor.Context.OptimizationContext.Context) == true)
			{
				ISqlPredicate? subStrPredicate = null;

				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.StartsWith:
					{
						subStrPredicate = ast.Equal(
							new SqlFunction(typeof(string), "Substr", 
								predicate.Expr1, 
								ast.One,
								new SqlFunction(typeof(int), "Length", predicate.Expr2)),
							predicate.Expr2);

						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						subStrPredicate = ast.Equal(
							new SqlFunction(typeof(string), "Substr",
								predicate.Expr1,
								ast.Negate(
									new SqlFunction(typeof(int), "Length", predicate.Expr2),
									typeof(int))
							),
							predicate.Expr2);

						break;
					}
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						subStrPredicate = ast.Greater(
							new SqlFunction(typeof(int), "InStr", predicate.Expr1, predicate.Expr2),
							ast.Zero);

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

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expression, ConvertVisitor<RunOptimizationContext> visitor)
		{
			expression = base.ConvertExpressionImpl(expression, visitor);

			if (expression is SqlBinaryExpression be)
			{
				switch (be.Operation)
				{
					case "+": return be.SystemType == typeof(string)? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence) : expression;
					case "^": // (a + b) - (a & b) * 2
						return Sub(
							Add(be.Expr1, be.Expr2, be.SystemType),
							Mul(new SqlBinaryExpression(be.SystemType, be.Expr1, "&", be.Expr2), 2), be.SystemType);
				}
			}
			else if (expression is SqlFunction func)
			{
				switch (func.Name)
				{
					case "Space"   : return new SqlFunction(func.SystemType, "PadR", new SqlValue(" "), func.Parameters[0]);
					case "Convert" :
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
							if (IsDateDataType(func.Parameters[0], "Date"))
								return new SqlFunction(func.SystemType, "Date", func.Parameters[1]);
							return new SqlFunction(func.SystemType, "DateTime", func.Parameters[1]);
						}

						return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, func.Parameters[1], func.Parameters[0]);
					}
				}
			}

			return expression;
		}

		public override ISqlPredicate ConvertPredicateImpl(ISqlPredicate predicate, ConvertVisitor<RunOptimizationContext> visitor)
		{
			if (predicate is SqlPredicate.ExprExpr exprExpr)
			{
				var (left, op, right, _) = exprExpr;
				var leftType  = QueryHelper.GetDbDataType(left);
				var rightType = QueryHelper.GetDbDataType(right);

				if ((IsDateTime(leftType) || IsDateTime(rightType)) &&
				    !( left.TryEvaluateExpression(visitor.Context.OptimizationContext.Context, out var value1) && value1 == null ||
				      right.TryEvaluateExpression(visitor.Context.OptimizationContext.Context, out var value2) && value2 == null))
				{
					bool modified = false;

					if (left is not SqlFunction { Name: PseudoFunctions.CONVERT or "DateTime" })
					{
						left = ast.Convert(new SqlDataType(leftType), left, fromType: new SqlDataType(leftType));
						modified = true;
					}
					
					if (right is not SqlFunction { Name: PseudoFunctions.CONVERT or "DateTime" })
					{
						right = ast.Convert(new SqlDataType(rightType), right, fromType: new SqlDataType(rightType));
						modified = true;
					}

					if (modified)
						predicate = ast.Comparison(left, op, right);
				}
			}

			return base.ConvertPredicateImpl(predicate, visitor);
		}


		private static bool IsDateTime(DbDataType dbDataType)
		{
			if (dbDataType.DataType == DataType.Date           ||
				dbDataType.DataType == DataType.Time           ||
				dbDataType.DataType == DataType.DateTime       ||
				dbDataType.DataType == DataType.DateTime2      ||
				dbDataType.DataType == DataType.DateTimeOffset ||
				dbDataType.DataType == DataType.SmallDateTime  ||
				dbDataType.DataType == DataType.Timestamp)
				return true;

			if (dbDataType.DataType != DataType.Undefined)
				return false;

			return IsDateTime(dbDataType.SystemType);
		}

		private static bool IsDateTime(Type type)
		{
			return    type == typeof(DateTime)
			          || type == typeof(DateTimeOffset)
			          || type == typeof(DateTime?)
			          || type == typeof(DateTimeOffset?);
		}

		protected override ISqlExpression ConvertConvertion(SqlFunction func)
		{
			if (!func.DoNotOptimize)
			{
				var from = (SqlDataType)func.Parameters[1];
				var to   = (SqlDataType)func.Parameters[0];

				// prevent same-type conversion removal as it is necessary in case of SQLite
				// to ensure that we get proper type, because converted value could have any type actually
				if (from.Type.EqualsDbOnly(to.Type))
					func.DoNotOptimize = true;
			}

			return base.ConvertConvertion(func);
		}
	}
}
