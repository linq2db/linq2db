namespace LinqToDB.Schema
{
	/// <summary>
	/// Kind of callable object.
	/// </summary>
	public enum CallableKind
	{
		/// <summary>
		/// Scalar function.
		/// </summary>
		ScalarFunction,
		/// <summary>
		/// Aggregate function.
		/// </summary>
		AggregateFunction,
		/// <summary>
		/// Table function.
		/// </summary>
		TableFunction,
		/// <summary>
		/// Stored procedure.
		/// </summary>
		StoredProcedure,
	}
}
