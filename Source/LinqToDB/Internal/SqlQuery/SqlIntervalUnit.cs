namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Unit an interval operation is expressed in.
	/// </summary>
	/// <remarks>
	/// This is a unit, not a type: it says what an amount is counted in, and says nothing about whether the interval
	/// is a fixed duration or a calendar interval - that is <see cref="SqlIntervalDomain"/>. The calendar units at
	/// the end of the list are only valid within <see cref="SqlIntervalDomain.Calendar"/>.
	/// <para>
	/// Members are ordered from the finest unit to the coarsest, but no arithmetic meaning is attached to the
	/// underlying values - do not compare them to decide which unit is finer.
	/// </para>
	/// </remarks>
	public enum SqlIntervalUnit
	{
		/// <summary>
		/// Nanoseconds.
		/// </summary>
		Nanosecond,

		/// <summary>
		/// 100-nanosecond units, matching <see cref="System.TimeSpan.Ticks"/>.
		/// </summary>
		Tick,

		/// <summary>
		/// Microseconds.
		/// </summary>
		Microsecond,

		/// <summary>
		/// Milliseconds.
		/// </summary>
		Millisecond,

		/// <summary>
		/// Seconds.
		/// </summary>
		Second,

		/// <summary>
		/// Minutes.
		/// </summary>
		Minute,

		/// <summary>
		/// Hours.
		/// </summary>
		Hour,

		/// <summary>
		/// Days. Exactly 24 hours within <see cref="SqlIntervalDomain.Duration"/>.
		/// </summary>
		Day,

		/// <summary>
		/// Weeks. Exactly 7 days within <see cref="SqlIntervalDomain.Duration"/>.
		/// </summary>
		Week,

		/// <summary>
		/// Calendar months. Valid only within <see cref="SqlIntervalDomain.Calendar"/>.
		/// </summary>
		Month,

		/// <summary>
		/// Calendar quarters. Valid only within <see cref="SqlIntervalDomain.Calendar"/>.
		/// </summary>
		Quarter,

		/// <summary>
		/// Calendar years. Valid only within <see cref="SqlIntervalDomain.Calendar"/>.
		/// </summary>
		Year,
	}
}
