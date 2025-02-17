namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Oracle's INSERT ALL/FIRST query type.
	/// </summary>
	public enum MultiInsertType
	{
		/// <summary>
		/// INSERT ALL operation without WHEN conditions.
		/// </summary>
		Unconditional,
		/// <summary>
		/// INSERT ALL operation with WHEN conditions.
		/// </summary>
		All,
		/// <summary>
		/// INSERT FIRST operation.
		/// </summary>
		First,
	}
}
