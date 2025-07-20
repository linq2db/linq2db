using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.PostgreSQL
{
	sealed class PostgreSQLSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
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
					Type: var type,
				}:
					return new SqlExpression(type, "Position({0} in {1})", Precedence.Primary, p0, p1);

				case {
					Name: "CharIndex",
					Parameters: [var p0, var p1, var p2],
					Type: var type,
				}:
					return Add<int>(
						new SqlExpression(type, "Position({0} in {1})", Precedence.Primary,
							p0,
							(ISqlExpression)Visit(
								new SqlFunction(MappingSchema.GetDbDataType(typeof(string)), "Substring",
									p1,
									p2,
									Sub<int>(
										(ISqlExpression)Visit(
											Factory.Length(p1)),
										p2))
							)),
						Sub(p2, 1));

				default:
					return base.ConvertSqlFunction(func);
			};
		}

		// TODO: remove and use DataType check when we implement DbType parsing to DbDataType
		internal static bool IsJson(DbDataType type, out bool isJsonB)
		{
			isJsonB = type.DataType == DataType.BinaryJson
				|| type.DbType?.Equals("jsonb", StringComparison.OrdinalIgnoreCase) == true;

			return isJsonB
				|| type.DataType is DataType.Json
				|| type.DbType?.Equals("json", StringComparison.OrdinalIgnoreCase) == true;
		}

		protected override IQueryElement VisitExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			if (predicate.Operator is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual)
			{
				// conversions with at least one type being json or jsonb should be done using jsonb type
				var left  = QueryHelper.GetDbDataType(predicate.Expr1, MappingSchema);
				var right = QueryHelper.GetDbDataType(predicate.Expr2, MappingSchema);

				// | is correct, we need to run both
				if ((IsJson(left, out var leftJsonB) | IsJson(right, out var rightJsonB)) && !(leftJsonB && rightJsonB))
				{
					var expr1 = leftJsonB
						? predicate.Expr1
						: new SqlCastExpression(predicate.Expr1, new DbDataType(predicate.Expr1.SystemType ?? typeof(object), DataType.BinaryJson), null, isMandatory: true);
					var expr2 = rightJsonB
						? predicate.Expr2
						: new SqlCastExpression(predicate.Expr2, new DbDataType(predicate.Expr2.SystemType ?? typeof(object), DataType.BinaryJson), null, isMandatory: true);

					predicate = new SqlPredicate.ExprExpr(expr1, predicate.Operator, expr2, predicate.UnknownAsValue);
				}
			}

			return base.VisitExprExprPredicate(predicate);
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			if (cast.SystemType.ToUnderlying() == typeof(bool))
			{
				if (cast.IsMandatory && cast.Expression.SystemType?.ToNullableUnderlying() == typeof(bool))
				{
					// do nothing
				}
				else if (cast.Expression is not SqlSearchCondition and not SqlCaseExpression)
				{
					return ConvertBooleanToCase(cast.Expression, cast.ToType);
				}
			}

			cast = FloorBeforeConvert(cast);
			return base.ConvertConversion(cast);
		}
	}
}
