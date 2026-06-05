namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Describes how a provider places <c>NULL</c> values in an <c>ORDER BY</c> by default (i.e. when no
	/// <c>NULLS FIRST</c>/<c>NULLS LAST</c> is specified). Used to elide a requested <see cref="Sql.NullsPosition"/>
	/// when it already matches the provider's natural placement for the item's sort direction.
	/// </summary>
	public enum NullsDefaultOrdering
	{
		/// <summary>
		/// The natural placement is unknown — a requested <see cref="Sql.NullsPosition"/> is always honored and never elided.
		/// </summary>
		Unknown,

		/// <summary>
		/// <c>NULL</c> sorts as the smallest value: ascending ⇒ <c>NULLS FIRST</c>, descending ⇒ <c>NULLS LAST</c>
		/// (e.g. SQL Server, MySQL, SQLite, Firebird 2.0+).
		/// </summary>
		Smallest,

		/// <summary>
		/// <c>NULL</c> sorts as the largest value: ascending ⇒ <c>NULLS LAST</c>, descending ⇒ <c>NULLS FIRST</c>
		/// (e.g. Oracle, PostgreSQL, DB2).
		/// </summary>
		Largest,

		/// <summary>
		/// <c>NULL</c> always sorts first, regardless of sort direction.
		/// </summary>
		AlwaysFirst,

		/// <summary>
		/// <c>NULL</c> always sorts last, regardless of sort direction (e.g. ClickHouse).
		/// </summary>
		AlwaysLast,
	}
}
