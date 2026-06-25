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
		/// KeyedQuery strategy: the main query results are buffered, distinct parent keys are extracted
		/// client-side, and child records are loaded in a single batch query using <c>WHERE key IN (...)</c>
		/// or a <c>VALUES</c> table join. Useful when parent entities have many/wide columns, since the
		/// child preamble carries only keys rather than the full parent entity.
		/// </summary>
		KeyedQuery,

		/// <summary>
		/// CteUnion strategy: when two or more child associations at the same level use
		/// <see cref="LinqExtensions.WithUnionLoadStrategy{T}(System.Linq.IQueryable{T})"/>,
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
