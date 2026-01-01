namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Type of JOIN operation.
	/// </summary>
	public enum JoinType
	{
		/// <summary>
		/// Don't use this type.
		/// It defines intermediate join type calculated by SelectManyBuilder and shouldn't be used outside of this class.
		/// </summary>
		Auto,
		/// <summary>
		/// INNER JOIN.
		/// Also represents CROSS JOIN when join conditions not set.
		/// </summary>
		Inner,
		/// <summary>
		/// LEFT (OUTER) JOIN.
		/// </summary>
		Left,
		/// <summary>
		/// CROSS JOIN.
		/// </summary>
		Cross,
		/// <summary>
		/// CROSS APPLY.
		/// </summary>
		CrossApply,
		/// <summary>
		/// OUTER APPLY.
		/// </summary>
		OuterApply,
		/// <summary>
		/// RIGHT (OUTER) JOIN.
		/// </summary>
		Right,
		/// <summary>
		/// FULL (OUTER) JOIN.
		/// </summary>
		Full,
		/// <summary>
		/// Intermediate fake JOIN type, added by SelectManyBuilder and replaced with real type in query optimizer.
		/// </summary>
		RightApply,
		/// <summary>
		/// Intermediate fake JOIN type, added by SelectManyBuilder and replaced with real type in query optimizer.
		/// </summary>
		FullApply,
	}
}
