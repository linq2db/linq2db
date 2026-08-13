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

		/// <summary>
		/// <c>//</c>, DuckDB's integer division - its <c>/</c> produces a double even between two integers.
		/// </summary>
		protected override ISqlExpression TruncateDivide(ISqlExpression value, long divisor)
		{
			var longType = Factory.GetDbDataType(typeof(long));

			return Factory.Expression(longType, Precedence.Multiplicative, "{0} // {1}", value, Factory.Value(longType, divisor));
		}

		/// <inheritdoc />
		public override bool CanLowerIntervalDifference => true;

		/// <summary>
		/// Elapsed ticks from <c>date_diff</c> at microsecond resolution.
		/// </summary>
		/// <remarks>
		/// A DuckDB timestamp holds microseconds, so the count is exact and scaling it to ticks - ten of them to
		/// the microsecond - loses nothing. Taken through the count rather than DuckDB's own <c>INTERVAL</c>,
		/// because an interval carries months and days alongside the microseconds and only the count is a single
		/// number.
		/// </remarks>
		protected override ISqlExpression? ElapsedTicks(SqlIntervalDifferenceExpression element)
		{
			var longType     = Factory.GetDbDataType(typeof(long));
			var microseconds = Factory.Function(longType, "Date_Diff",
				Factory.Value(Factory.GetDbDataType(typeof(string)), "microsecond"), element.Start, element.End);

			return Factory.Multiply(longType, microseconds, TimeSpan.TicksPerMillisecond / 1000);
		}

		/// <summary>
		/// Shifts by an interval built from the microsecond count, which is what DuckDB stores.
		/// </summary>
		protected override ISqlExpression? LowerTemporalArithmetic(SqlTemporalArithmeticExpression element)
		{
			var longType = Factory.GetDbDataType(typeof(long));

			// Through the truncating divide, for the reason TruncateDivide states above: DuckDB's / produces a
			// double even between two integers, and To_Microseconds takes a BIGINT - so a tick count that is not a
			// whole number of microseconds would hand a fraction to an integer parameter.
			var microseconds = TruncateDivide(element.Interval, TimeSpan.TicksPerMillisecond / 1000);
			var interval     = Factory.Function(longType.WithDataType(DataType.Interval), "To_Microseconds", microseconds);
			var type         = Factory.GetDbDataType(element.Temporal);

			// The date is cast rather than passed through: a parameter reaches DuckDB as an untyped literal, and
			// it will not choose between the overloads of + for a string and an interval.
			var temporal = Factory.Cast(element.Temporal, type.WithDataType(DataType.DateTime2), true);

			return element.IsSubtract
				? Factory.Sub(type, temporal, interval)
				: Factory.Add(type, temporal, interval);
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
