using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.PostgreSQL
{
	public class PostgreSQLSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public PostgreSQLSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override bool SupportsNullInColumn               => false;
		protected override bool ConcatRequiresExplicitStringCast => false;

		/// <summary>
		/// The elapsed time itself, as a native <c>interval</c>: PostgreSQL subtracts two date/time values into one
		/// directly, and <c>TimeSpan</c> is already mapped to it, so the value needs no decomposition on either side.
		/// </summary>
		protected override ISqlExpression? LowerIntervalDifference(SqlIntervalDifferenceExpression element)
		{
			return Elapsed(element);
		}

		/// <summary>
		/// A timestamp takes an interval directly, so the shift is the operator itself.
		/// </summary>
		/// <remarks>
		/// A difference lowers to a native <c>interval</c> here, so it is added as it stands. Every other interval
		/// arrives as a tick count, which PostgreSQL reads as an ordinary number and refuses to add to a date - so
		/// it is turned into an interval first.
		/// </remarks>
		protected override ISqlExpression? LowerTemporalArithmetic(SqlTemporalArithmeticExpression element)
		{
			var type = Factory.GetDbDataType(element.Temporal);

			var interval = QueryHelper.UnwrapNullablity(element.Interval) is SqlIntervalDifferenceExpression
				? element.Interval
				: IntervalFromTicks(element.Interval);

			return element.IsSubtract
				? Factory.Sub(type, element.Temporal, interval)
				: Factory.Add(type, element.Temporal, interval);
		}

		/// <summary>
		/// A tick count as a native interval, counted in microseconds.
		/// </summary>
		/// <remarks>
		/// The microsecond is what a timestamp stores, so nothing is lost that the value could have kept: dividing
		/// the ticks by ten drops only the digit PostgreSQL would have dropped on its way into the column.
		/// </remarks>
		ISqlExpression IntervalFromTicks(ISqlExpression ticks)
		{
			var longType     = Factory.GetDbDataType(typeof(long));
			var intervalType = longType.WithDataType(DataType.Interval);

			return Factory.Multiply(intervalType,
				Factory.Div(longType, ticks, Factory.Value(longType, TimeSpan.TicksPerMillisecond / 1000)),
				Factory.NotNullExpression(intervalType, "Interval '1 microsecond'"));
		}

		/// <inheritdoc />
		public override bool CanLowerIntervalDifference => true;

		/// <summary>
		/// Ticks summed field by field out of the interval, rather than from its epoch.
		/// </summary>
		/// <remarks>
		/// <c>EXTRACT(EPOCH ...)</c> is the obvious form and the wrong one: it returns a double before PostgreSQL
		/// 14, whose spacing grows past a tick over a range of decades. Every other field is bounded - seconds stay
		/// below sixty - so a double carries them exactly however far apart the two timestamps are, and the day
		/// count is whole. A timestamp difference never carries months, so days are the coarsest field there is.
		/// <para>
		/// Fields of a negative interval are all negative, so the sum needs no sign handling.
		/// </para>
		/// </remarks>
		protected override ISqlExpression? ElapsedTicks(SqlIntervalDifferenceExpression element)
		{
			var elapsed    = Elapsed(element);
			var longType   = Factory.GetDbDataType(typeof(long));
			var doubleType = Factory.GetDbDataType(typeof(double));

			// PostgreSQL stores microseconds, so scaling the seconds field to ticks lands on a whole number.
			var seconds = Factory.Cast(
				Factory.Function(doubleType, "Round",
					Factory.Multiply(doubleType, Extract("second", elapsed, doubleType), (double)TimeSpan.TicksPerSecond)),
				longType, true);

			return Factory.Add(longType, WholeField(elapsed, "day", TimeSpan.TicksPerDay),
				Factory.Add(longType, WholeField(elapsed, "hour", TimeSpan.TicksPerHour),
					Factory.Add(longType, WholeField(elapsed, "minute", TimeSpan.TicksPerMinute), seconds)));
		}

		ISqlExpression WholeField(ISqlExpression elapsed, string part, long ticksPerUnit)
		{
			var longType = Factory.GetDbDataType(typeof(long));

			return Factory.Multiply(longType,
				Factory.Cast(Extract(part, elapsed, Factory.GetDbDataType(typeof(double))), longType, true),
				ticksPerUnit);
		}

		/// <summary>
		/// The elapsed interval, built to stand on its own wherever it is embedded.
		/// </summary>
		/// <remarks>
		/// Declared with no precedence, which the builder reads as "always parenthesise". A subtraction otherwise
		/// binds tighter than an addition, so <c>base + end - start</c> would be emitted for
		/// <c>base + (end - start)</c>: the same number, but a different grouping of types, and PostgreSQL has no
		/// operator adding one timestamp to another.
		/// </remarks>
		ISqlExpression Elapsed(SqlIntervalDifferenceExpression element)
		{
			return new SqlBinaryExpression(IntervalType, AsTimestamp(element.End), "-", AsTimestamp(element.Start), Precedence.Unknown);
		}

		DbDataType IntervalType => Factory.GetDbDataType(typeof(TimeSpan)).WithDataType(DataType.Interval);

		/// <summary>
		/// Widens a <c>date</c> operand to a <c>timestamp</c>.
		/// </summary>
		/// <remarks>
		/// <c>date - date</c> is the one subtraction PostgreSQL answers with an integer count of days rather than an
		/// interval. A date carries no time of day, so widening it is lossless and makes the operation uniform.
		/// </remarks>
		ISqlExpression AsTimestamp(ISqlExpression value)
		{
			var type = QueryHelper.GetDbDataType(value, MappingSchema);

			return type.DataType == DataType.Date
				? Factory.Cast(value, type.WithDataType(DataType.DateTime2))
				: value;
		}

		/// <summary>
		/// Subtracting two timestamps in PostgreSQL yields a real <c>interval</c>, already split into days and a
		/// time of day, so the components can be read straight out of it - no boundary counting, no anchoring.
		/// </summary>
		/// <remarks>
		/// The split is what makes this match the CLR: <c>'2026-01-03 13:30' - '2026-01-01 10:00'</c> is
		/// <c>2 days 03:30:00</c>, so <c>EXTRACT(HOUR ...)</c> is 3 - the hours within the day, exactly what
		/// <c>TimeSpan.Hours</c> means - and negatives come back as <c>-2 days -03:30:00</c>, giving -3 as the CLR
		/// does. A constructed interval is not normalised the same way, which is why this is applied only to a
		/// difference of two timestamps.
		/// <para>
		/// Totals go through <c>EPOCH</c>, which is the whole interval in seconds and needs no decomposition -
		/// PostgreSQL stores microseconds, so the double it returns carries the full stored precision. Ticks are
		/// the exception, and take the decomposition instead - see below.
		/// </para>
		/// </remarks>
		protected override ISqlExpression? LowerIntervalPart(SqlIntervalPartExpression element)
		{
			if (QueryHelper.UnwrapNullablity(element.Interval) is not SqlIntervalDifferenceExpression difference)
				return base.LowerIntervalPart(element);

			// Ticks is the one total EPOCH cannot answer, and the reason is the divisor rather than the epoch.
			// Every other unit is a whole number of seconds, but a tick is a ten-millionth of one, and no binary
			// double holds that: the nearest one prints as 9.9999999999999995E-08, and PostgreSQL reads a literal
			// carrying an exponent as an exact numeric rather than as the double it came from. So the division is
			// exactly wrong, by five parts in 10^17 - which reaches a whole tick once the span passes sixty-three
			// years, and grows from there. Handing it to the base implementation takes it to ElapsedTicks, which
			// has no such divisor.
			if (element is { Unit: SqlIntervalUnit.Tick, Kind: SqlIntervalPartKind.Total })
				return base.LowerIntervalPart(element);

			var elapsed = Elapsed(difference);

			if (element.Kind == SqlIntervalPartKind.Total)
			{
				if (!SqlIntervalUnits.TryGetTicksRatio(element.Unit, out var ticksPerUnit, out var denominator) || denominator != 1)
					return null;

				var doubleType = Factory.GetDbDataType(typeof(double));
				var seconds    = Extract("epoch", elapsed, doubleType);

				return Factory.Cast(
					Factory.Div(doubleType, seconds,
						Factory.Value(doubleType, (double)ticksPerUnit / TimeSpan.TicksPerSecond)),
					element.Type);
			}

			var part = element.Unit switch
			{
				SqlIntervalUnit.Day    => "day",
				SqlIntervalUnit.Hour   => "hour",
				SqlIntervalUnit.Minute => "minute",
				SqlIntervalUnit.Second => "second",
				_                      => null,
			};

			// EXTRACT names no field below the second, but that is a limit of this shortcut rather than of the
			// provider: the base implementation reaches the same components from ElapsedTicks, which is exact here,
			// using PostgreSQL's own integer division and remainder - both of which follow the CLR's rules.
			if (part == null)
				return base.LowerIntervalPart(element);

			// second carries the fractional part in PostgreSQL, so truncate it to match TimeSpan.Seconds.
			var extracted = Extract(part, elapsed, Factory.GetDbDataType(typeof(double)));

			return Factory.Cast(Factory.Function(Factory.GetDbDataType(typeof(long)), "Trunc", extracted), element.Type);
		}

		/// <summary>
		/// <c>Extract</c> as a function node rather than raw text. Its argument uses the standard
		/// <c>field FROM value</c> form, which is not a comma-separated argument list - the same shape the date
		/// part translation already builds.
		/// </summary>
		ISqlExpression Extract(string part, ISqlExpression value, DbDataType resultType)
		{
			return Factory.Function(resultType, "Extract", Factory.Expression(resultType, $"{part} From {{0}}", value));
		}

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
				case "%":
				{
					// PostgreSQL '%' operator supports only decimal and numeric types

					var fromType = QueryHelper.GetDbDataType(element.Expr1, MappingSchema);
					if (fromType.SystemType.UnwrapNullableType() != typeof(decimal))
					{
						var toType          = MappingSchema.GetDbDataType(typeof(decimal));
						var newExpr1        = PseudoFunctions.MakeCast(element.Expr1, toType);
						var systemType      = typeof(decimal);
						if (fromType.SystemType.IsNullableType)
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

				_ => base.ConvertSqlFunction(func),
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

		protected internal override IQueryElement VisitExprExprPredicate(SqlPredicate.ExprExpr predicate)
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

		protected override ISqlExpression WrapColumnExpression(ISqlExpression expr)
		{
			if (expr is SqlValue
				{
					Value: uint or long or ulong or float or double or decimal,
				} value)
			{
				expr = new SqlCastExpression(expr, value.ValueType, null, isMandatory: true);
			}

			if (expr is SqlParameter { IsQueryParameter: false } param)
			{
				var paramType = param.Type.SystemType.UnwrapNullableType();
				if (paramType == typeof(uint)
					|| paramType == typeof(long)
					|| paramType == typeof(ulong)
					|| paramType == typeof(float)
					|| paramType == typeof(double)
					|| paramType == typeof(decimal))
					expr = new SqlCastExpression(expr, param.Type, null, isMandatory: true);
			}

			return base.WrapColumnExpression(expr);
		}
	}
}
