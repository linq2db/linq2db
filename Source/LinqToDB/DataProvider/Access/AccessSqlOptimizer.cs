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
			statement = CorrectInnerJoins(statement);

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

		SqlStatement CorrectInnerJoins(SqlStatement statement)
		{
			statement.Visit(static e =>
			{
				if (e.ElementType == QueryElementType.SqlQuery)
				{
					var sqlQuery = (SelectQuery)e;

					for (var tIndex = 0; tIndex < sqlQuery.From.Tables.Count; tIndex++)
					{
						var t = sqlQuery.From.Tables[tIndex];
						for (int i = 0; i < t.Joins.Count; i++)
						{
							var join = t.Joins[i];
							if (join.JoinType == JoinType.Inner)
							{
								bool moveUp = false;

								if (join.Table.Joins.Count > 0 && join.Table.Joins[0].JoinType == JoinType.Inner)
								{
									// INNER JOIN Table1 t1
									//		INNER JOIN Table2 t2 ON ...
									// ON t1.Field = t2.Field
									//

									var usedSources = new HashSet<ISqlTableSource>();
									QueryHelper.GetUsedSources(join.Condition, usedSources);

									if (usedSources.Contains(join.Table.Joins[0].Table.Source))
									{
										moveUp = true;
									}
								}
								else
								{
									// Check for join with unbounded condition
									//
									// INNER JOIN Table1 t1 ON other.Field = 1
									//

									var usedSources = new HashSet<ISqlTableSource>();
									QueryHelper.GetUsedSources(join.Condition, usedSources);

									moveUp = usedSources.Count < 2;
								}

								if (moveUp)
								{
									// Convert to old style JOIN
									//
									sqlQuery.From.Tables.Insert(tIndex + 1, join.Table);
									sqlQuery.From.Where.ConcatSearchCondition(join.Condition);

									t.Joins.RemoveAt(i);
									--i;
								}
							}
						}
					}
				}

			});

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
				case PseudoFunctions.TO_LOWER: return func.WithName("LCase");
				case PseudoFunctions.TO_UPPER: return func.WithName("UCase");
				case "Length"                : return func.WithName("LEN");
			}
			return base.ConvertFunction(func);
		}
	}
}
