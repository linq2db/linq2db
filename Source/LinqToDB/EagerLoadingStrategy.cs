namespace LinqToDB
{
	/// <summary>
	/// Specifies the strategy used to execute eager loading (LoadWith/ThenLoad) preamble queries.
	/// </summary>
	public enum EagerLoadingStrategy
	{
		/// <summary>
		/// Default strategy: issues a pre-query using SELECT DISTINCT on the full parent entity joined to
		/// child rows via SelectMany. This is the existing behaviour.
		/// </summary>
		Default,

		/// <summary>
		/// PostQuery strategy: same preamble model as <see cref="Default"/>, but the parent-side of the
		/// preamble join projects only the key columns (SELECT DISTINCT key FROM parent) instead of the
		/// full entity. Useful when parent entities have many/wide columns.
		/// </summary>
		PostQuery,

		/// <summary>
		/// CteUnion strategy: combines all eager-load preambles for a single query level into one
		/// CTE + UNION ALL query. Requires the underlying database to support Common Table Expressions.
		/// Falls back to <see cref="PostQuery"/> when CTEs are not supported.
		/// </summary>
		CteUnion,
	}
}
