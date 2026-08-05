namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Semantic family an interval belongs to.
	/// </summary>
	/// <remarks>
	/// The two families are not interchangeable and must never be mixed by an optimization. A calendar month added
	/// to a date depends on the date it is added to, and a calendar day can be 23 or 25 hours long across a daylight
	/// saving transition, while a fixed duration of one day is always exactly 24 hours.
	/// </remarks>
	public enum SqlIntervalDomain
	{
		/// <summary>
		/// A fixed-length duration, with CLR <see cref="System.TimeSpan"/> semantics.
		/// </summary>
		Duration,

		/// <summary>
		/// A calendar-relative interval, whose elapsed length depends on the point in time it is applied to.
		/// Corresponds to SQL <c>INTERVAL YEAR TO MONTH</c> and to the month component of a PostgreSQL interval.
		/// </summary>
		Calendar,
	}
}
