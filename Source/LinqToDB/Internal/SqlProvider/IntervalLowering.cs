using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.SqlProvider
{
	/// <summary>
	/// Default lowering of interval nodes onto integral storage: the strategy used when a provider has no native
	/// interval type, or has one but the column is not mapped to it.
	/// </summary>
	/// <remarks>
	/// This is one strategy among several - native <c>INTERVAL</c>, <c>TIME</c>, OLE Automation dates - so it lives
	/// apart from the visitor that selects it. Every method returns <see langword="null"/> when the result cannot be
	/// produced exactly, which leaves the node untranslated rather than approximated.
	/// <para>
	/// Truncating division is taken as a parameter rather than emitted here, because the SQL for it varies by
	/// provider - see <c>SqlExpressionConvertVisitor.TruncateDivide</c>.
	/// </para>
	/// </remarks>
	public static class IntervalLowering
	{
		/// <summary>
		/// Lowers a component or total of an interval into ordinary arithmetic.
		/// </summary>
		/// <param name="factory">Expression factory.</param>
		/// <param name="element">Part to lower.</param>
		/// <param name="truncateDivide">Integer division truncating toward zero, as the provider spells it.</param>
		/// <param name="truncateRemainder">Remainder of that same division.</param>
		public static ISqlExpression? LowerPart(ISqlExpressionFactory factory, SqlIntervalPartExpression element, Func<ISqlExpression, long, ISqlExpression> truncateDivide, Func<ISqlExpression, long, ISqlExpression> truncateRemainder)
		{
			if (element.Interval is not SqlIntervalExpression interval || interval.IntervalType.Domain != SqlIntervalDomain.Duration)
				return null;

			var ticks = ToTicks(factory, interval);

			return ticks == null ? null : FromTicks(factory, ticks, element, truncateDivide, truncateRemainder);
		}

		/// <summary>
		/// The same member arithmetic as <see cref="LowerPart"/>, over a tick count the caller already has.
		/// </summary>
		/// <remarks>
		/// Where a provider can produce elapsed ticks directly, this answers every member from that one value,
		/// which is both shorter and closer to what .NET does than counting each unit separately. It is the second
		/// choice all the same: counting cannot overflow, and a fine-grained difference over a long range can.
		/// </remarks>
		public static ISqlExpression? FromTicks(ISqlExpressionFactory factory, ISqlExpression ticks, SqlIntervalPartExpression element, Func<ISqlExpression, long, ISqlExpression> truncateDivide, Func<ISqlExpression, long, ISqlExpression> truncateRemainder)
		{
			return element.Kind == SqlIntervalPartKind.Total
				? Total(factory, ticks, element.Unit, element.Type)
				: Component(factory, ticks, element.Unit, element.Within, element.Type, truncateDivide, truncateRemainder);
		}

		/// <summary>
		/// Elapsed whole units between two date/time values, reproducing <c>_ticks / TicksPerUnit</c> without ever
		/// materialising the tick count.
		/// </summary>
		/// <param name="factory">Expression factory.</param>
		/// <param name="difference">The difference whose elapsed units are wanted.</param>
		/// <param name="unit">Unit to count in.</param>
		/// <param name="countBoundaries">Provider's <c>DATEDIFF</c> - boundary counting.</param>
		/// <param name="shiftDate">Provider's <c>DATEADD</c> - must be exact.</param>
		/// <remarks>
		/// The provider's own difference counts crossed boundaries, so 10:59:30 to 11:01:00 reports two minutes
		/// where only one and a half elapsed. Shifting the start by that count and comparing against the end says
		/// whether it overshot, and the correction is at most one unit either way.
		/// <para>
		/// Counting whole units cannot overflow the way a fine-grained difference over the same range does - that
		/// limit is what caps <c>DATEDIFF(millisecond, ...)</c> at about 24 days.
		/// </para>
		/// </remarks>
		public static ISqlExpression? ElapsedUnits(
			ISqlExpressionFactory                                                     factory,
			SqlIntervalDifferenceExpression                                           difference,
			SqlIntervalUnit                                                           unit,
			Func<SqlIntervalUnit, ISqlExpression, ISqlExpression, ISqlExpression?>     countBoundaries,
			Func<SqlIntervalUnit, ISqlExpression, ISqlExpression, ISqlExpression?>     shiftDate)
		{
			var count = countBoundaries(unit, difference.Start, difference.End);
			if (count == null)
				return null;

			var anchor = shiftDate(unit, count, difference.Start);
			if (anchor == null)
				return null;

			var longType = factory.GetDbDataType(typeof(long));
			var one      = factory.Value(longType, 1L);

			// Forwards and the anchor landed past the end: one unit too many. Backwards and it landed before the
			// end: one too few. The two cases are separate because the sign decides which way "too far" points.
			var overshotForward = factory.SearchCondition()
				.Add(factory.GreaterOrEqual(difference.End, difference.Start))
				.Add(factory.Greater(anchor, difference.End));

			var overshotBackward = factory.SearchCondition()
				.Add(factory.Less(difference.End, difference.Start))
				.Add(factory.Less(anchor, difference.End));

			return factory.Condition(overshotForward, factory.Sub(longType, count, one),
				factory.Condition(overshotBackward, factory.Add(longType, count, one), count));
		}

		/// <summary>
		/// The whole interval expressed in <paramref name="unit"/>, fraction included - <c>TotalHours</c> and
		/// friends.
		/// </summary>
		/// <remarks>
		/// Built as whole units plus the leftover, never as one division of a tick count: a century in ticks
		/// exceeds <see cref="long"/>, so the undivided form is not representable at all. The leftover is measured
		/// between the anchor and the end, a window shorter than one unit, so the fine count is always small.
		/// </remarks>
		public static ISqlExpression? ElapsedTotal(
			ISqlExpressionFactory                                                 factory,
			SqlIntervalDifferenceExpression                                       difference,
			SqlIntervalUnit                                                       unit,
			ISqlExpression                                                        wholeUnits,
			SqlIntervalUnit?                                                      finestUnit,
			Func<SqlIntervalUnit, ISqlExpression, ISqlExpression, ISqlExpression?> countBoundaries,
			Func<SqlIntervalUnit, ISqlExpression, ISqlExpression, ISqlExpression?> shiftDate)
		{
			if (finestUnit == null)
				return null;

			if (!SqlIntervalUnits.TryGetTicksRatio(unit, out var ticksPerUnit, out var unitDenominator) || unitDenominator != 1)
				return null;

			// The fine unit may be finer than a tick - a nanosecond is a hundredth of one - so its ratio is taken
			// whole rather than requiring a denominator of one.
			if (!SqlIntervalUnits.TryGetTicksRatio(finestUnit.Value, out var ticksPerFine, out var fineDenominator))
				return null;

			var anchor = shiftDate(unit, wholeUnits, difference.Start);
			if (anchor == null)
				return null;

			var leftover = countBoundaries(finestUnit.Value, anchor, difference.End);
			if (leftover == null)
				return null;

			var doubleType = factory.GetDbDataType(typeof(double));
			var finePerUnit = (double)ticksPerUnit * fineDenominator / ticksPerFine;

			return factory.Add(doubleType,
				factory.Cast(wholeUnits, doubleType, true),
				factory.Div(doubleType, factory.Cast(leftover, doubleType, true), factory.Value(doubleType, finePerUnit)));
		}

		/// <summary>
		/// Wraps a whole-unit count into its CLR component, mirroring <c>TimeSpan</c> term for term:
		/// <c>Hours</c> is <c>(_ticks / TicksPerHour) % 24</c>, and this is the <c>% 24</c>.
		/// </summary>
		/// <remarks>
		/// <c>Days</c> does not wrap - a duration may legitimately exceed a year, and .NET applies no modulo to it.
		/// </remarks>
		public static ISqlExpression WrapComponent(
			ISqlExpressionFactory                     factory,
			ISqlExpression                            wholeUnits,
			SqlIntervalUnit                           unit,
			SqlIntervalUnit?                          within,
			Func<ISqlExpression, long, ISqlExpression> truncateRemainder)
		{
			// The modulus is the enclosing unit expressed in this one, so it comes from the ratio table rather
			// than from a switch of its own. A component with nothing enclosing it does not wrap.
			if (!TryGetWrap(unit, within, out var enclosingWrap))
				return wholeUnits;

			return truncateRemainder(wholeUnits, enclosingWrap);
		}

		/// <summary>
		/// How many <paramref name="unit"/> fit in <paramref name="within"/>, when the component wraps at all.
		/// </summary>
		static bool TryGetWrap(SqlIntervalUnit unit, SqlIntervalUnit? within, out long wrap)
		{
			wrap = 0;

			if (within is not { } enclosing)
				return false;

			if (!SqlIntervalUnits.TryGetTicksRatio(unit, out var unitTicks, out var unitDivisor))
				return false;

			if (!SqlIntervalUnits.TryGetTicksRatio(enclosing, out var enclosingTicks, out var enclosingDivisor))
				return false;

			// (enclosingTicks / enclosingDivisor) / (unitTicks / unitDivisor), which stays integral for every pair
			// the model allows - each unit divides the next evenly.
			var numerator   = enclosingTicks * unitDivisor;
			var denominator = unitTicks * enclosingDivisor;

			if (denominator == 0 || numerator % denominator != 0)
				return false;

			wrap = numerator / denominator;

			return wrap > 1;
		}

		/// <summary>
		/// Converts an interval's stored amount to ticks, exactly.
		/// </summary>
		public static ISqlExpression? ToTicks(ISqlExpressionFactory factory, SqlIntervalExpression interval)
		{
			if (!SqlIntervalUnits.TryGetTicksRatio(interval.IntervalType.Resolution, out var numerator, out var denominator))
				return null;

			// A sub-tick storage unit would need a division whose truncation rule differs between providers for
			// negative values. Rather than guess, leave it untranslated.
			if (denominator != 1)
				return null;

			var longType = factory.GetDbDataType(typeof(long));
			var ticks    = factory.Cast(interval.Value, longType, true);

			return numerator == 1 ? ticks : factory.Multiply(longType, ticks, numerator);
		}

		static ISqlExpression? Total(ISqlExpressionFactory factory, ISqlExpression ticks, SqlIntervalUnit unit, DbDataType resultType)
		{
			if (!SqlIntervalUnits.TryGetTicksRatio(unit, out var numerator, out var denominator))
				return null;

			var doubleType = factory.GetDbDataType(typeof(double));
			var value      = factory.Cast(ticks, doubleType, true);

			if (denominator != 1)
				value = factory.Multiply(doubleType, value, (double)denominator);

			var total = numerator == 1 ? value : factory.Div(doubleType, value, factory.Value(doubleType, (double)numerator));

			return factory.Cast(total, resultType);
		}

		static ISqlExpression? Component(ISqlExpressionFactory factory, ISqlExpression ticks, SqlIntervalUnit unit, SqlIntervalUnit? within, DbDataType resultType, Func<ISqlExpression, long, ISqlExpression> truncateDivide, Func<ISqlExpression, long, ISqlExpression> truncateRemainder)
		{
			var longType = factory.GetDbDataType(typeof(long));

			if (!SqlIntervalUnits.TryGetTicksRatio(unit, out var ticksPerUnit, out var divisor))
				return null;

			// A sub-tick unit cannot be divided out of a tick count - it is the scaled remainder instead. Two
			// different factors meet here and must not be confused: the modulus is how many ticks the enclosing
			// unit holds, while the scale is how many of this unit fit in a tick. TimeSpan.Nanoseconds is
			// (ticks % 10) * 100 - ten ticks to a microsecond, a hundred nanoseconds to a tick.
			if (divisor != 1)
			{
				if (ticksPerUnit != 1 || within is not { } subTickEnclosing)
					return null;

				if (!SqlIntervalUnits.TryGetTicksRatio(subTickEnclosing, out var ticksPerEnclosing, out var enclosingDivisor) || enclosingDivisor != 1)
					return null;

				return factory.Cast(factory.Multiply(longType, truncateRemainder(ticks, ticksPerEnclosing), divisor), resultType);
			}

			var whole = truncateDivide(ticks, ticksPerUnit);

			// The enclosing unit is stated by the node, so there is no table here - and no assumption that it is
			// the next coarser one, which is what makes microseconds-within-a-second expressible alongside
			// microseconds-within-a-millisecond.
			return factory.Cast(
				TryGetWrap(unit, within, out var wrap) ? truncateRemainder(whole, wrap) : whole,
				resultType);
		}
	}
}
