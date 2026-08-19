namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Well-known annotation names carried on <see cref="CteClause.Annotations"/>.
	/// Cross-provider keys are flat. Provider-specific keys, when they appear, belong in nested classes.
	/// </summary>
	public static class CteAnnotationNames
	{
		/// <summary>
		/// Boolean. When <see langword="true"/> emits <c>AS MATERIALIZED</c>,
		/// when <see langword="false"/> emits <c>AS NOT MATERIALIZED</c>.
		/// Currently recognized by PostgreSQL 12+, SQLite 3.35+, and ClickHouse 26.3+.
		/// </summary>
		/// <remarks>
		/// Expected value type is <see cref="bool"/>. Annotation values participate in both
		/// query-cache keying (via <see cref="CteClause.GetElementHashCode"/> and the
		/// <see cref="Linq.IExpressionCacheKey"/>-marked <see cref="Linq.Builder.CteAnnotationsContainer"/>)
		/// and LinqService round-trips, so values must hash stably and serialize via the
		/// standard <c>SerializationConverter</c>. Non-primitive values are not supported today.
		/// </remarks>
		public const string Materialized = "cte::materialized";
	}
}
