namespace LinqToDB.DataProvider.PostgreSQL
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;

	public class PostgreSQLSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public PostgreSQLSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override bool SupportsNullInColumn       => false;

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var searchPredicate = ConvertSearchStringPredicateViaLike(predicate);

			if (false == predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) && searchPredicate is SqlPredicate.Like likePredicate)
			{
				searchPredicate = new SqlPredicate.Like(likePredicate.Expr1, likePredicate.IsNot, likePredicate.Expr2, likePredicate.Escape, "ILIKE");
			}

			return searchPredicate;
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "^": return new SqlBinaryExpression(element.SystemType, element.Expr1, "#", element.Expr2);
				case "+": return element.SystemType == typeof(string) ? new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) : element;
				case "%":
				{
					// PostgreSQL '%' operator supports only decimal and numeric types

					var fromType = QueryHelper.GetDbDataType(element.Expr1, MappingSchema);
					if (fromType.SystemType.ToNullableUnderlying() != typeof(decimal))
					{
						var toType          = MappingSchema.GetDbDataType(typeof(decimal));
						var newExpr1        = PseudoFunctions.MakeCast(element.Expr1, toType);
						var systemType      = typeof(decimal);
						if (fromType.SystemType.IsNullable())
							systemType = systemType.AsNullable();

						var newExpr =  PseudoFunctions.MakeMandatoryCast(new SqlBinaryExpression(systemType, newExpr1, element.Operation, element.Expr2), toType);
						return Visit(Optimize(newExpr));
					}
					break;
				}
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case "CharIndex" :
				{
					return func.Parameters.Length == 2
						? new SqlExpression(func.SystemType, "Position({0} in {1})", Precedence.Primary,
							func.Parameters[0], func.Parameters[1])
						: Add<int>(
							new SqlExpression(func.SystemType, "Position({0} in {1})", Precedence.Primary,
								func.Parameters[0],
								(ISqlExpression)Visit(
									new SqlFunction(typeof(string), "Substring",
										func.Parameters[1],
										func.Parameters[2],
										Sub<int>(
											(ISqlExpression)Visit(
												new SqlFunction(typeof(int), "Length", func.Parameters[1])),
											func.Parameters[2]))
								)),
							Sub(func.Parameters[2], 1));
				}
			}

			return base.ConvertSqlFunction(func);
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			if (cast.SystemType.ToUnderlying() == typeof(bool))
			{
				if (cast.Expression is not SqlSearchCondition and not SqlCaseExpression)
				{
					return ConvertBooleanToCase(cast.Expression, cast.ToType);
				}
			}
			cast = FloorBeforeConvert(cast);
			return base.ConvertConversion(cast);
		}
	}
}
