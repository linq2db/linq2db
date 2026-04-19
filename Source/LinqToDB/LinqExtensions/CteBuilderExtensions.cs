using System;

using JetBrains.Annotations;

using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB
{
	/// <summary>
	/// Extension methods that expose CTE options on <see cref="ICteBuilder"/>.
	/// </summary>
	[PublicAPI]
	public static class CteBuilderExtensions
	{
		/// <summary>
		/// Sets the <c>MATERIALIZED</c> / <c>NOT MATERIALIZED</c> CTE hint.
		/// Recognized by PostgreSQL 12+, SQLite 3.35+, and ClickHouse 26.3+. Other providers silently ignore it.
		/// </summary>
		/// <param name="cteBuilder">Builder provided by the <c>AsCte</c> callback.</param>
		/// <param name="materialized">
		/// <see langword="true"/> to emit <c>AS MATERIALIZED</c>, <see langword="false"/> to emit <c>AS NOT MATERIALIZED</c>.
		/// </param>
		/// <returns>The same builder for chaining.</returns>
		public static ICteBuilder IsMaterialized(this ICteBuilder cteBuilder, bool materialized = true)
		{
			ArgumentNullException.ThrowIfNull(cteBuilder);

			((IAnnotatableBuilderInternal)cteBuilder).SetAnnotation(CteAnnotationNames.Materialized, materialized);

			return cteBuilder;
		}
	}
}
