using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.MySql
{
	public class MySqlSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public MySqlSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override bool ConcatRequiresExplicitStringCast => false;

		/// <summary>
		/// <c>DIV</c>, because MySQL's <c>/</c> is a decimal division even between two integers.
		/// </summary>
		protected override ISqlExpression TruncateDivide(ISqlExpression value, long divisor)
		{
			var longType = Factory.GetDbDataType(typeof(long));

			return Factory.Expression(longType, Precedence.Multiplicative, "{0} DIV {1}", value, Factory.Value(longType, divisor));
		}

		/// <inheritdoc />
		public override bool CanLowerIntervalDifference => true;

		/// <summary>
		/// Elapsed ticks from <c>TIMESTAMPDIFF</c> at microsecond resolution.
		/// </summary>
		/// <remarks>
		/// A microsecond is the finest unit MySQL stores, so the count is exact and scaling it to ticks - ten of
		/// them to the microsecond - loses nothing. The result is a <c>BIGINT</c> of microseconds, which runs out
		/// far beyond any range <see cref="TimeSpan"/> can hold.
		/// </remarks>
		protected override ISqlExpression? ElapsedTicks(SqlIntervalDifferenceExpression element)
		{
			var longType     = Factory.GetDbDataType(typeof(long));
			var microseconds = Factory.Function(longType, "TimestampDiff", Factory.Fragment("Microsecond"), element.Start, element.End);

			return Factory.Multiply(longType, microseconds, TimeSpan.TicksPerMillisecond / 1000);
		}

		/// <inheritdoc />
		/// <remarks>Lowered below without going through <c>FinestDateUnit</c>, which the default reads.</remarks>
		public override bool CanLowerIntervalShift => true;

		/// <summary>
		/// Shifts through <c>DATE_ADD</c> at microsecond resolution - the interval's own unit here.
		/// </summary>
		/// <remarks>
		/// The amount is an expression rather than a literal, and MySQL takes it as written, so the whole tick
		/// count converts in one step with no decomposition into coarser units.
		/// </remarks>
		protected override ISqlExpression? LowerTemporalArithmetic(SqlTemporalArithmeticExpression element)
		{
			var longType = Factory.GetDbDataType(typeof(long));

			// Through the truncating divide, for the reason TruncateDivide states above: MySQL's / is a decimal
			// division even between two integers, so a tick count that is not a whole number of microseconds would
			// reach INTERVAL as a fraction rather than truncating toward zero the way the rest of the lowering does.
			var microseconds = TruncateDivide(element.Interval, TimeSpan.TicksPerMillisecond / 1000);

			return Factory.Function(Factory.GetDbDataType(element.Temporal),
				element.IsSubtract ? "Date_Sub" : "Date_Add",
				element.Temporal,
				Factory.Expression(longType.WithDataType(DataType.Interval), "Interval {0} Microsecond", microseconds));
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			cast = FloorBeforeConvert(cast);

			var castType = cast.SystemType.ToUnderlying();

			if ((castType == typeof(double) || castType == typeof(float)) && cast.Expression.SystemType == typeof(decimal))
				return cast.Expression;

			return base.ConvertConversion(cast);
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			return element switch
			{
				// | has lower priority than & in MySQL...
				SqlBinaryExpression(var type, var ex1, "|", var ex2) when element.Precedence == Precedence.Bitwise
					=> new SqlBinaryExpression(type, ex1, "|", ex2, Precedence.Bitwise - 1),
				_ => base.ConvertSqlBinaryExpression(element),
			};
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var caseSensitive = predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext);

			if (caseSensitive is null or false)
			{
				var searchExpr = predicate.Expr2;
				var dataExpr   = predicate.Expr1;

#pragma warning disable CA1508 // https://github.com/dotnet/roslyn-analyzers/issues/6868
				if (caseSensitive == false)
#pragma warning restore CA1508
				{
					searchExpr = PseudoFunctions.MakeToLower(searchExpr, MappingSchema);
					dataExpr   = PseudoFunctions.MakeToLower(dataExpr, MappingSchema);
				}

				ISqlPredicate? newPredicate = null;
				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						newPredicate = new SqlPredicate.ExprExpr(
							new SqlFunction(MappingSchema.GetDbDataType(typeof(int)), "LOCATE", searchExpr, dataExpr), SqlPredicate.Operator.Greater,
							new SqlValue(0), null);
						break;
					}
				}

				if (newPredicate != null)
				{
					newPredicate = newPredicate.MakeNot(predicate.IsNot);

					return newPredicate;
				}

#pragma warning disable CA1508 // https://github.com/dotnet/roslyn-analyzers/issues/6868
				if (caseSensitive == false)
#pragma warning restore CA1508
				{
					predicate = new SqlPredicate.SearchString(
						dataExpr,
						predicate.IsNot,
						searchExpr,
						predicate.Kind,
						new SqlValue(false));
				}
			}
			else
			{
				predicate = new SqlPredicate.SearchString(
					new SqlExpression(MappingSchema.GetDbDataType(typeof(string)), "{0} COLLATE utf8_bin", Precedence.Primary, predicate.Expr1),
					predicate.IsNot,
					predicate.Expr2,
					predicate.Kind,
					new SqlValue(false));
			}

			return ConvertSearchStringPredicateViaLike(predicate);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			return func switch
			{
				{ Name: PseudoFunctions.LENGTH } => func.WithName("CHAR_LENGTH"),
				_ => base.ConvertSqlFunction(func),
			};
		}

		protected override ISqlExpression WrapColumnExpression(ISqlExpression expr)
		{
			if (expr is SqlValue
				{
					Value: decimal or uint or ulong or long or double,
				} value)
			{
				expr = new SqlCastExpression(expr, value.ValueType, null, isMandatory: true);
			}
			else if (expr is SqlParameter param)
			{
				var paramType = param.Type.SystemType.UnwrapNullableType();
				if (paramType == typeof(uint)
					|| paramType == typeof(ulong)
					|| paramType == typeof(long)
					|| paramType == typeof(double)
					|| paramType == typeof(decimal))
					expr = new SqlCastExpression(expr, param.Type, null, isMandatory: true);
			}

			return base.WrapColumnExpression(expr);
		}
	}
}
