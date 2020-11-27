namespace LinqToDB.DataProvider.PostgreSQL
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	class PostgreSQLSqlOptimizer : BasicSqlOptimizer
	{
		public PostgreSQLSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement Finalize(SqlStatement statement)
		{
			CheckAliases(statement, int.MaxValue);

			return base.Finalize(statement);
		}

		public override SqlStatement TransformStatementMutable(SqlStatement statement)
		{
			return statement.QueryType switch
			{
				QueryType.Delete => GetAlternativeDelete((SqlDeleteStatement)statement),
				QueryType.Update => GetAlternativeUpdateFrom((SqlUpdateStatement)statement),
				_                => statement,
			};
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expr, EvaluationContext context)
		{
			expr = base.ConvertExpressionImpl(expr, context);

			if (expr is SqlBinaryExpression be)
			{
				switch (be.Operation)
				{
					case "^": return new SqlBinaryExpression(be.SystemType, be.Expr1, "#", be.Expr2);
					case "+": return be.SystemType == typeof(string) ? new SqlBinaryExpression(be.SystemType, be.Expr1, "||", be.Expr2, be.Precedence) : expr;
				}
			}
			else if (expr is SqlFunction func)
			{
				switch (func.Name)
				{
					case "Convert"   :
						if (func.SystemType.ToUnderlying() == typeof(bool))
						{
							var ex = AlternativeConvertToBoolean(func, 1);
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
									ConvertExpressionImpl(new SqlFunction(typeof(string), "Substring",
										func.Parameters[1],
										func.Parameters[2],
										Sub<int>(
											ConvertExpressionImpl(
												new SqlFunction(typeof(int), "Length", func.Parameters[1]), context), func.Parameters[2])), context)),
								Sub(func.Parameters[2], 1));
				}
			}

			return expr;
		}

	}
}
