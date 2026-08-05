using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	public class DuckDBSqlExpressionConvertVisitor(bool allowModify) : SqlExpressionConvertVisitor(allowModify)
	{
		protected override bool SupportsNullInColumn             => false;
		protected override bool ConcatRequiresExplicitStringCast => false;

		private protected override ISqlExpression ConvertNativeDurationToTicks(ISqlExpression expression, DbDataType longType)
		{
			// DuckDB stores intervals as independent month, day and microsecond components. Its own
			// comparison contract treats a month as 30 days; use the same rule when a native interval
			// is explicitly mapped as a TimeSpan duration. Keep every term integral so large values do
			// not pass through epoch(interval)'s DOUBLE result and lose microseconds.
			const string months = "(date_part('year', {0}) * 12 + date_part('month', {0}))";
			const string days   = $"(({months}) * 30 + date_part('day', {{0}}))";
			const string ticks  = $"(({days}) * 864000000000 + date_part('hour', {{0}}) * 36000000000 + date_part('minute', {{0}}) * 600000000 + date_part('microsecond', {{0}}) * 10)";
			return Factory.Expression(longType, ticks, expression);
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var searchPredicate = ConvertSearchStringPredicateViaLike(predicate);

			if (predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) == false
				&& searchPredicate is SqlPredicate.Like likePredicate)
			{
				return new SqlPredicate.Like(likePredicate.Expr1, likePredicate.IsNot, likePredicate.Expr2, likePredicate.Escape, "ILIKE");
			}

			return searchPredicate;
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			return element.Operation switch
			{
				"^" => new SqlExpression(element.Type, "xor({0}, {1})", Precedence.Primary, element.Expr1, element.Expr2),

				// DuckDB performs float division by default (5/2 = 2.5), use integer division operator // for integer types
				"/" when element.SystemType.IsIntegerType =>
					new SqlBinaryExpression(element.SystemType, element.Expr1, "//", element.Expr2, element.Precedence),

				_ => base.ConvertSqlBinaryExpression(element),
			};
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			return func switch
			{
				{
					Name      : "CharIndex",
					Parameters: [var p0, var p1],
					Type      : var type,
				} => new SqlExpression(type, "Position({0} in {1})", Precedence.Primary, p0, p1),

				{
					Name      : "CharIndex",
					Parameters: [var p0, var p1, var p2],
					Type      : var type,
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
