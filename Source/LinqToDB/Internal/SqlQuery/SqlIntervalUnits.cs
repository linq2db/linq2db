using System;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Exact conversions between interval units and <see cref="TimeSpan"/> ticks.
	/// </summary>
	/// <remarks>
	/// The ratio is kept as a pair of integers rather than a single factor because nanoseconds are finer than a
	/// tick - one tick is 100 nanoseconds - so no integral "ticks per unit" exists for them. Keeping it rational
	/// lets every conversion stay in exact integer arithmetic; a floating factor would quietly lose the low digits
	/// of a large tick count.
	/// </remarks>
	public static class SqlIntervalUnits
	{
		/// <summary>
		/// Gets the exact ratio converting an amount in <paramref name="unit"/> to ticks:
		/// <c>ticks = amount * numerator / denominator</c>.
		/// </summary>
		/// <param name="unit">Unit to convert from.</param>
		/// <param name="numerator">Ratio numerator.</param>
		/// <param name="denominator">Ratio denominator, always positive.</param>
		/// <returns>
		/// <see langword="false"/> for calendar units, whose elapsed length depends on the point in time they are
		/// applied to and therefore has no fixed tick count. Callers must reject rather than approximate those.
		/// </returns>
		public static bool TryGetTicksRatio(SqlIntervalUnit unit, out long numerator, out long denominator)
		{
			denominator = 1;

			switch (unit)
			{
				case SqlIntervalUnit.Nanosecond:  numerator = 1; denominator = 100;   return true;
				case SqlIntervalUnit.Tick:        numerator = 1;                      return true;
				case SqlIntervalUnit.Microsecond: numerator = TimeSpan.TicksPerMillisecond / 1000; return true;
				case SqlIntervalUnit.Millisecond: numerator = TimeSpan.TicksPerMillisecond; return true;
				case SqlIntervalUnit.Second:      numerator = TimeSpan.TicksPerSecond; return true;
				case SqlIntervalUnit.Minute:      numerator = TimeSpan.TicksPerMinute; return true;
				case SqlIntervalUnit.Hour:        numerator = TimeSpan.TicksPerHour;   return true;
				case SqlIntervalUnit.Day:         numerator = TimeSpan.TicksPerDay;    return true;
				case SqlIntervalUnit.Week:        numerator = TimeSpan.TicksPerDay * 7; return true;

				// Month, Quarter, Year - calendar units, no fixed tick count.
				default:                          numerator = 0;                      return false;
			}
		}

		/// <summary>
		/// Converts an amount expressed in <paramref name="unit"/> to ticks, exactly.
		/// </summary>
		/// <param name="amount">Amount in <paramref name="unit"/> units.</param>
		/// <param name="unit">Unit <paramref name="amount"/> is expressed in.</param>
		/// <param name="ticks">Resulting tick count.</param>
		/// <returns>
		/// <see langword="false"/> when the unit is a calendar unit, or when the result overflows
		/// <see cref="long"/>. A silent wrap here would be a wrong duration, so overflow is a failure, not a value.
		/// </returns>
		public static bool TryToTicks(long amount, SqlIntervalUnit unit, out long ticks)
		{
			ticks = 0;

			if (!TryGetTicksRatio(unit, out var numerator, out var denominator))
				return false;

			try
			{
				// Integer division truncates toward zero, which is what a sub-tick amount must do: 99ns and -99ns
				// both become 0 ticks, -101ns becomes -1.
				ticks = checked(amount * numerator / denominator);
			}
			catch (OverflowException)
			{
				return false;
			}

			return true;
		}
	}
}
