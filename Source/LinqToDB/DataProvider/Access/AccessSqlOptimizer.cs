using LinqToDB.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.Access
{
	using SqlProvider;
	using SqlQuery;

	class AccessSqlOptimizer : BasicSqlOptimizer
	{
		public AccessSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

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
			throw new LinqException("Access does not support `Replace` function which is required for such query.");
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
				throw new LinqToDBException("Access does not support update query limitation");

			statement = CorrectUpdateTable(statement);

			if (!statement.SelectQuery.OrderBy.IsEmpty)
				statement.SelectQuery.OrderBy.Items.Clear();

			return statement;
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate, ConvertVisitor<RunOptimizationContext> visitor)
		{
			var like = ConvertSearchStringPredicateViaLike(predicate, visitor);

			if (predicate.CaseSensitive.EvaluateBoolExpression(visitor.Context.OptimizationContext.Context) == true)
			{
				SqlPredicate.ExprExpr? subStrPredicate = null;

				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.StartsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(int), "InStr",
									new SqlValue(1),
									predicate.Expr1,
									predicate.Expr2,
									new SqlValue(0)),
								SqlPredicate.Operator.Equal,
								new SqlValue(1), null);

						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						var indexExpr = new SqlBinaryExpression(typeof(int),
							new SqlBinaryExpression(typeof(int),
								new SqlFunction(typeof(int), "Length", predicate.Expr1), "-",
								new SqlFunction(typeof(int), "Length", predicate.Expr2)), "+",
							new SqlValue(1));

						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(int), "InStr",
									indexExpr,
									predicate.Expr1,
									predicate.Expr2,
									new SqlValue(0)),
								SqlPredicate.Operator.Equal,
								indexExpr, null);

						break;
					}
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(typeof(int), "InStr",
									new SqlValue(1),
									predicate.Expr1,
									predicate.Expr2,
									new SqlValue(0)),
								SqlPredicate.Operator.GreaterOrEqual,
								new SqlValue(1), null);
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

		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
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
