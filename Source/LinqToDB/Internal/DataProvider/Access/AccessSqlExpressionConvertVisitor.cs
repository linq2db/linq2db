using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Access
{
	public class AccessSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public AccessSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		/// <summary>
		/// Access has no <c>FLOOR</c> or <c>CEILING</c>, but its <c>Fix</c> is exactly truncation toward zero -
		/// the semantic the base composes those two functions to reach.
		/// </summary>
		/// <remarks>
		/// <c>Int</c> would be wrong here: it rounds down, so <c>Int(-2.5)</c> is -3 where CLR integer division
		/// gives -2. Access division is floating, so no cast is needed before <c>Fix</c>.
		/// </remarks>
		protected override ISqlExpression TruncateDivide(ISqlExpression value, long divisor)
		{
			var longType = Factory.GetDbDataType(typeof(long));

			return Factory.Function(longType, "Fix", Factory.Div(longType, value, Factory.Value(longType, divisor)));
		}

		/// <summary>
		/// A second is as fine as Access counts - <c>DateDiff</c> has no millisecond part at all.
		/// </summary>
		/// <remarks>
		/// The whole-unit members are still exact, because the anchor correction compares actual dates rather than
		/// trusting the count. What this limits is the fraction of a <c>Total</c> and anything below a second,
		/// which an OLE Automation date - a floating day number - could not carry reliably anyway.
		/// </remarks>
		protected override SqlIntervalUnit? FinestDateUnit => SqlIntervalUnit.Second;

		/// <summary>
		/// A second is also as fine as the measurement resolves, so a component asked for below one is identically
		/// zero rather than merely imprecise.
		/// </summary>
		/// <remarks>
		/// Access refuses such a member either way - there is no millisecond date part to extract and no tick count
		/// to divide - but where it refuses decides what a caller can do about it. Declined here, while the
		/// expression is still being built, the message names the unit and the resolution, and a projection stays
		/// free to fall back to .NET, which holds both dates and answers exactly. Left to the builder, the refusal
		/// arrives as an exception with no such escape.
		/// </remarks>
		public override SqlIntervalUnit IntervalResolution => SqlIntervalUnit.Second;

		/// <summary>
		/// Access counts seconds, and an OLE Automation date holds fractions of one, so a tick count derived from
		/// it is only good to the second - which is why the members are counted instead.
		/// </summary>
		protected override bool ElapsedTicksResolveMembers => false;

		/// <inheritdoc />
		public override bool CanLowerIntervalPart => true;

		/// <summary>
		/// No tick count from Access at all.
		/// </summary>
		/// <remarks>
		/// <c>DateDiff</c> hands back a 32-bit count, and scaling seconds to ticks overflows it after about three
		/// and a half minutes - the driver answers <c>Numeric value out of range</c>. There is no wider integer to
		/// reach for, so the interval never becomes a value here and its member translator says so.
		/// </remarks>
		protected override ISqlExpression? ElapsedTicks(SqlIntervalDifferenceExpression element)
		{
			return null;
		}

		/// <summary>
		/// No shift by an interval either, for the reason <see cref="ElapsedTicks"/> gives.
		/// </summary>
		/// <remarks>
		/// The amount reaches a shift as a tick count whatever it was built from, and that is the one number Access
		/// cannot hold - scaling to ticks overflows its arithmetic and the driver answers <c>Numeric value out of
		/// range</c>. Refusing by name is the whole of the difference between this and a date that comes back wrong.
		/// <para>
		/// Access shifts dates perfectly well in seconds; what it cannot do is take delivery of the amount in ticks.
		/// Should a coarser hand-off ever exist, this is the override to drop.
		/// </para>
		/// </remarks>
		/// <inheritdoc />
		/// <remarks>
		/// Declared false although <see cref="FinestDateUnit"/> is named: the base implementation would spend the
		/// amount through <c>DateAdd</c>, and this provider refuses the shift outright below.
		/// </remarks>
		public override bool CanLowerIntervalShift => false;

		protected override ISqlExpression? LowerTemporalArithmetic(SqlTemporalArithmeticExpression element)
		{
			return null;
		}

		const string DateDiffFunction = "DateDiff";
		const string DateAddFunction  = "DateAdd";
		const string ToDoubleFunction = "CDbl";

		const int SecondsPerDay = 24 * 60 * 60;

		static string? DatePartName(SqlIntervalUnit unit)
		{
			return unit switch
			{
				SqlIntervalUnit.Day    => "d",
				SqlIntervalUnit.Hour   => "h",
				SqlIntervalUnit.Minute => "n",
				SqlIntervalUnit.Second => "s",
				_                      => null,
			};
		}

		protected override ISqlExpression? ShiftDate(SqlIntervalUnit unit, ISqlExpression amount, ISqlExpression date)
		{
			var part = DatePartName(unit);

			return part == null
				? null
				: Factory.Function(Factory.GetDbDataType(date), DateAddFunction, Factory.Value(part), amount, date);
		}

		/// <summary>
		/// Boundary counting through <c>DateDiff</c>, whose count is a 32-bit value - so a count in seconds is taken
		/// across the sub-day remainder rather than across the whole span.
		/// </summary>
		/// <remarks>
		/// A second count reaches the 32-bit limit after about sixty-eight years, which is inside the range a person's
		/// age can reach. Past it Access answers <c>Numeric value out of range</c> instead of a number, and it does so
		/// before any cast here can widen the result - the overflow happens while Access computes the count. Counting
		/// days first and seconds only from there keeps both counts small: days stay 32-bit for any date Access can
		/// hold, and the remainder spans at most a day. Every coarser datepart is counted directly, being far from the
		/// limit - minutes reach it only after four thousand years.
		/// <para>
		/// The two counts telescope, so the split is exact rather than an approximation. <c>DateDiff</c> truncates both
		/// operands to the unit and subtracts, which makes it additive through any intermediate point, and shifting by
		/// whole days leaves the time of day alone - so the day count contributes exactly its own seconds. That holds
		/// even when the day count overshoots the end, as it does between an evening and the following morning: the
		/// remainder comes back negative by the same amount.
		/// </para>
		/// <para>
		/// <c>CDbl</c> is what keeps the product from overflowing in turn - Access multiplies in 32-bit integers and a
		/// century of days is past that once scaled to seconds. A cast to a wider integer would not do: Access has none
		/// to name, and a cast to a floating type renders as nothing here.
		/// </para>
		/// <para>
		/// This provider is the only one that gets here - it is the only override of
		/// <see cref="SqlExpressionConvertVisitor.ElapsedTicksResolveMembers"/> to <see langword="false"/>, and
		/// everywhere else a member is taken from the tick count instead.
		/// </para>
		/// </remarks>
		protected override ISqlExpression? CountDateBoundaries(SqlIntervalUnit unit, ISqlExpression start, ISqlExpression end)
		{
			var part = DatePartName(unit);

			if (part == null)
				return null;

			var intType  = Factory.GetDbDataType(typeof(int));
			var longType = Factory.GetDbDataType(typeof(long));

			if (unit != SqlIntervalUnit.Second)
			{
				return Factory.Cast(
					Factory.Function(intType, DateDiffFunction, Factory.Value(part), start, end),
					longType, true);
			}

			var days   = Factory.Function(intType, DateDiffFunction, Factory.Value("d"), start, end);
			var anchor = Factory.Function(Factory.GetDbDataType(start), DateAddFunction, Factory.Value("d"), days, start);

			var remainder  = Factory.Function(intType, DateDiffFunction, Factory.Value(part), anchor, end);
			var doubleType = Factory.GetDbDataType(typeof(double));

			return Factory.Add(doubleType,
				Factory.Multiply(doubleType, Factory.Function(doubleType, ToDoubleFunction, days), SecondsPerDay),
				remainder);
		}

		static readonly string[] AccessLikeCharactersToEscape = {"_", "?", "*", "%", "#", "-", "!"};

		public override bool LikeIsEscapeSupported => false;

		public override string[] LikeCharactersToEscape => AccessLikeCharactersToEscape;

		protected override bool SupportsNullIf => false;

		public override ISqlPredicate ConvertLikePredicate(SqlPredicate.Like predicate)
		{
			if (predicate.Escape != null)
			{
				return new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, predicate.Expr2, null);
			}

			return base.ConvertLikePredicate(predicate);
		}

		protected override string EscapeLikePattern(string str)
		{
			var newStr = DataTools.EscapeUnterminatedBracket(str);
			if (string.Equals(newStr, str, StringComparison.Ordinal))
				newStr = newStr.Replace("[", "[[]", StringComparison.Ordinal);

			foreach (var s in LikeCharactersToEscape)
				newStr = newStr.Replace(s, "[" + s + "]", StringComparison.Ordinal);

			return newStr;
		}

		public override ISqlExpression EscapeLikeCharacters(ISqlExpression expression, ref ISqlExpression? escape)
		{
			// TODO: implement for ACE engine, as it has REPLACE
			throw new LinqToDBException("Access does not support `Replace` function which is required for such query.");
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var like   = ConvertSearchStringPredicateViaLike(predicate);
			var result = like;

			if (predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) == true)
			{
				SqlPredicate.ExprExpr? subStrPredicate = null;

				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.StartsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(MappingSchema.GetDbDataType(typeof(int)),
									"InStr",
									new SqlValue(1),
									predicate.Expr1,
									predicate.Expr2,
									new SqlValue(0)),
								SqlPredicate.Operator.Equal,
								new SqlValue(1), null);

						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						var indexExpr = new SqlBinaryExpression(
							typeof(int),
							new SqlBinaryExpression(
								typeof(int),
								Factory.Length(predicate.Expr1),
								"-",
								Factory.Length(predicate.Expr2)
							),
							"+",
							new SqlValue(1));

						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(MappingSchema.GetDbDataType(typeof(int)),
									"InStr",
									indexExpr,
									predicate.Expr1,
									predicate.Expr2,
									new SqlValue(0)),
								SqlPredicate.Operator.Equal,
								indexExpr, null);

						break;
					}
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(MappingSchema.GetDbDataType(typeof(int)),
									"InStr",
									new SqlValue(1),
									predicate.Expr1,
									predicate.Expr2,
									new SqlValue(0)),
								SqlPredicate.Operator.GreaterOrEqual,
								new SqlValue(1), null);
						break;
					}

				}

				if (subStrPredicate != null)
				{
					result = new SqlSearchCondition(predicate.IsNot, canBeUnknown: null, like, subStrPredicate.MakeNot(predicate.IsNot));
				}
			}

			return result;
		}

		public override ISqlExpression ConvertCoalesce(SqlCoalesceExpression element)
		{
			if (element.SystemType == null)
				return element;

			// Strip NULL-literal operands before folding to IIF, matching base ConvertCoalesce —
			// otherwise a no-op guard like Coalesce(x, NULL) folds to IIF(x IS NULL, NULL, x)
			// (issue #5531).
			var reduced = RemoveNullValues(element);
			if (reduced is not SqlCoalesceExpression coalesce)
				return reduced;

			element = coalesce;

			if (element.Expressions.Length == 2)
			{
				return new SqlConditionExpression(new SqlPredicate.IsNull(element.Expressions[0], false), element.Expressions[1], element.Expressions[0]);
			}

			if (element.Expressions.Length > 2)
			{
				return new SqlConditionExpression(new SqlPredicate.IsNull(element.Expressions[0], false), new SqlCoalesceExpression(GetSubArray(element.Expressions)), element.Expressions[0]);
			}

			static ISqlExpression[] GetSubArray(ISqlExpression[] array)
			{
				var parms = new ISqlExpression[array.Length - 1];
				Array.Copy(array, 1, parms, 0, parms.Length);
				return parms;
			}

			return element;
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			return func switch
			{
				{ Name: PseudoFunctions.TO_LOWER } => func.WithName("LCase"),
				{ Name: PseudoFunctions.TO_UPPER } => func.WithName("UCase"),
				{ Name: PseudoFunctions.LENGTH } => func.WithName("Len"),

				{
					Name: "CharIndex",
					Parameters: [var p0, var p1],
					Type: var type,
				} => new SqlFunction(type, "InStr", new SqlValue(1), p1, p0, new SqlValue(1)),

				{
					Name: "CharIndex",
					Parameters: [var p0, var p1, var p2],
					Type: var type,
				} => new SqlFunction(type, "InStr", p2, p1, p0, new SqlValue(1)),

				_ => base.ConvertSqlFunction(func),
			};
		}

		public override ISqlExpression ConvertSqlUnaryExpression(SqlUnaryExpression element)
		{
			if (element.Operation is SqlUnaryOperation.BitwiseNegation)
				return new SqlBinaryExpression(element.Type, new SqlValue(-1), "-", element.Expr);

			return base.ConvertSqlUnaryExpression(element);
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			var expression = cast.Expression;
			var funcName   = string.Empty;

			switch (cast.SystemType.ToUnderlying().TypeCode)
			{
				case TypeCode.String   : funcName = "CStr";  break;
				case TypeCode.Boolean  : funcName = "CBool"; break;
				case TypeCode.DateTime :
					if (IsDateDataType(cast.ToType, "Date"))
						funcName = "DateValue";
					else if (IsTimeDataType(cast.ToType))
						funcName = "TimeValue";
					else
						funcName = "CDate";
					break;

				default:
					if (cast.SystemType == typeof(DateTime))
						goto case TypeCode.DateTime;

					return expression;
			}

			if (!string.IsNullOrEmpty(funcName))
			{
				var isNotNull = new SqlPredicate.IsNull(expression, true);
				var funcCall = new SqlFunction(cast.Type, funcName, parametersNullability: ParametersNullabilityType.NotNullable, canBeNull: false, expression);
				return new SqlConditionExpression(isNotNull, funcCall, new SqlValue(cast.Type, null));
			}

			return expression;
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			return element switch
			{
				SqlBinaryExpression(var type, var ex1, "%", var ex2) => new SqlBinaryExpression(type, ex1, "MOD", ex2, Precedence.Additive - 1),
				SqlBinaryExpression(var type, var ex1, "&", var ex2) => new SqlBinaryExpression(type, ex1, "BAND", ex2, Precedence.Bitwise),
				SqlBinaryExpression(var type, var ex1, "|", var ex2) => new SqlBinaryExpression(type, ex1, "BOR", ex2, Precedence.Bitwise - 1),
				_ => base.ConvertSqlBinaryExpression(element),
			};
		}
	}
}
