namespace LinqToDB
{
	/// <summary>
	/// NULL handling for the selector and column-name <see cref="LinqExtensions"/> <c>Unpivot</c> overloads that take
	/// it; the multi-value overloads keep every row.
	/// </summary>
	public enum UnpivotNulls
	{
		/// <summary>Rows whose value cell is NULL are excluded (the SQL <c>UNPIVOT</c> default).</summary>
		ExcludeNulls = 0,

		/// <summary>Rows whose value cell is NULL are kept (the SQL <c>INCLUDE NULLS</c> semantics).</summary>
		IncludeNulls = 1,
	}
}
