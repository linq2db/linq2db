namespace LinqToDB.DataProvider.Access
{
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	class AccessSqlOptimizer : BasicSqlOptimizer
	{
		public AccessSqlOptimizer(SqlProviderFlags sqlProviderFlags, AstFactory ast) 
			: base(sqlProviderFlags, ast)
		{ }

		public override bool CanCompareSearchConditions => true;

		protected static string[] AccessLikeCharactersToEscape = {"_", "?", "*", "%", "#", "-", "!"};

		public override bool   LikeIsEscapeSupported => false;

		public override string[] LikeCharactersToEscape => AccessLikeCharactersToEscape;

		public override ISqlPredicate ConvertLikePredicate(MappingSchema mappingSchema, SqlPredicate.Like predicate,
			EvaluationContext context)
		{
			if (predicate.Escape != null)
			{
				return new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, predicate.Expr2, null);
			}

			return base.ConvertLikePredicate(mappingSchema, predicate, context);
		}

		protected override string EscapeLikePattern(string str)
		{
			var newStr = DataTools.EscapeUnterminatedBracket(str);
			if (newStr == str)
				newStr = newStr.Replace("[", "[[]");

			foreach (var s in LikeCharactersToEscape)
				newStr = newStr.Replace(s, "[" + s + "]");

			return newStr;
		}

		public override ISqlExpression EscapeLikeCharacters(ISqlExpression expression, ref ISqlExpression? escape)
		{
			return ThrowHelper.ThrowLinqException<ISqlExpression>("Access does not support `Replace` function which is required for such query.");
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			return statement.QueryType switch
			{
				QueryType.Delete => GetAlternativeDelete((SqlDeleteStatement)statement),
				QueryType.Update => CorrectAccessUpdate((SqlUpdateStatement)statement),
				_                => statement,
			};
		}

		private SqlUpdateStatement CorrectAccessUpdate(SqlUpdateStatement statement)
		{
			if (statement.SelectQuery.Select.HasModifier)
				ThrowHelper.ThrowLinqToDBException("Access does not support update query limitation");

			statement = CorrectUpdateTable(statement);

			if (!statement.SelectQuery.OrderBy.IsEmpty)
				statement.SelectQuery.OrderBy.Items.Clear();

			return statement;
		}

		public override bool ConvertCountSubQuery(SelectQuery subQuery)
		{
			return !subQuery.Where.IsEmpty;
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate, ConvertVisitor<RunOptimizationContext> visitor)
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
							new SqlFunction(typeof(int), "InStr",
								ast.One,
								predicate.Expr1,
								predicate.Expr2,
								ast.Zero),
							ast.One);

						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						var indexExpr = ast.Add<int>(
							ast.Subtract<int>(
								ast.Length(predicate.Expr1), 
								ast.Length(predicate.Expr2)),
							ast.One);

						subStrPredicate = ast.Equal(
							new SqlFunction(typeof(int), "InStr",
								indexExpr,
								predicate.Expr1,
								predicate.Expr2,
								ast.Zero),
							indexExpr);

						break;
					}
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						subStrPredicate = ast.GreaterEqual(
							new SqlFunction(typeof(int), "InStr",
								ast.One,
								predicate.Expr1,
								predicate.Expr2,
								ast.Zero),
							ast.One);

						break;
					}

				}

				if (subStrPredicate != null)
				{
					return predicate.IsNot
						? ast.Or(like, ast.Not(subStrPredicate))
						: ast.And(like, subStrPredicate);
				}
			}

			return like;
		}

		protected override ISqlExpression ConvertFunction(ISqlExpression expr)
		{
			if (expr is not SqlFunction func) return expr;

			switch (func.Name)
			{
				case PseudoFunctions.TO_LOWER: return new SqlFunction(func.SystemType, "LCase", func.IsAggregate, func.IsPure, func.Precedence, func.Parameters);
				case PseudoFunctions.TO_UPPER: return new SqlFunction(func.SystemType, "UCase", func.IsAggregate, func.IsPure, func.Precedence, func.Parameters);
				case "Length"                : return new SqlFunction(func.SystemType, "LEN",   func.IsAggregate, func.IsPure, func.Precedence, func.Parameters);
			}
			
			return base.ConvertFunction(func);
		}
	}
}
