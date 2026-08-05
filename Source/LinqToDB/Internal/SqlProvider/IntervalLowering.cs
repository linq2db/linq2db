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
		public static ISqlExpression? LowerPart(ISqlExpressionFactory factory, SqlIntervalPartExpression element, Func<ISqlExpression, long, ISqlExpression> truncateDivide)
		{
			if (element.Interval is not SqlIntervalExpression interval || interval.IntervalType.Domain != SqlIntervalDomain.Duration)
				return null;

			var ticks = ToTicks(factory, interval);
			if (ticks == null)
				return null;

			return element.Kind == SqlIntervalPartKind.Total
				? Total(factory, ticks, element.Unit, element.Type)
				: Component(factory, ticks, element.Unit, element.Type, truncateDivide);
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

		static ISqlExpression? Component(ISqlExpressionFactory factory, ISqlExpression ticks, SqlIntervalUnit unit, DbDataType resultType, Func<ISqlExpression, long, ISqlExpression> truncateDivide)
		{
			var longType = factory.GetDbDataType(typeof(long));

			// TimeSpan.Nanoseconds is the nanosecond part within the current microsecond, and a tick is 100ns, so
			// it is the scaled tick remainder - not a division of the whole tick count.
			if (unit == SqlIntervalUnit.Nanosecond)
				return factory.Cast(factory.Multiply(longType, Remainder(factory, ticks, 10, truncateDivide), 100L), resultType);

			if (!SqlIntervalUnits.TryGetTicksRatio(unit, out var ticksPerUnit, out var denominator) || denominator != 1)
				return null;

			// Days is the whole day count and does not wrap - a duration may legitimately exceed a year. Every
			// finer component wraps within the next coarser unit, as the CLR members do.
			var wrap = unit switch
			{
				SqlIntervalUnit.Day         => (long?)null,
				SqlIntervalUnit.Hour        => 24L,
				SqlIntervalUnit.Minute      => 60L,
				SqlIntervalUnit.Second      => 60L,
				SqlIntervalUnit.Millisecond => 1000L,
				SqlIntervalUnit.Microsecond => 1000L,
				_                           => null,
			};

			if (wrap == null && unit != SqlIntervalUnit.Day)
				return null;

			var whole = truncateDivide(ticks, ticksPerUnit);

			return factory.Cast(wrap == null ? whole : Remainder(factory, whole, wrap.Value, truncateDivide), resultType);
		}

		/// <summary>
		/// Remainder consistent with the supplied truncation: <c>value - trunc(value / divisor) * divisor</c>.
		/// </summary>
		static ISqlExpression Remainder(ISqlExpressionFactory factory, ISqlExpression value, long divisor, Func<ISqlExpression, long, ISqlExpression> truncateDivide)
		{
			var longType = factory.GetDbDataType(typeof(long));

			return factory.Sub(longType, value, factory.Multiply(longType, truncateDivide(value, divisor), divisor));
		}
	}
}
