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
			switch (func)
			{
				case {
					Name: "CharIndex",
					Parameters: [var p0, var p1],
					SystemType: var type,
				}:
					return new SqlExpression(type, "Position({0} in {1})", Precedence.Primary, p0, p1);

				case {
					Name: "CharIndex",
					Parameters: [var p0, var p1, var p2],
					SystemType: var type,
				}:
					return Add<int>(
						new SqlExpression(type, "Position({0} in {1})", Precedence.Primary,
							p0,
							(ISqlExpression)Visit(
								new SqlFunction(typeof(string), "Substring",
									p1,
									p2,
									Sub<int>(
										(ISqlExpression)Visit(
											new SqlFunction(typeof(int), "Length", p1)),
										p2))
							)),
						Sub(p2, 1));

				default:
					return base.ConvertSqlFunction(func);
			};
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
