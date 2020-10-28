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


		protected static string[] AccessLikeCharactersToEscape = {"_", "?", "*", "%", "#", "-", "!"};

		public override bool   LikeIsEscapeSupported => false;


		public override ISqlPredicate ConvertLikePredicate(MappingSchema mappingSchema, SqlPredicate.Like predicate,
			EvaluationContext context)
		{
			if (predicate.Escape != null)
			{
				return new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, predicate.Expr2, null, predicate.IsSqlLike);
			}

			return base.ConvertLikePredicate(mappingSchema, predicate, context);
		}

		public override ISqlExpression EscapeLikeCharacters(ISqlExpression expression, ref ISqlExpression? escape)
		{
			throw new LinqException("Access does not supports `Replace` function which is required for such query.");
		}

		public override SqlStatement TransformStatementMutable(SqlStatement statement)
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

		public override bool ConvertCountSubQuery(SelectQuery subQuery)
		{
			return !subQuery.Where.IsEmpty;
		}
	}
}
