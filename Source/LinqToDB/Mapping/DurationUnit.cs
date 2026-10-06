namespace LinqToDB.Mapping
{
	/// <summary>
	/// Unit in which a <see cref="System.TimeSpan"/> duration is stored in a database column.
	/// </summary>
	/// <remarks>
	/// Calendar units such as month or year cannot be listed: a <see cref="System.TimeSpan"/> is a fixed-length
	/// duration and their length depends on the point in time they are applied to. The week is fixed-length and
	/// absent for a different reason - a duration is not usually stored counted in weeks - so it is left out until
	/// something asks for it rather than ruled out. Adding a unit is additive, and the lowering already knows the
	/// ratio.
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
		/// <remarks>
		/// The only unit finer than a tick, and the one the server cannot compute with. A value stored in it round
		/// trips, but reaching a tick count from it is a division, and no lowering performs one - integer division
		/// rounds a negative value differently from one provider to the next, so a chosen rule would answer
		/// differently depending on the database. In consequence a member asked for in SQL is refused rather than
		/// answered, and so is a comparison or a membership test against a column declared in any other unit, since
		/// the two have no unit they can meet in. A plain projection is unaffected: the column is read and the
		/// member computed in .NET, which answers exactly.
		/// </remarks>
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
