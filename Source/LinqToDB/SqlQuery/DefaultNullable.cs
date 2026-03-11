namespace LinqToDB.SqlQuery
{
	// TODO: review: why we even expose this to public API?
	/// <summary>
	/// Specify how <c>[NOT] NULL</c> should be emitted for table columns in CREATE TABLE statement.
	/// </summary>
	public enum DefaultNullable
	{
		/// <summary>
		/// No defaults available, both <c>NULL</c> and <c>NOT NULL</c> should be generated always.
		/// </summary>
		None,
		/// <summary>
		/// Generate <c>NOT NULL</c> only, <c>NULL</c> is default behavior.
		/// </summary>
		Null,
		/// <summary>
		/// Generate <c>NULL</c> only, <c>NOT NULL</c> is default behavior.
		/// </summary>
		NotNull,
	}
}
