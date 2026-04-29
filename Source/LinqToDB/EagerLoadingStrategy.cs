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
		/// KeyedQuery strategy: same preamble model as <see cref="Default"/>, but the parent-side of the
		/// preamble join projects only the key columns (SELECT DISTINCT key FROM parent) instead of the
		/// full entity. Useful when parent entities have many/wide columns.
		/// </summary>
		KeyedQuery,

		/// <summary>
		/// CteUnion strategy: when two or more child associations at the same level use
		/// <see cref="LinqExtensions.AsUnionQuery{T}(System.Linq.IQueryable{T})"/>,
		/// their preamble queries are combined into a single UNION ALL query with a wide carrier tuple.
		/// This reduces round-trips (e.g., two children: 3 → 2 SELECTs). Column slots are reused across
		/// branches when the nullable CLR type matches. Remaps to <see cref="KeyedQuery"/> when the current
		/// provider does not support CTEs; if the CteUnion batch cannot be formed (e.g., only one child
		/// association is present or the carrier exceeds <c>MaxColumnCount</c>) the whole strategy falls
		/// back through <see cref="KeyedQuery"/> and finally <see cref="Default"/>.
		/// </summary>
		CteUnion,
	}
}
