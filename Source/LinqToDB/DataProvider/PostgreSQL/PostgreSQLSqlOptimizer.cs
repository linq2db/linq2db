using System;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Extensions;
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	class PostgreSQLSqlOptimizer : BasicSqlOptimizer
	{
		public PostgreSQLSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override bool CanCompareSearchConditions => true;

		public override SqlStatement Finalize(MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions)
		{
			CheckAliases(statement, int.MaxValue);

			return base.Finalize(mappingSchema, statement, dataOptions);
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions)
		{
			return statement.QueryType switch
			{
				QueryType.Delete => GetAlternativeDelete             ((SqlDeleteStatement)statement, dataOptions),
				QueryType.Update => GetAlternativeUpdatePostgreSqlite((SqlUpdateStatement)statement, dataOptions),
				_                => statement,
			};
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate, ConvertVisitor<RunOptimizationContext> visitor)
		{
			var searchPredicate = ConvertSearchStringPredicateViaLike(predicate, visitor);

			if (false == predicate.CaseSensitive.EvaluateBoolExpression(visitor.Context.OptimizationContext.Context) && searchPredicate is SqlPredicate.Like likePredicate)
			{
				searchPredicate = new SqlPredicate.Like(likePredicate.Expr1, likePredicate.IsNot, likePredicate.Expr2, likePredicate.Escape, "ILIKE");
			}

			return searchPredicate;
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expression, ConvertVisitor<RunOptimizationContext> visitor)
		{
			expression = base.ConvertExpressionImpl(expression, visitor);

			if (expression is SqlBinaryExpression be)
			{
				switch (be.Operation)
				{
					case "^": return new SqlBinaryExpression(be.SystemType, be.Expr1, "#", be.Expr2);
					case "+": return be.SystemType == typeof(string) ? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence) : expression;
				}
			}
			else if (expression is SqlFunction func)
			{
				switch (func.Name)
				{
					case "Convert"   :
						if (func.SystemType.ToUnderlying() == typeof(bool))
						{
							var ex = AlternativeConvertToBoolean(func, visitor.Context.DataOptions, 1);
							if (ex != null)
								return ex;
						}

						// Another cast syntax
						//
						// rreturn new SqlExpression(func.SystemType, "{0}::{1}", Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);
						return new SqlExpression(func.SystemType, "Cast({0} as {1})", Precedence.Primary, FloorBeforeConvert(func), func.Parameters[0]);

					case "CharIndex" :
						return func.Parameters.Length == 2
							? new SqlExpression(func.SystemType, "Position({0} in {1})", Precedence.Primary,
								func.Parameters[0], func.Parameters[1])
							: Add<int>(
								new SqlExpression(func.SystemType, "Position({0} in {1})", Precedence.Primary,
									func.Parameters[0],
									ConvertExpressionImpl(
										new SqlFunction(typeof(string), "Substring",
										func.Parameters[1],
										func.Parameters[2],
										Sub<int>(
											ConvertExpressionImpl(
													new SqlFunction(typeof(int), "Length", func.Parameters[1]), visitor), func.Parameters[2])),
										visitor)),
								Sub(func.Parameters[2], 1));
				}
			}

			return expression;
		}

	}
}
