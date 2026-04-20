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
		/// Recognized by PostgreSQL 12+, SQLite 3.35+, and ClickHouse 26.3+. Other providers silently drop the
		/// hint without tracing — if you rely on the hint for performance, be aware it is a no-op on those providers.
		/// </summary>
		/// <param name="cteBuilder">Builder provided by the <c>AsCte</c> callback.</param>
		/// <param name="materialized">
		/// <see langword="true"/> to emit <c>AS MATERIALIZED</c>, <see langword="false"/> to emit <c>AS NOT MATERIALIZED</c>.
		/// </param>
		/// <returns>The same builder for chaining.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="cteBuilder"/> is <see langword="null"/>.</exception>
		/// <exception cref="NotSupportedException">
		/// <paramref name="cteBuilder"/> does not implement <see cref="IAnnotatableBuilderInternal"/>.
		/// In-box builders always do; custom <see cref="ICteBuilder"/> implementations must opt in.
		/// </exception>
		public static ICteBuilder IsMaterialized(this ICteBuilder cteBuilder, bool materialized = true)
		{
			ArgumentNullException.ThrowIfNull(cteBuilder);

			if (cteBuilder is not IAnnotatableBuilderInternal annotatableBuilder)
				throw new NotSupportedException(
					$"The provided {nameof(ICteBuilder)} implementation ({cteBuilder.GetType().FullName}) does not support annotations. "
					+ $"Implement {nameof(IAnnotatableBuilderInternal)} to enable extensions such as {nameof(IsMaterialized)}.");

			annotatableBuilder.SetAnnotation(CteAnnotationNames.Materialized, materialized);

			return cteBuilder;
		}
	}
}
