namespace LinqToDB.Mapping
{
	/// <summary>
	/// Unit in which a <see cref="System.TimeSpan"/> duration is stored in a database column.
	/// </summary>
	/// <remarks>
	/// Only fixed-length units are listed. Calendar units such as month or year are deliberately absent:
	/// a <see cref="System.TimeSpan"/> is a fixed-length duration and cannot represent them, because their
	/// length depends on the point in time they are applied to.
	/// <para>
	/// Members are ordered from the finest unit to the coarsest, but no arithmetic meaning is attached to
	/// the underlying values - do not compare them to decide which unit is finer.
	/// </para>
	/// </remarks>
	public enum DurationUnit
	{
		/// <summary>
		/// Nanoseconds. Note that <see cref="System.TimeSpan"/> resolution is 100 nanoseconds, so stored
		/// values are always multiples of 100 and reading a finer value cannot be represented exactly.
		/// </summary>
		Nanosecond,

		/// <summary>
		/// 100-nanosecond units, matching <see cref="System.TimeSpan.Ticks"/>. This is the only unit that can
		/// represent every <see cref="System.TimeSpan"/> value exactly.
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
		/// Days, always exactly 24 hours.
		/// </summary>
		Day,
	}
}
