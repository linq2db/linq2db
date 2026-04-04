using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	public class DuckDBSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public DuckDBSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override bool SupportsNullInColumn => false;

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
				case "^": return new SqlExpression(element.Type, "xor({0}, {1})", Precedence.Primary, element.Expr1, element.Expr2);
				case "+" when element.SystemType == typeof(string): return new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence);

				// DuckDB performs float division by default (5/2 = 2.5)
				// Use integer division operator // for integer types
				case "/" when element.SystemType != null && element.SystemType.IsIntegerType:
					return new SqlBinaryExpression(element.SystemType!, element.Expr1, "//", element.Expr2, element.Precedence);

			}

			return base.ConvertSqlBinaryExpression(element);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			return func switch
			{
				{
					Name: "CharIndex",
					Parameters: [var p0, var p1],
					Type: var type,
				} => new SqlExpression(type, "Position({0} in {1})", Precedence.Primary, p0, p1),

				{
					Name: "CharIndex",
					Parameters: [var p0, var p1, var p2],
					Type: var type,
				} => Add<int>(
						new SqlExpression(
							type,
							"Position({0} in {1})",
							Precedence.Primary,
							p0,
							(ISqlExpression)Visit(
								new SqlFunction(MappingSchema.GetDbDataType(typeof(string)), "Substring",
									p1,
									p2,
									Sub<int>(
										(ISqlExpression)Visit(
											Factory.Length(p1)),
										p2))
							)
						),
						Sub(p2, 1)
					),

				// DuckDB lpad/rpad require VARCHAR first argument
				{
					Name: "Lpad" or "Rpad",
					Parameters: [var str, ..],
				} when str.SystemType != typeof(string) =>
					base.ConvertSqlFunction(new SqlFunction(func.Type, func.Name, false, true,
						new SqlCastExpression(str, new DbDataType(typeof(string), DataType.VarChar), null, false), func.Parameters[1], func.Parameters[2])),

				_ => base.ConvertSqlFunction(func),
			};
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			if (cast.SystemType.ToUnderlying() == typeof(bool))
			{
				if (cast.IsMandatory && cast.Expression.SystemType?.UnwrapNullableType() == typeof(bool))
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
