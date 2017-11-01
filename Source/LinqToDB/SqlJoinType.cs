namespace LinqToDB
{
	/// <summary>
	/// Defines join type. Used with join LINQ helpers.
	/// </summary>
	public enum SqlJoinType
	{
		/// <summary>
		/// Inner join.
		/// </summary>
		Inner,
		/// <summary>
		/// Left outer join.
		/// </summary>
		Left,
		/// <summary>
		/// Right outer join.
		/// </summary>
		Right,
		/// <summary>
		/// Full outer join.
		/// </summary>
		Full
	}
}