using System;

using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider
{
	/// <summary>
	/// Provider-specific DML mechanics that can't be expressed through SQL generation alone: command-scenario
	/// construction (how one logical operation maps to ordered execution steps), physical grouping of those steps,
	/// and "table not found" detection. Resolved from the data context's service provider by query runners that execute DML.
	/// </summary>
	public interface IDmlService
	{
		/// <summary>
		/// Returns <see langword="true"/> if the given exception indicates the target table does not exist.
		/// Used by <c>DropTable</c> to decide whether a "not exists" suppression request should
		/// swallow a given error.
		/// </summary>
		bool IsTableNotFoundException(Exception exception);

		/// <summary>
		/// Builds the logical <see cref="SqlCommandScenario"/> (ordered steps + outcome) for <paramref name="statement"/>,
		/// or <see langword="null"/> to fall back to the legacy command-splitting path
		/// (<see cref="ISqlBuilder.CommandCount"/> / <c>BuildCommand</c>). Use <paramref name="factory"/> to construct any
		/// synthetic statements a step needs (e.g. an identity-retrieval <c>SELECT</c>).
		/// </summary>
		SqlCommandScenario? BuildCommandScenario(SqlStatement statement, SqlProviderFlags flags, ISqlExpressionFactory factory);

		/// <summary>
		/// Plans how a scenario's steps map to physical command groups (round-trips): contiguous non-gated steps may be
		/// combined into one command when <see cref="SqlProviderFlags.IsMultiStatementBatchSupported"/> is set.
		/// </summary>
		SqlCommandGroupPlan PlanScenario(SqlCommandScenario scenario, SqlProviderFlags flags);
	}
}
