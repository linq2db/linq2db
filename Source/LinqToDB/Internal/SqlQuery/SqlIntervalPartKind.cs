namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Which of the two different readings of an interval component is requested.
	/// </summary>
	/// <remarks>
	/// The distinction is not cosmetic: for an interval of 90 minutes, <see cref="Component"/> of
	/// <see cref="SqlIntervalUnit.Minute"/> is 30, while <see cref="Total"/> is 90. Collapsing them is the bug behind
	/// translating <c>TimeSpan.TotalHours</c> into a date-part boundary count.
	/// </remarks>
	public enum SqlIntervalPartKind
	{
		/// <summary>
		/// The component within the next coarser unit, truncated toward zero, as <c>TimeSpan.Minutes</c> does.
		/// </summary>
		Component,

		/// <summary>
		/// The whole interval expressed in the requested unit, keeping the fractional part, as
		/// <c>TimeSpan.TotalMinutes</c> does.
		/// </summary>
		Total,
	}
}
